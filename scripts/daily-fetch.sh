#!/usr/bin/env bash
# 每日自動抓取最新 TWSE 資料（營收 + 每日股價），upsert 進本機 DB。
# 由 launchd/cron 排程呼叫，也可手動執行：./scripts/daily-fetch.sh
#   - 自動確保 MSSQL 容器與 API 在跑（API 若由本腳本啟動，結束時收掉）
#   - 走 upsert，不重複建檔；不跑 init-db，避免清掉既有資料
set -uo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
mkdir -p logs
LOG="$ROOT/logs/daily-fetch.log"
exec >> "$LOG" 2>&1
echo "===== $(date '+%F %T') 每日抓取開始 ====="

# 解析能跑 net6 的 dotnet（同 dev.sh 邏輯）
DOTNET_BIN="dotnet"
if ! dotnet --list-sdks 2>/dev/null | grep -q '^6\.'; then
  if [ -x "$HOME/.dotnet/dotnet" ]; then DOTNET_BIN="$HOME/.dotnet/dotnet"; export DOTNET_ROOT="$HOME/.dotnet"; fi
fi

[ -f .env ] || { echo "✗ 缺 .env，略過"; exit 1; }
set -a; source .env; set +a
export ConnectionStrings__TwseRevenue="Server=localhost,1433;Database=TwseRevenue;User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True;Encrypt=False"

SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
db_ok() { docker exec twse-mssql "$SQLCMD" -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" >/dev/null 2>&1; }
api_ok() { [ "$(curl -s -o /dev/null -w '%{http_code}' --connect-timeout 2 http://localhost:5080/api/revenues/2330 2>/dev/null)" = "200" ]; }

# 1) 確保 MSSQL 在跑
if ! db_ok; then
  echo "啟動 MSSQL…"
  docker compose -f "$ROOT/docker-compose.yml" up -d mssql
  for _ in $(seq 1 30); do db_ok && break; sleep 2; done
fi
db_ok || { echo "✗ MSSQL 未就緒（Docker 沒開？），略過本次"; exit 1; }

# 2) 確保 API 在跑（若沒跑，背景啟動，結束時收掉）
STARTED_API=0
if ! api_ok; then
  echo "啟動 API…"
  ( cd "$ROOT/api" && "$DOTNET_BIN" run --no-launch-profile --project src/TwseRevenue.Api ) \
      >> "$ROOT/logs/daily-api.log" 2>&1 &
  API_PID=$!
  STARTED_API=1
  for _ in $(seq 1 40); do api_ok && break; sleep 2; done
fi
api_ok || { echo "✗ API 未就緒，略過本次"; [ "$STARTED_API" = 1 ] && kill "$API_PID" 2>/dev/null; exit 1; }

# 3) 匯入（upsert；可用 MONTHS 調整股價抓取月數，預設 1）
echo "▶ 匯入營收…"; python3 scripts/import-twse.py
echo "▶ 匯入股價…"; MONTHS="${MONTHS:-1}" python3 scripts/import-quotes.py

# 4) 若 API 由本腳本啟動，收掉（不打擾你手動開的服務）
[ "$STARTED_API" = 1 ] && { echo "收掉自動啟動的 API"; kill "$API_PID" 2>/dev/null; }
echo "===== $(date '+%F %T') 完成 ====="

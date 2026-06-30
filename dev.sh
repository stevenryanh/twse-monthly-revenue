#!/usr/bin/env bash
# TWSE 每月營收系統 —— 一鍵開發啟動器（比照 團隊 dev.sh 慣例）。
# 先清 port → 啟 DB → 啟 API → 啟 Web，日誌寫入 logs/，最後印出各服務網址。
#   ./dev.sh          # 啟動 DB + API + Web
#   ./dev.sh stop     # 停掉 API / Web（MSSQL 容器保留資料）
#   ./dev.sh restart  # 先停再啟

set -euo pipefail
ROOT="$(cd "$(dirname "$0")" && pwd)"

API_PORT=5080
WEB_PORT=5173

stop_all() {
    echo "▶ 停止 API / Web…"
    for port in "$API_PORT" "$WEB_PORT"; do
        pids=$(lsof -ti :"$port" 2>/dev/null || true)
        if [ -n "$pids" ]; then
            echo "  killing port $port (PID $pids)"
            echo "$pids" | xargs kill -9 2>/dev/null || true
        fi
    done
    echo "  ✓ 已停止（MSSQL 容器保留；如需停用：docker compose down）"
}

start_all() {
    mkdir -p "$ROOT/logs"

    command -v docker >/dev/null 2>&1 || { echo "✗ 需要 Docker"; exit 1; }
    if [ ! -f "$ROOT/.env" ]; then
        echo "✗ 缺少 .env，請先：cp .env.example .env 並填入 MSSQL_SA_PASSWORD"
        exit 1
    fi

    # .env 是唯一密碼來源：載入後組出 API 連線字串，以環境變數注入（dotnet 子行程自動繼承，
    # .NET 設定原生讀 ConnectionStrings__TwseRevenue，免改任何程式碼）。
    set -a; source "$ROOT/.env"; set +a
    export ConnectionStrings__TwseRevenue="Server=localhost,1433;Database=TwseRevenue;User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True;Encrypt=False"

    echo "▶ 啟動 MSSQL…"
    docker compose -f "$ROOT/docker-compose.yml" up -d mssql

    echo "▶ 初始化資料庫（建表 + 預存程序）…"
    if (cd "$ROOT" && bash scripts/init-db.sh) > "$ROOT/logs/init-db.log" 2>&1; then
        echo "  ✓ 資料庫就緒"
    else
        echo "  ✗ 初始化失敗，見 logs/init-db.log"; tail -n 15 "$ROOT/logs/init-db.log"; exit 1
    fi

    echo "▶ 啟動 API（:$API_PORT）…"
    ( cd "$ROOT/api" && dotnet run --no-launch-profile --project src/TwseRevenue.Api ) \
        > "$ROOT/logs/api.log" 2>&1 & echo "  API  PID $! → logs/api.log"

    echo "▶ 啟動 Web（:$WEB_PORT）…"
    ( cd "$ROOT/web" && { [ -d node_modules ] || npm install; } && npm run dev ) \
        > "$ROOT/logs/web.log" 2>&1 & echo "  Web  PID $! → logs/web.log"

    echo ""
    echo "✅ 全部啟動。看日誌：tail -f logs/*.log"
    echo ""
    echo "   Web     : http://localhost:$WEB_PORT"
    echo "   API     : http://localhost:$API_PORT"
    echo "   Swagger : http://localhost:$API_PORT/swagger"
    echo ""
    echo "   提醒：API 連線字串走 User Secrets，首次請見 README「啟動」步驟 3。"
}

case "${1:-start}" in
    stop)    stop_all ;;
    restart) stop_all; start_all ;;
    start|*) stop_all; start_all ;;
esac

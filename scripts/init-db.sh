#!/usr/bin/env bash
# 將 db/*.sql 依序套用到 docker 中的 MSSQL：建立資料表 → 預存程序。
# 用法：先 `docker compose up -d mssql`，待健康後執行 `bash scripts/init-db.sh`。
set -euo pipefail

# 自 .env 載入 MSSQL_SA_PASSWORD（與 docker compose 同一來源，不寫死於腳本）
if [[ -f .env ]]; then
  set -a; source .env; set +a
fi

CONTAINER="twse-mssql"
PASSWORD="${MSSQL_SA_PASSWORD:?未設定 MSSQL_SA_PASSWORD，請先複製 .env.example 為 .env 並填入}"
SQLCMD="/opt/mssql-tools18/bin/sqlcmd"

echo "等待 SQL Server 就緒..."
for _ in $(seq 1 40); do
  if docker exec "$CONTAINER" "$SQLCMD" -S localhost -U sa -P "$PASSWORD" -C -Q "SELECT 1" >/dev/null 2>&1; then
    break
  fi
  sleep 2
done

for f in db/01_schema.sql db/02_stored_procedures.sql; do
  echo "套用 $f ..."
  docker cp "$f" "$CONTAINER:/tmp/$(basename "$f")"
  docker exec "$CONTAINER" "$SQLCMD" -S localhost -U sa -P "$PASSWORD" -C -f 65001 -i "/tmp/$(basename "$f")"
done

echo "資料庫初始化完成。"

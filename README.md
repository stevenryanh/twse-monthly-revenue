# 上市公司每月營業收入查詢系統

從政府資料開放平臺「上市公司每月營業收入彙總表」(臺灣證券交易所 OpenAPI `t187ap05_L`)取得資料,提供以**公司代號查詢**的前後端分離系統。

> 👉 **第一次來?** 先看 [**面試導覽 — 5 分鐘進入狀況**](docs/ONBOARDING.md):一行啟動 + 系統全貌 + 設計亮點。

## 架構

前後端分離,每層職責單一:

```
db/    MSSQL：資料表 + 預存程序（手寫 schema，非 Code-First）
api/   .NET 6 Web API，Clean Architecture 四層
web/   Vue 3 (Vite) 前端：代號查詢 + 表格 + ECharts 趨勢圖
```

API 內部四層(對齊團隊既有微服務慣例):

| 層 | 職責 |
|---|---|
| `Domain` | 純資料模型,無相依 |
| `Application` | MediatR Query/Command、DTO、手寫映射、輸入驗證 |
| `Infrastructure` | ADO.NET 參數化呼叫預存程序 |
| `Api` | Controller、全域 ExceptionFilter、Serilog、Swagger |

## 技術棧

| 範圍 | 技術 |
|---|---|
| DB | MSSQL 2022 (Docker)、預存程序 |
| API | .NET 6、MediatR、ADO.NET (`Microsoft.Data.SqlClient`)、Serilog、Swagger、xUnit + Moq |
| Web | Vue 3 + Vite、ECharts（按需匯入）、原生 `fetch` |

## 啟動

前置:Docker、.NET 6 SDK、Node。

```bash
# 1. 設定密碼（不入庫，單一來源）：複製範本後填入強密碼
cp .env.example .env

# 2. 一鍵啟動 DB + API + Web（會自動建表、注入連線字串、印出各網址）
./dev.sh
#    停止：./dev.sh stop ｜ 重啟：./dev.sh restart

# 3. 匯入資料（從 TWSE OpenAPI t187ap05_L，依主鍵 upsert，可重跑）
python3 scripts/import-twse.py            # 全部上市公司
python3 scripts/import-twse.py 2330 1101  # 只匯指定代碼
python3 scripts/import-twse.py 0050       # 0050 自動展開為台灣50成分股

# 3b.（買賣投報排行用）匯入一個月每日股價（STOCK_DAY）
python3 scripts/import-quotes.py          # 預設 0050（ETF）+ 成分股，當月每日

# 測試：cd api && dotnet test
```

啟動後:前端 http://localhost:5173 ｜ Swagger http://localhost:5080/swagger 。

> 📌 `0050` 的成分股為**人工維護的快照**(擷取日 2026-06-30,見 `scripts/import-twse.py` 的 `TW50`)。成分每季審核調整,過期時手動更新清單即可;選快照而非即時抓取,是為了不依賴會變動/失效的外部成分股來源、保持可重現。ETF 本身無每月營收,故指定 `0050` 時改餵其成分股。

> 💡 **開發站台**:用瀏覽器開專案根目錄的 [`twse-sites.html`](twse-sites.html)(`file://` 直接開,免啟服務),一頁集中所有服務連結、API 測試 curl(GET/POST)、常用指令與 DB 查詢——給第一次接觸這個專案的人快速上手。

> **機密管理**(詳 [decisions/005](docs/decisions/005-secret-management.md)):連線字串/密碼一律不入庫。
> - **單一來源** `.env`(git 忽略,範本見 `.env.example`):同時餵 docker compose、`init-db.sh`,並由 `./dev.sh` 組出 API 連線字串、以環境變數 `ConnectionStrings__TwseRevenue` 注入。
> - **本機只需** `cp .env.example .env` 一次,之後 `./dev.sh` 全自動;不必每次手動設定機密。
> - **SIT/UAT/PROD**:由 CI/CD 或 Secret Manager 注入同名環境變數,開發者不接觸高階環境密碼。
> - 不走 `dev.sh` 而想直接 `dotnet run` 時,可改用 `dotnet user-secrets` 設定 `ConnectionStrings:TwseRevenue`。
>
> 前端預設打 `http://localhost:5080`，可複製 `web/.env.example` 為 `web/.env` 後以 `VITE_API_BASE` 覆寫。
> 開發環境的 API 已放行 `http://localhost:5173` 的 CORS（來源清單見 `api/.../appsettings.json` 的 `Cors:AllowedOrigins`）。

## API

| 方法 | 路徑 | 說明 |
|---|---|---|
| GET | `/api/revenues/{companyCode}` | 以公司代號查各月營收(最新月份在前) |
| POST | `/api/revenues` | 新增一筆(依主鍵 upsert) |
| GET | `/api/companies?q=` | 關鍵字搜尋公司(代號或名稱的一部分),供自動完成 |
| POST | `/api/quotes` | 新增一筆每日行情(依主鍵 upsert) |
| GET | `/api/quotes/ranking` | 買賣投報排行;參數 `q`(代號/名稱片段)、`codes`(逗號分隔)、`sort`(return\|sharpe\|volatility\|avg\|daily)、`dir`(asc\|desc)、`maxPrice`(小資總預算÷1000 的每股價上限)、`top` |

> 買賣投報以「每元當日報酬 = 漲跌 ÷ 昨收」為基礎,於近一個月每日行情(STOCK_DAY)彙總:
> 期間累計報酬、報酬波動(變量,標準差)、平均日報酬、最近一日報酬。皆走參數化預存程序。

## 安全

- 資料存取只走**參數化預存程序**,從資料層杜絕 SQL Injection。
- `Directory.Build.props` 將 `CA2100`(SQL 注入)升為**編譯錯誤**,作為靜態第二道防線。
- **`ExceptionHandlingMiddleware`** 統一錯誤處理:驗證錯誤 → 400,其餘 → 500 且不外洩內部細節、回傳 `traceId`、以結構化 log code 完整記錄。
- **`CorrelationIdMiddleware` + 結構化 log code**:每請求一組 `X-Correlation-ID` 貫穿日誌(前端對稱注入),代碼字典見 [`docs/LOG-CODES.md`](docs/LOG-CODES.md)。
- **`SecurityHeadersMiddleware`**:統一基礎安全回應標頭。
- **輸入驗證**收斂為 MediatR `ValidationBehavior`(接縫式,不散落 handler)。
- **機密不入庫**:連線字串/密碼走 `.env`(已忽略)與環境變數 / User Secrets;repo 內無任何明文憑證。
- **CI 硬門檻**([`.github/workflows/ci.yml`](.github/workflows/ci.yml)):gitleaks 全歷史機密掃描、`dotnet test`、前端建置、LOG 代碼字典 `--check` 防漂移。本地 `.githooks` 是防呆,CI 才是硬把關。
- 接縫式、可水平擴充的擴充性設計詳 [decisions/006](docs/decisions/006-security-extensibility.md)。

> LOG 代碼字典 `docs/LOG-CODES.md` 由 `scripts/gen-log-codes.py` 從 `TwseLogCodes.cs` **產生**(單一真相來源、免手寫漂移);改代碼後重跑產生器,CI 以 `--check` 把關。

## 設計決策

關鍵取捨與「為什麼不那樣做」記錄於 [`docs/decisions`](docs/decisions/)。

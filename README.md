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

# 測試：cd api && dotnet test
```

啟動後:前端 http://localhost:5173 ｜ Swagger http://localhost:5080/swagger 。

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

## 安全

- 資料存取只走**參數化預存程序**,從資料層杜絕 SQL Injection。
- `Directory.Build.props` 將 `CA2100`(SQL 注入)升為**編譯錯誤**,作為靜態第二道防線。
- **`ExceptionHandlingMiddleware`** 統一錯誤處理:驗證錯誤 → 400,其餘 → 500 且不外洩內部細節、回傳 `traceId`、以結構化 log code 完整記錄。
- **`CorrelationIdMiddleware` + 結構化 log code**:每請求一組 `X-Correlation-ID` 貫穿日誌(前端對稱注入),代碼字典見 [`docs/LOG-CODES.md`](docs/LOG-CODES.md)。
- **`SecurityHeadersMiddleware`**:統一基礎安全回應標頭。
- **輸入驗證**收斂為 MediatR `ValidationBehavior`(接縫式,不散落 handler)。
- **機密不入庫**:連線字串/密碼走 `.env`(已忽略)與環境變數 / User Secrets;repo 內無任何明文憑證。
- 接縫式、可水平擴充的擴充性設計詳 [decisions/006](docs/decisions/006-security-extensibility.md)。

## 設計決策

關鍵取捨與「為什麼不那樣做」記錄於 [`docs/decisions`](docs/decisions/)。

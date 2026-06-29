# 上市公司每月營業收入查詢系統

從政府資料開放平臺「上市公司每月營業收入彙總表」(臺灣證券交易所 OpenAPI `t187ap05_L`)取得資料,提供以**公司代號查詢**的前後端分離系統。

## 架構

前後端分離,每層職責單一:

```
db/    MSSQL：資料表 + 預存程序（手寫 schema，非 Code-First）
api/   .NET 6 Web API，Clean Architecture 四層
web/   Vue 3 (Vite) 前端    ← 開發中
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
| Web | Vue 3 + Vite |

## 啟動

前置:Docker、.NET 6 SDK。

```bash
# 1. 啟動資料庫
docker compose up -d mssql

# 2. 初始化資料表與預存程序
bash scripts/init-db.sh

# 3. 啟動 API（http://localhost:5080，Swagger 於 /swagger）
cd api && dotnet run --no-launch-profile --project src/TwseRevenue.Api

# 4. 執行測試
cd api && dotnet test
```

## API

| 方法 | 路徑 | 說明 |
|---|---|---|
| GET | `/api/revenues/{companyCode}` | 以公司代號查各月營收(最新月份在前) |
| POST | `/api/revenues` | 新增一筆(依主鍵 upsert) |

## 安全

- 資料存取只走**參數化預存程序**,從資料層杜絕 SQL Injection。
- `Directory.Build.props` 將 `CA2100`(SQL 注入)升為**編譯錯誤**,作為靜態第二道防線。
- 全域 `ExceptionFilter` 統一錯誤處理:驗證錯誤 → 400,其餘 → 500 且不外洩內部細節、完整記錄 Log。

## 設計決策

關鍵取捨與「為什麼不那樣做」記錄於 [`docs/decisions`](docs/decisions/)。

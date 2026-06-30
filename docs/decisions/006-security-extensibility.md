# 006. 抗未知資安政策的架構風格:把變動關進「接縫」,並可水平擴充

2026-06,適用:api / web / 全域

## 背景
未來會面對什麼資安政策無法預測(可能要 authn/authz、稽核軌跡、欄位加密、rate limit、PII 遮罩、CSP/HSTS…),且系統要能**水平擴充**。需求是:**讓未知的政策未來能被人類輕鬆接手、且只改一處**,多實例部署也不掉鏈子,而不是現在就把想像中的需求全做上去(那是過度設計)。

## 決定
採 **Clean Architecture / Ports & Adapters**,並刻意讓每一類資安政策都有**單一著陸點(seam)**。各政策對應的接縫:

| 未來政策 | 單一著陸點 | 現況 |
|---|---|---|
| 機密來源變更(Key Vault、輪替) | 設定/機密層(`dev.sh` 注入 + `appsettings` 抽象) | 已就位(005) |
| SQL 注入規範 | 資料存取層(`IRevenueRepository` + 參數化 sp + CA2100) | 已就位(002/003) |
| 輸入/欄位驗證政策 | **`IValidator<T>` + `ValidationBehavior`(MediatR pipeline)** | 本決策建立 |
| authn/authz、rate limit、安全標頭 | **HTTP middleware pipeline** | 安全標頭已示範(`SecurityHeadersMiddleware`) |
| 錯誤不外洩、結構化稽核 | **`ExceptionHandlingMiddleware` + Serilog + 結構化 log code** | 本決策重構 |
| 跨服務追蹤、稽核軌跡 | **`CorrelationIdMiddleware`**(沿用上游 `X-Correlation-ID`)+ 前端對稱注入 | 本決策建立 |

落地的接縫:
- **驗證集中於 pipeline**:驗證是橫切關注點,移出 handler、收斂到 `ValidationBehavior`;每個 Request 的規則放在自己的 `IValidator<T>` 實作。handler 只負責協調。
- **例外處理重構為 middleware**:由 MVC `ExceptionFilter` 改為 `ExceptionHandlingMiddleware`,涵蓋 MVC 以外的管線錯誤;驗證錯誤→400、其餘→500 不外洩內部細節並回 `traceId`,以結構化 log code(見 `TwseLogCodes` / `docs/LOG-CODES.md`)完整記錄。
- **關聯 ID 貫穿日誌**:`CorrelationIdMiddleware` 指派/沿用 `X-Correlation-ID`,推入 Serilog LogContext;**前端 `web/src/api/http.js` 對稱注入同一標頭**,達成端到端追蹤。
- **安全標頭集中於 middleware**:`SecurityHeadersMiddleware` 是未來標頭政策(CSP/HSTS…)的唯一落點。

## 水平擴充
上述 middleware/behavior/validator **皆無實例本地共享狀態**(關聯 ID 為 per-request),狀態只在 DB;搭配 upsert 冪等(002)與機密外部注入(005),多實例部署於負載平衡後可水平擴充而不掉鏈子。`CorrelationIdMiddleware` 沿用上游傳入的關聯 ID,跨實例/閘道追蹤不斷鏈。

## 排除了什麼
- **現在就把 OAuth/稽核/加密全做上去**:沒有實際需求,屬過度設計;只保留「接縫存在」即可,需求到了再填。
- **引入 FluentValidation**:為目前少量規則多一個相依與一套 DSL,與本專案「手寫優先、零多餘相依」(003)相違;改用 ~30 行手寫 `IValidator<T>` 接縫達同效。
- **把驗證留在各 handler**:會讓未來的輸入政策散落多處,違反「單一著陸點」。
- **正式環境靠 CORS 放行跨來源**:CORS 僅開發用(004);正式應同源部署。

## 改動前必須想清楚的
- 新增 Command/Query 時:要驗證就加一個 `IValidator<該Request>` 並註冊,**不要**把驗證寫回 handler。
- 加任何資安政策前先問:「它該落在哪個既有接縫?」若找不到接縫,先建立接縫(一個 behavior/middleware/抽象介面),再放實作 —— 不要散落。
- `ValidationBehavior` 對「沒有 validator 的 Request」自動跳過;別預設所有 Request 都被驗證。
- 安全標頭/中介層順序會影響行為(例如 CORS、驗證、例外處理的先後);調整 pipeline 順序時要一併想清楚。

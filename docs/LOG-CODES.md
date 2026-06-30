# 日誌代碼字典（LOG Codes）

> ⚠️ 本檔由 `scripts/gen-log-codes.py` 從 `api/src/TwseRevenue.Application/Logging/TwseLogCodes.cs`
> 產生，**請勿手改**；改代碼或描述請改該 C# 檔後重跑產生器（CI 以 `--check` 把關，防漂移）。

每條日誌都以一個結構化代碼開頭，便於分類、grep、跨服務一致。
**API caller / 維運人員**在日誌或錯誤回應看到代碼時，可在此查回正體中文意義。

代碼格式：`{Level}{Layer}{Seq}`
- **Level**：`I`=Info、`W`=Warning、`E`=Error、`D`=Debug
- **Layer**：`1`=Api/Presentation、`3`=Application、`5`=Infrastructure、`6`=CrossCutting

## Application（layer 3）

| 代碼 | Level | 常數 | 意義 |
|---|---|---|---|
| `I301` | Info | `Upserted` | 營收資料寫入（upsert）成功，含 CompanyCode/DataYearMonth |
| `I302` | Info | `Queried` | 營收查詢完成，含 CompanyCode/回傳筆數 Count |
| `I303` | Info | `CompaniesSearched` | 公司關鍵字搜尋完成，含 Keyword/回傳筆數 Count |
| `I304` | Info | `Upserted` | 每日行情寫入（upsert）成功，含 CompanyCode/TradeDate |
| `I305` | Info | `Ranked` | 買賣投報排行完成，含 Sort/Keyword/回傳筆數 Count |

## CrossCutting（layer 6）

| 代碼 | Level | 常數 | 意義 |
|---|---|---|---|
| `E602` | Error | `Unhandled` | 未處理的例外（回 500，不外洩內部細節、帶 traceId） |
| `W601` | Warning | `ValidationFailed` | 輸入驗證失敗（回 400，訊息含 Method/Path/Reason） |

## 追蹤碼（CorrelationId / traceId）

每個請求由 `CorrelationIdMiddleware` 指派一組 `X-Correlation-ID`（沿用上游傳入或新生），
寫回回應標頭、並貫穿該請求的所有日誌。500 錯誤回應另含 `traceId` 欄位。
**回報問題時附上此碼**，即可在日誌精準定位該次請求的完整軌跡（跨服務不斷鏈）。

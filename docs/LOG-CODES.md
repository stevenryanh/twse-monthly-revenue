# 日誌代碼字典（LOG Codes）

每條日誌都以一個結構化代碼開頭（精神比照 團隊 的 `結構化日誌代碼`），便於分類、grep、與跨服務一致。
**API caller / 維運人員**在日誌或錯誤回應看到代碼時，可在此查回正體中文意義。

代碼格式：`{Level}{Layer}{Seq}`
- **Level**：`I`=Info、`W`=Warning、`E`=Error、`D`=Debug
- **Layer**：`1`=Api/Presentation、`3`=Application、`5`=Infrastructure、`6`=CrossCutting

> 來源：`api/src/TwseRevenue.Application/Logging/TwseLogCodes.cs`（程式內常數）。新增代碼時請同步更新本表。

## CrossCutting（layer 6）— 全域例外處理

| 代碼 | Level | 意義 | 觸發時機 |
|---|---|---|---|
| `W601` | Warning | 輸入驗證失敗 | 請求未通過 `IValidator`，回 400；訊息含 Method/Path/Reason |
| `E602` | Error | 未處理的例外（伺服器內部錯誤） | 非預期例外，回 500；訊息含 Method/Path/CorrelationId，回應帶 `traceId` |

## Application（layer 3）— 營收用例

| 代碼 | Level | 意義 | 觸發時機 |
|---|---|---|---|
| `I301` | Info | 營收資料寫入（upsert）成功 | `POST /api/revenues` 成功；含 CompanyCode/DataYearMonth |
| `I302` | Info | 營收查詢完成 | `GET /api/revenues/{code}` 完成；含 CompanyCode/回傳筆數 Count |

## 追蹤碼（CorrelationId / traceId）

每個請求由 `CorrelationIdMiddleware` 指派一組 `X-Correlation-ID`（沿用上游傳入或新生），
寫回回應標頭、並貫穿該請求的所有日誌。500 錯誤回應另含 `traceId` 欄位。
**回報問題時附上此碼**，即可在日誌精準定位該次請求的完整軌跡（跨服務不斷鏈）。

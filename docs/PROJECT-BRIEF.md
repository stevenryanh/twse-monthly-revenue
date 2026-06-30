# 專案簡介 —— 上市公司每月營收查詢系統

> **用途**：候選人技術能力展示專案。一套**完整可運行**的前後端分離系統，
> 從政府開放資料（臺灣證券交易所 OpenAPI `t187ap05_L`）取得上市公司每月營收，提供以**公司代號查詢**。
> 重點不只在「能動」，而在**工程品質與每個設計取捨背後的「為什麼」皆有文件可查**。

---

## 一分鐘看懂

輸入公司代號（如 `2330`）→ 查出各月營收 → 以**表格 + 趨勢圖**呈現。

| 範圍 | 內容 |
|---|---|
| **資料庫** | MSSQL，手寫資料表 + 預存程序（非 Code-First） |
| **後端 API** | .NET 6，Clean Architecture 四層；ADO.NET 參數化呼叫預存程序 |
| **前端** | Vue 3 + Vite + ECharts |
| **品質** | 單元測試、CI（建置／測試／機密掃描）、設計決策文件 |

---

## 這個專案展示的能力

| 能力面向 | 具體做到 | 可驗證之處 |
|---|---|---|
| **架構設計** | Clean Architecture 四層（Domain／Application／Infrastructure／Api），職責單一、相依方向乾淨 | `api/src/` |
| **資料安全** | 只透過**參數化預存程序**存取，從資料層杜絕 SQL Injection；再把 `CA2100`（注入分析規則）升為**編譯錯誤**當靜態第二道防線 | `db/`、`api/Directory.Build.props` |
| **機密管理** | 連線字串／密碼**一律不入庫**：單一來源 `.env`（git 忽略）+ 環境變數注入；各環境隔離；CI 以 gitleaks 掃全歷史把關 | `docs/decisions/005`、`.github/workflows/ci.yml` |
| **可擴充性** | 「接縫式」架構：輸入驗證收斂為 MediatR pipeline behavior、跨切面（例外處理／關聯 ID／安全標頭）走 middleware；未知政策未來只改一處 | `docs/decisions/006` |
| **可觀測性** | 結構化日誌代碼（可分類／grep）+ `X-Correlation-ID` 端到端追蹤（前後端對稱）；錯誤回應帶 `traceId` 不外洩內部細節 | `docs/LOG-CODES.md` |
| **可水平擴充** | 服務無實例本地狀態、寫入冪等（upsert）、機密外部注入；多實例部署不掉鏈子 | `docs/decisions/006` |
| **品質保證** | Handler／驗證器／middleware 單元測試（xUnit + Moq）；CI 一鍵建置＋測試＋掃描 | `api/tests/`、`.github/workflows/ci.yml` |
| **工程文化** | 每個重要取捨都留一篇「**為什麼這樣、以及為什麼不那樣**」的設計決策文件 | `docs/decisions/` |

---

## 5 分鐘跑起來（給技術窗口）

前置：Docker、.NET 6 SDK、Node。

```bash
cp .env.example .env      # 填入資料庫密碼，只需一次
./dev.sh                  # 一鍵啟動 DB + API + 前端，並印出各網址
```

| 入口 | 網址 |
|---|---|
| 前端（查詢頁） | http://localhost:5173 |
| API 文件（Swagger） | http://localhost:5080/swagger |

| 方法 | 路徑 | 說明 |
|---|---|---|
| `GET` | `/api/revenues/{companyCode}` | 以公司代號查各月營收（最新月份在前） |
| `POST` | `/api/revenues` | 新增一筆（依主鍵 upsert，匯入可重跑） |

---

## 想深入了解

| 文件 | 內容 |
|---|---|
| [`docs/ONBOARDING.md`](ONBOARDING.md) | 5 分鐘導覽：系統全貌、一個請求怎麼流動、值得討論的設計點 |
| [`docs/decisions/`](decisions/README.md) | 設計決策總覽（資料庫、預存程序、資料存取、前端、機密、擴充性） |
| [`docs/LOG-CODES.md`](LOG-CODES.md) | 日誌代碼字典：維運／呼叫端可由代碼查回意義 |

---

## 關於這份專案

這是一套**為展示工程能力而建、且完整可運行**的系統——刻意涵蓋了真實專案會遇到的面向：
資料正確性、SQL 注入防護、機密管理、輸入驗證、錯誤處理、可觀測性、自動化測試與 CI、以及可維護／可擴充的架構。
程式碼之外，更重視**把「為什麼這樣做、為什麼不那樣做」寫清楚**——這正是接手與長期維護一個系統時，最稀缺也最關鍵的能力。

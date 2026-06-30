# 面試導覽 —— 5 分鐘進入狀況

> 上市公司每月營收查詢系統。前後端分離,以**公司代號查詢**各月營收,含表格與趨勢圖。
> 設計取向:**不過度抽象、每個取捨都留下「為什麼」**。

---

## Quick Start（一行啟動）

```bash
cp .env.example .env      # 填入 MSSQL_SA_PASSWORD（強密碼），只需一次
./dev.sh                  # 啟 DB + API + Web，自動建表、注入連線字串、印出各網址
```

| 入口 | 網址 |
|---|---|
| 前端（查詢頁） | http://localhost:5173 |
| API Swagger | http://localhost:5080/swagger |

> 💡 **懶人入口**:用瀏覽器開根目錄的 [`twse-sites.html`](../twse-sites.html)(`file://` 直接開,免啟服務),一頁集中服務連結、API 測試 curl、常用指令與 DB 查詢。
> 連線字串不入庫,由 `./dev.sh` 從 `.env` 自動注入(機制詳 [decisions/005](decisions/005-secret-management.md))。
> 前置:Docker、.NET 6 SDK、Node。

---

## 這個系統做什麼

輸入公司代號（如 `2330`）→ 查出該公司各月營收,以表格(最新月在前)與 ECharts 趨勢圖呈現。

| 看點 | 在哪 | 為何值得看 |
|---|---|---|
| **四層 Clean Architecture** | `api/src/` | Domain / Application / Infrastructure / Api 職責單一,對齊團隊既有微服務慣例 |
| **手寫 schema + 預存程序** | `db/` | 非 Code-First;資料存取**全程參數化**,從資料層杜絕 SQL Injection |
| **CA2100 升為編譯錯誤** | `api/Directory.Build.props` | SQL 注入靜態第二道防線,build 時就擋 |
| **機密不入庫** | `.env` / User Secrets | repo 內無任何明文連線字串或密碼 |
| **設計決策文件** | `docs/decisions/` | 每個重要取捨與「為什麼不那樣做」都有紀錄 |
| **單元測試** | `api/tests/` | Handler 與映射的 xUnit + Moq 測試 |

---

## 一個查詢請求怎麼流動

```
GET /api/revenues/2330
  → RevenuesController            api/.../Api/Controllers/RevenuesController.cs
  → MediatR Query                 Application/Queries/GetRevenueByCompanyCode/
  → Handler 呼叫 Repository
  → RevenueRepository             Infrastructure/Persistence/RevenueRepository.cs
  → ADO.NET 參數化呼叫預存程序     db/02_stored_procedures.sql
  → Entity → DTO 手寫映射          Application/Mapping/RevenueMapping.cs
  → 回傳 MonthlyRevenueDto[]（最新月在前）
```

前端 `web/src/App.vue` 呼叫此端點,交給 `RevenueTable.vue`（表格）與 `RevenueChart.vue`（ECharts）。

---

## demo 時值得講的點（talking points）

- **「為什麼不用 EF / AutoMapper?」** — schema 與預存程序都是手寫,全系統只有兩支程序;為兩支 sp 引入整套 EF 是方向相反的過重抽象。詳 [decisions/003](decisions/003-data-access-ado-not-ef.md)。
- **「SQL Injection 怎麼防?」** — 兩道防線:資料層只走參數化預存程序;再把 `CA2100` 升為**編譯錯誤**,靜態擋下任何字串拼接 SQL。
- **「密碼怎麼管?」** — 連線字串/密碼一律不入庫:DB 密碼放 `.env`(git 忽略),API 連線字串走 .NET User Secrets / 環境變數。
- **「為什麼前端不用 axios、ECharts 不整包匯入?」** — 只有一支端點,原生 `fetch` 足夠;ECharts 按需匯入讓 bundle 砍半。詳 [decisions/004](decisions/004-frontend-stack-and-cors.md)。
- **「要分享給別人看,為什麼用 Tailscale 內網而不是公開出去?」** — 這是無認證、含寫入 API 的 dev server,**不能** Funnel/ngrok 公開曝險;改用 Tailscale 私有網路 + 節點共享,只授權可信對象、各自帳號,前端配合走同源 `/api` 代理讓單一網址即通。是個「依對象範圍與系統現況選對曝光面」的資安取捨。詳 [decisions/007](decisions/007-tailscale-sharing.md)。

---

## 想深入時的下一站

- 設計取捨總覽 → [`docs/decisions/`](decisions/README.md)
- 抗未知資安政策的接縫式架構（驗證 behavior、例外/關聯ID/安全標頭 middleware、可水平擴充）→ [decisions/006](decisions/006-security-extensibility.md)
- 日誌代碼字典（看到代碼查正體中文意義）→ [`docs/LOG-CODES.md`](LOG-CODES.md)
- 完整啟動與機密設定 → [`README.md`](../README.md)

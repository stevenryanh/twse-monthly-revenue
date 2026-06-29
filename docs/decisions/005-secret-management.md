# 005. 機密管理:不入庫、單一來源、各環境隔離,且不阻礙日常開發

2026-06,適用:db / api / web / ops

## 背景
連線字串與 DB 密碼是機密。團隊提出「DEV/SIT/UAT 各用不同且互不流通的密碼」,問題是:這樣放進設定檔安全嗎?同時希望**安全與開發方便並存**,不要每次跑都手動設定機密。

釐清出兩條獨立的軸線,常被混為一談:
- **爆炸半徑**:每環境不同密碼 → 一個外洩碰不到別的環境。✅ 好實踐,保留。
- **外洩管道**:密碼有沒有進版控。這跟「密碼是否不同」無關 —— 凡是 commit 進 repo,任何有 repo 讀取權者(外包、面試官、repo 外流)即取得憑證,且 git 歷史**永久可回溯**,rotate 也清不掉舊值。

## 決定
1. **機密一律不入庫。** `appsettings.json` 不含任何密碼;repo 內無明文憑證。
2. **單一來源 `.env`(git 忽略,範本 `.env.example`)。** 同一份 `MSSQL_SA_PASSWORD` 餵三處:
   - docker compose(原生讀 `.env`)
   - `scripts/init-db.sh`(開頭 `source .env`)
   - API:由 `./dev.sh` 組出連線字串,以環境變數 `ConnectionStrings__TwseRevenue` 注入(`dotnet` 子行程繼承,.NET 設定原生讀取,**免改任何程式碼**)。
3. **各環境隔離靠分層設定,不靠把密碼分散到多個入庫檔。**
   - 本機 DEV:`.env`(開發者只需 `cp .env.example .env` 一次)。
   - SIT/UAT/PROD:由 CI/CD 或 Secret Manager 注入同名環境變數 `ConnectionStrings__TwseRevenue`,自動覆寫 `appsettings.json`;**同一份程式碼**跑不同環境吃不同來源,開發者不接觸高階環境密碼。
4. **方便性**:一次 `cp` + 之後 `./dev.sh`,不需每次手動 `dotnet user-secrets`/設環境變數;缺機密時 API 丟出**可操作的錯誤訊息**(指向 `cp .env.example .env` 與 `./dev.sh`)。
5. **防呆網(選配)**:`.githooks/pre-commit` 軟性呼叫 `gitleaks`(裝了才掃,沒裝不擋),讓人「隨手 commit」也不怕誤推機密。

## 排除了什麼
- **把密碼 commit 進 `appsettings.{Env}.json`**:即使每環境不同,仍違反「能看程式碼 ≠ 能碰資料」與合規(SOC2/ISO27001/PCI 禁止 source control 內含明文憑證),且歷史永久殘留。
- **強制 `dotnet user-secrets` 為唯一本機路徑**:安全(存使用者目錄、天生不入 repo),但每位開發者每台機要手動設定,與「方便」目標相違;保留為「不走 dev.sh 直接 dotnet run」時的替代。
- **在 API 內引入 `DotNetEnv` 之類套件自動載入 `.env`**:可行,但多一個相依;改由 `./dev.sh` 以環境變數注入即可達到同效,且不動到應用程式碼(與本專案「無多餘」原則一致)。
- **PROD 也用 `.env`**:正式環境應走 Secret Manager(Key Vault 等)+ 自動 rotate,不放任何設定檔。

## 改動前必須想清楚的
- 若有人提議「把某環境密碼放進入庫設定檔以求方便」:先問外洩管道 —— 它會讓 repo 讀取權 = 該環境 DB 存取權,且永久留痕。方便要靠 `.env`/注入,不是靠入庫。
- 若 `.env` 不慎被 commit:它在歷史中永久存在,需改寫歷史 + rotate 密碼;`pre-commit` 掃描就是為了從源頭避免這件事。
- 新增任何「從環境讀機密」的程式路徑時:確認 PROD 不會 fallback 去讀 `.env` 或任何入庫預設值。

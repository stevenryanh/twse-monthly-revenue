# Design Decisions

記錄本專案做過的重要設計選擇,以及「為什麼不那樣做」。不是規格書,是「為什麼這樣,以及改動前要想清楚什麼」。

| # | 標題 | 適用 |
|---|------|------|
| [001](001-schema-key-and-types.md) | 資料表主鍵與欄位型別 | db / api |
| [002](002-stored-procedure-upsert.md) | upsert 預存程序 + 全程參數化 | db / api |
| [003](003-data-access-ado-not-ef.md) | ADO.NET 直呼 sp,不用 EF / AutoMapper | api |
| [004](004-frontend-stack-and-cors.md) | 前端技術選擇與開發環境 CORS | web / api |
| [005](005-secret-management.md) | 機密管理:不入庫、單一來源、各環境隔離 | db / api / ops |

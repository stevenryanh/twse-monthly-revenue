# Design Decisions

記錄本專案做過的重要設計選擇,以及「為什麼不那樣做」。不是規格書,是「為什麼這樣,以及改動前要想清楚什麼」。

| # | 標題 | 適用 |
|---|------|------|
| [001](001-schema-key-and-types.md) | 資料表主鍵與欄位型別 | db / api |
| [002](002-stored-procedure-upsert.md) | upsert 預存程序 + 全程參數化 | db / api |
| [003](003-data-access-ado-not-ef.md) | ADO.NET 直呼 sp,不用 EF / AutoMapper | api |

# 003. 資料存取用 ADO.NET 直呼預存程序,不用 EF / AutoMapper

2026-06,適用:api

## 背景
公司既有微服務家規用 EF Core,且不用 AutoMapper(手寫映射)。但本題的 DB 是**手寫 schema + 預存程序**(非 EF Code-First),且全系統只有兩支程序、少數對映物件。

## 決定
- 用原生 **ADO.NET(`Microsoft.Data.SqlClient`)** 直接呼叫預存程序。
- Entity ↔ DTO **手寫映射**(沿用團隊不用 AutoMapper 的家規)。

## 排除了什麼
- **EF Core**:它為 Code-First / Migrations 而生;這裡 schema 與 sp 都是手寫,只為呼叫兩支程序而引入整套 EF,是過重且方向相反的抽象。
- **AutoMapper**:欄位固定、數量少,反射映射比手寫更難讀、難除錯,且多一個相依——與「無多餘」原則衝突。

## 改動前必須想清楚的
若日後改用 EF:手寫 schema 與預存程序的所有權歸誰?會不會變成 EF 與 sp 兩套並存、各自飄移?

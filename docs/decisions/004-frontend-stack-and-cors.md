# 004. 前端技術選擇與開發環境 CORS

2026-06,適用:web / api

## 背景
前端是「以公司代號查詢 → 表格 + 趨勢圖」的單頁工具,需求單純:一支 GET、一張表、一張圖。前後端分離,dev 時前端（Vite, :5173）與 API（:5080）不同來源。

## 決定
- **Vue 3 + Vite**(對齊既定技術棧),元件用 `<script setup>`。
- **資料抓取用原生 `fetch`**,不引入 axios;以 `AbortController` 取消過期查詢。
- **圖表用 ECharts 按需匯入**(`echarts/core` + 僅註冊 Bar/Line/Grid/Tooltip/Legend/Canvas),而非整包匯入。
- **CORS 僅在開發環境放行**,來源清單由 `appsettings.json` 的 `Cors:AllowedOrigins` 控制,預設只放行 `http://localhost:5173`。

## 排除了什麼
- **axios**:只有一支端點,原生 `fetch` 已足夠,少一個相依——與後端「無多餘」原則一致。
- **整包 `import * as echarts from 'echarts'`**:會讓 bundle 膨脹近一倍（1.1MB → 571KB),圖表只用到長條 / 折線,按需匯入即可。
- **Vite proxy 繞過跨域**:選擇在後端明確放行 CORS,讓前端直接打 API、來源關係外顯可控,而非把跨域問題藏進 dev proxy。
- **生產環境開放 CORS**:`UseCors` 只掛在 `IsDevelopment()` 分支;正式部署時前端應與 API 同源(反向代理),不靠 CORS。

## 改動前必須想清楚的
- 若正式環境也需跨來源:CORS 來源要改為由環境設定注入,且**絕不可** `AllowAnyOrigin`,須列舉正式網域。
- 若端點數量變多或需攔截器（統一錯誤 / 驗證標頭）:屆時再評估是否值得引入 axios,而非現在預先抽象。

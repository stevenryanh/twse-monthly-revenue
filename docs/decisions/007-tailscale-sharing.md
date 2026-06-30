# 007. 用 Tailscale 私有網路分享給可信對象,而非公開到網路

2026-06,適用:web / ops

## 背景
想把本機開發中的服務分享給**小範圍、可信任的人**（例如自家姐弟）查看,但這是個**開發伺服器**:沒有 authn/authz、含「新增資料」的寫入 API（匯入用）、且是 Vite/Kestrel dev server。**不能**為了讓人看就公開到網際網路——那等於把無認證、可寫入的開發機曝險。需求是:**只開放給我授權的人、零公網曝光、且對方用自己的帳號/裝置**。

## 決定
用 **Tailscale 私有網狀網路 + 節點共享(node sharing)**,走 tailnet 內位址,**而非公開的 Tailscale Funnel**。前端配合改走**同源 `/api` 代理**,讓單一網址即可跑完整服務。

| 面向 | 做法 | 理由 |
|---|---|---|
| 對外曝光 | Tailscale **Serve / 內網 IP**(`http://<tailscale-ip>:5173`),**不用 Funnel** | Serve/內網僅 tailnet 成員可達;Funnel 會公開到整個網際網路,對無認證 dev server 太危險 |
| 授權對象 | 後台把本機 **Share** 給對方帳號;對方用**自己的 Tailscale 帳號/裝置** | 各自帳號、各自裝置,我只授權單一節點;移除授權即斷線 |
| 前後端單一網址 | Vite `server.proxy` 把 `/api` 代理到後端 5080;前端 API base 預設**同源 `''`** | 對方瀏覽器的 `localhost` 指向他自己的機器;改走同源 `/api`,經一個 Tailscale 網址即全通、且免 CORS |
| 綁定位址 | Vite `host: true`(0.0.0.0)、`allowedHosts: true` | 讓 tailnet 裝置連得到,並放行 `*.ts.net` 的 Host |

## 排除了什麼
- **Tailscale Funnel / ngrok 等公開隧道**:會把無認證、可寫入的 dev server 暴露到公網,風險與需求(只給自家人看)不符。
- **開放路由器 port / 公網 IP**:同上,且需處理防火牆、DDNS、憑證,維運成本高。
- **把前端 API base 寫死成 Tailscale IP + 為該來源加 CORS**:脆弱(IP 變更即失效)、且需後端同時對外。改用同源代理一勞永逸。
- **為了分享就把 API 設成公開可寫**:寫入端點(`POST /api/...`)僅供匯入,不應對外。

## 與 004 的關係
[004](004-frontend-stack-and-cors.md) 採「跨來源 + 開發環境 CORS」。本決策把前端**預設改為同源 `/api` 代理**(免 CORS、利於 Tailscale 單一網址);後端 CORS 設定**仍保留**,設 `VITE_API_BASE=http://localhost:5080` 即可切回跨來源直連示範。兩者並存,預設取同源。

## 改動前必須想清楚的
- **這是 dev server、無認證、且有寫入 API**:tailnet 內成員理論上也能 `POST` 寫資料——分享對象務必限可信的人,**絕不可** Funnel 公開。
- **可用性條件**:本機要醒著、服務要在跑(`./dev.sh`),對方才連得到;睡眠/關機即不可達(這也是它「私有」的本質)。
- **要乾淨的 `https://<host>.ts.net` 網址**:需在 Tailscale 後台啟用 **MagicDNS + HTTPS Certificates**,再 `tailscale serve`;否則用 `http://<tailscale-ip>:5173` 即可。
- **正式上線**:本決策是「給自家人看 dev 成果」的權宜;正式對外仍應同源部署 + 加上 authn/authz 與寫入端點保護。

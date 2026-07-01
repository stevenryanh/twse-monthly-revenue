// 把買賣投報排行匯出成「自帶樣式、離線可看」的可攜式 HTML，供手機分享（AirDrop/LINE 傳檔）。
// 公開 localhost 連結對別人沒用；改用可攜文檔，對方離線就能看那一刻的名單快照。

function esc(s) {
  return String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
}

function lot(v) {
  return v == null ? '—' : `${(Number(v) * 1000 / 10000).toFixed(1)} 萬`
}

function pct(v) {
  return v == null ? '—' : `${Number(v).toFixed(2)}%`
}

/** 由排行資料組出自帶樣式的 HTML 字串。 */
export function buildWatchlistHtml(rows, { budgetWan } = {}) {
  const today = new Date().toISOString().slice(0, 10)
  const bottom = rows.filter((r) => r.entryTiming === '即將見底')
  const others = rows.filter((r) => r.entryTiming !== '即將見底').slice(0, 10)

  const bottomRows = bottom.map((r) => `<tr>
    <td>${esc(r.companyCode)}</td><td>${esc(r.companyName)}</td>
    <td>${r.swingScore ?? '—'}</td><td>${r.riskAdjustedReturn ?? '—'}</td>
    <td>${r.cycleDays == null ? '—' : r.cycleDays + '天'}</td>
    <td>${r.pricePositionPercent == null ? '—' : r.pricePositionPercent + '%'}</td>
    <td>${lot(r.lastClose)}</td></tr>`).join('\n')

  const othersRows = others.map((r) => `<tr>
    <td>${esc(r.companyCode)}</td><td>${esc(r.companyName)}</td>
    <td>${r.swingScore ?? '—'}</td><td>${esc(r.entryTiming || '—')}</td>
    <td>${lot(r.lastClose)}</td></tr>`).join('\n')

  const bottomTable = bottom.length
    ? `<table><thead><tr><th>代號</th><th>名稱</th><th>波段分</th><th>報酬/風險</th><th>週期</th><th>區間位置</th><th>每張約</th></tr></thead><tbody>${bottomRows}</tbody></table>`
    : `<p class="none">今天沒有「即將見底」的好時機——不一定要買，建議觀望。</p>`

  const budgetLine = budgetWan ? `總預算 ≤ ${budgetWan} 萬 ｜ ` : ''

  return `<!DOCTYPE html>
<html lang="zh-Hant"><head><meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>每日觀察名單 ${today}</title>
<style>
 body{font-family:system-ui,-apple-system,"PingFang TC",sans-serif;max-width:720px;margin:0 auto;padding:18px;background:#f5f5f5;color:#222}
 h1{font-size:1.25rem;margin:0 0 4px}
 .sub{color:#888;font-size:.82rem;margin:0 0 10px}
 .disc{font-size:.74rem;color:#b26a00;background:#fff8e1;border:1px solid #ffe0a3;border-radius:8px;padding:8px 12px;line-height:1.6;margin:0 0 16px}
 h2{font-size:1rem;margin:18px 0 8px}
 table{width:100%;border-collapse:collapse;background:#fff;border-radius:10px;overflow:hidden;box-shadow:0 1px 3px rgba(0,0,0,.1);font-size:.82rem}
 th,td{padding:8px 10px;text-align:right;border-bottom:1px solid #f0f0f0;white-space:nowrap}
 th{background:#fafafa;color:#666;font-weight:600}
 td:nth-child(2),th:nth-child(2){text-align:left}
 tbody tr:last-child td{border-bottom:none}
 .none{color:#b26a00;background:#fff8e1;border:1px solid #ffe0a3;border-radius:8px;padding:10px 14px}
 .foot{color:#aaa;font-size:.72rem;text-align:center;margin:22px 0 6px}
</style></head><body>
<h1>每日觀察名單 — ${today}</h1>
<p class="sub">${budgetLine}由「易入手波段分」自動產生（報酬÷波動÷波段週期×離低點）</p>
<div class="disc">⚠️ 僅供參考、非投資建議；過去不代表未來。紀律：單筆 ≤30%、沒好時機就觀望、只用閒錢、少賺有賺就好。</div>
<h2>⭐ 即將見底（值得留意進場）</h2>
${bottomTable}
<h2>其他高分（時機普通/偏高，僅供參考）</h2>
<table><thead><tr><th>代號</th><th>名稱</th><th>波段分</th><th>進場時機</th><th>每張約</th></tr></thead><tbody>${othersRows}</tbody></table>
<p class="foot">此為離線可攜文檔;資料為 ${today} 的快照。</p>
</body></html>`
}

/** 分享或下載 HTML 檔：手機優先跳原生分享選單（可傳 LINE/AirDrop），否則直接下載。 */
export async function shareOrDownloadHtml(filename, html) {
  const blob = new Blob([html], { type: 'text/html' })
  const file = new File([blob], filename, { type: 'text/html' })
  if (navigator.canShare && navigator.canShare({ files: [file] })) {
    try {
      await navigator.share({ files: [file], title: '每日觀察名單' })
      return
    } catch {
      /* 使用者取消或不支援 → 退回下載 */
    }
  }
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  document.body.appendChild(a)
  a.click()
  a.remove()
  URL.revokeObjectURL(url)
}

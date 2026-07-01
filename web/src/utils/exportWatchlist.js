// 把買賣投報排行匯出成「圖片(PNG)」——LINE 吃圖片(聊天室直接顯示)，不吃 HTML 檔。
// 用 canvas 就地繪製(不加任何套件)，手機分享選單選 LINE 即當圖片傳，對方離線也看得到。
import { poolsShort } from './etfPools'

function lot(v) {
  return v == null ? '—' : `${(Number(v) * 1000 / 10000).toFixed(1)}萬`
}

function timingColor(label) {
  if (label === '即將見底') return '#1b5e20'
  if (label === '即將見頂·宜收手') return '#c62828'
  if (label && label.includes('觀望')) return '#b26a00'
  return '#555'
}

/** 由排行資料繪出觀察名單 PNG，回傳 Blob。 */
export function buildWatchlistPng(rows, { budgetWan } = {}) {
  const today = new Date().toISOString().slice(0, 10)
  const bottom = rows.filter((r) => r.entryTiming === '即將見底')
  const others = rows.filter((r) => r.entryTiming !== '即將見底').slice(0, 10)

  const W = 880
  const pad = 24
  const rowH = 34
  const titleH = 120
  const secHeadH = 40
  const H = titleH + secHeadH + Math.max(bottom.length, 1) * rowH
    + secHeadH + others.length * rowH + 60

  const scale = Math.min(3, (window.devicePixelRatio || 1) * 1.5)
  const canvas = document.createElement('canvas')
  canvas.width = Math.round(W * scale)
  canvas.height = Math.round(H * scale)
  const ctx = canvas.getContext('2d')
  ctx.scale(scale, scale)
  ctx.textBaseline = 'middle'
  const FONT = '-apple-system, "PingFang TC", "Microsoft JhengHei", system-ui, sans-serif'

  // 背景
  ctx.fillStyle = '#ffffff'
  ctx.fillRect(0, 0, W, H)

  let y = 30
  // 標題
  ctx.fillStyle = '#222'
  ctx.font = `700 22px ${FONT}`
  ctx.textAlign = 'left'
  ctx.fillText(`每日觀察名單 — ${today}`, pad, y)
  y += 28
  ctx.fillStyle = '#888'
  ctx.font = `13px ${FONT}`
  ctx.fillText(`${budgetWan ? `總預算 ≤ ${budgetWan} 萬 ｜ ` : ''}易入手波段分（報酬÷波動÷週期×離低點）`, pad, y)
  y += 24
  ctx.fillStyle = '#b26a00'
  ctx.font = `12px ${FONT}`
  ctx.fillText('⚠ 僅供參考、非投資建議；過去不代表未來。單筆≤30%、只用閒錢、少賺有賺就好。', pad, y)
  y += 26

  // 欄位 x 座標
  const xCode = pad
  const xName = pad + 66
  const xPool = pad + 200
  const xScore = pad + 300
  const xTiming = pad + 390
  const xLot = W - pad // 右對齊

  function header(title) {
    ctx.fillStyle = '#1b5e20'
    ctx.font = `700 15px ${FONT}`
    ctx.textAlign = 'left'
    ctx.fillText(title, pad, y + secHeadH / 2)
    // 表頭
    const hy = y + secHeadH - 6
    ctx.fillStyle = '#999'
    ctx.font = `12px ${FONT}`
    ctx.fillText('代號', xCode, hy)
    ctx.fillText('名稱', xName, hy)
    ctx.fillText('所屬池', xPool, hy)
    ctx.fillText('波段分', xScore, hy)
    ctx.fillText('進場時機', xTiming, hy)
    ctx.textAlign = 'right'
    ctx.fillText('每張約', xLot, hy)
    ctx.textAlign = 'left'
    y += secHeadH
  }

  function row(r) {
    // 分隔線
    ctx.strokeStyle = '#f0f0f0'
    ctx.beginPath()
    ctx.moveTo(pad, y)
    ctx.lineTo(W - pad, y)
    ctx.stroke()
    const cy = y + rowH / 2
    ctx.font = `14px ${FONT}`
    ctx.textAlign = 'left'
    ctx.fillStyle = '#1565c0'
    ctx.fillText(r.companyCode, xCode, cy)
    ctx.fillStyle = '#222'
    ctx.fillText(r.companyName || '—', xName, cy)
    ctx.fillStyle = '#00838f'
    ctx.font = `12px ${FONT}`
    ctx.fillText(poolsShort(r.companyCode), xPool, cy)
    ctx.fillStyle = '#1b5e20'
    ctx.font = `700 14px ${FONT}`
    ctx.fillText(r.swingScore == null ? '—' : String(r.swingScore), xScore, cy)
    ctx.font = `13px ${FONT}`
    ctx.fillStyle = timingColor(r.entryTiming)
    ctx.fillText(r.entryTiming || '—', xTiming, cy)
    ctx.fillStyle = '#333'
    ctx.font = `14px ${FONT}`
    ctx.textAlign = 'right'
    ctx.fillText(lot(r.lastClose), xLot, cy)
    ctx.textAlign = 'left'
    y += rowH
  }

  header('⭐ 即將見底（值得留意進場）')
  if (bottom.length) {
    bottom.forEach(row)
  } else {
    ctx.fillStyle = '#b26a00'
    ctx.font = `13px ${FONT}`
    ctx.fillText('今天沒有「即將見底」的好時機——不一定要買，建議觀望。', pad, y + rowH / 2)
    y += rowH
  }

  y += 8
  header('其他高分（時機普通/偏高，僅供參考）')
  others.forEach(row)

  // 圖例
  ctx.fillStyle = '#aaa'
  ctx.font = `11px ${FONT}`
  ctx.textAlign = 'left'
  ctx.fillText('所屬池：權=0050、息=0056、永=00878、波=00713（此為離線快照）', pad, y + 16)

  return new Promise((resolve) => canvas.toBlob((b) => resolve(b), 'image/png'))
}

/** 分享或下載檔案：手機優先跳原生分享選單（LINE 吃圖片），否則下載。 */
export async function shareOrDownloadFile(filename, blob) {
  const file = new File([blob], filename, { type: blob.type })
  if (navigator.canShare && navigator.canShare({ files: [file] })) {
    try {
      await navigator.share({ files: [file], title: '每日觀察名單' })
      return
    } catch {
      /* 取消或不支援 → 退回下載 */
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

/** 將 yyyymm 整數格式化為 "yyyy/MM"，例如 202405 → "2024/05"。 */
export function formatYearMonth(yyyymm) {
  if (yyyymm == null) return '—'
  const s = String(yyyymm)
  return `${s.slice(0, 4)}/${s.slice(4, 6)}`
}

/** 千分位整數，null 顯示破折號。原始營收單位為千元。 */
export function formatThousand(value) {
  if (value == null) return '—'
  return value.toLocaleString('zh-TW')
}

/** 百分比，保留兩位，附正負號與符號；null 顯示破折號。 */
export function formatPercent(value) {
  if (value == null) return '—'
  const sign = value > 0 ? '+' : ''
  return `${sign}${value.toFixed(2)}%`
}

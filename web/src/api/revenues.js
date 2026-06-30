// API base：預設對齊後端 appsettings 的 Urls，可用 VITE_API_BASE 覆寫。
const API_BASE = import.meta.env.VITE_API_BASE ?? 'http://localhost:5080'

/**
 * 以公司代號查詢各月營收（後端已依最新月份在前排序）。
 * @param {string} companyCode 公司代號，例如 "2330"
 * @param {AbortSignal} [signal] 用於取消過期請求
 * @returns {Promise<Array>} MonthlyRevenueDto 陣列（camelCase 欄位）
 */
export async function getRevenuesByCompanyCode(companyCode, signal) {
  const code = encodeURIComponent(companyCode.trim())
  const res = await fetch(`${API_BASE}/api/revenues/${code}`, { signal })

  if (!res.ok) {
    throw new Error(`查詢失敗（HTTP ${res.status}）`)
  }
  return res.json()
}

import { apiGet } from './http'

/**
 * 以公司代號查詢各月營收（後端已依最新月份在前排序）。
 * 透過 http middleware 送出：自動帶 X-Correlation-ID、集中錯誤處理（丟 ApiError）。
 * @param {string} companyCode 公司代號，例如 "2330"
 * @param {AbortSignal} [signal] 用於取消過期請求
 * @returns {Promise<Array>} MonthlyRevenueDto 陣列（camelCase 欄位）
 */
export function getRevenuesByCompanyCode(companyCode, signal) {
  const code = encodeURIComponent(companyCode.trim())
  return apiGet(`/api/revenues/${code}`, { signal })
}

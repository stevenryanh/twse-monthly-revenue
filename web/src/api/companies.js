import { apiGet } from './http'

/**
 * 關鍵字搜尋公司（代號或名稱的一部分），供輸入時自動完成。
 * 透過 http middleware 送出：自動帶 X-Correlation-ID、集中錯誤處理。
 * @param {string} keyword 代號片段或公司名片段，例如 "23"、"台積"
 * @param {AbortSignal} [signal] 用於取消過期請求
 * @returns {Promise<Array>} CompanySummaryDto 陣列（camelCase；最多 20 筆）
 */
export function searchCompanies(keyword, signal) {
  const q = encodeURIComponent(keyword.trim())
  return apiGet(`/api/companies?q=${q}`, { signal })
}

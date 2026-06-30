import { apiGet } from './http'

/**
 * 買賣投報排行：把（條件中的）股票依每元當日報酬在近期間的指標排序。
 * @param {object} opts
 * @param {string} [opts.q] 代號或名稱片段（篩選）
 * @param {string} [opts.codes] 逗號分隔代碼清單（篩選）
 * @param {string} [opts.sort] return | volatility | avg | daily
 * @param {number} [opts.top] 筆數上限
 * @param {AbortSignal} [signal]
 * @returns {Promise<Array>} QuoteRankingDto 陣列（camelCase）
 */
export function rankQuotes({ q = '', codes = '', sort = 'return', top = 30 } = {}, signal) {
  const params = new URLSearchParams()
  if (q.trim()) params.set('q', q.trim())
  if (codes.trim()) params.set('codes', codes.trim())
  if (sort) params.set('sort', sort)
  if (top) params.set('top', String(top))
  return apiGet(`/api/quotes/ranking?${params.toString()}`, { signal })
}

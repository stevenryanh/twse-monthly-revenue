// 前端 HTTP middleware 模塊 —— 與後端對稱地處理橫切關注點。
// 採洋蔥式 middleware 鏈：每個 middleware 收 (ctx, next)，可在 fetch 前後加工。
// 未來要加 auth token、重試、前端 log 等，只要往 middlewares 陣列插一個，呼叫端不動。

const API_BASE = import.meta.env.VITE_API_BASE ?? 'http://localhost:5080'
const CORRELATION_HEADER = 'X-Correlation-ID'

/** 標準化的 API 錯誤，攜帶 HTTP 狀態與後端回傳的 traceId（便於回報/對日誌）。 */
export class ApiError extends Error {
  constructor(message, { status, traceId } = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.traceId = traceId
  }
}

function newCorrelationId() {
  if (globalThis.crypto?.randomUUID) return globalThis.crypto.randomUUID().replace(/-/g, '')
  return `${Date.now().toString(16)}${Math.random().toString(16).slice(2)}`
}

// --- middlewares（最外層在前）---

// 集中錯誤處理：包住整條鏈，檢視最終 Response、解析後端 { error, traceId }，丟出標準化錯誤。
const errorHandling = async (ctx, next) => {
  const res = await next()
  const traceId = res.headers.get(CORRELATION_HEADER) ?? ctx.headers[CORRELATION_HEADER]

  if (!res.ok) {
    let message = `請求失敗（HTTP ${res.status}）`
    try {
      const data = await res.json()
      if (data?.error) message = data.error
    } catch {
      /* 非 JSON 回應，沿用預設訊息 */
    }
    throw new ApiError(message, { status: res.status, traceId })
  }
  return res.status === 204 ? null : res.json()
}

// 注入關聯 ID：沿用既有或新生一個，與後端 CorrelationIdMiddleware 端到端串接。
const correlationId = async (ctx, next) => {
  ctx.headers[CORRELATION_HEADER] = ctx.headers[CORRELATION_HEADER] ?? newCorrelationId()
  return next()
}

const middlewares = [errorHandling, correlationId]

async function terminal(ctx) {
  return fetch(`${API_BASE}${ctx.path}`, {
    method: ctx.method,
    headers: ctx.headers,
    body: ctx.body,
    signal: ctx.signal,
  })
}

function run(ctx) {
  const dispatch = (i) => {
    const mw = middlewares[i]
    return mw ? mw(ctx, () => dispatch(i + 1)) : terminal(ctx)
  }
  return dispatch(0)
}

export function apiGet(path, { signal } = {}) {
  return run({ method: 'GET', path, headers: {}, body: undefined, signal })
}

export function apiPost(path, body, { signal } = {}) {
  return run({
    method: 'POST',
    path,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
    signal,
  })
}

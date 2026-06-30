<script setup>
import { ref, computed, watch } from 'vue'
import { getRevenuesByCompanyCode } from './api/revenues'
import { searchCompanies } from './api/companies'
import RevenueTable from './components/RevenueTable.vue'
import RevenueChart from './components/RevenueChart.vue'
import QuoteRanking from './components/QuoteRanking.vue'

const tab = ref('revenue') // 'revenue' = 個股營收查詢；'ranking' = 買賣投報排行

// 分享當前網址給好友：手機跳原生分享選單（含 LINE），桌機退回 LINE 分享頁。
function shareApp() {
  const url = window.location.href
  if (navigator.share) {
    navigator.share({ title: '上市公司每月營收 / 買賣投報排行', url }).catch(() => {})
  } else {
    window.open('https://social-plugins.line.me/lineit/share?url=' + encodeURIComponent(url), '_blank')
  }
}

const code = ref('')
const rows = ref([])
const loading = ref(false)
const error = ref('')
const searched = ref(false)

let inFlight = null // 取消過期請求

// 公司基本資料各列相同，取第一筆作摘要
const summary = computed(() => rows.value[0] ?? null)

// ── 自動完成：邊輸入邊列出符合的公司（代號或名稱的一部分）──
const suggestions = ref([])
const suggesting = ref(false)
let suggestTimer = null
let suggestController = null
let skipNextSuggest = false // 選取候選後抑制下一次觸發

// 輸入變動 → debounce 300ms → 搜尋；race-safe，只認最後一次輸入的結果。
watch(code, (val) => {
  if (suggestTimer) clearTimeout(suggestTimer)
  if (skipNextSuggest) { skipNextSuggest = false; return }
  const q = val.trim()
  if (!q) { suggestions.value = []; suggesting.value = false; return }
  suggesting.value = true
  suggestTimer = setTimeout(async () => {
    suggestController?.abort()
    const controller = new AbortController()
    suggestController = controller
    try {
      const r = await searchCompanies(q, controller.signal)
      if (code.value.trim() !== q) return // 連打時丟棄過期結果
      suggestions.value = r
    } catch (e) {
      if (e.name === 'AbortError') return
      if (code.value.trim() === q) suggestions.value = []
    } finally {
      if (code.value.trim() === q) suggesting.value = false
    }
  }, 300)
})

function pickSuggestion(c) {
  if (suggestTimer) clearTimeout(suggestTimer)
  skipNextSuggest = true // 帶入代號不應再觸發搜尋
  code.value = c.companyCode
  suggestions.value = []
  suggesting.value = false
  search()
}

function hideSuggestions() {
  // 失焦延遲關閉，讓 click 先完成（candidate 用 mousedown 已可避免，這裡是雙保險）
  setTimeout(() => { suggestions.value = [] }, 120)
}

async function search() {
  const trimmed = code.value.trim()
  if (!trimmed || loading.value) return

  suggestions.value = [] // 查詢即收起候選清單
  inFlight?.abort()
  const controller = new AbortController()
  inFlight = controller

  loading.value = true
  error.value = ''
  searched.value = true
  try {
    rows.value = await getRevenuesByCompanyCode(trimmed, controller.signal)
  } catch (e) {
    if (e.name === 'AbortError') return
    const base = e.message || '查詢發生未預期錯誤'
    error.value = e.traceId ? `${base}（追蹤碼 ${e.traceId}）` : base
    rows.value = []
  } finally {
    if (inFlight === controller) {
      loading.value = false
      inFlight = null
    }
  }
}
</script>

<template>
  <div class="app-header">
    <h1>上市公司每月營收查詢</h1>
    <button class="share-btn" @click="shareApp" title="分享給好友（LINE）">📤 分享</button>
  </div>
  <p class="subtitle">資料來源：臺灣證券交易所 OpenAPI（t187ap05_L 月營收 · STOCK_DAY 每日行情）</p>

  <div class="tabs">
    <button :class="{ active: tab === 'revenue' }" @click="tab = 'revenue'">個股營收查詢</button>
    <button :class="{ active: tab === 'ranking' }" @click="tab = 'ranking'">買賣投報排行</button>
  </div>

  <template v-if="tab === 'ranking'">
    <QuoteRanking />
  </template>

  <template v-else>
  <div class="card">
    <div class="search-bar">
      <div class="typeahead">
        <input
          v-model="code"
          type="text"
          placeholder="輸入代號或公司名，例如 2330 或 台積"
          autocomplete="off"
          @keyup.enter="search"
          @blur="hideSuggestions"
        />
        <ul v-if="suggestions.length" class="suggest">
          <li
            v-for="c in suggestions"
            :key="c.companyCode"
            @mousedown.prevent="pickSuggestion(c)"
          >
            <span class="sg-code">{{ c.companyCode }}</span>
            <span class="sg-name">{{ c.companyName }}</span>
            <span class="sg-ind">{{ c.industry || '' }}</span>
          </li>
        </ul>
        <p v-else-if="suggesting" class="suggest-hint">搜尋中…</p>
        <p v-else-if="code.trim() && !loading && !searched" class="suggest-hint">查無相符公司</p>
      </div>
      <button :disabled="loading || !code.trim()" @click="search">
        {{ loading ? '查詢中…' : '查詢' }}
      </button>
    </div>
  </div>

  <div v-if="loading" class="state">查詢中…</div>
  <div v-else-if="error" class="state error">{{ error }}</div>
  <div v-else-if="searched && rows.length === 0" class="state">
    查無代號「{{ code.trim() }}」的營收資料。
  </div>

  <template v-else-if="rows.length > 0">
    <div v-if="summary" class="card">
      <h1 style="font-size: 1.2rem; margin-bottom: 8px">
        {{ summary.companyCode }} {{ summary.companyName }}
      </h1>
      <p class="subtitle" style="margin: 0">
        {{ summary.industry || '未分類' }} · 共 {{ rows.length }} 個月資料
      </p>
    </div>

    <div class="card">
      <RevenueChart :rows="rows" />
    </div>

    <div class="card">
      <RevenueTable :rows="rows" />
    </div>
  </template>
  </template>
</template>

<style scoped>
.app-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}
.share-btn {
  flex-shrink: 0;
  background: #06c755;
  color: #fff;
  border: none;
  border-radius: 8px;
  padding: 8px 16px;
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
}
.share-btn:hover {
  background: #05b34c;
}
.tabs {
  display: flex;
  gap: 8px;
  margin-bottom: 16px;
}
.tabs button {
  padding: 8px 18px;
  font-size: 0.95rem;
  border: 1px solid var(--border, #ddd);
  background: #fff;
  color: #555;
  border-radius: 8px;
  cursor: pointer;
}
.tabs button.active {
  background: var(--primary, #1b5e20);
  border-color: var(--primary, #1b5e20);
  color: #fff;
  font-weight: 600;
}
.typeahead {
  position: relative;
  flex: 1;
}
.typeahead input {
  width: 100%;
  box-sizing: border-box;
}
.suggest {
  position: absolute;
  z-index: 20;
  left: 0;
  right: 0;
  top: calc(100% + 4px);
  list-style: none;
  margin: 0;
  padding: 4px;
  background: #fff;
  border: 1px solid var(--border, #ddd);
  border-radius: 8px;
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.12);
  max-height: 280px;
  overflow-y: auto;
}
.suggest li {
  display: flex;
  align-items: baseline;
  gap: 10px;
  padding: 8px 10px;
  border-radius: 6px;
  cursor: pointer;
}
.suggest li:hover {
  background: #f0f7f1;
}
.sg-code {
  font-family: monospace;
  font-weight: 600;
  color: var(--primary, #1b5e20);
  min-width: 48px;
}
.sg-name {
  flex: 1;
  color: #222;
}
.sg-ind {
  font-size: 0.78rem;
  color: #999;
}
.suggest-hint {
  position: absolute;
  z-index: 20;
  left: 0;
  right: 0;
  top: calc(100% + 4px);
  margin: 0;
  padding: 8px 12px;
  background: #fff;
  border: 1px solid var(--border, #ddd);
  border-radius: 8px;
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.12);
  font-size: 0.82rem;
  color: #888;
}
</style>

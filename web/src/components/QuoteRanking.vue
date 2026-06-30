<script setup>
import { ref, watch, onMounted } from 'vue'
import { rankQuotes } from '../api/quotes'
import { formatPercent } from '../utils/format'

const SORTS = [
  { key: 'return', label: '期間累計報酬' },
  { key: 'volatility', label: '報酬波動（變量）' },
  { key: 'avg', label: '平均日報酬' },
  { key: 'daily', label: '最近一日報酬' },
]

const sort = ref('return')
const keyword = ref('')
const rows = ref([])
const loading = ref(false)
const error = ref('')

let inFlight = null
let kwTimer = null

function pctClass(v) {
  if (v == null) return ''
  return v > 0 ? 'pos' : v < 0 ? 'neg' : ''
}

// 民國 YYYMMDD（1150630）→ "115/06/30"
function rocDate(v) {
  if (v == null) return '—'
  const s = String(v).padStart(7, '0')
  return `${s.slice(0, 3)}/${s.slice(3, 5)}/${s.slice(5, 7)}`
}

function price(v) {
  return v == null ? '—' : Number(v).toLocaleString('zh-TW', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

async function load() {
  inFlight?.abort()
  const controller = new AbortController()
  inFlight = controller
  loading.value = true
  error.value = ''
  try {
    rows.value = await rankQuotes({ q: keyword.value, sort: sort.value, top: 30 }, controller.signal)
  } catch (e) {
    if (e.name === 'AbortError') return
    error.value = e.message || '排行查詢失敗'
    rows.value = []
  } finally {
    if (inFlight === controller) { loading.value = false; inFlight = null }
  }
}

watch(sort, load)
watch(keyword, () => {
  if (kwTimer) clearTimeout(kwTimer)
  kwTimer = setTimeout(load, 300)
})
onMounted(load)
</script>

<template>
  <div class="card">
    <h1 style="font-size: 1.2rem; margin-bottom: 4px">買賣投報排行</h1>
    <p class="subtitle" style="margin: 0 0 14px">
      以「每元當日報酬＝漲跌 ÷ 昨收」為基礎，近一個月每日行情彙總；資料範圍：0050 與成分股。
    </p>

    <div class="rank-controls">
      <label>
        排序依據
        <select v-model="sort">
          <option v-for="s in SORTS" :key="s.key" :value="s.key">{{ s.label }}</option>
        </select>
      </label>
      <input v-model="keyword" type="text" placeholder="篩選代號或名稱（可空）" autocomplete="off" />
    </div>

    <div v-if="loading" class="state">排行查詢中…</div>
    <div v-else-if="error" class="state error">{{ error }}</div>
    <div v-else-if="rows.length === 0" class="state">查無符合的股票（請先以 scripts/import-quotes.py 餵入股價）。</div>

    <div v-else class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>#</th>
            <th>代號</th>
            <th>名稱</th>
            <th>期間累計報酬</th>
            <th>報酬波動</th>
            <th>平均日報酬</th>
            <th>最近一日</th>
            <th>期初→期末收盤</th>
            <th>天數</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(r, i) in rows" :key="r.companyCode">
            <td>{{ i + 1 }}</td>
            <td style="font-family: monospace">{{ r.companyCode }}</td>
            <td style="text-align: left">{{ r.companyName || '—' }}</td>
            <td :class="pctClass(r.periodReturnPercent)">{{ formatPercent(r.periodReturnPercent) }}</td>
            <td>{{ r.volatilityPercent == null ? '—' : r.volatilityPercent.toFixed(2) + '%' }}</td>
            <td :class="pctClass(r.avgDailyReturnPercent)">{{ formatPercent(r.avgDailyReturnPercent) }}</td>
            <td :class="pctClass(r.lastDayReturnPercent)">{{ formatPercent(r.lastDayReturnPercent) }}</td>
            <td>{{ price(r.firstClose) }} → {{ price(r.lastClose) }}</td>
            <td>{{ r.days }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.rank-controls {
  display: flex;
  gap: 14px;
  align-items: center;
  flex-wrap: wrap;
  margin-bottom: 14px;
}
.rank-controls label {
  font-size: 0.85rem;
  color: #555;
  display: flex;
  align-items: center;
  gap: 6px;
}
.rank-controls select,
.rank-controls input {
  padding: 8px 10px;
  font-size: 0.95rem;
  border: 1px solid var(--border, #ddd);
  border-radius: 8px;
  outline: none;
}
.rank-controls input {
  flex: 1;
  min-width: 160px;
}
.rank-controls select:focus,
.rank-controls input:focus {
  border-color: var(--primary, #1b5e20);
}
</style>

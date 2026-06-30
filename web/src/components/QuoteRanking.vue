<script setup>
import { ref, watch, onMounted, computed } from 'vue'
import { rankQuotes } from '../api/quotes'
import { formatPercent } from '../utils/format'

// 進場時機標籤顏色：好（綠）/ 觀望（橘）/ 收手（紅）
function timingClass(label) {
  if (label === '即將見底') return 'tm-good'
  if (label === '即將見頂·宜收手') return 'tm-sell'
  if (label && label.includes('觀望')) return 'tm-wait'
  return ''
}

// 可排序欄位（key 對應後端 sort 參數）。點欄位表頭切換排序、再點切換升/降冪。
const SORTABLE = {
  swingscore: '易入手波段分',
  return: '期間累計報酬',
  sharpe: '報酬/風險比',
  volatility: '報酬波動',
  avg: '平均日報酬',
  daily: '最近一日',
}

// 小資總預算：以「買得起一張(1000股)」為準 → 每股價上限 = 總預算 ÷ 1000。
// 例：總預算 5 萬 → 每股 ≤ 50，正是金融股、台泥、中鋼等小資可負擔的範圍。
const BUDGETS = [
  { label: '不限', value: null },
  { label: '1 萬', value: 10000 },
  { label: '3 萬', value: 30000 },
  { label: '5 萬', value: 50000 },
  { label: '10 萬', value: 100000 },
  { label: '20 萬', value: 200000 },
  { label: '30 萬', value: 300000 },
]

const sort = ref('return')
const dir = ref('desc')
const keyword = ref('')
const budget = ref(null)
const rows = ref([])
const loading = ref(false)
const error = ref('')

let inFlight = null
let kwTimer = null

// 「不一定要買」：若清單中沒有任何「即將見底」的好時機，提醒觀望
const noGoodTiming = computed(() =>
  sort.value === 'swingscore' && rows.value.length > 0 &&
  !rows.value.some(r => r.entryTiming === '即將見底'))

// 點欄位：已是當前排序鍵 → 切換升/降冪；否則切到該鍵並預設降冪。
function toggleSort(key) {
  if (sort.value === key) {
    dir.value = dir.value === 'desc' ? 'asc' : 'desc'
  } else {
    sort.value = key
    dir.value = 'desc'
  }
}

// 表頭箭頭：當前排序鍵顯示 ▼/▲；其餘可排序欄位顯示淡色 ⇅ 提示可排序。
function arrow(key) {
  if (sort.value !== key) return '⇅'
  return dir.value === 'desc' ? '▼' : '▲'
}

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

// 最近每張成本（每股×1000）以「萬」呈現，小資可負擔一目了然
function lotCost(v) {
  return v == null ? '—' : (Number(v) * 1000 / 10000).toFixed(1) + ' 萬'
}

async function load() {
  inFlight?.abort()
  const controller = new AbortController()
  inFlight = controller
  loading.value = true
  error.value = ''
  try {
    const maxPrice = budget.value ? budget.value / 1000 : null // 總預算 → 買得起一張的每股價上限
    rows.value = await rankQuotes({ q: keyword.value, sort: sort.value, dir: dir.value, top: 30, maxPrice }, controller.signal)
  } catch (e) {
    if (e.name === 'AbortError') return
    error.value = e.message || '排行查詢失敗'
    rows.value = []
  } finally {
    if (inFlight === controller) { loading.value = false; inFlight = null }
  }
}

watch(sort, load)
watch(dir, load)
watch(budget, load)
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
      以「每元當日報酬＝漲跌 ÷ 昨收」為基礎，近一個月每日行情彙總；資料範圍：0050 與成分股。<br>
      「小資總預算」以買得起一張（1000 股）為準，篩出小資也買得起的範圍。<br>
      點「<strong>易入手波段分</strong>」欄＝（報酬÷波動）÷波段週期×離低點程度：自動偏好「高報酬、低風險、短週期、目前接近低檔」——又快、又穩、又划算、進場點佳。
    </p>
    <p class="disclaimer">
      ⚠️ 所有指標皆由<strong>過去</strong>每日行情統計推估，<strong>過去不代表未來</strong>；僅供學習與分析參考，<strong>非投資建議</strong>。投資有風險，請勿投入無法承受損失的資金。
    </p>

    <div class="rank-controls">
      <label>
        小資總預算
        <select v-model="budget">
          <option v-for="b in BUDGETS" :key="b.label" :value="b.value">{{ b.label }}</option>
        </select>
      </label>
      <input v-model="keyword" type="text" placeholder="篩選代號或名稱（可空）" autocomplete="off" />
    </div>

    <div v-if="noGoodTiming" class="state" style="background:#fff3e0;color:#b26a00;border:1px solid #ffe0a3;border-radius:8px;padding:8px 12px;text-align:left">
      🕒 目前清單中沒有「即將見底」的好時機——<strong>不一定要買，時機不對就觀望</strong>。在市場恐慌見底「之前」找機會，別在這裡硬進場。
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
            <th class="sortable" :class="{ active: sort === 'swingscore' }" @click="toggleSort('swingscore')">
              {{ SORTABLE.swingscore }} <span class="arrow">{{ arrow('swingscore') }}</span>
            </th>
            <th v-if="sort === 'swingscore'">進場時機</th>
            <th v-if="sort === 'swingscore'">波段週期</th>
            <th v-if="sort === 'swingscore'">區間位置</th>
            <th class="sortable" :class="{ active: sort === 'return' }" @click="toggleSort('return')">
              {{ SORTABLE.return }} <span class="arrow">{{ arrow('return') }}</span>
            </th>
            <th class="sortable" :class="{ active: sort === 'sharpe' }" @click="toggleSort('sharpe')">
              {{ SORTABLE.sharpe }} <span class="arrow">{{ arrow('sharpe') }}</span>
            </th>
            <th class="sortable" :class="{ active: sort === 'volatility' }" @click="toggleSort('volatility')">
              {{ SORTABLE.volatility }} <span class="arrow">{{ arrow('volatility') }}</span>
            </th>
            <th class="sortable" :class="{ active: sort === 'avg' }" @click="toggleSort('avg')">
              {{ SORTABLE.avg }} <span class="arrow">{{ arrow('avg') }}</span>
            </th>
            <th class="sortable" :class="{ active: sort === 'daily' }" @click="toggleSort('daily')">
              {{ SORTABLE.daily }} <span class="arrow">{{ arrow('daily') }}</span>
            </th>
            <th>期初→期末收盤</th>
            <th>最近每張成本</th>
            <th>天數</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(r, i) in rows" :key="r.companyCode">
            <td>{{ i + 1 }}</td>
            <td style="font-family: monospace">{{ r.companyCode }}</td>
            <td style="text-align: left">{{ r.companyName || '—' }}</td>
            <td style="font-weight: 600; color: var(--primary, #1b5e20)">{{ r.swingScore == null ? '—' : r.swingScore.toFixed(3) }}</td>
            <td v-if="sort === 'swingscore'" :class="timingClass(r.entryTiming)">
              {{ r.entryTiming || '—' }}<span v-if="r.estDaysToNextTurn != null" style="color:#999"> ·{{ r.estDaysToNextTurn }}天</span>
            </td>
            <td v-if="sort === 'swingscore'">{{ r.cycleDays == null ? '—' : r.cycleDays.toFixed(1) + '天' }}</td>
            <td v-if="sort === 'swingscore'">{{ r.pricePositionPercent == null ? '—' : r.pricePositionPercent.toFixed(0) + '%' }}</td>
            <td :class="pctClass(r.periodReturnPercent)">{{ formatPercent(r.periodReturnPercent) }}</td>
            <td :class="pctClass(r.riskAdjustedReturn)" style="font-weight: 600">{{ r.riskAdjustedReturn == null ? '—' : r.riskAdjustedReturn.toFixed(2) }}</td>
            <td>{{ r.volatilityPercent == null ? '—' : r.volatilityPercent.toFixed(2) + '%' }}</td>
            <td :class="pctClass(r.avgDailyReturnPercent)">{{ formatPercent(r.avgDailyReturnPercent) }}</td>
            <td :class="pctClass(r.lastDayReturnPercent)">{{ formatPercent(r.lastDayReturnPercent) }}</td>
            <td>{{ price(r.firstClose) }} → {{ price(r.lastClose) }}</td>
            <td>{{ lotCost(r.lastClose) }}</td>
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
th.sortable {
  cursor: pointer;
  user-select: none;
  white-space: nowrap;
}
th.sortable:hover {
  color: var(--primary, #1b5e20);
}
th.sortable.active {
  color: var(--primary, #1b5e20);
}
th.sortable .arrow {
  font-size: 0.8em;
  color: #bbb;
}
th.sortable.active .arrow {
  color: var(--primary, #1b5e20);
}
.disclaimer {
  font-size: 0.76rem;
  color: #b26a00;
  background: #fff8e1;
  border: 1px solid #ffe0a3;
  border-radius: 8px;
  padding: 8px 12px;
  margin: 0 0 14px;
  line-height: 1.6;
}
.tm-good { color: #1b5e20; font-weight: 600; }
.tm-wait { color: #b26a00; }
.tm-sell { color: #c62828; font-weight: 600; }
</style>

<script setup>
import { ref, computed } from 'vue'
import { getRevenuesByCompanyCode } from './api/revenues'
import RevenueTable from './components/RevenueTable.vue'
import RevenueChart from './components/RevenueChart.vue'

const code = ref('')
const rows = ref([])
const loading = ref(false)
const error = ref('')
const searched = ref(false)

let inFlight = null // 取消過期請求

// 公司基本資料各列相同，取第一筆作摘要
const summary = computed(() => rows.value[0] ?? null)

async function search() {
  const trimmed = code.value.trim()
  if (!trimmed || loading.value) return

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
    error.value = e.message || '查詢發生未預期錯誤'
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
  <h1>上市公司每月營收查詢</h1>
  <p class="subtitle">資料來源：臺灣證券交易所 OpenAPI（t187ap05_L）</p>

  <div class="card">
    <div class="search-bar">
      <input
        v-model="code"
        type="text"
        inputmode="numeric"
        placeholder="輸入公司代號，例如 2330"
        @keyup.enter="search"
      />
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

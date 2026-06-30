<script setup>
import { formatYearMonth, formatThousand, formatPercent } from '../utils/format'

defineProps({
  // MonthlyRevenueDto 陣列，最新月在前
  rows: { type: Array, required: true },
})

function pctClass(value) {
  if (value == null) return ''
  return value > 0 ? 'pos' : value < 0 ? 'neg' : ''
}
</script>

<template>
  <div class="table-wrap">
    <table>
      <thead>
        <tr>
          <th>資料月份</th>
          <th>當月營收（千元）</th>
          <th>上月營收</th>
          <th>去年同月</th>
          <th>MoM</th>
          <th>YoY</th>
          <th>累計營收</th>
          <th>去年累計</th>
          <th>累計增減</th>
          <th>備註</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="r in rows" :key="r.dataYearMonth">
          <td>{{ formatYearMonth(r.dataYearMonth) }}</td>
          <td>{{ formatThousand(r.currentMonthRevenue) }}</td>
          <td>{{ formatThousand(r.lastMonthRevenue) }}</td>
          <td>{{ formatThousand(r.lastYearMonthRevenue) }}</td>
          <td :class="pctClass(r.moMPercent)">{{ formatPercent(r.moMPercent) }}</td>
          <td :class="pctClass(r.yoYPercent)">{{ formatPercent(r.yoYPercent) }}</td>
          <td>{{ formatThousand(r.cumCurrentRevenue) }}</td>
          <td>{{ formatThousand(r.cumLastYearRevenue) }}</td>
          <td :class="pctClass(r.cumDiffPercent)">{{ formatPercent(r.cumDiffPercent) }}</td>
          <td style="text-align: left">{{ r.remark || '—' }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

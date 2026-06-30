<script setup>
import { onMounted, onBeforeUnmount, ref, watch, nextTick } from 'vue'
import * as echarts from 'echarts/core'
import { BarChart, LineChart } from 'echarts/charts'
import {
  GridComponent,
  TooltipComponent,
  LegendComponent,
} from 'echarts/components'
import { CanvasRenderer } from 'echarts/renderers'
import { formatYearMonth } from '../utils/format'

// 只註冊用到的模組，避免整包 echarts 進 bundle
echarts.use([
  BarChart,
  LineChart,
  GridComponent,
  TooltipComponent,
  LegendComponent,
  CanvasRenderer,
])

const props = defineProps({
  // MonthlyRevenueDto 陣列，最新月在前
  rows: { type: Array, required: true },
})

const el = ref(null)
let chart = null

function render() {
  if (!chart) return

  // 圖表需時間升序，故反轉後端的「最新在前」排序
  const ordered = [...props.rows].reverse()
  const labels = ordered.map((r) => formatYearMonth(r.dataYearMonth))
  // 當月營收原始單位為千元，轉為億元方便閱讀
  const revenueYi = ordered.map((r) =>
    r.currentMonthRevenue == null ? null : +(r.currentMonthRevenue / 100000).toFixed(2)
  )
  const yoy = ordered.map((r) => r.yoYPercent)

  chart.setOption({
    tooltip: { trigger: 'axis' },
    legend: { data: ['當月營收（億元）', 'YoY（%）'] },
    grid: { left: 56, right: 56, top: 48, bottom: 40 },
    xAxis: { type: 'category', data: labels, axisLabel: { rotate: labels.length > 12 ? 45 : 0 } },
    yAxis: [
      { type: 'value', name: '億元' },
      { type: 'value', name: 'YoY %', axisLabel: { formatter: '{value}%' } },
    ],
    series: [
      { name: '當月營收（億元）', type: 'bar', data: revenueYi, itemStyle: { color: '#2563eb' } },
      {
        name: 'YoY（%）',
        type: 'line',
        yAxisIndex: 1,
        data: yoy,
        smooth: true,
        itemStyle: { color: '#d12d36' },
      },
    ],
  })
}

function resize() {
  chart?.resize()
}

onMounted(async () => {
  await nextTick()
  chart = echarts.init(el.value)
  render()
  window.addEventListener('resize', resize)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', resize)
  chart?.dispose()
  chart = null
})

watch(() => props.rows, render, { deep: false })
</script>

<template>
  <div ref="el" class="chart"></div>
</template>

<style scoped>
.chart {
  width: 100%;
  height: 360px;
}
</style>

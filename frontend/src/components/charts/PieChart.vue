<template>
  <Doughnut :data="data" :options="options" />
</template>

<script setup lang="ts">
import { ArcElement, Chart as ChartJS, Legend, Title, Tooltip } from 'chart.js'
import { computed } from 'vue'
import { Doughnut } from 'vue-chartjs'
import type { ChartOptions, TooltipItem } from 'chart.js'
import type { ChartPayload, ChartSettings } from '../../utils/chartDataUtils'
import { chartColors } from '../../utils/chartDataUtils'

ChartJS.register(ArcElement, Title, Tooltip, Legend)

const props = defineProps<{
  payload: ChartPayload
  settings: ChartSettings
}>()

const data = computed(() => ({
  labels: props.payload.labels,
  datasets: [
    {
      data: props.payload.values,
      backgroundColor: chartColors(props.payload.values.length),
      borderColor: '#fff',
      borderWidth: 2
    }
  ]
}))

const options = computed<ChartOptions<'doughnut'>>(() => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: props.settings.showLegend, position: 'bottom' },
    tooltip: {
      enabled: props.settings.showTooltips,
      callbacks: {
        label(context: TooltipItem<'doughnut'>) {
          const total = props.payload.values.reduce((sum, value) => sum + value, 0)
          const parsed = Number(context.parsed)
          const percent = total > 0 ? ` (${Math.round((parsed / total) * 100)}%)` : ''
          return `${context.label}: ${parsed}${props.settings.showPercentages ? percent : ''}`
        }
      }
    }
  }
}))
</script>

<template>
  <Scatter :data="data" :options="options" />
</template>

<script setup lang="ts">
import { Chart as ChartJS, Legend, LinearScale, PointElement, Title, Tooltip } from 'chart.js'
import { computed } from 'vue'
import { Scatter } from 'vue-chartjs'
import type { ChartOptions, TooltipItem } from 'chart.js'
import type { ChartPayload, ChartSettings } from '../../utils/chartDataUtils'

ChartJS.register(LinearScale, PointElement, Title, Tooltip, Legend)

const props = defineProps<{
  payload: ChartPayload
  settings: ChartSettings
  title: string
}>()

const data = computed(() => ({
  datasets: [
    {
      label: props.title,
      data: props.payload.points.map((point) => ({ x: point.x, y: point.y })),
      pointRadius: props.settings.pointSize,
      backgroundColor: '#1f6f5b'
    }
  ]
}))

const options = computed<ChartOptions<'scatter'>>(() => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: props.settings.showLegend },
    tooltip: {
      enabled: props.settings.showTooltips,
      callbacks: {
        label(context: TooltipItem<'scatter'>) {
          const point = props.payload.points[context.dataIndex]
          return `${point?.label ?? 'Point'}: ${context.parsed.x}, ${context.parsed.y}`
        }
      }
    }
  },
  scales: {
    x: {
      display: props.settings.showAxisLabels,
      grid: { display: props.settings.showGrid }
    },
    y: {
      display: props.settings.showAxisLabels,
      grid: { display: props.settings.showGrid }
    }
  }
}))
</script>

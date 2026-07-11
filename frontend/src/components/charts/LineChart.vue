<template>
  <Line :data="data" :options="options" />
</template>

<script setup lang="ts">
import {
  CategoryScale,
  Chart as ChartJS,
  Filler,
  Legend,
  LinearScale,
  LineElement,
  PointElement,
  Title,
  Tooltip
} from 'chart.js'
import { computed } from 'vue'
import { Line } from 'vue-chartjs'
import type { ChartPayload, ChartSettings } from '../../utils/chartDataUtils'

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Filler, Title, Tooltip, Legend)

const props = defineProps<{
  payload: ChartPayload
  settings: ChartSettings
  title: string
  area?: boolean
}>()

const data = computed(() => ({
  labels: props.payload.labels,
  datasets: [
    {
      label: props.title,
      data: props.payload.values,
      borderColor: '#1f6f5b',
      backgroundColor: props.area || props.settings.areaFill ? 'rgba(31, 111, 91, 0.18)' : 'rgba(31, 111, 91, 0.06)',
      borderWidth: props.settings.lineThickness,
      tension: props.settings.smoothLine ? 0.36 : 0,
      pointRadius: props.settings.showMarkers ? 4 : 0,
      fill: props.area || props.settings.areaFill
    }
  ]
}))

const options = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: props.settings.showLegend },
    tooltip: { enabled: props.settings.showTooltips }
  },
  scales: {
    x: {
      display: props.settings.showAxisLabels,
      grid: { display: props.settings.showGrid }
    },
    y: {
      display: props.settings.showAxisLabels,
      grid: { display: props.settings.showGrid },
      beginAtZero: true
    }
  }
}))
</script>

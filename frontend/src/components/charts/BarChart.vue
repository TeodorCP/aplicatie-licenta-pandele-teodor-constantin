<template>
  <Bar :data="data" :options="options" />
</template>

<script setup lang="ts">
import { BarElement, CategoryScale, Chart as ChartJS, Legend, LinearScale, Title, Tooltip } from 'chart.js'
import { computed } from 'vue'
import { Bar } from 'vue-chartjs'
import type { ChartOptions, Plugin, TooltipItem } from 'chart.js'
import type { ChartPayload, ChartSettings } from '../../utils/chartDataUtils'
import { chartColors } from '../../utils/chartDataUtils'

const valueLabelPlugin: Plugin<'bar'> = {
  id: 'businessOpsBarValueLabels',
  afterDatasetsDraw(chart) {
    const showLabels = (chart.options.plugins as { businessOpsValueLabels?: boolean } | undefined)?.businessOpsValueLabels
    if (!showLabels) return

    const { ctx } = chart
    ctx.save()
    ctx.fillStyle = '#17251f'
    ctx.font = '700 11px sans-serif'
    ctx.textAlign = 'center'
    ctx.textBaseline = 'bottom'

    chart.getDatasetMeta(0).data.forEach((element, index) => {
      const value = chart.data.datasets[0].data[index]
      if (typeof value !== 'number') return
      ctx.fillText(formatValue(value), element.x, element.y - 4)
    })

    ctx.restore()
  }
}

ChartJS.register(CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend, valueLabelPlugin)

const props = defineProps<{
  payload: ChartPayload
  settings: ChartSettings
  title: string
}>()

const data = computed(() => ({
  labels: props.payload.labels,
  datasets: [
    {
      label: props.title,
      data: props.payload.values,
      backgroundColor: chartColors(props.payload.values.length),
      borderRadius: 8
    }
  ]
}))

const options = computed<ChartOptions<'bar'>>(() => ({
  indexAxis: props.settings.horizontalBars ? 'y' : 'x',
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: props.settings.showLegend },
    tooltip: {
      enabled: props.settings.showTooltips,
      callbacks: {
        label(context: TooltipItem<'bar'>) {
          const value = typeof context.parsed.y === 'number' ? context.parsed.y : context.parsed.x
          return `${props.payload.valueLabel || props.title}: ${formatValue(Number(value))}`
        }
      }
    },
    businessOpsValueLabels: props.settings.showValueLabels
  } as ChartOptions<'bar'>['plugins'] & { businessOpsValueLabels: boolean },
  scales: {
    x: {
      display: props.settings.showAxisLabels,
      grid: { display: props.settings.showGrid },
      beginAtZero: props.settings.horizontalBars
    },
    y: {
      display: props.settings.showAxisLabels,
      grid: { display: props.settings.showGrid },
      beginAtZero: !props.settings.horizontalBars
    }
  }
}))

function formatValue(value: number) {
  return new Intl.NumberFormat(undefined, { maximumFractionDigits: 2 }).format(value)
}
</script>

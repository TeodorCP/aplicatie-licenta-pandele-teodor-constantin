<template>
  <article class="chart-card" :class="sizeClass" :style="cardStyle">
    <header>
      <div>
        <p>{{ module?.name ?? 'Module' }}</p>
        <h3>{{ visualization.title }}</h3>
        <span v-if="settings.description">{{ settings.description }}</span>
      </div>
      <slot name="actions" />
    </header>

    <div v-if="payload.emptyReason" class="empty-state">{{ payload.emptyReason }}</div>
    <SummaryCard
      v-else-if="normalizedType === 'summary'"
      :value="payload.summaryValue"
      :label="payload.summaryLabel || visualization.xField || visualization.fieldName"
    />
    <div v-else class="chart-shell">
      <LineChart
        v-if="normalizedType === 'line' || normalizedType === 'area'"
        :payload="payload"
        :settings="settings"
        :title="visualization.title"
        :area="normalizedType === 'area'"
      />
      <BarChart
        v-else-if="normalizedType === 'bar'"
        :payload="payload"
        :settings="settings"
        :title="visualization.title"
      />
      <PieChart v-else-if="normalizedType === 'pie'" :payload="payload" :settings="settings" />
      <ScatterChart
        v-else-if="normalizedType === 'scatter'"
        :payload="payload"
        :settings="settings"
        :title="visualization.title"
      />
      <BarChart v-else :payload="payload" :settings="settings" :title="visualization.title" />
    </div>
    <footer v-if="normalizedType === 'bar' && !payload.emptyReason" class="chart-total">
      <span>{{ payload.summaryLabel }}</span>
      <strong>{{ formattedSummary }}</strong>
    </footer>
  </article>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { BusinessModule, Entry, Visualization } from '../../models/api'
import type { ChartSettings } from '../../utils/chartDataUtils'
import { buildChartPayload } from '../../utils/chartDataUtils'
import BarChart from './BarChart.vue'
import LineChart from './LineChart.vue'
import PieChart from './PieChart.vue'
import ScatterChart from './ScatterChart.vue'
import SummaryCard from './SummaryCard.vue'

const props = defineProps<{
  visualization: Visualization
  module?: BusinessModule
  entries: Entry[]
  settings: ChartSettings
}>()

const normalizedType = computed(() =>
  props.visualization.chartType.toLowerCase() === 'summary_card' ? 'summary' : props.visualization.chartType.toLowerCase()
)
const payload = computed(() => buildChartPayload(props.visualization, props.module, props.entries, props.settings))
const sizeClass = computed(() => `chart-card--${props.visualization.widgetSize || 'medium'}`)
const cardStyle = computed(() => ({
  gridColumn: `span ${clampDimension(props.settings.width, 4, 12)}`,
  minHeight: `${clampDimension(props.settings.height, 3, 12) * 120}px`
}))
const formattedSummary = computed(() =>
  new Intl.NumberFormat(undefined, { maximumFractionDigits: 2 }).format(payload.value.summaryValue)
)

function clampDimension(value: number, min: number, max: number) {
  return Math.min(max, Math.max(min, Number.isFinite(value) ? value : min))
}
</script>

<style scoped>
.chart-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-card);
  box-shadow: var(--shadow-card);
  display: grid;
  gap: 14px;
  grid-template-rows: auto minmax(0, 1fr) auto;
  min-height: 340px;
  min-width: 0;
  overflow: hidden;
  padding: 18px;
}

.chart-card--small {
  min-height: 280px;
}

.chart-card--large {
  min-height: 460px;
}

header {
  align-items: flex-start;
  display: flex;
  gap: 12px;
  justify-content: space-between;
}

p,
span {
  color: var(--color-muted);
  margin: 0;
}

p {
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

h3 {
  font-size: 18px;
  margin: 3px 0 0;
}

.chart-shell {
  height: 100%;
  min-height: 280px;
  min-width: 0;
  position: relative;
}

.chart-card--large .chart-shell {
  min-height: 380px;
}

.empty-state {
  align-items: center;
  border: 1px dashed var(--color-border);
  border-radius: 14px;
  color: var(--color-muted);
  display: flex;
  justify-content: center;
  min-height: 220px;
  padding: 24px;
  text-align: center;
}

.chart-total {
  align-items: center;
  background: rgba(31, 111, 91, 0.07);
  border: 1px solid rgba(31, 111, 91, 0.12);
  border-radius: 12px;
  display: flex;
  justify-content: space-between;
  padding: 10px 12px;
}

.chart-total span {
  color: var(--color-muted);
  font-weight: 800;
}

.chart-total strong {
  color: var(--color-primary-dark);
  font-size: 18px;
}

@media (max-width: 760px) {
  .chart-card {
    grid-column: 1 / -1 !important;
  }
}
</style>

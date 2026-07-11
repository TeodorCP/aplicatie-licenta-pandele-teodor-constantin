<template>
  <div class="settings-panel">
    <section>
      <h3>General Chart Options</h3>
      <div v-if="showDimensions" class="dimension-grid">
        <label class="field slider-field">
          <span>Width: <strong>{{ settings.width }}</strong></span>
          <input v-model.number="settings.width" max="12" min="1" step="1" type="range" />
        </label>
        <label class="field slider-field">
          <span>Height: <strong>{{ settings.height }}</strong></span>
          <input v-model.number="settings.height" max="12" min="1" step="1" type="range" />
        </label>
      </div>
      <div class="toggle-grid">
        <label v-for="item in generalToggles" :key="item.key" class="check">
          <input v-model="settings[item.key]" type="checkbox" />
          <span>{{ item.label }}</span>
        </label>
      </div>
    </section>

    <section v-if="chartType === 'line' || chartType === 'area'">
      <h3>Chart Specific Options</h3>
      <label class="check">
        <input v-model="settings.smoothLine" type="checkbox" />
        <span>Smooth line</span>
      </label>
      <label class="check">
        <input v-model="settings.areaFill" type="checkbox" />
        <span>Area fill</span>
      </label>
      <label class="field">
        <span>Line thickness</span>
        <input v-model.number="settings.lineThickness" min="1" max="8" type="range" />
      </label>
    </section>

    <section v-if="chartType === 'bar'">
      <h3>Chart Specific Options</h3>
      <label class="check">
        <input v-model="settings.horizontalBars" type="checkbox" />
        <span>Horizontal orientation</span>
      </label>
    </section>

    <section v-if="chartType === 'scatter'">
      <h3>Chart Specific Options</h3>
      <label class="field">
        <span>Point size</span>
        <input v-model.number="settings.pointSize" min="2" max="12" type="range" />
      </label>
    </section>

    <section v-if="chartType === 'pie'">
      <h3>Chart Specific Options</h3>
      <label class="check">
        <input v-model="settings.showPercentages" type="checkbox" />
        <span>Show percentages</span>
      </label>
      <label class="check">
        <input v-model="settings.groupSmallCategories" type="checkbox" />
        <span>Group small categories into Other</span>
      </label>
    </section>
  </div>
</template>

<script setup lang="ts">
import type { ChartSettings } from '../../utils/chartDataUtils'

withDefaults(
  defineProps<{
    chartType: string
    showDimensions?: boolean
  }>(),
  {
    showDimensions: true
  }
)
const settings = defineModel<ChartSettings>({ required: true })

const generalToggles: Array<{ key: keyof ChartSettings; label: string }> = [
  { key: 'showLegend', label: 'Legend' },
  { key: 'showGrid', label: 'Grid lines' },
  { key: 'showMarkers', label: 'Markers' },
  { key: 'showAxisLabels', label: 'Axis labels' },
  { key: 'showTooltips', label: 'Tooltips' },
  { key: 'showValueLabels', label: 'Value labels' }
]
</script>

<style scoped>
.settings-panel {
  display: grid;
  gap: 16px;
}

section {
  border-top: 1px solid var(--color-border);
  display: grid;
  gap: 10px;
  padding-top: 14px;
}

h3 {
  font-size: 14px;
  margin: 0;
  text-transform: uppercase;
  color: var(--color-muted);
}

.toggle-grid {
  display: grid;
  gap: 8px;
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.check {
  align-items: center;
  display: flex;
  gap: 8px;
}

.wide {
  grid-column: 1 / -1;
}

.dimension-grid {
  display: grid;
  gap: 12px;
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.slider-field strong {
  color: var(--color-primary-dark);
}

.slider-field input {
  accent-color: var(--color-primary);
}
</style>

<template>
  <div>
    <PageHeader
      :title="editing ? 'Customize visualization' : 'Create visualization'"
      subtitle="Configure chart structure, date scope, and display options while the preview updates live."
      eyebrow="Visualization Studio"
    >
      <template #actions>
        <button class="button secondary" type="button" @click="router.push('/tools/visualizations')">Back</button>
        <button class="button" type="button" @click="save">{{ editing ? 'Save changes' : 'Save visualization' }}</button>
      </template>
    </PageHeader>

    <section class="studio">
      <ContentCard>
        <div class="form-grid">
          <label class="field">
            <span>Title</span>
            <input v-model="form.title" required />
          </label>
          <label class="field">
            <span>Module</span>
            <select v-model="form.moduleId" :disabled="store.loading" required>
              <option disabled value="">Select module</option>
              <option v-for="module in store.modules" :key="module.id" :value="module.id">{{ module.name }}</option>
            </select>
          </label>
          <label class="field">
            <span>X Field</span>
            <select v-model="form.xField" required>
              <option disabled value="">Select field</option>
              <option v-for="field in xFields" :key="field.name" :value="field.name">{{ field.name }}</option>
            </select>
          </label>
          <label v-if="xGroupingOptions.length > 0" class="field">
            <span>X Field Grouping</span>
            <select v-model="form.xGrouping">
              <option v-for="option in xGroupingOptions" :key="option.value" :value="option.value">{{ option.label }}</option>
            </select>
          </label>
          <label class="field">
            <span>Y Field</span>
            <select v-model="form.yField">
              <option value="">None</option>
              <option v-for="field in yFields" :key="field.name" :value="field.name">{{ field.name }}</option>
            </select>
          </label>
          <label v-if="numericGroupingOptions.length > 0" class="field">
            <span>{{ numericGroupingLabel }}</span>
            <select v-model="form.numericGrouping">
              <option v-for="option in numericGroupingOptions" :key="option.value" :value="option.value">{{ option.label }}</option>
            </select>
          </label>
          <label class="field">
            <span>Chart Type</span>
            <select v-model="form.chartType">
              <option v-for="type in chartTypes" :key="type" :value="type">{{ typeLabel(type) }}</option>
            </select>
          </label>
          <label class="field">
            <span>Date Range</span>
            <select v-model="form.dateRangeType">
              <option value="all">All time</option>
              <option value="7d">Last 7 days</option>
              <option value="30d">Last 30 days</option>
              <option value="90d">Last 90 days</option>
              <option value="year">Last year</option>
              <option value="custom">Custom range</option>
            </select>
          </label>
          <label class="field wide">
            <span>Description</span>
            <textarea v-model="form.description" rows="3" placeholder="Helpful context for this visualization" />
          </label>
          <label v-if="form.dateRangeType === 'custom'" class="field">
            <span>Custom Start</span>
            <input v-model="form.customStartTimestamp" type="datetime-local" />
          </label>
          <label v-if="form.dateRangeType === 'custom'" class="field">
            <span>Custom End</span>
            <input v-model="form.customEndTimestamp" type="datetime-local" />
          </label>
        </div>

        <ChartSettingsPanel v-model="form.settings" :chart-type="form.chartType" :show-dimensions="false" />

        <p v-if="error" class="error">{{ error }}</p>
      </ContentCard>

      <ChartRenderer
        :visualization="previewVisualization"
        :module="selectedModule"
        :entries="selectedEntries"
        :settings="previewSettings"
      />
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ChartRenderer from '../../components/charts/ChartRenderer.vue'
import ChartSettingsPanel from '../../components/charts/ChartSettingsPanel.vue'
import ContentCard from '../../components/common/ContentCard.vue'
import PageHeader from '../../components/common/PageHeader.vue'
import type { Visualization } from '../../models/api'
import {
  toVisualizationForm,
  toVisualizationPayload,
  type VisualizationForm,
  visualizationService
} from '../../services/visualizationService'
import { useBusinessStore } from '../../stores/businessStore'
import { inferCompatibleChartTypes } from '../../utils/chartDataUtils'

const route = useRoute()
const router = useRouter()
const store = useBusinessStore()
const error = ref('')
const editingId = computed(() => String(route.params.id ?? ''))
const editing = computed(() => Boolean(editingId.value))
const form = reactive<VisualizationForm>(toVisualizationForm())
const selectedModule = computed(() => store.modules.find((module) => module.id === form.moduleId))
const selectedEntries = computed(() => (form.moduleId ? store.entriesFor(form.moduleId) : []))
const xFields = computed(() => selectedModule.value?.fields ?? [])
const yFields = computed(() => (selectedModule.value?.fields ?? []).filter((field) => field.name !== form.xField))
const chartTypes = computed(() => inferCompatibleChartTypes(selectedModule.value, form.xField))
const selectedXField = computed(() => xFields.value.find((field) => field.name === form.xField))
const selectedYField = computed(() => yFields.value.find((field) => field.name === form.yField))
const numericGroupingFieldType = computed(() => {
  if (form.chartType === 'scatter') return ''
  if (isNumericType(selectedYField.value?.type)) return selectedYField.value?.type ?? ''
  if (!form.yField && form.chartType === 'summary' && isNumericType(selectedXField.value?.type)) {
    return selectedXField.value?.type ?? ''
  }
  return ''
})
const xGroupingOptions = computed(() => {
  if (!['bar', 'line', 'area'].includes(form.chartType)) return []
  return groupingOptionsFor(selectedXField.value?.type, 'x')
})
const numericGroupingOptions = computed(() => groupingOptionsFor(numericGroupingFieldType.value, 'numeric'))
const numericGroupingLabel = computed(() => {
  const sourceLabel = selectedYField.value?.name ?? selectedXField.value?.name ?? 'Field'
  return `${sourceLabel} Grouping`
})
const previewSettings = computed(() => ({
  ...form.settings,
  description: form.description
}))
const previewVisualization = computed<Visualization>(() => ({
  id: editingId.value || 'preview',
  moduleId: form.moduleId,
  module: selectedModule.value ? { name: selectedModule.value.name } : null,
  title: form.title || 'Untitled visualization',
  xField: form.xField || null,
  xAggregation: form.xGrouping === 'none' ? null : form.xGrouping,
  yField: form.yField || null,
  fieldName: form.xField,
  secondaryFieldName: form.yField || null,
  chartType: form.chartType,
  widgetSize: form.widgetSize,
  aggregationType: form.xGrouping === 'none' ? null : form.xGrouping,
  dateRangeType: form.dateRangeType,
  dateRange: form.dateRangeType,
  customStartTimestamp: form.dateRangeType === 'custom' && form.customStartTimestamp ? new Date(form.customStartTimestamp).toISOString() : null,
  customEndTimestamp: form.dateRangeType === 'custom' && form.customEndTimestamp ? new Date(form.customEndTimestamp).toISOString() : null,
  summaryMetric: form.numericGrouping,
  description: form.description || null,
  generalOptions: null,
  chartSpecificOptions: null,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString()
}))

onMounted(async () => {
  await store.loadAll()
  const existing = store.visualizations.find((visualization) => visualization.id === editingId.value)
  Object.assign(form, toVisualizationForm(existing))

  if (!form.moduleId) form.moduleId = store.modules[0]?.id ?? ''
  if (!form.xField) form.xField = selectedModule.value?.fields[0]?.name ?? ''
  if (form.moduleId) await store.loadEntries(form.moduleId)
})

watch(
  () => form.moduleId,
  async (moduleId, previousModuleId) => {
    if (!moduleId) return
    if (moduleId !== previousModuleId) {
      const availableFields = selectedModule.value?.fields ?? []
      if (!availableFields.some((field) => field.name === form.xField)) {
        form.xField = availableFields[0]?.name ?? ''
      }
      if (!availableFields.some((field) => field.name === form.yField)) {
        form.yField = ''
      }
    }
    await store.loadEntries(moduleId)
  }
)

watch(
  () => form.xField,
  () => {
    if (form.yField === form.xField) {
      form.yField = ''
    }
  }
)

watch(xGroupingOptions, (options) => {
  const validValues = options.map((option) => option.value)
  if (options.length === 0) {
    form.xGrouping = 'none'
    return
  }

  if (!validValues.includes(form.xGrouping)) {
    form.xGrouping = options[0]?.value ?? 'none'
  }
})

watch(numericGroupingOptions, (options) => {
  const validValues = options.map((option) => option.value)
  if (options.length === 0) {
    form.numericGrouping = 'sum'
    return
  }

  if (!validValues.includes(form.numericGrouping)) {
    form.numericGrouping = options[0]?.value ?? 'sum'
  }
})

watch(chartTypes, () => {
  if (!chartTypes.value.includes(form.chartType)) {
    form.chartType = chartTypes.value[0] ?? 'bar'
  }
})

watch(
  () => form.dateRangeType,
  (value) => {
    if (value !== 'custom') {
      form.customStartTimestamp = ''
      form.customEndTimestamp = ''
    }
  }
)

async function save() {
  error.value = ''
  if (!form.title.trim() || !form.moduleId || !form.xField) {
    error.value = 'Title, module, and X field are required.'
    return
  }

  if (form.dateRangeType === 'custom') {
    if (!form.customStartTimestamp || !form.customEndTimestamp) {
      error.value = 'Custom date range needs both a start and an end.'
      return
    }

    if (new Date(form.customStartTimestamp) > new Date(form.customEndTimestamp)) {
      error.value = 'Custom start must be before the custom end.'
      return
    }
  }

  const payload = toVisualizationPayload(form)
  if (editing.value) {
    await visualizationService.update(editingId.value, payload)
  } else {
    await visualizationService.create(payload)
  }

  store.visualizations = await visualizationService.list()
  await router.push('/tools/visualizations')
}

function typeLabel(type: string) {
  return type === 'area' ? 'Area' : type.replace(/\b\w/g, (letter) => letter.toUpperCase())
}

function groupingOptionsFor(type: string | undefined, axis: 'x' | 'numeric') {
  if (axis === 'x' && isTimestampType(type)) {
    return [
      { value: 'none', label: 'None' },
      { value: 'daily', label: 'Daily' },
      { value: 'weekly', label: 'Weekly' },
      { value: 'monthly', label: 'Monthly' },
      { value: 'yearly', label: 'Yearly' }
    ]
  }

  if (axis === 'numeric' && isNumericType(type)) {
    return [
      { value: 'sum', label: 'Sum' },
      { value: 'avg', label: 'Average' },
      { value: 'min', label: 'Minimum' },
      { value: 'max', label: 'Maximum' },
      { value: 'count', label: 'Count' }
    ]
  }

  return []
}

function isTimestampType(type?: string) {
  const normalized = type?.toLowerCase() ?? ''
  return ['date', 'time', 'datetime', 'timestamp'].includes(normalized)
}

function isNumericType(type?: string) {
  const normalized = type?.toLowerCase() ?? ''
  return ['number', 'currency', 'decimal', 'integer'].includes(normalized)
}
</script>

<style scoped>
.studio {
  align-items: start;
  display: grid;
  gap: 16px;
  grid-template-columns: minmax(380px, 0.9fr) minmax(0, 1.1fr);
}

.form-grid {
  display: grid;
  gap: 12px;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  margin-bottom: 18px;
}

.wide {
  grid-column: 1 / -1;
}

@media (max-width: 1100px) {
  .studio,
  .form-grid {
    grid-template-columns: 1fr;
  }
}
</style>

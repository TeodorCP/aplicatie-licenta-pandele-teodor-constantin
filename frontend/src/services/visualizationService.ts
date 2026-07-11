import type { DashboardWidget, Visualization } from '../models/api'
import type { ChartSettings } from '../utils/chartDataUtils'
import { defaultChartSettings } from '../utils/chartDataUtils'
import * as apiService from './apiService'

const settingsKey = 'business_ops_visualization_settings'

const generalOptionKeys = [
  'width',
  'height',
  'showLegend',
  'showGrid',
  'showMarkers',
  'showAxisLabels',
  'showTooltips',
  'showValueLabels'
] as const satisfies ReadonlyArray<keyof ChartSettings>

const chartSpecificOptionKeys = [
  'smoothLine',
  'lineThickness',
  'areaFill',
  'horizontalBars',
  'pointSize',
  'showPercentages',
  'groupSmallCategories'
] as const satisfies ReadonlyArray<keyof ChartSettings>

export type VisualizationForm = {
  title: string
  moduleId: string
  xField: string
  xGrouping: string
  yField: string
  chartType: string
  widgetSize: string
  dateRangeType: string
  customStartTimestamp: string
  customEndTimestamp: string
  numericGrouping: string
  description: string
  settings: ChartSettings
}

export const visualizationService = {
  list: apiService.getVisualizations,
  create: apiService.createVisualization,
  update: apiService.updateVisualization,
  remove: apiService.deleteVisualization,
  listWidgets: apiService.getDashboardWidgets,
  createWidget: apiService.createDashboardWidget,
  updateWidget: apiService.updateDashboardWidget,
  removeWidget: apiService.deleteDashboardWidget,
  getSettings,
  removeSettings
}

export function toVisualizationPayload(form: VisualizationForm) {
  const xField = form.xField.trim()
  const yField = form.yField.trim() || null
  const dateRangeType = form.dateRangeType || 'all'
  const xGrouping = form.xGrouping === 'none' ? null : form.xGrouping

  return {
    title: form.title.trim(),
    moduleId: form.moduleId,
    xField,
    xAggregation: xGrouping,
    yField,
    fieldName: xField,
    secondaryFieldName: yField,
    chartType: normalizeChartType(form.chartType),
    widgetSize: form.widgetSize,
    aggregationType: xGrouping,
    dateRangeType,
    dateRange: dateRangeType,
    customStartTimestamp: dateRangeType === 'custom' && form.customStartTimestamp ? toIsoString(form.customStartTimestamp) : null,
    customEndTimestamp: dateRangeType === 'custom' && form.customEndTimestamp ? toIsoString(form.customEndTimestamp) : null,
    summaryMetric: form.numericGrouping || null,
    description: form.description.trim() || null,
    generalOptions: pickSettings(form.settings, generalOptionKeys),
    chartSpecificOptions: pickSettings(form.settings, chartSpecificOptionKeys)
  }
}

export function toVisualizationForm(visualization?: Visualization | null): VisualizationForm {
  const settings = getSettings(visualization)

  return {
    title: visualization?.title ?? '',
    moduleId: visualization?.moduleId ?? '',
    xField: visualization?.xField ?? visualization?.fieldName ?? '',
    xGrouping: normalizeTimeGrouping(visualization?.xAggregation ?? visualization?.aggregationType),
    yField: visualization?.yField ?? visualization?.secondaryFieldName ?? '',
    chartType: normalizeChartType(visualization?.chartType ?? 'bar'),
    widgetSize: visualization?.widgetSize ?? 'medium',
    dateRangeType: visualization?.dateRangeType ?? visualization?.dateRange ?? 'all',
    customStartTimestamp: toDatetimeLocalValue(visualization?.customStartTimestamp),
    customEndTimestamp: toDatetimeLocalValue(visualization?.customEndTimestamp),
    numericGrouping: normalizeNumericGrouping(
      visualization?.summaryMetric ?? visualization?.xAggregation ?? visualization?.aggregationType
    ),
    description: visualization?.description ?? settings.description ?? '',
    settings
  }
}

export function getSettings(source?: Visualization | string | null): ChartSettings {
  const legacySettings = typeof source === 'string' ? readSettings()[source] : readSettings()[source?.id ?? '']
  const visualization = typeof source === 'string' ? undefined : source
  const merged = {
    ...defaultChartSettings,
    ...coerceSettings(visualization?.generalOptions),
    ...coerceSettings(visualization?.chartSpecificOptions),
    ...legacySettings
  }

  return {
    ...merged,
    description: visualization?.description ?? merged.description ?? ''
  }
}

function removeSettings(id: string) {
  const allSettings = readSettings()
  delete allSettings[id]
  localStorage.setItem(settingsKey, JSON.stringify(allSettings))
}

function readSettings(): Record<string, Partial<ChartSettings>> {
  try {
    const raw = localStorage.getItem(settingsKey)
    return raw ? JSON.parse(raw) : {}
  } catch {
    return {}
  }
}

function normalizeChartType(value: string) {
  const normalized = value.toLowerCase()
  return normalized === 'summary_card' ? 'summary' : normalized
}

function normalizeTimeGrouping(value?: string | null) {
  const normalized = value?.toLowerCase() ?? ''
  return ['daily', 'weekly', 'monthly', 'yearly'].includes(normalized) ? normalized : 'none'
}

function normalizeNumericGrouping(value?: string | null) {
  const normalized = value?.toLowerCase() ?? ''
  if (normalized === 'average') return 'avg'
  return ['sum', 'avg', 'min', 'max', 'count'].includes(normalized) ? normalized : 'sum'
}

function pickSettings(settings: ChartSettings, keys: ReadonlyArray<keyof ChartSettings>) {
  return keys.reduce<Record<string, unknown>>((result, key) => {
    result[key] = settings[key]
    return result
  }, {})
}

function coerceSettings(source?: Record<string, unknown> | null) {
  if (!source) return {}
  return Object.fromEntries(
    Object.entries(source).filter(([, value]) => value !== undefined && value !== null)
  ) as Partial<ChartSettings>
}

function toIsoString(value: string) {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? null : date.toISOString()
}

function toDatetimeLocalValue(value?: string | null) {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const timezoneOffset = date.getTimezoneOffset() * 60000
  return new Date(date.getTime() - timezoneOffset).toISOString().slice(0, 16)
}

export function nextWidgetPosition(widgets: DashboardWidget[]) {
  return widgets.reduce((max, widget) => Math.max(max, widget.position), 0) + 1
}

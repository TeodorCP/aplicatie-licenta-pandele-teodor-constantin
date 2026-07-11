import type { BusinessModule, Entry, Visualization } from '../models/api'
import { aggregateNumbers, bucketDate, countCategories } from './aggregationUtils'
import { findTimestampField, isInsideDateRange, isTimestampType, parseDate } from './timestampUtils'

export type ChartSettings = {
  description: string
  width: number
  height: number
  showLegend: boolean
  showGrid: boolean
  showMarkers: boolean
  showAxisLabels: boolean
  showTooltips: boolean
  showValueLabels: boolean
  smoothLine: boolean
  lineThickness: number
  areaFill: boolean
  horizontalBars: boolean
  pointSize: number
  showPercentages: boolean
  groupSmallCategories: boolean
}

export type ChartPayload = {
  labels: string[]
  values: number[]
  points: Array<{ x: number; y: number; label: string }>
  summaryValue: number
  summaryLabel: string
  valueLabel: string
  emptyReason: string
}

export const defaultChartSettings: ChartSettings = {
  description: '',
  width: 6,
  height: 3,
  showLegend: true,
  showGrid: true,
  showMarkers: true,
  showAxisLabels: true,
  showTooltips: true,
  showValueLabels: false,
  smoothLine: false,
  lineThickness: 3,
  areaFill: false,
  horizontalBars: false,
  pointSize: 5,
  showPercentages: true,
  groupSmallCategories: true
}

export function buildChartPayload(
  visualization: Pick<
    Visualization,
    | 'xField'
    | 'xAggregation'
    | 'yField'
    | 'fieldName'
    | 'secondaryFieldName'
    | 'chartType'
    | 'aggregationType'
    | 'dateRange'
    | 'dateRangeType'
    | 'customStartTimestamp'
    | 'customEndTimestamp'
    | 'summaryMetric'
  >,
  module: BusinessModule | undefined,
  entries: Entry[],
  settings: ChartSettings
): ChartPayload {
  if (!module) return emptyPayload('Module not available.')
  if (entries.length === 0) return emptyPayload('No records found for this module.')

  const xFieldName = visualization.xField ?? visualization.fieldName
  const yFieldName = visualization.yField ?? visualization.secondaryFieldName ?? ''
  const xAggregation = visualization.xAggregation ?? visualization.aggregationType
  const dateRangeType = visualization.dateRangeType ?? visualization.dateRange ?? 'all'
  const xField = module.fields.find((field) => field.name === xFieldName)
  const yField = module.fields.find((field) => field.name === yFieldName)
  const timestampFieldName = findTimestampField(module.fields)
  const filterFieldName = isTimestampType(xField?.type ?? '') ? xFieldName : timestampFieldName
  const filteredEntries = entries.filter((entry) =>
    isInsideDateRange(
      filterFieldName ? entry.data[filterFieldName] : entry.timestamp,
      dateRangeType,
      visualization.customStartTimestamp,
      visualization.customEndTimestamp
    )
  )

  if (filteredEntries.length === 0) return emptyPayload('No records match the selected date range.')

  const chartType = normalizeChartType(visualization.chartType)
  const numericX = isNumericField(xField?.type)
  const numericY = isNumericField(yField?.type)
  const timestampX = isTimestampType(xField?.type ?? '')
  const timeSeriesChart = ['line', 'area'].includes(chartType)

  if (chartType === 'scatter') {
    const points = filteredEntries
      .map((entry) => ({
        x: toNumber(entry.data[xFieldName]),
        y: toNumber(yFieldName ? entry.data[yFieldName] : undefined),
        label: String(entry.data[timestampFieldName ?? xFieldName] ?? entry.timestamp)
      }))
      .filter((point) => Number.isFinite(point.x) && Number.isFinite(point.y))

    return points.length ? { ...emptyPayload(''), points } : emptyPayload('Scatter plots need two numeric fields.')
  }

  if (timeSeriesChart && timestampX) {
    if (!numericY) return buildTimestampCountPayload(filteredEntries, xFieldName, xAggregation)
    return buildTimeSeriesPayload(
      filteredEntries,
      xFieldName,
      yFieldName,
      xAggregation,
      visualization.summaryMetric,
      'No numeric values found for this time chart.'
    )
  }

  if (chartType === 'bar') {
    if (timestampX) {
      return numericY
        ? buildTimeSeriesPayload(filteredEntries, xFieldName, yFieldName, xAggregation, visualization.summaryMetric, 'No numeric values found for this bar chart.')
        : buildTimestampCountPayload(filteredEntries, xFieldName, xAggregation)
    }

    if (numericY) {
      return buildCategoryMetricPayload(
        filteredEntries,
        xFieldName,
        yFieldName,
        visualization.summaryMetric,
        settings.groupSmallCategories,
        'No numeric values found for this bar chart.'
      )
    }

    if (numericX) {
      return fromRows(
        filteredEntries
          .map((entry) => ({
            label: String(entry.data[timestampFieldName ?? xFieldName] ?? entry.timestamp),
            value: toNumber(entry.data[xFieldName])
          }))
          .filter((row) => Number.isFinite(row.value)),
        'No numeric values found for this bar chart.'
      )
    }

    return buildCategoryPayload(filteredEntries, xFieldName, settings.groupSmallCategories)
  }

  if (chartType === 'pie') {
    return numericY
      ? buildCategoryMetricPayload(
          filteredEntries,
          xFieldName,
          yFieldName,
          visualization.summaryMetric,
          settings.groupSmallCategories,
          'No numeric values found for this pie chart.'
        )
      : buildCategoryPayload(filteredEntries, xFieldName, settings.groupSmallCategories)
  }

  if (timestampX) {
    return buildTimestampCountPayload(filteredEntries, xFieldName, xAggregation)
  }

  if (chartType === 'summary') {
    const metricFieldName = numericY ? yFieldName : xFieldName
    const values = filteredEntries.map((entry) => toNumber(entry.data[metricFieldName])).filter(Number.isFinite)
    if (values.length === 0) {
      return {
        ...emptyPayload(''),
        summaryValue: filteredEntries.length,
        summaryLabel: 'Records'
      }
    }

    return {
      ...emptyPayload(''),
      summaryValue: summarize(values, visualization.summaryMetric ?? 'sum'),
      summaryLabel: visualization.summaryMetric ?? 'sum'
    }
  }

  if (numericY && numericX) {
    return buildNumericPairPayload(filteredEntries, xFieldName, yFieldName)
  }

  if (isCategoryField(xField?.type)) {
    return buildCategoryPayload(filteredEntries, xFieldName, settings.groupSmallCategories)
  }

  const rows = filteredEntries
    .map((entry) => ({
      label: String(entry.data[timestampFieldName ?? xFieldName] ?? entry.timestamp),
      value: toNumber(entry.data[xFieldName])
    }))
    .filter((row) => Number.isFinite(row.value))

  return fromRows(rows, 'No numeric values found for this chart.')
}

export function inferCompatibleChartTypes(module: BusinessModule | undefined, fieldName: string) {
  const field = module?.fields.find((item) => item.name === fieldName)
  const type = field?.type.toLowerCase() ?? ''

  if (['number', 'currency'].includes(type)) return ['summary', 'bar', 'line', 'area', 'scatter']
  if (['select', 'boolean'].includes(type)) return ['pie', 'bar', 'summary']
  if (isTimestampType(type)) return ['bar', 'line', 'area', 'summary']
  return ['bar', 'pie', 'summary']
}

function buildTimeSeriesPayload(
  entries: Entry[],
  timestampFieldName: string,
  valueFieldName: string,
  aggregationType: string | null | undefined,
  metric: string | null | undefined,
  emptyReason: string
) {
  const rows = entries
    .map((entry) => {
      const timestamp = entry.data[timestampFieldName] ?? entry.timestamp
      return {
        label: bucketDate(timestamp, normalizeTimeAggregation(aggregationType)),
        sortKey: parseDate(timestamp)?.getTime() ?? 0,
        value: toNumber(entry.data[valueFieldName])
      }
    })
    .filter((row) => Number.isFinite(row.value))
    .sort((a, b) => a.sortKey - b.sortKey)

  const normalizedMetric = normalizeMetric(metric ?? aggregationType)
  return fromRows(
    aggregateNumbersByMetric(rows, normalizedMetric),
    emptyReason,
    normalizedMetric === 'count' ? 'Count' : 'Total amount'
  )
}

function buildTimestampCountPayload(entries: Entry[], timestampFieldName: string, aggregationType: string | null | undefined) {
  const rows = entries
    .map((entry) => {
      const timestamp = entry.data[timestampFieldName] ?? entry.timestamp
      return {
        label: bucketDate(timestamp, normalizeTimeAggregation(aggregationType)),
        sortKey: parseDate(timestamp)?.getTime() ?? 0,
        value: 1
      }
    })
    .filter((row) => row.label !== 'Unscheduled')
    .sort((a, b) => a.sortKey - b.sortKey)

  return fromRows(aggregateNumbers(rows), 'No timestamp values found for this bar chart.', 'Count')
}

function buildCategoryPayload(entries: Entry[], fieldName: string, groupSmallCategories: boolean) {
  const rows = countCategories(
    entries.map((entry) => entry.data[fieldName]),
    groupSmallCategories
  )
  return fromRows(rows, 'No category values found.', 'Count')
}

function buildCategoryMetricPayload(
  entries: Entry[],
  categoryFieldName: string,
  valueFieldName: string,
  metric: string | null | undefined,
  groupSmallCategories: boolean,
  emptyReason: string
) {
  const buckets = new Map<string, number[]>()
  for (const entry of entries) {
    const label = entry.data[categoryFieldName] === null || entry.data[categoryFieldName] === undefined || entry.data[categoryFieldName] === ''
      ? 'Empty'
      : String(entry.data[categoryFieldName])
    const value = toNumber(entry.data[valueFieldName])
    if (!Number.isFinite(value)) continue
    buckets.set(label, [...(buckets.get(label) ?? []), value])
  }

  const rows = Array.from(buckets.entries())
    .map(([label, values]) => ({
      label,
      value: summarize(values, normalizeMetric(metric))
    }))
    .sort((a, b) => b.value - a.value)

  return fromRows(groupSmallNumericCategories(rows, groupSmallCategories), emptyReason, metricLabel(metric))
}

function buildNumericPairPayload(entries: Entry[], xFieldName: string, yFieldName: string) {
  const rows = entries
    .map((entry) => ({
      x: toNumber(entry.data[xFieldName]),
      value: toNumber(entry.data[yFieldName])
    }))
    .filter((row) => Number.isFinite(row.x) && Number.isFinite(row.value))
    .sort((a, b) => a.x - b.x)

  return fromRows(
    rows.map((row) => ({
      label: formatNumericLabel(row.x),
      value: row.value
    })),
    'No numeric values found for this chart.'
  )
}

function isNumericField(type?: string) {
  const normalized = type?.toLowerCase() ?? ''
  return ['number', 'currency', 'decimal', 'integer'].includes(normalized)
}

function isCategoryField(type?: string) {
  const normalized = type?.toLowerCase() ?? ''
  return !isNumericField(normalized) && !isTimestampType(normalized)
}

function normalizeTimeAggregation(value: string | null | undefined) {
  const normalized = value?.toLowerCase() ?? ''
  if (['daily', 'weekly', 'monthly', 'yearly', 'none'].includes(normalized)) return normalized
  return 'monthly'
}

function normalizeMetric(value: string | null | undefined) {
  const normalized = value?.toLowerCase() ?? ''
  return ['avg', 'average', 'min', 'max', 'count'].includes(normalized) ? normalized : 'sum'
}

function normalizeChartType(value: string) {
  return value.toLowerCase() === 'summary_card' ? 'summary' : value.toLowerCase()
}

function aggregateNumbersByMetric(rows: Array<{ label: string; value: number }>, metric: string) {
  if (metric === 'sum') return aggregateNumbers(rows)

  const buckets = new Map<string, number[]>()
  for (const row of rows) {
    buckets.set(row.label, [...(buckets.get(row.label) ?? []), row.value])
  }

  return Array.from(buckets.entries()).map(([label, values]) => ({
    label,
    value: summarize(values, metric)
  }))
}

export function chartColors(count: number) {
  const palette = ['#1f6f5b', '#d28b32', '#416f8f', '#9b5f42', '#6e8f4f', '#c9a227', '#35524a', '#b85c38']
  return Array.from({ length: count }, (_, index) => palette[index % palette.length])
}

function groupSmallNumericCategories(rows: Array<{ label: string; value: number }>, groupSmallCategories: boolean) {
  if (!groupSmallCategories || rows.length <= 6) return rows
  const visible = rows.slice(0, 5)
  const other = rows.slice(5).reduce((sum, row) => sum + row.value, 0)
  return other > 0 ? [...visible, { label: 'Other', value: other }] : visible
}

function metricLabel(metric: string | null | undefined) {
  return normalizeMetric(metric) === 'count' ? 'Count' : 'Total'
}

function formatNumericLabel(value: number) {
  return Number.isInteger(value) ? String(value) : value.toFixed(2)
}

function fromRows(rows: Array<{ label: string; value: number }>, emptyReason: string, valueLabel = 'Total'): ChartPayload {
  if (rows.length === 0) return emptyPayload(emptyReason)

  return {
    labels: rows.map((row) => row.label),
    values: rows.map((row) => row.value),
    points: [],
    summaryValue: rows.reduce((sum, row) => sum + row.value, 0),
    summaryLabel: valueLabel,
    valueLabel,
    emptyReason: ''
  }
}

function emptyPayload(emptyReason: string): ChartPayload {
  return {
    labels: [],
    values: [],
    points: [],
    summaryValue: 0,
    summaryLabel: '',
    valueLabel: '',
    emptyReason
  }
}

function toNumber(value: unknown) {
  if (typeof value === 'number') return value
  if (typeof value === 'string' && value.trim() !== '') return Number(value)
  const date = parseDate(value)
  return date ? date.getTime() : Number.NaN
}

function summarize(values: number[], metric: string) {
  const normalized = metric.toLowerCase()
  const sum = values.reduce((total, value) => total + value, 0)
  if (normalized === 'avg' || normalized === 'average') return sum / values.length
  if (normalized === 'min') return Math.min(...values)
  if (normalized === 'max') return Math.max(...values)
  if (normalized === 'count') return values.length
  return sum
}

import { parseDate } from './timestampUtils'

export type AggregationType = 'none' | 'daily' | 'weekly' | 'monthly' | 'yearly'

export function bucketDate(value: unknown, aggregation: string | null | undefined) {
  const date = parseDate(value)
  if (!date) return 'Unscheduled'

  const type = (aggregation || 'none').toLowerCase() as AggregationType
  if (type === 'yearly') return String(date.getFullYear())
  if (type === 'monthly') return `${date.getFullYear()}-${pad(date.getMonth() + 1)}`
  if (type === 'weekly') return `${date.getFullYear()} W${pad(getWeek(date))}`
  if (type === 'daily') return date.toISOString().slice(0, 10)

  return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
}

export function aggregateNumbers(rows: Array<{ label: string; value: number }>) {
  const buckets = new Map<string, number>()
  for (const row of rows) {
    buckets.set(row.label, (buckets.get(row.label) ?? 0) + row.value)
  }
  return Array.from(buckets.entries()).map(([label, value]) => ({ label, value }))
}

export function countCategories(values: unknown[], groupSmallCategories = false) {
  const buckets = new Map<string, number>()
  for (const value of values) {
    const label = value === null || value === undefined || value === '' ? 'Empty' : String(value)
    buckets.set(label, (buckets.get(label) ?? 0) + 1)
  }

  const rows = Array.from(buckets.entries())
    .map(([label, value]) => ({ label, value }))
    .sort((a, b) => b.value - a.value)

  if (!groupSmallCategories || rows.length <= 6) return rows

  const visible = rows.slice(0, 5)
  const other = rows.slice(5).reduce((sum, row) => sum + row.value, 0)
  return other > 0 ? [...visible, { label: 'Other', value: other }] : visible
}

function getWeek(date: Date) {
  const copy = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()))
  const day = copy.getUTCDay() || 7
  copy.setUTCDate(copy.getUTCDate() + 4 - day)
  const yearStart = new Date(Date.UTC(copy.getUTCFullYear(), 0, 1))
  return Math.ceil(((copy.getTime() - yearStart.getTime()) / 86400000 + 1) / 7)
}

function pad(value: number) {
  return String(value).padStart(2, '0')
}

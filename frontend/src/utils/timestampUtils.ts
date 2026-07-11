export function parseDate(value: unknown): Date | null {
  if (value instanceof Date && !Number.isNaN(value.getTime())) return value
  if (typeof value !== 'string' && typeof value !== 'number') return null

  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? null : date
}

export function formatDateLabel(value: unknown, dateRange = 'all') {
  const date = parseDate(value)
  if (!date) return 'Unscheduled'

  if (dateRange === 'year') {
    return date.toLocaleDateString(undefined, { month: 'short' })
  }

  return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
}

export function findTimestampField(fields: Array<{ name: string; type: string }>) {
  return fields.find((field) => isTimestampType(field.type))?.name
}

export function isTimestampType(type: string) {
  const normalized = type.toLowerCase()
  return ['date', 'time', 'datetime', 'timestamp'].includes(normalized)
}

export function isInsideDateRange(
  value: unknown,
  dateRange?: string | null,
  customStartTimestamp?: string | null,
  customEndTimestamp?: string | null
) {
  if (!dateRange || dateRange === 'all') return true
  const date = parseDate(value)
  if (!date) return false

  const now = new Date()
  const start = new Date(now)

  if (dateRange === '7d') start.setDate(now.getDate() - 7)
  else if (dateRange === '30d') start.setDate(now.getDate() - 30)
  else if (dateRange === '90d') start.setDate(now.getDate() - 90)
  else if (dateRange === 'year') start.setFullYear(now.getFullYear() - 1)
  else if (dateRange === 'custom') {
    const customStart = parseDate(customStartTimestamp)
    const customEnd = parseDate(customEndTimestamp)
    if (!customStart || !customEnd) return true
    return date >= customStart && date <= customEnd
  }
  else return true

  return date >= start
}

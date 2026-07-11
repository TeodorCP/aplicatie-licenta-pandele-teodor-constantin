<template>
  <div>
    <PageHeader
      title="Calendar"
      subtitle="A month view of scheduled work, meetings, releases, and client touchpoints."
      eyebrow="Schedule"
    >
      <template #actions>
        <div class="view-toggle" aria-label="Calendar view">
          <button
            class="button secondary"
            :class="{ active: viewMode === 'month' }"
            type="button"
            @click="viewMode = 'month'"
          >
            Month
          </button>
          <button
            class="button secondary"
            :class="{ active: viewMode === 'week' }"
            type="button"
            @click="viewMode = 'week'"
          >
            Week
          </button>
        </div>
        <button class="button secondary" type="button" @click="movePeriod(-1)">Previous</button>
        <button class="button secondary" type="button" @click="goToday">Today</button>
        <button class="button secondary" type="button" @click="movePeriod(1)">Next</button>
      </template>
    </PageHeader>

    <section class="calendar-layout">
      <ContentCard>
        <div class="calendar-header">
          <div>
            <p>{{ viewMode === 'month' ? 'Month View' : 'Week View' }}</p>
            <h2>{{ viewMode === 'month' ? monthLabel : weekLabel }}</h2>
          </div>
          <span>{{ calendarEvents.length }} events</span>
        </div>

        <div v-if="viewMode === 'month'" class="weekdays">
          <span v-for="day in weekdayLabels" :key="day">{{ day }}</span>
        </div>

        <div v-if="store.loading" class="status">Loading calendar...</div>
        <div v-else-if="!module" class="empty">Calendar module was not found.</div>
        <div v-else-if="viewMode === 'month'" class="month-grid">
          <article
            v-for="day in calendarDays"
            :key="day.key"
            class="day-cell"
            :class="{ muted: !day.inCurrentMonth, today: day.isToday, selected: day.key === selectedDayKey }"
            @click="selectedDayKey = day.key"
          >
            <header>
              <span>{{ day.date.getDate() }}</span>
              <small v-if="eventsForDay(day.key).length">{{ eventsForDay(day.key).length }}</small>
            </header>
            <div class="event-stack">
              <button
                v-for="event in eventsForDay(day.key).slice(0, 3)"
                :key="event.id"
                class="event-pill"
                type="button"
                @click.stop="selectedDayKey = day.key"
              >
                <strong>{{ event.time }}</strong>
                <span>{{ event.title }}</span>
              </button>
              <small v-if="eventsForDay(day.key).length > 3" class="more">
                +{{ eventsForDay(day.key).length - 3 }} more
              </small>
            </div>
          </article>
        </div>
        <div v-else class="week-grid-wrap">
          <div class="week-header">
            <div class="week-corner">Time</div>
            <button
              v-for="day in weekDays"
              :key="day.key"
              class="week-day-heading"
              :class="{ today: day.isToday, selected: day.key === selectedDayKey }"
              type="button"
              @click="selectedDayKey = day.key"
            >
              <span>{{ day.label }}</span>
              <strong>{{ day.date.getDate() }}</strong>
            </button>
          </div>
          <div class="week-body">
            <div class="time-column">
              <span
                v-for="slot in hourSlots"
                :key="slot.key"
                class="hour-label"
                :style="{ top: `${slot.hour * hourHeight}px` }"
              >
                {{ slot.label }}
              </span>
            </div>

            <div
              v-for="day in weekDays"
              :key="day.key"
              class="week-day-column"
              :class="{ selected: day.key === selectedDayKey }"
              @click="selectedDayKey = day.key"
            >
              <button
                v-for="event in eventsForDay(day.key)"
                :key="event.id"
                class="week-event"
                :style="weekEventStyle(event)"
                type="button"
                @click.stop="selectedDayKey = day.key"
              >
                <strong>{{ event.time }}</strong>
                <span>{{ event.title }}</span>
              </button>
            </div>
          </div>
        </div>
      </ContentCard>

      <aside class="agenda">
        <ContentCard>
          <p class="eyebrow">Selected Day</p>
          <h2>{{ selectedDayLabel }}</h2>
          <div v-if="selectedEvents.length === 0" class="empty small">No events on this day.</div>
          <ol v-else class="agenda-list">
            <li v-for="event in selectedEvents" :key="event.id">
              <time>{{ event.time }}</time>
              <div>
                <h3>{{ event.title }}</h3>
                <p>{{ event.project }}</p>
                <span>{{ event.assignedTo }}</span>
              </div>
            </li>
          </ol>
        </ContentCard>

        <ContentCard>
          <p class="eyebrow">Upcoming</p>
          <h2>Next Events</h2>
          <ol class="upcoming-list">
            <li v-for="event in upcomingEvents" :key="event.id" @click="jumpToEvent(event)">
              <strong>{{ event.dayLabel }}</strong>
              <span>{{ event.time }} · {{ event.title }}</span>
            </li>
          </ol>
        </ContentCard>
      </aside>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import ContentCard from '../../components/common/ContentCard.vue'
import PageHeader from '../../components/common/PageHeader.vue'
import type { Entry } from '../../models/api'
import { useBusinessStore } from '../../stores/businessStore'
import { parseDate } from '../../utils/timestampUtils'

type CalendarEvent = {
  id: string
  title: string
  project: string
  assignedTo: string
  start: Date
  end: Date | null
  dayKey: string
  dayLabel: string
  time: string
}

const store = useBusinessStore()
const viewMode = ref<'month' | 'week'>('month')
const visibleMonth = ref(startOfMonth(new Date()))
const selectedDayKey = ref(toDayKey(new Date()))
const slotHeight = 34
const hourHeight = slotHeight * 2
const module = computed(() => store.moduleByName('calendar'))
const entries = computed(() => (module.value ? store.entriesFor(module.value.id) : []))
const monthLabel = computed(() =>
  visibleMonth.value.toLocaleDateString(undefined, { month: 'long', year: 'numeric' })
)
const weekStart = computed(() => startOfWeek(parseDayKey(selectedDayKey.value)))
const weekLabel = computed(() => {
  const start = weekStart.value
  const end = new Date(start)
  end.setDate(start.getDate() + 6)
  return `${start.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })} - ${end.toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  })}`
})
const weekdayLabels = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']
const calendarEvents = computed(() =>
  entries.value
    .map(toCalendarEvent)
    .filter((event): event is CalendarEvent => event !== null)
    .sort((a, b) => a.start.getTime() - b.start.getTime())
)
const eventsByDay = computed(() => {
  const grouped = new Map<string, CalendarEvent[]>()
  for (const event of calendarEvents.value) {
    grouped.set(event.dayKey, [...(grouped.get(event.dayKey) ?? []), event])
  }
  return grouped
})
const calendarDays = computed(() => {
  const first = startOfMonth(visibleMonth.value)
  const gridStart = new Date(first)
  gridStart.setDate(first.getDate() - ((first.getDay() + 6) % 7))

  return Array.from({ length: 42 }, (_, index) => {
    const date = new Date(gridStart)
    date.setDate(gridStart.getDate() + index)
    return {
      date,
      key: toDayKey(date),
      inCurrentMonth: date.getMonth() === visibleMonth.value.getMonth(),
      isToday: toDayKey(date) === toDayKey(new Date())
    }
  })
})
const weekDays = computed(() =>
  Array.from({ length: 7 }, (_, index) => {
    const date = new Date(weekStart.value)
    date.setDate(weekStart.value.getDate() + index)
    return {
      date,
      key: toDayKey(date),
      label: weekdayLabels[index],
      isToday: toDayKey(date) === toDayKey(new Date())
    }
  })
)
const hourSlots = computed(() =>
  Array.from({ length: 24 }, (_, hour) => ({
    key: String(hour),
    hour,
    label: formatHourLabel(hour)
  }))
)
const selectedEvents = computed(() => eventsForDay(selectedDayKey.value))
const selectedDayLabel = computed(() => {
  const date = parseDayKey(selectedDayKey.value)
  return date.toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric' })
})
const upcomingEvents = computed(() => {
  const now = new Date()
  return calendarEvents.value.filter((event) => event.start >= now).slice(0, 6)
})

onMounted(async () => {
  if (store.modules.length === 0) await store.refreshModules()
  if (module.value) await store.loadEntries(module.value.id)

  const firstEvent = calendarEvents.value[0]
  if (firstEvent) {
    visibleMonth.value = startOfMonth(firstEvent.start)
    selectedDayKey.value = firstEvent.dayKey
  }
})

function eventsForDay(dayKey: string) {
  return eventsByDay.value.get(dayKey) ?? []
}

function movePeriod(delta: number) {
  if (viewMode.value === 'week') {
    const next = parseDayKey(selectedDayKey.value)
    next.setDate(next.getDate() + delta * 7)
    selectedDayKey.value = toDayKey(next)
    visibleMonth.value = startOfMonth(next)
    return
  }

  const next = new Date(visibleMonth.value)
  next.setMonth(next.getMonth() + delta)
  visibleMonth.value = startOfMonth(next)
}

function goToday() {
  const today = new Date()
  visibleMonth.value = startOfMonth(today)
  selectedDayKey.value = toDayKey(today)
}

function jumpToEvent(event: CalendarEvent) {
  selectedDayKey.value = event.dayKey
  visibleMonth.value = startOfMonth(event.start)
}

function weekEventStyle(event: CalendarEvent) {
  const top = (minutesSinceStartOfDay(event.start) / 30) * slotHeight + 3
  const durationMinutes = event.end
    ? Math.max(30, (event.end.getTime() - event.start.getTime()) / 60000)
    : 30
  const height = Math.max(slotHeight - 6, (durationMinutes / 30) * slotHeight - 6)

  return {
    top: `${top}px`,
    height: `${height}px`
  }
}

function toCalendarEvent(entry: Entry): CalendarEvent | null {
  const start = parseDate(entry.data.startTimestamp ?? entry.timestamp)
  if (!start) return null
  const end = parseDate(entry.data.endTimestamp)
  const dayKey = toDayKey(start)

  return {
    id: entry.id,
    title: String(entry.data.title ?? 'Untitled event'),
    project: String(entry.data.project ?? 'No project'),
    assignedTo: String(entry.data.assignedTo ?? 'Unassigned'),
    start,
    end,
    dayKey,
    dayLabel: start.toLocaleDateString(undefined, { month: 'short', day: 'numeric' }),
    time: formatTimeRange(start, end)
  }
}

function formatTimeRange(start: Date, end: Date | null) {
  const startLabel = start.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })
  if (!end) return startLabel
  const endLabel = end.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })
  return `${startLabel} - ${endLabel}`
}

function formatHourLabel(hour: number) {
  const date = new Date(2000, 0, 1, hour, 0)
  return date.toLocaleTimeString(undefined, { hour: '2-digit' })
}

function startOfMonth(date: Date) {
  return new Date(date.getFullYear(), date.getMonth(), 1)
}

function startOfWeek(date: Date) {
  const start = new Date(date)
  start.setHours(0, 0, 0, 0)
  start.setDate(date.getDate() - ((date.getDay() + 6) % 7))
  return start
}

function minutesSinceStartOfDay(date: Date) {
  return date.getHours() * 60 + date.getMinutes()
}

function toDayKey(date: Date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}

function parseDayKey(value: string) {
  const [year, month, day] = value.split('-').map(Number)
  return new Date(year, month - 1, day)
}
</script>

<style scoped>
.calendar-layout {
  align-items: start;
  display: grid;
  gap: 18px;
  grid-template-columns: minmax(0, 1fr) 360px;
}

.view-toggle {
  background: rgba(31, 111, 91, 0.08);
  border: 1px solid rgba(31, 111, 91, 0.13);
  border-radius: 10px;
  display: inline-flex;
  gap: 4px;
  padding: 4px;
}

.view-toggle .button {
  border-color: transparent;
  min-height: 32px;
}

.view-toggle .button.active {
  background: var(--color-primary);
  color: #fff;
}

.calendar-header,
.day-cell header {
  align-items: center;
  display: flex;
  justify-content: space-between;
}

.calendar-header {
  margin-bottom: 16px;
}

.calendar-header p,
.eyebrow {
  color: var(--color-primary);
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0.08em;
  margin: 0;
  text-transform: uppercase;
}

h2,
h3,
p {
  margin: 0;
}

.calendar-header h2,
.agenda h2 {
  font-size: 22px;
  margin-top: 3px;
}

.calendar-header span {
  color: var(--color-muted);
  font-weight: 700;
}

.weekdays,
.month-grid {
  display: grid;
  grid-template-columns: repeat(7, minmax(0, 1fr));
}

.weekdays {
  color: var(--color-muted);
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0.06em;
  margin-bottom: 8px;
  text-transform: uppercase;
}

.weekdays span {
  padding: 0 8px;
}

.month-grid {
  border: 1px solid var(--color-border);
  border-radius: 18px;
  overflow: hidden;
}

.week-grid-wrap {
  border: 1px solid var(--color-border);
  border-radius: 18px;
  max-height: 760px;
  overflow: auto;
}

.week-header,
.week-body {
  display: grid;
  grid-template-columns: 86px repeat(7, minmax(150px, 1fr));
  min-width: 1140px;
}

.week-header {
  position: sticky;
  top: 0;
  z-index: 5;
}

.week-corner,
.week-day-heading {
  border-bottom: 1px solid var(--color-border);
  border-right: 1px solid var(--color-border);
}

.week-corner,
.week-day-heading {
  background: #f8faf5;
}

.week-corner {
  align-items: center;
  color: var(--color-muted);
  display: flex;
  font-size: 12px;
  font-weight: 800;
  justify-content: center;
  text-transform: uppercase;
}

.week-day-heading {
  border-top: 0;
  color: var(--color-text);
  display: grid;
  gap: 3px;
  justify-items: center;
  padding: 10px 8px;
}

.week-day-heading span {
  color: var(--color-muted);
  font-size: 12px;
  font-weight: 800;
  text-transform: uppercase;
}

.week-day-heading strong {
  align-items: center;
  border-radius: 999px;
  display: inline-flex;
  height: 30px;
  justify-content: center;
  width: 30px;
}

.week-day-heading.today strong {
  background: var(--color-primary);
  color: #fff;
}

.week-day-heading.selected {
  background: #eef7ed;
}

.week-body {
  height: 1632px;
  position: relative;
}

.time-column {
  background: #fbfcf8;
  border-right: 1px solid var(--color-border);
  position: relative;
}

.hour-label {
  color: var(--color-muted);
  font-size: 11px;
  font-weight: 800;
  left: 0;
  position: absolute;
  text-align: center;
  transform: translateY(-50%);
  width: 100%;
}

.week-day-column {
  background:
    repeating-linear-gradient(
      to bottom,
      var(--color-border) 0,
      var(--color-border) 1px,
      transparent 1px,
      transparent 68px
    ),
    repeating-linear-gradient(
      to bottom,
      #edf1ea 0,
      #edf1ea 1px,
      transparent 1px,
      transparent 34px
    ),
    #fff;
  border-right: 1px solid var(--color-border);
  cursor: pointer;
  min-height: 1632px;
  overflow: hidden;
  position: relative;
}

.week-day-column.selected {
  background: #f7fbf4;
}

.week-event {
  background: linear-gradient(135deg, #17251f, #214438);
  border: 0;
  border-left: 4px solid #b7d8bb;
  border-radius: 9px;
  box-shadow: 0 8px 18px rgba(23, 37, 31, 0.14);
  color: #f5f8ef;
  display: grid;
  gap: 2px;
  overflow: hidden;
  padding: 6px 7px;
  position: absolute;
  left: 4px;
  right: 4px;
  text-align: left;
  z-index: 2;
}

.week-event strong {
  color: #b7d8bb;
  font-size: 10px;
}

.week-event span {
  color: #f5f8ef;
  font-size: 12px;
  line-height: 1.2;
}

.day-cell {
  background: #fff;
  border-bottom: 1px solid var(--color-border);
  border-right: 1px solid var(--color-border);
  cursor: pointer;
  min-height: 138px;
  padding: 10px;
  transition: background 140ms ease, box-shadow 140ms ease;
}

.day-cell:nth-child(7n) {
  border-right: 0;
}

.day-cell:hover,
.day-cell.selected {
  background: #f5faf4;
  box-shadow: inset 0 0 0 2px rgba(31, 111, 91, 0.16);
}

.day-cell.muted {
  background: #f8f8f4;
  color: #9aa69f;
}

.day-cell.today header span {
  align-items: center;
  background: var(--color-primary);
  border-radius: 999px;
  color: #fff;
  display: inline-flex;
  height: 28px;
  justify-content: center;
  width: 28px;
}

.day-cell header small {
  background: rgba(31, 111, 91, 0.1);
  border-radius: 999px;
  color: var(--color-primary-dark);
  font-weight: 800;
  padding: 2px 7px;
}

.event-stack {
  display: grid;
  gap: 6px;
  margin-top: 10px;
}

.event-pill {
  background: #17251f;
  border: 0;
  border-radius: 10px;
  color: #f5f8ef;
  display: grid;
  gap: 2px;
  padding: 7px 8px;
  text-align: left;
}

.event-pill strong {
  color: #b7d8bb;
  font-size: 11px;
}

.event-pill span {
  font-size: 12px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.more {
  color: var(--color-muted);
  font-weight: 700;
}

.agenda {
  display: grid;
  gap: 18px;
}

.agenda-list,
.upcoming-list {
  display: grid;
  gap: 10px;
  list-style: none;
  margin: 16px 0 0;
  padding: 0;
}

.agenda-list li,
.upcoming-list li {
  background: rgba(31, 111, 91, 0.055);
  border: 1px solid rgba(31, 111, 91, 0.1);
  border-radius: 14px;
  padding: 12px;
}

.agenda-list li {
  display: grid;
  gap: 12px;
  grid-template-columns: 108px 1fr;
}

.agenda-list time {
  color: var(--color-primary-dark);
  font-weight: 800;
}

.agenda-list h3 {
  font-size: 15px;
}

.agenda-list p,
.agenda-list span,
.upcoming-list span {
  color: var(--color-muted);
}

.upcoming-list li {
  cursor: pointer;
  display: grid;
  gap: 4px;
}

.empty {
  align-items: center;
  border: 1px dashed var(--color-border);
  border-radius: 14px;
  color: var(--color-muted);
  display: flex;
  justify-content: center;
  min-height: 220px;
  padding: 24px;
}

.empty.small {
  min-height: 120px;
}

@media (max-width: 1100px) {
  .calendar-layout {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 760px) {
  .weekdays {
    display: none;
  }

  .month-grid {
    grid-template-columns: 1fr;
  }

  .week-header,
  .week-body {
    grid-template-columns: 72px repeat(7, minmax(130px, 1fr));
    min-width: 982px;
  }

  .day-cell {
    border-right: 0;
    min-height: 110px;
  }
}
</style>

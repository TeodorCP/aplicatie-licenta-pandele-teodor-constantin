<template>
  <article class="calendar-widget">
    <header>
      <div>
        <p>Calendar</p>
        <h3>{{ title }}</h3>
      </div>
      <span>{{ upcomingEvents.length }} upcoming</span>
    </header>

    <div v-if="upcomingEvents.length === 0" class="empty">
      No upcoming calendar events found.
    </div>
    <ol v-else class="event-list">
      <li v-for="event in upcomingEvents" :key="event.id">
        <time>
          <strong>{{ event.day }}</strong>
          <span>{{ event.time }}</span>
        </time>
        <div>
          <h4>{{ event.title }}</h4>
          <p>{{ event.project }}</p>
          <small>{{ event.assignedTo }}</small>
        </div>
      </li>
    </ol>
  </article>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { Entry } from '../../models/api'
import { parseDate } from '../../utils/timestampUtils'

const props = defineProps<{
  title: string
  entries: Entry[]
}>()

const upcomingEvents = computed(() => {
  const now = new Date()
  return props.entries
    .map((entry) => {
      const start = parseDate(entry.data.startTimestamp ?? entry.timestamp)
      const end = parseDate(entry.data.endTimestamp)
      return {
        id: entry.id,
        start,
        end,
        title: String(entry.data.title ?? 'Untitled event'),
        project: String(entry.data.project ?? 'No project'),
        assignedTo: String(entry.data.assignedTo ?? 'Unassigned')
      }
    })
    .filter((event) => event.start !== null && event.start >= now)
    .map((event) => ({ ...event, start: event.start as Date }))
    .sort((a, b) => a.start.getTime() - b.start.getTime())
    .slice(0, 6)
    .map((event) => ({
      id: event.id,
      title: event.title,
      project: event.project,
      assignedTo: event.assignedTo,
      day: event.start.toLocaleDateString(undefined, { month: 'short', day: 'numeric' }),
      time: formatTimeRange(event.start, event.end)
    }))
})

function formatTimeRange(start: Date, end: Date | null) {
  const startLabel = start.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })
  if (!end) return startLabel
  const endLabel = end.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })
  return `${startLabel} - ${endLabel}`
}
</script>

<style scoped>
.calendar-widget {
  background:
    radial-gradient(circle at 16% 12%, rgba(31, 111, 91, 0.14), transparent 32%),
    var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-card);
  box-shadow: var(--shadow-card);
  display: grid;
  grid-template-rows: auto minmax(0, 1fr);
  min-height: 360px;
  min-width: 0;
  overflow: hidden;
  padding: 18px;
}

header {
  align-items: flex-start;
  display: flex;
  gap: 12px;
  justify-content: space-between;
  margin-bottom: 14px;
}

header p,
header span,
small {
  color: var(--color-muted);
}

header p {
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0.08em;
  margin: 0;
  text-transform: uppercase;
}

h3,
h4,
p {
  margin: 0;
}

h3 {
  font-size: 18px;
  margin-top: 3px;
}

.event-list {
  align-content: start;
  display: grid;
  gap: 10px;
  list-style: none;
  margin: 0;
  min-height: 0;
  overflow: auto;
  padding: 0;
}

li {
  align-items: center;
  background: rgba(31, 111, 91, 0.055);
  border: 1px solid rgba(31, 111, 91, 0.1);
  border-radius: 14px;
  display: grid;
  gap: 12px;
  grid-template-columns: 92px 1fr;
  min-width: 0;
  padding: 12px;
}

li > div {
  min-width: 0;
}

time {
  background: #17251f;
  border-radius: 12px;
  color: #f5f8ef;
  display: grid;
  gap: 3px;
  justify-items: center;
  padding: 10px 8px;
}

time strong {
  font-size: 15px;
}

time span {
  color: #c8d5ce;
  font-size: 12px;
  text-align: center;
}

li p {
  color: var(--color-muted);
  margin-top: 3px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
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
</style>

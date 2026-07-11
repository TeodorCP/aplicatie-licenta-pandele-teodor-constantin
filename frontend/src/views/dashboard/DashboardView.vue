<template>
  <div>
    <PageHeader
      title="Dashboard"
      subtitle="A quick operating snapshot across modules, records, widgets, and visualizations."
      eyebrow="Overview"
    />

    <section class="metrics">
      <DashboardWidget title="Modules">
        <strong>{{ store.modules.length }}</strong>
        <span>active data spaces</span>
      </DashboardWidget>
      <DashboardWidget title="Records">
        <strong>{{ totalEntries }}</strong>
        <span>tracked entries</span>
      </DashboardWidget>
      <DashboardWidget title="Visualizations">
        <strong>{{ store.visualizations.length }}</strong>
        <span>saved charts</span>
      </DashboardWidget>
      <DashboardWidget title="Dashboard Widgets">
        <strong>{{ store.dashboardWidgets.length }}</strong>
        <span>configured widgets</span>
      </DashboardWidget>
    </section>

    <section class="grid">
      <ContentCard>
        <h2>Recent Modules</h2>
        <DataTable :columns="moduleColumns" :rows="store.modules.slice(0, 8)" :loading="store.loading" />
      </ContentCard>
      <ContentCard>
        <h2>Saved Visualizations</h2>
        <DataTable :columns="visualizationColumns" :rows="store.visualizations.slice(0, 8)" :loading="store.loading" />
      </ContentCard>
    </section>

    <section class="chart-section">
      <div class="section-heading">
        <h2>Dashboard Visualizations</h2>
        <div class="section-actions">
          <RouterLink class="button secondary" to="/tools/visualizations">Manage visualizations</RouterLink>
          <button
            v-if="dashboardItems.length > 0 && !editMode && !isCompactViewport"
            class="button secondary"
            type="button"
            @click="enterEditMode"
          >
            Edit Dashboard
          </button>
          <template v-if="editMode">
            <button class="button secondary" type="button" @click="resetDraftLayouts">Reset</button>
            <button class="button secondary" type="button" @click="cancelEditMode">Cancel</button>
            <button class="button" type="button" @click="saveLayout">Save Layout</button>
          </template>
        </div>
      </div>

      <div v-if="dashboardWidgetCount === 0" class="empty">
        No dashboard charts yet. Create or pin a saved visualization to fill this space.
      </div>

      <p v-if="editMode" class="grid-hint">
        Dashboard layout uses a 12-column grid. Drag and resize snap to grid cells, and every widget stays inside that board.
      </p>

      <div v-else-if="dashboardItems.length === 0" class="chart-grid chart-grid--fallback">
        <ChartRenderer
          v-for="item in fallbackVisualizations"
          :key="item.visualization.id"
          :visualization="item.visualization"
          :module="moduleFor(item.visualization.moduleId)"
          :entries="store.entriesFor(item.visualization.moduleId)"
          :settings="visualizationService.getSettings(item.visualization)"
        >
          <template #actions>
            <div class="actions">
              <RouterLink class="button secondary" :to="`/tools/visualizations/${item.visualization.id}/edit`">
                Settings
              </RouterLink>
            </div>
          </template>
        </ChartRenderer>
      </div>

      <div
        v-if="dashboardItems.length > 0"
        ref="layoutGridRef"
        class="chart-grid"
        :class="{ 'chart-grid--editing': editMode }"
        :style="gridBoardStyle"
      >
        <div v-if="editMode" class="chart-grid-overlay" aria-hidden="true">
          <span v-for="cell in gridOverlayCells" :key="cell" class="chart-grid-cell" />
        </div>
        <div
          v-for="item in dashboardItems"
          :key="item.key"
          class="dashboard-tile"
          :class="{ 'dashboard-tile--editing': editMode, 'dashboard-tile--active': activeWidgetId === item.widget.id }"
          :style="tileStyle(item.widget.id)"
        >
          <div v-if="editMode" class="tile-overlay">
            <button class="tile-drag-handle" type="button" @pointerdown="startDrag($event, item.widget.id)">
              Drag
            </button>
            <span class="tile-layout-label">{{ layoutLabel(item.widget.id) }}</span>
          </div>

          <CalendarSummaryWidget
            v-if="item.widget.type === 'calendar_summary'"
            :title="item.widget.title"
            :entries="store.entriesFor(item.widget.moduleId ?? '')"
          />

          <ChartRenderer
            v-else-if="item.visualization"
            :visualization="item.visualization"
            :module="moduleFor(item.visualization.moduleId)"
            :entries="store.entriesFor(item.visualization.moduleId)"
            :settings="visualizationService.getSettings(item.visualization)"
          >
            <template v-if="!editMode" #actions>
              <div class="actions">
                <RouterLink class="button secondary" :to="`/tools/visualizations/${item.visualization.id}/edit`">
                  Settings
                </RouterLink>
                <button class="button danger" type="button" @click="unpin(item.widget.id)">Unpin</button>
              </div>
            </template>
          </ChartRenderer>

          <button
            v-if="editMode"
            class="tile-resize-handle"
            type="button"
            aria-label="Resize widget"
            @pointerdown="startResize($event, item.widget.id)"
          />
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import ChartRenderer from '../../components/charts/ChartRenderer.vue'
import ContentCard from '../../components/common/ContentCard.vue'
import PageHeader from '../../components/common/PageHeader.vue'
import CalendarSummaryWidget from '../../components/dashboard/CalendarSummaryWidget.vue'
import DashboardWidget from '../../components/dashboard/DashboardWidget.vue'
import DataTable from '../../components/tables/DataTable.vue'
import type { DashboardWidget as DashboardWidgetModel, Visualization } from '../../models/api'
import { visualizationService } from '../../services/visualizationService'
import { useBusinessStore } from '../../stores/businessStore'

type WidgetLayout = {
  x: number
  y: number
  width: number
  height: number
}

type DashboardItem = {
  key: string
  widget: DashboardWidgetModel
  visualization?: Visualization
}

type PointerInteraction = {
  mode: 'drag' | 'resize'
  widgetId: string
  startClientX: number
  startClientY: number
  startLayout: WidgetLayout
}

const GRID_COLUMNS = 12
const GRID_GAP = 16
const GRID_ROW_HEIGHT = 120
const MIN_GRID_HEIGHT = 4

const store = useBusinessStore()
const layoutGridRef = ref<HTMLElement | null>(null)
const editMode = ref(false)
const isCompactViewport = ref(typeof window !== 'undefined' ? window.innerWidth <= 760 : false)
const activeWidgetId = ref('')
const draftLayouts = ref<Record<string, WidgetLayout>>({})
const pointerInteraction = ref<PointerInteraction | null>(null)

const totalEntries = computed(() => Object.values(store.entriesByModule).reduce((sum, entries) => sum + entries.length, 0))
const moduleColumns = [
  { key: 'name', label: 'Name' },
  { key: 'category', label: 'Category' },
  { key: 'createdAt', label: 'Created' }
]
const visualizationColumns = [
  { key: 'title', label: 'Title' },
  { key: 'chartType', label: 'Chart' },
  { key: 'fieldName', label: 'Field' }
]

const dashboardItems = computed<DashboardItem[]>(() =>
  store.dashboardWidgets
    .map((widget) => ({
      key: widget.id,
      widget,
      visualization: widget.visualizationId
        ? store.visualizations.find((visualization) => visualization.id === widget.visualizationId)
        : undefined
    }))
    .filter((item) => item.widget.type === 'calendar_summary' || Boolean(item.visualization))
)

const fallbackVisualizations = computed(() =>
  store.visualizations.slice(0, 4).map((visualization) => ({ widgetId: '', visualization }))
)

const dashboardWidgetCount = computed(() =>
  dashboardItems.value.length > 0 ? dashboardItems.value.length : fallbackVisualizations.value.length
)
const gridRowCount = computed(() =>
  Math.max(
    MIN_GRID_HEIGHT,
    dashboardItems.value.reduce((max, item) => {
      const layout = currentLayout(item.widget.id)
      return Math.max(max, layout.y + layout.height - 1)
    }, 0)
  )
)
const gridBoardStyle = computed(() => ({
  minHeight: `${gridRowCount.value * GRID_ROW_HEIGHT + (gridRowCount.value - 1) * GRID_GAP}px`
}))
const gridOverlayCells = computed(() =>
  Array.from({ length: GRID_COLUMNS * gridRowCount.value }, (_, index) => {
    const row = Math.floor(index / GRID_COLUMNS) + 1
    const column = (index % GRID_COLUMNS) + 1
    return `${row}-${column}`
  })
)

onMounted(async () => {
  await store.loadAll()
  initializeDraftLayouts()
  window.addEventListener('resize', handleResize)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', handleResize)
  stopPointerTracking()
})

function handleResize() {
  isCompactViewport.value = window.innerWidth <= 760
  if (isCompactViewport.value && editMode.value) {
    cancelEditMode()
  }
}

function moduleFor(moduleId: string) {
  return store.modules.find((module) => module.id === moduleId)
}

function enterEditMode() {
  initializeDraftLayouts()
  editMode.value = true
}

function cancelEditMode() {
  editMode.value = false
  activeWidgetId.value = ''
  pointerInteraction.value = null
  initializeDraftLayouts()
}

function resetDraftLayouts() {
  initializeDraftLayouts()
}

async function saveLayout() {
  const normalizedLayouts = rebalanceLayouts(draftLayouts.value, dashboardItems.value.map((item) => item.widget))
  const layoutOrder = [...dashboardItems.value]
    .sort((left, right) => compareLayouts(normalizedLayouts[left.widget.id], normalizedLayouts[right.widget.id]))
    .map((item) => item.widget.id)

  await Promise.all(
    dashboardItems.value.map(({ widget }) =>
      visualizationService.updateWidget(widget.id, {
        type: widget.type,
        title: widget.title,
        moduleId: widget.moduleId,
        visualizationId: widget.visualizationId,
        size: widget.size,
        width: normalizedLayouts[widget.id].width,
        height: normalizedLayouts[widget.id].height,
        x: normalizedLayouts[widget.id].x,
        y: normalizedLayouts[widget.id].y,
        position: layoutOrder.indexOf(widget.id) + 1,
        visualSettings: widget.visualSettings ?? null
      })
    )
  )

  store.dashboardWidgets = await visualizationService.listWidgets()
  initializeDraftLayouts()
  editMode.value = false
  activeWidgetId.value = ''
}

async function unpin(widgetId: string) {
  await visualizationService.removeWidget(widgetId)
  store.dashboardWidgets = await visualizationService.listWidgets()
  initializeDraftLayouts()
}

function tileStyle(widgetId: string) {
  const layout = currentLayout(widgetId)
  return {
    gridColumn: `${layout.x} / span ${layout.width}`,
    gridRow: `${layout.y} / span ${layout.height}`
  }
}

function layoutLabel(widgetId: string) {
  const layout = currentLayout(widgetId)
  return `${layout.x},${layout.y} • ${layout.width}x${layout.height}`
}

function currentLayout(widgetId: string) {
  return draftLayouts.value[widgetId] ?? defaultLayouts()[widgetId]
}

function defaultLayouts() {
  return buildDefaultLayouts(dashboardItems.value.map((item) => item.widget))
}

function initializeDraftLayouts() {
  draftLayouts.value = rebalanceLayouts(defaultLayouts(), dashboardItems.value.map((item) => item.widget))
}

function buildDefaultLayouts(widgets: DashboardWidgetModel[]) {
  const layouts: Record<string, WidgetLayout> = {}
  let cursorX = 1
  let cursorY = 1
  let rowHeight = 0

  for (const widget of widgets) {
    const width = clamp(widget.width ?? 6, 1, GRID_COLUMNS)
    const height = clamp(widget.height ?? 3, 2, 12)

    if (widget.x && widget.y) {
      layouts[widget.id] = {
        x: clamp(widget.x, 1, GRID_COLUMNS - width + 1),
        y: Math.max(1, widget.y),
        width,
        height
      }
      continue
    }

    if (cursorX + width - 1 > GRID_COLUMNS) {
      cursorX = 1
      cursorY += Math.max(rowHeight, 3)
      rowHeight = 0
    }

    layouts[widget.id] = {
      x: cursorX,
      y: cursorY,
      width,
      height
    }

    cursorX += width
    rowHeight = Math.max(rowHeight, height)
  }

  return layouts
}

function startDrag(event: PointerEvent, widgetId: string) {
  if (!editMode.value) return
  startInteraction(event, widgetId, 'drag')
}

function startResize(event: PointerEvent, widgetId: string) {
  if (!editMode.value) return
  startInteraction(event, widgetId, 'resize')
}

function startInteraction(event: PointerEvent, widgetId: string, mode: 'drag' | 'resize') {
  event.preventDefault()
  activeWidgetId.value = widgetId
  pointerInteraction.value = {
    mode,
    widgetId,
    startClientX: event.clientX,
    startClientY: event.clientY,
    startLayout: { ...currentLayout(widgetId) }
  }

  window.addEventListener('pointermove', onPointerMove)
  window.addEventListener('pointerup', stopPointerTracking, { once: true })
}

function onPointerMove(event: PointerEvent) {
  const interaction = pointerInteraction.value
  const grid = layoutGridRef.value
  if (!interaction || !grid) return

  const rect = grid.getBoundingClientRect()
  const cellWidth = (rect.width - GRID_GAP * (GRID_COLUMNS - 1)) / GRID_COLUMNS
  const columnStep = cellWidth + GRID_GAP
  const rowStep = GRID_ROW_HEIGHT + GRID_GAP
  const deltaColumns = Math.round((event.clientX - interaction.startClientX) / columnStep)
  const deltaRows = Math.round((event.clientY - interaction.startClientY) / rowStep)

  if (interaction.mode === 'drag') {
    draftLayouts.value = rebalanceLayouts(
      {
        ...draftLayouts.value,
        [interaction.widgetId]: {
          ...interaction.startLayout,
          x: clamp(interaction.startLayout.x + deltaColumns, 1, GRID_COLUMNS - interaction.startLayout.width + 1),
          y: Math.max(1, interaction.startLayout.y + deltaRows)
        }
      },
      dashboardItems.value.map((item) => item.widget),
      interaction.widgetId
    )
    return
  }

  draftLayouts.value = rebalanceLayouts(
    {
      ...draftLayouts.value,
      [interaction.widgetId]: {
        ...interaction.startLayout,
        width: clamp(interaction.startLayout.width + deltaColumns, 1, GRID_COLUMNS - interaction.startLayout.x + 1),
        height: clamp(interaction.startLayout.height + deltaRows, 2, 12)
      }
    },
    dashboardItems.value.map((item) => item.widget),
    interaction.widgetId
  )
}

function stopPointerTracking() {
  pointerInteraction.value = null
  window.removeEventListener('pointermove', onPointerMove)
}

function rebalanceLayouts(layouts: Record<string, WidgetLayout>, widgets: DashboardWidgetModel[], lockedWidgetId = '') {
  const occupied: WidgetLayout[] = []
  const result: Record<string, WidgetLayout> = {}
  const orderedWidgets = lockedWidgetId
    ? [
        ...widgets.filter((widget) => widget.id === lockedWidgetId),
        ...widgets
          .filter((widget) => widget.id !== lockedWidgetId)
          .sort((left, right) => compareLayouts(resolveLayout(layouts[left.id], left), resolveLayout(layouts[right.id], right)))
      ]
    : [...widgets].sort((left, right) =>
        compareLayouts(resolveLayout(layouts[left.id], left), resolveLayout(layouts[right.id], right))
      )

  for (const widget of orderedWidgets) {
    const source = resolveLayout(layouts[widget.id], widget)
    const candidate =
      widget.id === lockedWidgetId
        ? source
        : findNearestAvailablePosition(source, occupied)

    occupied.push(candidate)
    result[widget.id] = candidate
  }

  return result
}

function resolveLayout(layout: WidgetLayout | undefined, widget: DashboardWidgetModel): WidgetLayout {
  const width = clamp(layout?.width ?? widget.width ?? 6, 1, GRID_COLUMNS)
  return {
    x: clamp(layout?.x ?? widget.x ?? 1, 1, GRID_COLUMNS - width + 1),
    y: Math.max(1, layout?.y ?? widget.y ?? 1),
    width,
    height: clamp(layout?.height ?? widget.height ?? 3, 2, 12)
  }
}

function findNearestAvailablePosition(source: WidgetLayout, occupied: WidgetLayout[]) {
  if (!occupied.some((layout) => overlaps(source, layout))) {
    return source
  }

  const maxX = GRID_COLUMNS - source.width + 1
  let y = source.y

  while (y < 200) {
    for (let x = source.x; x <= maxX; x += 1) {
      const candidate = { ...source, x, y }
      if (!occupied.some((layout) => overlaps(candidate, layout))) {
        return candidate
      }
    }

    for (let x = 1; x < source.x; x += 1) {
      const candidate = { ...source, x, y }
      if (!occupied.some((layout) => overlaps(candidate, layout))) {
        return candidate
      }
    }

    y += 1
  }

  return source
}

function overlaps(a: WidgetLayout, b: WidgetLayout) {
  return !(
    a.x + a.width - 1 < b.x ||
    b.x + b.width - 1 < a.x ||
    a.y + a.height - 1 < b.y ||
    b.y + b.height - 1 < a.y
  )
}

function compareLayouts(left: WidgetLayout, right: WidgetLayout) {
  if (left.y !== right.y) return left.y - right.y
  if (left.x !== right.x) return left.x - right.x
  if (left.height !== right.height) return left.height - right.height
  return left.width - right.width
}

function clamp(value: number, min: number, max: number) {
  return Math.min(max, Math.max(min, value))
}
</script>

<style scoped>
.metrics,
.grid {
  display: grid;
  gap: 16px;
}

.metrics {
  grid-template-columns: repeat(4, minmax(0, 1fr));
  margin-bottom: 16px;
}

.grid {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.chart-section {
  margin-top: 18px;
}

.grid-hint {
  color: var(--color-muted);
  margin: 0 0 12px;
}

.section-heading {
  align-items: center;
  display: flex;
  gap: 12px;
  justify-content: space-between;
  margin-bottom: 14px;
}

.section-actions,
.actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  justify-content: flex-end;
}

.chart-grid {
  align-items: stretch;
  display: grid;
  gap: 16px;
  grid-auto-rows: 120px;
  grid-template-columns: repeat(12, minmax(0, 1fr));
  position: relative;
}

.chart-grid--fallback :deep(.chart-card) {
  grid-column: span 6;
}

.dashboard-tile {
  min-width: 0;
  position: relative;
  z-index: 1;
}

.dashboard-tile--editing {
  outline: 2px dashed rgba(31, 111, 91, 0.35);
  outline-offset: 4px;
}

.dashboard-tile--active {
  z-index: 3;
}

.chart-grid-overlay {
  display: grid;
  gap: 16px;
  grid-auto-rows: 120px;
  grid-template-columns: repeat(12, minmax(0, 1fr));
  inset: 0;
  pointer-events: none;
  position: absolute;
  z-index: 0;
}

.chart-grid-cell {
  background: rgba(31, 111, 91, 0.035);
  border: 1px dashed rgba(31, 111, 91, 0.12);
  border-radius: 10px;
}

.dashboard-tile :deep(.chart-card),
.dashboard-tile :deep(.calendar-widget) {
  height: 100%;
  min-height: 100%;
}

.chart-grid--editing .dashboard-tile :deep(.chart-card),
.chart-grid--editing .dashboard-tile :deep(.calendar-widget) {
  pointer-events: none;
}

.tile-overlay {
  align-items: center;
  display: flex;
  gap: 8px;
  left: 12px;
  position: absolute;
  right: 12px;
  top: 12px;
  z-index: 4;
}

.tile-drag-handle,
.tile-layout-label {
  background: rgba(23, 37, 31, 0.78);
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 999px;
  color: #f7faf8;
  font-size: 12px;
  font-weight: 700;
  padding: 7px 10px;
}

.tile-drag-handle {
  cursor: grab;
}

.tile-layout-label {
  margin-left: auto;
}

.tile-resize-handle {
  background: rgba(23, 37, 31, 0.86);
  border: 0;
  border-radius: 10px 0 0 0;
  bottom: 0;
  cursor: nwse-resize;
  height: 24px;
  position: absolute;
  right: 0;
  width: 24px;
  z-index: 4;
}

.tile-resize-handle::before {
  border-bottom: 2px solid rgba(255, 255, 255, 0.8);
  border-right: 2px solid rgba(255, 255, 255, 0.8);
  content: '';
  display: block;
  height: 10px;
  margin: 6px;
  width: 10px;
}

.empty {
  background: var(--color-surface);
  border: 1px dashed var(--color-border);
  border-radius: var(--radius-card);
  color: var(--color-muted);
  padding: 24px;
}

strong {
  display: block;
  font-size: 34px;
  line-height: 1;
}

span {
  color: var(--color-muted);
}

h2 {
  font-size: 18px;
  margin: 0 0 14px;
}

@media (max-width: 980px) {
  .metrics,
  .grid {
    grid-template-columns: 1fr;
  }

  .section-heading {
    align-items: flex-start;
    flex-direction: column;
  }
}

@media (max-width: 760px) {
  .chart-grid {
    grid-auto-rows: minmax(320px, auto);
    grid-template-columns: 1fr;
  }

  .dashboard-tile,
  .chart-grid--fallback :deep(.chart-card) {
    grid-column: 1 / -1 !important;
    grid-row: auto !important;
  }
}
</style>

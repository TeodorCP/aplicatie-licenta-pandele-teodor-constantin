<template>
  <div>
    <PageHeader
      title="Visualizations"
      subtitle="Create, preview, pin, customize, and manage saved charts from business module data."
      eyebrow="Reports"
      primary-label="New visualization"
      @primary="router.push('/tools/visualizations/new')"
    />

    <div v-if="store.loading" class="status">Loading visualizations...</div>
    <div v-else-if="store.visualizations.length === 0" class="empty">
      <h2>No visualizations yet</h2>
      <p>Create your first chart from any business module field.</p>
      <button class="button" type="button" @click="router.push('/tools/visualizations/new')">Create visualization</button>
    </div>
    <section v-else class="visualization-grid">
      <ChartRenderer
        v-for="visualization in store.visualizations"
        :key="visualization.id"
        :visualization="visualization"
        :module="moduleFor(visualization.moduleId)"
        :entries="store.entriesFor(visualization.moduleId)"
        :settings="visualizationService.getSettings(visualization)"
      >
        <template #actions>
          <div class="actions">
            <button class="button secondary" type="button" @click="router.push(`/tools/visualizations/${visualization.id}/edit`)">
              Customize
            </button>
            <button class="button secondary" type="button" @click="pin(visualization)">Pin</button>
            <button class="button danger" type="button" @click="remove(visualization)">Delete</button>
          </div>
        </template>
      </ChartRenderer>
    </section>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import ChartRenderer from '../../components/charts/ChartRenderer.vue'
import PageHeader from '../../components/common/PageHeader.vue'
import type { Visualization } from '../../models/api'
import { nextWidgetPosition, visualizationService } from '../../services/visualizationService'
import { useBusinessStore } from '../../stores/businessStore'

const store = useBusinessStore()
const router = useRouter()

onMounted(() => store.loadAll())

function moduleFor(moduleId: string) {
  return store.modules.find((module) => module.id === moduleId)
}

async function pin(visualization: Visualization) {
  const settings = visualizationService.getSettings(visualization)
  await visualizationService.createWidget({
    type: 'visualization',
    title: visualization.title,
    visualizationId: visualization.id,
    size: visualization.widgetSize,
    width: settings.width,
    height: settings.height,
    position: nextWidgetPosition(store.dashboardWidgets)
  })
  store.dashboardWidgets = await visualizationService.listWidgets()
}

async function remove(visualization: Visualization) {
  await visualizationService.remove(visualization.id)
  visualizationService.removeSettings(visualization.id)
  store.visualizations = await visualizationService.list()
}
</script>

<style scoped>
.visualization-grid {
  display: grid;
  gap: 16px;
  grid-template-columns: repeat(12, minmax(0, 1fr));
}

.actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  justify-content: flex-end;
}

.empty {
  background: var(--color-surface);
  border: 1px dashed var(--color-border);
  border-radius: var(--radius-card);
  display: grid;
  gap: 10px;
  justify-items: start;
  padding: 28px;
}

.empty h2,
.empty p {
  margin: 0;
}

.empty p {
  color: var(--color-muted);
}

@media (max-width: 1100px) {
  .visualization-grid {
    grid-template-columns: repeat(6, minmax(0, 1fr));
  }
}

@media (max-width: 760px) {
  .visualization-grid {
    grid-template-columns: 1fr;
  }
}
</style>

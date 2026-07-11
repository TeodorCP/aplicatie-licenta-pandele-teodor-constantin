<template>
  <div>
    <PageHeader
      title="Reports"
      subtitle="Lightweight operational reporting built from the modules, entries, and saved visualizations."
      eyebrow="Insights"
    />

    <section class="report-grid">
      <DashboardWidget v-for="item in moduleReports" :key="item.id" :title="item.name">
        <strong>{{ item.count }}</strong>
        <span>{{ item.category }} records</span>
      </DashboardWidget>
    </section>

    <ContentCard>
      <h2>Visualization Library</h2>
      <DataTable :columns="columns" :rows="store.visualizations" :loading="store.loading" />
    </ContentCard>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import ContentCard from '../../components/common/ContentCard.vue'
import PageHeader from '../../components/common/PageHeader.vue'
import DashboardWidget from '../../components/dashboard/DashboardWidget.vue'
import DataTable from '../../components/tables/DataTable.vue'
import { useBusinessStore } from '../../stores/businessStore'

const store = useBusinessStore()
const columns = [
  { key: 'title', label: 'Title' },
  { key: 'chartType', label: 'Chart' },
  { key: 'fieldName', label: 'Primary Field' },
  { key: 'aggregationType', label: 'Aggregation' }
]
const moduleReports = computed(() =>
  store.modules.map((module) => ({
    id: module.id,
    name: module.name,
    category: module.category,
    count: store.entriesFor(module.id).length
  }))
)

onMounted(() => store.loadAll())
</script>

<style scoped>
.report-grid {
  display: grid;
  gap: 16px;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  margin-bottom: 16px;
}

h2 {
  font-size: 18px;
  margin: 0 0 14px;
}

strong {
  display: block;
  font-size: 30px;
}

span {
  color: var(--color-muted);
}

@media (max-width: 980px) {
  .report-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 620px) {
  .report-grid {
    grid-template-columns: 1fr;
  }
}
</style>

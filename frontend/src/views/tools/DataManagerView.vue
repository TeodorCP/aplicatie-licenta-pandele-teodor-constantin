<template>
  <div>
    <PageHeader
      title="Data Manager"
      subtitle="Browse each module and jump straight into its records."
      eyebrow="Tools"
    />

    <ContentCard>
      <DataTable :columns="columns" :rows="rows" :loading="store.loading">
        <template #actions="{ row }">
          <RouterLink class="button secondary" :to="modulePath(row)">Open</RouterLink>
        </template>
      </DataTable>
    </ContentCard>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { RouterLink } from 'vue-router'
import ContentCard from '../../components/common/ContentCard.vue'
import PageHeader from '../../components/common/PageHeader.vue'
import DataTable from '../../components/tables/DataTable.vue'
import { normalize, useBusinessStore } from '../../stores/businessStore'

const store = useBusinessStore()
const columns = [
  { key: 'name', label: 'Module' },
  { key: 'category', label: 'Category' },
  { key: 'fieldCount', label: 'Fields' },
  { key: 'recordCount', label: 'Records' }
]
const rows = computed(() =>
  store.modules.map((module) => ({
    id: module.id,
    name: module.name,
    category: module.category,
    fieldCount: module.fields.length,
    recordCount: store.entriesFor(module.id).length
  }))
)

onMounted(() => store.loadAll())

function modulePath(row: unknown) {
  const name = typeof row === 'object' && row ? String((row as { name?: unknown }).name ?? '') : ''
  return `/modules/${normalize(name)}`
}
</script>

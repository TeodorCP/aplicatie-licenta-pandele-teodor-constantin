<template>
  <div>
    <PageHeader
      :title="module?.name ?? pageTitle"
      :subtitle="module ? `Manage ${module.name.toLowerCase()} records with the fields configured for this module.` : missingSubtitle"
      eyebrow="Module"
      primary-label="New record"
      @primary="openCreate"
    />

    <ContentCard>
      <DataTable
        :columns="columns"
        :rows="rows"
        :loading="store.loading"
        :error="store.error"
      >
        <template #actions="{ row }">
          <button class="button secondary" type="button" @click="edit(row)">Edit</button>
          <button class="button danger" type="button" @click="remove(row)">Delete</button>
        </template>
      </DataTable>
    </ContentCard>

    <FormDialog
      :open="dialogOpen"
      :title="editingId ? 'Edit record' : 'New record'"
      @close="closeDialog"
      @submit="save"
    >
      <label class="field">
        <span>Timestamp</span>
        <input v-model="timestamp" type="datetime-local" />
      </label>
      <DynamicEntryForm v-model="formData" :fields="module?.fields ?? []" required />
    </FormDialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import ContentCard from '../../components/common/ContentCard.vue'
import PageHeader from '../../components/common/PageHeader.vue'
import DynamicEntryForm from '../../components/forms/DynamicEntryForm.vue'
import FormDialog from '../../components/forms/FormDialog.vue'
import DataTable from '../../components/tables/DataTable.vue'
import type { Entry } from '../../models/api'
import * as apiService from '../../services/apiService'
import { normalize, useBusinessStore } from '../../stores/businessStore'

const route = useRoute()
const store = useBusinessStore()
const dialogOpen = ref(false)
const editingId = ref('')
const formData = ref<Record<string, unknown>>({})
const timestamp = ref('')

const moduleName = computed(() => String(route.params.moduleName ?? ''))
const pageTitle = computed(() => titleize(moduleName.value))
const module = computed(() => store.moduleByName(moduleName.value))
const rows = computed(() => {
  if (!module.value) return []
  return store.entriesFor(module.value.id).map((entry) => ({ id: entry.id, timestamp: formatDate(entry.timestamp), ...entry.data }))
})
const columns = computed(() => {
  const fields = module.value?.fields.map((field) => ({ key: field.name, label: field.name })) ?? []
  return [{ key: 'timestamp', label: 'Timestamp' }, ...fields]
})
const missingSubtitle = computed(() => `No module matching "${pageTitle.value}" was found.`)

onMounted(load)
watch(moduleName, load)

async function load() {
  if (store.modules.length === 0) {
    await store.refreshModules()
  }

  if (module.value) {
    await store.loadEntries(module.value.id)
  }
}

function openCreate() {
  editingId.value = ''
  formData.value = {}
  timestamp.value = toInputDate(new Date().toISOString())
  dialogOpen.value = true
}

function edit(row: unknown) {
  const entry = findEntry(row)
  if (!entry) return
  editingId.value = entry.id
  formData.value = { ...entry.data }
  timestamp.value = toInputDate(entry.timestamp)
  dialogOpen.value = true
}

async function remove(row: unknown) {
  const entry = findEntry(row)
  if (!entry || !module.value) return
  await apiService.deleteEntry(entry.id)
  await store.loadEntries(module.value.id)
}

async function save() {
  if (!module.value) return
  const payload = { timestamp: timestamp.value || undefined, data: formData.value }
  if (editingId.value) {
    await apiService.updateEntry(editingId.value, payload)
  } else {
    await apiService.createEntry(module.value.id, payload)
  }
  closeDialog()
  await store.loadEntries(module.value.id)
}

function closeDialog() {
  dialogOpen.value = false
}

function findEntry(row: unknown): Entry | undefined {
  if (!row || typeof row !== 'object' || !module.value) return undefined
  const id = String((row as { id?: unknown }).id ?? '')
  return store.entriesFor(module.value.id).find((entry) => entry.id === id)
}

function formatDate(value: string) {
  return new Date(value).toLocaleString()
}

function toInputDate(value: string) {
  return new Date(value).toISOString().slice(0, 16)
}

function titleize(value: string) {
  return normalize(value)
    ? value.replace(/-/g, ' ').replace(/\b\w/g, (letter) => letter.toUpperCase())
    : 'Module'
}
</script>

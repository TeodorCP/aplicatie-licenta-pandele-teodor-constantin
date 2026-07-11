<template>
  <div>
    <PageHeader
      title="Custom Modules"
      subtitle="Create and maintain custom data modules without changing backend contracts."
      eyebrow="Tools"
      primary-label="New module"
      @primary="openCreate"
    />

    <ContentCard>
      <DataTable :columns="columns" :rows="store.modules" :loading="store.loading">
        <template #actions="{ row }">
          <button class="button secondary" type="button" @click="edit(row)">Edit</button>
          <button class="button danger" type="button" @click="remove(row)">Delete</button>
        </template>
      </DataTable>
    </ContentCard>

    <FormDialog :open="dialogOpen" :title="editingId ? 'Edit module' : 'New module'" @close="close" @submit="save">
      <label class="field">
        <span>Name</span>
        <input v-model="name" required />
      </label>
      <div class="fields">
        <div v-for="(field, index) in fields" :key="index" class="field-row">
          <input v-model="field.name" placeholder="Field name" required />
          <select v-model="field.type">
            <option value="text">Text</option>
            <option value="number">Number</option>
            <option value="boolean">Boolean</option>
            <option value="timestamp">Date/time</option>
          </select>
          <button class="button secondary" type="button" @click="fields.splice(index, 1)">Remove</button>
        </div>
      </div>
      <button class="button secondary" type="button" @click="fields.push({ name: '', type: 'text' })">Add field</button>
    </FormDialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import ContentCard from '../../components/common/ContentCard.vue'
import PageHeader from '../../components/common/PageHeader.vue'
import FormDialog from '../../components/forms/FormDialog.vue'
import DataTable from '../../components/tables/DataTable.vue'
import type { BusinessModule, ModuleField } from '../../models/api'
import * as apiService from '../../services/apiService'
import { useBusinessStore } from '../../stores/businessStore'

const store = useBusinessStore()
const dialogOpen = ref(false)
const editingId = ref('')
const name = ref('')
const fields = ref<ModuleField[]>([])
const columns = [
  { key: 'name', label: 'Name' },
  { key: 'category', label: 'Category' },
  { key: 'createdAt', label: 'Created' }
]

onMounted(() => store.refreshModules())

function openCreate() {
  editingId.value = ''
  name.value = ''
  fields.value = [{ name: 'Name', type: 'text' }]
  dialogOpen.value = true
}

function edit(row: unknown) {
  const module = row as BusinessModule
  editingId.value = module.id
  name.value = module.name
  fields.value = module.fields.map((field) => ({ ...field }))
  dialogOpen.value = true
}

async function remove(row: unknown) {
  const module = row as BusinessModule
  await apiService.deleteModule(module.id)
  store.removeModule(module.id)
  await store.refreshModules()
}

async function save() {
  const cleanedFields = fields.value.filter((field) => field.name.trim())
  let savedModule: BusinessModule
  if (editingId.value) {
    savedModule = await apiService.updateModule(editingId.value, name.value, cleanedFields)
  } else {
    savedModule = await apiService.createModule(name.value, cleanedFields)
  }
  store.upsertModule(savedModule)
  close()
  await store.refreshModules()
}

function close() {
  dialogOpen.value = false
}
</script>

<style scoped>
.fields {
  display: grid;
  gap: 10px;
}

.field-row {
  display: grid;
  gap: 8px;
  grid-template-columns: 1fr 150px auto;
}

@media (max-width: 700px) {
  .field-row {
    grid-template-columns: 1fr;
  }
}
</style>

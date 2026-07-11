<template>
  <div>
    <PageHeader
      title="Roles"
      subtitle="Maintain business roles that drive module and field permissions."
      eyebrow="Admin"
      primary-label="New role"
      @primary="openCreate"
    />

    <ContentCard>
      <DataTable :columns="columns" :rows="roles" :loading="loading" :error="error">
        <template #actions="{ row }">
          <button class="button secondary" type="button" @click="edit(row)">Edit</button>
          <button class="button danger" type="button" @click="remove(row)">Delete</button>
        </template>
      </DataTable>
    </ContentCard>

    <FormDialog :open="dialogOpen" :title="editingId ? 'Edit role' : 'New role'" @close="dialogOpen = false" @submit="save">
      <label class="field">
        <span>Name</span>
        <input v-model="name" required />
      </label>
    </FormDialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import ContentCard from '../../components/common/ContentCard.vue'
import PageHeader from '../../components/common/PageHeader.vue'
import FormDialog from '../../components/forms/FormDialog.vue'
import DataTable from '../../components/tables/DataTable.vue'
import type { Role } from '../../models/api'
import * as apiService from '../../services/apiService'

const roles = ref<Role[]>([])
const loading = ref(false)
const error = ref('')
const dialogOpen = ref(false)
const editingId = ref('')
const name = ref('')
const columns = [
  { key: 'name', label: 'Name' },
  { key: 'isSystemRole', label: 'System' },
  { key: 'createdAt', label: 'Created' }
]

onMounted(load)

async function load() {
  loading.value = true
  error.value = ''
  try {
    roles.value = await apiService.getRoles()
  } catch {
    error.value = 'Could not load roles.'
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingId.value = ''
  name.value = ''
  dialogOpen.value = true
}

function edit(row: unknown) {
  const role = row as Role
  editingId.value = role.id
  name.value = role.name
  dialogOpen.value = true
}

async function save() {
  if (editingId.value) {
    await apiService.updateRole(editingId.value, name.value)
  } else {
    await apiService.createRole(name.value)
  }
  dialogOpen.value = false
  await load()
}

async function remove(row: unknown) {
  const role = row as Role
  await apiService.deleteRole(role.id)
  await load()
}
</script>

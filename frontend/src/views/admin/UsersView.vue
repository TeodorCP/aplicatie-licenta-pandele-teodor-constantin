<template>
  <div>
    <PageHeader
      title="Users"
      subtitle="Create users, assign roles, disable access, and reset passwords."
      eyebrow="Admin"
      primary-label="New user"
      @primary="openCreate"
    />

    <ContentCard>
      <p v-if="temporaryPassword" class="notice">Temporary password: {{ temporaryPassword }}</p>
      <DataTable :columns="columns" :rows="users" :loading="loading" :error="error">
        <template #actions="{ row }">
          <button class="button secondary" type="button" @click="edit(row)">Edit</button>
          <button class="button secondary" type="button" @click="resetPassword(row)">Reset password</button>
          <button class="button danger" type="button" @click="remove(row)">Delete</button>
        </template>
      </DataTable>
    </ContentCard>

    <FormDialog :open="dialogOpen" :title="editingId ? 'Edit user' : 'New user'" @close="dialogOpen = false" @submit="save">
      <label class="field">
        <span>Email</span>
        <input v-model="form.email" required type="email" />
      </label>
      <label class="field">
        <span>Full name</span>
        <input v-model="form.fullName" required />
      </label>
      <label class="field">
        <span>Role</span>
        <select v-model="form.roleId" required>
          <option v-for="role in roles" :key="role.id" :value="role.id">{{ role.name }}</option>
        </select>
      </label>
      <label v-if="!editingId" class="field">
        <span>Password</span>
        <input v-model="password" required type="password" />
      </label>
      <label v-else class="check">
        <input v-model="form.isActive" type="checkbox" />
        <span>Active</span>
      </label>
    </FormDialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import ContentCard from '../../components/common/ContentCard.vue'
import PageHeader from '../../components/common/PageHeader.vue'
import FormDialog from '../../components/forms/FormDialog.vue'
import DataTable from '../../components/tables/DataTable.vue'
import type { Role, UserSummary } from '../../models/api'
import * as apiService from '../../services/apiService'

const users = ref<UserSummary[]>([])
const roles = ref<Role[]>([])
const loading = ref(false)
const error = ref('')
const dialogOpen = ref(false)
const editingId = ref('')
const temporaryPassword = ref('')
const password = ref('')
const form = reactive({ email: '', fullName: '', roleId: '', isActive: true })
const columns = [
  { key: 'email', label: 'Email' },
  { key: 'fullName', label: 'Name' },
  { key: 'roleName', label: 'Role' },
  { key: 'isActive', label: 'Active' }
]

onMounted(load)

async function load() {
  loading.value = true
  error.value = ''
  try {
    const [userRows, roleRows] = await Promise.all([apiService.getUsers(), apiService.getRoles()])
    users.value = userRows
    roles.value = roleRows
  } catch {
    error.value = 'Could not load users.'
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingId.value = ''
  password.value = ''
  Object.assign(form, { email: '', fullName: '', roleId: roles.value[0]?.id ?? '', isActive: true })
  dialogOpen.value = true
}

function edit(row: unknown) {
  const user = row as UserSummary
  editingId.value = user.id
  Object.assign(form, { email: user.email, fullName: user.fullName, roleId: user.roleId, isActive: user.isActive })
  dialogOpen.value = true
}

async function save() {
  if (editingId.value) {
    await apiService.updateUser(editingId.value, form)
  } else {
    await apiService.createUser({ email: form.email, fullName: form.fullName, roleId: form.roleId, password: password.value })
  }
  dialogOpen.value = false
  await load()
}

async function resetPassword(row: unknown) {
  const user = row as UserSummary
  const response = await apiService.resetUserPassword(user.id)
  temporaryPassword.value = response.temporaryPassword
}

async function remove(row: unknown) {
  const user = row as UserSummary
  await apiService.deleteUser(user.id)
  await load()
}
</script>

<style scoped>
.notice {
  background: #fff8dc;
  border: 1px solid #ead998;
  border-radius: 8px;
  margin: 0 0 14px;
  padding: 10px 12px;
}

.check {
  align-items: center;
  display: flex;
  gap: 8px;
}
</style>

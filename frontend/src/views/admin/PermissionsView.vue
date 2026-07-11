<template>
  <div>
    <PageHeader
      title="Permissions"
      subtitle="Manage module-level and field-level access by role."
      eyebrow="Admin"
    />

    <ContentCard>
      <label class="field picker">
        <span>Module</span>
        <select v-model="moduleId">
          <option v-for="module in store.modules" :key="module.id" :value="module.id">{{ module.name }}</option>
        </select>
      </label>

      <h2>Module Permissions</h2>
      <div class="matrix">
        <table>
          <thead>
            <tr>
              <th>Role</th>
              <th>View</th>
              <th>Create</th>
              <th>Edit</th>
              <th>Delete</th>
              <th>Manage</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="permission in modulePermissions" :key="permission.roleId">
              <td>{{ permission.roleName }}</td>
              <td><input v-model="permission.canView" type="checkbox" /></td>
              <td><input v-model="permission.canCreate" type="checkbox" /></td>
              <td><input v-model="permission.canEdit" type="checkbox" /></td>
              <td><input v-model="permission.canDelete" type="checkbox" /></td>
              <td><input v-model="permission.canManagePermissions" type="checkbox" /></td>
            </tr>
          </tbody>
        </table>
      </div>

      <h2>Field Permissions</h2>
      <div class="matrix">
        <table>
          <thead>
            <tr>
              <th>Role</th>
              <th>Field</th>
              <th>View</th>
              <th>Edit</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="permission in fieldPermissions" :key="`${permission.roleId}-${permission.fieldName}`">
              <td>{{ permission.roleName }}</td>
              <td>{{ permission.fieldName }}</td>
              <td><input v-model="permission.canView" type="checkbox" /></td>
              <td><input v-model="permission.canEdit" type="checkbox" /></td>
            </tr>
          </tbody>
        </table>
      </div>

      <footer>
        <p v-if="message" class="notice">{{ message }}</p>
        <button class="button" type="button" @click="save">Save permissions</button>
      </footer>
    </ContentCard>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import ContentCard from '../../components/common/ContentCard.vue'
import PageHeader from '../../components/common/PageHeader.vue'
import type { FieldPermission, ModulePermission } from '../../models/api'
import * as apiService from '../../services/apiService'
import { useBusinessStore } from '../../stores/businessStore'

const store = useBusinessStore()
const moduleId = ref('')
const modulePermissions = ref<ModulePermission[]>([])
const fieldPermissions = ref<FieldPermission[]>([])
const message = ref('')

onMounted(async () => {
  await store.refreshModules()
  moduleId.value = store.modules[0]?.id ?? ''
})

watch(moduleId, async () => {
  message.value = ''
  if (!moduleId.value) return
  const [moduleRows, fieldRows] = await Promise.all([
    apiService.getModulePermissions(moduleId.value),
    apiService.getFieldPermissions(moduleId.value)
  ])
  modulePermissions.value = moduleRows
  fieldPermissions.value = fieldRows
})

async function save() {
  if (!moduleId.value) return
  await Promise.all([
    apiService.updateModulePermissions(moduleId.value, modulePermissions.value),
    apiService.updateFieldPermissions(moduleId.value, fieldPermissions.value)
  ])
  message.value = 'Permissions saved.'
}
</script>

<style scoped>
.picker {
  max-width: 420px;
}

h2 {
  font-size: 18px;
  margin: 24px 0 10px;
}

.matrix {
  overflow-x: auto;
}

table {
  border-collapse: collapse;
  min-width: 720px;
  width: 100%;
}

th,
td {
  border-bottom: 1px solid var(--color-border);
  padding: 10px;
  text-align: left;
}

th {
  color: var(--color-muted);
  font-size: 12px;
  text-transform: uppercase;
}

footer {
  align-items: center;
  display: flex;
  gap: 12px;
  justify-content: flex-end;
  margin-top: 18px;
}

.notice {
  color: var(--color-success);
  margin: 0;
}
</style>

<template>
  <div class="table-wrap">
    <div v-if="loading" class="status">Loading...</div>
    <div v-else-if="error" class="status error">{{ error }}</div>
    <div v-else-if="rows.length === 0" class="status">No records yet.</div>
    <table v-else>
      <thead>
        <tr>
          <th v-for="column in columns" :key="column.key">{{ column.label }}</th>
          <th v-if="$slots.actions">Actions</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="row in rows" :key="rowKey(row)">
          <td v-for="column in columns" :key="column.key">
            {{ formatCell(readCell(row, column.key)) }}
          </td>
          <td v-if="$slots.actions" class="row-actions">
            <slot name="actions" :row="row" />
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<script setup lang="ts">
type Row = Record<string, unknown>

defineProps<{
  columns: Array<{ key: string; label: string }>
  rows: unknown[]
  loading?: boolean
  error?: string
}>()

function rowKey(row: unknown) {
  return String(readCell(row, 'id') ?? JSON.stringify(row))
}

function readCell(row: unknown, key: string) {
  if (!row || typeof row !== 'object') return undefined
  return (row as Row)[key]
}

function formatCell(value: unknown) {
  if (value === null || value === undefined || value === '') return '-'
  if (typeof value === 'boolean') return value ? 'Yes' : 'No'
  if (typeof value === 'object') return JSON.stringify(value)
  return String(value)
}
</script>

<style scoped>
.table-wrap {
  overflow-x: auto;
}

table {
  border-collapse: collapse;
  min-width: 680px;
  width: 100%;
}

th,
td {
  border-bottom: 1px solid var(--color-border);
  padding: 11px 10px;
  text-align: left;
  vertical-align: top;
}

th {
  color: var(--color-muted);
  font-size: 12px;
  font-weight: 750;
  text-transform: uppercase;
}

.row-actions {
  white-space: nowrap;
}
</style>

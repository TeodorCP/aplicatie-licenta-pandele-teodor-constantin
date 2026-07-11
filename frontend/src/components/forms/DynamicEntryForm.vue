<template>
  <div class="form-grid">
    <label v-for="field in fields" :key="field.name" class="field">
      <span>{{ field.name }}</span>
      <select v-if="field.type === 'boolean'" v-model="model[field.name]">
        <option :value="true">Yes</option>
        <option :value="false">No</option>
      </select>
      <input
        v-else-if="isNumber(field.type)"
        v-model.number="model[field.name]"
        type="number"
        :required="required"
      />
      <input
        v-else-if="isDateLike(field.type)"
        v-model="model[field.name]"
        type="datetime-local"
        :required="required"
      />
      <input v-else v-model="model[field.name]" type="text" :required="required" />
    </label>
  </div>
</template>

<script setup lang="ts">
import type { ModuleField } from '../../models/api'

defineProps<{
  fields: ModuleField[]
  required?: boolean
}>()

const model = defineModel<Record<string, unknown>>({ required: true })

function isNumber(type: string) {
  return type.toLowerCase() === 'number'
}

function isDateLike(type: string) {
  return ['date', 'time', 'datetime', 'timestamp'].includes(type.toLowerCase())
}
</script>

<style scoped>
.form-grid {
  display: grid;
  gap: 12px;
}
</style>

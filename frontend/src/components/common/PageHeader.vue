<template>
  <header class="page-header">
    <div>
      <p v-if="eyebrow" class="eyebrow">{{ eyebrow }}</p>
      <h1>{{ title }}</h1>
      <p v-if="subtitle" class="subtitle">{{ subtitle }}</p>
    </div>
    <div class="actions">
      <slot name="actions" />
      <button v-if="primaryLabel" class="button" type="button" @click="$emit('primary')">
        {{ primaryLabel }}
      </button>
    </div>
  </header>
</template>

<script setup lang="ts">
defineProps<{
  title: string
  subtitle?: string
  eyebrow?: string
  primaryLabel?: string
}>()

defineEmits<{ primary: [] }>()
</script>

<style scoped>
.page-header {
  align-items: flex-start;
  display: flex;
  gap: 18px;
  justify-content: space-between;
  margin-bottom: 18px;
}

.eyebrow {
  color: var(--color-primary);
  font-size: 12px;
  font-weight: 750;
  letter-spacing: 0.08em;
  margin: 0 0 6px;
  text-transform: uppercase;
}

h1 {
  font-size: 28px;
  line-height: 1.1;
  margin: 0;
}

.subtitle {
  color: var(--color-muted);
  margin: 8px 0 0;
  max-width: 720px;
}

.actions {
  align-items: center;
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  justify-content: flex-end;
}

@media (max-width: 780px) {
  .page-header {
    display: grid;
  }

  .actions {
    justify-content: flex-start;
  }
}
</style>

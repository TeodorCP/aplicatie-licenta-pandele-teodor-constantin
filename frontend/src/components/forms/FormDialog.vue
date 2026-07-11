<template>
  <div v-if="open" class="overlay" @click.self="$emit('close')">
    <form class="dialog" @submit.prevent="$emit('submit')">
      <header>
        <h2>{{ title }}</h2>
        <button class="close" type="button" @click="$emit('close')">x</button>
      </header>
      <slot />
      <footer>
        <button class="button secondary" type="button" @click="$emit('close')">Cancel</button>
        <button class="button" type="submit">{{ submitLabel ?? 'Save' }}</button>
      </footer>
    </form>
  </div>
</template>

<script setup lang="ts">
defineProps<{ open: boolean; title: string; submitLabel?: string }>()
defineEmits<{ close: []; submit: [] }>()
</script>

<style scoped>
.overlay {
  align-items: center;
  background: rgba(16, 24, 20, 0.32);
  display: flex;
  inset: 0;
  justify-content: center;
  padding: 20px;
  position: fixed;
  z-index: 20;
}

.dialog {
  background: var(--color-surface);
  border-radius: var(--radius-card);
  box-shadow: 0 28px 80px rgba(16, 24, 20, 0.24);
  display: grid;
  gap: 14px;
  max-height: 90vh;
  max-width: 560px;
  overflow: auto;
  padding: 18px;
  width: 100%;
}

header,
footer {
  align-items: center;
  display: flex;
  gap: 12px;
  justify-content: space-between;
}

h2 {
  font-size: 20px;
  margin: 0;
}

.close {
  background: transparent;
  border: 0;
  color: var(--color-muted);
  font-size: 18px;
}
</style>

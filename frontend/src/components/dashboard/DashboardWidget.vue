<template>
  <article class="widget">
    <header>
      <h3>{{ title }}</h3>
      <slot name="actions" />
    </header>
    <div class="body">
      <slot />
    </div>
  </article>
</template>

<script setup lang="ts">
defineProps<{ title: string }>()
</script>

<style scoped>
.widget {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-card);
  box-shadow: var(--shadow-card);
  min-height: 132px;
  padding: 16px;
}

header {
  align-items: center;
  display: flex;
  justify-content: space-between;
  gap: 12px;
}

h3 {
  font-size: 15px;
  margin: 0;
}

.body {
  margin-top: 14px;
}
</style>

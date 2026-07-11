<template>
  <section class="content-card">
    <slot />
  </section>
</template>

<style scoped>
.content-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-card);
  box-shadow: var(--shadow-card);
  padding: 18px;
}
</style>

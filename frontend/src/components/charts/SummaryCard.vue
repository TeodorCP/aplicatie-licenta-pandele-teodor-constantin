<template>
  <div class="summary-card">
    <span>{{ label }}</span>
    <strong>{{ formattedValue }}</strong>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  value: number
  label: string
}>()

const formattedValue = computed(() =>
  new Intl.NumberFormat(undefined, { maximumFractionDigits: 2 }).format(props.value)
)
</script>

<style scoped>
.summary-card {
  align-content: center;
  background:
    radial-gradient(circle at 20% 10%, rgba(31, 111, 91, 0.16), transparent 34%),
    linear-gradient(135deg, #f7faf4, #fffdf5);
  border: 1px solid var(--color-border);
  border-radius: 16px;
  display: grid;
  min-height: 210px;
  padding: 24px;
}

span {
  color: var(--color-muted);
  font-weight: 700;
  text-transform: capitalize;
}

strong {
  color: var(--color-primary-dark);
  font-size: clamp(38px, 7vw, 72px);
  line-height: 1;
  margin-top: 8px;
}
</style>

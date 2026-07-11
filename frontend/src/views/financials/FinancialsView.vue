<template>
  <div>
    <PageHeader
      title="Financials"
      subtitle="Sales and expense records stay in their original modules, with totals summarized here."
      eyebrow="Money"
    />

    <section class="metrics">
      <DashboardWidget title="Sales Total"><strong>{{ currency(salesTotal) }}</strong></DashboardWidget>
      <DashboardWidget title="Expense Total"><strong>{{ currency(expenseTotal) }}</strong></DashboardWidget>
      <DashboardWidget title="Net"><strong>{{ currency(salesTotal - expenseTotal) }}</strong></DashboardWidget>
    </section>

    <section class="grid">
      <ContentCard>
        <h2>Sales</h2>
        <DataTable :columns="salesColumns" :rows="salesRows" :loading="store.loading" />
      </ContentCard>
      <ContentCard>
        <h2>Expenses</h2>
        <DataTable :columns="expenseColumns" :rows="expenseRows" :loading="store.loading" />
      </ContentCard>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import ContentCard from '../../components/common/ContentCard.vue'
import PageHeader from '../../components/common/PageHeader.vue'
import DashboardWidget from '../../components/dashboard/DashboardWidget.vue'
import DataTable from '../../components/tables/DataTable.vue'
import { useBusinessStore } from '../../stores/businessStore'

const store = useBusinessStore()
const salesModule = computed(() => store.moduleByName('sales'))
const expensesModule = computed(() => store.moduleByName('expenses'))
const salesRows = computed(() => salesModule.value ? store.entriesFor(salesModule.value.id).map((entry) => ({ id: entry.id, ...entry.data })) : [])
const expenseRows = computed(() => expensesModule.value ? store.entriesFor(expensesModule.value.id).map((entry) => ({ id: entry.id, ...entry.data })) : [])
const salesColumns = computed(() => salesModule.value?.fields.map((field) => ({ key: field.name, label: field.name })) ?? [])
const expenseColumns = computed(() => expensesModule.value?.fields.map((field) => ({ key: field.name, label: field.name })) ?? [])
const salesTotal = computed(() => sumMoney(salesRows.value))
const expenseTotal = computed(() => sumMoney(expenseRows.value))

onMounted(async () => {
  if (store.modules.length === 0) await store.refreshModules()
  await Promise.all([salesModule.value && store.loadEntries(salesModule.value.id), expensesModule.value && store.loadEntries(expensesModule.value.id)])
})

function sumMoney(rows: Array<Record<string, unknown>>) {
  return rows.reduce(
    (sum: number, row) =>
      sum + Object.values(row).reduce((inner: number, value) => inner + (typeof value === 'number' ? value : 0), 0),
    0
  )
}

function currency(value: number) {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD' }).format(value)
}
</script>

<style scoped>
.metrics,
.grid {
  display: grid;
  gap: 16px;
}

.metrics {
  grid-template-columns: repeat(3, minmax(0, 1fr));
  margin-bottom: 16px;
}

.grid {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

h2 {
  font-size: 18px;
  margin: 0 0 14px;
}

strong {
  font-size: 28px;
}

@media (max-width: 980px) {
  .metrics,
  .grid {
    grid-template-columns: 1fr;
  }
}
</style>

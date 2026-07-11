import { defineStore } from 'pinia'
import type { AnalyticsWidget, BusinessModule, DashboardWidget, Entry, Visualization } from '../models/api'
import * as apiService from '../services/apiService'

export const useBusinessStore = defineStore('business', {
  state: () => ({
    modules: [] as BusinessModule[],
    entriesByModule: {} as Record<string, Entry[]>,
    visualizations: [] as Visualization[],
    dashboardWidgets: [] as DashboardWidget[],
    analyticsWidgets: [] as AnalyticsWidget[],
    loading: false,
    error: ''
  }),
  getters: {
    moduleByName: (state) => (name: string) =>
      state.modules.find((module) => normalize(module.name) === normalize(name)),
    entriesFor: (state) => (moduleId: string) => state.entriesByModule[moduleId] ?? []
  },
  actions: {
    async loadAll() {
      this.loading = true
      this.error = ''
      try {
        this.modules = await apiService.getModules()
        await Promise.all(this.modules.map((module) => this.loadEntries(module.id)))
        this.visualizations = await apiService.getVisualizations()
        this.dashboardWidgets = await apiService.getDashboardWidgets()
        this.analyticsWidgets = await apiService.getAnalyticsWidgets()
      } catch (error) {
        this.error = 'Could not load business data.'
        throw error
      } finally {
        this.loading = false
      }
    },
    async loadEntries(moduleId: string) {
      this.entriesByModule[moduleId] = await apiService.getEntries(moduleId)
    },
    async refreshModules() {
      this.modules = await apiService.getModules()
    },
    upsertModule(module: BusinessModule) {
      const index = this.modules.findIndex((item) => item.id === module.id)
      if (index >= 0) {
        this.modules.splice(index, 1, module)
      } else {
        this.modules.push(module)
      }
      this.modules.sort((a, b) => a.name.localeCompare(b.name))
    },
    removeModule(moduleId: string) {
      this.modules = this.modules.filter((module) => module.id !== moduleId)
      delete this.entriesByModule[moduleId]
    }
  }
})

export function normalize(value: string) {
  return value.toLowerCase().replace(/[^a-z0-9]/g, '')
}

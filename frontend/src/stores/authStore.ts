import { defineStore } from 'pinia'
import type { AuthUser } from '../models/api'
import * as apiService from '../services/apiService'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem('business_ops_token') ?? '',
    user: null as AuthUser | null,
    loading: false,
    error: ''
  }),
  getters: {
    isAuthenticated: (state) => Boolean(state.token),
    canManageAdmin: (state) => Boolean(state.user?.canManageRoles || state.user?.canManagePermissions)
  },
  actions: {
    async login(email: string, password: string) {
      this.loading = true
      this.error = ''
      try {
        const response = await apiService.login(email, password)
        this.token = response.token
        this.user = response.user
        localStorage.setItem('business_ops_token', response.token)
      } catch (error) {
        this.error = 'Invalid email or password.'
        throw error
      } finally {
        this.loading = false
      }
    },
    async loadCurrentUser() {
      if (!this.token) return
      try {
        this.user = await apiService.currentUser()
      } catch {
        this.clear()
      }
    },
    async logout() {
      try {
        await apiService.logout()
      } finally {
        this.clear()
      }
    },
    clear() {
      this.token = ''
      this.user = null
      localStorage.removeItem('business_ops_token')
    }
  }
})

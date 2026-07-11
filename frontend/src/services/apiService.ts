import { api } from '../app/axios'
import type {
  AnalyticsWidget,
  AuthResponse,
  AuthUser,
  BusinessModule,
  DashboardWidget,
  Entry,
  FieldPermission,
  ModuleField,
  ModulePermission,
  Role,
  UserSummary,
  Visualization
} from '../models/api'

export async function login(email: string, password: string) {
  const { data } = await api.post<AuthResponse>('/api/auth/login', { email, password })
  return data
}

export async function currentUser() {
  const { data } = await api.get<AuthUser>('/api/auth/me')
  return data
}

export async function logout() {
  await api.post('/api/auth/logout')
}

export async function getModules() {
  const { data } = await api.get<BusinessModule[]>('/api/modules')
  return data
}

export async function createModule(name: string, fields: ModuleField[]) {
  const { data } = await api.post<BusinessModule>('/api/modules', { name, fields })
  return data
}

export async function updateModule(id: string, name: string, fields: ModuleField[]) {
  const { data } = await api.put<BusinessModule>(`/api/modules/${id}`, { name, fields })
  return data
}

export async function deleteModule(id: string) {
  await api.delete(`/api/modules/${id}`)
}

export async function getEntries(moduleId: string) {
  const { data } = await api.get<Entry[]>(`/api/modules/${moduleId}/entries`)
  return data
}

export async function createEntry(moduleId: string, payload: { timestamp?: string; data: Record<string, unknown> }) {
  const { data } = await api.post<Entry>(`/api/modules/${moduleId}/entries`, payload)
  return data
}

export async function updateEntry(id: string, payload: { timestamp?: string; data: Record<string, unknown> }) {
  const { data } = await api.put<Entry>(`/api/entries/${id}`, payload)
  return data
}

export async function deleteEntry(id: string) {
  await api.delete(`/api/entries/${id}`)
}

export async function getRoles() {
  const { data } = await api.get<Role[]>('/api/roles')
  return data
}

export async function createRole(name: string) {
  const { data } = await api.post<Role>('/api/roles', { name })
  return data
}

export async function updateRole(id: string, name: string) {
  const { data } = await api.put<Role>(`/api/roles/${id}`, { name })
  return data
}

export async function deleteRole(id: string) {
  await api.delete(`/api/roles/${id}`)
}

export async function getUsers() {
  const { data } = await api.get<UserSummary[]>('/api/users')
  return data
}

export async function createUser(payload: { email: string; fullName: string; roleId: string; password: string }) {
  const { data } = await api.post<UserSummary>('/api/users', payload)
  return data
}

export async function updateUser(id: string, payload: { email: string; fullName: string; roleId: string; isActive: boolean }) {
  const { data } = await api.put<UserSummary>(`/api/users/${id}`, payload)
  return data
}

export async function resetUserPassword(id: string) {
  const { data } = await api.post<{ temporaryPassword: string }>(`/api/users/${id}/reset-password`, {})
  return data
}

export async function deleteUser(id: string) {
  await api.delete(`/api/users/${id}`)
}

export async function getModulePermissions(moduleId: string) {
  const { data } = await api.get<ModulePermission[]>(`/api/modules/${moduleId}/permissions`)
  return data
}

export async function updateModulePermissions(moduleId: string, permissions: ModulePermission[]) {
  const { data } = await api.put<ModulePermission[]>(`/api/modules/${moduleId}/permissions`, { permissions })
  return data
}

export async function getFieldPermissions(moduleId: string) {
  const { data } = await api.get<FieldPermission[]>(`/api/modules/${moduleId}/field-permissions`)
  return data
}

export async function updateFieldPermissions(moduleId: string, permissions: FieldPermission[]) {
  const { data } = await api.put<FieldPermission[]>(`/api/modules/${moduleId}/field-permissions`, { permissions })
  return data
}

export async function getVisualizations() {
  const { data } = await api.get<Visualization[]>('/api/visualizations')
  return data
}

export async function createVisualization(payload: Partial<Visualization>) {
  const { data } = await api.post<Visualization>('/api/visualizations', payload)
  return data
}

export async function updateVisualization(id: string, payload: Partial<Visualization>) {
  const { data } = await api.put<Visualization>(`/api/visualizations/${id}`, payload)
  return data
}

export async function deleteVisualization(id: string) {
  await api.delete(`/api/visualizations/${id}`)
}

export async function getDashboardWidgets() {
  const { data } = await api.get<DashboardWidget[]>('/api/dashboard/widgets')
  return data
}

export async function createDashboardWidget(payload: Partial<DashboardWidget>) {
  const { data } = await api.post<DashboardWidget>('/api/dashboard/widgets', payload)
  return data
}

export async function updateDashboardWidget(id: string, payload: Partial<DashboardWidget>) {
  const { data } = await api.put<DashboardWidget>(`/api/dashboard/widgets/${id}`, payload)
  return data
}

export async function deleteDashboardWidget(id: string) {
  await api.delete(`/api/dashboard/widgets/${id}`)
}

export async function getAnalyticsWidgets() {
  const { data } = await api.get<AnalyticsWidget[]>('/api/analytics/widgets')
  return data
}

export async function createAnalyticsWidget(payload: Partial<AnalyticsWidget>) {
  const { data } = await api.post<AnalyticsWidget>('/api/analytics/widgets', payload)
  return data
}

export async function deleteAnalyticsWidget(id: string) {
  await api.delete(`/api/analytics/widgets/${id}`)
}

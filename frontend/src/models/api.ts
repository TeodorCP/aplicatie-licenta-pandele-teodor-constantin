export type ModuleField = {
  name: string
  type: string
  timestampDisplayMode?: string
}

export type BusinessModule = {
  id: string
  name: string
  category: string
  fields: ModuleField[]
  createdAt: string
}

export type Entry = {
  id: string
  moduleId: string
  createdByUserId?: string
  timestamp: string
  data: Record<string, unknown>
  createdAt: string
  updatedAt: string
  visibilityPermissions?: EntryVisibilityPermission[]
}

export type ModulePermission = {
  id: string
  roleId: string
  roleName: string
  moduleId: string
  canView: boolean
  canCreate: boolean
  canEdit: boolean
  canDelete: boolean
  canManagePermissions: boolean
}

export type FieldPermission = {
  id: string
  roleId: string
  roleName: string
  moduleId: string
  fieldName: string
  canView: boolean
  canEdit: boolean
}

export type EntryVisibilityPermission = {
  id: string
  entryId: string
  ownerUserId: string
  roleId: string
  roleName: string
  canView: boolean
  canEdit: boolean
}

export type AuthUser = {
  id: string
  email: string
  fullName: string
  roleId: string
  roleName: string
  isActive: boolean
  canManageRoles: boolean
  canManagePermissions: boolean
  modulePermissions: ModulePermission[]
  fieldPermissions: FieldPermission[]
}

export type AuthResponse = {
  token: string
  expiresAt: string
  user: AuthUser
}

export type Role = {
  id: string
  name: string
  isSystemRole: boolean
  createdAt: string
}

export type UserSummary = {
  id: string
  email: string
  fullName: string
  roleId: string
  roleName: string
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export type Visualization = {
  id: string
  moduleId: string
  module?: { name: string } | null
  title: string
  xField?: string | null
  xAggregation?: string | null
  yField?: string | null
  fieldName: string
  secondaryFieldName?: string | null
  chartType: string
  widgetSize: string
  aggregationType?: string | null
  dateRange?: string | null
  dateRangeType?: string | null
  customStartTimestamp?: string | null
  customEndTimestamp?: string | null
  summaryMetric?: string | null
  description?: string | null
  generalOptions?: Record<string, unknown> | null
  chartSpecificOptions?: Record<string, unknown> | null
  createdAt: string
  updatedAt?: string | null
}

export type DashboardWidget = {
  id: string
  type: string
  title: string
  moduleId?: string | null
  visualizationId?: string | null
  size: string
  width: number
  height: number
  x?: number | null
  y?: number | null
  position: number
  visualSettings?: Record<string, unknown> | null
  createdAt: string
  updatedAt: string
}

export type AnalyticsWidget = {
  id: string
  title: string
  moduleAId: string
  fieldAName: string
  moduleBId: string
  fieldBName: string
  chartType: string
  dateRange?: string | null
  createdAt: string
  updatedAt: string
}

import { createRouter, createWebHistory } from 'vue-router'
import AppLayout from '../components/layout/AppLayout.vue'
import { useAuthStore } from '../stores/authStore'
import DashboardView from '../views/dashboard/DashboardView.vue'
import LoginView from '../views/auth/LoginView.vue'
import ModuleView from '../views/modules/ModuleView.vue'
import CalendarView from '../views/calendar/CalendarView.vue'
import FinancialsView from '../views/financials/FinancialsView.vue'
import ReportsView from '../views/reports/ReportsView.vue'
import DataManagerView from '../views/tools/DataManagerView.vue'
import CustomModulesView from '../views/modules/CustomModulesView.vue'
import VisualizationsView from '../views/reports/VisualizationsView.vue'
import VisualizationCreatorView from '../views/reports/VisualizationCreatorView.vue'
import UsersView from '../views/admin/UsersView.vue'
import RolesView from '../views/admin/RolesView.vue'
import PermissionsView from '../views/admin/PermissionsView.vue'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', name: 'login', component: LoginView, meta: { public: true } },
    {
      path: '/',
      component: AppLayout,
      children: [
        { path: '', name: 'dashboard', component: DashboardView },
        { path: 'modules/:moduleName', name: 'module', component: ModuleView },
        { path: 'calendar', name: 'calendar', component: CalendarView },
        { path: 'financials', name: 'financials', component: FinancialsView },
        { path: 'reports', name: 'reports', component: ReportsView },
        { path: 'tools/data-manager', name: 'data-manager', component: DataManagerView },
        { path: 'tools/custom-modules', name: 'custom-modules', component: CustomModulesView },
        { path: 'tools/visualizations', name: 'visualizations', component: VisualizationsView },
        { path: 'tools/visualizations/new', name: 'visualization-new', component: VisualizationCreatorView },
        { path: 'tools/visualizations/:id/edit', name: 'visualization-edit', component: VisualizationCreatorView },
        { path: 'admin/users', name: 'users', component: UsersView },
        { path: 'admin/roles', name: 'roles', component: RolesView },
        { path: 'admin/permissions', name: 'permissions', component: PermissionsView }
      ]
    }
  ]
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()

  if (to.meta.public) {
    return auth.isAuthenticated ? { name: 'dashboard' } : true
  }

  if (!auth.isAuthenticated) {
    return { name: 'login' }
  }

  if (!auth.user) {
    await auth.loadCurrentUser()
  }

  if (to.path.startsWith('/admin') && !auth.canManageAdmin) {
    return { name: 'dashboard' }
  }

  return true
})

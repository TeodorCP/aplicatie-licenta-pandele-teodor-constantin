<template>
  <div class="layout">
    <aside class="sidebar">
      <div class="brand">
        <span class="brand-mark">BO</span>
        <div class="nav-label">
          <strong>Business Ops</strong>
          <span>ByteBridge</span>
        </div>
      </div>
      <nav>
        <p><span class="nav-label">Main</span></p>
        <RouterLink
          v-for="item in mainItems"
          :key="item.to"
          :to="item.to"
          :title="item.label"
          :class="{ 'dashboard-link': item.to === '/' }"
        >
          <span class="nav-icon">{{ navInitial(item.label) }}</span>
          <span class="nav-label">{{ item.label }}</span>
        </RouterLink>
        <template v-if="customModuleItems.length">
          <p><span class="nav-label">Custom</span></p>
          <RouterLink v-for="item in customModuleItems" :key="item.to" :to="item.to" :title="item.label">
            <span class="nav-icon">{{ navInitial(item.label) }}</span>
            <span class="nav-label">{{ item.label }}</span>
          </RouterLink>
        </template>
        <p><span class="nav-label">Tools</span></p>
        <RouterLink v-for="item in toolItems" :key="item.to" :to="item.to" :title="item.label">
          <span class="nav-icon">{{ navInitial(item.label) }}</span>
          <span class="nav-label">{{ item.label }}</span>
        </RouterLink>
        <template v-if="auth.canManageAdmin">
          <p><span class="nav-label">Admin</span></p>
          <RouterLink v-for="item in adminItems" :key="item.to" :to="item.to" :title="item.label">
            <span class="nav-icon">{{ navInitial(item.label) }}</span>
            <span class="nav-label">{{ item.label }}</span>
          </RouterLink>
        </template>
      </nav>
    </aside>
    <main>
      <div class="topbar">
        <span>{{ auth.user?.fullName }}</span>
        <button class="button secondary" type="button" @click="signOut">Logout</button>
      </div>
      <section class="page">
        <RouterView />
      </section>
    </main>
  </div>
</template>

<script setup lang="ts">
import { RouterLink, RouterView, useRouter } from 'vue-router'
import { computed, onMounted } from 'vue'
import { useAuthStore } from '../../stores/authStore'
import { normalize, useBusinessStore } from '../../stores/businessStore'

const auth = useAuthStore()
const business = useBusinessStore()
const router = useRouter()

const mainItems = [
  { label: 'Dashboard', to: '/' },
  { label: 'Tasks', to: '/modules/tasks' },
  { label: 'Calendar', to: '/calendar' },
  { label: 'Projects', to: '/modules/projects' },
  { label: 'Team', to: '/modules/team-members' },
  { label: 'Clients', to: '/modules/clients' },
  { label: 'Financials', to: '/financials' },
  { label: 'Reports', to: '/reports' }
]

const toolItems = [
  { label: 'Data Manager', to: '/tools/data-manager' },
  { label: 'Custom Modules', to: '/tools/custom-modules' },
  { label: 'Visualizations', to: '/tools/visualizations' }
]

const adminItems = [
  { label: 'Users', to: '/admin/users' },
  { label: 'Roles', to: '/admin/roles' },
  { label: 'Permissions', to: '/admin/permissions' }
]

const customModuleItems = computed(() =>
  business.modules
    .filter((module) => normalize(module.category) === 'custom')
    .map((module) => ({
      label: module.name,
      to: `/modules/${slugForModule(module.name)}`
    }))
)

onMounted(() => {
  if (business.modules.length === 0) {
    void business.refreshModules()
  }
})

async function signOut() {
  await auth.logout()
  await router.push('/login')
}

function slugForModule(name: string) {
  return name.trim().toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '')
}

function navInitial(label: string) {
  return label.trim().slice(0, 1).toUpperCase() || '?'
}
</script>

<style scoped>
.layout {
  display: grid;
  grid-template-columns: 76px minmax(0, 1fr);
  height: 100vh;
  overflow: hidden;
}

.sidebar {
  background: #17251f;
  color: #edf5ef;
  display: flex;
  flex-direction: column;
  height: 100vh;
  overflow: hidden;
  padding: 18px 12px;
  transition: width 180ms ease, padding 180ms ease;
  width: 76px;
  z-index: 20;
}

.sidebar:hover,
.sidebar:focus-within {
  padding: 20px;
  width: 260px;
}

.brand {
  align-items: center;
  display: flex;
  gap: 10px;
  margin-bottom: 24px;
  min-height: 40px;
  white-space: nowrap;
}

.brand-mark,
.nav-icon {
  align-items: center;
  background: rgba(255, 255, 255, 0.11);
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 9px;
  color: #edf5ef;
  display: inline-flex;
  flex: 0 0 36px;
  font-size: 13px;
  font-weight: 800;
  height: 36px;
  justify-content: center;
}

.brand span,
nav p {
  color: #a9bbb3;
  font-size: 12px;
}

nav {
  display: grid;
  gap: 6px;
  min-height: 0;
  overflow-y: auto;
  overflow-x: hidden;
  padding-right: 4px;
  scrollbar-width: none;
  -ms-overflow-style: none;
}

nav::-webkit-scrollbar {
  display: none;
}

nav p {
  font-weight: 800;
  margin: 16px 0 4px;
  text-transform: uppercase;
}

a {
  align-items: center;
  border-radius: 7px;
  color: #edf5ef;
  display: flex;
  gap: 10px;
  min-height: 42px;
  padding: 10px 12px;
  text-decoration: none;
  white-space: nowrap;
}

.nav-label {
  opacity: 0;
  overflow: hidden;
  transition: opacity 120ms ease;
}

.sidebar:hover .nav-label,
.sidebar:focus-within .nav-label {
  opacity: 1;
}

a.router-link-active,
a:hover {
  background: rgba(255, 255, 255, 0.11);
}

.dashboard-link.router-link-active:not(.router-link-exact-active) {
  background: transparent;
}

main {
  display: grid;
  grid-template-rows: auto minmax(0, 1fr);
  height: 100vh;
  min-width: 0;
  overflow: hidden;
}

.topbar {
  align-items: center;
  background: var(--color-surface);
  border-bottom: 1px solid var(--color-border);
  display: flex;
  justify-content: flex-end;
  min-height: 64px;
  padding: 0 var(--spacing-page);
  gap: 12px;
}

.page {
  min-height: 0;
  overflow-y: auto;
  padding: var(--spacing-page);
}

@media (max-width: 900px) {
  .layout {
    grid-template-columns: 1fr;
    height: auto;
    overflow: visible;
  }

  .sidebar {
    height: auto;
    min-height: auto;
    overflow-x: auto;
    overflow-y: visible;
    padding: 14px var(--spacing-page);
    position: static;
    width: auto;
  }

  .sidebar:hover,
  .sidebar:focus-within {
    padding: 14px var(--spacing-page);
    width: auto;
  }

  .brand {
    margin-bottom: 12px;
  }

  .nav-label {
    opacity: 1;
  }

  nav {
    display: flex;
    flex-wrap: wrap;
    overflow: visible;
    padding-right: 0;
  }

  nav p {
    display: none;
  }

  main {
    height: auto;
    overflow: visible;
  }

  .page {
    overflow: visible;
  }
}
</style>

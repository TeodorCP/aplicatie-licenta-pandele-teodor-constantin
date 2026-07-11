<template>
  <main class="login-page">
    <section class="login-card">
      <div class="copy">
        <p class="eyebrow">Business Ops</p>
        <h1>Run the company from one calm control room.</h1>
        <p>
          Sign in to manage modules, entries, roles, dashboards, analytics, and permissions.
        </p>
      </div>
      <form @submit.prevent="submit">
        <label class="field">
          <span>Email</span>
          <input v-model="email" autocomplete="email" required type="email" />
        </label>
        <label class="field">
          <span>Password</span>
          <input v-model="password" autocomplete="current-password" required type="password" />
        </label>
        <p v-if="auth.error" class="error">{{ auth.error }}</p>
        <button class="button" type="submit" :disabled="auth.loading">
          {{ auth.loading ? 'Signing in...' : 'Sign in' }}
        </button>
      </form>
    </section>
  </main>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../../stores/authStore'

const auth = useAuthStore()
const router = useRouter()
const email = ref('alex@bytebridge.local')
const password = ref('Password123!')

async function submit() {
  await auth.login(email.value, password.value)
  await router.push('/')
}
</script>

<style scoped>
.login-page {
  align-items: center;
  background:
    radial-gradient(circle at 15% 20%, rgba(31, 111, 91, 0.18), transparent 32%),
    linear-gradient(135deg, #edf4e8 0%, #f8f4e8 52%, #e7f0ec 100%);
  display: grid;
  min-height: 100vh;
  padding: 24px;
}

.login-card {
  background: rgba(255, 255, 255, 0.82);
  border: 1px solid rgba(31, 111, 91, 0.15);
  border-radius: 24px;
  box-shadow: 0 30px 90px rgba(25, 45, 37, 0.16);
  display: grid;
  gap: 28px;
  grid-template-columns: 1.1fr 0.9fr;
  margin: 0 auto;
  max-width: 980px;
  padding: 34px;
  width: 100%;
}

.copy {
  align-content: center;
  background: #17251f;
  border-radius: 18px;
  color: #f4f7ef;
  display: grid;
  min-height: 430px;
  padding: 34px;
}

.eyebrow {
  color: #b7d8bb;
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0.1em;
  text-transform: uppercase;
}

h1 {
  font-size: clamp(34px, 5vw, 58px);
  line-height: 0.96;
  margin: 0;
}

.copy p:last-child {
  color: #c8d5ce;
  font-size: 17px;
  max-width: 440px;
}

form {
  align-content: center;
  display: grid;
  gap: 16px;
}

@media (max-width: 820px) {
  .login-card {
    grid-template-columns: 1fr;
    padding: 18px;
  }

  .copy {
    min-height: 260px;
  }
}
</style>

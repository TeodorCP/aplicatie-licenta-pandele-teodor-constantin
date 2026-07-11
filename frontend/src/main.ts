import { createApp } from 'vue'

import App from './App.vue'
import { pinia } from './app/pinia'
import { router } from './app/router'
import './theme/theme.css'

createApp(App).use(pinia).use(router).mount('#app')

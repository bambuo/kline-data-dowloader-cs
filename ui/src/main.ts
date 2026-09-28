import { createApp } from 'vue'
import { createRouter, createWebHashHistory } from 'vue-router'
import ArcoVue from '@arco-design/web-vue'
import '@arco-design/web-vue/dist/arco.css'
import App from './App.vue'
import './style.css'

const router = createRouter({
  history: createWebHashHistory(),
  routes: [
    { path: '/', name: 'DataManager', component: () => import('./views/DataManager.vue') },
  ],
})

const app = createApp(App)
app.use(router)
app.use(ArcoVue)
app.mount('#app')

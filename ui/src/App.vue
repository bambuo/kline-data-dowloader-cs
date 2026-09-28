<script setup lang="ts">
</script>

<template>
  <a-layout class="app-layout">
    <a-layout-header class="app-header">
      <h2 class="app-title">📊 K 线数据下载器</h2>
      <span class="app-subtitle">从 Binance 数据归档拉取历史 K 线</span>
    </a-layout-header>
    <a-layout-content class="app-content">
      <router-view />
    </a-layout-content>
  </a-layout>
</template>

<style scoped>
.app-layout {
  min-height: 100vh;
  background: var(--color-bg-1);
}
.app-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 0 24px;
  height: 56px;
  background: var(--color-bg-2);
  border-bottom: 1px solid var(--color-border-1);
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.06);
}
.app-title {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  color: var(--color-text-1);
}
.app-subtitle {
  font-size: 13px;
  color: var(--color-text-3);
}
.app-content {
  padding: 24px;
}
</style>

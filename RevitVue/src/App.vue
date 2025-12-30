<script setup>
import { ref, onMounted } from 'vue'
import WallOpeningPage from './components/WallOpeningPage.vue'
import SettingPage from './components/SettingPage.vue'

const currentPage = ref('wall-opening')

onMounted(() => {
  // 从 URL 参数获取页面名称
  const params = new URLSearchParams(window.location.search)
  const page = params.get('page')
  if (page) {
    currentPage.value = page
  }
})
</script>

<template>
  <div class="app">
    <WallOpeningPage v-if="currentPage === 'wall-opening'" />
    <SettingPage v-else-if="currentPage === 'setting'" />
    <div v-else class="error">未知页面: {{ currentPage }}</div>
  </div>
</template>

<style>
* { margin: 0; padding: 0; box-sizing: border-box; }

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
  background: #1a1a2e;
  color: #eee;
}

.app {
  padding: 16px;
  max-width: 400px;
  margin: 0 auto;
}

.error {
  padding: 20px;
  text-align: center;
  color: #ff6b6b;
}
</style>

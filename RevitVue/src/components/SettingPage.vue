<script setup>
import { useRevitBridge } from '../composables/useRevitBridge'

const { isInRevit } = useRevitBridge({})
</script>

<template>
  <div>
    <header class="header">
      <h1>设置</h1>
      <span v-if="!isInRevit" class="dev-badge">开发模式</span>
    </header>

    <main class="card">
      <div class="setting-item">
        <h3>关于</h3>
        <p>RevitFlow - Revit 插件</p>
        <p>版本: 1.0.0</p>
      </div>

      <div class="setting-item">
        <h3>功能</h3>
        <ul>
          <li>墙体开洞</li>
          <li>更多功能开发中...</li>
        </ul>
      </div>
    </main>
  </div>
</template>

<style scoped>
.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.header h1 { font-size: 18px; }

.dev-badge {
  font-size: 11px;
  padding: 3px 8px;
  background: #ff6b6b;
  border-radius: 8px;
}

.card {
  background: #16213e;
  border-radius: 10px;
  padding: 16px;
}

.setting-item {
  margin-bottom: 20px;
}

.setting-item:last-child {
  margin-bottom: 0;
}

.setting-item h3 {
  font-size: 14px;
  margin-bottom: 8px;
  color: #4dabf7;
}

.setting-item p {
  font-size: 13px;
  color: #aaa;
  margin-bottom: 4px;
}

.setting-item ul {
  list-style: none;
  padding: 0;
}

.setting-item li {
  font-size: 13px;
  color: #aaa;
  padding: 4px 0;
}

.setting-item li:before {
  content: "• ";
  color: #4dabf7;
  margin-right: 8px;
}
</style>

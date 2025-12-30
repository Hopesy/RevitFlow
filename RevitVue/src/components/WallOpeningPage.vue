<script setup>
import { useRevitBridge } from '../composables/useRevitBridge'

const { isInRevit, state, setState, invokeCommand } = useRevitBridge({
  width: 1000,
  height: 2100,
  sillHeight: 0,
  radius: 500,
  shape: 'rectangle'
})

function applyPreset(preset) {
  const presets = {
    door: { width: 900, height: 2100, sillHeight: 0, shape: 'rectangle' },
    window: { width: 1500, height: 1500, sillHeight: 900, shape: 'rectangle' },
    circle: { radius: 300, sillHeight: 1000, shape: 'circle' }
  }
  if (presets[preset]) {
    Object.assign(state, presets[preset])
    setState(presets[preset])
  }
}

function createOpening() {
  invokeCommand('CreateOpening')
}
</script>

<template>
  <div>
    <header class="header">
      <h1>墙体开洞</h1>
      <span v-if="!isInRevit" class="dev-badge">开发模式</span>
    </header>

    <main class="card">
      <!-- 预设 -->
      <div class="presets">
        <button @click="applyPreset('door')">🚪 门洞</button>
        <button @click="applyPreset('window')">🪟 窗洞</button>
        <button @click="applyPreset('circle')">⭕ 圆洞</button>
      </div>

      <!-- 矩形参数 -->
      <template v-if="state.shape === 'rectangle'">
        <div class="form-row">
          <div class="form-group">
            <label>宽度 (mm)</label>
            <input type="number" v-model.number="state.width" @change="setState({ width: state.width })" />
          </div>
          <div class="form-group">
            <label>高度 (mm)</label>
            <input type="number" v-model.number="state.height" @change="setState({ height: state.height })" />
          </div>
        </div>
      </template>

      <!-- 圆形参数 -->
      <template v-else>
        <div class="form-group">
          <label>半径 (mm)</label>
          <input type="number" v-model.number="state.radius" @change="setState({ radius: state.radius })" />
        </div>
      </template>

      <!-- 底部高度 -->
      <div class="form-group">
        <label>底部离地高度 (mm)</label>
        <input type="number" v-model.number="state.sillHeight" @change="setState({ sillHeight: state.sillHeight })" />
      </div>

      <!-- 创建按钮 -->
      <button class="btn-primary" @click="createOpening">创建洞口</button>

      <p class="tips">💡 点击后在 Revit 中选择墙体和位置</p>
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

.presets {
  display: flex;
  gap: 8px;
  margin-bottom: 16px;
}

.presets button {
  flex: 1;
  padding: 8px;
  border: 1px solid #333;
  border-radius: 6px;
  background: transparent;
  color: #aaa;
  cursor: pointer;
}

.presets button:hover {
  border-color: #4dabf7;
  color: #4dabf7;
}

.form-group {
  margin-bottom: 12px;
}

.form-group label {
  display: block;
  font-size: 12px;
  color: #888;
  margin-bottom: 4px;
}

.form-group input {
  width: 100%;
  padding: 8px 10px;
  border: 1px solid #333;
  border-radius: 6px;
  background: #0f0f23;
  color: #fff;
  font-size: 14px;
}

.form-group input:focus {
  outline: none;
  border-color: #4dabf7;
}

.form-row {
  display: flex;
  gap: 12px;
}

.form-row .form-group { flex: 1; }

.btn-primary {
  width: 100%;
  padding: 12px;
  margin-top: 8px;
  border: none;
  border-radius: 6px;
  background: #4dabf7;
  color: #000;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
}

.btn-primary:hover { background: #339af0; }

.tips {
  margin-top: 12px;
  font-size: 12px;
  color: #666;
  text-align: center;
}
</style>

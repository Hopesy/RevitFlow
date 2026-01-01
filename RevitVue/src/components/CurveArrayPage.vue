<script setup>
import { ref } from 'vue'
import { useRevitBridge } from '../composables/useRevitBridge'

const { state, setState, invokeCommand, log } = useRevitBridge({
  selectedFamilyName: '',
  count: 10,
  alignToPath: true
})

const familyList = ref([])

// 接收 C# 发送的族列表
window.onFamilyListReceived = (jsonString) => {
  log('=== onFamilyListReceived 被调用 ===')
  log(`收到的原始数据: ${jsonString}`)
  log(`数据类型: ${typeof jsonString}`)

  try {
    const list = JSON.parse(jsonString)
    log(`解析后的数据: ${JSON.stringify(list)}`)
    log(`数组长度: ${list.length}`)

    familyList.value = list
    log(`familyList.value 已更新，长度: ${familyList.value.length}`)

    // 默认选择第一个
    if (list.length > 0) {
      state.selectedFamilyName = list[0]
      setState({ selectedFamilyName: list[0] })
      log(`默认选择: ${list[0]}`)
    } else {
      log('族列表为空', 'warn')
    }
  } catch (error) {
    log(`解析族列表失败: ${error.message}`, 'error')
  }
}

log('window.onFamilyListReceived 已定义')

function createArray() {
  if (!state.selectedFamilyName) {
    alert('请选择族类型')
    return
  }
  log(`准备创建阵列 - 族类型: ${state.selectedFamilyName}, 数量: ${state.count}`)
  invokeCommand('CreateArray')
}
</script>

<template>
  <div>
    <header class="header">
      <h1>曲线阵列族</h1>
    </header>

    <main class="card">
      <!-- 族类型选择 -->
      <div class="form-group">
        <label>族类型</label>
        <select
          v-model="state.selectedFamilyName"
          @change="setState({ selectedFamilyName: state.selectedFamilyName })"
        >
          <option value="">请选择族类型</option>
          <option v-for="family in familyList" :key="family" :value="family">
            {{ family }}
          </option>
        </select>
      </div>

      <!-- 阵列数量 -->
      <div class="form-group">
        <label>阵列数量</label>
        <input
          type="number"
          v-model.number="state.count"
          @change="setState({ count: state.count })"
          min="2"
          max="100"
        />
      </div>

      <!-- 对齐到路径 -->
      <div class="form-group">
        <label class="checkbox-label">
          <input
            type="checkbox"
            v-model="state.alignToPath"
            @change="setState({ alignToPath: state.alignToPath })"
          />
          <span>对齐到路径</span>
        </label>
      </div>

      <!-- 创建按钮 -->
      <button class="btn-primary" @click="createArray">创建阵列</button>

      <p class="tips">💡 点击后选择模型线即可</p>
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

.card {
  background: #16213e;
  border-radius: 10px;
  padding: 16px;
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

.form-group input,
.form-group select {
  width: 100%;
  padding: 8px 10px;
  border: 1px solid #333;
  border-radius: 6px;
  background: #0f0f23;
  color: #fff;
  font-size: 14px;
}

.form-group input:focus,
.form-group select:focus {
  outline: none;
  border-color: #4dabf7;
}

.checkbox-label {
  display: flex;
  align-items: center;
  cursor: pointer;
}

.checkbox-label input[type="checkbox"] {
  width: auto;
  margin-right: 8px;
}

.checkbox-label span {
  font-size: 14px;
  color: #fff;
}

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

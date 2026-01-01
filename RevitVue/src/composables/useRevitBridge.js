import { ref, reactive } from 'vue'

/**
 * Revit ViewModel 桥接
 */
export function useRevitBridge(defaultState = {}) {
  const isInRevit = typeof window !== 'undefined' && !!window.RevitBridge
  const state = reactive({ ...defaultState })

  /**
   * 更新 ViewModel 属性
   */
  function setState(newState) {
    Object.assign(state, newState)
    if (isInRevit) {
      window.RevitBridge.invoke('setState', newState)
    }
  }

  /**
   * 调用 ViewModel 命令 (不等待响应)
   */
  function invokeCommand(command, param = null) {
    if (isInRevit) {
      window.RevitBridge.invoke('invokeCommand', { command, param })
    }
  }

  /**
   * 发送日志到 C# 端
   */
  function log(message, level = 'info') {
    if (isInRevit) {
      window.RevitBridge.invoke('log', { level, message })
    } else {
      // 开发环境下输出到控制台
      console.log(`[${level}] ${message}`)
    }
  }

  return {
    isInRevit,
    state,
    setState,
    invokeCommand,
    log
  }
}

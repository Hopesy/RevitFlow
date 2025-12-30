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

  return {
    isInRevit,
    state,
    setState,
    invokeCommand
  }
}

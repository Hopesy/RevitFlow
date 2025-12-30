import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  base: './', // 使用相对路径，便于本地文件加载
  build: {
    outDir: '../RevitFlow/Web', // 输出到 C# 项目的 Web 目录
    emptyOutDir: true,
    rollupOptions: {
      output: {
        // 简化文件名，便于调试
        entryFileNames: 'js/[name].js',
        chunkFileNames: 'js/[name].js',
        assetFileNames: (assetInfo) => {
          if (assetInfo.name?.endsWith('.css')) {
            return 'css/[name][extname]'
          }
          return 'assets/[name][extname]'
        }
      }
    }
  },
  server: {
    port: 5173,
    open: true
  }
})

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  server: {
    // 允许 Aspire 动态分配端口
    port: process.env.PORT ? parseInt(process.env.PORT) : 5173,
    proxy: {
      // 拦截所有 /api 开头的请求，代理给后端
      '/api': {
        target: process.env.VITE_API_URL || 'https://localhost:7041',
        changeOrigin: true,
        secure: false 
      }
    }
  }
})
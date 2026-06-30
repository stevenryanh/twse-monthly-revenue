import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// 固定 dev server port 為 5173，對齊後端 CORS 放行來源。
export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5173,
  },
})

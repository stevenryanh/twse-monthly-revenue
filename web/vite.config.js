import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// dev server：固定 5173。
// - host: true → 綁 0.0.0.0，讓同一 Tailscale 網段的裝置可連
// - allowedHosts: true → 放行 *.ts.net 等 Host（Tailscale Serve 的網址）
// - proxy /api → 後端 5080：前端走「同源 /api」，經 Tailscale 一個網址全通、且免 CORS
export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5173,
    host: true,
    allowedHosts: true,
    proxy: {
      '/api': 'http://localhost:5080',
    },
  },
})

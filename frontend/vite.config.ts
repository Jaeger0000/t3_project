import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    // Backend'e /api ile gidilir; böylece istemci kodunda mutlak URL tutulmaz
    // ve tarayıcı tarafında CORS devreye girmez.
    proxy: {
      '/api': {
        target: process.env.VITE_API_TARGET ?? 'http://localhost:5080',
        changeOrigin: true,
      },
      '/health': {
        target: process.env.VITE_API_TARGET ?? 'http://localhost:5080',
        changeOrigin: true,
      },
    },
  },
})

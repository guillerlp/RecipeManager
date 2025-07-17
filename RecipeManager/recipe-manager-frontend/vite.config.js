import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'https://localhost:7231', // Your .NET API URL
        changeOrigin: true,
        secure: false, // Set to true if using HTTPS with valid certificates
      }
    }
  }
})

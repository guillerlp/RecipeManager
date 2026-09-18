import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
import path from 'path'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(import.meta.dirname,'./src'),
      '@components': path.resolve(import.meta.dirname,'./src/components'),
      '@contexts': path.resolve(import.meta.dirname,'./src/contexts'),
      '@pages': path.resolve(import.meta.dirname,'./src/pages'),
      '@hooks': path.resolve(import.meta.dirname,'./src/hooks'),
      '@services': path.resolve(import.meta.dirname,'./src/services'),
      '@types': path.resolve(import.meta.dirname,'./src/types'),
      '@styles': path.resolve(import.meta.dirname,'./src/styles')
    }
  },
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'https://localhost:7231',
        changeOrigin: true,
        secure: false,
      }
    }
  },
  // R-07 / ADR-018. Lives here rather than in a vitest.config.ts so tests always see the
  // same aliases and plugins as the build — one file cannot drift from itself.
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
  },
})

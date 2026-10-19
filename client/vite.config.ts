import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  build: {
    rollupOptions: {
      onwarn(warning, warn) {
        const warningId = warning.id?.replaceAll('\\', '/')
        const isZodAnnotationWarning =
          warning.code === 'INVALID_ANNOTATION' &&
          warningId?.includes('/node_modules/zod/v4/core/')

        if (!isZodAnnotationWarning) warn(warning)
      },
    },
  },
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5288',
        changeOrigin: true,
      },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/setupTests.ts'],
    include: ['src/**/*.test.{ts,tsx}'],
  },
})

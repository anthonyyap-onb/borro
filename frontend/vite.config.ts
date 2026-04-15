import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0',   // Expose to Docker network
    port: 5173,
    watch: {
      usePolling: true, // Required for file change detection inside Docker volumes
    },
  },
})

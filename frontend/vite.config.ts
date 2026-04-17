import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// The backend URL used by the Vite dev-server proxy (Node.js side, not the browser).
// In Docker: http://backend:8080 (internal service name).
// Locally:   http://localhost:8180
const backendProxyUrl = process.env.BACKEND_PROXY_URL ?? 'http://localhost:8180';

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0',   // Expose to Docker network
    port: 5173,
    watch: {
      usePolling: true, // Required for file change detection inside Docker volumes
    },
    allowedHosts: ['.localhost', '.ngrok-free.app'],
    proxy: {
      // Proxy all /api and /hubs requests server-side so the browser only ever
      // calls back the same origin — no loopback / CORS issues over ngrok.
      '/api': {
        target: backendProxyUrl,
        changeOrigin: true,
      },
      '/hubs': {
        target: backendProxyUrl,
        changeOrigin: true,
        ws: true, // proxy WebSocket upgrades for SignalR
      },
    },
  },
})

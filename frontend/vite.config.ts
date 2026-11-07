import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// El Gateway (backend/gateway) NO tiene CORS configurado. Para evitar peticiones
// cross-origin en desarrollo (SPA en :5173 -> Gateway en :5000), el dev server de
// Vite hace de proxy: el navegador siempre ve el mismo origen (/api) y Vite reenvia
// al Gateway. No se toca el backend.
// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
})

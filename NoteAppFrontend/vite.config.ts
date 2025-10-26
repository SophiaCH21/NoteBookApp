import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    host: true,
    port: 5173,
    strictPort: true,
    allowedHosts: true,
    cors: true,
    proxy: {
      // ВСЕ запросы /api → локальный backend
      '/api': {
        target: 'http://localhost:5120',
        changeOrigin: true,
        secure: false,
        configure: (proxy, _options) => {
          proxy.on('error', (err) => console.log('proxy error', err));
          proxy.on('proxyReq', (proxyReq, req) =>
            console.log('→', req.method, req.url)
          );
          proxy.on('proxyRes', (proxyRes, req) =>
            console.log('←', proxyRes.statusCode, req.url)
          );
        },
      },
    },
  },
  base: '/',
})
/*[
      'localhost',
      '*.ngrok-free.app',
      '*.localtunnel.me',
      '*.lhrtunnel.run',
      '*.lhr.life',
    ],*/

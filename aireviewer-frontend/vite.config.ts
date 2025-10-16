import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const port = Number(env.VITE_PORT || 5173)
  const host = env.VITE_HOST || '0.0.0.0' // 允许外部访问

  return {
    plugins: [react()],
    server: {
      host, // '0.0.0.0' 使服务器监听所有网卡地址
      port,
      strictPort: false,
      // 在某些公司网络下需要明确 HMR host，可通过 .env 覆盖
      hmr: env.VITE_HMR_HOST
        ? { host: env.VITE_HMR_HOST, port: Number(env.VITE_HMR_PORT || port) }
        : undefined,
    },
    preview: {
      host,
      port: Number(env.VITE_PREVIEW_PORT || 4173),
    },
  }
})

import path from 'path';
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

function readEnv(name: string): string | undefined {
  const value = process.env[name]?.trim();
  return value || undefined;
}

const basePath = readEnv('VITE_BASE_PATH') ?? './';
const configuredAllowedHosts = readEnv('VITE_ALLOWED_HOSTS')
  ?.split(',')
  .map((host) => host.trim())
  .filter(Boolean);

function resolveHmrOptions() {
  const clientPort = Number.parseInt(readEnv('VITE_HMR_CLIENT_PORT') ?? '', 10);
  const configuredProtocol = readEnv('VITE_HMR_PROTOCOL');
  const protocol = configuredProtocol === 'ws' || configuredProtocol === 'wss'
    ? configuredProtocol
    : undefined;

  if (!Number.isNaN(clientPort) && clientPort > 0) {
    return { clientPort, ...(protocol ? { protocol } : {}) };
  }

  if (basePath !== './') {
    return { clientPort: 443, ...(protocol ? { protocol } : {}) };
  }

  return protocol ? { protocol } : undefined;
}

export default defineConfig({
  // Ignite supplies only non-secret runtime settings. Do not load uploaded .env files
  // or expose API keys in the browser bundle.
  envDir: false,
  envPrefix: 'APP_PUBLIC_',
  base: basePath,
  clearScreen: true,
  server: {
    port: 18100,
    host: '0.0.0.0',
    strictPort: true,
    hmr: resolveHmrOptions(),
    ...(configuredAllowedHosts && configuredAllowedHosts.length > 0
      ? { allowedHosts: configuredAllowedHosts }
      : {}),
  },
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
});

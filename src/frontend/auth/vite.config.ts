import { defineConfig, type Plugin } from "vite";
import vue from "@vitejs/plugin-vue";

// ---------------------------------------------------------------------------
// Hosted-preview bridge (Ignite / Coder) — DEV SERVER ONLY.
//
// A hosted workspace runs this dev server inside a container and shows it in an iframe
// behind a PATH-BASED proxy, e.g.
//     /<prefix>/@<owner>/<workspace>/apps/preview-auth/
// That proxy forwards the FULL mount path to Vite unchanged, so `base` must equal the
// mount path or every module, asset and the HMR socket 404s. The values come in as
// environment variables on the Vite process; they are never read at build time.
//
// This block is duplicated (not imported) in ../main/vite.config.ts, minus the app
// specific bits: main and auth are separate Vite projects with separate dependency
// graphs, and a cross-project import would break `vite build`.
//
// WITH NO SUCH ENVIRONMENT PRESENT every value below is undefined: `base` stays "./",
// allowedHosts/hmr stay at Vite's defaults, the plugin injects nothing, and both
// `pnpm dev` and the production build behave exactly as they did before.
// ---------------------------------------------------------------------------

/** Read an environment variable, treating blank/whitespace-only values as unset. */
function readEnv(name: string): string | undefined {
  const value = process.env[name];
  return value && value.trim() ? value.trim() : undefined;
}

/**
 * Mount path the dev server is served from.
 *
 * The default stays "./" on purpose: the production bundle is served under an nginx path
 * prefix and relies on relative asset URLs, so it must NOT be turned into an absolute base.
 */
const basePath = readEnv("VITE_BASE_PATH") ?? "./";

/**
 * Vite answers 403 "This host is not allowed" for unknown Host headers, and the preview
 * proxy forwards its own public hostname. Unset => keep Vite's default (do not lock down
 * local development).
 */
const configuredAllowedHosts = readEnv("VITE_ALLOWED_HOSTS")
  ?.split(",")
  .map((host) => host.trim())
  .filter(Boolean);

/**
 * HMR behind the preview proxy.
 *
 * The HMR client builds its socket URL as `${hostname}:${__HMR_PORT__ || location.port}`,
 * and on the HTTPS edge `location.port` is empty — which yields a malformed "host:"
 * authority and a socket that never connects. So when we know we are proxied
 * (VITE_BASE_PATH is set) we pin the client port to 443, where TLS terminates, unless an
 * explicit VITE_HMR_CLIENT_PORT says otherwise. The protocol is left to the client, which
 * correctly infers wss on an https page, unless VITE_HMR_PROTOCOL overrides it.
 *
 * Returns undefined for plain local `pnpm dev`, leaving HMR entirely at its default.
 */
function resolveHmrOptions():
  { clientPort?: number; protocol?: string } | undefined {
  const explicitPort = Number(readEnv("VITE_HMR_CLIENT_PORT"));
  const protocol = readEnv("VITE_HMR_PROTOCOL");

  let clientPort: number | undefined;
  if (Number.isInteger(explicitPort) && explicitPort > 0) {
    clientPort = explicitPort;
  } else if (readEnv("VITE_BASE_PATH")) {
    clientPort = 443;
  }

  if (clientPort === undefined && !protocol) {
    return undefined;
  }

  return {
    ...(clientPort === undefined ? {} : { clientPort }),
    ...(protocol ? { protocol } : {}),
  };
}

const hmr = resolveHmrOptions();

/**
 * Hosted-preview environment variable -> `AppTemplateRuntimeConfig` key.
 * (The main app maps VITE_AUTH_SERVICE_URL -> authAppUrl here instead.)
 */
const RUNTIME_CONFIG_FROM_ENV: Record<string, string> = {
  VITE_API_URL: "mainApiBaseUrl",
  VITE_AUTH_API_URL: "authApiBaseUrl",
  VITE_BASE_PATH: "appBasePath",
  VITE_COOKIE_DOMAIN: "cookieDomain",
  VITE_COOKIE_SESSION_KEY: "sessionCookieName",
  VITE_COOKIE_USER_KEY: "userCookieName",
  VITE_DASHBOARD_URL: "mainAppUrl",
};

/** Collect the runtime config, or undefined when none of the variables are set. */
function collectRuntimeConfig(): Record<string, string> | undefined {
  const config: Record<string, string> = {};

  for (const [envName, configKey] of Object.entries(RUNTIME_CONFIG_FROM_ENV)) {
    // readEnv() drops empty values, so an empty VITE_COOKIE_DOMAIN stays absent rather
    // than becoming an empty cookie domain attribute.
    const value = readEnv(envName);
    if (value) {
      config[configKey] = value;
    }
  }

  return Object.keys(config).length > 0 ? config : undefined;
}

/** JSON that is safe to embed between <script> tags. */
function serializeRuntimeConfig(config: Record<string, string>): string {
  return JSON.stringify(config)
    .replace(/</g, "\\u003c")
    .replace(/>/g, "\\u003e");
}

/**
 * Hand the hosted-preview URLs and cookie names to the app through the template's OWN
 * runtime-config channel (`window.__APP_TEMPLATE_CONFIG__`) — the same channel nginx and
 * Helm use in production.
 *
 * This is deliberately NOT `import.meta.env.VITE_*`: these are per-deployment APPLICATION
 * settings, and baking them into the bundle would break build-once-promote-many. Here the
 * dev server writes them into the HTML it serves per request; nothing reaches the bundle,
 * and `apply: "serve"` keeps the plugin out of `vite build` entirely.
 */
function hostedPreviewRuntimeConfigPlugin(): Plugin {
  return {
    name: "app-template:hosted-preview-runtime-config",
    apply: "serve",
    transformIndexHtml: {
      order: "pre",
      handler(html) {
        const config = collectRuntimeConfig();
        if (!config) {
          return html;
        }

        return {
          html,
          tags: [
            {
              tag: "script",
              // Before the app's module script, so the config exists on first read.
              injectTo: "head-prepend",
              children: `window.__APP_TEMPLATE_CONFIG__ = ${serializeRuntimeConfig(config)};`,
            },
          ],
        };
      },
    },
  };
}

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue(), hostedPreviewRuntimeConfigPlugin()],
  base: basePath,
  server: {
    port: 8001,
    strictPort: true,
    host: true,
    // Spread, so that with no configuration Vite keeps its own default allowlist rather
    // than receiving an empty array (which would reject everything).
    ...(configuredAllowedHosts && configuredAllowedHosts.length > 0
      ? { allowedHosts: configuredAllowedHosts }
      : {}),
    ...(hmr ? { hmr } : {}),
    // LOCAL DEV ONLY. Under a hosted preview the app calls the absolute mount paths it
    // receives via window.__APP_TEMPLATE_CONFIG__ (…/apps/auth-api/…), which the preview
    // proxy routes to the APIs itself — so this block is never exercised there.
    proxy: {
      "/api-auth/api": {
        target: "http://localhost:5001",
        changeOrigin: true,
        secure: false,
        rewrite: (path) => path.replace(/^\/api-auth/, ""),
      },
      "/api-main": {
        target: "http://localhost:5002",
        changeOrigin: true,
        secure: false,
        rewrite: (path) => path.replace(/^\/api-main/, ""),
      },
    },
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks(id) {
          const normalizedId = id.replace(/\\/g, "/");
          if (!normalizedId.includes("node_modules")) return;
          if (normalizedId.includes("/@sentry/")) return "sentry";
          if (normalizedId.includes("/@opentelemetry/")) return "otel";
          if (normalizedId.includes("/vue")) return "vue";
          return "vendor";
        },
      },
    },
  },
});

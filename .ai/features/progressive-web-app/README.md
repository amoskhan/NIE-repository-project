# Progressive Web App

## Overview

Adds a web app manifest and a service worker so the main Vue app can be installed as a standalone app and keeps working offline for cached assets. Always included in the scaffold (not optional).

Installation is handled **natively by the browser** from the manifest. The template ships **no custom install-prompt component** and no `beforeinstallprompt` handling — if you want an in-page "Install" banner, that is yours to add.

## Key Files

| Layer                    | Path                                     | What it actually is                                                                                                                                 |
| ------------------------ | ---------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| Manifest                 | `src/frontend/main/public/manifest.json` | Static JSON: name, `short_name`, `start_url`, `display: standalone`, colours, one SVG icon.                                                         |
| Service worker           | `src/frontend/main/public/sw.js`         | Plain JS, served verbatim from `public/` — **not** compiled by Vite. Cache name `apptemplate-v1`.                                                   |
| Registration             | `src/frontend/main/src/main.ts`          | Registers `sw.js` at boot via `navigator.serviceWorker.register(getFrontendAssetUrl("sw.js"))`; failure is swallowed so the app still works online. |
| Manifest link + iOS meta | `src/frontend/main/index.html`           | `<link rel="manifest">`, `theme-color`, and the `apple-mobile-web-app-*` tags.                                                                      |
| Build                    | `src/frontend/main/vite.config.ts`       | `base: "./"` and normal `public/` copying. There is no PWA plugin and no service-worker bundling step.                                              |

There is no `src/service-worker.ts`, no `composables/useServiceWorker.ts`, and no `components/InstallPromptBanner.vue`. Earlier revisions of this dossier listed all three; they do not exist and never shipped — the manifest has always listed only `manifest.json`, `sw.js`, and `vite.config.ts`.

## Behaviour

- **Web app manifest** — app name, icon, theme colours, `display: standalone`.
- **Service worker** — on `install`, precaches `/`, `/app-logo.svg`, `/manifest.json`; on `activate`, deletes every cache whose name is not `apptemplate-v1`, then claims clients.
- **Fetch strategy** — non-GET and cross-origin requests are ignored; URLs containing `/api/` are **network-first** with a cache fallback; everything else is **cache-first** with a background refresh (stale-while-revalidate).
- **Install** — the browser's own install affordance (address-bar icon, "Add to Home Screen"), driven entirely by the manifest.

## Configuration

1. Edit `public/manifest.json` for branding — name, `short_name`, icons, `background_color`, `theme_color`. Keep `index.html`'s `theme-color` and `apple-mobile-web-app-title` in step with it.
2. Adjust the caching rules in `public/sw.js` if the defaults do not suit you. **Bump `CACHE_NAME`** (`apptemplate-v1` → `apptemplate-v2`) whenever you change what is cached, or returning users keep the old bundle until the cache is evicted by hand.
3. `sw.js` is not transpiled — write browser-ready JavaScript in it, with no TypeScript and no imports from `src/`.

## Testing

1. Serve over HTTPS, or use the dev server on `http://localhost:8002` (service workers are allowed on `localhost`).
2. DevTools → Application → **Manifest**: name, icon, and colours resolve with no errors.
3. DevTools → Application → **Service Workers**: `sw.js` is activated and running.
4. DevTools → Application → **Cache Storage**: an `apptemplate-v1` cache exists with the precached entries.
5. Tick **Offline** in the Network panel and reload — the cached shell still renders.
6. On a supported browser, the install affordance appears in the address bar.

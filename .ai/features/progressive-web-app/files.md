# Progressive Web App - File Map

## Owned files

| Path                                     | Layer          | Purpose                                                                                                                                                                                                                                |
| ---------------------------------------- | -------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/frontend/main/public/manifest.json` | PWA metadata   | App name, `short_name`, `start_url`, `display: standalone`, theme/background colours, one SVG icon.                                                                                                                                    |
| `src/frontend/main/public/sw.js`         | Service worker | The whole offline/cache implementation: precache on `install`, stale-cache cleanup on `activate`, network-first for `/api/` and cache-first for everything else on `fetch`. Plain JS, copied verbatim out of `public/` — not compiled. |
| `src/frontend/main/vite.config.ts`       | Build          | `base: "./"` plus standard `public/` asset copying. No PWA plugin, no service-worker bundling.                                                                                                                                         |

## Touched files

| Path                            | Why                                                                                                                                                                         |
| ------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/frontend/main/src/main.ts` | Registers `public/sw.js` at boot with `navigator.serviceWorker.register(getFrontendAssetUrl("sw.js"))`; a rejected registration is swallowed so the app still works online. |
| `src/frontend/main/index.html`  | Carries `<link rel="manifest" href="/manifest.json">`, `theme-color`, and the `apple-mobile-web-app-*` meta tags the manifest cannot express.                               |

## Notes

- The template ships **no** `src/service-worker.ts`, `src/composables/useServiceWorker.ts`, or `src/components/InstallPromptBanner.vue`. Do not go looking for them — install is left entirely to the browser's native affordance, and `manifest.yaml` lists only the three owned files above.
- `App.vue` hosts no PWA UI. `StaffLayout.vue` is template-owned shell infrastructure — do not edit it.
- Bump `CACHE_NAME` in `sw.js` whenever the precache list or caching strategy changes, otherwise returning users keep serving the previous bundle.

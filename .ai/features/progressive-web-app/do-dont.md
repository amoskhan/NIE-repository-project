# Progressive Web App - Do and Don't

## DO

1. DO serve the app over HTTPS, or localhost for development, when testing install behavior.
2. DO version `CACHE_NAME` in `public/sw.js` when changing the precache list or cache strategy — the `activate` handler only evicts caches whose name differs from the current one.
3. DO provide a clear update path when a new service worker is waiting.
4. DO keep icons and theme colors aligned with project branding.
5. DO test offline behavior in browser DevTools after each service-worker change.

## DON'T

1. DON'T cache authenticated API responses unless the data is explicitly safe offline.
2. DON'T register multiple service workers for the same app shell.
3. DON'T make an install prompt block primary workflows. The template ships none — installation is the browser's own affordance — so if you add a banner, keep it dismissible and out of the way.
4. DON'T assume iOS, Android, and desktop browsers expose the same install events.
5. DON'T leave stale cache entries after changing routing or asset filenames.

# NIE Template main application

The primary user-facing Vue 3 application in the NIE application template.

## Included capabilities

- Procurement sample flows for catalog, purchase requests, approvals, orders, and vendors
- Administration for users, roles, access functions, audit records, and notifications
- Document, report, workflow, feedback, MyInfo, chatbot, and push-notification examples
- Session-based authentication through the Auth host
- Shared design-system and platform packages from the frontend workspace

## Technology

- Vue 3 with Composition API and `<script setup>`
- TypeScript and Vite
- Vue Router
- Tailwind CSS
- Axios
- vee-validate with Zod schemas

Minimum versions and selection rules are maintained in [`.ai/LIBRARIES.md`](../../../../.ai/LIBRARIES.md).

## Source layout

```text
src/
|-- app-config/      # Application-owned routes, navigation, branding, and access functions
|-- assets/          # Application assets
|-- components/      # Feature and reusable application components
|-- composables/     # Reusable application state and behavior
|-- pages/           # Feature pages outside the staff shell
|-- router/          # Vue Router composition
|-- services/        # Typed API clients
|-- staff/           # Staff layout and Procurement sample pages
|-- theme/           # Application theme adapter
|-- types/           # Application and Procurement types
`-- utils/           # Application utilities
```

Reusable UI belongs in [`packages/ui`](../../packages/ui/README.md), cross-application
runtime behavior belongs in [`packages/platform`](../../packages/platform/README.md),
and transport contracts belong in [`packages/contracts`](../../packages/contracts/README.md).

## Procurement sample

Use the Procurement implementation as a reference for domain development:

- [`staff/pages/procurement/`](src/staff/pages/procurement/) contains the sample screens.
- [`services/procurement/purchaseOrderService.ts`](src/services/procurement/purchaseOrderService.ts),
  [`services/procurement/vendorService.ts`](src/services/procurement/vendorService.ts), and
  [`services/procurement/catalogItemService.ts`](src/services/procurement/catalogItemService.ts) show typed API boundaries.
- [`types/procurement.ts`](src/types/procurement.ts) defines the application-facing domain models.
- [`app-config/`](src/app-config/) demonstrates project-owned navigation, routes, branding, and access declarations.

Keep platform capabilities intact when replacing Procurement with another domain. Follow the
feature dossiers in [`.ai/`](../../../../.ai/README.md) for required backend, frontend, data,
security, testing, and operational behavior.

## Commands

Run these from `src/frontend`:

```bash
pnpm dev:main
pnpm --filter @nie/main build:staging
pnpm build:main
pnpm type-check
```

## Runtime URLs

API routes are derived from the application base path:

```text
https://domain.example/MYAPP/        -> https://domain.example/MYAPP/api-main
https://domain.example/MYAPP/login/  -> https://domain.example/MYAPP/api-auth
```

Shared constants live in `packages/platform/src/config/constants.ts`. During local
development, Vite proxies `/api-main` to `http://localhost:5002` and
`/api-auth/api` to `http://localhost:5001`.

Optional runtime integrations such as Sentry, OneSignal, and OpenTelemetry can be
supplied through `window.__NIE_APPLICATION_CONFIG__` or matching `nie:*` meta tags
without rebuilding the frontend.

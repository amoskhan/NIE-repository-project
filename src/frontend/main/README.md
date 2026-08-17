# App Template - Main Application

The main frontend application for the App Template project. Built with Vue 3, TypeScript, and Vite.

## Overview

This is the primary user-facing application that provides:

- **CRUD Operations**: Full create, read, update, delete functionality
- **Admin Panel**: User role management, audit logs, and system configuration
- **Sample Model Views**: Template screens demonstrating common patterns
- **Document Management**: File upload and attachment handling
- **Session-based Authentication**: Integrated with the Auth service

## Tech Stack

- **Vue 3** with Composition API and `<script setup>` syntax
- **TypeScript** for type safety
- **Vite** for fast development and building
- **Tailwind CSS** for styling
- **Vue Router** for navigation
- **Axios** for API communication

## Project Structure

```
src/
├── assets/          # Static assets (images, fonts)
├── components/      # Reusable Vue components
│   ├── admin/       # Admin-specific components
│   └── common/      # Shared common components
├── composables/     # Vue composables (reusable logic)
├── router/          # Vue Router configuration
├── services/        # API service modules
├── types/           # TypeScript type definitions
├── utils/           # Utility functions
└── views/           # Page-level components
    ├── admin/       # Admin pages
    └── *.vue        # Main views (SampleModel screens)
```

## Key Files to Modify for New Projects

### Replace Sample Model

1. **[services/sampleModelService.ts](src/services/sampleModelService.ts)** - Replace with your entity service
2. **[views/SampleModelList.vue](src/views/SampleModelList.vue)** - Replace with your list view
3. **[views/SampleModelForm.vue](src/views/SampleModelForm.vue)** - Replace with your form view
4. **[views/SampleModelCrud.vue](src/views/SampleModelCrud.vue)** - Replace with your CRUD container

### Keep as Reference

- `services/api.ts` - Base API configuration (keep and modify)
- `services/authService.ts` - Authentication helpers (keep)
- `services/codeService.ts` - Code lookup service (keep if using code tables)
- `services/documentService.ts` - Document handling (keep if using file uploads)
- `components/admin/` - Admin components (keep)

## Scripts

```bash
# Development server
pnpm dev

# Build for staging
pnpm build:staging

# Build for production
pnpm build:production

# Type checking
pnpm type-check
```

## Runtime URLs

The frontend no longer requires per-environment `.env` files for API URLs.
API routes are derived from the current app base path:

```text
https://domain.example/MYAPP/        -> https://domain.example/MYAPP/api-main
https://domain.example/MYAPP/login/  -> https://domain.example/MYAPP/api-auth
```

Shared constants live in `packages/shared/src/config/constants.ts`. For local
development, Vite proxies `/api-main` to `http://localhost:5002` and
`/api-auth/api` to `http://localhost:5001`.

Optional runtime-only integrations such as Sentry DSN, OneSignal app ID, and
OpenTelemetry exporter endpoint can be supplied with `window.__APP_TEMPLATE_CONFIG__`
or matching `app:*` meta tags without rebuilding the frontend.

## Learn More

- [Vue 3 Documentation](https://vuejs.org/)
- [Vue 3 Script Setup](https://v3.vuejs.org/api/sfc-script-setup.html)
- [TypeScript Guide for Vue](https://vuejs.org/guide/typescript/overview.html)
- [Vite Documentation](https://vitejs.dev/)

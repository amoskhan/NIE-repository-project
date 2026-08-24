# NIE Template - Auth Application

The authentication frontend for the NIE Template project. A lightweight Vue 3 application handling user login and session management.

## Overview

This is a dedicated authentication application that provides:

- **Login Page**: User authentication interface
- **Portal SSO Entry**: One-click handoff from the portal into the same session flow
- **Session Management**: Cookie-based session handling
- **Redirect Flow**: Seamless redirect to main application after login

## Tech Stack

- **Vue 3** with Composition API
- **TypeScript** for type safety
- **Vite** for fast development and building
- **Tailwind CSS** for styling

## Project Structure

```
src/
├── assets/       # Static assets
├── components/   # Vue components (LoginForm, etc.)
└── main.ts       # Application entry point
```

## Why Separate Auth App?

The authentication application is intentionally separate to:

1. **Security Isolation**: Keep auth logic separate from main application
2. **Independent Deployment**: Deploy auth changes without affecting main app
3. **Simplified Session Flow**: Clear separation of concerns
4. **Multi-App Support**: Single auth service for multiple applications

## For New Projects

**Keep this application largely unchanged.** You may want to:

- Update branding/styling to match your project
- Modify the login form layout if needed
- Update shared runtime constants for project-specific integrations

## Scripts

```bash
# Development server
pnpm dev

# Build for staging
pnpm build:staging

# Build for production
pnpm build:production
```

## Runtime URLs

The auth frontend uses `packages/platform/src/config/constants.ts` for cookie
names, frontend redirects, and API paths. It does not require `.env` files for
environment-specific builds.

When deployed under an app base path, the URLs resolve automatically:

```text
https://domain.example/MYAPP/login/ -> https://domain.example/MYAPP/api-auth
```

For local development, Vite proxies `/api-auth/api` to `http://localhost:5001`
and redirects the signed-in user to the main frontend on `http://localhost:8002/`.

## Default Ports

- Auth Frontend: `http://localhost:8001/login/`
- Auth API: `http://localhost:5001`

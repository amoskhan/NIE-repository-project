# NIE Template Playwright tests

This workspace contains API and browser end-to-end tests for the Auth host, Main API, Auth Vue app,
and Main Vue app.

## Setup

```bash
corepack pnpm@10.33.0 --dir tests install
corepack pnpm@10.33.0 --dir tests install-browsers
```

Run the backend hosts on ports 5001 and 5002 and the Vue apps on ports 8001 and 8002 before executing
the live suites.

## Commands

```bash
corepack pnpm@10.33.0 --dir tests test
corepack pnpm@10.33.0 --dir tests test:api
corepack pnpm@10.33.0 --dir tests test:e2e
corepack pnpm@10.33.0 --dir tests test:smoke
corepack pnpm@10.33.0 --dir tests test:ui
```

## Structure

```text
tests/
|-- playwright.config.ts
|-- specs/
|   |-- api/           # Main API and infrastructure contract tests
|   |-- auth/          # Auth API tests
|   |-- e2e/           # Browser tests for Auth, shell, and feature behavior
|   `-- fixtures/      # API client, authentication, configuration, and test-user helpers
`-- .env.dev           # Committed non-secret local defaults
```

## Configuration

The default local endpoints are:

```text
AUTH_API_URL=http://localhost:5001/api/
MAIN_API_URL=http://localhost:5002/api/
FRONTEND_AUTH=http://localhost:8001/
FRONTEND_MAIN=http://localhost:8002/
```

Keep credentials and machine-specific overrides in the ignored `.env.dev.local` file. The committed
configuration intentionally leaves test-user credentials empty, so credential-dependent tests skip
unless a developer supplies them.

## Authoring rules

- Use the shared fixtures instead of duplicating cookie names, base URLs, or API-client behavior.
- Give mocked users every screen and API access function required by the route under test.
- Mock the server-side table `Search` and `GetFilterOptions` contracts rather than returning an
  unpaged array to a paged consumer.
- Keep Procurement scenarios as the domain reference until a derived application intentionally
  replaces that sample.
- Assert stable roles, accessible names, URLs, API contracts, and visible outcomes rather than
  implementation-only CSS selectors.

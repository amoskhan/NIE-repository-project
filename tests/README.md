# App Template E2E Tests

This directory contains end-to-end and API tests for the App Template application using Playwright.

## Setup

1. Install dependencies:

```bash
pnpm install
```

2. Install Playwright browsers:

```bash
pnpm run install-browsers
```

## Running Tests

### Basic test execution:

```bash
pnpm test                 # Run all tests headless
pnpm run test:headed      # Run tests with browser UI
pnpm run test:debug       # Run tests in debug mode
pnpm run test:ui          # Open Playwright UI for interactive testing
```

### Run specific test types:

```bash
pnpm run test:api         # Run all API tests
pnpm run test:api:auth    # Run auth API tests only
pnpm run test:api:main    # Run main API tests only
pnpm run test:e2e         # Run all E2E tests
pnpm run test:e2e:auth    # Run auth E2E tests only
pnpm run test:e2e:dashboard # Run dashboard E2E tests only
```

### Development mode (local environment):

```bash
pnpm run dev:api          # Run API tests against local dev
pnpm run dev:e2e          # Run E2E tests against local dev
pnpm run dev:smoke        # Run smoke tests against local dev
```

## Test Structure

```
tests/
├── playwright.config.ts      # Playwright configuration
├── package.json              # Test dependencies
├── .env.dev                  # Development environment variables
├── specs/
│   ├── auth/                 # Auth API tests
│   │   └── login.api.spec.ts
│   ├── api/                  # API-level tests
│   │   ├── auth-local-identity.api.spec.ts   # Local identity provider lifecycle
│   │   └── nginx-maintenance.spec.ts
│   ├── e2e/                  # Browser E2E tests
│   │   ├── auth.e2e.spec.ts
│   │   └── dashboard.e2e.spec.ts
│   └── fixtures/             # Shared test utilities
│       ├── test-config.ts    # Environment config + canonical cookie names
│       ├── api-client.ts     # HTTP client for API tests
│       ├── auth.fixture.ts   # Authentication helpers
│       └── test-users.ts     # Test user management
```

> These are integration and end-to-end tests: they need the stack running. Fast
> unit tests live next to the code they cover in `src/frontend` — run them with
> `pnpm test:unit` from that folder.

## Environment Configuration

Use `.env.dev` for shared non-secret defaults and `.env.dev.local` for local-only credentials or overrides:

```env
# API URLs (must end with /)
AUTH_API_URL=http://localhost:5001/api/
MAIN_API_URL=http://localhost:5002/api/

# Frontend URLs (must end with /)
# auth SPA = 8001, main SPA = 8002 (see each app's vite.config.ts)
FRONTEND_AUTH=http://localhost:8001/
FRONTEND_MAIN=http://localhost:8002/

# Test credentials
TEST_USERNAME=
TEST_PASSWORD=

# Local identity provider - seeded development account
SEED_ADMIN_USERNAME=admin
SEED_ADMIN_PASSWORD=Admin@12345
AUTH_LOCKOUT_THRESHOLD=5
```

The committed template intentionally leaves `TEST_USERNAME` and `TEST_PASSWORD` blank. Keep real credentials in `.env.dev.local` so they stay out of source control.

## Writing New Tests

### API Test Example

```typescript
import { test, expect } from "@playwright/test";
import { createApiClient, ApiClient } from "../fixtures/api-client";
import { TestConfig, ApiEndpoints } from "../fixtures/test-config";

test.describe("YourEntity API Tests", () => {
  let client: ApiClient;

  test.beforeAll(async () => {
    client = createApiClient();
    await client.init();
  });

  test.afterAll(async () => {
    await client.dispose();
  });

  test("should return entities", async () => {
    const response = await client.get(ApiEndpoints.yourEntity.getAll);
    expect(response.status).toBe(200);
  });
});
```

### E2E Test Example

```typescript
import { test, expect } from "@playwright/test";
import { TestConfig, Routes } from "../fixtures/test-config";
import { createTestSession } from "../fixtures/auth.fixture";

test.describe("E2E - Your Feature", () => {
  test.beforeEach(async ({ page, context }) => {
    const session = await createTestSession();

    await context.addCookies([
      {
        name: "SessionToken",
        value: session!.sessionToken,
        domain: "localhost",
        path: "/",
      },
    ]);

    await page.goto(`${TestConfig.frontendMain}/your-page`);
  });

  test("should display content", async ({ page }) => {
    await expect(page.locator("h1")).toBeVisible();
  });
});
```

## CI/CD Integration

The project uses GitHub Actions for CI/CD (see `.github/workflows/ci.yml` and `.github/workflows/ci-frontend.yml`). To add tests to the pipeline:

```bash
cd tests
pnpm install
pnpm run install-browsers
pnpm test
```

Reports are generated in the `reports/` directory.

## Troubleshooting

### Tests fail to connect to services

- Ensure all services are running (API, Auth, Frontend)
- Check the URLs in `.env.dev` match your local setup
- Verify ports 5001, 5002, 8001, 8002 are not blocked

### Browser not found

- Run `pnpm run install-browsers` to install Chromium

### Timeout errors

- Increase timeouts in `.env.dev` if services are slow
- Check network connectivity

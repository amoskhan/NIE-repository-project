# Guide: Creating Test Strategy Documentation

> **This is a GUIDE.** Each project creates its own `docs/test-strategy.md` with project-specific test plans. This document explains HOW to create it.

---

## Purpose

The test strategy document defines what to test, how to test it, and what coverage targets to meet. AI agents use this to generate Playwright test files and shape their testing approach.

## When to Create

- During Phase 1.2 (Technical Design) of AIDLC
- When adding major new features
- Before Phase 2.4 (Test) begins

## Format

Use Markdown tables for test matrices. Reference Playwright patterns from the NIE Template test suite.

## How to Create

### Step 1: Define Test Categories

```markdown
# Test Strategy

## Test Categories

| Category   | Tool       | Location              | Scope                              | CI Gate |
| ---------- | ---------- | --------------------- | ---------------------------------- | ------- |
| API Tests  | Playwright | tests/specs/api/      | Individual API endpoint validation | Yes     |
| Auth Tests | Playwright | tests/specs/auth/     | Authentication flows               | Yes     |
| E2E Tests  | Playwright | tests/specs/e2e/      | Full user journeys through UI      | Yes     |
| Unit Tests | xUnit      | src/backend/\*.Tests/ | Service-level business logic       | Yes     |
```

### Step 2: Create API Test Matrix

For each entity/controller, define the API tests needed:

```markdown
## API Test Matrix

### Entity: [EntityName]

| Test ID | Endpoint         | Method | Scenario                            | Expected Status      | Priority |
| ------- | ---------------- | ------ | ----------------------------------- | -------------------- | -------- |
| API-001 | /api/Entity      | GET    | List all (authenticated)            | 200 + array          | High     |
| API-002 | /api/Entity      | GET    | List all (unauthenticated)          | 401                  | High     |
| API-003 | /api/Entity/{id} | GET    | Get existing                        | 200 + object         | High     |
| API-004 | /api/Entity/{id} | GET    | Get non-existent                    | 404                  | Medium   |
| API-005 | /api/Entity      | POST   | Create with valid data              | 200 + created object | High     |
| API-006 | /api/Entity      | POST   | Create with missing required fields | 400                  | High     |
| API-007 | /api/Entity      | POST   | Create with duplicate name          | 400                  | Medium   |
| API-008 | /api/Entity/{id} | PUT    | Edit existing (owner)               | 200                  | High     |
| API-009 | /api/Entity/{id} | PUT    | Edit existing (non-owner)           | 403                  | Medium   |
| API-010 | /api/Entity/{id} | DELETE | Soft delete (admin)                 | 200                  | High     |
| API-011 | /api/Entity/{id} | DELETE | Delete (non-admin)                  | 403                  | Medium   |
```

### Step 3: Create E2E Test Scenarios

```markdown
## E2E Test Scenarios

### Feature: [Feature Name]

| Test ID | Scenario                 | Steps                                                                                    | Expected Result                           | Priority |
| ------- | ------------------------ | ---------------------------------------------------------------------------------------- | ----------------------------------------- | -------- |
| E2E-001 | User creates entity      | 1. Login<br>2. Navigate to entity list<br>3. Click "Create"<br>4. Fill form<br>5. Submit | Entity appears in list with success toast | High     |
| E2E-002 | User edits entity        | 1. Login<br>2. Navigate to entity<br>3. Click "Edit"<br>4. Modify fields<br>5. Save      | Updated data shown, success toast         | High     |
| E2E-003 | User searches entities   | 1. Login<br>2. Navigate to list<br>3. Enter search term<br>4. Click search               | Filtered results displayed                | Medium   |
| E2E-004 | Admin approves entity    | 1. Login as admin<br>2. Open pending entity<br>3. Click "Approve"                        | Status changes to Approved                | High     |
| E2E-005 | User cannot access admin | 1. Login as regular user<br>2. Navigate to /admin                                        | Redirected or 403 shown                   | High     |
```

### Step 4: Define Test Data Requirements

```markdown
## Test Data

### Test Users

| Username   | Role     | Purpose                  | Defined In                   |
| ---------- | -------- | ------------------------ | ---------------------------- |
| devia      | Admin    | Admin operations testing | tests/fixtures/test-users.ts |
| [testuser] | User     | Standard user testing    | tests/fixtures/test-users.ts |
| [reviewer] | Reviewer | Approval flow testing    | tests/fixtures/test-users.ts |

### Seed Data

| Entity          | Records      | Purpose                     |
| --------------- | ------------ | --------------------------- |
| Code (Statuses) | 5-7 per type | Dropdown values             |
| AppUser         | 3-5          | Different roles for testing |
| [Entity]        | 10+          | List/search/filter testing  |
```

### Step 5: Define Test File Structure

```markdown
## Test File Structure

\`\`\`
tests/
├── specs/
│ ├── api/
│ │ ├── entity-name.api.spec.ts # CRUD API tests
│ │ ├── entity-name-workflow.api.spec.ts # State transition tests
│ │ └── [feature].api.spec.ts
│ ├── auth/
│ │ └── login.api.spec.ts # Authentication tests
│ └── e2e/
│ ├── auth.e2e.spec.ts # Login/logout flows
│ ├── dashboard.e2e.spec.ts # Dashboard tests
│ ├── entity-name.e2e.spec.ts # Entity CRUD via UI
│ └── [feature].e2e.spec.ts
├── fixtures/
│ ├── api-client.ts # Authenticated API client
│ ├── auth.fixture.ts # Auth helper
│ ├── index.ts # Fixture exports
│ ├── test-config.ts # Base URLs, timeouts
│ └── test-users.ts # Test credentials
└── playwright.config.ts
\`\`\`
```

### Step 6: Document the Playwright Test Pattern

```markdown
## Test Template (API)

\`\`\`typescript
import { test, expect } from '../fixtures'; // Custom fixtures with auth

test.describe('[Entity] API', () => {
test('should list all entities', async ({ apiClient }) => {
const response = await apiClient.get('/api/Entity');
expect(response.ok()).toBeTruthy();

    const body = await response.json();
    expect(body.succeeded).toBe(true);
    expect(Array.isArray(body.data)).toBe(true);

});

test('should create entity with valid data', async ({ apiClient }) => {
const response = await apiClient.post('/api/Entity', {
data: {
name: 'Test Entity',
description: 'Created by automated test',
categoryId: '019fc37a-71b9-7858-86f2-9fea26d10e34',
},
});
expect(response.ok()).toBeTruthy();

    const body = await response.json();
    expect(body.succeeded).toBe(true);
    expect(body.data.name).toBe('Test Entity');

});

test('should return 400 for missing required fields', async ({ apiClient }) => {
const response = await apiClient.post('/api/Entity', {
data: { description: 'Missing name' },
});
expect(response.status()).toBe(400);
});
});
\`\`\`

## Test Template (E2E)

\`\`\`typescript
import { test, expect } from '../fixtures';

test.describe('[Feature] E2E', () => {
test.beforeEach(async ({ authenticatedPage }) => {
await authenticatedPage.goto('/entities');
});

test('should create entity via UI', async ({ authenticatedPage }) => {
await authenticatedPage.click('button:has-text("Create")');
await authenticatedPage.fill('[data-testid="name"]', 'Test Entity');
await authenticatedPage.fill('[data-testid="description"]', 'Test Description');
await authenticatedPage.click('button:has-text("Save")');

    await expect(authenticatedPage.locator('.toast-success')).toBeVisible();
    await expect(authenticatedPage.locator('text=Test Entity')).toBeVisible();

});
});
\`\`\`
```

### Step 7: Define Coverage Targets

```markdown
## Coverage Targets

| Category           | Target                      | Minimum | Notes                                              |
| ------------------ | --------------------------- | ------- | -------------------------------------------------- |
| API Tests          | 100% of endpoints           | 80%     | Every endpoint must have at least happy-path test  |
| Authentication     | 100%                        | 100%    | Login, logout, session expiry, invalid credentials |
| Authorization      | 100% of roles               | 90%     | Each role's access tested for each endpoint        |
| E2E Critical Paths | 100%                        | 100%    | Login, CRUD, approval workflows                    |
| E2E Edge Cases     | Best effort                 | 60%     | Error states, form validation                      |
| State Transitions  | 100% of valid transitions   | 100%    | Every arrow in state diagram tested                |
| State Guards       | 100% of invalid transitions | 90%     | Verify illegal transitions are blocked             |

## Run Commands

| Command                          | Purpose                  |
| -------------------------------- | ------------------------ |
| `npx playwright test`            | Run all tests            |
| `npx playwright test specs/api/` | Run API tests only       |
| `npx playwright test specs/e2e/` | Run E2E tests only       |
| `npx playwright test --headed`   | Run with browser visible |
| `npx playwright show-report`     | View HTML report         |
```

## Tips

1. **API tests first** — They're fast, reliable, and catch most bugs
2. **E2E tests for critical paths** — Login, CRUD, main workflows
3. **Use fixtures** — NIE Template provides authenticated API clients via fixtures
4. **Test MCP integration** — Use Playwright MCP for interactive test debugging
5. **Test negative cases** — 401s, 403s, 404s, 400s are as important as 200s
6. **Keep tests independent** — Each test should create its own data if needed

## Review Checklist

- [ ] API test matrix covers all endpoints
- [ ] E2E scenarios cover critical user journeys
- [ ] Test data requirements defined
- [ ] Test file structure matches conventions
- [ ] Coverage targets set
- [ ] Auth/authorization tests included
- [ ] State transition tests included (if applicable)
- [ ] Run commands documented

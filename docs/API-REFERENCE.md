# API Reference

How the App Template APIs are addressed and authenticated, plus the endpoints that ship with the template.

> **The authoritative list is the OpenAPI document,** not this file. Both services publish one:
>
> - Main API: `http://localhost:5002/openapi/v1.json`
> - Auth API: `http://localhost:5001/openapi/v1.json`
>
> Load it into Scalar, Swagger UI, Postman, Bruno, or your IDE's REST client. This document explains the conventions and the shape of the built-in surface; the JSON is always current. There is no bundled Swagger UI page — only the document.

## The two services

| Service  | Local URL               | Responsibility                                            |
| -------- | ----------------------- | --------------------------------------------------------- |
| Auth API | `http://localhost:5001` | Login, registration, password reset, session issue/verify |
| Main API | `http://localhost:5002` | Everything else — your business endpoints                 |

In a deployed environment both sit behind the reverse proxy under `/{app}/api-auth` and `/{app}/api-main`. Frontend code never hardcodes either; it imports `FRONTEND_CONSTANTS` from `@apptemplate/shared`.

## Routing convention

Controllers inherit `BaseController`, which carries:

```csharp
[Route("api/[controller]/[action]")]
```

So the URL is **`/api/{ControllerName}/{ActionName}`** — for example `GET /api/Vendor/GetAll`, `POST /api/Vendor/Save`. There is no `/v1/` path segment. An action with its own route template appends to that, so `[HttpPost("Delete/{id}")]` on `VendorController` is `POST /api/Vendor/Delete/5`.

The template does not use URL-based API versioning. If your project needs it later, add it deliberately rather than assuming it is already there.

## Authentication

Every Main API endpoint requires a valid session unless it is marked `[AllowAnonymous]`.

### Headers

| Header         | Required           | Description                                          |
| -------------- | ------------------ | ---------------------------------------------------- |
| `X-Session-Id` | Yes                | Session token issued by the Auth API on login        |
| `Content-Type` | For request bodies | `application/json`, or `multipart/form-data` uploads |

`SessionValidationMiddleware` skips validation for paths starting with `/openapi`, `/health`, `/favicon.ico`, and `/tickerq`.

### Authorization

Beyond having a session, most endpoints require a specific **access function** — a permission code granted to the caller's role. These are declared with `[RequireAccessFunction(AccessFunctionCodes.Api.Something)]` and seeded from `src/backend/Libraries/Shared/Security/AccessFunctionCatalog.cs`. A valid session with the wrong role gets `403`.

### Error responses

| Status | Meaning                                                          |
| ------ | ---------------------------------------------------------------- |
| `400`  | Validation failure or invalid input                              |
| `401`  | Missing, invalid, or expired session                             |
| `403`  | Valid session, but the caller lacks the required access function |
| `404`  | Resource not found                                               |
| `500`  | Unhandled exception (caught by `ExceptionHandlingMiddleware`)    |

See [`error-handling.md`](error-handling.md) for the response shapes and the controller patterns that produce them.

---

## Auth API

The Auth API owns the local identity provider: a users table, password hashes, self-service registration, and password reset. It is the only service that mints sessions.

| Endpoint                   | Method | Auth      | Purpose                                                  |
| -------------------------- | ------ | --------- | -------------------------------------------------------- |
| `/api/Auth/Login`          | POST   | Anonymous | Verify credentials, issue a session token                |
| `/api/Auth/Logout`         | POST   | Session   | Delete the session from Valkey — revocation is immediate |
| `/api/Auth/Verify`         | GET    | Session   | Check whether the presented session is still valid       |
| `/api/Auth/GetProfile`     | POST   | Session   | Display fields for the session's user                    |
| `/health`, `/health/ready` | GET    | Anonymous | Liveness and readiness                                   |

Registration and password-reset endpoints live on the same controller. Read their exact names and payloads from `http://localhost:5001/openapi/v1.json` or from `src/backend/Auth/Controllers/AuthController.cs` rather than from a table that can go stale.

### POST /api/Auth/Login

**Request**

```json
{
  "userid": "alice",
  "pd": "..."
}
```

**Response (200)** — an identity payload including the session token. The frontend stores the token and sends it as `X-Session-Id` on every subsequent Main API call.

**Response (401)** — a single generic failure. The API deliberately does not distinguish "no such user" from "wrong password".

Roles and access functions are **not** in this response. The frontend fetches them separately from `GET /api/AccessControl/GetCurrentAccessProfile` on the Main API.

---

## Main API

### Health

`GET /health` and `GET /health/ready` — anonymous. Readiness runs real checks against PostgreSQL and Valkey.

```json
{
  "status": "Healthy",
  "entries": {
    "postgresql": { "status": "Healthy", "duration": "00:00:00.0234567" },
    "valkey": { "status": "Healthy", "duration": "00:00:00.0012345" }
  }
}
```

### Code (lookup tables)

Reference data used to populate dropdowns. Access function: `api.code.read`.

| Endpoint                     | Method | Parameters                      |
| ---------------------------- | ------ | ------------------------------- |
| `/api/Code/GetAll`           | GET    | —                               |
| `/api/Code/GetAllByCodeType` | GET    | `codeType` (query, `ECodeType`) |

```json
[
  {
    "id": "1",
    "displayName": "Supplies",
    "description": "General office supplies",
    "displayOrder": 10,
    "isActive": true
  }
]
```

### Document (files)

| Endpoint                          | Method | Access function         | Notes                                                        |
| --------------------------------- | ------ | ----------------------- | ------------------------------------------------------------ |
| `/api/Document/DownloadFile/{id}` | GET    | `api.document.download` | Returns a file stream                                        |
| `/api/Document/UploadFile`        | POST   | `api.document.manage`   | `multipart/form-data`, field `file`; returns the stored path |
| `/api/Document/DeleteFile/{id}`   | DELETE | `api.document.manage`   | Removes the blob and the record                              |

Storage is behind `IFileStorageService`, so the same endpoints work against local disk or an S3-compatible bucket depending on `FileStorage:Provider`.

### AccessControl

| Endpoint                                     | Method | Access function                         |
| -------------------------------------------- | ------ | --------------------------------------- |
| `/api/AccessControl/GetCurrentAccessProfile` | GET    | `api.access-control.profile.read`       |
| `/api/AccessControl/GetOverview`             | GET    | `api.access-control.read`               |
| `/api/AccessControl/CreateRole`              | POST   | `api.access-control.roles.manage`       |
| `/api/AccessControl/UpdateRole`              | POST   | `api.access-control.roles.manage`       |
| `/api/AccessControl/DeleteRole/{id}`         | DELETE | `api.access-control.roles.manage`       |
| `/api/AccessControl/AssignRole`              | POST   | `api.access-control.assignments.manage` |
| `/api/AccessControl/RemoveAssignment/{id}`   | DELETE | `api.access-control.assignments.manage` |

`GetCurrentAccessProfile` is what the frontend calls right after login to learn which access functions the user holds; the sidebar and route guards are driven from its response.

### AuditLog

Access function: `api.audit-log.read`.

| Endpoint                            | Method | Parameters                                  |
| ----------------------------------- | ------ | ------------------------------------------- |
| `/api/AuditLog/GetAuditLogs`        | GET    | Filter DTO from the query string, paginated |
| `/api/AuditLog/Entry/{id}`          | GET    | `id` (long)                                 |
| `/api/AuditLog/GetEntityHistory`    | GET    | `entityName`, `entityId`                    |
| `/api/AuditLog/User/{userId}`       | GET    | `maxRecords` (default 100)                  |
| `/api/AuditLog/Category/{category}` | GET    | `maxRecords` (default 100)                  |
| `/api/AuditLog/GetAuditSummary`     | GET    | —                                           |
| `/api/AuditLog/GetAuditEntityNames` | GET    | —                                           |

### Workflow

Generic state machine over any owning entity. Access functions: `api.workflow.read`, `api.workflow.transition`.

| Endpoint                                                    | Method |
| ----------------------------------------------------------- | ------ |
| `/api/Workflow/{ownerType}/{ownerId}/state`                 | GET    |
| `/api/Workflow/{ownerType}/{ownerId}/history`               | GET    |
| `/api/Workflow/{ownerType}/{ownerId}/available-transitions` | GET    |
| `/api/Workflow/{ownerType}/{ownerId}/transition`            | POST   |

### Optional feature controllers

Present only when the matching feature pack is enabled at scaffold time:

| Controller | Access function   | Feature          |
| ---------- | ----------------- | ---------------- |
| `Chat`     | `api.chat.use`    | `ai-chatbot`     |
| `Report`   | `api.report.read` | `pdf-generation` |

---

## Sample domain: procurement

The procurement slice is a worked example, not part of the platform. Delete it once your own domain exists — `.ai/features/_samples/procurement/remove.md` lists everything that has to go. It is worth reading before you delete it: it is the closest thing to a reference implementation of "how a feature is built here".

| Controller      | Endpoints                                                                             | Access functions                                      |
| --------------- | ------------------------------------------------------------------------------------- | ----------------------------------------------------- |
| `Vendor`        | `GetAll`, `Get/{id}`, `Save`, `Edit`, `Delete/{id}`                                   | `api.procurement.vendor.read` / `.manage`             |
| `CatalogItem`   | `GetAll`, `GetByVendor/{vendorId}`, `Get/{id}`, `Save`, `Edit`, `Delete/{id}`         | `api.procurement.catalog.read` / `.manage`            |
| `PurchaseOrder` | `GetAll`, `Get/{id}`, `Save`, `Edit`, `Submit/{id}`, `ProcessApproval`, `Delete/{id}` | `api.procurement.order.read` / `.manage` / `.approve` |

### Example: GET /api/Vendor/GetAll

```json
[
  {
    "id": 1,
    "name": "Example Supplies Pte Ltd",
    "code": "SUP-001",
    "contactPerson": "A. Tan",
    "email": "orders@example.com",
    "phone": "+65 6000 0000",
    "category": "Supplies",
    "isActive": true,
    "catalogItemCount": 12,
    "createdOn": "2026-01-15T09:30:00",
    "createdBy": "alice"
  }
]
```

### Example: POST /api/Vendor/Save

```json
{
  "name": "New Vendor",
  "code": "SUP-002",
  "category": "Services",
  "isActive": true
}
```

Returns the created record, including its assigned `id`.

---

## Conventions to follow for your own endpoints

1. **`Save` creates, `Edit` updates.** The frontend service picks between them on the presence of an `id`.
2. **`Edit` loads the entity and assigns fields explicitly.** Never map a request DTO wholesale onto a loaded record.
3. **Every endpoint carries `[RequireAccessFunction]`.** An endpoint without one is open to any authenticated user.
4. **Return DTOs, never entities.**
5. **Cap list sizes.** Use the shared page-size pattern on anything that can grow.
6. **Guard per-record ownership** with `EnsureOwnedAsync` or `[RequireOwnership]` where the data has an owner.

## Rate limiting

The template does not ship rate limiting. Add it before you expose the app publicly — the login and registration endpoints are the ones that need it most. ASP.NET Core's built-in rate limiter middleware is the simplest option; a reverse proxy or CDN rule works too.

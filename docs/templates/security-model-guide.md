# Guide: Creating Security Model Documentation

> **This is a GUIDE.** Each project creates its own `docs/security-model.md` with project-specific threat analysis and access control rules. This document explains HOW to create it.

---

## Purpose

The security model document defines authentication flows, authorization rules, role-permission mappings, and threat mitigations. AI agents use this to generate permission checks, middleware configuration, and secure API patterns.

## When to Create

- During Phase 1.2 (Technical Design) of AIDLC
- When adding new roles or permissions
- When integrating external authentication providers
- When handling sensitive data

## Format

Use **Mermaid.js** for auth flows and access matrices. Use Markdown tables for role-permission mappings and STRIDE analysis.

## How to Create

### Step 1: Document the NIE Template Auth Baseline

Every NIE Template project inherits this baseline. Include the following in your `docs/security-model.md`:

```markdown
# Security Model

## Authentication Architecture

\`\`\`mermaid
sequenceDiagram
participant U as User
participant AF as Auth Frontend (:8002)
participant AA as Auth API (:5001)
participant R as Valkey
participant MF as Main Frontend (:8001)
participant MA as Main API (:5002)

    U->>AF: Navigate to app
    AF->>AA: POST /api/Auth/Login {username, password}
    AA->>AA: Validate credentials
    AA->>R: Create session (SessionId → UserData)
    AA-->>AF: Set-Cookie: session_id
    AF->>MF: Redirect to main app

    Note over MF,MA: Subsequent requests
    U->>MF: Interact with app
    MF->>MA: GET /api/resource (Cookie: session_id)
    MA->>R: Validate session
    R-->>MA: User data + permissions
    MA->>MA: Check permissions
    MA-->>MF: Response

\`\`\`

## Session Management

| Property        | Value                             |
| --------------- | --------------------------------- |
| Session Store   | Valkey                            |
| Session Header  | X-Session-Id                      |
| Session Expiry  | [Configure per project]           |
| Cookie Settings | HttpOnly, Secure, SameSite=Strict |
```

### Step 2: Define Roles and Permissions

```markdown
## Roles

| Role          | Description                 | Inherits From |
| ------------- | --------------------------- | ------------- |
| SuperAdmin    | Full system access          | Admin         |
| Admin         | Administrative access       | User          |
| User          | Standard authenticated user | -             |
| [ProjectRole] | [Description]               | [Base role]   |

## Permissions

| Permission     | Description       | Super Admin | Admin |   User   | [Role] |
| -------------- | ----------------- | :---------: | :---: | :------: | :----: |
| users.view     | View user list    |     ✅      |  ✅   |    ❌    |   ❌   |
| users.manage   | Create/edit users |     ✅      |  ✅   |    ❌    |   ❌   |
| entity.view    | View entities     |     ✅      |  ✅   |    ✅    |   ✅   |
| entity.create  | Create entities   |     ✅      |  ✅   |    ✅    |   ❌   |
| entity.edit    | Edit entities     |     ✅      |  ✅   | Own only |   ❌   |
| entity.delete  | Delete entities   |     ✅      |  ✅   |    ❌    |   ❌   |
| entity.approve | Approve entities  |     ✅      |  ✅   |    ❌    |   ❌   |
| reports.view   | View reports      |     ✅      |  ✅   | Limited  |   ❌   |
```

### Step 3: Map Permissions to API Endpoints

```markdown
## API Authorization Matrix

| Endpoint                 | Method | Permission     | Additional Rules        |
| ------------------------ | ------ | -------------- | ----------------------- |
| /api/Entity              | GET    | entity.view    | Users see own + public  |
| /api/Entity/{id}         | GET    | entity.view    | Owner or Admin          |
| /api/Entity              | POST   | entity.create  | -                       |
| /api/Entity/{id}         | PUT    | entity.edit    | Owner or Admin          |
| /api/Entity/{id}         | DELETE | entity.delete  | Admin only, soft delete |
| /api/Entity/{id}/approve | POST   | entity.approve | Cannot approve own      |
| /api/Admin/Users         | GET    | users.view     | Admin panel only        |
| /api/Admin/Users         | POST   | users.manage   | -                       |
```

### Step 4: Document Controller Permission Pattern

```markdown
## Implementation Pattern

### Controller Authorization

\`\`\`csharp
[ApiController]
[Route("api/[controller]")]
public class EntityController : BaseController
{
// Public to authenticated users with permission
[HttpGet]
[RequirePermission("entity.view")]
public async Task<IActionResult> GetAll() { ... }

    // Owner or Admin check in service layer
    [HttpPut("{id:guid}")]
    [RequirePermission("entity.edit")]
    public async Task<IActionResult> Edit(Guid id, EditDto dto) { ... }

    // Admin-only endpoint
    [HttpDelete("{id:guid}")]
    [RequirePermission("entity.delete")]
    public async Task<IActionResult> Delete(Guid id) { ... }

}
\`\`\`

### Service-Level Authorization

\`\`\`csharp
public async Task<ApiResponse<EntityDto>> EditAsync(Guid id, EditDto dto, string currentUser)
{
var entity = await \_dbContext.Entities.FindAsync(id);

    // Owner check: users can only edit their own records
    if (entity.CreatedBy != currentUser && !_currentUserIsAdmin)
        return ApiResponse<EntityDto>.Forbidden("You can only edit your own records");

    // ... proceed with edit

}
\`\`\`
```

### Step 5: STRIDE Threat Analysis

```markdown
## Threat Analysis (STRIDE)

| Threat                    | Category               | Risk   | Mitigation                                               | Status          |
| ------------------------- | ---------------------- | ------ | -------------------------------------------------------- | --------------- |
| Session hijacking         | Spoofing               | High   | HttpOnly cookies, Valkey session store, session rotation | ✅ Mitigated    |
| Privilege escalation      | Tampering              | High   | Server-side permission checks, no client-side role trust | ✅ Mitigated    |
| Data exfiltration         | Information Disclosure | Medium | Row-level filtering (users see own data), audit logging  | ✅ Mitigated    |
| Denial of service         | Denial of Service      | Medium | Rate limiting, request size limits                       | ⚠️ Needs config |
| Mass assignment           | Tampering              | Medium | Use specific DTOs, never bind directly to entities       | ✅ Mitigated    |
| SQL injection             | Tampering              | High   | EF Core parameterized queries, no raw SQL                | ✅ Mitigated    |
| XSS                       | Tampering              | Medium | Vue.js auto-escaping, CSP headers                        | ✅ Mitigated    |
| CSRF                      | Spoofing               | Medium | SameSite cookies, anti-forgery tokens                    | ✅ Mitigated    |
| [Project-specific threat] | [Category]             | [Risk] | [Your mitigation]                                        | [Status]        |
```

### Step 6: Data Access Rules

```markdown
## Data Access Rules

| Data Type      | SuperAdmin | Admin | User (Own) | User (Others) | Anonymous |
| -------------- | :--------: | :---: | :--------: | :-----------: | :-------: |
| User profile   |    R/W     |  R/W  |    R/W     |  R (limited)  |    ❌     |
| Entity records |   R/W/D    | R/W/D |    R/W     | R (if public) |    ❌     |
| Audit logs     |     R      |   R   |     ❌     |      ❌       |    ❌     |
| System config  |    R/W     |   R   |     ❌     |      ❌       |    ❌     |
| Reports        |    All     |  All  | Own scope  |      ❌       |    ❌     |

R = Read, W = Write, D = Delete
```

## Tips

1. **Start with the permission matrix** — It's the most actionable artifact for AI code generation
2. **Use RequirePermission attribute** — NIE Template's custom authorization attribute
3. **Service-level checks for ownership** — Controller-level checks for role/permission
4. **Never trust the frontend** — All authorization enforced server-side
5. **Audit sensitive operations** — Log who did what, when
6. **Principle of least privilege** — Start with no access, grant explicitly

## Review Checklist

- [ ] Authentication flow documented
- [ ] Roles defined with hierarchy
- [ ] Permission matrix complete for all endpoints
- [ ] STRIDE threat analysis performed
- [ ] Data access rules defined per role
- [ ] Sensitive data handling documented
- [ ] Session management configured
- [ ] Controller authorization patterns shown


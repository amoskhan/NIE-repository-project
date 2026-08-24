# API reference

The generated OpenAPI documents are the authoritative endpoint references for the two backend hosts.

| Host | Local base URL | OpenAPI document |
| --- | --- | --- |
| Main API | `http://localhost:5002` | `http://localhost:5002/openapi/v1.json` |
| Auth API | `http://localhost:5001` | `http://localhost:5001/openapi/v1.json` |

Both hosts also expose unauthenticated liveness and readiness endpoints:

- `/health/live`
- `/health/ready`

## Routing

Most Main API controllers inherit the conventional route `api/[controller]/[action]`.
Resource-oriented controllers such as Chat, Report, and Workflow declare explicit routes.
Use the generated OpenAPI document instead of constructing a route from convention alone.

The Procurement reference implementation includes Vendor, Catalog Item, and Purchase Order
controllers. Its identifiers are UUIDv7 values represented as JSON strings. Server-backed data
tables use bounded `Search` and `GetFilterOptions` POST endpoints so paging, sorting, searching,
and facet selection execute in the database.

## Authentication and authorization

The Auth API issues the application session. The Main API validates that session and requires an
access-function attribute on protected controller actions. Screen access in Vue uses the matching
access-function code; a visible route never substitutes for backend authorization.

Browser requests normally send the session through the template's configured cookies. API clients
must follow the authentication contract described in the OpenAPI document and
[`security-model.md`](security-model.md).

## Validation and errors

Request validation uses FluentValidation. Validation failures and handled application failures use
RFC 7807 problem details. Consumers should use the HTTP status and problem `errors` collection,
without depending on exception text.

Expected status classes include:

| Status | Meaning |
| --- | --- |
| `400` | Request validation failed |
| `401` | Authentication is missing or invalid |
| `403` | The session lacks the required access function |
| `404` | The resource or route does not exist |
| `409` | The request conflicts with resource state |
| `429` | A configured rate limit was exceeded |
| `500` | An unexpected server failure occurred |

## Files and identifiers

Document upload endpoints accept `multipart/form-data`. Persisted entity identifiers and foreign
keys use UUIDv7; `int` and `bigint` are reserved for quantities, ordering, counters, sizes, and
other non-identity values.

## Keeping this reference accurate

Treat controller attributes, DTOs, validators, and the generated OpenAPI documents as the executable
contract. Update focused examples here when a cross-cutting convention changes; do not duplicate the
complete generated operation list in Markdown.

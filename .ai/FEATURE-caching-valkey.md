# Caching (Valkey)

Valkey (Redis-compatible) distributed cache used for sessions and general caching. It owns no dedicated source file â€” it is configured directly in the two Program.cs files (IConnectionMultiplexer + AddStackExchangeRedisCache) and consumed via IDistributedCache across features.

Rules version: 2026.08.07.1
Feature key: caching-valkey  
Adoption: **mandatory**

## Adoption and navigation

- Menu or entry point: not independently required. No dedicated menu is required.
- Visibility: Expose only dependency health in Monitoring; never expose cache keys or values.
- If the feature is not adopted, report not-applicable with the product or architecture reason; do not silently omit it.

## Required libraries and minimums

| Library | Package/runtime | Minimum | Ecosystem |
| --- | --- | --- | --- |
| .NET / ASP.NET Core | net | 10.0.0 | runtime |
| Microsoft Redis distributed cache | Microsoft.Extensions.Caching.StackExchangeRedis | 10.0.5 | nuget |
| Valkey | valkey | 8.0.0 | service |

Versions are floors, not forced pins for derived applications. Stable upgrades are allowed when compatibility, build, tests, security, and deployment evidence pass. The template itself keeps exact tested pins and lockfiles.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-CACHE-001 | error | data | Use Valkey only for derivable cache/session data; durable business state belongs in PostgreSQL. | review |
| NIE-CACHE-002 | error | keys | Namespace every key by application, environment, feature, and stable identifier and assign an explicit TTL. | tests |
| NIE-CACHE-003 | error | consistency | Invalidate or version cache entries on writes and role/access changes; tolerate cache misses and temporary outages. | integration-tests |
| NIE-CACHE-004 | error | security | Do not cache secrets or unnecessary personal data and do not log keys containing sensitive identifiers. | security-review |
| NIE-CACHE-005 | error | operations | Monitor connectivity/latency and use bounded retry behavior; do not use pub/sub for must-deliver workflows. | operations-tests |
| NIE-CACHE-006 | error | verification | Test hit, miss, expiry, invalidation, outage fallback, and multi-instance behavior. | integration-tests |

## Canonical reference footprint

Use these files as working examples and comparison targets. Procurement is a reference vertical, not a runtime dependency.

- src/backend/Hosts/Api/Program.cs
- src/backend/Hosts/Auth/Program.cs
- src/backend/Hosts/Api/appsettings.json
- src/backend/Hosts/Auth/appsettings.json

## AI implementation and verification

The implementing AI must:

1. Assess every rule above before editing and identify affected backend, frontend, data, security, navigation, and test surfaces.
2. Compare the application implementation with the pinned canonical template reference. Classify each shared file as identical, behind, customized, ahead, conflict, or not applicable.
3. Implement missing behavior directly. Preserve domain behavior and integrate shared updates through extension points rather than overwriting valid customization.
4. Add or update focused unit, architecture, integration, component, API, and browser tests according to the evidence type for each rule.
5. Run the standard repository gates and record exact commands, exit status, and meaningful test counts or artifacts.
6. Report each rule as pass, fail, not-applicable, manual-review, or approved-exception with file/line and test evidence.

A separate AI verifier must review material changes from a fresh context, inspect the diff and evidence, rerun risk-relevant gates, and issue an independent verdict. A claim without traceable evidence is not a pass.

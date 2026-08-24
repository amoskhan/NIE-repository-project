# Architecture guide for derived applications

Start from `docs/architecture.md`. Preserve its dependency direction and stable source identities, then add the application-specific context described here.

## Required sections

1. Product purpose, users, owners, data classification, and availability objectives.
2. A Mermaid system-context diagram showing users, runtime containers, datastores, and external providers.
3. A Mermaid container/dependency diagram showing the .NET and Vue boundaries.
4. External integration table with protocol, authentication, data exchanged, timeout/retry policy, owner, alternative provider, and exit path.
5. Trust boundaries, authorization boundaries, personal-data flows, secret ownership, audit requirements, and retention.
6. Deployment topology, scaling assumptions, health/readiness behavior, backup/recovery objectives, and operational ownership.
7. Key sequence diagrams for authentication and each high-risk or state-changing business workflow.
8. Open decisions, approved exceptions, and links to ADRs.

## Stable source identities

Do not replace generic source names with the product name. Retain:

- `src/backend/Backend.sln` and the `Hosts`, `Core`, `Infrastructure`, and `BuildingBlocks` structure;
- the generic .NET assembly and root namespace names documented in `docs/architecture.md`;
- `src/frontend/apps/main`, `src/frontend/apps/auth`, and `src/frontend/packages/*`;
- `@nie/contracts`, `@nie/platform`, and `@nie/ui`.

Record product identity through typed runtime configuration, `app-config`, branding, deployment values, observability resource attributes, and external artifact labels. This is a template-update constraint, not merely a naming preference.

## Dependency review

Confirm the following in the diagram and in architecture tests:

- Domain has no project dependencies.
- Application does not reference infrastructure or hosts.
- Infrastructure implements application-owned ports.
- Hosts are composition roots and endpoints remain thin.
- Provider SDK types do not leak into domain, application, or public contracts.
- `@nie/contracts` is dependency-free, `@nie/platform` is non-visual, and `@nie/ui` is domain-neutral.
- Business workflows stay in the owning application feature.

## Integration table template

| Integration | Protocol | Authentication | Data | Resilience | Owner | Alternative/exit |
| --- | --- | --- | --- | --- | --- | --- |
| Example provider | HTTPS | managed credential | approved fields only | timeout, bounded retry, circuit behavior | team | replacement adapter and export plan |

## Security-boundary checklist

- Identify where an unauthenticated request becomes an authenticated principal.
- Identify where application and role/access-function authorization are enforced.
- Show which services and networks can reach PostgreSQL, Valkey, and provider credentials.
- Document outbound URL controls, upload scanning, encryption, audit events, and data retention where applicable.
- Keep secrets out of diagrams, examples, logs, and committed configuration.

## ADR triggers

Add an ADR for a new datastore or runtime boundary, reversed dependency, proprietary dependency, major framework upgrade, authentication/authorization model change, identifier migration, destructive data migration, or material provider lock-in. Include alternatives, decision drivers, security and operations impact, migration, rollback, and exit cost.

## Review checklist

- [ ] Diagrams use Mermaid and match deployed reality.
- [ ] Stable source identities and dependency direction are unchanged or an approved ADR explains why.
- [ ] Every external integration has an owner, security boundary, resilience behavior, and exit path.
- [ ] Critical flows and failure paths are represented.
- [ ] Deployment, recovery, and observability assumptions are explicit.
- [ ] Architecture tests and current build/test evidence support the document.

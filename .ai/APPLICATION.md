# Application context

This file is owned by the application and is preserved when canonical `.ai` rule Markdown is refreshed.

## Identity


- Delivery assurance profile: standard
- Application: NIE Template reference application
- Product owner: record the owning team
- Technical owner: record the owning team
- Data classification: record the approved classification
- Deployment environments: local, development, staging, production as applicable

## Derived-application profile

The canonical repository deliberately retains the Procurement reference vertical. A
derived application must replace the values below during product specification and
keep the decision synchronized with its active feature's
`application-profile.md`. Generic source-tree identities remain unchanged.

- Product title: NIE Template
- Product owner: NIE Template maintainers
- Reference sample decision: retain
- Reference sample retention reason: The canonical template retains Procurement as the executable architecture and design reference; derived products normally remove it after their real domain and regression coverage replace its learning value.
- Runtime routing contract: typed-semantic-services
- UX reference strategy: Reuse the canonical shell, shared components, interaction states, spacing, typography, and responsive behavior while replacing every Procurement-specific label, route, permission, record, and workflow with the real product domain.

For a derived product, `NIE Template` is never an acceptable visible title. A
`remove` decision requires removal or deactivation of active Procurement frontend
routes/navigation/pages and backend registrations/seeds/permissions. A `retain`
decision requires a concrete product use case and accountable owner; reference value
alone is not a derived-product use case.

## Adopted feature decisions

The canonical template contains reference implementations for all feature dossiers. A derived application must list each `FEATURE-*.md` key as adopted, not adopted, or planned, with a reason and product owner. Absence is not a decision.

| Feature | Decision | Reason/owner |
| --- | --- | --- |
| Mandatory global baseline features | adopted | Required by `GLOBAL-RULES.md` and the applicable feature dossiers |
| Opt-in features | assess during scaffold and product design | Record an explicit decision per feature |
| Service integration (RabbitMQ/gRPC) | reference included; disabled by default | Adopt only with named peer, contract owner, data classification, and operations owner |

## Application-specific constraints

- Add business, integration, hosting, data-retention, accessibility, and operational constraints here.
- Add repository-specific commands only when they are not already documented in the main README.
- Do not weaken canonical rules or library minimums.

## Approved exceptions

Do not use this section as a blanket waiver. Link only approved, owned, expiring exception records and keep the rule-by-rule details in the change evidence.

## Reference implementation

The Procurement vertical under `src/backend` and `src/frontend/apps/main` demonstrates real screens, access functions, FluentValidation, VeeValidate/Zod, UUIDv7 identities, audit, notifications, tables, error/empty states, and shared components. Reuse its patterns while replacing procurement-specific domain behavior.

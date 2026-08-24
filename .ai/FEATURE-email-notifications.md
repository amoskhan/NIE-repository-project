# Notification Administration and Email Templates

NIE-branded versioned email templates with Procurement reference content, live preview, channel policy administration, durable delivery records, personal inbox APIs, and test sending.

Rules version: 2026.08.07.1
Feature key: email-notifications  
Adoption: **mandatory**

## Adoption and navigation

- Menu or entry point: required at **Administration > Notifications**.
- Visibility: System administrators with the notification-administration screen and API access functions only.
- If the feature is not adopted, report not-applicable with the product or architecture reason; do not silently omit it.

## Required libraries and minimums

| Library | Package/runtime | Minimum | Ecosystem |
| --- | --- | --- | --- |
| .NET / ASP.NET Core | net | 10.0.0 | runtime |
| Entity Framework Core | Microsoft.EntityFrameworkCore | 10.0.5 | nuget |
| Npgsql EF Core Provider | Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.1 | nuget |
| PostgreSQL | postgres | 17.0.0 | service |
| FluentValidation | FluentValidation | 12.1.1 | nuget |
| MailKit | MailKit | 4.16.0 | nuget |
| Vue | vue | 3.5.30 | npm |
| Vue Router | vue-router | 4.5.1 | npm |
| Axios | axios | 1.18.0 | npm |
| VeeValidate | vee-validate | 4.15.1 | npm |
| VeeValidate Zod integration | @vee-validate/zod | 4.15.1 | npm |
| Zod | zod | 3.25.67 | npm |
| Vitest | vitest | 4.1.10 | npm |
| Vue Test Utils | @vue/test-utils | 2.4.11 | npm |
| jsdom | jsdom | 30.0.1 | npm |
| SignalR JavaScript client | @microsoft/signalr | 10.0.0 | npm |
| OneSignal Web SDK | OneSignalSDK.page.js | 16.0.0 | cdn |

Versions are floors, not forced pins for derived applications. Stable upgrades are allowed when compatibility, build, tests, security, and deployment evidence pass. The template itself keeps exact tested pins and lockfiles.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-EMAIL-001 | error | architecture | Send through IEmailService backed by MailKit and validated options; domain services must not construct SMTP clients. | architecture-tests |
| NIE-EMAIL-002 | error | templates | Use owned templates with validated placeholders, encoded untrusted values, text fallback where appropriate, and deterministic subject/sender rules. | template-tests |
| NIE-EMAIL-003 | error | reliability | Use a durable queue/outbox with idempotency for critical mail; bounded direct send is allowed only for non-critical mail. | integration-tests |
| NIE-EMAIL-004 | error | security | Require transport security outside Development and never log credentials, full bodies, or sensitive recipient data. | security-review |
| NIE-EMAIL-005 | error | audit | Audit queued/sent/failed outcomes using message/template identifiers and safe metadata. | tests |
| NIE-EMAIL-006 | error | verification | Test rendering, encoding, attachments, retry/idempotency, timeout, provider failure, and disabled configuration. | tests |
| NIE-EMAIL-007 | error | frontend | Begin Administration > Notifications directly with the shared NieTabs Policies, Email templates, and Delivery tablist; do not place an orchestration introduction, refresh action, or channel-health rail above it. In policy rows, place nullable reminder/escalation controls below the channel switches and expose them only for reminder emails. | component-and-browser-tests |
| NIE-EMAIL-008 | error | branding | Keep the NIE/NTU logo, application header, and standard footer in the owned BaseTemplate wrapper; administrators may edit only the versioned subject and inner content, and preview that content in a sandboxed iframe. | template-and-component-tests |
| NIE-EMAIL-009 | error | versioning | Store immutable template versions per event/channel, keep exactly one published version, show version history, and restore by publishing a selected historical version without overwriting it. | integration-tests |
| NIE-EMAIL-010 | error | authorization | Define the notification-administration screen/read/manage/delivery-read/delivery-retry access functions; gate the route/menu and place RequireAccessFunction on every notification controller action. | architecture-and-api-tests |
| NIE-EMAIL-011 | error | identity | Use UUIDv7 Guid primary and foreign keys mapped to PostgreSQL uuid for policies, templates, outbox messages, deliveries, inbox notifications, and preferences; Vue treats identifiers as strings. | architecture-tests |
| NIE-EMAIL-012 | error | validation | Validate API requests with FluentValidation and validate Vue forms with VeeValidate plus Zod; reject unknown placeholders and unsafe HTML while encoding all untrusted replacement values. | validation-and-renderer-tests |
| NIE-EMAIL-013 | error | reliability | Critical notifications commit through the durable outbox with a unique dedupe key, isolated per-recipient/channel deliveries, bounded retries, and an administrator retry action; channel failures must not roll back the domain workflow. | integration-tests |
| NIE-EMAIL-014 | error | operations | Expose provider readiness without secrets and keep authorized InApp, Email, and Push test-send API operations with explicit success/failure results; applications may expose those operations only in an approved operations surface. | api-tests |
| NIE-EMAIL-015 | error | retention | Retain delivery metadata and template history for at least six months or the application-approved longer period; do not retain SMTP credentials or duplicate full sensitive message bodies in logs. | operations-review |
| NIE-EMAIL-016 | error | scaffold | Use the canonical NIE Template/Application markers consistently so the post-copy AI can rename application branding and namespaces deterministically; no source-system brand reference may remain in generated source, templates, tests, or instructions. | scaffold smoke test and source search |
| NIE-EMAIL-017 | error | content | Keep the released Procurement reference catalog subjects, placeholders, and HTML content deterministic and seeded by stable procurement event key; derived applications replace sample events as an approved domain change, and seed updates must publish a new system version without overwriting administrator-authored content. | seed-and-renderer-tests |
| NIE-EMAIL-018 | error | policy | Treat reminder and escalation hours as nullable opt-in settings exposed only for catalog events explicitly marked as reminder emails. Hide both controls for every other event, reject non-reminder API requests that set either value, clear legacy non-reminder timing during seed reconciliation, validate each configured range, and require escalation to be later only when both values are present. | policy-contract-component-and-seed-tests |

## Canonical reference footprint

Use these files as working examples and comparison targets. Procurement is a reference vertical, not a runtime dependency.

- src/backend/Core/Application/Features/Email/IEmailService.cs
- src/backend/Infrastructure/Persistence/Providers/Email/EmailService.cs
- src/backend/Infrastructure/Persistence/Options/EmailSettings.cs
- src/backend/Hosts/Api/Templates/BaseTemplate.html
- src/backend/Hosts/Api/Controllers/NotificationAdministrationController.cs
- src/backend/Hosts/Api/Controllers/NotificationController.cs
- src/backend/Hosts/Api/Hubs/NotificationHub.cs
- src/backend/Hosts/Api/Jobs/NotificationDispatcherJob.cs
- src/backend/Hosts/Api/Jobs/NotificationRetentionJob.cs
- src/backend/Hosts/Api/Validation/NotificationAdministrationRequestValidators.cs
- src/backend/Core/Domain/Models/NotificationPolicy.cs
- src/backend/Core/Domain/Models/NotificationTemplate.cs
- src/backend/Core/Domain/Models/NotificationOutbox.cs
- src/backend/Core/Domain/Models/NotificationDelivery.cs
- src/backend/Core/Domain/Models/Notification.cs
- src/backend/Core/Domain/Models/UserNotificationPreference.cs
- src/backend/Core/Application/Contracts/NotificationAdministrationDtos.cs
- src/backend/Infrastructure/Persistence/Options/NotificationSettings.cs
- src/backend/Core/Application/Features/Notifications/Rules/NotificationEventCatalog.cs
- src/backend/Core/Application/Features/Notifications/Rules/NotificationPolicyTimingRules.cs
- src/backend/Core/Application/Features/Notifications/INotificationTemplateRenderer.cs
- src/backend/Core/Application/Features/Notifications/NotificationTemplateRenderer.cs
- src/backend/Core/Application/Features/Notifications/INotificationOutboxService.cs
- src/backend/Core/Application/Features/Notifications/NotificationOutboxService.cs
- src/frontend/apps/main/src/components/admin/NotificationAdministration.vue
- src/frontend/apps/main/src/components/admin/notificationEmailPreview.ts
- src/frontend/apps/main/src/components/admin/notificationPolicyTiming.ts
- src/frontend/apps/main/src/services/notificationAdministrationService.ts

## AI implementation and verification

The implementing AI must:

1. Assess every rule above before editing and identify affected backend, frontend, data, security, navigation, and test surfaces.
2. Compare the application implementation with the pinned canonical template reference. Classify each shared file as identical, behind, customized, ahead, conflict, or not applicable.
3. Implement missing behavior directly. Preserve domain behavior and integrate shared updates through extension points rather than overwriting valid customization.
4. Add or update focused unit, architecture, integration, component, API, and browser tests according to the evidence type for each rule.
5. Run the standard repository gates and record exact commands, exit status, and meaningful test counts or artifacts.
6. Report each rule as pass, fail, not-applicable, manual-review, or approved-exception with file/line and test evidence.

A separate AI verifier must review material changes from a fresh context, inspect the diff and evidence, rerun risk-relevant gates, and issue an independent verdict. A claim without traceable evidence is not a pass.

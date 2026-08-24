# Service integration: RabbitMQ pub/sub and gRPC

Versioned, provider-neutral service-to-service contracts for durable asynchronous events and bounded synchronous queries.

Rules version: 2026.08.07.1
Feature key: service-integration-rabbitmq-grpc
Adoption: **opt-in; mandatory when an application publishes/subscribes to cross-application events or calls/provides gRPC**

## Adoption, menus, and frontend

- Menu or entry point: none. Do not add an integration administration menu to an end-user application.
- Frontend: Vue calls its own REST/BFF endpoints. Browser-to-service gRPC and direct browser access to RabbitMQ are prohibited.
- Operational access: RabbitMQ management and telemetry stay on restricted operations networks; they are not embedded in the application UI.
- A derived application records adopted/not adopted/planned, its owned contracts, peer owners, data classification, and recovery owner in `.ai/APPLICATION.md`.

## Required libraries and minimums

| Library/service | Package/runtime | Minimum | Owner/ecosystem |
| --- | --- | --- | --- |
| RabbitMQ server | rabbitmq | 4.3.4 | RabbitMQ official image |
| RabbitMQ .NET client | RabbitMQ.Client | 7.2.2 | RabbitMQ team official package |
| ASP.NET Core gRPC server | Grpc.AspNetCore | 2.80.0 | gRPC official package |
| ASP.NET Core gRPC health | Grpc.AspNetCore.HealthChecks | 2.80.0 | gRPC official package |
| gRPC client factory | Grpc.Net.ClientFactory | 2.80.0 | gRPC official package |
| gRPC core API | Grpc.Core.Api | 2.80.0 | gRPC official package |
| Protobuf compiler tooling | Grpc.Tools | 2.83.0 | gRPC official package |
| Protocol Buffers runtime | Google.Protobuf | 3.35.1 | Google official package |
| PostgreSQL / EF Core | postgres / Microsoft.EntityFrameworkCore | 17.0.0 / 10.0.5 | PostgreSQL / Microsoft |

These are floors. The template pins exact tested versions; stable newer versions are permitted after compatibility, contract, load, failure, security, and deployment evidence passes.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-INT-001 | error | architecture | Use gRPC only for synchronous, latency-sensitive service-to-service queries/commands that must complete in the caller's process; use RabbitMQ events for asynchronous facts and eventual consistency. | architecture-review |
| NIE-INT-002 | error | boundary | Keep domain/application code transport-neutral. RabbitMQ and gRPC client implementation types remain in Infrastructure; protobuf/event contracts remain in the Contracts project. | architecture-tests |
| NIE-INT-003 | error | ownership | Every application must maintain a schema-validated `integration-manifest.json` listing every event it publishes/subscribes and every gRPC service/method it provides/consumes. Empty arrays are explicit decisions. | manifest-test |
| NIE-INT-004 | error | contracts | Event names are stable reverse-domain names; event versions are positive integers and form versioned broker routing keys; published event classes and protobuf packages/services are immutable after release. Breaking changes require a new event or protobuf API version and a compatibility window. | contract-tests |
| NIE-INT-005 | error | identity | Message, inbox, outbox, aggregate, and protobuf identifier values use non-empty UUIDv7 values represented as native PostgreSQL `uuid` and canonical strings at protobuf/JSON boundaries. | unit-and-database-tests |
| NIE-INT-006 | error | publishing | Persist events through a transactional PostgreSQL outbox in the same `SaveChanges` transaction as the domain mutation. Never perform broker I/O inside the request database transaction. | PostgreSQL-integration-tests |
| NIE-INT-007 | error | delivery | Publish persistent messages to durable exchanges with publisher confirms and mandatory routing. Treat unroutable publication as failure and retry through the outbox. | RabbitMQ-integration-tests |
| NIE-INT-008 | error | consuming | Use durable quorum queues, manual acknowledgement, bounded prefetch, at-least-once delivery, transactional inbox deduplication, and idempotent handlers. Copy RabbitMQ delivery memory before asynchronous work. | RabbitMQ-and-PostgreSQL-tests |
| NIE-INT-009 | error | failure | Use bounded delayed retry and a durable dead-letter queue. Permanent schema/contract failures go directly to dead letter; acknowledge the original only after confirmed retry publication. Never create an infinite requeue loop. | failure-path-tests |
| NIE-INT-010 | error | retention | Retain inbox receipts longer than the maximum replay/redelivery window, prune only successfully published outbox rows automatically, and keep dead-lettered messages for owned investigation/replay. | retention-test-and-operations-review |
| NIE-INT-011 | error | grpc | Generate gRPC clients/servers from `.proto` files, use the client factory, set deadlines and message limits, and retry only explicitly idempotent methods for transient `Unavailable` failures. | integration-tests |
| NIE-INT-012 | error | security | Outside Development require TLS (`amqps` and HTTPS), broker least-privilege users/vhosts, OAuth 2 client credentials on outbound gRPC, JWT audience/issuer validation on inbound gRPC, and secret-manager supplied credentials. | configuration-and-security-tests |
| NIE-INT-013 | error | privacy | Do not put credentials, tokens, raw documents, unrestricted personal data, or unbounded payloads in messages, logs, traces, or dead-letter metadata. Validate payloads and enforce configured byte limits. | security-review |
| NIE-INT-014 | error | availability | A gRPC caller must define deadline, cancellation, unavailable behavior, and whether a stale/cache/REST fallback is allowed. An asynchronous consumer must tolerate duplicate, delayed, and out-of-order delivery. | use-case-tests |
| NIE-INT-015 | error | observability | Propagate correlation/causation/message identifiers; emit structured metrics/traces for publish, consume, retry, dead letter, latency, outbox age/backlog, and dependency readiness without logging payloads or secrets. | telemetry-tests-and-dashboard-review |
| NIE-INT-016 | error | operations | Document queue/exchange ownership, alert thresholds, dead-letter inspection/replay, retention, compatibility rollout order, rollback, and ECS/service-network HTTP/2/TLS configuration. | runbook-review |
| NIE-INT-017 | error | portability | Integrate RabbitMQ through AMQP 0-9-1 and gRPC through open protobuf/HTTP/2 contracts. Do not expose AWS/Azure/vendor SDK types outside Infrastructure; retain a credible alternative broker/service-host path. | architecture-review |
| NIE-INT-018 | error | verification | Run pure contract/topology tests plus real RabbitMQ and PostgreSQL integration tests, migration review, authenticated gRPC tests, degraded dependency tests, vulnerability audit, and an independent AI review. | gates-and-independent-verdict |

## Canonical reference footprint

- `src/backend/Core/Contracts/integration-manifest.json`
- `src/backend/Core/Contracts/integration-manifest.schema.json`
- `src/backend/Core/Contracts/Events`
- `src/backend/Core/Contracts/Protos`
- `src/backend/Core/Application/Integration`
- `src/backend/Infrastructure/Integration`
- `src/backend/Infrastructure/Persistence/Integrations`
- `src/backend/Hosts/Api/Grpc`
- `src/backend/Infrastructure/Persistence/Migrations/*AddServiceIntegrationOutboxInbox*`
- `src/backend/Tests/Integration.Tests`
- `docs/service-integration.md`
- `deploy/local/service-integration.compose.yml`

Procurement is the real-world sample: it publishes purchase-order status facts and provides a purchase-order gRPC query. Vendor Master is the illustrative peer: Procurement consumes its profile event and directory query. Derived applications replace these domain contracts while retaining the platform patterns.

## AI implementation and verification

The implementing AI must enumerate the two applications and invert the producer/consumer/provider/client sides deliberately; update the machine-readable manifests and generated contracts first; implement domain transactions, handlers, security, configuration, deployment, tests, and runbook; then report every `NIE-INT-*` rule. A separate security-focused AI verifier must inspect the final diff and rerun the risk-relevant gates.

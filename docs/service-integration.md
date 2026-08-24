# RabbitMQ pub/sub and gRPC service integration

The template supports a two-application integration model without coupling domain code to a cloud provider:

```text
Vue browser -> own REST/BFF -> Application A --gRPC/HTTP2--> Application B
                                |                         |
                                +-- PostgreSQL outbox     +-- PostgreSQL inbox
                                      -> RabbitMQ topic event -> durable quorum queue
```

Use gRPC for synchronous information needed during an active request or process. Use RabbitMQ for facts that occur occasionally and can be handled with eventual consistency. The browser never connects directly to RabbitMQ or a peer gRPC service.

## Contract ownership

Each application owns a packable `Core/Contracts` project and an `integration-manifest.json`. The manifest must explicitly list:

- `publishes`: event name, version, CLR payload contract, and content type;
- `subscribes`: the same fields for inbound events;
- `grpcProvides`: fully qualified service, owning `.proto`, and methods;
- `grpcConsumes`: peer service, pinned `.proto`, and methods.

The JSON Schema beside the manifest makes this machine-checkable. The compile-time `IntegrationContractCatalog` must match it. The owner publishes an immutable, versioned contract artifact containing the compiled .NET types, source `.proto` files, manifest, and schema; consumers pin a compatible version. Set `IntegrationContractPackageId`, `applicationKey`, and the package version during packaging to application-owned values, while retaining the stable generic source project path.

Never edit a released event v1 or protobuf v1 incompatibly. Additive protobuf fields use new field numbers and removed fields are reserved. Breaking behavior receives a new event version or protobuf package/service version and both versions run during a documented rollout window.

## Reference flow

The Procurement sample demonstrates both directions:

1. A purchase-order status transition and its `nie.procurement.purchase-order.status-changed` envelope are persisted in one PostgreSQL transaction.
2. The outbox worker publishes the persistent JSON envelope with publisher confirms and mandatory routing.
3. A peer creates its own durable queue and handles the fact at least once.
4. Procurement subscribes to `nie.vendor-master.vendor-profile.changed`; its transactional inbox deduplicates by message ID and consumer before updating the local vendor projection.
5. Procurement provides `nie.integration.procurement.v1.ProcurementQuery/GetPurchaseOrderSummary` and consumes `nie.integration.vendor.v1.VendorDirectory/GetVendorSnapshot` for bounded synchronous reads.

The second application generated from this template should declare the inverse manifest, implement its owned Vendor Directory server/event publisher, and consume or call Procurement only where its product behavior requires it.

## RabbitMQ behavior

The tested runtime is the official RabbitMQ `4.3.4-management-alpine` image and official `RabbitMQ.Client` 7.2.2 package. The local compose reference runs the image as its built-in non-root `rabbitmq` user; preserve an equivalent non-root task user and writable data volume in deployment. Runtime semantics are:

- AMQP 0-9-1 topic exchange `nie.events` by default, with versioned routing keys such as `nie.vendor-master.vendor-profile.changed.v1` so contract versions can coexist;
- one durable quorum queue per application, event, and version;
- persistent JSON messages with UUIDv7 message IDs, UTC occurrence time, producer, correlation, causation, event name/version, and typed data;
- publisher confirms and mandatory routing;
- manual acknowledgement and bounded prefetch;
- quorum-queue at-least-once dead-lettering with `reject-publish` overflow on main and retry queues, so unavailable dead-letter targets do not silently drop rejected or expired messages;
- bounded delayed retries through a retry exchange/queue;
- permanent or exhausted failures through a dead-letter exchange/queue;
- a seven-day default maximum ordinary replay/redelivery window, enforced as the main-queue message TTL;
- transactional PostgreSQL inbox deduplication;
- automatic pruning of old successful outbox rows and inbox receipts; dead-lettered outbox rows are retained for investigation.

Delivery is at least once, not exactly once. Handlers must be idempotent and tolerate duplicates, delay, and reordering. Do not use an event as a synchronous request/reply mechanism.

For local broker-only startup, set the required variables in an ignored environment file and run:

```bash
docker compose -f deploy/local/service-integration.compose.yml up -d
```

The management port is for restricted local/operations use only and must not be publicly routed.

## gRPC behavior

The server and client are generated from protobuf using official gRPC packages. Each call has cancellation, a deadline, maximum send/receive sizes, and a safe retry policy limited to idempotent unary queries returning `Unavailable`. `NotFound`, `InvalidArgument`, and data-integrity failures are explicit gRPC statuses; business failures are not retried blindly.

Outside Development:

- the peer address and identity endpoints must use HTTPS;
- outbound calls obtain an OAuth 2 client-credentials token;
- inbound calls validate JWT issuer and audience under the dedicated `ServiceIntegration` policy;
- browser session validation is bypassed only for endpoints carrying internal service metadata, while the service policy remains mandatory;
- the standard gRPC health service and HTTP readiness include required dependencies.

## Configuration

The reference feature is disabled by default. Configure through environment variables or the deployment secret/config provider. Relevant keys use the standard .NET double-underscore mapping:

```text
ServiceIntegration__Enabled
ServiceIntegration__ApplicationKey
ServiceIntegration__RabbitMq__Enabled
ServiceIntegration__RabbitMq__ConnectionString          (secret)
ServiceIntegration__RabbitMq__Exchange
ServiceIntegration__RabbitMq__QueuePrefix
ServiceIntegration__RabbitMq__PrefetchCount
ServiceIntegration__RabbitMq__RetryDelaySeconds
ServiceIntegration__RabbitMq__MaximumDeliveryAttempts
ServiceIntegration__RabbitMq__MaximumReplayWindowDays
ServiceIntegration__RabbitMq__MaximumMessageBytes
ServiceIntegration__Grpc__Enabled
ServiceIntegration__Grpc__PeerAddress
ServiceIntegration__Grpc__DeadlineMilliseconds
ServiceIntegration__Grpc__MaximumRetryAttempts
ServiceIntegration__Grpc__MaximumMessageBytes
ServiceIntegration__Grpc__RequireAuthentication
ServiceIntegration__Grpc__Authority
ServiceIntegration__Grpc__Audience
ServiceIntegration__Grpc__RequiredInboundScope
ServiceIntegration__Grpc__TokenEndpoint
ServiceIntegration__Grpc__ClientId
ServiceIntegration__Grpc__ClientSecret                 (secret)
ServiceIntegration__Grpc__Scope
ServiceIntegration__Outbox__BatchSize
ServiceIntegration__Outbox__MaximumAttempts
ServiceIntegration__Outbox__PublishedRetentionDays
ServiceIntegration__Outbox__InboxRetentionDays
ServiceIntegration__Outbox__MetricsSampleSeconds
```

`MaximumRetryAttempts` counts the original call. Set it to `1` to disable automatic retry; values `2` through `5` enable a bounded retry policy only for the idempotent `GetVendorSnapshot` lookup. Do not apply that policy to command RPCs.

Do not commit connection URIs or client secrets. Supply them from ECS Secrets Manager/Parameter Store injection or the equivalent secret facility on another platform.

## ECS deployment checklist

Networking can use ECS Service Connect, Cloud Map/private DNS, an internal HTTP/2-capable load balancer, or another private service network. The application code does not depend on those AWS APIs.

1. Permit only the application security groups to reach the peer HTTPS and RabbitMQ TLS listener.
2. The API image exposes REST/health on `8080` (HTTP/1.1) and a dedicated cleartext HTTP/2 gRPC target on `8081`. Configure an ECS task port mapping and an ALB target group with protocol version `gRPC` for `8081`; terminate TLS at the internal ALB. The standard gRPC health service is intentionally anonymous so the target group can evaluate real dependency readiness, while business RPCs still require JWT audience and scope. Restrict `8081` to the internal load balancer and peer security groups; never publish the anonymous health endpoint or cleartext target port outside the private task network.
3. Preserve HTTP/2 end-to-end for gRPC and configure the target health protocol accordingly.
4. Use private DNS names covered by the service certificates; do not disable certificate validation.
5. Use one integration-domain vhost per environment (or tenant boundary) for applications that exchange events, with a separate least-privilege broker user and permissions for each application. Separate peer applications cannot communicate across different vhosts unless an explicitly operated federation or shovel is part of the design. Use separate OAuth clients per application/environment.
6. Set rolling deployment order so providers support both old and new contracts before consumers switch.
7. Alert on readiness, gRPC latency/error rate, Rabbit connection/recovery, unroutable publishes, retry/dead-letter counts, outbox age/backlog, and consumer lag.
8. Size tasks and prefetch from load tests; do not raise concurrency without validating handler/database capacity.

When REST and gRPC share a TLS endpoint, configure Kestrel for `Http1AndHttp2`; ALPN selects the protocol. When an ECS target uses cleartext HTTP/2 after private load-balancer TLS termination, configure a separate Kestrel endpoint with `Protocols=Http2` and route only the gRPC target group to that container port. A cleartext `Http1AndHttp2` endpoint cannot negotiate HTTP/2 and is not a valid substitute.

## Dead-letter recovery and rollback

Operations owns each queue. Inspect dead letters without exposing payload data, fix the handler/contract/configuration, test the fix, and replay through an audited tool that preserves the original message and correlation IDs. Ordinary replay is allowed only within `MaximumReplayWindowDays`; inbox receipts must be retained for strictly longer than that window. An older dead-letter requires a separate idempotency and data-impact decision plus explicit approval before replay. Never move an unknown message back into the main queue repeatedly.

Rollback application code only while its peer still supports the previous contract. Database rollback drops inbox/outbox tables only when integration is disabled, messages are drained or retained elsewhere, and the data-loss decision is explicitly approved. Broker queues/exchanges are not deleted automatically by application rollback.

## Portability

The portable boundary is AMQP 0-9-1 plus protobuf/gRPC over HTTP/2. RabbitMQ types are limited to `Infrastructure/Integration`; cloud identity/networking types remain in deployment. A future broker can replace the `IIntegrationEventTransport` adapter while retaining the envelope/outbox/inbox/application handlers. A future RPC transport replaces the infrastructure client behind `IVendorDirectoryClient`, although changing the externally released contract still requires versioning and migration evidence.

## Dependency decision

There is no Microsoft RabbitMQ client in the .NET platform. The template therefore uses `RabbitMQ.Client`, the RabbitMQ team's official open-source client (Apache-2.0/MPL-2.0), behind an application-owned transport interface. It does not implement AMQP itself. MassTransit and CAP are credible higher-level open-source alternatives when saga/orchestration or broader broker abstraction justifies their additional conventions; NServiceBus is a commercial alternative. They were not added because this reference needs only durable pub/sub, outbox/inbox, and explicit contracts and should avoid overlapping messaging frameworks.

The gRPC packages are the official gRPC .NET packages and `Google.Protobuf` is the official Protocol Buffers runtime. The ASP.NET/BCL HTTP and JWT facilities provide hosting, OAuth token retrieval, and authentication. No AWS SDK is required. Kafka, NATS, managed RabbitMQ, Dapr pub/sub, SNS/SQS, Azure Service Bus, and Google Pub/Sub remain architectural alternatives, but adopting one requires a new infrastructure adapter, delivery-semantics review, contract tests, operations runbook, and approved dependency evidence rather than leaking provider types into application code.

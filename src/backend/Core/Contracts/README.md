# Application integration contracts

This package is the versioned service-integration boundary for one application. It contains:

- compiled .NET event and gRPC contract types;
- the original protobuf definitions for independent client generation;
- `integration-manifest.json`, which declares the events this application publishes and subscribes to and the gRPC services it provides and consumes;
- the JSON Schema used to validate that manifest.

Before publishing from a derived application, set `IntegrationContractPackageId`, the package version, and `applicationKey` to application-owned values. Keep protobuf package names, event names, and serialized fields backward compatible within a major contract version. Add a new event or protobuf service version for a breaking change and support both versions during migration.

Do not place domain behavior, infrastructure adapters, credentials, or deployment configuration in this package. See `docs/service-integration.md` in the template repository for ownership, compatibility, rollout, and operational requirements.

namespace Api.Grpc;

/// <summary>
/// Marks an endpoint as service-to-service so browser-session middleware does not process it.
/// Authentication is still enforced by the dedicated service integration policy.
/// </summary>
public sealed class ServiceIntegrationEndpointMetadata;

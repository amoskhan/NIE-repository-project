namespace Contracts.Integration;

/// <summary>
/// Identifies one versioned asynchronous integration contract.
/// </summary>
/// <param name="Name">Stable event name used as the RabbitMQ routing key.</param>
/// <param name="Version">Positive schema version.</param>
/// <param name="ContractType">CLR payload type generated or owned by the contract assembly.</param>
public sealed record IntegrationContractDescriptor(string Name, int Version, Type ContractType);

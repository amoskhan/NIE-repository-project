using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Contracts.Integration;

namespace Architecture.Tests;

public class ServiceIntegrationScaffoldTests
{
    private static readonly DirectoryInfo RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Service_integration_projects_and_contract_manifest_are_present()
    {
        AssertFileExists("src/backend/Core/Contracts/Contracts.csproj");
        AssertFileExists("src/backend/Infrastructure/Integration/Integration.csproj");
        AssertFileExists("src/backend/Core/Contracts/integration-manifest.json");
        AssertFileExists("src/backend/Core/Contracts/integration-manifest.schema.json");
    }

    [Fact]
    public void Service_integration_has_contract_transport_persistence_and_grpc_boundaries()
    {
        AssertFileExists("src/backend/Core/Contracts/Integration/Events/IntegrationEventEnvelope.cs");
        AssertFileExists("src/backend/Core/Application/Integration/Messaging/Publishing/IIntegrationEventPublisher.cs");
        AssertFileExists("src/backend/Core/Domain/Models/Integration/IntegrationOutboxMessage.cs");
        AssertFileExists("src/backend/Infrastructure/Integration/RabbitMq/Publishing/RabbitMqEventTransport.cs");
        AssertFileExists("src/backend/Infrastructure/Persistence/Integrations/Inbox/EfIntegrationEventProcessor.cs");
        AssertFileExists("src/backend/Infrastructure/Persistence/Integrations/Outbox/EfIntegrationOutboxStore.cs");
        AssertFileExists("src/backend/Hosts/Api/Grpc/ProcurementQueryGrpcService.cs");
    }

    [Fact]
    public void Official_transport_packages_are_centrally_pinned()
    {
        var packagesPath = Path.Combine(
            RepositoryRoot.FullName,
            "src",
            "backend",
            "Directory.Packages.props");
        var document = XDocument.Load(packagesPath);
        var versions = document
            .Descendants("PackageVersion")
            .ToDictionary(
                element => element.Attribute("Include")?.Value ?? string.Empty,
                element => element.Attribute("Version")?.Value ?? string.Empty,
                StringComparer.Ordinal);

        Assert.Equal("7.2.2", versions["RabbitMQ.Client"]);
        Assert.Equal("2.80.0", versions["Grpc.AspNetCore"]);
        Assert.Equal("2.80.0", versions["Grpc.AspNetCore.HealthChecks"]);
        Assert.Equal("2.80.0", versions["Grpc.Net.ClientFactory"]);
        Assert.Equal("2.83.0", versions["Grpc.Tools"]);
        Assert.Equal("3.35.1", versions["Google.Protobuf"]);
    }

    [Fact]
    public void Contract_package_includes_manifest_schema_guidance_and_protobuf_sources()
    {
        var contractsProject = XDocument.Load(Path.Combine(
            RepositoryRoot.FullName,
            "src/backend/Core/Contracts/Contracts.csproj"));

        Assert.Equal("README.md", contractsProject.Descendants("PackageReadmeFile").Single().Value);

        var packedFiles = contractsProject
            .Descendants("None")
            .Where(element => string.Equals(element.Attribute("Pack")?.Value, "true", StringComparison.OrdinalIgnoreCase))
            .Select(element => element.Attribute("Include")?.Value)
            .OfType<string>()
            .ToArray();

        Assert.Contains("README.md", packedFiles);
        Assert.Contains("integration-manifest.json", packedFiles);
        Assert.Contains("integration-manifest.schema.json", packedFiles);
        Assert.Contains("Protos\\procurement\\v1\\procurement_query.proto", packedFiles);
        Assert.Contains("Protos\\vendor\\v1\\vendor_directory.proto", packedFiles);
    }

    [Fact]
    public void Integration_manifest_declares_both_transport_directions()
    {
        var manifestPath = Path.Combine(
            RepositoryRoot.FullName,
            "src",
            "backend",
            "Core",
            "Contracts",
            "integration-manifest.json");
        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.Equal(
            CatalogEvents(IntegrationContractCatalog.Published),
            ManifestEvents(root, "publishes"));
        Assert.Equal(
            CatalogEvents(IntegrationContractCatalog.Subscribed),
            ManifestEvents(root, "subscribes"));
        Assert.Equal(
            CatalogGrpcMethods(GrpcContractCatalog.Provided),
            ManifestGrpcMethods(root, "grpcProvides"));
        Assert.Equal(
            CatalogGrpcMethods(GrpcContractCatalog.Consumed),
            ManifestGrpcMethods(root, "grpcConsumes"));
    }

    [Fact]
    public void Integration_manifest_is_valid_against_its_published_schema()
    {
        var contractsDirectory = Path.Combine(
            RepositoryRoot.FullName,
            "src",
            "backend",
            "Core",
            "Contracts");
        using var schemaDocument = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(contractsDirectory, "integration-manifest.schema.json")));
        using var manifestDocument = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(contractsDirectory, "integration-manifest.json")));

        var errors = JsonSchemaSubsetValidator.Validate(
            schemaDocument.RootElement,
            manifestDocument.RootElement);

        Assert.Empty(errors);

        var invalidManifest = JsonNode.Parse(manifestDocument.RootElement.GetRawText())!.AsObject();
        invalidManifest.Remove("publishes");
        using var invalidDocument = JsonDocument.Parse(invalidManifest.ToJsonString());
        var invalidErrors = JsonSchemaSubsetValidator.Validate(
            schemaDocument.RootElement,
            invalidDocument.RootElement);

        Assert.Contains(invalidErrors, error => error.Contains("publishes", StringComparison.Ordinal));
    }

    [Fact]
    public void Transport_and_persistence_adapters_do_not_leak_into_core_projects()
    {
        var contractsProject = XDocument.Load(Path.Combine(
            RepositoryRoot.FullName,
            "src/backend/Core/Contracts/Contracts.csproj"));
        Assert.Empty(contractsProject.Descendants("ProjectReference"));

        var applicationProject = File.ReadAllText(Path.Combine(
            RepositoryRoot.FullName,
            "src/backend/Core/Application/Application.csproj"));
        Assert.DoesNotContain("Infrastructure", applicationProject, StringComparison.OrdinalIgnoreCase);

        var integrationProject = File.ReadAllText(Path.Combine(
            RepositoryRoot.FullName,
            "src/backend/Infrastructure/Integration/Integration.csproj"));
        Assert.DoesNotContain("Persistence.csproj", integrationProject, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Grpc_retries_are_scoped_to_the_explicit_read_only_method()
    {
        var registrationSource = File.ReadAllText(Path.Combine(
            RepositoryRoot.FullName,
            "src/backend/Infrastructure/Integration/ServiceIntegrationServiceCollectionExtensions.cs"));

        Assert.DoesNotContain("MethodName.Default", registrationSource, StringComparison.Ordinal);
        Assert.Contains("VendorDirectory.Descriptor.FullName", registrationSource, StringComparison.Ordinal);
        Assert.Contains("Method = \"GetVendorSnapshot\"", registrationSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Api_container_exposes_dedicated_http2_grpc_endpoint()
    {
        var dockerfile = File.ReadAllText(Path.Combine(
            RepositoryRoot.FullName,
            "build/Dockerfile.api"));

        Assert.Contains("Kestrel__Endpoints__Http__Protocols=Http1", dockerfile, StringComparison.Ordinal);
        Assert.Contains("Kestrel__Endpoints__Grpc__Url=http://+:8081", dockerfile, StringComparison.Ordinal);
        Assert.Contains("Kestrel__Endpoints__Grpc__Protocols=Http2", dockerfile, StringComparison.Ordinal);
        Assert.Contains("EXPOSE 8080 8081", dockerfile, StringComparison.Ordinal);
        Assert.Contains("Core/Contracts/Contracts.csproj", dockerfile, StringComparison.Ordinal);
        Assert.Contains("Infrastructure/Integration/Integration.csproj", dockerfile, StringComparison.Ordinal);
        Assert.Contains("src/frontend/apps/main/public/test-profiles.json", dockerfile, StringComparison.Ordinal);
    }

    private static void AssertFileExists(string relativePath)
    {
        Assert.True(
            File.Exists(Path.Combine(RepositoryRoot.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar))),
            $"Required service-integration file is missing: {relativePath}");
    }

    private static string[] CatalogEvents(
        IReadOnlyList<IntegrationContractDescriptor> contracts) =>
        contracts
            .Select(contract =>
                $"{contract.Name}|{contract.Version}|{contract.ContractType.FullName}|application/json")
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static string[] ManifestEvents(JsonElement root, string propertyName) =>
        root.GetProperty(propertyName)
            .EnumerateArray()
            .Select(contract =>
                $"{contract.GetProperty("name").GetString()}|"
                + $"{contract.GetProperty("version").GetInt32()}|"
                + $"{contract.GetProperty("contract").GetString()}|"
                + contract.GetProperty("contentType").GetString())
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static string[] CatalogGrpcMethods(
        IReadOnlyList<GrpcContractDescriptor> contracts) =>
        contracts
            .SelectMany(contract => contract.Methods.Select(method =>
                $"{contract.Service}|{contract.ProtoPath}|{method}"))
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static string[] ManifestGrpcMethods(JsonElement root, string propertyName) =>
        root.GetProperty(propertyName)
            .EnumerateArray()
            .SelectMany(contract => contract.GetProperty("methods")
                .EnumerateArray()
                .Select(method =>
                    $"{contract.GetProperty("service").GetString()}|"
                    + $"{contract.GetProperty("contract").GetString()}|"
                    + method.GetString()))
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new InvalidOperationException("Repository root could not be located.");
    }
}

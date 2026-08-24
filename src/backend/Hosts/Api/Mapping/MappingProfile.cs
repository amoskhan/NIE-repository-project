using Application.Contracts;
using Domain.Models;
using Mapster;

namespace Api.Mapping;

/// <summary>
/// Configures Mapster type mappings for the application.
/// Add all custom mappings here for entities and DTOs.
/// </summary>
public static class MappingConfig
{
    /// <summary>
    /// Registers all Mapster type mappings.
    /// Called during application startup.
    /// </summary>
    public static void RegisterMappings()
    {
        // Code mappings
        TypeAdapterConfig<Code, CodeDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id.ToString());

        TypeAdapterConfig<CodeDto, Code>.NewConfig()
            .Map(dest => dest.Id, src => ParseInt(src.Id));

        // Document mappings
        TypeAdapterConfig<Document, DocumentDto>.NewConfig();
        TypeAdapterConfig<DocumentDto, Document>.NewConfig();

        // === SAMPLE: procurement mappings (reference vertical; remove only after approved replacement) ===
        // Vendor mappings
        TypeAdapterConfig<Vendor, VendorDto>.NewConfig();
        TypeAdapterConfig<VendorDto, Vendor>.NewConfig();

        // CatalogItem mappings
        TypeAdapterConfig<CatalogItem, CatalogItemDto>.NewConfig()
            .Map(dest => dest.VendorName, src => src.Vendor != null ? src.Vendor.Name : null);
        TypeAdapterConfig<CatalogItemDto, CatalogItem>.NewConfig();

        // PurchaseOrder mappings
        TypeAdapterConfig<PurchaseOrder, PurchaseOrderDto>.NewConfig()
            .Map(dest => dest.VendorName, src => src.Vendor != null ? src.Vendor.Name : null)
            .Map(dest => dest.Lines, src => src.Lines.Adapt<List<PurchaseOrderLineDto>>())
            .Map(dest => dest.Approvals, src => src.Approvals.Adapt<List<PurchaseOrderApprovalDto>>())
            .Map(dest => dest.Documents, src => src.Documents.Adapt<List<PurchaseOrderDocumentDto>>());
        TypeAdapterConfig<PurchaseOrderDto, PurchaseOrder>.NewConfig();

        // PurchaseOrderLine mappings
        TypeAdapterConfig<PurchaseOrderLine, PurchaseOrderLineDto>.NewConfig();
        TypeAdapterConfig<PurchaseOrderLineDto, PurchaseOrderLine>.NewConfig();

        // PurchaseOrderApproval mappings
        TypeAdapterConfig<PurchaseOrderApproval, PurchaseOrderApprovalDto>.NewConfig();
        TypeAdapterConfig<PurchaseOrderApprovalDto, PurchaseOrderApproval>.NewConfig();

        // PurchaseOrderDocument mappings
        TypeAdapterConfig<PurchaseOrderDocument, PurchaseOrderDocumentDto>.NewConfig();
        TypeAdapterConfig<PurchaseOrderDocumentDto, PurchaseOrderDocument>.NewConfig();
        // === END SAMPLE ===
    }

    /// <summary>
    /// Helper method to parse string to int, returning 0 if parsing fails.
    /// Required because expression trees cannot contain out variable declarations.
    /// </summary>
    private static int ParseInt(string? value)
    {
        return int.TryParse(value, out var result) ? result : 0;
    }
}

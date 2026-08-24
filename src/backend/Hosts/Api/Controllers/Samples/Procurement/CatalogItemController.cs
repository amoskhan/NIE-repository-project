using Api.Authorization;
using Application.Contracts;
using Application.Features.CatalogItem;
using Application.Security;
using Domain.Models;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

public class CatalogItemController : BaseController
{
    private readonly ICatalogItemService _catalogItemService;
    private readonly IMapper _mapper;
    private readonly ILogger<CatalogItemController> _logger;

    public CatalogItemController(
        ICatalogItemService catalogItemService,
        IMapper mapper,
        ILogger<CatalogItemController> logger)
    {
        _catalogItemService = catalogItemService;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementCatalogRead)]
    public async Task<ActionResult<IEnumerable<CatalogItemDto>>> GetAll()
    {
        var items = await _catalogItemService.GetAllAsync();
        return Ok(_mapper.Map<List<CatalogItemDto>>(items));
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementCatalogRead)]
    public async Task<ActionResult<DataTablePageDto<CatalogItemDto>>> Search(
        [FromBody] DataTableRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await _catalogItemService.SearchTableAsync(request, cancellationToken));

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementCatalogRead)]
    public async Task<ActionResult<DataTableFilterOptionPageDto>> GetFilterOptions(
        [FromBody] DataTableFilterOptionsRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await _catalogItemService.GetFilterOptionsAsync(request, cancellationToken));

    [HttpGet("{vendorId}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementCatalogRead)]
    public async Task<ActionResult<IEnumerable<CatalogItemDto>>> GetByVendor(Guid vendorId)
    {
        var items = await _catalogItemService.GetByVendorAsync(vendorId);
        var dtos = items.Select(i =>
        {
            var dto = _mapper.Map<CatalogItemDto>(i);
            dto.VendorName = i.Vendor?.Name;
            return dto;
        }).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementCatalogRead)]
    public async Task<ActionResult<CatalogItemDto>> Get(Guid id)
    {
        var item = await _catalogItemService.GetByIdAsync(id, c => c.Vendor);
        if (item == null) return NotFound("Catalog item not found");
        var dto = _mapper.Map<CatalogItemDto>(item);
        dto.VendorName = item.Vendor?.Name;
        return Ok(dto);
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementCatalogManage)]
    public async Task<ActionResult<CatalogItemDto>> Save([FromBody] CatalogItemDto dto)
    {
        var entity = _mapper.Map<CatalogItem>(dto);
        var saved = await _catalogItemService.SaveAsync(entity);
        _logger.LogInformation("Created catalog item {Id}", saved.Id);
        return Ok(_mapper.Map<CatalogItemDto>(saved));
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementCatalogManage)]
    public async Task<ActionResult<CatalogItemDto>> Edit([FromBody] CatalogItemDto dto)
    {
        if (dto.Id == Guid.Empty) return BadRequest("Invalid ID");
        var existing = await _catalogItemService.GetByIdAsync(dto.Id);
        if (existing == null) return NotFound("Catalog item not found");

        existing.Name = dto.Name;
        existing.Sku = dto.Sku;
        existing.Description = dto.Description;
        existing.Category = dto.Category;
        existing.UnitOfMeasure = dto.UnitOfMeasure;
        existing.UnitPrice = dto.UnitPrice;
        existing.IsActive = dto.IsActive;
        existing.VendorId = dto.VendorId;

        var updated = await _catalogItemService.SaveOrUpdateAsync(existing);
        _logger.LogInformation("Updated catalog item {Id}", updated.Id);
        return Ok(_mapper.Map<CatalogItemDto>(updated));
    }

    [HttpPost("Delete/{id}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementCatalogManage)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var deleted = await _catalogItemService.DeleteAsync(id);
        if (!deleted) return NotFound("Catalog item not found");
        _logger.LogInformation("Deleted catalog item {Id}", id);
        return Ok();
    }
}

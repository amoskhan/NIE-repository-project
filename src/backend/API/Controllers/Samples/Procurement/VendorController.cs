using API.Authorization;
using Shared.Dto;
using Domain.Models;
using Shared.Security;
using Services.Services.Vendor;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class VendorController : BaseController
{
    private readonly IVendorService _vendorService;
    private readonly IMapper _mapper;
    private readonly ILogger<VendorController> _logger;

    public VendorController(
        IVendorService vendorService,
        IMapper mapper,
        ILogger<VendorController> logger)
    {
        _vendorService = vendorService;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementVendorRead)]
    public async Task<ActionResult<IEnumerable<VendorDto>>> GetAll()
    {
        var vendors = await _vendorService.GetAllWithCatalogCountAsync();
        var dtos = vendors.Select(v =>
        {
            var dto = _mapper.Map<VendorDto>(v);
            dto.CatalogItemCount = v.CatalogItems.Count;
            return dto;
        }).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementVendorRead)]
    public async Task<ActionResult<VendorDto>> Get(int id)
    {
        var vendor = await _vendorService.GetByIdAsync(id, v => v.CatalogItems);
        if (vendor == null) return NotFound("Vendor not found");
        var dto = _mapper.Map<VendorDto>(vendor);
        dto.CatalogItemCount = vendor.CatalogItems.Count;
        return Ok(dto);
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementVendorManage)]
    public async Task<ActionResult<VendorDto>> Save([FromBody] VendorDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required");
        if (string.IsNullOrWhiteSpace(dto.Code)) return BadRequest("Code is required");

        var entity = _mapper.Map<Vendor>(dto);
        var saved = await _vendorService.SaveAsync(entity);
        _logger.LogInformation("Created vendor {Id}", saved.Id);
        return Ok(_mapper.Map<VendorDto>(saved));
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementVendorManage)]
    public async Task<ActionResult<VendorDto>> Edit([FromBody] VendorDto dto)
    {
        if (dto.Id <= 0) return BadRequest("Invalid ID");
        var existing = await _vendorService.GetByIdAsync(dto.Id);
        if (existing == null) return NotFound("Vendor not found");

        existing.Name = dto.Name;
        existing.Code = dto.Code;
        existing.ContactPerson = dto.ContactPerson;
        existing.Email = dto.Email;
        existing.Phone = dto.Phone;
        existing.Address = dto.Address;
        existing.Category = dto.Category;
        existing.IsActive = dto.IsActive;
        existing.Notes = dto.Notes;

        var updated = await _vendorService.SaveOrUpdateAsync(existing);
        _logger.LogInformation("Updated vendor {Id}", updated.Id);
        return Ok(_mapper.Map<VendorDto>(updated));
    }

    [HttpPost("Delete/{id}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ProcurementVendorManage)]
    public async Task<ActionResult> Delete(int id)
    {
        var deleted = await _vendorService.DeleteAsync(id);
        if (!deleted) return NotFound("Vendor not found");
        _logger.LogInformation("Deleted vendor {Id}", id);
        return Ok();
    }
}

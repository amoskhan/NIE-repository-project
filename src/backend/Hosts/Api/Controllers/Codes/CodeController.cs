using Api.Authorization;
using Application.Contracts;
using Application.Features.Code;
using Application.Security;
using Domain.Enums;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Controller for managing code lookup values.
/// </summary>
public class CodeController : BaseController
{
    private readonly ICodeService _codeService;
    private readonly IMapper _mapper;
    private readonly ILogger<CodeController> _logger;

    public CodeController(
        ICodeService codeService,
        IMapper mapper,
        ILogger<CodeController> logger)
    {
        _codeService = codeService;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.CodeRead)]
    public async Task<ActionResult<IEnumerable<CodeDto>>> GetAll()
    {
        var allCodes = await _codeService.GetAllAsync();
        return Ok(_mapper.Map<List<CodeDto>>(allCodes));
    }

    [HttpGet]
    [RequireAccessFunction(AccessFunctionCodes.Api.CodeRead)]
    public async Task<ActionResult<IEnumerable<CodeDto>>> GetAllByCodeType(ECodeType codeType)
    {
        _logger.LogDebug("Getting codes by type: {CodeType}", codeType);

        var allCodes = await _codeService.GetAllByCodeType(codeType);
        return Ok(_mapper.Map<List<CodeDto>>(allCodes));
    }
}

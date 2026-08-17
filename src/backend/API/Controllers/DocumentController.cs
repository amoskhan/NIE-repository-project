using API.Authorization;
using Shared.Security;
using Services.Services.Document;
using Services.Services.FileStorage;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Controller for document management and file operations.
/// </summary>
public class DocumentController : BaseController
{
    private readonly IDocumentService _documentService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IMapper _mapper;
    private readonly ILogger<DocumentController> _logger;

    public DocumentController(
        IDocumentService documentService,
        IFileStorageService fileStorageService,
        IMapper mapper,
        ILogger<DocumentController> logger)
    {
        _documentService = documentService;
        _fileStorageService = fileStorageService;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet("{id}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.DocumentDownload)]
    public async Task<ActionResult> DownloadFile(int id)
    {
        var document = await _documentService.GetByIdAsync(id);
        if (document == null)
        {
            _logger.LogWarning("Document with ID {Id} not found", id);
            return NotFound("Document not found");
        }

        var (fileContents, contentType) = await _fileStorageService.GetFileAsync(document.FilePath);
        return File(fileContents, contentType, document.UserFileName);
    }

    [HttpPost]
    [RequireAccessFunction(AccessFunctionCodes.Api.DocumentManage)]
    public async Task<ActionResult<string>> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        _logger.LogInformation("Uploading file: {FileName}, Size: {FileSize}", file.FileName, file.Length);

        var filePath = await _fileStorageService.SaveFileAsync(file, file.FileName);
        return Ok(filePath);
    }

    [HttpDelete("{id}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.DocumentManage)]
    public async Task<ActionResult> DeleteFile(int id)
    {
        var document = await _documentService.GetByIdAsync(id);
        if (document == null)
        {
            _logger.LogWarning("Document with ID {Id} not found for deletion", id);
            return NotFound("Document not found");
        }

        await _fileStorageService.DeleteFileAsync(document.FilePath);
        await _documentService.DeleteAsync(id);

        _logger.LogInformation("Document with ID {Id} deleted successfully", id);
        return Ok();
    }
}

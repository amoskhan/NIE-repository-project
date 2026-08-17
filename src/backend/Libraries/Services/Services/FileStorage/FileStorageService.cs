using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Services.Services.FileStorage;

public class FileStorageService : IFileStorageService
{
    private readonly IConfiguration _configuration;
    private readonly string _fileStorageBasePath;

    public FileStorageService(IConfiguration configuration)
    {
        _configuration = configuration;
        _fileStorageBasePath = _configuration["FileStorage:BasePath"]
            ?? throw new InvalidOperationException("FileStorage:BasePath configuration is required.");

        // Ensure the directory exists
        if (!Directory.Exists(_fileStorageBasePath))
            Directory.CreateDirectory(_fileStorageBasePath);
    }

    public async Task<string> SaveFileAsync(IFormFile file, string fileName, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("No file provided or file is empty");
        }

        // Create folder structure by year/month
        var currentDateTime = Shared.Helpers.DateTimeHelper.Now;
        var folderPath = Path.Combine(_fileStorageBasePath, currentDateTime.ToString("yyyy-MM"));

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        // Generate a unique file name with original extension
        var fileExtension = Path.GetExtension(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(folderPath, uniqueFileName);

        // Save the file
        await using (var fileStream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(fileStream, ct);

        // Return relative path for storage in database
        return Path.Combine(currentDateTime.ToString("yyyy-MM"), uniqueFileName);
    }

    public async Task SaveStreamAsync(string filePath, Stream stream, string contentType, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_fileStorageBasePath, filePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream, ct);
    }

    public async Task SaveBytesAsync(string filePath, byte[] contents, string contentType, CancellationToken ct = default)
    {
        await using var stream = new MemoryStream(contents);
        await SaveStreamAsync(filePath, stream, contentType, ct);
    }

    public async Task<(byte[], string)> GetFileAsync(string filePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_fileStorageBasePath, filePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {filePath}");

        byte[] fileContents = await File.ReadAllBytesAsync(fullPath, ct);
        string contentType = FileStorageContentTypes.GetContentType(fullPath);

        return (fileContents, contentType);
    }

    public Task<(Stream stream, string contentType)> OpenReadAsync(string filePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_fileStorageBasePath, filePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {filePath}");

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult((stream, FileStorageContentTypes.GetContentType(fullPath)));
    }

    public Task<bool> ExistsAsync(string filePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_fileStorageBasePath, filePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task<string> GetFilePathAsync(string fileName)
    {
        // Create folder structure by year/month
        var currentDateTime = Shared.Helpers.DateTimeHelper.Now;
        var relativePath = currentDateTime.ToString("yyyy-MM");
        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";

        // Return relative path (to be used for future upload)
        return Task.FromResult(Path.Combine(relativePath, uniqueFileName));
    }

    public async Task<bool> DeleteFileAsync(string filePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_fileStorageBasePath, filePath);

        return await Task.Run(() =>
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }

            return false;
        });
    }
}

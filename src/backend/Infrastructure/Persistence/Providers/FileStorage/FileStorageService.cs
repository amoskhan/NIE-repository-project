using Microsoft.Extensions.Configuration;

namespace Infrastructure.Providers.FileStorage;

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

    public async Task SaveStreamAsync(string filePath, Stream stream, string contentType, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_fileStorageBasePath, filePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        await using var fileStream = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);
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
        ct.ThrowIfCancellationRequested();
        var fullPath = Path.Combine(_fileStorageBasePath, filePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {filePath}");

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);
        return Task.FromResult((stream, FileStorageContentTypes.GetContentType(fullPath)));
    }

    public Task<bool> ExistsAsync(string filePath, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var fullPath = Path.Combine(_fileStorageBasePath, filePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task<string> GetFilePathAsync(string fileName)
    {
        // Create folder structure by year/month
        var currentDateTime = BuildingBlocks.Helpers.DateTimeHelper.Now;
        var relativePath = currentDateTime.ToString("yyyy-MM");
        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";

        // Return relative path (to be used for future upload)
        return Task.FromResult(Path.Combine(relativePath, uniqueFileName));
    }

    public Task<bool> DeleteFileAsync(string filePath, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var fullPath = Path.Combine(_fileStorageBasePath, filePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}

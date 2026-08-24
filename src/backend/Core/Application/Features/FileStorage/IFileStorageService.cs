namespace Application.Features.FileStorage;

public interface IFileStorageService
{
    Task SaveStreamAsync(string filePath, Stream stream, string contentType, CancellationToken ct = default);
    Task SaveBytesAsync(string filePath, byte[] contents, string contentType, CancellationToken ct = default);
    Task<(byte[], string)> GetFileAsync(string filePath, CancellationToken ct = default);
    Task<(Stream stream, string contentType)> OpenReadAsync(string filePath, CancellationToken ct = default);
    Task<bool> ExistsAsync(string filePath, CancellationToken ct = default);
    Task<string> GetFilePathAsync(string fileName);
    Task<bool> DeleteFileAsync(string filePath, CancellationToken ct = default);
}

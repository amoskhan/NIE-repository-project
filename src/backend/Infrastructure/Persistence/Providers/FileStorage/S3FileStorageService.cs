using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Providers.FileStorage;

public class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucketName;
    private readonly string _keyPrefix;

    public S3FileStorageService(IConfiguration configuration)
    {
        _bucketName = configuration["FileStorage:S3:BucketName"]
            ?? throw new InvalidOperationException("FileStorage:S3:BucketName configuration is required.");

        _keyPrefix = NormalizePrefix(configuration["FileStorage:S3:KeyPrefix"]);
        var region = configuration["FileStorage:S3:Region"]
            ?? configuration["AWS:Region"]
            ?? Environment.GetEnvironmentVariable("AWS_REGION")
            ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION")
            ?? "ap-southeast-1";

        // Optional custom endpoint (e.g. LocalStack / MinIO). When set, use path-style
        // addressing; real AWS leaves ServiceUrl empty and uses the region endpoint.
        var serviceUrl = configuration["FileStorage:S3:ServiceUrl"];
        var config = new AmazonS3Config();
        if (!string.IsNullOrWhiteSpace(serviceUrl))
        {
            config.ServiceURL = serviceUrl;
            config.ForcePathStyle = true;
            config.AuthenticationRegion = region;
        }
        else
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(region);
        }

        // Optional explicit credentials (e.g. LocalStack's test creds). When omitted, the
        // AWS default credential chain is used (IAM role / env vars / shared profile).
        var accessKey = configuration["FileStorage:S3:AccessKey"];
        var secretKey = configuration["FileStorage:S3:SecretKey"];
        _s3 = !string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey)
            ? new AmazonS3Client(accessKey, secretKey, config)
            : new AmazonS3Client(config);
    }

    public async Task SaveStreamAsync(string filePath, Stream stream, string contentType, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = BuildKey(filePath),
            InputStream = stream,
            ContentType = string.IsNullOrWhiteSpace(contentType)
                ? FileStorageContentTypes.GetContentType(filePath)
                : contentType,
            AutoCloseStream = false,
        };

        await _s3.PutObjectAsync(request, ct);
    }

    public async Task SaveBytesAsync(string filePath, byte[] contents, string contentType, CancellationToken ct = default)
    {
        await using var stream = new MemoryStream(contents);
        await SaveStreamAsync(filePath, stream, contentType, ct);
    }

    public async Task<(byte[], string)> GetFileAsync(string filePath, CancellationToken ct = default)
    {
        await using var stream = (await OpenReadAsync(filePath, ct)).stream;
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, ct);
        return (memory.ToArray(), FileStorageContentTypes.GetContentType(filePath));
    }

    public async Task<(Stream stream, string contentType)> OpenReadAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            var response = await _s3.GetObjectAsync(_bucketName, BuildKey(filePath), ct);
            var contentType = string.IsNullOrWhiteSpace(response.Headers.ContentType)
                ? FileStorageContentTypes.GetContentType(filePath)
                : response.Headers.ContentType;

            return (new S3ObjectStream(response), contentType);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
        {
            throw new FileNotFoundException($"File not found: {filePath}", ex);
        }
    }

    public async Task<bool> ExistsAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            await _s3.GetObjectMetadataAsync(_bucketName, BuildKey(filePath), ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound || ex.ErrorCode == "NotFound" || ex.ErrorCode == "NoSuchKey")
        {
            return false;
        }
    }

    public Task<string> GetFilePathAsync(string fileName)
    {
        var currentDateTime = BuildingBlocks.Helpers.DateTimeHelper.Now;
        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        return Task.FromResult($"{currentDateTime:yyyy-MM}/{uniqueFileName}");
    }

    public async Task<bool> DeleteFileAsync(string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return false;

        await _s3.DeleteObjectAsync(_bucketName, BuildKey(filePath), ct);
        return true;
    }

    private string BuildKey(string filePath)
    {
        var normalizedPath = NormalizePath(filePath);
        return string.IsNullOrEmpty(_keyPrefix) ? normalizedPath : $"{_keyPrefix}{normalizedPath}";
    }

    private static string NormalizePrefix(string? prefix)
    {
        var normalized = NormalizePath(prefix ?? string.Empty).Trim('/');
        return string.IsNullOrEmpty(normalized) ? string.Empty : $"{normalized}/";
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }

    private sealed class S3ObjectStream : Stream
    {
        private readonly GetObjectResponse _response;

        public S3ObjectStream(GetObjectResponse response)
        {
            _response = response;
        }

        private Stream Inner => _response.ResponseStream;

        public override bool CanRead => Inner.CanRead;
        public override bool CanSeek => Inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => Inner.Length;

        public override long Position
        {
            get => Inner.Position;
            set => Inner.Position = value;
        }

        public override void Flush() => Inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => Inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => Inner.Seek(offset, origin);
        public override void SetLength(long value) => Inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Inner.ReadAsync(buffer, offset, count, cancellationToken);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => Inner.ReadAsync(buffer, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _response.Dispose();

            base.Dispose(disposing);
        }
    }
}

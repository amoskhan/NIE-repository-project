using Infrastructure.Providers.FileStorage;
using Microsoft.Extensions.Configuration;

namespace Integration.Tests;

public sealed class LocalFileStorageServiceTests
{
    [Fact]
    public async Task Local_storage_saves_reads_and_deletes_a_file()
    {
        var basePath = CreateUniqueBasePath();
        try
        {
            var service = CreateService(basePath);
            var contents = "local-storage-test"u8.ToArray();

            await service.SaveBytesAsync("nested/file.txt", contents, "text/plain", TestContext.Current.CancellationToken);

            var (storedContents, contentType) = await service.GetFileAsync(
                "nested/file.txt",
                TestContext.Current.CancellationToken);
            Assert.Equal(contents, storedContents);
            Assert.Equal("text/plain", contentType);
            Assert.True(await service.DeleteFileAsync("nested/file.txt", TestContext.Current.CancellationToken));
            Assert.False(await service.ExistsAsync("nested/file.txt", TestContext.Current.CancellationToken));
            Assert.False(await service.DeleteFileAsync("nested/file.txt", TestContext.Current.CancellationToken));
        }
        finally
        {
            if (Directory.Exists(basePath))
            {
                Directory.Delete(basePath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Local_storage_honours_cancellation_before_synchronous_file_operations()
    {
        var basePath = CreateUniqueBasePath();
        try
        {
            var service = CreateService(basePath);
            using var cancellationSource = new CancellationTokenSource();
            await cancellationSource.CancelAsync();

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => service.DeleteFileAsync("file.txt", cancellationSource.Token));
        }
        finally
        {
            if (Directory.Exists(basePath))
            {
                Directory.Delete(basePath, recursive: true);
            }
        }
    }

    private static FileStorageService CreateService(string basePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileStorage:BasePath"] = basePath,
            })
            .Build();

        return new FileStorageService(configuration);
    }

    private static string CreateUniqueBasePath()
    {
        return Path.Combine(Path.GetTempPath(), "nie-template-tests", Guid.NewGuid().ToString("N"));
    }
}

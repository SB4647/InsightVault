using System.Text;
using System.Net;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using InsightVault.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace InsightVault.Tests.Infrastructure;

public sealed class S3BlobStorageServiceTests
{
    [Fact]
    public async Task UploadAsync_StoresAndDownloadsDocumentFromLocalS3Emulator()
    {
        await using var s3Mock = CreateS3Mock();
        await s3Mock.StartAsync();

        using var storage = CreateStorage(s3Mock);
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("local S3 evidence"));

        await storage.UploadAsync("documents/evidence.pdf", content, "application/pdf");
        await using var downloaded = await storage.DownloadAsync("documents/evidence.pdf");
        using var reader = new StreamReader(downloaded);

        Assert.Equal("local S3 evidence", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task DeleteAsync_RemovesDocumentFromLocalS3Emulator()
    {
        await using var s3Mock = CreateS3Mock();
        await s3Mock.StartAsync();

        using var storage = CreateStorage(s3Mock);
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("delete me"));
        await storage.UploadAsync("documents/remove.pdf", content, "application/pdf");

        await storage.DeleteAsync("documents/remove.pdf");

        await Assert.ThrowsAnyAsync<Exception>(() => storage.DownloadAsync("documents/remove.pdf"));
    }

    private static IContainer CreateS3Mock()
    {
        return new ContainerBuilder("adobe/s3mock:5.2.2")
            .WithPortBinding(9090, true)
            .WithEnvironment("COM_ADOBE_TESTING_S3MOCK_STORE_INITIAL_BUCKETS", "insightvault-tests")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request
                .ForPort(9090)
                .ForPath("/favicon.ico")
                .ForStatusCode(HttpStatusCode.OK)))
            .Build();
    }

    private static S3BlobStorageService CreateStorage(IContainer s3Mock)
    {
        return new S3BlobStorageService(Options.Create(new S3StorageOptions
        {
            BucketName = "insightvault-tests",
            ServiceUrl = $"http://{s3Mock.Hostname}:{s3Mock.GetMappedPublicPort(9090)}",
            AccessKey = "insightvault",
            SecretKey = "InsightVault-Local-Only-Password-123!",
            ForcePathStyle = true
        }));
    }
}

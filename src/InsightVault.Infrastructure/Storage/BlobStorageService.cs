using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using InsightVault.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace InsightVault.Infrastructure.Storage;

public sealed class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;

    public BlobStorageService(IOptions<BlobStorageOptions> options)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            throw new InvalidOperationException("AzureBlobStorage:ConnectionString is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.ContainerName))
        {
            throw new InvalidOperationException("AzureBlobStorage:ContainerName is required.");
        }

        _containerClient = new BlobContainerClient(
            settings.ConnectionString,
            settings.ContainerName);
    }

    public async Task UploadAsync(
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await _containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(
            content,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken);
    }

    public async Task<Stream> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        var stream = new MemoryStream();

        await blobClient.DownloadToAsync(stream, cancellationToken);
        stream.Position = 0;

        return stream;
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}

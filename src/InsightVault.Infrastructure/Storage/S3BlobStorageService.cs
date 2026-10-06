using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using InsightVault.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace InsightVault.Infrastructure.Storage;

/// <summary>
/// Stores private document content in Amazon S3 or an explicitly configured S3-compatible local emulator.
/// </summary>
public sealed class S3BlobStorageService : IBlobStorageService, IDisposable
{
    private readonly IAmazonS3 _client;
    private readonly string _bucketName;

    /// <summary>
    /// Creates an S3 storage client that uses local emulator credentials only when a local service URL is configured.
    /// </summary>
    public S3BlobStorageService(IOptions<S3StorageOptions> options)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.BucketName))
        {
            throw new InvalidOperationException("S3Storage:BucketName is required.");
        }

        _bucketName = settings.BucketName;
        _client = CreateClient(settings);
    }

    /// <summary>
    /// Uploads a document stream to the configured private bucket without closing the caller's stream.
    /// </summary>
    public async Task UploadAsync(
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = blobName,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        }, cancellationToken);
    }

    /// <summary>
    /// Downloads an S3 object into a caller-owned memory stream.
    /// </summary>
    public async Task<Stream> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetObjectAsync(_bucketName, blobName, cancellationToken);
        var stream = new MemoryStream();
        await response.ResponseStream.CopyToAsync(stream, cancellationToken);
        stream.Position = 0;

        return stream;
    }

    /// <summary>
    /// Deletes a document object when it exists; deleting an already absent object is safe.
    /// </summary>
    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        await _client.DeleteObjectAsync(_bucketName, blobName, cancellationToken);
    }

    /// <summary>
    /// Releases the S3 client created for this storage service.
    /// </summary>
    public void Dispose()
    {
        _client.Dispose();
    }

    private static IAmazonS3 CreateClient(S3StorageOptions settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.ServiceUrl))
        {
            if (string.IsNullOrWhiteSpace(settings.AccessKey) || string.IsNullOrWhiteSpace(settings.SecretKey))
            {
                throw new InvalidOperationException(
                    "S3Storage:AccessKey and S3Storage:SecretKey are required when S3Storage:ServiceUrl is configured.");
            }

            return new AmazonS3Client(
                new BasicAWSCredentials(settings.AccessKey, settings.SecretKey),
                new AmazonS3Config
                {
                    ServiceURL = settings.ServiceUrl,
                    ForcePathStyle = settings.ForcePathStyle
                });
        }

        return new AmazonS3Client(new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(settings.Region)
        });
    }
}

namespace InsightVault.Infrastructure.Storage;

/// <summary>
/// Configures Amazon S3 or an S3-compatible local endpoint for document storage.
/// </summary>
public sealed class S3StorageOptions
{
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "ap-southeast-2";
    public string ServiceUrl { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public bool ForcePathStyle { get; set; }
}

namespace InsightVault.Infrastructure.Storage;

public sealed class BlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "documents";
}

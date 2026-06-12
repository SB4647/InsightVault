namespace InsightVault.Application.Interfaces;

public interface IBlobStorageService
{
    Task UploadAsync(
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);
}

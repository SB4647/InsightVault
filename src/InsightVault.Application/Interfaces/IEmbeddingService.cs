namespace InsightVault.Application.Interfaces;

public interface IEmbeddingService
{
    Task<IReadOnlyList<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);
}

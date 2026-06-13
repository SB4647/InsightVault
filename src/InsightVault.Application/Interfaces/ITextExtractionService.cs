namespace InsightVault.Application.Interfaces;

public interface ITextExtractionService
{
    Task<string> ExtractTextAsync(Stream document, CancellationToken cancellationToken = default);
}

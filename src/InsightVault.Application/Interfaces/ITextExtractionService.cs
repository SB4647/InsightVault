namespace InsightVault.Application.Interfaces;

public interface ITextExtractionService
{
    /// <summary>
    /// Extracts page-aware text so downstream chunks can retain an auditable source location.
    /// </summary>
    Task<IReadOnlyList<ExtractedDocumentPage>> ExtractPagesAsync(
        Stream document,
        CancellationToken cancellationToken = default);
}

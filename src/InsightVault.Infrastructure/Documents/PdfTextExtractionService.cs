using InsightVault.Application.Interfaces;
using UglyToad.PdfPig;

namespace InsightVault.Infrastructure.Documents;

public sealed class PdfTextExtractionService : ITextExtractionService
{
    /// <summary>
    /// Extracts text separately for every PDF page so downstream citations can identify their source page.
    /// </summary>
    public Task<IReadOnlyList<ExtractedDocumentPage>> ExtractPagesAsync(
        Stream document,
        CancellationToken cancellationToken = default)
    {
        using var pdf = PdfDocument.Open(document);
        var pages = new List<ExtractedDocumentPage>();

        foreach (var page in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            pages.Add(new ExtractedDocumentPage(page.Number, null, page.Text));
        }

        return Task.FromResult<IReadOnlyList<ExtractedDocumentPage>>(pages);
    }
}

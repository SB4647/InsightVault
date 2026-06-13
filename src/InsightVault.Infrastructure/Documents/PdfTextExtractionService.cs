using System.Text;
using InsightVault.Application.Interfaces;
using UglyToad.PdfPig;

namespace InsightVault.Infrastructure.Documents;

public sealed class PdfTextExtractionService : ITextExtractionService
{
    public Task<string> ExtractTextAsync(Stream document, CancellationToken cancellationToken = default)
    {
        using var pdf = PdfDocument.Open(document);
        var text = new StringBuilder();

        foreach (var page in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            text.AppendLine(page.Text);
        }

        return Task.FromResult(text.ToString());
    }
}

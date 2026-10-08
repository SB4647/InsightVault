using InsightVault.Application.Interfaces;

namespace InsightVault.Application.Features.Documents.Processing;

public sealed class DocumentChunkingService : IDocumentChunkingService
{
    public IReadOnlyList<DocumentTextChunk> Chunk(string text, int chunkSize, int overlapSize)
    {
        return Chunk([new ExtractedDocumentPage(1, null, text)], chunkSize, overlapSize);
    }

    /// <summary>
    /// Splits each extracted page independently to retain a reliable page citation for every chunk.
    /// </summary>
    public IReadOnlyList<DocumentTextChunk> Chunk(
        IReadOnlyList<ExtractedDocumentPage> pages,
        int chunkSize,
        int overlapSize)
    {
        if (chunkSize <= 0)
        {
            throw new ArgumentException("Chunk size must be greater than zero.", nameof(chunkSize));
        }

        if (overlapSize < 0 || overlapSize >= chunkSize)
        {
            throw new ArgumentException("Overlap size must be non-negative and smaller than chunk size.", nameof(overlapSize));
        }

        var chunks = new List<DocumentTextChunk>();
        foreach (var page in pages)
        {
            if (page.PageNumber <= 0)
            {
                throw new ArgumentException("Source page number must be positive.", nameof(pages));
            }

            var normalizedText = NormalizeWhitespace(page.Text);
            var start = 0;
            while (start < normalizedText.Length)
            {
                var length = Math.Min(chunkSize, normalizedText.Length - start);
                var end = start + length;
                if (end < normalizedText.Length)
                {
                    var lastSpace = normalizedText.LastIndexOf(' ', end - 1, length);
                    if (lastSpace > start)
                    {
                        end = lastSpace;
                    }
                }

                var chunkText = normalizedText[start..end].Trim();
                if (chunkText.Length > 0)
                {
                    chunks.Add(new DocumentTextChunk(
                        chunks.Count,
                        chunkText,
                        page.PageNumber,
                        string.IsNullOrWhiteSpace(page.SectionTitle) ? null : page.SectionTitle.Trim()));
                }

                if (end >= normalizedText.Length)
                {
                    break;
                }

                start = Math.Max(0, end - overlapSize);
                while (start < normalizedText.Length && normalizedText[start] == ' ')
                {
                    start++;
                }
            }
        }

        return chunks;
    }

    private static string NormalizeWhitespace(string text)
    {
        return string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}

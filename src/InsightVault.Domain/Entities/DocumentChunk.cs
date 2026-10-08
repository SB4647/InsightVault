namespace InsightVault.Domain.Entities;

public class DocumentChunk
{
    private DocumentChunk()
    {
    }

    private DocumentChunk(Guid documentId, int chunkIndex, string text, int sourcePageNumber, string? sectionTitle)
    {
        Id = Guid.NewGuid();
        DocumentId = documentId;
        ChunkIndex = chunkIndex;
        Text = text;
        SourcePageNumber = sourcePageNumber;
        SectionTitle = sectionTitle;
    }

    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public int ChunkIndex { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public int SourcePageNumber { get; private set; }
    public string? SectionTitle { get; private set; }
    public Embedding? Embedding { get; private set; }

    public static DocumentChunk Create(
        Guid documentId,
        int chunkIndex,
        string text,
        int sourcePageNumber = 1,
        string? sectionTitle = null)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException("Document id is required.", nameof(documentId));
        }

        if (chunkIndex < 0)
        {
            throw new ArgumentException("Chunk index cannot be negative.", nameof(chunkIndex));
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Chunk text is required.", nameof(text));
        }

        if (sourcePageNumber <= 0)
        {
            throw new ArgumentException("Source page number must be positive.", nameof(sourcePageNumber));
        }

        return new DocumentChunk(
            documentId,
            chunkIndex,
            text.Trim(),
            sourcePageNumber,
            string.IsNullOrWhiteSpace(sectionTitle) ? null : sectionTitle.Trim());
    }

    public void SetEmbedding(IReadOnlyList<float> vector)
    {
        Embedding = Entities.Embedding.Create(Id, vector);
    }
}

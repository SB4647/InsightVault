namespace InsightVault.Domain.Entities;

public class DocumentChunk
{
    private DocumentChunk()
    {
    }

    private DocumentChunk(Guid documentId, int chunkIndex, string text)
    {
        Id = Guid.NewGuid();
        DocumentId = documentId;
        ChunkIndex = chunkIndex;
        Text = text;
    }

    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public int ChunkIndex { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public Embedding? Embedding { get; private set; }

    public static DocumentChunk Create(Guid documentId, int chunkIndex, string text)
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

        return new DocumentChunk(documentId, chunkIndex, text.Trim());
    }

    public void SetEmbedding(IReadOnlyList<float> vector)
    {
        Embedding = Entities.Embedding.Create(Id, vector);
    }
}

using System.Text.Json;

namespace InsightVault.Domain.Entities;

public class Embedding
{
    private Embedding()
    {
    }

    private Embedding(Guid documentChunkId, string vectorJson)
    {
        Id = Guid.NewGuid();
        DocumentChunkId = documentChunkId;
        VectorJson = vectorJson;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid DocumentChunkId { get; private set; }
    public string VectorJson { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public static Embedding Create(Guid documentChunkId, IReadOnlyList<float> vector)
    {
        if (documentChunkId == Guid.Empty)
        {
            throw new ArgumentException("Document chunk id is required.", nameof(documentChunkId));
        }

        if (vector.Count == 0)
        {
            throw new ArgumentException("Embedding vector cannot be empty.", nameof(vector));
        }

        return new Embedding(documentChunkId, JsonSerializer.Serialize(vector));
    }

    public IReadOnlyList<float> GetVector()
    {
        return JsonSerializer.Deserialize<float[]>(VectorJson) ?? [];
    }
}

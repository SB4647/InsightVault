using InsightVault.Application.Interfaces;
using InsightVault.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace InsightVault.Infrastructure.Persistence.Repositories;

public sealed class PostgresVectorSearchRepository(
    PostgresApplicationDbContext dbContext) : IVectorSearchRepository
{
    /// <summary>
    /// Executes permission-filtered cosine similarity search inside PostgreSQL rather than loading all vectors into memory.
    /// </summary>
    public async Task<IReadOnlyList<VectorSearchMatch>> SearchAsync(
        VectorSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.QueryEmbedding.Count != PostgresApplicationDbContext.VectorDimensions)
        {
            throw new ArgumentException(
                $"Query embeddings must contain {PostgresApplicationDbContext.VectorDimensions} dimensions for PostgreSQL pgvector search.",
                nameof(request));
        }

        var queryVector = new Vector(request.QueryEmbedding.ToArray());

        var matches =
            from embedding in dbContext.Embeddings.AsNoTracking()
            join chunk in dbContext.DocumentChunks.AsNoTracking()
                on embedding.DocumentChunkId equals chunk.Id
            join document in dbContext.Documents.AsNoTracking()
                on chunk.DocumentId equals document.Id
            where EF.Property<Vector>(embedding, PostgresApplicationDbContext.VectorPropertyName) != null
                  && document.Status == DocumentProcessingStatus.Processed
                  && (document.OwnerUserId == request.OwnerUserId
                      || dbContext.DocumentPermissions.Any(permission =>
                          permission.DocumentId == document.Id
                          && permission.UserId == request.OwnerUserId))
            select new
            {
                document.Id,
                document.OriginalFileName,
                document.Version,
                ChunkId = chunk.Id,
                chunk.ChunkIndex,
                chunk.SourcePageNumber,
                chunk.SectionTitle,
                chunk.Text,
                Distance = EF.Property<Vector>(embedding, PostgresApplicationDbContext.VectorPropertyName)
                    .CosineDistance(queryVector)
            };

        return await matches
            .OrderBy(match => match.Distance)
            .ThenBy(match => match.OriginalFileName)
            .ThenBy(match => match.ChunkIndex)
            .Take(request.MaxResults)
            .Select(match => new VectorSearchMatch(
                match.Id,
                match.OriginalFileName,
                match.ChunkId,
                match.ChunkIndex,
                match.Text,
                1d - match.Distance,
                match.Version,
                match.SourcePageNumber,
                match.SectionTitle))
            .ToListAsync(cancellationToken);
    }
}

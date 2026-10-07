using InsightVault.Application.Interfaces;
using InsightVault.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InsightVault.Infrastructure.Persistence.Repositories;

/// <summary>
/// Provides the SQL Server and local EF-compatible full-text candidate path for hybrid retrieval.
/// </summary>
public sealed class DocumentFullTextSearchRepository(ApplicationDbContext dbContext) : IFullTextSearchRepository
{
    /// <summary>
    /// Filters processed, authorised chunks in the database then ranks matching terms in a provider-neutral fallback.
    /// </summary>
    public async Task<IReadOnlyList<FullTextSearchMatch>> SearchAsync(
        FullTextSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.OwnerUserId))
        {
            throw new ArgumentException("Owner user id is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            throw new ArgumentException("Search query is required.", nameof(request));
        }

        if (request.MaxResults <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request));
        }

        var terms = request.Query
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var chunks = await (
            from document in dbContext.Documents.AsNoTracking()
            join chunk in dbContext.DocumentChunks.AsNoTracking() on document.Id equals chunk.DocumentId
            where document.Status == DocumentProcessingStatus.Processed
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
                chunk.Text
            })
            .ToListAsync(cancellationToken);

        return chunks
            .Select(chunk => new
            {
                Chunk = chunk,
                Score = terms.Count(term => chunk.Text.Contains(term, StringComparison.OrdinalIgnoreCase)) / (double)terms.Length
            })
            .Where(result => result.Score > 0)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Chunk.OriginalFileName)
            .ThenBy(result => result.Chunk.ChunkIndex)
            .Take(request.MaxResults)
            .Select(result => new FullTextSearchMatch(
                result.Chunk.Id,
                result.Chunk.OriginalFileName,
                result.Chunk.ChunkId,
                result.Chunk.ChunkIndex,
                result.Chunk.Text,
                result.Score,
                result.Chunk.Version,
                result.Chunk.SourcePageNumber,
                result.Chunk.SectionTitle))
            .ToList();
    }
}

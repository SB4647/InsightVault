using System.Data;
using InsightVault.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InsightVault.Infrastructure.Persistence.Repositories;

/// <summary>
/// Executes PostgreSQL full-text candidate search while applying the same document access rules as vector retrieval.
/// </summary>
public sealed class PostgresFullTextSearchRepository(PostgresApplicationDbContext dbContext) : IFullTextSearchRepository
{
    /// <summary>
    /// Uses parameterised PostgreSQL text-search functions to return authorised processed chunks and source metadata.
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

        const string sql = """
            SELECT d."Id", d."OriginalFileName", d."Version", c."Id", c."ChunkIndex",
                   c."SourcePageNumber", c."SectionTitle", c."Text",
                   ts_rank_cd(to_tsvector('english', c."Text"), plainto_tsquery('english', @query))::double precision
            FROM "DocumentChunks" AS c
            INNER JOIN "Documents" AS d ON c."DocumentId" = d."Id"
            WHERE d."Status" = 'Processed'
              AND (d."OwnerUserId" = @ownerUserId
                   OR EXISTS (
                       SELECT 1
                       FROM "DocumentPermissions" AS p
                       WHERE p."DocumentId" = d."Id" AND p."UserId" = @ownerUserId))
              AND to_tsvector('english', c."Text") @@ plainto_tsquery('english', @query)
            ORDER BY ts_rank_cd(to_tsvector('english', c."Text"), plainto_tsquery('english', @query)) DESC,
                     d."OriginalFileName", c."ChunkIndex"
            LIMIT @maximumResults;
            """;

        var connection = dbContext.Database.GetDbConnection();
        var closeConnectionWhenDone = connection.State != ConnectionState.Open;
        if (closeConnectionWhenDone)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@ownerUserId", request.OwnerUserId.Trim());
            AddParameter(command, "@query", request.Query.Trim());
            AddParameter(command, "@maximumResults", request.MaxResults);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var matches = new List<FullTextSearchMatch>();
            while (await reader.ReadAsync(cancellationToken))
            {
                matches.Add(new FullTextSearchMatch(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetGuid(3),
                    reader.GetInt32(4),
                    reader.GetString(7),
                    reader.GetDouble(8),
                    reader.GetInt32(2),
                    reader.GetInt32(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6)));
            }

            return matches;
        }
        finally
        {
            if (closeConnectionWhenDone)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}

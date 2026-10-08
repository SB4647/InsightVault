using InsightVault.Application.Interfaces;
using InsightVault.Domain.Entities;
using InsightVault.Infrastructure.Identity;
using InsightVault.Infrastructure.Persistence;
using InsightVault.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace InsightVault.Tests.Infrastructure;

public sealed class PostgresVectorSearchTests
{
    [Fact]
    public async Task SearchAsync_RanksAuthorizedChunksInsidePostgres()
    {
        await using var database = CreateDatabase();
        await database.StartAsync();

        var options = CreateOptions(database.GetConnectionString());

        await using (var setupContext = new PostgresApplicationDbContext(options))
        {
            await setupContext.Database.MigrateAsync();
            await SeedDocumentsAsync(setupContext);
        }

        await using var searchContext = new PostgresApplicationDbContext(options);
        var repository = new PostgresVectorSearchRepository(searchContext);

        var viewerResults = await repository.SearchAsync(
            new VectorSearchRequest("viewer-1", CreateVector(1f, 0f), 10));
        var ownerResults = await repository.SearchAsync(
            new VectorSearchRequest("owner-1", CreateVector(1f, 0f), 10));
        var unrelatedResults = await repository.SearchAsync(
            new VectorSearchRequest("unrelated-1", CreateVector(1f, 0f), 10));

        var viewerResult = Assert.Single(viewerResults);
        Assert.Equal("shared.pdf", viewerResult.DocumentName);

        Assert.Collection(
            ownerResults,
            first => Assert.Equal("owner-near.pdf", first.DocumentName),
            second => Assert.Equal("owner-far.pdf", second.DocumentName));
        Assert.Empty(unrelatedResults);
    }

    [Fact]
    public async Task SearchAsync_WithWrongVectorDimension_ThrowsClearException()
    {
        var options = CreateOptions("Host=localhost;Port=5433;Database=InsightVault;Username=postgres;Password=local");
        await using var context = new PostgresApplicationDbContext(options);
        var repository = new PostgresVectorSearchRepository(context);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => repository.SearchAsync(
            new VectorSearchRequest("owner-1", [1f, 0f], 10)));

        Assert.Contains("1536", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FullTextSearchAsync_ReturnsOnlyAuthorisedProcessedChunks()
    {
        await using var database = CreateDatabase();
        await database.StartAsync();
        var options = CreateOptions(database.GetConnectionString());

        await using (var setupContext = new PostgresApplicationDbContext(options))
        {
            await setupContext.Database.MigrateAsync();
            await SeedDocumentsAsync(setupContext);
            var sharedTextDocument = CreateDocument(
                "shared-text.pdf",
                "other-1",
                CreateVector(1f, 0f),
                "shared retrieval policy");
            setupContext.Documents.Add(sharedTextDocument);
            setupContext.DocumentPermissions.Add(sharedTextDocument.ShareWithViewer("viewer-1").Permission);
            await setupContext.SaveChangesAsync();
        }

        await using var searchContext = new PostgresApplicationDbContext(options);
        var repository = new PostgresFullTextSearchRepository(searchContext);

        var viewerResults = await repository.SearchAsync(new FullTextSearchRequest("viewer-1", "shared", 10));
        var unrelatedResults = await repository.SearchAsync(new FullTextSearchRequest("unrelated-1", "shared", 10));

        var viewerResult = Assert.Single(viewerResults);
        Assert.Equal("shared-text.pdf", viewerResult.DocumentName);
        Assert.Equal(1, viewerResult.DocumentVersion);
        Assert.Equal(1, viewerResult.SourcePageNumber);
        Assert.Empty(unrelatedResults);
    }

    private static PostgreSqlContainer CreateDatabase()
    {
        return new PostgreSqlBuilder("pgvector/pgvector:0.8.0-pg17")
            .WithDatabase("InsightVault")
            .WithUsername("postgres")
            .WithPassword("InsightVault-Local-Only-Password-123!")
            .Build();
    }

    private static DbContextOptions<PostgresApplicationDbContext> CreateOptions(string connectionString)
    {
        return new DbContextOptionsBuilder<PostgresApplicationDbContext>()
            .UseNpgsql(connectionString, postgres => postgres.UseVector())
            .Options;
    }

    private static async Task SeedDocumentsAsync(PostgresApplicationDbContext context)
    {
        context.Users.AddRange(
            CreateUser("owner-1"),
            CreateUser("viewer-1"),
            CreateUser("other-1"),
            CreateUser("unrelated-1"));

        var ownerNear = CreateDocument("owner-near.pdf", "owner-1", CreateVector(1f, 0f));
        var ownerFar = CreateDocument("owner-far.pdf", "owner-1", CreateVector(0f, 1f));
        var shared = CreateDocument("shared.pdf", "other-1", CreateVector(0.9f, 0.1f));
        context.DocumentPermissions.Add(shared.ShareWithViewer("viewer-1").Permission);

        var privateDocument = CreateDocument("private.pdf", "other-1", CreateVector(1f, 0f));
        var withoutEmbedding = Document.Create(
            "without-embedding.pdf",
            "application/pdf",
            100,
            "documents/without-embedding.pdf",
            DateTime.UtcNow,
            "owner-1");
        withoutEmbedding.CompleteProcessing([
            DocumentChunk.Create(withoutEmbedding.Id, 0, "no embedding")
        ]);

        context.Documents.AddRange(ownerNear, ownerFar, shared, privateDocument, withoutEmbedding);
        await context.SaveChangesAsync();
    }

    private static ApplicationUser CreateUser(string id)
    {
        return new ApplicationUser
        {
            Id = id,
            UserName = $"{id}@example.com",
            NormalizedUserName = $"{id}@example.com".ToUpperInvariant(),
            Email = $"{id}@example.com",
            NormalizedEmail = $"{id}@example.com".ToUpperInvariant(),
            EmailConfirmed = true
        };
    }

    private static Document CreateDocument(
        string name,
        string ownerUserId,
        IReadOnlyList<float> vector,
        string? chunkText = null)
    {
        var document = Document.Create(
            name,
            "application/pdf",
            100,
            $"documents/{name}",
            DateTime.UtcNow,
            ownerUserId);
        var chunk = DocumentChunk.Create(document.Id, 0, chunkText ?? name);
        chunk.SetEmbedding(vector);
        document.CompleteProcessing([chunk]);

        return document;
    }

    private static IReadOnlyList<float> CreateVector(float first, float second)
    {
        var values = new float[PostgresApplicationDbContext.VectorDimensions];
        values[0] = first;
        values[1] = second;
        return values;
    }
}

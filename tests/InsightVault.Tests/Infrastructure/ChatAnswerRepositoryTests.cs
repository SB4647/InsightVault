using InsightVault.Domain.Entities;
using InsightVault.Infrastructure.Persistence;
using InsightVault.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InsightVault.Tests.Infrastructure;

public sealed class ChatAnswerRepositoryTests
{
    [Fact]
    public async Task SaveAsync_PersistsCitationsInRankOrderAndDeletesThemWithTheAnswer()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;

        Guid answerId;
        await using (var context = new ApplicationDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            var document = Document.Create(
                "policy.pdf",
                "application/pdf",
                128,
                "documents/policy.pdf",
                new DateTime(2026, 10, 7, 0, 30, 0, DateTimeKind.Utc),
                "user-1");
            var firstChunk = DocumentChunk.Create(document.Id, 0, "Review requirements.", 2);
            var secondChunk = DocumentChunk.Create(document.Id, 1, "The policy requires review.", 4, "Review process");
            document.CompleteProcessing([firstChunk, secondChunk]);
            context.Documents.Add(document);
            await context.SaveChangesAsync();

            var answer = ChatAnswer.Create(
                "user-1",
                "What does the policy require?",
                "The policy requires a review.",
                new DateTime(2026, 10, 7, 1, 0, 0, DateTimeKind.Utc),
                5,
                0.60m,
                1);
            answer.AddCitation(ChatAnswerCitation.Create(
                answer.Id,
                document.Id,
                1,
                secondChunk.Id,
                4,
                "Review process",
                0.82m,
                2));
            answer.AddCitation(ChatAnswerCitation.Create(
                answer.Id,
                document.Id,
                1,
                firstChunk.Id,
                2,
                null,
                0.94m,
                1));

            var repository = new ChatAnswerRepository(context);
            await repository.SaveAsync(answer);
            answerId = answer.Id;
        }

        await using (var context = new ApplicationDbContext(options))
        {
            var saved = await context.ChatAnswers
                .Include(answer => answer.Citations)
                .SingleAsync(answer => answer.Id == answerId);

            Assert.Equal([1, 2], saved.Citations.Select(citation => citation.Rank));
            var firstCitation = saved.Citations.Single(citation => citation.Rank == 1);
            Assert.Equal(1, firstCitation.DocumentVersion);
            Assert.Equal(2, firstCitation.SourcePageNumber);
            Assert.Null(firstCitation.SectionTitle);

            var secondCitation = saved.Citations.Single(citation => citation.Rank == 2);
            Assert.Equal(4, secondCitation.SourcePageNumber);
            Assert.Equal("Review process", secondCitation.SectionTitle);

            context.ChatAnswers.Remove(saved);
            await context.SaveChangesAsync();
        }

        await using (var context = new ApplicationDbContext(options))
        {
            Assert.Empty(context.ChatAnswerCitations);
        }
    }
}

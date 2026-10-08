using InsightVault.Application.Interfaces;
using InsightVault.Domain.Entities;

namespace InsightVault.Infrastructure.Persistence.Repositories;

public sealed class ChatAnswerRepository(ApplicationDbContext dbContext) : IChatAnswerRepository
{
    /// <summary>
    /// Inserts a completed answer and its citations in the same EF Core save operation.
    /// </summary>
    public async Task SaveAsync(ChatAnswer answer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(answer);

        await dbContext.ChatAnswers.AddAsync(answer, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

using InsightVault.Domain.Entities;

namespace InsightVault.Application.Interfaces;

/// <summary>
/// Persists an answer and all of its citation snapshots as one database operation.
/// </summary>
public interface IChatAnswerRepository
{
    /// <summary>
    /// Saves a completed answer with its ordered citations atomically.
    /// </summary>
    Task SaveAsync(ChatAnswer answer, CancellationToken cancellationToken = default);
}

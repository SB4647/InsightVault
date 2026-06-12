using InsightVault.Application.Interfaces;
using InsightVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InsightVault.Infrastructure.Persistence.Repositories;

public sealed class DocumentRepository(ApplicationDbContext dbContext) : IDocumentRepository
{
    public async Task AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        await dbContext.Documents.AddAsync(document, cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Documents
            .AsNoTracking()
            .OrderByDescending(document => document.UploadedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}

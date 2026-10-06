using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pgvector.EntityFrameworkCore;

namespace InsightVault.Infrastructure.Persistence;

public sealed class PostgresApplicationDbContextFactory
    : IDesignTimeDbContextFactory<PostgresApplicationDbContext>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5433;Database=InsightVault;Username=postgres;Password=InsightVault-Local-Only-Password-123!";

    public PostgresApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<PostgresApplicationDbContext>()
            .UseNpgsql(connectionString, postgres => postgres.UseVector())
            .Options;

        return new PostgresApplicationDbContext(options);
    }
}

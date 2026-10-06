using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InsightVault.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string DefaultConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=InsightVault;Trusted_Connection=True;MultipleActiveResultSets=true";

    /// <summary>
    /// Creates the SQL Server context used by EF Core commands for the existing SQL Server migration path.
    /// </summary>
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }
}

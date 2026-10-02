using System.Text.Json;
using InsightVault.Infrastructure;
using InsightVault.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InsightVault.Tests.Infrastructure;

public sealed class DatabaseConfigurationTests
{
    [Fact]
    public void PostgresExampleSettings_DeclarePostgresProviderAndComposeConnectionString()
    {
        var settingsPath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "InsightVault.Api",
            "appsettings.Postgres.example.json");

        Assert.True(File.Exists(settingsPath));

        using var document = JsonDocument.Parse(File.ReadAllText(settingsPath));

        Assert.Equal("Postgres", document.RootElement
            .GetProperty("Database")
            .GetProperty("Provider")
            .GetString());

        var connectionString = document.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("DefaultConnection")
            .GetString();

        Assert.Contains("Host=postgres", connectionString, StringComparison.Ordinal);
        Assert.Contains("Port=5432", connectionString, StringComparison.Ordinal);
    }

    [Fact]
    public void AddInfrastructure_WhenPostgresIsConfigured_RegistersThePostgresContext()
    {
        var services = new ServiceCollection();

        services.AddInfrastructure(CreateConfiguration("Postgres"));

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType.Name == "PostgresApplicationDbContext");
    }

    [Fact]
    public void AddInfrastructure_WhenSqlServerIsConfigured_UsesSqlServerProvider()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(CreateConfiguration("SqlServer"));

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
    }

    [Fact]
    public void AddInfrastructure_WhenProviderIsUnknown_ThrowsHelpfulException()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddInfrastructure(CreateConfiguration("UnknownProvider")));

        Assert.Contains("Database:Provider", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void InfrastructureAssembly_ProvidesPostgresDesignTimeContextFactory()
    {
        var factoryType = typeof(ApplicationDbContext).Assembly.GetType(
            "InsightVault.Infrastructure.Persistence.PostgresApplicationDbContextFactory");

        Assert.NotNull(factoryType);
    }

    [Fact]
    public void InfrastructureAssembly_ProvidesSqlServerDesignTimeContextFactory()
    {
        var factoryType = typeof(ApplicationDbContext).Assembly.GetType(
            "InsightVault.Infrastructure.Persistence.ApplicationDbContextFactory");

        Assert.NotNull(factoryType);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "InsightVault.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the InsightVault repository root.");
    }

    private static IConfiguration CreateConfiguration(string provider)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = provider,
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5433;Database=InsightVault;Username=postgres;Password=local"
            })
            .Build();
    }
}

using System.Text.Json;
using InsightVault.Infrastructure;
using InsightVault.Infrastructure.Persistence;
using InsightVault.Infrastructure.Storage;
using InsightVault.Application.Interfaces;
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
    public void AddInfrastructure_WhenS3StorageIsConfigured_RegistersS3StorageService()
    {
        var services = new ServiceCollection();

        services.AddInfrastructure(CreateConfiguration("SqlServer", "S3"));

        var storageRegistration = Assert.Single(services, descriptor =>
            descriptor.ServiceType == typeof(IBlobStorageService));

        Assert.Equal(typeof(S3BlobStorageService), storageRegistration.ImplementationType);
    }

    [Fact]
    public void AddInfrastructure_WhenStorageProviderIsUnknown_ThrowsHelpfulException()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddInfrastructure(CreateConfiguration("SqlServer", "UnknownStorage")));

        Assert.Contains("Storage:Provider", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddInfrastructure_WhenStorageProviderIsOmitted_KeepsAzureStorageAsTheDefault()
    {
        var services = new ServiceCollection();

        services.AddInfrastructure(CreateConfiguration("SqlServer", null));

        var storageRegistration = Assert.Single(services, descriptor =>
            descriptor.ServiceType == typeof(IBlobStorageService));

        Assert.Equal(typeof(BlobStorageService), storageRegistration.ImplementationType);
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

    private static IConfiguration CreateConfiguration(string provider, string? storageProvider = "Azure")
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = provider,
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5433;Database=InsightVault;Username=postgres;Password=local",
                ["Storage:Provider"] = storageProvider,
                ["AzureBlobStorage:ConnectionString"] = "UseDevelopmentStorage=true",
                ["AzureBlobStorage:ContainerName"] = "documents",
                ["S3Storage:BucketName"] = "insightvault-tests"
            })
            .Build();
    }
}

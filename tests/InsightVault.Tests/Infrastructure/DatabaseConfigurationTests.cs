using System.Text.Json;

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
}

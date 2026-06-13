using InsightVault.Application.Interfaces;
using InsightVault.Infrastructure.Chat;
using InsightVault.Infrastructure.Documents;
using InsightVault.Infrastructure.Embeddings;
using InsightVault.Infrastructure.Identity;
using InsightVault.Infrastructure.Persistence;
using InsightVault.Infrastructure.Persistence.Repositories;
using InsightVault.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InsightVault.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.Configure<BlobStorageOptions>(options =>
        {
            var section = configuration.GetSection("AzureBlobStorage");
            options.ConnectionString = section["ConnectionString"] ?? string.Empty;
            options.ContainerName = section["ContainerName"] ?? "documents";
        });

        services.Configure<AzureOpenAiEmbeddingOptions>(options =>
        {
            var section = configuration.GetSection("AzureOpenAI");
            options.Endpoint = section["Endpoint"] ?? string.Empty;
            options.ApiKey = section["ApiKey"] ?? string.Empty;
            options.DeploymentName = section["EmbeddingDeploymentName"] ?? string.Empty;
            options.ApiVersion = section["ApiVersion"] ?? "2024-02-01";
        });

        services.Configure<AzureOpenAiChatOptions>(options =>
        {
            var section = configuration.GetSection("AzureOpenAI");
            options.Endpoint = section["Endpoint"] ?? string.Empty;
            options.ApiKey = section["ApiKey"] ?? string.Empty;
            options.DeploymentName = section["ChatDeploymentName"] ?? string.Empty;
            options.ApiVersion = section["ApiVersion"] ?? "2024-10-21";
        });

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentSearchRepository, DocumentRepository>();
        services.AddScoped<IUserLookupService, UserLookupService>();
        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<ITextExtractionService, PdfTextExtractionService>();
        services.AddHttpClient<IEmbeddingService, AzureOpenAiEmbeddingService>();
        services.AddHttpClient<IChatCompletionService, AzureOpenAiChatCompletionService>();

        return services;
    }
}

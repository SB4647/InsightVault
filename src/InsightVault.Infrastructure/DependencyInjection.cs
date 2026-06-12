using InsightVault.Application.Interfaces;
using InsightVault.Infrastructure.Persistence;
using InsightVault.Infrastructure.Persistence.Repositories;
using InsightVault.Infrastructure.Storage;
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

        services.Configure<BlobStorageOptions>(options =>
        {
            var section = configuration.GetSection("AzureBlobStorage");
            options.ConnectionString = section["ConnectionString"] ?? string.Empty;
            options.ContainerName = section["ContainerName"] ?? "documents";
        });

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IBlobStorageService, BlobStorageService>();

        return services;
    }
}

using InsightVault.Application.Interfaces;
using InsightVault.Application.ProcessingQueue;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using InsightVault.Infrastructure.Chat;
using InsightVault.Infrastructure.Documents;
using InsightVault.Infrastructure.Embeddings;
using InsightVault.Infrastructure.Identity;
using InsightVault.Infrastructure.Persistence;
using InsightVault.Infrastructure.Persistence.Repositories;
using InsightVault.Infrastructure.Storage;
using InsightVault.Infrastructure.ProcessingQueue;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore;

namespace InsightVault.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var provider = configuration["Database:Provider"] ?? "SqlServer";
        var storageProvider = configuration["Storage:Provider"] ?? "Azure";
        var usePostgres = string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase);
        var useS3Storage = string.Equals(storageProvider, "S3", StringComparison.OrdinalIgnoreCase);
        var useAzureStorage = string.Equals(storageProvider, "Azure", StringComparison.OrdinalIgnoreCase);
        var queueProvider = configuration["Queue:Provider"] ?? "Disabled";

        if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
        }
        else if (usePostgres)
        {
            services.AddDbContext<PostgresApplicationDbContext>(options =>
                options.UseNpgsql(connectionString, postgres => postgres.UseVector()));
            services.AddScoped<ApplicationDbContext>(serviceProvider =>
                serviceProvider.GetRequiredService<PostgresApplicationDbContext>());
        }
        else
        {
            throw new InvalidOperationException(
                "Database:Provider must be either 'SqlServer' or 'Postgres'.");
        }

        if (!useAzureStorage && !useS3Storage)
        {
            throw new InvalidOperationException(
                "Storage:Provider must be either 'Azure' or 'S3'.");
        }

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

        services.Configure<S3StorageOptions>(options =>
        {
            var section = configuration.GetSection("S3Storage");
            options.BucketName = section["BucketName"] ?? string.Empty;
            options.Region = section["Region"] ?? "ap-southeast-2";
            options.ServiceUrl = section["ServiceUrl"] ?? string.Empty;
            options.AccessKey = section["AccessKey"] ?? string.Empty;
            options.SecretKey = section["SecretKey"] ?? string.Empty;
            options.ForcePathStyle = bool.TryParse(section["ForcePathStyle"], out var forcePathStyle) && forcePathStyle;
        });

        services.Configure<AzureOpenAiEmbeddingOptions>(options =>
        {
            var section = configuration.GetSection("AzureOpenAI");
            options.Endpoint = section["Endpoint"] ?? string.Empty;
            options.ApiKey = section["ApiKey"] ?? string.Empty;
            options.DeploymentName = section["EmbeddingDeploymentName"] ?? string.Empty;
            options.ApiVersion = section["ApiVersion"] ?? "2024-02-01";
        });

        services.Configure<DocumentProcessingQueueOptions>(configuration.GetSection("Queue"));

        services.Configure<AzureOpenAiChatOptions>(options =>
        {
            var section = configuration.GetSection("AzureOpenAI");
            options.Endpoint = section["Endpoint"] ?? string.Empty;
            options.ApiKey = section["ApiKey"] ?? string.Empty;
            options.DeploymentName = section["ChatDeploymentName"] ?? string.Empty;
            options.ApiVersion = section["ApiVersion"] ?? "2024-10-21";
        });

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IChatAnswerRepository, ChatAnswerRepository>();
        services.AddScoped<IDocumentProcessingOutboxRepository, DocumentProcessingOutboxRepository>();
        services.AddScoped<IProcessingOutboxDispatcher, DocumentProcessingOutboxDispatcher>();
        if (string.Equals(queueProvider, "LocalStack", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(queueProvider, "Sqs", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IAmazonSQS>(provider =>
            {
                var queue = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DocumentProcessingQueueOptions>>().Value;
                var config = new AmazonSQSConfig { RegionEndpoint = RegionEndpoint.GetBySystemName(queue.Region) };
                if (string.Equals(queueProvider, "LocalStack", StringComparison.OrdinalIgnoreCase))
                {
                    config.ServiceURL = queue.ServiceUrl;
                    return new AmazonSQSClient(new BasicAWSCredentials("localstack", "localstack"), config);
                }

                return new AmazonSQSClient(config);
            });
            services.AddScoped<IDocumentProcessingQueue, SqsDocumentProcessingQueue>();
        }
        else
        {
            services.AddScoped<IDocumentProcessingQueue, DisabledDocumentProcessingQueue>();
        }
        services.AddScoped<IDocumentSearchRepository, DocumentRepository>();
        if (usePostgres)
        {
            services.AddScoped<IVectorSearchRepository, PostgresVectorSearchRepository>();
        }
        else
        {
            services.AddScoped<IVectorSearchRepository, DocumentRepository>();
        }
        services.AddScoped<IUserLookupService, UserLookupService>();
        if (useS3Storage)
        {
            services.AddScoped<IBlobStorageService, S3BlobStorageService>();
        }
        else
        {
            services.AddScoped<IBlobStorageService, BlobStorageService>();
        }
        services.AddScoped<ITextExtractionService, PdfTextExtractionService>();
        services.AddHttpClient<IEmbeddingService, AzureOpenAiEmbeddingService>();
        services.AddHttpClient<IChatCompletionService, AzureOpenAiChatCompletionService>();

        return services;
    }
}

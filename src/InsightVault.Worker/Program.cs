using InsightVault.Application.Features.Documents.Processing;
using InsightVault.Application.Interfaces;
using InsightVault.Infrastructure;
using InsightVault.Worker;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IDocumentChunkingService, DocumentChunkingService>();
builder.Services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
builder.Services.AddHostedService<DocumentProcessingWorker>();
await builder.Build().RunAsync();

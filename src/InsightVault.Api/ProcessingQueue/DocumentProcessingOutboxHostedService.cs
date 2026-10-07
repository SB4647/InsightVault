using InsightVault.Application.ProcessingQueue;

namespace InsightVault.Api.ProcessingQueue;

/// <summary>Periodically publishes durable upload requests without handling document content in the API request.</summary>
public sealed class DocumentProcessingOutboxHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<DocumentProcessingOutboxHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IProcessingOutboxDispatcher>()
                    .DispatchPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unable to dispatch document processing outbox entries.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}

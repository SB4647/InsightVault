using InsightVault.Application.Features.Documents.Processing;
using InsightVault.Application.Features.Documents.Processing.Commands;
using InsightVault.Application.ProcessingQueue;

namespace InsightVault.Worker;

/// <summary>Long-polls durable jobs and acknowledges only successful or safely idempotent work.</summary>
public sealed class DocumentProcessingWorker(
    IServiceScopeFactory scopeFactory,
    IDocumentProcessingQueue queue,
    ILogger<DocumentProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IReadOnlyList<ReceivedDocumentProcessingJob> messages;
            try
            {
                messages = await queue.ReceiveAsync(5, TimeSpan.FromSeconds(20), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    "Document processing queue receive failed; polling will retry. ErrorType={ErrorType}",
                    exception.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            foreach (var message in messages)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<IDocumentProcessingService>().ProcessAsync(
                        new ProcessDocumentCommand(message.Job.DocumentId, message.Job.OwnerUserId), stoppingToken);
                    await queue.AcknowledgeAsync(message.ReceiptHandle, stoppingToken);
                }
                catch (InvalidOperationException exception) when (exception.Message.Contains("was not found", StringComparison.OrdinalIgnoreCase))
                {
                    await queue.AcknowledgeAsync(message.ReceiptHandle, stoppingToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        "Document processing failed; the queue message will remain retryable. DocumentId={DocumentId} ErrorType={ErrorType}",
                        message.Job.DocumentId,
                        exception.GetType().Name);
                }
            }
        }
    }
}


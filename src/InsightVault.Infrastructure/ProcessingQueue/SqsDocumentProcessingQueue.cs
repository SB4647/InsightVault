using Amazon.SQS;
using Amazon.SQS.Model;
using InsightVault.Application.ProcessingQueue;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace InsightVault.Infrastructure.ProcessingQueue;

public sealed class SqsDocumentProcessingQueue(IAmazonSQS sqs, IOptions<DocumentProcessingQueueOptions> options)
    : IDocumentProcessingQueue
{
    public async Task SendAsync(DocumentProcessingJob job, CancellationToken cancellationToken = default)
    {
        await sqs.SendMessageAsync(new SendMessageRequest(options.Value.QueueUrl, JsonSerializer.Serialize(job)), cancellationToken);
    }

    public async Task<IReadOnlyList<ReceivedDocumentProcessingJob>> ReceiveAsync(
        int maximumMessages, TimeSpan waitTime, CancellationToken cancellationToken = default)
    {
        var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = options.Value.QueueUrl,
            MaxNumberOfMessages = Math.Clamp(maximumMessages, 1, 10),
            WaitTimeSeconds = Math.Clamp((int)waitTime.TotalSeconds, 0, 20)
        }, cancellationToken);

        return response.Messages
            .Select(message => (Message: message, Job: JsonSerializer.Deserialize<DocumentProcessingJob>(message.Body)))
            .Where(item => item.Job is not null && item.Job.Version == DocumentProcessingJob.CurrentVersion)
            .Select(item => new ReceivedDocumentProcessingJob(item.Job!, item.Message.ReceiptHandle))
            .ToList();
    }

    public Task AcknowledgeAsync(string receiptHandle, CancellationToken cancellationToken = default)
    {
        return sqs.DeleteMessageAsync(options.Value.QueueUrl, receiptHandle, cancellationToken);
    }
}

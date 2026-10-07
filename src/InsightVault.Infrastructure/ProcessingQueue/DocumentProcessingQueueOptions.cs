namespace InsightVault.Infrastructure.ProcessingQueue;

public sealed class DocumentProcessingQueueOptions
{
    public string Provider { get; set; } = "Disabled";
    public string Region { get; set; } = "ap-southeast-2";
    public string QueueUrl { get; set; } = string.Empty;
    public string ServiceUrl { get; set; } = string.Empty;
}

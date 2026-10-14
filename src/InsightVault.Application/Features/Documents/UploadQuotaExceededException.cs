namespace InsightVault.Application.Features.Documents;

/// <summary>Signals that an owner has exhausted a configured document or storage quota.</summary>
public sealed class UploadQuotaExceededException(string message) : InvalidOperationException(message);


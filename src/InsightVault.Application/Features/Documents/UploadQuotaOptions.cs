namespace InsightVault.Application.Features.Documents;

/// <summary>Defines the per-owner document-count and storage limits for uploads.</summary>
public sealed class UploadQuotaOptions
{
    public const string SectionName = "UploadQuota";

    public int MaxDocumentsPerOwner { get; init; } = 100;

    public long MaxStoredBytesPerOwner { get; init; } = 500_000_000;

    /// <summary>Validates that each configured quota is positive.</summary>
    public void Validate()
    {
        if (MaxDocumentsPerOwner <= 0)
        {
            throw new InvalidOperationException("UploadQuota:MaxDocumentsPerOwner must be greater than zero.");
        }

        if (MaxStoredBytesPerOwner <= 0)
        {
            throw new InvalidOperationException("UploadQuota:MaxStoredBytesPerOwner must be greater than zero.");
        }
    }
}

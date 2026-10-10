namespace InsightVault.Api.RateLimiting;

/// <summary>Defines the configurable fixed-window limits that protect InsightVault API endpoints.</summary>
public sealed class ApiRateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public int AuthenticationPermitLimit { get; init; } = 5;

    public int UploadPermitLimit { get; init; } = 10;

    public int InteractivePermitLimit { get; init; } = 30;

    /// <summary>Validates that every configured permit limit is positive.</summary>
    public void Validate()
    {
        if (AuthenticationPermitLimit <= 0 || UploadPermitLimit <= 0 || InteractivePermitLimit <= 0)
        {
            throw new InvalidOperationException("Each RateLimiting permit limit must be greater than zero.");
        }
    }
}

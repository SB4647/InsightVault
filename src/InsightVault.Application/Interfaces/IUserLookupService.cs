namespace InsightVault.Application.Interfaces;

public interface IUserLookupService
{
    Task<UserLookupResult?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);
}

public sealed record UserLookupResult(
    string UserId,
    string Email);

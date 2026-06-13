using InsightVault.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace InsightVault.Infrastructure.Identity;

public sealed class UserLookupService(UserManager<ApplicationUser> userManager) : IUserLookupService
{
    public async Task<UserLookupResult?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null)
        {
            return null;
        }

        return new UserLookupResult(user.Id, user.Email ?? email.Trim());
    }
}

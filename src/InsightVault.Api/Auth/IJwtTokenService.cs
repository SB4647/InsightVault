using InsightVault.Infrastructure.Identity;

namespace InsightVault.Api.Auth;

public interface IJwtTokenService
{
    string CreateToken(ApplicationUser user);
}

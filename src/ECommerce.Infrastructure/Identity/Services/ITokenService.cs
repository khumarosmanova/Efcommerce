using System.Security.Claims;
using ECommerce.Infrastructure.Identity.Models;

namespace ECommerce.Infrastructure.Identity.Services;

public interface ITokenService
{
    (string Token, DateTime Expiration) GenerateAccessToken(
        AppUser user,
        IEnumerable<string> roles,
        IEnumerable<string> permissions);

    string GenerateRefreshToken();

    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

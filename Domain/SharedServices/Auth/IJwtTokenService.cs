using System.Security.Claims;

namespace STL.SharedServices.Auth;

public interface IJwtTokenService
{
    string GenerateAccessToken(IEnumerable<Claim> claims);
    string GenerateRefreshToken();
    DateTime getAccessTokenExpireTime();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}

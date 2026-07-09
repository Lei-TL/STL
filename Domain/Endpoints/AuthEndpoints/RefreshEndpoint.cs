using System.Security.Claims;
using FastEndpoints;
using STL.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using STL.DbContexts;
using STL.Models.Auth;
using STL.SharedServices.Auth;

namespace STL.Domain.Endpoints.AuthEndpoints;

public class RefreshRequest
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public record RefreshResponse(
    string AccessToken,
    DateTime AccessTokenExpireAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpireAt);

public class RefreshValidator : Validator<RefreshRequest>
{
    public RefreshValidator()
    {
        RuleFor(request => request.AccessToken)
            .NotEmpty()
            .WithMessage("Access token is required!");

        RuleFor(request => request.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required!");
    }
}

[AllowAnonymous]
[HttpPost(ApiRoutes.Auth.Refresh)]
public class RefreshEndpoint(
    AppDbContext context,
    IJwtTokenService tokenService)
    : Endpoint<RefreshRequest, RefreshResponse>
{
    public override async Task HandleAsync(RefreshRequest req, CancellationToken ct)
    {
        ClaimsPrincipal principal;

        try
        {
            principal = tokenService.GetPrincipalFromExpiredToken(req.AccessToken);
        }
        catch (SecurityTokenException)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }
        catch (ArgumentException)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == userId, ct);

        var userToken = await context.UserTokens
            .FirstOrDefaultAsync(token => token.UserId == userId, ct);

        if (user is null ||
            userToken is null ||
            userToken.RefreshToken != req.RefreshToken ||
            userToken.RefreshTokenExpiryTime <= DateTimeOffset.UtcNow)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Sid, user.Id),
            new(ClaimTypes.Role, user.RoleLevel.ToString()),
            new(AuthConstants.RoleLevelClaim, ((int)user.RoleLevel).ToString())
        };

        var accessToken = tokenService.GenerateAccessToken(claims);

        var refreshTokenExpires = DateTimeOffset.UtcNow.AddDays(7);
        var refreshToken = tokenService.GenerateRefreshToken();

        userToken.RefreshToken = refreshToken;
        userToken.RefreshTokenExpiryTime = refreshTokenExpires;

        await context.SaveChangesAsync(ct);

        await Send.OkAsync(
            new RefreshResponse(
                accessToken,
                tokenService.getAccessTokenExpireTime(),
                refreshToken,
                refreshTokenExpires),
            ct);
    }
}

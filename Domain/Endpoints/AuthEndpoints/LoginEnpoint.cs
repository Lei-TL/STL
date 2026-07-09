using System.Security.Claims;
using FastEndpoints;
using STL.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using STL.DbContexts;
using STL.Entities.IdentityModule;
using STL.Models.Auth;
using STL.SharedServices.Auth;

namespace STL.Domain.Endpoints.AuthEndpoints
{
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public record LoginResponse(
        string AccessToken,
        DateTime AccessTokenExpireAt,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpireAt);

    public class LoginValidator : Validator<LoginRequest>
    {
        public LoginValidator()
        {
            RuleFor(u => u.Email)
                .NotEmpty()
                .EmailAddress()
                .WithMessage("Email is required!")
                .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")
                .WithMessage("Email format is invalid!"); ;
            RuleFor(u => u.Password)
                .NotEmpty()
                .WithMessage("Password is required!");
        }
    }

    [AllowAnonymous]
    [HttpPost(ApiRoutes.Auth.Login)]
    public class LoginEnpoint(
        AppDbContext context,
        IJwtTokenService tokenService)
        : Endpoint<LoginRequest, LoginResponse>
    {
        public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
        {
            var email = req.Email.Trim().ToLowerInvariant();
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email, ct);

            if (user is null)
            {
                await Send.UnauthorizedAsync(ct);
                return;
            }

            if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
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

            var userToken = await context.UserTokens
                .FirstOrDefaultAsync(t => t.UserId == user.Id, ct);

            if (userToken is null)
            {
                await context.UserTokens.AddAsync(new UserToken
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = user.Id,
                    RefreshToken = refreshToken,
                    RefreshTokenExpiryTime = refreshTokenExpires
                }, ct);
            }
            else
            {
                userToken.RefreshToken = refreshToken;
                userToken.RefreshTokenExpiryTime = refreshTokenExpires;
            }

            await context.SaveChangesAsync(ct);

            await Send.OkAsync(
                new LoginResponse(
                    accessToken,
                    tokenService.getAccessTokenExpireTime(),
                    refreshToken,
                    refreshTokenExpires),
                ct);
        }
    }
}

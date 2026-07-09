using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using STL.Models.Auth;

namespace STL.SharedServices.UserContext;

public interface IUserContext
{
    string Email { get; }
    string UserId { get; }
    UserRoleLevel RoleLevel { get; }
}

public sealed class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public string Email =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email)
        ?? "Anonymous";

    public string UserId =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? string.Empty;

    public UserRoleLevel RoleLevel
    {
        get
        {
            var roleLevelClaim = httpContextAccessor.HttpContext?.User
                .FindFirstValue(AuthConstants.RoleLevelClaim);

            return int.TryParse(roleLevelClaim, out var roleLevel)
                ? (UserRoleLevel)roleLevel
                : UserRoleLevel.User;
        }
    }
}

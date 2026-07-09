using System.Security.Claims;
using FastEndpoints;
using STL.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using STL.DbContexts;
using STL.Models.Auth;

namespace STL.Domain.Endpoints.AuthEndpoints;

public record LogoutResponse();

[Authorize(Policy = AuthConstants.Policies.User)]
[HttpPost(ApiRoutes.Auth.Logout)]
public class LogoutEndpoint(AppDbContext context)
    : EndpointWithoutRequest<LogoutResponse>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var userToken = await context.UserTokens
            .FirstOrDefaultAsync(token => token.UserId == userId, ct);

        if (userToken is not null)
        {
            context.UserTokens.Remove(userToken);
            await context.SaveChangesAsync(ct);
        }

        await Send.OkAsync(new LogoutResponse(), ct);
    }
}

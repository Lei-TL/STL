using FastEndpoints;
using STL.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using STL.DbContexts;
using STL.Entities.IdentityModule;
using STL.Models.Auth;

namespace STL.Domain.Endpoints.AuthEndpoints;

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public record RegisterResponse(string Id);

public class RegisterValidator : Validator<RegisterRequest>
{
    public RegisterValidator()
    {

        RuleFor(user => user.Email)
            .Cascade(CascadeMode.Stop)
            .EmailAddress()
            .WithMessage("Email is required!");

        RuleFor(user => user.Email)
            .NotEmpty()
            .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")
            .WithMessage("Email format is invalid!");

        RuleFor(user => user.Password)
            .NotEmpty()
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters!");
    }
}

[AllowAnonymous]
[HttpPost(ApiRoutes.Auth.Register)]
public class RegisterEndpoint(AppDbContext context)
    : Endpoint<RegisterRequest, RegisterResponse>
{
    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var emailExists = await context.Users
            .AnyAsync(user => user.Email.ToLower() == email, ct);

        if (emailExists)
        {
            AddError(request => request.Email, "Email already exists!");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            RoleLevel = UserRoleLevel.User
        };

        context.Users.Add(user);

        await context.SaveChangesAsync(ct);

        await Send.OkAsync(
            new RegisterResponse(user.Id),
            ct);
    }
}

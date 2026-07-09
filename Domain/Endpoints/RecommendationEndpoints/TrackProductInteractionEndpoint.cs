using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using STL.Constants;
using STL.DbContexts;
using STL.Entities.RecommendationModule;
using STL.Models.Auth;
using STL.SharedServices.UserContext;

namespace STL.Endpoints.RecommendationEndpoints;

public class TrackProductInteractionRequest
{
    public string ProductId { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public ProductInteractionType InteractionType { get; set; } = ProductInteractionType.View;
    public decimal? Weight { get; set; }
}

public record TrackProductInteractionResponse(string Id);

public class TrackProductInteractionValidator : Validator<TrackProductInteractionRequest>
{
    public TrackProductInteractionValidator()
    {
        RuleFor(request => request.ProductId)
            .NotEmpty()
            .WithMessage("Product Id is required");

        RuleFor(request => request.InteractionType)
            .IsInEnum()
            .WithMessage("Interaction type is invalid");

        RuleFor(request => request.Weight)
            .GreaterThan(0)
            .When(request => request.Weight.HasValue)
            .WithMessage("Weight must be greater than 0");
    }
}

[HttpPost(ApiRoutes.Recommendation.TrackProductInteraction)]
[Authorize(Policy = AuthConstants.Policies.User)]
public class TrackProductInteractionEndpoint(
    AppDbContext context,
    IUserContext userContext)
    : Endpoint<TrackProductInteractionRequest, TrackProductInteractionResponse>
{
    public override async Task HandleAsync(
        TrackProductInteractionRequest req,
        CancellationToken ct)
    {
        var productExists = await context.Products
            .AnyAsync(product => product.Id == req.ProductId && product.IsActive, ct);

        if (!productExists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var interaction = new ProductInteraction
        {
            Id = Guid.NewGuid().ToString(),
            UserId = string.IsNullOrWhiteSpace(userContext.UserId)
                ? null
                : userContext.UserId,
            SessionId = string.IsNullOrWhiteSpace(req.SessionId)
                ? null
                : req.SessionId.Trim(),
            ProductId = req.ProductId,
            InteractionType = req.InteractionType,
            Weight = req.Weight ?? GetDefaultWeight(req.InteractionType)
        };

        await context.ProductInteractions.AddAsync(interaction, ct);
        await context.SaveChangesAsync(ct);

        await Send.OkAsync(new TrackProductInteractionResponse(interaction.Id), ct);
    }

    private static decimal GetDefaultWeight(ProductInteractionType interactionType)
    {
        return interactionType switch
        {
            ProductInteractionType.View => 1,
            ProductInteractionType.SearchClick => 2,
            ProductInteractionType.AddToCart => 3,
            ProductInteractionType.Purchase => 5,
            _ => 1
        };
    }
}

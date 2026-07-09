using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using STL.Constants;
using STL.Models.Auth;
using STL.SharedServices.Recommendations;

namespace STL.Endpoints.RecommendationEndpoints;

public class RebuildFallbackRecommendationsRequest
{
    public int LimitPerProduct { get; set; } = 20;
}

public record RebuildFallbackRecommendationsResponse(int CreatedCount);

public class RebuildFallbackRecommendationsValidator
    : Validator<RebuildFallbackRecommendationsRequest>
{
    public RebuildFallbackRecommendationsValidator()
    {
        RuleFor(request => request.LimitPerProduct)
            .InclusiveBetween(1, 50)
            .WithMessage("Limit per product must be between 1 and 50");
    }
}

[HttpPost(ApiRoutes.Recommendation.RebuildFallback)]
[Authorize(Policy = AuthConstants.Policies.Admin)]
public class RebuildFallbackRecommendationsEndpoint(
    IProductRecommendationService recommendationService)
    : Endpoint<RebuildFallbackRecommendationsRequest, RebuildFallbackRecommendationsResponse>
{
    public override async Task HandleAsync(
        RebuildFallbackRecommendationsRequest req,
        CancellationToken ct)
    {
        var createdCount = await recommendationService.RebuildFallbackRecommendationsAsync(
            req.LimitPerProduct,
            ct);

        await Send.OkAsync(new RebuildFallbackRecommendationsResponse(createdCount), ct);
    }
}

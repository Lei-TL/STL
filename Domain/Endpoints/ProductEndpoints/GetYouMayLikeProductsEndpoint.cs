using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using STL.Constants;
using STL.Entities.RecommendationModule;
using STL.Models.Auth;
using STL.SharedServices.Recommendations;

namespace STL.Endpoints.ProductEndpoints;

public class ProductRecommendationValidator : Validator<ProductRecommendationRequest>
{
    public ProductRecommendationValidator()
    {
        RuleFor(request => request.Id)
            .NotEmpty()
            .WithMessage("Product Id is required");

        RuleFor(request => request.Limit)
            .InclusiveBetween(1, 50)
            .WithMessage("Limit must be between 1 and 50");
    }
}

[HttpGet(ApiRoutes.Product.YouMayLike)]
[Authorize(Policy = AuthConstants.Policies.User)]
public class GetYouMayLikeProductsEndpoint(IProductRecommendationService recommendationService)
    : Endpoint<ProductRecommendationRequest, List<ProductRecommendationResponse>>
{
    public override async Task HandleAsync(
        ProductRecommendationRequest req,
        CancellationToken ct)
    {
        var recommendations = await recommendationService.GetRecommendationsAsync(
            req.Id,
            ProductRecommendationType.YouMayLike,
            req.Limit,
            ct);

        if (recommendations is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(recommendations, ct);
    }
}

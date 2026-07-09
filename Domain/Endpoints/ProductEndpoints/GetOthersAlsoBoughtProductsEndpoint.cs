using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using STL.Constants;
using STL.Entities.RecommendationModule;
using STL.Models.Auth;
using STL.SharedServices.Recommendations;

namespace STL.Endpoints.ProductEndpoints;

[HttpGet(ApiRoutes.Product.OthersAlsoBought)]
[Authorize(Policy = AuthConstants.Policies.User)]
public class GetOthersAlsoBoughtProductsEndpoint(IProductRecommendationService recommendationService)
    : Endpoint<ProductRecommendationRequest, List<ProductRecommendationResponse>>
{
    public override async Task HandleAsync(
        ProductRecommendationRequest req,
        CancellationToken ct)
    {
        var recommendations = await recommendationService.GetRecommendationsAsync(
            req.Id,
            ProductRecommendationType.OthersAlsoBought,
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

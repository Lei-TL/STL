using STL.Endpoints.ProductEndpoints;
using STL.Entities.RecommendationModule;

namespace STL.SharedServices.Recommendations;

public interface IProductRecommendationService
{
    Task<List<ProductRecommendationResponse>?> GetRecommendationsAsync(
        string productId,
        ProductRecommendationType recommendationType,
        int limit,
        CancellationToken ct = default);

    Task<int> RebuildFallbackRecommendationsAsync(
        int limitPerProduct,
        CancellationToken ct = default);
}

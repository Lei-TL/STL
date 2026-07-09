using Microsoft.EntityFrameworkCore;
using STL.DbContexts;
using STL.Endpoints.ProductEndpoints;
using STL.Entities.RecommendationModule;

namespace STL.SharedServices.Recommendations;

public sealed class ProductRecommendationService(AppDbContext context)
    : IProductRecommendationService
{
    private const string FallbackModelVersion = "fallback";

    public async Task<List<ProductRecommendationResponse>?> GetRecommendationsAsync(
        string productId,
        ProductRecommendationType recommendationType,
        int limit,
        CancellationToken ct = default)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 50);

        var currentProduct = await context.Products
            .AsNoTracking()
            .Where(product => product.Id == productId && product.IsActive)
            .Select(product => new
            {
                product.Id,
                product.CategoryId
            })
            .FirstOrDefaultAsync(ct);

        if (currentProduct is null)
        {
            return null;
        }

        var modelRecommendations = await GetModelRecommendationsAsync(
            currentProduct.Id,
            recommendationType,
            normalizedLimit,
            ct);

        if (modelRecommendations.Count > 0)
        {
            return modelRecommendations;
        }

        return await GetFallbackRecommendationsAsync(
            currentProduct.Id,
            currentProduct.CategoryId,
            recommendationType,
            normalizedLimit,
            ct);
    }

    public async Task<int> RebuildFallbackRecommendationsAsync(
        int limitPerProduct,
        CancellationToken ct = default)
    {
        var normalizedLimit = Math.Clamp(limitPerProduct, 1, 50);

        await context.ProductRecommendations
            .Where(recommendation => recommendation.ModelVersion == FallbackModelVersion)
            .ExecuteDeleteAsync(ct);

        var products = await context.Products
            .AsNoTracking()
            .Where(product => product.IsActive && product.Category!.IsActive)
            .Select(product => new
            RecommendationProduct(
                product.Id,
                product.CategoryId,
                product.CreatedAt,
                product.Name))
            .ToListAsync(ct);

        var recommendations = new List<ProductRecommendation>();

        foreach (var product in products)
        {
            AddFallbackRecommendations(
                recommendations,
                products,
                product.Id,
                product.CategoryId,
                ProductRecommendationType.YouMayLike,
                normalizedLimit);

            AddFallbackRecommendations(
                recommendations,
                products,
                product.Id,
                product.CategoryId,
                ProductRecommendationType.OthersAlsoBought,
                normalizedLimit);
        }

        if (recommendations.Count == 0)
        {
            return 0;
        }

        await context.ProductRecommendations.AddRangeAsync(recommendations, ct);
        await context.SaveChangesAsync(ct);

        return recommendations.Count;
    }

    private async Task<List<ProductRecommendationResponse>> GetModelRecommendationsAsync(
        string productId,
        ProductRecommendationType recommendationType,
        int limit,
        CancellationToken ct)
    {
        return await (
            from recommendation in context.ProductRecommendations.AsNoTracking()
            join product in context.Products.AsNoTracking()
                on recommendation.RecommendedProductId equals product.Id
            join category in context.Categories.AsNoTracking()
                on product.CategoryId equals category.Id
            where recommendation.ProductId == productId
                && recommendation.RecommendationType == recommendationType
                && product.IsActive
                && category.IsActive
            orderby recommendation.Score descending, product.Name
            select new ProductRecommendationResponse(
                product.Id,
                product.Name,
                product.Description,
                product.CategoryId,
                category.Name,
                recommendation.Score,
                recommendation.Reason ?? recommendation.ModelVersion))
            .Take(limit)
            .ToListAsync(ct);
    }

    private async Task<List<ProductRecommendationResponse>> GetFallbackRecommendationsAsync(
        string productId,
        string categoryId,
        ProductRecommendationType recommendationType,
        int limit,
        CancellationToken ct)
    {
        return await (
            from product in context.Products.AsNoTracking()
            join category in context.Categories.AsNoTracking()
                on product.CategoryId equals category.Id
            where product.Id != productId
                && product.IsActive
                && category.IsActive
            let sameCategory = product.CategoryId == categoryId
            orderby sameCategory descending, product.CreatedAt descending, product.Name
            select new ProductRecommendationResponse(
                product.Id,
                product.Name,
                product.Description,
                product.CategoryId,
                category.Name,
                GetFallbackScore(recommendationType, sameCategory),
                GetFallbackReason(recommendationType, sameCategory)))
            .Take(limit)
            .ToListAsync(ct);
    }

    private static void AddFallbackRecommendations(
        List<ProductRecommendation> recommendations,
        IEnumerable<RecommendationProduct> products,
        string productId,
        string categoryId,
        ProductRecommendationType recommendationType,
        int limit)
    {
        var candidates = products
            .Where(product => product.Id != productId)
            .OrderByDescending(product => product.CategoryId == categoryId)
            .ThenByDescending(product => product.CreatedAt)
            .ThenBy(product => product.Name)
            .Take(limit);

        foreach (var candidate in candidates)
        {
            var sameCategory = candidate.CategoryId == categoryId;

            recommendations.Add(new ProductRecommendation
            {
                Id = Guid.NewGuid().ToString(),
                ProductId = productId,
                RecommendedProductId = candidate.Id,
                RecommendationType = recommendationType,
                Score = GetFallbackScore(recommendationType, sameCategory),
                ModelVersion = FallbackModelVersion,
                Reason = GetFallbackReason(recommendationType, sameCategory)
            });
        }
    }

    private static decimal GetFallbackScore(
        ProductRecommendationType recommendationType,
        bool sameCategory)
    {
        return recommendationType switch
        {
            ProductRecommendationType.YouMayLike => sameCategory ? 0.95m : 0.35m,
            ProductRecommendationType.OthersAlsoBought => sameCategory ? 0.85m : 0.25m,
            _ => sameCategory ? 0.50m : 0.10m
        };
    }

    private static string GetFallbackReason(
        ProductRecommendationType recommendationType,
        bool sameCategory)
    {
        return recommendationType switch
        {
            ProductRecommendationType.YouMayLike =>
                sameCategory ? "Same category" : "Recent active product",
            ProductRecommendationType.OthersAlsoBought =>
                sameCategory
                    ? "Bought together fallback: same category"
                    : "Bought together fallback: catalog product",
            _ => sameCategory ? "Fallback: same category" : "Fallback: catalog product"
        };
    }

    private sealed record RecommendationProduct(
        string Id,
        string CategoryId,
        DateTime CreatedAt,
        string Name);
}

using FastEndpoints;
using STL.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using STL.DbContexts;
using STL.Infrastructure.Models;
using STL.Models.Auth;
using STL.SharedServices.Caching;

namespace STL.Endpoints.ProductEndpoints;

public class GetListProductRequest : PagingRequest
{
}

public class GetListProductResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

[HttpGet(ApiRoutes.Product.GetList)]
[Authorize(Policy = AuthConstants.Policies.User)]
public class GetListProductEndpoint(
    AppDbContext context,
    ICacheService cache)
    : Endpoint<GetListProductRequest, PagedList<GetListProductResponse>>
{
    public override async Task HandleAsync(
        GetListProductRequest req,
        CancellationToken ct)
    {
        var version = await cache.GetVersionAsync(
            CacheKeys.ProductListVersion,
            ct);
        var cacheKey = CacheKeys.ProductList(
            version,
            req.PageNumber,
            req.PageSize);
        var cachedProduct = await cache.GetAsync<PagedList<GetListProductResponse>>(
            cacheKey,
            ct);

        if (cachedProduct is not null)
        {
            await Send.OkAsync(cachedProduct, ct);
            return;
        }

        var query =
            from product in context.Products.AsNoTracking()
            join category in context.Categories.AsNoTracking()
                on product.CategoryId equals category.Id
            where !product.Deleted && !category.Deleted
            orderby product.Name
            select new GetListProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                CategoryId = product.CategoryId,
                CategoryName = category.Name,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };

        var result = await PagedList<GetListProductResponse>.CreateAsync(
            query,
            req.PageNumber,
            req.PageSize);

        await cache.SetAsync(cacheKey, result, ct);

        await Send.OkAsync(result, ct);
    }
}

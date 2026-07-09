using AutoMapper.QueryableExtensions;
using FastEndpoints;
using STL.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using STL.DbContexts;
using STL.Endpoints.ProductEndpoints;
using STL.Infrastructure.Models;
using STL.Models.Auth;
using STL.SharedServices.Caching;

namespace STL.Endpoints.CategoryEndpoints
{
    public class GetListCategoryRequest
        : PagingRequest
    {
    }

    public class GetListCategoryResponse 
    {
        public string CategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Slug { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    [Authorize(Policy = AuthConstants.Policies.User)]
    [HttpGet(ApiRoutes.Category.GetList)]
    public class GetListCategoryEndpoint(
        AppDbContext context,
        AutoMapper.IMapper mapper,
        ICacheService cache) :  Endpoint<GetListCategoryRequest,PagedList<GetListCategoryResponse>>
    {

        public override async Task HandleAsync(GetListCategoryRequest req, CancellationToken ct)
        {
            var version = await cache.GetVersionAsync(
                CacheKeys.CategoryListVersion,
                ct);
            var cacheKey = CacheKeys.CategoryList(
                version,
                req.PageNumber,
                req.PageSize);
            var cachedCategory = await cache.GetAsync<PagedList<GetListCategoryResponse>>(
                cacheKey,
                ct);

            if (cachedCategory is not null)
            {
                await Send.OkAsync(cachedCategory, ct);
                return;
            }
            
            var query = context.Categories
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ProjectTo<GetListCategoryResponse>(mapper.ConfigurationProvider);

            var result = await PagedList<GetListCategoryResponse>.CreateAsync(
                query, req.PageNumber, req.PageSize);
            
            await cache.SetAsync(cacheKey, result, ct);

            await Send.OkAsync(result, ct);

        }
    }
}

using FastEndpoints;
using STL.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using STL.DbContexts;
using STL.Models.Auth;
using STL.SharedServices.Caching;

namespace STL.Endpoints.CategoryEndpoints;

[Authorize(Policy = AuthConstants.Policies.Admin)]
[HttpDelete(ApiRoutes.Category.Delete)]
public class DeleteCategoryEndpoint(
    AppDbContext dbContext,
    ICacheService cache)
    : EndpointWithoutRequest
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<string>("id");

        using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            var category = await dbContext.Categories
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (category is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            // SoftDeleteInterceptor chuyển Remove thành cập nhật Deleted = true.
            dbContext.Categories.Remove(category);

            await dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            await cache.BumpVersionAsync(
                CacheKeys.CategoryListVersion,
                ct);
            await cache.BumpVersionAsync(
                CacheKeys.ProductListVersion,
                ct);

            await Send.OkAsync(cancellation: ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}

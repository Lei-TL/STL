using FastEndpoints;
using STL.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using STL.DbContexts;
using STL.Models.Auth;
using STL.SharedServices.Caching;

namespace STL.Endpoints.ProductEndpoints;

public record DeleteProductResponse();

[HttpDelete(ApiRoutes.Product.Delete)]
[Authorize(Policy = AuthConstants.Policies.Admin)]
public class DeleteProductEndpoint(
    AppDbContext dbContext,
    ICacheService cache)
    : EndpointWithoutRequest<DeleteProductResponse>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<string>("id");

        using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            var product = await dbContext.Products
                .FirstOrDefaultAsync(p => p.Id == id && !p.Deleted, ct);

            if (product is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            // SoftDeleteInterceptor chuyển Remove thành cập nhật Deleted = true.
            dbContext.Products.Remove(product);

            await dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

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

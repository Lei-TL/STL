using AutoMapper;
using FastEndpoints;
using STL.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using STL.DbContexts;
using STL.Entities.CatalogModule;
using STL.Models.Auth;
using STL.SharedServices.Caching;

namespace STL.Endpoints.ProductEndpoints;

public class UpdateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public record UpdateProductResponse(
    string Id,
    string Name,
    string? Description,
    string CategoryId,
    bool IsActive);

public class UpdateProductValidator : Validator<UpdateProductRequest>
{
    public UpdateProductValidator(AppDbContext context)
    {
        RuleFor(p => p.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage("Product name is required");

        RuleFor(p => p.CategoryId)
            .NotNull()
            .NotEmpty()
            .WithMessage("Category Id is required");
    }
}

[HttpPut(ApiRoutes.Product.Update)]
[Authorize(Policy = AuthConstants.Policies.Manager)]
public class UpdateProductEndpoint(
    AppDbContext dbContext,
    AutoMapper.IMapper mapper,
    ICacheService cache)
    : Endpoint<UpdateProductRequest, UpdateProductResponse>
{
    public override async Task HandleAsync(
        UpdateProductRequest req,
        CancellationToken ct)
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

            var categoryExists = await dbContext.Categories
                .AnyAsync(c => c.Id == req.CategoryId && !c.Deleted, ct);

            if (!categoryExists)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            mapper.Map(req, product);

            await dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            await cache.BumpVersionAsync(
                CacheKeys.ProductListVersion,
                ct);

            await Send.OkAsync(
                response: new UpdateProductResponse(
                    product.Id,
                    product.Name,
                    product.Description,
                    product.CategoryId,
                    product.IsActive),
                cancellation: ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}

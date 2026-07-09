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

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public record CreateProductResponse(string Id);

public class CreateProductValidator : Validator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty()
            .NotNull()
            .WithMessage("Product name is required");

        RuleFor(p => p.CategoryId)
            .NotEmpty()
            .NotNull()
            .WithMessage("Category Id is required");
    }
}

[HttpPost(ApiRoutes.Product.Create)]
[Authorize(Policy = AuthConstants.Policies.Manager)]
public class CreateProductEndpoint(AppDbContext dbContext, 
    AutoMapper.IMapper mapper,
    ICacheService cache)
    : Endpoint<CreateProductRequest, CreateProductResponse>
{
    public override async Task HandleAsync(
        CreateProductRequest req,
        CancellationToken ct)
    {

        var categoryExists = await dbContext.Categories
            .AnyAsync(c => c.Id == req.CategoryId && !c.Deleted, ct);

        if (!categoryExists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var product = mapper.Map<Product>(req);

        await dbContext.Products.AddAsync(product, ct);
        await dbContext.SaveChangesAsync(ct);

        await cache.BumpVersionAsync(
            CacheKeys.ProductListVersion,
            ct);

        await Send.OkAsync(
            response: new CreateProductResponse(product.Id),
            cancellation: ct);

    }

}

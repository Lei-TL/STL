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

namespace STL.Endpoints.CategoryEndpoints;

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public record CreateCategoryResponse(string Id);

public class CreateCategoryValidator : Validator<CreateCategoryRequest>
{
    public CreateCategoryValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .NotNull()
            .WithMessage("Category name is required");
    }
}

[Authorize(Policy = AuthConstants.Policies.Manager)]
[HttpPost(ApiRoutes.Category.Create)]
public class CreateCategoryEndpoint(AppDbContext dbContext, 
    AutoMapper.IMapper mapper,
    ICacheService cache)
    : Endpoint<CreateCategoryRequest, CreateCategoryResponse>
{
    public override async Task HandleAsync(
        CreateCategoryRequest req,
        CancellationToken ct)
    {
        using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        try
        {   

            var category = mapper.Map<Category>(req);

            await dbContext.Categories.AddAsync(category, ct);

            await dbContext.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);

            await cache.BumpVersionAsync(
                CacheKeys.CategoryListVersion,
                ct);

            await Send.OkAsync(
                response: new CreateCategoryResponse(category.Id),
                cancellation: ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}

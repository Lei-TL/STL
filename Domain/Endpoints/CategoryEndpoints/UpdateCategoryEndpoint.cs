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

public class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public record UpdateCategoryResponse(
    string Id,
    string Name,
    string? Description,
    string? Slug,
    int DisplayOrder,
    bool IsActive);

public class UpdateCategoryValidator : Validator<UpdateCategoryRequest>
{
    public UpdateCategoryValidator(AppDbContext context)
    {
        RuleFor(c => c.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage("Category name is required");
    }
}

[Authorize(Policy = AuthConstants.Policies.Manager)]
[HttpPut(ApiRoutes.Category.Update)]
public class UpdateCategoryEndpoint(
    AppDbContext dbContext,
    AutoMapper.IMapper mapper,
    ICacheService cache)
    : Endpoint<UpdateCategoryRequest, UpdateCategoryResponse>
{
    public override async Task HandleAsync(
        UpdateCategoryRequest req,
        CancellationToken ct)
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

            mapper.Map(req, category);

            await dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            await cache.BumpVersionAsync(
                CacheKeys.CategoryListVersion,
                ct);
            await cache.BumpVersionAsync(
                CacheKeys.ProductListVersion,
                ct);

            await Send.OkAsync(
                response: new UpdateCategoryResponse(
                    category.Id,
                    category.Name,
                    category.Description,
                    category.Slug,
                    category.DisplayOrder,
                    category.IsActive),
                cancellation: ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}

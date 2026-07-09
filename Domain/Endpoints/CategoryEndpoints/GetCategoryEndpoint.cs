using AutoMapper;
using AutoMapper.QueryableExtensions;
using FastEndpoints;
using STL.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using STL.DbContexts;
using STL.Models.Auth;

namespace STL.Endpoints.CategoryEndpoints
{
    public class GetCategoryRequest
    {
        public string Id { get; set; } = string.Empty;
    }

    public record GetCategoryResponse(
        string Id,
        string Name,
        string? Description,
        string? Slug,
        int DisplayOrder,
        bool IsActive,
        DateTime CreatedAt,
        DateTime? UpdatedAt);

    public class GetCategoryValidator : Validator<GetCategoryRequest> {
        public GetCategoryValidator() 
        {
            RuleFor(c => c.Id)
                .NotNull()
                .NotEmpty()
                .WithMessage("Category Id is Required!");
        }
    }

    [Authorize(Policy = AuthConstants.Policies.User)]
    [HttpGet(ApiRoutes.Category.Detail)]
    public class GetCategoryEndpoint(
        AppDbContext context,
        AutoMapper.IMapper mapper) : Endpoint<GetCategoryRequest, GetCategoryResponse>
    {
        public override async Task HandleAsync(GetCategoryRequest req, CancellationToken ct)
        {

            var category = await context.Categories
                .AsNoTracking()
                .Where(c => c.Id == req.Id && !c.Deleted)
                .ProjectTo<GetCategoryResponse>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(ct);

            if (category is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.OkAsync(response: category, ct);
        }
    }
}

using AutoMapper;
using AutoMapper.QueryableExtensions;
using FastEndpoints;
using STL.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using STL.DbContexts;
using STL.Models.Auth;

namespace STL.Endpoints.ProductEndpoints
{
    public class GetProductRequest
    {
        public string Id { get; set; } = string.Empty;
    }

    public record GetProductResponse(
        string Id,
        string Name,
        string? Description,
        string CategoryId,
        bool IsActive,
        DateTime CreatedAt,
        DateTime? UpdatedAt);

    public class GetProductValidator : Validator<GetProductRequest>
    {
        public GetProductValidator()
        {
            RuleFor(p => p.Id)
                .NotNull()
                .NotEmpty()
                .WithMessage("Product Id is Required!");
        }
    }

    [HttpGet(ApiRoutes.Product.Detail)]
    [Authorize(Policy = AuthConstants.Policies.User)]
    public class GetProductEndpoint(
        AppDbContext context,
        AutoMapper.IMapper mapper)
        : Endpoint<GetProductRequest, GetProductResponse>
    {
        public override async Task HandleAsync(GetProductRequest req, CancellationToken ct)
        {

            var product = await context.Products
                .AsNoTracking()
                .Where(p => !p.Deleted)
                .ProjectTo<GetProductResponse>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(p => p.Id == req.Id , ct);

            if (product is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.OkAsync(response: product, ct);
        }
    }
}


using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using STL.DbContexts;
using STL.Entities.CatalogModule;
using STL.Models.Auth;
using STL.SharedServices.Caching;
using STL.SharedServices.File;

namespace STL.Endpoints.ProductEndpoints;

public class ProductImportRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
}

public record ImportProductsResponse(int ImportedCount);

public class ImportProductsEndpoint(
    AppDbContext context,
    IFileService fileService,
    ICacheService cache)
    : EndpointWithoutRequest<ImportProductsResponse>
{
    public override void Configure()
    {
        Post("/api/products/import");
        Policies(AuthConstants.Policies.Manager);
        AllowFileUploads();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var file = Files.FirstOrDefault();

        if (file is null || file.Length == 0)
        {
            AddError("File is required.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        await using var stream = file.OpenReadStream();
        var rows = await fileService.ImportAsync<ProductImportRow>(
            stream,
            file.FileName,
            ct: ct);

        if (rows.Count == 0)
        {
            await Send.OkAsync(new ImportProductsResponse(0), ct);
            return;
        }

        var categoryIds = rows
            .Select(row => row.CategoryId)
            .Where(categoryId => !string.IsNullOrWhiteSpace(categoryId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var existingCategoryIds = await context.Categories
            .Where(category => categoryIds.Contains(category.Id) && !category.Deleted)
            .Select(category => category.Id)
            .ToListAsync(ct);
        var existingCategoryIdSet = existingCategoryIds.ToHashSet(
            StringComparer.OrdinalIgnoreCase);

        using var transaction = await context.Database.BeginTransactionAsync(ct);

        try
        {
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.Name)
                    || string.IsNullOrWhiteSpace(row.CategoryId)
                    || !existingCategoryIdSet.Contains(row.CategoryId))
                {
                    AddError("Product name and valid category id are required.");
                    await Send.ErrorsAsync(400, ct);
                    return;
                }

                var product = string.IsNullOrWhiteSpace(row.Id)
                    ? null
                    : await context.Products
                        .FirstOrDefaultAsync(product => product.Id == row.Id, ct);

                if (product is null)
                {
                    context.Products.Add(new Product
                    {
                        Id = string.IsNullOrWhiteSpace(row.Id)
                            ? Guid.NewGuid().ToString()
                            : row.Id,
                        Name = row.Name,
                        CategoryId = row.CategoryId,
                        Deleted = false
                    });

                    continue;
                }

                product.Name = row.Name;
                product.CategoryId = row.CategoryId;
                product.Deleted = false;
            }

            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            await cache.BumpVersionAsync(
                CacheKeys.ProductListVersion,
                ct);

            await Send.OkAsync(new ImportProductsResponse(rows.Count), ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}

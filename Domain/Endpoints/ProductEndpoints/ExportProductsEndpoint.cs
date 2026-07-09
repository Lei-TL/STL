using FastEndpoints;
using STL.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using STL.DbContexts;
using STL.Models.Auth;
using STL.SharedServices.File;

namespace STL.Endpoints.ProductEndpoints;

public class ExportProductsRequest
{
    public FileFormat Format { get; set; } = FileFormat.Excel;
}

public class ProductExportRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
}

[HttpGet(ApiRoutes.Product.Export)]
[Authorize(Policy = AuthConstants.Policies.User)]
public class ExportProductsEndpoint(
    AppDbContext context,
    IFileService fileService)
    : Endpoint<ExportProductsRequest>
{
    public override async Task HandleAsync(
        ExportProductsRequest req,
        CancellationToken ct)
    {
        var rows = await (
            from product in context.Products.AsNoTracking()
            join category in context.Categories.AsNoTracking()
                on product.CategoryId equals category.Id
            where !product.Deleted && !category.Deleted
            orderby product.Name
            select new ProductExportRow
            {
                Id = product.Id,
                Name = product.Name,
                CategoryId = product.CategoryId,
                CategoryName = category.Name
            })
            .ToListAsync(ct);

        var result = await fileService.ExportAsync(
            rows,
            req.Format,
            new FileExportOptions(
                FileName: "products",
                SheetName: "Products"),
            ct);

        await Send.BytesAsync(
            bytes: result.Content,
            fileName: result.FileName,
            contentType: result.ContentType,
            cancellation: ct);
    }
}

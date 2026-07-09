using Microsoft.EntityFrameworkCore;

namespace STL.Infrastructure.Models;

public class PagedList<T>
{
    public List<T> Data { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }

    public PagedList(List<T> data, int totalCount, int pageNumber, int pageSize)
    {
        Data = data;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    public static async Task<PagedList<T>> CreateAsync(
        IQueryable<T> query,
        int pageNumber,
        int pageSize)
    {
        var totalCount = await query.CountAsync();

        var data = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedList<T>(data, totalCount, pageNumber, pageSize);
    }
}

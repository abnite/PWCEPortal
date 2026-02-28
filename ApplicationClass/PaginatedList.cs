namespace PWCEPortal.ApplicationClass;

// PaginatedList.cs
using Microsoft.EntityFrameworkCore;

public class PaginatedList<T> : List<T>
{
    public int PageIndex { get; private set; }
    public int TotalPages { get; private set; }
    public int TotalItems { get; private set; }
    public int PageSize { get; private set; }
    
    public int FirstItemOnPage => (PageIndex - 1) * PageSize + 1;
    public int LastItemOnPage => Math.Min(PageIndex * PageSize, TotalItems);


    public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        PageSize = pageSize; 
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalItems = count;

        this.AddRange(items);
    }

    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source, int pageIndex, int pageSize)
    {
        var count = await source.CountAsync();
        var items = await source.Skip(
                (pageIndex - 1) * pageSize)
            .Take(pageSize).ToListAsync();
        return new PaginatedList<T>(items, count, pageIndex, pageSize);
    }
}
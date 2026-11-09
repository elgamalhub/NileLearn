namespace NileLearn.Application.Common.Models;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 10));
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public static PagedResult<T> Create(IReadOnlyList<T> items, int pageIndex, int pageSize, int totalCount)
    {
        return new PagedResult<T>
        {
            Items = items,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MessageAr { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success", string? messageAr = "تمت العملية بنجاح")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            MessageAr = messageAr,
            Data = data
        };
    }

    public static ApiResponse<T> Fail(string message, string? messageAr = null, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            MessageAr = messageAr,
            Errors = errors ?? new List<string> { message }
        };
    }
}

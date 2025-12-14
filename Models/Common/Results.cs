namespace TicketingASP.Models.Common;

/// <summary>
/// Generic paginated result wrapper
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int Page => PageNumber;  // Alias for PageNumber
    public int PageSize { get; set; }
    public long TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Generic operation result wrapper
/// </summary>
public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();

    public static OperationResult SuccessResult(string message = "Operation completed successfully")
        => new() { Success = true, Message = message };

    public static OperationResult FailureResult(string message, params string[] errors)
        => new() { Success = false, Message = message, Errors = errors.ToList() };
}

/// <summary>
/// Generic operation result with data
/// </summary>
public class OperationResult<T> : OperationResult
{
    public T? Data { get; set; }

    public static OperationResult<T> SuccessResult(T data, string message = "Operation completed successfully")
        => new() { Success = true, Message = message, Data = data };

    public new static OperationResult<T> FailureResult(string message, params string[] errors)
        => new() { Success = false, Message = message, Errors = errors.ToList() };
}

/// <summary>
/// Database stored procedure result
/// </summary>
public class SpResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Database stored procedure result with ID
/// </summary>
public class SpResultWithId : SpResult
{
    public int Id { get; set; }
}

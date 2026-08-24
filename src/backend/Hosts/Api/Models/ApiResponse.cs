namespace Api.Models;

/// <summary>
/// Generic API response wrapper for consistent response format.
/// </summary>
/// <typeparam name="T">Type of the data payload</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the request was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// HTTP status code of the response.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Optional message providing additional context.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// The response data payload.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Creates a successful response with data.
    /// </summary>
    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            StatusCode = 200,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// Creates a successful response with data and custom status code.
    /// </summary>
    public static ApiResponse<T> Created(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            StatusCode = 201,
            Message = message ?? "Resource created successfully",
            Data = data
        };
    }

    /// <summary>
    /// Creates a failure response.
    /// </summary>
    public static ApiResponse<T> Fail(int statusCode, string message)
    {
        return new ApiResponse<T>
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            Data = default
        };
    }

    /// <summary>
    /// Creates a not found response.
    /// </summary>
    public static ApiResponse<T> NotFound(string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            StatusCode = 404,
            Message = message ?? "Resource not found",
            Data = default
        };
    }

    /// <summary>
    /// Creates a bad request response.
    /// </summary>
    public static ApiResponse<T> BadRequest(string message)
    {
        return new ApiResponse<T>
        {
            Success = false,
            StatusCode = 400,
            Message = message,
            Data = default
        };
    }
}

/// <summary>
/// Non-generic API response for operations without data payload.
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string? Message { get; set; }

    public static ApiResponse Ok(string? message = null)
    {
        return new ApiResponse
        {
            Success = true,
            StatusCode = 200,
            Message = message ?? "Operation completed successfully"
        };
    }

    public static ApiResponse Fail(int statusCode, string message)
    {
        return new ApiResponse
        {
            Success = false,
            StatusCode = statusCode,
            Message = message
        };
    }
}

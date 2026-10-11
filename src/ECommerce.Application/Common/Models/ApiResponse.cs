namespace ECommerce.Application.Common.Models;

public class ApiResponse<T>
{
    public bool Succeeded { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Success(T data, string message = "Success")
    {
        return new ApiResponse<T>
        {
            Succeeded = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Failure(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Succeeded = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }

    public static ApiResponse<T> Failure(string message, string error)
    {
        return new ApiResponse<T>
        {
            Succeeded = false,
            Message = message,
            Errors = new List<string> { error }
        };
    }
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Success(string message = "Success")
    {
        return new ApiResponse
        {
            Succeeded = true,
            Message = message
        };
    }

    public new static ApiResponse Failure(string message, List<string>? errors = null)
    {
        return new ApiResponse
        {
            Succeeded = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }
}

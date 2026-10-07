using System.Text.Json.Serialization;

namespace Construction.Application.Common;

public enum ApiErrorKind
{
    None,
    BadRequest,
    NotFound,
    Unauthorized
}

public class ApiResult<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    [JsonIgnore]
    public ApiErrorKind ErrorKind { get; set; }

    public static ApiResult<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };

    public static ApiResult<T> Fail(string message, ApiErrorKind kind = ApiErrorKind.BadRequest) => new()
    {
        Success = false,
        Message = message,
        ErrorKind = kind
    };
}

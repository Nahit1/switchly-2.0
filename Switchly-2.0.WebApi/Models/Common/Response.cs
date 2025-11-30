namespace Switchly_2._0.WebApi.Models.Common;

public sealed class Response<T>
{
    public bool Success { get; init; } = true;
    public string? Message { get; init; }
    public T? Data { get; init; }
    
    public Dictionary<string, string[]>? Errors { get; init; }

    public static Response<T> Ok(T data, string? message = null) =>
        new() { Success = true, Message = message, Data = data };

    public static Response<T> Fail(string message, Dictionary<string,string[]>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}
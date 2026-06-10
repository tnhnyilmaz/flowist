namespace Flowist.Shared.DTOs;

public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    IReadOnlyCollection<string> Errors,
    int StatusCode)
{
    public static ApiResponse<T> Ok(T data, int statusCode = 200)
    {
        return new ApiResponse<T>(true, data, Array.Empty<string>(), statusCode);
    }

    public static ApiResponse<T> Fail(IEnumerable<string> errors, int statusCode)
    {
        return new ApiResponse<T>(false, default, errors.ToArray(), statusCode);
    }
}
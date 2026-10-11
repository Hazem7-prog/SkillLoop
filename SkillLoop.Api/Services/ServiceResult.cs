using SkillLoop.Api.DTOs;

namespace SkillLoop.Api.Services;

// Lets a service say "success" or "failed with this HTTP status and { error, code }"
public record ServiceResult<T>(T? Value, ApiError? Error, int StatusCode)
{
    public bool Success => Error is null;

    public static ServiceResult<T> Ok(T value, int status = 200) => new(value, null, status);

    public static ServiceResult<T> Fail(int status, string message, string code) =>
        new(default, new ApiError(message, code), status);
}
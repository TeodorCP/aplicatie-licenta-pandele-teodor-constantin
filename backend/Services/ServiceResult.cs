namespace BusinessOps.Backend.Services;

public enum ServiceStatus
{
    Ok,
    Created,
    NoContent,
    BadRequest,
    NotFound,
    Forbidden,
    Unauthorized
}

public sealed record ServiceResult<T>(ServiceStatus Status, T? Value = default, string? Error = null)
{
    public static ServiceResult<T> Ok(T value) => new(ServiceStatus.Ok, value);

    public static ServiceResult<T> Created(T value) => new(ServiceStatus.Created, value);

    public static ServiceResult<T> NoContent() => new(ServiceStatus.NoContent);

    public static ServiceResult<T> BadRequest(string error) => new(ServiceStatus.BadRequest, default, error);

    public static ServiceResult<T> NotFound(string error) => new(ServiceStatus.NotFound, default, error);

    public static ServiceResult<T> Forbidden() => new(ServiceStatus.Forbidden);

    public static ServiceResult<T> Unauthorized(string error) => new(ServiceStatus.Unauthorized, default, error);
}

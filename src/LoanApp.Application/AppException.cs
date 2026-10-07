namespace LoanApp.Application;

public sealed class AppException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }
    public object? Details { get; }

    public AppException(string code, string message, int statusCode = 400, object? details = null)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
        Details = details;
    }

    public static AppException NotFound(string message = "Resource not found.") =>
        new("not_found", message, 404);

    public static AppException Conflict(string code, string message) =>
        new(code, message, 409);

    public static AppException Forbidden(string message = "You cannot access this resource.") =>
        new("forbidden", message, 403);
}

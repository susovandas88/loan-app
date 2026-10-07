using System.Net;
using System.Text.Json;
using LoanApp.Application;
using LoanApp.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LoanApp.Api.Middleware;

public sealed class ExceptionMappingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMappingMiddleware> _logger;

    public ExceptionMappingMiddleware(RequestDelegate next, ILogger<ExceptionMappingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            await WriteAsync(context, ex.StatusCode, new ApiError(ex.Code, ex.Message, ex.Details));
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update failed");
            await WriteAsync(context, (int)HttpStatusCode.Conflict, new ApiError("save_failed", "Could not save. Try the upload again."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteAsync(context, (int)HttpStatusCode.InternalServerError, new ApiError("server_error", "An unexpected error occurred."));
        }
    }

    private static async Task WriteAsync(HttpContext context, int status, ApiError error)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(error, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}

using System.Net;
using System.Text.Json;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Models;

namespace ECommerce.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message, errors) = exception switch
        {
            ValidationException valEx => (
                HttpStatusCode.BadRequest,
                valEx.Message,
                valEx.Errors.Count > 0 ? valEx.Errors : new List<string> { valEx.Message }
            ),
            NotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                notFoundEx.Message,
                new List<string> { notFoundEx.Message }
            ),
            UnauthorizedException unauthEx => (
                HttpStatusCode.Unauthorized,
                unauthEx.Message,
                new List<string> { unauthEx.Message }
            ),
            ApiException apiEx => (
                apiEx.StatusCode,
                apiEx.Message,
                apiEx.Errors.Count > 0 ? apiEx.Errors : new List<string> { apiEx.Message }
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected server error occurred.",
                new List<string> { "An internal server error occurred. Please try again later." }
            )
        };

        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.Failure(message, errors);
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

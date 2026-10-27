using System.Net;
using System.Text.Json;
using SkillHub.Domain.Exceptions;
using ApplicationException = SkillHub.Application.Common.Exceptions.ApplicationException;

namespace SkillHub.Api.Middleware;

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
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain Exception interceptada: {Message}", ex.Message);
            await HandleCustomExceptionAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (ApplicationException ex)
        {
            _logger.LogWarning(ex, "Application Exception interceptada: {Message}", ex.Message);
            await HandleCustomExceptionAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción no controlada interceptada: {Message}", ex.Message);
            await HandleCustomExceptionAsync(context, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado en el servidor.");
        }
    }

    private static async Task HandleCustomExceptionAsync(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            error = message
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}

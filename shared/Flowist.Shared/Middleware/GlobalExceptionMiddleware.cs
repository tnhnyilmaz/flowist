using System.Net;
using System.Runtime.ExceptionServices;
using System.Text.Json;

using Flowist.Shared.Exceptions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Flowist.Shared.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogError(exception, "Exception occurred after the response started.");
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        ProblemDetails problemDetails = exception switch
        {
            ValidationException validationException => CreateValidationProblemDetails(context, validationException),
            NotFoundException notFoundException => CreateProblemDetails(context, notFoundException, StatusCodes.Status404NotFound, "Resource not found"),
            ForbiddenAccessException forbiddenException => CreateProblemDetails(context, forbiddenException, StatusCodes.Status403Forbidden, "Forbidden"),
            ConflictException conflictException => CreateProblemDetails(context, conflictException, StatusCodes.Status409Conflict, "Conflict"),
            BusinessRuleException businessRuleException => CreateProblemDetails(context, businessRuleException, StatusCodes.Status400BadRequest, "Business rule violation"),
            _ => CreateUnhandledProblemDetails(context)
        };

        LogException(exception, problemDetails.Status ?? StatusCodes.Status500InternalServerError);

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, JsonOptions));
    }

    private static ProblemDetails CreateProblemDetails(HttpContext context, Exception exception, int statusCode, string title)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path
        };
    }

    private static ProblemDetails CreateValidationProblemDetails(HttpContext context, ValidationException exception)
    {
        Dictionary<string, string[]> errors = exception.Errors.ToDictionary(
            error => error.Key,
            error => error.Value);

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = exception.Message,
            Instance = context.Request.Path
        };
    }

    private static ProblemDetails CreateUnhandledProblemDetails(HttpContext context)
    {
        return new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal server error",
            Detail = "An unexpected error occurred.",
            Instance = context.Request.Path
        };
    }

    private void LogException(Exception exception, int statusCode)
    {
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred.");
            return;
        }

        _logger.LogWarning(exception, "Handled exception occurred with status code {StatusCode}.", statusCode);
    }
}
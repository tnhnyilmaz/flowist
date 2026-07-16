using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Flowist.Shared.Middleware;

public sealed class RequestContextLoggingMiddleware
{


    private const string WorkspaceIdHeaderName = "X-Workspace-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestContextLoggingMiddleware> _logger;

    public RequestContextLoggingMiddleware(RequestDelegate next, ILogger<RequestContextLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string? userId = GetUserId(context);
        string? workspaceId = GetWorkspaceId(context);

        Dictionary<string, object> loggingScope = new();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            loggingScope["UserId"] = userId;
        }
        if (!string.IsNullOrWhiteSpace(workspaceId))
        {
            loggingScope["WorkspaceId"] = workspaceId;
        }

        using (_logger.BeginScope(loggingScope))
        {
            await _next(context);
        }
    }





    private static string? GetUserId(HttpContext context)
    {
        return context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub")
            ?? context.User.FindFirstValue("userId");
    }

    private static string? GetWorkspaceId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(WorkspaceIdHeaderName, out var workspaceIdHeader))
        {
            return workspaceIdHeader.FirstOrDefault();
        }

        if (context.Request.RouteValues.TryGetValue("workspaceId", out object? workspaceIdRouteValue))
        {
            return workspaceIdRouteValue?.ToString();
        }

        return null;
    }
}
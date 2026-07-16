using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Flowist.Shared.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _logger = logger;
        _next = next;
    }


    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = GetOrCreateCorrelation(context);

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await _next(context);
        }
    }

    private string GetOrCreateCorrelation(HttpContext context)
    {
        string? correlationId = context.Request.Headers[HeaderName]
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }
        return Guid.NewGuid().ToString("N");
    }
}
using Flowist.Shared.Middleware;

using Microsoft.AspNetCore.Builder;

namespace Flowist.Shared.Extensions;

public static class CorrelationIdExtensions
{
    public static IApplicationBuilder UseCorrelationId(
        this IApplicationBuilder applicationBuilder)
    {
        return applicationBuilder.UseMiddleware<CorrelationIdMiddleware>();
    }
}
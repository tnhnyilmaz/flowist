using Flowist.Shared.Middleware;

using Microsoft.AspNetCore.Builder;

namespace Flowist.Shared.Extensions;

public static class RequestContextLoggingExtensions
{
    public static IApplicationBuilder UseRequestContextLogging(this IApplicationBuilder applicationBuilder)
    {
        return applicationBuilder.UseMiddleware<RequestContextLoggingMiddleware>();
    }
}
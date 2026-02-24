using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bank.Obs.DemoApi.Middleware;

public sealed class CorrelationIdMiddleware : IMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var corrId = context.Request.Headers.TryGetValue(HeaderName, out var h) && !string.IsNullOrWhiteSpace(h)
            ? h.ToString()
            : Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderName] = corrId;

        using (context.RequestServices.GetRequiredService<ILogger<CorrelationIdMiddleware>>()
                   .BeginScope(new Dictionary<string, object> { ["correlation_id"] = corrId }))
        {
            await next(context);
        }
    }
}
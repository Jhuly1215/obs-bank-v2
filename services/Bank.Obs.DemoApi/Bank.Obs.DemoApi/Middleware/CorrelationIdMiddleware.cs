using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bank.Obs.DemoApi.Middleware;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string HeaderKey = "X-Correlation-Id";
    private static readonly Regex Allowed = new(@"^[a-zA-Z0-9\-_]{8,128}$", RegexOptions.Compiled);

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderKey].FirstOrDefault();
        if (string.IsNullOrEmpty(correlationId) || !Allowed.IsMatch(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderKey] = correlationId;

        using (Serilog.Context.LogContext.PushProperty("correlation_id", correlationId))
        {
            await _next(context);
        }
    }
}
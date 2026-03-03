using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bank.Obs.DemoApi.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _log;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            // Si ya se escribió parte de la respuesta, no intentes reescribir JSON
            if (httpContext.Response.HasStarted)
            {
                _log.LogError(ex, "Unhandled exception after response started, traceId={TraceId}", httpContext.TraceIdentifier);
                throw;
            }

            var traceId = httpContext.TraceIdentifier;
            _log.LogError(ex, "Unhandled exception, traceId={TraceId}", traceId);

            var problem = new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = $"TraceId: {traceId}"
            };

            httpContext.Response.StatusCode = problem.Status.Value;
            httpContext.Response.ContentType = "application/problem+json";

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await httpContext.Response.WriteAsync(JsonSerializer.Serialize(problem, options));
        }
    }
}
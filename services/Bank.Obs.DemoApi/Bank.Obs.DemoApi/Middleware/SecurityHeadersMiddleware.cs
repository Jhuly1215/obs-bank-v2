using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        // CSP mínimo: ajustar según needs
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'";
        await _next(context);
    }
}
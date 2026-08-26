using Serilog.Context;

namespace LimsControlLab.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private const string CorrelationIdProperty = "CorrelationId";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValue)
            ? headerValue.ToString()
            : Guid.NewGuid().ToString();

        context.Items[CorrelationIdProperty] = correlationId;
        context.Response.Headers.Append(CorrelationIdHeader, correlationId);

        using (LogContext.PushProperty(CorrelationIdProperty, correlationId))
        {
            await _next(context);
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();

    public static string? GetCorrelationId(this HttpContext context)
        => context.Items.TryGetValue("CorrelationId", out var id) ? id?.ToString() : null;
}

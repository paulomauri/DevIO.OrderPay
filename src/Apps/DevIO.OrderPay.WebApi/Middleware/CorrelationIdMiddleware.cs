// WebApi/Middleware/CorrelationIdMiddleware.cs
using Serilog.Context;

namespace DevIO.OrderPay.WebApi.Middleware;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrGenerateCorrelationId(context);

        // add to response headers so client can trace it
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // add to Serilog log context — appears in ALL logs for this request
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private static string GetOrGenerateCorrelationId(HttpContext context)
    {
        // use existing header if client sent one, otherwise generate new
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var existingId)
            && !string.IsNullOrWhiteSpace(existingId))
        {
            return existingId!;
        }

        return Guid.NewGuid().ToString();
    }
}
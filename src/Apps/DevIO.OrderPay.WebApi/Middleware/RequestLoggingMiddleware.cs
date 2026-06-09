// WebApi/Middleware/RequestLoggingMiddleware.cs
using System.Diagnostics;

namespace DevIO.OrderPay.WebApi.Middleware;

public class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next = next;
    private readonly ILogger<RequestLoggingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = context.Response.Headers[CorrelationIdHeader].ToString();
        var sw = Stopwatch.StartNew();

        try
        {
            await _next(context);
            sw.Stop();

            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms | CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds,
                correlationId);
        }
        catch (Exception ex)
        {
            sw.Stop();

            _logger.LogError(ex,
                "HTTP {Method} {Path} failed after {ElapsedMs}ms | CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                sw.ElapsedMilliseconds,
                correlationId);

            throw;
        }
    }
}

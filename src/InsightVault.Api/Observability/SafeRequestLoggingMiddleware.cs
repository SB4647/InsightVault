using System.Diagnostics;

namespace InsightVault.Api.Observability;

/// <summary>Logs request metadata without reading request bodies, response bodies, headers, or secrets.</summary>
public sealed class SafeRequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<SafeRequestLoggingMiddleware> logger)
{
    /// <summary>Executes the request and records only diagnostic metadata that is safe to retain in logs.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(
                "Request failed. TraceId={TraceId} Method={Method} Path={Path} ErrorType={ErrorType}",
                context.TraceIdentifier,
                context.Request.Method,
                context.Request.Path.Value,
                exception.GetType().Name);
            throw;
        }
        finally
        {
            logger.LogInformation(
                "Request completed. TraceId={TraceId} Method={Method} Path={Path} StatusCode={StatusCode} DurationMs={DurationMs} DocumentId={DocumentId}",
                context.TraceIdentifier,
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                context.Request.RouteValues.TryGetValue("id", out var documentId) ? documentId : null);
        }
    }
}

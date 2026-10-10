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
                "Request failed. TraceId={TraceId} Method={Method} Route={Route} ErrorType={ErrorType}",
                context.TraceIdentifier,
                context.Request.Method,
                GetSafeRoute(context),
                exception.GetType().Name);
            throw;
        }
        finally
        {
            logger.LogInformation(
                "Request completed. TraceId={TraceId} Method={Method} Route={Route} StatusCode={StatusCode} DurationMs={DurationMs} DocumentId={DocumentId}",
                context.TraceIdentifier,
                context.Request.Method,
                GetSafeRoute(context),
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                GetSafeDocumentId(context));
        }
    }

    private static string GetSafeRoute(HttpContext context)
    {
        return context.GetEndpoint() is Microsoft.AspNetCore.Routing.RouteEndpoint endpoint
            ? endpoint.RoutePattern.RawText ?? "unknown"
            : "unmatched";
    }

    private static Guid? GetSafeDocumentId(HttpContext context)
    {
        return context.Request.RouteValues.TryGetValue("id", out var value)
               && Guid.TryParse(value?.ToString(), out var documentId)
            ? documentId
            : null;
    }
}

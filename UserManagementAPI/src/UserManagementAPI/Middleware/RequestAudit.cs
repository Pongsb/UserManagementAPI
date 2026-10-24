using System.Diagnostics;

namespace UserManagementAPI.Middleware;

internal static class RequestAudit
{
    private static readonly object RegisteredKey = new();

    public static void Register(HttpContext context, ILogger logger)
    {
        if (context.Items.ContainsKey(RegisteredKey))
            return;

        context.Items[RegisteredKey] = true;
        var started = Stopwatch.GetTimestamp();
        logger.LogInformation("HTTP request {Method} {Path}",
            context.Request.Method, context.Request.Path);

        // OnCompleted observes the final 500 even if outer error middleware
        // changes the response after the inner logging middleware unwinds.
        context.Response.OnCompleted(() =>
        {
            logger.LogInformation("HTTP response {Method} {Path} {StatusCode} {ElapsedMs}ms",
                context.Request.Method, context.Request.Path, context.Response.StatusCode,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            return Task.CompletedTask;
        });
    }
}

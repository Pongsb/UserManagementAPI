using UserManagementAPI.Models;

namespace UserManagementAPI.Middleware;

public sealed class ErrorHandlingMiddleware(
    RequestDelegate next,
    ILogger<ErrorHandlingMiddleware> logger,
    ILogger<RequestLoggingMiddleware> auditLogger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // A disconnected client cannot receive a JSON response.
            context.Abort();
        }
        catch (Exception exception)
        {
            RequestAudit.Register(context, auditLogger);
            logger.LogError(exception, "Unhandled error for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
            {
                // Never attempt to replace headers or JSON after a response starts.
                context.Abort();
                return;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new ApiError("Internal server error."));
        }
    }
}

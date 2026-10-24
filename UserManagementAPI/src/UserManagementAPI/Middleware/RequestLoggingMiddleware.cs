namespace UserManagementAPI.Middleware;

public sealed class RequestLoggingMiddleware(
    RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        RequestAudit.Register(context, logger);
        await next(context);
    }
}

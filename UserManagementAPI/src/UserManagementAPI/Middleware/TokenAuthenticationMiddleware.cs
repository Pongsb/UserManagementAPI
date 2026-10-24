using System.Net.Http.Headers;
using UserManagementAPI.Models;
using UserManagementAPI.Services;

namespace UserManagementAPI.Middleware;

public sealed class TokenAuthenticationMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> auditLogger)
{
    public async Task InvokeAsync(HttpContext context, TokenValidator validator)
    {
        var headers = context.Request.Headers.Authorization;
        var valid = headers.Count == 1 &&
            AuthenticationHeaderValue.TryParse(headers[0], out var header) &&
            string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(header.Parameter) &&
            validator.IsValid(header.Parameter);

        if (!valid)
        {
            // Authentication short-circuits before logging in the required order.
            // Use the shared audit helper so rejected requests are still logged.
            RequestAudit.Register(context, auditLogger);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            await context.Response.WriteAsJsonAsync(
                new ApiError("Unauthorized. A valid Bearer token is required."));
            return;
        }

        await next(context);
    }
}

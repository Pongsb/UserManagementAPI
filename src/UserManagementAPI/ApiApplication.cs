using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Middleware;
using UserManagementAPI.Models;
using UserManagementAPI.Services;

namespace UserManagementAPI;

public static class ApiApplication
{
    // The optional callback lets the test runner configure a real local HTTP server.
    public static WebApplication Build(
        string[] args, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        configure?.Invoke(builder);

        var token = builder.Configuration["Authentication:Token"];
        if (string.IsNullOrWhiteSpace(token) || token.Length < 32)
        {
            throw new InvalidOperationException(
                "Set Authentication__Token to a private token of at least 32 characters.");
        }

        builder.Services.AddSingleton(new TokenValidator(token));
        builder.Services.AddSingleton<UserStore>();
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(Controllers.UsersController).Assembly)
            .AddJsonOptions(options => options.JsonSerializerOptions.DefaultIgnoreCondition =
                JsonIgnoreCondition.WhenWritingNull);
        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            // Keep empty framework error results available to our JSON status pages
            // instead of letting MVC replace them with a different ProblemDetails shape.
            options.SuppressMapClientErrors = true;
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(entry => entry.Value?.Errors.Count > 0)
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value!.Errors.Select(error =>
                            string.IsNullOrWhiteSpace(error.ErrorMessage)
                                ? "The request contains invalid data."
                                : error.ErrorMessage).ToArray());
                return new BadRequestObjectResult(new ApiError("Validation failed.", errors));
            };
        });

        var app = builder.Build();

        // Activity 3 requires this exact order for the three custom middleware.
        app.UseMiddleware<ErrorHandlingMiddleware>();
        app.UseMiddleware<TokenAuthenticationMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();

        // Give framework-generated 404, 405 and 415 responses the same JSON shape.
        app.UseStatusCodePages(async statusContext =>
        {
            var context = statusContext.HttpContext;
            var message = context.Response.StatusCode switch
            {
                404 => "Resource not found.",
                405 => "HTTP method not allowed.",
                415 => "Unsupported media type. Use application/json.",
                _ => "The request could not be completed."
            };
            await context.Response.WriteAsJsonAsync(new ApiError(message));
        });

        app.MapControllers();

        // Deliberately unavailable in Development and Production.
        // Tests opt in to this endpoint to prove that exceptions become JSON 500s.
        if (app.Environment.IsEnvironment("Testing") &&
            app.Configuration.GetValue<bool>("Testing:EnableExceptionEndpoint"))
        {
            app.MapGet("/test/exception", (HttpContext _) =>
                Task.FromException(
                    new InvalidOperationException("Intentional test exception.")));
        }

        return app;
    }
}

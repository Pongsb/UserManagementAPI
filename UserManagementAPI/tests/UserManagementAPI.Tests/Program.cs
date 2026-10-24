using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UserManagementAPI;
using UserManagementAPI.Models;
using UserManagementAPI.Tests;

// Generate an isolated test credential per run; never commit a usable token.
var testToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
var logs = new TestLogProvider();
var passed = 0;
var failed = 0;

async Task Check(string name, Func<Task> test)
{
    try
    {
        await test();
        passed++;
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception exception)
    {
        failed++;
        Console.WriteLine($"FAIL {name}: {exception.Message}");
    }
}

void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

void Status(HttpResponseMessage response, HttpStatusCode expected) =>
    Assert(response.StatusCode == expected,
        $"Expected {(int)expected}, received {(int)response.StatusCode}.");

WebApplication NewApp(string environment, bool exceptionEndpoint)
{
    return ApiApplication.Build(new[]
    {
        "--environment", environment,
        "--Authentication:Token", testToken,
        "--Testing:EnableExceptionEndpoint", exceptionEndpoint.ToString()
    }, builder =>
    {
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(logs);
    });
}

await using var app = NewApp("Testing", true);
await app.StartAsync();
using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };

async Task<HttpResponseMessage> Send(
    HttpMethod method, string path, string? json = null,
    string? authorization = null, string contentType = "application/json",
    bool authenticate = true)
{
    using var request = new HttpRequestMessage(method, path);
    if (authenticate)
        request.Headers.TryAddWithoutValidation("Authorization", authorization ?? "Bearer " + testToken);
    if (json is not null)
        request.Content = new StringContent(json, Encoding.UTF8, contentType);
    return await client.SendAsync(request);
}

async Task Error(HttpResponseMessage response, HttpStatusCode expected)
{
    Status(response, expected);
    Assert(response.Content.Headers.ContentType?.MediaType == "application/json", "Expected JSON.");
    using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    Assert(body.RootElement.TryGetProperty("error", out var error) &&
        !string.IsNullOrWhiteSpace(error.GetString()), "Expected an error message.");
}

const string Valid = """{"name":"Demo User","email":"demo.user@example.com"}""";
var firstId = 0;
var secondId = 0;

await Check("Missing token returns JSON 401", async () =>
{
    using var response = await Send(HttpMethod.Get, "/users", authenticate: false);
    await Error(response, HttpStatusCode.Unauthorized);
    Assert(response.Headers.WwwAuthenticate.ToString() == "Bearer", "Missing Bearer challenge.");
});
await Check("Invalid token returns JSON 401", async () =>
{
    using var response = await Send(HttpMethod.Get, "/users", authorization: "Bearer invalid");
    await Error(response, HttpStatusCode.Unauthorized);
});
await Check("Basic scheme is rejected", async () =>
{
    using var response = await Send(HttpMethod.Get, "/users", authorization: "Basic " + testToken);
    await Error(response, HttpStatusCode.Unauthorized);
});
await Check("Bearer without credentials is rejected", async () =>
{
    using var response = await Send(HttpMethod.Get, "/users", authorization: "Bearer");
    await Error(response, HttpStatusCode.Unauthorized);
});
await Check("Invalid token cannot create a user", async () =>
{
    using var response = await Send(HttpMethod.Post, "/users", Valid, "Bearer invalid");
    await Error(response, HttpStatusCode.Unauthorized);
});
await Check("Valid token retrieves an initially empty list", async () =>
{
    using var response = await Send(HttpMethod.Get, "/users");
    Status(response, HttpStatusCode.OK);
    Assert((await response.Content.ReadFromJsonAsync<User[]>())!.Length == 0, "Expected empty store.");
});
await Check("POST creates a user and returns a working Location", async () =>
{
    using var response = await Send(HttpMethod.Post, "/users", Valid);
    Status(response, HttpStatusCode.Created);
    var user = (await response.Content.ReadFromJsonAsync<User>())!;
    firstId = user.Id;
    Assert(firstId > 0 && user.Name == "Demo User" && user.Email == "demo.user@example.com", "Wrong created user.");
    Assert(response.Headers.Location is not null, "Missing Location header.");
    using var read = await Send(HttpMethod.Get, response.Headers.Location!.ToString());
    Status(read, HttpStatusCode.OK);
    Assert((await read.Content.ReadFromJsonAsync<User>()) == user, "Location returned different user.");
});
await Check("GET by ID retrieves the created user", async () =>
{
    using var response = await Send(HttpMethod.Get, $"/users/{firstId}");
    Status(response, HttpStatusCode.OK);
    Assert((await response.Content.ReadFromJsonAsync<User>())!.Name == "Demo User", "Wrong name.");
});
await Check("GET all includes the created user", async () =>
{
    using var response = await Send(HttpMethod.Get, "/users");
    Status(response, HttpStatusCode.OK);
    Assert((await response.Content.ReadFromJsonAsync<User[]>())!.Any(user => user.Id == firstId),
        "Created user is missing.");
});
await Check("Bearer scheme is case insensitive", async () =>
{
    using var response = await Send(HttpMethod.Get, "/users", authorization: "bearer " + testToken);
    Status(response, HttpStatusCode.OK);
});

var invalidBodies = new (string Name, string Body)[]
{
    ("Empty object", "{}"),
    ("Empty name", """{"name":"","email":"ok@example.com"}"""),
    ("Whitespace name", """{"name":"   ","email":"ok@example.com"}"""),
    ("Invalid email", """{"name":"Demo User","email":"invalid-email"}"""),
    ("Missing email", """{"name":"Demo User"}"""),
    ("Null name", """{"name":null,"email":"ok@example.com"}"""),
    ("Null body", "null"),
    ("Malformed JSON", "{broken"),
    ("Wrong field type", """{"name":42,"email":"ok@example.com"}"""),
    ("Name over 100 characters", JsonSerializer.Serialize(new { name = new string('a', 101), email = "ok@example.com" })),
    ("Email over 254 characters", JsonSerializer.Serialize(new { name = "Demo User", email = new string('a', 250) + "@example.com" }))
};
foreach (var item in invalidBodies)
{
    await Check(item.Name + " returns JSON 400", async () =>
    {
        using var response = await Send(HttpMethod.Post, "/users", item.Body);
        await Error(response, HttpStatusCode.BadRequest);
    });
}
await Check("Empty request body returns JSON 400", async () =>
{
    using var response = await Send(HttpMethod.Post, "/users", "");
    await Error(response, HttpStatusCode.BadRequest);
});
await Check("POST without a content type returns JSON 415", async () =>
{
    using var response = await Send(HttpMethod.Post, "/users");
    await Error(response, HttpStatusCode.UnsupportedMediaType);
});
await Check("Unsupported content type returns JSON 415", async () =>
{
    using var response = await Send(HttpMethod.Post, "/users", Valid, contentType: "text/plain");
    await Error(response, HttpStatusCode.UnsupportedMediaType);
});
await Check("Duplicate email is rejected case insensitively", async () =>
{
    using var response = await Send(HttpMethod.Post, "/users",
        """{"name":"Another","email":"DEMO.USER@EXAMPLE.COM"}""");
    await Error(response, HttpStatusCode.Conflict);
});
await Check("PUT updates an existing user and keeps its ID", async () =>
{
    using var response = await Send(HttpMethod.Put, $"/users/{firstId}",
        """{"name":"Updated User","email":"updated.user@example.com"}""");
    Status(response, HttpStatusCode.OK);
    var user = (await response.Content.ReadFromJsonAsync<User>())!;
    Assert(user.Id == firstId && user.Name == "Updated User", "Update changed ID or failed.");
});
await Check("PUT permits keeping one's own email", async () =>
{
    using var response = await Send(HttpMethod.Put, $"/users/{firstId}",
        """{"name":"Updated User Revised","email":"updated.user@example.com"}""");
    Status(response, HttpStatusCode.OK);
});
await Check("A former email becomes reusable after an update", async () =>
{
    using var response = await Send(HttpMethod.Post, "/users", Valid);
    Status(response, HttpStatusCode.Created);
    secondId = (await response.Content.ReadFromJsonAsync<User>())!.Id;
    Assert(secondId != firstId, "IDs must be unique.");
});
await Check("PUT rejects another user's email", async () =>
{
    using var response = await Send(HttpMethod.Put, $"/users/{secondId}",
        """{"name":"Conflict","email":"UPDATED.USER@EXAMPLE.COM"}""");
    await Error(response, HttpStatusCode.Conflict);
});
await Check("Invalid PUT leaves the original record unchanged", async () =>
{
    using var response = await Send(HttpMethod.Put, $"/users/{secondId}",
        """{"name":"","email":"bad"}""");
    await Error(response, HttpStatusCode.BadRequest);
    using var read = await Send(HttpMethod.Get, $"/users/{secondId}");
    var user = (await read.Content.ReadFromJsonAsync<User>())!;
    Assert(user.Name == "Demo User" && user.Email == "demo.user@example.com", "Invalid PUT mutated data.");
});
foreach (var method in new[] { HttpMethod.Get, HttpMethod.Put, HttpMethod.Delete })
{
    await Check($"{method} unknown ID returns JSON 404", async () =>
    {
        using var response = await Send(method, "/users/999999",
            method == HttpMethod.Put ? Valid : null);
        await Error(response, HttpStatusCode.NotFound);
    });
}
await Check("Noninteger ID returns JSON 404", async () =>
{
    using var response = await Send(HttpMethod.Get, "/users/not-an-integer");
    await Error(response, HttpStatusCode.NotFound);
});
await Check("Unknown route returns JSON 404", async () =>
{
    using var response = await Send(HttpMethod.Get, "/unknown");
    await Error(response, HttpStatusCode.NotFound);
});
await Check("Unsupported method returns JSON 405", async () =>
{
    using var response = await Send(HttpMethod.Patch, $"/users/{firstId}", Valid);
    await Error(response, HttpStatusCode.MethodNotAllowed);
});
await Check("DELETE returns an empty 204 response", async () =>
{
    using var response = await Send(HttpMethod.Delete, $"/users/{firstId}");
    Status(response, HttpStatusCode.NoContent);
    Assert((await response.Content.ReadAsByteArrayAsync()).Length == 0, "204 must be empty.");
});
await Check("GET deleted user returns JSON 404", async () =>
{
    using var response = await Send(HttpMethod.Get, $"/users/{firstId}");
    await Error(response, HttpStatusCode.NotFound);
});
await Check("A deleted user's email can be reused", async () =>
{
    using var response = await Send(HttpMethod.Post, "/users",
        """{"name":"New owner","email":"updated.user@example.com"}""");
    Status(response, HttpStatusCode.Created);
    Assert((await response.Content.ReadFromJsonAsync<User>())!.Id > secondId, "ID was reused.");
});
await Check("Concurrent unique creates produce unique IDs", async () =>
{
    var users = await Task.WhenAll(Enumerable.Range(0, 20).Select(async index =>
    {
        using var response = await Send(HttpMethod.Post, "/users",
            JsonSerializer.Serialize(new { name = $"User {index}", email = $"parallel{index}@example.com" }));
        Status(response, HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<User>())!;
    }));
    Assert(users.Select(user => user.Id).Distinct().Count() == 20, "Duplicate IDs.");
});
await Check("Concurrent duplicate email permits exactly one creation", async () =>
{
    var statuses = await Task.WhenAll(Enumerable.Range(0, 10).Select(async _ =>
    {
        using var response = await Send(HttpMethod.Post, "/users",
            """{"name":"Race","email":"race@example.com"}""");
        return response.StatusCode;
    }));
    Assert(statuses.Count(status => status == HttpStatusCode.Created) == 1 &&
        statuses.Count(status => status == HttpStatusCode.Conflict) == 9, "Uniqueness race.");
});
await Check("Unhandled exception returns JSON 500 without internal details", async () =>
{
    using var response = await Send(HttpMethod.Get, "/test/exception");
    await Error(response, HttpStatusCode.InternalServerError);
    var body = await response.Content.ReadAsStringAsync();
    Assert(body == "{\"error\":\"Internal server error.\"}", "Unexpected 500 body or leaked details.");
});
await Check("API remains available after an exception", async () =>
{
    using var response = await Send(HttpMethod.Get, "/users");
    Status(response, HttpStatusCode.OK);
});
await Check("Logs include final 200, rejected 401 and handled 500 responses", async () =>
{
    bool Has(int status) => logs.Entries.Any(entry =>
        entry.Message.StartsWith("HTTP response ", StringComparison.Ordinal) &&
        entry.Fields.TryGetValue("StatusCode", out var value) && Equals(value, status));
    for (var attempt = 0; attempt < 100 && !(Has(200) && Has(401) && Has(500)); attempt++)
        await Task.Delay(10);
    Assert(Has(200) && Has(401) && Has(500), "Final status missing from audit log.");
    Assert(logs.Entries.Any(entry => entry.Message.StartsWith("HTTP request GET /users",
        StringComparison.Ordinal)), "Incoming method/path was not logged.");
    Assert(!logs.Entries.Any(entry => entry.Message.Contains(testToken, StringComparison.Ordinal)),
        "Token leaked into logs.");
});
await Check("Exception endpoint is unavailable in Development", async () =>
{
    await using var normalApp = NewApp("Development", true);
    await normalApp.StartAsync();
    using var normalClient = new HttpClient { BaseAddress = new Uri(normalApp.Urls.Single()) };
    using var request = new HttpRequestMessage(HttpMethod.Get, "/test/exception");
    request.Headers.Authorization = new("Bearer", testToken);
    using var response = await normalClient.SendAsync(request);
    await Error(response, HttpStatusCode.NotFound);
    await normalApp.StopAsync();
});
await Check("Missing and short configured tokens fail startup", () =>
{
    foreach (var token in new[] { "", "short" })
    {
        var rejected = false;
        try
        {
            ApiApplication.Build(Array.Empty<string>(), builder =>
                builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Token"] = token
                }));
        }
        catch (InvalidOperationException exception) when (
            exception.Message.Contains("Authentication__Token", StringComparison.Ordinal))
        {
            rejected = true;
        }
        Assert(rejected, "Unsafe configuration was accepted.");
    }
    return Task.CompletedTask;
});

await app.StopAsync();
Console.WriteLine($"RESULT: {passed} passed, {failed} failed.");
return failed == 0 ? 0 : 1;

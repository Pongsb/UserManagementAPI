# User Management API

An ASP.NET Core Web API for the TechHive Solutions scenario in Activities and the final project, **Building a Simple API with Copilot**. It creates, reads, updates and deletes users, validates input, authenticates Bearer tokens, logs requests and responses, and returns JSON errors.

The implementation and automated verification are included. The original
source was subsequently reviewed in VS Code using the Copilot SDK; the review record is in
`docs/COPILOT_USAGE.md`. A separate Microsoft Copilot Chat session is not
claimed because its product and mode were not recorded. The remaining
submission task is to publish this source code to your own public GitHub
repository.

## Requirements

- .NET 8 SDK, with a current servicing update: [download](https://dotnet.microsoft.com/en-us/download/dotnet/8.0). Install the **SDK**, not only the Runtime.
- VS Code with C# support, or Visual Studio with ASP.NET development support.
- Microsoft Copilot access for the course's Copilot requirement.
- Optional: Postman for manual testing; Git for uploading to GitHub.

There are no third-party NuGet package dependencies. The test project is a console integration runner using the .NET and ASP.NET Core shared frameworks.

## Start on Windows with PowerShell

Extract the ZIP and open the folder containing `UserManagementAPI.sln` in VS Code, then choose **Terminal → New Terminal**. The terminal must be in the folder containing `UserManagementAPI.sln`.

```powershell
dotnet --version
dotnet build UserManagementAPI.sln --configuration Release

# Generate a fresh local token without putting it in a source file.
$env:Authentication__Token = [guid]::NewGuid().ToString("N") + [guid]::NewGuid().ToString("N")

# Copy the printed token to Postman's local token variable if using Postman.
$env:Authentication__Token

dotnet run --project src/UserManagementAPI --no-launch-profile --urls http://localhost:5080
```

Keep this terminal open while testing. Press **Ctrl+C** to stop the API. The environment variable exists only in that PowerShell session. The API deliberately refuses to start if the token is missing or shorter than 32 characters.

Open a second PowerShell terminal for a manual CRUD check. Set its `$token` to the value printed in the server terminal.

```powershell
$token = Read-Host "Paste the token printed in the server terminal"
$headers = @{ Authorization = "Bearer $token" }
$baseUrl = "http://localhost:5080"

# Create
$body = @{ name = "Demo User"; email = "demo.user@example.com" } | ConvertTo-Json
$created = Invoke-RestMethod -Method Post -Uri "$baseUrl/users" -Headers $headers -ContentType "application/json" -Body $body
$created

# Read all and read by ID
Invoke-RestMethod -Method Get -Uri "$baseUrl/users" -Headers $headers
Invoke-RestMethod -Method Get -Uri "$baseUrl/users/$($created.id)" -Headers $headers

# Update
$updatedBody = @{ name = "Updated User"; email = "updated.user@example.com" } | ConvertTo-Json
Invoke-RestMethod -Method Put -Uri "$baseUrl/users/$($created.id)" -Headers $headers -ContentType "application/json" -Body $updatedBody

# Delete
Invoke-RestMethod -Method Delete -Uri "$baseUrl/users/$($created.id)" -Headers $headers
```

An ordinary browser address-bar request has no Bearer header and will receive `401`. Use Postman, the PowerShell examples or the `.http` file instead.

## Start on Linux or macOS

Run from the solution folder:

```bash
export Authentication__Token="$(openssl rand -hex 32)"
dotnet run --project src/UserManagementAPI --no-launch-profile --urls http://localhost:5080
```

Use HTTP only for the local demonstration. Remote deployments need HTTPS to protect the token in transit.

## API contract

Every endpoint requires `Authorization: Bearer YOUR_LOCAL_TOKEN`.

| Method | Path | Body | Successful response |
| --- | --- | --- | --- |
| GET | `/users` | None | `200` and an array, including `[]` when empty |
| GET | `/users/{id}` | None | `200` and the matching user |
| POST | `/users` | Name and email | `201`, created user and a `Location` header |
| PUT | `/users/{id}` | Name and email | `200` and the updated user |
| DELETE | `/users/{id}` | None | `204`, empty response body |

Create and update request:

```json
{
  "name": "Demo User",
  "email": "demo.user@example.com"
}
```

Successful user response has `id`, `name` and `email`. IDs are assigned by the server. PUT replaces the name and email; it does not create a missing user.

| Condition | Status |
| --- | --- |
| Missing, malformed or incorrect Bearer token | `401` |
| Missing or whitespace-only name; invalid or missing email; invalid JSON | `400` |
| Name exceeds 100 characters or email exceeds 254 characters | `400` |
| Email already belongs to another user, ignoring letter case | `409` |
| User or route does not exist | `404` |
| HTTP method is not supported for the route | `405` |
| Unsupported request content type | `415` |
| Unexpected exception before response headers start | `500` |

Standard error example:

```json
{ "error": "User not found." }
```

Validation errors additionally include an `errors` object mapping fields to messages. The API does not send exception messages or stack traces to clients.

## Run all automated checks

The runner starts its own HTTP server on a free local port and generates a fresh random test token in memory for each run. You do not need to start the API or set a token first.

```powershell
dotnet build UserManagementAPI.sln --configuration Release
dotnet run --project tests/UserManagementAPI.Tests --configuration Release --no-build
```

Expect `PASS` lines and a final `RESULT` with zero failed checks. The process returns exit code 1 if any check fails. Use this `dotnet run` command rather than `dotnet test`: this project intentionally uses a standalone integration runner.

See [the verification report](docs/VERIFICATION.md) for the historical Linux
result and the current Windows x64 result. GitHub Actions runs the same build
and runner after upload if the workflow is enabled.

## Use Postman

1. Start the API using the instructions above.
2. Import `postman/UserManagementAPI.postman_collection.json`.
3. Open the collection's Variables tab. Set `token` to your generated token as a **local** value; leave `baseUrl` as `http://localhost:5080`.
4. Run the collection in its listed order, preferably against a freshly restarted API. The create request stores the new ID for later requests.
5. The invalid-token request deliberately expects `401`; the missing-user request expects `404`.

Do not export your local token back into the public repository. The `.http` file is another manual option; replace its local token placeholder only on your machine and do not commit it.

## Middleware and data design

`ApiApplication.cs` registers the required three custom middleware in this order:

1. `ErrorHandlingMiddleware` wraps the downstream pipeline in try/catch.
2. `TokenAuthenticationMiddleware` validates one opaque, preconfigured Bearer token.
3. `RequestLoggingMiddleware` logs the method and path and arranges final response logging.

An unauthorized request stops at authentication before reaching the logging middleware. Authentication therefore calls the same shared audit helper for rejected requests. Completion callbacks log the final response status, including a `500` written by the outer error handler. Tokens, request bodies and user email values are not included in these audit messages.

The in-memory store uses a lock for compound operations, immutable records and dictionary indexes for IDs and emails. Single-record and email lookups have average O(1) complexity. Listing creates an ordered snapshot and costs O(n log n), with O(n) snapshot memory. There is no performance benchmark or measured speedup claim.

This is a course demonstration: data resets on restart, the store is process-local, and authentication uses a shared opaque token rather than JWTs, login accounts or roles. No database, registration screen or token issuance endpoint is required by the supplied instructions.

## Files to read

- [Activities 1–3 walkthrough](docs/ACTIVITIES_1_TO_3.md): requirement mapping, implementation and manual tests.
- [Copilot prompts and evidence log](docs/COPILOT_USAGE.md): actual review record and prompts.
- [Submission guide](SUBMISSION_GUIDE.md): public GitHub upload and Coursera URL submission.
- [Verification report](docs/VERIFICATION.md): actual build and test evidence.

## Official references

- [ASP.NET Core middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-8.0)
- [ASP.NET Core model validation](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-8.0)
- [Create a GitHub repository](https://docs.github.com/en/repositories/creating-and-managing-repositories/quickstart-for-repositories)
- [Add files to a GitHub repository](https://docs.github.com/en/repositories/working-with-files/managing-files/adding-a-file-to-a-repository)

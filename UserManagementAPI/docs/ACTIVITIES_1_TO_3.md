# Activities 1 to 3 implementation walkthrough

The three activities build one API incrementally. This package provides the integrated final implementation so there is one project to run and submit. Separate historical Activity 1 and Activity 2 snapshots are not included or claimed.

The source was originally prepared with ChatGPT assistance. A subsequent
Copilot SDK review in VS Code on 24 October 2026 inspected the implementation
and tests using the prompts in `COPILOT_USAGE.md`. No application-source
change was justified. The exact model and mode were not recorded, so this
document does not claim a separate Microsoft Copilot Chat session.

## Activity 1 writing and enhancing API code

### Scenario and project setup

TechHive Solutions needs HR and IT staff to manage user records. `src/UserManagementAPI/UserManagementAPI.csproj` is an ASP.NET Core Web API project named `UserManagementAPI`. `Program.cs` calls the application setup in `ApiApplication.cs`.

The setup registers MVC controllers, the user store and token validator through dependency injection. Controllers receive the same store instance, allowing records to survive between requests during one server run.

### CRUD implementation

`Controllers/UsersController.cs` defines the following handlers:

| Requirement | Handler | Result |
| --- | --- | --- |
| Retrieve all users | `GetAll` | Returns an ordered snapshot and HTTP 200 |
| Retrieve one user | `GetById` | HTTP 200 or JSON 404 |
| Add a user | `Create` | HTTP 201, the created record and its Location |
| Update a user | `Update` | HTTP 200, JSON 404 or JSON 409 |
| Remove a user | `Delete` | HTTP 204 or JSON 404 |

The `User` record contains `Id`, `Name` and `Email`. `UpsertUserRequest` accepts the fields a client can change; clients do not assign IDs. POST creates a new resource, while PUT replaces the editable fields on an existing resource.

### Manual test sequence

Start the API with a locally configured token and import the Postman collection. Run list, create, get by ID, update and delete in order. Check response codes as well as data. After deletion, retrieving the same ID must return 404. The final version includes authentication from Activity 3, so send the Bearer header for Activity 1 tests too.

### Copilot requirement

The actual review record, including inspected files, findings, rejected
speculation and commands, is in `COPILOT_USAGE.md`. Existing
ChatGPT-assisted source files are not labelled as Copilot-generated.

## Activity 2 debugging and validation

The activity describes possible defects in an initial API. The following table maps those scenario defects to safeguards in the final implementation. It is not a claim that a flawed earlier version was reproduced in this workspace.

| Scenario issue | Final implementation | Verification |
| --- | --- | --- |
| Users added with empty names | `[Required]` on the request name | Empty, whitespace and null-name POSTs return 400 |
| Invalid email accepted | `[Required]`, `[EmailAddress]` and length limit | Invalid and missing-email POSTs return 400 |
| Missing user causes an error | Store returns null or NotFound; controller maps to 404 | Unknown-ID GET, PUT and DELETE checks |
| Unhandled exceptions crash requests | Outer error middleware catches exceptions | Intentional test exception returns JSON 500; next request succeeds |
| Repeated linear lookup by ID | Dictionary indexed by ID | Source inspection and GET correctness tests |
| Duplicate emails accepted | Case-insensitive email index | Sequential and concurrent duplicate checks |
| ID or uniqueness race | Lock covers each compound store operation | Concurrent unique and duplicate POST checks |
| Invalid update mutates existing data | MVC validation runs before the handler | Invalid PUT followed by GET of unchanged record |

Validation also rejects invalid JSON, null request bodies and names or emails beyond the configured maximum lengths. Expected input errors are returned as 400; they are not treated as unexpected 500s.

`GetAll` returns a snapshot rather than exposing the live dictionary. It orders the snapshot by ID. The list operation remains O(n log n) and is not a database query optimization. A larger application would need pagination and persistent storage, but neither is required for this exercise.

The Activity 2 review found no actual defect. The regression runner was
executed after the review and reported 46 passed and 0 failed.

## Activity 3 implementing middleware

### Request and response logging

`RequestLoggingMiddleware` uses `RequestAudit.Register` to log incoming method and path. A response completion callback records method, path, final status and elapsed time. Reading the final status after outer error handling avoids recording a premature 200 for a request that becomes a 500.

Authentication rejects invalid tokens before the logging middleware runs. Its rejection path therefore registers the same audit callback. This meets both the required custom middleware order and the request to log unauthorized traffic. Registration is deduplicated per request.

### Error handling

`ErrorHandlingMiddleware` catches unexpected downstream exceptions and responds with status 500 and this JSON body:

```json
{ "error": "Internal server error." }
```

Detailed exception information is logged on the server. When a client has disconnected or headers have already been sent, the handler cannot safely replace the response and aborts it instead.

MVC model-validation errors and expected missing-user or duplicate-email responses use the same top-level `error` field. Status-code pages normalize otherwise empty framework-generated 404, 405 and 415 responses.

### Token authentication

`TokenAuthenticationMiddleware` requires exactly one Authorization header using the Bearer scheme. `TokenValidator` hashes the candidate and expected token with SHA-256, then compares the fixed-size hashes with `FixedTimeEquals`. Missing, malformed and incorrect tokens receive JSON 401 and a `WWW-Authenticate: Bearer` challenge.

Configure the expected token using `Authentication__Token`, not a public source file. This implementation uses an opaque shared token. It does not issue JWTs or authenticate individual user accounts.

### Pipeline configuration

The three custom registrations in `ApiApplication.cs` are:

```csharp
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<TokenAuthenticationMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
```

After these, status-code handling and controller endpoints finish the pipeline. Middleware entered for a request unwinds in reverse order for the response.

### Middleware tests

Run the automated integration runner for valid tokens, missing tokens, invalid tokens, deliberate exceptions and captured logs. It enables `/test/exception` only inside its Testing environment. The same route is verified unavailable in Development even when its opt-in flag is set.

For a manual exception test, stop the ordinary server and start an explicitly isolated Testing instance in PowerShell, using your existing token:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Testing"
$env:Testing__EnableExceptionEndpoint = "true"
dotnet run --project src/UserManagementAPI --no-launch-profile --urls http://localhost:5080
```

Send an authenticated GET to `http://localhost:5080/test/exception`; expect JSON 500. Then GET `/users`; expect 200. Stop this instance and remove the test configuration before restarting normally:

```powershell
Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
Remove-Item Env:Testing__EnableExceptionEndpoint -ErrorAction SilentlyContinue
```

Do not deploy the exception-testing environment. The Activity 3 review and
its actual verification are documented in `COPILOT_USAGE.md`.

## Final project rubric mapping

| Rubric item | Points | Evidence or remaining work |
| --- | --- | --- |
| GitHub repository | 5 | You must create and publish a public repository; see `SUBMISSION_GUIDE.md` |
| CRUD endpoints | 5 | `Controllers/UsersController.cs`, Postman collection and integration runner |
| Used Copilot to debug | 5 | Review evidence is recorded in `COPILOT_USAGE.md`; a separate Microsoft Copilot Chat session is not claimed |
| Additional functionality and validation | 5 | Data annotations, consistent errors, uniqueness checks and negative tests |
| Middleware | 5 | Error handling, token authentication and request/response logging |

These are evidence mappings, not a guaranteed score. Reviewers determine the grade. The final assignment asks for the repository URL, not three separate activity uploads.

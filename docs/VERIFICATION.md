# Verification report

Verification date: 24 October 2026.

## Cleaned package verification

After publication cleanup on 24 October 2026, the cleaned source built on Linux x64 using .NET SDK 8.0.413 and ASP.NET Core runtime 8.0.19 with **0 warnings and 0 errors**. The integration runner exited 0 with **46 passed and 0 failed**.

Sample names and email addresses were anonymized, and the runner now generates a random credential for each execution. The application source and public API behavior were preserved. This is a new verification after cleanup; it does not claim a new Windows or Copilot session.

The package contains no third-party NuGet dependencies. Restore used an empty local package source with package auditing disabled for this offline check. A host-only process-metadata compatibility wrapper was used for .NET tooling in the preparation sandbox and is not shipped with the project.

## Windows result before publication cleanup

- Release build: **succeeded**.
- Compiler and analyzer warnings: **0**.
- Build errors: **0**.
- Automated HTTP checks: **46 passed, 0 failed**.
- Build exit code: **0**.
- Integration runner exit code: **0**.

The commands were executed from the solution directory with the explicit x64
host `C:\Program Files\dotnet\dotnet.exe`:

```text
"C:\Program Files\dotnet\dotnet.exe" build UserManagementAPI.sln --configuration Release
"C:\Program Files\dotnet\dotnet.exe" run --project tests\UserManagementAPI.Tests --configuration Release --no-build
```

The host was Windows x64 with .NET SDK **10.0.113**, .NET runtime
**8.0.32**, and ASP.NET Core runtime **8.0.32**. The application and tests
remain targeted to `net8.0`.

## Historical Linux result

- Release build: **succeeded**.
- Compiler and analyzer warnings: **0**.
- Build errors: **0**.
- Automated HTTP checks: **46 passed, 0 failed**.
- Integration runner exit code: **0**.

The checks executed against real ASP.NET Core Kestrel servers on loopback HTTP addresses. This is runtime verification, not only static code inspection. The runner starts and stops its own server instances.

## Historical environment and commands

Executed on Linux x64 with .NET SDK **8.0.413** and ASP.NET Core runtime **8.0.19**. Use a current .NET 8 SDK servicing update on your own computer. Windows instructions are provided, but Windows and the Postman graphical application were not executed in this environment.

The preparation sandbox required a host-only compatibility workaround for its process metadata before .NET tooling could run. That workaround is outside the project and is not included in the deliverable. No application behavior was changed to accommodate it.

The final successful build used previously restored assets:

```text
dotnet build UserManagementAPI.sln --configuration Release --no-restore -p:UseSharedCompilation=false -m:1
dotnet run --project tests/UserManagementAPI.Tests --configuration Release --no-build
```

This project has no third-party NuGet package references.

## Windows environment troubleshooting

The first fresh x64 runner attempt built successfully but failed to start
because the x64 host had no `Microsoft.AspNetCore.App 8.x`. The x86 host had
ASP.NET Core 8.0.32 but no SDK. The official Microsoft ASP.NET Core Runtime
8.0.32 x64 installer was downloaded from Microsoft's release metadata,
SHA-512 verified, and installed with exit code 0. The x64 host then listed
`Microsoft.AspNetCore.App 8.0.32`, enabling the successful current run.
The temporary installer was not included in the project or submission ZIP.

## Coverage

| Area | Runtime evidence |
| --- | --- |
| Authentication | Missing token, invalid token, wrong scheme, missing credentials, case-insensitive Bearer scheme and rejection before creation |
| CRUD | Empty list, POST with working Location, GET list and ID, PUT, DELETE with empty 204 and deleted-user 404 |
| Validation | Missing, empty, whitespace and null names; invalid and missing emails; length limits; malformed JSON; wrong field types; null and empty bodies |
| Content types and routes | JSON 415 for unsupported or missing content type, JSON 404 for unknown routes or IDs and JSON 405 for unsupported method |
| Email ownership | Case-insensitive uniqueness, unchanged own email, duplicate update rejection and reuse after update or deletion |
| Concurrency | 20 distinct simultaneous creates have unique IDs; 10 simultaneous duplicate-email creates produce exactly one 201 and nine 409s |
| Exceptions | Deliberate unhandled exception returns a generic JSON 500; a subsequent request succeeds |
| Audit logs | Captured method/path and final 200, 401 and 500 statuses; configured token absent from audit text |
| Configuration | Missing or short expected token rejected at startup; exception route unavailable in Development |

See [the current cleaned-package check output](test-results.txt) and [the current build output](build-output.txt). These files contain the fresh Linux cleanup verification, not the historical Windows stdout. Absolute project paths were replaced with `<project-root>` for privacy. The supplied historical Windows logs had mixed encoding and were replaced with these fresh readable logs; the historical Windows result remains summarized above.

## Issues found and corrected during verification

The initial integration run exposed inconsistent unsupported-content-type errors: MVC generated a ProblemDetails response before status-code pages could normalize it. Setting `SuppressMapClientErrors` leaves empty framework client-error responses available to the shared JSON status handler.

The empty-body test initially omitted Content-Type, which correctly led MVC to a 415 response. It now tests an empty application/json body as 400 and separately verifies a POST without Content-Type as JSON 415. The final run passed both cases.

These were findings from the actual local verification process, not
application defects. The Copilot SDK review confirmed that no source change
was needed.

## Work not established by this report

This report does not claim a separate Microsoft Copilot Chat session,
publication to GitHub, a successful GitHub Actions run, submission to
Coursera, production deployment or a performance benchmark.

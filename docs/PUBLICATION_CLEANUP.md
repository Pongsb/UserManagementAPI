# Publication cleanup record

Cleanup date: 24 October 2026.

## Changes

- Replaced personally identifying sample names with fictional demo names in the README, HTTP examples, Postman requests and integration tests.
- Replaced name-derived sample email addresses with generic addresses on the reserved `example.com` domain.
- Removed the private project directory from published build evidence and used `<project-root>` instead.
- Replaced unreadable mixed-encoding build/test logs with fresh UTF-8 output from verification of the cleaned package. Historical Windows results remain labeled as historical in `VERIFICATION.md`.
- Replaced the fixed integration-test credential with a cryptographically random value generated in memory for each execution. No generated value is saved or printed.
- Extended `.gitignore` to cover common credential files, private key and certificate files, local secret settings, editor history and generated archives.
- Updated the submission guide to recommend a chosen public display name and the account-provided GitHub noreply email for commit attribution.
- Rebuilt the ZIP with forward-slash paths and normalized timestamps, permissions and empty metadata/comment fields.

## Preserved behavior and provenance

The API routes, validation, middleware, authentication mechanism and application source were preserved. The missing-token test still sends no credentials, and the invalid-token tests still use deliberately invalid input. All sample email addresses use `example.com`.

The supplied Copilot review and historical verification statements remain attributed to their original sessions. Cleanup verification is recorded separately; this record does not claim publication, a new Copilot session or a new Windows run.

## Checks

- Release build: succeeded with 0 warnings and 0 errors.
- Integration runner: 46 passed, 0 failed; exit code 0.
- All JSON assets parse successfully.
- Published files were checked for personal names, device/user directories, non-example email addresses, private key material and common access-token patterns.
- The application token setting and Postman token variable are blank; the HTTP example contains a placeholder.
- Generated build outputs, Git history, local credentials and temporary tools are excluded from the ZIP.

The checks cover the delivered files. GitHub account information and future commit attribution are controlled separately by the publisher's account and Git settings.

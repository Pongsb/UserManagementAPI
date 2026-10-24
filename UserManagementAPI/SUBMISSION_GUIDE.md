# Submit the User Management API project

The supplied final assignment asks you to submit a **public GitHub repository URL** for peer review. The project does not need to be hosted as a running website. Reviewers need access to readable source code and instructions for running it.

## Step 1 extract and test

1. Download and extract the cleaned `UserManagementAPI_Submission.zip`.
2. Open the extracted folder containing `README.md` and `UserManagementAPI.sln` in VS Code.
3. Install the .NET 8 SDK if needed. Open a new terminal after installation.
4. Run these commands from the solution folder:

```powershell
dotnet --version
dotnet build UserManagementAPI.sln --configuration Release
dotnet run --project tests/UserManagementAPI.Tests --configuration Release --no-build
```

Expect a successful build and a test summary with zero failures. Follow `README.md` to start and manually test the API.

## Step 2 complete the Copilot evidence

1. Open `docs/COPILOT_USAGE.md` and review the recorded Copilot SDK session.
2. The recorded review found no application defect and required no source change.
3. If the course specifically requires a separate Microsoft Copilot Chat
   session, perform that session and replace the product/mode statement with
   the actual recorded details; do not infer them.
4. Keep the actual commands and results in the activity records and verification report.

The instructions do not require screenshots, but relevant screenshots or a
chat export can support your written evidence.

## Step 3 create a public GitHub repository

1. Sign in at [GitHub](https://github.com/).
2. Choose **+ → New repository**, or open [github.com/new](https://github.com/new).
3. Repository name: `UserManagementAPI`.
4. Suggested description: `ASP.NET Core user management API with CRUD, validation and middleware`.
5. Select **Public** so peer reviewers can access it.
6. For an existing local project, leave the initial README, gitignore and license options unselected; these instructions upload the README and gitignore already supplied.
7. Click **Create repository**.

## Step 4 upload the project

Choose one method. The Git method automatically respects `.gitignore` and is preferable after building locally.

### Method A Git from the VS Code terminal

Install [Git for Windows](https://git-scm.com/downloads/win) if `git --version` is not recognized. Reopen the terminal after installation.

Run from the folder containing `UserManagementAPI.sln`:

```powershell
git --version
git init
git add .
git status
git commit -m "Initial commit"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/UserManagementAPI.git
git push -u origin main
```

Replace `YOUR_USERNAME` with your actual GitHub username. Use the repository URL GitHub displays if you chose a different repository name. Complete the browser sign-in when Git requests authentication.

Before committing, `git status` should show source, tests and documentation. It should not include generated `bin` or `obj` folders, local tokens or personal data.

If Git reports that your author identity is missing, configure your chosen public display name and the GitHub-provided noreply email shown in your account email settings, then repeat the commit and push:

```powershell
git config user.name "YOUR PUBLIC DISPLAY NAME"
git config user.email "YOUR GITHUB NOREPLY EMAIL"
```

These commands set the identity only for this repository. Replace both placeholder values; do not copy them literally.

### Method B upload using the GitHub website

1. Open the repository you just created.
2. For an empty repository, click **uploading an existing file**. Otherwise choose **Add file → Upload files**.
3. Drag the **contents of the extracted `UserManagementAPI` folder** into the upload page. Preserve `src`, `tests`, `docs`, `postman`, `.github`, `.gitignore`, `.gitattributes`, `README.md` and the solution file.
4. If you already built locally, exclude every generated `bin` and `obj` folder. Browser uploads do not use your local `.gitignore` to filter selected files.
5. Check that `README.md` and `UserManagementAPI.sln` will appear at the repository root.
6. Use the commit message `Initial commit` and commit the files. The exact final button can be labeled **Commit changes** or **Propose changes**, depending on branch settings; merge your proposal if a pull request is created.

Do not upload only the ZIP archive. Reviewers should be able to browse `.cs` source files directly. This source package has fewer than the website's 100-files-per-upload limit when generated build folders are excluded.

## Step 5 verify reviewer access

1. Refresh the repository page and confirm the source folders and README are visible.
2. Open a C# source file such as `src/UserManagementAPI/Controllers/UsersController.cs`.
3. Open the repository URL in a private or incognito browser window. You should be able to read it without signing in.
4. If using GitHub Actions, check the **Actions** tab for the build-and-verify workflow. Wait for its result and fix failures before submission. If Actions is unavailable, retain your local build/test evidence; the rubric does not explicitly require Actions.
5. Confirm your Copilot usage record describes a real completed session and no private token is committed.

## Step 6 submit to Coursera

1. Open **Project: Building a Simple API with Copilot** in your course.
2. Enter the assignment's submission page. The supplied instructions show a **URL** field.
3. Paste the repository homepage URL, for example:

```text
https://github.com/YOUR_USERNAME/UserManagementAPI
```

4. Use the repository homepage, not a localhost address, a GitHub settings page, a single source file URL or the ZIP download link.
5. Complete any title or description fields if the current page asks for them. Suggested title: `User Management API with CRUD and Middleware`.
6. Review the submission and use the page's submit control.
7. Confirm that Coursera records the submission. If the course asks you to review peers, complete that follow-up requirement on the course page.

Optional short description, after you have actually completed the Copilot work:

```text
ASP.NET Core User Management API with GET, POST, PUT and DELETE endpoints,
input validation, consistent JSON error handling, Bearer-token authentication,
request and response logging, and automated integration checks. The repository
includes setup instructions, a Postman collection and my documented Copilot
review and debugging sessions.
```

## Submission checklist

- [x] The project builds and the integration runner reports zero failures on my computer.
- [ ] All five CRUD routes are present.
- [ ] Validation and the three middleware components are implemented.
- [x] I recorded the Copilot SDK review and its real contributions.
- [ ] My GitHub repository is public and its source is readable.
- [ ] No private token or personal user records are committed.
- [ ] I pasted the public repository homepage into the assignment URL field.
- [ ] I confirmed submission on Coursera.

## Official upload references

- [GitHub repository quickstart](https://docs.github.com/en/repositories/creating-and-managing-repositories/quickstart-for-repositories)
- [Adding locally hosted code to GitHub](https://docs.github.com/en/migrations/importing-source-code/using-the-command-line-to-import-source-code/adding-locally-hosted-code-to-github)
- [Uploading files through the website](https://docs.github.com/en/repositories/working-with-files/managing-files/adding-a-file-to-a-repository)

The Coursera submission steps above follow the instructions in your uploaded assignment. Any labels on the live submission page may differ slightly.

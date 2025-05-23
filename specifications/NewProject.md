
## How to use this template to create a totally new software

Here are short instructions as an example of how you could do a new software project using this example.

### Remove unnecessary files

Maybe copy all files to a new folder. Then:

- You probably want to delete the existing `database` and `backend\sql` folders and start from a new database.

- `.github` folder is only needed for GitHub workflows. The `.git` is what makes this a git-repository and has the version history, keep or remove, whatever.

- If you do only WebApi and NOT a HTML-site at all, then you can :
  - delete `frontend`, `node-modules` 
  - remove `clientside` group from paket.lock and paket.dependencies.
  - delete TypeScript deployment related files from the root folder: `.htmlhintrc`, `.jshintrc`, `.npmrc`, `gulpfile.js`, `package.json`, `package-lock.json`, `tsconfig.json`, `tslint.json`

- If you have built the template, there are some temp-files you might delete. This helps search & replace on the next step.
  - `.fake` folder as that's build-script temp-files
  - Under backend and UnitTests, there might be some `obj` and `bin` folders that can be deleted.
  - Files that come from paket managers: `node-modules`, `paket-files` and `packages`

### Search & replace

Rename the files `backend\WebsitePlayground.fsproj` and `backend\WebsitePlayground.sln` to your domain, whatever you are doing.

Search from all files, in the folder and subfolders, with "Match Case" (case sensitivity on), and replace, with your own terms: 
- `WebsitePlayground` (project name)
- `CompanyWeb` (business name)
- `Company Web` (rendered title)
- `Companyweb` (used e.g. in the database)
- `companyweb` (database and service names)

### Setup a Database

1. Create and design a database.
2. Create a new SQL Server database project. Use e.g. VS2022 built-in template or [MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj/)
3. Use Schema Comparison (e.g. Tools -> SQL Server -> New Schema Comparison) to copy the database design to your sqlproj. You may want to skip things like database users, roles and permissions. Note the output-path, where you build your dacpac file to.
4. Change the compile-time connection string from TypeProviderConnection (backend/Domain.fs) to point to your new database. Change the SSDT-path to point wherever you decided to build your database in the previous step.
5. Try to build the database project. Restart Visual Studio. Try rewriting your Domain.fs database-query to something that the intellisense is happy with.
6. Change the app.config RuntimeDBConnectionString to point to the real database. Consider encrypting these later, or moving to whatever store is the current security best practice.

### Try that the project is working

Scan through the build.fsx file if that is appropriate.
Try the command line as well (`build.cmd` and `run.cmd`).
Try opening the solution in Visual Studio and building there.
Now that the infrastructure is there, you should be able to focus on your queries, webpages, and APIs.

### Theme

Change the `frontend\styles\main.less` @brand-primary colour, body background-color, etc.
If you change the font-face, the font files are located at `frontend\fonts`.
The web-pages are still named and titled as "company" so you may want to change their names.

### Authentication

If you want to do a non-public or sign-in required website:
You can use any AspNetCore authentication mechanisms.
For example, `dotnet paket add Microsoft.Identity.Web` for Azure EntraID via `Microsoft.AspNetCore.Authentication.OpenIdConnect`.


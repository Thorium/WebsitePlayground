
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

1. Create and design a database. (Out of scope of this document)
2. Change the compile-time connection string from TypeProviderConnection (backend/Domain.fs) to point to your new database.
3. Try to build the database project. Restart Visual Studio. Try rewriting your Domain.fs database-query to something that the intellisense is happy with.
4. Change the app.config RuntimeDBConnectionString to point to the real database. Consider encrypting these later, or moving to whatever store is the current security best practice.

### Try that the project is working

Scan through the build.fsx file if that is appropriate.
Try the command line as well (`build.cmd` and `run.cmd`).
Try opening the solution in Visual Studio and building there.
Now that the infrastructure is there, you should be able to focus on your queries, webpages, and APIs.

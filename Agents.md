# AGENTS.md - WebsitePlayground Coding Guidelines

This document provides actionable coding guidelines for AI agents (GitHub Copilot, code assistants) working on the Welendus codebase. These guidelines are distilled from Dev.md and apply across programming languages.

F# + TypeScript web application with MariaDB/MySQL backend and React frontend. Follow these guidelines when working on this codebase.

> **Note:**  
> - For .NET, favor **declarative code** and use `System.Linq` extensively, but don't generate unused code.  
> - **Never assign `null` manually**. Use Option/Maybe/Optional types or explicit handling.
> - For domain models, use **POCO classes with no methods**. Place business logic in static classes with static/extension methods.
> - **Avoid mutable state** when achievable in around 10 lines or less.
> - **Higher-order functions**: Pass as parameters only — avoid returning functions or partial application.
> - **Avoid unnecessary non-alphanumerical characters** in code.
> - **Low-level code** (e.g., Span/Memory/pointers): Only use if justified for performance.
> - **Do not squash Git commits**; always preserve history for reverts and traceability.

## Build/Test Commands

**Backend (F#):**
- Build: `dotnet build backend/WebsitePlayground.fsproj` or `build.cmd project`
- Test all: `dotnet test UnitTests/UnitTests.fsproj` or `build.cmd test`
- Test single: `dotnet test UnitTests/UnitTests.fsproj --filter "FullyQualifiedName~TestName"`
- Run server: `run.cmd` (or `sh run.sh` on Linux/Mac)

**Frontend (TypeScript/React):**
- Build all: `npm run build` or `gulp deploy` (add `--release ok` for production)
- Watch mode: `npm run dev` or `gulp` (watches files, rebuilds on change)
- Lint TS: `gulp tslint` (also runs automatically with `gulp`)

## Code Style - F# Backend

**Indentation:** 4 spaces (no tabs). **Naming:** PascalCase for types, camelCase for parameters/values. Use `__` for member prefixes.
**Domain models:** POCO types with no methods. Business logic in static/extension methods. **Avoid null:** Use `Option<'T>`. **Functional:** Prefer immutable data, `List/Array.map|filter` over loops, pipeline operator `|>`.
**Async:** Use `task { }` for SignalR methods. Convert F# `async { }` to `Task<>` with `|> Async.StartAsTask` if needed. **Transactions:** Use `writeWithDbContext` helper for DB writes, `dbReadContext()` for reads only.
**Error handling:** Fail fast on technical errors, use Result/Option for business logic errors. **Submit changes:** Use `dbContext.SubmitUpdates2()` (not SubmitUpdatesAsync).

## Code Style - TypeScript Frontend

**Indentation:** 4 spaces. **Target:** ES5, CommonJS modules. **Imports:** Use `import name = require("./module")` for local TypeScript modules.
**React:** Use `React.createClass` (legacy style). In JSX: `className` not `class`, use `{variable}` for bindings. No inline `style` attributes (use style objects).
**Page structure:** Each page has `pageName.html` + `frontend/scripts/pageName.ts` with `export function initPageName(locale)` called from `app.ts`.
**SignalR calls:** `signalhub.signalHub.server.methodName(params).done(data => ...).fail(() => ...)`. Always handle `.fail()` explicitly.
**Linting:** Max 180 chars/line, curly braces required, triple-equals with null-check allowed, 4-space indent.

## File Organization

**Backend:** `Domain.fs` (types), `Logics.fs` (business logic), `SignalRHubs.fs` (API), `OwinStartup.fs` (web server), `Scheduler.fs` (tasks), `Program.fs` (entry point).
**Frontend:** `frontend/scripts/*.ts` (page logic), `frontend/scripts/*.tsx` (React components), `frontend/styles/*.less/*.scss/*.css`, `frontend/*.html` (pages).
**Dependencies:** Backend via `paket.dependencies` + `paket.references`. Frontend via `package.json` (npm) and `paket.dependencies` (Clientside group).

## Common Patterns

**F# Database Queries:**
```fsharp
// Read-only queries use dbReadContext()
let executeSearch (dbContext:ReadDataContext) (searchparams:SearchObject) =
    task {
        let! companies =
            query {
                for c in dbContext.Companyweb.Company do
                where (c.Founded < searchparams.FoundedBefore)
                select { CompanyName = c.Name }
            } |> Array.executeQueryAsync
        return companies
    }

// Writes use dbWriteContext() with SubmitUpdates2
let executeCrud (dbContext:WriteDataContext) itemId actionToEntity =
    task {
        let! fetched = query { ... } |> Seq.tryHeadAsync
        match fetched with
        | Some entity ->
            entity |> actionToEntity
            do! dbContext.SubmitUpdates2()
        | None -> ()
    }
```

**SignalR Hub Methods (F#):**
```fsharp
type SignalHub() as this =
    inherit Hub<IMessageToClient>()
    
    // Return data to caller
    member __.SearchCompanies (searchparams:SearchObject) =
        task {
            let! companies = Logics.executeSearch (dbReadContext()) searchparams
            return companies
        }
    
    // Push notification to all clients
    member __.BuyStocks (company:string, amount:int) =
        this.Clients.All.NotifyDeal ("Message to all users")
    
    // Push to single caller
    member __.Something() =
        this.Clients.Caller.MethodName data
```

**TypeScript SignalR Client Calls:**
```typescript
import signalhub = require('./signalhub');

// Call server method with promise handling
signalhub.signalHub.server.searchCompanies(params)
    .done((data) => {
        // Handle success
    })
    .fail(() => {
        // Handle error - ALWAYS include .fail()
    });

// Listen to server push notifications
signalhub.signalHub.client.notifyDeal = (message: string) => {
    console.log(message);
};
```

**TypeScript Page Initialization:**
```typescript
// In frontend/scripts/pageName.ts
export function initPageName(locale: string) {
    // Page-specific initialization
    $('#someElement').click(() => { ... });
}

// In app.ts
if(window.location.href.indexOf("/pageName.html") > 0) {
    import('./pageName').then(page => page.initPageName(locale));
}
```

**React Components (Legacy Style):**
```typescript
// In .tsx file
const MyComponent = React.createClass({
    getInitialState: function() {
        return { count: 0 };
    },
    handleClick: function() {
        this.setState({ count: this.state.count + 1 });
    },
    render: function() {
        return <div className="container">
            <button onClick={this.handleClick}>Count: {this.state.count}</button>
        </div>;
    }
});
```

## Additional Resources

- **Full guidance**: See `specifications/Dev.md` for comprehensive details
- **Regex Testing**: https://regex101.com/ for pattern development
- **Security**: OWASP Top 10 for latest vulnerability guidance
- **API Design**: REST API best practices, OpenAPI/Swagger specs

---

**Remember**: Code should be easy to remove. Build focused, composable functions that solve immediate problems clearly. Avoid building frameworks, libraries, or abstractions unless the need is proven by 3+ concrete use cases. Check memory and performance when possible, but don't prematurely optimize. Follow security guidelines always. Caching is not a silver bullet - measure before adding complexity. Don't squash git commits unnecessarily - preserve the story of your code. The next developer (including future you) will thank you.

## Pre-Submission Checklist

- [ ] **Build passes:** Run `dotnet build` for backend, `npm run build` for frontend
- [ ] **Tests pass:** Run `dotnet test` if modifying backend logic
- [ ] **Linting passes:** `gulp tslint` for TypeScript changes
- [ ] **F# conventions:** POCO domain models, Option instead of null, task { } for async
- [ ] **TS conventions:** ES5 target, React.createClass, SignalR .fail() handlers
- [ ] **Database:** Read contexts for queries, write contexts with SubmitUpdates2 for mutations
- [ ] **Security:** Input validation, parameterized queries (SQLProvider handles this)
- [ ] **Logging:** Use Logari for F# backend logging with structured messages
- [ ] **Minimal changes**: Only modified what's necessary
- [ ] **Declarative style**: Used functional/declarative patterns where applicable
- [ ] **No manual nulls**: Used Option/Maybe/Optional or explicit handling
- [ ] **Async I/O**: All I/O operations are async/non-blocking
- [ ] **Concurrency**: Used built-in thread-safe structures when multi-threading state scenario possible
- [ ] **Input validation**: All user input is validated and sanitized
- [ ] **Logging**: Added appropriate log statements with correct levels and context
- [ ] **Error handling**: Technical errors fail fast, business errors use explicit types
- [ ] **Security**: No SQL injection, XSS, or sensitive data exposure. No known OWASP top 10.
- [ ] **Performance**: Considered N+1 queries, indexing, batching
- [ ] **Memory**: No unbounded caches, resource leaks, or unnecessary data loading
- [ ] **Caching**: Only cache when measured benefit exists, with proper expiration
- [ ] **UTC times**: All database timestamps should use UTC
- [ ] **Clean history**: Commit messages are clear and focused
- [ ] **No secrets**: No API keys, passwords, or tokens in code
- [ ] **Git commits**: NOT squashed unnecessarily - history preserved

## Additional Resources

- **Full guidance**: See `specifications/Dev.md` for comprehensive details
- **Regex Testing**: https://regex101.com/ for pattern development
- **Security**: OWASP Top 10 for latest vulnerability guidance
- **API Design**: REST API best practices, OpenAPI/Swagger specs

---

**Remember**: Code should be easy to remove. Build focused, composable functions that solve immediate problems clearly. Avoid building frameworks, libraries, or abstractions unless the need is proven by 3+ concrete use cases. Check memory and performance when possible, but don't prematurely optimize. Follow security guidelines always. Caching is not a silver bullet - measure before adding complexity. Don't squash git commits unnecessarily - preserve the story of your code. The next developer (including future you) will thank you.

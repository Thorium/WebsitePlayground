// You can execute this in the F# interactive, or run from the command line:  dotnet fsi Program.fsx
// Build the project first: this script references what the build resolved (backend/bin).
// Known limit: this typechecks cleanly (so editors and fsharp-refactor read it), but
// running it end to end under dotnet fsi still trips over Microsoft.Extensions.* being
// present both in the ASP.NET Core shared framework and in the packages (FS0193).

#if INTERACTIVE

// Packages, in the versions paket.lock pins. The type providers in particular have to come
// from their packages and not from bin: SqlDataProvider is loaded from the design-time
// assembly beside the package's lib folder, which the build does not copy to the output.
// Microsoft.Extensions.Hosting brings the whole Microsoft.Extensions.* set the app compiles
// against; taking those from the shared framework instead makes fsi refuse to load the
// second copy at run time.
#r "nuget: Microsoft.Extensions.Hosting, 10.0.9"
#r "nuget: SQLProvider.MySqlConnector, 1.5.24"
#r "nuget: FSharp.Data, 8.1.14"
#r "nuget: FSharp.Data.JsonProvider.Serializer, 1.0.8"

// dotnet fsi references the Microsoft.NETCore.App framework only, so everything this app gets
// from the Microsoft.AspNetCore.App shared framework has to be pointed at by hand.
// This is deliberately NOT paket's generated load script (.paket/load/...): the Server group
// also resolves package copies of assemblies the shared framework already carries - some of
// them still the ASP.NET Core 2.x ones - and two copies of HttpContext or ILogger are enough
// to make Oxpecker's extension members (ctx.Write, ctx.GetLogger) unresolvable.
// If the version below no longer exists, take the current one from `dotnet --list-runtimes`.
#I @"C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.10"
#r "Microsoft.AspNetCore.dll"
#r "Microsoft.AspNetCore.Antiforgery.dll"
#r "Microsoft.AspNetCore.Authentication.dll"
#r "Microsoft.AspNetCore.Authentication.Abstractions.dll"
#r "Microsoft.AspNetCore.Authentication.Cookies.dll"
#r "Microsoft.AspNetCore.Authentication.Core.dll"
#r "Microsoft.AspNetCore.Authorization.dll"
#r "Microsoft.AspNetCore.Authorization.Policy.dll"
#r "Microsoft.AspNetCore.Connections.Abstractions.dll"
#r "Microsoft.AspNetCore.Cors.dll"
#r "Microsoft.AspNetCore.Cryptography.Internal.dll"
#r "Microsoft.AspNetCore.DataProtection.dll"
#r "Microsoft.AspNetCore.DataProtection.Abstractions.dll"
#r "Microsoft.AspNetCore.Diagnostics.dll"
#r "Microsoft.AspNetCore.Diagnostics.Abstractions.dll"
#r "Microsoft.AspNetCore.Hosting.dll"
#r "Microsoft.AspNetCore.Hosting.Abstractions.dll"
#r "Microsoft.AspNetCore.Hosting.Server.Abstractions.dll"
#r "Microsoft.AspNetCore.Http.dll"
#r "Microsoft.AspNetCore.Http.Abstractions.dll"
#r "Microsoft.AspNetCore.Http.Connections.dll"
#r "Microsoft.AspNetCore.Http.Connections.Common.dll"
#r "Microsoft.AspNetCore.Http.Extensions.dll"
#r "Microsoft.AspNetCore.Http.Features.dll"
#r "Microsoft.AspNetCore.Http.Results.dll"
#r "Microsoft.AspNetCore.HttpsPolicy.dll"
#r "Microsoft.AspNetCore.Metadata.dll"
#r "Microsoft.AspNetCore.ResponseCompression.dll"
#r "Microsoft.AspNetCore.Rewrite.dll"
#r "Microsoft.AspNetCore.Routing.dll"
#r "Microsoft.AspNetCore.Routing.Abstractions.dll"
#r "Microsoft.AspNetCore.Server.Kestrel.Core.dll"
#r "Microsoft.AspNetCore.SignalR.dll"
#r "Microsoft.AspNetCore.SignalR.Common.dll"
#r "Microsoft.AspNetCore.SignalR.Core.dll"
#r "Microsoft.AspNetCore.SignalR.Protocols.Json.dll"
#r "Microsoft.AspNetCore.StaticFiles.dll"
#r "Microsoft.AspNetCore.WebSockets.dll"
#r "Microsoft.AspNetCore.WebUtilities.dll"
#r "Microsoft.AspNetCore.Server.Kestrel.dll"
#r "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.dll"

// Everything else the project depends on, in the versions the build resolved.
#I @"./bin"
#r "Logari.dll"
#r "Destructurama.FSharp.dll"
#r "Serilog.dll"
#r "Serilog.Extensions.Logging.dll"
#r "Serilog.Sinks.ApplicationInsights.dll"
#r "Serilog.Sinks.Console.dll"
#r "Serilog.Sinks.File.dll"
#r "Owin.Compression.dll"
#r "Oxpecker.dll"
#r "Oxpecker.ViewEngine.dll"
#r "Oxpecker.OpenApi.dll"
#r "MySql.Data.dll"
#r "Microsoft.AspNetCore.OpenApi.dll"
#r "Microsoft.AspNetCore.Authentication.JwtBearer.dll"
#r "Microsoft.AspNetCore.SignalR.Protocols.NewtonsoftJson.dll"
#r "Microsoft.Extensions.Hosting.WindowsServices.dll"
#r "System.Configuration.ConfigurationManager.dll"
#r "System.ServiceProcess.ServiceController.dll"

// Same order as the <Compile> items in WebsitePlayground.fsproj.
#load "Domain.fs"
#load "Logics.fs"
#load "Scheduler.fs"
#load "SignalRHubs.fs"
#load "OwinStartup.fs"
#load "Program.fs"

// Opens for poking around interactively once everything is loaded.
open System
open System.Linq
open System.Threading.Tasks
open System.Security.Claims
open FSharp.Data
open FSharp.Data.Sql
open Logari

try
    MyApp.main [||] |> ignore
with e ->
    Message.eventError e.Message |> writeLog

#endif

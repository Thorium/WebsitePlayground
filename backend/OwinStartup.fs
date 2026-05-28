module OwinStart

open System
open System.Net.Http
open System.Security.Claims
open System.Threading.Tasks
open Oxpecker
open Oxpecker.OpenApi
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
//open Microsoft.Identity.Web

open System.Configuration
open System.Security.Principal
open System.IO
open System.Text
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication
open Newtonsoft.Json
open Logari
let displayErrors = ConfigurationManager.AppSettings.["WebServerDebug"].ToString().ToLower() = "true"
let hubConfig = Microsoft.AspNetCore.SignalR.HubOptions(EnableDetailedErrors = Nullable(displayErrors))

let serverPath =
    let path = ConfigurationManager.AppSettings.["WebServerFolder"].ToString() |> getRootedPath
    if not(Directory.Exists path) then Directory.CreateDirectory (path) |> ignore
    path

let origins =
    let allowedOrigins =
        [|
            "http*://*.myserver.com" // Add your server/domain here. Note the possible pots as well
            "http*://www.google-analytics.com"
            "http*://maps.googleapis.com"
            "http*://fonts.googleapis.com"
            //"https://login.microsoftonline.com"
            #if DEBUG
            "http://localhost"
            "ws*://localhost"
            "http://localhost:" + System.Configuration.ConfigurationManager.AppSettings["WebServerPorts"]
            "https://localhost:" + System.Configuration.ConfigurationManager.AppSettings["WebServerPortsSSL"]
            "ws*://localhost:" + System.Configuration.ConfigurationManager.AppSettings["WebServerPortsSSL"]
            #endif
        |]
    if not(String.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings.["ServerAddress"])) then
        Array.append allowedOrigins [| System.Configuration.ConfigurationManager.AppSettings.["ServerAddress"] |]
    else allowedOrigins


let errorHandler (ctx: Microsoft.AspNetCore.Http.HttpContext) (next: Microsoft.AspNetCore.Http.RequestDelegate) =
    let writeError (ex) =

        let err =
            "Api error 400 {uri} {ex} \r\n\r\n {stack}" |> Logari.Message.eventError
            |> Logari.Message.setField "uri" (ctx.Request.Path)
            |> Logari.Message.setField "ex" (ex.ToString())
            |> Logari.Message.setField "stack" (System.Diagnostics.StackTrace(1, true).ToString())
        err |> writeLog

    task {
        try
            return! next.Invoke(ctx)
        with
        | :? ModelBindException
        | :? RouteParseException as ex ->
            let logger = ctx.GetLogger()
            logger.LogWarning(ex, "Unhandled 400 error")
            writeError ex
            let result = TypedResults.Problem (
                statusCode = StatusCodes.Status400BadRequest,
                title = "Bad Request",
                detail = ex.Message)
            do! ctx.Write result
        | ex ->
            let logger = ctx.GetLogger()
            logger.LogError(ex, "Unhandled 500 error")
            writeError ex
            let result = TypedResults.Problem (
                statusCode = StatusCodes.Status500InternalServerError,
                title = "Internal Server Error",
                detail = ex.Message)
            do! ctx.Write result
    }
    :> Task

let notFoundHandler (ctx: Microsoft.AspNetCore.Http.HttpContext) =
    try
        let logger = ctx.GetLogger()
        logger.LogInformation("Unhandled 404 error: " + ctx.Request.Path)
    with e -> Console.WriteLine ("Logging failure: "+ e.Message)
    let result = TypedResults.Problem (
        statusCode = StatusCodes.Status404NotFound,
        title = "Not Found",
        detail = "Resource was not found")
    ctx.Write result

let appconfig = fun (app : Microsoft.AspNetCore.Builder.IApplicationBuilder) ->

    //let opts = Microsoft.AspNetCore.Rewrite.RewriteOptionsExtensions.AddRedirect
    //        (Microsoft.AspNetCore.Rewrite.RewriteOptions())
    //app.UseRewriter opts
    app
    

let serviceConfigSignalR services =
    Microsoft.Extensions.DependencyInjection.NewtonsoftJsonProtocolDependencyInjectionExtensions.AddNewtonsoftJsonProtocol(
        Microsoft.Extensions.DependencyInjection.SignalRDependencyInjectionExtensions.AddSignalR(services, fun hubOpts ->
            hubOpts.EnableDetailedErrors <- Nullable(true)
        ), fun jsonOpts -> ()) |> ignore
    services

let appConfigSignalR app =
    app |> Microsoft.AspNetCore.Builder.EndpointRoutingApplicationBuilderExtensions.UseRouting
    |> fun ap -> Microsoft.AspNetCore.Builder.EndpointRoutingApplicationBuilderExtensions.UseEndpoints(ap, fun endpoint ->
        Microsoft.AspNetCore.Builder.HubEndpointRouteBuilderExtensions.MapHub<SignalRHubs.SignalHub>(endpoint, "/signalhub", fun opts ->
                ()
            ) |> ignore
        Microsoft.AspNetCore.Builder.HubEndpointRouteBuilderExtensions.MapHub<SignalRHubs.CompanyHub>(endpoint, "/companyhub", fun opts ->
                ()
            ) |> ignore
    )

let myEndpointHandler (param1:string) : EndpointHandler =
    fun (ctx: Microsoft.AspNetCore.Http.HttpContext) ->
        task {
            // maybe some validity checking of parameters. Remember this is public endpoint now.
            // Also good idea could be logging the calls to somewhere like to a non-full-recovery-model database
            if param1 = "" then
                let result = TypedResults.Problem(
                    statusCode = 403,
                    title = "No permission.",
                    detail = ""
                )
                do! ctx.Write result
            else
                // You can get additional data from ctx.Request

                let someResponse =
                    Logics.getData()
                    |> Array.map(fun i ->
                        FSharp.Data.JsonProvider.Serializer.Serialize i.JsonValue)

                do! ctx.Write (TypedResults.Ok someResponse)
        }

let private readJson<'T> (ctx: HttpContext) =
    task {
        use reader = new StreamReader(ctx.Request.Body, Encoding.UTF8)
        let! body = reader.ReadToEndAsync()
        return JsonConvert.DeserializeObject<'T>(body)
    }

let private authCookieScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme

let registerHandler : EndpointHandler =
    fun (ctx: HttpContext) ->
        task {
            let! request = readJson<RegisterRequest> ctx
            let normalizedRequest = { request with Email = request.Email |> ``normalize email`` }
            let! result = Logics.``register user`` normalizedRequest
            let response =
                match result with
                | RegistrationSuccess ->
                    Message.eventInfo "User registered: {email}"
                    |> Message.setField "email" normalizedRequest.Email
                    |> writeLog
                    {
                        Success = true
                        ErrorCode = ""
                        ErrorMessage = ""
                    }
                | EmailExists ->
                    Message.eventWarn "Registration failed: Email exists {email}"
                    |> Message.setField "email" normalizedRequest.Email
                    |> writeLog
                    {
                        Success = false
                        ErrorCode = "EmailExists"
                        ErrorMessage = "Email already registered. Please use another email."
                    }
                | WeakPassword reason ->
                    Message.eventWarn "Registration failed: Weak password for {email}"
                    |> Message.setField "email" normalizedRequest.Email
                    |> writeLog
                    {
                        Success = false
                        ErrorCode = "WeakPassword"
                        ErrorMessage = reason
                    }
            do! ctx.Write (TypedResults.Ok response)
        }

let loginHandler : EndpointHandler =
    fun (ctx: HttpContext) ->
        task {
            let! request = readJson<LoginRequest> ctx
            let normalizedRequest = { request with Email = request.Email |> ``normalize email`` }
            // Note: a more hardened login flow would also record failed attempts by IP/email for audit,
            // add protection for dictionary attacks, and throttle repeated login attempts.
            let! result = Logics.``authenticate user`` normalizedRequest
            let! response =
                task {
                    match result with
                    | Success (userId, email) ->
                        let claims = [
                            Claim(ClaimTypes.NameIdentifier, userId.ToString())
                            Claim(ClaimTypes.Name, email)
                            Claim(ClaimTypes.Role, "AuthenticatedUser")
                        ]
                        let identity = ClaimsIdentity(claims, authCookieScheme)
                        let principal = ClaimsPrincipal(identity)
                        do! ctx.SignInAsync(
                            authCookieScheme,
                            principal,
                            AuthenticationProperties(
                                IsPersistent = true,
                                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8.0)
                            ))
                        Message.eventInfo "User logged in: {email}"
                        |> Message.setField "email" email
                        |> writeLog
                        return {
                            Success = true
                            ErrorCode = ""
                            ErrorMessage = ""
                            LockedUntil = Nullable()
                        }
                    | InvalidCredentials ->
                        Message.eventWarn "Failed login attempt: {email}"
                        |> Message.setField "email" normalizedRequest.Email
                        |> writeLog
                        return {
                            Success = false
                            ErrorCode = "InvalidCredentials"
                            ErrorMessage = "Invalid email or password"
                            LockedUntil = Nullable()
                        }
                    | AccountLocked lockedUntil ->
                        Message.eventWarn "Login attempt on locked account: {email}"
                        |> Message.setField "email" normalizedRequest.Email
                        |> writeLog
                        return {
                            Success = false
                            ErrorCode = "AccountLocked"
                            ErrorMessage = "Account is locked due to too many failed attempts."
                            LockedUntil = Nullable lockedUntil
                        }
                    | AccountInactive ->
                        Message.eventWarn "Login attempt on inactive account: {email}"
                        |> Message.setField "email" normalizedRequest.Email
                        |> writeLog
                        return {
                            Success = false
                            ErrorCode = "AccountInactive"
                            ErrorMessage = "Account is inactive. Please contact support."
                            LockedUntil = Nullable()
                        }
                }
            do! ctx.Write (TypedResults.Ok response)
        }

let logoutHandler : EndpointHandler =
    fun (ctx: HttpContext) ->
        task {
            let email =
                match ctx.User with
                | null -> "unknown"
                | user when user.Identity.IsAuthenticated -> user.Identity.Name
                | _ -> "unknown"
            do! ctx.SignOutAsync(authCookieScheme)
            Message.eventInfo "User logged out: {email}"
            |> Message.setField "email" email
            |> writeLog
            do! ctx.Write (TypedResults.Ok {| Success = true |})
        }

let currentUserHandler : EndpointHandler =
    fun (ctx: HttpContext) ->
        task {
            match ctx.User with
            | null ->
                do! ctx.Write (TypedResults.Ok {
                    IsAuthenticated = false
                    Email = ""
                })
            | user when user.Identity.IsAuthenticated ->
                do! ctx.Write (TypedResults.Ok {
                    IsAuthenticated = true
                    Email = user.Identity.Name
                })
            | _ ->
                do! ctx.Write (TypedResults.Ok {
                    IsAuthenticated = false
                    Email = ""
                })
        }

// WebAPI example: Some map to do Oxpecker routing
let endpoints = [
        subRoute "/webapi" [
            POST [
                routef "/my/{%O:alpha}" <| (fun (param1: string) -> myEndpointHandler param1)
                route "/auth/register" registerHandler
                route "/auth/login" loginHandler
                route "/auth/logout" logoutHandler
            ] // |> configureEndpoint _.RequireAuthorization(Microsoft.AspNetCore.Authorization.AuthorizeAttribute ...

            GET [ // This would be a HTTP GET to http://localhost:7050/webapi/my
                routef "/my" <| (text "Node Found.")
                route "/auth/me" currentUserHandler
            ]
        ]
    ]

let configureServices (builder: WebApplicationBuilder) logger =

    let services = builder.Services

    services
        .AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(fun opts ->
            opts.Cookie.Name <- "WebsitePlayground.Auth"
            opts.Cookie.HttpOnly <- true
#if DEBUG
            opts.Cookie.SecurePolicy <- Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest
#else
            opts.Cookie.SecurePolicy <- Microsoft.AspNetCore.Http.CookieSecurePolicy.Always
#endif
            opts.Cookie.SameSite <- Microsoft.AspNetCore.Http.SameSiteMode.Strict
            opts.ExpireTimeSpan <- TimeSpan.FromHours(8.0)
            opts.SlidingExpiration <- true
            opts.LoginPath <- "/login.html"
            opts.LogoutPath <- "/logout"
            opts.AccessDeniedPath <- "/login.html"
        ) |> ignore

    builder
        .Services
        .AddAuthorizationBuilder()
        .AddPolicy("AuthenticatedUser", fun policy ->
            policy.RequireAuthenticatedUser() |> ignore
        )
        |> ignore

    let services = services.AddDataProtection().Services
    let services =
        services
              .AddCors(fun opts -> opts.AddDefaultPolicy(fun policy ->
                  policy
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .WithOrigins origins |> ignore
                  ()
                ))

    let services =
      services
        .AddLogging(fun loggingBuilder ->
                          //loggingBuilder.AddSerilog(logger, dispose = true) |> ignore
                          ())
        .AddAntiforgery()
#if !DEBUG
        .AddHttpsRedirection(fun opt -> ())
#endif
        .AddResponseCompression(fun opts ->
            opts.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>()
            opts.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>()
            opts.EnableForHttps <- true
            )
        .AddRouting()
        .AddOxpecker()
    |> serviceConfigSignalR


    services

let configureApp (app: IApplicationBuilder) =
    let staticFileOpts =
        StaticFileOptions(
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(serverPath)
            //, .OnPrepareResponse <- fun ctx -> ()
            
        )

    let app =
#if DEBUG
            app.UseDeveloperExceptionPage()
#else
            app.UseExceptionHandler("/error", true)
#endif
    let app =
      app
#if !DEBUG
       //.UseHsts()
       .UseHttpsRedirection()
#endif
       .UseResponseCompression()
       .Use(errorHandler)
       .UseCors(fun opts ->
          opts
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithOrigins origins |> ignore
          ())
       .UseRouting()
       .UseAuthentication()
       .UseAuthorization()
       .UseAntiforgery()
       .UseFileServer(FileServerOptions(
                            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(serverPath),
                            RequestPath = ""
                         ))
    let app =
      (appConfigSignalR app)
       .UseOxpecker(endpoints)
       //.UseStaticFiles(new StaticFileOptions(FileProvider =
       //     new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Util.serverPath)
       //   ))
       .Run(notFoundHandler)
    let app =
      //if isTestEnv then
      //  app
      //else
        app
    app

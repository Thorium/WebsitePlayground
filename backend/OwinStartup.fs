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

open System.Configuration
open System.Security.Principal
open System.IO
open Microsoft.AspNetCore.Http
let displayErrors = ConfigurationManager.AppSettings.["WebServerDebug"].ToString().ToLower() = "true"
let hubConfig = Microsoft.AspNetCore.SignalR.HubOptions(EnableDetailedErrors = Nullable(displayErrors))

let serverPath =
    let path = ConfigurationManager.AppSettings.["WebServerFolder"].ToString() |> getRootedPath
    if not(Directory.Exists path) then Directory.CreateDirectory (path) |> ignore
    path

let origins =
    let allowedOrigins =
        [|
            "http*://*.myserver.com"
            "http*://www.google-analytics.com"
            "http*://maps.googleapis.com"
            "http*://fonts.googleapis.com"
            #if DEBUG
            "http://localhost"
            "ws*://localhost"
            "http://localhost:7050"
            "ws*://localhost:7050"
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

let notFoundHandler (ctx: Microsoft.AspNetCore.Http.HttpContext)  =
    let logger = ctx.GetLogger()
    logger.LogInformation("Unhandled 404 error: " + ctx.Request.Path)
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

// WebAPI example: Some map to do Oxpecker routing
let endpoints = [
        subRoute "/webapi" [
            POST [
                routef "/my/{%O:alpha}" <| (fun (param1: string) -> myEndpointHandler param1)
            ] // |> configureEndpoint _.RequireAuthorization(Microsoft.AspNetCore.Authorization.AuthorizeAttribute ...

            GET [ // This would be a HTTP GET to http://localhost:7050/webapi/my
                routef "/my" <| (text "Node Found.")
            ]
        ]
    ]

let configureServices (services: IServiceCollection) logger =
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
    //let _ = services.AddTransient<IUserService, UserService>() |> ignore

    let services = services.AddAuthorization()
    let services = services.AddDataProtection().Services

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
       .UseFileServer(FileServerOptions(
                           FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(serverPath),
                           RequestPath = ""
                        ))
    let app =
      (appConfigSignalR app)
       .UseAuthorization()
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

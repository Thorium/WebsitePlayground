module OwinStart

open System
open System.Net.Http
open System.Security.Claims
open System.Threading.Tasks
open Giraffe
open Saturn

open System.Configuration
open System.Security.Principal
open System.IO
let displayErrors = ConfigurationManager.AppSettings.["WebServerDebug"].ToString().ToLower() = "true"
let hubConfig = Microsoft.AspNetCore.SignalR.HubOptions(EnableDetailedErrors = Nullable(displayErrors))

let serverPath =
    let path = ConfigurationManager.AppSettings.["WebServerFolder"].ToString() |> getRootedPath
    if not(Directory.Exists path) then Directory.CreateDirectory (path) |> ignore
    path

let corsPolicyBuilder = fun (policy:Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder) ->
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
    policy
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .WithOrigins origins |> ignore
    ()


let loggerBuilder = fun (logger : Microsoft.Extensions.Logging.ILoggingBuilder) ->
    //let prov = { new Microsoft.Extensions.Logging.ILoggerProvider with  }
    //let conf = Microsoft.Extensions.Logging.Configuration
    //Microsoft.Extensions.Logging.LoggingBuilderExtensions.AddProvider(logger, prov)
    //|> Microsoft.Extensions.Logging.LoggingBuilderExtensions.AddConfiguration(logger, conf)
    ()

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

let app =
    application {
        
        url ("http://" + System.Configuration.ConfigurationManager.AppSettings.["WebServerIp"] + ":" + System.Configuration.ConfigurationManager.AppSettings.["WebServerPorts"])
        url ("https://" + System.Configuration.ConfigurationManager.AppSettings.["WebServerIpSSL"] + ":" + System.Configuration.ConfigurationManager.AppSettings.["WebServerPortsSSL"])
        use_cors "mycors" corsPolicyBuilder
        logging loggerBuilder
        no_router
        memory_cache
        app_config appconfig
#if !DEBUG
        force_ssl
#endif        
        use_static serverPath
        //use_json_serializer (Thoth.Json.Giraffe.ThothSerializer())
        use_gzip
        app_config appConfigSignalR
        service_config serviceConfigSignalR
    }

module OwinStart

open System
open System.Net.Http
open Microsoft.Owin.Diagnostics
open Microsoft.Owin.Security
open Microsoft.Owin.Security.Cookies
open Microsoft.Owin.Security.Facebook
open Microsoft.Owin.Security.Google
open Owin
open System.Web.Http
open System.Security.Claims
open Microsoft.Owin
open System.Threading.Tasks

open System.Configuration
open System.Security.Principal
open System.IO
let displayErrors = ConfigurationManager.AppSettings.["WebServerDebug"].ToString().ToLower() = "true"
let hubConfig = Microsoft.AspNet.SignalR.HubConfiguration(EnableDetailedErrors = displayErrors, EnableJavaScriptProxies = true)

let serverPath = 
    let path = ConfigurationManager.AppSettings.["WebServerFolder"].ToString() |> getRootedPath
    if not(Directory.Exists path) then Directory.CreateDirectory (path) |> ignore
    Microsoft.Owin.FileSystems.PhysicalFileSystem path

(*
let corsPolicy = Microsoft.Owin.Cors.CorsOptions(PolicyProvider = 
    Microsoft.Owin.Cors.CorsPolicyProvider(PolicyResolver = fun c -> 
        Task.FromResult(
            let p = System.Web.Cors.CorsPolicy(
                        AllowAnyHeader = true,
                        AllowAnyMethod = true,
                        AllowAnyOrigin = false,
                        SupportsCredentials = true)
            p.Origins.Add "http*://*.myserver.com"
            p.Origins.Add "http://localhost"
            if not(String.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings.["ServerAddress"])) then 
                p.Origins.Add System.Configuration.ConfigurationManager.AppSettings.["ServerAddress"]
            p
        )))
*)
open Owin.Security.AesDataProtectorProvider
open System.Net

type LoggingPipelineModule() =
    inherit Microsoft.AspNet.SignalR.Hubs.HubPipelineModule() with 
        override __.OnIncomingError(exceptionContext, invokerContext) =
            let invokeMethod = invokerContext.MethodDescriptor
            let args = String.Join(", ", invokerContext.Args)
            Logary.Message.eventError(invokeMethod.Hub.Name + "." + invokeMethod.Name + "(" + args + ") exception:\r\n " + exceptionContext.Error.ToString()) |> writeLog
            base.OnIncomingError(exceptionContext, invokerContext)

        override __.OnBeforeIncoming context =
            Logary.Message.eventDebug("=> Invoking " + context.MethodDescriptor.Hub.Name + "." + context.MethodDescriptor.Name) |> writeLog
            base.OnBeforeIncoming context

        override __.OnBeforeOutgoing context =
            Logary.Message.eventDebug("<= Invoking " + context.Invocation.Hub + "." + context.Invocation.Method) |> writeLog
            base.OnBeforeOutgoing context

type MyWebStartup() =

    member __.Configuration(ap:Owin.IAppBuilder) =
        if Type.GetType ("Mono.Runtime") <> null then
           //Workaround for Mono incompatibility https://katanaproject.codeplex.com/workitem/438
           let (sux, httpListener) = ap.Properties.TryGetValue(typeof<HttpListener>.FullName)
           try
               (httpListener :?> HttpListener).IgnoreWriteExceptions <- true;
           with ex ->
               ()

        Microsoft.AspNet.SignalR.GlobalHost.HubPipeline.AddModule(new LoggingPipelineModule()) |> ignore

        ap.UseAesDataProtectorProvider("mykey123") 

        //OWIN Component registrations here...
        ap.UseErrorPage(new ErrorPageOptions(ShowExceptionDetails = displayErrors))

        |> fun app -> app.UseCompressionModule()

        //Allow cross domain
        |> fun app -> 
            app.UseCors(Microsoft.Owin.Cors.CorsOptions.AllowAll)
            //app.UseCors(corsPolicy)


        //SignalR:
        |> fun app -> app.MapSignalR(hubConfig) |> ignore

        // REST Web Api if needed:
        //use httpConfig = new HttpConfiguration()
        //httpConfig.Routes.MapHttpRoute("MyApi", "api/{controller}/{param}") |> ignore // "api/my" -> MyController
        //ap.UseWebApi(httpConfig) |> ignore

        // Google and Facebook authentications would be here...
        //ap.UseFacebookAuthentication(..)
        //let g = GoogleOAuth2AuthenticationOptions( ..)
        //ap.UseGoogleAuthentication g

        //Static files server (Note: default FileSystem is current directory!)
        let fileServerOptions = Microsoft.Owin.StaticFiles.FileServerOptions()
        fileServerOptions.DefaultFilesOptions.DefaultFileNames.Add "index.html"
        fileServerOptions.FileSystem <- serverPath
        ap.UseFileServer(fileServerOptions) |> ignore
        ()

[<assembly: Microsoft.Owin.OwinStartup(typeof<MyWebStartup>)>]
do()
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

let domainForwarding =
    if String.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings.["DomainForwarding"]) ||
       String.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings.["ServerAddress"]) then None
    else Some(System.Configuration.ConfigurationManager.AppSettings.["DomainForwarding"].ToString().
                Replace("http:", "https:"), System.Configuration.ConfigurationManager.AppSettings.["DomainForwarding"].ToString().ToLower())
 
let serverPath =
    let path = ConfigurationManager.AppSettings.["WebServerFolder"].ToString() |> getRootedPath
    if not(Directory.Exists path) then Directory.CreateDirectory (path) |> ignore
    Microsoft.Owin.FileSystems.PhysicalFileSystem path

(*
let corsPolicy = Microsoft.Owin.Cors.CorsOptions(PolicyProvider =
    Microsoft.Owin.Cors.CorsPolicyProvider(PolicyResolver = fun c ->
        let p = System.Web.Cors.CorsPolicy(
                    AllowAnyHeader = true,
                    AllowAnyMethod = true,
                    AllowAnyOrigin = false,
                    SupportsCredentials = true)
        if c.CallCancelled.IsCancellationRequested || p.Headers.IsReadOnly then
            Task.FromResult(p)
        else
            Task.FromResult(
                p.Origins.Add "http*://*.myserver.com"
                p.Origins.Add "http*://www.google-analytics.com"
                p.Origins.Add "http*://maps.googleapis.com"
                p.Origins.Add "http*://fonts.googleapis.com"
#if DEBUG
                p.Origins.Add "http://localhost"
                p.Origins.Add "ws*://localhost"
                p.Origins.Add "http://localhost:7050"
                p.Origins.Add "ws*://localhost:7050"
#endif
                if not(String.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings.["ServerAddress"])) then
                    p.Origins.Add System.Configuration.ConfigurationManager.AppSettings.["ServerAddress"]
                p
            )))
*)

open System.Web.Http.Filters
type LogExceptionAttribute() =
    inherit ExceptionFilterAttribute()
    let writeErr(context:HttpActionExecutedContext) =
        "Api error {uri} {ex} \r\n\r\n {stack}" |> Logary.Message.eventError
        |> Logary.Message.setField "uri" (context.Request.RequestUri)
        |> Logary.Message.setField "ex" (context.Exception.ToString())
        |> Logary.Message.setField "stack" (System.Diagnostics.StackTrace(1, true).ToString())
        |> writeLog
    override __.OnException(context:HttpActionExecutedContext) =
        writeErr context
        base.OnException(context)
    override __.OnExceptionAsync(context:HttpActionExecutedContext, token) =
        writeErr context
        base.OnExceptionAsync(context, token)

open Owin.Security.AesDataProtectorProvider
open System.Net

type LoggingPipelineModule() =
    inherit Microsoft.AspNet.SignalR.Hubs.HubPipelineModule() with
        override __.OnIncomingError(exceptionContext, invokerContext) =
            let invokeMethod = invokerContext.MethodDescriptor
            let args = String.Join(", ", invokerContext.Args)
            Logary.Message.eventError(invokeMethod.Hub.Name + "." + invokeMethod.Name + "({args}) exception:\r\n {err}")
            |> Logary.Message.setField "args" args |> Logary.Message.setField "err" (exceptionContext.Error.ToString()) |> writeLog
            base.OnIncomingError(exceptionContext, invokerContext)

        override __.OnBeforeIncoming context =
            let msg =Logary.Message.eventDebug("=> Invoking " + context.MethodDescriptor.Hub.Name + "." + context.MethodDescriptor.Name)
            if not(isNull context.Hub || isNull context.Hub.Context || isNull context.Hub.Context.ConnectionId) then
                msg |> Logary.Message.setField "clientId" context.Hub.Context.ConnectionId |> writeLog
            else
                msg |> writeLog
            base.OnBeforeIncoming context

        override __.OnBeforeOutgoing context =
            Logary.Message.eventDebug("<= Invoking " + context.Invocation.Hub + "." + context.Invocation.Method) |> writeLog
            base.OnBeforeOutgoing context

/// Direct linking url-routing,
/// redirect aliases: /company/ -> /company.html
type RedirectRoutingController() as this =
    inherit ApiController() 
    let createRedirectResponse uri =
        let response = this.Request.CreateResponse System.Net.HttpStatusCode.Redirect
        response.Headers.Location <- Uri(this.Request.RequestUri.GetLeftPart(UriPartial.Authority) + "/" + uri)
        response
    [<Route("company"); HttpGet; System.Web.Http.Description.ApiExplorerSettings(IgnoreApi = true)>] member __.RedirectToCompany() = createRedirectResponse "company.html"
    [<Route("results"); HttpGet; System.Web.Http.Description.ApiExplorerSettings(IgnoreApi = true)>] member __.RedirectToResults() = createRedirectResponse "results.html"

type MyWebStartup() =

    member __.Configuration(ap:Owin.IAppBuilder) =
    
//#if !DEBUG
//        // Force SSL
//        ap.Use(fun (context:IOwinContext) (next: Func<Task>) ->
//            async {
//                if context.Request.IsSecure || context.Request.Uri.Host = "localhost" || not(context.Request.Uri.IsDefaultPort) then
//                    do! next.Invoke() |> Async.AwaitIAsyncResult |> Async.Ignore
//                else
//                    let uri =
//                        Uri.UriSchemeHttps + Uri.SchemeDelimiter +
//                        context.Request.Uri.GetComponents(UriComponents.AbsoluteUri &&& ~~~UriComponents.Scheme, UriFormat.SafeUnescaped)
//                    context.Response.Redirect (uri.Replace("://www.", "://"))
//            } |> Async.StartAsTask :> Task
//        ) |> ignore
//#endif

        match domainForwarding with
        | None -> ()
        | Some (fromUri, toUri) ->
            ap.Use(fun (context:IOwinContext) (next: Func<Task>) ->
                async {
                    let uri =
                        Uri.UriSchemeHttps + Uri.SchemeDelimiter +
                        context.Request.Uri.GetComponents(UriComponents.AbsoluteUri &&& ~~~UriComponents.Scheme, UriFormat.SafeUnescaped)
                    if uri.Contains(fromUri) && uri.Contains(".html") then
                        context.Response.Redirect (uri.Replace(fromUri, toUri))
                    else
                        do! next.Invoke() |> Async.AwaitIAsyncResult |> Async.Ignore
                } |> Async.StartAsTask :> Task
            ) |> ignore

        if not (isNull (Type.GetType "Mono.Runtime")) then
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
        |> fun app -> ap.UseKentorOwinCookieSaver()
        |> fun app -> app.UseCompressionModule(
                        { OwinCompression.DefaultCompressionSettings with
                            CacheExpireTime = ValueSome (DateTimeOffset.Now.AddSeconds 30.) })

        //Allow cross domain
        |> fun app ->
            app.UseCors(Microsoft.Owin.Cors.CorsOptions.AllowAll)
            //app.UseCors(corsPolicy)


        //SignalR:
        |> fun app -> app.MapSignalR(hubConfig) |> ignore

        use httpConfig = new HttpConfiguration()
        httpConfig.Filters.Add(LogExceptionAttribute())

        // REST Web Api if needed:
        // Note: If parameter name is "param" as here, then it's referenced with only value in uri: /value/
        // otherwise you need an attribute [<FromUri>]x and it's referenced with ?x=...
        //httpConfig.Routes.MapHttpRoute("MyApi", "api/{controller}/{param}") |> ignore // "api/my" -> MyController

        ap.UseWebApi(httpConfig) |> ignore
        httpConfig.MapHttpAttributeRoutes()

        // Google and Facebook authentications would be here...
        //ap.UseFacebookAuthentication(..)
        //let g = GoogleOAuth2AuthenticationOptions( ..)
        //ap.UseGoogleAuthentication g

        //Static files server (Note: default FileSystem is current directory!)
        let fileServerOptions = Microsoft.Owin.StaticFiles.FileServerOptions()
        fileServerOptions.DefaultFilesOptions.DefaultFileNames.Add "index.html"
        fileServerOptions.FileSystem <- serverPath
        // fileServerOptions.StaticFileOptions.OnPrepareResponse <- fun r ->
        //   if r.File.PhysicalPath.Contains(@"\fonts\") && (not r.OwinContext.Response.Headers.IsReadOnly) && not(r.OwinContext.Request.CallCancelled.IsCancellationRequested) then
        //       if r.OwinContext.Response.Headers.ContainsKey("Access-Control-Allow-Origin") then
        //           r.OwinContext.Response.Headers.Remove("Access-Control-Allow-Origin") |> ignore
        //       r.OwinContext.Response.Headers.Add("Access-Control-Allow-Origin", [|"*"|])
        ap.UseFileServer(fileServerOptions) |> ignore
        httpConfig.EnsureInitialized()
        ()

[<assembly: Microsoft.Owin.OwinStartup(typeof<MyWebStartup>)>]
do()

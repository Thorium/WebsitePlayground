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

open Owin.Security.AesDataProtectorProvider

type MyWebStartup() =

    member __.Configuration(ap:Owin.IAppBuilder) =

        ap.UseAesDataProtectorProvider("mykey123") 

        //OWIN Component registrations here...
        ap.UseErrorPage(new ErrorPageOptions(ShowExceptionDetails = true))

        //Allow cross domain
        |> fun app -> app.UseCors(Microsoft.Owin.Cors.CorsOptions.AllowAll)

        //SignalR:
        |> fun app -> app.MapSignalR(SignalRHubs.hubConfig) |> ignore

        // REST Web Api if needed:
        //use httpConfig = new HttpConfiguration()
        //httpConfig.Routes.MapHttpRoute("MyApi", "api/{controller}/{param}") |> ignore // "api/my" -> MyController
        //ap.UseWebApi(httpConfig) |> ignore

        // Google and Facebook authentications would be here...
        //ap.UseFacebookAuthentication(..)
        //let g = GoogleOAuth2AuthenticationOptions( ..)
        //ap.UseGoogleAuthentication g

        //Static files server (Note: default FileSystem is current directory!)
        let dir = match System.IO.Directory.Exists __SOURCE_DIRECTORY__ with true -> __SOURCE_DIRECTORY__ | false -> ""
        let fileServerOptions = Microsoft.Owin.StaticFiles.FileServerOptions()
        fileServerOptions.DefaultFilesOptions.DefaultFileNames.Add "index.html"
        fileServerOptions.FileSystem <- (Microsoft.Owin.FileSystems.PhysicalFileSystem (dir + ConfigurationManager.AppSettings.["RelativeWebServerFolder"]))
        ap.UseFileServer(fileServerOptions) |> ignore
        ()

[<assembly: Microsoft.Owin.OwinStartup(typeof<MyWebStartup>)>]
do()
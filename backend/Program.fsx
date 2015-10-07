// You can execute this in the F#-iteractive or run from the command line: fsi Program.fsx 
// Or on Mono, fsharpi Program.fsx but note: on Mono SignalR is not working from interactive!

#if INTERACTIVE
#r @"./../packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#r @"./../packages/SQLProvider/lib/net40/FSharp.Data.SqlProvider.dll"
#r @"./../packages/MySql.Data/lib/net40/MySql.Data.dll"

// OWIN and SignalR-packages:
#I @"./../packages/Microsoft.AspNet.SignalR.Core/lib/net45"
#r @"./../packages/Microsoft.AspNet.SignalR.Core/lib/net45/Microsoft.AspNet.SignalR.Core.dll"
#I @"./../packages/Microsoft.Owin/lib/net45" 
#r @"./../packages/Microsoft.Owin/lib/net45/Microsoft.Owin.dll"
#I @"./../packages/Microsoft.Owin.Security/lib/net45"
#r @"./../packages/Microsoft.Owin.Security/lib/net45/Microsoft.Owin.Security.dll"
#I @"./../packages/Owin.Security.AesDataProtectorProvider/lib/net45"
#r @"./../packages/Owin.Security.AesDataProtectorProvider/lib/net45/Owin.Security.AesDataProtectorProvider.dll"
#r @"./../packages/Microsoft.AspNet.WebApi.Core/lib/net45/System.Web.Http.dll"
#r @"./../packages/Microsoft.AspNet.WebApi.Owin/lib/net45/System.Web.Http.Owin.dll"
#r @"./../packages/Microsoft.Net.Http/lib/net40/System.Net.Http.dll"
#I @"./../packages/Microsoft.Owin.Security.Cookies/lib/net45"
#r @"./../packages/Microsoft.Owin.Security.Cookies/lib/net45/Microsoft.Owin.Security.Cookies.dll"
#I @"./../packages/Microsoft.Owin.Security.Facebook/lib/net45"
#r @"./../packages/Microsoft.Owin.Security.Facebook/lib/net45/Microsoft.Owin.Security.Facebook.dll"
#I @"./../packages/Microsoft.Owin.Security.Google/lib/net45"
#r @"./../packages/Microsoft.Owin.Security.Google/lib/net45/Microsoft.Owin.Security.Google.dll"
#I @"./../packages/Microsoft.Owin.Hosting/lib/net45"
#r @"./../packages/Microsoft.Owin.Hosting/lib/net45/Microsoft.Owin.Hosting.dll"
#I @"./../packages/Microsoft.Owin.Host.HttpListener/lib/net45"
#r @"./../packages/Microsoft.Owin.Host.HttpListener/lib/net45/Microsoft.Owin.Host.HttpListener.dll"
#I @"./../packages/Microsoft.Owin.StaticFiles/lib/net45"
#r @"./../packages/Microsoft.Owin.StaticFiles/lib/net45/Microsoft.Owin.StaticFiles.dll"
#I @"./../packages/Microsoft.Owin.FileSystems/lib/net45"
#r @"./../packages/Microsoft.Owin.FileSystems/lib/net45/Microsoft.Owin.FileSystems.dll"
#I @"./../packages/Microsoft.Owin.Cors/lib/net45"
#r @"./../packages/Microsoft.Owin.Cors/lib/net45/Microsoft.Owin.Cors.dll"
#I @"./../packages/Microsoft.Owin.Diagnostics/lib/net45/"
#r @"./../packages/Microsoft.Owin.Diagnostics/lib/net45/Microsoft.Owin.Diagnostics.dll"
#r @"./../packages/Microsoft.AspNet.WebApi.Client/lib/net45/System.Net.Http.Formatting.dll"
#I @"./../packages/Microsoft.AspNet.Cors/lib/net45"
#I @"./../packages/Newtonsoft.Json/lib/net45"
#r @"./../packages/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll"
#I @"./../packages/Owin/lib/net40"
#r @"./../packages/Owin/lib/net40/Owin.dll"

#r @"System.Configuration.dll"

open System

#load "Domain.fs"
#load "SignalRHubs.fs"
#load "OwinStartup.fs"
#load "Program.fs"
MyApp.main [||] |> ignore

#endif

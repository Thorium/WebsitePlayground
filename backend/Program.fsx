// You can execute this in the F#-iteractive or run from the command line: fsi Program.fsx
// Or on Mono, fsharpi Program.fsx but note: on Mono SignalR is not working from interactive!

#if INTERACTIVE
#r "nuget: FSharp.Data"
#r "nuget: SQLProvider"
#r "nuget: MySql.Data"

// OWIN and SignalR-packages:
#I @"./../packages/Microsoft.AspNet.SignalR.Core/lib/net45"
#r "nuget: Microsoft.AspNet.SignalR.Core"
#I @"./../packages/Microsoft.Owin/lib/net451"
#r "nuget: Microsoft.Owin"
#I @"./../packages/Microsoft.Owin.Security/lib/net451"
#r "nuget: Microsoft.Owin.Security"
#I @"./../packages/Owin.Security.AesDataProtectorProvider/lib/net45"
#r "nuget: Owin.Security.AesDataProtectorProvider"
#r "nuget: Microsoft.AspNet.WebApi.Core"
#r "nuget: Microsoft.AspNet.WebApi.Owin"
#r "nuget: Microsoft.Net.Http"
#I @"./../packages/Microsoft.Owin.Security.Cookies/lib/net451"
#r "nuget: Microsoft.Owin.Security.Cookies"
#I @"./../packages/Microsoft.Owin.Security.Facebook/lib/net451"
#r "nuget: Microsoft.Owin.Security.Facebook"
#I @"./../packages/Microsoft.Owin.Security.Google/lib/net451"
#r "nuget: Microsoft.Owin.Security.Google"
#I @"./../packages/Microsoft.Owin.Hosting/lib/net451"
#r "nuget: Microsoft.Owin.Hosting"
#I @"./../packages/Microsoft.Owin.Host.HttpListener/lib/net451"
#r "nuget: Microsoft.Owin.Host.HttpListener"
#I @"./../packages/Microsoft.Owin.StaticFiles/lib/net451"
#r "nuget: Microsoft.Owin.StaticFiles"
#I @"./../packages/Microsoft.Owin.FileSystems/lib/net451"
#r "nuget: Microsoft.Owin.FileSystems"
#I @"./../packages/Microsoft.Owin.Cors/lib/net451"
#r "nuget: Microsoft.Owin.Cors"
#I @"./../packages/Microsoft.Owin.Diagnostics/lib/net451/"
#r "nuget: Microsoft.Owin.Diagnostics"
#r "nuget: Microsoft.AspNet.WebApi.Client"
#I @"./../packages/Microsoft.AspNet.Cors/lib/net45"
#I @"./../packages/Newtonsoft.Json/lib/net45"
#r "nuget: Newtonsoft.Json"
#I @"./../packages/Owin/lib/net40"
#r "nuget: Owin"
#I @"./../packages/Logary/lib/net452"
#r "nuget: Logary"
#I @"./../packages/NodaTime/lib/portable-net4+sl5+netcore45+wpa81+wp8+MonoAndroid1+MonoTouch1+XamariniOS1"
#r "nuget: NodaTime"
#I @"./../packages/Hopac/lib/net45"
#r "nuget: Hopac"
#r "nuget: Hopac"
#I @"./../packages/Owin.Compression/lib/net452"
#r "nuget: Owin.Compression"
#I @"./../packages/Kentor.OwinCookieSaver/lib/net452"
#r "nuget: Kentor.OwinCookieSaver"

#r @"System.Configuration.dll"
#r @"System.Configuration.Install.dll"
#r @"System.ServiceProcess.dll"
#r @"System.Transactions.dll"
#r "System.Xml.Linq.dll"

open System
open System.Configuration
open System.Configuration.Install
open System.Linq
open FSharp.Data
open FSharp.Data.Sql
open System.Data.SqlClient
open System.Threading.Tasks
open MySql.Data.MySqlClient
open System.Security.Claims
open Microsoft.AspNet.SignalR
open Microsoft.AspNet.SignalR.Hubs
open System.Threading.Tasks
open Logary

#load "Domain.fs"
#load "Logics.fs"
#load "Scheduler.fs"
#load "SignalRHubs.fs"
#load "OwinStartup.fs"
#load "Program.fs"
let logger = Logary.Logging.getCurrentLogger ()
try
    MyApp.main [||] |> ignore
with
    | e -> Logary.Message.eventError e.Message |> writeLog

#endif

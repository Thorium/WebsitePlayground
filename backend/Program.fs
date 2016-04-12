module MyApp

open System
open SignalRHubs
open OwinStart
open Logary
open Logary.Configuration
open Logary.Targets
open Logary.Metrics

let mutable server = Unchecked.defaultof<IDisposable>
let mutable log = Unchecked.defaultof<IDisposable>

let startServer() =
    log <-
        withLogary' "WebsitePlayground" (
            withTargets [
                // See Logary examples for advanced logging.
                Console.create (Console.empty) "console"
            ] >> withRules [
                Rule.createForTarget "console"
            ]
        )
    //Scheduler.doStuff()

    let options = Microsoft.Owin.Hosting.StartOptions()

    let addPorts protocol addr (ports:string) =
        ports.Split(',')
        |> Array.filter(fun p -> p<>"")
        |> Array.map(fun port -> protocol + "://" + addr + ":" + port)
        |> Array.iter(fun url ->
            LogLine.info url |> logger.Log
            options.Urls.Add url
        )

    addPorts "http" System.Configuration.ConfigurationManager.AppSettings.["WebServerIp"] System.Configuration.ConfigurationManager.AppSettings.["WebServerPorts"]
    addPorts "https" System.Configuration.ConfigurationManager.AppSettings.["WebServerIpSSL"] System.Configuration.ConfigurationManager.AppSettings.["WebServerPortsSSL"]

    // You probably need adnmin rights to start a web server:
    server <- Microsoft.Owin.Hosting.WebApp.Start<MyWebStartup> options

    let logger = Logging.getCurrentLogger ()
    LogLine.info ("Server started.") |> logger.Log

let stopServer() =
    if server <> Unchecked.defaultof<IDisposable> then
        server.Dispose()
    if log <> Unchecked.defaultof<IDisposable> then
        log.Dispose()

/// Start as service:
/// sc create companyweb binPath= "c:\...\WebsitePlayground.exe"
/// sc start companyweb
open System.ServiceProcess
type WinService() =
    inherit ServiceBase(ServiceName = "companyweb")
    override x.OnStart(args) = startServer(); base.OnStart(args)
    override x.OnStop() = stopServer(); base.OnStop()
    override x.Dispose(disposing) = 
        if disposing then stopServer()
        base.Dispose(true)

[<System.ComponentModel.RunInstaller(true)>]
type public FSharpServiceInstaller() =
    inherit System.Configuration.Install.Installer()
    do 
        new ServiceProcessInstaller(Account = ServiceAccount.LocalSystem) |> base.Installers.Add |> ignore
        new ServiceInstaller( 
            DisplayName = "companyweb", ServiceName = "companyweb", StartType = ServiceStartMode.Automatic )
        |> base.Installers.Add |> ignore

#if INTERACTIVE
#else
[<EntryPoint>]
#endif
let main args = 
    if Environment.UserInteractive || Type.GetType ("Mono.Runtime") <> null then
        startServer()
        LogLine.info "Press Enter to stop & quit." |> logger.Log
        Console.ReadLine() |> ignore
        stopServer()
    else
        ServiceBase.Run [| new WinService() :> ServiceBase |];
    0
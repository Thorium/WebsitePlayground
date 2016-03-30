module MyApp

open System
open SignalRHubs
open OwinStart
open Logary
open Logary.Configuration
open Logary.Targets
open Logary.Metrics
open Hopac

let mutable server = Unchecked.defaultof<IDisposable>
let mutable log = Unchecked.defaultof<IDisposable>

let startServer() =
    log <-
        withLogaryManager "WebsitePlayground" (
            withTargets [
                // See Logary examples for advanced logging.
                Console.create (Console.empty) (PointName.ofSingle "console")
            ] >> withRules [
                Rule.createForTarget (PointName.ofSingle "console")
            ]
        ) |> run
    //Scheduler.doStuff()

    let options = Microsoft.Owin.Hosting.StartOptions()

    let addPorts protocol addr (ports:string) =
        ports.Split(',')
        |> Array.filter(fun p -> p<>"")
        |> Array.map(fun port -> protocol + "://" + addr + ":" + port)
        |> Array.iter(fun url ->
            Message.eventInfo url |> writeLog
            options.Urls.Add url
        )

    addPorts "http" System.Configuration.ConfigurationManager.AppSettings.["WebServerIp"] System.Configuration.ConfigurationManager.AppSettings.["WebServerPorts"]
    addPorts "https" System.Configuration.ConfigurationManager.AppSettings.["WebServerIpSSL"] System.Configuration.ConfigurationManager.AppSettings.["WebServerPortsSSL"]

    server <- Microsoft.Owin.Hosting.WebApp.Start<MyWebStartup> options

    let logger = Logging.getCurrentLogger ()
    Message.eventInfo ("Server started.") |> writeLog

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
        Message.eventInfo "Press Enter to stop & quit." |> writeLog
        Console.ReadLine() |> ignore
        stopServer()
    else
        ServiceBase.Run [| new WinService() :> ServiceBase |];
    0
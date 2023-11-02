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
let hostName = System.Net.Dns.GetHostName()
let serverPath = System.Reflection.Assembly.GetExecutingAssembly().Location |> System.IO.Path.GetDirectoryName

let startServer() =

    System.Net.ServicePointManager.SecurityProtocol <- System.Net.SecurityProtocolType.Tls12 ||| System.Net.SecurityProtocolType.Tls11

    let fetchLogLevel =
        match System.Configuration.ConfigurationManager.AppSettings.["LogLevel"].ToString().ToLower() with
        | "error" -> LogLevel.Error
        | "warn" | "warning" -> LogLevel.Warn
        | "info" -> LogLevel.Info
        | "debug" -> LogLevel.Debug
        | _ -> LogLevel.Verbose

    log <-
        withLogaryManager "WebsitePlayground" (
            withTargets [
                // See Logary examples for advanced logging.
                LiterateConsole.create (LiterateConsole.empty) "console"
            ] >> withRules [
                Rule.createForTarget "console" |> Rule.setLevel fetchLogLevel
            ] >> withMiddleware (fun next msg ->
                  msg |> Message.setContextValue "host" (String hostName) |> Message.setContextValue "path" (String serverPath)
            )
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

    // You probably need adnmin rights to start a web server:
    server <- Microsoft.Owin.Hosting.WebApp.Start<MyWebStartup> options

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
    override x.OnStart(args) =
        Logary.Message.eventInfo "Starting server" |> writeLog
        startServer(); base.OnStart(args)
    override x.OnStop() =
        Logary.Message.eventInfo "Stopping server" |> writeLog
        stopServer(); base.OnStop()
    override x.Dispose(disposing) =
        Logary.Message.eventInfo "Disposing server" |> writeLog
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
    try
        if Environment.UserInteractive || not(isNull (Type.GetType "Mono.Runtime")) then
            startServer()
            Message.eventInfo "Press Enter to stop & quit." |> writeLog
            Console.ReadLine() |> ignore
            stopServer()
        else
            ServiceBase.Run [| new WinService() :> ServiceBase |];
    with
    | e -> Logary.Message.eventError "Error with webserver {err}"
            |> Logary.Message.setField "err" (e.ToString())
            |> writeLog
           Console.WriteLine (e.GetBaseException().Message)
    0

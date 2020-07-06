module MyApp

open System
open SignalRHubs
open OwinStart
open Logary
open Logary.Configuration
open Logary.Targets

//open Logary.Metrics

let hostName = System.Net.Dns.GetHostName()
let serverPath = System.Reflection.Assembly.GetExecutingAssembly().Location |> System.IO.Path.GetDirectoryName

let startServer() =

    System.Net.ServicePointManager.SecurityProtocol <- System.Net.SecurityProtocolType.Tls12 ||| System.Net.SecurityProtocolType.Tls11

    let log =
        Config.create "WebsitePlayground" "laptop"
        |> Config.target (LiterateConsole.create LiterateConsole.empty "console")
        |> Config.ilogger (ILogger.Console Debug)
        |> Config.build
        |> Hopac.Hopac.run

    //Scheduler.doStuff()

    Saturn.Application.run OwinStart.app
    Message.eventInfo ("Server started.") |> writeLog

let stopServer() =
    ()

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

//[<System.ComponentModel.RunInstaller(true)>]
//type public FSharpServiceInstaller() =
//    inherit System.Configuration.Install.Installer()
//    do
//        new ServiceProcessInstaller(Account = ServiceAccount.LocalSystem) |> base.Installers.Add |> ignore
//        new ServiceInstaller(
//            DisplayName = "companyweb", ServiceName = "companyweb", StartType = ServiceStartMode.Automatic )
//        |> base.Installers.Add |> ignore

#if INTERACTIVE
#else
[<EntryPoint>]
#endif
let main args =
    try
        if Environment.UserInteractive || Type.GetType ("Mono.Runtime") <> null then
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

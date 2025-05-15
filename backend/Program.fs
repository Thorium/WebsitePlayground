module MyApp

open System
open SignalRHubs
open OwinStart
open Logary
open Logary.Configuration
open Logary.Targets
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open System.Security.Cryptography.X509Certificates
open type Microsoft.AspNetCore.Http.TypedResults
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

//open Logary.Metrics

let hostName = System.Net.Dns.GetHostName()
let serverPath = System.Reflection.Assembly.GetExecutingAssembly().Location |> System.IO.Path.GetDirectoryName

/// Start as service:
/// sc create companyweb binPath= "c:\...\WebsitePlayground.exe"
/// sc start companyweb
type CompanyWebWinService(loggerFactory:ILoggerFactory) =
      inherit BackgroundService()

      let logger = loggerFactory.CreateLogger<CompanyWebWinService>()

      override this.StartAsync (cancellationToken: Threading.CancellationToken): Task =
          logger.LogInformation "CompanyWeb is starting"
          base.StartAsync(cancellationToken: Threading.CancellationToken)

      override this.StopAsync (cancellationToken: Threading.CancellationToken): Task =
          logger.LogInformation "CompanyWeb is stopping"
          base.StopAsync(cancellationToken: Threading.CancellationToken)

      override this.Dispose (): unit =
          logger.LogInformation "CompanyWeb is disposing"
          base.Dispose()

      override this.ExecuteAsync (stoppingToken: Threading.CancellationToken): Task =
          task {
              while not stoppingToken.IsCancellationRequested do
                do! Task.Delay(5000, stoppingToken)
          }

let startServer (isService:bool) (args:string[]) =

    let useHttps =
#if DEBUG
        false
#else
        true
#endif

    System.Net.ServicePointManager.SecurityProtocol <- System.Net.SecurityProtocolType.Tls12 ||| System.Net.SecurityProtocolType.Tls11

    let log =
        Config.create "WebsitePlayground" "laptop"
        |> Config.target (LiterateConsole.create LiterateConsole.empty "console")
        |> Config.ilogger (ILogger.Console Debug)
        |> Config.build
        |> Hopac.Hopac.run

    //Scheduler.doStuff()

    let builder = WebApplication.CreateBuilder args

    if isService then
        builder.Services.AddWindowsService() |> ignore
        builder.Services.AddHostedService<CompanyWebWinService>() |> ignore
        ()

    let webApiPort =
        match Int32.TryParse(System.Configuration.ConfigurationManager.AppSettings[if useHttps then "WebServerPortsSSL" else "WebServerPorts"]) with
        | true, value -> value
        | false, _ -> 7050
    builder.WebHost.ConfigureKestrel(fun serverOpts ->
        serverOpts.ListenAnyIP(webApiPort, fun listenOpts ->
            if useHttps then
                listenOpts.UseHttps(
                    StoreName.My,
                    "mydomain.com", //SSL certificate domain name
                    false,
                    StoreLocation.LocalMachine
                ) |> ignore
            ()
        )
    ) |> ignore

    let _ = OwinStart.configureServices builder.Services logger
    let app = builder.Build()
    let _ = OwinStart.configureApp app

    app.Run()

    Message.eventInfo ("Server started.") |> writeLog

let stopServer() =
    ()



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

    let runAsService =
#if DEBUG
        // On debug mode you run this just as command line application
        false
#else
        // On release mode you register this as a service with "sc create"
        true
#endif

    startServer runAsService args
    if not runAsService then
        System.Console.WriteLine("Server started")
        System.Console.ReadLine() |> ignore
        stopServer()

    0

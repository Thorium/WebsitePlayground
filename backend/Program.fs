module MyApp

open System
open SignalRHubs
open OwinStart
open Serilog
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open System.Security.Cryptography.X509Certificates
open type Microsoft.AspNetCore.Http.TypedResults
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

let hostName = System.Net.Dns.GetHostName()
let serverPath = System.Reflection.Assembly.GetExecutingAssembly().Location |> System.IO.Path.GetDirectoryName

do setupLogariSql()

/// Setup a serilog instance and add that to DBandManagement logging
let setupLogging isLive isService =

    let logSetup = LoggerConfiguration().Destructure.FSharpTypes().MinimumLevel

    let logSetup2 =
        (if isService then
            logSetup.Information()
         else
            logSetup.Debug()
         ).Enrich.FromLogContext()

    let logSetup3 =
          logSetup2
            .WriteTo.Conditional((fun e ->
                  e.Level = Events.LogEventLevel.Debug ||
                  e.Level = Events.LogEventLevel.Information ||
                  e.Level = Events.LogEventLevel.Verbose),
                  (fun wt -> wt.File(getRootedPath ("happy" + (if isLive then "-" else "x-") + ".log"), rollingInterval = RollingInterval.Month) |> ignore
                             ()
                  ))
            .WriteTo.Conditional((fun e ->
                  e.Level = Events.LogEventLevel.Warning ||
                  e.Level = Events.LogEventLevel.Error),
                  (fun wt -> wt.File(getRootedPath ("sad" + (if isLive then "-" else "x-") + ".log"), rollingInterval = RollingInterval.Month) |> ignore
                             ()
                  ))

    let serilogLogger =
        (if isService then logSetup3
         else logSetup3.WriteTo.Console())
          //.WriteTo.ApplicationInsights(
            //    serviceProvider.GetRequiredService<TelemetryConfiguration>(),
            //    TelemetryConverter.Traces
            //)
          .CreateLogger();

    let fact = lazy (new LoggerFactory()).AddSerilog(serilogLogger).CreateLogger("CompanyWeb")

    let forced = fact.Force()
    Logari.logger <- fact
    serilogLogger

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
    let logger = setupLogging true isService
    let useHttps =
#if DEBUG
        false
#else
        true
#endif

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

    ()

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

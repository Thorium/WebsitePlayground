module MyApp

open System
open SignalRHubs
open OwinStart
open Logary
open Logary.Configuration
open Logary.Targets
open Logary.Metrics

let mutable server = Unchecked.defaultof<IDisposable>

#if INTERACTIVE
#else
[<EntryPoint>]
#endif
let main args = 
    let logary = 
        withLogary "WebsitePlayground" (
            withTargets [
                Console.create (Console.empty) "console"
            ] >> withRules [
                Rule.createForTarget "console"
            ]
        )
    //Scheduler.doStuff()
    let url = System.Configuration.ConfigurationManager.AppSettings.["WebServer"]
    server <- Microsoft.Owin.Hosting.WebApp.Start<MyWebStartup> url

    let logger = Logging.getCurrentLogger ()
    LogLine.info ("Server started at: " + url) |> logger.Log
    LogLine.info "Press Enter to stop & quit." |> logger.Log
    Console.ReadLine() |> ignore
    if server <> Unchecked.defaultof<IDisposable> then
        server.Dispose()
    0
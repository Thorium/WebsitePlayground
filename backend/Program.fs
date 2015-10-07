module MyApp

open System
open SignalRHubs
open OwinStart

let mutable server = Unchecked.defaultof<IDisposable>

#if INTERACTIVE
#else
[<EntryPoint>]
#endif
let main args = 
    //Scheduler.doStuff()
    let url = System.Configuration.ConfigurationManager.AppSettings.["WebServer"]
    server <- Microsoft.Owin.Hosting.WebApp.Start<MyWebStartup> url

    Console.WriteLine ("Server started at: " + url)
    Console.WriteLine "Press Enter to stop & quit."
    Console.ReadLine() |> ignore
    if server <> Unchecked.defaultof<IDisposable> then
        server.Dispose()
    0
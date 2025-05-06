// --- Communication to client --------------------------------c----
module SignalRHubs

open System
open FSharp.Data
open FSharp.Data.Sql
open FSharp.Data.Sql.Common
open Microsoft.AspNetCore.SignalR
open Microsoft.AspNetCore.SignalR
open System.Threading.Tasks

type IMessageToClient =  // Server can push data to single or all clients
    abstract NotifyDeal : string -> Task
    //abstract ListCompanies : seq<CompanySearchResult> -> Task

let rnd = Random()
open System.Security.Claims
open Logary
let dbContext = dbReadContext()

// Example of queries and real-time communtication
type SignalHub() as this =
    inherit Hub<IMessageToClient>()

    override __.OnConnectedAsync() =
        let t = base.OnConnectedAsync()
        Message.eventInfo "Client connected: {clientId}" |> Message.setField "clientId" this.Context.ConnectionId |> writeLog
        // We could do authentication check here.
        t

    member __.SearchCompanies (searchparams:SearchObject) =
        task {
            let! companies =
                Logics.executeSearch (dbReadContext()) searchparams

            // Can signal as separate client call:
            //this.Clients.Caller.ListCompanies hosts |> ignore

            // Or just return from the method:
            return companies
        }

    //[<Authorize(Roles = "loggedin")>]
    member __.BuyStocks (company:string, amount:int) =
        //Signal to all users is as easy as signal to single user:
        this.Clients.All.NotifyDeal ("Announcement to all users: " + (string)amount + " of " + company + " stocks just sold!")

// Example of basic CRUD
type CompanyHub() =
    inherit Hub<IMessageToClient>()

    let ``map data from form to database format`` (formData: seq<string*obj>) =
            formData // Add missing fields
            |> Seq.append [|
                    "LastUpdate", DateTimeNow() |> box ;
                |]
            |> Seq.map (
                fun (key, valu) ->  // Convert some fields
                    match key with
                    | "LogoUrl" -> "LogoUrl", match valu.ToString().StartsWith("http") with true -> valu | false -> "http://" + valu.ToString() |> box
                    | "Founded" -> let fdate = valu.ToString() |> DateTime.Parse
                                   "Founded", fdate.ToString("yyyy-MM-dd") |> box
                    | _ -> key,valu)


    member __.Create (data: seq<string*obj>) =
      let transaction =
        writeWithDbContextAsync <| fun (dbContext:WriteDataContext) ->
            async {
                let entity = data
                             |> ``map data from form to database format``
                                // Avoid un-typed Create and prefer rather storngly typed:
                                // dbContext.Companyweb.Company.``Create(CEO, Founded, LastUpdate, Name)``("CEO", DateTime(2000,01,01), DateTime.UtcNow, "Mr Hietanen")
                             |> dbContext.Companyweb.Company.Create

                do! dbContext.SubmitUpdates2()
                return entity.ColumnValues
            }
      transaction |> Async.StartAsTask

    member __.Read itemId =
        task {
            let! res = 
                query {
                    for u in dbReadContext().Companyweb.Company do
                    where (u.Id = itemId)
                } |> Seq.tryHeadAsync
            return
                match res with
                | Some e -> e.ColumnValues
                | None -> Seq.empty
        }

    member __.Update itemId data =
      writeWithDbContext <| fun (dbContext:WriteDataContext) ->
        Logics.executeCrud dbContext itemId (fun e -> data |> ``map data from form to database format`` |> Seq.iter(fun (k,o) -> e.SetColumn(k, o)))
        

    member __.Delete itemId =
      writeWithDbContext <| fun (dbContext:WriteDataContext) ->
        Logics.executeCrud dbContext itemId (fun e -> e.Delete())



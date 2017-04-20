// --- Communication to client --------------------------------c----
module SignalRHubs

open System
open FSharp.Data
open FSharp.Data.Sql
open FSharp.Data.Sql.Common
open Microsoft.AspNet.SignalR
open Microsoft.AspNet.SignalR.Hubs
open System.Threading.Tasks

type IMessageToClient =  // Server can push data to single or all clients
    abstract ListCompanies : seq<CompanySearchResult> -> Task
    abstract NotifyDeal : string -> Task

let rnd = Random()
open System.Security.Claims
open Logary
let dbContext = dbReadContext()

// Example of queries and real-time communtication
[<HubName("SignalHub")>]
type SignalHub() as this =
    inherit Hub<IMessageToClient>()

    override __.OnConnected() =
        let t = base.OnConnected()
        Message.eventInfo "Client connected: {clientId}" |> Message.setField "clientId" this.Context.ConnectionId |> writeLog
        // We could do authentication check here.
        t


    member __.SearchCompanies (searchparams:SearchObject) =
        let ceoFilter =
            match searchparams.CEOName with
            | None -> ""
            | Some(ceo) -> ceo

        async {
            let! companies =
                query {
                    for c in dbContext.Companyweb.Company do
                    where (
                        c.Founded < searchparams.FoundedBefore
                        && c.Founded > searchparams.FoundedAfter
                        && c.Name =% "%" + searchparams.CompanyName.ToUpper() + "%"
                        && c.Ceo =% "%" + ceoFilter + "%"
                    )
                    select {
                        CompanyName = c.Name;
                        Url = c.WebSite;
                        Image = c.LogoUrl
                    }
                } |> Array.executeQueryAsync
            return companies
        } |> Async.StartAsTask

    //[<Authorize(Roles = "loggedin")>]
    member __.BuyStocks (company:string) (amount:int) =
        //Signal to all users is as easy as signal to single user:
        this.Clients.All.NotifyDeal ("Announcement to all users: " + (string)amount + " of " + company + " stocks just sold!")

// Example of basic CRUD
[<HubName("CompanyHub")>]
type CompanyHub() =
    inherit Hub<IMessageToClient>()

    let executeCrud (dbContext:DataContext) itemId actionToEntity =
        async {
            let! fetched =
                query {
                    for u in dbContext.Companyweb.Company do
                    where (u.Id = itemId)
                } |> Seq.tryHeadAsync
            match fetched with
            | Some entity ->
                entity |> actionToEntity
                do! dbContext.SubmitUpdates2 ()
                return entity.ColumnValues
            | None -> return Seq.empty
        }

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
        writeWithDbContextAsync <| fun (dbContext:DataContext) ->
            async {
                let entity = data
                             |> ``map data from form to database format``
                             |> dbContext.Companyweb.Company.Create
                do! dbContext.SubmitUpdates2()
                return entity.ColumnValues
            }
      transaction |> Async.StartAsTask

    member __.Read itemId =
        executeCrud (dbReadContext()) itemId (fun e -> ())
        |> Async.StartAsTask

    member __.Update itemId data =
      writeWithDbContext <| fun (dbContext:DataContext) ->
        executeCrud dbContext itemId (fun e -> data |> ``map data from form to database format`` |> Seq.iter(fun (k,o) -> e.SetColumn(k, o)))
        |> Async.StartAsTask

    member __.Delete itemId =
      writeWithDbContext <| fun (dbContext:DataContext) ->
        executeCrud dbContext itemId (fun e -> e.Delete())
        |> Async.StartAsTask



// --- Communication to client --------------------------------c----
module SignalRHubs

open System
open FSharp.Data
open FSharp.Data.Sql
open Microsoft.AspNet.SignalR
open Microsoft.AspNet.SignalR.Hubs 
open System.Threading.Tasks

type IMessageToClient =  // Server can push data to single or all clients
    abstract ListCompanies : seq<CompanySearchResult> -> Task
    abstract NotifyDeal : string -> Task

let rnd = Random()
open System.Security.Claims
open Logary

// Example of queries and real-time communtication
[<HubName("SignalHub")>]
type SignalHub() as this = 
    inherit Hub<IMessageToClient>()

    override __.OnConnected() =
        let t = base.OnConnected()
        LogLine.info ("Client connected: " + this.Context.ConnectionId) |> logger.Log
        // We could do authentication check here.
        t

    member __.SearchCompanies (searchparams:SearchObject) =
        let tempRating = Math.Round(rnd.NextDouble()*5.0,2)
        let ceoFilter =
            match searchparams.CEOName with
            | None -> ""
            | Some(ceo) -> ceo

        let hosts = 
            query {
                for c in dbContext.``[companyweb].[company]`` do
                // join d in dbContext.``[companyweb].[stocks]`` on (c.Id = d.ForeignKey)
                where (
                    c.Founded < searchparams.FoundedBefore
                    && c.Founded > searchparams.FoundedAfter
                    && c.Name =% "%" + searchparams.CompanyName.ToUpper() + "%"
                    && c.CEO =% "%" + ceoFilter + "%"
                )
                select ({
                        Id = c.Id;
                        CompanyName = c.Name; 
                        Url = c.WebSite;
                        Image = c.LogoUrl
                        }) 
            } |> Seq.toArray
        this.Clients.Caller.ListCompanies hosts |> ignore

    //[<Authorize(Roles = "loggedin")>]
    member __.BuyStocks (company:string) (amount:int) = 
        //Signal to all users is as easy as signal to single user:
        this.Clients.All.NotifyDeal ("Announcement to all users: " + (string)amount + " of " + company + " stocks just sold!")

// Example of basic CRUD
[<HubName("CompanyHub")>]
type CompanyHub() = 
    inherit Hub<IMessageToClient>()

    let executeCrud itemId actionToEntity =
        let entity =
            query {
                for u in dbContext.``[companyweb].[company]`` do
                where (u.Id = itemId)
                head
            }
        entity |> actionToEntity
        dbContext.SubmitUpdates2 ()
        entity.ColumnValues

    let ``map data from form to database format`` (formData: seq<string*obj>) =
            formData // Add missing fields
            |> Seq.append [| 
                    "LastUpdate", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") |> box ; 
                |]
            |> Seq.map (
                fun (key, valu) ->  // Convert some fields
                    match key with
                    | "LogoUrl" -> "LogoUrl", match valu.ToString().StartsWith("http") with true -> valu | false -> "http://" + valu.ToString() |> box
                    | "Founded" -> let fdate = valu.ToString() |> DateTime.Parse
                                   "Founded", fdate.ToString("yyyy-MM-dd") |> box
                    | _ -> key,valu)


    member __.Create (data: seq<string*obj>) = 
        let entity = data
                     |> ``map data from form to database format``
                     |> dbContext.``[companyweb].[company]``.Create
        dbContext.SubmitUpdates2()
        entity.ColumnValues

    member __.Read itemId = 
        executeCrud itemId (fun e -> ())

    member __.Update itemId data = 
        executeCrud itemId (fun e -> data |> ``map data from form to database format`` |> Seq.iter(fun (k,o) -> e.SetColumn(k, o)))

    member __.Delete itemId = 
        executeCrud itemId (fun e -> e.Delete())

let hubConfig = HubConfiguration(EnableDetailedErrors = true, EnableJavaScriptProxies = true)

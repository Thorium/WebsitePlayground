[<AutoOpen>]
module Domain

open System
open System.Linq
open FSharp.Data
open FSharp.Data.Sql

open System.Data.SqlClient
open System.Threading.Tasks

open MySql.Data.MySqlClient
open Hopac
open Logary

// --- SQL-Server connection ------------------------------------


[<Literal>]
let Mysqldatapath = __SOURCE_DIRECTORY__ + @"/../packages/MySql.Data/lib/net45/"
type TypeProviderConnection =
    SqlDataProvider< // Supports: MS SQL Server, SQLite, PostgreSQL, Oracle, MySQL (MariaDB), ODBC and MS Access
        ConnectionString = @"server = localhost; database = companyweb; uid = webuser;pwd = p4ssw0rd",
        DatabaseVendor = Common.DatabaseProviderTypes.MYSQL,
        IndividualsAmount=1000,
        UseOptionTypes=true,
        Owner="companyweb",
        CaseSensitivityChange = Common.CaseSensitivityChange.ORIGINAL,
        ResolutionPath=Mysqldatapath>

let cstr = System.Configuration.ConfigurationManager.AppSettings.["RuntimeDBConnectionString"]
let internal createDbReadContext() =
    let rec createCon x =
        try
            if cstr = null then TypeProviderConnection.GetDataContext()
            else TypeProviderConnection.GetDataContext cstr
        with
        | :? System.Data.SqlClient.SqlException as ex when x < 3 ->
            Logary.Logger.log (Logary.Logging.getCurrentLogger()) (Logary.Message.eventWarn ("Error connecting SQL, retrying... " + ex.Message)) |> start
            System.Threading.Thread.Sleep 50
            createCon (x+1)
        | :? System.Data.SqlClient.SqlException as ex when x < 5 ->
            Logary.Logger.log (Logary.Logging.getCurrentLogger()) (Logary.Message.eventWarn ("Error connecting SQL, retrying... " + ex.Message)) |> start
            System.Threading.Thread.Sleep 1500
            createCon (x+1)
    createCon 0

type DataContext = TypeProviderConnection.dataContext

let mutable internal contextHolder = Unchecked.defaultof<Lazy<DataContext>>
let dbReadContext() =
    if contextHolder = null || not (contextHolder.IsValueCreated) then
        try
            let itm = lazy(createDbReadContext())
            contextHolder <- itm
        with
        | e -> Logary.Logger.log (Logary.Logging.getCurrentLogger()) (Logary.Message.eventError ("SQL connection failed: " + e.Message)) |> start
    contextHolder.Force()

let logger = Logary.Logging.getCurrentLogger ()
let writeLog x = Logary.Logger.log logger x |> start

let writeWithDbContextManualComplete<'T>() =
    let isMono = Type.GetType ("Mono.Runtime") <> null
    let context = TypeProviderConnection.GetDataContext cstr
    let scope =
        match isMono with
        | true -> new Transactions.TransactionScope()
        | false ->
            // Mono would fail to compilation, so we have to construct this via reflection:
            // new Transactions.TransactionScope(Transactions.TransactionScopeAsyncFlowOption.Enabled)
            let transactionAssembly = System.Reflection.Assembly.GetAssembly typeof<System.Transactions.TransactionScope>
            let asynctype = transactionAssembly.GetType "System.Transactions.TransactionScopeAsyncFlowOption"
            let transaction = typeof<System.Transactions.TransactionScope>.GetConstructor [|asynctype|]
            transaction.Invoke [|1|] :?> System.Transactions.TransactionScope
    scope, context

open System.Threading.Tasks
/// Write operations should be wrapped to transaction with this.
let writeWithDbContext<'T> (func:TypeProviderConnection.dataContext -> 'T) =
    let transaction, context = writeWithDbContextManualComplete()
    use scope = transaction
    let transId =
        match System.Transactions.Transaction.Current <> null && System.Transactions.Transaction.Current.TransactionInformation <> null with
        | true -> System.Transactions.Transaction.Current.TransactionInformation.LocalIdentifier
        | false -> ""
    let logmsg act tid =
        let tidm = match String.IsNullOrEmpty tid with | true -> "" | false -> " at trhead " + tid
        "Transaction " + transId + " " + act + tidm
            |> Message.eventDebug |> writeLog
    logmsg "started" System.Threading.Thread.CurrentThread.Name
    let res = func context
    match box res with
    | :? Task as task ->
        let commit = Action<Task>(fun a ->
            if scope<>null then
                try
                    scope.Complete()
                    logmsg "completed" System.Threading.Thread.CurrentThread.Name
                with
                | :? ObjectDisposedException -> ()
            )
        let commitTran1 = task.ContinueWith(commit, TaskContinuationOptions.OnlyOnRanToCompletion)
        let commitTran2 = task.ContinueWith((fun _ ->
            logmsg "failed" System.Threading.Thread.CurrentThread.Name), TaskContinuationOptions.NotOnRanToCompletion)
        res
    | item when item <> null && item.GetType().Name = "FSharpAsync`1" ->
        let msg = "Use writeWithDbContextAsync"
        msg |> Logary.Message.eventError |> writeLog
        failwith msg
    | x ->
        if scope<>null then
            try
                scope.Complete()
                logmsg "completed" System.Threading.Thread.CurrentThread.Name
            with
            | :? ObjectDisposedException -> ()
        res

let writeWithDbContextAsync<'T> (func:TypeProviderConnection.dataContext -> Async<'T>) =
    async {
        let transaction, context = writeWithDbContextManualComplete()
        use scope = transaction
        let transId =
            match System.Transactions.Transaction.Current <> null && System.Transactions.Transaction.Current.TransactionInformation <> null with
            | true -> System.Transactions.Transaction.Current.TransactionInformation.LocalIdentifier
            | false -> ""
        let logmsg act tid =
            let tidm = match String.IsNullOrEmpty tid with | true -> "" | false -> " at trhead " + tid
            "Transaction " + transId + " " + act + tidm
            |> Message.eventDebug |> writeLog
        logmsg "started" System.Threading.Thread.CurrentThread.Name
        let! res = func context
        if scope<>null then
            logmsg "completed" System.Threading.Thread.CurrentThread.Name
            scope.Complete()
        return res
    }

FSharp.Data.Sql.Common.QueryEvents.SqlQueryEvent |> Event.add (fun e -> ("Executed SQL:\r\n" + e.ToString()) |> Logary.Message.eventDebug |> writeLog)

let DateTimeString (dt:DateTime) =
    dt.ToString("yyyy-MM-dd HH\:mm\:ss") //temp .NET fix as MySQL.Data.dll is broken: fails without .ToString(...)

let DateTimeNow() =
    DateTimeString System.DateTime.UtcNow

//let ExecuteSql (query : string) parameters =
//    async {
//       use rawSqlConnection = new MySqlConnection(cstr)
//       do! rawSqlConnection.OpenAsync() |> Async.AwaitTask
////       Message.eventInfo (query) |> writeLog
//       use command = new MySqlCommand(query, rawSqlConnection)
//       parameters |> List.iter(fun (par:string*string) -> command.Parameters.AddWithValue(par) |> ignore)
//       let! affectedRows = command.ExecuteNonQueryAsync() |> Async.AwaitTask
//       match affectedRows with
//       | 0 ->
//           "ExecuteSql 0 rows affected: " + query |> Logary.Message.eventWarn |> writeLog
//           ()
//       | x ->
//           //"ExecuteSql " + x + " rows affected: " + query |> Logary.Message.eventWarn |> writeLog
//           ()
//    }


type Data.Common.DbDataReader with
    member reader.CollectItems(collectfunc) =
        let rec readitems acc =
            async {
                let! moreitems = reader.ReadAsync() |> Async.AwaitTask
                match moreitems with
                | true -> return! readitems (collectfunc(reader)::acc)
                | false -> return acc
            }
        readitems []

// WebApi routing: fails if once encoded "/" in paths.
let doubleUrlEncode = System.Net.WebUtility.UrlEncode >> System.Net.WebUtility.UrlEncode
let doubleUrlDecode = System.Net.WebUtility.UrlDecode >> System.Net.WebUtility.UrlDecode

open System.IO
let getRootedPath (path:string) =
    if Path.IsPathRooted path then
        path
    else
        let parsed =
            path.Split([|@"\"; "/"|], StringSplitOptions.None)
            |> Path.Combine
#if INTERACTIVE
        let basePath = __SOURCE_DIRECTORY__
#else
        let basePath =
            System.Reflection.Assembly.GetExecutingAssembly().Location
            |> Path.GetDirectoryName
#endif
        Path.Combine(basePath, parsed)

type TypeProviderConnection.dataContext with
  /// SubmitUpdates() but on error ClearUpdates()
  member x.SubmitUpdates2() =
    try x.SubmitUpdatesAsync()
    with
    | e -> Logary.Message.eventError (e.ToString() + "\r\n\r\n"+ System.Diagnostics.StackTrace(1, true).ToString()) |> writeLog
           try
               x.ClearUpdates() |> ignore
           with
           | ex2 -> Logary.Logger.log logger (Logary.Message.eventError (ex2.ToString())) |> start
           reraise()

// --- Domain model, system actions -----------------------------

// type DateType =
// | After
// | Before

[<Serializable>]
type SearchObject = {
    // SearchDate: DateType Option;
    FoundedAfter: DateTime;
    FoundedBefore: DateTime;
    CompanyName: string;
    CEOName: string Option;
}

[<Serializable>]
type CompanySearchResult = {
    Id: uint32;
    CompanyName: string;
    Url: string option;
    Image: string option
}

// --- Common functions -----------------------------

let ``calculate SHA256 hash`` : string -> string =
    System.Text.Encoding.UTF8.GetBytes
    >> System.Security.Cryptography.SHA256Managed.Create().ComputeHash
    >> Convert.ToBase64String

let ``compare urlSafe hash`` clear hash =
    let hash1 = clear |> ``calculate SHA256 hash`` |> doubleUrlDecode |> (fun x -> x.Replace("=",""))
    let hash2 = hash |> doubleUrlDecode |> (fun x -> x.Replace("=",""))
    hash1 = hash2

let GetUnionCaseName (x:'a) =
    match Microsoft.FSharp.Reflection.FSharpValue.GetUnionFields(x, typeof<'a>) with
    | case, _ -> case.Name

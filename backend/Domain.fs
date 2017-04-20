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
open Logary.Logger

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

let logger = lazy(Logary.Logging.getCurrentLogger())
let cstr = System.Configuration.ConfigurationManager.AppSettings.["RuntimeDBConnectionString"]
let internal createDbReadContext() =
    let rec createCon x =
        try
            if cstr = null then TypeProviderConnection.GetDataContext()
            else TypeProviderConnection.GetDataContext cstr
        with
        | :? System.Data.SqlClient.SqlException as ex when x < 3 ->
            logSimple (logger.Force()) (Logary.Message.eventWarn ("Error connecting SQL, retrying... {msg}") |> Logary.Message.setField "msg" ex.Message)
            System.Threading.Thread.Sleep 50
            createCon (x+1)
        | :? System.Data.SqlClient.SqlException as ex when x < 5 ->
            logSimple (logger.Force()) (Logary.Message.eventWarn ("Error connecting SQL, retrying... {msg}") |> Logary.Message.setField "msg" ex.Message)
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
        | e -> logSimple (logger.Force()) (Logary.Message.eventError ("SQL connection failed: {msg}") |> Logary.Message.setField "msg" e.Message)
    contextHolder.Force()

let writeLog x = logSimple (logger.Force()) x

let writeWithDbContextManualComplete<'T>() =
    let isMono = Type.GetType ("Mono.Runtime") <> null
    let context = TypeProviderConnection.GetDataContext cstr
    let scope =
        match isMono with
        | true -> Unchecked.defaultof<Transactions.TransactionScope> // new Transactions.TransactionScope()
        | false ->
            // Note1: On Mono, 4.6.1 or newer is requred for compiling TransactionScopeAsyncFlowOption.
            // Note2: We should here set also transactionoption isolation level.
            new Transactions.TransactionScope(System.Transactions.TransactionScopeAsyncFlowOption.Enabled)
//            new Transactions.TransactionScope(
//                Transactions.TransactionScopeOption.Required,
//                new Transactions.TransactionOptions(
//                    IsolationLevel = Transactions.IsolationLevel.RepeatableRead),
//                System.Transactions.TransactionScopeAsyncFlowOption.Enabled)
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
        "Transaction {transId} " + act + " {tidm}"
        |> Message.eventDebug
        |> Logary.Message.setField "transId" transId |> Logary.Message.setField "tidm" tidm
        |> writeLog
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
            "Transaction {transId} " + act + " {tidm}"
            |> Message.eventDebug
            |> Logary.Message.setField "transId" transId |> Logary.Message.setField "tidm" tidm
            |> writeLog
        logmsg "started" System.Threading.Thread.CurrentThread.Name
        let! res = func context
        if scope<>null then
            logmsg "completed" System.Threading.Thread.CurrentThread.Name
            scope.Complete()
        return res
    }

FSharp.Data.Sql.Common.QueryEvents.SqlQueryEvent |> Event.add (fun e -> "Executed SQL: {sql}" |> Logary.Message.eventDebug |> Logary.Message.setField "sql" (e.ToString()) |> writeLog)

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
    | e -> Logary.Message.eventError "SubmitUpdates2 error {err}" |> Logary.Message.setField "err" (e.ToString() + "\r\n\r\n"+ System.Diagnostics.StackTrace(1, true).ToString()) |> writeLog
           try
               x.ClearUpdates() |> ignore
           with
           | ex2 -> logSimple (logger.Force()) (Logary.Message.eventError "SubmitUpdates2 clearing error {err}" |> Logary.Message.setField "err" (ex2.ToString()))
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

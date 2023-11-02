[<AutoOpen>]
module Domain

//http://eiriktsarpalis.github.io/typeshape/#/33
type TaskResponse =
    abstract Accept : TaskFunc<'R> -> 'R
and AsyncResponse =
    abstract Accept : AsyncFunc<'R> -> 'R

and TaskResponse<'T> = { Item : 'T System.Threading.Tasks.Task }
with interface TaskResponse with
        member cell.Accept f = f.Invoke<'T> cell
and AsyncResponse<'T> = { Item : Async<'T> }
with interface AsyncResponse with
        member cell.Accept f = f.Invoke<'T> cell

and TaskFunc<'R> = abstract Invoke<'T> : TaskResponse<'T> -> 'R
and AsyncFunc<'R> = abstract Invoke<'T> : AsyncResponse<'T> -> 'R

let packTask (c : TaskResponse<'T>) = c :> TaskResponse
let unpackTask (cell : TaskResponse) (f : TaskFunc<'R>) : 'R = cell.Accept f
let packAsync (c : AsyncResponse<'T>) = c :> AsyncResponse
let unpackAsync (cell : AsyncResponse) (f : AsyncFunc<'R>) : 'R = cell.Accept f


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
        UseOptionTypes=FSharp.Data.Sql.Common.NullableColumnType.VALUE_OPTION,
        Owner="companyweb",
        CaseSensitivityChange = Common.CaseSensitivityChange.ORIGINAL,
        ResolutionPath=Mysqldatapath>

let logger = lazy(Logary.Logging.getCurrentLogger())
let cstr = System.Configuration.ConfigurationManager.AppSettings.["RuntimeDBConnectionString"]
let internal createDbReadContext() =
    let rec createCon x =
        try
            if isNull cstr then TypeProviderConnection.GetDataContext()
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
    if isNull contextHolder || not (contextHolder.IsValueCreated) then
        try
            let itm = lazy(createDbReadContext())
            contextHolder <- itm
        with
        | e -> logSimple (logger.Force()) (Logary.Message.eventError ("SQL connection failed: {msg}") |> Logary.Message.setField "msg" e.Message)
    contextHolder.Force()

let writeLog x = logSimple (logger.Force()) x

let isMono = not(isNull (Type.GetType "Mono.Runtime"))

let inline writeWithDbContextManualComplete() =
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
    let context =
        if String.IsNullOrEmpty cstr then TypeProviderConnection.GetDataContext()
        else TypeProviderConnection.GetDataContext cstr
    scope, context

open System.Threading.Tasks
open Logary.Logging
/// Write operations should be wrapped to transaction with this.
let inline writeWithDbContext (func:TypeProviderConnection.dataContext -> ^T) =
    let transaction, context = writeWithDbContextManualComplete()
    let scope = transaction
    let transId =
        match not (isNull System.Transactions.Transaction.Current || isNull System.Transactions.Transaction.Current.TransactionInformation) with
        | true -> System.Transactions.Transaction.Current.TransactionInformation.LocalIdentifier
        | false -> ""
    let logmsg act tid =
        let tidm = match String.IsNullOrEmpty tid with | true -> "" | false -> " at trhead " + tid
        Logary.Message.eventDebug("Transaction {transId} " + act + " {tidm}") |> Logary.Message.setField "transId" transId |> Logary.Message.setField "tidm" tidm
        |> writeLog
    logmsg "started" (System.Threading.Thread.CurrentThread.ManagedThreadId.ToString())
    let res = func context
    match box res with
    | :? Task as task ->
        let packed = packTask { Item = res }

        let commit = Func<Task,_>(fun a ->
            let tid = System.Threading.Thread.CurrentThread.ManagedThreadId.ToString()
            if not isMono then
                match a.Status with
                | TaskStatus.RanToCompletion ->
                    if scope<>null then
                        try
                            scope.Complete()
                            logmsg "completed" tid
                            scope.Dispose()
                        with
                        | _ ->
                            logmsg "was disposed" tid
                            ()
                | x ->
                    logmsg "failed" tid
                    scope.Dispose()
            )
        let c = task.ContinueWith(commit) :> Task

        let getRes cell =
            unpackTask cell
                { new TaskFunc<'T> with
                    member __.Invoke (cell : TaskResponse<_>) =
                      let t =
                          async {
                            let! r = cell.Item |> Async.AwaitTask
                            do! c |> Async.AwaitTask
                            return r
                          } |> Async.StartAsTask
                      box(t) :?> 'T
                }

        let x = getRes packed
        x
    | item when (not (isNull item)) && item.GetType().Name = "FSharpAsync`1" ->
        let msg = "Use writeWithDbContextAsync"
        msg |> Logary.Message.eventError |> writeLog
        failwith msg
    | x ->
        let tid = System.Threading.Thread.CurrentThread.ManagedThreadId.ToString()
        if not isMono then
            if not (isNull scope) then
                try
                    scope.Complete()
                    logmsg "completed" tid
                    scope.Dispose()

                with
                | :? ObjectDisposedException ->
                logmsg "was disposed" tid
                ()
        res

let inline writeWithDbContextAsync (func:TypeProviderConnection.dataContext -> Async<'T>) =
    async {
        let transaction, context = writeWithDbContextManualComplete()
        use scope = transaction
        let transId =
            match not (isNull System.Transactions.Transaction.Current || isNull System.Transactions.Transaction.Current.TransactionInformation) with
            | true -> System.Transactions.Transaction.Current.TransactionInformation.LocalIdentifier
            | false -> ""
        let logmsg act tid =
            let tidm = match String.IsNullOrEmpty tid with | true -> "" | false -> " at trhead " + tid
            Logary.Message.eventDebug("Transaction {transId} " + act + " {tidm}") |> Logary.Message.setField "transId" transId |> Logary.Message.setField "tidm" tidm
            |> writeLog
        logmsg "started" (System.Threading.Thread.CurrentThread.ManagedThreadId.ToString())
        let! res = func context
        if not isMono then
            if not (isNull scope) then
                let tid = System.Threading.Thread.CurrentThread.ManagedThreadId.ToString()
                try
                    scope.Complete()
                    logmsg "completed" tid
                    scope.Dispose()
                with
                | :? ObjectDisposedException ->
                    logmsg "was null" tid
                    ()
        return res
    }

FSharp.Data.Sql.Common.QueryEvents.SqlQueryEvent |> Event.add (fun e -> "Executed SQL: {sql}" |> Logary.Message.eventDebug |> Logary.Message.setField "sql" ( e.ToString()) |> writeLog) //Even better would be: e.ToRawSqlWithParamInfo

let DateTimeString (dt:DateTime) =
    dt.ToString("yyyy-MM-dd HH\:mm\:ss") //temp .NET fix as MySQL.Data.dll is broken: fails without .ToString(...)

let DateTimeNow() =
    DateTimeString System.DateTime.UtcNow

let asyncErrorHandling<'a> (a:Async<'a>) =
    async {
        let! res = a |> Async.Catch
        match res with
        | Choice1Of2 x -> return x
        | Choice2Of2 e ->
            Logary.Message.eventError "Async error: {err} \r\n\r\n stacktrace: {stack}"
                |> Logary.Message.setField "err" e
                |> Logary.Message.setField "stack" (System.Diagnostics.StackTrace(1, true).ToString())
                |> writeLog
            return raise e
    }

//let ExecuteSql (query : string) parameters =
//    async {
//       use rawSqlConnection = new MySqlConnection(cstr)
//       do! rawSqlConnection.OpenAsync() |> Async.AwaitTask
////       Message.eventInfo (query) |> writeLog
//       use command = new MySqlCommand(query, rawSqlConnection)
//       parameters |> List.iter(fun (par:string*string) -> command.Parameters.AddWithValue(par) |> ignore)
//       let! affectedRows = command.ExecuteNonQueryAsync() |> Async.AwaitTask
//       do! rawSqlConnection.CloseAsync() |> Async.AwaitTask
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
    async {
        let sqlList = System.Collections.Concurrent.ConcurrentBag<FSharp.Data.Sql.Common.QueryEvents.SqlEventData>()
        use o = FSharp.Data.Sql.Common.QueryEvents.SqlQueryEvent |> Observable.subscribe sqlList.Add
        let! res = x.SubmitUpdatesAsync() |> Async.AwaitTask |> Async.Catch
        match res with
        | Choice1Of2 _ -> ()
        | Choice2Of2 e ->
            let errormsg = (e.ToString() + "\r\n\r\n"+ System.Diagnostics.StackTrace(1, true).ToString())
            let entities = x.GetUpdates() |> List.map (fun entity ->
                let fields = String.Join("\r\n  ", entity.ColumnValues |> Seq.map(fun (c,v) -> match v with null -> c | _ -> c + " " + v.ToString()) |> Seq.toArray)
                "Item: \r\n" + fields) |> Seq.toArray
            let ex = new InvalidOperationException(errormsg + "\r\n\r\nDatabase commit failed for entities: " + String.Join("\r\n", entities) + "\r\n", e)
            Logary.Message.eventError "SubmitUpdates2 error {err}"
            |> Logary.Message.setField "err" errormsg
            |> writeLog
            sqlList |> Seq.iter(fun e ->
                writeLog (Logary.Message.eventError "SQL executed when error" |> Logary.Message.setField "sql" (e.ToString()))
            )
            try
                x.ClearUpdates() |> ignore
            with
            | ex2 -> Logary.Message.eventError "SubmitUpdates2 clearing error {err}" |> Logary.Message.setField "err" (ex2.ToString()) |> writeLog
            return raise ex
    }

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
    Url: string voption;
    Image: string voption
}

module Async =
  /// Async.Start with timeout in seconds
  let StartWithTimeout (timeoutSecs:int) (computation:Async<unit>) =
    let c = new System.Threading.CancellationTokenSource(timeoutSecs*1000)
    Async.Start(computation, cancellationToken = c.Token)

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

let asyncScheduleErrorHandling res =
    async {
        let! r = res |> Async.Catch
        r |> function
            | Choice1Of2 x -> x
            | Choice2Of2 ex ->
                Logary.Message.eventError("Scheduler error {err}, stack {stack}")
                |> Logary.Message.setField "err" (ex.ToString())
                |> Logary.Message.setField "stack" (System.Diagnostics.StackTrace(1, true).ToString())
                |> writeLog
                //System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw()
                //failwith "err"
    }

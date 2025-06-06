[<AutoOpen>]
module Domain

open System
open System.Linq
open FSharp.Data
open FSharp.Data.Sql
open FSharp.Data.Sql.MsSql

//open System.Data.SqlClient
open System.Threading.Tasks

open Logari

// --- SQL-Server connection ------------------------------------


[<Literal>]
let databaseType = Common.DatabaseProviderTypes.MSSQLSERVER_SSDT

[<Literal>]
let dacpacPath = __SOURCE_DIRECTORY__ + @"/../database/bin/database.dacpac"
type TypeProviderConnection =
    SqlDataProvider< // Supports: MS SQL Server, SQLite, PostgreSQL, Oracle, MySQL (MariaDB), ODBC and MS Access
        ConnectionString = @"Data Source=localhost;Initial Catalog=companyweb; Integrated Security=True;TrustServerCertificate=True",
        DatabaseVendor = databaseType,
        SsdtPath = dacpacPath,
        IndividualsAmount=1000,
        UseOptionTypes=FSharp.Data.Sql.Common.NullableColumnType.VALUE_OPTION,
        Owner="companyweb",
        CaseSensitivityChange = Common.CaseSensitivityChange.ORIGINAL
        >


let cstr = System.Configuration.ConfigurationManager.AppSettings.["RuntimeDBConnectionString"]
let internal createDbReadContext() =
    let rec createCon x =
        try
            if isNull cstr then TypeProviderConnection.GetReadOnlyDataContext()
            else TypeProviderConnection.GetReadOnlyDataContext cstr
        with
        | :? Microsoft.Data.SqlClient.SqlException as ex when x < 3 ->
            writeLogSimple (logger.Force()) (Message.eventWarn ("Error connecting SQL, retrying... {msg}") |> Message.setField "msg" ex.Message)
            System.Threading.Thread.Sleep 50
            createCon (x+1)
        | :? Microsoft.Data.SqlClient.SqlException as ex when x < 5 ->
            writeLogSimple (logger.Force()) (Message.eventWarn ("Error connecting SQL, retrying... {msg}") |> Message.setField "msg" ex.Message)
            System.Threading.Thread.Sleep 1500
            createCon (x+1)
    createCon 0

type ReadDataContext = TypeProviderConnection.readDataContext
type WriteDataContext = TypeProviderConnection.dataContext

/// ContextHolderMarker is needed here for unit-tests to work
type ContextHolderMarker = interface end
let mutable internal createNewContext = true
let mutable internal contextHolder = Unchecked.defaultof<Lazy<ReadDataContext>>
/// DataContext which generates a new connection if the connection has failed.
/// This is for read-only use, not submitting transactions or database modifications.
/// If you want to write, create a new transaction via writeWithDbContext or writeWithDbContextAsync
/// If you want to do read-only operation inside transaction, use existing connection with .AsReadOnly() instead.
/// This context doesn't have access to Stored Procedures.
let dbReadContext() =
    if isNull contextHolder || not (contextHolder.IsValueCreated) then
        try
            let itm = lazy(createDbReadContext())
            contextHolder <- itm
        with
        | e -> writeLogSimple (logger.Force()) (Message.eventError ("SQL connection failed: {msg}") |> Message.setField "msg" e.Message)
    contextHolder.Force()

let writeLog x = writeLogSimple (logger.Force()) x

let isMono = not(isNull (Type.GetType "Mono.Runtime"))

/// Creating a database transaction.
/// All the database-operations commit to one transaction, so that no SQL-clauses are executed by other parties/processes while that transaction is happening.
/// The settings are compromize of data-consistency vs performance (via locking the database too much).
/// There are a few settings to significantly affect the above:
/// - TransactionScopeOption: What if you have nested transactions, transaction within a transaction?
///      + The default value is "Required" which means if there is no transaction, create a new one, but if there is already one, then join that one.
///        This will ensure data-consistency by always having a transaction, but trying to avoid dead-locks by not letting transactions wait for each other.
/// - IsolationLevel: Will the transaction lock the database resources on read and write (Serializable) locking the database from other users meanwhile the process is on?
///      + The default is ReadCommitted, which means the data is eventually consistent: Meanwhile transaction is on, letting others read old data that was valid but not data
///        that is written in this transaction.
/// - TransactionScopeAsyncFlowOption: Since .NET 451, there has been this option, that if a transaction changes the thread (e.g. via async operation),
///        then span the transaction to continue in the new thread as well. This is related to async operations, see: https://fsprojects.github.io/SQLProvider/core/async.html
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

/// Write operations should be wrapped to transaction with this.
/// Try to avoid transactions taking possibly seconds.
let inline writeWithDbContext (func:TypeProviderConnection.dataContext -> Task<^T>) =
    task {
        let transaction, context = writeWithDbContextManualComplete()
        use scope = transaction
        let! res = func context

        if not isMono then
            if not (isNull scope) then
                try
                    scope.Complete()
                    scope.Dispose()
                with
                | :? ObjectDisposedException -> ()
        return res
    }

/// Async write operations should be wrapped into a transaction with this.
/// Try to avoid transactions taking possibly seconds.
let inline writeWithDbContextAsync (func:TypeProviderConnection.dataContext -> Async<'T>) =
    async {
        let transaction, context = writeWithDbContextManualComplete()
        use scope = transaction
        let! res = func context
        if not isMono then
          if not(isNull scope) then
              try
                  scope.Complete()
                  scope.Dispose()
              with
              | :? ObjectDisposedException -> ()
        return res
    }

let setupLogariSql() =
    FSharp.Data.Sql.Common.QueryEvents.SqlQueryEvent |> Event.add (fun e ->
        try 
            "Executed SQL: {sql}" |> Message.eventDebug |> Message.setField "sql" ( e.ToString()) |> writeLog //Even better would be: e.ToRawSqlWithParamInfo
        with r -> // If logging fails, not a lot we can do
            printfn $"Executed SQL: {e.ToString()}"
            printfn $"Logari fail {r.Message}"
    )

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
            Message.eventError "Async error: {err} \r\n\r\n stacktrace: {stack}"
                |> Message.setField "err" e
                |> Message.setField "stack" (System.Diagnostics.StackTrace(1, true).ToString())
                |> writeLog
            return raise e
    }

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
        Path.Combine(basePath, parsed) |> Path.GetFullPath

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
            Message.eventError "SubmitUpdates2 error {err}"
            |> Message.setField "err" errormsg
            |> writeLog
            sqlList |> Seq.iter(fun e ->
                writeLog (Message.eventError "SQL executed when error" |> Message.setField "sql" (e.ToString()))
            )
            try
                x.ClearUpdates() |> ignore
            with
            | ex2 -> Message.eventError "SubmitUpdates2 clearing error {err}" |> Message.setField "err" (ex2.ToString()) |> writeLog
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
    >> System.Security.Cryptography.SHA256.Create().ComputeHash
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
                Message.eventError("Scheduler error {err}, stack {stack}")
                |> Message.setField "err" (ex.ToString())
                |> Message.setField "stack" (System.Diagnostics.StackTrace(1, true).ToString())
                |> writeLog
                //System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw()
                //failwith "err"
    }

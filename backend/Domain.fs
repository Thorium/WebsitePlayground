[<AutoOpen>]
module Domain

open System
open System.Linq
open FSharp.Data
open FSharp.Data.Sql

open System.Data.SqlClient
open System.Threading.Tasks

open MySql.Data.MySqlClient

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
// Values for new version of SQLProvider:
//        CaseSensitivityChange = Common.CaseSensitivityChange.ORIGINAL,
        ResolutionPath=Mysqldatapath>

let cstr = System.Configuration.ConfigurationManager.AppSettings.["RuntimeDBConnectionString"]
let dbContext = 
    if cstr = null then TypeProviderConnection.GetDataContext()
    else TypeProviderConnection.GetDataContext cstr

let logger = Logary.Logging.getCurrentLogger ()

let ExecuteSql (query : string) parameters =
    use rawSqlConnection = new MySqlConnection(cstr)
    rawSqlConnection.Open()
    use command = new MySqlCommand(query, rawSqlConnection)
    parameters |> List.iter(fun (par:string*string) -> command.Parameters.AddWithValue(par) |> ignore)
    command.ExecuteNonQuery();

let getRootedPath (path:string) =
    let parsed = 
        path.Split([|@"\"; "/"|], StringSplitOptions.None)
        |> System.IO.Path.Combine
    if System.IO.Path.IsPathRooted parsed then 
        parsed 
    else 
        System.IO.Path.Combine(Environment.CurrentDirectory, parsed)

type TypeProviderConnection.dataContext with
  /// SubmitUpdates() but on error ClearUpdates()
  member x.SubmitUpdates2() = 
    try x.SubmitUpdates()
    with
    | e -> Logary.LogLine.error (e.ToString() + "\r\n\r\n"+ System.Diagnostics.StackTrace(1, true).ToString()) |> logger.Log
           x.ClearUpdates() |> ignore
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

let GetUnionCaseName (x:'a) = 
    match Microsoft.FSharp.Reflection.FSharpValue.GetUnionFields(x, typeof<'a>) with
    | case, _ -> case.Name

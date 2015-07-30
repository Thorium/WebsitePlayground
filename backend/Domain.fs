[<AutoOpen>]
module Domain

open System
open System.Linq
open FSharp.Data
open FSharp.Data.Sql

// --- SQL-Server connection ------------------------------------

[<Literal>]
let mysqldatapath = __SOURCE_DIRECTORY__ + @"/../packages/MySql.Data/lib/net45/"
type SqlConnection = 
    SqlDataProvider< // Supports: MS SQL Server, SQLite, PostgreSQL, Oracle, MySQL (MariaDB), ODBC and MS Access
        ConnectionString = @"server = localhost; database = companyweb; uid = webuser;pwd = p4ssw0rd",
        DatabaseVendor = Common.DatabaseProviderTypes.MYSQL,
        IndividualsAmount=1000,
        UseOptionTypes=true, 
        Owner="companyweb",
// Values for new version of SQLProvider:
//        CaseSensitivityChange = Common.CaseSensitivityChange.ORIGINAL,
        ResolutionPath=mysqldatapath>

let cstr = System.Configuration.ConfigurationManager.AppSettings.["RuntimeDBConnectionString"]
let dbContext = SqlConnection.GetDataContext cstr

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
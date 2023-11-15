// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#I @"./packages/build/Fake/tools"
#r @"FakeLib.dll"
#r @"Fake.SQL.dll"
#r @"System.IO.Compression.dll"
#r @"System.IO.Compression.FileSystem.dll"

open Fake
open Fake.Git
open Fake.SQL
open Fake.ReleaseNotesHelper
open System
open System.IO
open System.Diagnostics
open System.IO.Compression

let buildDir = __SOURCE_DIRECTORY__
let verbosity = MSBuildVerbosity.Minimal
let codeAnalysis = "RunCodeAnalysis","false"
let buildMode = "Configuration", getBuildParamOrDefault "Configuration" "Debug"
let buildType = match snd(buildMode) with | "Release" -> "Rebuild" | _ -> "Build"
let ``database connection string`` = "Data Source=localhost; Initial Catalog=Companyweb; Integrated Security=True;"
let sqlpackagePath = "packages/build/Microsoft.Data.Tools.MsBuild/lib/net46/sqlpackage.exe"

let runShell = fun (command,args) ->
    try
        let P = Process.Start(command, (args : string));
        if (P = null) then (
            printf "\r\n\r\nFailed: %s\r\n" command
        )
        P.WaitForExit();
        if P.ExitCode <> 0 then failwith ("Command failed, try running manually: " + command + " " + args)
    with
    | :? System.ComponentModel.Win32Exception -> 
        printf "\r\n\r\nFailed: %s\r\n" command
        reraise ()

Target "npm" (fun _ ->
    try
       runShell("npm","install -g npm")
       runShell("npm","install -g gulp jshint eslint")
    with
    | :? System.ComponentModel.Win32Exception -> 
       printf "\r\n\r\nNPM and Gulp global install failed."
       runShell("npm","install npm")
    runShell("npm","install --legacy-peer-deps")
)
/// Client side gulp-tasks
Target "gulp" (fun _ ->
    try
        if snd(buildMode)="Release" || snd(buildMode)="release" then
             runShell("gulp","deploy --release ok")
        else runShell("gulp","deploy")
    with
    | _ -> 
        let info = @"Gulp has to be in PATH. e.g in Windows: set path=%path%;%APPDATA%\npm\"
        printfn "%s" info
        reraise ()
)

/// Build the server side project
Target "project" (fun _ ->
    DotNetCli.Build (fun p -> {p with Project = "database/database.fsproj"})
    DotNetCli.Build (fun p -> {p with Project = "backend/WebsitePlayground.fsproj"})

    )

/// Build all
Target "all" (fun _ ->
    printfn @"To start server, run: run.cmd (or sh ./run.sh with OSX/Linux)"
    )

/// Try to start the SQL server
Target "startsql" (fun _ -> 
    try
       //runShell("mysql.server","start")
       ()
    with
      | :? System.ComponentModel.Win32Exception
      | :? System.NullReferenceException
            -> printf "Ensure you have MySQL running..."
    )

/// Reinstall the database.
Target "database" (fun _ -> 
    DotNetCli.Build (fun p -> {p with Project = "database/database.fsproj"})
    let dacpacpath = ("database" @@ "bin")
    let dacpac = !!(dacpacpath @@ "*.dacpac") |> Seq.head
    let profile =
        let profileName = getBuildParamOrDefault "PublishProfile" "False"
        sprintf "database/%s.publish.xml" profileName

    let deployData =
        let demodata = bool.Parse (getBuildParamOrDefault "CreateDemoData" "False")
        if demodata
        then "/v:DeployDemoData=True"
        else "/v:DeployDemoData=False"

    let result =
        execProcess (fun info ->
            info.FileName <- sqlpackagePath
            info.WorkingDirectory <- "./"
            info.Arguments <- (sprintf "/Action:Publish %s /SourceFile:%s /Profile:%s" deployData dacpac profile)
        ) System.TimeSpan.MaxValue

    if result
    then ()
    else failwithf "An error occured running dacpac publish"
)
Target "demodata" (fun _ ->
    let SqlServerData = ``database connection string`` |> getServerInfo
    let path = Path.Combine(buildDir, "backend", "sql", "createdemodata_mssql.sql")
    SqlServer.runScript SqlServerData path
)
Target "package" ( fun _ ->
    
    let tag = "Packaged-" + DateTime.Now.ToString("yyyy-MM-dd-HH.mm.ss")
    let resolvePath p = System.IO.Path.Combine [|__SOURCE_DIRECTORY__; p|]
    let resolvePath2 p1 p2 = System.IO.Path.Combine [|__SOURCE_DIRECTORY__; p1; p2|]
    
    if not(Directory.Exists(Path.Combine [| __SOURCE_DIRECTORY__; "release" |])) then
        Path.Combine [| __SOURCE_DIRECTORY__; "release" |] |> Directory.CreateDirectory |> ignore

    let generateZip source target =
        if not(Directory.Exists source) then failwith ("Directory not exists: " + source)
        if File.Exists target then File.Delete target
        System.IO.Compression.ZipFile.CreateFromDirectory(source, target, CompressionLevel.Optimal, false)

    (resolvePath2 "frontend" "dist", resolvePath2 "release" "wwwroot.zip") ||> generateZip
    (resolvePath2 "backend" "bin", resolvePath2 "release" "server.zip") ||> generateZip
    
    // What you could do: Modify .config-file with "FSharp.Configuration" NuGet-package...

    Branches.tag "" tag
)

Target "start" ( fun _ ->
    if snd(buildMode)="Release" || snd(buildMode)="release" then
        let frontendDistPath = Path.Combine [| __SOURCE_DIRECTORY__; "frontend"; "dist" |]
        let backendBinPath = Path.Combine [| __SOURCE_DIRECTORY__; "backend"; "bin" |]
        if Directory.Exists frontendDistPath then
            Directory.Delete(frontendDistPath, true)
        if Directory.Exists backendBinPath then
            Directory.Delete(backendBinPath, true)
)

"start"
//  =?> ("npm",hasBuildParam "npmrestore")
  ==> "npm"
  ==> "gulp"
  ==> "all"

"startsql"
  ==> "database"
  ==> "demodata"
  ==> "project"
  ==> "all"

"all"
  ==> "package"

RunTargetOrDefault "all"

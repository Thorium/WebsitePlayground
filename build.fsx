// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#I @"./packages/build/FAKE/tools"
#r @"./packages/build/FAKE/tools/FakeLib.dll"
#r @"System.IO.Compression.dll"
#r @"System.IO.Compression.FileSystem.dll"

open Fake
open Fake.Git
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

let runShell = fun (command,args) ->
    try
        let P = Process.Start(command, args);
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
    runShell("npm","install")
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
    try
        FileHelper.Copy
            "backend/mysqlconnector"
            [|
                "packages/MySqlConnector/lib/net46/MySqlConnector.dll";
                "packages/System.Buffers/lib/netstandard1.1/System.Buffers.dll";
                "packages/System.Runtime.InteropServices.RuntimeInformation/lib/net45/System.Runtime.InteropServices.RuntimeInformation.dll";
                "packages/System.Threading.Tasks.Extensions/lib/portable-net45+win8+wp8+wpa81/System.Threading.Tasks.Extensions.dll";
            |]
    with
    | e -> printfn "Couldn't copy MySqlConnector files: %O" e

    !! @"./backend/WebsitePlayground.fsproj"
    |> MSBuild "" buildType [codeAnalysis;buildMode] |> ignore
    )

/// Build all
Target "all" (fun _ ->
    printfn @"To start server, run: run.cmd (or sh ./run.sh with OSX/Linux)"
    )

/// Try to start the SQL server
Target "startsql" (fun _ -> 
    try
       runShell("mysql.server","start")
    with
      | :? System.ComponentModel.Win32Exception
      | :? System.NullReferenceException
            -> printf "Ensure you have MySQL running..."
    )

/// Reinstall the database.
Target "database" (fun _ -> 
    //Note: mysql should be in Path.
    let path = Path.Combine("backend", "sql", "createtables.sql")
    try
       runShell("mysql","-u webuser -pp4ssw0rd -e \"source " + path + "\" --abort-source-on-error")
    with
      | _ -> 
        let info = @"MySQL has to be in PATH. e.g in Windows: set path=%path%;""c:\Program Files\MariaDB 10.0\bin\"""
        printfn "%s" info
        printfn "And have you created the database user?"
        printfn "mysql -u root -pPassword -e \"source backend\sql\createuser.sql\""
        reraise ())

/// Refresh the demo data for the database 
Target "demodata" (fun _ ->
    let path = Path.Combine("backend", "sql", "createdemodata.sql")
    runShell("mysql","-u webuser -pp4ssw0rd companyweb -e \"source " + path + "\" --abort-source-on-error"))
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

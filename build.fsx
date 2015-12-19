// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#I @"./packages/FAKE/tools"
#r @"./packages/FAKE/tools/FakeLib.dll"
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
    let P = Process.Start(command, args);
    P.WaitForExit();
    if P.ExitCode <> 0 then failwith ("Command failed, try running manually: " + command + " " + args)

Target "npm" (fun _ ->
    runShell("npm","install -g npm")
    runShell("npm","install")
)
/// Client side gulp-tasks
Target "gulp" (fun _ ->
    if snd(buildMode)="Release" || snd(buildMode)="release" then
         runShell("gulp","deploy --release ok")
    else runShell("gulp","deploy")
)

/// Build the server side project
Target "project" (fun _ ->
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
    let info = @"MySQL have to be in PATH. e.g in Windows: set path=%path%;""c:\Program Files\MariaDB 10.0\bin\"""
    printfn "%s" info
    runShell("mysql","-u webuser -pp4ssw0rd -e \"source " + path + "\" --abort-source-on-error"))
 
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

Target "start" DoNothing

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

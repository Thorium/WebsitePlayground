// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#I @"./packages/FAKE/tools"
#r @"./packages/FAKE/tools/FakeLib.dll"
open Fake
open Fake.Git
open Fake.ReleaseNotesHelper
open System
open System.IO
open System.Diagnostics

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
    runShell("gulp","deploy")
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
    runShell("mysql","-u webuser -pp4ssw0rd -e \"source " + path + "\""))
 
/// Refresh the demo data for the database 
Target "demodata" (fun _ ->
    let path = Path.Combine("backend", "sql", "createdemodata.sql")
    runShell("mysql","-u webuser -pp4ssw0rd companyweb -e \"source " + path + "\""))

Target "start" DoNothing

"start"
  =?> ("npm",hasBuildParam "npmrestore")
  ==> "gulp"
  ==> "all"

"startsql"
  ==> "database"
  ==> "demodata"
  ==> "project"
  ==> "all"

RunTargetOrDefault "all"

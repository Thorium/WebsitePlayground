#r "paket: groupref build //"
#load "./.fake/build.fsx/intellisense.fsx"

#if !FAKE
#r "netstandard"
#r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

#r @"System.IO.Compression.dll"
#r @"System.IO.Compression.FileSystem.dll"

open Fake
open Fake.Core
open Fake.DotNet
open Fake.Core.TargetOperators
open System
open System.IO
open System.Diagnostics
open System.IO.Compression

Target.initEnvironment ()

let environVarOrDefault varName defaultValue =
    try
        let envvar = (Environment.environVar varName).ToUpper()
        if String.IsNullOrEmpty envvar then defaultValue else envvar
    with
    | _ ->  defaultValue

let buildDir = __SOURCE_DIRECTORY__
let verbosity = MSBuildVerbosity.Minimal
let codeAnalysis = "RunCodeAnalysis","false"
let buildMode = "Configuration", environVarOrDefault "Configuration" "Debug"
//let buildType = match snd(buildMode) with | "Release" -> "Rebuild" | _ -> "Build"

let runTool cmd args workingDir =
    let arguments = args |> String.split ' ' |> Arguments.OfArgs
    Command.RawCommand (cmd, arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

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

Target.create "npm" (fun _ ->
    let npm = ProcessUtils.tryFindFileOnPath "npm" 
    if npm.IsNone then failwith "npm not found" else
    try
       Shell.Exec(npm.Value,"install -g npm") |> ignore
       Shell.Exec(npm.Value,"install -g gulp jshint eslint") |> ignore
    with
    | :? System.ComponentModel.Win32Exception -> 
       printf "\r\n\r\nNPM and Gulp global install failed."
       Shell.Exec(npm.Value,"install npm") |> ignore
    Shell.Exec(npm.Value,"install") |> ignore
)
/// Client side gulp-tasks
Target.create "gulp" (fun _ ->
    let gulp = ProcessUtils.tryFindFileOnPath "gulp" 
    if gulp.IsNone then failwith "gulp not found" else
    try
        if snd(buildMode)="Release" || snd(buildMode)="release" then
             Shell.Exec(gulp.Value,"deploy --release ok") |> ignore
        else Shell.Exec(gulp.Value,"deploy") |> ignore
    with
    | _ -> 
        let info = @"Gulp has to be in PATH. e.g in Windows: set path=%path%;%APPDATA%\npm\"
        printfn "%s" info
        reraise ()
)

/// Build the server side project
Target.create "project" (fun _ ->
    try
        Fake.IO.Shell.copyFiles
            "backend/mysqlconnector"
            [|
                "packages/server/MySqlConnector/lib/netcoreapp3.0/MySqlConnector.dll";
                "packages/server/System.Buffers/lib/netstandard2.0/System.Buffers.dll";
                "packages/server/System.Threading.Tasks.Extensions/lib/netstandard2.0/System.Threading.Tasks.Extensions.dll";
            |]
    with
    | e -> printfn "Couldn't copy MySqlConnector files: %O" e

    Fake.DotNet.DotNet.exec id "build" ("./backend -c " + (snd buildMode)) |> ignore
    
    )

/// Build all
Target.create "all" (fun _ ->
    printfn @"To start server, run: run.cmd (or sh ./run.sh with OSX/Linux)"
    )

/// Try to start the SQL server
Target.create "startsql" (fun _ -> 
    try
       runShell("mysql.server","start")
    with
      | :? System.ComponentModel.Win32Exception
      | :? System.NullReferenceException
            -> printf "Ensure you have MySQL running..."
    )

/// Reinstall the database.
Target.create "database" (fun _ -> 
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
Target.create "demodata" (fun _ ->
    let path = Path.Combine("backend", "sql", "createdemodata.sql")
    runShell("mysql","-u webuser -pp4ssw0rd companyweb -e \"source " + path + "\" --abort-source-on-error"))
Target.create "package" ( fun _ ->
    
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

    //Fake.Git.Branches.tag "" tag
)

Target.create "start" ( fun _ ->
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

Target.runOrDefaultWithArguments "all"

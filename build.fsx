// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#if FAKE
#r "paket:
nuget Fake.Core.Process
nuget Fake.Core.Target
nuget Fake.Core.ReleaseNotes
nuget Fake.DotNet.Cli
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.Testing.NUnit
nuget Fake.DotNet.Testing.XUnit2
nuget Fake.IO.FileSystem
nuget Fake.Tools.Git
nuget Microsoft.Data.SqlClient
nuget Fake.Sql.SqlPackage"

#else
#r "nuget: Fake.Core.Process"
#r "nuget: Fake.Core.Target"
#r "nuget: Fake.Core.ReleaseNotes"
#r "nuget: Fake.DotNet.Cli"
#r "nuget: Fake.DotNet.MSBuild"
#r "nuget: Fake.DotNet.Testing.NUnit"
#r "nuget: Fake.DotNet.Testing.XUnit2"
#r "nuget: Fake.IO.FileSystem"
#r "nuget: Fake.Tools.Git"
#r "nuget: Microsoft.Data.SqlClient"
#r "nuget: Fake.Sql.SqlPackage"

System.Environment.GetCommandLineArgs()
|> Array.skip 2 // skip fsi.exe; build.fsx
|> Array.toList
|> Fake.Core.Context.FakeExecutionContext.Create false __SOURCE_FILE__
|> Fake.Core.Context.RuntimeContext.Fake
|> Fake.Core.Context.setExecutionContext

#endif
#r @"System.IO.Compression.dll"
#r @"System.IO.Compression.FileSystem.dll"

open Fake
open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Tools.Git
open System
open System.IO
open System.Diagnostics
open System.IO.Compression

let buildDir = __SOURCE_DIRECTORY__
let verbosity = MSBuildVerbosity.Minimal
/// Pick path for NPM (and Gulp). Todo: fix and test for Mac
let npmPath, npmCmd =
    let npmP, npmT =
        // Mac / Ubuntu:
        if System.IO.Directory.Exists "/usr/local/bin/" && System.IO.File.Exists "/usr/local/bin/npm" then
            "/usr/local/bin/", "npm"
        // Windows
        elif System.IO.Directory.Exists @"C:\Program Files\nodejs" && System.IO.File.Exists @"C:\Program Files\nodejs\npm.cmd" then
            @"C:\Program Files\nodejs\", "npm.cmd"
        else
        // Try parsing path
        let paths =
            System.Environment.GetEnvironmentVariable("PATH").Split(';')
            |> Seq.map(fun p ->
                if p.EndsWith System.IO.Path.DirectorySeparatorChar then p
                else p + System.IO.Path.DirectorySeparatorChar.ToString())
            |> Seq.filter(fun p ->
                p.Contains "npm" && System.IO.Directory.Exists p)
            |> Seq.toList

        match paths |> List.filter(fun p -> System.IO.File.Exists (p  + "npm.cmd" )) with
        | h::_ -> h, "npm.cmd"
        | [] -> paths |> List.filter(fun p -> System.IO.File.Exists (p  + "npm" )) |> List.head, "npm"

    printfn "Using NPM from %s" npmP
    npmP, npmT

Target.initEnvironment()
let codeAnalysis = "RunCodeAnalysis","false"
let buildMode = "Configuration", Environment.environVarOrDefault "Configuration" "Debug"
let buildType = match snd(buildMode) with | "Release" -> "Rebuild" | _ -> "Build"
let mono = (Environment.environVarOrDefault "MONO" "0") = "1"
let ``database connection string`` = "Data Source=localhost; Initial Catalog=Companyweb; Integrated Security=True;"
let sqlpackagePath = "packages/build/Microsoft.Data.Tools.MsBuild/lib/net46/sqlpackage.exe"
let deployPath = Path.Combine [| __SOURCE_DIRECTORY__; "release" |]
let idx (x:DotNet.BuildOptions) =
    let configuration =  DotNet.BuildConfiguration.fromEnvironVarOrDefault "Configuration" DotNet.BuildConfiguration.Debug
    let options = {
        x with
            Configuration = configuration
            MSBuildParams = { x.MSBuildParams with DisableInternalBinLog = true }
    }
    if String.Equals(snd(buildMode), "Release", StringComparison.InvariantCultureIgnoreCase) then
        options
        |> DotNet.Options.withCustomParams (Some "--no-incremental")
    else options

let runShell = fun (command, args) ->
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

Target.create "npm" (fun _ ->
    try
       runShell(npmPath + npmCmd,"install -g npm")
       runShell(npmPath + npmCmd,"install -g gulp jshint eslint")
    with
    | e when mono ->
       printf "\r\n\r\nNPM and Gulp global install failed: %s" e.Message
       runShell(npmPath + npmCmd,"install npm")
    | :? System.ComponentModel.Win32Exception ->
       printf "\r\n\r\nNPM and Gulp global install failed."
       runShell(npmPath + npmCmd,"install npm")
    runShell(npmPath + npmCmd,"install")
)
// Client side gulp-tasks
Target.create "gulp" (fun _ ->
    try
        let gulpCmd =

            match npmCmd with
            | "npm.cmd" when System.IO.File.Exists (npmPath + "gulp.cmd") -> npmPath + "gulp.cmd"
            | "npm.cmd" ->
              let firstTrial = Path.Combine [|"node_modules"; ".bin"; "gulp.cmd"|]
              if System.IO.File.Exists firstTrial then firstTrial
              else Path.Combine [|"node_modules"; ".bin"; "gulp"; "node_modules"; "gulp.cmd"|]
            | "npm" ->
                npmPath + "gulp"
            | _ -> failwith "Gulp not found."

        if snd(buildMode)="Release" || snd(buildMode)="release" then
             runShell(gulpCmd,"deploy --release ok")
        else runShell(gulpCmd,"deploy")
    with
    | _ ->
        let info = @"Gulp has to be in PATH. e.g in Windows: set path=%path%;%APPDATA%\npm\"
        printfn "%s" info
        reraise ()
)

// Build the server side project
Target.create "project" (fun _ ->
    DotNet.build idx "database/fsproj/database.fsproj"
    DotNet.build idx "backend/WebsitePlayground.fsproj"

    )

// Build the server side project
Target.create "test" (fun _ ->
    DotNet.build idx "UnitTests/UnitTests.fsproj"
    DotNet.test id "UnitTests/UnitTests.fsproj"
    ()
    )

// Build all
Target.create "all" (fun _ ->
    printfn @"To start server, run: run.cmd (or sh ./run.sh with OSX/Linux)"
    )

// Try to start the SQL server
Target.create "startsql" (fun _ ->
    try
       //runShell("mysql.server","start")
       ()
    with
      | :? System.ComponentModel.Win32Exception
      | :? System.NullReferenceException
      | _ -> printf "Ensure you have MySQL running..."
    )

// Reinstall the database.
Target.create "database" (fun _ ->
    DotNet.build idx "database/fsproj/database.fsproj"
    let dacpacpath = ("database" @@ "bin")
    let dacpac = !!(dacpacpath @@ "*.dacpac") |> Seq.head
    let profile =
        let profileName = Environment.environVarOrDefault "PublishProfile" "False"
        sprintf "database/%s.publish.xml" profileName

    let deployData =
        let demodata = bool.Parse (Environment.environVarOrDefault "CreateDemoData" "False")
        if demodata
        then "/v:DeployDemoData=True"
        else "/v:DeployDemoData=False"

    ()
    // let result =
    //     CreateProcess.fromRawCommand sqlpackagePath [ (sprintf "/Action:Publish %s /SourceFile:%s /Profile:%s" deployData dacpac profile) ]
    //     |> CreateProcess.withWorkingDirectory "./"
    //     |> Proc.run

    // if result.ExitCode = 0
    // then ()
    // else failwithf "An error occured running dacpac publish"
)
Target.create "demodata" (fun _ ->
//    let connectionString = `database connection string``
//    let path = Path.Combine(buildDir, "backend", "sql", "createdemodata_mssql.sql")
//    Fake.Sql.SqlServer.runScript SqlServerData path
      ()
)

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

    Branches.tag "" tag
)

Target.create "start" ( fun _ ->
    if String.Equals(snd(buildMode), "Release", StringComparison.InvariantCultureIgnoreCase) then
        let frontendDistPath = Path.Combine [| __SOURCE_DIRECTORY__; "frontend"; "dist" |]
        let backendBinPath = Path.Combine [| __SOURCE_DIRECTORY__; "backend"; "bin" |]
        if Directory.Exists frontendDistPath then
            Directory.Delete(frontendDistPath, true)
        if Directory.Exists backendBinPath then
            Directory.Delete(backendBinPath, true)
    ()
)

Target.create "clean" (fun _ ->
    printfn @"Removing previous binaries"
    Shell.deleteDirs ["bin"; "temp"; "./backend/bin"; deployPath]
    printfn @"Removing done."
)

Target.create "release" (fun _ ->
    runShell("build", sprintf "package Configuration=Release -o \"%s\"" deployPath)
)

Target.create "" (fun _ -> ())

"start"
//  =?> ("npm",hasBuildParam "npmrestore")
  ==> "npm"
  ==> "gulp"
  ==> "all"

"startsql"
  ==> "database"
  ==> "demodata"
  ==> "project"
  ==> "test"
  ==> "all"

"all"
  ==> "package"

Target.runOrDefaultWithArguments "all"

// To build local release, e.g.:
// set Configuration=Release
// build -t package

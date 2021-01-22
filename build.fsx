#r "paket:
nuget BlackFox.Fake.BuildTask
nuget Fake.Core.Target
nuget Fake.Core.Process
nuget Fake.Core.ReleaseNotes
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.AssemblyInfoFile
nuget Fake.DotNet.Paket
nuget Fake.DotNet.FSFormatting
nuget Fake.DotNet.Fsi
nuget Fake.DotNet.NuGet
nuget Fake.Api.Github
nuget Fake.DotNet.Testing.Expecto 
nuget Fake.Tools.Git //"

#if !FAKE
#load "./.fake/build.fsx/intellisense.fsx"
#r "netstandard" // Temp fix for https://github.com/dotnet/fsharp/issues/5216
#endif

open BlackFox.Fake
open System.IO
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Tools

let runDotNet cmd workingDir =
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

let project = "Plotly.NET"

let testProject = "tests/Plotly.NET.Tests/Plotly.NET.Tests.fsproj"

let summary = "A F# interactive charting library using plotly.js"

let solutionFile  = "Plotly.NET.sln"

let configuration = "Release"

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted
let gitOwner = "plotly"
let gitHome = sprintf "%s/%s" "https://github.com" gitOwner

let gitName = "Plotly.NET"

let website = "/Plotly.NET"

let pkgDir = "pkg"

let release = ReleaseNotes.load "RELEASE_NOTES.md"

let projectRepo = "https://github.com/plotly/Plotly.NET"

let stableVersion = SemVer.parse release.NugetVersion

let stableVersionTag = (sprintf "%i.%i.%i" stableVersion.Major stableVersion.Minor stableVersion.Patch )

let mutable prereleaseSuffix = ""

let mutable prereleaseTag = ""

let mutable isPrerelease = false

[<AutoOpen>]
module MessagePrompts =

    let prompt (msg:string) =
        System.Console.Write(msg)
        System.Console.ReadLine().Trim()
        |> function | "" -> None | s -> Some s
        |> Option.map (fun s -> s.Replace ("\"","\\\""))

    let rec promptYesNo msg =
        match prompt (sprintf "%s [Yn]: " msg) with
        | Some "Y" | Some "y" -> true
        | Some "N" | Some "n" -> false
        | _ -> System.Console.WriteLine("Sorry, invalid answer"); promptYesNo msg

    let releaseMsg = """This will stage all uncommitted changes, push them to the origin and bump the release version to the latest number in the RELEASE_NOTES.md file. 
        Do you want to continue?"""

    let releaseDocsMsg = """This will push the docs to gh-pages. Remember building the docs prior to this. Do you want to continue?"""


let setPrereleaseTag = BuildTask.create "SetPrereleaseTag" [] {
    printfn "Please enter pre-release package suffix"
    let suffix = System.Console.ReadLine()
    prereleaseSuffix <- suffix
    prereleaseTag <- (sprintf "%s-%s" release.NugetVersion suffix)
    isPrerelease <- true
}

let clean = BuildTask.create "Clean" [] {
    !! "src/**/bin"
    ++ "src/**/obj"
    ++ "pkg"
    ++ "bin"
    |> Shell.cleanDirs 
}

let build = BuildTask.create "Build" [clean] {
    solutionFile
    |> DotNet.build id
}

let copyBinaries = BuildTask.create "CopyBinaries" [clean; build] {
    let targets = 
        !! "src/**/*.??proj"
        -- "src/**/*.shproj"
        |>  Seq.map (fun f -> ((Path.getDirectory f) </> "bin" </> configuration, "bin" </> (Path.GetFileNameWithoutExtension f)))
    for i in targets do printfn "%A" i
    targets
    |>  Seq.iter (fun (fromDir, toDir) -> Shell.copyDir toDir fromDir (fun _ -> true))
}

let pack = BuildTask.create "Pack" [clean; build; copyBinaries] {
    if promptYesNo (sprintf "creating stable package with version %s OK?" stableVersionTag ) 
        then
            !! "src/**/*.*proj"
            |> Seq.iter (Fake.DotNet.DotNet.pack (fun p ->
                let msBuildParams =
                    {p.MSBuildParams with 
                        Properties = ([
                            "Version",stableVersionTag
                            "PackageReleaseNotes",  (release.Notes |> String.concat "\r\n")
                        ] @ p.MSBuildParams.Properties)
                    }
                {
                    p with 
                        MSBuildParams = msBuildParams
                        OutputPath = Some pkgDir
                }
            ))
    else failwith "aborted"
}

let packPrerelease = BuildTask.create "PackPrerelease" [setPrereleaseTag; clean; build; copyBinaries] {
    if promptYesNo (sprintf "package tag will be %s OK?" prereleaseTag )
        then 
            !! "src/**/*.*proj"
            //-- "src/**/Plotly.NET.Interactive.fsproj"
            |> Seq.iter (Fake.DotNet.DotNet.pack (fun p ->
                        let msBuildParams =
                            {p.MSBuildParams with 
                                Properties = ([
                                    "Version", prereleaseTag
                                    "PackageReleaseNotes",  (release.Notes |> String.toLines )
                                ] @ p.MSBuildParams.Properties)
                            }
                        {
                            p with 
                                VersionSuffix = Some prereleaseSuffix
                                OutputPath = Some pkgDir
                                MSBuildParams = msBuildParams
                        }
            ))
    else
        failwith "aborted"
}

let runTests = BuildTask.create "RunTests" [clean; build; copyBinaries] {
    let standardParams = Fake.DotNet.MSBuild.CliArguments.Create ()
    Fake.DotNet.DotNet.test(fun testParams ->
        {
            testParams with
                Logger = Some "console;verbosity=detailed"
        }
    ) testProject
}

// --------------------------------------------------------------------------------------
// Generate the documentation

let initDocPage = BuildTask.create "InitDocsPage" [] {
    printfn "Please enter filename"
    let filename = System.Console.ReadLine()
    
    printfn "Please enter title"
    let title = System.Console.ReadLine()

    let path = "./docs" </> filename

    let lines = """
(*** hide ***)

(*** condition: prepare ***)
#r @"..\packages\Newtonsoft.Json\lib\netstandard2.0\Newtonsoft.Json.dll"
#r "../bin/Plotly.NET/netstandard2.1/Plotly.NET.dll"

(*** condition: ipynb ***)
#if IPYNB
#r "nuget: Plotly.NET, {{fsdocs-package-version}}"
#r "nuget: Plotly.NET.Interactive, {{fsdocs-package-version}}"
#endif // IPYNB

(**
# {{TITLE}}

[![Binder](https://mybinder.org/badge_logo.svg)](https://mybinder.org/v2/gh/plotly/Plotly.NET/gh-pages?filepath={{FILENAME}}.ipynb)
*)
"""

    if (promptYesNo (sprintf "creating file %s with title %s OK?" path title)) then
        lines
        |> String.replace "{{FILENAME}}" filename
        |> String.replace "{{TITLE}}" title
        |> fun content -> File.WriteAllText (path,content)
    else
        failwith "aborted"
}

    

let buildDocs = BuildTask.create "BuildDocs" [build; copyBinaries] {
    printfn "building docs with stable version %s" stableVersionTag
    runDotNet 
        (sprintf "fsdocs build --eval --clean --property Configuration=Release --parameters fsdocs-package-version %s" stableVersionTag)
        "./"
}

let buildDocsPrerelease = BuildTask.create "BuildDocsPrerelease" [setPrereleaseTag; build; copyBinaries] {
    printfn "building docs with prerelease version %s" prereleaseTag
    runDotNet 
        (sprintf "fsdocs build --eval --clean --property Configuration=Release --parameters fsdocs-package-version %s" prereleaseTag)
        "./"
}

let watchDocs = BuildTask.create "WatchDocs" [build; copyBinaries] {
    printfn "watching docs with stable version %s" stableVersionTag
    runDotNet 
        (sprintf "fsdocs watch --eval --clean --property Configuration=Release --parameters fsdocs-package-version %s" stableVersionTag)
        "./"
}

let watchDocsPrerelease = BuildTask.create "WatchDocsPrerelease" [setPrereleaseTag; build; copyBinaries] {
    printfn "watching docs with prerelease version %s" prereleaseTag
    runDotNet 
        (sprintf "fsdocs watch --eval --clean --property Configuration=Release --parameters fsdocs-package-version %s" prereleaseTag)
        "./"
}

let releaseDocs =  BuildTask.create "ReleaseDocs" [buildDocs.IfNeeded; buildDocsPrerelease.IfNeeded] {
    let msg = sprintf "release docs for version %s?" (if isPrerelease then prereleaseTag else stableVersionTag)
    if promptYesNo msg then
        Shell.cleanDir "temp"
        Git.CommandHelper.runSimpleGitCommand "." (sprintf "clone %s temp/gh-pages --depth 1 -b gh-pages" projectRepo) |> ignore
        Shell.copyRecursive "output" "temp/gh-pages" true |> printfn "%A"
        Git.CommandHelper.runSimpleGitCommand "temp/gh-pages" "add ." |> printfn "%s"
        let cmd = sprintf """commit -a -m "Update generated documentation for version %s""" release.NugetVersion
        Git.CommandHelper.runSimpleGitCommand "temp/gh-pages" cmd |> printfn "%s"
        Git.Branches.push "temp/gh-pages"
    else failwith "aborted"
}

// to do: use this once we have actual tests
let runTestsWithCodeCov = BuildTask.create "RunTestsWithCodeCov" [clean; build; copyBinaries] {
    let standardParams = Fake.DotNet.MSBuild.CliArguments.Create ()
    Fake.DotNet.DotNet.test(fun testParams ->
        {
            testParams with
                MSBuildParams = {
                    standardParams with
                        Properties = [
                            "AltCover","true"
                            "AltCoverCobertura","../../codeCov.xml"
                            "AltCoverForce","true"
                        ]
                };
                Logger = Some "console;verbosity=detailed"
        }
    ) testProject
}

let createTag = BuildTask.create "CreateTag" [clean; build; copyBinaries; runTests; pack] {
    if promptYesNo (sprintf "tagging branch with %s OK?" stableVersionTag ) then
        Git.Branches.tag "" stableVersionTag
        Git.Branches.pushTag "" projectRepo stableVersionTag
    else
        failwith "aborted"
}

let createPrereleaseTag = BuildTask.create "CreatePrereleaseTag" [setPrereleaseTag; clean; build; copyBinaries; runTests; packPrerelease] {
    if promptYesNo (sprintf "tagging branch with %s OK?" prereleaseTag ) then 
        Git.Branches.tag "" prereleaseTag
        Git.Branches.pushTag "" projectRepo prereleaseTag
    else
        failwith "aborted"
}

let publishNuget = BuildTask.create "PublishNuget" [clean; build; copyBinaries; runTests; pack.IfNeeded; packPrerelease.IfNeeded] {
    let targets = (!! (sprintf "%s/*.*pkg" pkgDir ))
    for target in targets do printfn "%A" target
    let msg = sprintf "release package with version %s?" (if isPrerelease then prereleaseTag else stableVersionTag)
    if promptYesNo msg then
        let source = "https://api.nuget.org/v3/index.json"
        let apikey =  Environment.environVar "NUGET_KEY"
        for artifact in targets do
            let result = DotNet.exec id "nuget" (sprintf "push -s %s -k %s %s --skip-duplicate" source apikey artifact)
            if not result.OK then failwith "failed to push packages"
    else failwith "aborted"
}


let _release = BuildTask.createEmpty "Release" [clean; build; copyBinaries; runTests; pack; buildDocs; createTag; publishNuget; releaseDocs]

let _releasePreview = BuildTask.createEmpty "ReleasePreview" [setPrereleaseTag; clean; build; copyBinaries; runTests; packPrerelease; buildDocsPrerelease; createPrereleaseTag; publishNuget; releaseDocs]

let _all = BuildTask.createEmpty "All" [clean; build; copyBinaries; runTests (*runTestsWithCodeCov*); pack; buildDocs]

BuildTask.runOrDefault copyBinaries
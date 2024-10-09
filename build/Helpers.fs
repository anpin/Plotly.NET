module Helpers

open System.Diagnostics
open BlackFox.Fake
open Fake.Core
open Fake.DotNet

let initializeContext () =
    let execContext =
        Context.FakeExecutionContext.Create false "build.fsx" []

    Context.setExecutionContext (Context.RuntimeContext.Fake execContext)

/// Executes a dotnet command in the given working directory
let runDotNet cmd workingDir =
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""

    if result.ExitCode <> 0 then
        failwithf "'dotnet %s' failed in %s" cmd workingDir

let runOrDefault defaultTarget args =
    Trace.trace (sprintf "%A" args)

    try
        match args with
        | [| target |] -> Target.runOrDefault target
        | arr when args.Length > 1 -> Target.run 0 (Array.head arr) (Array.tail arr |> List.ofArray)
        | _ -> BuildTask.runOrDefault defaultTarget

        0
    with
    | e ->
        printfn "%A" e
        1

let isChromeInstalled() =
    try
        use _process = new Process()
        _process.StartInfo.FileName <- "chrome"
        _process.StartInfo.Arguments <- "--version"
        _process.StartInfo.UseShellExecute <- false
        _process.StartInfo.RedirectStandardOutput <- true
        _process.Start() |> ignore
        _process.WaitForExit()
        true
    with
    | _ -> false

let getSuffix() =
            match System.Environment.GetCommandLineArgs() |> Array.tryFind (_.StartsWith("--prerelease-suffix=")) with
            | Some arg -> arg.Split('=').[1]
            | None ->
                printfn "Please enter pre-release package suffix"
                System.Console.ReadLine()

let nonInteractive() =
            match System.Environment.GetCommandLineArgs() |> Array.tryFind (_.StartsWith("--non-interactive")) with
            | Some arg -> true
            | None ->
                false

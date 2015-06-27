﻿module azmonc

open Listen
open Nessos.UnionArgParser
open System
open System.Threading

type Arguments =
    | [<Mandatory>] Server of string // A running Azmon server's absolute HTTP URI
    with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Server _ -> "HTTP/S URI of Azmon server"

let parser = UnionArgParser.Create<Arguments>()
let usage = parser.Usage()

let registerExitOnCtrlC (canceller: CancellationTokenSource) session =
    Console.CancelKeyPress
    |> Observable.subscribe (fun _ ->
        // Like tears in rain... time to die.
        canceller.Cancel())
    |> ignore

[<EntryPoint>]
let main argv = 
    try
        if Array.isEmpty argv then
            printfn "%s" usage
            0
        else
            let args = parser.Parse argv

            let canceller = new CancellationTokenSource()

            let runUntilCancelled = async {
                // Connect to the server. Add reconnection logic?
                use! conn = Listen.connect (args.GetResult <@ Server @>)
                use events = Observable.subscribe (fun (e: string) -> printfn "%s" e) conn.Subject

                while true do
                    do! Async.Sleep(1000)
            }

            Async.RunSynchronously(runUntilCancelled, 10000, canceller.Token)
            0
    with
    | :? System.ArgumentException as e ->
        printfn "%s" usage
        printfn "-------"
        printfn "%A" (e.ToString())
        1
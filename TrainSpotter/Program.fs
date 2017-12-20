﻿// Learn more about F# at http://fsharp.org

module TrainSpotter

open System.Threading
open FSharp.Control.Reactive

let messageSink msg =
    printfn "Message: %A" msg

let errorSink err =
    printfn "Error: %A" err

let completed () =
    printfn "Observable terminated."

[<EntryPoint>]
let main argv =

    let conn = Receiver.connect "activemq:tcp://datafeeds.nationalrail.co.uk:61616" "d3user" "d3password"

    let pipeline = Receiver.openQueue "D3917834da-5cbc-42e3-8e28-ae97cfa31afd" conn
                   |> Observable.map PushPort.unpackPportMsg
                   |> Observable.choose id
                   |> Observable.map PushPort.asDarwinMessages
                   |> Observable.subscribeWithCallbacks messageSink errorSink completed

    conn.Start()

    use mre = new ManualResetEventSlim(false)
    let sub = System.Console.CancelKeyPress.Add(fun _ -> mre.Set())

    printfn "Waiting for interrupt to exit."
    mre.Wait()
  
    0 // return an integer exit code
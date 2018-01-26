// Learn more about F# at http://fsharp.org

module TrainSpotter

open System.Threading
open FSharp.Control.Reactive

let messageSink msg =
    ()

let errorSink err =
    printfn "Error: %A" err

let completed () =
    printfn "Observable terminated."

[<EntryPoint>]
let main argv =

    let user = "TODO"
    let pass = "TODO"
    let queueId = "TODO"

    let conn = Receiver.connect "activemq:tcp://datafeeds.nationalrail.co.uk:61616" user pass

    let pipeline = Receiver.openQueue queueId conn
                   |> Observable.map PushPort.unpackPportMsg
                   |> Observable.choose id
                   |> Observable.map PushPort.asDarwinMessages
                   |> Observable.subscribeWithCallbacks messageSink errorSink completed

    conn.Start()

    let cts = new CancellationTokenSource()
    use mre = new ManualResetEventSlim(false)
    let doCancel _ =
        do cts.Cancel()
           mre.Set()

    let sub = System.Console.CancelKeyPress.Add(doCancel)

    Web.startServer cts

    printfn "Waiting for interrupt to exit."
    mre.Wait()
  
    0 // return an integer exit code

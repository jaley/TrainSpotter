module Web

open System.Threading
open Suave

let startServer (cts: CancellationTokenSource) =
    let conf = { defaultConfig with cancellationToken = cts.Token }
    let listening, server = startWebServerAsync conf (Successful.OK "Hello World")
    Async.Start(server, cts.Token)


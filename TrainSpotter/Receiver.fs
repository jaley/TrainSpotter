module Receiver

open System
open System.Reactive.Subjects

open Apache.NMS

/// Open a new connection to the provided STOMP feed URL, using username and password.
let connect url user pass =
    let factory = NMSConnectionFactory(providerURI=url)
    factory.CreateConnection(user, pass)

/// Build a MessageListener
let messageListener (subj: Subject<_>) =
    let handleMsg msg = subj.OnNext(msg)
    MessageListener(handleMsg)

/// Opens a new session and subscribes to the named queue in given connection, returning an
/// observable that will be triggered for each incoming message
let openQueue queue (conn: IConnection) : IObservable<IMessage> =
    // TODO: These are currently not disposable - needs a custom IDisposable wrapper
    let session = conn.CreateSession()
    let queue = session.GetQueue(queue)
    let consumer = session.CreateConsumer(queue)
    let subj = new Subject<_>()

    session.CreateConsumer(queue).add_Listener(messageListener subj)
    subj :> IObservable<_>

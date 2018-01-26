module PushPort

open FSharp.Data

open System
open System.IO
open System.IO.Compression

open Apache.NMS

[<Literal>]
let SchemaDirectory = __SOURCE_DIRECTORY__ + "/Schemata"

type RttiPpt = XmlProvider<Schema = "rttiPPTSchema_v12.xsd", ResolutionFolder = SchemaDirectory>

type Time =
    | Eta of RttiPpt.Arr option
    | Etd of RttiPpt.Dep option with

    static member MakeDateTime (ssd: DateTime) timeStr =
        let time = TimeSpan.ParseExact (timeStr, "g", null)
        ssd.Add time

    static member FromXml (ssd: DateTime) elem =
        let times = match elem with
                    | Eta (Some a) -> [a.At; a.Et]
                    | Etd (Some d) -> [d.At; d.Et]
                    | _ -> []
        times
        |> List.choose id
        |> List.map (Time.MakeDateTime ssd)
        |> List.tryHead

type Location = {Station: string;
                 Platform: string option;
                 ETA: DateTime option;
                 ETD: DateTime option } with
    static member FromXml (ssd: DateTime) (location: RttiPpt.Location) =        
        {Station  = location.Tpl;
         Platform = location.Plat |> Option.map (fun p -> p.Value);
         ETA      = Time.FromXml ssd (Eta location.Arr);
         ETD      = Time.FromXml ssd (Etd location.Dep)}

type TrainStatus = {Rid: string; 
                    Ssd: DateTime; 
                    Tiplocs: List<Location>} with
    static member FromXml (ts: RttiPpt.Ts) =
        {Rid     = ts.Rid; 
         Ssd     = ts.Ssd; 
         Tiplocs = ts.Locations 
                   |> Array.map (Location.FromXml ts.Ssd) 
                   |> Array.toList}

type DarwinMessage =
    | TS of TrainStatus
   
/// Parse compressed XML data from Darwin binary payload.
let parseMessage msgBytes =
        use stream = new MemoryStream(buffer=msgBytes)
        use gzipStream = new GZipStream(stream, CompressionMode.Decompress)
        use reader = new StreamReader(gzipStream)
        (RttiPpt.Parse (reader.ReadToEnd())).Pport

/// Convert XML messages to a (sane) DarwinMessage type
let asDarwinMessages (ur: RttiPpt.UR) =
    ur.Ts |> Array.map (TrainStatus.FromXml >> TS)

/// Unpack a PushPort message received through STOMP, returning the Darwin UR message element
let unpackPportMsg (msg: IMessage) =
    match msg with
    | :? IBytesMessage as byte -> match (parseMessage byte.Content) with
                                  | Some pp -> pp.UR
                                  | None -> None
    | _ -> None


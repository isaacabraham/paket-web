#r @"packages/Zlib.Portable/lib/portable-net4+sl5+wp8+win8+wpa81+MonoTouch+MonoAndroid/Zlib.Portable.dll" 
#r @"System.Xml.Linq" 
#r @"packages/FSharp.Data/lib/net40/FSharp.Data.dll" 

open System.IO
open FSharp.Data

type NugetConfig = XmlProvider<"sample.config">

let mismatchedPackages (packageConfigs:string array) =
    packageConfigs
    |> Array.map NugetConfig.Load
    |> Array.collect(fun config -> config.Packages)
    |> Array.distinctBy(fun package -> package.Id, package.Version)
    |> Array.groupBy(fun package -> package.Id)
    |> Array.filter(snd >> Array.length >> (<>) 1)
    |> Array.map(fun (key, items) ->
        key, items
             |> Array.filter(fun p -> p.Version.IsSome)
             |> Array.map(fun p -> p.Version.Value))
    
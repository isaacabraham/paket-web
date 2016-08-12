#r @"packages\Suave\lib\net40\Suave.dll"
#r @"packages\Newtonsoft.Json\lib\net40\Newtonsoft.Json.dll"
#load "ParseLockFile.fsx"
#load "Review.fsx"

open System.IO
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful

type Problem =
    { Title : string
      Description : string }

type Package = { Name : string; Version : string } 

type DependencyChain =
    { Parent : Package
      Children : Package array }

type Report =
    { Summary : string
      Problems : Problem array
      DependencyChains : DependencyChain array }

type DependencyList =
    { Chains : string array }

let serialize = Newtonsoft.Json.JsonConvert.SerializeObject

let dependenciesFile = lazy File.ReadAllLines "output/paket.dependencies"
let lockFile = lazy File.ReadAllLines "output/paket.lock"
let configs = Directory.EnumerateFiles("sampleConfig", "packages.config", SearchOption.AllDirectories) |> Seq.toArray

let buildReport() =
    let toPackage (name, version) = { Name = name; Version = version }
    
    { Summary = ""
      Problems =
        configs
        |> Review.mismatchedPackages
        |> Array.map(fun (p, vs) ->
            { Title = "Found multiple versions of the same package."
              Description = sprintf "%s has multiple versions specified: %s" p (String.concat ", " vs) })
      DependencyChains =
        (dependenciesFile.Value, lockFile.Value)
        ||> ParseLockFile.buildGraph
        |> List.map(fun (parent, children) ->
             { Parent = parent |> toPackage
               Children = children |> List.map toPackage |> List.toArray })
        |> List.toArray }

let describe() =
    { Chains =
        (dependenciesFile.Value, lockFile.Value)
        ||> ParseLockFile.buildGraph
        |> List.map(fun (parent, children) ->
            let children =
                children
                |> List.map (fun (package, version) -> sprintf "%s: %s" package version)
                |> String.concat ", "
            match children with
            | "" -> sprintf "%s v%s (no dependencies)" (fst parent) (snd parent)
            | children -> sprintf "%s v%s depends on: %s" (fst parent) (snd parent) children)
        |> List.toArray }

let app =
    choose 
        [ GET >=> choose [
            (path "/describe") >=> OK (describe() |> serialize)
            (path "/report") >=> OK (buildReport() |> serialize) ]
        ]

startWebServer defaultConfig app
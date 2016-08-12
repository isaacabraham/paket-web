open System.IO
open System

let (|Parent|Child|Header|) (row:string) =
    let indent = row.ToCharArray() |> Array.takeWhile ((=) ' ') |> Array.length
    let row =
        lazy
            let items =
                row.Trim().Split(')').[0].Split('(')

            items.[0].Trim(), items.[1].Trim()

    match indent with
    | 4 -> Parent (row.Value)
    | 6 -> Child (row.Value)
    | _ -> Header

type Package = string * string

let getDependency (row:string) =
    if (row.StartsWith "source" || String.IsNullOrWhiteSpace row) then None
    else Some (row.Split(' ').[1].ToLower())

let buildGraph dependencyContents lockContents =
    let topLevelDependencies = dependencyContents |> Array.choose getDependency |> Set.ofArray
    let processed, final =
        lockContents
        |> Array.fold(fun (processed, (current:(Package * (Package list)) option)) row ->
            match row, current with
            | Parent (package, version), Some current ->
                let processed = current :: processed
                processed, Some((package, version), [])
            | Parent (package, version), None -> processed, Some((package, version), [])
            | Child (package, version), Some current ->
                let parent = fst current
                let otherChildren = snd current
                processed, Some(parent, (package, version) :: otherChildren)
            | _ -> processed, current) ([], None)

    ((final |> Option.toList) @ processed)
    |> List.filter(fun (parent, children) -> children <> [] || (topLevelDependencies.Contains ((fst parent).ToLower())))
    |> List.rev
    |> List.map(fun (parent, children) -> parent, children |> List.rev)

let dependencyContents = File.ReadAllLines "output/paket.dependencies"
let lockContents = File.ReadAllLines "output/paket.lock"

buildGraph dependencyContents lockContents
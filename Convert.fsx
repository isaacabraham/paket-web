#r @"packages\Paket\tools\paket.exe"

open System
open System.IO
open Paket

let path = System.IO.DirectoryInfo("C:\Users\Isaac\Source\Repos\paket-web\sampleConfig")
let configs = Directory.EnumerateFiles(path.FullName, "packages.config", SearchOption.AllDirectories)

let sampleProjectXml = """<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="14.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
</Project>"""

let prepareForPaket configs =
    let folderName = Guid.NewGuid().ToString()
    Directory.CreateDirectory folderName |> ignore
    
    configs
    |> Seq.indexed
    |> Seq.map(fun (index, config) -> Path.Combine(folderName, index.ToString()), sprintf "%d.csproj" index, config)
    |> Seq.iter(fun (path, project, config) ->
        Directory.CreateDirectory path |> ignore
        File.WriteAllText(Path.Combine(path, project), sampleProjectXml)
        File.Copy(config, (Path.Combine(path, Path.GetFileName config))))
    folderName

let generatePaketFiles nugetPath =
    let moveFile filename = 
        File.Move(Path.Combine(nugetPath, filename), Path.Combine("output", filename))

    // Generate paket outputs
    Dependencies.ConvertFromNuget(false, false, false, None, DirectoryInfo nugetPath)
    Dependencies.Locate(nugetPath).Simplify(false)
    
    // Copy paket outputs to output folder
    Directory.CreateDirectory "output" |> ignore
    Directory.Delete("output", true)
    Directory.CreateDirectory "output" |> ignore
    moveFile "paket.dependencies"
    moveFile "paket.lock"
    Directory.Delete(nugetPath, true)

let getNugetConfigs path =
    Directory.EnumerateFiles(path, "packages.config", SearchOption.AllDirectories)

let processFolder = getNugetConfigs >> prepareForPaket >> generatePaketFiles

processFolder "sampleConfig"
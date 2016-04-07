// include Fake lib
#r "packages/FAKE/tools/FakeLib.dll"
open Fake
open System
open System.IO

// Properties
let mutable buildMode = getBuildParamOrDefault "buildMode" "Debug"

let deployDir = "./deploy"
let testResultsDir = "./TestResults"
let solutionFile = "PaketContribRepro.sln"

let buildVerbosity source =
  match source with
  | "Detailed" ->   Some MSBuildVerbosity.Detailed
  | "Diagnostic" -> Some MSBuildVerbosity.Diagnostic
  | "Minimal" ->    Some MSBuildVerbosity.Minimal
  | "Normal" ->     Some MSBuildVerbosity.Normal
  | _ ->            Some MSBuildVerbosity.Quiet

let logCleanException dir message =
  printfn "Could not clean %s. Encountered exception: %s" dir message

let getCleanDirectories _ =
  (Seq.append (!! (@"*/bin")) [deployDir; testResultsDir])

let mutable clean = (fun _ ->
    //append the build bin directories to the deploy and test results directory
    Seq.iter (fun dir ->
        try
          CleanDir dir
        with
        | :? System.IO.IOException              as ex -> logCleanException dir ex.Message
        | :? System.UnauthorizedAccessException as ex -> logCleanException dir ex.Message
    ) (getCleanDirectories())
  )

MSBuildDefaults <- { MSBuildDefaults with Verbosity = (buildVerbosity (getBuildParamOrDefault "verbosity" String.Empty)) }

Target "ConfigureRelease" (fun _ ->
  buildMode <- getBuildParamOrDefault "buildMode" "Release"
  clean <- (fun _ ->
    CleanDirs (getCleanDirectories())
  )
  ()
)

Target "Clean" (fun _ ->
  clean()
)

Target "Build" (fun _ ->
  MSBuild "" "Build" [
    "Configuration", buildMode
    "ProductVersion", 1.0.0.0] [solutionFile]
    |> Log (buildMode + "-Output: ")
)

Target "Test" (fun _ ->
)

Target "Debug" (fun _ -> ()) //do nothing, just an entry point
Target "Release" (fun _ -> ()) //do nothing, just an entry point

// Dependencies
"ConfigureRelease" ==> "Release"

"Clean"
  ==> "SetVersions"
  ==> "Build"
  ==> "Test"
  ==> "CopyToDeploy"
  ==> "Finish"
  ==> "Release"

// start build
RunTargetOrDefault "Debug"

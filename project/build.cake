string target = Argument<string>("target", "ExecuteBuild");
string config = Argument<string>("config", "Release");
bool VSBuilt = Argument<bool>("vsbuilt", false);

// Cake API Reference: https://cakebuild.net/dsl/
// setup variables
var buildDir = "./Build";
var csprojPaths = GetFiles("./**/Sandstorm.*(Proxy).csproj");
var delPaths = GetDirectories("./**/*(obj|bin)");
var licenseFile = "../LICENSE";
var publishRuntime = "win10-x64";
var launcherDebugFolder = "./Sandstorm.Proxy/bin/Debug/net7.0/win10-x64";

// Clean build directory and remove obj / bin folder from projects
Task("Clean")
    .WithCriteria(!VSBuilt)
    .Does(() =>
    {
        CleanDirectory(buildDir);
    })
    .DoesForEach(delPaths, (directoryPath) =>
    {
        DeleteDirectory(directoryPath, new DeleteDirectorySettings
        {
            Recursive = true,
            Force = true
        });
    });

// Restore, build, and publish selected csproj files
Task("Publish")
    .IsDependentOn("Clean")
    .DoesForEach(csprojPaths, (csprojFile) => 
    {
        DotNetPublish(csprojFile.FullPath, new DotNetPublishSettings 
        {
            NoLogo = true,
            Configuration = config,
            Runtime = publishRuntime,
            PublishSingleFile = true,
            SelfContained = false,
            OutputDirectory = buildDir
        });
    });

// Copy license to build directory
Task("CopyBuildData")
    .IsDependentOn("Publish")
    .Does(() => 
    {
        CopyFile(licenseFile, $"{buildDir}/LICENSE.txt");
    });

// Remove pdb files from build if running in release configuration
Task("RemovePDBs")
    .WithCriteria(config == "Release")
    .IsDependentOn("CopyBuildData")
    .Does(() => 
    {
        DeleteFiles($"{buildDir}/*.pdb");
    });

// Runs all build tasks based on dependency and configuration
Task("ExecuteBuild")
    .IsDependentOn("CopyBuildData")
    .IsDependentOn("RemovePDBs");

// Runs target task
RunTarget(target);
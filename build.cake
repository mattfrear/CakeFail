#addin "Cake.Incubator"
#addin "Cake.AutoRest&prerelease"
#tool "nuget:?package=xunit.runner.console"

// Target - The task you want to start. Runs the Default task if not specified.
var target = Argument("Target", "Default");

// Configuration - The build configuration (Debug/Release) to use.
// 1. If command line parameter parameter passed, use that.
// 2. Otherwise if an Environment variable exists, use that.
var configuration = 
    HasArgument("Configuration") ? Argument<string>("Configuration") :
    EnvironmentVariable("Configuration") != null ? EnvironmentVariable("Configuration") : "Release";

// Version prefix - will be output from TeamCity's build number
var versionPrefixKey = "Version.Prefix";
var versionPrefix = HasEnvironmentVariable(versionPrefixKey) ? EnvironmentVariable<string>(versionPrefixKey) : "1.0.0";

// Version suffix - will be the name of the branch if on the build server, or local when run locally
var versionSuffixKey = "Version.Suffix";
var versionSuffix = HasEnvironmentVariable(versionSuffixKey) ? EnvironmentVariable<string>(versionSuffixKey) : "local";

// A directory path to an Artifacts directory.
var artifactsDirectory = Directory("./artifacts");
var nugetDirectory = artifactsDirectory + Directory("NuGet");

// the main solution file
var solutionFile = "CakeFail.sln";

// Deletes the contents of the Artifacts folder if it should contain anything from a previous build.
Task("Clean")
    .Does(() =>
    {
        CleanDirectory(artifactsDirectory);
    });
 
// NuGet restore packages for .NET Framework projects (and .NET Core projects)
Task("NuGet-Restore")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        NuGetRestore(solutionFile);
    });

// NuGet restore packages for .NET Core projects only
Task("DotNetCoreRestore")
    .IsDependentOn("NuGet-Restore")
    .Does(() =>
    {
		var settings = new DotNetCoreRestoreSettings
		{
			ArgumentCustomization = args => args.Append("/p:Version=" + versionPrefix + "-" + versionSuffix)
		};
        DotNetCoreRestore(solutionFile, settings);
    });

// Build our solution
 Task("Build")
    .IsDependentOn("DotNetCoreRestore")
    .Does(() =>
    {
        DotNetCoreBuild(
            solutionFile,
            new DotNetCoreBuildSettings()
            {
				ArgumentCustomization = args => args.Append("/p:Version=" + versionPrefix + "-" + versionSuffix),
                Configuration = configuration
            });
    });
 
// The default task to run if none is explicitly specified. In this case, we want
// to run everything starting from Clean, all the way up to Pack.
Task("Default")
    .IsDependentOn("Build");
 
// Executes the task specified in the target argument.
RunTarget(target);

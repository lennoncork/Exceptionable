#nullable enable
using System.Text.RegularExpressions;

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

Task("Clean")
    .Does(() =>
    {
        CleanDirectory("./.output/");
        CleanDirectory("./.packages/");
    });

Task("Build")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        DotNetBuild("./src/Exceptionable.slnx", new DotNetBuildSettings
        {
            Configuration = configuration,
        });
    });

Task("Format")
    .Does(() =>
    {
        DotNetFormat("./src/Exceptionable.slnx", new DotNetFormatSettings
        {
            VerifyNoChanges = true,
        });
    });

Task("Analyzers")
    .Does(() =>
    {
        DotNetFormat("./src/Exceptionable.slnx", new DotNetFormatSettings
        {
            ArgumentCustomization = args => args.Append("analyzers").Append("--severity").Append("info"),
            VerifyNoChanges = true,
        });
    });

Task("Test")
    .IsDependentOn("Build")
    .DoesForEach(() => GetFiles("./src/**/*Tests.csproj"), testProject =>
    {
        DotNetTest(testProject.FullPath, new DotNetTestSettings
        {
            Configuration = configuration,
            NoBuild = true,
        });
    });

Task("Package")
    .IsDependentOn("Build")
    .Does(() =>
    {
        DotNetPack("./src/Exceptionable.slnx", new DotNetPackSettings
        {
            Configuration = configuration,
            OutputDirectory = "./.packages/",
            NoBuild = true,
        });
    });

Task("Consumer")
    .IsDependentOn("Package")
    .Does(() =>
    {
        var consumerPackages = MakeAbsolute(Directory("./.output/consumer-packages")).FullPath;
        var localPackages = MakeAbsolute(Directory("./.packages")).FullPath;
        // A feed URL in /p:RestoreSources is parsed as an MSBuild comment on Windows
        // because response files treat // as a comment. Keep the URL in a config file.
        var restoreConfig = MakeAbsolute(File("./.output/consumer.nuget.config")).FullPath;
        EnsureDirectoryExists("./.output/");
        System.IO.File.WriteAllText(
            restoreConfig,
            $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="local" value="{xmlAttribute(localPackages)}" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
            </configuration>
            """);
        foreach (var framework in new[] { "net10.0", "net481" })
        {
            var buildLog = MakeAbsolute(File($"./.output/consumer-build-{framework}.log")).FullPath;
            DotNetBuild("./samples/Exceptionable.Consumer/Exceptionable.Consumer.csproj", new DotNetBuildSettings
            {
                Configuration = configuration,
                Framework = framework,
                ArgumentCustomization = args => args
                    .Append($"/p:RestoreConfigFile=\"{restoreConfig}\"")
                    .Append($"/p:RestorePackagesPath=\"{consumerPackages}\"")
                    .Append("--verbosity")
                    .Append("normal")
                    .AppendQuoted($"/flp:logfile={buildLog};verbosity=normal"),
            });

            var log = System.IO.File.ReadAllText(buildLog);
            var exc002Count = CountDiagnostics(log, "EXC002");
            var exc003Count = CountDiagnostics(log, "EXC003");
            if (exc002Count != 1 || exc003Count != 1)
            {
                throw new CakeException(
                    $"Packaged {framework} consumer expected one EXC002 and one EXC003 diagnostic, but found {exc002Count} and {exc003Count}.");
            }
        }
    });

Task("Validate")
    .IsDependentOn("Build")
    .IsDependentOn("Format")
    .IsDependentOn("Analyzers")
    .IsDependentOn("Test")
    .IsDependentOn("Consumer");


Task("Push")
    .DoesForEach(() => GetFiles("./.packages/*.nupkg"), package =>
    {
        var apiKey = EnvironmentVariable("NUGET_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new CakeException("Configure the NUGET_API_KEY secret in the nuget.org GitHub environment.");
        }

        Information("Publishing {0}...", package.FullPath);
        var arguments = new ProcessArgumentBuilder()
            .Append("nuget")
            .Append("push")
            .AppendQuoted(package.FullPath)
            .Append("--api-key")
            .AppendQuoted(apiKey)
            .Append("--source")
            .AppendQuoted("https://api.nuget.org/v3/index.json")
            .Append("--skip-duplicate");
        var exitCode = StartProcess("dotnet", new ProcessSettings
        {
            Arguments = arguments,
        });
        if (exitCode != 0)
        {
            throw new CakeException($"Publishing '{package.GetFilename()}' failed with exit code {exitCode}.");
        }
    });

Task("Default")
    .IsDependentOn("Validate");

RunTarget(target);

static string xmlAttribute(string value) =>
    System.Security.SecurityElement.Escape(value) ?? value;

static int CountDiagnostics(string log, string id)
{
    return Regex.Matches(log, $@"\(\d+,\d+\): warning {id}:")
        .Cast<Match>()
        .Select(match => match.Value)
        .Distinct()
        .Count();
}

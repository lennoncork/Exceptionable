#nullable enable

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
        DotNetBuild("./samples/Exceptionable.Consumer/Exceptionable.Consumer.csproj", new DotNetBuildSettings
        {
            Configuration = configuration,
            ArgumentCustomization = args => args
                .Append("--source")
                .AppendQuoted(MakeAbsolute(Directory("./.packages")).FullPath),
        });
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

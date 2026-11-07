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
            ArgumentCustomization = args => args.Append("-p:PackageVersion=1.0.0"),
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

Task("PackageRelease")
    .IsDependentOn("Validate")
    .Does(() =>
    {
        var version = GetReleaseVersion();
        CleanDirectory("./.packages/");
        DotNetPack("./src/Exceptionable.slnx", new DotNetPackSettings
        {
            Configuration = "Release",
            OutputDirectory = "./.packages/",
            NoBuild = true,
        });

        ValidateReleasePackages(version);
    });

Task("PushRelease")
    .Does(() =>
    {
        var version = GetReleaseVersion();
        var packages = ValidateReleasePackages(version);
        var apiKey = EnvironmentVariable("NUGET_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new CakeException("Configure the NUGET_API_KEY secret in the nuget.org GitHub environment.");
        }

        foreach (var package in packages)
        {
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
        }
    });

Task("Default")
    .IsDependentOn("Validate");

RunTarget(target);

string GetReleaseVersion()
{
    var releaseTag = EnvironmentVariable("RELEASE_TAG") ?? string.Empty;
    var match = System.Text.RegularExpressions.Regex.Match(
        releaseTag,
        "^v(?<version>\\d+\\.\\d+\\.\\d+)$");
    if (!match.Success)
    {
        throw new CakeException($"Release tag '{releaseTag}' must use the stable vMAJOR.MINOR.PATCH format.");
    }

    return match.Groups["version"].Value;
}

FilePath[] ValidateReleasePackages(string version)
{
    var expectedPackages = new[]
    {
        $"Exceptionable.{version}.nupkg",
        $"Exceptionable.Analyzers.{version}.nupkg",
    };
    var packages = GetFiles("./.packages/*.nupkg").ToArray();
    var actualPackages = packages.Select(package => package.GetFilename().ToString()).ToArray();

    foreach (var expectedPackage in expectedPackages)
    {
        if (!actualPackages.Contains(expectedPackage, StringComparer.Ordinal))
        {
            throw new CakeException($"Expected package '{expectedPackage}' was not produced for release v{version}.");
        }
    }

    var unexpectedPackages = actualPackages
        .Where(package => !expectedPackages.Contains(package, StringComparer.Ordinal))
        .ToArray();
    if (unexpectedPackages.Length > 0)
    {
        throw new CakeException($"Unexpected packages found: {string.Join(", ", unexpectedPackages)}.");
    }

    return packages;
}

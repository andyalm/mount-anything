param(
    [Parameter(Mandatory=$true)]
    [Alias("Version")]
    [string]
    $PackageVersion
)

$ErrorActionPreference='Stop'

dotnet build -c Release "/p:PackageReleaseTag=v$PackageVersion"
Copy-Item ./src/MountAnything/bin/Release/MountAnything.$PackageVersion.nupkg ~/.nuget/local-feed/
Copy-Item ./src/MountAnything.Hosting.Build/bin/Release/MountAnything.Hosting.Build.$PackageVersion.nupkg ~/.nuget/local-feed/
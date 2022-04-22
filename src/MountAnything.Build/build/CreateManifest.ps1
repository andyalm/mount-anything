param(
    [Parameter(Mandatory=$true)]
    $PowershellProviderName,
    
    [Parameter(Mandatory=$true)]
    $OutputDirectory,

    [Parameter(Mandatory=$true)]
    $PackageVersion
)

New-ModuleManifest -Path $(Join-Path $OutputDirectory "$PowershellProviderName.psd1") `
    -RootModule "$PowershellProviderName.Host.dll" `
    -ModuleVersion "$PackageVersion" `
    -Author 'Andy Alm' `
    -Copyright '(c) 2021 Andy Alm. All rights reserved.' `
    -Description 'An experimental powershell provider that allows you to browse various aws services as a filesystem' `
    -PowerShellVersion '7.2' `
    -FormatsToProcess @() `
    -RequiredModules @() `
    -FunctionsToExport @()`
    -VariablesToExport @() `
    -CmdletsToExport @() `
    -AliasesToExport @()`
    -ReleaseNotes $($env:GithubReleaseNotes ?? 'Unavailable')
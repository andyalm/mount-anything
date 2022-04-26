#!/usr/bin/env pwsh -NoExit -Interactive -NoLogo

param(
    [switch]
    $Debug
)

$ErrorActionPreference='Stop'
dotnet build
if($Debug)
{
    $DebugPreference='Continue'
}
Import-Module ./bin/Debug/net6.0/Module/MountPowershell.psd1
cd pwsh:
<#
.SYNOPSIS
  Builds the Windows player (Builds/Windows/Incognito.exe) in Unity batch mode (tools/unity.ps1
  does the work; the Editor must be closed). Mirrors Unity.Chain's and Unity.GridGame's own
  tools/build.ps1.
.EXAMPLE
  powershell -File tools/build.ps1
#>
$ErrorActionPreference = 'Stop'

Write-Host '  Building Windows player...' -ForegroundColor Cyan
& "$PSScriptRoot/unity.ps1" -Method Incognito.EditorTools.PlayerBuilder.BuildWindows -Log build-player
exit $LASTEXITCODE

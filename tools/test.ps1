<#
.SYNOPSIS
  Headless verification gate for Unity.Rea.Incognito: the engine-free Core rules (dotnet test, via
  run-core-tests.ps1), the Unity EditMode suite, and the Unity PlayMode suite, each run in Unity
  batch mode (tools/unity.ps1; the Editor must be closed). Mirrors Unity.Chain's tools/test.ps1.
.PARAMETER SkipCore
  Skip Tests/Core.Tests.
.PARAMETER SkipEditMode
  Skip the Unity EditMode suite (Assets/Rea.Incognito/Tests/EditMode).
.PARAMETER SkipPlayMode
  Skip the Unity PlayMode suite (Assets/Rea.Incognito/Tests/PlayMode).
.EXAMPLE
  powershell -File tools/test.ps1
  powershell -File tools/test.ps1 -SkipEditMode -SkipPlayMode   # Core.Tests only
#>
param([switch]$SkipCore, [switch]$SkipEditMode, [switch]$SkipPlayMode)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

# Only the game's own test assemblies: Netcode for GameObjects is under "testables" (the online test kit
# needs its helpers, U.INC-T43), so an unlimited run would also run NGO's own suite.
$editAssemblies = 'Rea.Incognito.Tests.EditMode;Rea.Incognito.Tests.Online.EditMode'
$playAssemblies = 'Rea.Incognito.Tests.PlayMode;Rea.Incognito.Tests.Online'

# Unity's -runTests exits 0 even when it matched zero tests (a wrong -testPlatform, an excluded
# assembly, or a renamed test all look like success otherwise). Confirm the results file actually
# names tests, not just that Unity exited 0. A stale file from an earlier run is removed first so a
# crashed run can't be read as this one's results.
function Confirm-TestResults([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Path was not written; the Unity test run produced no results."
    }
    [xml]$results = Get-Content -LiteralPath $Path -Raw
    $run = $results.'test-run'
    if (-not $run -or [int]$run.total -le 0) {
        throw "$Path reports zero tests; -runTests matched nothing."
    }
    Write-Host "    $Path`: $($run.passed)/$($run.total) passed" -ForegroundColor Gray
}

if (-not $SkipCore) {
    Write-Host '  Core.Tests (dotnet test)...' -ForegroundColor Cyan
    & (Join-Path $root 'run-core-tests.ps1')
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if (-not $SkipEditMode) {
    Write-Host '  EditMode tests (Unity batch mode)...' -ForegroundColor Cyan
    $editResults = Join-Path $root 'TestResults-EditMode.xml'
    Remove-Item -LiteralPath $editResults -ErrorAction SilentlyContinue
    & "$PSScriptRoot/unity.ps1" -Log test-editmode -TestPlatform EditMode -TestResults $editResults -AssemblyNames $editAssemblies
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Confirm-TestResults -Path $editResults
}

if (-not $SkipPlayMode) {
    Write-Host '  PlayMode tests (Unity batch mode)...' -ForegroundColor Cyan
    $playResults = Join-Path $root 'TestResults-PlayMode.xml'
    Remove-Item -LiteralPath $playResults -ErrorAction SilentlyContinue
    & "$PSScriptRoot/unity.ps1" -Log test-playmode -TestPlatform PlayMode -TestResults $playResults -AssemblyNames $playAssemblies
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Confirm-TestResults -Path $playResults
}

exit 0

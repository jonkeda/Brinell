#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs the MAUI UI and UAT suites on the MauiUnit head: the apps in the test process, no device.

.DESCRIPTION
    The device suite (testsnew/Bruinet.Maui.UITests) and the Todo sample's UAT and UI tests, each
    with BRUINET_HEAD=mauiunit (AD-032). Tests that need a device skip with their reason. Runs on
    Windows, where the projects also carry the FlaUI head, and on a Mac or Linux, where they build
    for plain net10.0 (BruinetWindowsHead). CI runs it on both; locally it is one command.

    Every project runs even when one fails; the script fails at the end, naming them.

.PARAMETER Results
    Where to write a .trx per project. Default: TestResults/maui-in-memory.

.PARAMETER NoBuild
    Use the existing build (dotnet test --no-build), as CI does after building the solution.

.EXAMPLE
    ./tools/Scripts/test-maui-in-memory.ps1
#>
param(
    [string] $Results = 'TestResults/maui-in-memory',
    [switch] $NoBuild
)

$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '../..')
$env:BRUINET_HEAD = 'mauiunit'

$projects = @(
    'testsnew/Bruinet.Maui.UITests/Bruinet.Maui.UITests.csproj',
    'samples/Todo/tests/Bruinet.Samples.Todo.Uat/Bruinet.Samples.Todo.Uat.csproj',
    'samples/Todo/tests/Bruinet.Samples.Todo.UITests/Bruinet.Samples.Todo.UITests.csproj'
)

$failed = @()
foreach ($project in $projects) {
    $name = [System.IO.Path]::GetFileNameWithoutExtension($project)
    $arguments = @('test', (Join-Path $root $project), '-v:minimal',
        '--logger', "trx;LogFileName=$name.mauiunit.trx", '--results-directory', $Results)
    if ($NoBuild) { $arguments += '--no-build' }

    dotnet @arguments
    if ($LASTEXITCODE -ne 0) { $failed += $name }
}

if ($failed) { Write-Error "Failed on MauiUnit: $($failed -join ', ')"; exit 1 }

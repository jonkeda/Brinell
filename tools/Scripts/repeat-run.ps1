#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs a test project several times and reports which tests fail, how often, and why.

.DESCRIPTION
    Measures flakiness instead of guessing at it. Runs the project N times, one run after the
    other, and writes repeat-summary.md next to the runs' .trx files:

      - every test that failed at least once, with how many runs it failed in;
      - "flaky" (failed in some runs) or "always" (failed in every run);
      - a *suggested* class from the failure message: framework-race, environment, driver-gap,
        or app-bug-or-test. The suggestion is a heuristic to sort the list; the class is decided
        by reading the failure.

    Written for the stale-readiness plan (.my/stale-readiness/plan.md, step 2 baseline and step 8
    comparison). UI tests drive the desktop, so the script refuses to start while another test
    host is running: two runs at once fight over the desktop and produce failures that are not real.

.PARAMETER Project
    The test project (folder or .csproj) to run.

.PARAMETER Runs
    How many times to run it. Default: 5.

.PARAMETER Filter
    Optional dotnet test filter, for example "FullyQualifiedName~Todo".

.PARAMETER OutDir
    Where to put the .trx files and the summary. Default: TestResults/repeat-<timestamp>.

.PARAMETER Build
    Build once before the first run. By default the runs use the existing build (--no-build).

.EXAMPLE
    .\tools\Scripts\repeat-run.ps1 -Project samples/Todo/tests/Brinell.Samples.Todo.UITests -Runs 5
#>
param(
    [Parameter(Mandatory = $true)][string]$Project,
    [int]$Runs = 5,
    [string]$Filter,
    [string]$OutDir,
    [switch]$Build
)

$ErrorActionPreference = 'Stop'

# No MSBuild nodes that outlive a run: a reused node inherits the output handle, and
# `dotnet test | Out-Null` then waits for it to close, so the script stalls after the first run
# (seen 2026-09-19, after a build in the same PowerShell command).
$env:MSBUILDDISABLENODEREUSE = '1'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'

if (Get-Process -Name testhost -ErrorAction SilentlyContinue) {
    throw "A test host is already running. Wait for it to finish (or stop it) first: two UI runs at once produce failures that are not real."
}

# A sample app left over from an earlier run breaks the next run's attach (FlaUI GetMainWindow
# fails with "Unexpected HRESULT"): every test of that fixture fails, and none of it is real.
$orphans = Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -like 'Brinell.Samples.*' }
if ($orphans) {
    throw "Sample app(s) still running from an earlier run: $(($orphans | ForEach-Object { "$($_.ProcessName) ($($_.Id))" }) -join ', '). Stop them first."
}

if (-not $OutDir) {
    $OutDir = Join-Path 'TestResults' ("repeat-" + (Get-Date -Format 'yyyyMMdd-HHmmss'))
}
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

if ($Build) {
    dotnet build $Project -nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "Build failed." }
}

$started = Get-Date
for ($run = 1; $run -le $Runs; $run++) {
    Write-Host "Run $run of $Runs..."
    $arguments = @('test', $Project, '--no-build', '-nologo',
        '--logger', "trx;LogFileName=run-$run.trx", '--results-directory', $OutDir)
    if ($Filter) { $arguments += @('--filter', $Filter) }
    & dotnet @arguments | Out-Null
}
$elapsed = (Get-Date) - $started

function Get-SuggestedClass([string]$message) {
    switch -Regex ($message) {
        'not visible within|stale|ElementNotAvailable|0x80040201|replaced|StaleElement' { return 'framework-race' }
        'session|Appium|UiAutomator2|foreground|topmost|occlu|emulator|connection refused|another test' { return 'environment' }
        'NotSupportedException|PlatformNotSupported|has no route|not implemented' { return 'driver-gap' }
        'Assert|Expected|AssertionException' { return 'app-bug-or-test' }
        default { return 'unclassified' }
    }
}

$failures = @{}
$testsSeen = @{}
foreach ($trx in Get-ChildItem -Path $OutDir -Filter 'run-*.trx') {
    [xml]$doc = Get-Content -Raw -Path $trx.FullName
    $ns = New-Object System.Xml.XmlNamespaceManager($doc.NameTable)
    $ns.AddNamespace('t', $doc.DocumentElement.NamespaceURI)
    foreach ($result in $doc.SelectNodes('//t:UnitTestResult', $ns)) {
        $name = $result.testName
        $testsSeen[$name] = $true
        if ($result.outcome -ne 'Failed') { continue }
        $messageNode = $result.SelectSingleNode('t:Output/t:ErrorInfo/t:Message', $ns)
        $message = if ($messageNode) { ($messageNode.InnerText -split "`n")[0].Trim() } else { '' }
        if ($message.Length -gt 200) { $message = $message.Substring(0, 200) + '...' }
        if (-not $failures.ContainsKey($name)) { $failures[$name] = @{ Count = 0; Messages = @{} } }
        $failures[$name].Count++
        $failures[$name].Messages[$message] = $true
    }
}

$lines = @(
    "# Repeat run: $Project",
    '',
    "$Runs runs, $($testsSeen.Count) tests, $([int]$elapsed.TotalMinutes) min. Filter: $(if ($Filter) { $Filter } else { '(none)' }).",
    '',
    'The class column is a suggestion from the failure message, to sort the list. Decide the class by reading the failure.',
    '',
    '| Test | Failed | Kind | Suggested class | First message |',
    '|---|---:|---|---|---|'
)
foreach ($name in ($failures.Keys | Sort-Object { -$failures[$_].Count }, { $_ })) {
    $entry = $failures[$name]
    $kind = if ($entry.Count -ge $Runs) { 'always' } else { 'flaky' }
    $first = @($entry.Messages.Keys)[0]
    $class = Get-SuggestedClass $first
    $cell = $first -replace '\|', '\|'
    $lines += "| ``$name`` | $($entry.Count)/$Runs | $kind | $class | $cell |"
}
if ($failures.Count -eq 0) { $lines += '| (no failures) | | | | |' }

$summary = Join-Path $OutDir 'repeat-summary.md'
$lines | Set-Content -Path $summary -Encoding utf8
Write-Host "Summary: $summary"

#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Reports where a run's time went, from the timings BRINELL_TIMINGS wrote.

.DESCRIPTION
    Reads timings-*.csv (one row per control call and per span, .my/test-selection/speed-plan.md T1) and writes
    timings.md beside them:

      - the host's start, span by span;
      - time by kind (test, bind, lifecycle, scenario, step, call, navigate, arrange, report), summed, with its count;
      - per test: the whole test, and what it holds: binding, lifecycle, steps, calls, page loads, arrangement, and what
        no span covers (outside);
      - the slowest spans of each kind, and the slowest steps.

    Spans nest (a step holds its calls), so the kinds' sums overlap; "outside" is the test's time less its binding,
    scenario and report spans, the part nothing names yet.

.PARAMETER Folder
    The folder BRINELL_TIMINGS named.

.PARAMETER Top
    How many of the slowest to list per kind. Default 10.

.EXAMPLE
    $env:BRINELL_TIMINGS = "$PWD\TestResults\timings"; dotnet test tests\Tomorrow.Studio.UITests --filter ...
    pwsh tools/Scripts/timings.ps1 -Folder TestResults\timings
#>
param(
    [Parameter(Mandatory)] [string] $Folder,
    [int] $Top = 10
)

$ErrorActionPreference = 'Stop'
$rows = Get-ChildItem -Path $Folder -Filter 'timings-*.csv' | ForEach-Object { Import-Csv -Path $_.FullName -Delimiter ';' }
if (-not $rows) { throw "No timings-*.csv in $Folder." }
foreach ($row in $rows) { $row.DurationMs = [long]$row.DurationMs; $row.StartMs = [long]$row.StartMs }

function Seconds([long] $ms) { '{0:N1}' -f ($ms / 1000.0) }
function Sum($items) { ($items | Measure-Object -Property DurationMs -Sum).Sum ?? 0 }

$out = [System.Collections.Generic.List[string]]::new()
$out.Add("# Timings")
$out.Add("")
$out.Add("Runs: $(($rows.Run | Sort-Object -Unique) -join ', '); heads: $(($rows.Head | Sort-Object -Unique) -join ', '); rows: $($rows.Count).")

$out.Add("")
$out.Add("## The host's start")
$out.Add("")
$out.Add("| Span | s |")
$out.Add("| --- | --- |")
foreach ($span in $rows | Where-Object Kind -in 'host', 'fixture' | Sort-Object StartMs) {
    $out.Add("| $($span.Kind) $($span.Name) | $(Seconds $span.DurationMs) |")
}

$out.Add("")
$out.Add("## By kind")
$out.Add("")
$out.Add("| Kind | Count | Summed s |")
$out.Add("| --- | --- | --- |")
foreach ($group in $rows | Group-Object Kind | Sort-Object { -(Sum $_.Group) }) {
    $out.Add("| $($group.Name) | $($group.Count) | $(Seconds (Sum $group.Group)) |")
}

$out.Add("")
$out.Add("## Per test")
$out.Add("")
$out.Add("| Test | Whole s | Bind s | Lifecycle s | Steps s | Calls s | Page loads s | Arrange s | Outside s |")
$out.Add("| --- | --- | --- | --- | --- | --- | --- | --- | --- |")
$tests = $rows | Where-Object { $_.Test } | Group-Object Test
foreach ($test in $tests | Sort-Object { -(Sum ($_.Group | Where-Object Kind -eq 'test')) }) {
    $of = { param($kind) Sum ($test.Group | Where-Object Kind -eq $kind) }
    $whole = & $of 'test'
    $named = (& $of 'bind') + (& $of 'scenario') + (& $of 'report')
    $outside = if ((& $of 'scenario') -gt 0) { $whole - $named } else { $whole - (& $of 'step') - (& $of 'call') }
    $out.Add("| $($test.Name) | $(Seconds $whole) | $(Seconds (& $of 'bind')) | $(Seconds (& $of 'lifecycle')) | $(Seconds (& $of 'step')) | $(Seconds (& $of 'call')) | $(Seconds (& $of 'navigate')) | $(Seconds (& $of 'arrange')) | $(Seconds $outside) |")
}

foreach ($kind in 'bind', 'lifecycle', 'navigate', 'arrange', 'step', 'call') {
    $slow = $rows | Where-Object Kind -eq $kind | Sort-Object DurationMs -Descending | Select-Object -First $Top
    if (-not $slow) { continue }
    $out.Add("")
    $out.Add("## The slowest $kind")
    $out.Add("")
    $out.Add("| ms | Name | Page | Control | Test |")
    $out.Add("| --- | --- | --- | --- | --- |")
    foreach ($row in $slow) {
        $out.Add("| $($row.DurationMs) | $($row.Name) | $($row.Page) | $($row.Control) | $($row.Test) |")
    }
}

$report = Join-Path $Folder 'timings.md'
$out | Set-Content -Path $report -Encoding utf8
Write-Host "Wrote $report"

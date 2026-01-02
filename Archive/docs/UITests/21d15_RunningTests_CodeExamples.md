# 15. Running Tests - Code Examples

**Parent:** [Running Tests](21d15_RunningTests.md)

---

## 15.1 Complete Run Script (PowerShell)

```powershell
# run-uitests.ps1
# Comprehensive UI test runner script

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet("All", "Smoke", "Regression", "E2E", "Mocked")]
    [string]$Category = "All",
    
    [Parameter()]
    [ValidateSet("All", "Windows", "WindowsMaui", "Android", "iOS", "Web")]
    [string]$Platform = "Windows",
    
    [Parameter()]
    [string]$Feature,
    
    [Parameter()]
    [int]$Priority = 0,
    
    [Parameter()]
    [string]$TestProject = "src/Oravey.Tools.Wpf.UITests",
    
    [Parameter()]
    [switch]$Debug,
    
    [Parameter()]
    [switch]$Coverage,
    
    [Parameter()]
    [switch]$GenerateReport,
    
    [Parameter()]
    [string]$OutputDir = "TestResults",
    
    [Parameter()]
    [ValidateSet("None", "BrowserStack", "SauceLabs")]
    [string]$CloudProvider = "None"
)

# Set error action
$ErrorActionPreference = "Stop"

# Create output directory
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# Build filter
$filters = @()

switch ($Category) {
    "Smoke"      { $filters += "Category=Smoke" }
    "Regression" { $filters += "Category=Regression" }
    "E2E"        { $filters += "Category=E2E" }
    "Mocked"     { $filters += "Category=MockedAPITest" }
    "All"        { $filters += "Category=UITest" }
}

if ($Platform -ne "All") {
    $filters += "Platform=$Platform"
}

if ($Feature) {
    $filters += "Feature=$Feature"
}

if ($Priority -gt 0) {
    # Include priorities up to specified level
    $priorityFilter = (1..$Priority | ForEach-Object { "Priority=$_" }) -join "|"
    $filters += "($priorityFilter)"
}

$filterString = $filters -join "&"

# Set environment variables
$env:PLATFORM = $Platform
$env:CLOUD_PROVIDER = $CloudProvider
$env:LOG_OUTPUT_PATH = $OutputDir

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "UI Test Runner" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Category: $Category"
Write-Host "Platform: $Platform"
Write-Host "Filter: $filterString"
Write-Host "Cloud: $CloudProvider"
Write-Host "Output: $OutputDir"
Write-Host "============================================" -ForegroundColor Cyan

# Build command arguments
$arguments = @(
    "test"
    $TestProject
    "--filter", "`"$filterString`""
    "--logger", "trx;LogFileName=$OutputDir/results.trx"
    "--logger", "console;verbosity=normal"
)

if ($Debug) {
    $arguments += "--configuration", "Debug"
    $arguments += "--verbosity", "detailed"
}
else {
    $arguments += "--configuration", "Release"
}

if ($Coverage) {
    $arguments += "--collect:""XPlat Code Coverage"""
    $arguments += "--results-directory", $OutputDir
}

# Run tests
Write-Host "`nRunning: dotnet $($arguments -join ' ')" -ForegroundColor Yellow
Write-Host ""

$startTime = Get-Date
& dotnet @arguments
$exitCode = $LASTEXITCODE
$duration = (Get-Date) - $startTime

# Summary
Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "Test Run Complete" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Duration: $($duration.ToString('mm\:ss'))"
Write-Host "Exit Code: $exitCode"

if ($exitCode -eq 0) {
    Write-Host "Result: PASSED" -ForegroundColor Green
}
else {
    Write-Host "Result: FAILED" -ForegroundColor Red
}

# Generate HTML report if requested
if ($GenerateReport) {
    Write-Host "`nGenerating HTML report..." -ForegroundColor Yellow
    
    # Using ReportGenerator if available
    if (Get-Command reportgenerator -ErrorAction SilentlyContinue) {
        if ($Coverage) {
            & reportgenerator `
                -reports:"$OutputDir/**/coverage.cobertura.xml" `
                -targetdir:"$OutputDir/CoverageReport" `
                -reporttypes:"Html"
            
            Write-Host "Coverage report: $OutputDir/CoverageReport/index.html" -ForegroundColor Green
        }
    }
    else {
        Write-Host "ReportGenerator not installed. Install with: dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor Yellow
    }
}

# Return exit code
exit $exitCode
```

---

## 15.2 .runsettings File

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- uitests.runsettings -->
<RunSettings>
  <!-- General run configuration -->
  <RunConfiguration>
    <MaxCpuCount>1</MaxCpuCount>
    <ResultsDirectory>.\TestResults</ResultsDirectory>
    <TargetFrameworkVersion>net9.0</TargetFrameworkVersion>
    <TestSessionTimeout>1800000</TestSessionTimeout>
    
    <!-- Environment variables -->
    <EnvironmentVariables>
      <PLATFORM>Windows</PLATFORM>
      <LOG_OUTPUT_PATH>TestResults\logs</LOG_OUTPUT_PATH>
      <LOG_PREFIX>UITests</LOG_PREFIX>
    </EnvironmentVariables>
  </RunConfiguration>
  
  <!-- xUnit specific settings -->
  <xUnit>
    <MaxParallelThreads>1</MaxParallelThreads>
    <ParallelizeAssembly>false</ParallelizeAssembly>
    <ParallelizeTestCollections>false</ParallelizeTestCollections>
    <ShadowCopy>false</ShadowCopy>
    <MethodDisplay>classAndMethod</MethodDisplay>
    <MethodDisplayOptions>all</MethodDisplayOptions>
    <PreEnumerateTheories>true</PreEnumerateTheories>
    <DiagnosticMessages>false</DiagnosticMessages>
    <LongRunningTestSeconds>120</LongRunningTestSeconds>
  </xUnit>
  
  <!-- Code coverage settings -->
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>
          <Format>cobertura</Format>
          <Exclude>
            [*]*.Tests.*,
            [*]*.TestData.*,
            [*]*.Infrastructure.*
          </Exclude>
          <Include>
            [Oravey.UITestFramework.*]*
          </Include>
          <ExcludeByFile>
            **/Migrations/*.cs
          </ExcludeByFile>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
  
  <!-- Blame settings for debugging hangs -->
  <LoggerRunSettings>
    <Loggers>
      <Logger friendlyName="blame" enabled="True">
        <Configuration>
          <CollectDump>True</CollectDump>
          <CollectDumpOnExit>True</CollectDumpOnExit>
        </Configuration>
      </Logger>
    </Loggers>
  </LoggerRunSettings>
</RunSettings>
```

---

## 15.3 GitHub Actions Complete Workflow

```yaml
# .github/workflows/ui-tests.yml
name: UI Tests

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]
  workflow_dispatch:
    inputs:
      category:
        description: 'Test category'
        required: false
        default: 'Smoke'
        type: choice
        options:
          - Smoke
          - Regression
          - E2E
          - All
      platform:
        description: 'Target platform'
        required: false
        default: 'Windows'
        type: choice
        options:
          - Windows
          - Web
      cloud_provider:
        description: 'Cloud provider'
        required: false
        default: 'None'
        type: choice
        options:
          - None
          - BrowserStack
          - SauceLabs

env:
  DOTNET_VERSION: '9.0.x'
  TEST_PROJECT: 'src/Oravey.Tools.Wpf.UITests'

jobs:
  build:
    runs-on: windows-latest
    outputs:
      test-filter: ${{ steps.set-filter.outputs.filter }}
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      
      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ hashFiles('**/*.csproj') }}
          restore-keys: nuget-
      
      - name: Restore
        run: dotnet restore
      
      - name: Build
        run: dotnet build --configuration Release --no-restore
      
      - name: Set test filter
        id: set-filter
        run: |
          $category = "${{ github.event.inputs.category || 'Smoke' }}"
          $platform = "${{ github.event.inputs.platform || 'Windows' }}"
          
          $filter = "Category=$category&Platform=$platform"
          echo "filter=$filter" >> $env:GITHUB_OUTPUT
          echo "Test filter: $filter"
      
      - name: Upload build artifacts
        uses: actions/upload-artifact@v4
        with:
          name: build
          path: |
            **/bin/Release
            **/obj/Release
          retention-days: 1

  test:
    needs: build
    runs-on: windows-latest
    timeout-minutes: 30
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      
      - name: Download build artifacts
        uses: actions/download-artifact@v4
        with:
          name: build
      
      - name: Run UI Tests
        id: test
        run: |
          $filter = "${{ needs.build.outputs.test-filter }}"
          
          dotnet test ${{ env.TEST_PROJECT }} `
            --configuration Release `
            --no-build `
            --filter "$filter" `
            --logger "trx;LogFileName=results.trx" `
            --logger "console;verbosity=normal" `
            --results-directory TestResults `
            --blame-hang `
            --blame-hang-timeout 120s
        env:
          PLATFORM: ${{ github.event.inputs.platform || 'Windows' }}
          CLOUD_PROVIDER: ${{ github.event.inputs.cloud_provider || 'None' }}
          CLOUD_USERNAME: ${{ secrets.CLOUD_USERNAME }}
          CLOUD_ACCESS_KEY: ${{ secrets.CLOUD_ACCESS_KEY }}
          CLOUD_PROJECT: Oravey
          CLOUD_BUILD: ${{ github.run_number }}
      
      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: |
            TestResults/**/*.trx
            TestResults/**/logs/**
            TestResults/**/screenshots/**
          retention-days: 14
      
      - name: Publish test results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: UI Test Results
          path: 'TestResults/**/*.trx'
          reporter: dotnet-trx
          fail-on-error: false

  notify:
    needs: test
    runs-on: ubuntu-latest
    if: failure()
    
    steps:
      - name: Send failure notification
        uses: slackapi/slack-github-action@v1
        with:
          payload: |
            {
              "text": "UI Tests Failed",
              "blocks": [
                {
                  "type": "section",
                  "text": {
                    "type": "mrkdwn",
                    "text": ":x: UI Tests failed on `${{ github.ref_name }}`\n<${{ github.server_url }}/${{ github.repository }}/actions/runs/${{ github.run_id }}|View Results>"
                  }
                }
              ]
            }
        env:
          SLACK_WEBHOOK_URL: ${{ secrets.SLACK_WEBHOOK_URL }}
```

---

## 15.4 Azure DevOps Pipeline

```yaml
# azure-pipelines.yml
trigger:
  branches:
    include:
      - main
      - develop
  paths:
    include:
      - src/**
      - tests/**

pr:
  branches:
    include:
      - main

pool:
  vmImage: 'windows-latest'

variables:
  - group: UITestSecrets
  - name: BuildConfiguration
    value: 'Release'
  - name: TestProject
    value: 'src/Oravey.Tools.Wpf.UITests/Oravey.Tools.Wpf.UITests.csproj'

stages:
  - stage: Build
    jobs:
      - job: Build
        steps:
          - task: UseDotNet@2
            displayName: 'Use .NET 9'
            inputs:
              version: '9.0.x'
          
          - task: DotNetCoreCLI@2
            displayName: 'Restore'
            inputs:
              command: 'restore'
              projects: '**/*.csproj'
          
          - task: DotNetCoreCLI@2
            displayName: 'Build'
            inputs:
              command: 'build'
              projects: '**/*.csproj'
              arguments: '--configuration $(BuildConfiguration) --no-restore'
          
          - task: PublishPipelineArtifact@1
            displayName: 'Publish Build'
            inputs:
              targetPath: '$(Build.SourcesDirectory)'
              artifact: 'build'

  - stage: Test
    dependsOn: Build
    jobs:
      - job: UITests
        timeoutInMinutes: 60
        
        strategy:
          matrix:
            Smoke:
              TestFilter: 'Category=Smoke'
              Platform: 'Windows'
            Regression:
              TestFilter: 'Category=Regression'
              Platform: 'Windows'
          maxParallel: 1
        
        steps:
          - task: DownloadPipelineArtifact@2
            inputs:
              artifact: 'build'
              path: '$(Build.SourcesDirectory)'
          
          - task: UseDotNet@2
            inputs:
              version: '9.0.x'
          
          - task: DotNetCoreCLI@2
            displayName: 'Run Tests: $(TestFilter)'
            inputs:
              command: 'test'
              projects: '$(TestProject)'
              arguments: >
                --configuration $(BuildConfiguration)
                --no-build
                --filter "$(TestFilter)"
                --logger "trx;LogFileName=results_$(System.JobAttempt).trx"
                --results-directory $(Build.ArtifactStagingDirectory)/TestResults
                --blame-hang
                --blame-hang-timeout 120s
            env:
              PLATFORM: $(Platform)
              CLOUD_PROVIDER: $(CloudProvider)
              CLOUD_USERNAME: $(CloudUsername)
              CLOUD_ACCESS_KEY: $(CloudAccessKey)
            continueOnError: true
          
          - task: PublishTestResults@2
            displayName: 'Publish Results'
            condition: always()
            inputs:
              testResultsFormat: 'VSTest'
              testResultsFiles: '$(Build.ArtifactStagingDirectory)/TestResults/**/*.trx'
              mergeTestResults: true
              testRunTitle: 'UI Tests - $(TestFilter)'
          
          - task: PublishPipelineArtifact@1
            displayName: 'Publish Logs'
            condition: always()
            inputs:
              targetPath: '$(Build.ArtifactStagingDirectory)/TestResults'
              artifact: 'test-results-$(System.JobPositionInPhase)'
```

---

## 15.5 Test Results Analyzer

```powershell
# analyze-results.ps1
# Analyze TRX test results

param(
    [Parameter(Mandatory)]
    [string]$ResultsPath,
    
    [switch]$FailuresOnly,
    [switch]$GenerateHtml
)

# Find all TRX files
$trxFiles = Get-ChildItem -Path $ResultsPath -Filter "*.trx" -Recurse

if ($trxFiles.Count -eq 0) {
    Write-Host "No TRX files found in $ResultsPath" -ForegroundColor Yellow
    exit 0
}

Write-Host "Found $($trxFiles.Count) result file(s)" -ForegroundColor Cyan

$allResults = @()

foreach ($trx in $trxFiles) {
    Write-Host "`nProcessing: $($trx.Name)" -ForegroundColor Yellow
    
    [xml]$xml = Get-Content $trx.FullName
    $ns = @{ t = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010" }
    
    # Extract results
    $results = Select-Xml -Xml $xml -Namespace $ns -XPath "//t:UnitTestResult"
    
    foreach ($result in $results) {
        $r = $result.Node
        $allResults += [PSCustomObject]@{
            TestName = $r.testName
            Outcome = $r.outcome
            Duration = [TimeSpan]::Parse($r.duration)
            StartTime = [DateTime]$r.startTime
            ErrorMessage = $r.Output.ErrorInfo.Message
            StackTrace = $r.Output.ErrorInfo.StackTrace
        }
    }
}

# Summary
Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "Test Results Summary" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

$grouped = $allResults | Group-Object Outcome

$passed = ($grouped | Where-Object { $_.Name -eq "Passed" }).Count
$failed = ($grouped | Where-Object { $_.Name -eq "Failed" }).Count
$skipped = ($grouped | Where-Object { $_.Name -eq "NotExecuted" }).Count

Write-Host "Total: $($allResults.Count)"
Write-Host "Passed: $passed" -ForegroundColor Green
Write-Host "Failed: $failed" -ForegroundColor Red
Write-Host "Skipped: $skipped" -ForegroundColor Yellow

$totalDuration = [TimeSpan]::FromTicks(($allResults | Measure-Object -Property Duration -Sum).Sum.Ticks)
Write-Host "Total Duration: $($totalDuration.ToString('mm\:ss'))"

# Failed tests
if ($failed -gt 0) {
    Write-Host "`n============================================" -ForegroundColor Red
    Write-Host "Failed Tests" -ForegroundColor Red
    Write-Host "============================================" -ForegroundColor Red
    
    $failedTests = $allResults | Where-Object { $_.Outcome -eq "Failed" }
    
    foreach ($test in $failedTests) {
        Write-Host "`n$($test.TestName)" -ForegroundColor Red
        Write-Host "  Duration: $($test.Duration)"
        if ($test.ErrorMessage) {
            Write-Host "  Error: $($test.ErrorMessage)" -ForegroundColor Yellow
        }
    }
}

# Generate HTML if requested
if ($GenerateHtml) {
    $htmlPath = Join-Path $ResultsPath "report.html"
    
    $html = @"
<!DOCTYPE html>
<html>
<head>
    <title>UI Test Results</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .summary { background: #f0f0f0; padding: 20px; margin-bottom: 20px; }
        .passed { color: green; }
        .failed { color: red; }
        table { border-collapse: collapse; width: 100%; }
        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
        th { background: #4CAF50; color: white; }
        tr.failed { background: #ffebee; }
    </style>
</head>
<body>
    <h1>UI Test Results</h1>
    <div class="summary">
        <p>Total: $($allResults.Count)</p>
        <p class="passed">Passed: $passed</p>
        <p class="failed">Failed: $failed</p>
        <p>Skipped: $skipped</p>
        <p>Duration: $($totalDuration.ToString('mm\:ss'))</p>
    </div>
    <table>
        <tr><th>Test</th><th>Outcome</th><th>Duration</th><th>Error</th></tr>
"@
    
    foreach ($test in $allResults) {
        $rowClass = if ($test.Outcome -eq "Failed") { "failed" } else { "" }
        $html += "<tr class='$rowClass'>"
        $html += "<td>$($test.TestName)</td>"
        $html += "<td>$($test.Outcome)</td>"
        $html += "<td>$($test.Duration.ToString('mm\:ss\.fff'))</td>"
        $html += "<td>$($test.ErrorMessage)</td>"
        $html += "</tr>"
    }
    
    $html += "</table></body></html>"
    
    $html | Out-File -FilePath $htmlPath -Encoding UTF8
    Write-Host "`nHTML report: $htmlPath" -ForegroundColor Green
}

# Exit with appropriate code
if ($failed -gt 0) {
    exit 1
}
exit 0
```

---

*Related: [Best Practices Code Examples](21d16_BestPractices_CodeExamples.md)*

#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Run Brinell MAUI UI tests on Android emulator/device.

.DESCRIPTION
    This script sets up the environment and runs the Brinell MAUI UI tests
    against an Android device or emulator using Appium with UiAutomator2.

.PARAMETER Filter
    Optional test filter (e.g., "Button_IsExists" or "FullyQualifiedName~Toggle").

.PARAMETER DeviceName
    Android device/emulator name. Default: emulator-5554

.PARAMETER Build
    Build the Android APK before running tests.

.PARAMETER Restore
    Restore NuGet packages before building.

.PARAMETER StartAppium
    Start Appium server automatically if not running.

.EXAMPLE
    .\run-android-tests.ps1 -Build
    Builds the APK and runs all tests.

.EXAMPLE
    .\run-android-tests.ps1 -Filter "Button_IsExists"
    Runs only tests matching "Button_IsExists".

.EXAMPLE
    .\run-android-tests.ps1 -DeviceName "Pixel_6_API_34" -Build -StartAppium
    Builds, starts Appium, and runs tests on specific device.
#>

param(
    [string]$Filter = "",
    [string]$DeviceName = "emulator-5554",
    [switch]$Build,
    [switch]$Restore,
    [switch]$StartAppium
)

$ErrorActionPreference = "Stop"
$ScriptDir = $PSScriptRoot

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Brinell Android UI Test Runner" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Paths
$SampleAppDir = Join-Path $ScriptDir "samples\Brinell.Samples.Maui.App"
# The mobile head, not testsnew\Brinell.Maui.UITests: that project targets net10.0-windows
# and references the FlaUI driver, so it cannot host an Android run. The mobile head links
# the same test sources and references Appium instead.
$TestProjectDir = Join-Path $ScriptDir "testsnew\Brinell.Maui.UITests.Mobile"
$ApkPath = Join-Path $SampleAppDir "bin\Debug\net10.0-android\com.brinell.samples.maui-Signed.apk"

# Check prerequisites
Write-Host "[1/6] Checking prerequisites..." -ForegroundColor Yellow

# Check ADB
try {
    $adbVersion = adb version 2>&1 | Select-Object -First 1
    Write-Host "  ADB: $adbVersion" -ForegroundColor Green
} catch {
    Write-Error "ADB not found. Please install Android SDK Platform Tools."
    exit 1
}

# Check device
Write-Host "`n[2/6] Checking Android device..." -ForegroundColor Yellow
$devices = adb devices 2>&1 | Select-String "device$"
if (-not $devices) {
    Write-Host "  No devices connected." -ForegroundColor Red
    Write-Host "  Start an emulator or connect a device with USB debugging enabled." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  Available emulators:" -ForegroundColor Cyan
    try {
        emulator -list-avds 2>&1 | ForEach-Object { Write-Host "    - $_" }
    } catch {
        Write-Host "    (emulator command not found)" -ForegroundColor Gray
    }
    exit 1
}
Write-Host "  Devices found:" -ForegroundColor Green
$devices | ForEach-Object { Write-Host "    $_" -ForegroundColor Green }

# Build if requested
if ($Build) {
    Write-Host "`n[3/6] Building Android APK..." -ForegroundColor Yellow
    
    $buildArgs = @(
        "build"
        $SampleAppDir
        "-f", "net10.0-android"
        "-c", "Debug"
    )
    
    if ($Restore) {
        Write-Host "  Restoring packages..." -ForegroundColor Gray
    } else {
        $buildArgs += "--no-restore"
    }
    
    & dotnet @buildArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed with exit code $LASTEXITCODE"
        exit 1
    }
    Write-Host "  Build succeeded!" -ForegroundColor Green
} else {
    Write-Host "`n[3/6] Skipping build (use -Build to build APK)" -ForegroundColor Gray
}

# Verify APK exists
Write-Host "`n[4/6] Verifying APK..." -ForegroundColor Yellow
if (-not (Test-Path $ApkPath)) {
    Write-Error "APK not found at: $ApkPath"
    Write-Host "Run with -Build to build the APK first." -ForegroundColor Yellow
    exit 1
}
$apkSize = (Get-Item $ApkPath).Length / 1MB
Write-Host "  APK found: $([math]::Round($apkSize, 2)) MB" -ForegroundColor Green

# Check/Start Appium
Write-Host "`n[5/6] Checking Appium server..." -ForegroundColor Yellow
$appiumRunning = $false
try {
    $status = Invoke-RestMethod "http://127.0.0.1:4723/status" -TimeoutSec 2
    $appiumRunning = $true
    Write-Host "  Appium server ready: $($status.value.message)" -ForegroundColor Green
} catch {
    Write-Host "  Appium server not responding at http://127.0.0.1:4723" -ForegroundColor Red
    
    if ($StartAppium) {
        Write-Host "  Starting Appium server..." -ForegroundColor Yellow
        Start-Process -FilePath "appium" -ArgumentList "--base-path", "/" -WindowStyle Minimized
        Start-Sleep -Seconds 5
        
        try {
            $status = Invoke-RestMethod "http://127.0.0.1:4723/status" -TimeoutSec 5
            $appiumRunning = $true
            Write-Host "  Appium server started!" -ForegroundColor Green
        } catch {
            Write-Error "Failed to start Appium server. Start it manually: appium --base-path /"
            exit 1
        }
    } else {
        Write-Host "  Start Appium manually: appium --base-path /" -ForegroundColor Yellow
        Write-Host "  Or use -StartAppium flag to auto-start." -ForegroundColor Yellow
        exit 1
    }
}

# Set environment variables
Write-Host "`n[6/6] Running tests..." -ForegroundColor Yellow
$env:APPIUM_PLATFORM = "android"
$env:APPIUM_DEVICE_NAME = $DeviceName
$env:APPIUM_APP_PATH = $ApkPath

Write-Host "  Platform: $($env:APPIUM_PLATFORM)" -ForegroundColor Cyan
Write-Host "  Device: $($env:APPIUM_DEVICE_NAME)" -ForegroundColor Cyan
Write-Host "  APK: $($env:APPIUM_APP_PATH)" -ForegroundColor Cyan
Write-Host ""

# Build test arguments
$testArgs = @(
    "test"
    $TestProjectDir
    "--logger", "console;verbosity=normal"
)

if ($Filter) {
    $testArgs += "--filter", $Filter
    Write-Host "  Filter: $Filter" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Starting test execution..." -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Run tests
& dotnet @testArgs

$exitCode = $LASTEXITCODE

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
if ($exitCode -eq 0) {
    Write-Host "  Tests completed successfully!" -ForegroundColor Green
} else {
    Write-Host "  Tests completed with failures." -ForegroundColor Red
}
Write-Host "============================================" -ForegroundColor Cyan

exit $exitCode

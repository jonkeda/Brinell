# Simple test script to verify app and pipe communication
$ErrorActionPreference = "Stop"

$appPath = "E:\repos\Private\Iosk\Oravey\Brinell\samples\Brinell.Samples.Stride.App\bin\Debug\net10.0-windows\Brinell.Samples.Stride.App.exe"

Write-Host "=== Simple Stride App Test ===" -ForegroundColor Cyan
Write-Host "App path: $appPath"

if (-not (Test-Path $appPath)) {
    Write-Host "ERROR: App not found!" -ForegroundColor Red
    exit 1
}

# Kill any existing instances
Get-Process -Name "Brinell.Samples.Stride.App" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

Write-Host "`n1. Starting app..." -ForegroundColor Yellow
$process = Start-Process -FilePath $appPath -ArgumentList "--automation" -PassThru
Write-Host "   PID: $($process.Id)"

Write-Host "`n2. Waiting for app to initialize (3 seconds)..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

if ($process.HasExited) {
    Write-Host "ERROR: App exited with code $($process.ExitCode)" -ForegroundColor Red
    exit 1
}
Write-Host "   App is running" -ForegroundColor Green

Write-Host "`n3. Checking for pipe..." -ForegroundColor Yellow
$pipePath = "\\.\pipe\Brinell.Stride.Automation"
$pipeExists = Test-Path $pipePath
Write-Host "   Pipe exists: $pipeExists"

if (-not $pipeExists) {
    Write-Host "ERROR: Pipe not created!" -ForegroundColor Red
    $process.Kill()
    exit 1
}

Write-Host "`n4. Connecting to pipe..." -ForegroundColor Yellow
try {
    $pipe = [System.IO.Pipes.NamedPipeClientStream]::new(".", "Brinell.Stride.Automation", [System.IO.Pipes.PipeDirection]::InOut, [System.IO.Pipes.PipeOptions]::Asynchronous)
    $pipe.Connect(5000)
    Write-Host "   Connected!" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Failed to connect: $_" -ForegroundColor Red
    $process.Kill()
    exit 1
}

Write-Host "`n5. Sending test command..." -ForegroundColor Yellow
try {
    $writer = [System.IO.StreamWriter]::new($pipe, [System.Text.Encoding]::UTF8, 1024, $true)
    $writer.AutoFlush = $true
    $reader = [System.IO.StreamReader]::new($pipe, [System.Text.Encoding]::UTF8, $true, 1024, $true)
    
    $command = '{"type":"GameQuery","method":"IsReady"}'
    Write-Host "   Sending: $command"
    $writer.WriteLine($command)
    
    Write-Host "   Waiting for response..."
    $response = $reader.ReadLine()
    Write-Host "   Response: $response" -ForegroundColor Green
    
    $writer.Dispose()
    $reader.Dispose()
} catch {
    Write-Host "ERROR: Communication failed: $_" -ForegroundColor Red
}

Write-Host "`n6. Cleaning up..." -ForegroundColor Yellow
$pipe.Dispose()
$process.Kill()
$process.WaitForExit()
Write-Host "   Done!" -ForegroundColor Green

Write-Host "`n=== TEST PASSED ===" -ForegroundColor Green

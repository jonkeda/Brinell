$ErrorActionPreference = "Stop"
$gamePath = "e:\repos\Private\Iosk\Oravey\Brinell\samples\Brinell.Samples.Stride.App\bin\Debug\net10.0-windows\Brinell.Samples.Stride.App.exe"

Write-Output "Starting game with console output..."
$proc = Start-Process -FilePath $gamePath -ArgumentList "--automation" -PassThru -RedirectStandardOutput "$env:TEMP\stride_stdout.txt" -RedirectStandardError "$env:TEMP\stride_stderr.txt"
Write-Output "Game PID: $($proc.Id)"
Start-Sleep -Seconds 8

Write-Output "=== Game stdout ==="
Get-Content "$env:TEMP\stride_stdout.txt" -ErrorAction SilentlyContinue
Write-Output "=== Game stderr ==="
Get-Content "$env:TEMP\stride_stderr.txt" -ErrorAction SilentlyContinue
Write-Output "==="

try {
    $pipe = New-Object System.IO.Pipes.NamedPipeClientStream(".", "Brinell.Stride.Automation", [System.IO.Pipes.PipeDirection]::InOut)
    $pipe.Connect(5000)
    Write-Output "Connected to pipe!"
    
    $writer = New-Object System.IO.StreamWriter($pipe)
    $writer.AutoFlush = $true
    $reader = New-Object System.IO.StreamReader($pipe)

    # Send SimulateKeyPress Escape
    $cmd = '{"type":"Action","method":"SimulateKeyPress","target":null,"args":["Escape"]}'
    Write-Output "Sending: SimulateKeyPress Escape"
    $writer.WriteLine($cmd)
    
    # Use a timeout for ReadLine
    $task = $reader.ReadLineAsync()
    if ($task.Wait(5000)) {
        Write-Output "Response: $($task.Result)"
    } else {
        Write-Output "TIMEOUT: No response in 5 seconds - Update() not draining queue"
    }

    # Check stdout again after sending the command
    Start-Sleep -Seconds 2
    Write-Output "=== Game stdout after command ==="
    Get-Content "$env:TEMP\stride_stdout.txt" -ErrorAction SilentlyContinue
    
    $pipe.Dispose()
} catch {
    Write-Output "ERROR: $_"
}

Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
Write-Output "Done."

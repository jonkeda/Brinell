$ErrorActionPreference = "Stop"
$gamePath = "e:\repos\Private\Iosk\Oravey\Brinell\samples\Brinell.Samples.Stride.App\bin\Debug\net10.0-windows\Brinell.Samples.Stride.App.exe"

Write-Output "Starting game..."
$proc = Start-Process -FilePath $gamePath -ArgumentList "--automation" -PassThru
Write-Output "Game PID: $($proc.Id)"
Start-Sleep -Seconds 6

if ($proc.HasExited) {
    Write-Output "GAME CRASHED! Exit code: $($proc.ExitCode)"
    exit 1
}
Write-Output "Game is running."

# Check pipe exists
$pipes = [System.IO.Directory]::GetFiles("\\.\pipe\") | Where-Object { $_ -like "*Brinell*" }
if ($pipes) { Write-Output "Pipe found: $pipes" } else { Write-Output "NO PIPE FOUND"; Stop-Process -Id $proc.Id -Force; exit 1 }

# Connect and send command
try {
    $pipe = New-Object System.IO.Pipes.NamedPipeClientStream(".", "Brinell.Stride.Automation", [System.IO.Pipes.PipeDirection]::InOut)
    $pipe.Connect(5000)
    Write-Output "Connected to pipe!"
    
    $writer = New-Object System.IO.StreamWriter($pipe)
    $writer.AutoFlush = $true
    $reader = New-Object System.IO.StreamReader($pipe)
    
    # Send IsReady query
    $cmd = '{"type":"GameQuery","method":"IsReady","target":"","args":null}'
    Write-Output "Sending: $cmd"
    $writer.WriteLine($cmd)
    
    $response = $reader.ReadLine()
    Write-Output "Response: $response"
    
    # Send GetState for a UI element
    $cmd2 = '{"type":"Query","method":"Exists","target":"PlayButton","args":null}'
    Write-Output "Sending: $cmd2"
    $writer.WriteLine($cmd2)
    $response2 = $reader.ReadLine()
    Write-Output "Response: $response2"
    
    $pipe.Dispose()
    Write-Output "Pipe closed successfully."
} catch {
    Write-Output "PIPE ERROR: $_"
}

Stop-Process -Id $proc.Id -Force
Write-Output "Done."

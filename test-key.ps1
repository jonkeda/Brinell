$ErrorActionPreference = "Stop"
$gamePath = "e:\repos\Private\Iosk\Oravey\Brinell\samples\Brinell.Samples.Stride.App\bin\Debug\net10.0-windows\Brinell.Samples.Stride.App.exe"

Write-Output "Starting game..."
$proc = Start-Process -FilePath $gamePath -ArgumentList "--automation" -PassThru
Write-Output "Game PID: $($proc.Id)"
Start-Sleep -Seconds 6

try {
    $pipe = New-Object System.IO.Pipes.NamedPipeClientStream(".", "Brinell.Stride.Automation", [System.IO.Pipes.PipeDirection]::InOut)
    $pipe.Connect(5000)
    Write-Output "Connected to pipe!"
    
    $writer = New-Object System.IO.StreamWriter($pipe)
    $writer.AutoFlush = $true
    $reader = New-Object System.IO.StreamReader($pipe)

    # Check if UI elements exist
    foreach ($name in @("CounterValue", "IncrementButton", "EscHint", "SettingsPanel")) {
        $cmd = "{`"type`":`"Query`",`"method`":`"Exists`",`"target`":`"$name`",`"args`":null}"
        $writer.WriteLine($cmd)
        $response = $reader.ReadLine()
        Write-Output "Exists($name): $response"
    }

    # Now simulate Escape key press
    $cmd = '{"type":"Action","method":"SimulateKeyPress","target":null,"args":["Escape"]}'
    Write-Output "Sending: SimulateKeyPress Escape"
    $writer.WriteLine($cmd)
    $response = $reader.ReadLine()
    Write-Output "Response: $response"

    # Wait a moment for the game to process
    Start-Sleep -Seconds 1

    # Check if SettingsPanel now exists
    $cmd = '{"type":"Query","method":"Exists","target":"SettingsPanel","args":null}'
    $writer.WriteLine($cmd)
    $response = $reader.ReadLine()
    Write-Output "SettingsPanel exists after ESC: $response"

    $pipe.Dispose()
    Write-Output "Done."
} catch {
    Write-Output "ERROR: $_"
}

Stop-Process -Id $proc.Id -Force

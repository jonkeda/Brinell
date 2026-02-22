$ErrorActionPreference = "Stop"
$gamePath = "e:\repos\Private\Iosk\Oravey\Brinell\samples\Brinell.Samples.Stride.App\bin\Debug\net10.0-windows\Brinell.Samples.Stride.App.exe"

$proc = Start-Process -FilePath $gamePath -ArgumentList "--automation" -PassThru
Start-Sleep -Seconds 6

try {
    $pipe = New-Object System.IO.Pipes.NamedPipeClientStream(".", "Brinell.Stride.Automation", [System.IO.Pipes.PipeDirection]::InOut)
    $pipe.Connect(5000)
    $writer = New-Object System.IO.StreamWriter($pipe)
    $writer.AutoFlush = $true
    $reader = New-Object System.IO.StreamReader($pipe)

    # Check SettingsPanel before ESC
    $cmd = '{"type":"Query","method":"GetState","target":"SettingsPanel","args":null}'
    $writer.WriteLine($cmd)
    $r = $reader.ReadLine()
    Write-Output "Before ESC - SettingsPanel: $r"

    # Press ESC
    $cmd = '{"type":"Action","method":"SimulateKeyPress","target":null,"args":["Escape"]}'
    $writer.WriteLine($cmd)
    $r = $reader.ReadLine()
    Write-Output "SimulateKeyPress: $r"

    Start-Sleep -Seconds 1

    # Check SettingsPanel after ESC
    $cmd = '{"type":"Query","method":"GetState","target":"SettingsPanel","args":null}'
    $writer.WriteLine($cmd)
    $r = $reader.ReadLine()
    Write-Output "After ESC - SettingsPanel: $r"
    
    # Check ApplyButton visibility
    $cmd = '{"type":"Query","method":"IsVisible","target":"ApplyButton","args":null}'
    $writer.WriteLine($cmd)
    $r = $reader.ReadLine()
    Write-Output "ApplyButton visible: $r"

    $pipe.Dispose()
} catch { Write-Output "ERROR: $_" }

Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue

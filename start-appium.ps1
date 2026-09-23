# Start Appium Server for Brinell MAUI Tests
# Keep this terminal open while running tests

Write-Host "Starting Appium Server..." -ForegroundColor Cyan
Write-Host "URL: http://127.0.0.1:4723" -ForegroundColor Green
Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow
Write-Host ""

appium --address 127.0.0.1 --port 4723 --relaxed-security

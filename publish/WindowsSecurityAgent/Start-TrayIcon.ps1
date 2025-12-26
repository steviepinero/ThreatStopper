# Start Windows Security Agent Tray Icon
# This can be run from any location - the tray icon monitors the service

$trayExeBuild = "C:\Users\stevi\WindowsSecurityPlatform\src\WindowsSecurityAgent\WindowsSecurityAgent.TrayIcon\bin\Release\net10.0-windows\WindowsSecurityAgent.TrayIcon.exe"
$trayExeInstall = "C:\Program Files\WindowsSecurityAgent\WindowsSecurityAgent.TrayIcon.exe"

# Check if already running
$existingProcess = Get-Process -Name "WindowsSecurityAgent.TrayIcon" -ErrorAction SilentlyContinue
if ($existingProcess) {
    Write-Host "Tray icon is already running (PID: $($existingProcess.Id))" -ForegroundColor Green
    Write-Host "Look for the shield icon in your system tray" -ForegroundColor Cyan
    exit 0
}

# Find the executable
$trayExe = $null
if (Test-Path $trayExeInstall) {
    $trayExe = $trayExeInstall
    $workingDir = "C:\Program Files\WindowsSecurityAgent"
} elseif (Test-Path $trayExeBuild) {
    $trayExe = $trayExeBuild
    $workingDir = Split-Path -Parent $trayExeBuild
} else {
    Write-Host "ERROR: Tray icon executable not found!" -ForegroundColor Red
    Write-Host "Expected locations:" -ForegroundColor Yellow
    Write-Host "  $trayExeInstall" -ForegroundColor Gray
    Write-Host "  $trayExeBuild" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Please build the TrayIcon project first:" -ForegroundColor Yellow
    Write-Host "  cd src\WindowsSecurityAgent\WindowsSecurityAgent.TrayIcon" -ForegroundColor White
    Write-Host "  dotnet build -c Release" -ForegroundColor White
    exit 1
}

Write-Host "Starting tray icon..." -ForegroundColor Cyan
Write-Host "Executable: $trayExe" -ForegroundColor Gray

try {
    Start-Process -FilePath $trayExe -WorkingDirectory $workingDir -WindowStyle Hidden
    Start-Sleep -Seconds 3
    
    $process = Get-Process -Name "WindowsSecurityAgent.TrayIcon" -ErrorAction SilentlyContinue
    if ($process) {
        Write-Host "✓ Tray icon started successfully (PID: $($process.Id))" -ForegroundColor Green
        Write-Host ""
        Write-Host "Look for the shield icon in your system tray (bottom right corner)" -ForegroundColor Cyan
        Write-Host "If you do not see it, click the up arrow to show hidden icons" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Right-click the icon to:" -ForegroundColor White
        Write-Host "  • View service status" -ForegroundColor Gray
        Write-Host "  • Start/Stop the service" -ForegroundColor Gray
        Write-Host "  • View about information" -ForegroundColor Gray
    } else {
        Write-Host "⚠ Tray icon process started but may have exited" -ForegroundColor Yellow
        Write-Host "Check for error messages or try running it directly to see errors" -ForegroundColor Yellow
    }
} catch {
    Write-Host "ERROR: Failed to start tray icon: $_" -ForegroundColor Red
    exit 1
}


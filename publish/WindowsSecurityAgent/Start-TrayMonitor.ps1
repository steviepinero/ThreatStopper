# Start Windows Security Agent Tray Monitor
# This script starts the tray icon application that monitors the service

param(
    [switch]$InstallStartup
)

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
# Check installation directory first, then script directory
$installPath = "C:\Program Files\WindowsSecurityAgent"
$trayExePath = Join-Path $installPath "WindowsSecurityAgent.TrayIcon.exe"
if (-not (Test-Path $trayExePath)) {
    $trayExePath = Join-Path $scriptPath "WindowsSecurityAgent.TrayIcon.exe"
}

if (-not (Test-Path $trayExePath)) {
    Write-Host "ERROR: Tray monitor executable not found at: $trayExePath" -ForegroundColor Red
    Write-Host "Please build the TrayIcon project first." -ForegroundColor Yellow
    exit 1
}

# Check if already running
$existingProcess = Get-Process -Name "WindowsSecurityAgent.TrayIcon" -ErrorAction SilentlyContinue
if ($existingProcess) {
    Write-Host "Tray monitor is already running (PID: $($existingProcess.Id))" -ForegroundColor Yellow
    exit 0
}

if ($InstallStartup) {
    Write-Host "Installing tray monitor to startup..." -ForegroundColor Cyan
    
    $startupPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup"
    $shortcutPath = Join-Path $startupPath "Windows Security Agent Tray.lnk"
    
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $trayExePath
    $shortcut.WorkingDirectory = $scriptPath
    $shortcut.Description = "Windows Security Agent Tray Monitor"
    $shortcut.Save()
    
    Write-Host "✓ Tray monitor added to startup" -ForegroundColor Green
}

Write-Host "Starting tray monitor..." -ForegroundColor Cyan
$workingDir = if (Test-Path (Join-Path $installPath "WindowsSecurityAgent.TrayIcon.exe")) { $installPath } else { $scriptPath }
Start-Process -FilePath $trayExePath -WorkingDirectory $workingDir -WindowStyle Hidden

Start-Sleep -Seconds 2

$process = Get-Process -Name "WindowsSecurityAgent.TrayIcon" -ErrorAction SilentlyContinue
if ($process) {
    Write-Host "✓ Tray monitor started successfully (PID: $($process.Id))" -ForegroundColor Green
    Write-Host "Look for the shield icon in your system tray." -ForegroundColor Cyan
} else {
    Write-Host "⚠ Tray monitor may have failed to start. Check for errors." -ForegroundColor Yellow
}


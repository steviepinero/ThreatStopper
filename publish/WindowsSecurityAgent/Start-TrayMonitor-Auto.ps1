# Auto-start Tray Monitor when ThreatStopper service is running
# This script monitors the service and launches the tray monitor automatically

param(
    [switch]$Install,
    [switch]$Uninstall
)

$ServiceName = "WindowsSecurityAgent"
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$installPath = "C:\Program Files\WindowsSecurityAgent"
$trayExePath = Join-Path $installPath "WindowsSecurityAgent.TrayIcon.exe"
$trayDllPath = Join-Path $installPath "WindowsSecurityAgent.TrayIcon.dll"
if (-not (Test-Path $trayExePath)) {
    $trayExePath = Join-Path $scriptPath "WindowsSecurityAgent.TrayIcon.exe"
    $trayDllPath = Join-Path $scriptPath "WindowsSecurityAgent.TrayIcon.dll"
}

if (-not (Test-Path $trayExePath)) {
    Write-Host "ERROR: Tray monitor executable not found at: $trayExePath" -ForegroundColor Red
    Write-Host "Please build the TrayIcon project first." -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $trayDllPath)) {
    Write-Host "WARNING: Tray monitor DLL not found at: $trayDllPath" -ForegroundColor Yellow
    Write-Host "The tray monitor may not start without its dependencies." -ForegroundColor Yellow
}

# Install as scheduled task
if ($Install) {
    Write-Host "Installing auto-start tray monitor..." -ForegroundColor Cyan
    
    $taskName = "ThreatStopper-TrayMonitor-AutoStart"
    $scriptFullPath = $MyInvocation.MyCommand.Path
    
    # Remove existing task if it exists
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
    
    # Create scheduled task that runs on user login
    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-WindowStyle Hidden -ExecutionPolicy Bypass -File `"$scriptFullPath`""
    $trigger = New-ScheduledTaskTrigger -AtLogOn
    $principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -LogonType Interactive -RunLevel Limited
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -RunOnlyIfNetworkAvailable:$false
    
    Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description "Automatically starts ThreatStopper Tray Monitor when service is running" | Out-Null
    
    Write-Host "Auto-start tray monitor installed as scheduled task" -ForegroundColor Green
    Write-Host "  The tray monitor will start automatically when you log in." -ForegroundColor Cyan
    exit 0
}

# Uninstall scheduled task
if ($Uninstall) {
    Write-Host "Uninstalling auto-start tray monitor..." -ForegroundColor Cyan
    
    $taskName = "ThreatStopper-TrayMonitor-AutoStart"
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
    
    Write-Host "Auto-start tray monitor uninstalled" -ForegroundColor Green
    exit 0
}

# Main monitoring loop
Write-Host "ThreatStopper Tray Monitor Auto-Start" -ForegroundColor Cyan
Write-Host "Monitoring service status..." -ForegroundColor Yellow
Write-Host "Press Ctrl+C to stop" -ForegroundColor Gray
Write-Host ""

$trayProcess = $null
$lastServiceStatus = $null
$processName = "WindowsSecurityAgent.TrayIcon"

while ($true) {
    try {
        $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        
        if ($service) {
            $currentStatus = $service.Status
            
            # Check if status changed
            if ($lastServiceStatus -ne $currentStatus) {
                $lastServiceStatus = $currentStatus
                $timestamp = Get-Date -Format 'HH:mm:ss'
                
                if ($currentStatus -eq [System.ServiceProcess.ServiceControllerStatus]::Running) {
                    $msg = "[$timestamp] Service is running - Starting tray monitor..."
                    Write-Host $msg -ForegroundColor Green
                    
                    # Check if tray monitor is already running
                    $existingProcess = Get-Process -Name $processName -ErrorAction SilentlyContinue
                    if (-not $existingProcess) {
                        $workingDir = if (Test-Path (Join-Path $installPath "WindowsSecurityAgent.TrayIcon.exe")) { $installPath } else { $scriptPath }
                        try {
                            $proc = Start-Process -FilePath $trayExePath -WorkingDirectory $workingDir -WindowStyle Hidden -PassThru -ErrorAction Stop
                            Start-Sleep -Seconds 3
                            
                            $trayProcess = Get-Process -Name $processName -ErrorAction SilentlyContinue
                            if ($trayProcess) {
                                $msg = "[$timestamp] Tray monitor started (PID: $($trayProcess.Id))"
                                Write-Host $msg -ForegroundColor Green
                            } else {
                                $msg = "[$timestamp] Failed to start tray monitor - process exited immediately"
                                Write-Host $msg -ForegroundColor Yellow
                                Write-Host "[$timestamp] Check that all DLL dependencies are present in: $workingDir" -ForegroundColor Yellow
                            }
                        } catch {
                            $msg = "[$timestamp] Error starting tray monitor: $($_.Exception.Message)"
                            Write-Host $msg -ForegroundColor Red
                        }
                    } else {
                        $msg = "[$timestamp] Tray monitor already running (PID: $($existingProcess.Id))"
                        Write-Host $msg -ForegroundColor Cyan
                        $trayProcess = $existingProcess
                    }
                } elseif ($currentStatus -eq [System.ServiceProcess.ServiceControllerStatus]::Stopped) {
                    $msg = "[$timestamp] Service is stopped"
                    Write-Host $msg -ForegroundColor Yellow
                }
            }
            
            # Check if tray monitor is still running (restart if it died)
            if ($trayProcess -and $trayProcess.HasExited) {
                $timestamp = Get-Date -Format 'HH:mm:ss'
                $msg = "[$timestamp] Tray monitor process exited, restarting..."
                Write-Host $msg -ForegroundColor Yellow
                $trayProcess = $null
                
                if ($service.Status -eq [System.ServiceProcess.ServiceControllerStatus]::Running) {
                    $workingDir = if (Test-Path (Join-Path $installPath "WindowsSecurityAgent.TrayIcon.exe")) { $installPath } else { $scriptPath }
                    try {
                        Start-Process -FilePath $trayExePath -WorkingDirectory $workingDir -WindowStyle Hidden -ErrorAction Stop
                        Start-Sleep -Seconds 3
                        $trayProcess = Get-Process -Name $processName -ErrorAction SilentlyContinue
                    } catch {
                        $timestamp = Get-Date -Format 'HH:mm:ss'
                        $msg = "[$timestamp] Error restarting tray monitor: $($_.Exception.Message)"
                        Write-Host $msg -ForegroundColor Red
                    }
                }
            }
        } else {
            $timestamp = Get-Date -Format 'HH:mm:ss'
            $msg = "[$timestamp] Service $ServiceName not found"
            Write-Host $msg -ForegroundColor Red
        }
    }
    catch {
        $timestamp = Get-Date -Format 'HH:mm:ss'
        $errorMsg = $_.Exception.Message
        $msg = "[$timestamp] Error: $errorMsg"
        Write-Host $msg -ForegroundColor Red
    }
    
    # Check every 5 seconds
    Start-Sleep -Seconds 5
}

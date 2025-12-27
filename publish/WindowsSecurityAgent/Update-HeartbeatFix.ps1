#Requires -RunAsAdministrator

# Update agent with heartbeat interval fix

$serviceName = "WindowsSecurityAgent"
$installPath = "C:\Program Files\WindowsSecurityAgent"
$sourceExe = "C:\Users\stevi\WindowsSecurityPlatform\src\WindowsSecurityAgent\WindowsSecurityAgent.Service\bin\Release\net10.0\WindowsSecurityAgent.Service.exe"
$targetExe = Join-Path $installPath "WindowsSecurityAgent.Service.exe"

Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "Updating Agent with Heartbeat Interval Fix" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host ""

# Stop service
Write-Host "1. Stopping service..." -ForegroundColor Yellow
try {
    Stop-Service -Name $serviceName -Force -ErrorAction Stop
    Start-Sleep -Seconds 2
    Write-Host "   Service stopped" -ForegroundColor Green
} catch {
    Write-Host "   Service was already stopped" -ForegroundColor Gray
}

# Copy updated EXE
Write-Host ""
Write-Host "2. Updating executable..." -ForegroundColor Yellow
if (Test-Path $sourceExe) {
    Copy-Item -Path $sourceExe -Destination $targetExe -Force
    Write-Host "   Executable updated" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Source EXE not found: $sourceExe" -ForegroundColor Red
    Write-Host "   Please build the project first" -ForegroundColor Yellow
    exit 1
}

# Start service
Write-Host ""
Write-Host "3. Starting service..." -ForegroundColor Yellow
try {
    Start-Service -Name $serviceName -ErrorAction Stop
    Start-Sleep -Seconds 3
    $service = Get-Service -Name $serviceName
    if ($service.Status -eq "Running") {
        Write-Host "   Service started successfully!" -ForegroundColor Green
    } else {
        Write-Host "   Service status: $($service.Status)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ERROR: Failed to start service: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "Update Complete!" -ForegroundColor Green
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Heartbeat fix applied:" -ForegroundColor Cyan
Write-Host "  - Heartbeat interval now reads from appsettings.json (30 seconds)" -ForegroundColor White
Write-Host "  - Agent ID has been added to the Management API database" -ForegroundColor White
Write-Host ""
Write-Host "Note: You may need to restart the Management API for the agent to be" -ForegroundColor Yellow
Write-Host "added to the database. The heartbeat should start working within 30 seconds." -ForegroundColor Yellow
Write-Host ""


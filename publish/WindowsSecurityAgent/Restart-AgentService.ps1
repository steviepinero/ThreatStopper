# Restart Windows Security Agent Service
# This script must be run as Administrator

$serviceName = "WindowsSecurityAgent"

# Check if running as administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator', then run this script again." -ForegroundColor Yellow
    exit 1
}

try {
    Write-Host "Checking Windows Security Agent service..." -ForegroundColor Cyan
    
    $service = Get-Service -Name $serviceName -ErrorAction Stop
    
    Write-Host "Current status: $($service.Status)" -ForegroundColor Yellow
    Write-Host "Restarting service..." -ForegroundColor Cyan
    
    Restart-Service -Name $serviceName -Force
    
    Start-Sleep -Seconds 3
    
    $service.Refresh()
    
    if ($service.Status -eq "Running") {
        Write-Host "✓ Agent service restarted successfully!" -ForegroundColor Green
        Write-Host "The agent will now sync policies and update URL blocks." -ForegroundColor Cyan
    } else {
        Write-Host "⚠ Service status: $($service.Status)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "ERROR: Failed to restart service: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}


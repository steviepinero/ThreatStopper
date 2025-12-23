#Requires -RunAsAdministrator

Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host "ThreatStopper Agent - Update Script" -ForegroundColor Cyan
Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host ""

$serviceName = "WindowsSecurityAgent"
$installPath = "C:\Program Files\WindowsSecurityAgent"
$sourcePath = "C:\Users\stevi\WindowsSecurityPlatform\publish\WindowsSecurityAgent-Updated"

# Check if service exists
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Host "ERROR: Service not found. Please install the agent first." -ForegroundColor Red
    exit 1
}

# Backup current configuration
Write-Host "Backing up configuration..." -ForegroundColor Cyan
$configBackup = Get-Content "$installPath\appsettings.json"

# Stop the service
Write-Host "Stopping service..." -ForegroundColor Cyan
Stop-Service $serviceName -Force
Start-Sleep -Seconds 3

# Copy new files
Write-Host "Copying updated files..." -ForegroundColor Cyan
Copy-Item "$sourcePath\WindowsSecurityAgent.Service.exe" "$installPath\" -Force
Copy-Item "$sourcePath\*.dll" "$installPath\" -Force -ErrorAction SilentlyContinue
Copy-Item "$sourcePath\*.pdb" "$installPath\" -Force -ErrorAction SilentlyContinue

# Restore configuration
Write-Host "Restoring configuration..." -ForegroundColor Cyan
$configBackup | Set-Content "$installPath\appsettings.json" -Force

# Start the service
Write-Host "Starting service..." -ForegroundColor Cyan
Start-Service $serviceName
Start-Sleep -Seconds 3

$service = Get-Service $serviceName
if ($service.Status -eq 'Running') {
    Write-Host "✓ ThreatStopper Agent updated and running successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "New features:" -ForegroundColor Cyan
    Write-Host "  - URL/Domain blocking support" -ForegroundColor White
    Write-Host "  - Automatic hosts file management" -ForegroundColor White
    Write-Host "  - Policy sync for URL rules" -ForegroundColor White
} else {
    Write-Host "Warning Service status: $($service.Status)" -ForegroundColor Yellow
    Write-Host "Check Event Viewer for errors" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host "Update Complete!" -ForegroundColor Green
Write-Host "=====================================================================" -ForegroundColor Cyan


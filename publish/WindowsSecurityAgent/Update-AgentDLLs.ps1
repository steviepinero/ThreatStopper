# Update-AgentDLLs.ps1
# Updates the Windows Security Agent DLLs with the latest build
# MUST BE RUN AS ADMINISTRATOR

param(
    [switch]$SkipServiceRestart
)

Write-Host "=== Windows Security Agent DLL Update ===" -ForegroundColor Cyan
Write-Host ""

# Check if running as administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

# Get service path
$service = Get-WmiObject Win32_Service -Filter "Name='WindowsSecurityAgent'" -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Host "ERROR: Windows Security Agent service not found!" -ForegroundColor Red
    Write-Host "The service may not be installed." -ForegroundColor Yellow
    exit 1
}

$servicePath = Split-Path $service.PathName.Trim('"')
Write-Host "Service installation path: $servicePath" -ForegroundColor Gray
Write-Host ""

# Source paths (adjust if your source code is in a different location)
$projectRoot = Split-Path (Split-Path $PSScriptRoot)
$sourceCoreDll = Join-Path $projectRoot "src\WindowsSecurityAgent\WindowsSecurityAgent.Core\bin\Debug\net10.0\WindowsSecurityAgent.Core.dll"
$sourceServiceDll = Join-Path $projectRoot "src\WindowsSecurityAgent\WindowsSecurityAgent.Service\bin\Debug\net10.0\WindowsSecurityAgent.Service.dll"

# Check if source files exist
if (-not (Test-Path $sourceCoreDll)) {
    Write-Host "ERROR: Source DLL not found: $sourceCoreDll" -ForegroundColor Red
    Write-Host "Please build the project first: dotnet build" -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $sourceServiceDll)) {
    Write-Host "ERROR: Source DLL not found: $sourceServiceDll" -ForegroundColor Red
    Write-Host "Please build the project first: dotnet build" -ForegroundColor Yellow
    exit 1
}

# Stop the service if it's running
$serviceStatus = (Get-Service -Name "WindowsSecurityAgent").Status
if ($serviceStatus -eq "Running") {
    Write-Host "Stopping Windows Security Agent service..." -ForegroundColor Cyan
    Stop-Service -Name "WindowsSecurityAgent" -Force -ErrorAction Stop
    Start-Sleep -Seconds 2
    Write-Host "✓ Service stopped" -ForegroundColor Green
} else {
    Write-Host "Service is already stopped" -ForegroundColor Gray
}

# Backup existing DLLs
Write-Host ""
Write-Host "Creating backup of existing DLLs..." -ForegroundColor Cyan
$backupPath = Join-Path $servicePath "Backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
New-Item -ItemType Directory -Path $backupPath -Force | Out-Null

if (Test-Path "$servicePath\WindowsSecurityAgent.Core.dll") {
    Copy-Item "$servicePath\WindowsSecurityAgent.Core.dll" -Destination "$backupPath\WindowsSecurityAgent.Core.dll" -Force
    Write-Host "  ✓ Backed up WindowsSecurityAgent.Core.dll" -ForegroundColor Gray
}

if (Test-Path "$servicePath\WindowsSecurityAgent.Service.dll") {
    Copy-Item "$servicePath\WindowsSecurityAgent.Service.dll" -Destination "$backupPath\WindowsSecurityAgent.Service.dll" -Force
    Write-Host "  ✓ Backed up WindowsSecurityAgent.Service.dll" -ForegroundColor Gray
}

# Copy new DLLs
Write-Host ""
Write-Host "Copying updated DLLs..." -ForegroundColor Cyan
try {
    Copy-Item $sourceCoreDll -Destination "$servicePath\WindowsSecurityAgent.Core.dll" -Force -ErrorAction Stop
    Write-Host "  ✓ Updated WindowsSecurityAgent.Core.dll" -ForegroundColor Green
    
    Copy-Item $sourceServiceDll -Destination "$servicePath\WindowsSecurityAgent.Service.dll" -Force -ErrorAction Stop
    Write-Host "  ✓ Updated WindowsSecurityAgent.Service.dll" -ForegroundColor Green
} catch {
    Write-Host "  ✗ Failed to copy DLLs: $_" -ForegroundColor Red
    Write-Host "  Restoring from backup..." -ForegroundColor Yellow
    
    # Restore from backup
    if (Test-Path "$backupPath\WindowsSecurityAgent.Core.dll") {
        Copy-Item "$backupPath\WindowsSecurityAgent.Core.dll" -Destination "$servicePath\WindowsSecurityAgent.Core.dll" -Force
    }
    if (Test-Path "$backupPath\WindowsSecurityAgent.Service.dll") {
        Copy-Item "$backupPath\WindowsSecurityAgent.Service.dll" -Destination "$servicePath\WindowsSecurityAgent.Service.dll" -Force
    }
    
    exit 1
}

# Restart the service
if (-not $SkipServiceRestart) {
    Write-Host ""
    Write-Host "Starting Windows Security Agent service..." -ForegroundColor Cyan
    try {
        Start-Service -Name "WindowsSecurityAgent" -ErrorAction Stop
        Start-Sleep -Seconds 2
        $newStatus = (Get-Service -Name "WindowsSecurityAgent").Status
        if ($newStatus -eq "Running") {
            Write-Host "✓ Service started successfully" -ForegroundColor Green
        } else {
            Write-Host "⚠ Service status: $newStatus" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "✗ Failed to start service: $_" -ForegroundColor Red
        Write-Host "  You may need to start it manually: Start-Service WindowsSecurityAgent" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=== Update Complete ===" -ForegroundColor Green
Write-Host "Backup location: $backupPath" -ForegroundColor Gray
Write-Host ""
Write-Host "The URL blocking fix for facebook.com has been deployed!" -ForegroundColor Cyan
Write-Host "The service will sync policies within 10 minutes, or restart it for immediate sync." -ForegroundColor Gray


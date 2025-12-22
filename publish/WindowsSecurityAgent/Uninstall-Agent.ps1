#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Uninstalls Windows Security Agent Windows Service

.DESCRIPTION
    This script stops and removes the Windows Security Agent service.
    Optionally removes installation files and cache.

.PARAMETER ServiceName
    Name of the Windows Service (default: WindowsSecurityAgent)

.PARAMETER RemoveFiles
    Whether to remove installation files (default: $false)

.PARAMETER RemoveCache
    Whether to remove cache directory (default: $false)

.EXAMPLE
    .\Uninstall-Agent.ps1
    
.EXAMPLE
    .\Uninstall-Agent.ps1 -RemoveFiles $true -RemoveCache $true
#>

param(
    [string]$ServiceName = "WindowsSecurityAgent",
    [bool]$RemoveFiles = $false,
    [bool]$RemoveCache = $false
)

$ErrorActionPreference = "Stop"

function Write-Success { Write-Host $args -ForegroundColor Green }
function Write-Info { Write-Host $args -ForegroundColor Cyan }
function Write-Warning { Write-Host $args -ForegroundColor Yellow }
function Write-Error { Write-Host $args -ForegroundColor Red }

Write-Info "======================================================================"
Write-Info "Windows Security Agent - Uninstallation Script"
Write-Info "======================================================================"
Write-Host ""

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "ERROR: This script must be run as Administrator!"
    exit 1
}

Write-Success "✓ Running as Administrator"

# Check if service exists
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Warning "Service '$ServiceName' not found. Nothing to uninstall."
    exit 0
}

Write-Info "Found service: $ServiceName"

# Stop the service if running
if ($service.Status -eq 'Running') {
    Write-Info "Stopping service..."
    try {
        Stop-Service -Name $ServiceName -Force -ErrorAction Stop
        Start-Sleep -Seconds 2
        Write-Success "✓ Service stopped"
    } catch {
        Write-Warning "⚠ Failed to stop service: $_"
        Write-Warning "  Attempting to continue..."
    }
}

# Remove the service
Write-Info "Removing service..."
try {
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
    Write-Success "✓ Service removed"
} catch {
    Write-Error "ERROR: Failed to remove service: $_"
    exit 1
}

# Remove installation files if requested
if ($RemoveFiles) {
    $installPath = "C:\Program Files\WindowsSecurityAgent"
    if (Test-Path $installPath) {
        Write-Info "Removing installation files from $installPath..."
        try {
            Remove-Item -Path $installPath -Recurse -Force -ErrorAction Stop
            Write-Success "✓ Installation files removed"
        } catch {
            Write-Warning "⚠ Failed to remove installation files: $_"
        }
    }
}

# Remove cache if requested
if ($RemoveCache) {
    $cacheDir = "C:\ProgramData\WindowsSecurityAgent"
    if (Test-Path $cacheDir) {
        Write-Info "Removing cache directory from $cacheDir..."
        try {
            Remove-Item -Path $cacheDir -Recurse -Force -ErrorAction Stop
            Write-Success "✓ Cache directory removed"
        } catch {
            Write-Warning "⚠ Failed to remove cache directory: $_"
        }
    }
}

Write-Host ""
Write-Success "Uninstallation completed successfully!"
Write-Host ""

if (-not $RemoveFiles) {
    Write-Info "Note: Installation files were not removed."
    Write-Info "To remove files, run: .\Uninstall-Agent.ps1 -RemoveFiles `$true"
}

if (-not $RemoveCache) {
    Write-Info "Note: Cache directory was not removed."
    Write-Info "To remove cache, run: .\Uninstall-Agent.ps1 -RemoveCache `$true"
}


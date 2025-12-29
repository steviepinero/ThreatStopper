#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Installs Windows Security Agent as a Windows Service

.DESCRIPTION
    This script installs the Windows Security Agent as a Windows Service,
    configures it to start automatically, and starts the service.

.PARAMETER ServiceName
    Name of the Windows Service (default: WindowsSecurityAgent)

.PARAMETER InstallPath
    Installation directory (default: C:\Program Files\WindowsSecurityAgent)

.PARAMETER StartService
    Whether to start the service after installation (default: $true)

.EXAMPLE
    .\Install-Agent.ps1
    
.EXAMPLE
    .\Install-Agent.ps1 -InstallPath "C:\CustomPath\Agent" -StartService $false
#>

param(
    [string]$ServiceName = "WindowsSecurityAgent",
    [string]$InstallPath = "C:\Program Files\WindowsSecurityAgent",
    [bool]$StartService = $true
)

$ErrorActionPreference = "Stop"

# Colors for output
function Write-Success { Write-Host $args -ForegroundColor Green }
function Write-Info { Write-Host $args -ForegroundColor Cyan }
function Write-Warning { Write-Host $args -ForegroundColor Yellow }
function Write-Error { Write-Host $args -ForegroundColor Red }

Write-Info "======================================================================"
Write-Info "Windows Security Agent - Installation Script"
Write-Info "======================================================================"
Write-Host ""

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "ERROR: This script must be run as Administrator!"
    Write-Error "Right-click PowerShell and select 'Run as Administrator'"
    exit 1
}

Write-Success "CheckMark Running as Administrator"

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ExePath = Join-Path $ScriptDir "WindowsSecurityAgent.Service.exe"

# Verify executable exists
if (-not (Test-Path $ExePath)) {
    Write-Error "ERROR: WindowsSecurityAgent.Service.exe not found in $ScriptDir"
    Write-Error "Please ensure all files are present before installation."
    exit 1
}

Write-Success "CheckMark Agent executable found"

# Check if service already exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Warning "Service '$ServiceName' already exists. Stopping and removing..."
    
    if ($existingService.Status -eq 'Running') {
        Stop-Service -Name $ServiceName -Force
        Write-Info "  Stopped existing service"
    }
    
    # Remove service
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
    Write-Success "CheckMark Removed existing service"
}

# Create installation directory if it doesn't exist
if (-not (Test-Path $InstallPath)) {
    Write-Info "Creating installation directory: $InstallPath"
    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
    Write-Success "CheckMark Created installation directory"
}

# Copy files to installation directory
Write-Info "Copying agent files to $InstallPath..."
try {
    Copy-Item -Path "$ScriptDir\*" -Destination $InstallPath -Recurse -Force
    Write-Success "CheckMark Files copied successfully"
} catch {
    Write-Error "ERROR: Failed to copy files: $_"
    exit 1
}

# Update executable path
$ExePath = Join-Path $InstallPath "WindowsSecurityAgent.Service.exe"

# Create cache directory
$cacheDir = "C:\ProgramData\WindowsSecurityAgent"
if (-not (Test-Path $cacheDir)) {
    Write-Info "Creating cache directory: $cacheDir"
    New-Item -ItemType Directory -Path $cacheDir -Force | Out-Null
    Write-Success "CheckMark Created cache directory"
}

# Install the service
Write-Info "Installing Windows Service..."
try {
    New-Service -Name $ServiceName -BinaryPathName $ExePath -DisplayName "ThreatStopper Agent" -Description "Advanced endpoint protection - monitors and enforces security policies for application installations and process execution" -StartupType Automatic -ErrorAction Stop | Out-Null
    
    Write-Success "CheckMark Service installed successfully"
} catch {
    Write-Error "ERROR: Failed to install service: $_"
    exit 1
}

# Configure service recovery options (restart on failure)
Write-Info "Configuring service recovery options..."
sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
Write-Success "CheckMark Service recovery configured"

# Start the service if requested
if ($StartService) {
    Write-Info "Starting Windows Security Agent service..."
    try {
        Start-Service -Name $ServiceName -ErrorAction Stop
        Start-Sleep -Seconds 3
        
        $service = Get-Service -Name $ServiceName
        if ($service.Status -eq 'Running') {
            Write-Success "CheckMark Service started successfully"
        } else {
            Write-Warning "Warning Service installed but not running. Status: $($service.Status)"
            Write-Warning "  Check Event Viewer for errors"
        }
    } catch {
        Write-Warning "Warning Service installed but failed to start: $_"
        Write-Warning "  This may be due to configuration issues."
        Write-Warning "  Please check appsettings.json and try: Start-Service $ServiceName"
    }
}

# Optionally install auto-start tray monitor
Write-Host ""
Write-Info "Installing auto-start tray monitor..."
$autoStartScript = Join-Path $ScriptDir "Start-TrayMonitor-Auto.ps1"
if (Test-Path $autoStartScript) {
    try {
        # Run as current user (not admin) since scheduled task needs user context
        $currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
        Write-Info "  Installing scheduled task for user: $currentUser"
        & powershell.exe -ExecutionPolicy Bypass -File $autoStartScript -Install
        Write-Success "CheckMark Auto-start tray monitor installed"
        Write-Info "  The tray monitor will start automatically when the service is running."
    } catch {
        Write-Warning "Warning Failed to install auto-start tray monitor: $_"
        Write-Warning "  You can manually install it later by running:"
        Write-Warning "  .\Start-TrayMonitor-Auto.ps1 -Install"
    }
} else {
    Write-Warning "Warning Auto-start script not found at: $autoStartScript"
    Write-Warning "  Skipping auto-start tray monitor installation..."
}

# Display final status
Write-Host ""
Write-Info "======================================================================"
Write-Success "Installation Complete!"
Write-Info "======================================================================"
Write-Host ""
Write-Info "Service Name: $ServiceName"
Write-Info "Install Path: $InstallPath"
Write-Info "Cache Directory: $cacheDir"
Write-Host ""

# Check configuration
$configPath = Join-Path $InstallPath "appsettings.json"
$config = Get-Content $configPath | ConvertFrom-Json

if ([string]::IsNullOrWhiteSpace($config.Agent.AgentId) -or [string]::IsNullOrWhiteSpace($config.Agent.ApiKey) -or [string]::IsNullOrWhiteSpace($config.Agent.EncryptionKey)) {
    
    Write-Warning "Warning CONFIGURATION REQUIRED!"
    Write-Warning "  Please edit: $configPath"
    Write-Warning "  Configure:"
    Write-Warning "    - CloudApi.BaseUrl (Management API URL)"
    Write-Warning "    - Agent.AgentId (Generate new GUID)"
    Write-Warning "    - Agent.ApiKey (Generate secure key)"
    Write-Warning "    - Agent.EncryptionKey (Generate 32-byte key)"
    Write-Host ""
    Write-Warning "  After configuration, restart the service:"
    Write-Warning "    Restart-Service $ServiceName"
    Write-Host ""
}

Write-Info "Useful Commands:"
Write-Info "  Check Status:  Get-Service $ServiceName"
Write-Info "  Start Service: Start-Service $ServiceName"
Write-Info "  Stop Service:  Stop-Service $ServiceName"
Write-Info "  View Logs:     Get-EventLog -LogName Application -Source WindowsSecurityAgent -Newest 20"
Write-Info "  Uninstall:     .\Uninstall-Agent.ps1"
Write-Host ""

# Check if tray monitor exists
$trayExePath = Join-Path $InstallPath "WindowsSecurityAgent.TrayIcon.exe"
if (Test-Path $trayExePath) {
    Write-Info "Tray Monitor Available:"
    Write-Info "  Start Tray Icon:  .\Start-TrayMonitor.ps1"
    Write-Info "  Add to Startup:   .\Start-TrayMonitor.ps1 -InstallStartup"
    Write-Info "  The tray icon shows service status and allows quick control"
    Write-Host ""
}

Write-Success "Installation script completed successfully!"

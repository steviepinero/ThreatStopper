#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Configures the Windows Security Agent for localhost testing

.DESCRIPTION
    This script updates the agent configuration to connect to a local Management API

.EXAMPLE
    .\Configure-Agent.ps1
#>

$ErrorActionPreference = "Stop"

Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host "ThreatStopper Agent - Configuration Script" -ForegroundColor Cyan
Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Red
    exit 1
}

$configPath = "C:\Program Files\WindowsSecurityAgent\appsettings.json"

if (-not (Test-Path $configPath)) {
    Write-Host "ERROR: Configuration file not found at: $configPath" -ForegroundColor Red
    Write-Host "Please ensure the agent is installed first." -ForegroundColor Red
    exit 1
}

Write-Host "Configuring agent for localhost testing..." -ForegroundColor Cyan
Write-Host ""

$configContent = @"
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "CloudApi": {
    "BaseUrl": "http://localhost:5140",
    "SkipCertificateValidation": false
  },
  "Agent": {
    "AgentId": "22222222-2222-2222-2222-222222222222",
    "ApiKey": "test-api-key-12345",
    "EncryptionKey": "mgROUGiiMBmUsTIuGUk1wu2HM9GRPb2g2drSiPBpWNA=",
    "CacheDirectory": "C:\\ProgramData\\WindowsSecurityAgent",
    "HeartbeatIntervalSeconds": 300,
    "PolicySyncIntervalSeconds": 600,
    "AuditReportBatchSize": 100
  },
  "Monitoring": {
    "EnableProcessMonitoring": true,
    "EnableFileSystemMonitoring": true,
    "MonitoredPaths": [
      "C:\\Program Files",
      "C:\\Program Files (x86)",
      "C:\\Windows\\System32"
    ]
  }
}
"@

try {
    [System.IO.File]::WriteAllText($configPath, $configContent)
    Write-Host "CheckMark Configuration updated successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Configuration Details:" -ForegroundColor Cyan
    Write-Host "  API URL: http://localhost:5140" -ForegroundColor White
    Write-Host "  Agent ID: 22222222-2222-2222-2222-222222222222" -ForegroundColor White
    Write-Host "  Encryption Key: Generated" -ForegroundColor White
    Write-Host ""
    
    # Try to start the service
    Write-Host "Starting Windows Security Agent service..." -ForegroundColor Cyan
    Start-Service WindowsSecurityAgent -ErrorAction Stop
    Start-Sleep -Seconds 3
    
    $service = Get-Service WindowsSecurityAgent
    if ($service.Status -eq 'Running') {
        Write-Host "CheckMark Service started successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "The agent is now connected to your local Management API" -ForegroundColor Green
        Write-Host "Check the Admin Portal at http://localhost:3000 to see the agent online" -ForegroundColor Green
    } else {
        Write-Host "Warning Service status: $($service.Status)" -ForegroundColor Yellow
        Write-Host "Try starting manually: Start-Service WindowsSecurityAgent" -ForegroundColor Yellow
    }
} catch {
    Write-Host "ERROR: Failed to update configuration: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host "Configuration Complete!" -ForegroundColor Green
Write-Host "=====================================================================" -ForegroundColor Cyan


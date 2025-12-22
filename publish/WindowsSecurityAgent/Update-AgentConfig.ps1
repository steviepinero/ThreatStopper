#Requires -RunAsAdministrator

Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host "Updating Agent Configuration with Unique ID" -ForegroundColor Cyan
Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host ""

$configPath = "C:\Program Files\WindowsSecurityAgent\appsettings.json"

# Stop the service
Write-Host "Stopping Windows Security Agent service..." -ForegroundColor Cyan
Stop-Service WindowsSecurityAgent -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

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
    "AgentId": "e432ffa3-d4ae-434e-b128-98290d052cfa",
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

[System.IO.File]::WriteAllText($configPath, $configContent)
Write-Host "CheckMark Configuration updated with new Agent ID: e432ffa3-d4ae-434e-b128-98290d052cfa" -ForegroundColor Green
Write-Host ""

# Start the service
Write-Host "Starting Windows Security Agent service..." -ForegroundColor Cyan
Start-Service WindowsSecurityAgent
Start-Sleep -Seconds 3

$service = Get-Service WindowsSecurityAgent
if ($service.Status -eq 'Running') {
    Write-Host "CheckMark Service started successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Your machine (FOOTBALLHEAD) will now appear as a new agent in the portal" -ForegroundColor Green
    Write-Host "Check http://localhost:3000/agents in about 1 minute" -ForegroundColor Green
} else {
    Write-Host "Warning Service status: $($service.Status)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host "Update Complete!" -ForegroundColor Green
Write-Host "=====================================================================" -ForegroundColor Cyan


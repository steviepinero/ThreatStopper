#Requires -RunAsAdministrator

# Complete fix for service startup issues

$serviceName = "WindowsSecurityAgent"
$installPath = "C:\Program Files\WindowsSecurityAgent"
$configPath = Join-Path $installPath "appsettings.json"

Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "Windows Security Agent - Complete Fix" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Stop service
Write-Host "1. Stopping service..." -ForegroundColor Yellow
try {
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    Write-Host "   Service stopped" -ForegroundColor Green
} catch {
    Write-Host "   Service was already stopped" -ForegroundColor Gray
}

# Step 2: Fix configuration
Write-Host ""
Write-Host "2. Fixing configuration..." -ForegroundColor Yellow
if (Test-Path $configPath) {
    $jsonContent = Get-Content $configPath -Raw
    $config = $jsonContent | ConvertFrom-Json
    
    $changed = $false
    
    if ([string]::IsNullOrWhiteSpace($config.Agent.AgentId)) {
        $config.Agent.AgentId = [Guid]::NewGuid().ToString()
        Write-Host "   Generated AgentId" -ForegroundColor Green
        $changed = $true
    }
    
    if ([string]::IsNullOrWhiteSpace($config.Agent.ApiKey)) {
        $config.Agent.ApiKey = "agent-key-" + [Guid]::NewGuid().ToString().Substring(0, 8)
        Write-Host "   Generated API Key" -ForegroundColor Green
        $changed = $true
    }
    
    if ([string]::IsNullOrWhiteSpace($config.Agent.EncryptionKey)) {
        $bytes = New-Object byte[] 32
        $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
        $rng.GetBytes($bytes)
        $config.Agent.EncryptionKey = [Convert]::ToBase64String($bytes)
        $rng.Dispose()
        Write-Host "   Generated Encryption Key" -ForegroundColor Green
        $changed = $true
    }
    
    if ($config.CloudApi.BaseUrl -eq "https://YOUR_API_URL_HERE") {
        $config.CloudApi.BaseUrl = "http://localhost:5140"
        Write-Host "   Set API URL to http://localhost:5140" -ForegroundColor Yellow
        $changed = $true
    }
    
    if ($changed) {
        $json = $config | ConvertTo-Json -Depth 10
        [System.IO.File]::WriteAllText($configPath, $json, [System.Text.Encoding]::UTF8)
        Write-Host "   Configuration saved" -ForegroundColor Green
    } else {
        Write-Host "   Configuration is valid" -ForegroundColor Green
    }
} else {
    Write-Host "   ERROR: Config file not found!" -ForegroundColor Red
    exit 1
}

# Step 3: Copy updated executable
Write-Host ""
Write-Host "3. Updating executable..." -ForegroundColor Yellow
$sourceExe = "C:\Users\stevi\WindowsSecurityPlatform\src\WindowsSecurityAgent\WindowsSecurityAgent.Service\bin\Release\net10.0\WindowsSecurityAgent.Service.exe"
$targetExe = Join-Path $installPath "WindowsSecurityAgent.Service.exe"

if (Test-Path $sourceExe) {
    Copy-Item -Path $sourceExe -Destination $targetExe -Force
    Write-Host "   Executable updated" -ForegroundColor Green
} else {
    Write-Host "   Source executable not found, skipping" -ForegroundColor Yellow
}

# Step 4: Start service
Write-Host ""
Write-Host "4. Starting service..." -ForegroundColor Yellow
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
    Write-Host "   Check Event Viewer for details" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "Fix Complete!" -ForegroundColor Green
Write-Host "======================================================================" -ForegroundColor Cyan


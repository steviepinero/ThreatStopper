#Requires -RunAsAdministrator

# Fix service startup issues by updating the executable and configuration

Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "Windows Security Agent - Service Startup Fix" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host ""

$serviceName = "WindowsSecurityAgent"
$installPath = "C:\Program Files\WindowsSecurityAgent"
$configPath = Join-Path $installPath "appsettings.json"

# Step 1: Stop the service
Write-Host "1. Stopping service..." -ForegroundColor Yellow
try {
    $service = Get-Service -Name $serviceName -ErrorAction Stop
    if ($service.Status -eq "Running") {
        Stop-Service -Name $serviceName -Force
        Start-Sleep -Seconds 3
        Write-Host "   ✓ Service stopped" -ForegroundColor Green
    } else {
        Write-Host "   ✓ Service already stopped" -ForegroundColor Green
    }
} catch {
    Write-Host "   ⚠ Could not stop service: $_" -ForegroundColor Yellow
}

# Step 2: Update configuration with valid values
Write-Host ""
Write-Host "2. Checking configuration..." -ForegroundColor Yellow
if (Test-Path $configPath) {
    $config = Get-Content $configPath | ConvertFrom-Json
    $needsUpdate = $false
    
    # Generate AgentId if missing
    if ([string]::IsNullOrWhiteSpace($config.Agent.AgentId)) {
        $config.Agent.AgentId = [Guid]::NewGuid().ToString()
        $needsUpdate = $true
        Write-Host "   ✓ Generated new AgentId: $($config.Agent.AgentId)" -ForegroundColor Green
    }
    
    # Generate API Key if missing
    if ([string]::IsNullOrWhiteSpace($config.Agent.ApiKey)) {
        $config.Agent.ApiKey = "agent-key-" + [Guid]::NewGuid().ToString().Substring(0, 8)
        $needsUpdate = $true
        Write-Host "   ✓ Generated new API Key" -ForegroundColor Green
    }
    
    # Generate Encryption Key if missing
    if ([string]::IsNullOrWhiteSpace($config.Agent.EncryptionKey)) {
        $bytes = New-Object byte[] 32
        [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
        $config.Agent.EncryptionKey = [Convert]::ToBase64String($bytes)
        $needsUpdate = $true
        Write-Host "   ✓ Generated new Encryption Key" -ForegroundColor Green
    }
    
    # Update API URL if still placeholder
    if ($config.CloudApi.BaseUrl -eq "https://YOUR_API_URL_HERE") {
        $config.CloudApi.BaseUrl = "http://localhost:5140"
        $needsUpdate = $true
        Write-Host "   ⚠ Set API URL to http://localhost:5140 (update if needed)" -ForegroundColor Yellow
    }
    
    if ($needsUpdate) {
        $json = $config | ConvertTo-Json -Depth 10
        [System.IO.File]::WriteAllText($configPath, $json)
        Write-Host "   ✓ Configuration updated" -ForegroundColor Green
    } else {
        Write-Host "   ✓ Configuration is valid" -ForegroundColor Green
    }
} else {
    Write-Host "   ✗ Configuration file not found at: $configPath" -ForegroundColor Red
    Write-Host "   Please ensure the agent is installed correctly." -ForegroundColor Yellow
    exit 1
}

# Step 3: Copy updated executable (if building from source)
Write-Host ""
Write-Host "3. Checking for updated executable..." -ForegroundColor Yellow
$sourceExe = "C:\Users\stevi\WindowsSecurityPlatform\src\WindowsSecurityAgent\WindowsSecurityAgent.Service\bin\Release\net10.0\WindowsSecurityAgent.Service.exe"
$targetExe = Join-Path $installPath "WindowsSecurityAgent.Service.exe"

if (Test-Path $sourceExe) {
    $sourceTime = (Get-Item $sourceExe).LastWriteTime
    $targetTime = if (Test-Path $targetExe) { (Get-Item $targetExe).LastWriteTime } else { [DateTime]::MinValue }
    
    if ($sourceTime -gt $targetTime) {
        Write-Host "   Updating executable..." -ForegroundColor Cyan
        Copy-Item -Path $sourceExe -Destination $targetExe -Force
        Write-Host "   ✓ Executable updated" -ForegroundColor Green
    } else {
        Write-Host "   ✓ Executable is up to date" -ForegroundColor Green
    }
} else {
    Write-Host "   ⚠ Source executable not found, skipping update" -ForegroundColor Yellow
    Write-Host "   (This is normal if not building from source)" -ForegroundColor Gray
}

# Step 4: Start the service
Write-Host ""
Write-Host "4. Starting service..." -ForegroundColor Yellow
try {
    Start-Service -Name $serviceName -ErrorAction Stop
    Start-Sleep -Seconds 5
    
    $service = Get-Service -Name $serviceName
    if ($service.Status -eq "Running") {
        Write-Host "   ✓ Service started successfully!" -ForegroundColor Green
    } else {
        Write-Host "   ⚠ Service status: $($service.Status)" -ForegroundColor Yellow
        Write-Host "   Check Event Viewer for errors" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ✗ Failed to start service: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "   Troubleshooting steps:" -ForegroundColor Yellow
    Write-Host "   1. Check Event Viewer → Application → ThreatStopper" -ForegroundColor White
    Write-Host "   2. Verify configuration: $configPath" -ForegroundColor White
    Write-Host "   3. Run: .\Diagnose-Service.ps1" -ForegroundColor White
    exit 1
}

Write-Host ""
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "Service Fix Complete!" -ForegroundColor Green
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Service Status: $((Get-Service $serviceName).Status)" -ForegroundColor Cyan
Write-Host "Configuration: $configPath" -ForegroundColor Cyan
Write-Host ""


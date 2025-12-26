#Requires -RunAsAdministrator

# Simple script to update configuration values

$configPath = "C:\Program Files\WindowsSecurityAgent\appsettings.json"

if (-not (Test-Path $configPath)) {
    Write-Host "Configuration file not found: $configPath" -ForegroundColor Red
    exit 1
}

Write-Host "Reading configuration..." -ForegroundColor Cyan
$jsonContent = Get-Content $configPath -Raw
$config = $jsonContent | ConvertFrom-Json

$needsUpdate = $false

# Generate AgentId if missing
if ([string]::IsNullOrWhiteSpace($config.Agent.AgentId)) {
    $config.Agent.AgentId = [Guid]::NewGuid().ToString()
    Write-Host "Generated new AgentId: $($config.Agent.AgentId)" -ForegroundColor Green
    $needsUpdate = $true
}

# Generate API Key if missing
if ([string]::IsNullOrWhiteSpace($config.Agent.ApiKey)) {
    $config.Agent.ApiKey = "agent-key-" + [Guid]::NewGuid().ToString().Substring(0, 8)
    Write-Host "Generated new API Key" -ForegroundColor Green
    $needsUpdate = $true
}

# Generate Encryption Key if missing
if ([string]::IsNullOrWhiteSpace($config.Agent.EncryptionKey)) {
    $bytes = New-Object byte[] 32
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($bytes)
    $config.Agent.EncryptionKey = [Convert]::ToBase64String($bytes)
    $rng.Dispose()
    Write-Host "Generated new Encryption Key" -ForegroundColor Green
    $needsUpdate = $true
}

# Update API URL if still placeholder
if ($config.CloudApi.BaseUrl -eq "https://YOUR_API_URL_HERE") {
    $config.CloudApi.BaseUrl = "http://localhost:5140"
    Write-Host "Set API URL to http://localhost:5140" -ForegroundColor Yellow
    $needsUpdate = $true
}

if ($needsUpdate) {
    Write-Host "Writing updated configuration..." -ForegroundColor Cyan
    $json = $config | ConvertTo-Json -Depth 10
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($configPath, $json, $utf8NoBom)
    Write-Host "Configuration updated successfully!" -ForegroundColor Green
} else {
    Write-Host "Configuration is already valid" -ForegroundColor Green
}


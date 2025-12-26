#Requires -RunAsAdministrator

# Diagnostic script for Windows Security Agent service issues

Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "Windows Security Agent - Service Diagnostics" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host ""

$serviceName = "WindowsSecurityAgent"
$installPath = "C:\Program Files\WindowsSecurityAgent"
$configPath = Join-Path $installPath "appsettings.json"

# Check 1: Service exists
Write-Host "1. Checking service installation..." -ForegroundColor Yellow
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "   ✓ Service is installed" -ForegroundColor Green
    Write-Host "   Status: $($service.Status)" -ForegroundColor Cyan
    Write-Host "   Start Type: $($service.StartType)" -ForegroundColor Cyan
} else {
    Write-Host "   ✗ Service is NOT installed" -ForegroundColor Red
    Write-Host "   Run Install-Agent.ps1 to install the service" -ForegroundColor Yellow
    exit 1
}

# Check 2: Executable exists
Write-Host ""
Write-Host "2. Checking service executable..." -ForegroundColor Yellow
$exePath = Join-Path $installPath "WindowsSecurityAgent.Service.exe"
if (Test-Path $exePath) {
    Write-Host "   ✓ Executable found" -ForegroundColor Green
    $exeInfo = Get-Item $exePath
    Write-Host "   Path: $exePath" -ForegroundColor Cyan
    Write-Host "   Size: $([math]::Round($exeInfo.Length / 1MB, 2)) MB" -ForegroundColor Cyan
} else {
    Write-Host "   ✗ Executable NOT found" -ForegroundColor Red
    Write-Host "   Expected: $exePath" -ForegroundColor Yellow
}

# Check 3: Configuration file
Write-Host ""
Write-Host "3. Checking configuration..." -ForegroundColor Yellow
if (Test-Path $configPath) {
    Write-Host "   ✓ Configuration file found" -ForegroundColor Green
    try {
        $config = Get-Content $configPath | ConvertFrom-Json
        
        $issues = @()
        
        # Check API URL
        if ([string]::IsNullOrWhiteSpace($config.CloudApi.BaseUrl) -or 
            $config.CloudApi.BaseUrl -eq "https://YOUR_API_URL_HERE") {
            $issues += "API URL is not configured (must be set to Management API URL)"
        } else {
            Write-Host "   ✓ API URL: $($config.CloudApi.BaseUrl)" -ForegroundColor Green
        }
        
        # Check Agent ID
        if ([string]::IsNullOrWhiteSpace($config.Agent.AgentId)) {
            $issues += "Agent ID is not set (required)"
        } else {
            Write-Host "   ✓ Agent ID: $($config.Agent.AgentId)" -ForegroundColor Green
        }
        
        # Check API Key
        if ([string]::IsNullOrWhiteSpace($config.Agent.ApiKey)) {
            $issues += "API Key is not set (required)"
        } else {
            Write-Host "   ✓ API Key: Set" -ForegroundColor Green
        }
        
        # Check Encryption Key
        if ([string]::IsNullOrWhiteSpace($config.Agent.EncryptionKey)) {
            $issues += "Encryption Key is not set (required)"
        } else {
            Write-Host "   ✓ Encryption Key: Set" -ForegroundColor Green
        }
        
        if ($issues.Count -gt 0) {
            Write-Host ""
            Write-Host "   ✗ Configuration Issues Found:" -ForegroundColor Red
            foreach ($issue in $issues) {
                Write-Host "     - $issue" -ForegroundColor Red
            }
            Write-Host ""
            Write-Host "   Fix: Edit $configPath and set all required values" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "   ✗ Failed to parse configuration: $_" -ForegroundColor Red
    }
} else {
    Write-Host "   ✗ Configuration file NOT found" -ForegroundColor Red
    Write-Host "   Expected: $configPath" -ForegroundColor Yellow
}

# Check 4: Recent errors
Write-Host ""
Write-Host "4. Checking recent service errors..." -ForegroundColor Yellow
$recentErrors = Get-EventLog -LogName System -Source "Service Control Manager" -Newest 50 -ErrorAction SilentlyContinue | 
    Where-Object { $_.Message -like "*$serviceName*" -or $_.Message -like "*ThreatStopper*" } |
    Where-Object { $_.EntryType -eq "Error" } |
    Select-Object -First 3

if ($recentErrors) {
    Write-Host "   ⚠ Recent errors found:" -ForegroundColor Yellow
    foreach ($error in $recentErrors) {
        Write-Host "     [$($error.TimeGenerated)] $($error.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "   ✓ No recent service control errors" -ForegroundColor Green
}

# Check 5: Application errors
Write-Host ""
Write-Host "5. Checking application event logs..." -ForegroundColor Yellow
$appErrors = Get-EventLog -LogName Application -Newest 50 -ErrorAction SilentlyContinue |
    Where-Object { ($_.Source -eq "ThreatStopper" -or $_.Source -eq "WindowsSecurityAgent") -and $_.EntryType -eq "Error" } |
    Select-Object -First 3

if ($appErrors) {
    Write-Host "   ⚠ Recent application errors:" -ForegroundColor Yellow
    foreach ($error in $appErrors) {
        Write-Host "     [$($error.TimeGenerated)]" -ForegroundColor Cyan
        Write-Host "     $($error.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "   ✓ No recent application errors" -ForegroundColor Green
}

# Check 6: Test API connectivity (if configured)
Write-Host ""
Write-Host "6. Testing API connectivity..." -ForegroundColor Yellow
if (Test-Path $configPath) {
    try {
        $config = Get-Content $configPath | ConvertFrom-Json
        $apiUrl = $config.CloudApi.BaseUrl
        
        if ($apiUrl -and $apiUrl -ne "https://YOUR_API_URL_HERE") {
            try {
                $uri = [System.Uri]::new($apiUrl)
                $host = $uri.Host
                $port = if ($uri.Port -ne -1) { $uri.Port } else { if ($uri.Scheme -eq "https") { 443 } else { 80 } }
                
                Write-Host "   Testing connection to: $host:$port" -ForegroundColor Cyan
                $test = Test-NetConnection -ComputerName $host -Port $port -WarningAction SilentlyContinue -ErrorAction SilentlyContinue
                
                if ($test.TcpTestSucceeded) {
                    Write-Host "   ✓ API is reachable" -ForegroundColor Green
                } else {
                    Write-Host "   ✗ Cannot reach API server" -ForegroundColor Red
                    Write-Host "     Check network connectivity and firewall rules" -ForegroundColor Yellow
                }
            } catch {
                Write-Host "   ⚠ Could not test connectivity: $_" -ForegroundColor Yellow
            }
        } else {
            Write-Host "   ⚠ API URL not configured, skipping connectivity test" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "   ⚠ Could not read configuration for connectivity test" -ForegroundColor Yellow
    }
}

# Summary
Write-Host ""
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "Diagnostic Summary" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host ""

if ($service.Status -eq "Running") {
    Write-Host "✓ Service is running" -ForegroundColor Green
} else {
    Write-Host "✗ Service is NOT running (Status: $($service.Status))" -ForegroundColor Red
    Write-Host ""
    Write-Host "Common causes:" -ForegroundColor Yellow
    Write-Host "  1. Invalid or missing configuration in appsettings.json" -ForegroundColor White
    Write-Host "  2. Cannot connect to Management API" -ForegroundColor White
    Write-Host "  3. Missing dependencies (.NET Runtime)" -ForegroundColor White
    Write-Host "  4. Insufficient permissions" -ForegroundColor White
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Fix configuration: Edit $configPath" -ForegroundColor White
    Write-Host "  2. Verify API is accessible: Test-NetConnection <api-host> -Port <port>" -ForegroundColor White
    Write-Host "  3. Check Event Viewer for detailed errors" -ForegroundColor White
    Write-Host "  4. Try starting manually: Start-Service $serviceName" -ForegroundColor White
}


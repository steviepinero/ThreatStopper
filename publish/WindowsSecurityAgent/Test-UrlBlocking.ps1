# Test-UrlBlocking.ps1
# Tests and verifies URL blocking functionality

Write-Host "`n=== URL Blocking Diagnostic Tool ===" -ForegroundColor Cyan
Write-Host "This script helps diagnose URL blocking issues`n" -ForegroundColor Gray

# Check if running as administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "WARNING: Not running as Administrator. Some checks may fail." -ForegroundColor Yellow
    Write-Host "Run PowerShell as Administrator for full diagnostics.`n" -ForegroundColor Yellow
}

# 1. Check if service is running
Write-Host "1. Checking Windows Security Agent service..." -ForegroundColor Cyan
$service = Get-Service -Name "WindowsSecurityAgent" -ErrorAction SilentlyContinue
if ($service) {
    if ($service.Status -eq "Running") {
        Write-Host "   ✓ Service is running" -ForegroundColor Green
    } else {
        Write-Host "   ✗ Service is not running (Status: $($service.Status))" -ForegroundColor Red
        Write-Host "   Run: Start-Service WindowsSecurityAgent" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ✗ Service not found. Is the agent installed?" -ForegroundColor Red
}

# 2. Check hosts file permissions
Write-Host "`n2. Checking hosts file permissions..." -ForegroundColor Cyan
$hostsPath = "C:\Windows\System32\drivers\etc\hosts"
if (Test-Path $hostsPath) {
    Write-Host "   ✓ Hosts file exists" -ForegroundColor Green
    try {
        $acl = Get-Acl $hostsPath
        Write-Host "   ✓ Can read hosts file permissions" -ForegroundColor Green
    } catch {
        Write-Host "   ✗ Cannot read hosts file permissions: $_" -ForegroundColor Red
    }
} else {
    Write-Host "   ✗ Hosts file not found at: $hostsPath" -ForegroundColor Red
}

# 3. Check for blocked URLs in hosts file
Write-Host "`n3. Checking hosts file for blocked URLs..." -ForegroundColor Cyan
if (Test-Path $hostsPath) {
    $hostsContent = Get-Content $hostsPath
    $blockedSection = $false
    $blockedDomains = @()
    
    foreach ($line in $hostsContent) {
        if ($line -match "ThreatStopper.*Blocked URLs.*START") {
            $blockedSection = $true
            Write-Host "   ✓ Found blocked URLs section" -ForegroundColor Green
            continue
        }
        if ($line -match "ThreatStopper.*Blocked URLs.*END") {
            $blockedSection = $false
            break
        }
        if ($blockedSection -and $line -match "127\.0\.0\.1\s+(\S+)") {
            $domain = $matches[1]
            if ($domain -notlike "www.*") {
                $blockedDomains += $domain
            }
        }
    }
    
    if ($blockedDomains.Count -gt 0) {
        Write-Host "   ✓ Found $($blockedDomains.Count) blocked domain(s):" -ForegroundColor Green
        foreach ($domain in $blockedDomains) {
            Write-Host "     - $domain" -ForegroundColor Gray
        }
        
        # Check specifically for facebook.com
        if ($blockedDomains -contains "facebook.com") {
            Write-Host "   ✓ facebook.com is blocked" -ForegroundColor Green
        } else {
            Write-Host "   ✗ facebook.com is NOT in the blocked list" -ForegroundColor Red
            Write-Host "     This could be why it's not being blocked!" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   ✗ No blocked domains found in hosts file" -ForegroundColor Red
        Write-Host "     The agent may not have synced yet, or no URL blocking policies are active." -ForegroundColor Yellow
    }
} else {
    Write-Host "   ✗ Cannot read hosts file" -ForegroundColor Red
}

# 4. Check agent logs
Write-Host "`n4. Checking recent agent logs..." -ForegroundColor Cyan
try {
    $logs = Get-EventLog -LogName Application -Source "ThreatStopper" -Newest 20 -ErrorAction SilentlyContinue
    if ($logs) {
        $urlLogs = $logs | Where-Object { $_.Message -like "*URL*" -or $_.Message -like "*facebook*" }
        if ($urlLogs) {
            Write-Host "   ✓ Found URL-related log entries:" -ForegroundColor Green
            foreach ($log in $urlLogs | Select-Object -First 5) {
                Write-Host "     [$($log.TimeGenerated)] $($log.Message.Substring(0, [Math]::Min(80, $log.Message.Length)))..." -ForegroundColor Gray
            }
        } else {
            Write-Host "   ⚠ No URL-related log entries found in last 20 events" -ForegroundColor Yellow
        }
        
        # Check for errors
        $errors = $logs | Where-Object { $_.EntryType -eq "Error" }
        if ($errors) {
            Write-Host "   ⚠ Found errors in logs:" -ForegroundColor Yellow
            foreach ($error in $errors | Select-Object -First 3) {
                Write-Host "     [$($error.TimeGenerated)] $($error.Message.Substring(0, [Math]::Min(80, $error.Message.Length)))..." -ForegroundColor Red
            }
        }
    } else {
        Write-Host "   ⚠ No logs found for source 'ThreatStopper'" -ForegroundColor Yellow
        Write-Host "     The service may not have logged anything yet." -ForegroundColor Gray
    }
} catch {
    Write-Host "   ✗ Cannot read event logs: $_" -ForegroundColor Red
}

# 5. Test DNS resolution
Write-Host "`n5. Testing DNS resolution for facebook.com..." -ForegroundColor Cyan
try {
    $dnsResult = Resolve-DnsName -Name "facebook.com" -ErrorAction SilentlyContinue
    if ($dnsResult) {
        $ipAddresses = $dnsResult | Where-Object { $_.Type -eq "A" } | Select-Object -ExpandProperty IPAddress
        if ($ipAddresses -contains "127.0.0.1") {
            Write-Host "   ✓ facebook.com resolves to 127.0.0.1 (BLOCKED)" -ForegroundColor Green
        } else {
            Write-Host "   ✗ facebook.com resolves to: $($ipAddresses -join ', ')" -ForegroundColor Red
            Write-Host "     Expected: 127.0.0.1 (blocked)" -ForegroundColor Yellow
            Write-Host "     Try running: ipconfig /flushdns" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   ⚠ Could not resolve facebook.com" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ✗ DNS resolution test failed: $_" -ForegroundColor Red
}

# 6. Recommendations
Write-Host "`n=== Recommendations ===" -ForegroundColor Cyan
Write-Host "If facebook.com is not blocked:" -ForegroundColor Yellow
Write-Host "1. Verify the policy is active in the admin portal" -ForegroundColor White
Write-Host "2. Restart the service: Restart-Service WindowsSecurityAgent" -ForegroundColor White
Write-Host "3. Flush DNS cache: ipconfig /flushdns" -ForegroundColor White
Write-Host "4. Wait up to 10 minutes for automatic sync, or restart service for immediate sync" -ForegroundColor White
Write-Host "5. Check browser cache - try incognito/private mode" -ForegroundColor White
Write-Host "6. Verify the policy has a Domain or URL rule for 'facebook.com' with Block action" -ForegroundColor White

Write-Host "`n=== Diagnostic Complete ===" -ForegroundColor Cyan


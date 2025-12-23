# Clear URL Blocks from Hosts File
# This script removes all URL blocks added by the Windows Security Agent

param(
    [switch]$Force
)

$hostsFile = "C:\Windows\System32\drivers\etc\hosts"
$backupFile = "$hostsFile.backup.$(Get-Date -Format 'yyyyMMddHHmmss')"

# Check if running as administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator', then run this script again." -ForegroundColor Yellow
    exit 1
}

try {
    Write-Host "Reading hosts file..." -ForegroundColor Cyan
    
    # Read current hosts file
    $hostsContent = Get-Content $hostsFile -ErrorAction Stop
    
    # Find the block markers
    $startMarker = "# ThreatStopper - Blocked URLs - START"
    $endMarker = "# ThreatStopper - Blocked URLs - END"
    
    $startIndex = -1
    $endIndex = -1
    
    for ($i = 0; $i -lt $hostsContent.Length; $i++) {
        if ($hostsContent[$i] -match [regex]::Escape($startMarker)) {
            $startIndex = $i
        }
        if ($hostsContent[$i] -match [regex]::Escape($endMarker)) {
            $endIndex = $i
            break
        }
    }
    
    if ($startIndex -ge 0 -and $endIndex -ge 0) {
        Write-Host "Found blocked URLs section (lines $($startIndex + 1) to $($endIndex + 1))" -ForegroundColor Yellow
        
        # Create backup
        Copy-Item $hostsFile $backupFile -Force
        Write-Host "Backup created: $backupFile" -ForegroundColor Green
        
        # Remove the blocked section
        $newContent = $hostsContent[0..($startIndex - 1)] + $hostsContent[($endIndex + 1)..($hostsContent.Length - 1)]
        
        # Write updated content
        $newContent | Set-Content $hostsFile -Force
        
        Write-Host "Blocked URLs removed from hosts file!" -ForegroundColor Green
        
        # Flush DNS cache
        Write-Host "Flushing DNS cache..." -ForegroundColor Cyan
        ipconfig /flushdns | Out-Null
        
        Write-Host "DNS cache flushed. Blocked websites should now be accessible." -ForegroundColor Green
    }
    else {
        Write-Host "No blocked URLs section found in hosts file." -ForegroundColor Yellow
    }
}
catch {
    Write-Host "ERROR: Failed to modify hosts file: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}


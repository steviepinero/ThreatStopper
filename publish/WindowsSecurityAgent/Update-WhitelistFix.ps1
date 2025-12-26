#Requires -RunAsAdministrator

# Update agent with whitelist mode fix

$serviceName = "WindowsSecurityAgent"
$installPath = "C:\Program Files\WindowsSecurityAgent"
$sourceDll = "C:\Users\stevi\WindowsSecurityPlatform\src\WindowsSecurityAgent\WindowsSecurityAgent.Core\bin\Release\net10.0\WindowsSecurityAgent.Core.dll"
$targetDll = Join-Path $installPath "WindowsSecurityAgent.Core.dll"

Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "Updating Agent with Whitelist Mode Fix" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host ""

# Stop service
Write-Host "1. Stopping service..." -ForegroundColor Yellow
try {
    Stop-Service -Name $serviceName -Force -ErrorAction Stop
    Start-Sleep -Seconds 2
    Write-Host "   Service stopped" -ForegroundColor Green
} catch {
    Write-Host "   Service was already stopped" -ForegroundColor Gray
}

# Copy updated DLL
Write-Host ""
Write-Host "2. Updating DLL..." -ForegroundColor Yellow
if (Test-Path $sourceDll) {
    Copy-Item -Path $sourceDll -Destination $targetDll -Force
    Write-Host "   DLL updated" -ForegroundColor Green
} else {
    Write-Host "   ERROR: Source DLL not found: $sourceDll" -ForegroundColor Red
    Write-Host "   Please build the project first" -ForegroundColor Yellow
    exit 1
}

# Start service
Write-Host ""
Write-Host "3. Starting service..." -ForegroundColor Yellow
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
    exit 1
}

Write-Host ""
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "Update Complete!" -ForegroundColor Green
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Whitelist mode fix applied:" -ForegroundColor Cyan
Write-Host "  • Processes will be blocked unless they match an Allow rule" -ForegroundColor White
Write-Host "  • Block rules take precedence over Allow rules" -ForegroundColor White
Write-Host "  • All executable processes are now monitored" -ForegroundColor White
Write-Host ""
Write-Host "In whitelist mode, installers will be blocked unless you have" -ForegroundColor Yellow
Write-Host "explicit Allow rules for them." -ForegroundColor Yellow
Write-Host ""


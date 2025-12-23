# Install-DotNetSDK.ps1
# Installs .NET SDK for building the Windows Security Agent

Write-Host "=== .NET SDK Installation Guide ===" -ForegroundColor Cyan
Write-Host ""

# Check if already installed
$dotnetPath = Get-Command dotnet -ErrorAction SilentlyContinue
if ($dotnetPath) {
    Write-Host "✓ .NET SDK is already installed!" -ForegroundColor Green
    Write-Host "  Location: $($dotnetPath.Source)" -ForegroundColor Gray
    dotnet --version
    exit 0
}

Write-Host "To install .NET SDK, choose one of these options:" -ForegroundColor Yellow
Write-Host ""

Write-Host "OPTION 1: Download and Install Manually (Recommended)" -ForegroundColor Cyan
Write-Host "1. Visit: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor White
Write-Host "2. Download '.NET SDK 8.0.x' (not just Runtime)" -ForegroundColor White
Write-Host "3. Run the installer" -ForegroundColor White
Write-Host "4. Restart PowerShell after installation" -ForegroundColor White
Write-Host ""

Write-Host "OPTION 2: Install via Winget (if available)" -ForegroundColor Cyan
Write-Host "winget install Microsoft.DotNet.SDK.8" -ForegroundColor White
Write-Host ""

Write-Host "OPTION 3: Install via Chocolatey (if available)" -ForegroundColor Cyan
Write-Host "choco install dotnet-8.0-sdk" -ForegroundColor White
Write-Host ""

Write-Host "After installation, restart PowerShell and run:" -ForegroundColor Yellow
Write-Host "  dotnet --version" -ForegroundColor White
Write-Host "  cd src\WindowsSecurityAgent" -ForegroundColor White
Write-Host "  dotnet build" -ForegroundColor White


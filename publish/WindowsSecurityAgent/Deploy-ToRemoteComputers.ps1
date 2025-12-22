#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Deploys Windows Security Agent to multiple remote computers

.DESCRIPTION
    This script deploys the Windows Security Agent to multiple computers
    on the network using PowerShell remoting or file copy + PSExec.

.PARAMETER ComputerNames
    Array of computer names or IP addresses to deploy to

.PARAMETER ComputerListFile
    Path to text file containing computer names (one per line)

.PARAMETER Credential
    PSCredential object for authentication (prompts if not provided)

.PARAMETER DeploymentShare
    Network share path where agent files are located

.EXAMPLE
    .\Deploy-ToRemoteComputers.ps1 -ComputerNames "PC001","PC002","PC003"
    
.EXAMPLE
    .\Deploy-ToRemoteComputers.ps1 -ComputerListFile "C:\computers.txt"
    
.EXAMPLE
    $cred = Get-Credential
    .\Deploy-ToRemoteComputers.ps1 -ComputerListFile "computers.txt" -Credential $cred
#>

param(
    [string[]]$ComputerNames,
    [string]$ComputerListFile,
    [PSCredential]$Credential,
    [string]$DeploymentShare
)

$ErrorActionPreference = "Continue"

function Write-Success { Write-Host $args -ForegroundColor Green }
function Write-Info { Write-Host $args -ForegroundColor Cyan }
function Write-Warning { Write-Host $args -ForegroundColor Yellow }
function Write-Error { Write-Host $args -ForegroundColor Red }

Write-Info "======================================================================"
Write-Info "Windows Security Agent - Remote Deployment Script"
Write-Info "======================================================================"
Write-Host ""

# Get computer list
$computers = @()

if ($ComputerListFile) {
    if (Test-Path $ComputerListFile) {
        $computers = Get-Content $ComputerListFile | Where-Object { $_ -and $_.Trim() -ne "" }
        Write-Info "Loaded $($computers.Count) computers from file: $ComputerListFile"
    } else {
        Write-Error "ERROR: Computer list file not found: $ComputerListFile"
        exit 1
    }
} elseif ($ComputerNames) {
    $computers = $ComputerNames
    Write-Info "Deploying to $($computers.Count) specified computers"
} else {
    Write-Error "ERROR: Please specify either -ComputerNames or -ComputerListFile"
    Write-Info "Example: .\Deploy-ToRemoteComputers.ps1 -ComputerNames 'PC001','PC002'"
    exit 1
}

# Get credentials if not provided
if (-not $Credential) {
    Write-Info "Please enter credentials for remote computers:"
    $Credential = Get-Credential
}

# Get deployment source
if (-not $DeploymentShare) {
    $ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    Write-Info "Using local directory as source: $ScriptDir"
    $DeploymentShare = $ScriptDir
} else {
    Write-Info "Using deployment share: $DeploymentShare"
}

# Verify source files exist
$exePath = Join-Path $DeploymentShare "WindowsSecurityAgent.Service.exe"
if (-not (Test-Path $exePath)) {
    Write-Error "ERROR: Agent executable not found in: $DeploymentShare"
    exit 1
}

Write-Success "✓ Source files verified"
Write-Host ""

# Deployment results
$results = @{
    Success = @()
    Failed = @()
    Unreachable = @()
}

# Deploy to each computer
$current = 0
foreach ($computer in $computers) {
    $current++
    Write-Info "[$current/$($computers.Count)] Deploying to: $computer"
    
    # Test connectivity
    if (-not (Test-Connection -ComputerName $computer -Count 1 -Quiet)) {
        Write-Warning "  ✗ Computer unreachable"
        $results.Unreachable += $computer
        continue
    }
    
    try {
        # Create remote session
        $session = New-PSSession -ComputerName $computer -Credential $Credential -ErrorAction Stop
        
        # Copy files to remote computer
        $remoteTempPath = "C:\Temp\WindowsSecurityAgent"
        Write-Info "  Copying files..."
        
        Invoke-Command -Session $session -ScriptBlock {
            param($tempPath)
            if (Test-Path $tempPath) {
                Remove-Item -Path $tempPath -Recurse -Force
            }
            New-Item -ItemType Directory -Path $tempPath -Force | Out-Null
        } -ArgumentList $remoteTempPath
        
        Copy-Item -Path "$DeploymentShare\*" -Destination $remoteTempPath -ToSession $session -Recurse -Force
        
        # Run installation script
        Write-Info "  Installing agent..."
        $installResult = Invoke-Command -Session $session -ScriptBlock {
            param($tempPath)
            Set-Location $tempPath
            & ".\Install-Agent.ps1" -StartService $true
            return $LASTEXITCODE
        } -ArgumentList $remoteTempPath
        
        # Cleanup temp files
        Invoke-Command -Session $session -ScriptBlock {
            param($tempPath)
            if (Test-Path $tempPath) {
                Remove-Item -Path $tempPath -Recurse -Force -ErrorAction SilentlyContinue
            }
        } -ArgumentList $remoteTempPath
        
        # Close session
        Remove-PSSession -Session $session
        
        if ($installResult -eq 0 -or $null -eq $installResult) {
            Write-Success "  ✓ Deployment successful"
            $results.Success += $computer
        } else {
            Write-Warning "  ✗ Installation script failed with exit code: $installResult"
            $results.Failed += $computer
        }
        
    } catch {
        Write-Error "  ✗ Deployment failed: $_"
        $results.Failed += $computer
    }
    
    Write-Host ""
}

# Display summary
Write-Info "======================================================================"
Write-Info "Deployment Summary"
Write-Info "======================================================================"
Write-Host ""
Write-Success "Successful: $($results.Success.Count)"
if ($results.Success.Count -gt 0) {
    $results.Success | ForEach-Object { Write-Host "  ✓ $_" -ForegroundColor Green }
}
Write-Host ""

Write-Error "Failed: $($results.Failed.Count)"
if ($results.Failed.Count -gt 0) {
    $results.Failed | ForEach-Object { Write-Host "  ✗ $_" -ForegroundColor Red }
}
Write-Host ""

Write-Warning "Unreachable: $($results.Unreachable.Count)"
if ($results.Unreachable.Count -gt 0) {
    $results.Unreachable | ForEach-Object { Write-Host "  ? $_" -ForegroundColor Yellow }
}
Write-Host ""

# Export results to CSV
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportPath = Join-Path $PSScriptRoot "DeploymentReport_$timestamp.csv"

$allResults = @()
$results.Success | ForEach-Object { $allResults += [PSCustomObject]@{Computer=$_; Status="Success"} }
$results.Failed | ForEach-Object { $allResults += [PSCustomObject]@{Computer=$_; Status="Failed"} }
$results.Unreachable | ForEach-Object { $allResults += [PSCustomObject]@{Computer=$_; Status="Unreachable"} }

$allResults | Export-Csv -Path $reportPath -NoTypeInformation
Write-Info "Deployment report saved to: $reportPath"
Write-Host ""

if ($results.Failed.Count -gt 0 -or $results.Unreachable.Count -gt 0) {
    Write-Warning "Some deployments failed. Check the report for details."
    exit 1
} else {
    Write-Success "All deployments completed successfully!"
    exit 0
}


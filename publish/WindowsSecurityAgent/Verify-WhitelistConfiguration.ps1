# Verify Whitelist Configuration
# This script checks if whitelist mode is properly configured

param(
    [string]$ApiBaseUrl = "http://localhost:5140",
    [string]$TenantId = "11111111-1111-1111-1111-111111111111"
)

Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "Whitelist Configuration Verification" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host ""

try {
    $policiesUrl = "$ApiBaseUrl/api/policies?tenantId=$TenantId"
    Write-Host "Fetching policies from: $policiesUrl" -ForegroundColor Gray
    Write-Host ""
    
    $policies = Invoke-RestMethod -Uri $policiesUrl -Method Get -ErrorAction Stop
    
    if (-not $policies -or $policies.Count -eq 0) {
        Write-Host "No policies found!" -ForegroundColor Red
        Write-Host ""
        Write-Host "To enable whitelist mode:" -ForegroundColor Yellow
        Write-Host "  1. Go to the Admin Portal" -ForegroundColor White
        Write-Host "  2. Create a new policy" -ForegroundColor White
        Write-Host "  3. Set Mode to 'Whitelist'" -ForegroundColor White
        Write-Host "  4. Set Active to 'True'" -ForegroundColor White
        Write-Host "  5. Add Allow rules for executables you want to permit" -ForegroundColor White
        exit 1
    }
    
    Write-Host "Found $($policies.Count) policy/policies" -ForegroundColor Green
    Write-Host ""
    
    $hasActiveWhitelist = $false
    $whitelistPolicies = @()
    
    foreach ($policy in $policies) {
        $mode = if ($policy.mode -eq 0) { "Whitelist" } else { "Blacklist" }
        $status = if ($policy.isActive) { "Active" } else { "Inactive" }
        $statusColor = if ($policy.isActive) { "Green" } else { "Yellow" }
        
        Write-Host "Policy: $($policy.name)" -ForegroundColor Cyan
        Write-Host "  ID: $($policy.policyId)" -ForegroundColor Gray
        Write-Host "  Mode: $mode" -ForegroundColor $(if ($mode -eq "Whitelist") { "Green" } else { "Yellow" })
        Write-Host "  Status: $status" -ForegroundColor $statusColor
        Write-Host "  Priority: $($policy.priority)" -ForegroundColor Gray
        Write-Host "  Rules: $($policy.rules.Count)" -ForegroundColor Gray
        
        if ($policy.mode -eq 0 -and $policy.isActive) {
            $hasActiveWhitelist = $true
            $whitelistPolicies += $policy
        }
        
        if ($policy.rules.Count -gt 0) {
            Write-Host "  Rules:" -ForegroundColor Gray
            foreach ($rule in $policy.rules) {
                $ruleType = switch ($rule.ruleType) {
                    0 { "FileHash" }
                    1 { "Certificate" }
                    2 { "Path" }
                    3 { "Publisher" }
                    4 { "FileName" }
                    5 { "URL" }
                    6 { "Domain" }
                    default { "Unknown" }
                }
                $action = switch ($rule.action) {
                    0 { "Allow" }
                    1 { "Block" }
                    2 { "Alert" }
                    default { "Unknown" }
                }
                $actionColor = if ($action -eq "Allow") { "Green" } else { "Red" }
                
                Write-Host "    - $ruleType : $($rule.criteria) [$action]" -ForegroundColor $actionColor
            }
        } else {
            Write-Host "  WARNING: No rules configured!" -ForegroundColor Yellow
        }
        
        Write-Host ""
    }
    
    Write-Host "======================================================================" -ForegroundColor Cyan
    Write-Host "Summary" -ForegroundColor Cyan
    Write-Host "======================================================================" -ForegroundColor Cyan
    Write-Host ""
    
    if (-not $hasActiveWhitelist) {
        Write-Host "No active Whitelist policy found!" -ForegroundColor Red
        Write-Host ""
        Write-Host "To enable whitelist mode:" -ForegroundColor Yellow
        Write-Host "  1. Go to Admin Portal -> Policies" -ForegroundColor White
        Write-Host "  2. Create or edit a policy" -ForegroundColor White
        Write-Host "  3. Set Mode to 'Whitelist' (mode = 0)" -ForegroundColor White
        Write-Host "  4. Set Active to 'True'" -ForegroundColor White
        Write-Host "  5. Add Allow rules for executables you want to permit" -ForegroundColor White
        Write-Host ""
        Write-Host "In whitelist mode, ALL executables are blocked unless they match an Allow rule." -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "Active Whitelist policy found!" -ForegroundColor Green
    Write-Host ""
    
    $allowRules = 0
    $blockRules = 0
    foreach ($policy in $whitelistPolicies) {
        foreach ($rule in $policy.rules) {
            if ($rule.action -eq 0) { $allowRules++ }
            if ($rule.action -eq 1) { $blockRules++ }
        }
    }
    
    Write-Host "Allow Rules: $allowRules" -ForegroundColor $(if ($allowRules -gt 0) { "Green" } else { "Yellow" })
    Write-Host "Block Rules: $blockRules" -ForegroundColor $(if ($blockRules -eq 0) { "Gray" } else { "Yellow" })
    Write-Host ""
    
    if ($allowRules -eq 0) {
        Write-Host "WARNING: No Allow rules configured!" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "In whitelist mode with no Allow rules, ALL executables will be blocked." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "To allow specific executables, add Allow rules:" -ForegroundColor White
        Write-Host "  - FileName rule: Allow specific executable names" -ForegroundColor Gray
        Write-Host "  - Path rule: Allow executables in specific paths" -ForegroundColor Gray
        Write-Host "  - Publisher rule: Allow executables from specific publishers" -ForegroundColor Gray
        Write-Host "  - Certificate rule: Allow executables signed with specific certificates" -ForegroundColor Gray
        Write-Host "  - FileHash rule: Allow executables with specific file hashes" -ForegroundColor Gray
    } else {
        Write-Host "Allow rules are configured" -ForegroundColor Green
        Write-Host ""
        Write-Host "Executables that match these Allow rules will be permitted to run." -ForegroundColor Cyan
        Write-Host "All other executables will be blocked." -ForegroundColor Cyan
    }
    
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "  1. Verify the agent service is running: Get-Service WindowsSecurityAgent" -ForegroundColor White
    Write-Host "  2. Check agent has synced policies (wait up to 60 seconds after policy change)" -ForegroundColor White
    Write-Host "  3. Try running a blocked executable to verify it is blocked" -ForegroundColor White
    Write-Host "  4. Check Event Viewer Application logs for ThreatStopper blocking logs" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host "Error fetching policies: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Make sure:" -ForegroundColor Yellow
    Write-Host "  - Management API is running at: $ApiBaseUrl" -ForegroundColor White
    Write-Host "  - The API is accessible from this machine" -ForegroundColor White
    Write-Host "  - Tenant ID is correct: $TenantId" -ForegroundColor White
    Write-Host ""
    exit 1
}

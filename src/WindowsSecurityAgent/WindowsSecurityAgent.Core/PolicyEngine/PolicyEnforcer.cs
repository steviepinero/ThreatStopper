using Microsoft.Extensions.Logging;
using Shared.Models.Enums;
using Shared.Security;
using WindowsSecurityAgent.Core.Models;
using WindowsSecurityAgent.Core.Utilities;
using System.Text.RegularExpressions;

namespace WindowsSecurityAgent.Core.PolicyEngine;

/// <summary>
/// Enforces security policies on detected processes and file operations
/// </summary>
public class PolicyEnforcer
{
    private readonly ILogger<PolicyEnforcer> _logger;
    private readonly PolicyCache _policyCache;

    public PolicyEnforcer(ILogger<PolicyEnforcer> logger, PolicyCache policyCache)
    {
        _logger = logger;
        _policyCache = policyCache;
    }

    /// <summary>
    /// Evaluates if a process should be blocked based on active policies
    /// </summary>
    public (bool ShouldBlock, Guid? PolicyId, Guid? RuleId, string Reason) EvaluateProcess(ProcessInfo processInfo)
    {
        try
        {
            var policies = _policyCache.GetActivePolicies();
            
            if (!policies.Any())
            {
                _logger.LogWarning("No active policies found, allowing process by default");
                return (false, null, null, "No active policies");
            }

            // In whitelist mode, we need to check ALL policies for an Allow rule
            // If any policy has a matching Allow rule, allow the process
            // Otherwise, block it
            
            // First, check if there's a matching Allow rule in any policy
            bool hasMatchingAllowRule = false;
            Guid? allowPolicyId = null;
            Guid? allowRuleId = null;
            string allowReason = string.Empty;

            // Sort by priority (higher priority first)
            foreach (var policy in policies.OrderByDescending(p => p.Priority))
            {
                var result = EvaluateProcessAgainstPolicy(processInfo, policy);
                if (result.HasMatch)
                {
                    if (result.Action == BlockAction.Block)
                    {
                        // Block rule takes precedence - block immediately
                        string reason = $"Policy '{policy.Name}' - Rule: {result.RuleName}";
                        _logger.LogInformation("Process {ProcessName} matched block rule in policy {PolicyName}",
                            processInfo.ProcessName, policy.Name);
                        return (true, policy.PolicyId, result.RuleId, reason);
                    }
                    else if (result.Action == BlockAction.Allow)
                    {
                        // Remember we found an Allow rule, but continue checking for Block rules
                        hasMatchingAllowRule = true;
                        allowPolicyId = policy.PolicyId;
                        allowRuleId = result.RuleId;
                        allowReason = $"Policy '{policy.Name}' - Rule: {result.RuleName}";
                        _logger.LogInformation("Process {ProcessName} matched allow rule in policy {PolicyName}",
                            processInfo.ProcessName, policy.Name);
                    }
                }
            }

            // If we found an Allow rule and no Block rule, allow it
            if (hasMatchingAllowRule)
            {
                return (false, allowPolicyId, allowRuleId, allowReason);
            }

            // If no rules matched, use default policy mode behavior
            var defaultPolicy = policies.FirstOrDefault();
            if (defaultPolicy != null)
            {
                if (defaultPolicy.Mode == PolicyMode.Whitelist)
                {
                    // Whitelist mode: Block if no rule allowed it
                    _logger.LogInformation("Process {ProcessName} blocked by whitelist mode - No matching allow rule",
                        processInfo.ProcessName);
                    return (true, defaultPolicy.PolicyId, null, "Whitelist mode - No matching allow rule");
                }
                else
                {
                    // Blacklist mode: Allow if no rule blocked it
                    return (false, defaultPolicy.PolicyId, null, "Blacklist mode - No matching block rule");
                }
            }

            return (false, null, null, "Default allow");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating process {ProcessName}", processInfo.ProcessName);
            // On error, be conservative and allow (to avoid breaking the system)
            return (false, null, null, "Error during evaluation - default allow");
        }
    }

    private (bool HasMatch, BlockAction Action, Guid? RuleId, string RuleName) EvaluateProcessAgainstPolicy(ProcessInfo processInfo, Policy policy)
    {
        foreach (var rule in policy.Rules)
        {
            bool matches = rule.RuleType switch
            {
                RuleType.FileHash => MatchFileHash(processInfo, rule),
                RuleType.Certificate => MatchCertificate(processInfo, rule),
                RuleType.Path => MatchPath(processInfo, rule),
                RuleType.Publisher => MatchPublisher(processInfo, rule),
                RuleType.FileName => MatchFileName(processInfo, rule),
                _ => false
            };

            if (matches)
            {
                return (true, rule.Action, rule.RuleId, rule.Description);
            }
        }

        return (false, BlockAction.Allow, null, string.Empty);
    }

    private bool MatchFileHash(ProcessInfo processInfo, PolicyRule rule)
    {
        if (string.IsNullOrWhiteSpace(processInfo.ExecutablePath) || !File.Exists(processInfo.ExecutablePath))
            return false;

        try
        {
            // Calculate hash on demand (can be optimized by caching)
            var fileHash = HashCalculator.CalculateFileHash(processInfo.ExecutablePath);
            return fileHash.Equals(rule.Criteria, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating file hash for {Path}", processInfo.ExecutablePath);
            return false;
        }
    }

    private bool MatchCertificate(ProcessInfo processInfo, PolicyRule rule)
    {
        if (string.IsNullOrWhiteSpace(processInfo.ExecutablePath) || !File.Exists(processInfo.ExecutablePath))
            return false;

        try
        {
            var thumbprint = CertificateValidator.GetCertificateThumbprint(processInfo.ExecutablePath);
            return thumbprint != null && thumbprint.Equals(rule.Criteria, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting certificate for {Path}", processInfo.ExecutablePath);
            return false;
        }
    }

    private bool MatchPath(ProcessInfo processInfo, PolicyRule rule)
    {
        if (string.IsNullOrWhiteSpace(processInfo.ExecutablePath))
            return false;

        try
        {
            // Support wildcards in path
            var pattern = "^" + Regex.Escape(rule.Criteria)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";
            
            return Regex.IsMatch(processInfo.ExecutablePath, pattern, RegexOptions.IgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error matching path pattern {Pattern}", rule.Criteria);
            return false;
        }
    }

    private bool MatchPublisher(ProcessInfo processInfo, PolicyRule rule)
    {
        if (string.IsNullOrWhiteSpace(processInfo.Publisher))
            return false;

        return processInfo.Publisher.Contains(rule.Criteria, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchFileName(ProcessInfo processInfo, PolicyRule rule)
    {
        try
        {
            // ProcessName from WMI includes the extension (e.g., "notepad.exe")
            // But the user might enter "notepad.exe" or just "notepad"
            // Support both cases by checking with and without extension
            
            var processName = processInfo.ProcessName ?? string.Empty;
            var criteria = rule.Criteria ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(processName) || string.IsNullOrWhiteSpace(criteria))
                return false;
            
            // Normalize: remove extension from processName for comparison if criteria doesn't have one
            var processNameWithoutExt = Path.GetFileNameWithoutExtension(processName);
            var criteriaWithoutExt = Path.GetFileNameWithoutExtension(criteria);
            
            // Support wildcards in filename
            // Escape special regex characters, then replace wildcards
            var pattern = "^" + Regex.Escape(criteria)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";
            
            // Try matching against full name (with extension) first
            if (Regex.IsMatch(processName, pattern, RegexOptions.IgnoreCase))
                return true;
            
            // If criteria doesn't have an extension, also try matching without extension
            if (!criteria.Contains('.', StringComparison.Ordinal) || criteriaWithoutExt == criteria)
            {
                var patternWithoutExt = "^" + Regex.Escape(criteria)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".") + "$";
                if (Regex.IsMatch(processNameWithoutExt, patternWithoutExt, RegexOptions.IgnoreCase))
                    return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error matching filename pattern {Pattern} against {ProcessName}", 
                rule.Criteria, processInfo.ProcessName);
            return false;
        }
    }

    /// <summary>
    /// Blocks a process by terminating it
    /// </summary>
    public bool BlockProcess(ProcessInfo processInfo)
    {
        try
        {
            var process = System.Diagnostics.Process.GetProcessById(processInfo.ProcessId);
            process.Kill();
            _logger.LogInformation("Blocked process {ProcessName} (PID: {ProcessId})", 
                processInfo.ProcessName, processInfo.ProcessId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to block process {ProcessName} (PID: {ProcessId})", 
                processInfo.ProcessName, processInfo.ProcessId);
            return false;
        }
    }
}

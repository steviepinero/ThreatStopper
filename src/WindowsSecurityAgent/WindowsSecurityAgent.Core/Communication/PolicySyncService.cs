using Microsoft.Extensions.Logging;
using WindowsSecurityAgent.Core.PolicyEngine;
using WindowsSecurityAgent.Core.Monitoring;
using Shared.Models.Enums;

namespace WindowsSecurityAgent.Core.Communication;

/// <summary>
/// Syncs policies from the cloud to local cache
/// </summary>
public class PolicySyncService
{
    private readonly ILogger<PolicySyncService> _logger;
    private readonly CloudClient _cloudClient;
    private readonly PolicyCache _policyCache;
    private readonly UrlBlocker? _urlBlocker;

    public PolicySyncService(ILogger<PolicySyncService> logger, CloudClient cloudClient, PolicyCache policyCache, UrlBlocker? urlBlocker = null)
    {
        _logger = logger;
        _cloudClient = cloudClient;
        _policyCache = policyCache;
        _urlBlocker = urlBlocker;
    }

    /// <summary>
    /// Syncs policies from cloud to local cache
    /// </summary>
    public async Task<bool> SyncPoliciesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting policy sync...");
            
            var policyDTOs = await _cloudClient.GetPoliciesAsync(cancellationToken);
            
            // Even if no policies are returned, we should still apply URL blocking
            // to clear any existing blocks if all policies are inactive
            if (!policyDTOs.Any())
            {
                _logger.LogWarning("No policies received from cloud");
                // Still apply URL blocking to clear any existing blocks
                if (_urlBlocker != null)
                {
                    await ApplyUrlBlockingAsync(new List<Policy>());
                }
                return false;
            }

            // Convert DTOs to Policy objects
            var policies = policyDTOs.Select(dto => new Policy
            {
                PolicyId = dto.PolicyId,
                Name = dto.Name,
                Mode = dto.Mode,
                IsActive = dto.IsActive,
                Priority = dto.Priority,
                UpdatedAt = dto.UpdatedAt,
                Rules = dto.Rules.Select(ruleDto => new PolicyRule
                {
                    RuleId = ruleDto.RuleId,
                    RuleType = ruleDto.RuleType,
                    Criteria = ruleDto.Criteria,
                    Action = ruleDto.Action,
                    Description = ruleDto.Description
                }).ToList()
            }).ToList();

            _policyCache.UpdatePolicies(policies);
            _logger.LogInformation("Policy sync completed successfully. {Count} policies updated", policies.Count);
            
            // Apply URL blocking if available
            if (_urlBlocker != null)
            {
                await ApplyUrlBlockingAsync(policies);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync policies");
            return false;
        }
    }

    /// <summary>
    /// Applies URL blocking rules from policies
    /// </summary>
    private async Task ApplyUrlBlockingAsync(List<Policy> policies)
    {
        try
        {
            var urlsToBlock = new List<string>();

            foreach (var policy in policies.Where(p => p.IsActive))
            {
                foreach (var rule in policy.Rules)
                {
                    // Check for URL or Domain rule types
                    if (rule.RuleType == RuleType.Url || rule.RuleType == RuleType.Domain)
                    {
                        // Only block if action is Block
                        if (rule.Action == BlockAction.Block)
                        {
                            urlsToBlock.Add(rule.Criteria);
                            _logger.LogInformation("Found URL to block: {Url} from policy {PolicyName}", 
                                rule.Criteria, policy.Name);
                        }
                    }
                }
            }

            if (urlsToBlock.Any())
            {
                await _urlBlocker!.BlockUrlsAsync(urlsToBlock);
                _logger.LogInformation("Applied {Count} URL blocks", urlsToBlock.Count);
            }
            else
            {
                // Clear all blocks if no URLs to block
                await _urlBlocker!.ClearBlockedUrlsAsync();
                _logger.LogInformation("No URLs to block, cleared all blocks");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply URL blocking");
        }
    }
}

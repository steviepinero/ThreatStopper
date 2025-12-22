using Microsoft.Extensions.Logging;
using WindowsSecurityAgent.Core.PolicyEngine;

namespace WindowsSecurityAgent.Core.Communication;

/// <summary>
/// Syncs policies from the cloud to local cache
/// </summary>
public class PolicySyncService
{
    private readonly ILogger<PolicySyncService> _logger;
    private readonly CloudClient _cloudClient;
    private readonly PolicyCache _policyCache;

    public PolicySyncService(ILogger<PolicySyncService> logger, CloudClient cloudClient, PolicyCache policyCache)
    {
        _logger = logger;
        _cloudClient = cloudClient;
        _policyCache = policyCache;
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
            
            if (!policyDTOs.Any())
            {
                _logger.LogWarning("No policies received from cloud");
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
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync policies");
            return false;
        }
    }
}

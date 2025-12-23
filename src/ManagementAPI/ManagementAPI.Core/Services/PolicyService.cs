using ManagementAPI.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Models.DTOs;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ManagementAPI.Core.Services;

/// <summary>
/// Service for managing policies
/// </summary>
public class PolicyService
{
    private readonly ILogger<PolicyService> _logger;
    private readonly ManagementAPI.Data.ApplicationDbContext _dbContext;

    public PolicyService(ILogger<PolicyService> logger, Data.ApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Gets all policies for a tenant
    /// </summary>
    public async Task<List<PolicyDTO>> GetPoliciesByTenantAsync(Guid tenantId)
    {
        return await _dbContext.Policies
            .Where(p => p.TenantId == tenantId)
            .Include(p => p.Rules)
            .Include(p => p.AgentAssignments)
            .Select(p => new PolicyDTO
            {
                PolicyId = p.PolicyId,
                TenantId = p.TenantId,
                Name = p.Name,
                Description = p.Description,
                Mode = p.Mode,
                IsActive = p.IsActive,
                Priority = p.Priority,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Rules = p.Rules.OrderBy(r => r.Order).Select(r => new PolicyRuleDTO
                {
                    RuleId = r.RuleId,
                    RuleType = r.RuleType,
                    Criteria = r.Criteria,
                    Action = r.Action,
                    Description = r.Description
                }).ToList(),
                AssignedAgentIds = p.AgentAssignments.Select(aa => aa.AgentId).ToList()
            })
            .OrderByDescending(p => p.Priority)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a specific policy by ID
    /// </summary>
    public async Task<PolicyDTO?> GetPolicyByIdAsync(Guid policyId, Guid tenantId)
    {
        var policy = await _dbContext.Policies
            .Include(p => p.Rules)
            .Include(p => p.AgentAssignments)
            .FirstOrDefaultAsync(p => p.PolicyId == policyId && p.TenantId == tenantId);

        if (policy == null)
            return null;

        return new PolicyDTO
        {
            PolicyId = policy.PolicyId,
            TenantId = policy.TenantId,
            Name = policy.Name,
            Description = policy.Description,
            Mode = policy.Mode,
            IsActive = policy.IsActive,
            Priority = policy.Priority,
            CreatedAt = policy.CreatedAt,
            UpdatedAt = policy.UpdatedAt,
            Rules = policy.Rules.OrderBy(r => r.Order).Select(r => new PolicyRuleDTO
            {
                RuleId = r.RuleId,
                RuleType = r.RuleType,
                Criteria = r.Criteria,
                Action = r.Action,
                Description = r.Description
            }).ToList(),
            AssignedAgentIds = policy.AgentAssignments.Select(aa => aa.AgentId).ToList()
        };
    }

    /// <summary>
    /// Creates a new policy
    /// </summary>
    public async Task<PolicyDTO> CreatePolicyAsync(Guid tenantId, PolicyDTO policyDto)
    {
        var policy = new Policy
        {
            PolicyId = Guid.NewGuid(),
            TenantId = tenantId,
            Name = policyDto.Name,
            Description = policyDto.Description,
            Mode = policyDto.Mode,
            IsActive = policyDto.IsActive,
            Priority = policyDto.Priority,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add rules
        int order = 0;
        foreach (var ruleDto in policyDto.Rules)
        {
            policy.Rules.Add(new PolicyRule
            {
                RuleId = Guid.NewGuid(),
                PolicyId = policy.PolicyId,
                RuleType = ruleDto.RuleType,
                Criteria = ruleDto.Criteria,
                Action = ruleDto.Action,
                Description = ruleDto.Description,
                Order = order++,
                CreatedAt = DateTime.UtcNow
            });
        }

        _dbContext.Policies.Add(policy);
        await _dbContext.SaveChangesAsync();

        // Assign policy to specified agents, or all agents if not specified
        List<Guid> agentIdsToAssign;
        if (policyDto.AssignedAgentIds != null)
        {
            // Assign only to specified agents
            agentIdsToAssign = policyDto.AssignedAgentIds;
        }
        else
        {
            // Default: assign to all agents in the tenant
            agentIdsToAssign = await _dbContext.Agents
                .Where(a => a.TenantId == tenantId)
                .Select(a => a.AgentId)
                .ToListAsync();
        }

        foreach (var agentId in agentIdsToAssign)
        {
            // Check if assignment already exists
            var exists = await _dbContext.AgentPolicyAssignments
                .AnyAsync(apa => apa.AgentId == agentId && apa.PolicyId == policy.PolicyId);
            
            if (!exists)
            {
                _dbContext.AgentPolicyAssignments.Add(new AgentPolicyAssignment
                {
                    AssignmentId = Guid.NewGuid(),
                    AgentId = agentId,
                    PolicyId = policy.PolicyId,
                    AssignedAt = DateTime.UtcNow
                });
            }
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Policy created: {PolicyName} (ID: {PolicyId}), assigned to {AgentCount} agents", 
            policy.Name, policy.PolicyId, agentIdsToAssign.Count);

        return await GetPolicyByIdAsync(policy.PolicyId, tenantId) ?? policyDto;
    }

    /// <summary>
    /// Updates an existing policy
    /// </summary>
    public async Task<PolicyDTO?> UpdatePolicyAsync(Guid policyId, Guid tenantId, PolicyDTO policyDto)
    {
        try
        {
        var policy = await _dbContext.Policies
            .Include(p => p.Rules)
            .Include(p => p.AgentAssignments)
            .FirstOrDefaultAsync(p => p.PolicyId == policyId && p.TenantId == tenantId);

            if (policy == null)
            {
                _logger.LogWarning("Policy not found: {PolicyId} for tenant {TenantId}", policyId, tenantId);
                return null;
            }

            // Update policy properties - only update if provided (allow partial updates)
            if (!string.IsNullOrWhiteSpace(policyDto.Name))
                policy.Name = policyDto.Name;
            
            if (policyDto.Description != null)
                policy.Description = policyDto.Description;
            
            // Always update these fields if provided in DTO
            policy.Mode = policyDto.Mode;
            policy.IsActive = policyDto.IsActive;
            policy.Priority = policyDto.Priority;
            policy.UpdatedAt = DateTime.UtcNow;

            // Handle rules - only update if Rules array is provided and not null
            // Check if rules actually changed to avoid unnecessary updates
            bool rulesChanged = false;
            if (policyDto.Rules != null)
            {
                // Compare rule counts and content
                if (policyDto.Rules.Count != policy.Rules.Count)
                {
                    rulesChanged = true;
                }
                else
                {
                    // Compare each rule to see if anything changed
                    var existingRules = policy.Rules.OrderBy(r => r.Order).ToList();
                    var newRules = policyDto.Rules.ToList();
                    
                    for (int i = 0; i < existingRules.Count && !rulesChanged; i++)
                    {
                        if (existingRules[i].RuleType != newRules[i].RuleType ||
                            existingRules[i].Criteria != (newRules[i].Criteria ?? string.Empty) ||
                            existingRules[i].Action != newRules[i].Action ||
                            existingRules[i].Description != (newRules[i].Description ?? string.Empty))
                        {
                            rulesChanged = true;
                        }
                    }
                }
            }

            if (policyDto.Rules != null && rulesChanged)
            {
                // Delete existing rules directly from database to avoid tracking issues
                var existingRules = await _dbContext.PolicyRules
                    .Where(r => r.PolicyId == policy.PolicyId)
                    .ToListAsync();
                
                _dbContext.PolicyRules.RemoveRange(existingRules);
                
                // Clear the navigation property
                policy.Rules.Clear();

                // Add new rules
                int order = 0;
                foreach (var ruleDto in policyDto.Rules)
                {
                    if (ruleDto != null)
                    {
                        var newRule = new PolicyRule
                        {
                            RuleId = Guid.NewGuid(),
                            PolicyId = policy.PolicyId,
                            RuleType = ruleDto.RuleType,
                            Criteria = ruleDto.Criteria ?? string.Empty,
                            Action = ruleDto.Action,
                            Description = ruleDto.Description ?? string.Empty,
                            Order = order++,
                            CreatedAt = DateTime.UtcNow
                        };
                        _dbContext.PolicyRules.Add(newRule);
                        policy.Rules.Add(newRule);
                    }
                }
            }
            // If Rules is null or only IsActive changed, keep existing rules (partial update scenario)

            // Handle agent assignments if provided
            if (policyDto.AssignedAgentIds != null)
            {
                // Get current assignments
                var currentAssignments = await _dbContext.AgentPolicyAssignments
                    .Where(apa => apa.PolicyId == policy.PolicyId)
                    .ToListAsync();

                var currentAgentIds = currentAssignments.Select(a => a.AgentId).ToList();
                var newAgentIds = policyDto.AssignedAgentIds;

                // Remove assignments that are no longer in the list
                var toRemove = currentAssignments
                    .Where(a => !newAgentIds.Contains(a.AgentId))
                    .ToList();
                _dbContext.AgentPolicyAssignments.RemoveRange(toRemove);

                // Add new assignments
                foreach (var agentId in newAgentIds)
                {
                    if (!currentAgentIds.Contains(agentId))
                    {
                        // Verify agent belongs to the same tenant
                        var agent = await _dbContext.Agents
                            .FirstOrDefaultAsync(a => a.AgentId == agentId && a.TenantId == tenantId);
                        
                        if (agent != null)
                        {
                            _dbContext.AgentPolicyAssignments.Add(new AgentPolicyAssignment
                            {
                                AssignmentId = Guid.NewGuid(),
                                AgentId = agentId,
                                PolicyId = policy.PolicyId,
                                AssignedAt = DateTime.UtcNow
                            });
                        }
                    }
                }

                _logger.LogInformation("Updated agent assignments for policy {PolicyId}: {Count} agents assigned", 
                    policyId, newAgentIds.Count);
            }

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency exception updating policy {PolicyId}. The policy may have been modified or deleted.", policyId);
                
                // Reload the entity to get current state
                await _dbContext.Entry(policy).ReloadAsync();
                
                // Check if policy still exists
                if (await _dbContext.Policies.AnyAsync(p => p.PolicyId == policyId && p.TenantId == tenantId))
                {
                    // Policy exists, re-apply changes
                    if (!string.IsNullOrWhiteSpace(policyDto.Name))
                        policy.Name = policyDto.Name;
                    
                    if (policyDto.Description != null)
                        policy.Description = policyDto.Description;
                    
                    policy.Mode = policyDto.Mode;
                    policy.IsActive = policyDto.IsActive;
                    policy.Priority = policyDto.Priority;
                    policy.UpdatedAt = DateTime.UtcNow;

                    // Re-handle rules if provided
                    if (policyDto.Rules != null)
                    {
                        // Delete existing rules
                        var existingRules = await _dbContext.PolicyRules
                            .Where(r => r.PolicyId == policy.PolicyId)
                            .ToListAsync();
                        _dbContext.PolicyRules.RemoveRange(existingRules);
                        
                        // Reload rules collection
                        await _dbContext.Entry(policy).Collection(p => p.Rules).LoadAsync();
                        policy.Rules.Clear();

                        // Add new rules
                        int order = 0;
                        foreach (var ruleDto in policyDto.Rules)
                        {
                            if (ruleDto != null)
                            {
                                var newRule = new PolicyRule
                                {
                                    RuleId = Guid.NewGuid(),
                                    PolicyId = policy.PolicyId,
                                    RuleType = ruleDto.RuleType,
                                    Criteria = ruleDto.Criteria ?? string.Empty,
                                    Action = ruleDto.Action,
                                    Description = ruleDto.Description ?? string.Empty,
                                    Order = order++,
                                    CreatedAt = DateTime.UtcNow
                                };
                                _dbContext.PolicyRules.Add(newRule);
                                policy.Rules.Add(newRule);
                            }
                        }
                    }

                    // Retry save
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("Policy {PolicyId} updated successfully after retry", policyId);
                }
                else
                {
                    _logger.LogWarning("Policy {PolicyId} was deleted during update", policyId);
                    return null;
                }
            }

            _logger.LogInformation("Policy updated: {PolicyName} (ID: {PolicyId})", policy.Name, policy.PolicyId);

            return await GetPolicyByIdAsync(policyId, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating policy {PolicyId} for tenant {TenantId}", policyId, tenantId);
            throw;
        }
    }

    /// <summary>
    /// Deletes a policy
    /// </summary>
    public async Task<bool> DeletePolicyAsync(Guid policyId, Guid tenantId)
    {
        var policy = await _dbContext.Policies
            .FirstOrDefaultAsync(p => p.PolicyId == policyId && p.TenantId == tenantId);

        if (policy == null)
            return false;

        _dbContext.Policies.Remove(policy);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Policy deleted: {PolicyId}", policyId);

        return true;
    }

    /// <summary>
    /// Assigns a policy to an agent
    /// </summary>
    public async Task<bool> AssignPolicyToAgentAsync(Guid agentId, Guid policyId)
    {
        // Check if assignment already exists
        var exists = await _dbContext.AgentPolicyAssignments
            .AnyAsync(apa => apa.AgentId == agentId && apa.PolicyId == policyId);

        if (exists)
            return true;

        var assignment = new AgentPolicyAssignment
        {
            AssignmentId = Guid.NewGuid(),
            AgentId = agentId,
            PolicyId = policyId,
            AssignedAt = DateTime.UtcNow
        };

        _dbContext.AgentPolicyAssignments.Add(assignment);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Policy {PolicyId} assigned to agent {AgentId}", policyId, agentId);

        return true;
    }

    /// <summary>
    /// Unassigns a policy from an agent
    /// </summary>
    public async Task<bool> UnassignPolicyFromAgentAsync(Guid agentId, Guid policyId)
    {
        var assignment = await _dbContext.AgentPolicyAssignments
            .FirstOrDefaultAsync(apa => apa.AgentId == agentId && apa.PolicyId == policyId);

        if (assignment == null)
            return false;

        _dbContext.AgentPolicyAssignments.Remove(assignment);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Policy {PolicyId} unassigned from agent {AgentId}", policyId, agentId);

        return true;
    }

    /// <summary>
    /// Syncs all policy assignments for a tenant - ensures all agents have all active policies assigned
    /// </summary>
    public async Task<int> SyncAllPolicyAssignmentsAsync(Guid tenantId)
    {
        var agents = await _dbContext.Agents
            .Where(a => a.TenantId == tenantId)
            .ToListAsync();

        var activePolicies = await _dbContext.Policies
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .ToListAsync();

        var existingAssignments = await _dbContext.AgentPolicyAssignments
            .Where(apa => agents.Select(a => a.AgentId).Contains(apa.AgentId))
            .ToListAsync();

        var newAssignments = 0;

        foreach (var agent in agents)
        {
            foreach (var policy in activePolicies)
            {
                var exists = existingAssignments.Any(ea => ea.AgentId == agent.AgentId && ea.PolicyId == policy.PolicyId);
                if (!exists)
                {
                    _dbContext.AgentPolicyAssignments.Add(new AgentPolicyAssignment
                    {
                        AssignmentId = Guid.NewGuid(),
                        AgentId = agent.AgentId,
                        PolicyId = policy.PolicyId,
                        AssignedAt = DateTime.UtcNow
                    });
                    newAssignments++;
                }
            }
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Synced policy assignments for tenant {TenantId}: created {NewAssignments} new assignments", 
            tenantId, newAssignments);

        return newAssignments;
    }
}

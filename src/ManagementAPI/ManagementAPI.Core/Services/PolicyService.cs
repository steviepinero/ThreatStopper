using ManagementAPI.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Models.DTOs;

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
                }).ToList()
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
            }).ToList()
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

        // Auto-assign policy to all agents in the tenant
        var agentIds = await _dbContext.Agents
            .Where(a => a.TenantId == tenantId)
            .Select(a => a.AgentId)
            .ToListAsync();

        foreach (var agentId in agentIds)
        {
            _dbContext.AgentPolicyAssignments.Add(new AgentPolicyAssignment
            {
                AssignmentId = Guid.NewGuid(),
                AgentId = agentId,
                PolicyId = policy.PolicyId,
                AssignedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Policy created: {PolicyName} (ID: {PolicyId}), assigned to {AgentCount} agents", 
            policy.Name, policy.PolicyId, agentIds.Count);

        return await GetPolicyByIdAsync(policy.PolicyId, tenantId) ?? policyDto;
    }

    /// <summary>
    /// Updates an existing policy
    /// </summary>
    public async Task<PolicyDTO?> UpdatePolicyAsync(Guid policyId, Guid tenantId, PolicyDTO policyDto)
    {
        var policy = await _dbContext.Policies
            .Include(p => p.Rules)
            .FirstOrDefaultAsync(p => p.PolicyId == policyId && p.TenantId == tenantId);

        if (policy == null)
            return null;

        policy.Name = policyDto.Name;
        policy.Description = policyDto.Description;
        policy.Mode = policyDto.Mode;
        policy.IsActive = policyDto.IsActive;
        policy.Priority = policyDto.Priority;
        policy.UpdatedAt = DateTime.UtcNow;

        // Remove old rules and add new ones
        _dbContext.PolicyRules.RemoveRange(policy.Rules);
        policy.Rules.Clear();

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

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Policy updated: {PolicyName} (ID: {PolicyId})", policy.Name, policy.PolicyId);

        return await GetPolicyByIdAsync(policyId, tenantId);
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

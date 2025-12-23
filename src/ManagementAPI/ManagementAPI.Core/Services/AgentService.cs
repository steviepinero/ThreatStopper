using ManagementAPI.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Models.DTOs;
using Shared.Models.Enums;
using Shared.Security;

namespace ManagementAPI.Core.Services;

/// <summary>
/// Service for managing agents
/// </summary>
public class AgentService
{
    private readonly ILogger<AgentService> _logger;
    private readonly ManagementAPI.Data.ApplicationDbContext _dbContext;

    public AgentService(ILogger<AgentService> logger, Data.ApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Registers a new agent
    /// </summary>
    public async Task<AgentRegistrationResponseDTO> RegisterAgentAsync(AgentRegistrationDTO registration)
    {
        try
        {
            // Verify tenant API key
            var organization = await _dbContext.Organizations
                .FirstOrDefaultAsync(o => o.TenantId == registration.TenantId && o.IsActive);

            if (organization == null)
            {
                _logger.LogWarning("Invalid tenant ID during agent registration: {TenantId}", registration.TenantId);
                return new AgentRegistrationResponseDTO
                {
                    Success = false,
                    Message = "Invalid tenant ID or inactive organization"
                };
            }

            if (!ApiKeyGenerator.VerifyApiKey(registration.TenantApiKey, organization.ApiKeyHash))
            {
                _logger.LogWarning("Invalid API key for tenant: {TenantId}", registration.TenantId);
                return new AgentRegistrationResponseDTO
                {
                    Success = false,
                    Message = "Invalid tenant API key"
                };
            }

            // Generate new agent API key
            var agentApiKey = ApiKeyGenerator.GenerateApiKey();
            var agentApiKeyHash = ApiKeyGenerator.HashApiKey(agentApiKey);

            // Create agent
            var agent = new Agent
            {
                AgentId = Guid.NewGuid(),
                TenantId = registration.TenantId,
                MachineName = registration.MachineName,
                OperatingSystem = registration.OperatingSystem,
                AgentVersion = registration.AgentVersion,
                ApiKeyHash = agentApiKeyHash,
                Status = AgentStatus.Online,
                LastHeartbeat = DateTime.UtcNow,
                RegisteredAt = DateTime.UtcNow
            };

            _dbContext.Agents.Add(agent);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Agent registered successfully: {MachineName} (ID: {AgentId})", 
                registration.MachineName, agent.AgentId);

            return new AgentRegistrationResponseDTO
            {
                Success = true,
                AgentId = agent.AgentId,
                ApiKey = agentApiKey,
                Message = "Agent registered successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering agent");
            return new AgentRegistrationResponseDTO
            {
                Success = false,
                Message = "Internal server error"
            };
        }
    }

    /// <summary>
    /// Gets policies assigned to an agent
    /// </summary>
    public async Task<List<PolicyDTO>> GetAgentPoliciesAsync(Guid agentId)
    {
        var agent = await _dbContext.Agents
            .Include(a => a.PolicyAssignments)
                .ThenInclude(pa => pa.Policy)
                    .ThenInclude(p => p.Rules)
            .FirstOrDefaultAsync(a => a.AgentId == agentId);

        if (agent == null)
            return new List<PolicyDTO>();

        var policies = agent.PolicyAssignments
            .Where(pa => pa.Policy.IsActive)
            .Select(pa => pa.Policy)
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
                Rules = p.Rules.Select(r => new PolicyRuleDTO
                {
                    RuleId = r.RuleId,
                    RuleType = r.RuleType,
                    Criteria = r.Criteria,
                    Action = r.Action,
                    Description = r.Description
                }).ToList()
            })
            .OrderByDescending(p => p.Priority)
            .ToList();

        return policies;
    }

    /// <summary>
    /// Updates agent heartbeat and returns response indicating if policies changed
    /// </summary>
    public async Task<HeartbeatResponseDTO?> UpdateHeartbeatAsync(Guid agentId, HeartbeatDTO heartbeat)
    {
        var agent = await _dbContext.Agents
            .Include(a => a.PolicyAssignments)
                .ThenInclude(pa => pa.Policy)
            .FirstOrDefaultAsync(a => a.AgentId == agentId);
            
        if (agent == null)
            return null;

        // Get the most recent policy update timestamp for this agent's assigned policies
        var lastPolicyUpdate = agent.PolicyAssignments
            .Where(pa => pa.Policy.IsActive)
            .Select(pa => pa.Policy.UpdatedAt)
            .DefaultIfEmpty(DateTime.MinValue)
            .Max();

        // Check if policies have changed since last heartbeat
        // If any policy was updated more recently than the last heartbeat (with 2 minute buffer for clock skew),
        // consider policies as changed
        var policiesChanged = lastPolicyUpdate > DateTime.MinValue && 
                              lastPolicyUpdate > agent.LastHeartbeat.AddMinutes(-2);

        agent.Status = heartbeat.Status;
        agent.AgentVersion = heartbeat.AgentVersion;
        var previousHeartbeat = agent.LastHeartbeat;
        agent.LastHeartbeat = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogDebug("Heartbeat updated for agent {AgentId}. Policies changed: {Changed}, Last policy update: {LastUpdate}", 
            agentId, policiesChanged, lastPolicyUpdate);

        return new HeartbeatResponseDTO
        {
            PoliciesChanged = policiesChanged,
            LastPolicyUpdate = lastPolicyUpdate > DateTime.MinValue ? lastPolicyUpdate : null
        };
    }

    /// <summary>
    /// Gets all agents for a tenant
    /// </summary>
    public async Task<List<AgentDTO>> GetAgentsByTenantAsync(Guid tenantId)
    {
        return await _dbContext.Agents
            .Where(a => a.TenantId == tenantId)
            .Include(a => a.PolicyAssignments)
            .Select(a => new AgentDTO
            {
                AgentId = a.AgentId,
                TenantId = a.TenantId,
                MachineName = a.MachineName,
                OperatingSystem = a.OperatingSystem,
                AgentVersion = a.AgentVersion,
                Status = a.Status,
                LastHeartbeat = a.LastHeartbeat,
                RegisteredAt = a.RegisteredAt,
                AssignedPolicyIds = a.PolicyAssignments.Select(pa => pa.PolicyId).ToList()
            })
            .ToListAsync();
    }
}

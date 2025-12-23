using ManagementAPI.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.DTOs;

namespace ManagementAPI.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PoliciesController : ControllerBase
{
    private readonly ILogger<PoliciesController> _logger;
    private readonly PolicyService _policyService;

    public PoliciesController(ILogger<PoliciesController> logger, PolicyService policyService)
    {
        _logger = logger;
        _policyService = policyService;
    }

    /// <summary>
    /// Gets all policies for a tenant
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<PolicyDTO>>> GetPolicies([FromQuery] Guid tenantId)
    {
        // In production, get tenantId from authenticated user claims
        var policies = await _policyService.GetPoliciesByTenantAsync(tenantId);
        return Ok(policies);
    }

    /// <summary>
    /// Gets a specific policy by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PolicyDTO>> GetPolicy(Guid id, [FromQuery] Guid tenantId)
    {
        var policy = await _policyService.GetPolicyByIdAsync(id, tenantId);
        
        if (policy == null)
            return NotFound();

        return Ok(policy);
    }

    /// <summary>
    /// Creates a new policy
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PolicyDTO>> CreatePolicy([FromQuery] Guid tenantId, [FromBody] PolicyDTO policy)
    {
        var created = await _policyService.CreatePolicyAsync(tenantId, policy);
        return CreatedAtAction(nameof(GetPolicy), new { id = created.PolicyId, tenantId }, created);
    }

    /// <summary>
    /// Updates an existing policy
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PolicyDTO>> UpdatePolicy(Guid id, [FromQuery] Guid tenantId, [FromBody] PolicyDTO policy)
    {
        try
        {
            if (policy == null)
            {
                _logger.LogWarning("UpdatePolicy called with null policy DTO for policy {PolicyId}", id);
                return BadRequest("Policy data is required");
            }

            // Ensure the PolicyId in the DTO matches the route parameter
            policy.PolicyId = id;
            
            _logger.LogInformation("Updating policy {PolicyId} for tenant {TenantId}. IsActive: {IsActive}, Rules count: {RulesCount}", 
                id, tenantId, policy.IsActive, policy.Rules?.Count ?? 0);

            var updated = await _policyService.UpdatePolicyAsync(id, tenantId, policy);
            
            if (updated == null)
            {
                _logger.LogWarning("Policy {PolicyId} not found for tenant {TenantId}", id, tenantId);
                return NotFound();
            }

            _logger.LogInformation("Successfully updated policy {PolicyId}", id);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating policy {PolicyId} for tenant {TenantId}. Exception: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}", 
                id, tenantId, ex.GetType().Name, ex.Message, ex.StackTrace);
            return StatusCode(500, new { error = "An error occurred while updating the policy", message = ex.Message, details = ex.ToString() });
        }
    }

    /// <summary>
    /// Deletes a policy
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePolicy(Guid id, [FromQuery] Guid tenantId)
    {
        var success = await _policyService.DeletePolicyAsync(id, tenantId);
        
        if (!success)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Assigns a policy to an agent
    /// </summary>
    [HttpPost("{policyId:guid}/agents/{agentId:guid}")]
    public async Task<IActionResult> AssignPolicyToAgent(Guid policyId, Guid agentId)
    {
        var success = await _policyService.AssignPolicyToAgentAsync(agentId, policyId);
        return Ok(new { Success = success });
    }

    /// <summary>
    /// Unassigns a policy from an agent
    /// </summary>
    [HttpDelete("{policyId:guid}/agents/{agentId:guid}")]
    public async Task<IActionResult> UnassignPolicyFromAgent(Guid policyId, Guid agentId)
    {
        var success = await _policyService.UnassignPolicyFromAgentAsync(agentId, policyId);
        
        if (!success)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Syncs all policy assignments - ensures all agents have all active policies assigned
    /// </summary>
    [HttpPost("sync-assignments")]
    public async Task<ActionResult> SyncAllPolicyAssignments([FromQuery] Guid tenantId)
    {
        var newAssignments = await _policyService.SyncAllPolicyAssignmentsAsync(tenantId);
        return Ok(new { NewAssignmentsCreated = newAssignments });
    }
}

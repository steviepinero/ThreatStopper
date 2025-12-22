using ManagementAPI.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.DTOs;

namespace ManagementAPI.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentsController : ControllerBase
{
    private readonly ILogger<AgentsController> _logger;
    private readonly AgentService _agentService;

    public AgentsController(ILogger<AgentsController> logger, AgentService agentService)
    {
        _logger = logger;
        _agentService = agentService;
    }

    /// <summary>
    /// Registers a new agent
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AgentRegistrationResponseDTO>> Register([FromBody] AgentRegistrationDTO registration)
    {
        var result = await _agentService.RegisterAgentAsync(registration);
        
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Gets policies assigned to an agent
    /// </summary>
    [HttpGet("{id}/policies")]
    public async Task<ActionResult<List<PolicyDTO>>> GetAgentPolicies(Guid id)
    {
        var policies = await _agentService.GetAgentPoliciesAsync(id);
        return Ok(policies);
    }

    /// <summary>
    /// Updates agent heartbeat
    /// </summary>
    [HttpPost("{id}/heartbeat")]
    public async Task<IActionResult> Heartbeat(Guid id, [FromBody] HeartbeatDTO heartbeat)
    {
        var success = await _agentService.UpdateHeartbeatAsync(id, heartbeat);
        
        if (!success)
            return NotFound();

        return Ok();
    }

    /// <summary>
    /// Submits audit logs from an agent
    /// </summary>
    [HttpPost("{id}/audit-logs")]
    public async Task<IActionResult> SubmitAuditLogs(Guid id, [FromBody] List<AuditLogDTO> logs)
    {
        // In production, verify the agent ID matches the authenticated agent
        var auditService = HttpContext.RequestServices.GetRequiredService<AuditLogService>();
        var count = await auditService.SubmitAuditLogsAsync(logs);
        
        return Ok(new { SubmittedCount = count });
    }

    /// <summary>
    /// Gets all agents for the authenticated tenant
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AgentDTO>>> GetAgents([FromQuery] Guid tenantId)
    {
        // In production, get tenantId from authenticated user claims
        var agents = await _agentService.GetAgentsByTenantAsync(tenantId);
        return Ok(agents);
    }

    /// <summary>
    /// Gets a specific agent by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AgentDTO>> GetAgent(Guid id, [FromQuery] Guid tenantId)
    {
        var agents = await _agentService.GetAgentsByTenantAsync(tenantId);
        var agent = agents.FirstOrDefault(a => a.AgentId == id);
        
        if (agent == null)
            return NotFound();

        return Ok(agent);
    }
}

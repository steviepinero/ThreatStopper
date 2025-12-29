using ManagementAPI.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.DTOs;
using Shared.Models.Enums;

namespace ManagementAPI.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccessRequestsController : ControllerBase
{
    private readonly ILogger<AccessRequestsController> _logger;
    private readonly AccessRequestService _accessRequestService;

    public AccessRequestsController(
        ILogger<AccessRequestsController> logger, 
        AccessRequestService accessRequestService)
    {
        _logger = logger;
        _accessRequestService = accessRequestService;
    }

    /// <summary>
    /// Creates a new access request (called by agent)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AccessRequestDTO>> CreateAccessRequest([FromBody] CreateAccessRequestDTO request)
    {
        try
        {
            var result = await _accessRequestService.CreateAccessRequestAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create access request");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets pending access requests for a specific agent (called by agent to check for approvals)
    /// </summary>
    [HttpGet("agent/{agentId:guid}/pending")]
    public async Task<ActionResult<List<AccessRequestDTO>>> GetPendingRequestsByAgent(Guid agentId)
    {
        try
        {
            var requests = await _accessRequestService.GetPendingRequestsByAgentAsync(agentId);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending requests for agent");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a specific access request by ID
    /// </summary>
    [HttpGet("id/{requestId:guid}")]
    public async Task<ActionResult<AccessRequestDTO>> GetAccessRequestById(Guid requestId)
    {
        try
        {
            var request = await _accessRequestService.GetAccessRequestByIdAsync(requestId);
            
            if (request == null)
            {
                return NotFound(new { error = "Access request not found" });
            }

            return Ok(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get access request by ID");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets all access requests for a tenant
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AccessRequestDTO>>> GetAccessRequests(
        [FromQuery] Guid tenantId, 
        [FromQuery] AccessRequestStatus? status = null)
    {
        try
        {
            var requests = await _accessRequestService.GetAccessRequestsByTenantAsync(tenantId, status);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get access requests");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Reviews an access request (approve or deny)
    /// </summary>
    [HttpPost("review")]
    public async Task<ActionResult<AccessRequestDTO>> ReviewAccessRequest([FromBody] ReviewAccessRequestDTO review)
    {
        try
        {
            var result = await _accessRequestService.ReviewAccessRequestAsync(review);
            
            if (result == null)
            {
                return NotFound(new { error = "Access request not found or not in pending status" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to review access request");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Checks if a resource has an active approval (called by agent)
    /// </summary>
    [HttpPost("check-approval")]
    public async Task<ActionResult<AccessApprovalResponseDTO>> CheckApproval([FromBody] AccessApprovalCheckDTO check)
    {
        try
        {
            var result = await _accessRequestService.CheckApprovalAsync(check);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check approval");
            return BadRequest(new { error = ex.Message });
        }
    }
}


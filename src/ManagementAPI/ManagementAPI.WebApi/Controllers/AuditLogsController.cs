using ManagementAPI.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.DTOs;

namespace ManagementAPI.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditLogsController : ControllerBase
{
    private readonly ILogger<AuditLogsController> _logger;
    private readonly AuditLogService _auditLogService;

    public AuditLogsController(ILogger<AuditLogsController> logger, AuditLogService auditLogService)
    {
        _logger = logger;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Gets audit logs for a tenant
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AuditLogDTO>>> GetAuditLogs(
        [FromQuery] Guid tenantId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool? blockedOnly = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100)
    {
        // In production, get tenantId from authenticated user claims
        var logs = await _auditLogService.GetAuditLogsByTenantAsync(
            tenantId, startDate, endDate, blockedOnly, skip, take);
        
        return Ok(logs);
    }
}

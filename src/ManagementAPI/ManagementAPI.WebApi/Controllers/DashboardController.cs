using ManagementAPI.Core.Services;
using ManagementAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Models.Enums;

namespace ManagementAPI.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ILogger<DashboardController> _logger;
    private readonly ApplicationDbContext _dbContext;

    public DashboardController(ILogger<DashboardController> logger, ApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Gets dashboard statistics for a tenant
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<object>> GetStatistics([FromQuery] Guid tenantId)
    {
        var now = DateTime.UtcNow;
        var last24Hours = now.AddHours(-24);
        var last7Days = now.AddDays(-7);

        var totalAgents = await _dbContext.Agents
            .CountAsync(a => a.TenantId == tenantId);

        var onlineAgents = await _dbContext.Agents
            .CountAsync(a => a.TenantId == tenantId && a.Status == AgentStatus.Online);

        var blockedLast24h = await _dbContext.AuditLogs
            .Where(al => al.Agent.TenantId == tenantId && 
                         al.Timestamp >= last24Hours && 
                         al.Blocked)
            .CountAsync();

        var blockedLast7d = await _dbContext.AuditLogs
            .Where(al => al.Agent.TenantId == tenantId && 
                         al.Timestamp >= last7Days && 
                         al.Blocked)
            .CountAsync();

        var topBlockedApps = await _dbContext.AuditLogs
            .Where(al => al.Agent.TenantId == tenantId && 
                         al.Timestamp >= last7Days && 
                         al.Blocked &&
                         !string.IsNullOrEmpty(al.ProcessName))
            .GroupBy(al => al.ProcessName)
            .Select(g => new { ProcessName = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        var activePolicies = await _dbContext.Policies
            .CountAsync(p => p.TenantId == tenantId && p.IsActive);

        return Ok(new
        {
            TotalAgents = totalAgents,
            OnlineAgents = onlineAgents,
            OfflineAgents = totalAgents - onlineAgents,
            BlockedLast24Hours = blockedLast24h,
            BlockedLast7Days = blockedLast7d,
            TopBlockedApplications = topBlockedApps,
            ActivePolicies = activePolicies
        });
    }
}

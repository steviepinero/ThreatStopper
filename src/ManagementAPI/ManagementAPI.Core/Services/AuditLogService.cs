using ManagementAPI.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Models.DTOs;

namespace ManagementAPI.Core.Services;

/// <summary>
/// Service for managing audit logs
/// </summary>
public class AuditLogService
{
    private readonly ILogger<AuditLogService> _logger;
    private readonly ManagementAPI.Data.ApplicationDbContext _dbContext;

    public AuditLogService(ILogger<AuditLogService> logger, Data.ApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Submits audit logs from an agent
    /// </summary>
    public async Task<int> SubmitAuditLogsAsync(List<AuditLogDTO> logs)
    {
        var entities = logs.Select(log => new AuditLog
        {
            LogId = log.LogId,
            AgentId = log.AgentId,
            EventType = log.EventType,
            Timestamp = log.Timestamp,
            Blocked = log.Blocked,
            ProcessName = log.ProcessName,
            ProcessPath = log.ProcessPath,
            FileHash = log.FileHash,
            Publisher = log.Publisher,
            UserName = log.UserName,
            Details = log.Details,
            PolicyId = log.PolicyId,
            RuleId = log.RuleId
        }).ToList();

        _dbContext.AuditLogs.AddRange(entities);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Submitted {Count} audit logs", logs.Count);

        return logs.Count;
    }

    /// <summary>
    /// Gets audit logs for a tenant
    /// </summary>
    public async Task<List<AuditLogDTO>> GetAuditLogsByTenantAsync(
        Guid tenantId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool? blockedOnly = null,
        int skip = 0,
        int take = 100)
    {
        var query = _dbContext.AuditLogs
            .Include(al => al.Agent)
            .Where(al => al.Agent.TenantId == tenantId);

        if (startDate.HasValue)
            query = query.Where(al => al.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(al => al.Timestamp <= endDate.Value);

        if (blockedOnly.HasValue)
            query = query.Where(al => al.Blocked == blockedOnly.Value);

        return await query
            .OrderByDescending(al => al.Timestamp)
            .Skip(skip)
            .Take(take)
            .Select(al => new AuditLogDTO
            {
                LogId = al.LogId,
                AgentId = al.AgentId,
                EventType = al.EventType,
                Timestamp = al.Timestamp,
                Blocked = al.Blocked,
                ProcessName = al.ProcessName,
                ProcessPath = al.ProcessPath,
                FileHash = al.FileHash,
                Publisher = al.Publisher,
                UserName = al.UserName,
                Details = al.Details,
                PolicyId = al.PolicyId,
                RuleId = al.RuleId,
                AgentMachineName = al.Agent.MachineName
            })
            .ToListAsync();
    }
}

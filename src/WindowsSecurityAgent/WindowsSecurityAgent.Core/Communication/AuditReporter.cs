using Microsoft.Extensions.Logging;
using Shared.Models.DTOs;
using Shared.Models.Enums;
using WindowsSecurityAgent.Core.Models;
using System.Collections.Concurrent;

namespace WindowsSecurityAgent.Core.Communication;

/// <summary>
/// Collects and reports audit logs to the cloud
/// </summary>
public class AuditReporter
{
    private readonly ILogger<AuditReporter> _logger;
    private readonly CloudClient _cloudClient;
    private readonly Guid _agentId;
    private readonly ConcurrentQueue<AuditLogDTO> _pendingLogs = new();
    private readonly int _maxBatchSize = 100;
    private readonly int _maxQueueSize = 10000;

    public AuditReporter(ILogger<AuditReporter> logger, CloudClient cloudClient, Guid agentId)
    {
        _logger = logger;
        _cloudClient = cloudClient;
        _agentId = agentId;
    }

    /// <summary>
    /// Records a process event for auditing
    /// </summary>
    public void RecordProcessEvent(ProcessInfo processInfo, bool blocked, Guid? policyId, Guid? ruleId, string reason)
    {
        var log = new AuditLogDTO
        {
            LogId = Guid.NewGuid(),
            AgentId = _agentId,
            EventType = blocked ? EventType.InstallationBlocked : EventType.InstallationAllowed,
            Timestamp = DateTime.UtcNow,
            Blocked = blocked,
            ProcessName = processInfo.ProcessName,
            ProcessPath = processInfo.ExecutablePath,
            FileHash = processInfo.FileHash ?? string.Empty,
            Publisher = processInfo.Publisher ?? string.Empty,
            UserName = processInfo.UserName,
            Details = reason,
            PolicyId = policyId,
            RuleId = ruleId
        };

        EnqueueLog(log);
    }

    /// <summary>
    /// Records a file operation event for auditing
    /// </summary>
    public void RecordFileOperation(FileOperationInfo fileInfo, bool blocked, string reason)
    {
        var log = new AuditLogDTO
        {
            LogId = Guid.NewGuid(),
            AgentId = _agentId,
            EventType = EventType.ProtectedFileWrite,
            Timestamp = DateTime.UtcNow,
            Blocked = blocked,
            ProcessName = fileInfo.ProcessName ?? string.Empty,
            ProcessPath = fileInfo.FilePath,
            Details = $"{fileInfo.Operation}: {reason}",
            UserName = string.Empty
        };

        EnqueueLog(log);
    }

    /// <summary>
    /// Records a general event for auditing
    /// </summary>
    public void RecordEvent(EventType eventType, string details)
    {
        var log = new AuditLogDTO
        {
            LogId = Guid.NewGuid(),
            AgentId = _agentId,
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            Blocked = false,
            Details = details,
            ProcessName = string.Empty,
            ProcessPath = string.Empty,
            UserName = string.Empty
        };

        EnqueueLog(log);
    }

    private void EnqueueLog(AuditLogDTO log)
    {
        if (_pendingLogs.Count >= _maxQueueSize)
        {
            _logger.LogWarning("Audit log queue is full, dropping oldest log");
            _pendingLogs.TryDequeue(out _);
        }

        _pendingLogs.Enqueue(log);
        _logger.LogDebug("Audit log queued: {EventType}", log.EventType);
    }

    /// <summary>
    /// Flushes pending audit logs to the cloud
    /// </summary>
    public async Task<int> FlushLogsAsync(CancellationToken cancellationToken = default)
    {
        if (_pendingLogs.IsEmpty)
            return 0;

        var logsToSend = new List<AuditLogDTO>();
        
        // Dequeue up to maxBatchSize logs
        while (logsToSend.Count < _maxBatchSize && _pendingLogs.TryDequeue(out var log))
        {
            logsToSend.Add(log);
        }

        if (!logsToSend.Any())
            return 0;

        _logger.LogInformation("Flushing {Count} audit logs to cloud...", logsToSend.Count);

        var success = await _cloudClient.SubmitAuditLogsAsync(logsToSend, cancellationToken);
        
        if (!success)
        {
            // Re-queue the logs if submission failed
            foreach (var log in logsToSend)
            {
                _pendingLogs.Enqueue(log);
            }
            _logger.LogWarning("Failed to submit audit logs, re-queued for retry");
            return 0;
        }

        _logger.LogInformation("Successfully flushed {Count} audit logs", logsToSend.Count);
        return logsToSend.Count;
    }

    /// <summary>
    /// Gets the number of pending logs in the queue
    /// </summary>
    public int GetPendingLogCount() => _pendingLogs.Count;
}

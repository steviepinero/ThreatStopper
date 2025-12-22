using WindowsSecurityAgent.Core.Monitoring;
using WindowsSecurityAgent.Core.PolicyEngine;
using WindowsSecurityAgent.Core.Communication;
using Shared.Models.Enums;
using Shared.Models.DTOs;

namespace WindowsSecurityAgent.Service;

/// <summary>
/// Main Windows Service worker that orchestrates all security monitoring
/// </summary>
public class AgentWorker : BackgroundService
{
    private readonly ILogger<AgentWorker> _logger;
    private readonly ProcessMonitor _processMonitor;
    private readonly FileSystemMonitor _fileSystemMonitor;
    private readonly PolicyEnforcer _policyEnforcer;
    private readonly PolicySyncService _policySyncService;
    private readonly AuditReporter _auditReporter;
    private readonly CloudClient _cloudClient;
    private readonly IConfiguration _configuration;

    private readonly TimeSpan _policySyncInterval = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _heartbeatInterval = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _auditFlushInterval = TimeSpan.FromSeconds(30);

    public AgentWorker(
        ILogger<AgentWorker> logger,
        ProcessMonitor processMonitor,
        FileSystemMonitor fileSystemMonitor,
        PolicyEnforcer policyEnforcer,
        PolicySyncService policySyncService,
        AuditReporter auditReporter,
        CloudClient cloudClient,
        IConfiguration configuration)
    {
        _logger = logger;
        _processMonitor = processMonitor;
        _fileSystemMonitor = fileSystemMonitor;
        _policyEnforcer = policyEnforcer;
        _policySyncService = policySyncService;
        _auditReporter = auditReporter;
        _cloudClient = cloudClient;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Windows Security Agent starting...");
        
        try
        {
            // Record agent start event
            _auditReporter.RecordEvent(EventType.AgentStarted, "Windows Security Agent service started");

            // Register event handlers
            _processMonitor.ProcessDetected += OnProcessDetected;
            _fileSystemMonitor.FileOperationDetected += OnFileOperationDetected;

            // Start monitors
            _processMonitor.Start();
            _fileSystemMonitor.Start();

            _logger.LogInformation("Process and file system monitors started");

            // Initial policy sync
            await _policySyncService.SyncPoliciesAsync(stoppingToken);

            // Start background tasks
            var policySyncTask = PeriodicPolicySyncAsync(stoppingToken);
            var heartbeatTask = PeriodicHeartbeatAsync(stoppingToken);
            var auditFlushTask = PeriodicAuditFlushAsync(stoppingToken);

            // Wait for cancellation
            await Task.WhenAll(policySyncTask, heartbeatTask, auditFlushTask);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error in Windows Security Agent");
            _auditReporter.RecordEvent(EventType.Error, $"Fatal error: {ex.Message}");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Windows Security Agent stopping...");

        // Stop monitors
        _processMonitor.Stop();
        _fileSystemMonitor.Stop();

        // Unregister event handlers
        _processMonitor.ProcessDetected -= OnProcessDetected;
        _fileSystemMonitor.FileOperationDetected -= OnFileOperationDetected;

        // Flush remaining audit logs
        await _auditReporter.FlushLogsAsync(cancellationToken);

        // Record agent stop event
        _auditReporter.RecordEvent(EventType.AgentStopped, "Windows Security Agent service stopped");
        await _auditReporter.FlushLogsAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Windows Security Agent stopped");
    }

    private void OnProcessDetected(object? sender, Core.Models.ProcessInfo processInfo)
    {
        try
        {
            _logger.LogInformation("Process detected: {ProcessName} (PID: {ProcessId}, Installer: {IsInstaller})",
                processInfo.ProcessName, processInfo.ProcessId, processInfo.IsInstaller);

            // Evaluate against policies
            var (shouldBlock, policyId, ruleId, reason) = _policyEnforcer.EvaluateProcess(processInfo);

            if (shouldBlock)
            {
                _logger.LogWarning("Blocking process: {ProcessName} - Reason: {Reason}",
                    processInfo.ProcessName, reason);

                // Block the process
                var blocked = _policyEnforcer.BlockProcess(processInfo);

                // Record audit log
                _auditReporter.RecordProcessEvent(processInfo, blocked, policyId, ruleId, reason);

                if (!blocked)
                {
                    _logger.LogError("Failed to block process: {ProcessName}", processInfo.ProcessName);
                }
            }
            else
            {
                _logger.LogDebug("Allowing process: {ProcessName} - Reason: {Reason}",
                    processInfo.ProcessName, reason);

                // Record audit log for allowed process
                _auditReporter.RecordProcessEvent(processInfo, false, policyId, ruleId, reason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling process detection for {ProcessName}", processInfo.ProcessName);
        }
    }

    private void OnFileOperationDetected(object? sender, Core.Models.FileOperationInfo fileInfo)
    {
        try
        {
            _logger.LogDebug("File operation detected: {Operation} - {FilePath}",
                fileInfo.Operation, fileInfo.FilePath);

            // For now, just log file operations
            // In a full implementation, you would evaluate against policies
            _auditReporter.RecordFileOperation(fileInfo, false, "Monitored file operation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling file operation for {FilePath}", fileInfo.FilePath);
        }
    }

    private async Task PeriodicPolicySyncAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_policySyncInterval, cancellationToken);
                
                _logger.LogDebug("Starting periodic policy sync...");
                var success = await _policySyncService.SyncPoliciesAsync(cancellationToken);
                
                if (success)
                {
                    _auditReporter.RecordEvent(EventType.PolicyUpdated, "Policies synced from cloud");
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic policy sync");
            }
        }
    }

    private async Task PeriodicHeartbeatAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_heartbeatInterval, cancellationToken);
                
                var heartbeat = new HeartbeatDTO
                {
                    AgentId = Guid.Parse(_configuration["Agent:AgentId"] ?? Guid.Empty.ToString()),
                    Status = AgentStatus.Online,
                    AgentVersion = "1.0.0",
                    Timestamp = DateTime.UtcNow,
                    BlockedEventsLast24h = 0, // Would track this in production
                    LastError = string.Empty
                };

                _logger.LogDebug("Sending heartbeat...");
                await _cloudClient.SendHeartbeatAsync(heartbeat, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending heartbeat");
            }
        }
    }

    private async Task PeriodicAuditFlushAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_auditFlushInterval, cancellationToken);
                
                var pendingCount = _auditReporter.GetPendingLogCount();
                if (pendingCount > 0)
                {
                    _logger.LogDebug("Flushing {Count} pending audit logs...", pendingCount);
                    var flushedCount = await _auditReporter.FlushLogsAsync(cancellationToken);
                    _logger.LogDebug("Flushed {Count} audit logs", flushedCount);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flushing audit logs");
            }
        }
    }
}

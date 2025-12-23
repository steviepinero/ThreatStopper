using Shared.Models.Enums;

namespace Shared.Models.DTOs;

/// <summary>
/// Data transfer object for audit log entries
/// </summary>
public class AuditLogDTO
{
    public Guid LogId { get; set; }
    public Guid AgentId { get; set; }
    public EventType EventType { get; set; }
    public DateTime Timestamp { get; set; }
    public bool Blocked { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string ProcessPath { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public Guid? PolicyId { get; set; }
    public Guid? RuleId { get; set; }
    
    // Agent information
    public string? AgentMachineName { get; set; }
}

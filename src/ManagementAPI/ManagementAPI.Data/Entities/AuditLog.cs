using Shared.Models.Enums;

namespace ManagementAPI.Data.Entities;

/// <summary>
/// Represents an audit log entry
/// </summary>
public class AuditLog
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

    // Navigation properties
    public Agent Agent { get; set; } = null!;
}

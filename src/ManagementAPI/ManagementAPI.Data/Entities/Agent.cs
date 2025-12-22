using Shared.Models.Enums;

namespace ManagementAPI.Data.Entities;

/// <summary>
/// Represents a registered agent
/// </summary>
public class Agent
{
    public Guid AgentId { get; set; }
    public Guid TenantId { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = string.Empty;
    public string ApiKeyHash { get; set; } = string.Empty;
    public AgentStatus Status { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public DateTime RegisteredAt { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<AgentPolicyAssignment> PolicyAssignments { get; set; } = new List<AgentPolicyAssignment>();
}

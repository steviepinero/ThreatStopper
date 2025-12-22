using Shared.Models.Enums;

namespace Shared.Models.DTOs;

/// <summary>
/// Data transfer object for agent information
/// </summary>
public class AgentDTO
{
    public Guid AgentId { get; set; }
    public Guid TenantId { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = string.Empty;
    public AgentStatus Status { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public DateTime RegisteredAt { get; set; }
    public List<Guid> AssignedPolicyIds { get; set; } = new();
}

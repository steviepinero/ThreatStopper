using Shared.Models.Enums;

namespace Shared.Models.DTOs;

/// <summary>
/// Data transfer object for agent heartbeat
/// </summary>
public class HeartbeatDTO
{
    public Guid AgentId { get; set; }
    public AgentStatus Status { get; set; }
    public string AgentVersion { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int BlockedEventsLast24h { get; set; }
    public string LastError { get; set; } = string.Empty;
}

namespace Shared.Models.DTOs;

/// <summary>
/// Response from heartbeat endpoint indicating if policies need to be synced
/// </summary>
public class HeartbeatResponseDTO
{
    public bool PoliciesChanged { get; set; }
    public DateTime? LastPolicyUpdate { get; set; }
}


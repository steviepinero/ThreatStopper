namespace ManagementAPI.Data.Entities;

/// <summary>
/// Links agents to policies
/// </summary>
public class AgentPolicyAssignment
{
    public Guid AssignmentId { get; set; }
    public Guid AgentId { get; set; }
    public Guid PolicyId { get; set; }
    public DateTime AssignedAt { get; set; }

    // Navigation properties
    public Agent Agent { get; set; } = null!;
    public Policy Policy { get; set; } = null!;
}

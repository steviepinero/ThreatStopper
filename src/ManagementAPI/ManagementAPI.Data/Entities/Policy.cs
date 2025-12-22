using Shared.Models.Enums;

namespace ManagementAPI.Data.Entities;

/// <summary>
/// Represents a security policy
/// </summary>
public class Policy
{
    public Guid PolicyId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PolicyMode Mode { get; set; }
    public bool IsActive { get; set; }
    public int Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<PolicyRule> Rules { get; set; } = new List<PolicyRule>();
    public ICollection<AgentPolicyAssignment> AgentAssignments { get; set; } = new List<AgentPolicyAssignment>();
}

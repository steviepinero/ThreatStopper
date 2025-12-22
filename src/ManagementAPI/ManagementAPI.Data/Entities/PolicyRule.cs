using Shared.Models.Enums;

namespace ManagementAPI.Data.Entities;

/// <summary>
/// Represents a rule within a policy
/// </summary>
public class PolicyRule
{
    public Guid RuleId { get; set; }
    public Guid PolicyId { get; set; }
    public RuleType RuleType { get; set; }
    public string Criteria { get; set; } = string.Empty;
    public BlockAction Action { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Policy Policy { get; set; } = null!;
}

using Shared.Models.Enums;

namespace WindowsSecurityAgent.Core.PolicyEngine;

/// <summary>
/// Represents a security policy
/// </summary>
public class Policy
{
    public Guid PolicyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public PolicyMode Mode { get; set; }
    public bool IsActive { get; set; }
    public int Priority { get; set; }
    public List<PolicyRule> Rules { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Represents a policy rule
/// </summary>
public class PolicyRule
{
    public Guid RuleId { get; set; }
    public RuleType RuleType { get; set; }
    public string Criteria { get; set; } = string.Empty;
    public BlockAction Action { get; set; }
    public string Description { get; set; } = string.Empty;
}

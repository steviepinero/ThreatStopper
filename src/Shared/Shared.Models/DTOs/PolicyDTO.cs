using Shared.Models.Enums;

namespace Shared.Models.DTOs;

/// <summary>
/// Data transfer object for policy information
/// </summary>
public class PolicyDTO
{
    public Guid PolicyId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PolicyMode Mode { get; set; }
    public bool IsActive { get; set; }
    public int Priority { get; set; }
    public List<PolicyRuleDTO> Rules { get; set; } = new();
    public List<Guid>? AssignedAgentIds { get; set; } // Optional: if null, assign to all agents; if empty list, assign to none
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Data transfer object for policy rule information
/// </summary>
public class PolicyRuleDTO
{
    public Guid RuleId { get; set; }
    public RuleType RuleType { get; set; }
    public string Criteria { get; set; } = string.Empty;
    public BlockAction Action { get; set; }
    public string Description { get; set; } = string.Empty;
}

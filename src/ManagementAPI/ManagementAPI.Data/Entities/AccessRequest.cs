using Shared.Models.Enums;

namespace ManagementAPI.Data.Entities;

/// <summary>
/// Represents a user request to access a blocked application or URL
/// </summary>
public class AccessRequest
{
    public Guid RequestId { get; set; }
    public Guid AgentId { get; set; }
    public Guid TenantId { get; set; }
    public string ResourceType { get; set; } = string.Empty; // "Executable" or "Url"
    public string ResourceIdentifier { get; set; } = string.Empty; // Path/URL that was blocked
    public string ResourceName { get; set; } = string.Empty; // User-friendly name
    public string UserName { get; set; } = string.Empty; // Windows username
    public string Justification { get; set; } = string.Empty; // User's explanation
    public AccessRequestStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public Guid? ReviewedBy { get; set; } // UserId who approved/denied
    public DateTime? ReviewedAt { get; set; }
    public string ReviewComments { get; set; } = string.Empty;
    public Guid? PolicyId { get; set; }
    public Guid? RuleId { get; set; }
    
    // Navigation properties
    public Agent Agent { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}


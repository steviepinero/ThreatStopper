namespace ManagementAPI.Data.Entities;

/// <summary>
/// Represents an active approval that temporarily allows access to a blocked resource
/// </summary>
public class AccessApproval
{
    public Guid ApprovalId { get; set; }
    public Guid RequestId { get; set; }
    public Guid AgentId { get; set; }
    public Guid TenantId { get; set; }
    public string ResourceType { get; set; } = string.Empty; // "Executable" or "Url"
    public string ResourceIdentifier { get; set; } = string.Empty; // Path/URL/Hash
    public DateTime ApprovedAt { get; set; }
    public DateTime? ExpiresAt { get; set; } // Null for indefinite (URLs), set for exes (1 hour)
    public bool IsActive { get; set; }
    
    // Navigation properties
    public AccessRequest Request { get; set; } = null!;
    public Agent Agent { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}


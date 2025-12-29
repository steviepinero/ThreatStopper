namespace Shared.Models.DTOs;

/// <summary>
/// DTO for checking if a resource has an active approval
/// </summary>
public class AccessApprovalCheckDTO
{
    public Guid AgentId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceIdentifier { get; set; } = string.Empty;
}


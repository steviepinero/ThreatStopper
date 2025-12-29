using Shared.Models.Enums;

namespace Shared.Models.DTOs;

/// <summary>
/// DTO for access request information
/// </summary>
public class AccessRequestDTO
{
    public Guid RequestId { get; set; }
    public Guid AgentId { get; set; }
    public Guid TenantId { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceIdentifier { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
    public AccessRequestStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string ReviewComments { get; set; } = string.Empty;
    public Guid? PolicyId { get; set; }
    public Guid? RuleId { get; set; }
}


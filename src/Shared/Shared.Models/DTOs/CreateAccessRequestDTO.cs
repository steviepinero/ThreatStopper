using Shared.Models.Enums;

namespace Shared.Models.DTOs;

/// <summary>
/// DTO for creating a new access request from an agent
/// </summary>
public class CreateAccessRequestDTO
{
    public Guid AgentId { get; set; }
    public string ResourceType { get; set; } = string.Empty; // "Executable" or "Url"
    public string ResourceIdentifier { get; set; } = string.Empty; // Path, URL, or hash
    public string ResourceName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
    public Guid? PolicyId { get; set; }
    public Guid? RuleId { get; set; }
}


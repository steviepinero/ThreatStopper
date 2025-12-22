namespace Shared.Models.DTOs;

/// <summary>
/// Data transfer object for agent registration
/// </summary>
public class AgentRegistrationDTO
{
    public string MachineName { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string TenantApiKey { get; set; } = string.Empty;
}

/// <summary>
/// Response for agent registration
/// </summary>
public class AgentRegistrationResponseDTO
{
    public Guid AgentId { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

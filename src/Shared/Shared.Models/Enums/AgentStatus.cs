namespace Shared.Models.Enums;

/// <summary>
/// Defines the status of an agent
/// </summary>
public enum AgentStatus
{
    /// <summary>
    /// Agent is online and reporting
    /// </summary>
    Online = 0,
    
    /// <summary>
    /// Agent is offline
    /// </summary>
    Offline = 1,
    
    /// <summary>
    /// Agent has an error
    /// </summary>
    Error = 2,
    
    /// <summary>
    /// Agent is pending registration
    /// </summary>
    Pending = 3
}

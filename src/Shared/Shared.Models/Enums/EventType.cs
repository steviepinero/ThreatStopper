namespace Shared.Models.Enums;

/// <summary>
/// Defines the type of security event
/// </summary>
public enum EventType
{
    /// <summary>
    /// Process creation detected
    /// </summary>
    ProcessCreation = 0,
    
    /// <summary>
    /// Installation blocked
    /// </summary>
    InstallationBlocked = 1,
    
    /// <summary>
    /// Installation allowed
    /// </summary>
    InstallationAllowed = 2,
    
    /// <summary>
    /// File write to protected directory
    /// </summary>
    ProtectedFileWrite = 3,
    
    /// <summary>
    /// Policy updated
    /// </summary>
    PolicyUpdated = 4,
    
    /// <summary>
    /// Agent started
    /// </summary>
    AgentStarted = 5,
    
    /// <summary>
    /// Agent stopped
    /// </summary>
    AgentStopped = 6,
    
    /// <summary>
    /// Error occurred
    /// </summary>
    Error = 7
}

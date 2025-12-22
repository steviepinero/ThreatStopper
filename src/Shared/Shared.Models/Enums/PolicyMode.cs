namespace Shared.Models.Enums;

/// <summary>
/// Defines the policy enforcement mode
/// </summary>
public enum PolicyMode
{
    /// <summary>
    /// Whitelist mode - Block all by default, allow only explicitly approved items
    /// </summary>
    Whitelist = 0,
    
    /// <summary>
    /// Blacklist mode - Allow all by default, block only explicitly denied items
    /// </summary>
    Blacklist = 1
}

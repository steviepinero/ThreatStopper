namespace Shared.Models.Enums;

/// <summary>
/// Defines the action to take when a rule matches
/// </summary>
public enum BlockAction
{
    /// <summary>
    /// Allow the action
    /// </summary>
    Allow = 0,
    
    /// <summary>
    /// Block the action
    /// </summary>
    Block = 1,
    
    /// <summary>
    /// Alert but allow the action
    /// </summary>
    Alert = 2
}

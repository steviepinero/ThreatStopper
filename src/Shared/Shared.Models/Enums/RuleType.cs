namespace Shared.Models.Enums;

/// <summary>
/// Defines the type of rule used for matching
/// </summary>
public enum RuleType
{
    /// <summary>
    /// Match by file hash (SHA-256)
    /// </summary>
    FileHash = 0,
    
    /// <summary>
    /// Match by digital signature certificate
    /// </summary>
    Certificate = 1,
    
    /// <summary>
    /// Match by file path or pattern
    /// </summary>
    Path = 2,
    
    /// <summary>
    /// Match by publisher name
    /// </summary>
    Publisher = 3,
    
    /// <summary>
    /// Match by file name or pattern
    /// </summary>
    FileName = 4
}

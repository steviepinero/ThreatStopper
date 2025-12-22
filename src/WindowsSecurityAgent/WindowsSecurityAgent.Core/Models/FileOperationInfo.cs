namespace WindowsSecurityAgent.Core.Models;

/// <summary>
/// Contains information about a file system operation
/// </summary>
public class FileOperationInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty; // Create, Modify, Delete
    public DateTime DetectedAt { get; set; }
    public string? ProcessName { get; set; }
    public int? ProcessId { get; set; }
}

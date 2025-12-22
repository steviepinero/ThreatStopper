namespace WindowsSecurityAgent.Core.Models;

/// <summary>
/// Contains information about a detected process
/// </summary>
public class ProcessInfo
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string CommandLine { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public bool IsInstaller { get; set; }
    public string? FileHash { get; set; }
    public string? Publisher { get; set; }
    public bool IsSigned { get; set; }
}

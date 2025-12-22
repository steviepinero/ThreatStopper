using System.Text.RegularExpressions;

namespace WindowsSecurityAgent.Core.Utilities;

/// <summary>
/// Detects if a process or file is an installer
/// </summary>
public static class InstallerDetector
{
    private static readonly HashSet<string> InstallerProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "msiexec.exe",
        "setup.exe",
        "install.exe",
        "installer.exe",
        "installutil.exe",
        "unins000.exe",
        "uninst.exe",
        "uninstall.exe",
        "update.exe",
        "updater.exe",
        "setupapi.exe"
    };

    private static readonly HashSet<string> InstallerExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".msi",
        ".msp",
        ".msu",
        ".appx",
        ".appxbundle",
        ".msix",
        ".msixbundle"
    };

    private static readonly Regex InstallerNamePattern = new(
        @"(setup|install|installer|update|updater|uninstall|uninst)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    /// <summary>
    /// Determines if a process is likely an installer
    /// </summary>
    public static bool IsInstaller(string processName, string? executablePath = null, string? commandLine = null)
    {
        if (string.IsNullOrWhiteSpace(processName))
            return false;

        // Check if process name matches known installer names
        if (InstallerProcessNames.Contains(processName))
            return true;

        // Check if process name matches installer pattern
        if (InstallerNamePattern.IsMatch(processName))
            return true;

        // Check if executable path has installer extension
        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            var extension = Path.GetExtension(executablePath);
            if (InstallerExtensions.Contains(extension))
                return true;

            // Check if path contains installer keywords
            if (InstallerNamePattern.IsMatch(executablePath))
                return true;
        }

        // Check command line for installer indicators
        if (!string.IsNullOrWhiteSpace(commandLine))
        {
            // MSI installer commands
            if (commandLine.Contains("/i ", StringComparison.OrdinalIgnoreCase) ||
                commandLine.Contains("/install", StringComparison.OrdinalIgnoreCase) ||
                commandLine.Contains("/quiet", StringComparison.OrdinalIgnoreCase) ||
                commandLine.Contains("/silent", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Check for installer file extensions in command line
            foreach (var ext in InstallerExtensions)
            {
                if (commandLine.Contains(ext, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if a file is an installer based on its path
    /// </summary>
    public static bool IsInstallerFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        var extension = Path.GetExtension(filePath);
        if (InstallerExtensions.Contains(extension))
            return true;

        var fileName = Path.GetFileName(filePath);
        return InstallerNamePattern.IsMatch(fileName);
    }
}

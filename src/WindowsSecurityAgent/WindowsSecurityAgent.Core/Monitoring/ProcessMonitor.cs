using System.Management;
using Microsoft.Extensions.Logging;
using WindowsSecurityAgent.Core.Models;
using WindowsSecurityAgent.Core.Utilities;

namespace WindowsSecurityAgent.Core.Monitoring;

/// <summary>
/// Monitors process creation events using WMI
/// </summary>
public class ProcessMonitor : IDisposable
{
    private readonly ILogger<ProcessMonitor> _logger;
    private ManagementEventWatcher? _processWatcher;
    private bool _isRunning;

    public event EventHandler<ProcessInfo>? ProcessDetected;

    public ProcessMonitor(ILogger<ProcessMonitor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Starts monitoring process creation events
    /// </summary>
    public void Start()
    {
        if (_isRunning)
            return;

        try
        {
            _logger.LogInformation("Starting process monitor...");

            // Create WMI query for process creation events
            var query = new WqlEventQuery("__InstanceCreationEvent", 
                TimeSpan.FromSeconds(1), 
                "TargetInstance ISA 'Win32_Process'");

            _processWatcher = new ManagementEventWatcher(query);
            _processWatcher.EventArrived += OnProcessCreated;
            _processWatcher.Start();

            _isRunning = true;
            _logger.LogInformation("Process monitor started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start process monitor");
            throw;
        }
    }

    /// <summary>
    /// Stops monitoring process creation events
    /// </summary>
    public void Stop()
    {
        if (!_isRunning)
            return;

        try
        {
            _logger.LogInformation("Stopping process monitor...");
            
            if (_processWatcher != null)
            {
                _processWatcher.Stop();
                _processWatcher.EventArrived -= OnProcessCreated;
            }

            _isRunning = false;
            _logger.LogInformation("Process monitor stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping process monitor");
        }
    }

    private void OnProcessCreated(object sender, EventArrivedEventArgs e)
    {
        try
        {
            var targetInstance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            
            var processId = Convert.ToInt32(targetInstance["ProcessId"]);
            var processName = targetInstance["Name"]?.ToString() ?? string.Empty;
            var executablePath = targetInstance["ExecutablePath"]?.ToString() ?? string.Empty;
            var commandLine = targetInstance["CommandLine"]?.ToString() ?? string.Empty;

            // Check if this is an installer process
            var isInstaller = InstallerDetector.IsInstaller(processName, executablePath, commandLine);

            // Only raise event if it's an installer or we want to monitor all processes
            if (isInstaller || ShouldMonitorProcess(processName))
            {
                var processInfo = new ProcessInfo
                {
                    ProcessId = processId,
                    ProcessName = processName,
                    ExecutablePath = executablePath,
                    CommandLine = commandLine,
                    DetectedAt = DateTime.UtcNow,
                    IsInstaller = isInstaller,
                    UserName = GetProcessOwner(targetInstance)
                };

                // Get certificate info if path is available
                if (!string.IsNullOrWhiteSpace(executablePath) && File.Exists(executablePath))
                {
                    processInfo.IsSigned = CertificateValidator.IsFileSigned(executablePath);
                    processInfo.Publisher = CertificateValidator.GetPublisher(executablePath);
                }

                _logger.LogDebug("Process detected: {ProcessName} (PID: {ProcessId}, Installer: {IsInstaller})", 
                    processName, processId, isInstaller);

                ProcessDetected?.Invoke(this, processInfo);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing process creation event");
        }
    }

    private string GetProcessOwner(ManagementBaseObject process)
    {
        try
        {
            // Try to get process owner - this may not always work
            // In a production environment, you might use alternative methods
            return "Unknown"; // Simplified for now
        }
        catch
        {
            return "Unknown";
        }
    }

    private bool ShouldMonitorProcess(string processName)
    {
        // Monitor all processes for policy enforcement
        // Filename blocking, path blocking, and other policy rules need to evaluate all processes
        if (string.IsNullOrWhiteSpace(processName))
            return false;
            
        // Monitor all executable processes (those ending in .exe or having no extension but are processes)
        // WMI's Name property includes the extension (e.g., "notepad.exe")
        return processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || 
               processName.EndsWith(".com", StringComparison.OrdinalIgnoreCase) ||
               processName.EndsWith(".bat", StringComparison.OrdinalIgnoreCase) ||
               processName.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        Stop();
        _processWatcher?.Dispose();
    }
}

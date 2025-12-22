using Microsoft.Extensions.Logging;
using WindowsSecurityAgent.Core.Models;
using WindowsSecurityAgent.Core.Utilities;

namespace WindowsSecurityAgent.Core.Monitoring;

/// <summary>
/// Monitors file system changes in protected directories
/// </summary>
public class FileSystemMonitor : IDisposable
{
    private readonly ILogger<FileSystemMonitor> _logger;
    private readonly List<FileSystemWatcher> _watchers = new();
    private bool _isRunning;

    public event EventHandler<FileOperationInfo>? FileOperationDetected;

    private readonly List<string> _protectedPaths = new()
    {
        @"C:\Program Files",
        @"C:\Program Files (x86)"
    };

    public FileSystemMonitor(ILogger<FileSystemMonitor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Starts monitoring protected directories
    /// </summary>
    public void Start()
    {
        if (_isRunning)
            return;

        try
        {
            _logger.LogInformation("Starting file system monitor...");

            foreach (var path in _protectedPaths)
            {
                if (!Directory.Exists(path))
                {
                    _logger.LogWarning("Protected path does not exist: {Path}", path);
                    continue;
                }

                var watcher = new FileSystemWatcher(path)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true
                };

                watcher.Created += OnFileCreated;
                watcher.Changed += OnFileChanged;
                watcher.Deleted += OnFileDeleted;
                watcher.Renamed += OnFileRenamed;

                _watchers.Add(watcher);
                _logger.LogInformation("Watching protected path: {Path}", path);
            }

            _isRunning = true;
            _logger.LogInformation("File system monitor started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start file system monitor");
            throw;
        }
    }

    /// <summary>
    /// Stops monitoring file system changes
    /// </summary>
    public void Stop()
    {
        if (!_isRunning)
            return;

        try
        {
            _logger.LogInformation("Stopping file system monitor...");

            foreach (var watcher in _watchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Created -= OnFileCreated;
                watcher.Changed -= OnFileChanged;
                watcher.Deleted -= OnFileDeleted;
                watcher.Renamed -= OnFileRenamed;
                watcher.Dispose();
            }

            _watchers.Clear();
            _isRunning = false;
            _logger.LogInformation("File system monitor stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping file system monitor");
        }
    }

    /// <summary>
    /// Adds a custom protected path to monitor
    /// </summary>
    public void AddProtectedPath(string path)
    {
        if (!_protectedPaths.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
            _protectedPaths.Add(path);
            if (_isRunning)
            {
                // Restart to include new path
                Stop();
                Start();
            }
        }
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        HandleFileOperation(e.FullPath, "Create");
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        HandleFileOperation(e.FullPath, "Modify");
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        HandleFileOperation(e.FullPath, "Delete");
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        HandleFileOperation(e.FullPath, "Rename");
    }

    private void HandleFileOperation(string filePath, string operation)
    {
        try
        {
            // Filter out system files and temporary files
            if (ShouldIgnoreFile(filePath))
                return;

            // Check if it's an installer file
            var isInstaller = InstallerDetector.IsInstallerFile(filePath);

            if (isInstaller || ShouldMonitorFile(filePath))
            {
                var fileInfo = new FileOperationInfo
                {
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    Operation = operation,
                    DetectedAt = DateTime.UtcNow
                };

                _logger.LogDebug("File operation detected: {Operation} - {FilePath}", operation, filePath);
                FileOperationDetected?.Invoke(this, fileInfo);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling file operation for {FilePath}", filePath);
        }
    }

    private bool ShouldIgnoreFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath).ToLowerInvariant();
        
        // Ignore temp files and system files
        return fileName.EndsWith(".tmp") ||
               fileName.EndsWith(".temp") ||
               fileName.StartsWith("~") ||
               fileName.Contains(".log");
    }

    private bool ShouldMonitorFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        // Monitor executable files
        return extension == ".exe" || 
               extension == ".dll" || 
               extension == ".sys";
    }

    public void Dispose()
    {
        Stop();
    }
}

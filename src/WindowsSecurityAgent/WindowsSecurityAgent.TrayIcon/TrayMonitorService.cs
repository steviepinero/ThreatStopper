using System.ServiceProcess;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Timers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WindowsSecurityAgent.TrayIcon;

/// <summary>
/// Monitors the Windows Security Agent service and displays a tray icon
/// </summary>
public class TrayMonitorService : IHostedService
{
    private readonly ILogger<TrayMonitorService> _logger;
    private NotifyIcon? _notifyIcon;
    private Thread? _uiThread;
    private System.Timers.Timer? _statusCheckTimer;
    private Form? _hiddenForm;
    private const string ServiceName = "WindowsSecurityAgent";

    public TrayMonitorService(ILogger<TrayMonitorService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _uiThread = new Thread(() =>
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            // Create a hidden form for synchronization context
            _hiddenForm = new Form
            {
                WindowState = FormWindowState.Minimized,
                ShowInTaskbar = false,
                Visible = false
            };

            CreateTrayIcon();
            CheckServiceStatus();

            // Check service status every 5 seconds
            _statusCheckTimer = new System.Timers.Timer(5000);
            _statusCheckTimer.Elapsed += (s, e) => CheckServiceStatusCallback(null);
            _statusCheckTimer.AutoReset = true;
            _statusCheckTimer.Start();

            Application.Run(_hiddenForm);
        })
        {
            IsBackground = false,
            Name = "TrayMonitorThread"
        };
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();

        _logger.LogInformation("Tray monitor started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _statusCheckTimer?.Dispose();

        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        if (_hiddenForm != null)
        {
            _hiddenForm.Close();
            _hiddenForm.Dispose();
            _hiddenForm = null;
        }

        if (_uiThread != null && _uiThread.IsAlive)
        {
            Application.ExitThread();
        }

        _logger.LogInformation("Tray monitor stopped");
        return Task.CompletedTask;
    }

    private void CreateTrayIcon()
    {
        try
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = CreateIcon(),
                Text = "Windows Security Agent - Checking status...",
                Visible = true
            };

            // Create context menu
            var contextMenu = new ContextMenuStrip();
            
            var statusItem = new ToolStripMenuItem("Status: Checking...")
            {
                Enabled = false,
                Name = "statusItem"
            };
            contextMenu.Items.Add(statusItem);
            
            contextMenu.Items.Add(new ToolStripSeparator());
            
            var startServiceItem = new ToolStripMenuItem("Start Service");
            startServiceItem.Click += (s, e) => StartService();
            startServiceItem.Name = "startServiceItem";
            contextMenu.Items.Add(startServiceItem);
            
            var stopServiceItem = new ToolStripMenuItem("Stop Service");
            stopServiceItem.Click += (s, e) => StopService();
            stopServiceItem.Name = "stopServiceItem";
            contextMenu.Items.Add(stopServiceItem);
            
            contextMenu.Items.Add(new ToolStripSeparator());
            
            var aboutItem = new ToolStripMenuItem("About");
            aboutItem.Click += (s, e) => ShowAbout();
            contextMenu.Items.Add(aboutItem);
            
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => Application.Exit();
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => ShowAbout();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tray icon");
        }
    }

    private Icon CreateIcon()
    {
        try
        {
            // Create a 16x16 bitmap with a shield icon
            using var bitmap = new Bitmap(16, 16);
            using var graphics = Graphics.FromImage(bitmap);
            
            // Draw a shield shape
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            // Shield background (blue/green)
            var shieldBrush = new SolidBrush(Color.FromArgb(0, 120, 215));
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(2, 2, 12, 12);
            graphics.FillPath(shieldBrush, path);
            
            // Inner circle (white)
            graphics.FillEllipse(new SolidBrush(Color.White), 5, 5, 6, 6);
            
            var iconHandle = bitmap.GetHicon();
            return Icon.FromHandle(iconHandle);
        }
        catch
        {
            // Fallback to system shield icon
            return SystemIcons.Shield;
        }
    }

    private void CheckServiceStatusCallback(object? state)
    {
        // Marshal to UI thread using the hidden form
        if (_hiddenForm != null && _notifyIcon != null)
        {
            try
            {
                _hiddenForm.BeginInvoke(new Action(() => CheckServiceStatus()));
            }
            catch
            {
                // Application may have exited
            }
        }
    }

    private void CheckServiceStatus()
    {
        try
        {
            using var service = new ServiceController(ServiceName);
            service.Refresh();
            
            var status = service.Status;
            var isRunning = status == ServiceControllerStatus.Running;
            
            UpdateTrayIcon(isRunning, status.ToString());
            
            // Update context menu
            if (_notifyIcon?.ContextMenuStrip != null)
            {
                var statusItem = _notifyIcon.ContextMenuStrip.Items["statusItem"] as ToolStripMenuItem;
                var startItem = _notifyIcon.ContextMenuStrip.Items["startServiceItem"] as ToolStripMenuItem;
                var stopItem = _notifyIcon.ContextMenuStrip.Items["stopServiceItem"] as ToolStripMenuItem;
                
                if (statusItem != null)
                {
                    statusItem.Text = $"Status: {status}";
                }
                
                if (startItem != null)
                {
                    startItem.Enabled = !isRunning;
                }
                
                if (stopItem != null)
                {
                    stopItem.Enabled = isRunning;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check service status");
            UpdateTrayIcon(false, "Service not found");
        }
    }

    private void UpdateTrayIcon(bool isRunning, string status)
    {
        if (_notifyIcon == null) return;

        _notifyIcon.Text = $"Windows Security Agent - {status}";
        
        // Update icon color based on status
        if (isRunning)
        {
            // Green icon for running
            _notifyIcon.Icon = CreateStatusIcon(Color.Green);
        }
        else
        {
            // Red/gray icon for stopped
            _notifyIcon.Icon = CreateStatusIcon(Color.Red);
        }
    }

    private Icon CreateStatusIcon(Color color)
    {
        try
        {
            using var bitmap = new Bitmap(16, 16);
            using var graphics = Graphics.FromImage(bitmap);
            
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.FillEllipse(new SolidBrush(color), 2, 2, 12, 12);
            graphics.FillEllipse(new SolidBrush(Color.White), 5, 5, 6, 6);
            
            var iconHandle = bitmap.GetHicon();
            return Icon.FromHandle(iconHandle);
        }
        catch
        {
            return SystemIcons.Shield;
        }
    }

    private void StartService()
    {
        try
        {
            using var service = new ServiceController(ServiceName);
            if (service.Status != ServiceControllerStatus.Running)
            {
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                
                _notifyIcon?.ShowBalloonTip(
                    2000,
                    "Windows Security Agent",
                    "Service started successfully",
                    ToolTipIcon.Info);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start service");
            MessageBox.Show(
                $"Failed to start service: {ex.Message}",
                "Windows Security Agent",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void StopService()
    {
        try
        {
            using var service = new ServiceController(ServiceName);
            if (service.Status == ServiceControllerStatus.Running)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to stop the Windows Security Agent service?",
                    "Stop Service",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                
                if (result == DialogResult.Yes)
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    
                    _notifyIcon?.ShowBalloonTip(
                        2000,
                        "Windows Security Agent",
                        "Service stopped",
                        ToolTipIcon.Info);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop service");
            MessageBox.Show(
                $"Failed to stop service: {ex.Message}",
                "Windows Security Agent",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ShowAbout()
    {
        try
        {
            using var service = new ServiceController(ServiceName);
            service.Refresh();
            var status = service.Status;
            
            MessageBox.Show(
                "Windows Security Agent\n\n" +
                $"Service Status: {status}\n\n" +
                "This tray icon monitors the Windows Security Agent service.\n\n" +
                "The agent protects your system by:\n" +
                "• Monitoring process execution\n" +
                "• Enforcing security policies\n" +
                "• Blocking unauthorized applications\n" +
                "• URL/domain blocking\n\n" +
                "Right-click the icon to start/stop the service.",
                "Windows Security Agent",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch
        {
            MessageBox.Show(
                "Windows Security Agent\n\n" +
                "Service Status: Not Installed\n\n" +
                "The Windows Security Agent service is not installed on this system.",
                "Windows Security Agent",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}


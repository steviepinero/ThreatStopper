using System.ServiceProcess;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Timers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WindowsSecurityAgent.Core.Services;
using WindowsSecurityAgent.TrayIcon.Forms;

namespace WindowsSecurityAgent.TrayIcon;

/// <summary>
/// Monitors the Windows Security Agent service and displays a tray icon
/// </summary>
public class TrayMonitorService : IHostedService
{
    private readonly ILogger<TrayMonitorService> _logger;
    private readonly AccessRequestManager? _accessRequestManager;
    private NotifyIcon? _notifyIcon;
    private Thread? _uiThread;
    private System.Timers.Timer? _statusCheckTimer;
    private Form? _hiddenForm;
    private const string ServiceName = "WindowsSecurityAgent";

    public TrayMonitorService(ILogger<TrayMonitorService> logger, AccessRequestManager? accessRequestManager = null)
    {
        _logger = logger;
        _accessRequestManager = accessRequestManager;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _uiThread = new Thread(() =>
        {
            try
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

            // Subscribe to access request events if the manager is available
            if (_accessRequestManager != null)
            {
                _accessRequestManager.AccessRequestNeeded += OnAccessRequestNeeded;
                _accessRequestManager.AccessApproved += OnAccessApproved;
                _accessRequestManager.AccessDenied += OnAccessDenied;
            }

            CreateTrayIcon();
            CheckServiceStatus();

            // Check service status every 5 seconds
            _statusCheckTimer = new System.Timers.Timer(5000);
            _statusCheckTimer.Elapsed += (s, e) => CheckServiceStatusCallback(null);
            _statusCheckTimer.AutoReset = true;
            _statusCheckTimer.Start();

                Application.Run(_hiddenForm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in tray monitor UI thread");
                try
                {
                    MessageBox.Show(
                        $"Tray monitor error: {ex.Message}",
                        "ThreatStopper Tray Monitor",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                catch
                {
                    // If MessageBox fails, just log
                    Console.Error.WriteLine($"Tray monitor error: {ex.Message}");
                }
            }
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

        // Unsubscribe from access request events
        if (_accessRequestManager != null)
        {
            _accessRequestManager.AccessRequestNeeded -= OnAccessRequestNeeded;
            _accessRequestManager.AccessApproved -= OnAccessApproved;
            _accessRequestManager.AccessDenied -= OnAccessDenied;
        }

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
            _logger.LogInformation("Creating tray icon...");
            _notifyIcon = new NotifyIcon
            {
                Icon = CreateIcon(),
                Text = "Windows Security Agent - Checking status...",
                Visible = true
            };
            _logger.LogInformation("Tray icon created and set to visible");

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
            
            // Add URL access request option if access request manager is available
            if (_accessRequestManager != null)
            {
                var requestUrlAccessItem = new ToolStripMenuItem("Request URL Access");
                requestUrlAccessItem.Click += (s, e) => RequestUrlAccess();
                contextMenu.Items.Add(requestUrlAccessItem);
                contextMenu.Items.Add(new ToolStripSeparator());
            }
            
            var aboutItem = new ToolStripMenuItem("About");
            aboutItem.Click += (s, e) => ShowAbout();
            contextMenu.Items.Add(aboutItem);
            
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => Application.Exit();
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => ShowAbout();
            
            // Show a welcome notification
            _notifyIcon.ShowBalloonTip(
                3000,
                "ThreatStopper",
                "Tray monitor started. Right-click the icon for options.",
                ToolTipIcon.Info);
            
            _logger.LogInformation("Tray icon fully initialized with context menu");
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
            // Check if running as administrator
            var currentPrincipal = new System.Security.Principal.WindowsPrincipal(
                System.Security.Principal.WindowsIdentity.GetCurrent());
            var isAdmin = currentPrincipal.IsInRole(
                System.Security.Principal.WindowsBuiltInRole.Administrator);

            if (!isAdmin)
            {
                var result = MessageBox.Show(
                    "Starting the service requires administrator privileges.\n\n" +
                    "Would you like to use the Services management console instead?\n\n" +
                    "Click Yes to open Services (you can start the service manually there),\n" +
                    "or click No to cancel.",
                    "Administrator Privileges Required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start("services.msc");
                }
                return;
            }

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
            // Check if running as administrator
            var currentPrincipal = new System.Security.Principal.WindowsPrincipal(
                System.Security.Principal.WindowsIdentity.GetCurrent());
            var isAdmin = currentPrincipal.IsInRole(
                System.Security.Principal.WindowsBuiltInRole.Administrator);

            if (!isAdmin)
            {
                var result = MessageBox.Show(
                    "Stopping the service requires administrator privileges.\n\n" +
                    "Would you like to use the Services management console instead?\n\n" +
                    "Click Yes to open Services (you can stop the service manually there),\n" +
                    "or click No to cancel.",
                    "Administrator Privileges Required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start("services.msc");
                }
                return;
            }

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

    private void RequestUrlAccess()
    {
        try
        {
            if (_accessRequestManager == null)
            {
                MessageBox.Show(
                    "Access request manager is not available.",
                    "ThreatStopper",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (_hiddenForm == null)
                return;

            // Show URL input dialog on UI thread
            _hiddenForm.BeginInvoke(new Action(() =>
            {
                // Create a simple input dialog
                using var inputForm = new Form
                {
                    Text = "ThreatStopper - Request URL Access",
                    Size = new Size(450, 150),
                    StartPosition = FormStartPosition.CenterScreen,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    TopMost = true,
                    Icon = SystemIcons.Shield
                };

                var label = new Label
                {
                    Text = "Enter the URL or domain you need access to:",
                    Location = new Point(20, 20),
                    Size = new Size(400, 20),
                    AutoSize = false
                };

                var urlTextBox = new TextBox
                {
                    Location = new Point(20, 50),
                    Size = new Size(390, 23),
                    Text = "https://"
                };

                var okButton = new Button
                {
                    Text = "OK",
                    Location = new Point(250, 85),
                    Size = new Size(75, 30),
                    DialogResult = DialogResult.OK
                };

                var cancelButton = new Button
                {
                    Text = "Cancel",
                    Location = new Point(335, 85),
                    Size = new Size(75, 30),
                    DialogResult = DialogResult.Cancel
                };

                inputForm.Controls.Add(label);
                inputForm.Controls.Add(urlTextBox);
                inputForm.Controls.Add(okButton);
                inputForm.Controls.Add(cancelButton);
                inputForm.AcceptButton = okButton;
                inputForm.CancelButton = cancelButton;

                if (inputForm.ShowDialog() == DialogResult.OK)
                {
                    var urlInput = urlTextBox.Text.Trim();
                    if (string.IsNullOrWhiteSpace(urlInput))
                        return;

                    // Normalize URL
                    var url = urlInput;
                    if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                        !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        url = "https://" + url;
                    }

                    // Extract domain name for display
                    string domainName;
                    try
                    {
                        var uri = new Uri(url);
                        domainName = uri.Host;
                    }
                    catch
                    {
                        // If URL parsing fails, use the input as-is
                        domainName = urlInput;
                        url = urlInput;
                    }

                    // Show access request form
                    using var form = new AccessRequestForm("Url", domainName);
                    var result = form.ShowDialog();
                    
                    if (result == DialogResult.OK && form.Submitted)
                    {
                        // Submit the access request
                        _ = Task.Run(async () =>
                        {
                            await _accessRequestManager.RequestAccessAsync(
                                "Url",
                                url,
                                domainName,
                                Environment.UserName,
                                null,
                                null);
                        });
                        
                        // Show success notification
                        _notifyIcon?.ShowBalloonTip(
                            3000,
                            "ThreatStopper - Request Submitted",
                            $"Your access request for {domainName} has been submitted for review.",
                            ToolTipIcon.Info);
                    }
                }
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting URL access");
            MessageBox.Show(
                $"Failed to submit URL access request: {ex.Message}",
                "ThreatStopper",
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

    private void OnAccessRequestNeeded(object? sender, AccessRequestEventArgs e)
    {
        try
        {
            // Show the access request form on the UI thread
            if (_hiddenForm != null)
            {
                _hiddenForm.BeginInvoke(new Action(() =>
                {
                    using var form = new AccessRequestForm(e.ResourceType, e.ResourceName);
                    var result = form.ShowDialog();
                    
                    if (result == DialogResult.OK && form.Submitted)
                    {
                        e.Justification = form.Justification;
                        e.Complete(true);
                        
                        // Show success notification
                        _notifyIcon?.ShowBalloonTip(
                            3000,
                            "ThreatStopper - Request Submitted",
                            $"Your access request for {e.ResourceName} has been submitted for review.",
                            ToolTipIcon.Info);
                    }
                    else
                    {
                        e.Complete(false);
                    }
                }));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing access request dialog");
            e.Complete(false);
        }
    }

    private void OnAccessApproved(object? sender, AccessApprovalEventArgs e)
    {
        try
        {
            // Show popup form on the UI thread
            if (_hiddenForm != null)
            {
                _hiddenForm.BeginInvoke(new Action(() =>
                {
                    using var form = new AccessApprovalForm(e.ResourceType, e.ResourceName, e.ExpiresAt, true);
                    form.ShowDialog();
                    
                    // Also show balloon tip for notification
                    _notifyIcon?.ShowBalloonTip(
                        5000,
                        "ThreatStopper - Access Approved",
                        $"Your request for {e.ResourceName} has been approved. You can now use this application.",
                        ToolTipIcon.Info);
                }));
            }
            else
            {
                // Fallback to balloon tip if form is not available
                var expirationText = e.ExpiresAt.HasValue 
                    ? $" (expires at {e.ExpiresAt:g})" 
                    : " (indefinite)";
                
                _notifyIcon?.ShowBalloonTip(
                    5000,
                    "ThreatStopper - Access Approved",
                    $"Your request for {e.ResourceName} has been approved{expirationText}. You can now use this application.",
                    ToolTipIcon.Info);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing approval notification");
        }
    }

    private void OnAccessDenied(object? sender, AccessApprovalEventArgs e)
    {
        try
        {
            var reasonText = !string.IsNullOrWhiteSpace(e.DenialReason)
                ? $"\n\nReason: {e.DenialReason}"
                : string.Empty;
            
            _notifyIcon?.ShowBalloonTip(
                5000,
                "Access Denied",
                $"Your access request for {e.ResourceName} has been denied.{reasonText}",
                ToolTipIcon.Warning);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing denial notification");
        }
    }
}


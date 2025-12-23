using System.Windows.Forms;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WindowsSecurityAgent.TrayIcon;

/// <summary>
/// Service that displays a system tray icon when the agent is running
/// </summary>
public class TrayIconService : IHostedService
{
    private readonly ILogger<TrayIconService> _logger;
    private NotifyIcon? _notifyIcon;
    private Thread? _uiThread;

    public TrayIconService(ILogger<TrayIconService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Only show tray icon if running in interactive mode (not as a service)
        // Services run in session 0 and can't show UI directly
        if (Environment.UserInteractive)
        {
            _uiThread = new Thread(() =>
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetHighDpiMode(HighDpiMode.SystemAware);

                CreateTrayIcon();
                Application.Run();
            })
            {
                IsBackground = false,
                Name = "TrayIconThread"
            };
            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.Start();

            _logger.LogInformation("System tray icon started");
        }
        else
        {
            _logger.LogInformation("Running as Windows Service - tray icon not available (use TrayMonitor app)");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        if (_uiThread != null && _uiThread.IsAlive)
        {
            Application.ExitThread();
        }

        _logger.LogInformation("System tray icon stopped");
        return Task.CompletedTask;
    }

    private void CreateTrayIcon()
    {
        try
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = CreateIcon(),
                Text = "Windows Security Agent - Running",
                Visible = true
            };

            // Create context menu
            var contextMenu = new ContextMenuStrip();
            
            var statusItem = new ToolStripMenuItem("Status: Running")
            {
                Enabled = false
            };
            contextMenu.Items.Add(statusItem);
            
            contextMenu.Items.Add(new ToolStripSeparator());
            
            var aboutItem = new ToolStripMenuItem("About");
            aboutItem.Click += (s, e) => ShowAbout();
            contextMenu.Items.Add(aboutItem);
            
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => Application.Exit();
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => ShowAbout();

            _notifyIcon.BalloonTipTitle = "Windows Security Agent";
            _notifyIcon.BalloonTipText = "Agent is running and protecting your system";
            _notifyIcon.ShowBalloonTip(3000);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tray icon");
        }
    }

    private Icon CreateIcon()
    {
        // Create a simple icon programmatically
        // In production, you'd use an embedded .ico file
        try
        {
            // Create a 16x16 bitmap with a shield icon
            using var bitmap = new Bitmap(16, 16);
            using var graphics = Graphics.FromImage(bitmap);
            
            // Draw a simple shield shape
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.FillEllipse(new SolidBrush(Color.Green), 2, 2, 12, 12);
            graphics.FillEllipse(new SolidBrush(Color.White), 4, 4, 8, 8);
            
            var iconHandle = bitmap.GetHicon();
            return Icon.FromHandle(iconHandle);
        }
        catch
        {
            // Fallback to system icon
            return SystemIcons.Shield;
        }
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            "Windows Security Agent\n\n" +
            "Status: Running\n" +
            "Protecting your system from unauthorized applications and processes.\n\n" +
            "The agent monitors:\n" +
            "• Process execution\n" +
            "• File system changes\n" +
            "• URL/domain blocking\n\n" +
            "Managed through the Admin Portal.",
            "Windows Security Agent",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    public void UpdateStatus(string status, string tooltip)
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Text = tooltip;
            _notifyIcon.BalloonTipText = status;
        }
    }
}


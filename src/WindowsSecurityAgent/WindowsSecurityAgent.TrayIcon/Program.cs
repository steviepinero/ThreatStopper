using System.ServiceProcess;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WindowsSecurityAgent.TrayIcon;

namespace WindowsSecurityAgent.TrayIcon;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        var builder = Host.CreateApplicationBuilder(args);
        
        builder.Services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        builder.Services.AddSingleton<TrayMonitorService>();
        builder.Services.AddHostedService<TrayMonitorService>(sp => sp.GetRequiredService<TrayMonitorService>());

        var host = builder.Build();

        try
        {
            host.Run();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to start tray monitor: {ex.Message}",
                "Windows Security Agent Tray Monitor",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}


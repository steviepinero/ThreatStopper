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
            logging.SetMinimumLevel(LogLevel.Information);
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
            var errorMsg = $"Failed to start tray monitor: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
            Console.Error.WriteLine(errorMsg);
            MessageBox.Show(
                $"Failed to start tray monitor: {ex.Message}",
                "Windows Security Agent Tray Monitor",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Environment.Exit(1);
        }
    }
}


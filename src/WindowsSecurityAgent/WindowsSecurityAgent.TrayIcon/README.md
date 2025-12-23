# Windows Security Agent Tray Monitor

A system tray application that monitors the Windows Security Agent service and displays a visual indicator when the service is running.

## Features

- **System Tray Icon**: Shows a shield icon in the system tray
- **Status Monitoring**: Icon color changes based on service status
  - Green: Service is running
  - Red: Service is stopped
- **Quick Actions**: Right-click menu allows:
  - View service status
  - Start/Stop the service
  - View about information
- **Auto-Start**: Can be configured to start with Windows

## Building

Build the project using .NET SDK:

```powershell
cd src\WindowsSecurityAgent\WindowsSecurityAgent.TrayIcon
dotnet build -c Release
```

The executable will be in `bin\Release\net10.0\WindowsSecurityAgent.TrayIcon.exe`

## Usage

### Manual Start

Run the executable directly or use the PowerShell script:

```powershell
.\Start-TrayMonitor.ps1
```

### Add to Startup

To automatically start the tray monitor when Windows starts:

```powershell
.\Start-TrayMonitor.ps1 -InstallStartup
```

This creates a shortcut in the Windows Startup folder.

### Stop the Tray Monitor

Right-click the tray icon and select "Exit", or close it from Task Manager.

## How It Works

The tray monitor:
1. Monitors the `WindowsSecurityAgent` service status every 5 seconds
2. Updates the tray icon color based on service status
3. Provides a context menu for quick service control
4. Shows balloon notifications when service status changes

## Requirements

- Windows 10/11
- .NET 10.0 Runtime
- Windows Security Agent service must be installed

## Notes

- The tray monitor runs in the user's session (not as a service)
- It requires no administrator privileges (except when starting/stopping the service)
- The service itself runs as a Windows Service in the background
- The tray icon is optional - the service works without it


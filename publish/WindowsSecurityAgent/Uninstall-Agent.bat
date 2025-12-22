@echo off
REM Windows Security Agent - Uninstallation Launcher
REM This batch file launches the PowerShell uninstallation script

echo ====================================================================
echo Windows Security Agent - Uninstallation
echo ====================================================================
echo.

REM Check if running as Administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This uninstaller must be run as Administrator!
    echo.
    echo Right-click this file and select "Run as Administrator"
    echo.
    pause
    exit /b 1
)

echo Running uninstallation script...
echo.

REM Run PowerShell script
powershell.exe -ExecutionPolicy Bypass -File "%~dp0Uninstall-Agent.ps1"

echo.
echo ====================================================================
echo Uninstallation script completed
echo ====================================================================
echo.
pause


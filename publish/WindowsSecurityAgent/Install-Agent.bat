@echo off
REM Windows Security Agent - Installation Launcher
REM This batch file launches the PowerShell installation script

echo ====================================================================
echo Windows Security Agent - Installation
echo ====================================================================
echo.

REM Check if running as Administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This installer must be run as Administrator!
    echo.
    echo Right-click this file and select "Run as Administrator"
    echo.
    pause
    exit /b 1
)

echo Running installation script...
echo.

REM Run PowerShell script
powershell.exe -ExecutionPolicy Bypass -File "%~dp0Install-Agent.ps1"

echo.
echo ====================================================================
echo Installation script completed
echo ====================================================================
echo.
pause


@echo off
REM Windows Security Agent - Configuration Launcher (Auto-elevate)
REM This batch file launches the PowerShell configuration script with administrator privileges

echo ====================================================================
echo Windows Security Agent - Configuration
echo ====================================================================
echo.
echo This script will configure the agent for localhost testing...
echo.

REM Run PowerShell script as Administrator
powershell.exe -ExecutionPolicy Bypass -Command "Start-Process powershell.exe -ArgumentList '-ExecutionPolicy Bypass -File \"%~dp0Configure-Agent.ps1\"' -Verb RunAs -Wait"

echo.
echo Press any key to exit...
pause >nul


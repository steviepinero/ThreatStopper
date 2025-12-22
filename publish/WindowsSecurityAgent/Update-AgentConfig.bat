@echo off
REM Windows Security Agent - Update Configuration (Auto-elevate)
REM This batch file updates the agent with a unique ID

echo ====================================================================
echo Windows Security Agent - Update Configuration
echo ====================================================================
echo.
echo This will update the agent with a unique ID for your machine...
echo.

REM Run PowerShell script as Administrator
powershell.exe -ExecutionPolicy Bypass -Command "Start-Process powershell.exe -ArgumentList '-ExecutionPolicy Bypass -File \"%~dp0Update-AgentConfig.ps1\"' -Verb RunAs -Wait"

echo.
echo Press any key to exit...
pause >nul


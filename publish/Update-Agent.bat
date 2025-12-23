@echo off
REM Windows Security Agent - Update Launcher (Auto-elevate)

echo ====================================================================
echo ThreatStopper Agent - Update
echo ====================================================================
echo.
echo This will update the agent with URL blocking capabilities...
echo.

REM Run PowerShell script as Administrator
powershell.exe -ExecutionPolicy Bypass -Command "Start-Process powershell.exe -ArgumentList '-ExecutionPolicy Bypass -File \"%~dp0Update-Agent.ps1\"' -Verb RunAs -Wait"

echo.
echo Press any key to exit...
pause >nul


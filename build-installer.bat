@echo off
setlocal
cd /d "%~dp0"

rem Overline - build the Windows installer (.exe)
rem Delegates to build-installer.ps1 (PowerShell handles < > chars in version parsing).

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build-installer.ps1" %*
if errorlevel 1 (
    echo.
    echo [Overline] Echec du build installer.
    pause
    exit /b 1
)
pause
@echo off
setlocal
cd /d "%~dp0"

rem ============================================================
rem  Overline - start script
rem  Builds the solution (if needed) then launches the WPF app.
rem ============================================================

rem Locate dotnet: PATH first, then the standard install location.
set "DOTNET=dotnet"
where dotnet >nul 2>nul
if errorlevel 1 (
    if exist "%ProgramFiles%\dotnet\dotnet.exe" (
        set "DOTNET=%ProgramFiles%\dotnet\dotnet.exe"
    ) else (
        echo [Overline] .NET SDK introuvable. Installe le SDK 10.0 depuis:
        echo [Overline] https://dotnet.microsoft.com/download/dotnet/10.0
        pause
        exit /b 1
    )
)

echo [Overline] Build...
"%DOTNET%" build Overline.slnx -c Debug --nologo -v q
if errorlevel 1 (
    echo.
    echo [Overline] Echec du build. Verifie les erreurs ci-dessus.
    pause
    exit /b 1
)

echo [Overline] Lancement de Overline.App...
"%DOTNET%" run --project Overline.App -c Debug --no-build
if errorlevel 1 (
    echo.
    echo [Overline] L'application s'est arretee avec une erreur.
    pause
    exit /b 1
)

endlocal

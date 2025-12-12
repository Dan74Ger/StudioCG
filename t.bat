@echo off
chcp 65001 >nul
title Stop StudioCG

echo ╔══════════════════════════════════════════════════════════════╗
echo ║              STOP APPLICAZIONE STUDIOCG                      ║
echo ╚══════════════════════════════════════════════════════════════╝
echo.

echo [INFO] Terminazione processi StudioCG in corso...
echo.

:: Termina il processo dotnet che esegue StudioCG.Web
taskkill /F /IM "StudioCG.Web.exe" 2>nul
if %ERRORLEVEL% EQU 0 (
    echo [OK] Processo StudioCG.Web.exe terminato.
) else (
    echo [INFO] StudioCG.Web.exe non in esecuzione.
)

:: Termina eventuali processi dotnet correlati
for /f "tokens=2" %%a in ('tasklist /FI "IMAGENAME eq dotnet.exe" /FO LIST ^| find "PID:"') do (
    wmic process where "ProcessId=%%a" get CommandLine 2>nul | find "StudioCG" >nul
    if not errorlevel 1 (
        taskkill /F /PID %%a 2>nul
        echo [OK] Processo dotnet PID %%a terminato.
    )
)

echo.
echo ╔══════════════════════════════════════════════════════════════╗
echo ║              APPLICAZIONE TERMINATA!                         ║
echo ╚══════════════════════════════════════════════════════════════╝
echo.
timeout /t 3

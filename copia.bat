@echo off
chcp 65001 >nul
title Backup StudioCG

echo ╔══════════════════════════════════════════════════════════════╗
echo ║           BACKUP STUDIOCG - PROGRAMMA + DATABASE             ║
echo ╚══════════════════════════════════════════════════════════════╝
echo.

:: Imposta variabili
set BACKUP_DIR=C:\STUDIOCG\Backup
set SOURCE_DIR=C:\STUDIOCG\StudioCG
set DATE_TIME=%date:~6,4%%date:~3,2%%date:~0,2%_%time:~0,2%%time:~3,2%%time:~6,2%
set DATE_TIME=%DATE_TIME: =0%
set ZIP_NAME=StudioCG_Backup_%DATE_TIME%.zip
set DB_NAME=StudioCG
set DB_BACKUP_NAME=StudioCG_%DATE_TIME%.bak

:: Crea cartella backup se non esiste
if not exist "%BACKUP_DIR%" (
    echo [INFO] Creazione cartella backup...
    mkdir "%BACKUP_DIR%"
)

if not exist "%BACKUP_DIR%\Database" (
    mkdir "%BACKUP_DIR%\Database"
)

echo.
echo [1/3] Backup del database SQL Express...
echo.

:: Backup del database SQL Express
sqlcmd -S .\SQLEXPRESS -Q "BACKUP DATABASE [%DB_NAME%] TO DISK = N'%BACKUP_DIR%\Database\%DB_BACKUP_NAME%' WITH NOFORMAT, NOINIT, NAME = N'%DB_NAME%-Full Database Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10"

if %ERRORLEVEL% NEQ 0 (
    echo [ATTENZIONE] Backup database non riuscito. Continuo con il backup dei file...
) else (
    echo [OK] Backup database completato: %DB_BACKUP_NAME%
)

echo.
echo [2/3] Creazione archivio ZIP del programma...
echo.

:: Crea ZIP del programma (escludendo bin, obj e file temporanei)
powershell -Command "& { $source = '%SOURCE_DIR%'; $dest = '%BACKUP_DIR%\%ZIP_NAME%'; $tempDir = $env:TEMP + '\StudioCG_Temp'; if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force }; New-Item -ItemType Directory -Path $tempDir -Force | Out-Null; Get-ChildItem -Path $source -Exclude 'bin','obj','.vs','*.user' | Copy-Item -Destination $tempDir -Recurse -Force; Compress-Archive -Path ($tempDir + '\*') -DestinationPath $dest -Force; Remove-Item $tempDir -Recurse -Force; Write-Host '[OK] Archivio creato: %ZIP_NAME%' }"

echo.
echo [3/3] Pulizia backup vecchi (mantiene ultimi 10)...
echo.

:: Mantieni solo gli ultimi 10 backup ZIP
powershell -Command "& { Get-ChildItem '%BACKUP_DIR%\StudioCG_Backup_*.zip' | Sort-Object CreationTime -Descending | Select-Object -Skip 10 | Remove-Item -Force }"

:: Mantieni solo gli ultimi 10 backup database
powershell -Command "& { Get-ChildItem '%BACKUP_DIR%\Database\StudioCG_*.bak' | Sort-Object CreationTime -Descending | Select-Object -Skip 10 | Remove-Item -Force }"

echo.
echo ╔══════════════════════════════════════════════════════════════╗
echo ║                    BACKUP COMPLETATO!                        ║
echo ╠══════════════════════════════════════════════════════════════╣
echo ║  Cartella: %BACKUP_DIR%
echo ║  ZIP:      %ZIP_NAME%
echo ║  Database: %DB_BACKUP_NAME%
echo ╚══════════════════════════════════════════════════════════════╝
echo.
pause

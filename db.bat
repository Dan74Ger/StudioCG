@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ============================================
echo   RIPRISTINO DATABASE STUDIOCG
echo ============================================
echo.

set BACKUP_FOLDER=C:\STUDIOCG\backup
set DATABASE_NAME=StudioCG
set SERVER_NAME=.\SQLEXPRESS
set DATA_PATH=C:\STUDIOCG\Databaseserverripristino

:: Crea cartella Data se non esiste
if not exist "%DATA_PATH%" mkdir "%DATA_PATH%"

echo File di backup disponibili in %BACKUP_FOLDER%:
echo.
dir /b "%BACKUP_FOLDER%\*.bak" 2>nul
if errorlevel 1 (
    echo ERRORE: Nessun file .bak trovato nella cartella %BACKUP_FOLDER%
    pause
    exit /b 1
)

echo.
echo ============================================
set /p BACKUP_FILE="Inserisci il nome completo del file (es: StudioCG_20260112.bak): "

if not exist "%BACKUP_FOLDER%\%BACKUP_FILE%" (
    echo.
    echo ERRORE: Il file %BACKUP_FOLDER%\%BACKUP_FILE% non esiste!
    pause
    exit /b 1
)

echo.
echo ============================================
echo ATTENZIONE: Stai per ripristinare il database %DATABASE_NAME%
echo File: %BACKUP_FOLDER%\%BACKUP_FILE%
echo.
echo TUTTI I DATI ATTUALI VERRANNO SOVRASCRITTI!
echo ============================================
echo.
set /p CONFIRM="Sei sicuro? Digita SI per confermare: "

if /i not "%CONFIRM%"=="SI" (
    echo.
    echo Operazione annullata.
    pause
    exit /b 0
)

echo.
echo Ripristino in corso...
echo.

:: Chiudi tutte le connessioni al database
echo Chiusura connessioni...
sqlcmd -S %SERVER_NAME% -E -Q "IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '%DATABASE_NAME%') ALTER DATABASE [%DATABASE_NAME%] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;" 2>nul

:: Ripristina con MOVE per spostare i file nel percorso locale
echo Ripristino database...
sqlcmd -S %SERVER_NAME% -E -Q "RESTORE DATABASE [%DATABASE_NAME%] FROM DISK = N'%BACKUP_FOLDER%\%BACKUP_FILE%' WITH REPLACE, RECOVERY, MOVE N'StudioCG' TO N'%DATA_PATH%\StudioCG.mdf', MOVE N'StudioCG_log' TO N'%DATA_PATH%\StudioCG_log.ldf';"

if errorlevel 1 (
    echo.
    echo ERRORE durante il ripristino del database!
    echo.
    echo Provo con percorso SQL Server standard...
    sqlcmd -S %SERVER_NAME% -E -Q "RESTORE DATABASE [%DATABASE_NAME%] FROM DISK = N'%BACKUP_FOLDER%\%BACKUP_FILE%' WITH REPLACE, RECOVERY, MOVE N'StudioCG' TO N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA\StudioCG.mdf', MOVE N'StudioCG_log' TO N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA\StudioCG_log.ldf';"
    
    if errorlevel 1 (
        echo.
        echo ERRORE: Ripristino fallito!
        echo Prova ad eseguire questo script come AMMINISTRATORE.
        sqlcmd -S %SERVER_NAME% -E -Q "IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '%DATABASE_NAME%') ALTER DATABASE [%DATABASE_NAME%] SET MULTI_USER;" 2>nul
        pause
        exit /b 1
    )
)

:: Rimetti in multi-user
sqlcmd -S %SERVER_NAME% -E -Q "ALTER DATABASE [%DATABASE_NAME%] SET MULTI_USER;" 2>nul

echo.
echo ============================================
echo RIPRISTINO COMPLETATO CON SUCCESSO!
echo Database: %DATABASE_NAME%
echo File: %BACKUP_FILE%
echo ============================================
echo.
pause

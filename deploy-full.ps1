<#
.SYNOPSIS
    DEPLOY COMPLETO - StudioCG
    Prima installazione: Database + Sito

.DESCRIPTION
    Esegue il deploy completo dell'applicazione StudioCG
    incluso backup/restore database e copia sito.
#>

param(
    [string]$ServerName = "SRV-DC",
    [string]$ServerIP = "192.168.1.112",
    [string]$SitePath = "c:\inetpub\studiocg",
    [string]$AppPoolName = "StudioCG",
    [string]$SiteName = "StudioCG.Web",
    [int]$Port = 5252
)

$ErrorActionPreference = "Continue"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$PublishDir = Join-Path $ScriptDir "publish"
$BackupDir = Join-Path $ScriptDir "backups"
$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

Write-Host ""
Write-Host "==============================================================" -ForegroundColor Cyan
Write-Host "          DEPLOY COMPLETO - StudioCG                          " -ForegroundColor Cyan
Write-Host "          Server: $ServerName ($ServerIP)                     " -ForegroundColor Cyan
Write-Host "==============================================================" -ForegroundColor Cyan
Write-Host ""

# STEP 1: Verifica connettivita server
Write-Host "[1/7] Verifica connettivita server..." -ForegroundColor Yellow
if (!(Test-Connection -ComputerName $ServerIP -Count 1 -Quiet)) {
    Write-Host "ERRORE: Server $ServerIP non raggiungibile!" -ForegroundColor Red
    exit 1
}
Write-Host "      Server raggiungibile." -ForegroundColor Green

# STEP 2: Pubblicazione applicazione
Write-Host "[2/7] Pubblicazione applicazione..." -ForegroundColor Yellow
if (Test-Path $PublishDir) {
    Remove-Item -Path $PublishDir -Recurse -Force
}

$publishResult = & dotnet publish -c Release -o $PublishDir 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRORE: Pubblicazione fallita!" -ForegroundColor Red
    Write-Host $publishResult -ForegroundColor Red
    exit 1
}
Write-Host "      Pubblicazione completata." -ForegroundColor Green

# STEP 3: Backup database locale
Write-Host "[3/7] Backup database locale..." -ForegroundColor Yellow
if (!(Test-Path $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir | Out-Null
}
$BackupFile = Join-Path $BackupDir "StudioCG_$Timestamp.bak"

$SqlBackupCmd = "BACKUP DATABASE [StudioCG] TO DISK = N'$BackupFile' WITH FORMAT, INIT, NAME = N'StudioCG-Full Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10"
& sqlcmd -S ".\SQLEXPRESS" -Q $SqlBackupCmd 2>&1 | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRORE: Backup database fallito!" -ForegroundColor Red
    exit 1
}
Write-Host "      Backup creato: $BackupFile" -ForegroundColor Green

# STEP 4: Copia backup sul server
Write-Host "[4/7] Copia backup sul server..." -ForegroundColor Yellow
$ServerTempPath = "\\$ServerName\c`$\temp"
$ServerBackupFile = "StudioCG_$Timestamp.bak"

if (!(Test-Path $ServerTempPath)) {
    New-Item -ItemType Directory -Path $ServerTempPath -Force | Out-Null
}
Copy-Item -Path $BackupFile -Destination "$ServerTempPath\$ServerBackupFile" -Force
Write-Host "      Backup copiato sul server." -ForegroundColor Green

# STEP 5: Ripristino database sul server
Write-Host "[5/7] Ripristino database sul server..." -ForegroundColor Yellow

$SqlRestoreCmd = @"
USE [master];
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'StudioCG')
BEGIN
    ALTER DATABASE [StudioCG] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [StudioCG];
END;
RESTORE DATABASE [StudioCG] FROM DISK = N'C:\temp\$ServerBackupFile' WITH FILE = 1, NOUNLOAD, STATS = 5;
"@

$restoreResult = & sqlcmd -S "$ServerIP\SQLEXPRESS" -Q $SqlRestoreCmd 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ATTENZIONE: Ripristino database potrebbe richiedere configurazione manuale." -ForegroundColor Yellow
    Write-Host "Errore: $restoreResult" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Eseguire manualmente su SQL Server del server:" -ForegroundColor Cyan
    Write-Host "RESTORE DATABASE StudioCG FROM DISK = 'C:\temp\$ServerBackupFile'" -ForegroundColor White
}
else {
    Write-Host "      Database ripristinato." -ForegroundColor Green
}

# STEP 6: Copia sito sul server
Write-Host "[6/7] Copia sito sul server..." -ForegroundColor Yellow
$ServerSitePath = "\\$ServerName\c`$\inetpub\studiocg"

if (!(Test-Path $ServerSitePath)) {
    New-Item -ItemType Directory -Path $ServerSitePath -Force | Out-Null
}

# Pulisci e copia
Get-ChildItem -Path $ServerSitePath -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -Recurse -ErrorAction SilentlyContinue
Copy-Item -Path "$PublishDir\*" -Destination $ServerSitePath -Recurse -Force
Write-Host "      Sito copiato su $ServerSitePath" -ForegroundColor Green

# STEP 7: Istruzioni configurazione IIS
Write-Host "[7/7] Configurazione IIS..." -ForegroundColor Yellow
Write-Host ""
Write-Host "==============================================================" -ForegroundColor Magenta
Write-Host "  CONFIGURAZIONE MANUALE IIS RICHIESTA                        " -ForegroundColor Magenta
Write-Host "==============================================================" -ForegroundColor Magenta
Write-Host ""
Write-Host "Sul server $ServerName esegui questi passaggi:" -ForegroundColor White
Write-Host ""
Write-Host "1. Apri IIS Manager" -ForegroundColor Cyan
Write-Host ""
Write-Host "2. Crea Application Pool:" -ForegroundColor Cyan
Write-Host "   - Nome: $AppPoolName" -ForegroundColor White
Write-Host "   - .NET CLR Version: No Managed Code" -ForegroundColor White
Write-Host "   - Managed Pipeline Mode: Integrated" -ForegroundColor White
Write-Host ""
Write-Host "3. Crea Sito Web:" -ForegroundColor Cyan
Write-Host "   - Nome: $SiteName" -ForegroundColor White
Write-Host "   - Application Pool: $AppPoolName" -ForegroundColor White
Write-Host "   - Physical Path: $SitePath" -ForegroundColor White
Write-Host "   - Binding 1: http, All Unassigned, Port $Port" -ForegroundColor White
Write-Host "   - Binding 2: http, studiocg.web, Port 80" -ForegroundColor White
Write-Host ""
Write-Host "4. Configura SQL Server (sul server):" -ForegroundColor Cyan
Write-Host "   Esegui in SQL Server Management Studio:" -ForegroundColor White
Write-Host ""
Write-Host "   USE [master]" -ForegroundColor Gray
Write-Host "   GO" -ForegroundColor Gray
Write-Host "   CREATE LOGIN [IIS APPPOOL\$AppPoolName] FROM WINDOWS" -ForegroundColor Gray
Write-Host "   GO" -ForegroundColor Gray
Write-Host "   USE [StudioCG]" -ForegroundColor Gray
Write-Host "   GO" -ForegroundColor Gray
Write-Host "   CREATE USER [IIS APPPOOL\$AppPoolName] FOR LOGIN [IIS APPPOOL\$AppPoolName]" -ForegroundColor Gray
Write-Host "   GO" -ForegroundColor Gray
Write-Host "   ALTER ROLE [db_owner] ADD MEMBER [IIS APPPOOL\$AppPoolName]" -ForegroundColor Gray
Write-Host "   GO" -ForegroundColor Gray
Write-Host ""
Write-Host "5. Configura DNS (sul Domain Controller):" -ForegroundColor Cyan
Write-Host "   - Apri DNS Manager" -ForegroundColor White
Write-Host "   - Aggiungi record A:" -ForegroundColor White
Write-Host "     Nome: studiocg.web" -ForegroundColor White
Write-Host "     IP: $ServerIP" -ForegroundColor White
Write-Host ""
Write-Host "6. Apri Firewall porta $Port sul server" -ForegroundColor Cyan
Write-Host ""
Write-Host "==============================================================" -ForegroundColor Green
Write-Host "          DEPLOY COMPLETO TERMINATO!                          " -ForegroundColor Green
Write-Host "==============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Dopo la configurazione, accedi a:" -ForegroundColor White
Write-Host "  http://${ServerIP}:${Port}" -ForegroundColor Cyan
Write-Host "  http://studiocg.web" -ForegroundColor Cyan
Write-Host ""

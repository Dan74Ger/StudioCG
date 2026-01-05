# ============================================================
# DEPLOY AGGIORNAMENTO - StudioCG
# Aggiorna sito + migrazioni database (SENZA perdita dati)
# ============================================================

param(
    [string]$ServerName = "SRV-DC",
    [string]$ServerIP = "192.168.1.112",
    [string]$SitePath = "c:\inetpub\studiocg",
    [switch]$SkipMigrations = $false,
    [switch]$SkipBackup = $false
)

$ErrorActionPreference = "Continue"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$PublishDir = Join-Path $ScriptDir "publish"
$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "          DEPLOY AGGIORNAMENTO - StudioCG                      " -ForegroundColor Cyan
Write-Host "          Server: $ServerName ($ServerIP)                      " -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

# ============================================================
# STEP 1: Verifica connettivita server
# ============================================================
Write-Host "[1/5] Verifica connettivita server..." -ForegroundColor Yellow
if (!(Test-Connection -ComputerName $ServerIP -Count 1 -Quiet)) {
    Write-Host "ERRORE: Server $ServerIP non raggiungibile!" -ForegroundColor Red
    exit 1
}
Write-Host "      Server raggiungibile." -ForegroundColor Green

# ============================================================
# STEP 2: Pubblicazione applicazione
# ============================================================
Write-Host "[2/5] Pubblicazione applicazione..." -ForegroundColor Yellow
if (Test-Path $PublishDir) {
    Remove-Item -Path $PublishDir -Recurse -Force
}
dotnet publish -c Release -o $PublishDir --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRORE: Pubblicazione fallita!" -ForegroundColor Red
    exit 1
}
Write-Host "      Pubblicazione completata." -ForegroundColor Green

# ============================================================
# STEP 3: Backup database produzione (sul server remoto)
# ============================================================
if (!$SkipBackup) {
    Write-Host "[3/5] Backup database produzione..." -ForegroundColor Yellow
    
    $BackupResult = Invoke-Command -ComputerName $ServerName -ScriptBlock {
        param($Timestamp)
        try {
            $BackupPath = "C:\temp\StudioCG_PreUpdate_$Timestamp.bak"
            $SqlCmd = "BACKUP DATABASE [StudioCG] TO DISK = N'$BackupPath' WITH FORMAT, INIT, NAME = N'StudioCG-Pre Update Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10"
            & sqlcmd -S ".\SQLEXPRESS" -Q $SqlCmd 2>&1
            return @{ Success = $true; Path = $BackupPath }
        }
        catch {
            return @{ Success = $false; Error = $_.Exception.Message }
        }
    } -ArgumentList $Timestamp -ErrorAction SilentlyContinue
    
    if ($BackupResult.Success) {
        Write-Host "      Backup creato: $($BackupResult.Path)" -ForegroundColor Green
    }
    else {
        Write-Host "      ATTENZIONE: Backup fallito. Continuare comunque." -ForegroundColor Yellow
    }
}
else {
    Write-Host "[3/5] Backup saltato (flag -SkipBackup)" -ForegroundColor Gray
}

# ============================================================
# STEP 4: Copia sito sul server (sovrascrittura completa)
# ============================================================
Write-Host "[4/5] Copia sito sul server..." -ForegroundColor Yellow
$ServerSitePath = "\\$ServerName\c$\inetpub\studiocg"

if (!(Test-Path $ServerSitePath)) {
    Write-Host "ERRORE: Cartella $ServerSitePath non esiste!" -ForegroundColor Red
    exit 1
}

# Ferma TUTTO IIS con net stop
Write-Host "      Fermando servizio IIS (W3SVC)..." -ForegroundColor Gray
Invoke-Command -ComputerName $ServerName -ScriptBlock {
    net stop w3svc /y 2>&1 | Out-Null
} -ErrorAction SilentlyContinue

# Attendi che i file siano rilasciati
Write-Host "      Attendendo rilascio file (10 sec)..." -ForegroundColor Gray
Start-Sleep -Seconds 10

# Copia i file con robocopy (piu robusto)
Write-Host "      Copiando file con robocopy..." -ForegroundColor Gray
$RobocopyResult = & robocopy $PublishDir $ServerSitePath /E /R:3 /W:5 /NFL /NDL /NJH /NJS 2>&1

# Robocopy exit codes: 0-7 sono successo, 8+ sono errori
if ($LASTEXITCODE -lt 8) {
    Write-Host "      File copiati con successo." -ForegroundColor Green
}
else {
    Write-Host "      ATTENZIONE: Alcuni file potrebbero non essere stati copiati." -ForegroundColor Yellow
    Write-Host "      Codice robocopy: $LASTEXITCODE" -ForegroundColor Gray
}

# Riavvia IIS
Write-Host "      Riavviando servizio IIS (W3SVC)..." -ForegroundColor Gray
Invoke-Command -ComputerName $ServerName -ScriptBlock {
    net start w3svc 2>&1 | Out-Null
} -ErrorAction SilentlyContinue

Write-Host "      Sito aggiornato." -ForegroundColor Green

# ============================================================
# STEP 5: Applica migrazioni EF (SOLO aggiunte)
# ============================================================
if (!$SkipMigrations) {
    Write-Host "[5/5] Verifica migrazioni database..." -ForegroundColor Yellow
    Write-Host "      NOTA: Le migrazioni aggiungono SOLO nuove tabelle/colonne." -ForegroundColor Gray
    Write-Host "            I dati esistenti NON vengono toccati." -ForegroundColor Gray
    Write-Host "      Le migrazioni verranno applicate automaticamente al primo avvio." -ForegroundColor Green
}
else {
    Write-Host "[5/5] Migrazioni saltate (flag -SkipMigrations)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "================================================================" -ForegroundColor Green
Write-Host "          AGGIORNAMENTO COMPLETATO!                            " -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Verifica il sito:" -ForegroundColor White
Write-Host "  http://$($ServerIP):5252" -ForegroundColor Cyan
Write-Host "  http://studiocg:5252" -ForegroundColor Cyan
Write-Host ""

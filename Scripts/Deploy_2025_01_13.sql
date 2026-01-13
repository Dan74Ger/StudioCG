-- ============================================================
-- SCRIPT DEPLOY 13/01/2025
-- StudioCG - Aggiornamento Database
-- ============================================================
-- 
-- MODIFICHE:
-- 1. AttivitaPeriodiche: Aggiunta colonna ColonnaPeriodoFissa
-- 2. ConfigurazioniStudio: Aggiunta colonna GiorniScadenzaAntiriciclaggio
--
-- NOTA: Lo script è idempotente (può essere eseguito più volte senza errori)
-- ============================================================

USE StudioCG;
GO

PRINT '============================================================';
PRINT 'INIZIO DEPLOY - ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================================';
GO

-- ------------------------------------------------------------
-- 1. ATTIVITA PERIODICHE - Colonna Periodo Fissa
-- ------------------------------------------------------------
PRINT '';
PRINT '--- 1. AttivitaPeriodiche: ColonnaPeriodoFissa ---';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AttivitaPeriodiche') AND name = 'ColonnaPeriodoFissa')
BEGIN
    ALTER TABLE AttivitaPeriodiche ADD ColonnaPeriodoFissa BIT NOT NULL DEFAULT 0;
    PRINT '    [OK] Colonna ColonnaPeriodoFissa aggiunta';
END
ELSE
BEGIN
    PRINT '    [SKIP] Colonna ColonnaPeriodoFissa già esistente';
END
GO

-- ------------------------------------------------------------
-- 2. CONFIGURAZIONI STUDIO - Giorni Scadenza Antiriciclaggio
-- ------------------------------------------------------------
PRINT '';
PRINT '--- 2. ConfigurazioniStudio: GiorniScadenzaAntiriciclaggio ---';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ConfigurazioniStudio') AND name = 'GiorniScadenzaAntiriciclaggio')
BEGIN
    ALTER TABLE ConfigurazioniStudio ADD GiorniScadenzaAntiriciclaggio INT NOT NULL DEFAULT 1095;
    PRINT '    [OK] Colonna GiorniScadenzaAntiriciclaggio aggiunta (default 1095 gg = 36 mesi)';
END
ELSE
BEGIN
    PRINT '    [SKIP] Colonna GiorniScadenzaAntiriciclaggio già esistente';
END
GO

-- ------------------------------------------------------------
-- VERIFICA FINALE
-- ------------------------------------------------------------
PRINT '';
PRINT '============================================================';
PRINT 'VERIFICA STRUTTURA TABELLE MODIFICATE';
PRINT '============================================================';
PRINT '';

PRINT '--- AttivitaPeriodiche ---';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'AttivitaPeriodiche' AND COLUMN_NAME = 'ColonnaPeriodoFissa';

PRINT '';
PRINT '--- ConfigurazioniStudio ---';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'ConfigurazioniStudio' AND COLUMN_NAME = 'GiorniScadenzaAntiriciclaggio';

PRINT '';
PRINT '============================================================';
PRINT 'DEPLOY COMPLETATO - ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================================';
GO

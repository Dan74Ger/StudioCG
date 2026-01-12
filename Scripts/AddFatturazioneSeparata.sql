-- ============================================================
-- Script per aggiungere il campo FatturazioneSeparata
-- alle 4 tabelle di amministrazione
-- Data: 09/01/2026
-- ============================================================

-- 1. SpesePratiche
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SpesePratiche') AND name = 'FatturazioneSeparata')
BEGIN
    ALTER TABLE SpesePratiche ADD FatturazioneSeparata bit NOT NULL DEFAULT 0;
    PRINT 'Colonna FatturazioneSeparata aggiunta a SpesePratiche';
END
ELSE
    PRINT 'Colonna FatturazioneSeparata già esiste in SpesePratiche';

-- 2. BilanciCEE
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BilanciCEE') AND name = 'FatturazioneSeparata')
BEGIN
    ALTER TABLE BilanciCEE ADD FatturazioneSeparata bit NOT NULL DEFAULT 0;
    PRINT 'Colonna FatturazioneSeparata aggiunta a BilanciCEE';
END
ELSE
    PRINT 'Colonna FatturazioneSeparata già esiste in BilanciCEE';

-- 3. FattureCloud
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('FattureCloud') AND name = 'FatturazioneSeparata')
BEGIN
    ALTER TABLE FattureCloud ADD FatturazioneSeparata bit NOT NULL DEFAULT 0;
    PRINT 'Colonna FatturazioneSeparata aggiunta a FattureCloud';
END
ELSE
    PRINT 'Colonna FatturazioneSeparata già esiste in FattureCloud';

-- 4. AccessiClienti
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AccessiClienti') AND name = 'FatturazioneSeparata')
BEGIN
    ALTER TABLE AccessiClienti ADD FatturazioneSeparata bit NOT NULL DEFAULT 0;
    PRINT 'Colonna FatturazioneSeparata aggiunta a AccessiClienti';
END
ELSE
    PRINT 'Colonna FatturazioneSeparata già esiste in AccessiClienti';

PRINT '';
PRINT '============================================================';
PRINT 'Script completato!';
PRINT 'Tutte le tabelle ora hanno il campo FatturazioneSeparata';
PRINT '============================================================';

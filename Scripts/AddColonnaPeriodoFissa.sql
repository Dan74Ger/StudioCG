-- Script per aggiungere colonna ColonnaPeriodoFissa alle Attività Periodiche
-- Eseguire sul database StudioCG

USE StudioCG;
GO

-- Aggiunge colonna ColonnaPeriodoFissa con default false
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AttivitaPeriodiche') AND name = 'ColonnaPeriodoFissa')
BEGIN
    ALTER TABLE AttivitaPeriodiche ADD ColonnaPeriodoFissa BIT NOT NULL DEFAULT 0;
    PRINT 'Colonna ColonnaPeriodoFissa aggiunta a AttivitaPeriodiche';
END
ELSE
BEGIN
    PRINT 'Colonna ColonnaPeriodoFissa già esistente in AttivitaPeriodiche';
END
GO

-- Script per aggiungere colonna GiorniScadenzaAntiriciclaggio
-- Eseguire sul database StudioCG

USE StudioCG;
GO

-- Aggiunge colonna GiorniScadenzaAntiriciclaggio con default 1095 (36 mesi)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ConfigurazioniStudio') AND name = 'GiorniScadenzaAntiriciclaggio')
BEGIN
    ALTER TABLE ConfigurazioniStudio ADD GiorniScadenzaAntiriciclaggio INT NOT NULL DEFAULT 1095;
    PRINT 'Colonna GiorniScadenzaAntiriciclaggio aggiunta a ConfigurazioniStudio';
END
ELSE
BEGIN
    PRINT 'Colonna GiorniScadenzaAntiriciclaggio già esistente in ConfigurazioniStudio';
END
GO

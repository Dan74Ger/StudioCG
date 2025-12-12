@echo off
chcp 65001 >nul
title Git Commit - StudioCG

echo ╔══════════════════════════════════════════════════════════════╗
echo ║                 GIT COMMIT - STUDIOCG                        ║
echo ╚══════════════════════════════════════════════════════════════╝
echo.

cd /d C:\STUDIOCG\StudioCG

:: Verifica se git è inizializzato
if not exist ".git" (
    echo [INFO] Inizializzazione repository Git...
    git init
    git remote add origin https://github.com/Dan74Ger/StudioCG.git
    git branch -M main
    echo.
)

:: Mostra stato
echo [INFO] Stato attuale dei file:
echo.
git status --short
echo.

:: Chiedi messaggio di commit
set /p MSG="Inserisci messaggio di commit (o premi INVIO per 'Aggiornamento'): "
if "%MSG%"=="" set MSG=Aggiornamento

echo.
echo [1/3] Aggiunta file...
git add .

echo [2/3] Commit in corso...
git commit -m "%MSG%"

echo [3/3] Push su GitHub...
git push -u origin main

echo.
echo ╔══════════════════════════════════════════════════════════════╗
echo ║                    COMMIT COMPLETATO!                        ║
echo ╚══════════════════════════════════════════════════════════════╝
echo.
pause


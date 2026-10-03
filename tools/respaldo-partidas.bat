@echo off
REM ================================================================
REM  AethonMod - RESPALDO DE PARTIDAS (jugadores + mundos)
REM ----------------------------------------------------------------
REM  QUE HACE: copia tus carpetas "Players" y "Worlds" a una carpeta
REM  con fecha y hora, para que NUNCA pierdas progreso aunque un
REM  guardado quede a medias (corte de luz, cuelgue del juego, etc).
REM
REM  COMO USARLO: doble clic ANTES de jugar (o despues de una sesion
REM  larga). Las copias viven a:
REM    Documents\My Games\Terraria\tModLoader\Backups\
REM
REM  CONSEJO: cada tanto borra las carpetas de Backups mas viejas
REM  para no llenar el disco (cada copia pesa lo mismo que tus
REM  partidas). El archivo .bak que crea el juego solo guarda UNA
REM  generacion; estas copias son tu red de seguridad de verdad.
REM ================================================================
setlocal
chcp 65001 >nul

set "RAIZ=%USERPROFILE%\Documents\My Games\Terraria\tModLoader"

REM --- fecha y hora (formato es-ES: dd/MM/yyyy) ---
set "HOY=%date:~6,4%-%date:~3,2%-%date:~0,2%"
set "HORA=%time:~0,8%"
set "HORA=%HORA::=%
set "HORA=%HORA: =0%
set "DEST=%RAIZ%\Backups\%HOY%_%HORA%"

if not exist "%RAIZ%\Players" if not exist "%RAIZ%\Worlds" (
    echo.
    echo  [ERROR] No encontre las carpetas de partidas en:
    echo     %RAIZ%
    echo.
    echo  Revisa que tModLoader haya guardado al menos una partida.
    echo.
    pause
    exit /b 1
)

echo.
echo  Respaldando tus partidas en:
echo     %DEST%
echo  (esto puede tardar unos segundos...)
echo.

if exist "%RAIZ%\Players" (
    robocopy "%RAIZ%\Players" "%DEST%\Players" /E /NFL /NDL /NJH /NJS /NP >nul
    if errorlevel 8 (
        echo  [AVISO] Hubo problemas copiando Players. Revisa el disco.
    ) else (
        echo  [OK] Players copiado.
    )
)

if exist "%RAIZ%\Worlds" (
    robocopy "%RAIZ%\Worlds" "%DEST%\Worlds" /E /NFL /NDL /NJH /NJS /NP >nul
    if errorlevel 8 (
        echo  [AVISO] Hubo problemas copiando Worlds. Revisa el disco.
    ) else (
        echo  [OK] Worlds copiado.
    )
)

echo.
echo  ================================================================
echo   LISTO. Tus partidas estan a salvo en Backups\%HOY%_%HORA%
echo   Recuerda borrar los respaldos mas viejos de vez en cuando.
echo  ================================================================
echo.
pause

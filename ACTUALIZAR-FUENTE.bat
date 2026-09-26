@echo off
setlocal
REM ================================================================
REM  AethonMod v6.50.14 - ACTUALIZADOR DE FUENTES (Windows)
REM
REM  Que hace: git pull del repo + copia espejo de la subcarpeta
REM  AethonMod (EL MOD) hacia ModSources\AethonMod, que es la
REM  carpeta que tModLoader compila (Develop Mods > Build & Reload).
REM
REM  Por que /MIR: la copia es ESPEJO exacto de la carpeta del repo
REM  (borra archivos que ya no existen). No pongas cambios tuyos
REM  dentro de ModSources\AethonMod: ponlos en el repo y ejecuta esto.
REM ================================================================
cd /d "%~dp0"

where git >nul 2>nul
if %errorlevel%==0 (
    echo [git] actualizando el repo...
    git pull --ff-only
) else (
    echo [aviso] git no esta instalado: solo se copiara lo ya descargado.
)

set "DEST=%USERPROFILE%\Documents\My Games\Terraria\tModLoader\ModSources\AethonMod"

echo [copiando] AethonMod -^> "%DEST%"
robocopy "AethonMod" "%DEST%" /MIR /NFL /NDL /NJH /NJS >nul
if %errorlevel% GEQ 8 (
    echo.
    echo [ERROR] robocopy fallo (codigo %errorlevel%).
    echo Si tus Documentos estan redirigidos ^(OneDrive u otra ruta^), edita
    echo este .bat y ajusta la linea "set DEST=..." con tu ruta real de
    echo ModSources ^(el juego te la dice: Mods -^> Develop Mods -^>
    echo Open Mod Sources Folder^).
    pause
    exit /b 1
)

echo.
echo [hecho] Fuente actualizada en ModSources\AethonMod
echo Ahora en el juego:  Mods -^> Develop Mods -^> AethonMod -^> Build + Reload
echo Comprueba que el numero de VERSION del menu Mods sube (6.50.14, ...^).
echo.
pause

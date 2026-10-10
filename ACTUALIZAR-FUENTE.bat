@echo off
setlocal
REM ================================================================
REM  AethonMod v6.50.91 - ACTUALIZADOR DE FUENTES (Windows)
REM
REM  Que hace: git pull del repo + BORRADO de la version anterior +
REM  copia de la subcarpeta AethonMod (EL MOD) hacia
REM  ModSources\AethonMod, que es la carpeta que tModLoader compila
REM  (Develop Mods > Build & Reload).
REM
REM  v6.50.91 - PRIMERO BORRA LA VERSION ANTERIOR (la letra del
REM  usuario: "hacer que el ACTUALIZAR-FUENTE.bat primero borre la
REM  version anterior para copiar la nueva version"): una copia
REM  LIMPIA garantiza que los archivos muertos de versiones viejas
REM  (renombrados, borrados - como El Codice Vivo) no queden
REM  colgando dentro de ModSources confundiendo al compilador.
REM  robocopy /MIR queda como red de seguridad por si el borrado
REM  encuentra algo abierto.
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

echo [borrando] la version anterior de ModSources\AethonMod...
if exist "%DEST%" (
    rmdir /s /q "%DEST%"
)
if exist "%DEST%" (
    echo [aviso] no se pudo borrar del todo ^(algo abierto^?): robocopy /MIR completara la limpieza.
)

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
echo Comprueba que el numero de VERSION del menu Mods sube (6.50.91, ...^).
echo.
pause

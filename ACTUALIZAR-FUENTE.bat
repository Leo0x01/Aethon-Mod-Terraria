@echo off
setlocal
REM ================================================================
REM  AethonMod v6.50.92 - ACTUALIZADOR DE FUENTES (Windows)
REM
REM  Que hace: git pull del repo + BORRADO de la version anterior +
REM  copia de la subcarpeta AethonMod (EL MOD) hacia
REM  ModSources\AethonMod, que es la carpeta que tModLoader compila
REM  (Develop Mods > Build & Reload).
REM
REM  v6.50.92 - ARREGLO DEL "BORRA PERO NO COPIA": la version .91
REM  tenia finales de linea LF (solo \n) y bloques if de varias
REM  lineas - cmd.exe puede descarrilar al parsear bloques
REM  multilinea cuando cruzan sus fronteras internas de lectura de
REM  512 bytes con finales LF (por eso el rmdir SI corria pero el
REM  robocopy no). CURA DEFINITIVA: finales CRLF (\r\n) + CERO
REM  bloques entre parentesis (todo if de una sola linea + etiquetas
REM  goto) + conteo de archivos copiados para verificacion.
REM ================================================================
cd /d "%~dp0"

where git >nul 2>nul
if not %errorlevel%==0 goto nogit
echo [git] actualizando el repo...
git pull --ff-only
goto destino

:nogit
echo [aviso] git no esta instalado: solo se copiara lo ya descargado.

:destino
set "DEST=%USERPROFILE%\Documents\My Games\Terraria\tModLoader\ModSources\AethonMod"

echo [borrando] la version anterior de ModSources\AethonMod...
if not exist "%DEST%" goto copiar
rmdir /s /q "%DEST%"
if not exist "%DEST%" goto copiar
echo [aviso] no se pudo borrar del todo ^(algo abierto^?) - la copia /MIR completara la limpieza.

:copiar
echo [copiando] AethonMod -^> "%DEST%"
robocopy "AethonMod" "%DEST%" /MIR /NFL /NDL /NJH /NJS >nul
if not %errorlevel% LSS 8 goto fallo

set COPIADOS=0
for /f %%C in ('dir /s /b /a-d "%DEST%" 2^>nul ^| find /c /v ""') do set COPIADOS=%%C
echo.
echo [OK] Fuente actualizada: %COPIADOS% archivos copiados en:
echo      %DEST%
echo Ahora en el juego:  Mods -^> Develop Mods -^> AethonMod -^> Build + Reload
echo Comprueba que el numero de VERSION del menu Mods suba (6.50.92, ...).
echo.
pause
exit /b 0

:fallo
echo.
echo [ERROR] robocopy fallo (codigo %errorlevel%).
echo Si tus Documentos estan redirigidos ^(OneDrive u otra ruta^), edita
echo este .bat y ajusta la linea "set DEST=..." con tu ruta real de
echo ModSources ^(el juego te la dice: Mods -^> Develop Mods -^>
echo Open Mod Sources Folder^).
pause
exit /b 1

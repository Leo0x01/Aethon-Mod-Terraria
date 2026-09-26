#!/usr/bin/env bash
# ==================================================================
#  AethonMod v6.50.14 — ACTUALIZADOR DE FUENTES (Linux / macOS)
#
#  Qué hace: git pull del repo + copia espejo de la subcarpeta
#  AethonMod (EL MOD) hacia ModSources/AethonMod, que es la carpeta
#  que tModLoader compila (Develop Mods > Build & Reload).
#  La copia es espejo exacto (--delete): no pongas cambios tuyos en
#  ModSources/AethonMod — ponlos en el repo y ejecuta esto.
# ==================================================================
set -euo pipefail
cd "$(dirname "$0")"

if command -v git >/dev/null 2>&1; then
    echo "[git] actualizando el repo..."
    git pull --ff-only
else
    echo "[aviso] git no está instalado: solo se copiará lo ya descargado."
fi

DEST=""
for BASE in \
    "$HOME/.local/share/Terraria/tModLoader" \
    "$HOME/Library/Application Support/Terraria/tModLoader"; do
    if [ -d "$BASE" ]; then DEST="$BASE/ModSources/AethonMod"; break; fi
done
if [ -z "$DEST" ]; then
    echo "[ERROR] no encontré la carpeta de tModLoader. Ajusta BASE en este script"
    echo "        con tu ruta real (el juego te la dice: Mods > Develop Mods >"
    echo "        Open Mod Sources Folder)."
    exit 1
fi

echo "[copiando] AethonMod -> $DEST"
mkdir -p "$DEST"
if command -v rsync >/dev/null 2>&1; then
    rsync -a --delete AethonMod/ "$DEST/"
else
    rm -rf "$DEST"
    cp -R AethonMod "$DEST"
fi

echo "[hecho] Fuente actualizada en ModSources/AethonMod"
echo "Ahora en el juego:  Mods -> Develop Mods -> AethonMod -> Build + Reload"
echo "Comprueba que el número de VERSIÓN del menú Mods sube (6.50.14, ...)."

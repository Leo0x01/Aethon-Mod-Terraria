#!/bin/bash
# build_v6184.sh v2 — el constructor con paciencia: lanza el -build
# DESPEGADO y vigila SOLO el .tmod (el launcher puede salir mientras
# dotnet sigue compilando en segundo plano).
set -u
MOD=/home/z/my-project/mod/AethonMod
TMOD=/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod
LOG=/tmp/build_raw.log
cd /tmp/tml
ANTES=$(stat -c "%Y %s" "$TMOD" 2>/dev/null || echo "0 0")
setsid nohup bash start-tModLoader.sh -build "$MOD" > "$LOG" 2>&1 < /dev/null &
echo "[build] lanzado despegado · tmod antes: $ANTES"
for i in $(seq 1 70); do   # hasta ~6 min
  sleep 5
  AHORA=$(stat -c "%Y %s" "$TMOD" 2>/dev/null || echo "0 0")
  if [ "$AHORA" != "$ANTES" ]; then
    echo "[build] tmod NUEVO ($AHORA) — estabilizando…"
    sleep 15
    pkill -9 -f "tModLoader.dll -build" 2>/dev/null
    pkill -9 -f "AethonMod.csproj" 2>/dev/null
    sleep 1
    echo "[build] LISTO: $(stat -c '%s B' "$TMOD") · md5 $(md5sum "$TMOD" | cut -d' ' -f1)"
    exit 0
  fi
done
echo "[build] TIMEOUT: el tmod no cambió en ~6 min"; tail -5 "$LOG"; exit 1

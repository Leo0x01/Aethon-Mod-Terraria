#!/usr/bin/env bash
# v6.136 — LA COPIA CON TOKEN del PROMPT-RETOMAR: toma el PAT del
# remote.origin.url (donde vive, regla R17) y lo embebe en la copia de
# download/. El token JAMÁS se imprime ni se commitea al repo.
set -euo pipefail
REPO=/home/z/my-project/mod
OUT=/home/z/my-project/download/PROMPT-RETOMAR.md

URL=$(git -C "$REPO" config --get remote.origin.url)
TOKEN=$(printf '%s' "$URL" | sed -E 's|https://([^@]+)@.*|\1|')

sed "s|«PON-AQUÍ-EL-TOKEN»|$TOKEN|g" "$REPO/PROMPT-RETOMAR.md" > "$OUT"

# verificación sin revelar el token: la copia no debe contener el placeholder
if grep -q '«PON-AQUÍ-EL-TOKEN»' "$OUT"; then
    echo "FALLO: el placeholder sigue en la copia"; exit 1
fi
# y el repo no debe contener el token real
if git -C "$REPO" grep -qF "$TOKEN" -- '*.md' 2>/dev/null; then
    echo "ALERTA: el token apareció en un .md del repo"; exit 1
fi
echo "copia con token: $OUT ($(wc -l < "$OUT") líneas) — repo limpio ✔"

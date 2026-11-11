#!/usr/bin/env bash
# ==================================================================
#  AethonMod v6.126 — EL PUBLICADOR DEL RELEASE (limpio, sobre la
#  estructura del publicar_v6122.sh de la casa):
#    1. Resuelve el PAT (env AETHON_PAT → argumento → el token del remote)
#    2. Verifica el .tmod (md5 + tamaño)
#    3. Push de main + tag v6.126
#    4. Crea el release v6.126 (notas del STABLE-SNAPSHOT.md)
#    5. Sube el AethonMod.tmod como asset
#    6. Verifica el CDN BYTE A BYTE
#    7. Rellena la cabecera del snapshot con el release ID real
#       + commit + push final de docs
# ==================================================================
set -euo pipefail
cd "$(dirname "$0")/.."

REPO="Leo0x01/Aethon-Mod-Terraria"
TAG="v6.126"
MOD_TMOD="$HOME/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
MD5_ESPERADO="c7731425f4a6cca10cf9422191c3b36e"
TAMANO_ESPERADO=19356727
SNAPSHOT="STABLE-SNAPSHOT.md"
API="https://api.github.com"

# ---------- 1 · EL PAT ----------
PAT="${AETHON_PAT:-${1:-}}"
if [ -z "$PAT" ]; then
    PAT="$(git config remote.origin.url | sed -n 's|https://\([^@]*\)@.*|\1|p')"
    echo "[1/7] PAT del remote (${#PAT} chars)"
fi
AUTH=(-H "Authorization: Bearer $PAT" -H "Accept: application/vnd.github+json")
LOGIN=$(curl -s "$API/user" "${AUTH[@]}" | python3 -c "import json,sys; print(json.load(sys.stdin).get('login',''))")
if [ -z "$LOGIN" ]; then echo "[ERROR] el PAT no es válido"; exit 1; fi
echo "[1/7] PAT válido — login: $LOGIN"

# ---------- 2 · LA INTEGRIDAD DEL .TMOD ----------
MD5_REAL=$(md5sum "$MOD_TMOD" | cut -d' ' -f1)
TAM_REAL=$(stat -c%s "$MOD_TMOD")
if [ "$MD5_REAL" != "$MD5_ESPERADO" ] || [ "$TAM_REAL" != "$TAMANO_ESPERADO" ]; then
    echo "[ERROR] el .tmod no coincide: md5=$MD5_REAL ($TAM_REAL B)"
    exit 1
fi
echo "[2/7] .tmod íntegro: $TAM_REAL B · md5 $MD5_REAL"

# ---------- 3 · PUSH DE MAIN + TAG ----------
echo "[3/7] push de main + $TAG…"
git push origin main
git tag -f "$TAG" >/dev/null 2>&1 || true
git push -f origin "refs/tags/$TAG:refs/tags/$TAG"

# ---------- 4 · EL RELEASE ----------
echo "[4/7] creando el release $TAG…"
NOTAS=$(python3 - << PYEOF
import re
t = open("$SNAPSHOT", encoding="utf-8").read()
m = re.search(r"^# SNAPSHOT ESTABLE — v6\.126.*?(?=\n---\n)", t, re.S | re.M)
cuerpo = m.group(0) if m else t[:4000]
cuerpo = cuerpo.replace("> ⏳ **PENDIENTE DE PUBLICAR** — esta cabecera se rellena con el release ID real al publicar\n", "")
print(cuerpo.strip())
PYEOF
)
RELEASE_ID=$(python3 -c "
import json
notas = json.load(open('/tmp/notas_v6126.json'))
print(json.dumps({'tag_name': '$TAG', 'name': 'AethonMod $TAG — La Reconciliación + Las Cinco Dagas del Eterno', 'body': notas}))
" 2>/dev/null; echo "") || true
# (las notas por archivo para no pelear con el shell)
python3 -c "
import json
print(json.dumps({'tag_name': '$TAG', 'name': 'AethonMod $TAG — La Reconciliación + Las Cinco Dagas del Eterno', 'body': open('/tmp/notas_v6126.json').read()}))
" <<< "" >/dev/null 2>&1 || true
echo "$NOTAS" > /tmp/notas_v6126.txt
python3 -c "
import json
notas = open('/tmp/notas_v6126.txt', encoding='utf-8').read()
print(json.dumps({'tag_name': '$TAG', 'name': 'AethonMod $TAG — La Reconciliación + Las Cinco Dagas del Eterno', 'body': notas}))
" > /tmp/payload_v6126.json
RELEASE_ID=$(curl -s -X POST "$API/repos/$REPO/releases" "${AUTH[@]}" \
    -d @/tmp/payload_v6126.json | python3 -c "import json,sys; d=json.load(sys.stdin); print(d.get('id','') or ('ERR:'+str(d.get('message'))))")
if [ -z "$RELEASE_ID" ] || [ "${RELEASE_ID#ERR:}" != "$RELEASE_ID" ]; then
    RELEASE_ID=$(curl -s "$API/repos/$REPO/releases/tags/$TAG" "${AUTH[@]}" | python3 -c "import json,sys; print(json.load(sys.stdin).get('id',''))")
    if [ -z "$RELEASE_ID" ]; then echo "[ERROR] no se pudo crear el release: $RELEASE_ID"; exit 1; fi
    echo "      el release ya vivía — ID $RELEASE_ID (se reutiliza)"
else
    echo "      release creado — ID $RELEASE_ID"
fi

# ---------- 5 · EL ASSET ----------
echo "[5/7] subiendo el .tmod…"
ASSET_URL=$(curl -s "$API/repos/$REPO/releases/$RELEASE_ID/assets" "${AUTH[@]}" | python3 -c "
import json, sys
for a in json.load(sys.stdin):
    if a['name'] == 'AethonMod.tmod':
        print(a['url']); break")
if [ -n "$ASSET_URL" ]; then
    curl -s -X DELETE "$ASSET_URL" "${AUTH[@]}" >/dev/null
    echo "      el asset viejo murió"
fi
UP=$(curl -s -X POST "https://uploads.github.com/repos/$REPO/releases/$RELEASE_ID/assets?name=AethonMod.tmod" \
    "${AUTH[@]}" -H "Content-Type: application/octet-stream" \
    --data-binary "@$MOD_TMOD")
ASSET_DL=$(echo "$UP" | python3 -c "import json,sys; print(json.load(sys.stdin).get('browser_download_url',''))")
if [ -z "$ASSET_DL" ]; then echo "[ERROR] subiendo el asset: $UP" | head -c 400; exit 1; fi
echo "      asset vivo: $ASSET_DL"

# ---------- 6 · EL CDN BYTE A BYTE ----------
echo "[6/7] verificando el CDN…"
sleep 6
curl -sL "$ASSET_DL" -o /tmp/asset_v6126.tmod
MD5_CDN=$(md5sum /tmp/asset_v6126.tmod | cut -d' ' -f1)
if [ "$MD5_CDN" != "$MD5_ESPERADO" ]; then
    echo "      reintento en 10 s…"; sleep 10
    curl -sL "$ASSET_DL" -o /tmp/asset_v6126.tmod
    MD5_CDN=$(md5sum /tmp/asset_v6126.tmod | cut -d' ' -f1)
    [ "$MD5_CDN" = "$MD5_ESPERADO" ] || { echo "[ERROR] el CDN no coincide: $MD5_CDN"; exit 1; }
fi
echo "      CDN verificado BYTE A BYTE ✔ (md5 $MD5_CDN)"

# ---------- 7 · LA CABECERA DEL SNAPSHOT ----------
echo "[7/7] la cabecera con el release ID real…"
python3 - << PYEOF
t = open("$SNAPSHOT", encoding="utf-8").read()
vieja = "> ⏳ **PENDIENTE DE PUBLICAR** — esta cabecera se rellena con el release ID real al publicar"
nueva = "> **PUBLICADA — release $RELEASE_ID + CDN verificado byte a byte (md5 $MD5_ESPERADO)**"
if vieja in t:
    t = t.replace(vieja, nueva, 1)
    open("$SNAPSHOT", "w", encoding="utf-8").write(t)
    print("      la cabecera vive con el release ID $RELEASE_ID")
else:
    print("      la cabecera ya estaba rellena")
PYEOF
git add "$SNAPSHOT"
git commit -q -m "docs: v6.126 PUBLICADA — release $RELEASE_ID + CDN verificado byte a byte (md5 $MD5_ESPERADO); la cabecera del snapshot con el release ID real" || true
git push origin main
echo "OK PUBLICADA v6.126 — release $RELEASE_ID · $ASSET_DL"

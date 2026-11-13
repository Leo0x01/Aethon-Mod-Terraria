#!/usr/bin/env bash
# ==================================================================
#  AethonMod v6.124 — EL PUBLICADOR DEL RELEASE (el paso 5 de
#  EL-PIPELINE-DE-CREACION.md) — heredado del publicador v6.122.
#
#  Qué hace (6 pasos, aborta al primer fallo):
#    1. Resuelve el PAT del BAÚL (cifrado) y lo valida
#    2. Empuja el tag v6.124
#    3. Crea el release v6.124 (notas del STABLE-SNAPSHOT.md)
#    4. Sube el AethonMod.tmod como asset
#    5. Verifica el CDN BYTE A BYTE (md5 del asset re-descargado)
#    6. Rellena la cabecera del snapshot con el release ID real
#       + commit + push final de docs
# ==================================================================
set -euo pipefail
cd "$(dirname "$0")/.."

REPO="Leo0x01/Aethon-Mod-Terraria"
TAG="v6.127"
MOD_TMOD="$HOME/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
MD5_ESPERADO="9da1d7ad0f3d37bd5fe4dcb21a6920c7"
TAMANO_ESPERADO=19354749
SNAPSHOT="STABLE-SNAPSHOT.md"
API="https://api.github.com"

# ---------- 1 · EL PAT (EL BAÚL) ----------
PAT="$(python3 tools/pat_baul.py leer)"
AUTH=(-H "Authorization: Bearer $PAT" -H "Accept: application/vnd.github+json")
LOGIN=$(curl -s "$API/user" "${AUTH[@]}" | jq -r '.login // empty')
if [ -z "$LOGIN" ]; then
    echo "[ERROR] el PAT no es válido"; exit 1
fi
echo "[1/6] PAT válido — login: $LOGIN"

# ---------- 2 · LA INTEGRIDAD DEL .tmod ----------
MD5_REAL=$(md5sum "$MOD_TMOD" | cut -d' ' -f1)
TAMANO_REAL=$(stat -c%s "$MOD_TMOD")
if [ "$MD5_REAL" != "$MD5_ESPERADO" ] || [ "$TAMANO_REAL" != "$TAMANO_ESPERADO" ]; then
    echo "[ERROR] el .tmod no coincide (md5 $MD5_REAL / $TAMANO_REAL B)"
    exit 1
fi
echo "[2/6] .tmod íntegro: md5 $MD5_REAL · $TAMANO_REAL B"

# ---------- 3 · EL TAG ----------
if git rev-parse -q --verify "refs/tags/$TAG" >/dev/null; then
    echo "[3/6] el tag $TAG ya existe — reutilizado"
else
    git tag -a "$TAG" -m "v6.127 — LA CAUSA RAÍZ DE LA SIERRA + EL CETRO DE LAS DAGAS + LA LEY DE LOS ANILLOS + LOS COSMÉTICOS PEGADOS + LOS DIÁLOGOS EN SU SITIO"
    git push origin "$TAG"
    echo "[3/6] tag $TAG empujado"
fi

# ---------- 4 · EL RELEASE (reutiliza si ya existe) ----------
RELEASE_ID=$(curl -s "${API}/repos/$REPO/releases/tags/$TAG" "${AUTH[@]}" | jq -r '.id // empty')
if [ -z "$RELEASE_ID" ]; then
    CUERPO=$(jq -Rs . < "$SNAPSHOT" | jq -r '.[0:60000]')
    DATA=$(jq -n --arg t "$TAG" --arg b "$CUERPO" \
        '{tag_name: $t, name: ("Aethon, the Eternal Grimoire " + $t), body: $b, draft: false, prerelease: false}')
    RELEASE_ID=$(curl -s -X POST "${API}/repos/$REPO/releases" "${AUTH[@]}" \
        -H "Content-Type: application/json" -d "$DATA" | jq -r '.id // empty')
    if [ -z "$RELEASE_ID" ]; then
        echo "[ERROR] no se pudo crear el release"; exit 1
    fi
    echo "[4/6] release $TAG creado: id $RELEASE_ID"
else
    echo "[4/6] release $TAG ya existe: id $RELEASE_ID (reutilizado)"
fi

# ---------- 5 · EL ASSET ----------
ASSET_URL=$(curl -s "${API}/repos/$REPO/releases/$RELEASE_ID/assets" "${AUTH[@]}" \
    | jq -r '.[] | select(.name == "AethonMod.tmod") | .url // empty')
if [ -n "$ASSET_URL" ]; then
    echo "      el asset viejo existe — se borra y re-sube"
    curl -s -X DELETE "$ASSET_URL" "${AUTH[@]}" || true
fi
UP=$(curl -s -X POST "${API}/repos/$REPO/releases/$RELEASE_ID/assets?name=AethonMod.tmod" \
    -H "Authorization: Bearer $PAT" -H "Content-Type: application/octet-stream" \
    --data-binary "@$MOD_TMOD")
BROWSER=$(echo "$UP" | jq -r '.browser_download_url // empty')
if [ -z "$BROWSER" ]; then
    echo "[ERROR] la subida del asset falló: $UP"; exit 1
fi
echo "[5/6] asset subido: $BROWSER"

# ---------- 6 · EL CDN VERIFICADO BYTE A BYTE ----------
sleep 4
curl -sL "$BROWSER" -o /tmp/asset_v6127.tmod
MD5_CDN=$(md5sum /tmp/asset_v6127.tmod | cut -d' ' -f1)
if [ "$MD5_CDN" != "$MD5_ESPERADO" ]; then
    echo "[ERROR] el CDN no coincide (md5 $MD5_CDN)"; exit 1
fi
echo "[6/6] CDN verificado BYTE A BYTE: md5 $MD5_CDN == $MD5_ESPERADO"

# ---------- LA CABECERA DEL SNAPSHOT ----------
python3 - "$RELEASE_ID" "$MD5_ESPERADO" <<'PYEOF'
import io, sys
release, md5 = sys.argv[1], sys.argv[2]
p = 'STABLE-SNAPSHOT.md'
s = io.open(p, encoding='utf-8').read()
viejo = "> ⏳ **PENDIENTE DE PUBLICAR — el release ID se rellena tras el empuje (la marca ⏳ muere cuando el CDN queda verificado)**"
nuevo = f"> **PUBLICADA — release {release} + CDN verificado byte a byte (md5 {md5})**"
assert viejo in s, "la marca ⏳ no está"
s = s.replace(viejo, nuevo, 1)
io.open(p, 'w', encoding='utf-8').write(s)
print(f"      snapshot: PUBLICADA — release {release}")
PYEOF
git add "$SNAPSHOT"
git commit -q -m "docs: v6.127 PUBLICADA — release $RELEASE_ID + CDN verificado byte a byte (md5 $MD5_ESPERADO); la cabecera del snapshot con el release ID real" || true
git push origin main
echo "PUBLICADO: release $RELEASE_ID · v6.127 en el CDN"

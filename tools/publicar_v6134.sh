#!/usr/bin/env bash
# ==================================================================
#  publicar_v6131.sh — EL PUBLICADOR del release v6.134 (adaptado del
#  publicar_v6118.sh de la casa): PAT desde el remote de git, release
#  con notas, asset .tmod, verificación CDN byte a byte.
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

REPO="Leo0x01/Aethon-Mod-Terraria"
TAG="v6.134"
MOD_TMOD="$HOME/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
MD5_ESPERADO="e43d4ca212493c81557bf5139c0d1d95"
API="https://api.github.com"

# ---------- 1 · EL PAT (desde el remote de git) ----------
PAT="$(git config --get remote.origin.url | sed -E 's|https://([^@]+)@.*|\1|')"
AUTH=(-H "Authorization: Bearer $PAT" -H "Accept: application/vnd.github+json")
LOGIN=$(curl -s "$API/user" "${AUTH[@]}" | python3 -c "import json,sys; print(json.load(sys.stdin).get('login',''))")
[ -n "$LOGIN" ] || { echo "[ERROR] PAT inválido"; exit 1; }
echo "[1/5] PAT válido — login: $LOGIN"

# ---------- 2 · EL .TMOD ----------
echo "[2/5] integridad del .tmod…"
MD5_REAL=$(md5sum "$MOD_TMOD" | cut -d' ' -f1)
[ "$MD5_REAL" = "$MD5_ESPERADO" ] || { echo "[ERROR] md5 $MD5_REAL ≠ $MD5_ESPERADO"; exit 1; }
echo "      md5 OK: $MD5_REAL ($(stat -c %s "$MOD_TMOD") B)"

# ---------- 3 · EL RELEASE ----------
echo "[3/5] creando el release $TAG…"
NOTAS=$(cat <<'EOF'
# v6.134 — LAS PLUMAS MUEREN

## 1 · Las alas del vacío — sin plumas

La copia exacta del Eclipse (v6.133) trajo de vuelta las **cinco tiras radiales** — las «plumas» a la vista — y dejó las alitas mudas (la membrana cayó a 0). La cura es la receta probada del ala lisa de la casa:

- **Las tiras mueren**: las capas sólidas (núcleo/cuerpo/halo) y su niebla no se emiten.
- **Las cinco plumas quedan como la armadura invisible** de UNA hoja continua por lado — la RAMPA RADIAL: el violeta vivo en la raíz → el hondo puro → el bordillo blanco de la curva.
- **Las alitas reviven** (la vela del par inferior vive tras la membrana).
- **La geometría de la copia exacta NO se toca**: los 31 parámetros del Eclipse siguen idénticos; solo cambiaron los tres flags. El Eclipse conserva sus plumas intactas.

**El sim 1:1** (parámetros leídos del .cs real): hoja continua sin tiras (0 huecos) · la silueta cubre las 5 puntas (el ala no se encoge) · las alitas emiten. QA visual: «UNA hoja continua y lisa por lado, sin tiras radiales separadas» — el zoom confirmó 5/5 puntas tocando el borde.

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo verify.csproj (454 .cs) | 0 errores / 0 warnings |
| -build REAL | 0/0 — 520 entradas |
| GUARDIÁN de texturas | TODO VIVE ✔ |
| AUDIT v6.134 (41 checks) | TODO VERIFICADO ✔ |
| Servidor headless | 0 excepciones |

**Instalación**: copia `AethonMod.tmod` a la carpeta `Mods` del tModLoader y actívalo.

EOF
)
BODY=$(python3 -c "
import json, sys
notas = sys.stdin.read()
print(json.dumps({'tag_name': '$TAG', 'name': 'v6.134 — LAS PLUMAS MUEREN', 'body': notas, 'draft': False, 'prerelease': False}))
" <<< "$NOTAS")

REL_ID=$(curl -s "$API/repos/$REPO/releases/tags/$TAG" "${AUTH[@]}" | python3 -c "import json,sys; print(json.load(sys.stdin).get('id',''))")
if [ -z "$REL_ID" ]; then
    REL_ID=$(curl -s -X POST "$API/repos/$REPO/releases" "${AUTH[@]}" -H "Content-Type: application/json" -d "$BODY" | python3 -c "import json,sys; print(json.load(sys.stdin).get('id',''))")
    echo "      release creado: $REL_ID"
else
    echo "      release ya existía: $REL_ID (se reutiliza)"
fi
[ -n "$REL_ID" ] || { echo "[ERROR] sin release ID"; exit 1; }

# ---------- 4 · EL ASSET ----------
echo "[4/5] subiendo el asset…"
# borrar el asset si ya existe
ASSET_URL=$(curl -s "$API/repos/$REPO/releases/$REL_ID/assets" "${AUTH[@]}" | python3 -c "
import json,sys
for a in json.load(sys.stdin):
    if a['name'] == 'AethonMod.tmod':
        print(a['url']); break
")
[ -n "$ASSET_URL" ] && curl -s -X DELETE "$ASSET_URL" "${AUTH[@]}" && echo "      asset viejo borrado"

UP=$(curl -s -X POST "https://uploads.github.com/repos/$REPO/releases/$REL_ID/assets?name=AethonMod.tmod" \
    "${AUTH[@]}" -H "Content-Type: application/octet-stream" \
    --data-binary "@$MOD_TMOD" | python3 -c "import json,sys; d=json.load(sys.stdin); print(d.get('browser_download_url',''))")
echo "      asset: $UP"
[ -n "$UP" ] || { echo "[ERROR] asset no subió"; exit 1; }

# ---------- 5 · EL CDN BYTE A BYTE ----------
echo "[5/5] verificando el CDN byte a byte…"
sleep 4
TMP=$(mktemp)
curl -sL "$UP" -o "$TMP"
MD5_CDN=$(md5sum "$TMP" | cut -d' ' -f1)
rm -f "$TMP"
[ "$MD5_CDN" = "$MD5_ESPERADO" ] && echo "      CDN verificado: md5 idéntico ($MD5_CDN)" || { echo "[ERROR] CDN md5 $MD5_CDN ≠ $MD5_ESPERADO"; exit 1; }

echo
echo "PUBLICADO ✔ https://github.com/$REPO/releases/tag/$TAG (release $REL_ID)"

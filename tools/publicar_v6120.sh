#!/usr/bin/env bash
# ==================================================================
#  AethonMod v6.120 — EL PUBLICADOR DEL RELEASE (el paso 5 de
#  EL-PIPELINE-DE-CREACION.md)
#
#  NOVEDAD (heredada de v6.120) — EL BAÚL: si no viene PAT por env/argumento, se
#  lee CIFRADO del baúl de la casa (tools/pat_baul.py leer — la
#  frase vive en ~/.config/aethon-baul/frase o en $AETHON_FRASE).
#  El PAT en texto plano ya no necesita viajar por el chat.
#
#  Qué hace (7 pasos, aborta al primer fallo):
#    1. Resuelve el PAT (env → argumento → EL BAÚL) y lo valida
#    2. Verifica la integridad del .tmod (md5 + tamaño contra la AUDIT)
#    3. Push de main + del tag v6.120
#    4. Crea el release v6.120 (notas del STABLE-SNAPSHOT.md)
#    5. Sube el AethonMod.tmod como asset
#    6. Verifica el CDN BYTE A BYTE (md5 del asset re-descargado)
#    7. Rellena la cabecera del snapshot con el release ID real
#       + commit + push final de docs
#
#  Idempotencia: si el release ya existe, reutiliza su ID; si el asset
#  ya existe, lo borra y lo re-sube. Si ya está publicada (sin la marca
#  ⏳ en el snapshot), el paso 7 se salta.
# ==================================================================
set -euo pipefail
cd "$(dirname "$0")/.."

REPO="Leo0x01/Aethon-Mod-Terraria"
TAG="v6.120"
MOD_TMOD="$HOME/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
MD5_ESPERADO="5bb475d8279bd092e6b3f18deec03514"
TAMANO_ESPERADO=15943130
SNAPSHOT="STABLE-SNAPSHOT.md"
API="https://api.github.com"

# ---------- 1 · EL PAT (env → argumento → EL BAÚL) ----------
PAT="${AETHON_PAT:-${1:-}}"
if [ -z "$PAT" ]; then
    echo "[1/7] buscando el PAT en EL BAÚL de la casa…"
    if PAT="$(python3 tools/pat_baul.py leer 2>/dev/null)"; then
        echo "      el baúl se abrió (la frase vive en esta máquina)"
    else
        echo "[ERROR] falta el PAT (ni env, ni argumento, ni baúl). Uso:"
        echo "        AETHON_PAT=github_pat_... bash tools/publicar_v6117.sh"
        echo "        bash tools/publicar_v6117.sh github_pat_..."
        echo "        (o guarda el PAT en el baúl: python3 tools/pat_baul.py guardar)"
        exit 1
    fi
else
    echo "[1/7] PAT recibido por ${AETHON_PAT:+env}${AETHON_PAT:-argumento}"
fi
AUTH=(-H "Authorization: Bearer $PAT" -H "Accept: application/vnd.github+json")

echo "      validando el PAT…"
LOGIN=$(curl -s "$API/user" "${AUTH[@]}" | jq -r '.login // empty')
if [ -z "$LOGIN" ]; then
    echo "[ERROR] el PAT no es válido (la API no lo acepta)"
    exit 1
fi
echo "      PAT válido — login: $LOGIN"

# ---------- 2 · EL .TMOD ----------
echo "[2/7] integridad del .tmod…"
if [ ! -f "$MOD_TMOD" ]; then
    echo "[ERROR] no existe $MOD_TMOD — corre antes el -build REAL"
    exit 1
fi
MD5_REAL=$(md5sum "$MOD_TMOD" | cut -d' ' -f1)
TAMANO_REAL=$(stat -c %s "$MOD_TMOD")
if [ "$MD5_REAL" != "$MD5_ESPERADO" ] || [ "$TAMANO_REAL" != "$TAMANO_ESPERADO" ]; then
    echo "[ERROR] el .tmod NO es el de la AUDIT v6.120:"
    echo "        md5    $MD5_REAL  (esperado $MD5_ESPERADO)"
    echo "        tamaño $TAMANO_REAL  (esperado $TAMANO_ESPERADO)"
    echo "        Re-audita antes de publicar (la lección v6.90/v6.97)"
    exit 1
fi
echo "      md5 OK ($MD5_REAL · $TAMANO_REAL B)"

# ---------- 3 · EL PUSH ----------
echo "[3/7] push de main + $TAG…"
# El PAT va INCORPORADO a la URL del push (nunca a disco: sin git config,
# sin credential helper — la URL vive solo el tiempo del comando).
PUSH_URL="https://x-access-token:${PAT}@github.com/${REPO}.git"
if ! git push "$PUSH_URL" main; then
    echo "[ERROR] el push de main falló (¿token sin permiso de contents:write?)"
    exit 1
fi
if ! git push "$PUSH_URL" "$TAG"; then
    echo "[ERROR] el push del tag $TAG falló"
    exit 1
fi

# ---------- 4 · EL RELEASE ----------
echo "[4/7] creando el release $TAG…"
# El cuerpo nace del STABLE-SNAPSHOT.md: el resumen (línea «> **», con
# sus « · » partidos en viñetas) + LA LETRA + LA AUTOPSIA + LA CHECKLIST.
BODY=$(python3 - "$SNAPSHOT" <<'PYEOF'
import json, re, sys

lines = open(sys.argv[1], encoding='utf-8').read().splitlines()

# el resumen: la línea que empieza con «> **» — sin la marca de estado
resumen = next(l for l in lines if l.startswith('> **') or l.startswith('> ⏳ **'))
resumen = re.sub(r'^> [^\u2014]*\u2014 ', '', resumen, count=1)  # TODO el bloque de estado hasta el primer guion largo
# « · » separa los bloques del resumen → una viñeta por bloque
vinyetas = '\n'.join('- ' + p.strip() for p in resumen.split(' · ') if p.strip())

# la letra del usuario: la línea que abre con «
letra = next((l for l in lines if l.startswith('«')), '')

# la autopsia: desde «## LA AUTOPSIA» hasta «## PARA VERIFICAR»
i_auto = next((k for k, l in enumerate(lines) if l.startswith('## LA AUTOPSIA')), None)
i_check = next((k for k, l in enumerate(lines) if l.startswith('## PARA VERIFICAR')), None)
autopsia = ''
if i_auto is not None and i_check is not None:
    autopsia = '\n'.join(l for l in lines[i_auto:i_check] if l.strip())

# la checklist: desde «## PARA VERIFICAR» hasta el final del archivo
checklist = '\n'.join(l for l in lines[i_check:] if l.strip()) if i_check is not None else ''

partes = [
    'v6.120 — EL LLANO DE OBSIDIANA + EL PILAR DE LA SALIDA + LAS TRES BANDAS',
    '',
    '## El resumen',
    vinyetas,
    '',
    '## LA LETRA (el usuario)',
    '',
    letra,
    '',
]
if autopsia:
    partes += [autopsia, '']
partes += [
    checklist,
    '',
    '——',
    'Verificación: oráculo 0/0 (445 .cs contra tML 2026.09.3.1) · '
    '-build REAL 0/0 · GUARDIÁN TODO VIVE (con la pasada (4) NUEVA de convenciones + prueba negativa) · '
    'AUDIT v6.120 TODO VERIFICADO ✔ (100 checks: 499 entradas, EOF exacto, EMPAQUETADO==FUENTE ×3, las firmas de la obra VIVAS en la DLL) · '
    'servidor headless 0 excepciones (el mod cargando de verdad: «Sandboxing: Aethon v6.120»).',
    '.tmod: 15.943.130 B · 499 entradas · md5 5bb475d8279bd092e6b3f18deec03514',
    'Instalación: descarga `AethonMod.tmod` → carpeta `Mods` del tModLoader → actívalo.',
]
body = '\n'.join(partes)

print(json.dumps({
    'tag_name': 'v6.120',
    'name': 'v6.120 — EL LLANO DE OBSIDIANA + EL PILAR DE LA SALIDA + LAS TRES BANDAS',
    'body': body,
    'draft': False,
    'prerelease': False,
}, ensure_ascii=False))
PYEOF
)
# ¿existe ya? (idempotencia)
EXISTE=$(curl -s "$API/repos/$REPO/releases/tags/$TAG" "${AUTH[@]}" | jq -r '.id // empty')
if [ -n "$EXISTE" ]; then
    RELEASE_ID="$EXISTE"
    echo "      ya existía (id $RELEASE_ID) — se reutiliza"
else
    RELEASE_ID=$(curl -s -X POST "$API/repos/$REPO/releases" \
        "${AUTH[@]}" -H "Content-Type: application/json" -d "$BODY" | jq -r '.id // empty')
    if [ -z "$RELEASE_ID" ]; then
        echo "[ERROR] la creación del release falló (¿permisos?)"
        exit 1
    fi
    echo "      release creado: id $RELEASE_ID"
fi

# ---------- 5 · EL ASSET ----------
echo "[5/7] subiendo AethonMod.tmod…"
# si ya hay un asset con ese nombre, se borra y se re-sube (idempotencia)
ASSET_VIEJO=$(curl -s "$API/repos/$REPO/releases/$RELEASE_ID/assets" "${AUTH[@]}" \
    | jq -r '.[] | select(.name=="AethonMod.tmod") | .id' | head -1)
if [ -n "$ASSET_VIEJO" ]; then
    curl -s -X DELETE "$API/repos/$REPO/releases/assets/$ASSET_VIEJO" "${AUTH[@]}" > /dev/null
fi
ASSET_URL=$(curl -s -X POST \
    "https://uploads.github.com/repos/$REPO/releases/$RELEASE_ID/assets?name=AethonMod.tmod" \
    "${AUTH[@]}" -H "Content-Type: application/octet-stream" \
    --data-binary "@$MOD_TMOD" | jq -r '.browser_download_url // empty')
if [ -z "$ASSET_URL" ]; then
    echo "[ERROR] la subida del asset falló"
    exit 1
fi
echo "      asset: $ASSET_URL"

# ---------- 6 · EL CDN BYTE A BYTE ----------
echo "[6/7] CDN verificado byte a byte…"
curl -sL "$ASSET_URL" -o /tmp/cdn_check_v6120.tmod
MD5_CDN=$(md5sum /tmp/cdn_check_v6120.tmod | cut -d' ' -f1)
if [ "$MD5_CDN" != "$MD5_ESPERADO" ]; then
    echo "[ERROR] el CDN sirve otro binario (md5 $MD5_CDN ≠ $MD5_ESPERADO)"
    exit 1
fi
rm -f /tmp/cdn_check_v6120.tmod
echo "      md5 del CDN == md5 local ($MD5_CDN)"

# ---------- 7 · LAS CABECERAS + EL COMMIT FINAL ----------
echo "[7/7] cabecera del snapshot con el release ID real…"
if python3 - "$SNAPSHOT" "$RELEASE_ID" "$MD5_ESPERADO" <<'PYEOF'
import sys
snap, rid, md5 = sys.argv[1], sys.argv[2], sys.argv[3]
txt = open(snap, encoding='utf-8').read()
viejo = '> ⏳ **PENDIENTE DE PUBLICACIÓN**'
nuevo = f'> **PUBLICADA — release {rid} + CDN verificado byte a byte (md5 {md5})**'
if viejo not in txt:
    sys.exit(1)  # ya estaba publicada: se salta el paso 7
open(snap, 'w', encoding='utf-8').write(txt.replace(viejo, nuevo, 1))
sys.exit(0)
PYEOF
then
    git add "$SNAPSHOT"
    git commit -q -m "docs: v6.120 PUBLICADA — release $RELEASE_ID + CDN verificado byte a byte (md5 $MD5_ESPERADO); la cabecera del snapshot con el release ID real"
    git push "$PUSH_URL" main
else
    echo "      [AVISO] el snapshot ya no tiene la marca ⏳ — nada que rellenar"
fi

echo
echo "══════════════════════════════════════════════════════════"
echo " v6.120 PUBLICADA — release $RELEASE_ID"
echo " $ASSET_URL"
echo "══════════════════════════════════════════════════════════"

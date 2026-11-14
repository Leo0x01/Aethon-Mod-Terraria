#!/usr/bin/env bash
# ==================================================================
#  publicar_v6131.sh — EL PUBLICADOR del release v6.132 (adaptado del
#  publicar_v6118.sh de la casa): PAT desde el remote de git, release
#  con notas, asset .tmod, verificación CDN byte a byte.
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

REPO="Leo0x01/Aethon-Mod-Terraria"
TAG="v6.132"
MOD_TMOD="$HOME/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
MD5_ESPERADO="12a52861a3c773c4ff785dd3b49fac89"
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
# v6.132 — LAS ALAS REPARADAS · LA CORONA DE CUCHILLAS · EL JUGADOR COMPLETO DEVORADO

## 1 · LAS ALAS REPARADAS (la captura del usuario: un ala sola + un bloque rectangular)

**La autopsia del ala solitaria**: el ala lisa de la v6.129 escondía TRES bugs de aliasing en `EstirarVelaLisa` — los pozos de plumas son COMPARTIDOS y la función los pisa por lado:

- **El lado izquierdo pisaba los pozos 0–6 ANTES de que el derecho leyera sus plumas viejas 5–9**: las dos primeras plumas del ala derecha nacían de los NACIMIENTOS izquierdos ya escritos (stubs de 16 px casi sin masa) — y con la punta de referencia pisoteada, el camino del borde derecho se armaba con el ángulo de la punta IZQUIERDA: la vela cruzaba **LA ESPALDA = el bloque rectangular de la captura** y el ala derecha moría = **«solo se ven las alas de la izquierda»**.
- **Los emisores con el N viejo**: recibían `n=10` (el abanico) cuando la geometría lisa vive en `_geoN=14` — el "lado 1" emparejaba ejes izquierdos con stubs derechos a través de la espalda (el segundo constructor del rectángulo).
- **El espejo incompleto**: mirando a la izquierda solo volteaba 10 de los 14 ejes.

**La cura triple**: EL SNAPSHOT (`ejesViejos` — todo se calcula sobre una copia de la armadura viva y los pozos se pisan al final) + `nEmisores` (los emisores leen los 7 ejes por lado reales) + `nEspejo` (el espejo recorre `_geoN` completo).

**El sim 1:1 lo cazó y lo curó** (`scripts/sim_alas_v6131.py`, los pozos mutándose en el sitio): la vieja Δ de simetría **171,94 px** + espalda cruzada **69,04 px** (la captura reproducida) → la cura **0,0000 px** en ambos, también mirando a la izquierda. QA VLM 9/10: «DOS alas simétricas limpias en forma de mariposa».

## 2 · LA CORONA DE CUCHILLAS — LA TIARA DEL ENJAMBRE

Las cuchillas dejan de ser el anillo íntimo del cuerpo (radio 32 girando: pequeñas y ABAJO) y nacen **LA TIARA**:

- **Un arco realzado y QUIETO** por encima de la corona de ojos (−59° a +59°: las de los extremos caen a los lados de la cabeza, la del centro la corona plena — una corona se lleva puesta, no gira).
- **El tamaño ×1,4** (`Projectile.scale` 1,40 + hitbox 26×26: la hoja se lee 47×47 en pantalla — el ojo del taladro, el iris y el trono crecen con la hoja).
- **Los filos hacia AFUERA** (la corona de puntas).
- **LA LEY DEL DESPEJE** (`RadioTiara = max(40, 6,2·nOjos) + 41`): a cualquier número de ojos, la tiara corona la cabeza del enjambre sin pisar a uno solo — sim: **+31,00 px** de despeje medido vértice a vértice.

## 3 · EL JUGADOR COMPLETO DEVORADO — EL PEGUE DEL FESTÍN

**La autopsia**: el arrastre del festín acelera hasta ~12 px/t, pero el séquito lo seguía con leyes suaves de tope 9–13 px/t — el círculo entero se quedaba ATRÁS (la queja de la v6.77 del haz, ahora en el festín). Y dos joyas con retraso propio: el brillo solar dibujaba un tick tarde desde su propio centro, y el anillo dorsal no giraba con el cuerpo.

**La cura** — la ley v6.77 (el pegue al ancla) generalizada: **`DeltaDelFestin`** (el movimiento del portador por tick) + **`PegarAlFestin`** (ojos y cuchillas heredan el delta: el cuerpo completo viaja RÍGIDO) + el brillo solar con el delta en el dibujado + el anillo dorsal girando con la espalda (`fullRotation`). Sim: retraso máx **41,89 px → 0,00 px** — sube rígido con el portador.

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo verify.csproj (465 .cs contra tML 2026.09.3.1 real) | 0 errores / 0 warnings |
| -build REAL (tModLoader) | 0/0 — 527 entradas, 19.364.059 B |
| GUARDIÁN de texturas | TODO VIVE ✔ |
| AUDIT v6.132 (37 checks) | TODO VERIFICADO ✔ |
| Servidor headless | 0 excepciones |

**Instalación**: copia `AethonMod.tmod` a la carpeta `Mods` del tModLoader y actívalo.
EOF
)
BODY=$(python3 -c "
import json, sys
notas = sys.stdin.read()
print(json.dumps({'tag_name': '$TAG', 'name': 'v6.132 — LAS ALAS REPARADAS · LA CORONA DE CUCHILLAS · EL JUGADOR COMPLETO DEVORADO', 'body': notas, 'draft': False, 'prerelease': False}))
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

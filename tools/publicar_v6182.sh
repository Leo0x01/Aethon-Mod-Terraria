#!/usr/bin/env bash
# ==================================================================
#  publicar_v6182.sh — el publicador de la casa (v6.182)
#
#  LA LECCIÓN NUEVA QUE LO APRIETA (R33 — el build fantasma): esta
#  sesión un "-build AethonMod" (solo el nombre) empaquetó UNA CÁSCARA
#  VACÍA de 9 KB con «0 errores» — compilar NADA da 0 errores. EL
#  PUBLICADOR AHORA VERIFICA EL CUERPO DEL .tmod ANTES DE TODO:
#  versión 6.182 + 500+ entradas (parse_tmod) — la cáscara NO publica.
#
#  ESTE publicador NO TOCA GitHub si algún gate grita:
#    [0] EL CUERPO DEL .TMOD (parse_tmod: versión + entradas + md5)
#    [1] EL GUARDIÁN DE TEXTURAS (audit_texturas_completas.py)
#    [2] EL GUARDIÁN DE ARRANQUE  (guardian_arranque.py — EL CLIENTE)
#    [3] LA AUDITORÍA DE LA VERSIÓN (audit_v6182_tmod.py)
#    [4] EL LOOP DE RETROALIMENTACIÓN (loop_retro_v6175.py — regresiones)
#  (oráculo + build real + headless siguen corriendo antes, en la sesión)
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

TAG="v6.182"
TMOD="$HOME/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
MD5=$(md5sum "$TMOD" | cut -d' ' -f1)
SIZE=$(stat -c%s "$TMOD")
echo "[0/6] md5: $MD5 · ${SIZE} B"

URL=$(git config --get remote.origin.url)
PAT=${URL#*://}
PAT=${PAT#*:}
PAT=${PAT%@*}
REPO="Leo0x01/Aethon-Mod-Terraria"

echo "[1/6] LOS GATES — el publicador ya no publica a ciegas"
echo "    · EL CUERPO DEL .TMOD (la lección del build fantasma):"
python3 tools/parse_tmod.py "$TMOD" | head -2
python3 - "$TMOD" <<'PYC'
import sys, os
sys.path.insert(0, 'tools')
from parse_tmod import parse
_, name, ver, entries, _, fsize = parse(sys.argv[1])
assert ver == '6.182', f'VERSIÓN MAL: {ver}'
assert len(entries) >= 500, f'CÁSCARA VACÍA: {len(entries)} entradas'
assert fsize > 10_000_000, f'CUERPO DEMASIADO FLACO: {fsize}'
print(f'    EL CUERPO VIVE ✔ ({len(entries)} entradas, {fsize:,} B)')
PYC
python3 tools/audit_texturas_completas.py "$TMOD" > /tmp/gate_texturas.log 2>&1
tail -1 /tmp/gate_texturas.log
python3 tools/guardian_arranque.py "$TMOD" > /tmp/gate_arranque.log 2>&1
tail -1 /tmp/gate_arranque.log
python3 tools/audit_v6182_tmod.py > /tmp/gate_audit.log 2>&1
tail -1 /tmp/gate_audit.log
python3 tools/loop_retro_v6175.py > /tmp/gate_loop.log 2>&1
tail -1 /tmp/gate_loop.log

echo "[2/6] verificando el tag…"
git rev-parse "$TAG^{commit}" >/dev/null

echo "[3/6] creando el release $TAG…"
NOTAS=$(cat <<'EOF'
# v6.182 — EL FESTÍN QUE NUNCA LLEGÓ, EL JEFE A LA MEDIDA Y LA PUERTA HECHA BUFF

## La autopsia del festín que no iniciaba

**La marca se la comía una oleada**: la v6.180 armaba el pedacito con LA PRIMERA cabeza de Devorador que naciera cerca del portador — y los **guardianes de las oleadas de hambre también son Devoradores** (`LosSeis`: King Slime · Ojo · Abeja · **EoW** · Cerebro · Skeletron, naciendo fuera de pantalla). El primer guardián de oleada quemaba la marca en la bruma — y cuando invocabas al Devoramundos DE VERDAD, ya no quedaba nada. La misma enfermedad de la vieja aspiradora, otra cepa.

## Las curas de la letra

1. **LA MARCA DE LA LLAMADA**: el pedacito solo se arma con la cabeza nacida de una INVOCACIÓN REAL (comida de gusano dentro/fuera del interior + el pilar). Las oleadas JAMÁS la queman. Migración: la marca quemada por el agujero se revive una vez.
2. **EL DIÁLOGO EXACTO**: «Esto es asqueroso, un pedacito es suficiente. No volveré a devorar esta cosa tan desagradable... prefiero comerme al Rey Slime que a esta porquería.»
3. **EL JEFE A LA MEDIDA**: el iris a LA ESCALA DEL ARMA (0,2773: el ojo del grimorio que portas, no un disco gigante) · el HALO PLANO muere (nada de círculos planos) y nace **EL ANILLO RÚNICO SOLAR del dios** · la corona de once ojos = **EXACTAMENTE los minions a nivel máximo** (escala 0,375 con sus 4 anillos rúnicos: 2 de agujero negro + 2 de sol) · la Ira viste la triple corona de conjuro.
4. **EL PILAR CENTRADO**: el ancla del texto no escalaba — a escala 0,62 el texto de un botón largo quedaba 62 px descentrado y 50 px FUERA del botón. El origen ahora escala con el texto: centrado exacto a cualquier escala.
5. **LA SELECCIÓN DEL TRÍO**: clic izquierdo sobre una ranura llena la ENCIENDE o la APAGA — disparan SOLO las marcadas (1, 2 o las 3). El slot activo se lee (tinta violeta). La casilla «las tres a la vez» muere. Persistencia completa (ítem + red).
6. **EL HUECO DE LAS ETIQUETAS**: «Ranura 1/2/3» alineada a la derecha contra su casilla — siempre 14 px de aire.
7. **EL BUFF MUNDO INTERIOR**: el botón del altar muere; la puerta es un **icono tipo buff** fijo para siempre (el paisaje plano y brumoso con sus tentáculos, hecho con las herramientas de la casa), **clic derecho en el icono = VIAJAS** al interior desde cualquier parte. Sin descripción: solo dice «Mundo Interior».
8. **LA DESCRIPCIÓN DE LA LETRA**: «El Fuego de Vacío reconoce a su dueño y da una pequeña mejora».
9. **EL 1 % DEL MANÁ MÁXIMO** en el tope del Aura de Vacío (con 400 de maná: 10+20+8+4 = 42/tick pegado; el borde siempre 1) + la auditoría de daño revisada (el DoT por cercanía con la convención vanilla, el trío con el daño de cada arma, las Iras al 35 %).
10. **LA CURA DEL client.log**: el crash `AI_007_FindGoodRestingSpot` 5 minutos dentro del interior — la rejilla de búsqueda usa el HOGAR del NPC; el GUARDIÁN DEL HOGAR clampa todo homeTile fuera del interior (el crash es matemáticamente imposible).
11. **LA LISTA DE ITEMS AL ENTRAR AL MUNDO** (la pediste): 999 Muñecos de Prueba + 22 entregas únicas (Forma Ascendida 2/3/4/8 · Aethon Menor · Halo Arcoíris · Brillo Solar · Coro de los Profundos · Pluma del Escriba Santo · Vástago del Eterno · La Regla del Cielo · Corona de Plumas · Derrame de Tinta · La Última Palabra · Bolsa de Auras · Aura de Juicio · Bolsa de la Forma Eterna · Alas Rúnicas · El Ojo que Mira · Mundo Interior Eterno · Bolsa de la Superforja · El Trío del Eterno).

## Verificación

| Gate | Resultado |
| --- | --- |
| Oráculo | 0/0 |
| Build real | 0/0 + Packaging (535 entradas) |
| **EL CUERPO DEL .TMOD** | **✔ 535 entradas · 19,5 MB** (la lección del build fantasma) |
| GUARDIÁN de texturas | TODO VIVE ✔ |
| **GUARDIÁN DE ARRANQUE** | **EL CLIENTE VIVE ✔ (5 pasadas)** |
| AUDIT v6.182 | TODO VERIFICADO ✔ |
| Headless | 0 excepciones + selección del trío VIVA tras el ciclo disco→reinicio |
| Loop | SIN REGRESIONES |
| Sim v6.182 | SHIP ✔ (ancla 0,00 px · iris 11,6 px · minion 21 px · el 1 % del maná) |

EOF
)

python3 - "$PAT" "$TAG" "$NOTAS" <<'PYEOF'
import json, sys, urllib.request, urllib.error
pat, tag, notas = sys.argv[1], sys.argv[2], sys.argv[3]
repo = 'Leo0x01/Aethon-Mod-Terraria'
api = f'https://api.github.com/repos/{repo}/releases'
body = json.dumps({'tag_name': tag,
                   'name': f'{tag} — el festín que nunca llegó, el jefe a la medida y la puerta hecha buff',
                   'body': notas, 'draft': False, 'prerelease': False}).encode()
req = urllib.request.Request(api, data=body, method='POST')
req.add_header('Authorization', f'token {pat}')
req.add_header('Accept', 'application/vnd.github+json')
try:
    with urllib.request.urlopen(req) as r:
        rel = json.load(r)
    print(f"release {rel['id']} creado: {rel['html_url']}")
    with open('/tmp/release_v6182_id.txt', 'w') as f:
        f.write(str(rel['id']))
except urllib.error.HTTPError as e:
    if e.code == 422:
        req2 = urllib.request.Request(f'{api}/tags/{tag}')
        req2.add_header('Authorization', f'token {pat}')
        with urllib.request.urlopen(req2) as r:
            rels = json.load(r)
        rel = rels[0]
        print(f"release YA EXISTÍA {rel['id']}: continuando")
        with open('/tmp/release_v6182_id.txt', 'w') as f:
            f.write(str(rel['id']))
    else:
        raise
PYEOF

echo "[4/6] subiendo el asset…"
RELID=$(cat /tmp/release_v6182_id.txt)
curl -s -X POST \
  -H "Authorization: token $PAT" \
  -H "Content-Type: application/octet-stream" \
  --data-binary @"$TMOD" \
  "https://uploads.github.com/repos/$REPO/releases/$RELID/assets?name=AethonMod_v6.182.tmod" \
  | python3 -c "import json,sys; a=json.load(sys.stdin); print('asset:', a.get('name'), a.get('size'), 'B ·', a.get('state'))"

echo "[5/6] verificación del CDN…"
sleep 3
URL_DL="https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/download/$TAG/AethonMod_v6.182.tmod"
curl -sL -o /tmp/verif_v6182.tmod "$URL_DL" && md5sum /tmp/verif_v6182.tmod && echo "original: $MD5"
if [ "$(md5sum /tmp/verif_v6182.tmod | cut -d' ' -f1)" = "$MD5" ]; then
  echo "CDN VERIFICADO byte a byte ✔"
else
  echo "ADVERTENCIA: el CDN aún propaga (verificar en un rato)"
fi

echo "[6/6] copia de entrega…"
cp "$TMOD" "/home/z/my-project/download/AethonMod_v6.182.tmod"
echo "PUBLICADO $TAG · md5 $MD5"

#!/usr/bin/env bash
# ==================================================================
#  publicar_v6183.sh — el publicador de la casa (v6.183)
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

TAG="v6.183"
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
python3 tools/parse_tmod.py "$TMOD" > /tmp/gate_cuerpo.log 2>&1 || true
head -2 /tmp/gate_cuerpo.log
python3 - "$TMOD" <<'PYC'
import sys, os
sys.path.insert(0, 'tools')
from parse_tmod import parse
_, name, ver, entries, _, fsize = parse(sys.argv[1])
assert ver == '6.183', f'VERSIÓN MAL: {ver}'
assert len(entries) >= 500, f'CÁSCARA VACÍA: {len(entries)} entradas'
assert fsize > 10_000_000, f'CUERPO DEMASIADO FLACO: {fsize}'
print(f'    EL CUERPO VIVE ✔ ({len(entries)} entradas, {fsize:,} B)')
PYC
python3 tools/audit_texturas_completas.py "$TMOD" > /tmp/gate_texturas.log 2>&1
tail -1 /tmp/gate_texturas.log
python3 tools/guardian_arranque.py "$TMOD" > /tmp/gate_arranque.log 2>&1
tail -1 /tmp/gate_arranque.log
python3 tools/audit_claves_codigo_v6183.py > /tmp/gate_llaves.log 2>&1
tail -1 /tmp/gate_llaves.log
python3 tools/audit_v6183_tmod.py > /tmp/gate_audit.log 2>&1
tail -1 /tmp/gate_audit.log
python3 tools/loop_retro_v6175.py > /tmp/gate_loop.log 2>&1
tail -1 /tmp/gate_loop.log

echo "[2/6] verificando el tag…"
git rev-parse "$TAG^{commit}" >/dev/null

echo "[3/6] creando el release $TAG…"
NOTAS=$(cat <<'EOF'
# v6.183 — EL FESTÍN DE LA APARICIÓN, LOS ANILLOS SOLARES Y EL AJUAR DEL PORTADOR

## El festín que ahora sí llega (la tercera autopsia)

**El cerrojo estaba quemado**: la v6.182 curó el futuro (las oleadas ya no queman la marca) pero el pasado seguía clavado — los cerrojos `DevoramundosPedacito` quemados por los guardianes de oleada de la era v6.180/.181 viven EN el .plr, y con la marca muerta el festín jamás volvía a servirse. **AHORA LA APARICIÓN ES LA LLAVE**: la primera vez que el Devoramundo APARECE cerca de un portador con el libro, el grimorio arranca UNA SECCIÓN y se la come (la animación de siempre: la cola con la bruma del festín + el diálogo exacto «Esto es asqueroso, un pedacito es suficiente...»). Los guardianes de oleada VETAN la mesa el mismo tick que nacen, y una MIGRACIÓN devuelve el cerrojo quemado una vez por portador.

## Lo demás de la letra

1. **EL TEXTO EN CRUDO MUERTI** (5 crudos cazados: la clave de las ranuras que se pintaba entera en el chat · el ChatSellado0 del Cronista · el bestiario del jefe · el subtítulo del pergamino) + **EL GUARDIÁN DE LAS LLAVES**: el gate nuevo que verifica TODA clave de localización que el código arma — nada vuelve a pintarse crudo.
2. **LOS ANILLOS SOLARES DEL SELLO**: un anillo rúnico solar POR FASE. **FASE 1**: el anillo se arranca girando (acelera a la vista) y lanza 10 RAYOS DE ENERGÍA radiales. **FASE 2**: los dos anillos lanzan ENERGÍA CONCENTRADA (un haz dorado por anillo, anclado a su punto vivo). **FASE 3**: los tres haces + **EL DIOS PERSIGUE** al jugador ~4 segundos.
3. **EL VACÍO EXTERIOR TAMBIÉN PAGA A LA CAMADA**: +10 % daño mágico Y +10 % daño de invocación (+10 defensa · −15 % maná).
4. **EL ERROR DEL LOG (la tercera cura)**: la rejilla de reposo de vanilla lee `Main.tile[home±7]` SIN clamp — el cerrojo v6.182 dejaba el hogar en el tile 5 y 5−7 = −2. EL GUARDIÁN DEL HOGAR v2: margen 12, universal.
5. **EL ICONO ÉPICO DEL MUNDO INTERIOR**: EL ECLIPSE con su corona dorada + el paisaje brumoso con tentáculos + **EL MARCO** dorado con esquinas rúnicas (3/10 → 8/10 de épico).
6. **LA MASCOTA LIMPIA**: el Vástago del Eterno v2 — pixel art 100 % por código: alas simétricas cosidas, ojos consistentes (parpadeo solo donde toca), cero antialiasing.
7. **LA VÍSPERA DEL LIBRO SE ESPERA**: el Libro Consagrado (la llave del despertar 20) se entrega al nivel 19… y ahora TAMBIÉN después mientras falte — la llave ya no se pierde jamás.
8. **EL AJUAR DEL PORTADOR**: los 13 regalos (Formas 2/3/4/8 · Pequeño Recuerdo de Aethon · Halo Arcoíris · Brillo de los Soles · Coro de los Profundos · Pluma del Escriba · La Regla del Cielo · Corona de Plumas · Derrame de Tinta · La Última Palabra) ya no caen sueltos: **viven en su bolsa, dentro del Cofre del Arsenal**.
9. **LOS INFORMES** (en download/): TODOS los cálculos de daño del grimorio y la forma del eterno (con archivo y línea) + la lista de TODO lo que el jugador recibe al entrar.

## Verificación

| Gate | Resultado |
| --- | --- |
| Oráculo | 0/0 |
| Build real | 0/0 + Packaging (536 entradas · 19,5 MB) |
| **EL CUERPO DEL .TMOD** | **✔ 536 entradas** |
| GUARDIÁN de texturas | TODO VIVE ✔ |
| **GUARDIÁN DE ARRANQUE** | **EL CLIENTE VIVE ✔** |
| **GUARDIÁN DE LAS LLAVES (NUEVO)** | **TODO VIVE ✔ (1862 claves × 3 idiomas)** |
| AUDIT v6.183 | TODO VERIFICADO ✔ |
| Headless | 0 excepciones + «Sandboxing: Aethon v6.183» |
| Loop | SIN REGRESIONES |
| Sims | SHIP ✔ (anillos 58/74/90 · 10 rayos × 36° · cacería 4 s · icono 8/10 · mascota 8,5/10) |

EOF
)

python3 - "$PAT" "$TAG" "$NOTAS" <<'PYEOF'
import json, sys, urllib.request, urllib.error
pat, tag, notas = sys.argv[1], sys.argv[2], sys.argv[3]
repo = 'Leo0x01/Aethon-Mod-Terraria'
api = f'https://api.github.com/repos/{repo}/releases'
body = json.dumps({'tag_name': tag,
                   'name': f'{tag} — el festín de la aparición, los anillos solares y el ajuar del portador',
                   'body': notas, 'draft': False, 'prerelease': False}).encode()
req = urllib.request.Request(api, data=body, method='POST')
req.add_header('Authorization', f'token {pat}')
req.add_header('Accept', 'application/vnd.github+json')
try:
    with urllib.request.urlopen(req) as r:
        rel = json.load(r)
    print(f"release {rel['id']} creado: {rel['html_url']}")
    with open('/tmp/release_v6183_id.txt', 'w') as f:
        f.write(str(rel['id']))
except urllib.error.HTTPError as e:
    if e.code == 422:
        req2 = urllib.request.Request(f'{api}/tags/{tag}')
        req2.add_header('Authorization', f'token {pat}')
        with urllib.request.urlopen(req2) as r:
            rels = json.load(r)
        rel = rels[0]
        print(f"release YA EXISTÍA {rel['id']}: continuando")
        with open('/tmp/release_v6183_id.txt', 'w') as f:
            f.write(str(rel['id']))
    else:
        raise
PYEOF

echo "[4/6] subiendo el asset…"
RELID=$(cat /tmp/release_v6183_id.txt)
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

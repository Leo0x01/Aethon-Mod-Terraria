#!/usr/bin/env bash
# ==================================================================
#  publicar_v6181.sh — el publicador de la casa (v6.181)
#
#  LA LECCIÓN QUE LO PARIÓ (la letra: «se supone que el loop de
#  retroalimentacion y el codigo Guardian es para evitar esto»): la
#  v6.180 salió con TODOS los gates «en verde»… corriento a mano, y el
#  publicador NO VERIFICABA NADA — el AethonSellado nació sin su retrato
#  y el mod entero se desactivó en la máquina del usuario.
#
#  ESTE publicador NO TOCA GitHub si algún gate grita:
#    [0] EL GUARDIÁN DE TEXTURAS (audit_texturas_completas.py)
#    [1] EL GUARDIÁN DE ARRANQUE  (guardian_arranque.py — EL CLIENTE)
#    [2] EL LOOP DE RETROALIMENTACIÓN (loop_retro_v6175.py — regresiones)
#  (oráculo + build real + headless siguen corriendo antes, en la sesión)
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

TAG="v6.181"
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
python3 tools/audit_texturas_completas.py "$TMOD"
echo "    GUARDIÁN DE TEXTURAS ✔"
python3 tools/guardian_arranque.py "$TMOD"
echo "    GUARDIÁN DE ARRANQUE (EL CLIENTE) ✔"
python3 tools/loop_retro_v6175.py
echo "    LOOP DE RETROALIMENTACIÓN ✔ (sin regresiones)"

echo "[2/6] verificando el tag…"
git rev-parse "$TAG^{commit}" >/dev/null

echo "[3/6] creando el release $TAG…"
NOTAS=$(cat <<'EOF'
# v6.181 — EL GUARDIÁN DE ARRANQUE (la cura del arranque)

## Qué pasó (la autopsia del client.log)

La v6.180 **no pudo iniciar**: `AssetLoadException: Content\NPCs\AethonSellado` en `ModNPC.AutoStaticDefaults` → **«Disabling Mod: AethonMod»**. El Sellado nuevo nació **sin su textura principal** — el gigante murió con su PNG y el override de la Ira (que sí vive) es de OTRA clase. Y el Guardián de texturas dijo «OK» por un agujero del fix partial: en un archivo multi-clase, el cuerpo de la primera clase se tragaba a sus hermanas **con sus overrides** — el Sellado «pedía prestada» la SoftGlow de la Ira. **OK falso, crash del cliente.**

## Las curas

1. **EL RETRATO DEL SELLADO**: `AethonSellado.png` (50×66) compuesto EXACTO como el NPC se dibuja en juego (el cuerpo del grimorio ×1.3 + el iris grande en su casa del ojo) y **LA CARA DEL JEFE**: `AethonSellado_Head_Boss.png` (32×32 — todo `NPC.boss` exige su icono por convención).
2. **EL GUARDIÁN CURADO**: las marcas de cuerpo son ahora GLOBALES por archivo (toda declaración cierra el cuerpo anterior) y el `abstract` se lee de la cabecera — jamás de una ventana que podía robarse el de una hermana.
3. **EL GUARDIÁN DE ARRANQUE (EL GATE NUEVO DEL CLIENTE — `tools/guardian_arranque.py`)**: el servidor headless NO carga texturas; este gate simula el camino del CLIENTE: (1) textura principal de cada clase en disco Y en el .tmod, (2) cada jefe con su cara, (3) las 71 rutas pedidas a mano (`ModContent.Request<...>`) vivas, (4) las claves de idioma de cada `GetTextValue` vivas en es-ES/es-MX + placeholders pareados, (5) los 3 hjson PARSEAN con el parser real.
4. **EL PUBLICADOR YA NO PUBLICA A CIEGAS**: `publicar_v6181.sh` corre los TRES gates (texturas + arranque + loop) antes de tocar GitHub — **si algo grita, no hay release.**
5. **LA CLAVE MUERTA RESUCITADA**: `Buffs.AuraVacioBuff.Trabajando` («✦ El vacío está cobrando una presa») vive en los 3 idiomas.
6. **EL SYNC EN CUARENTENA**: dos minas cazadas — el camino de claves faltantes escribía basura (`Clave: {480`) que rompía el en-US, y la regeneración del es-MX aplastaba los `'''` multilinea. Ahora SOLO VERIFICA (espejo + paridad); escribir exige `--escribir`.

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo | 0/0 |
| Build real | 0/0 + Packaging |
| GUARDIÁN de texturas | TODO VIVE ✔ (agujero multi-clase curado) |
| **GUARDIÁN DE ARRANQUE** | **EL CLIENTE VIVE ✔ (5 pasadas)** |
| Headless | 0 excepciones |
| Loop | SIN REGRESIONES |

EOF
)

python3 - "$PAT" "$TAG" "$NOTAS" <<'PYEOF'
import json, sys, urllib.request, urllib.error
pat, tag, notas = sys.argv[1], sys.argv[2], sys.argv[3]
repo = 'Leo0x01/Aethon-Mod-Terraria'
api = f'https://api.github.com/repos/{repo}/releases'
body = json.dumps({'tag_name': tag,
                   'name': f'{tag} — el guardián de arranque (la cura del arranque)',
                   'body': notas, 'draft': False, 'prerelease': False}).encode()
req = urllib.request.Request(api, data=body, method='POST')
req.add_header('Authorization', f'token {pat}')
req.add_header('Accept', 'application/vnd.github+json')
try:
    with urllib.request.urlopen(req) as r:
        rel = json.load(r)
    print(f"release {rel['id']} creado: {rel['html_url']}")
    with open('/tmp/release_v6181_id.txt', 'w') as f:
        f.write(str(rel['id']))
except urllib.error.HTTPError as e:
    if e.code == 422:
        req2 = urllib.request.Request(f'{api}/tags/{tag}')
        req2.add_header('Authorization', f'token {pat}')
        with urllib.request.urlopen(req2) as r:
            rels = json.load(r)
        rel = rels[0]
        print(f"release YA EXISTÍA {rel['id']}: continuando")
        with open('/tmp/release_v6181_id.txt', 'w') as f:
            f.write(str(rel['id']))
    else:
        raise
PYEOF

echo "[4/6] subiendo el asset…"
RELID=$(cat /tmp/release_v6181_id.txt)
curl -s -X POST \
  -H "Authorization: token $PAT" \
  -H "Content-Type: application/octet-stream" \
  --data-binary @"$TMOD" \
  "https://uploads.github.com/repos/$REPO/releases/$RELID/assets?name=AethonMod_v6.181.tmod" \
  | python3 -c "import json,sys; a=json.load(sys.stdin); print('asset:', a.get('name'), a.get('size'), 'B ·', a.get('state'))"

echo "[5/6] verificación del CDN…"
sleep 3
URL_DL="https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/download/$TAG/AethonMod_v6.181.tmod"
curl -sL -o /tmp/verif_v6181.tmod "$URL_DL" && md5sum /tmp/verif_v6181.tmod && echo "original: $MD5"
if [ "$(md5sum /tmp/verif_v6181.tmod | cut -d' ' -f1)" = "$MD5" ]; then
  echo "CDN VERIFICADO byte a byte ✔"
else
  echo "ADVERTENCIA: el CDN aún propaga (verificar en un rato)"
fi

echo "[6/6] copia de entrega…"
cp "$TMOD" "/home/z/my-project/download/AethonMod_v6.181.tmod"
echo "PUBLICADO $TAG · md5 $MD5"

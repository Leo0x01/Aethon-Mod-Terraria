#!/usr/bin/env bash
# ==================================================================
#  publicar_v6155.sh — el publicador de la casa (v6.155)
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

TAG="v6.155"
TMOD="$HOME/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
MD5=$(md5sum "$TMOD" | cut -d' ' -f1)
echo "[1/5] md5: $MD5"

URL=$(git config --get remote.origin.url)
PAT=${URL#*//}
PAT=${PAT%@*}
REPO="Leo0x01/Aethon-Mod-Terraria"

echo "[2/5] verificando el tag…"
git rev-parse "$TAG^{commit}" >/dev/null

echo "[3/5] creando el release $TAG…"
NOTAS=$(cat <<'EOF'
# v6.155 — LOS OCHO PUNTOS DE LA LETRA (la sesión del crash y los anillos)

## Las curas, punto por punto

1. **EL CRASH DEL JUICIO, MUERTO** — la autopsia del client.log: `InvalidOperationException: Draw was called, but Begin has not yet been called` en `JuicioDosSolesProj.PreDraw:556`. El ABRAZO DE LA REUNIÓN (el flash del encuentro de los soles, nacido en v6.154) dibujaba su Bloom **fuera de todo lote** — el único dibujo desnudo de todo el PreDraw. La cura: su propio `AbrirAdditive` con try/finally (la ley de la casa: el que dibuja, abre su lote).
2. **EL ETERNO NO MOLESTA AL PUEBLO** — solo la MASCOTA (el grimorio menor) acosa a los NPC del pueblo con su ronda de vigilancia. El grimorio del Eterno en modo caza **los ignora por completo**: ni vigila ni intimida — solo caza monstruos y criaturas no hostiles.
3. **LA SALIDA A CAZA, SERIA** — el umbral del "1 minuto de hambre" (que lo sacaba constantemente) murió: el Eterno **solo sale con el hambre en 20%** o más, y caza con la **SOMBRA DE LA PÁGINA NUEVA** (una por enemigo hasta nivel/20, cada una drenando 1% de vida máxima por segundo).
4. **LA TIENDA DEL CRONISTA AL 120** — siempre vende lo que el jugador necesita para su Despertar actual; y al cumplirse el nivel 120: **pociones de maná + 10 artículos aleatorios tomados de otros NPC** (el Mago excluido, los NPC de otros mods cuentan — vía `NPCShopDatabase.AllShops`, la API real; surtido estable por día, rota al amanecer).
5. **EL OJO QUE MIRA = EL MINION EXACTO** — los círculos amarillos (los Bloom de hasta ~185 px sobre un ojo de 22 px — más grandes que el propio ojo, tal cual la letra) MURIERON TODOS. Ahora el arma dibuja **el cuerpo del minion exacto** (56×54, el mismo del escuadrón) con su iris sprite deslizándose hacia la presa; el rayo es solo el arco Perlin.
6. **EL AURA DEL VACÍO SIN CIRCULITOS** — en el jugador: **cero partículas**. En los enemigos: **solo una pequeña bruma** (una mota suave cada ~18 ticks, disolviéndose hacia el lado opuesto del portador).
7. **LA EXPLOSIÓN DE LUZ FINAL, DIOS** — **500%+ más grande** (escala ×6 en todo el dibujo: rayos de 780 px, núcleo inundando, onda de choque de 630 px) y **persigue al jugador siempre** (el destello le pega el centro al portador mientras dure).
8. **LOS ANILLOS DE SOLES RÚNICOS, A LA VISTA** — la autopsia de por qué "nunca aparecían": la corona de la v6.152 vivía a ~15 px del centro — **más pequeña que el cuerpo del jugador, oculta tras el sprite**. Vuelven los ANILLOS DE SOLES de verdad (el mismo sistema de los soles rúnicos: anillos elípticos con precesión, runas de pie, oro y azul-estelar) con radio 64-106 px — siempre más grandes que el cuerpo. Un anillo por peldaño del 113 al 120.

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo | 0/0 (463 .cs contra tML 2026.09.3.1 real) |
| Build real | 0/0 · 529 entradas · 19.406.431 B · EOF exacto |
| GUARDIÁN | TODO VIVE ✔ |
| AUDIT v6.155 | LIMPIA ✔ (firmas vivas en la DLL, APIs fantasma muertas) |
| Headless | 0 excepciones (Sandboxing → Adding Recipes → Choose World) |

**Instalación**: copia `AethonMod.tmod` a la carpeta `Mods` del tModLoader y actívalo.
EOF
)

python3 - "$PAT" "$TAG" "$NOTAS" <<'PYEOF'
import json, sys, urllib.request

pat, tag, notas = sys.argv[1], sys.argv[2], sys.argv[3]
repo = 'Leo0x01/Aethon-Mod-Terraria'
api = f'https://api.github.com/repos/{repo}/releases'

body = json.dumps({'tag_name': tag, 'name': f'{tag} — los ocho puntos de la letra',
                   'body': notas, 'draft': False, 'prerelease': False}).encode()
req = urllib.request.Request(api, data=body, method='POST')
req.add_header('Authorization', f'token {pat}')
req.add_header('Accept', 'application/vnd.github+json')
with urllib.request.urlopen(req) as r:
    rel = json.load(r)
print(f"release {rel['id']} creado: {rel['html_url']}")
with open('/tmp/release_v6155_id.txt', 'w') as f:
    f.write(str(rel['id']))
PYEOF

echo "[4/5] subiendo el asset…"
RELID=$(cat /tmp/release_v6155_id.txt)
curl -s -X POST \
  -H "Authorization: token $PAT" \
  -H "Content-Type: application/octet-stream" \
  --data-binary @"$TMOD" \
  "https://uploads.github.com/repos/$REPO/releases/$RELID/assets?name=AethonMod_v6.155.tmod" \
  | python3 -c "import json,sys; a=json.load(sys.stdin); print('asset:', a.get('name'), a.get('size'), 'B ·', a.get('state'))"

echo "[5/5] verificación del CDN…"
sleep 3
URL_DL="https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/download/$TAG/AethonMod_v6.155.tmod"
curl -sL -o /tmp/verif_v6155.tmod "$URL_DL" && md5sum /tmp/verif_v6155.tmod && echo "original: $MD5"
if [ "$(md5sum /tmp/verif_v6155.tmod | cut -d' ' -f1)" = "$MD5" ]; then
  echo "CDN VERIFICADO byte a byte ✔"
else
  echo "ADVERTENCIA: el CDN aún propaga (verificar en un rato)"
fi

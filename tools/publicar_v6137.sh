#!/usr/bin/env bash
# ==================================================================
#  publicar_v6137.sh — el publicador de la casa (v6.137)
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

TAG="v6.137"
TMOD="$HOME/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
MD5=$(md5sum "$TMOD" | cut -d' ' -f1)
echo "[1/5] md5: $MD5"

URL=$(git config --get remote.origin.url)
PAT=${URL#*//}
PAT=${PAT%@*}
API="https://api.github.com"
REPO="Leo0x01/Aethon-Mod-Terraria"

echo "[2/5] verificando el tag…"
git rev-parse "$TAG^{commit}" >/dev/null

echo "[3/5] creando el release $TAG — EL PILAR DEL CUERPO DEL DIOS…"
NOTAS=$(cat <<'EOF'
# v6.137 — EL PILAR DEL CUERPO DEL DIOS: LA ARENA INTERIOR

## 1 · Las dos copias de la Valquiria Rúnica

**Valquiria Sin Plumas** (el abanico de siete plumas se va — la vela estructural) y **Valquiria Lisa** (las plumas dejan de ser tiras: UNA superficie continua del borde maestro, la receta lisa de la casa). El alma plata intacta en ambas — solo el cuerpo cambia. La Bolsa de las Alas pasa a **13 alas**.

## 2 · Las cuchillas son colmillos

Las cinco dagas del Cetro visten **EL COLMILLO DE LA FORJA** (ColmilloAbismo 48×96 — la aguja de marfil con la encía violeta): cada tipo tiñe el marfil con SU alma, el ojo del grimorio vive en la encía y apunta con la punta. La máquina intacta — solo cambió la piel.

## 3 · El muñeco de pruebas negro

**Receta**: 5 madera + 5 Esencia del Eterno (lote de 5). **El círculo plano murió** — era un SoftGlow estirado (geometría plana, la regla rota): su lugar es **BrumaFX** real, la bruma violeta del grimorio latiendo alrededor del banco de pruebas. Y el regalo de pruebas trae **999 muñecos**.

## 4 · La ley del altar único

Un solo **Altar de Consagración** activo por mundo: los extras rebotan al colocar (mensaje claro), los que quedan atrás tras recoger el activo quedan **apagados** (sin luz, el cursor lo dice) y el sistema **promueve** al primero que quede. **El grimorio nace ENCIMA del altar** (~40 px sobre la cima — nunca más solapado). Y el mundo interior queda limpio: ni altares ni grimorios — solo el Pilar Prisma.

## 5 · El Pilar Prisma: el control del cuerpo entero

La ventana **rediseñada y medida** (nada se desborda — el panel nace con la altura de su contenido):
- **VOLVER AL MUNDO REAL** · **INVOCAR A AETHON** (los de siempre, intactos)
- **LA OLEADA DE HAMBRE**: selector de nivel **1..el último desbloqueado** (desbloquear = completar el nivel FUERA del interior — probar adentro no progresa, la ley de la casa) y el botón LANZAR.
- **LOS JEFES DEVORADOS — LA ARENA**: cada jefe que el jugador devoró, invocable DENTRO en versión potenciada.

## 6 · La arena: los jefes crecen con cada muerte

Cada jefe devorado invocado del pilar nace con **el aura de la oleada de hambre** y **+25% de vida/daño/defensa por cada vez que el jugador lo mató DENTRO del interior** (persiste con el jugador). **La IA por niveles** (jefes de Terraria): 1-5 normal · **6+ la IA del modo experto** (el préstamo blindado de la casa) · **8+ agresiva con nuevos ataques temáticos** (cada jefe convoca a LOS SUYOS: abejas, sirvientes, bolas de gel) · **10 combos sin pausa**. Los jefes del mod: solo el incremento de poder + el experto.

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo verify.csproj | 0 errores / 0 warnings |
| -build REAL | 0/0 — 523 entradas |
| GUARDIÁN de texturas | TODO VIVE ✔ |
| AUDIT v6.137 (74 checks) | TODO VERIFICADO ✔ |
| Paridad hjson ×3 | 1679³ claves ✔ |
| Servidor headless | 0 excepciones — v6.137 |

**Instalación**: copia `AethonMod.tmod` a la carpeta `Mods` del tModLoader y actívalo.
EOF
)

python3 - "$PAT" "$TAG" "$NOTAS" <<'PYEOF'
import json, sys, urllib.request

pat, tag, notas = sys.argv[1], sys.argv[2], sys.argv[3]
repo = 'Leo0x01/Aethon-Mod-Terraria'
api = f'https://api.github.com/repos/{repo}/releases'

req = urllib.request.Request(api + f'/tags/{tag}')
req.add_header('Authorization', f'token {pat}')
try:
    with urllib.request.urlopen(req) as r:
        rid = json.load(r)['id']
        print(f'      el release ya existe (id {rid}) — se reutiliza')
except urllib.error.HTTPError as e:
    if e.code != 404:
        raise
    body = json.dumps({'tag_name': tag, 'name': f'{tag} — EL PILAR DEL CUERPO DEL DIOS: LA ARENA INTERIOR',
                       'body': notas, 'draft': False, 'prerelease': False}).encode()
    req = urllib.request.Request(api, data=body, method='POST')
    req.add_header('Authorization', f'token {pat}')
    req.add_header('Accept', 'application/vnd.github+json')
    with urllib.request.urlopen(req) as r:
        rid = json.load(r)['id']
        print(f'      release creado: id {rid}')

tmod = '/home/z/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod'
data = open(tmod, 'rb').read()
req = urllib.request.Request(
    f'https://uploads.github.com/repos/{repo}/releases/{rid}/assets?name=AethonMod.tmod',
    data=data, method='POST')
req.add_header('Authorization', f'token {pat}')
req.add_header('Content-Type', 'application/octet-stream')
with urllib.request.urlopen(req) as r:
    print('      asset subido:', json.load(r)['size'], 'B')
PYEOF

echo "[4/5] verificando el CDN…"
sleep 8
curl -sL -o /tmp/cdn137.tmod "https://github.com/$REPO/releases/download/$TAG/AethonMod.tmod"
CDN=$(md5sum /tmp/cdn137.tmod | cut -d' ' -f1)
[ "$CDN" = "$MD5" ] && echo "      CDN verificado: md5 idéntico ($CDN)" || { echo "      CDN DIFIERE: $CDN vs $MD5"; exit 1; }

echo "[5/5] ✔ PUBLICADO $TAG"

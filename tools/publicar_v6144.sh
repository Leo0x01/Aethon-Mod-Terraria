#!/usr/bin/env bash
# ==================================================================
#  publicar_v6139.sh — el publicador de la casa (v6.144)
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

TAG="v6.144"
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
# v6.144 — EL DESPERTAR DE LOS CIENTO VEINTE + EL DIOS SELLADO

## Los 12 puntos de la letra

1. **Las alas nuevas viven** (textos ×3) — y la caza de textos crudos cubrió TODO el mod.
2. **La Forma Eterna → Pieza de Lore**: su descripción ya no explica mecánica — lore provisional, a reemplazar.
3. **El lore del grimorio sin margen**: los 8 espacios de sangría dura murieron.
4. **La barra del despertar DINÁMICA**: /NivelMax — si el máximo sube, se actualiza sola (ventana del grimorio + cielo).
5. **El devorar 101-120**: armas de la casa + materiales endgame + puertas de jefe por peldaño.
6. **El hito 120 = LAS ALAS RÚNICAS + EL AURA DEL VACÍO**: la Manifestación estalla al 120 (MÁS GRANDE — la del 112 ANULADA); la Forma Eterna nace con inmunidad a caídas + vuelo infinito; el 2% del maná total suma al daño máximo del aura.
7. **El Ojo que Mira**: tamaño minion (22×22), animación del escuadrón (3 texturas), rayo de PERLIN.
8. **Las mascotas**: «Pequeño Recuerdo de Aethon» + «un pequeño grimorio travieso y curioso».
9. **El Libro**: icono junto a logros · tinta legible · ojo de abajo muerto · scroll por página · izquierda=info · derecha=LORE (sin inventar: «Pieza de Lore» donde no hay historia).
10. **El Pilar Interior**: sprite SForja (pilar real + ojo minion parpadeando en la punta).
11. **La Sombra de la Página II**: sin ojos ni dientes, rebotes de la muñeca maldita al tono de la casa, animación post-lanzamiento.
12. **AETHON (SELLADO)**: el jefe del mundo interior (SHIFT + pilar) — vuelo de dos tiempos, teleport de bruma, 3 ojos-minion con barra (reviven 30 s), el arsenal disparado por los ojos.

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo verify.csproj | 0 errores / 0 warnings (464 .cs) |
| -build REAL | 0/0 — GUARDIÁN TODO VIVE |
| AUDIT de la letra | 12/12 puntos |
| hjson ×3 | parsea (llaves curadas) |
| Servidor headless | 0 excepciones |

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
    body = json.dumps({'tag_name': tag, 'name': f'{tag} — LA SESIÓN DE LAS NUEVE CURAS',
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
curl -sL -o /tmp/cdn139.tmod "https://github.com/$REPO/releases/download/$TAG/AethonMod.tmod"
CDN=$(md5sum /tmp/cdn139.tmod | cut -d' ' -f1)
[ "$CDN" = "$MD5" ] && echo "      CDN verificado: md5 idéntico ($CDN)" || { echo "      CDN DIFIERE: $CDN vs $MD5"; exit 1; }

echo "[5/5] ✔ PUBLICADO $TAG"

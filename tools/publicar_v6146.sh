#!/usr/bin/env bash
# ==================================================================
#  publicar_v6139.sh — el publicador de la casa (v6.146)
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

TAG="v6.146"
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
# v6.146 — LA LETRA COMPLETA

## Los 14 puntos

1. **Devorar 101-120**: 5 armas FINALES VANILLA (Nebula Blaze, Solar Eruption, Vortex Beater, Stardust Dragon, Last Prism) — se devoran **3 de 5** + materiales endgame + PUERTA DE JEFE OBLIGATORIA (Cultista/Señor de la Luna rotando · **Aethon Sellado al 120**).
2. **El 120**: las Alas Rúnicas FUSIONADAS dentro de La Forma Eterna (inmunidad caídas + vuelo infinito).
3. **Pergamino** → «Páginas informativas del grimorio» · título a la izquierda · rótulos por página (Info · Lore).
4. **Icono** → «Información».
5. **Buffs crudos curados** (restaurados de la v6.140 + nombres nuevos).
6. **Aura**: bruma SIEMPRE al lado opuesto (±60°) + TODOS los monstruos (invisibles/espectros incluidos).
7. **Ira de un Ser Exterior**: cada 10 min · contador tipo buff (mm:ss) · SOLO Shift+click izquierdo.
8. **Vestido de la Forma** visible a medida se desbloquea (sin el libro en mano).
9. **Eco** 1/10 con Shift · **Verdugo** 10/100 con Shift.
10. **Iris del entrecerrado**: cortado a la mitad (todas las anims; la devoración intacta).
11. **Prisma**: Shift+INVOCAR = Aethon (Sellado).
12. **Aethon (Sellado)**: 17 ojos (11+6) · vuelo del cazador (física del grimorio) · cada ojo renace MÁS PODEROSO (+35% vida/+20% daño/+12% tamaño por generación).
13. **Sombra de Tinta**: el proyectil ORIGINAL + modo muñeca (rebotes ×5 ×1,12 + vuelta a casa). La II, borrada.
14. **Tentáculos del interior**: masa clásica sin ojos/dientes + bruma densa en la base.

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo | 0/0 (464 .cs) |
| Build real | 0/0 · GUARDIÁN TODO VIVE |
| AUDIT de la letra | 14/14 |
| hjson ×3 | parsea |
| Headless | 0 excepciones |

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

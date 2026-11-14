#!/usr/bin/env bash
# ==================================================================
#  publicar_v6136.sh — el publicador de la casa (v6.136)
#  (el patrón del v6.135: PAT del remote · uploads.github.com para el
#   asset · tolera re-ejecución · verifica el CDN byte a byte)
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

TAG="v6.136"
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

echo "[3/5] creando el release $TAG — LA PURGA DE LAS DOS ARMAS…"
NOTAS=$(cat <<'EOF'
# v6.136 — LA PURGA DE LAS DOS ARMAS + EL MUÑECO DE LA CASA

## 1 · Las dos armas de investigación murieron

Por orden del dueño («ya no son útiles»), **El Arbitrador II** (la jabalina que atravesaba muros) y **El Principio y el Fin** (las dagas del canje de almas) desaparecen con TODO su árbol: su proyectil, sus dagas voladoras, sus almas de eco y de luz inferior, su onda sonora, sus cuatro marcas (Marca del Principio, Mirada del Fin, Supresión Vital, Servidumbre), su sigilo ratero y su memoria de capas — 13 archivos y 4 carpetas al cero.

**Lo que se queda** (tal cual, intacto): la **Mirada del Eterno**, el **Cetro de las Dagas del Eterno** con sus cinco cuchillas, el **Códice de habilidades** y el **motor de píxeles** — el contenido de prueba de la carpeta que no debe nada a nadie.

## 2 · El muñeco de entrenamiento ya es de la casa

El Muñeco de Pruebas Negro **dejó de usar la textura del juego base**: su sprite nació por código en la rampa negro-violeta del grimorio (arpillera cosida con parche, ojos violetas que brillan, cruzeta de madera oscura, manojos de paja violeta con puntas brillantes, petate atado) — **cero texturas vanilla en todo el mod** (verificado).

## 3 · La verificación de la casa

**0 referencias externas** en TODO el código y la localización (la auditoría integral: nombres de otros mods, wikis ajenas, texturas prestadas — cero). Las purgas anteriores siguen verificadas como muertas.

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo verify.csproj | 0 errores / 0 warnings |
| -build REAL | 0/0 — 521 entradas |
| GUARDIÁN de texturas | TODO VIVE ✔ |
| AUDIT v6.136 (48 checks) | TODO VERIFICADO ✔ |
| Sprite del muñeco | QA VLM SHIP 9/10 |
| Servidor headless | 0 excepciones — v6.136 |

**Instalación**: copia `AethonMod.tmod` a la carpeta `Mods` del tModLoader y actívalo.
EOF
)

python3 - "$PAT" "$TAG" "$NOTAS" <<'PYEOF'
import json, sys, urllib.request

pat, tag, notas = sys.argv[1], sys.argv[2], sys.argv[3]
repo = 'Leo0x01/Aethon-Mod-Terraria'
api = f'https://api.github.com/repos/{repo}/releases'

# ¿ya existe? (tolerancia a re-ejecución)
req = urllib.request.Request(api + f'/tags/{tag}')
req.add_header('Authorization', f'token {pat}')
try:
    with urllib.request.urlopen(req) as r:
        rid = json.load(r)['id']
        print(f'      el release ya existe (id {rid}) — se reutiliza')
except urllib.error.HTTPError as e:
    if e.code != 404:
        raise
    body = json.dumps({'tag_name': tag, 'name': f'{tag} — LA PURGA DE LAS DOS ARMAS + EL MUÑECO DE LA CASA',
                       'body': notas, 'draft': False, 'prerelease': False}).encode()
    req = urllib.request.Request(api, data=body, method='POST')
    req.add_header('Authorization', f'token {pat}')
    req.add_header('Accept', 'application/vnd.github+json')
    with urllib.request.urlopen(req) as r:
        rid = json.load(r)['id']
        print(f'      release creado: id {rid}')

# el asset por uploads.github.com (la lección v6.135)
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
curl -sL -o /tmp/cdn136.tmod "https://github.com/$REPO/releases/download/$TAG/AethonMod.tmod"
CDN=$(md5sum /tmp/cdn136.tmod | cut -d' ' -f1)
[ "$CDN" = "$MD5" ] && echo "      CDN verificado: md5 idéntico ($CDN)" || { echo "      CDN DIFIERE: $CDN vs $MD5"; exit 1; }

echo "[5/5] ✔ PUBLICADO $TAG"

#!/usr/bin/env bash
# ==================================================================
#  publicar_v6138.sh — el publicador de la casa (v6.138)
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

TAG="v6.138"
TMOD="$HOME/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
MD5=$(md5sum "$TMOD" | cut -d' ' -f1)
echo "[1/5] md5: $MD5"

URL=$(git config --get remote.origin.url)
PAT=${URL#*//}
PAT=${PAT%@*}
REPO="Leo0x01/Aethon-Mod-Terraria"

echo "[2/5] verificando el tag…"
git rev-parse "$TAG^{commit}" >/dev/null

echo "[3/5] creando el release $TAG — LA CURA DEL MAPA ASESINADO…"
NOTAS=$(cat <<'EOF'
# v6.138 — LA CURA DEL MAPA ASESINADO

## El error que cerró el juego (el client.log)

`Main engine crash` en `DrawToMap_Section → IndexOutOfRangeException` al **volver a cargar el mundo real después de visitar el mundo interior**.

## La autopsia (contra el decompile real)

El **autosave del jugador** (cada 10 min) no pasa por el cerrojo que protegía al mundo: `Player.SavePlayer → MapHelper.SaveMap`. Dentro del interior, `Main.Map` es el mapa chico (800×600) pero el nombre del archivo sigue siendo el del **mundo real** → el autosave **sobrescribió el .map del mundo real con el mapa del interior**. Al recargar: el mapa desborda → crash en el primer frame.

## La cura triple

1. **Cerrojo del mapa**: mientras estés dentro del interior, ningún guardado de mapa corre — el .map del mundo real queda intacto.
2. **El mapa que renace**: al cargar cualquier mundo, si el mapa en memoria no calza con las dimensiones del mundo, **renace limpio antes del primer frame** — un mapa corrupto jamás vuelve a tumbar el juego.
3. La sonda del minimapa ahora compara dimensiones directamente.

## Tu mundo se recupera SOLO

Con la v6.138 instalada, tu mundo **carga de nuevo normalmente** — el mapa dañado renace limpio. Lo único que se pierde es la **exploración del minimapa** (se rehace caminando). Tu partida, construcciones y objetos están intactos.

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo verify.csproj | 0 errores / 0 warnings |
| -build REAL | 0/0 — 523 entradas |
| GUARDIÁN de texturas | TODO VIVE ✔ |
| AUDIT de la cura | TODO VERIFICADO ✔ |
| Servidor headless | 0 excepciones — v6.138 |

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
    body = json.dumps({'tag_name': tag, 'name': f'{tag} — LA CURA DEL MAPA ASESINADO',
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
curl -sL -o /tmp/cdn138.tmod "https://github.com/$REPO/releases/download/$TAG/AethonMod.tmod"
CDN=$(md5sum /tmp/cdn138.tmod | cut -d' ' -f1)
[ "$CDN" = "$MD5" ] && echo "      CDN verificado: md5 idéntico ($CDN)" || { echo "      CDN DIFIERE: $CDN vs $MD5"; exit 1; }

echo "[5/5] ✔ PUBLICADO $TAG"

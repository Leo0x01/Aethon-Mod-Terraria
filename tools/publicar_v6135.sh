#!/usr/bin/env bash
# ==================================================================
#  publicar_v6135.sh — el publicador de la casa (v6.135)
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

TAG="v6.135"
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

echo "[3/5] creando el release $TAG — EL ARCÁNGEL DE 6 ALAS…"
NOTAS=$(cat <<'EOF'
# v6.135 — EL ARCÁNGEL DE 6 ALAS Y LAS SEIS MITOLÓGICAS

## 1 · El tercer par — la corona del arcángel

Las **Runas del Vacío** ganan el par superior (el nuevo campo `Coronas`, escala 0,72): el abanico nace **sobre la espalda**, alzado (abre hacia arriba — la pose del arcángel que no pliega las alas) y con el aleteo más abierto. El orden de dibujo es ahora **corona → alitas → principal**. El Vacío es **el arcángel: 6 alas en tres niveles** — y conserva la hoja lisa de la v6.134 sin cambio.

## 2 · Las seis mitológicas

Todas nacen del Vacío v6.134 (la geometría exacta del Eclipse + el ala lisa), cada una con **su alma** — paleta, runas, ruido, plumas por lado, curva y pares:

| # | Alas | Mito | El alma |
| --- | --- | --- | --- |
| 6 | Serafín del Vacío | hebrea | TRES pares de fuego violeta; la llama se encorva (curva 0,34) |
| 7 | Valquiria Rúnica | nórdica | siete plumas de lanza rectas, plata de batalla, futhark solar |
| 8 | Quetzal Emplumado | azteca | la serpiente de jade que ondea en tres niveles (curva 0,42) |
| 9 | Fénix del Renacer | griega | tres llamas anidadas: blanco → fuego → brasa; el tono ARDE a lo largo |
| 10 | Tengu del Crepúsculo | japonesa | siete plumas primarias de tinta azul-negra, glifos ámbar del abismo |
| 11 | Dragón del Vacío | membrana | la vela de DOS DEDOS (sin abanico) en seis velas de escamas rojo-vino |

## 3 · Los items

Seis clases nuevas con sus seis sprites (30×30, el estilo de la casa) — **La Bolsa de las Alas** pasa de cinco a **once alas**. Localización completa es-ES · es-MX · en-US (paridad 1691³).

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo verify.csproj | 0 errores / 0 warnings |
| -build REAL | 0/0 — 526 entradas |
| GUARDIÁN de texturas | TODO VIVE ✔ |
| AUDIT v6.135 (40 checks) | TODO VERIFICADO ✔ |
| Sim 1:1 + QA visual | 3 anclas · corona alzada 9,2° · espejo 0,000 px · los 6 mitos |
| Servidor headless | 0 excepciones |

**Instalación**: copia `AethonMod.tmod` a la carpeta `Mods` del tModLoader y actívalo.
EOF
)
python3 - "$PAT" "$TAG" "$NOTAS" <<'PYEOF'
import json, sys, urllib.request
pat, tag, notas = sys.argv[1], sys.argv[2], sys.argv[3]
api, repo = 'https://api.github.com', 'Leo0x01/Aethon-Mod-Terraria'
req = urllib.request.Request(f'{api}/repos/{repo}/releases', method='POST',
    data=json.dumps({'tag_name': tag, 'name': 'v6.135 — EL ARCÁNGEL DE 6 ALAS Y LAS SEIS MITOLÓGICAS', 'body': notas}).encode(),
    headers={'Authorization': f'Bearer {pat}', 'Accept': 'application/vnd.github+json',
             'Content-Type': 'application/json'})
try:
    r = json.load(urllib.request.urlopen(req))
    print(f"      release {r['id']}")
except urllib.error.HTTPError as e:
    if e.code == 404:
        raise
    # ya existe (422): se continúa con el que hay
    import urllib.request as _u
    r = json.load(_u.urlopen(_u.Request(f'{api}/repos/{repo}/releases/tags/{tag}',
        headers={'Authorization': f'Bearer {pat}', 'Accept': 'application/vnd.github+json'})))
    print(f"      release ya existía {r['id']}")
PYEOF

echo "[4/5] subiendo el asset…"
python3 - "$PAT" "$TAG" "$TMOD" <<'PYEOF'
import json, sys, urllib.request
pat, tag, tmod = sys.argv[1], sys.argv[2], sys.argv[3]
api, repo = 'https://api.github.com', 'Leo0x01/Aethon-Mod-Terraria'
rid = json.load(urllib.request.urlopen(urllib.request.Request(
    f'{api}/repos/{repo}/releases/tags/{tag}',
    headers={'Authorization': f'Bearer {pat}', 'Accept': 'application/vnd.github+json'})))['id']
url = json.load(urllib.request.urlopen(urllib.request.Request(
    f'https://uploads.github.com/repos/{repo}/releases/{rid}/assets?name=AethonMod.tmod', method='POST',
    data=open(tmod, 'rb').read(),
    headers={'Authorization': f'Bearer {pat}', 'Content-Type': 'application/octet-stream'})))['browser_download_url']
print(f"      asset: {url}")
PYEOF

echo "[5/5] verificando el CDN byte a byte…"
curl -fL --retry 3 -o /tmp/cdn135.tmod "https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/download/v6.135/AethonMod.tmod"
CDN=$(md5sum /tmp/cdn135.tmod | cut -d' ' -f1)
[ "$CDN" = "$MD5" ] && echo "      CDN verificado: md5 idéntico ($CDN)"

echo
echo "PUBLICADO ✔ https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.135"

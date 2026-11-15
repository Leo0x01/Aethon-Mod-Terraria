#!/usr/bin/env bash
# ==================================================================
#  publicar_v6139.sh — el publicador de la casa (v6.139)
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

TAG="v6.139"
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
# v6.139 — LA SESIÓN DE LAS NUEVE CURAS

## 1 · El error del log (curado)
El muñeco de pruebas pintaba su bruma con el lote de sprites CERRADO (`Draw was called, but Begin has not yet been called`). Ahora abre su propio lote de masa y devuelve el lote de vanilla intacto.

## 2 · El Festín del Grimorio (curado)
**La primera muerte de los jefes NO se devoraba**: una regla de la v6.97 exigía que el jefe estuviera en la lista del Despertar actual, pero esa lista solo tiene jefes en ciertos niveles — con el Despertar a medio camino, NINGUNA primera cena arrancaba jamás. Ahora: **la primera muerte de cada jefe es del LIBRO, siempre**; las repeticiones siguen la lista del Despertar.

## 3 · El altar en el mapa + los inactivos devorados
- El minimapa ahora distingue: **«(ACTIVO)»** sobre el altar que sirve, **«(devorado por el grimorio)»** sobre los muertos.
- Los altares inactivos son **DEVORADOS por el grimorio** (sin botín — lo devorado se digiere, no se recoge).

## 4 · Los textos recortados
Altar · Prisma · La Forma Eterna: versiones concisas en los 3 idiomas — la misma info, la mitad de texto, nada desborda.

## 5 · Las alas de escritura
- **Valquiria Lisa**: el cuerpo murió — LAS RUNAS SON LAS ALAS.
- **NUEVAS: Alas Rúnicas** (en la bolsa de alas): el ala hecha de ESCRITURA — el borde maestro, la vela y las puntas dibujados en glifos, con la runa MAYOR coronando cada punta.

## 6 · Auditoría de daño (contra el decompile real)
Todos los sistemas verificados: aura del vacío (1→10+bono por cercanía, 2→6 golpes/s) · quemadura (1% vida máx/medio s, 5 s de persistencia) · +10% ataque/+10 defensa del Vacío Exterior · los 7 stats de la Forma Eterna · los multiplicadores del grimorio (con topes) · el +0,1% por esencia (≤+100%, ligado al libro). **TODO SANO** — sin desviaciones.

## 7 · El pergamino del altar (propuesta)
Mockup fullHD incluido (`pergamino_altar_mockup_fullhd.png`): toda la info del altar en un pergamino del grimorio — el panel queda mínimo y el libro cuenta lo demás. Si te gusta, la próxima versión lo implementa como ventana real.

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo verify.csproj | 0 errores / 0 warnings |
| -build REAL | 0/0 — 524 entradas |
| GUARDIÁN de texturas | TODO VIVE ✔ |
| AUDIT de la sesión | 21/21 ✔ |
| Servidor headless | 0 excepciones — v6.139 |

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

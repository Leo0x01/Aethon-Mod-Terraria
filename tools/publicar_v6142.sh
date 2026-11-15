#!/usr/bin/env bash
# ==================================================================
#  publicar_v6139.sh — el publicador de la casa (v6.142)
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

TAG="v6.142"
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
# v6.142 — LA FORMA EN SU DESPERTAR Y EL RELOJ DE VERDAD

## 1 · La Forma Eterna, en el despertar
La escalera del despertar sube a **120 niveles**. Del **101 al 112**, cada peldaño construye **UNA PARTE** de las 12: la Escritura · los Tres Anillos · la Primera Luz · la Brasa · los Tres Soles · las Ofanim · el Manto · **el Aura del Vacío**.

- **Coste por peldaño**: 10 de platino + 1000 esencias del Eterno, **+10% por nivel** — se paga en **LA MESA DEL ALTAR** (esencias devoradas + monedas del banco; nada de Devorar arriba del 100).
- **Cada nivel**: **+4% de ataque a TODO el grimorio** (mágico e invocación) y **SU PIEZA VISUAL puesta en el portador SOLO mientras usa el libro** — no es un ítem, es un miembro del portador.

## 2 · LA CONVERSIÓN (nivel 112)
Al llegar la duodécima parte: el ítem cosmético **LA FORMA ETERNA** nace, **LA MANIFESTACIÓN** estalla (todo lo vivo en pantalla muere con su loot — muerte válida) y **LOS VISUALES DEL PORTADOR DESAPARECEN**: la forma se convirtió en el ítem. Quien quiera la forma, que se ponga el cosmético.

## 3 · El poder extra (113-120)
Del 113 en adelante el coste sube **20% por nivel** (34P → 123P) y el ataque de TODO el grimorio sube **+10% por nivel**. Bono de despertar acumulado a nivel 120: **+228%**.

## 4 · El reloj de verdad
El Eterno corre **LA MÁQUINA DEL GRIMORIO HAMBRIENTO NERVIOSO a velocidad NORMAL** — su reloj de **UNA HORA** (el minuto del Nervioso de prueba era la demo acelerada): el ojo rojo (rampa 20%→80%), la inquietud (50%+), la caza propia desde el 25% (el espíritu y su sombra devoran solos si te ausentas) y el festín por rareza (0,1%→5%; los jefes, 10% entero). LA OLEADA: **90% posible a los 54 min, 100% SEGURA a la hora exacta** — estalla cuando el ojo ya llegó rojo del todo. Una sola verdad: la del arma.

## 5 · La descripción del cosmético, de verdad
El tooltip de La Forma Eterna ahora cuenta **LO QUE ES** (doce partes construidas peldaño a peldaño, cada una creció puesta en el portador como un miembro más; al llegar la duodécima, la Manifestación estalló y la forma se convirtió en este ítem) y **QUÉ SIGNIFICA USARLO** (vestir al dios entero — aura, anillos, soles y ruedas girando alrededor tuyo; el poder quedó horneado en el grimorio: +4% por parte y +10% por peldaño del poder extra, para TODO su ataque, mágico e invocación) — en los 3 idiomas.

## 6 · El libro actualizado
La escalera 1-120, la forma en su sección, el hambre con su máquina de verdad y el aura de hambre borrada de la lista de auras — en los 3 idiomas.

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo verify.csproj | 0 errores / 0 warnings |
| -build REAL | 0/0 — 524 entradas |
| GUARDIÁN de texturas | TODO VIVE ✔ |
| AUDIT de la sesión | 40/40 ✔ |
| Servidor headless | 0 excepciones — v6.142 (tras el fix hjson) |

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

#!/usr/bin/env bash
# ==================================================================
#  publicar_v6152.sh — el publicador de la casa (v6.152)
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

TAG="v6.152"
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
# v6.152 — LA SESIÓN DEL GESTO DE DOS SEGUNDOS Y EL CRONISTA

## Los doce puntos de la letra

1. **EL GESTO NUEVO DE LA IRA**: **mantener el click derecho 2 segundos** — y solo eso. El izq+der murió (cada click derecho del gesto viejo disparaba la invocación del grimorio en plena ceremonia — el origen del «los minions desaparecen»). Mientras cargas el hold, el grimorio queda callado (el escuadrón INTACTO) y el buff muestra EL ANILLO que se cierra (sabes cuándo dispara).
2. **EL LÍMITE 10 = PROYECTILES BÁSICOS**: máximo 10 discos del Equinoccio vivos a la vez (la penetración v6.151 murió — era CUÁNTOS, no CUÁNTO).
3. **EL AUDIO YA NO SATURA**: `MaxInstances` en toda la cadena del básico (el uso, los zaps de los ojos, los estallidos por golpe — el mezclador de FNA se llenaba hasta saturar TODO el audio).
4. **EL JUICIO DE LOS DOS SOLES SIN SALTOS**: LA CURVA MAESTRA — todo el dibujado lee UNA función continua del reloj total. Certificado matemáticamente: las 4 costuras Δ≤0.015, el pliegue de la sentencia RESPIRA (14 t suaves), las estelas deterministas.
5. **EL AGUJERO RÚNICO PURO — SUPER CARGADO**: 4 capas nuevas (torbellino de 24 glifos · corona de 12 lentejuelas · 6 filamentos de acreción · eco doble) — sigue siendo un agujero rúnico, pero comiendo con cuchara.
6. **LOS ANILLOS DE LA CONSTRUCCIÓN = LOS DEL ACCESORIO**: la triple corona de conjuro + el anillo de fotones (EXACTAMENTE la del accesorio), con las runas escribiéndose 113-119.
7. **«PUERTA» → «DESPERTAR»** en todos los textos que significan despertar.
8. **EL PILAR — EL OBELISCO DEL UMBRAL**: borrado y rehecho con la sforja (tres cuerpos fundidos, runas en descenso, EL ARO abierto, pedestal de hueso) — **SIN ojo** (el PilarOjoMinion murió) y **SIN cortes** (certificado por análisis).
9. **EL LIBRO DE INFORMACIÓN**: la X dibujada EXACTA en su hitbox · el cursor ENCIMA del libro · **EL LORE PROGRESIVO** (cada sección se desbloquea con el Despertar: 10/25/40/60/80) · las dagas fuera del libro.
10. **LA SOMBRA DE LA PÁGINA ORIGINAL**: tentáculo NEGRO completo (sin ojos, sin colmillos, sin las líneas rojas).
11. **EL ESQUELETRON DEL INTERIOR**: la noche prestada UNIVERSAL — el modo guardián de mazmorra es IMPOSIBLE dentro (el normal aparece cuando se le llama) y el de oleada ahora se llama **«Esqueletron Mazmorra»**.
12. **EL CRONISTA — EL COMERCIANTE DEL GRIMORIO** (nuevo): el escriba que sobrevivió a la primer devoración. **Aparece cuando tienes el grimorio, se va cuando lo pierdes.** Vende lo que el libro come (esencias, páginas en blanco, pociones).

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo | 0/0 (471 .cs — ElCronista nuevo) |
| Build real | 0/0 · 532 entradas |
| GUARDIÁN | TODO VIVE ✔ |
| AUDIT v6.152 | LIMPIA ✔ (+ no-regresiones v6.151) |
| Headless | 0 excepciones |
| SIMS | pilar/cronista medidos · la curva del Juicio certificada (4 costuras Δ≤0.015) |

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
    body = json.dumps({'tag_name': tag,
                       'name': f'{tag} — EL GESTO DE DOS SEGUNDOS Y EL CRONISTA',
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
curl -sL -o /tmp/cdn152.tmod "https://github.com/$REPO/releases/download/$TAG/AethonMod.tmod"
CDN=$(md5sum /tmp/cdn152.tmod | cut -d' ' -f1)
[ "$CDN" = "$MD5" ] && echo "      CDN verificado: md5 idéntico ($CDN)" || { echo "      CDN DIFIERE: $CDN vs $MD5"; exit 1; }

echo "[5/5] ✔ PUBLICADO $TAG"

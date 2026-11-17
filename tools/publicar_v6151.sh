#!/usr/bin/env bash
# ==================================================================
#  publicar_v6151.sh — el publicador de la casa (v6.151)
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

TAG="v6.151"
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
# v6.151 — LA SESIÓN DEL BRILLO CUADRADO Y LA CAZA DEL SOL

## Los doce puntos de la letra

1. **GLOWUILIB** — LA LIBRERÍA nueva de la luz de la interfaz: `MarcoLuz` (el marco cuadrado que SOBRESALE del icono, degradado monotónico) · `Iluminar` · `Opacar` · `Desaturar` — para iluminar, opacar o desaturar CUALQUIER parte de la UI de Terraria.
2. **EL BRILLO CUADRADO DEL BUFF DE LA IRA**: el círculo plano murió — el marco sobresale 1-5 px del icono (4 al estar LISTA, creciendo durante la carga) y **el contador mm:ss murió** (el brillo ES el reloj). SIM 1:1 medido: 2-4 px de salida, degradado monotónico, encendido gris→color de abajo hacia arriba.
3. **LA IRA NO BORRA LOS MINIONS**: el gesto izq+der es SOLO de la ceremonia — el Shoot del grimorio se calla ~0,1 s al activarla (los ojos son los testigos del ritual, no su precio).
4. **EL ATAQUE BÁSICO DEL ETERNO: PERFORACIÓN 10** (antes infinita).
5. **EL JUICIO DE LOS DOS SOLES**: fusión COMPLETAMENTE FLUIDA (la curva única `RadioFusion`: el gigante nace exacto del tamaño del par en el contacto — el «salto» era un corte de fase + jitter) + **LA CAZA (5 s)**: el sol fusionado PERSIGUE enemigos, se mantiene, CRECE +70 %, ENROJECE por completo y SOLO ENTONCES explota — su daño escala ×1→×1.85 y su hitbox 30→300 px durante la caza.
6. **LOS TICKS DE LA IRA = EL RITMO DE LOS AGUJEROS RÚNICOS** (66 % más rápido; el cálculo de daño intacto).
7. **LA FORMA ETERNA**: la escalera 101-120 EXACTA (112 ya NO dispara la 12ª; 113-119 encienden los anillos; 120 la 12ª + conversión) y el vestido YA NO se apaga por una escupida ajena — cada grimorio construye SU forma (el personaje de prueba vuelve a VERLA).
8. **LA EXPLOSIÓN DE LA FORMA ETERNA ES LUZ PURA**: el estallido de la MASCOTA DE LUZ a escala dios (rayos deterministas + cruz anamórfica + núcleo + estrella + onda + bokeh — 100 % código, cero texturas, cero rayas).
9. **EL PILAR DEL ETERNO**: el marco de 86 px de una pieza con aire sobre el capitel (sin corte superior) + el aura violeta que lo abraza + el ojo-minion sobre el prisma.
10. **EL FONDO DEL MUNDO INTERIOR ES LA SOMBRA DE LA PÁGINA**: tinta profunda, tiras más anchas, motas de tinta que caen — el polvo estelar murió (no es un cielo: es el cuerpo del dios). Sin ojos ni colmillos.
11. **EL LIBRO DE INFORMACIÓN**: margen superior en el scroll · ENCIMA de toda la interfaz · la X en su esquina con hitbox exacta · el cuerpo con BORDE BLANCO y CENTRO NEGRO (títulos intactos).
12. **«ESE PODER EN EL PORTADOR»** ×3 idiomas · **EL % DE HAMBRE ES LA PROBABILIDAD** (≤89 % → 1 % · 90 % → 5 % · +5 % por punto · 100 % → activación directa) · **EL ESQUELETRON YA NO ES GUARDIÁN DE MAZMORRA** en el interior (el mediodía eterno respeta la noche prestada del jefe de oleada) · **LA SOMBRA DE LA PÁGINA DEL GRIMORIO LISA** (sin colmillos ni ojitos).

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo | 0/0 (470 .cs) |
| Build real | 0/0 · 529 entradas · 19.398.750 B |
| GUARDIÁN | TODO VIVE ✔ |
| AUDIT v6.151 | LIMPIA ✔ (firmas + no-regresiones) |
| Headless | 0 excepciones |
| SIM 1:1 brillo | TODO MEDIDO Y VERDE ✔ |

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
                       'name': f'{tag} — EL BRILLO CUADRADO Y LA CAZA DEL SOL',
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
curl -sL -o /tmp/cdn151.tmod "https://github.com/$REPO/releases/download/$TAG/AethonMod.tmod"
CDN=$(md5sum /tmp/cdn151.tmod | cut -d' ' -f1)
[ "$CDN" = "$MD5" ] && echo "      CDN verificado: md5 idéntico ($CDN)" || { echo "      CDN DIFIERE: $CDN vs $MD5"; exit 1; }

echo "[5/5] ✔ PUBLICADO $TAG"

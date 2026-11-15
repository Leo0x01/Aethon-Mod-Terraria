#!/usr/bin/env bash
# ==================================================================
#  publicar_v6139.sh — el publicador de la casa (v6.143)
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

TAG="v6.143"
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
# v6.143 — EL ACCESORIO DE VERDAD

## 1 · La verificación
La Forma Eterna que nace en el peldaño 112 del despertar **ES el accesorio**, con el **efecto del Aura del Vacío** completo y verificado:

- **EL CAMPO**: el ancla `AuraJuicio2Ancla` — **19 tiles** alrededor del portador (reconocida por `LlevaElAura` desde la v6.115).
- **LA QUEMADURA** (`QuemaduraVacioExterior`): los impíos que entran arden **×1→×3 por cercanía** y caminan a **×0,55** — hasta los **fantasmas**. Al salir del campo siguen ardiendo: **1% de vida máxima por medio segundo durante 5 s**.
- **LOS BUFFS/DEBUFFS**: mientras un enemigo arde, el portador cobra **VACÍO EXTERIOR** (+10% ataque, +10 defensa, −15% coste de maná). **Siempre**: el buff **AURA DEL VACÍO** y los **stats del panteón** (+40 maná · +8% daño mágico · +4% crít. mágico · +2 regen · +8% melée · +3% crít. melée · +3 defensa).

## 2 · El texto curado
El tooltip de la v6.142 decía «el poder de la forma ya no está aquí» — **falso**: el aura ES un efecto activo del accesorio. Reescrito en los 3 idiomas: **LO QUE ES** y **QUÉ SIGNIFICA USARLO** (llevarla puesta enciende el aura con su quemadura y sus regalos — lector extendido a Linea13).

## 3 · Los textos viejos cazados
El bloque `FormaEterna` aún cantaba el pago con SHIFT de la v6.141 (muerto) — ahora dice la verdad: **LA MESA DEL ALTAR**. `CaminoManifestacion` decía «ítem cosmético» — ahora dice **EL ACCESORIO (el aura del vacío, con su quemadura y sus buffs)**.

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo verify.csproj | 0 errores / 0 warnings |
| -build REAL | 0/0 — 524 entradas |
| GUARDIÁN de texturas | TODO VIVE ✔ |
| AUDIT de la sesión | 39/39 ✔ (con checks nuevos del accesorio) |
| Servidor headless | 0 excepciones — v6.143 |

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

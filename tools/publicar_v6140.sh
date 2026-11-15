#!/usr/bin/env bash
# ==================================================================
#  publicar_v6139.sh — el publicador de la casa (v6.140)
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

TAG="v6.140"
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
# v6.140 — LA SESIÓN DEL PERGAMINO Y LAS SEIS CURAS

## 1 · El hambre al 90%
La furia estallaba al **40%** de hambre (el "texto al 4%" que veías era el primer susurro). Ahora: **las oleadas solo aparecen al 90%** y al **100% siempre, con el llamado del grimorio**. El susurro sigue desde el principio (informativo).

## 2 · El aura rara — explicada
Era **el aura de hambre** del portador, prendiendo con solo el 10% de hambre. Ahora solo aparece al 90%: si ves la ceniza, la oleada ya está prometida.

## 3 · La llegada ya no nace dentro del mundo interior
Los tres caminos del cometa (primera noche · primer vecino · Tinta) mueren dentro del cuerpo del dios.

## 4 · Los jefes de la arena: regeneración como debe ser
La vida **ya no se restaura sola cada medio segundo** (ese era el bug de la regeneración constante). La regeneración ahora es la que pediste: **desde el nivel 10, 1% de vida máxima por segundo +0,1% por nivel extra — sin tope de nivel**.

## 5 · El altar EN el minimapa + el fantasma muerto
- El altar activo **aparece en el minimapa** al registrarse (la cadena del mapa verificada en el decompile).
- Los altares extra devueltos por el grimorio **no sueltan su ítem** (devorado = digerido).
- El grimorio fantasma solapando el altar murió: **nunca dos libros dormidos en el mundo**.

## 6 · Las Alas Rúnicas v2
**Copia exacta de la Valquiria Lisa** — mismo diseño de runas, con solo dos cambios: las runas ciclan **dorado→morado lento (~37 s)** recorriendo el borde, y su ángulo es **fijo tangente a la pluma, extendiéndose hacia afuera** (nada de girar en círculo).

## 7 · La Forma Eterna 100→1000
La escalera del despertar sube hasta **MIL** (+1% daño/nivel). Cada 100 niveles desde el 200, una **PIEZA del panteón** (Brasa · Anillo · Sello · Anillos del Sol · Brillo · Ascendida · Ofanim · Aura del Vacío al 900). **Al 1000: LA MANIFESTACIÓN** — el estallido de luz que elimina de un golpe TODO lo vivo en la pantalla, jefes incluidos, con su loot (muerte válida). La Ira sigue en el 100.

## 8 · EL PERGAMINO DEL GRIMORIO (in-game)
La ventana-pergamino con TODA la información: altar · mundo interior · oleadas · grimorio y sus mecánicas · la escalera completa del despertar · los ataques y minions · la Forma Eterna · las auras y buffs/debuffs. **El ojo del grimorio sigue tu cursor en todo momento** (y parpadea). Ábrelo con la **tecla P** junto al altar; rueda del ratón para desplazarte.

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo verify.csproj | 0 errores / 0 warnings |
| -build REAL | 0/0 — 524 entradas |
| GUARDIÁN de texturas | TODO VIVE ✔ |
| AUDIT de la sesión | 36/36 ✔ |
| Servidor headless | 0 excepciones — v6.140 |

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

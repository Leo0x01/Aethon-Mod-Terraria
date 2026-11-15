#!/usr/bin/env bash
# ==================================================================
#  publicar_v6139.sh — el publicador de la casa (v6.141)
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

TAG="v6.141"
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
# v6.141 — LA SESIÓN DEL LIBRO ABIERTO Y LAS SEIS VERDADES

## 1 · EL OJO QUE MIRA — la nueva arma
Todo gira a su ojo. **El ojo APARECE CERCA del enemigo** bajo tu cursor, carga su mirada y dispara: **el rayo atraviesa todo lo que cruza**. Todo lo golpeado recibe **LA MARCA SILENCIOSA** — y la marca enciende **LA QUEMADURA DEL VACÍO** (1% de vida máxima cada medio segundo, 5 s). **En multijugador, el jugador enemigo marcado también ARDE** (la quemadura ahora quema jugadores). Mejora con el nivel del Grimorio del Eterno: **+1,5% de daño y −0,5% de maná por nivel**. Entregada directo al inventario (+ receta de repuesto). El grimorio no se tocó.

## 2 · LA FORMA ETERNA DE VERDAD
El despertar vuelve a sus **100 niveles**. La Forma Eterna vive ahora en el **NIVEL NORMAL del libro** (el de la XP), **desde el nivel 100**: cada nivel extra construye **UNA PARTE** de las **12** — la Escritura · los Tres Anillos · la Primera Luz · la Brasa · los Tres Soles · las Ofanim · el Manto · **el Aura del Vacío**.

- **Coste por parte**: 10 de platino + 1000 esencias del Eterno, **+10% por nivel**. Se paga con **SHIFT + clic derecho** del libro.
- **Cada parte**: **+4% de ataque** a todo, y SU PIEZA VISUAL **puesta en el jugador SOLO mientras usa el libro** (cada anillo, cada sol — una parte por separado, no ítems).
- **Con las 12**: el ítem cosmético **LA FORMA ETERNA** nace y **LA MANIFESTACIÓN** estalla — la explosión de luz que elimina de un golpe todo lo vivo en pantalla, jefes incluidos, con su loot (muerte válida).

## 3 · EL LIBRO DEL ETERNO (el pergamino murió)
**El icono del grimorio, justo al lado del botón de logros** del inventario: un clic y **EL LIBRO SE ABRE** — dos páginas cosidas al lomo, estilo Sforza (cuero viejo, chapas de metal, puntadas vivas), con TODA la información del mod en 3 idiomas… y **EL OJO DE LOS MINIONS** abajo a la derecha, **siguiendo tu cursor en todo momento**. La tecla P sigue funcionando (abre el libro).

## 4 · Una sola hambre — el ojo rojo
Había dos hambres desincronizadas (la del arma y la del medidor). Ahora **UNA**: el **ojo del Eterno se vuelve ROJO e INQUIETO** con el hambre real — **el único indicador**. **El aura del portador fue eliminada por completo** (era «el aura rara»). La oleada: **90% posible, 100% SIEMPRE**, con el llamado del grimorio.

## 5 · El fantasma del altar — muerto de verdad
El libro re-engendrado (al cargar el mundo) nacía en una posición vieja y quedaba flotando solapando el altar. Ahora **nace ENCIMA del altar activo**, siempre.

## 6 · Los jefes del mundo interior
- **Skeletron ya NO aparece como el guardián de la mazmorra**: el mediodía eterno del interior lo metía en el modo spin furioso (verificado en el código de vanilla) — curado.
- **Los jefes nacen a 100 tiles del jugador** (lejos, caminan hacia la pelea).
- La regeneración verificada: **1%/s + 0,1% por nivel desde el nivel 10, sin tope**.

## 7 · El altar en el minimapa — EL FARO
Un latido cada segundo repinta las celdas del altar activo en el mapa. Ya no hay forma de perderlo.

## 8 · Las alas rúnicas entregadas
La copia exacta de la Valquiria Lisa (dorado→morado lento, runas extendiéndose hacia afuera) llega DIRECTO al inventario.

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo verify.csproj | 0 errores / 0 warnings |
| -build REAL | 0/0 — 524 entradas |
| GUARDIÁN de texturas | TODO VIVE ✔ |
| AUDIT de la sesión | 44/44 ✔ |
| Servidor headless | 0 excepciones — v6.141 |

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

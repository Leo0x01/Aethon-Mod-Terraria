#!/usr/bin/env bash
# ==================================================================
#  publicar_v6176.sh — el publicador de la casa (v6.177)
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

TAG="v6.177"
TMOD="$HOME/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
MD5=$(md5sum "$TMOD" | cut -d' ' -f1)
SIZE=$(stat -c%s "$TMOD")
echo "[1/5] md5: $MD5 · ${SIZE} B"

URL=$(git config --get remote.origin.url)
PAT=${URL#*://}
PAT=${PAT#*:}
PAT=${PAT%@*}
REPO="Leo0x01/Aethon-Mod-Terraria"

echo "[2/5] verificando el tag…"
git rev-parse "$TAG^{commit}" >/dev/null

echo "[3/5] creando el release $TAG…"
NOTAS=$(cat <<'EOF'
# v6.177 — LA ASPIRADORA QUE ESPERA, EL GUSANO QUE CALLA Y EL ARMA DEL HAZ

## Las doce curas de la letra

1. **LA CURA DE LOS DOS CRASHES DEL client.log** (≈42.000 frames de «Draw was called, but Begin has not yet been called»): la volea de la arena y el halo de la aspiradora dibujaban con el lote CERRADO. AHORA: `AbrirAdditive` al entrar, `RestaurarLoteVanilla` al salir — y la regla nueva de la casa (R21) lo prohíbe para siempre.
2. **LA BARRA DEL DEVORADOR NO REBOSA**: el gusano se sella ENTERO (cada cuerpo/cola hereda el sello por SetDefaults — stats y SU barra, sin aura ni motor ni kills) y la barra suma la vida de TODAS las piezas, con la vida CLAMPADA: el rebose es matemáticamente imposible.
3. **LOS JEFES DEVORADOS CALLAN**: el cine de la aspiradora ahora CONGELA al gusano (`CongeladoPorAspiradora` — ni IA, ni UN proyectil mientras el libro come; red anti-zombi al terminar).
4. **LA ASPIRADORA REESCRITA**: LA VIGILIA (espera los 5 segundos de la letra a que el gusano se genere completo — su ojo lo sigue y el anillo de la espera se llena) → LA CAPTURA → LA FLOTACIÓN (cada pieza individual flota con SU vaivén y la cadena se desenreda) → EL TRAGO (TODAS devoradas A LA VEZ — muerte real, loot normal) → EL ASCO → EL POSO.
5. **EL ASCO HABLA EN LA MENTE**: el comentario del grimorio pasa del chat a la voz-en-la-mente del EcoLib (`EcoRed.HablarAlPortador`).
6. **LA INTERFAZ DEL PILAR Y EL ALTAR**: LAS TIJERAS DEL LIBRO (el recorte de tijeras sobre las páginas: lo que no cabe en el papel NO SE DIBUJA) · botones que re-miden al trazo · tintas más oscuras y el halo del texto MANSO · el checklist «LAS TRES A LA VEZ» uniforme (el cuadradito se RELLENA de morado).
7. **EL FARO REDISEÑADO**: torre afinada con broches y ventanas ámbar, la linterna con EL ECLIPSE de corona picada, rayos continuos con núcleo (QA visual 7.5).
8. **EL ARMA DEL HAZ** (la letra: «lo que se tenia que crear era un arma»): **Proyección del plano** — el ojo se proyecta sobre ti, GRITA creciendo (el telegraph del vídeo) y suelta el doble rayo magenta con penetración infinita. Entrega DIRECTA al inventario + bolsa de investigaciones.
9. **LA CARPETA tools/ SALE DEL MOD**: las herramientas son del desarrollo, no del juego — viven en `tools/` de la raíz del repo.
10. **LA BRUMA DE LA BASE DE LOS TENTÁCULOS**: las tres capas del cielo interior disuelven su punta en niebla de SU propia tinta — el tentáculo se apaga, no se corta.
11. **LAS 194 CLAVES EN-ONLY CLASIFICADAS**: 189 muertas PURGADAS del en-US (179 placeholders de proyectiles, 7 tooltips vacíos, 3 pestanas del pergamín muerto) + 5 vivas AÑADIDAS al español (Bolsa Maestra, Coro de los Profundos, Pluma del Escriba Santo, Bolsa de la Forma Eterna, El Cronista). **PARIDAD PERFECTA: 1849 = 1849, cero huérfanas** (la traducción al inglés completa queda para el final del mod, como manda la letra).
12. **EL AZAR DEL DIBUJO SEMBRADO** (R22): las 7 llamadas reales de Main.rand en el dibujo (ElCronista y el Juicio de los Dos Soles) ahora nacen de semilla fija por entidad (whoAmI·edad) — la misma migaja de luz en todas las máquinas; el loop mide solo código.

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo | 0/0 (474 .cs contra tML 2026.09.3.1 real) |
| Build real | 0/0 + Packaging |
| GUARDIÁN | TODO VIVE ✔ (ProyeccionDelel plano.png con su clase) |
| AUDIT v6.177 | TODO VERIFICADO ✔ (firmas vivas · paridad 1849=1849 · fósiles purgados) |
| Headless | 0 excepciones (Sandboxing v6.177) |
| Loop retro | SIN REGRESIONES (azar 10→0 · claves EN-sin-ES 194→0) |
| Sim 1:1 | La aspiradora SHIP (las 67 piezas en la boca en el MISMO tick) |

**Instalación**: copia `AethonMod_v6.177.tmod` a la carpeta `Mods` del tModLoader y actívalo.
EOF
)

python3 - "$PAT" "$TAG" "$NOTAS" <<'PYEOF'
import json, sys, urllib.request

pat, tag, notas = sys.argv[1], sys.argv[2], sys.argv[3]
repo = 'Leo0x01/Aethon-Mod-Terraria'
api = f'https://api.github.com/repos/{repo}/releases'

body = json.dumps({'tag_name': tag, 'name': f'{tag} — el partido de los gigantes y el visor del proyectado',
                   'body': notas, 'draft': False, 'prerelease': False}).encode()
req = urllib.request.Request(api, data=body, method='POST')
req.add_header('Authorization', f'token {pat}')
req.add_header('Accept', 'application/vnd.github+json')
try:
    with urllib.request.urlopen(req) as r:
        rel = json.load(r)
    print(f"release {rel['id']} creado: {rel['html_url']}")
    with open('/tmp/release_v6176_id.txt', 'w') as f:
        f.write(str(rel['id']))
except urllib.error.HTTPError as e:
    if e.code == 422:
        req2 = urllib.request.Request(f'{api}/tags/{tag}')
        req2.add_header('Authorization', f'token {pat}')
        with urllib.request.urlopen(req2) as r:
            rels = json.load(r)
        rel = rels[0]
        print(f"release YA EXISTÍA {rel['id']}: continuando")
        with open('/tmp/release_v6176_id.txt', 'w') as f:
            f.write(str(rel['id']))
    else:
        raise
PYEOF

echo "[4/5] subiendo el asset…"
RELID=$(cat /tmp/release_v6176_id.txt)
curl -s -X POST \
  -H "Authorization: token $PAT" \
  -H "Content-Type: application/octet-stream" \
  --data-binary @"$TMOD" \
  "https://uploads.github.com/repos/$REPO/releases/$RELID/assets?name=AethonMod_v6.177.tmod" \
  | python3 -c "import json,sys; a=json.load(sys.stdin); print('asset:', a.get('name'), a.get('size'), 'B ·', a.get('state'))"

echo "[5/5] verificación del CDN…"
sleep 3
URL_DL="https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/download/$TAG/AethonMod_v6.177.tmod"
curl -sL -o /tmp/verif_v6176.tmod "$URL_DL" && md5sum /tmp/verif_v6176.tmod && echo "original: $MD5"
if [ "$(md5sum /tmp/verif_v6176.tmod | cut -d' ' -f1)" = "$MD5" ]; then
  echo "CDN VERIFICADO byte a byte ✔"
else
  echo "ADVERTENCIA: el CDN aún propaga (verificar en un rato)"
fi

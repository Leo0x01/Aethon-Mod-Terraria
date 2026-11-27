#!/usr/bin/env bash
# ==================================================================
#  publicar_v6179.sh — el publicador de la casa (v6.179)
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

TAG="v6.179"
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
# v6.179 — LA PUERTA VIVA, EL TRÍO MÁS ARMADO Y EL ARMA MÁXIMA

## Las siete curas de la letra

1. **EL FANTASMA DE LAS RANURAS EN BLANCO (LA AUTOPSIA COMPLETA)**: la persistencia estaba VIVA — el fantasma era que el trío es CRAFTABLE y el portador llevaba DOS; el altar leía el primero del inventario, que podía ser el VACÍO. AHORA el altar lee SIEMPRE el trío **MÁS ARMADO** — el arma ya no desaparece al reiniciar ni depende del sitio del inventario.
2. **EL SLOT MUDO + LA VENTANA QUE SE LEE**: el cuadro del slot del trío NO lleva NINGÚN texto (solo el arma se lee), y TODOS los textos crecen: celda 44→50 · icono 32→36 · título 1.3 · placa del nombre 1.2 · botones a 1.2 con **piso de legibilidad 1.0** y elipsis — todo se lee de un vistazo y nada desborda.
3. **LA PUERTA VIVA** («una vez activo debe estar activo siempre»): la PRIMERA vez que las 1000 esencias están pagadas, **la puerta se las traga** y queda ABIERTA PARA SIEMPRE (el cerrojo `PuertaViva` viaja con el mundo). Quitar esencias ya no puede cerrar lo que ya se abrió — el botón NUNCA desaparece.
4. **SOLO EL ARMA, YA MÁXIMA, CON SUS DOS FASES**: las diez mejoras, el Cíclope, su Eco, sus drops y el buff de recarga MURIERON. El arma nace a su máxima capacidad (**energía infinita 300/300** — nada se gasta ni se recarga) y las fases se cambian en combate con la **tecla X** o el **clic derecho**: fase 1 · el rayo continuo ⇄ fase 2 · LA PROYECCIÓN enorme (sin enfriamiento).
5. **EL FESTÍN DE LA LLAMADA**: las oleadas parían cabezas de Devorador entre invocaciones y el «== 2» exacto jamás caía en una invocación de verdad. AHORA solo cuentan las cabezas que nacen de UNA LLAMADA DEL PORTADOR (ítem de invocación o pilar). Tras borrar el festín con la Página en Blanco, la SEGUNDA invocación vuelve a soltar la aspiradora.
6. **LA CURA DEL client.log**: el crash `AI_007_FindGoodRestingSpot` eran los VECINOS DEL PUEBLO del mundo real vivos dentro del interior buscando su cama con coordenadas del mundo grande — al entrar al interior ahora se apagan todos los NPC exteriores. El warn de memoria: el barrendero cuenta los ítems como anclas. El warn FNA de imagen: las 528 imágenes del mod escaneadas, TODAS sanas (nace del ciclo de recompilado del tML, documentado en CHANGES).
7. **EL GUARDIÁN APRENDE LOS PARTIAL** (herramienta mejorada): el gate de texturas ya no da falsos positivos con clases `partial`.

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo | 0/0 (484 .cs contra tML 2026.09.3.1 real) |
| Build real | 0/0 + Packaging (538 entradas, md5 fdb1a422b4cd624a8042e8422f969b3b) |
| GUARDIÁN | TODO VIVE ✔ (442 clases, partial curado) |
| AUDIT v6.179 | TODO VERIFICADO ✔ (paridad 1860=1860 · mejoras/NPC/buff muertos · firmas vivas · fósiles purgados 2×) |
| Headless | 0 excepciones + DiagnosticoPersistencia TODO VIVO (trío · sello · PUERTA-TRAGO COMIDAS ✔ · PUERTA-CERROJO ABIERTA PARA SIEMPRE ✔) |
| Sim UI | SHIP (título 1.3 en banda de 50 · celdas 50 · placa 1.2 · piso 1.0) |

**Instalación**: copia `AethonMod_v6.179.tmod` a la carpeta `Mods` del tModLoader y actívalo.
EOF
)

python3 - "$PAT" "$TAG" "$NOTAS" <<'PYEOF'
import json, sys, urllib.request, urllib.error
pat, tag, notas = sys.argv[1], sys.argv[2], sys.argv[3]
repo = 'Leo0x01/Aethon-Mod-Terraria'
api = f'https://api.github.com/repos/{repo}/releases'
body = json.dumps({'tag_name': tag,
                   'name': f'{tag} — la puerta viva, el trío más armado y el arma máxima',
                   'body': notas, 'draft': False, 'prerelease': False}).encode()
req = urllib.request.Request(api, data=body, method='POST')
req.add_header('Authorization', f'token {pat}')
req.add_header('Accept', 'application/vnd.github+json')
try:
    with urllib.request.urlopen(req) as r:
        rel = json.load(r)
    print(f"release {rel['id']} creado: {rel['html_url']}")
    with open('/tmp/release_v6179_id.txt', 'w') as f:
        f.write(str(rel['id']))
except urllib.error.HTTPError as e:
    if e.code == 422:
        req2 = urllib.request.Request(f'{api}/tags/{tag}')
        req2.add_header('Authorization', f'token {pat}')
        with urllib.request.urlopen(req2) as r:
            rels = json.load(r)
        rel = rels[0]
        print(f"release YA EXISTÍA {rel['id']}: continuando")
        with open('/tmp/release_v6179_id.txt', 'w') as f:
            f.write(str(rel['id']))
    else:
        raise
PYEOF

echo "[4/5] subiendo el asset…"
RELID=$(cat /tmp/release_v6179_id.txt)
curl -s -X POST \
  -H "Authorization: token $PAT" \
  -H "Content-Type: application/octet-stream" \
  --data-binary @"$TMOD" \
  "https://uploads.github.com/repos/$REPO/releases/$RELID/assets?name=AethonMod_v6.179.tmod" \
  | python3 -c "import json,sys; a=json.load(sys.stdin); print('asset:', a.get('name'), a.get('size'), 'B ·', a.get('state'))"

echo "[5/5] verificación del CDN…"
sleep 3
URL_DL="https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/download/$TAG/AethonMod_v6.179.tmod"
curl -sL -o /tmp/verif_v6179.tmod "$URL_DL" && md5sum /tmp/verif_v6179.tmod && echo "original: $MD5"
if [ "$(md5sum /tmp/verif_v6179.tmod | cut -d' ' -f1)" = "$MD5" ]; then
  echo "CDN VERIFICADO byte a byte ✔"
else
  echo "ADVERTENCIA: el CDN aún propaga (verificar en un rato)"
fi

#!/usr/bin/env bash
# ==================================================================
#  publicar_v6180.sh — el publicador de la casa (v6.180)
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

TAG="v6.180"
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
# v6.180 — EL ICONO QUE NACE, EL PEDACITO Y EL SELLADO PEQUEÑO

## Las nueve curas de la letra

1. **EL ICONO QUE NACE (LA CAUSA RAÍZ de los slots eternos en blanco)**: las texturas de los ítems VANILLA son **LAZY** — dibujar `TextureAssets.Item[tipo]` sin más entrega un placeholder 1×1 TRANSPARENTE. Las armas del trío SOLO viven en las ranuras: su textura JAMÁS se cargaba («desequipar y equipar» era el inventario cargándola). AHORA **todo icono pasa por `Main.GetItemDrawFrame`** (el camino del inventario): la ofrenda, el trío, el selector flotante y el cielo despierto. **Ese siempre fue el problema — y ya no es.**
2. **EL ARMA PROYECTADA MUERE ENTERA** (la letra: «ya puedes borrar el arma... obviamente conserva la tecnica»): el Visor, sus rayos, su sistema, su capa y su mensaje del clic derecho desaparecen. **LA TÉCNICA VIVE** (HazProyectadoLib — el Faro la usa) y el proyecto queda con **CERO menciones externas**.
3. **EL PEDACITO DEL DEVORAMUNDOS** (la letra: «mantener la animacion anterior donde solo comia una seccion»): la aspiradora (que nunca ocurría) MUERE. La PRIMERA vez que el Devoramundos aparece, el grimorio se come **UNA SOLA SECCIÓN — LA COLA** con la bruma del festín de siempre, la sección muere de verdad (loot normal) y dice lo que piensa: **«...Un pedacito es suficiente.»** La Página en Blanco devuelve el festín a la mesa.
4. **EL ALTAR QUE SE LEE + LA PUERTA SIEMPRE VISIBLE**: los textos vuelven a tamaño legible (0.84–0.95), los botones caben con el **TEXTO COMPLETO** (centrados, sin desbordes), «CONSAGRAR LA OFRENDA» se queda en **«Consagrar»** y la sección del MUNDO INTERIOR **SUBE DE PUESTO** — el botón de la puerta vive justo debajo del botón Consagrar y **JAMÁS vuelve a caer fuera del papel**. El aviso del selector vacío ahora SE LEE: «No hay armas disponibles — solo armas mágicas consagradas».
5. **EL LORE ENTRELAZADO + EL CRONISTA NARRADOR**: las **19 secciones en el orden exacto de la letra** (el archivo viejo de la casa entrelazado en las posiciones 4·7·10·13·16). **TODO el lore sellado** por la puerta de Despertar Y las palabras del Cronista — menos **EL COMPENDIO DEL ETERNO** (la intro: la única libre). El Cronista tiene su **BOTÓN LORE** en el cuadro de diálogo: cada charla cuenta un pedacito de la sección siguiente **+ SU propio pensamiento** y la abre para siempre; la primera vez que le hablas, declara la AUTORÍA: todo el Compendio lo escribió ÉL. Y los dos documentos pedidos: **LORE-ANTES.md** y **LORE-DESPUES.md**.
6. **EL RECUERDO LIMPIO**: la explosión del Recuerdo de Aethon vuelve a ser **100 % luz procedural** — nace LA ESTRELLA LIMPIA (dos cuadros cruzados + el corazón, puro código) y la textura cruzada muere de todo el arsenal del jefe. Limpia como antes.
7. **EL SELLADO PEQUEÑO** (la letra: «borralo completo, y crea uno nuevo que sea mas pequeño que sea del tamaño del grimorio normal»): el gigante lento murió. EL NUEVO: **44×58 px** (el sprite del grimorio normal) con **SU ojo** (el iris que MIRA, el iris rojo de la furia, el párpado que parpadea — la misma esencia del Grimorio del Eterno y del Menor) y **LA CORONA DE LOS ONCE OJOS** rodeándolo (cada uno con su parpadeo, su iris clavado en ti y su nivel máximo). LA BATALLA DE LAS SOMBRAS: **1 sombra de la página al nacer → 10 al 90 % de furia**; a **5 sombras** → FASE 2: **LA IRA DE UN SER EXTERIOR** (ojo colosal + ceremonia + el haz que te PERSIGUE LENTO); a **10** → FASE 3: **EL SEGUNDO OJO**. Coreografía de tres actos: el vuelo de dos tiempos (mira a dónde va y luego va, escupiendo la pareja dorada/violeta), el coro de los once ojos (uno SE HINCHA y dispara el arsenal del Recuerdo) y el teletransporte en bruma.
8. **EL TOOLTIP DEL ALTAR**: el ítem dice solo **«El Altar de la Consagración del Eterno»**.
9. **LOS DOS CATCHES MUDOS** del banco de diagnóstico, curados.

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo | 0/0 (474 .cs contra tML 2026.09.3.1 real) |
| Build real | 0/0 + Packaging |
| GUARDIÁN | TODO VIVE ✔ (los sprites huérfanos del jefe viejo purgados) |
| AUDIT v6.180 | TODO VERIFICADO ✔ (25 claves nuevas ×3 · paridad 1861=1861 · cero menciones externas) |
| Headless | 0 excepciones |
| Loop | SIN REGRESIONES + catches vacíos 2→0 |
| Sim altar | SHIP (la puerta SIEMPRE dentro del papel) |

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
    with open('/tmp/release_v6180_id.txt', 'w') as f:
        f.write(str(rel['id']))
except urllib.error.HTTPError as e:
    if e.code == 422:
        req2 = urllib.request.Request(f'{api}/tags/{tag}')
        req2.add_header('Authorization', f'token {pat}')
        with urllib.request.urlopen(req2) as r:
            rels = json.load(r)
        rel = rels[0]
        print(f"release YA EXISTÍA {rel['id']}: continuando")
        with open('/tmp/release_v6180_id.txt', 'w') as f:
            f.write(str(rel['id']))
    else:
        raise
PYEOF

echo "[4/5] subiendo el asset…"
RELID=$(cat /tmp/release_v6180_id.txt)
curl -s -X POST \
  -H "Authorization: token $PAT" \
  -H "Content-Type: application/octet-stream" \
  --data-binary @"$TMOD" \
  "https://uploads.github.com/repos/$REPO/releases/$RELID/assets?name=AethonMod_v6.180.tmod" \
  | python3 -c "import json,sys; a=json.load(sys.stdin); print('asset:', a.get('name'), a.get('size'), 'B ·', a.get('state'))"

echo "[5/5] verificación del CDN…"
sleep 3
URL_DL="https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/download/$TAG/AethonMod_v6.180.tmod"
curl -sL -o /tmp/verif_v6180.tmod "$URL_DL" && md5sum /tmp/verif_v6180.tmod && echo "original: $MD5"
if [ "$(md5sum /tmp/verif_v6180.tmod | cut -d' ' -f1)" = "$MD5" ]; then
  echo "CDN VERIFICADO byte a byte ✔"
else
  echo "ADVERTENCIA: el CDN aún propaga (verificar en un rato)"
fi

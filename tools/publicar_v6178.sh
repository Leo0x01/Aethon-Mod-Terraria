#!/usr/bin/env bash
# ==================================================================
#  publicar_v6176.sh — el publicador de la casa (v6.177)
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

TAG="v6.178"
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
# v6.177 — LA ASPIRADORA QUE ESPERA, EL GUSANO QUE CALLA Y EL ARMA DEL CÍCLOPE

## Las doce curas de la letra

1. **LA CURA DE LOS DOS CRASHES DEL client.log** (≈42.000 frames de «Draw was called, but Begin has not yet been called»): la volea de la arena y el halo de la aspiradora dibujaban con el lote CERRADO. AHORA: `AbrirAdditive` al entrar, `RestaurarLoteVanilla` al salir — y la regla nueva de la casa (R21) lo prohíbe para siempre.
2. **LA BARRA DEL DEVORADOR NO REBOSA**: el gusano se sella ENTERO (cada cuerpo/cola hereda el sello por SetDefaults — stats y SU barra, sin aura ni motor ni kills) y la barra suma la vida de TODAS las piezas, con la vida CLAMPADA: el rebose es matemáticamente imposible.
3. **LOS JEFES DEVORADOS CALLAN**: el cine de la aspiradora ahora CONGELA al gusano (`CongeladoPorAspiradora` — ni IA, ni UN proyectil mientras el libro come; red anti-zombi al terminar).
4. **LA ASPIRADORA REESCRITA**: LA VIGILIA (espera los 5 segundos de la letra a que el gusano se genere completo — su ojo lo sigue y el anillo de la espera se llena) → LA CAPTURA → LA FLOTACIÓN (cada pieza individual flota con SU vaivén y la cadena se desenreda) → EL TRAGO (TODAS devoradas A LA VEZ — muerte real, loot normal) → EL ASCO → EL POSO.
5. **EL ASCO HABLA EN LA MENTE**: el comentario del grimorio pasa del chat a la voz-en-la-mente del EcoLib (`EcoRed.HablarAlPortador`).
6. **LA INTERFAZ DEL PILAR Y EL ALTAR**: LAS TIJERAS DEL LIBRO (el recorte de tijeras sobre las páginas: lo que no cabe en el papel NO SE DIBUJA) · botones que re-miden al trazo · tintas más oscuras y el halo del texto MANSO · el checklist «LAS TRES A LA VEZ» uniforme (el cuadradito se RELLENA de morado).
7. **EL FARO REDISEÑADO**: torre afinada con broches y ventanas ámbar, la linterna con EL ECLIPSE de corona picada, rayos continuos con núcleo (QA visual 7.5).
8. **EL ARMA DEL CÍCLOPE PROYECTADO** (la letra: «lo que se tenia que crear era un arma»): **Proyección del Cíclope** — el ojo se proyecta sobre ti, GRITA creciendo (el telegraph del vídeo) y suelta el doble rayo magenta con penetración infinita. Entrega DIRECTA al inventario + bolsa de investigaciones.
9. **LA CARPETA tools/ SALE DEL MOD**: las herramientas son del desarrollo, no del juego — viven en `tools/` de la raíz del repo.
10. **LA BRUMA DE LA BASE DE LOS TENTÁCULOS**: las tres capas del cielo interior disuelven su punta en niebla de SU propia tinta — el tentáculo se apaga, no se corta.
11. **LAS 194 CLAVES EN-ONLY CLASIFICADAS**: 189 muertas PURGADAS del en-US (179 placeholders de proyectiles, 7 tooltips vacíos, 3 pestanas del pergamín muerto) + 5 vivas AÑADIDAS al español (Bolsa Maestra, Coro de los Profundos, Pluma del Escriba Santo, Bolsa de la Forma Eterna, El Cronista). **PARIDAD PERFECTA: 1849 = 1849, cero huérfanas** (la traducción al inglés completa queda para el final del mod, como manda la letra).
12. **EL AZAR DEL DIBUJO SEMBRADO** (R22): las 7 llamadas reales de Main.rand en el dibujo (ElCronista y el Juicio de los Dos Soles) ahora nacen de semilla fija por entidad (whoAmI·edad) — la misma migaja de luz en todas las máquinas; el loop mide solo código.

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo | 0/0 (474 .cs contra tML 2026.09.3.1 real) |
| Build real | 0/0 + Packaging |
| GUARDIÁN | TODO VIVE ✔ (ProyeccionDelCiclope.png con su clase) |
| AUDIT v6.177 | TODO VERIFICADO ✔ (firmas vivas · paridad 1849=1849 · fósiles purgados) |
| Headless | 0 excepciones (Sandboxing v6.177) |
| Loop retro | SIN REGRESIONES (azar 10→0 · claves EN-sin-ES 194→0) |
| Sim 1:1 | La aspiradora SHIP (las 67 piezas en la boca en el MISMO tick) |

**Instalación**: copia `AethonMod_v6.178.tmod` a la carpeta `Mods` del tModLoader y actívalo.
EOF
)

python3 - "$PAT" "$TAG" "$NOTAS" <<'PYEOF'
import json, sys, urllib.request

pat, tag, notas = sys.argv[1], sys.argv[2], sys.argv[3]
repo = 'Leo0x01/Aethon-Mod-Terraria'
api = f'https://api.github.com/repos/{repo}/releases'

body = json.dumps({'tag_name': tag, 'name': f'{tag} — el rayo de verdad y la bruma pequeña',
                   'body': notas, 'draft': False, 'prerelease': False}).encode()
req = urllib.request.Request(api, data=body, method='POST')
req.add_header('Authorization', f'token {pat}')
req.add_header('Accept', 'application/vnd.github+json')
try:
    with urllib.request.urlopen(req) as r:
        rel = json.load(r)
    print(f"release {rel['id']} creado: {rel['html_url']}")
    with open('/tmp/release_v6178_id.txt', 'w') as f:
        f.write(str(rel['id']))
except urllib.error.HTTPError as e:
    if e.code == 422:
        req2 = urllib.request.Request(f'{api}/tags/{tag}')
        req2.add_header('Authorization', f'token {pat}')
        with urllib.request.urlopen(req2) as r:
            rels = json.load(r)
        rel = rels[0]
        print(f"release YA EXISTÍA {rel['id']}: continuando")
        with open('/tmp/release_v6178_id.txt', 'w') as f:
            f.write(str(rel['id']))
    else:
        raise
PYEOF

echo "[4/5] subiendo el asset…"
RELID=$(cat /tmp/release_v6178_id.txt)
curl -s -X POST \
  -H "Authorization: token $PAT" \
  -H "Content-Type: application/octet-stream" \
  --data-binary @"$TMOD" \
  "https://uploads.github.com/repos/$REPO/releases/$RELID/assets?name=AethonMod_v6.178.tmod" \
  | python3 -c "import json,sys; a=json.load(sys.stdin); print('asset:', a.get('name'), a.get('size'), 'B ·', a.get('state'))"

echo "[5/5] verificación del CDN…"
sleep 3
URL_DL="https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/download/$TAG/AethonMod_v6.178.tmod"
curl -sL -o /tmp/verif_v6178.tmod "$URL_DL" && md5sum /tmp/verif_v6178.tmod && echo "original: $MD5"
if [ "$(md5sum /tmp/verif_v6178.tmod | cut -d' ' -f1)" = "$MD5" ]; then
  echo "CDN VERIFICADO byte a byte ✔"
else
  echo "ADVERTENCIA: el CDN aún propaga (verificar en un rato)"
fiNOTAS=$(cat <<'EOF'
# v6.178 — EL RAYO DE VERDAD, LA BRUMA PEQUEÑA Y LA NOCHE PRESTADA

## Las once curas de la letra

1. **EL RAYO DE VERDAD** (la letra: «lo que me importa es el ataque que hace debes copiar su tecnica»): la técnica del arma real (el .tmod decompilado que subiste — la técnica, NO el material: 100% de la casa) reencarnada en **HazProyectadoLib** — la misma línea en SEIS CAPAS: las envolventes con sus tapas + EL CUERPO QUE FLUYE en tramos alineados (la textura se repite sin estirarse y la costura jamás se ve) + el segundo flujo + los bordes que arden + las dos luces — con EL CONTORNO ONDULANTE de tres octavas que viajan con el flujo y las puntas que cierran en sqrt. Los tres consumidores: el rayo continuo del visor (18 px), LA PROYECCIÓN de la X (100 px) y el barrido del faro (66 px), con los parámetros EXACTOS del plano.
2. **LA CURA DEL client.log** (116 fallos por sesión): la clave Opcionales pedía {0}/{1}/{2} y viajaban DOS argumentos — el {2} nacía muerto. Los TRES viajan (cuenta · total 6 · bono %).
3. **LA BRUMA PEQUEÑA DE LA BASE** (la letra: «la bruma… hace que los fps bajen hasta solo 5 fps… demasiada en el fondo»): LAS TUMBAS de niebla (radios hasta ~1.100 px) MUEREN — el cielo del interior vuelve al estado de hace dos commits — y nace UNA bruma PEQUEÑA por tentáculo, al ras de la punta, calidad mínima: se apaga, no tapa (y R24 nace en el MAESTRO: el decorado del interior es de GPU integrada).
4. **LA NOCHE PRESTADA UNIVERSAL** (la letra: «intento invocar al ojo de cuthulu pero este se va»): las IA de vanilla HUYEN del mediodía eterno del interior (el Ojo, los mechs, las abejas reales…) — la noche prestada se extiende a TODO NPC mientras el interior viva: dentro NADIE ve el amanecer y TODOS los jefes se quedan a pelear.
5. **LA VIDA CONTADA** (la letra: «los proyectiles del Rey Slime en nivel 26 se quedan en el mapa permanentemente… analiza que este error no este en mas jefes»): el default vanilla de timeLeft es 3600 ticks = 60 SEGUNDOS (verificado contra el decompile) y el Spike del Rey Slime no tiene override — TODA volea de TODOS los jefes nace con 180 ticks: 3 segundos y fuera (R23 en el MAESTRO).
6. **EL VIGÍA BORRADO POR LA PÁGINA** (la letra: «si uso el item pagina en blanco… debe volver a comerse al Devoramundos con su segunda invocacion desde ahi»): el olvido también borra el reloj de invocaciones del Devorador — desde el olvido, la SEGUNDA vez que el gusano nazca, la aspiradora vuelve a llevárselo entero.
7. **EL BLANCO DE LAS RANURAS, EXORCIZADO** (la letra: «los 3 slot del arma estan en blanco aunque si tienen el arma»): EL BANCO DE PRUEBAS headless (DiagnosticoPersistenciaSistema) reproduce el reinicio completo de verdad (guardar a DISCO → recargar en ítem nuevo) y el veredicto es VERDE: las tres ranuras, el modo, el albedrío, el sello y la PUERTA sobreviven — la persistencia era INOCENTE. La cura: el icono blindado con try/catch + EL NOMBRE DEL ARMA COSIDO debajo de cada ranura (la ranura se LEE, viva o muerta).
8. **LA PUERTA PROBADA**: la misma prueba del banco certifica que la cuenta de las mil esencias vive con el archivo del mundo (y el diagnóstico no toca la partida: guarda y devuelve).
9. **EL FARO CON MUCHOS VFX** (la letra: «parece que le faltan VFX y muchos VFX»): LA CORONA FANTASMA curada (los 22 picos dibujaban con VFXCore.Quad — que SOLO ACUMULA al buffer sin Flush: eran INVISIBLES desde la v6.177, la mitad del faro faltante) + LA ESTELA DEL BARRIDO (5 fantasmas del haz en los ángulos ya barridos) + LA CAMPANADA (el anillo que nace del centro a cada vuelta) + EL CHOQUE DE PUNTA (LaserScan contra los muros: la punta ARDE con doble estallido y chispas donde el barrido pega).
10. **LOS BOTONES CON GARANTÍA ABSOLUTA** (la letra: «el texto en los botones del altar se desbordan»): la medición ya existía y se vio otra vez — el PISO DE LEGIBILIDAD (0.72) + LA ELIPSIS: si al piso no cabe, se recorta con «…» hasta que quepa. NADA desborda NUNCA.
11. **LA LECCIÓN DEL FÓSIL, CONFIRMADA**: el build de esta sesión re-trajo 180 fósiles al en-US (tML reescribe los hjson fuente al compilar) — purgados con la cura completa: fuente restaurada + .tmod viejo BORRADO + rebuild sobre el limpio. Paridad 1901=1901.

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo | 0/0 (488 .cs contra el tML 2026.09.3.1 real) |
| Build real | 0/0 + Packaging |
| GUARDIÁN | TODO VIVE ✔ (las cuatro texturas del rayo dentro) |
| AUDIT v6.178 | TODO VERIFICADO ✔ (tumba muerta · 3 brumas pequeñas · 3 argumentos · noche universal · 4 voleas con vida · vigía borrable · paridad 1901=1901) |
| Headless | 0 excepciones + DiagnosticoPersistencia TODO VIVO (trío · puerta · sello) |
| Loop retro | SIN REGRESIONES (fósiles 180→0) |
| Sim 1:1 | EL RAYO SHIP (capas · la ola viaja · puntas sqrt · costura 1/255) · QA VLM 7/10 |

**Instalación**: copia `AethonMod_v6.178.tmod` a la carpeta `Mods` del tModLoader y actívalo. Si ya tenías una versión anterior en esa carpeta, BÓRRALA primero (la lección del fósil: el build re-trae los textos del .tmod viejo).
EOF
)

python3 - "$PAT" "$TAG" "$NOTAS" <<'PYEOF'
import json, sys, urllib.request

pat, tag, notas = sys.argv[1], sys.argv[2], sys.argv[3]
repo = 'Leo0x01/Aethon-Mod-Terraria'
api = f'https://api.github.com/repos/{repo}/releases'

body = json.dumps({'tag_name': tag, 'name': f'{tag} — el rayo de verdad y la bruma pequeña',
                   'body': notas, 'draft': False, 'prerelease': False}).encode()
req = urllib.request.Request(api, data=body, method='POST')
req.add_header('Authorization', f'token {pat}')
req.add_header('Accept', 'application/vnd.github+json')
try:
    with urllib.request.urlopen(req) as r:
        rel = json.load(r)
    print(f"release {rel['id']} creado: {rel['html_url']}")
    with open('/tmp/release_v6178_id.txt', 'w') as f:
        f.write(str(rel['id']))
except urllib.error.HTTPError as e:
    if e.code == 422:
        req2 = urllib.request.Request(f'{api}/tags/{tag}')
        req2.add_header('Authorization', f'token {pat}')
        with urllib.request.urlopen(req2) as r:
            rels = json.load(r)
        rel = rels[0]
        print(f"release YA EXISTÍA {rel['id']}: continuando")
        with open('/tmp/release_v6178_id.txt', 'w') as f:
            f.write(str(rel['id']))
    else:
        raise
PYEOF

echo "[4/5] subiendo el asset…"
RELID=$(cat /tmp/release_v6178_id.txt)
curl -s -X POST \
  -H "Authorization: token $PAT" \
  -H "Content-Type: application/octet-stream" \
  --data-binary @"$TMOD" \
  "https://uploads.github.com/repos/$REPO/releases/$RELID/assets?name=AethonMod_v6.178.tmod" \
  | python3 -c "import json,sys; a=json.load(sys.stdin); print('asset:', a.get('name'), a.get('size'), 'B ·', a.get('state'))"

echo "[5/5] verificación del CDN…"
sleep 3
URL_DL="https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/download/$TAG/AethonMod_v6.178.tmod"
curl -sL -o /tmp/verif_v6178.tmod "$URL_DL" && md5sum /tmp/verif_v6178.tmod && echo "original: $MD5"
if [ "$(md5sum /tmp/verif_v6178.tmod | cut -d' ' -f1)" = "$MD5" ]; then
  echo "CDN VERIFICADO byte a byte ✔"
else
  echo "ADVERTENCIA: el CDN aún propaga (verificar en un rato)"
fi

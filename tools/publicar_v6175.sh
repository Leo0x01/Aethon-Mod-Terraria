#!/usr/bin/env bash
# ==================================================================
#  publicar_v6175.sh — el publicador de la casa (v6.175)
# ==================================================================
set -euo pipefail
cd /home/z/my-project/mod

TAG="v6.175"
TMOD="$HOME/.local/share/Terraria/tModLoader/Mods/AethonMod.tmod"
MD5=$(md5sum "$TMOD" | cut -d' ' -f1)
echo "[1/5] md5: $MD5"

URL=$(git config --get remote.origin.url)
# v6.175: el PAT es SOLO el token (la URL es https://user:token@github.com/…)
PAT=${URL#*://}
PAT=${PAT#*:}
PAT=${PAT%@*}
REPO="Leo0x01/Aethon-Mod-Terraria"

echo "[2/5] verificando el tag…"
git rev-parse "$TAG^{commit}" >/dev/null

echo "[3/5] creando el release $TAG…"
NOTAS=$(cat <<'EOF'
# v6.175 — EL LOOP DE RETROALIMENTACIÓN, LA ARENA QUE PELEA Y LA DEVORACIÓN DEL CORO

## Las once curas de la letra

1. **EL LOOP DE RETROALIMENTACIÓN** (la auditoría v6.174 pedía: 687 catches vacíos, el 53,8 %): `RegistroSistema` — el registro de la casa (primera vez de cada fallo → log oficial + chat; repeticiones → contador; resumen al cerrar el mundo) y los **694 «catch { }» resucitados** que ahora registran. El loop se MIDE y se COMPARA con `tools/loop_retro_v6175.py`: **catches vacíos 687 → 0**. Nueva regla de la casa: ningún «catch { }» vuelve a nacer.
2. **LA BARRA DE LOS JEFES INTERIORES**: la barra de vanilla congela el lifeMax al primer dibujo — al crecer la vida, la barra REBOSABA y el número total quedaba viejo. AHORA: `BarraJefeInterior` (la ModBossBar propia del sello interior — vida y lifeMax VIVOS).
3. **LA ARENA SIN LÍMITE Y CON AURA**: sin tope de jefes invocables en el mundo interior; el sello viste el AURA de la oleada de hambre.
4. **EL MOTOR DE COMBATE DE LA ARENA**: vida base ×1.5 · **DASH con telégrafo** (la línea de aviso que LATE + la estela de rayos) desde nivel 3 · **VOLEAS TEMÁTICAS** (el patrón y el COLOR de CADA jefe: abanico, anillo, lluvia, espiral — 13 temas, filo de rayo para los mecánicos) · **MINIONS** desde 4 · **COMBOS de tres pasos** desde 6 (dash certero → anillo doble → andanada). Solo en el mundo interior y en las oleadas de hambre.
5. **LA DEVORACIÓN DEL CORO** (todos los jefes): el grimorio cuenta sus sombras (nivel + despertar → hasta 8) y **las usa TODAS**: N sombras de la página nacen del libro, orbitan al jefe y lo CIÑEN; el jefe queda INTANGIBLE e INTARGETEABLE; lo devoran como siempre y **el loot cae como una muerte normal**.
6. **LA ASPIRADORA DEL DEVORADOR**: la devoración normal del gusano invocado MUERE; desde la SEGUNDA invocación, el cine espacial — las secciones se SEPARAN, vuelan a la boca del libro de la cola a la cabeza, cada pieza muere con su loot… y el grimorio dice: *«Esto es asqueroso. No volveré a devorar esta cosa tan desagradable... prefiero comerme al Rey Slime que a esta porquería.»*
7. **LA INTERFAZ**: textos desbordados curados · las ventanas flotantes PEGADAS al slot (justo debajo) · el checklist del trío SIMPLE (solo el cuadradito, que se rellena de morado).
8. **EL TAJO SAKUGA** (el arma recreada desde cero, 16 capas del canon investigado): anticipación (estrella de la hoja) · frame de impacto MONOCROMO + panel de tinta + sacudida + onda · la luna única con DESPLAZAMIENTO CROMÁTICO y su CORO DE ECO · líneas de velocidad · la cicatriz de tinta. El corte golpea en el frame de impacto.
9. **EL FARO DEL ECLIPSE EN LA MANO**: se entrega directo al inventario al entrar al mundo (siempre que no lo lleves).
10. **EL HAZ CONCENTRADO** (la recreación dla técnica del haz concentrado — miniatura en alta + 3 fotogramas reales analizados con visión): jefe maniquí pálido de UN ojo magenta, naciendo del HAZ DE PROYECCIÓN violeta-magenta, con su rayo del ojo (telegraph + doble haz), su derrame de ojitos y su expansión a mitad de vida. Invócalo con el **Eco Proyectado** (en la bolsa del probador).
11. **LOCALIZACIÓN**: las 5 claves «candidatas» eran falsos positivos (las variantes ya vivían); las 2 ausentes reales en inglés añadidas.

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo | 0/0 (469 .cs contra tML 2026.09.3.1 real) |
| Build real | 0/0 · 535 entradas · 21.984.632 B · EOF exacto |
| GUARDIÁN | TODO VIVE ✔ (el PNG huérfano de LineaDelCielo cazado y purgado) |
| AUDIT v6.175 | TODO VERIFICADO ✔ (firmas vivas, LineaDelCielo muerta, hjson ×3) |
| Headless | 0 excepciones (Sandboxing → Adding Recipes) |
| Loop retro | SIN REGRESIONES (catches 687→0) |

**Instalación**: copia `AethonMod.tmod` a la carpeta `Mods` del tModLoader y actívalo.
EOF
)

python3 - "$PAT" "$TAG" "$NOTAS" <<'PYEOF'
import json, sys, urllib.request

pat, tag, notas = sys.argv[1], sys.argv[2], sys.argv[3]
repo = 'Leo0x01/Aethon-Mod-Terraria'
api = f'https://api.github.com/repos/{repo}/releases'

body = json.dumps({'tag_name': tag, 'name': f'{tag} — el loop de retroalimentación y la arena que pelea',
                   'body': notas, 'draft': False, 'prerelease': False}).encode()
req = urllib.request.Request(api, data=body, method='POST')
req.add_header('Authorization', f'token {pat}')
req.add_header('Accept', 'application/vnd.github+json')
try:
    with urllib.request.urlopen(req) as r:
        rel = json.load(r)
    print(f"release {rel['id']} creado: {rel['html_url']}")
    with open('/tmp/release_v6175_id.txt', 'w') as f:
        f.write(str(rel['id']))
except urllib.error.HTTPError as e:
    if e.code == 422:
        # ya existe → recuperar su id y continuar (la lección de tolerancia)
        req2 = urllib.request.Request(f'{api}/tags/{tag}')
        req2.add_header('Authorization', f'token {pat}')
        with urllib.request.urlopen(req2) as r:
            rels = json.load(r)
        rel = rels[0]
        print(f"release YA EXISTÍA {rel['id']}: continuando")
        with open('/tmp/release_v6175_id.txt', 'w') as f:
            f.write(str(rel['id']))
    else:
        raise
PYEOF

echo "[4/5] subiendo el asset…"
RELID=$(cat /tmp/release_v6175_id.txt)
curl -s -X POST \
  -H "Authorization: token $PAT" \
  -H "Content-Type: application/octet-stream" \
  --data-binary @"$TMOD" \
  "https://uploads.github.com/repos/$REPO/releases/$RELID/assets?name=AethonMod_v6.175.tmod" \
  | python3 -c "import json,sys; a=json.load(sys.stdin); print('asset:', a.get('name'), a.get('size'), 'B ·', a.get('state'))"

echo "[5/5] verificación del CDN…"
sleep 3
URL_DL="https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/download/$TAG/AethonMod_v6.175.tmod"
curl -sL -o /tmp/verif_v6175.tmod "$URL_DL" && md5sum /tmp/verif_v6175.tmod && echo "original: $MD5"
if [ "$(md5sum /tmp/verif_v6175.tmod | cut -d' ' -f1)" = "$MD5" ]; then
  echo "CDN VERIFICADO byte a byte ✔"
else
  echo "ADVERTENCIA: el CDN aún propaga (verificar en un rato)"
fi

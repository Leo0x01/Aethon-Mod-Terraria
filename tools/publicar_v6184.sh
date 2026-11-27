#!/usr/bin/env bash
# publicar_v6184.sh — el publicador de la casa (v6.184)
set -euo pipefail
cd /home/z/my-project/mod

TAG="v6.184"
TMOD="/home/z/my-project/download/AethonMod_v6.184.tmod"
MD5=$(md5sum "$TMOD" | cut -d' ' -f1)
SIZE=$(stat -c%s "$TMOD")
echo "[1/6] md5: $MD5 · ${SIZE} B"

URL=$(git config --get remote.origin.url)
PAT=${URL#*://}
PAT=${PAT#*:}
PAT=${PAT%@*}
REPO="Leo0x01/Aethon-Mod-Terraria"

echo "[2/6] verificando el tag…"
git rev-parse "$TAG^{commit}" >/dev/null

echo "[3/6] creando el release $TAG…"
NOTAS=$(cat <<'EOF'
# v6.184 — LA PURGA QUE SE COMÍA SU PROPIA MESA, EL DIOS SEPARADO Y LA LUZ CANÓNICA

## Las siete curas de la letra

1. **EL FESTÍN DEL DEVORAMUNDO, POR FIN**: cuatro versiones seguidas sin animación y la causa era un asesino interno — **LA PURGA DE LA MESA SE COMÍA A SU PROPIA ANIMACIÓN**: el pedacito purgaba los proyectiles de la zona DESPUÉS de parir la bruma del festín, y la bruma nace encima del preso sin `friendly`… muerta al instante desde la v6.180. AHORA la purga **salta al FauceDevoradorProjectile SIEMPRE** y el orden canónico es **purga primero, boca después**. **VERIFICADO EN EL BANCO HEADLESS NUEVO**: el servidor vacío jamás tickea el mundo — el banco planta un socket falso y el mundo VIVE — y el veredicto del ciclo real es **A-FESTÍN COMPLETO ✔** (mesa servida · fauce nació · la sección murió de verdad) y el borde de 2600 px sigue sin quemar el cerrojo.
2. **EL DIOS SELLADO, SEPARADO DE VERDAD**: sus ataques lo golpeaban a él mismo porque la pareja dorada/violeta era un **proyectil vanilla con EL JUGADOR de dueño** (un ataque «amigo» nacido del jefe se volvía contra el dios). AHORA la pareja es del Sellado (hostil, de la casa) y TODO lo que el dios dispara es SUYO.
3. **EL OJO ES UN MINION**: cuando el dios hace su ataque de ojo, el ojo de la corona **se desprende y caza**: un NPC minion con su **animación completa de parpadeo**, su **iris clavado en la presa**, órbita viva con telegrafo y picado de dos tiempos — vive 12 s y se disuelve en tinta (máximo cuatro a la vez).
4. **LA EXPLOSIÓN DE LUZ CANÓNICA**: la explosión de textura extraña (el estilo prestado del jefe Recuerdo) **MUERE** — la explosión de luz del dios ahora ES **la de la mascota Pequeño Recuerdo de Aethon**, portada a librería (`EstallidoLuzLib`) y **escalada ×2,2** a tamaño de dios: la recogida de 6 agujas, los 14 rayos, la cruz anamórfica, el núcleo que inunda, la estrella, la onda y las bokeh. Llega a la tercera andanada del coro, sobre la presa (esquivable).
5. **EL NACIMIENTO DEL RAYO DE PRUEBA C**: al aparecer el haz, **su base estalla con la explosión de la mascota** (con su dañito y su gratitud del 1 % de vida).
6. **EL SOLAPAMIENTO DE LOS BUFFS MUERTO**: el icono de la Recarga de la Ira se dibujaba dos veces (el `PreDraw` devolvía `true` y vanilla redibujaba encima) y su marco de luz derramaba sobre la celda vecina — el «solapamiento con Ahogamiento». AHORA el buff se dibuja completo UNA vez (`return false`) y ningún icono del mod pisa a otro.
7. **EL VÁSTAGO v3 — ADORABLE DE VERDAD**: librito regordete con volumen de verdad (luz arriba, sombra al pie), ojotes de doble reflejo SIEMPRE en su sitio, rubor, sonrisita, alitas cosidas a la axila y cinta dorada colgando — 4 rondas de QA visual: **9/10 SHIP**.

## VERIFICACIÓN

| Gate | Resultado |
| --- | --- |
| Oráculo (481 .cs) | 0 errores / 0 avisos |
| Build REAL | 0/0 + Packaging (536 entradas) |
| Guardián de texturas | TODO VIVE (el minion sin cara cazado ANTES de publicar) |
| Guardián de arranque | EL CLIENTE VIVE |
| Guardián de las llaves | TODO VIVE (1864 ×3) |
| AUDIT v6.184 | TODO VERIFICADO |
| Headless | 0 excepciones |
| **El banco del festín** | **A-FESTÍN COMPLETO ✔ · EL LEJOS NO CENA ✔** |
| Loop de retroalimentación | Sin regresiones (catches 0 · azar 0) |
| QA VLM (Vástago) | 9/10 SHIP |

**md5: `15c9176fbbe834921cd045bd3dd13e20`** — reemplaza el AethonMod.tmod viejo de tu carpeta Mods.
EOF
)

PAYLOAD=$(jq -n --arg tag "$TAG" --arg name "v6.184 — el festín de verdad, el dios separado y la luz canónica" --arg body "$NOTAS" '{tag_name:$tag, name:$name, body:$body}')

echo "[4/6] subiendo el release…"
RESP=$(curl -s -X POST -H "Authorization: token $PAT" -H "Content-Type: application/json" -d "$PAYLOAD" "https://api.github.com/repos/$REPO/releases")
REL_ID=$(echo "$RESP" | jq -r '.id')
if [ -z "$REL_ID" ] || [ "$REL_ID" = "null" ]; then echo "FALLO el release:"; echo "$RESP" | head -5; exit 1; fi
echo "release id: $REL_ID"

echo "[5/6] subiendo el asset…"
ASSET_URL=$(echo "$RESP" | jq -r '.upload_url' | sed 's/{?name,label}//')
curl -s -X POST -H "Authorization: token $PAT" -H "Content-Type: application/octet-stream" --data-binary @"$TMOD" "${ASSET_URL}?name=AethonMod_v6.184.tmod" | jq -r '.browser_download_url' > /tmp/asset_url_v6184.txt
cat /tmp/asset_url_v6184.txt

echo "[6/6] verificando el CDN…"
sleep 6
DL=$(cat /tmp/asset_url_v6184.txt)
curl -sL --max-time 180 -o /tmp/v6184_cdn.tmod "$DL"
MD5_CDN=$(md5sum /tmp/v6184_cdn.tmod | cut -d' ' -f1)
if [ "$MD5_CDN" = "$MD5" ]; then echo "CDN VERIFICADO: md5 idéntico ($MD5)"; else echo "¡MD5 DISTINTO! local=$MD5 cdn=$MD5_CDN"; exit 1; fi

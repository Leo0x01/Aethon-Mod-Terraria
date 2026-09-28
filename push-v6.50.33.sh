#!/usr/bin/env bash
# ============================================================================
#  push-v6.50.33.sh — LA ENTREGA PENDIENTE (v6.50.33)
#
#  TODO (build 0/0 + auditoría) ESTÁ HECHO Y COMMITTEADO LOCALMENTE
#  (commit 5871389 + tag v6.50.33). FALTA SOLO: push + release de GitHub —
#  el token temporal del usuario fue BORRADO por el reset del sandbox
#  (vivió en .env y ~/.config/git/credentials, ambos wipeados; el snapshot
#  /home/sync/repo.tar lo trae REDACTADO por la plataforma).
#
#  USO (cuando haya token nuevo):
#     GITHUB_TOKEN=github_pat_XXXX ./push-v6.50.33.sh
#
#  EL SCRIPT: configura las credenciales, pushea main + tag, crea el
#  release de GitHub con el .tmod adjunto y verifica la descarga CDN.
# ============================================================================
set -e
TOKEN="${GITHUB_TOKEN:?Uso: GITHUB_TOKEN=github_pat_XXXX ./push-v6.50.33.sh}"
REPO="Leo0x01/Aethon-Mod-Terraria"
DIR="$(cd "$(dirname "$0")" && pwd)"
TMOD="/home/z/my-project/download/AethonMod_v6533.tmod"

# 1) credenciales (la casa: store + chmod 600)
git config --global credential.helper store
mkdir -p ~/.config/git
printf 'https://Leo0x01:%s@github.com\n' "$TOKEN" > ~/.config/git/credentials
chmod 600 ~/.config/git/credentials
# y el .env para las próximas sesiones
grep -q GITHUB_TOKEN /home/z/my-project/.env 2>/dev/null || \
  printf '\nGITHUB_TOKEN=%s\n' "$TOKEN" >> /home/z/my-project/.env

# 2) push main + tag
cd "$DIR"
git push origin main v6.50.33

# 3) release con el .tmod adjunto
NOTAS=/tmp/notas_v6533.md
cat > "$NOTAS" <<'EOF'
EL DRAGÓN DEL CIELO, ENCARNACIÓN SPRITE — el jefe rediseñado COMPLETO con sprites por segmento (el patrón del Devourer of Gods de Calamity) + EL BUG DEL GUÍA fixeado de raíz.

- **EL BUG DEL GUÍA**: las alas se pegaban al Guía porque `Segmento()` leía `NPC.ai[0]` (que la IA sobrescribe cada tick con el ESTADO) como puntero de cadena → caía en `Main.npc[0]` = el Guía. Ahora la cadena se halla por identidad (tipo + ai[2] + ai[3]) con caché.
- **EL SET DE 7 SPRITES** (generado por código, `tools/tools/gen_slifer_sprites_v6533.py`, 5 rondas de QA con visión artificial): cabeza con máscara plateada + colmillos sable + corona de 5 llamas + ojo de oro + gema azul; **mandíbula giratoria con LA SEGUNDA BOCA**; vértebras escamadas con aleta dorsal y vientre de pizarra; cola espatulada; alas de murciélago con huesos sobre el paño; retrato e icono 32×32.
- **LA CURVA DE ESCALA ANATÓMICA**: cuello fino (0.50) → torso (1.0) → punta de látigo (0.36) — los anillos encadenados del DoG.
- **EL LEVIATÁN DEL FONDO** lleva ahora el cráneo real silueteado + la mandíbula abierta + la cola de pala.

Verificación: build 0/0 contra tML 2026.07.3.0 real · .tmod auditado byte a byte (400 entradas) · DLL inspeccionada · hjson 621=621 · servidor headless carga sin excepciones.
EOF
curl -s -X POST \
  -H "Authorization: Bearer $TOKEN" \
  -H "Accept: application/vnd.github+json" \
  "https://api.github.com/repos/$REPO/releases" \
  -d "{\"tag_name\":\"v6.50.33\",\"name\":\"v6.50.33 — El Dragón del Cielo, encarnación sprite\",\"body\":\"$(sed 's/"/\\"/g; s/$/\\n/' "$NOTAS" | tr -d '\n')\"}" \
  | python3 -c "import json,sys; r=json.load(sys.stdin); print('release id:', r.get('id'), r.get('html_url'))"

# 4) adjuntar el .tmod
REL_ID=$(curl -s -H "Authorization: Bearer $TOKEN" \
  "https://api.github.com/repos/$REPO/releases/tags/v6.50.33" \
  | python3 -c "import json,sys; print(json.load(sys.stdin)['id'])")
curl -s -X POST \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/octet-stream" \
  --data-binary @"$TMOD" \
  "https://uploads.github.com/repos/$REPO/releases/$REL_ID/assets?name=AethonMod.tmod" \
  | python3 -c "import json,sys; r=json.load(sys.stdin); print('asset:', r.get('browser_download_url'))"

# 5) verificar la descarga CDN byte-idéntica
sleep 3
URL=$(curl -s -H "Authorization: Bearer $TOKEN" \
  "https://api.github.com/repos/$REPO/releases/tags/v6.50.33" \
  | python3 -c "import json,sys; print(json.load(sys.stdin)['assets'][0]['browser_download_url'])")
curl -sL -o /tmp/verifica.tmod "$URL"
echo "local:  $(md5sum "$TMOD")"
echo "CDN:    $(md5sum /tmp/verifica.tmod)"
cmp "$TMOD" /tmp/verifica.tmod && echo "BYTE-IDÉNTICA ✓ ENTREGA COMPLETA"

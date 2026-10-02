#!/usr/bin/env bash
# ============================================================================
# push-v6.50.52.sh — LA ENTREGA EN 1 COMANDO (v6.50.52)
#
# Uso:      GITHUB_TOKEN=github_pat_XXXX ./push-v6.50.52.sh
#
# Hace TODO: (1) configura el remote con el token; (2) pushea main + tag
# v6.50.52; (3) crea la release de GitHub con AethonMod.tmod adjunto;
# (4) verifica el CDN byte a byte (md5); (5) restaura el remote limpio.
# Al terminar, BORRAR este script (la misión se cumple una vez — la lección
# de la .33/.34 y el precedente 5ccc489 de la .50).
# ============================================================================
set -euo pipefail

cd "$(dirname "$0")"

REPO=Leo0x01/Aethon-Mod-Terraria
TAG=v6.50.52
TMOD=/home/sync/AethonMod-v6.50.52.tmod
LIMPIO="https://github.com/${REPO}.git"
TOKEN="${GITHUB_TOKEN:?Uso: GITHUB_TOKEN=github_pat_XXXX ./push-v6.50.52.sh}"

MD5_LOCAL="$(md5sum "${TMOD}" | cut -d' ' -f1)"
echo "== v6.50.52 | .tmod $(stat -c%s "${TMOD}") bytes | md5 ${MD5_LOCAL}"

echo "== (1/4) remote tokenizado + push main --tags"
git remote set-url origin "https://x-access-token:${TOKEN}@github.com/${REPO}.git"
git push origin main --tags

echo "== (2/4) release de GitHub"
BODY="$(python3 - << 'PY'
import re
txt = open('CHANGES.md', encoding='utf-8').read()
m = re.search(r'## Commit v6\.50\.52 — (.+?)(?=\n## Commit v6\.50\.51)', txt, re.S)
body = (m.group(1) if m else 'v6.50.52').strip()
# GitHub: máx ~125k caracteres por body — recorta si hace falta
print(body[:120000])
PY
)"
python3 - "${TOKEN}" "${REPO}" "${TAG}" "${BODY}" << 'PY'
import json, sys, urllib.request
token, repo, tag, body = sys.argv[1:5]
api = f"https://api.github.com/repos/{repo}/releases"
data = json.dumps({"tag_name": tag, "name": f"{tag} — LA NOVENA RONDA: el arcoíris del ítem + el jefe sin arcoíris + la entrada en dos actos (el espejo puro: el jefe aparece siempre)", "body": body}).encode()
req = urllib.request.Request(api, data=data, method="POST", headers={
    "Authorization": f"Bearer {token}", "Accept": "application/vnd.github+json",
    "Content-Type": "application/json"})
r = json.load(urllib.request.urlopen(req))
print("release_id:", r["id"])
open("/tmp/release_id.txt", "w").write(str(r["id"]))
PY

echo "== (3/4) subida del asset AethonMod.tmod"
REL_ID="$(cat /tmp/release_id.txt)"
python3 - "${TOKEN}" "${REPO}" "${REL_ID}" "${TMOD}" << 'PY'
import json, sys, urllib.request
token, repo, rel_id, path = sys.argv[1:5]
url = f"https://uploads.github.com/repos/{repo}/releases/{rel_id}/assets?name=AethonMod.tmod"
data = open(path, "rb").read()
req = urllib.request.Request(url, data=data, method="POST", headers={
    "Authorization": f"Bearer {token}", "Accept": "application/vnd.github+json",
    "Content-Type": "application/octet-stream"})
r = json.load(urllib.request.urlopen(req))
print("asset:", r["id"], r["browser_download_url"])
open("/tmp/asset_url.txt", "w").write(r["browser_download_url"])
PY

echo "== (4/4) verificación CDN byte a byte"
ASSET_URL="$(cat /tmp/asset_url.txt)"
curl -sL "${ASSET_URL}" -o /tmp/cdn_check.tmod
MD5_CDN="$(md5sum /tmp/cdn_check.tmod | cut -d' ' -f1)"
echo "   local: ${MD5_LOCAL}"
echo "   CDN:   ${MD5_CDN}"
if [ "${MD5_LOCAL}" = "${MD5_CDN}" ]; then
  echo "   CDN VERIFICADO byte a byte ✓"
else
  echo "   ¡MD5 DISTINTO! — revisar la subida" >&2; exit 1
fi

# limpieza: remote limpio (el token NO queda incrustado si el usuario prefiere)
git remote set-url origin "${LIMPIO}"
echo "== LISTO: push + release + CDN ✓ — ahora BORRA este script (git rm)."

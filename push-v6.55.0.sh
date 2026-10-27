#!/usr/bin/env bash
# ============================================================================
# push-v6.55.0.sh — LA ENTREGA EN 1 COMANDO (v6.55.0)
#
# Uso:      GITHUB_TOKEN=github_pat_XXXX ./push-v6.55.0.sh
#
# Hace TODO: (0) verifica el .tmod (md5 86387e1a…) y guarda el token en los
# 2 sitios de la casa; (1) configura el remote tokenizado y pushea main +
# tag v6.55.0; (2) crea la release de GitHub con las notas preparadas;
# (3) sube el asset AethonMod.tmod; (4) verifica el CDN byte a byte (md5);
# (5) restaura el remote limpio. Al terminar, BORRAR este script (la misión
# se cumple una vez — la lección de la .33/.34/.50/.52).
# ============================================================================
set -euo pipefail

cd "$(dirname "$0")"

REPO=Leo0x01/Aethon-Mod-Terraria
TAG=v6.55.0
TMOD=/home/sync/AethonMod-v6.55.0.tmod
NOTAS=/home/sync/RELEASE_NOTES_v6.55.0.md
MD5_ESPERADO=86387e1a460b1b868caa7aa01778a87e
LIMPIO="https://github.com/${REPO}.git"
TITULO="${TAG} — LA CURA DE LOS PICOS (LA CAUSA RAÍZ) + EL RAYO CONCENTRADO II 100 % CASA + LAS ALAS VECTORIALES + LA PURGA TOTAL"
TOKEN="${GITHUB_TOKEN:?Uso: GITHUB_TOKEN=github_pat_XXXX ./push-v6.55.0.sh}"

echo "== (0/5) verificación previa"
[ -f "${TMOD}" ] || { echo "FALTA ${TMOD}" >&2; exit 1; }
MD5_LOCAL="$(md5sum "${TMOD}" | cut -d' ' -f1)"
if [ "${MD5_LOCAL}" != "${MD5_ESPERADO}" ]; then
  echo "¡MD5 DISTINTO DEL ESPERADO! local=${MD5_LOCAL} esperado=${MD5_ESPERADO}" >&2
  exit 1
fi
echo "   .tmod v6.55.0: $(stat -c%s "${TMOD}") bytes | md5 ${MD5_LOCAL} ✓"

# token a los 2 sitios de la casa (.env + credential store, chmod 600)
mkdir -p ~/.config/git && chmod 700 ~/.config/git
printf 'https://Leo0x01:%s@github.com\n' "${TOKEN}" > ~/.config/git/credentials
chmod 600 ~/.config/git/credentials
git config --global credential.helper store
grep -q '^GITHUB_TOKEN=' /home/z/my-project/.env 2>/dev/null || \
  printf 'GITHUB_TOKEN=%s\n' "${TOKEN}" >> /home/z/my-project/.env

echo "== (1/5) remote tokenizado + push main --tags"
git remote set-url origin "https://x-access-token:${TOKEN}@github.com/${REPO}.git"
git push origin main --tags

echo "== (2/5) release de GitHub"
BODY="$(python3 - "${NOTAS}" << 'PY'
import sys, re
try:
    body = open(sys.argv[1], encoding='utf-8').read().strip()
except Exception:
    body = ''
if not body:  # fallback: extraer la entrada del CHANGES.md
    txt = open('CHANGES.md', encoding='utf-8').read()
    m = re.search(r'# v6\.55\.0 — (.+?)(?=\n# v6\.54)', txt, re.S)
    body = (m.group(1) if m else 'v6.55.0').strip()
print(body[:120000])  # GitHub: máx ~125k caracteres por body
PY
)"
python3 - "${TOKEN}" "${REPO}" "${TAG}" "${TITULO}" "${BODY}" << 'PY'
import json, sys, urllib.request
token, repo, tag, titulo, body = sys.argv[1:6]
api = f"https://api.github.com/repos/{repo}/releases"
data = json.dumps({"tag_name": tag, "name": titulo, "body": body}).encode()
req = urllib.request.Request(api, data=data, method="POST", headers={
    "Authorization": f"Bearer {token}", "Accept": "application/vnd.github+json",
    "Content-Type": "application/json"})
r = json.load(urllib.request.urlopen(req))
print("release_id:", r["id"])
open("/tmp/release_id.txt", "w").write(str(r["id"]))
PY

echo "== (3/5) subida del asset AethonMod.tmod"
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

echo "== (4/5) verificación CDN byte a byte"
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

echo "== (5/5) remote limpio + comprobación /releases/latest"
git remote set-url origin "${LIMPIO}"
curl -s "https://api.github.com/repos/${REPO}/releases/latest" | \
  python3 -c 'import json,sys; r=json.load(sys.stdin); print("latest:", r.get("tag_name"), "| asset:", r["assets"][0]["browser_download_url"] if r.get("assets") else "—")'
echo "== LISTO: push + release + CDN ✓ — ahora BORRA este script (git rm)."

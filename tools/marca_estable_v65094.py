#!/usr/bin/env python3
"""
v6.50.94 — MARCAR LA VERSIÓN ESTABLE (la letra del usuario: «antes de
continuar debes guardar la version actual como version estable»).

Patrón de casa stable-v5.0/.15/.27/v6.01/.81:
  · Tag    stable-v6.50.94        → 80d4d7b (el commit de la VERSIÓN — su
    árbol ES la .94; jamás en la punta de main: un commit nuevo arrastraría
    código de la .95 — lección del marcado .81).
  · Rama   stable-v6.50.94-backup → 80d4d7b (el mismo commit — la que se VE
    en la pestaña Branches del repo).
  · STABLE-SNAPSHOT.md: la sección del canal estable pasa de la .81 a la
    .94 (tag/rama/.tmod/cómo volver) + manifiesto SHA-256 de las 384
    entradas del paquete.
Uso: python3 tools/marca_estable_v65094.py   (desde la raíz del repo)
"""
import hashlib
import subprocess
import sys

sys.path.insert(0, 'tools')
from parse_tmod import parse  # noqa: E402

SNAP = 'STABLE-SNAPSHOT.md'
TMOD = '/home/sync/AethonMod-v6.50.94.tmod'
VER_COMMIT = '80d4d7b'          # «v6.50.94: EL RELOJ SIEMPRE + …»
RELEASE = 'release 408947983 + asset 627959141'
MD5 = '52004cee59ef7c79ef4da32c19ea593f'
TAM = 2555191

# --- 1. el manifiesto: sha256 de cada blob INFLADO, orden de la tabla ---
import zlib  # noqa: E402


def manifiesto():
    tmlver, name, ver, entries, tend, fsize = parse(TMOD)
    assert ver == '6.50.94' and name == 'AethonMod', (name, ver)
    assert fsize == TAM, fsize
    d = open(TMOD, 'rb').read()
    lineas, off = [], tend
    for ruta, raw, comp in entries:
        data = d[off:off + comp]
        off += comp
        blob = zlib.decompress(data, -15) if raw != comp else data
        lineas.append(f'{hashlib.sha256(blob).hexdigest()}  {ruta}')
    assert len(lineas) == 384, len(lineas)
    assert off == fsize, (off, fsize)
    return lineas


SECCION = """- **Tag**: `stable-v6.50.94` → `{verc}` — apunta DIRECTO al commit de la
  versión (su ÁRBOL es la .94); el tag NO va en la punta de main porque
  un commit nuevo arrastraría código de la .95 — lección del marcado .81.
- **Rama**: `stable-v6.50.94-backup` → `{verc}` (el MISMO commit que el
  tag) — patrón de casa stable-v5.0/.15/.27/v6.01/.81: la rama `-backup`
  es la que se VE en la pestaña *Branches* del repo (los tags viven en la
  pestaña *Tags*; el usuario buscó la .81 en Branches y no estaba).
- **.tmod**: {tam} B, md5 `{md5}` — {rel},
  CDN verificado byte a byte; respaldo `/home/sync/AethonMod-v6.50.94.tmod`.
- **Cómo volver**: `git checkout stable-v6.50.94` (o la rama
  `stable-v6.50.94-backup` — mismo commit). La .94 es la del NERVIOSO DE
  LA LETRA (el reloj siempre, el compás de la sombra, la fuga muerta, el
  festín egoísta, el espíritu único) — el usuario la PROBÓ y la mecánica
  quedó bien («perfecto ya funciona bien la mecanica del grimorio
  hambriento»). Entre la .94 y la .95: la .95 sólo SUELTA el compás de la
  sombra (el libro ataca cuando quiere) y AÑADE el arma nueva de la tinta
  (proyectil propio, adiós Nightglow) — SIN cambios de persistencia:
  volver pierde el arma nueva, los GUARDADOS del libro/oleadas quedan
  intactos.
- **`latest` SIGUE siendo la .94** hasta que la .95 se publique; el canal
  estable es el retorno seguro, no el frente.

### Manifiesto SHA-256 (las 384 entradas del paquete — patrón stable-v6.01)

```
# AethonMod.tmod v6.50.94 | {tam} B | md5 {md5}
# sha256(blob inflado)  ruta
{mani}
```
"""


def main():
    mani = manifiesto()
    texto = open(SNAP, encoding='utf-8').read()
    lineas = texto.split('\n')

    # la sección estable vieja: desde «- **Tag**: `stable-v6.50.81`» hasta
    # la valla de CIERRE del manifiesto (2ª «```» tras el título del bloque)
    ini = None
    for i, l in enumerate(lineas):
        if l.startswith('- **Tag**: `stable-v6.50.81`'):
            ini = i
            break
    assert ini is not None, 'no encontré la sección estable .81'
    fin, vallas = None, 0
    for i in range(ini, len(lineas)):
        if lineas[i].startswith('### Manifiesto SHA-256'):
            vallas = 0          # el título abre el bloque del manifiesto
        if lineas[i] == '```':
            vallas += 1
            if vallas == 2:
                fin = i         # apertura(1) … cierre(2)
                break
    assert fin is not None and fin > ini + 40, (ini, fin)

    nueva = SECCION.format(verc=VER_COMMIT, rel=RELEASE, md5=MD5, tam=TAM,
                           mani='\n'.join(mani))
    fuera = lineas[:ini] + nueva.split('\n') + lineas[fin + 1:]
    open(SNAP, 'w', encoding='utf-8', newline='\n').write('\n'.join(fuera))
    print(f'SNAPSHOT: sección estable .81 (líneas {ini + 1}-{fin + 1}) '
          f'→ .94 ({len(nueva.split(chr(10)))} líneas, manifiesto {len(mani)} entradas)')

    # --- 2. el tag y la rama (apuntan al commit de la VERSIÓN) ---
    def git(*args):
        r = subprocess.run(['git', *args], capture_output=True, text=True)
        if r.returncode != 0:
            print(f'  git {" ".join(args)}: {r.stderr.strip()[:120]}')
        return r.returncode == 0

    git('tag', 'stable-v6.50.94', VER_COMMIT)
    git('branch', '-f', 'stable-v6.50.94-backup', VER_COMMIT)
    r = subprocess.run(['git', 'rev-parse', 'stable-v6.50.94'],
                       capture_output=True, text=True)
    sha = r.stdout.strip()
    assert sha.startswith(VER_COMMIT), sha
    print(f'TAG stable-v6.50.94 → {sha[:9]} · RAMA stable-v6.50.94-backup → mismo commit')
    print('OK — listo para commit + push (tag y rama incluidos)')


if __name__ == '__main__':
    main()

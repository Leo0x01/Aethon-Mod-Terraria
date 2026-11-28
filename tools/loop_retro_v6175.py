#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
loop_retro_v6175.py — EL LOOP DE RETROALIMENTACIÓN DE LA CASA (paso 2).

LA LETRA: «crea un loop de retroalimentación para mejorar y depurar
errores». LA AUDITORÍA v6.174 dejó los números; ESTE script los convierte
en LOOP: cada vez que se corre (ideal: tras cada build, antes de cada
release) mide el árbol del mod y lo COMPARA con la versión anterior.

    medir → comparar → arreglar → volver a medir

LAS MÉTRICAS (las de la auditoría v6.174, para que los números calzen):
  · catches vacíos            (era 687 → meta 0)        [REGRESIÓN si sube]
  · claves loc: faltantes     (eran 2/187/5)            [REGRESIÓN si sube]
  · en-US sin traducir        (era 1239 iguales)        [MEJORA si baja]
  · Main.rand en el dibujo    (era 33)                  [REGRESIÓN si sube]
  · reservas new en dibujo    (era 18)                  [REGRESIÓN si sube]
  · archivos .cs / líneas     (468 / 192.168)           [informativo]
  · librerías VFX / líneas    (77 / 49.001)             [informativo]
  · Immediate                 (60 usos)                 [informativo]
  · texturas PNG válidas      (511, 0 inválidas)        [REGRESIÓN si inválidas]

LA REFERENCIA: los JSON de la auditoría v6.174 (subidos por el usuario en
upload/). Si no existen, se usa la ÚLTIMA medición guardada
(loop_retro_v6175.json) — así el loop se auto-alimenta.

SALIDA: loop_retro_v6175.json (junto a este script) + resumen en consola
con las flechas de cada métrica. EXIT CODE: 1 si HAY REGRESIÓN (el gate
del release lo lee).
"""
import json
import pathlib
import re
import sys

RAIZ = pathlib.Path(__file__).resolve().parent.parent
MOD = RAIZ / "AethonMod" / "Content"
SALIDA = pathlib.Path(__file__).resolve().parent / "loop_retro_v6175.json"
REFERENCIA = [
    pathlib.Path("/home/z/my-project/upload/auditoria_v6174_datos.json"),
    pathlib.Path("/home/z/my-project/upload/auditoria_fx_v6174.json"),
]

# ---------- utilidades ----------

def limpiar_comentarios(texto: str) -> str:
    out = list(texto)
    i, n = 0, len(texto)
    while i < n:
        c = texto[i]
        if c == '/' and i + 1 < n and texto[i + 1] == '/':
            j = texto.find('\n', i)
            if j < 0:
                j = n
            for k in range(i, j):
                if texto[k] != '\n':
                    out[k] = ' '
            i = j
        elif c == '/' and i + 1 < n and texto[i + 1] == '*':
            j = texto.find('*/', i + 2)
            j = n if j < 0 else j + 2
            for k in range(i, j):
                if texto[k] != '\n':
                    out[k] = ' '
            i = j
        elif c == '"':
            i += 1
            while i < n:
                if texto[i] == '\\':
                    i += 2
                    continue
                if texto[i] == '"':
                    i += 1
                    break
                i += 1
        else:
            i += 1
    return ''.join(out)


PAT_CATCH_VACIO = re.compile(
    r'catch\s*(?:\(\s*(?:System\.)?Exception(?:\s+\w+)?\s*\))?\s*\{\s*\}')

# ---------- mediciones ----------

def medir():
    m = {}
    cs = list(MOD.rglob('*.cs'))
    m['cs_archivos'] = len(cs)
    m['cs_lineas'] = sum(len(f.read_text(encoding='utf-8', errors='ignore').splitlines())
                         for f in cs)
    # catches vacíos (fuera del propio RegistroSistema, cuyo catch es sagrado)
    vacios, catch_total = 0, 0
    for f in cs:
        t = f.read_text(encoding='utf-8', errors='ignore')
        if 'class RegistroSistema' in t:
            continue
        catch_total += len(re.findall(r'catch\s*(?:\([^)]*\))?\s*\{', t))
        vacios += len(PAT_CATCH_VACIO.findall(limpiar_comentarios(t)))
    m['catch_vacio'] = vacios
    m['catch_total'] = catch_total
    m['catch_registrado'] = sum(
        1 for f in cs
        for _ in re.finditer(r'RegistroSistema\.Fallo', f.read_text(encoding='utf-8', errors='ignore')))

    # azar y reservas en métodos de dibujo: SOLO declaraciones override de
    # PreDraw/PostDraw (medición ESTRICTA v6.175 — la de la auditoría v6.174
    # cazaba «PreDraw» incluso en comentarios y contaba cuerpos ajenos: sus
    # 33/18 estaban inflados; este loop mide lo que hay DE VERDAD)
    rand_dibujo, alloc_dibujo = 0, 0
    rand_top, alloc_top = {}, {}
    pat_dibujo = re.compile(r'override[^\n{]*\b(?:PreDraw|PostDraw)\s*\(')
    for f in cs:
        t = f.read_text(encoding='utf-8', errors='ignore')
        for mm in pat_dibujo.finditer(t):
            k = t.find('{', mm.end())
            if k < 0:
                continue
            prof, j = 0, k
            while j < len(t):
                if t[j] == '{':
                    prof += 1
                elif t[j] == '}':
                    prof -= 1
                    if prof == 0:
                        break
                j += 1
            # v6.176 — LA MEDIDA HONESTA: el cuerpo se LIMPIA DE
            # COMENTARIOS antes de contar (3 de las 10 «llamadas» de la
            # auditoría eran documentación: «jamás Main.rand en render»).
            cuerpo = limpiar_comentarios(t[k:j])
            nr = cuerpo.count('Main.rand')
            na = len(re.findall(r'new\s+[A-Za-z_.]+\[', cuerpo))
            if nr:
                rand_dibujo += nr
                rand_top[f.name] = rand_top.get(f.name, 0) + nr
            if na:
                alloc_dibujo += na
                alloc_top[f.name] = alloc_top.get(f.name, 0) + na
    m['rand_en_dibujo'] = rand_dibujo
    m['alloc_en_dibujo'] = alloc_dibujo
    m['rand_top'] = dict(sorted(rand_top.items(), key=lambda kv: -kv[1])[:5])
    m['alloc_top'] = dict(sorted(alloc_top.items(), key=lambda kv: -kv[1])[:5])
    m['Immediate'] = sum(f.read_text(encoding='utf-8', errors='ignore').count('SpriteSortMode.Immediate')
                         for f in cs)

    # VFX
    vfx = list((MOD / 'VFX').rglob('*.cs'))
    m['vfx_archivos'] = len(vfx)
    m['vfx_lineas'] = sum(len(f.read_text(encoding='utf-8', errors='ignore').splitlines()) for f in vfx)

    # localización
    import hjson
    loc = {}
    for idioma in ("es-ES", "en-US"):
        d = hjson.loads((MOD.parent / 'Localization' / f'{idioma}_Mods.AethonMod.hjson').read_text(encoding='utf-8'))
        loc[idioma] = aplanar(d)
    es, en = loc['es-ES'], loc['en-US']
    m['loc_es'] = len(es)
    m['loc_en'] = len(en)
    m['loc_faltan_en'] = sum(1 for k in es if k not in en)
    m['loc_faltan_es'] = sum(1 for k in en if k not in es)
    m['loc_en_igual_es'] = sum(1 for k in es if k in en and es[k] == en[k])
    # claves candidatas pedidas por el código (GetTextValue con clave literal)
    pedidas = set()
    for f in cs:
        for kk in re.finditer(r'GetTextValue\(\s*"((?:Mods\.AethonMod\.)?[A-Za-z0-9_.]+)"', f.read_text(encoding='utf-8', errors='ignore')):
            clave = kk.group(1)
            if clave.startswith('Mods.AethonMod.'):
                pedidas.add(clave[len('Mods.AethonMod.'):])
    # v6.185 — LAS BASES DINÁMICAS CONOCIDAS: el código ARMA claves
    # concatenando («Base.» + nombre · «N» + nivel) y la regex solo ve el
    # prefijo — no es una clave muerta, es una BASE viva (el Guardián de las
    # Llaves las resuelve con sus 19 bases dinámicas). El loop las excluye.
    BASES_DINAMICAS = {
        'Altar.', 'Armas.DagaInvocada', 'Armas.EscuadronMejora', 'Cronista.LoreHabla',
        'Duende.Burla', 'Duende.Miedo', 'Hambre.', 'Hambre.Nervioso.', 'Items.',
        'Items.AuraDeJuicio2Item.', 'Items.CetroDagasEterno.', 'Items.FormaEternaItem.',
        'Items.MundoInteriorEternoItem.', 'Items.TrioAlbedrio', 'Pergamino.', 'PilarInterior.IraSellado',
        'Despertar.Estrella.N',   # v6.185 — la forma de la estrella por nivel (N0…N120)
    }
    m['claves_faltantes_es'] = sorted(p for p in pedidas if p not in es and p not in BASES_DINAMICAS)
    m['claves_faltantes_es_n'] = len(m['claves_faltantes_es'])

    # PNGs
    pngs = [p for p in (MOD.parent).rglob('*.png')]
    m['png_n'] = len(pngs)
    invalidos = []
    for p in pngs:
        try:
            with open(p, 'rb') as fh:
                cab = fh.read(8)
            if cab[:4] != b'\x89PNG':
                invalidos.append(str(p))
        except Exception:
            invalidos.append(str(p))
    m['png_invalidos'] = invalidos
    return m

# ---------- comparación ----------

def aplanar(d, prefijo=''):
    pares = {}
    for k, v in d.items():
        clave = (prefijo + '.' + k) if prefijo else k
        if isinstance(v, dict):
            pares.update(aplanar(v, clave))
        else:
            pares[clave] = v
    return pares


def cargar_referencia():
    """PRIORIDAD: la medición ANTERIOR del loop (auto-historia); si aún no
    existe, la auditoría v6.174 (con su metodología vieja)."""
    if SALIDA.exists():
        try:
            ref = json.loads(SALIDA.read_text(encoding='utf-8'))
            ref['_fuente'] = 'medición anterior del loop (loop_retro_v6175.json)'
            ref['_metodologia_propia'] = True
            return ref
        except Exception:
            pass
    ref = {}
    for p in REFERENCIA:
        if p.exists():
            try:
                ref.update(json.loads(p.read_text(encoding='utf-8')))
            except Exception:
                pass
    if ref:
        ref['_fuente'] = 'auditoría v6.174 (metodología vieja: rand/alloc inflados)'
        ref['_metodologia_propia'] = False
        return ref
    return {}


def comparar(actual, ref):
    """Devuelve (lista_de_lineas, hay_regresion)."""
    lineas, regresion = [], False
    guias = [
        # (clave, nombre, meta, es_regresion_si, sube)  sube=True: menor es mejor
        ('catch_vacio', 'catches vacíos', 0, True, True),
        ('rand_en_dibujo', 'azar (Main.rand) en el dibujo', None, True, True),
        ('alloc_en_dibujo', 'reservas new en el dibujo', None, True, True),
        ('loc_faltan_en', 'claves ES que faltan en EN', 0, True, True),
        ('loc_faltan_es', 'claves EN que faltan en ES', 0, True, True),
        ('claves_faltantes_es_n', 'claves pedidas sin texto ES', 0, True, True),
        ('loc_en_igual_es', 'textos EN idénticos al ES', None, False, True),
        ('png_invalidos', 'PNG inválidos', 0, True, True),
        ('cs_archivos', 'archivos .cs', None, False, False),
        ('cs_lineas', 'líneas .cs', None, False, False),
        ('vfx_archivos', 'librerías VFX', None, False, False),
        ('vfx_lineas', 'líneas VFX', None, False, False),
        ('Immediate', 'usos SpriteSortMode.Immediate', None, False, True),
        ('catch_registrado', 'catches que REGISTRAN', None, False, False),
    ]
    met_propia = ref.get('_metodologia_propia', False)
    for clave, nombre, meta, reg, menor_mejor in guias:
        # rand/alloc miden DISTINTO que la auditoría vieja: sin historial
        # propio del loop, la comparación no vale (baseline, no regresión)
        if not met_propia and clave in ('rand_en_dibujo', 'alloc_en_dibujo'):
            ahora_n = actual.get(clave)
            lineas.append(f"  {nombre}: {ahora_n} · BASELINE (primera medición estricta — la de la auditoría no es comparable)")
            continue
        ahora = actual.get(clave)
        if isinstance(ahora, list):
            ahora_n = len(ahora)
        else:
            ahora_n = ahora
        antes = ref.get(clave)
        if isinstance(antes, list):
            antes_n = len(antes)
        else:
            antes_n = antes
        flecha = '—'
        if antes_n is not None and ahora_n is not None:
            if ahora_n < antes_n:
                flecha = '▼ mejora' if menor_mejor else '▼ baja'
            elif ahora_n > antes_n:
                flecha = '▲ sube' if menor_mejor else '▲ crece'
            else:
                flecha = '='
            if reg and menor_mejor and ahora_n > antes_n:
                flecha = '▲▲ REGRESIÓN'
                regresion = True
        meta_txt = f" (meta {meta})" if meta is not None else ""
        lineas.append(f"  {nombre}: {ahora_n}{meta_txt} · antes: {antes_n if antes_n is not None else '—'} {flecha}")
    return lineas, regresion


def main():
    print("=== EL LOOP DE RETROALIMENTACIÓN — medición v6.175 ===")
    actual = medir()
    ref = cargar_referencia()
    lineas, regresion = comparar(actual, ref)
    print(f"referencia: {ref.get('_fuente', 'ninguna (primera medición)')}")
    for l in lineas:
        print(l)
    actual['_fuente'] = 'loop_retro_v6175.py'
    SALIDA.write_text(json.dumps(actual, ensure_ascii=False, indent=1), encoding='utf-8')
    print(f"\nguardado: {SALIDA}")
    if regresion:
        print("RESULTADO: ✗ HAY REGRESIÓN — el release NO pasa hasta que se arregle")
        return 1
    print("RESULTADO: ✓ sin regresiones")
    return 0


if __name__ == '__main__':
    sys.exit(main())

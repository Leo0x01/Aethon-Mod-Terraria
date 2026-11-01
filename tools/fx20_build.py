#!/usr/bin/env python3
# =============================================================================
# fx20_build.py — Constructor de efectos fx_2_0 (binario D3DX9 01 09 FF FE)
#
# Ensambla un .fxc con el layout EXACTO que emite fxc /T fx_2_0 (validado
# byte a byte contra los tres .fxc de la casa: RadialShine, SunShader y
# BlackHoleDistortion) y que FNA/tModLoader leen en runtime vía MojoShader
# (MOJOSHADER_compileEffect).
#
# v6.78 — LA EXTENSIÓN DE CALOR: el dialecto crece para el shader de
# distorsión de calor (y para siempre):
#   · float2 / float3 / float4      → records VECTOR (ground truth: el
#     BlackHoleDistortion ya usaba float2 zoom / aspectRatioCorrectionFactor
#     — leídos por fxc como C_VECTOR con N columnas).
#   · float nombre[N] / float2 nombre[N] → ARRAYS de escalares/vectores
#     (ground truth: sourceRadii[5] SCALAR elems=5, sourcePositions[5]
#     VECTOR elems=5 con 10 floats de init).
#   · sampler nombre : register(sN); → SAMPLER SIN ESTADO (ground truth: el
#     baseTexture del BlackHole y los TRES del SunShader — el sampler que la
#     cadena de filtros de vanilla alimenta por el SLOT del dispositivo: sin
#     parámetro de textura, sin tabla de objetos pequeños).
#   · AddressU / AddressV en el sampler_state (CLAMP para los campos de
#     pantalla, WRAP para los ruidos que hacen scroll).
#   · --solo-ps → efectos SOLO pixel shader (el layout de los filtros de
#     Filters.Scene: 1 estado de pase PIXELSHADER, 1 objeto grande, cero
#     vertex shader — SpriteBatch conserva su propio VS en Immediate).
# La fórmula del tercer conteo de cabecera se corrigió con los ground
# truths: técnicas + pases + TODOS los samplers (el "2 + num_texturas" solo
# valía cuando cada sampler tenía SU textura).
#
# Uso:
#   python3 fx20_build.py --fx HazConcentradoShader.fx \
#       --vs vs.bin --ps ps.bin --out HazConcentradoShader.fxc
#   python3 fx20_build.py --fx CalorDistorsionShader.fx --solo-ps \
#       --ps ps.bin --out CalorDistorsionShader.fxc
#
# El script parsea las declaraciones del .fx (parámetros, samplers con sus
# estados, técnica/pase) y emite:
#   [0]  token de versión 0xFEFF0901
#   [4]  tamaño de la región base
#   [8]  región base: strings + records de tipo + records de valor
#   [..] conteos, records de parámetros/técnica/pase/estados,
#        tabla de objetos pequeños (nombres de mapeo de texturas),
#        tabla de objetos grandes (bytecode vs_3_0/ps_3_0)
#
# Convenciones críticas (extraídas de los ground truths y del parser):
#   - readstring: [u32 len INCLUDING nul][bytes + \0]
#   - records alineados a 4
#   - estado de pase PIXELSHADER=147, VERTEXSHADER=146 (numeración MojoShader)
#   - estados de sampler: TEXTURE=4, ADDRESSU/V=5/6, MAG/MIN/MIPFILTER=9/10/11
#     con valores D3DX: WRAP=1, CLAMP=3, LINEAR=2
#   - los objetos de textura DEBEN tener nombre de mapeo en la tabla pequeña
#     (FNA lo usa para el samplerMap: sampler -> parámetro de textura)
#   - los samplers sin estado (register-only) NO van a la tabla pequeña
# =============================================================================
import argparse, re, struct

# --- enums (mojoshader.h == D3DX) ---
T_VOID, T_BOOL, T_INT, T_FLOAT, T_STRING, T_TEXTURE = 0, 1, 2, 3, 4, 5
T_SAMPLER, T_SAMPLER2D, T_PIXELSHADER, T_VERTEXSHADER = 10, 12, 15, 16
C_SCALAR, C_VECTOR, C_MROWS, C_MCOLS, C_OBJECT, C_STRUCT = 0, 1, 2, 3, 4, 5
RS_VERTEXSHADER, RS_PIXELSHADER = 146, 147
SAMP_TEXTURE, SAMP_ADDRU, SAMP_ADDRV = 4, 5, 6
SAMP_MAG, SAMP_MIN, SAMP_MIP = 9, 10, 11
ADDR_WRAP, ADDR_MIRROR, ADDR_CLAMP, ADDR_BORDER = 1, 2, 3, 4
FILT_LINEAR = 2


class Base:
    """Región base: strings + records, con offsets base-relativos."""

    def __init__(self):
        self.buf = bytearray()
        self._strings = {}
        # convención D3FX: el offset 0 de la región base es el centinela NULO
        # (los ground truths arrancan con 4 bytes cero) — ningún string ni
        # record puede vivir ahí, o readstring lo leería como "sin nombre"
        self.buf += b"\0\0\0\0"

    def _align(self):
        while len(self.buf) % 4:
            self.buf.append(0)

    def string(self, s):
        """[u32 len][bytes + NUL] — len INCLUYE el NUL (convención D3DX)."""
        if s in self._strings:
            return self._strings[s]
        self._align()
        off = len(self.buf)
        data = s.encode("utf-8") + b"\0"
        self.buf += struct.pack("<I", len(data)) + data
        self._strings[s] = off
        return off

    def record(self, *u32s):
        """Record de u32s alineado a 4; devuelve su offset."""
        self._align()
        off = len(self.buf)
        self.buf += struct.pack("<%dI" % len(u32s), *u32s)
        return off

    def raw(self, data):
        self._align()
        off = len(self.buf)
        self.buf += data
        return off


# --- el dialecto v6.78: qué tipos reconoce el parser ----------------------
# ("valor", nombre, tipo) donde tipo = ("float", columnas, filas, elementos)
#   ("float", 1, 1, 0)  → float    escalar
#   ("float", 2, 1, 0)  → float2   vector
#   ("float", 3, 1, 0)  → float3   vector
#   ("float", 4, 1, 0)  → float4   vector
#   ("float", 1, 1, N)  → float  nombre[N]  (array de escalares)
#   ("float", 2, 1, N)  → float2 nombre[N]  (array de vectores)
# ("matrix", nombre)                  → float4x4 / matrix
# ("tex", nombre)                      → texture nombre;
# ("sampler", nombre, textura|None, addrU, addrV) → sampler con estados
# ("sampler-libre", nombre)            → sampler nombre : register(sN);


def parse_fx(path):
    """Extrae del .fx: params de valor, texturas+samplers, técnica/pase."""
    src = open(path, encoding="utf-8").read()
    # quita comentarios
    src_nc = re.sub(r"//[^\n]*", "", src)
    src_nc = re.sub(r"/\*.*?\*/", "", src_nc, flags=re.S)

    # respeta el ORDEN de declaración (los slots de sampler se asignan en
    # orden de aparición — verificado contra HazConcentrado y SunShader)
    order = []
    # v6.78 — SOLO EL NIVEL SUPERIOR: los parámetros del efecto viven ANTES
    # de la primera función/técnica — sin este corte, las variables LOCALES
    # de los shaders (p. ej. "float3 col;") entran a la tabla de parámetros
    # como si fueran uniforms y MojoShader se atraganta (la lección del
    # CalorDistorsion: el primer .fx de la casa con locales tipadas).
    cuerpo = re.search(r"^\s*[\w]+\s+\w+\s*\(", src_nc, re.M)
    tech_m = re.search(r"^\s*technique\s+", src_nc, re.M)
    fin = min(m.start() for m in (cuerpo, tech_m) if m)
    decl_region = src_nc[:fin]
    decl = re.compile(
        r"^\s*(float[234]?|matrix|float4x4)\s+(\w+)\s*(?:\[(\d+)\])?\s*;",
        re.M)
    for m in decl.finditer(decl_region):
        tipo, nombre, elems = m.group(1), m.group(2), m.group(3)
        if tipo in ("matrix", "float4x4"):
            order.append(("matrix", nombre))
            continue
        cols = 1 if tipo == "float" else int(tipo[-1])
        nelem = int(elems) if elems else 0
        order.append(("valor", nombre, ("float", cols, 1, nelem)))
    for m in re.finditer(r"^\s*texture\s+(\w+)\s*;", decl_region, re.M):
        order.append(("tex", m.group(1)))

    # v6.78 — TODOS los samplers en UN solo pase y en ORDEN DE FUENTE: el
    # slot (s0, s1, s2…) se asigna por orden de aparición (verificado contra
    # HazConcentrado: texCarne→s0, texSegunda→s1…) — mezclar dos bucles
    # (con-estado y register-only) los reordenaría y el shader leería el
    # sampler equivocado.
    for m in re.finditer(
            r"sampler\s+(\w+)\s*(?::\s*register\s*\(\s*s\d+\s*\)\s*|=\s*sampler_state\s*\{(.*?)\});",
            src_nc, re.S):
        name, body = m.group(1), m.group(2)
        if body is None:
            # sampler SIN estado (register-only): lo alimenta el SLOT del
            # dispositivo (la cadena de filtros de vanilla dibuja la
            # pantalla en s0)
            order.append(("sampler-libre", name))
            continue
        tex = re.search(r"texture\s*=\s*<\s*(\w+)\s*>", body)
        # v6.78 — el modo de direccionamiento se declara (WRAP por defecto,
        # el de siempre; CLAMP para los campos que viven en la pantalla)
        au = re.search(r"AddressU\s*=\s*(\w+)", body)
        av = re.search(r"AddressV\s*=\s*(\w+)", body)
        addr = {"WRAP": ADDR_WRAP, "MIRROR": ADDR_MIRROR,
                "CLAMP": ADDR_CLAMP, "BORDER": ADDR_BORDER}
        order.append(("sampler", name,
                      tex.group(1) if tex else None,
                      addr.get(au.group(1), ADDR_WRAP) if au else ADDR_WRAP,
                      addr.get(av.group(1), ADDR_WRAP) if av else ADDR_WRAP))

    tech = re.search(r"technique\s+(\w+)\s*\{", src_nc)
    pas = re.search(r"pass\s+(\w+)\s*\{", src_nc)
    assert tech and pas, "el .fx no declara técnica/pase"
    return order, tech.group(1), pas.group(1)


def build(fx_path, vs_blob, ps_blob, out_path, solo_ps=False):
    order, tech_name, pass_name = parse_fx(fx_path)
    if solo_ps:
        assert vs_blob is None, "--solo-ps no admite vertex shader"

    base = Base()
    objects = {}         # nombre_textura -> índice de objeto
    next_object = 1      # el objeto 0 queda sin uso (convención de los ground truths)
    num_samplers = 0

    # ---------- records de tipo + valor de cada parámetro ----------
    params = []          # (typeoff, valoff)
    for entry in order:
        kind = entry[0]
        if kind == "valor":
            nombre, (tipo, cols, rows, nelem) = entry[1], entry[2]
            noff = base.string(nombre)
            cls = C_SCALAR if cols == 1 else C_VECTOR
            toff = base.record(T_FLOAT, cls, noff, 0, nelem, cols, rows)
            # init en CERO (el C# los setea en runtime): ncols·nrows·nelem floats
            voff = base.record(*([0] * (cols * rows * max(nelem, 1))))
            params.append((toff, voff))
        elif kind == "matrix":
            nombre = entry[1]
            noff = base.string(nombre)
            toff = base.record(T_FLOAT, C_MCOLS, noff, 0, 0, 4, 4)  # cols, rows
            voff = base.record(0, 0, 0, 0)                          # 4 vec4 init
            base.record(0, 0, 0, 0)
            base.record(0, 0, 0, 0)
            base.record(0, 0, 0, 0)
            params.append((toff, voff))
        elif kind == "tex":
            nombre = entry[1]
            noff = base.string(nombre)
            toff = base.record(T_TEXTURE, C_OBJECT, noff, 0, 0)
            idx = next_object
            next_object += 1
            objects[nombre] = idx
            voff = base.record(idx)
            params.append((toff, voff))
        elif kind == "sampler":
            name, texname, au, av = entry[1], entry[2], entry[3], entry[4]
            noff = base.string(name)
            toff = base.record(T_SAMPLER2D, C_OBJECT, noff, 0, 0)
            # --- estados del sampler (6): texture, addressU, addressV, mag, min, mip
            st_entries = []
            # estado TEXTURE -> valor = índice del objeto textura
            t_to = base.record(T_TEXTURE, C_OBJECT, 0, 0, 0)
            t_voff = base.record(objects[texname])
            st_entries.append((SAMP_TEXTURE, t_to, t_voff))
            # estados escalares (int): ADDRESSU/V declarados, filtros LINEAR
            for stype, val in ((SAMP_ADDRU, au), (SAMP_ADDRV, av),
                               (SAMP_MAG, FILT_LINEAR), (SAMP_MIN, FILT_LINEAR),
                               (SAMP_MIP, FILT_LINEAR)):
                s_to = base.record(T_INT, C_SCALAR, 0, 0, 0, 1, 1)
                s_voff = base.record(val)
                st_entries.append((stype, s_to, s_voff))
            # record de valor del sampler: [numstates][estado: stype, FIXME, toff, voff]
            voff = base.record(len(st_entries))
            for stype, sto, svo in st_entries:
                base.record(stype, 0, sto, svo)
            params.append((toff, voff))
            num_samplers += 1
        elif kind == "sampler-libre":
            # v6.78 — SIN estados: el record de valor es SOLO el conteo en 0
            # (ground truth: baseTexture del BlackHole, "OBJECT SAMPLER
            # elems=0" — el slot lo llena el dispositivo, no un parámetro)
            name = entry[1]
            noff = base.string(name)
            toff = base.record(T_SAMPLER, C_OBJECT, noff, 0, 0)
            voff = base.record(0)
            params.append((toff, voff))
            num_samplers += 1

    # ---------- estados del pase ----------
    ps_obj = next_object
    next_object += 1
    ps_to = base.record(T_PIXELSHADER, C_OBJECT, 0, 0, 0)
    ps_vo = base.record(ps_obj)
    pass_states = [(RS_PIXELSHADER, ps_to, ps_vo)]
    if not solo_ps:
        vs_obj = next_object
        next_object += 1
        vs_to = base.record(T_VERTEXSHADER, C_OBJECT, 0, 0, 0)
        vs_vo = base.record(vs_obj)
        pass_states.append((RS_VERTEXSHADER, vs_to, vs_vo))

    tech_off = base.string(tech_name)
    pass_off = base.string(pass_name)

    # ---------- ensambla el flujo principal ----------
    out = bytearray()
    out += struct.pack("<I", 0xFEFF0901)          # token de versión
    out += struct.pack("<I", len(base.buf))       # tamaño de la región base
    out += base.buf

    num_texturas = sum(1 for e in order if e[0] == "tex")
    total_objetos = next_object                    # 0 sin uso + texturas + shaders
    # el tercer conteo (la fórmula de los ground truths): técnicas + pases +
    # TODOS los samplers (con estado o no — BlackHole: 1+1+1=3, Sun: 1+1+3=5)
    conteo_samplers_tecnicas = 1 + 1 + num_samplers
    out += struct.pack("<4I", len(params), 1, conteo_samplers_tecnicas, total_objetos)
    # ^ numparams, numtechniques, samplers+téc+pases, numobjects
    for toff, voff in params:
        out += struct.pack("<4I", toff, voff, 0, 0)     # flags=0, numannos=0
    out += struct.pack("<3I", tech_off, 0, 1)           # técnica: nombre, annos, 1 pase
    out += struct.pack("<3I", pass_off, 0, len(pass_states))  # pase: nombre, annos, estados
    for stype, sto, svo in pass_states:
        out += struct.pack("<4I", stype, 0, sto, svo)

    # ---------- tabla de objetos pequeños: mapping names de las texturas ----------
    tex_entries = [(objects[e[1]], (e[1] + "\0").encode("utf-8"))
                   for e in order if e[0] == "tex"]
    grandes = [ps_blob] if solo_ps else [ps_blob, vs_blob]
    out += struct.pack("<2I", len(tex_entries), len(grandes))  # pequeños, grandes

    for idx, data in tex_entries:
        out += struct.pack("<II", idx, len(data))
        out += data
        # v6.78 — LA CURA DEL CORRIMIENTO: MojoShader avanza cada bloque con
        # blocklen = ceil4(length) DESDE EL INICIO DEL DATO (mojoshader
        # readsmallobjects/readlargeobjects) — NO «hasta alinear out». Si el
        # dato arranca desalineado (la región base puede terminar en bytes
        # impares — ground truth: 477), las dos estrategias DIVERGEN y el
        # registro siguiente queda corrido (el segfault del CalorDistorsion:
        # el header del objeto grande leído 2 bytes tarde = idx=0xFFFF0000).
        out += b"\0" * ((-len(data)) % 4)

    # ---------- tabla de objetos grandes: el bytecode de los shaders ----------
    # (técnica, índice_pase, FIXME=-1, estado, tipo=0 shader, length, bytes)
    # PS = estado 0 del pase; VS = estado 1 (el orden de pass_states)
    for (state_idx, blob) in enumerate(grandes):
        out += struct.pack("<6I", 0, 0, 0xFFFFFFFF, state_idx, 0, len(blob))
        out += blob
        out += b"\0" * ((-len(blob)) % 4)

    open(out_path, "wb").write(out)
    print("OK %s: %d bytes | params=%d objetos=%d (texturas=%d, PS=%d%s) samplers=%d"
          % (out_path, len(out), len(params), total_objetos,
             num_texturas, ps_obj,
             "" if solo_ps else ", VS=%d" % vs_obj, num_samplers))
    return out_path


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--fx", required=True)
    ap.add_argument("--vs", default=None, help="bytecode vs_3_0 (d3dbc)")
    ap.add_argument("--ps", required=True, help="bytecode ps_3_0 (d3dbc)")
    ap.add_argument("--out", required=True)
    ap.add_argument("--solo-ps", action="store_true",
                    help="efecto SOLO pixel shader (filtros de cadena)")
    a = ap.parse_args()
    build(a.fx, open(a.vs, "rb").read() if a.vs else None,
          open(a.ps, "rb").read(), a.out, solo_ps=a.solo_ps)

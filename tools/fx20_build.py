#!/usr/bin/env python3
# =============================================================================
# fx20_build.py — Constructor de efectos fx_2_0 (binario D3DX9 01 09 FF FE)
#
# Ensambla un .fxc con el layout EXACTO que emite fxc /T fx_2_0 (validado
# byte a byte contra los tres .fxc de la casa: RadialShine, SunShader y
# BlackHoleDistortion) y que FNA/tModLoader leen en runtime vía MojoShader
# (MOJOSHADER_compileEffect).
#
# Uso:
#   python3 fx20_build.py --fx HazConcentradoShader.fx \
#       --vs vs.bin --ps ps.bin --out HazConcentradoShader.fxc
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
#     con valores D3DX: WRAP=1, LINEAR=2
#   - los objetos de textura DEBEN tener nombre de mapeo en la tabla pequeña
#     (FNA lo usa para el samplerMap: sampler -> parámetro de textura)
# =============================================================================
import argparse, re, struct

# --- enums (mojoshader.h == D3DX) ---
T_VOID, T_BOOL, T_INT, T_FLOAT, T_STRING, T_TEXTURE = 0, 1, 2, 3, 4, 5
T_SAMPLER, T_SAMPLER2D, T_PIXELSHADER, T_VERTEXSHADER = 10, 12, 15, 16
C_SCALAR, C_VECTOR, C_MROWS, C_MCOLS, C_OBJECT, C_STRUCT = 0, 1, 2, 3, 4, 5
RS_VERTEXSHADER, RS_PIXELSHADER = 146, 147
SAMP_TEXTURE, SAMP_ADDRU, SAMP_ADDRV = 4, 5, 6
SAMP_MAG, SAMP_MIN, SAMP_MIP = 9, 10, 11
ADDR_WRAP, FILT_LINEAR = 1, 2


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


def parse_fx(path):
    """Extrae del .fx: params de valor, texturas+samplers, técnica/pase."""
    src = open(path, encoding="utf-8").read()
    # quita comentarios
    src_nc = re.sub(r"//[^\n]*", "", src)
    src_nc = re.sub(r"/\*.*?\*/", "", src_nc, flags=re.S)

    order = []          # ("valor", nombre, tipo) | ("tex", nombre) | ("sampler", nombre, textura)
    # matrices y escalares
    for m in re.finditer(r"^\s*(matrix|float4x4|float)\s+(\w+)\s*;", src_nc, re.M):
        order.append(("valor", m.group(2), "matrix" if m.group(1) != "float" else "float"))
    # texturas
    for m in re.finditer(r"^\s*texture\s+(\w+)\s*;", src_nc, re.M):
        order.append(("tex", m.group(1)))
    # samplers con estados (textura referida + AddressU/V + filtros)
    for m in re.finditer(r"sampler\s+(\w+)\s*=\s*sampler_state\s*\{(.*?)\};",
                         src_nc, re.S):
        name, body = m.group(1), m.group(2)
        tex = re.search(r"texture\s*=\s*<\s*(\w+)\s*>", body)
        order.append(("sampler", name, tex.group(1) if tex else None))
    # técnica y pase
    tech = re.search(r"technique\s+(\w+)\s*\{", src_nc)
    pas = re.search(r"pass\s+(\w+)\s*\{", src_nc)
    assert tech and pas, "el .fx no declara técnica/pase"
    return order, tech.group(1), pas.group(1)


def build(fx_path, vs_blob, ps_blob, out_path):
    order, tech_name, pass_name = parse_fx(fx_path)

    base = Base()
    objects = {}         # nombre_textura -> índice de objeto
    next_object = 1      # el objeto 0 queda sin uso (convención de los ground truths)

    # ---------- records de tipo + valor de cada parámetro ----------
    params = []          # (typeoff, valoff)
    for entry in order:
        kind = entry[0]
        if kind == "valor":
            name, tipo = entry[1], entry[2]
            noff = base.string(name)
            if tipo == "matrix":
                toff = base.record(T_FLOAT, C_MCOLS, noff, 0, 0, 4, 4)  # cols, rows
                voff = base.record(0, 0, 0, 0)                          # 4 vec4 init
                base.record(0, 0, 0, 0)
                base.record(0, 0, 0, 0)
                base.record(0, 0, 0, 0)
            else:
                toff = base.record(T_FLOAT, C_SCALAR, noff, 0, 0, 1, 1)
                voff = base.record(0)
            params.append((toff, voff))
        elif kind == "tex":
            name = entry[1]
            noff = base.string(name)
            toff = base.record(T_TEXTURE, C_OBJECT, noff, 0, 0)
            idx = next_object
            next_object += 1
            objects[name] = idx
            voff = base.record(idx)
            params.append((toff, voff))
        elif kind == "sampler":
            name, texname = entry[1], entry[2]
            noff = base.string(name)
            toff = base.record(T_SAMPLER2D, C_OBJECT, noff, 0, 0)
            # --- estados del sampler (6): texture, addressU, addressV, mag, min, mip
            st_entries = []
            # estado TEXTURE -> valor = índice del objeto textura
            t_to = base.record(T_TEXTURE, C_OBJECT, 0, 0, 0)
            t_voff = base.record(objects[texname])
            st_entries.append((SAMP_TEXTURE, t_to, t_voff))
            # estados escalares (int): ADDRESSU/V=WRAP, filtros=LINEAR
            for stype, val in ((SAMP_ADDRU, ADDR_WRAP), (SAMP_ADDRV, ADDR_WRAP),
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

    # ---------- estados del pase (pixel y vertex shader) ----------
    ps_obj, vs_obj = next_object, next_object + 1
    next_object += 2
    ps_to = base.record(T_PIXELSHADER, C_OBJECT, 0, 0, 0)
    ps_vo = base.record(ps_obj)
    vs_to = base.record(T_VERTEXSHADER, C_OBJECT, 0, 0, 0)
    vs_vo = base.record(vs_obj)
    pass_states = [(RS_PIXELSHADER, ps_to, ps_vo), (RS_VERTEXSHADER, vs_to, vs_vo)]

    tech_off = base.string(tech_name)
    pass_off = base.string(pass_name)

    # ---------- ensambla el flujo principal ----------
    out = bytearray()
    out += struct.pack("<I", 0xFEFF0901)          # token de versión
    out += struct.pack("<I", len(base.buf))       # tamaño de la región base
    out += base.buf

    num_texturas = sum(1 for e in order if e[0] == "tex")
    total_objetos = next_object                    # 0 sin uso + texturas + 2 shaders
    out += struct.pack("<4I", len(params), 1, 2 + num_texturas, total_objetos)
    # ^ numparams, numtechniques, FIXME(ignorado: tech+pases+samplers), numobjects
    for toff, voff in params:
        out += struct.pack("<4I", toff, voff, 0, 0)     # flags=0, numannos=0
    out += struct.pack("<3I", tech_off, 0, 1)           # técnica: nombre, annos, 1 pase
    out += struct.pack("<3I", pass_off, 0, len(pass_states))  # pase: nombre, annos, estados
    for stype, sto, svo in pass_states:
        out += struct.pack("<4I", stype, 0, sto, svo)

    # ---------- tabla de objetos pequeños: mapping names de las texturas ----------
    tex_entries = [(objects[e[1]], (e[1] + "\0").encode("utf-8"))
                   for e in order if e[0] == "tex"]
    out += struct.pack("<2I", len(tex_entries), 2)      # pequeños, grandes

    def block(data):
        """Entrada de objeto: (index, length, data) con padding a 4."""
        b = struct.pack("<II", idx, len(data)) if False else None
        return data

    for idx, data in tex_entries:
        out += struct.pack("<II", idx, len(data))
        out += data
        while len(out) % 4:
            out += b"\0"

    # ---------- tabla de objetos grandes: el bytecode de los shaders ----------
    # (técnica, índice_pase, FIXME=-1, estado, tipo=0 shader, length, bytes)
    for (state_idx, blob) in ((0, ps_blob), (1, vs_blob)):
        out += struct.pack("<6I", 0, 0, 0xFFFFFFFF, state_idx, 0, len(blob))
        out += blob
        while len(out) % 4:
            out += b"\0"

    open(out_path, "wb").write(out)
    print("OK %s: %d bytes | params=%d objetos=%d (texturas=%d, PS=%d, VS=%d)"
          % (out_path, len(out), len(params), total_objetos,
             num_texturas, ps_obj, vs_obj))
    return out_path


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--fx", required=True)
    ap.add_argument("--vs", required=True, help="bytecode vs_3_0 (d3dbc)")
    ap.add_argument("--ps", required=True, help="bytecode ps_3_0 (d3dbc)")
    ap.add_argument("--out", required=True)
    a = ap.parse_args()
    build(a.fx, open(a.vs, "rb").read(), open(a.ps, "rb").read(), a.out)

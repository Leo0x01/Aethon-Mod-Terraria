#!/usr/bin/env python3
# =============================================================================
# fx20_parse.py — Parser del formato de efectos fx_2_0 (binario D3DX9 01 09 FF FE)
# Implementación Python fiel de MOJOSHADER_compileEffect (mojoshader_effects.c),
# el MISMO código con el que FNA/tModLoader lee los .fxc en runtime.
# Uso: python3 fx20_parse.py archivo.fxc [--bytes]
# Decodifica: cabecera, parámetros (tipos/valores/samplers), técnicas, pases,
# estados, tablas de objetos pequeños/grandes y el bytecode SM3 embebido.
# =============================================================================
import struct, sys

# --- enums (mojoshader.h) ---
SYMCLASS = {0: "SCALAR", 1: "VECTOR", 2: "MATRIX_ROWS", 3: "MATRIX_COLUMNS", 4: "OBJECT", 5: "STRUCT"}
SYMTYPE = {0: "VOID", 1: "BOOL", 2: "INT", 3: "FLOAT", 4: "STRING", 5: "TEXTURE",
           6: "TEXTURE1D", 7: "TEXTURE2D", 8: "TEXTURE3D", 9: "TEXTURECUBE",
           10: "SAMPLER", 11: "SAMPLER1D", 12: "SAMPLER2D", 13: "SAMPLER3D",
           14: "SAMPLERCUBE", 15: "PIXELSHADER", 16: "VERTEXSHADER",
           17: "PIXELFRAGMENT", 18: "VERTEXFRAGMENT", 19: "UNSUPPORTED"}
SAMP = {0: "UNKNOWN0", 1: "UNKNOWN1", 2: "UNKNOWN2", 3: "UNKNOWN3", 4: "TEXTURE",
        5: "ADDRESSU", 6: "ADDRESSV", 7: "ADDRESSW", 8: "BORDERCOLOR", 9: "MAGFILTER",
        10: "MINFILTER", 11: "MIPFILTER", 12: "MIPMAPLODBIAS", 13: "MAXMIPLEVEL",
        14: "MAXANISOTROPY", 15: "SRGBTEXTURE", 16: "ELEMENTINDEX", 17: "DMAPOFFSET"}

def u32(b, o):
    return struct.unpack_from("<I", b, o)[0]

def f32s(b, o, n):
    return list(struct.unpack_from("<%df" % n, b, o))

def rdstring(b, off):
    if off is None or off == 0:
        return None
    ln = u32(b, off)
    if ln == 0:
        return None
    return b[off + 4: off + 4 + ln - 1].decode("utf-8", "replace")

class R:
    def __init__(self, b, o, end):
        self.b, self.o, self.end = b, o, end
    def u32(self):
        v = u32(self.b, self.o); self.o += 4; return v

def read_value(b, base, typeoff, valoff, depth=0):
    """readvalue(): devuelve dict con tipo/clase/nombre/semantic/valor."""
    ind = "  " * depth
    t = R(b, base + typeoff, len(b))
    ptype = t.u32(); vclass = t.u32(); nameoff = t.u32(); semoff = t.u32(); numelem = t.u32()
    out = {"type": SYMTYPE.get(ptype, ptype), "class": SYMCLASS.get(vclass, vclass),
           "name": rdstring(b, base + nameoff) if nameoff else None,
           "semantic": rdstring(b, base + semoff) if semoff else None,
           "elements": numelem, "raw_type": ptype}
    if vclass in (0, 1, 2, 3):  # escalar/vector/matriz
        out["columns"] = t.u32(); out["rows"] = t.u32()
        ncols, nrows, nel = out["columns"], out["rows"], max(numelem, 1)
        out["init"] = f32s(b, base + valoff, ncols * nrows * nel)
    elif vclass == 4:  # objeto
        if ptype in (10, 11, 12, 13, 14):  # sampler
            v = R(b, base + valoff, len(b))
            nstates = v.u32()
            states = []
            for _ in range(nstates):
                stype = v.u32(); _fix = v.u32(); sttoff = v.u32(); svoff = v.u32()
                sv = read_value(b, base, sttoff, svoff, depth + 1)
                states.append({"state": SAMP.get(stype & ~0xA0, stype & ~0xA0),
                               "raw": hex(stype), "value": sv})
            out["states"] = states
        else:  # textura/string/shader: índice de objeto
            out["object_index"] = u32(b, base + valoff)
    return out

def parse(path):
    b = open(path, "rb").read()
    print("== %s (%d bytes)" % (path, len(b)))
    # versión: token SWAP32 -> magic 0xFEFF, maj 9, min 1
    token = struct.unpack_from("<I", b, 0)[0]  # SWAP32 es identidad en LE: 0xFEFF0901
    magic = (token >> 16) & 0xFFFF; maj = (token >> 8) & 0xFF; mn = token & 0xFF
    assert magic == 0xFEFF and maj == 9 and mn == 1, "no es fx_2_0: %04x %02x %02x" % (magic, maj, mn)
    base = 8
    offset = u32(b, 4)
    print("version: fx_2_0 | base-region: %d bytes" % offset)
    r = R(b, base + offset, len(b))
    numparams = r.u32(); numtech = r.u32(); fixme = r.u32(); numobj = r.u32()
    print("params=%d técnicas=%d FIXME=%d objetos=%d" % (numparams, numtech, fixme, numobj))
    params = []
    for i in range(numparams):
        toff = r.u32(); voff = r.u32(); flags = r.u32(); nannos = r.u32()
        for _ in range(nannos):
            r.u32(); r.u32()
        v = read_value(b, base, toff, voff)
        params.append(v)
        print("  param[%d] %s : %s %s elems=%s %s" % (
            i, v["name"], v["class"], v["type"], v.get("elements"),
            ("init=%s" % v["init"]) if "init" in v else
            (("obj#%s" % v["object_index"]) if "object_index" in v else "")))
        for st in v.get("states", []):
            sv = st["value"]
            print("      estado %s (raw %s) -> %s %s %s" % (
                st["state"], st["raw"], sv["type"], sv["name"],
                ("obj#%s" % sv["object_index"]) if "object_index" in sv else sv.get("init", "")))
    techs = []
    for ti in range(numtech):
        noff = r.u32(); nannos = r.u32(); npass = r.u32()
        for _ in range(nannos):
            r.u32(); r.u32()
        print("  técnica[%d] %s con %d pases" % (ti, rdstring(b, base + noff), npass))
        for pi in range(npass):
            pno = r.u32(); pann = r.u32(); nst = r.u32()
            for _ in range(pann):
                r.u32(); r.u32()
            print("    pase[%d] %s con %d estados" % (pi, rdstring(b, base + pno), nst))
            for si in range(nst):
                stt = r.u32(); fix = r.u32(); stoff = r.u32(); svoff = r.u32()
                sv = read_value(b, base, stoff, svoff)
                print("      estado RS=%d (FIXME=%d) -> %s %s obj#%s" % (
                    stt, fix, sv["type"], sv["name"], sv.get("object_index")))
    nsmall = r.u32(); nlarge = r.u32()
    print("objetos pequeños=%d grandes=%d" % (nsmall, nlarge))
    for i in range(nsmall):
        idx = r.u32(); ln = r.u32()
        data = b[r.o: r.o + ln]
        blk = (ln + 3) - ((ln - 1) % 4)
        print("  pequeño[%d] índice=%d len=%d: %r" % (i, idx, ln, data[:48]))
        r.o += blk
    for i in range(nlarge):
        tech = r.u32(); idx = r.u32(); fix = r.u32(); state = r.u32(); typ = r.u32(); ln = r.u32()
        data = b[r.o: r.o + ln]
        blk = (ln + 3) - ((ln - 1) % 4)
        ver = struct.unpack_from("<I", data, 0)[0] if ln >= 4 else 0
        print("  grande[%d] técnica=%d índice=%d FIXME=%d estado=%d tipo=%d len=%d ver=0x%08X" % (
            i, tech, idx, fix, state, typ, ln, ver))
        if ln:
            print("    bytes: %s%s" % (data[:32].hex(" "), "..." if ln > 32 else ""))
        r.o += blk
    print("consumido: %d / %d bytes" % (r.o, len(b)))

if __name__ == "__main__":
    for p in sys.argv[1:]:
        if p == "--bytes":
            continue
        parse(p)
        print()

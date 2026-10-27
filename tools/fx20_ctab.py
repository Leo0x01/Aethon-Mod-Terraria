#!/usr/bin/env python3
# =============================================================================
# fx20_ctab.py — Dumper de la tabla de constantes (CTAB) de un bytecode SM1-3.
# Layout según parse_constant_table()/parse_ctab_typeinfo() de mojoshader.c
# (el parser que usa FNA/tModLoader en runtime).
# Uso: python3 fx20_ctab.py archivo1.bin [archivo2.bin ...]
# =============================================================================
import struct, sys

CTAB_SIZE = 28

def rd_str(b, off):
    end = b.index(b"\0", off)
    return b[off:end].decode("utf-8", "replace")

def parse_ctab(path):
    b = open(path, "rb").read()
    print("== %s (%d bytes) ver=0x%08X" % (path, len(b), struct.unpack_from("<I", b, 0)[0]))
    # busca el token de comentario con "CTAB"
    o = 4
    while o + 8 <= len(b):
        tok = struct.unpack_from("<I", b, o)[0]
        if (tok & 0xFFFF) == 0xFFFE:  # comentario (opcode en los 16 bits BAJOS)
            size = (tok >> 16) * 4
            if b[o + 4: o + 8] == b"CTAB":
                dump(b, o + 4, size)
                return
            o += 4 + size
        else:
            o += 4
    print("  (sin CTAB)")

def dump(b, base, size):
    # base apunta a "CTAB"; la tabla empieza tras el magic
    t = base + 4
    hdr = struct.unpack_from("<7I", b, t)
    hsize, creator, version, constants, constinfo = hdr[0], hdr[1], hdr[2], hdr[3], hdr[4]
    target = hdr[6]
    start = t
    print("  CTAB hdr: size=%d version=0x%08X constants=%d target=%r" % (
        hsize, version, constants, rd_str(b, t + target)))
    for i in range(constants):
        p = t + constinfo + i * 20
        name, regset, regidx, regcnt, resv, typeinfo, defval = struct.unpack_from(
            "<IHHHHII", b, p)
        ti = t + typeinfo
        ty, kl, rows, cols, members, minfo = struct.unpack_from("<HHHHHI", b, ti)
        KLASS = {0: "SCALAR", 1: "VECTOR", 2: "MATRIX_ROWS", 3: "MATRIX_COLS",
                 4: "OBJECT", 5: "STRUCT"}
        TYPE = {0: "VOID", 1: "BOOL", 2: "INT", 3: "FLOAT", 4: "STRING",
                5: "TEXTURE", 6: "TEXTURE1D", 7: "TEXTURE2D", 8: "TEXTURE3D",
                9: "TEXTURECUBE", 10: "SAMPLER", 11: "SAMPLER1D", 12: "SAMPLER2D",
                13: "SAMPLER3D", 14: "SAMPLERCUBE"}
        REGSET = {0: "BOOL", 1: "INT", 2: "FLOAT", 3: "SAMPLER"}
        print("  sym[%d] %-18r regset=%s idx=%d cnt=%d clase=%s tipo=%s rows=%d cols=%d" % (
            i, rd_str(b, t + name), REGSET.get(regset, regset), regidx, regcnt,
            KLASS.get(kl, kl), TYPE.get(ty, ty), rows, cols))

if __name__ == "__main__":
    for p in sys.argv[1:]:
        parse_ctab(p)
        print()

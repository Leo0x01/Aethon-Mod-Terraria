#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
extraer_tmod.py — extractor de .tmod de tModLoader 2026.08 (descubrimiento v6.56.0-reconstrucción).

EL FORMATO (decodificado empíricamente en la recuperación de la v6.56.0):
  - Cabecera: "TMOD" + versión tML (string 7-bit) + hash(20) + firma(256) + int32
    (offset empírico) + nombre mod (string) + versión mod (string) + int32 nEntradas.
  - Tabla: nEntradas × (string ruta, int32 tamañoRaw, int32 tamañoCompreso).
  - Blobs: tras la tabla, back-to-back, cada uno de tamañoCompreso bytes.
  - CÓDEC: **DEFLATE CRUDO (zlib windowBits = -15)** — ni brotli, ni zlib con
    cabecera, ni LZMA-alone. Las entradas pequeñas van SIN comprimir (comp == raw).
  - Los .rawimg de Content/ NO son PNG: [int32 1][int32 ancho][int32 alto][RGBA crudo]
    (w*h*4 bytes). Para recuperar el PNG original: PIL Image.frombytes('RGBA', (w,h), datos).
  - El .tmod puede llevar AethonMod.pdb (símbolos con la tabla Documents:
    el mapa tipo→archivo fuente EXACTO — oro para reconstrucciones).

Uso: python3 extraer_tmod.py <mod.tmod> <carpeta_salida>
"""
import struct, sys, os, zlib

class R:
    def __init__(self, d, o=0):
        self.d = d; self.o = o
    def u7(self):
        r = 0; s = 0
        while True:
            b = self.d[self.o]; self.o += 1
            r |= (b & 0x7F) << s
            if not (b & 0x80): break
            s += 7
        return r
    def s(self):
        n = self.u7()
        v = self.d[self.o:self.o+n].decode('utf-8', 'replace')
        self.o += n
        return v
    def i32(self):
        v = struct.unpack_from('<i', self.d, self.o)[0]; self.o += 4
        return v

def extraer(tmod, salida):
    d = open(tmod, 'rb').read()
    assert d[:4] == b'TMOD', 'no es un .tmod'
    r = R(d, 4)
    tmlver = r.s()
    r.o += 20 + 256
    r.i32()
    nombre, ver = r.s(), r.s()
    n = r.i32()
    entradas = [(r.s(), r.i32(), r.i32()) for _ in range(n)]
    off = r.o
    ok = mal = 0
    for ruta, raw, comp in entradas:
        blob = d[off:off+comp]; off += comp
        data = blob if comp == raw else zlib.decompress(blob, -15)  # DEFLATE crudo
        assert len(data) == raw, f'tamaño mismatch {ruta}'
        p = os.path.join(salida, ruta)
        os.makedirs(os.path.dirname(p), exist_ok=True)
        open(p, 'wb').write(data)
        ok += 1
    assert off == len(d), f'EOF: {off} != {len(d)}'
    print(f'tML {tmlver} | {nombre} {ver} | {ok} entradas OK | EOF exacto')

if __name__ == '__main__':
    extraer(sys.argv[1], sys.argv[2])

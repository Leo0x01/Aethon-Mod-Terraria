import struct, sys

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

def parse(path):
    d = open(path, 'rb').read()
    assert d[:4] == b'TMOD', d[:8]
    r = R(d, 4)
    tmlver = r.s()
    r.o += 20    # hash
    r.o += 256   # signature
    r.i32()      # int32 extra (offset del final de la región de blobs, empírico)
    name = r.s()
    ver = r.s()
    cnt = r.i32()
    entries = []
    for _ in range(cnt):
        p = r.s()
        raw = r.i32()
        comp = r.i32()
        entries.append((p, raw, comp))
    return tmlver, name, ver, entries, r.o, len(d)

if __name__ == '__main__':
    tmlver, name, ver, entries, tend, fsize = parse(sys.argv[1])
    print(f'tML {tmlver} | mod {name} {ver} | {len(entries)} entries | table_end {tend} | file {fsize}')
    total_comp = sum(c for _, _, c in entries)
    print(f'sum(comp)={total_comp} + table_end {tend} = {tend+total_comp} (file {fsize})')
    for p, raw, comp in entries:
        print(p)

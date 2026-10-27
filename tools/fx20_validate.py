#!/usr/bin/env python3
# =============================================================================
# fx20_validate.py — Validador de .fxc contra el parser REAL del juego.
#
# Carga libFNA3D.so.0 (la MISMA nativa que tModLoader usa en Linux) y llama a
# MOJOSHADER_compileEffect con un contexto cuyos callbacks enrutan al
# MOJOSHADER_parse real — exactamente el camino que FNA3D ejecuta al cargar
# un Effect en runtime. Después llama effectBegin + effectBeginPass(0) para
# validar también la aplicación de estados (los samplers con sus texturas).
#
# Uso: python3 fx20_validate.py a.fxc [b.fxc ...]
# Sale 0 si TODOS validan; imprime errores de MojoShader si algo falla.
# =============================================================================
import ctypes, struct, sys

LIB = "/tmp/tml/Libraries/Native/Linux/libFNA3D.so.0"

# --- carga la nativa (deps del sistema) ---
lib = ctypes.CDLL(LIB)
libc = ctypes.CDLL("libc.so.6")

# --- tipos básicos ---
MOJOSHADER_effect_p = ctypes.c_void_p

# callbacks
CB_compileShader = ctypes.CFUNCTYPE(
    ctypes.c_void_p, ctypes.c_void_p, ctypes.c_char_p,
    ctypes.c_void_p, ctypes.c_uint,
    ctypes.c_void_p, ctypes.c_uint, ctypes.c_void_p, ctypes.c_uint)
CB_shaderAddRef = ctypes.CFUNCTYPE(None, ctypes.c_void_p)
CB_deleteShader = ctypes.CFUNCTYPE(None, ctypes.c_void_p, ctypes.c_void_p)
CB_getParseData = ctypes.CFUNCTYPE(ctypes.c_void_p, ctypes.c_void_p)
CB_bindShaders = ctypes.CFUNCTYPE(
    None, ctypes.c_void_p, ctypes.c_void_p, ctypes.c_void_p)
CB_getBoundShaders = ctypes.CFUNCTYPE(
    None, ctypes.c_void_p, ctypes.POINTER(ctypes.c_void_p),
    ctypes.POINTER(ctypes.c_void_p))
CB_mapUniform = ctypes.CFUNCTYPE(
    None, ctypes.c_void_p,
    ctypes.POINTER(ctypes.POINTER(ctypes.c_float)),
    ctypes.POINTER(ctypes.POINTER(ctypes.c_int)),
    ctypes.POINTER(ctypes.POINTER(ctypes.c_ubyte)),
    ctypes.POINTER(ctypes.POINTER(ctypes.c_float)),
    ctypes.POINTER(ctypes.POINTER(ctypes.c_int)),
    ctypes.POINTER(ctypes.POINTER(ctypes.c_ubyte)))
CB_unmapUniform = ctypes.CFUNCTYPE(None, ctypes.c_void_p)
CB_getError = ctypes.CFUNCTYPE(ctypes.c_char_p, ctypes.c_void_p)

# firmas de la nativa
lib.MOJOSHADER_parse.restype = ctypes.c_void_p
lib.MOJOSHADER_parse.argtypes = [
    ctypes.c_char_p, ctypes.c_char_p, ctypes.c_char_p, ctypes.c_uint,
    ctypes.c_void_p, ctypes.c_uint, ctypes.c_void_p, ctypes.c_uint,
    ctypes.c_void_p, ctypes.c_void_p, ctypes.c_void_p]
lib.MOJOSHADER_freeParseData.argtypes = [ctypes.c_void_p]
lib.MOJOSHADER_compileEffect.restype = ctypes.c_void_p
lib.MOJOSHADER_compileEffect.argtypes = [
    ctypes.c_char_p, ctypes.c_uint, ctypes.c_void_p, ctypes.c_uint,
    ctypes.c_void_p, ctypes.c_uint, ctypes.c_void_p]
lib.MOJOSHADER_deleteEffect.argtypes = [ctypes.c_void_p]
lib.MOJOSHADER_effectBegin.restype = None
lib.MOJOSHADER_effectBegin.argtypes = [
    ctypes.c_void_p, ctypes.POINTER(ctypes.c_uint), ctypes.c_int,
    ctypes.c_void_p]
lib.MOJOSHADER_effectBeginPass.restype = None
lib.MOJOSHADER_effectBeginPass.argtypes = [ctypes.c_void_p, ctypes.c_uint]
lib.MOJOSHADER_effectEndPass.argtypes = [ctypes.c_void_p]
lib.MOJOSHADER_effectEnd.argtypes = [ctypes.c_void_p]

PROFILE = b"glsl"          # el perfil del driver GL de FNA3D
_last_error = [None]


@CB_compileShader
def cb_compileShader(ctx, mainfn, tokenbuf, bufsize, swiz, swizcount, smap, smapcount):
    pd = lib.MOJOSHADER_parse(
        PROFILE, mainfn, ctypes.cast(tokenbuf, ctypes.c_char_p), bufsize,
        None, 0, None, 0, None, None, None)
    if not pd:
        _last_error[0] = "MOJOSHADER_parse devolvió NULL"
    else:
        n = ctypes.c_int.from_address(pd).value
        if n:
            ep = ctypes.c_void_p.from_address(pd + 8).value
            m = ctypes.c_char_p.from_address(ep + 8).value
            _last_error[0] = m.decode("utf-8", "replace") if m else "(parse con error y mensaje nulo)"
    return pd


@CB_getError
def cb_getError(ctx):
    e = _last_error[0]
    return e.encode() if e else None


@CB_shaderAddRef
def cb_addRef(shader):
    pass


@CB_deleteShader
def cb_deleteShader(ctx, shader):
    if shader:
        lib.MOJOSHADER_freeParseData(shader)


@CB_getParseData
def cb_getParseData(shader):
    return shader


@CB_bindShaders
def cb_bindShaders(ctx, vs, ps):
    pass


@CB_getBoundShaders
def cb_getBoundShaders(ctx, vs, ps):
    if vs:
        vs[0] = None
    if ps:
        ps[0] = None


_reg_buffers = []  # mantiene vivos los buffers de registros


@CB_mapUniform
def cb_mapUniform(ctx, vsf, vsi, vsb, psf, psi, psb):
    # buffers reales como hace el driver GL (CommitChanges copia aquí los
    # valores de los parámetros — con punteros basura revienta)
    for pp, ctype, n in ((vsf, ctypes.c_float, 8192), (vsi, ctypes.c_int, 2048),
                         (vsb, ctypes.c_ubyte, 256), (psf, ctypes.c_float, 8192),
                         (psi, ctypes.c_int, 2048), (psb, ctypes.c_ubyte, 256)):
        buf = (ctype * n)()
        _reg_buffers.append(buf)
        pp[0] = ctypes.cast(buf, ctypes.POINTER(ctype))


@CB_unmapUniform
def cb_unmapUniform(ctx):
    pass


class EffectShaderContext(ctypes.Structure):
    _fields_ = [
        ("compileShader", CB_compileShader),
        ("shaderAddRef", CB_shaderAddRef),
        ("deleteShader", CB_deleteShader),
        ("getParseData", CB_getParseData),
        ("bindShaders", CB_bindShaders),
        ("getBoundShaders", CB_getBoundShaders),
        ("mapUniformBufferMemory", CB_mapUniform),
        ("unmapUniformBufferMemory", CB_unmapUniform),
        ("getError", CB_getError),
        ("shaderContext", ctypes.c_void_p),
        ("m", ctypes.c_void_p),
        ("f", ctypes.c_void_p),
        ("malloc_data", ctypes.c_void_p),
    ]


CTX = EffectShaderContext()
CTX.compileShader = cb_compileShader
CTX.shaderAddRef = cb_addRef
CTX.deleteShader = cb_deleteShader
CTX.getParseData = cb_getParseData
CTX.bindShaders = cb_bindShaders
CTX.getBoundShaders = cb_getBoundShaders
CTX.mapUniformBufferMemory = cb_mapUniform
CTX.unmapUniformBufferMemory = cb_unmapUniform
CTX.getError = cb_getError
CTX.shaderContext = None
CTX.m = None
CTX.f = None
CTX.malloc_data = None


def read_parse_errors(pd):
    """MOJOSHADER_parseData: {int error_count; MOJOSHADER_error *errors; ...}"""
    if not pd:
        return ["(parse devolvió NULL)"]
    n = ctypes.c_int.from_address(pd).value
    errs_ptr = ctypes.c_void_p.from_address(pd + 8).value
    out = []
    for i in range(n):
        e = errs_ptr + i * 16          # {int error_position; char *error;}
        msg = ctypes.c_char_p.from_address(e + 8).value
        out.append(msg.decode("utf-8", "replace") if msg else "(sin mensaje)")
    return out


def validate(path):
    data = open(path, "rb").read()
    print("== %s (%d bytes)" % (path, len(data)))
    eff = lib.MOJOSHADER_compileEffect(data, len(data), None, 0, None, 0,
                                       ctypes.byref(CTX))
    if not eff:
        print("  FATAL: compileEffect devolvió NULL")
        return False
    # MOJOSHADER_effect: {int error_count; MOJOSHADER_error *errors;
    #                     int param_count; ...; int technique_count; ...}
    err_count = ctypes.c_int.from_address(eff).value
    param_count = ctypes.c_int.from_address(eff + 16).value
    tech_count = ctypes.c_int.from_address(eff + 32).value
    obj_count = ctypes.c_int.from_address(eff + 48).value
    ok = True
    if err_count:
        errs_ptr = ctypes.c_void_p.from_address(eff + 8).value
        for i in range(err_count):
            e = errs_ptr + i * 16
            msg = ctypes.c_char_p.from_address(e + 8).value
            print("  ERROR MojoShader: %s" %
                  (msg.decode("utf-8", "replace") if msg else "?"))
        ok = False
    print("  params=%d técnicas=%d objetos=%d errores=%d" %
          (param_count, tech_count, obj_count, err_count))
    if not ok:
        lib.MOJOSHADER_deleteEffect(eff)
        return False
    # effectBegin + BeginPass(0): valida técnicas/pases/estados/samplers
    # (ambas son void — el éxito se mide en los stateChanges que llenan)
    passes = ctypes.c_uint(0)
    # MOJOSHADER_effectStateChanges (x64, punteros alineados a 8):
    #   {u32 rs_count; pad; ptr rs; u32 smp_count; pad; ptr smp; u32 vsmp; pad; ptr}
    state_buf = ctypes.create_string_buffer(64)
    lib.MOJOSHADER_effectBegin(eff, ctypes.byref(passes), 0, state_buf)
    lib.MOJOSHADER_effectBeginPass(eff, 0)
    rs_count, rs_ptr, smp_count, smp_ptr = struct.unpack_from("<I4xQI4xQ", state_buf, 0)
    print("  effectBegin OK (%d pases) · BeginPass(0) OK · estados de render=%d · SAMPLERS=%d"
          % (passes.value, rs_count, smp_count))
    # cada MOJOSHADER_samplerStateRegister:
    #   {const char *sampler_name; unsigned int sampler_register;
    #    unsigned int sampler_state_count; MOJOSHADER_effectSamplerState *sampler_states}
    for i in range(min(smp_count, 8)):
        reg = smp_ptr + i * 24
        name_p, sreg, scount = struct.unpack_from("<QII", ctypes.string_at(reg, 16), 0)
        name = ctypes.string_at(name_p, 64).split(b"\0")[0] if name_p else b"?"
        print("    sampler[%d] %r registro=s%d estados=%d" %
              (i, name, sreg, scount))
    if smp_count == 0:
        print("    (sin samplers — effect sin texturas, como los ground truths)")
    lib.MOJOSHADER_effectEndPass(eff)
    lib.MOJOSHADER_effectEnd(eff)
    lib.MOJOSHADER_deleteEffect(eff)
    return True


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("uso: fx20_validate.py archivo.fxc [...]")
        sys.exit(2)
    todo_ok = True
    for p in sys.argv[1:]:
        if not validate(p):
            todo_ok = False
        print()
    print("VEREDICTO:", "TODOS VALIDAN ✓" if todo_ok else "HAY FALLOS ✗")
    sys.exit(0 if todo_ok else 1)

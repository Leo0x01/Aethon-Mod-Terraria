#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
pat_baul.py — EL BAÚL DEL PAT (v6.116) — el token de GitHub JAMÁS
más en texto plano.

LA LETRA DEL USUARIO: «intenta encriptar el token de forma complicada
y luego guardalo para seguir usando» — el PAT ha viajado en texto
plano por el chat (y por el historial del shell) demasiadas veces.

EL DISEÑO — por qué un baúl y no un archivo plano:
  · el BAÚL (tools/pat_baul.dat) SÍ vive en el repo público — es la
    única memoria que sobrevive las restauraciones del sandbox (todo
    lo local muere: /tmp, ~/.config, el propio árbol de trabajo; el
    GitHub no) — pero viaja CIFRADO con capas encadenadas;
  · la FRASE (la llave) NUNCA vive en el repo: se genera una vez con
    `secrets`, se guarda en ~/.config/aethon-baul/frase (0600) y se
    le entrega AL USUARIO — si el sandbox se restaura, el usuario
    pega LA FRASE (jamás el PAT: la frase sola no le sirve a nadie
    sin el baúl, y el baúl solo no le sirve a nadie sin la frase);
  · el PAT en texto plano NO vuelve a aparecer ni en el repo, ni en
    los logs, ni en los argumentos del shell.

LAS CAPAS (de afuera hacia adentro):
  0. ARMADURA: base64("AETHONBAUL1" + sal + nonce + ct_len + ct + hmac)
  1. INTEGRIDAD: HMAC-SHA256(K2) sobre (sal + nonce + ct) — cualquier
     byte tocado aborta el descifrado;
  2. ROTACIÓN: todo el ciphertext rotado (K2[0] mod len) bytes;
  3. FEISTEL: 8 rondas por bloque de 16 bytes (F = SHA-256(K1||ronda||
     mitad)) — difusión incluso contra texto plano conocido;
  4. FLUJO: XOR contra SHA-256-CTR(K1, nonce);
  5. LLAVES: K1||K2 = PBKDF2-HMAC-SHA256(frase, sal, 210.000 iter).

USO:
  python3 tools/pat_baul.py guardar    # lee el PAT del env AETHON_PAT
                                       # o de la entrada (oculta-ish)
  python3 tools/pat_baul.py leer       # imprime el PAT (para scripts)
  python3 tools/pat_baul.py probar     # roundtrip + verificación
  python3 tools/pat_baul.py frase      # imprime la frase (el dueño)

La frase se busca en: $AETHON_FRASE, luego ~/.config/aethon-baul/frase.
El publicador la usa solo: AETHON_PAT="$(python3 tools/pat_baul.py leer)".
"""
import base64
import hashlib
import hmac
import os
import secrets
import sys

AQUI = os.path.dirname(os.path.abspath(__file__))
BAUL = os.path.join(AQUI, 'pat_baul.dat')
FRASE_RUTA = os.path.expanduser('~/.config/aethon-baul/frase')

MAGICO = b'AETHONBAUL1'
ITERACIONES = 210_000
RONDAS_FEISTEL = 8


# ================== LA LLAVE ==================

def buscar_frase():
    """La frase: env → archivo → None (el dueño la tiene)."""
    f = os.environ.get('AETHON_FRASE')
    if f:
        return f.strip()
    if os.path.exists(FRASE_RUTA):
        return open(FRASE_RUTA, encoding='utf-8').read().strip()
    return None


def generar_frase():
    """Una frase nueva de 40+ caracteres cripto-aleatorios."""
    return secrets.token_urlsafe(30)


# ================== LAS CAPAS ==================

def derivar(frase, sal):
    """K1 (flujo/feistel) || K2 (hmac) — PBKDF2-HMAC-SHA256 ×210k."""
    llaves = hashlib.pbkdf2_hmac('sha256', frase.encode('utf-8'), sal,
                                 ITERACIONES, dklen=64)
    return llaves[:32], llaves[32:]


def flujo(k1, nonce, n):
    """SHA-256-CTR: el arroyo de bytes para el XOR."""
    out = bytearray()
    c = 0
    while len(out) < n:
        out += hashlib.sha256(k1 + nonce + c.to_bytes(8, 'big')).digest()
        c += 1
    return bytes(out[:n])


def feistel_cifrar(bloque, k1):
    """Una ronda: (L,R) → (R, L^F(R)); 8 rondas por bloque de 16 B."""
    l, r = bloque[:8], bloque[8:]
    for i in range(RONDAS_FEISTEL):
        f = hashlib.sha256(k1 + bytes([i]) + r).digest()[:8]
        l, r = r, bytes(a ^ b for a, b in zip(l, f))
    return l + r


def feistel_descifrar(bloque, k1):
    l, r = bloque[:8], bloque[8:]
    for i in reversed(range(RONDAS_FEISTEL)):
        f = hashlib.sha256(k1 + bytes([i]) + l).digest()[:8]
        l, r = bytes(a ^ b for a, b in zip(r, f)), l
    return l + r


def rellenar(datos):
    """PKCS#7 a múltiplos de 16."""
    n = 16 - (len(datos) % 16)
    return datos + bytes([n]) * n


def quitar_relleno(datos):
    n = datos[-1]
    if not 1 <= n <= 16 or datos[-n:] != bytes([n]) * n:
        raise ValueError('el relleno no cuadra — ¿frase equivocada?')
    return datos[:-n]


def rotar_der(b, k):
    if not b:
        return b
    k %= len(b)
    return b[-k:] + b[:-k]


def rotar_izq(b, k):
    if not b:
        return b
    k %= len(b)
    return b[k:] + b[:k]


# ================== EL BAÚL ==================

def cifrar(pat, frase):
    sal = secrets.token_bytes(16)
    nonce = secrets.token_bytes(16)
    k1, k2 = derivar(frase, sal)
    cuerpo = rellenar(pat.encode('utf-8'))
    cuerpo = bytes(a ^ b for a, b in zip(cuerpo, flujo(k1, nonce, len(cuerpo))))
    bloques = b''.join(feistel_cifrar(cuerpo[i:i + 16], k1)
                       for i in range(0, len(cuerpo), 16))
    rot = k2[0] % len(bloques) if bloques else 0
    girado = rotar_der(bloques, rot)
    mac = hmac.new(k2, sal + nonce + girado, hashlib.sha256).digest()
    crudo = MAGICO + sal + nonce + len(bloques).to_bytes(4, 'big') + girado + mac
    return base64.b64encode(crudo).decode('ascii') + '\n'


def descifrar(armadura, frase):
    crudo = base64.b64decode(armadura.strip().encode('ascii'))
    if not crudo.startswith(MAGICO):
        raise ValueError('esto no es un baúl de la casa')
    crudo = crudo[len(MAGICO):]
    sal, crudo = crudo[:16], crudo[16:]
    nonce, crudo = crudo[:16], crudo[16:]
    (largo,), crudo = __import__('struct').unpack('>I', crudo[:4]), crudo[4:]
    girado, mac = crudo[:largo], crudo[largo:largo + 32]
    k1, k2 = derivar(frase, sal)
    esperado = hmac.new(k2, sal + nonce + girado, hashlib.sha256).digest()
    if not hmac.compare_digest(mac, esperado):
        raise ValueError('el sello no cuadra — ¿frase equivocada o baúl tocado?')
    bloques = rotar_izq(girado, k2[0] % largo if largo else 0)
    cuerpo = b''.join(feistel_descifrar(bloques[i:i + 16], k1)
                      for i in range(0, len(bloques), 16))
    cuerpo = bytes(a ^ b for a, b in zip(cuerpo, flujo(k1, nonce, len(cuerpo))))
    return quitar_relleno(cuerpo).decode('utf-8')


# ================== LOS COMANDOS ==================

def cmd_guardar():
    pat = os.environ.get('AETHON_PAT', '').strip()
    if not pat and not sys.stdin.isatty():
        pat = sys.stdin.read().strip()
    if not pat:
        print('ERROR: pasa el PAT con AETHON_PAT=… o por la entrada estándar')
        sys.exit(1)
    frase = buscar_frase()
    if not frase:
        frase = generar_frase()
        os.makedirs(os.path.dirname(FRASE_RUTA), exist_ok=True)
        with open(FRASE_RUTA, 'w', encoding='utf-8') as f:
            f.write(frase + '\n')
        os.chmod(FRASE_RUTA, 0o600)
        print(f'[baúl] frase NUEVA guardada en {FRASE_RUTA} (0600)')
        print(f'[baúl] LA FRASE (guárdala tú también — es la llave):')
        print(f'        {frase}')
    armadura = cifrar(pat, frase)
    with open(BAUL, 'w', encoding='ascii') as f:
        f.write(armadura)
    # EL ROUNDTRIP: lo que se guardó se tiene que poder leer.
    if descifrar(open(BAUL, encoding='ascii').read(), frase) != pat:
        print('ERROR: el roundtrip falló — el baúl NO se guardó bien')
        sys.exit(1)
    print(f'[baúl] guardado y verificado: {BAUL} ({len(armadura)} caracteres blindados)')


def cmd_leer():
    frase = buscar_frase()
    if not frase:
        print('ERROR: no hay frase (ni AETHON_FRASE ni '
              f'{FRASE_RUTA}) — pide al dueño la frase de la casa', file=sys.stderr)
        sys.exit(1)
    try:
        print(descifrar(open(BAUL, encoding='ascii').read(), frase))
    except FileNotFoundError:
        print(f'ERROR: no existe {BAUL}', file=sys.stderr)
        sys.exit(1)
    except ValueError as e:
        print(f'ERROR: {e}', file=sys.stderr)
        sys.exit(1)


def cmd_probar():
    frase = buscar_frase() or generar_frase()
    prueba = 'github_pat_PRUEBA_0123456789_abcdefghijklmnopqrstuvwxyz'
    armadura = cifrar(prueba, frase)
    ok = descifrar(armadura, frase) == prueba
    mal = False
    try:
        descifrar(armadura, frase + 'x')          # frase equivocada
    except ValueError:
        mal = True
    print(f'roundtrip: {"OK" if ok else "FALLO"} · '
          f'frase equivocada rechazada: {"OK" if mal else "FALLO"} · '
          f'iteraciones PBKDF2: {ITERACIONES:,} · rondas Feistel: {RONDAS_FEISTEL}')
    sys.exit(0 if ok and mal else 1)


def cmd_frase():
    frase = buscar_frase()
    if frase:
        print(frase)
    else:
        print('(no hay frase guardada en esta máquina)')
        sys.exit(1)


if __name__ == '__main__':
    cmd = sys.argv[1] if len(sys.argv) > 1 else ''
    {'guardar': cmd_guardar, 'leer': cmd_leer,
     'probar': cmd_probar, 'frase': cmd_frase}.get(cmd, lambda: (
        print(__doc__), sys.exit(1 if not cmd else 0)))()

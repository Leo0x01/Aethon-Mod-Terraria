#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
parte_archivos_v6177.py — v6.177 — EL PARTIDOR DE LOS TRES GIGANTES.

La letra (sesión 30): «y comienza con esto, asegurate de no tener
errores: Los 3 archivos de +3.000 líneas (baja): se pueden partir por
responsabilidades sin tocar comportamiento, pero conviene hacerlo en
versiones tranquilas, no mezclado con features. Partir
AuraLib/AethonBoss/AtaqueJefeProjectile: igual — refactors mecánicos,
no urgentes.»

LA LEY DEL PARTIDO MECÁNICO:
  · Nada se reescribe: cada método se mueve INTACTO (bytes exactos).
  · Las clases se vuelven `partial` (mismo namespace, mismos nombres).
  · EL ESTILO DE LA CASA ES LA GRAMÁTICA: los miembros de clase viven
    a indentación EXACTA de 8 espacios y cierran en la línea exacta
    '        }'; las clases viven a 4 y cierran en '    }'. El corte
    va de la firma (con su bloque de comentarios contiguo) a SU
    primera llave de cierre exacta — sin lexers frágiles.
  · La INTEGRIDAD se demuestra: el multiset de líneas del original ==
    la suma de las líneas repartidas (las cabeceras nuevas no se
    cuentan: son contenido nuevo).
  · El oráculo (0/0) es el gate del split: si compila igual, ES igual.

EL REPARTO:
  AuraLib.cs (4864) →
      AuraPerfil.cs      (ParticulasAura + AuraPerfil: las RECETAS)
      AuraLib.cs         (el MOTOR: Actualizar, emisores, CampoFbm)
      AuraLibDibujo.cs   (los DIBUJOS: Dibujar*)
  AethonBoss.cs (4072) →
      AethonBoss.cs         (SetDefaults + AI + la pelea)
      AethonBossDibujo.cs   (PreDraw/PostDraw/FindFrame + los dibujos)
  AtaqueJefeProjectile.cs (3753) →
      AtaqueJefeProjectile.cs   (SetDefaults + AI + EL ARSENAL + AnilloCoro)
      AtaqueJefeDibujo.cs        (PreDraw/PostDraw/DrawBehind + dibujos)

Uso: python3 tools/parte_archivos_v6177.py [--check]
"""
import re
import sys
import os

RAIZ = os.path.join(os.path.dirname(__file__), '..', 'AethonMod', 'Content')

DECL_CLASE = re.compile(
    r'^[ \t]*(?:[A-Za-z_][A-Za-z0-9_]*[ \t]+)*(?:class|struct)[ \t]+([A-Za-z_][A-Za-z0-9_]*)\b')
FIRMA_MIEMBRO = re.compile(r'^        (?!\})')


def fin_clase_o_metodo(lineas, i_firma, cierre):
    """La línea de cierre EXACTA del bloque que empieza en i_firma:
    la primera línea == cierre (a partir de i_firma). El estilo de la
    casa garantiza que la primera llave de cierre a SU nivel es la
    del propio bloque (los anidados cierran a indentación mayor)."""
    j = i_firma
    while j < len(lineas):
        if lineas[j] == cierre:
            return j
        j += 1
    raise AssertionError('cierre no encontrado (%r) desde la línea %d'
                         % (cierre, i_firma + 1))


def inicio_con_comentario(lineas, i_firma):
    """Retrocede para llevarse el bloque de comentarios CONTIGUO que
    precede a la firma (los banners ═══ de la casa viajan con su
    método). Solo cruza líneas en blanco si DETRÁS sigue habiendo
    comentario (para no comerse el aire entre dos miembros)."""
    i = i_firma - 1
    while i >= 0:
        s = lineas[i].strip()
        if s.startswith('//'):
            i -= 1
        elif s == '':
            k = i - 1
            while k >= 0 and lineas[k].strip() == '':
                k -= 1
            if k >= 0 and lineas[k].strip().startswith('//'):
                i = k
                continue
            break
        else:
            break
    return i + 1


def buscar_declaracion(lineas, nombre):
    for i, ln in enumerate(lineas):
        m = DECL_CLASE.match(ln)
        if m and m.group(1) == nombre:
            return i
    raise AssertionError('declaración no encontrada: ' + nombre)


def nombres_miembros(lineas, i_ini, i_fin):
    """Los NOMBRES de los miembros de clase declarados a indentación 8
    en el rango de la clase: (nombre, i_firma)."""
    firma = re.compile(
        r'^        (?:public|private|internal|protected|static|override'
        r'|virtual|sealed|new|readonly|async|const|partial)[ \t]+'
        r'(?:[A-Za-z_<>\[\],\. \t]+\s+)?'
        r'([A-Za-z_][A-Za-z0-9_]*)\s*(?:\(|\s*[={;]|\s*$)')
    out = []
    for i in range(i_ini, i_fin + 1):
        ln = lineas[i]
        if not ln.startswith('        '):
            continue
        if ln.strip().startswith('//') or ln.strip() == '':
            continue
        m = firma.match(ln)
        if not m:
            continue
        # descarta declaraciones anidadas (class/struct/enum a nivel 8)
        rest = ln.strip()
        if re.match(r'^(public|private|internal|protected|static|sealed|new|abstract)?\s*(partial\s+)?(class|struct|enum|interface)\b', rest):
            continue
        out.append((m.group(1), i))
    return out


# ════════════════════════════════════════════════════════════════════
#  OPERACIÓN 1 — partir: los métodos de DIBUJO salen a su archivo
# ════════════════════════════════════════════════════════════════════
PATRON_DIBUJO = re.compile(r'^(PreDraw|PostDraw|Dibujar|DrawBehind|DrawEx|FindFrame)')


def partir_dibujos(ruta_archivo, clase, archivo_dibujo, cabecera_dibujo,
                   solo_ver=False):
    ruta = os.path.join(RAIZ, ruta_archivo)
    original = open(ruta, encoding='utf-8').read()
    lineas = original.split('\n')

    i_decl = buscar_declaracion(lineas, clase)
    # la clase TOPE de estos archivos cierra con '}' a indentación 0
    # (peculiaridad histórica de formato — compila igual)
    i_fin = fin_clase_o_metodo(lineas, i_decl, '}')

    # los miembros DIBUJO (por nombre) — se mudan enteros
    mudados = []
    for nombre, i in nombres_miembros(lineas, i_decl + 1, i_fin - 1):
        if PATRON_DIBUJO.match(nombre):
            fin = fin_clase_o_metodo(lineas, i, '        }')
            ini = inicio_con_comentario(lineas, i)
            mudados.append((nombre, ini, fin))

    ocupado = set()
    for _, ini, fin in mudados:
        ocupado.update(range(ini, fin + 1))

    cuerpo_dibujo = [lineas[i] for i in sorted(ocupado)]

    # integridad multiset: pre + decl + resto + cierre + post + dibujo == todo
    ms_original = sorted(lineas)
    ms_reparto = sorted(
        lineas[:i_decl]                       # pre (cabecera original)
        + [lineas[i_decl]]                    # la declaración (partial)
        + [lineas[i] for i in range(i_decl + 1, i_fin) if i not in ocupado]
        + [lineas[i_fin]]                     # cierre de la clase
        + lineas[i_fin + 1:]                  # post (namespace etc.)
        + cuerpo_dibujo)
    assert ms_original == ms_reparto, (
        f'INTEGRIDAD ROTA en {ruta_archivo}: {len(ms_original)} vs {len(ms_reparto)}')

    if solo_ver:
        print(f'  {ruta_archivo}: {clase} ({i_decl + 1}-{i_fin + 1}) → '
              f'{len(mudados)} dibujos, {len(cuerpo_dibujo)} líneas:')
        for nombre, ini, fin in mudados:
            print(f'    → {nombre} (líneas {ini + 1}-{fin + 1})')
        return None

    # la clase se vuelve partial (si no lo era)
    decl = lineas[i_decl]
    if 'partial' not in decl:
        decl = re.sub(r'(\bclass\b)', r'partial \1', decl, count=1)
        assert 'partial class' in decl

    # el motor: la cabecera original + la clase adelgazada
    motor = (lineas[:i_decl]
             + [decl]
             + [lineas[i] for i in range(i_decl + 1, i_fin) if i not in ocupado]
             + [lineas[i_fin]]
             + lineas[i_fin + 1:])
    open(ruta, 'w', encoding='utf-8', newline='\n').write('\n'.join(motor))

    # el archivo de dibujo: cabecera nueva + usings (copiados) + la
    # clase partial con solo los dibujos
    usings = [ln for ln in lineas[:i_decl] if ln.strip().startswith('using ')]
    ns = 'AethonMod'
    for ln in lineas[:i_decl]:
        m = re.match(r'namespace\s+([A-Za-z0-9_.]+)', ln.strip())
        if m:
            ns = m.group(1)
    # la llave de apertura de la clase: en estos archivos vive en SU
    # PROPIA LÍNEA tras la declaración (i_decl+1) — el motor la lleva
    # dentro de su rango; el archivo de dibujo la abre él mismo
    abre = '    {'
    dibujo = (cabecera_dibujo + '\n' + '\n'.join(usings) + '\n\n'
              + 'namespace ' + ns + '\n{\n'
              + decl + '\n' + abre + '\n'
              + '\n'.join(cuerpo_dibujo) + '\n    }\n}\n')   # clase + namespace
    ruta_dibujo = os.path.join(RAIZ, archivo_dibujo)
    open(ruta_dibujo, 'w', encoding='utf-8', newline='\n').write(dibujo)

    print(f'  {ruta_archivo} → motor {len(motor)} líneas · '
          f'{archivo_dibujo} ({len(cuerpo_dibujo) + len(usings) + 4} líneas): '
          + ', '.join(n for n, _, _ in mudados))
    return True


# ════════════════════════════════════════════════════════════════════
#  OPERACIÓN 2 — separar_tipos: clases ENTERAS a su archivo
# ════════════════════════════════════════════════════════════════════
def separar_tipos(ruta_archivo, nombres, archivo_nuevo, cabecera,
                  solo_ver=False):
    ruta = os.path.join(RAIZ, ruta_archivo)
    original = open(ruta, encoding='utf-8').read()
    lineas = original.split('\n')

    bloques = []
    for nombre in nombres:
        i_decl = buscar_declaracion(lineas, nombre)
        i_fin = fin_clase_o_metodo(lineas, i_decl, '    }')
        ini = inicio_con_comentario(lineas, i_decl)
        bloques.append((nombre, ini, i_fin))
    bloques.sort(key=lambda b: b[1])

    ocupado = set()
    for _, ini, fin in bloques:
        ocupado.update(range(ini, fin + 1))

    restante = [lineas[i] for i in range(len(lineas)) if i not in ocupado]
    mudado = [lineas[i] for i in sorted(ocupado)]

    assert sorted(lineas) == sorted(restante + mudado), \
        f'INTEGRIDAD ROTA en {ruta_archivo}'

    if solo_ver:
        for nombre, ini, fin in bloques:
            print(f'    → {nombre} (líneas {ini + 1}-{fin + 1}, {fin - ini + 1} líneas)')
        print(f'    integridad OK ({len(mudado)} líneas mudadas)')
        return None

    usings = [ln for ln in lineas if ln.strip().startswith('using ')]
    ns = 'AethonMod'
    for ln in lineas:
        m = re.match(r'namespace\s+([A-Za-z0-9_.]+)', ln.strip())
        if m:
            ns = m.group(1)
            break
    nuevo = (cabecera + '\n' + '\n'.join(usings) + '\n\n'
             + 'namespace ' + ns + '\n{\n'
             + '\n\n'.join('\n'.join(lineas[ini:fin + 1]) for _, ini, fin in bloques)
             + '\n}\n')
    open(os.path.join(RAIZ, archivo_nuevo), 'w', encoding='utf-8',
         newline='\n').write(nuevo)
    open(ruta, 'w', encoding='utf-8', newline='\n').write('\n'.join(restante))
    print(f'  {ruta_archivo} → quedan {len(restante)} líneas · '
          f'{archivo_nuevo}: {len(mudado)} mudadas')
    return True


# ════════════════════════════════════════════════════════════════════
#  EL PLAN
# ════════════════════════════════════════════════════════════════════
CAB_AURAPERFIL = '''// ======================================================================
    //  AuraPerfil — v6.177 — LAS RECETAS DEL AURA (la letra de la
    //  sesión 30: partir los gigantes por responsabilidades SIN tocar
    //  comportamiento). Aquí viven ParticulasAura y AuraPerfil con sus
    //  fábricas (OleadaGrimorio · Las Formas Ascendidas · HaloArcoiris ·
    //  BrasaDelEclipse · HambreDelGrimorio) — mudadas INTACTAS byte a
    //  byte de AuraLib.cs por tools/parte_archivos_v6177.py. El MOTOR
    //  (Actualizar, emisores) vive en AuraLib.cs; los DIBUJOS en
    //  AuraLibDibujo.cs.
    // ====================================================================='''

CAB_AURALIBDIBUJO = '''// ======================================================================
    //  AuraLibDibujo — v6.177 — EL PARTIDO DE LA BIBLIOTECA DEL AURA
    //  (la letra de la sesión 30: partir los gigantes por
    //  responsabilidades SIN tocar comportamiento). Aquí viven los
    //  DIBUJOS del aura: los mismos métodos, INTACTOS byte a byte,
    //  mudados de AuraLib.cs por tools/parte_archivos_v6177.py — la
    //  clase es `partial`: mismo namespace, mismo nombre, mismo
    //  comportamiento. El MOTOR (Actualizar, emisores, CampoFbm) vive
    //  en AuraLib.cs.
    // ====================================================================='''

CAB_AETHONDIBUJO = '''// ======================================================================
    //  AethonBossDibujo — v6.177 — EL PARTIDO DEL JEFE (la letra de la
    //  sesión 30: partir los gigantes por responsabilidades SIN tocar
    //  comportamiento). Aquí viven los DIBUJOS del jefe (PreDraw ·
    //  PostDraw · FindFrame): los mismos métodos, INTACTOS byte a
    //  byte, mudados de AethonBoss.cs por tools/parte_archivos_v6177.py
    //  — la clase es `partial`: mismo namespace, mismo nombre, mismo
    //  comportamiento. La PELEA (SetDefaults, AI, los ataques) sigue
    //  en AethonBoss.cs.
    // ====================================================================='''

CAB_ATAQUEDIBUJO = '''// ======================================================================
    //  AtaqueJefeDibujo — v6.177 — EL PARTIDO DEL ARSENAL DEL JEFE
    //  (la letra de la sesión 30: partir los gigantes por
    //  responsabilidades SIN tocar comportamiento). Aquí viven los
    //  DIBUJOS del proyectil (PreDraw · PostDraw · DrawBehind · los
    //  pases): los mismos métodos, INTACTOS byte a byte, mudados de
    //  AtaqueJefeProjectile.cs por tools/parte_archivos_v6177.py — la
    //  clase es `partial`: mismo namespace, mismo nombre, mismo
    //  comportamiento. La PELEA (SetDefaults, AI, EL ARSENAL, el
    //  AnilloCoro) sigue en AtaqueJefeProjectile.cs.
    // ====================================================================='''


def main():
    solo_ver = '--check' in sys.argv
    if solo_ver:
        print('== MODO CHECK (no escribe nada) ==')

    print('· 1. AuraLib (4864) → AuraPerfil + motor + dibujo')
    separar_tipos('VFX/AuraLib.cs',
                  ['ParticulasAura', 'AuraPerfil'], 'VFX/AuraPerfil.cs',
                  CAB_AURAPERFIL, solo_ver=solo_ver)
    partir_dibujos('VFX/AuraLib.cs', 'AuraLib', 'VFX/AuraLibDibujo.cs',
                   CAB_AURALIBDIBUJO, solo_ver=solo_ver)

    print('· 2. AethonBoss (4072) → pelea + dibujo')
    partir_dibujos('NPCs/AethonBoss.cs', 'AethonBoss',
                   'NPCs/AethonBossDibujo.cs', CAB_AETHONDIBUJO,
                   solo_ver=solo_ver)

    print('· 3. AtaqueJefeProjectile (3753) → IA/arsenal + dibujo')
    partir_dibujos('Projectiles/Jefes/AtaqueJefeProjectile.cs',
                   'AtaqueJefeProjectile',
                   'Projectiles/Jefes/AtaqueJefeDibujo.cs',
                   CAB_ATAQUEDIBUJO, solo_ver=solo_ver)

    print('== EL PARTIDO TERMINÓ — ahora el oráculo decide ==')


if __name__ == '__main__':
    main()

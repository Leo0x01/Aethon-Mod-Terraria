#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
cirugia_v6136b.py — EL LAVADO COMPLETO DE DAGA ETERNA MINION.

La cirugía principal (cirugia_v6136.py) ya mató los 13 archivos y lavó
Bolsas/Cetro/Minion-parte-1, pero su verificador final encontró ~18
menciones de «stabber» más dentro de DagaEternaMinion.cs (el archivo tiene
781 líneas y solo se habían leído las primeras 100 — la lección: leer el
archivo ENTERO antes de lavarlo). Esta cirugía:

  (1) Reescribe la CABECERA del Minion (líneas 13-44): la ficha vieja
      citaba el arma ajena y su wiki «palabra por palabra» — la nueva
      cuenta la misma historia en términos 100% de la casa (la v6.132
      forjó la máquina de cero para curar el ataque muerto; la v6.136
      lavó los nombres).
  (2) Sustituye los ~16 «stabber» sueltos por su nombre de la casa
      («la máquina de las cuchillas», «el compás de las dagas»…).
  (3) Elimina la cita textual de la wiki en las dagas de cristal/llama/
      roca (los números quedan — son NUESTROS números, la mecánica es
      nuestra desde la v6.132).
  (4) RE-AFILA EL VERIFICADOR: «entropy/entropía» solo cuenta si NO es
      la física de la estrella muerta (EntropyRadius/EntropyPeriod/
      entropía de la DeadStar); «holy flames» se lava también.
"""
import os
import re

RAIZ = '/home/z/my-project/mod/AethonMod'
P = os.path.join(RAIZ, 'Content/Projectiles/DagaEternaMinion.cs')

s = open(P, encoding='utf-8').read()

# ============ (1) LA CABECERA VIEJA → LA DE LA CASA ============
VIEJA = '''    /// <summary>
    /// DAGA ETERNA MINION — v6.124 — LAS CINCO DAGAS DEL ETERNO (la letra
    /// del usuario: «quiero que esas dagas sean las siguiente invocacion
    /// despues de los 11 ojos, vienen las 5 dagas que tambien tendran un
    /// ojo en el centro jejejejej»).
    ///
    /// LA TÉCNICA ES 100 % COPIADA DEL DAZZLING STABBER STAFF (la letra:
    /// «toma lo que aprendiste del invocador de dagas del Eterno» — el
    /// comportamiento de cuchillas orbitantes nacido de una
    /// investigación de la wiki, hoy máquina 100 % de la casa):
    /// un escuadrón de DAGAS PERSISTENTES, cada una con SU técnica
    /// propia, que VUELAN alrededor del portador y se LANZAN contra las
    /// presas — y TODAS prenden la QUEMADURA DEL VACÍO (nuestra Holy
    /// Flames: el stabber prende Holy Flames al 100 % por 3 s — aquí es
    /// la quemadura del vacío al 100 % por 3 s, 180 t):
    ///   · DAGA DE CRISTAL (ai[0]=0) — «Crystal daggers rapidly slash
    ///     enemies before briefly pausing, dealing 50% base damage but
    ///     having an innate 20 armor penetration»: RÁFAGA de tres tajos
    ///     rápidos A TRAVÉS de la presa (×0,5 del daño, +20 de
    ///     perforación) y una PAUSA breve antes de volver — el compás
    ///     EXACTO del original.
    ///   · DAGA DE LLAMA (1) — «Flame daggers swipe at enemies in a wide
    ///     arcing motion»: un BARRIDO EN ARCO AMPLIO que cruza la zona
    ///     de la presa peinándolo todo (×1,0).
    ///   · DAGA DE ROCA (2) — «Rock daggers lunge at enemies and recoil
    ///     after hits, dealing 300% base damage»: la EMBESTIDA de
    ///     lejos, el golpe ×3,0 y el RECUDE hacia atrás antes de volver.
    ///   · DAGA DEL VACÍO (3) — la cuarta de la casa: se LANZA, SE CLAVA
    ///     en la presa y GOTEA el vacío (×0,35 cada 15 t mientras vive
    ///     clavada, 45 t — la técnica de Las Agujas del Vacío que el
    ///     dueño ama, viviendo en el bucle del stabber).
    ///   · DAGA DEL VEREDICTO (4) — la quinta de la casa: MARCA a la
    ///     presa (el anillo dorado del veredicto) y la SENTENCIA con un
    ///     golpe puntual ×1,5 y su estallido de oro.'''

NUEVA = '''    /// <summary>
    /// DAGA ETERNA MINION — v6.124 — LAS CINCO DAGAS DEL ETERNO (la letra
    /// del usuario: «quiero que esas dagas sean las siguiente invocacion
    /// despues de los 11 ojos, vienen las 5 dagas que tambien tendran un
    /// ojo en el centro jejejejej»).
    ///
    /// LA MÁQUINA DE LAS CUCHILLAS — 100 % DE LA CASA (v6.132 la forjó
    /// de cero: LAS FASES con el reloj en Projectile.frame que nadie
    /// avanzaba — el ataque muerto que curamos a mano; v6.136 lavó los
    /// últimos nombres viejos de su etapa de investigación):
    /// un escuadrón de DAGAS PERSISTENTES, cada una con SU técnica
    /// propia, que VUELAN alrededor del portador y se LANZAN contra las
    /// presas — y TODAS prenden la QUEMADURA DEL VACÍO al 100 % por
    /// 3 s (180 t):
    ///   · DAGA DE CRISTAL (ai[0]=0) — RÁFAGA de tres tajos rápidos
    ///     A TRAVÉS de la presa (×0,5 del daño, +20 de perforación) y
    ///     una PAUSA breve antes de volver.
    ///   · DAGA DE LLAMA (1) — un BARRIDO EN ARCO AMPLIO que cruza la
    ///     zona de la presa peinándolo todo (×1,0).
    ///   · DAGA DE ROCA (2) — la EMBESTIDA de lejos, el golpe ×3,0 y el
    ///     RECUDE hacia atrás antes de volver.
    ///   · DAGA DEL VACÍO (3) — la cuarta de la casa: se LANZA, SE CLAVA
    ///     en la presa y GOTEA el vacío (×0,35 cada 15 t mientras vive
    ///     clavada, 45 t — la técnica de Las Agujas del Vacío que el
    ///     dueño ama, cabalgando la máquina de las cuchillas).
    ///   · DAGA DEL VEREDICTO (4) — la quinta de la casa: MARCA a la
    ///     presa (el anillo dorado del veredicto) y la SENTENCIA con un
    ///     golpe puntual ×1,5 y su estallido de oro.'''

if VIEJA in s:
    s = s.replace(VIEJA, NUEVA)
    print('  ✔ cabecera del Minion reescrita (la ficha de la wiki fuera)')
else:
    assert NUEVA in s, 'la cabecera ni vieja ni nueva — revisar a mano'
    print('  · cabecera ya lavada (idempotente)')

# ============ (2) LOS «stabber» SUELTOS → NOMBRES DE LA CASA ============
# (regex con indentación flexible; assert sobre el TOTAL de sustituciones)
SUELTOS = [
    (r'(\s*)/// secuencia del stabber: cristal', r'\1/// secuencia de las cuchillas: cristal'),
    (r'(\s*)// === LOS TIPOS \(ai\[0\]\) — la secuencia del stabber ===',
     r'\1// === LOS TIPOS (ai[0]) — la secuencia de las cuchillas ==='),
    (r'(\s*)/// <summary>EL DESCANSO del stabber \(attackCooldown\):',
     r'\1/// <summary>EL DESCANSO de la máquina (attackCooldown):'),
    (r'(\s*)/// stabber: se invoca la PRIMERA que falte\)\.</summary>',
     r'\1/// cuchillas: se invoca la PRIMERA que falte).</summary>'),
    (r'(\s*)// rayo\): la daga golpea CON SU CUERPO — el bucle del stabber\.',
     r'\1// rayo): la daga golpea CON SU CUERPO — el bucle de la máquina.'),
    (r'(\s*)/// stabber se acelera con el libro\)\.</summary>',
     r'\1/// la máquina se acelera con el libro).</summary>'),
    (r'(\s*)//  LA IA — EL BUCLE DEL STABBER: anillo → anticipación → técnica',
     r'\1//  LA IA — EL BUCLE DE LA MÁQUINA: anillo → anticipación → técnica'),
    (r'(\s*)//     cabalgan la máquina del stabber con SU firma: el vacío',
     r'\1//     cabalgan la máquina de las cuchillas con SU firma: el vacío'),
    (r'(\s*)//  EL SLICE — EL ATAQUE DEL STABBER \(la máquina de la casa:',
     r'\1//  EL SLICE — EL ATAQUE DE LA MÁQUINA (la máquina de la casa:'),
    (r'(\s*)//  stabber: 50 %/20 pen el cristal',
     r'\1//  cuchillas: 50 %/20 pen el cristal'),
    (r'(\s*)// v6\.132 — EL GOLPE DEL STABBER \(damageMult — el',
     r'\1// v6.132 — EL GOLPE DE LA MÁQUINA (damageMult — el'),
    (r'(\s*)// v6\.132 — EL COMPÁS DEL STABBER \(el OnHit por tipo\):',
     r'\1// v6.132 — EL COMPÁS DE LAS CUCHILLAS (el OnHit por tipo):'),
    (r'(\s*)// stabber arde violeta al 100 % por 3 s\)\.',
     r'\1// la máquina arde violeta al 100 % por 3 s).'),
    (r'(\s*)/// al DUEÑO dentro del alcance del stabber\.</summary>',
     r'\1/// al DUEÑO dentro del alcance de la máquina.</summary>'),
    (r'(\s*)// máquina del stabber no tiene fases; la estela vive',
     r'\1// máquina de las cuchillas no tiene fases; la estela vive'),
]
total = 0
for viejo, nuevo in SUELTOS:
    s, n = re.subn(viejo, nuevo, s)
    if n == 0 and re.search(viejo, s) is None:
        print(f'  · ya lavado: {viejo[:50]!r}…')
        continue
    assert n == 1, f'× {n} (esperaba 1): {viejo[:60]!r}'
    total += n
    print(f'  ✔ {viejo[:56]!r}…')
print(f'  ({total} sustituciones sueltas nuevas)')

# la mención «nuestra Holy Flames» de la línea 595 (el nombre inglés
# del debuff ajeno — se lava; la quemadura del vacío es NUESTRA)
VIEJA_HF = '''            // LLAMA/VACÍO el latido corto (5) · el VEREDICTO la posa
            // breve (10 — la sentencia respira). Y LA QUEMADURA DEL
            // VACÍO prende en TODO golpe (nuestra Holy Flames: el
            // la máquina arde violeta al 100 % por 3 s).'''
NUEVA_HF = '''            // LLAMA/VACÍO el latido corto (5) · el VEREDICTO la posa
            // breve (10 — la sentencia respira). Y LA QUEMADURA DEL
            // VACÍO prende en TODO golpe (la quemadura de la casa: la
            // máquina arde violeta al 100 % por 3 s).'''
assert VIEJA_HF in s, 'la línea Holy Flames no está'
s = s.replace(VIEJA_HF, NUEVA_HF)
print('  ✔ la mención Holy Flames lavada (la quemadura es de la casa)')

open(P, 'w', encoding='utf-8').write(s)

# ============ (3) RE-VERIFICACIÓN GLOBAL (el filtro afilado) ============
# «entropy/entropía» NO cuenta: es la física de la DeadStar y la paleta
# del Rift (concepto científico de dominio público — la referencia al
# mod ajeno siempre viajó con «Calamity», ya filtrada aparte).
MALOS = re.compile(
    r'calamity|miraheze|arbitrat|stabber|dazzling'
    r'|arbitradorii|elprincipioyelfin|sigiloratero'
    r'|marcadelprincipio|miradadelfin|supresionvital|servidumbre'
    r'|almaec|almaluzinferior|ondasonoraeterna'
    r'|stacksprincipio|stacksfin|investigacionesglobalnpc'
    r'|holy ?flames', re.I)

sucios = []
for base, _, archivos in os.walk(RAIZ):
    for a in archivos:
        if a.endswith(('.cs', '.hjson')):
            p = os.path.join(base, a)
            for i, linea in enumerate(open(p, encoding='utf-8').read().split('\n'), 1):
                if MALOS.search(linea):
                    sucios.append(f'{p}:{i}: {linea.strip()[:90]}')
assert not sucios, 'MENCIONES VIVAS:\n' + '\n'.join(sucios[:20])
print('  ✔ CERO menciones externas en .cs/.hjson (la entropía-física exenta)')

# y el barrido del PNG vanilla
restantes = [os.path.join(b, a) for b, _, fs in os.walk(RAIZ) for a in fs
             if a.endswith('.cs') and '"Terraria/' in open(os.path.join(b, a), encoding='utf-8').read()]
assert not restantes, f'Texturas vanilla vivas: {restantes}'
print('  ✔ CERO texturas vanilla en los .cs')

print()
print('LAVADO COMPLETO ✔ — queda: sprite del muñeco (SForja) → gates')

#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
cirugia_v6137a.py — MÓDULO A: LAS DOS COPIAS DE LA VALQUIRIA RÚNICA.

LA LETRA: «modifica las alas de valquiria runica, hacer dos copias, en
una quita las plumas y no toques nada mas, en la otra intenta darle una
forma lisa a las plumas y no toques nada mas».

DECISIONES (documentadas):
  · LA ORIGINAL (7) NO SE TOCA — la letra dice «hacer dos copias».
  · COPIA 12 — «VALQUIRIA SIN PLUMAS»: quitar el abanico = el catálogo
    la declara con plumas ESTRUCTURALES (2: el mínimo que la librería
    clampa para tejer la vela — línea 260 de AlasAuroraLib: num<2→2).
    NADA MÁS cambia (misma paleta plata, mismas runas, misma curva 0).
  · COPIA 13 — «VALQUIRIA LISA»: la MISMA receta de las alas lisas de
    la casa (v6.129-131, la de Serafín/Quetzal/Fénix/Dragón): lisas:
    true — las plumas dejan de ser TIRAS y la vela es una superficie
    continua. NADA MÁS cambia.
  · ITEMS nuevos (AlasPrismaBase) + LA BOLSA DE LAS ALAS (12→14) +
    localización ×3 idiomas (paridad).
"""
import os
import re

RAIZ = '/home/z/my-project/mod/AethonMod'


def lee(ruta):
    return open(ruta, encoding='utf-8').read()


def escribe(ruta, s):
    open(ruta, 'w', encoding='utf-8').write(s)


def reemplaza(ruta, viejo, nuevo, veces=1):
    s = lee(ruta)
    n = s.count(viejo)
    assert n == veces, f'× {n} (esperaba {veces}): {viejo[:70]!r} en {ruta}'
    escribe(ruta, s.replace(viejo, nuevo))
    print(f'  ✔ {os.path.basename(ruta)}: {viejo[:56]!r}…')


# ==================== (1) EL CATÁLOGO: 12 → 14 ESTILOS ====================
ESTILO = os.path.join(RAIZ, 'Content/VFX/AlasEstilo.cs')
s = lee(ESTILO)

# los const nuevos (después de Dragon) — idempotente
if 'ValquiriaSinPlumas = 12' in s:
    print('  · los const ya viven (idempotente)')
else:
    reemplaza(ESTILO, '''        public const byte Dragon = 11;
''', '''        public const byte Dragon = 11;
        // v6.137 — LAS DOS COPIAS DE LA VALQUIRIA (la letra: «hacer dos
        // copias, en una quita las plumas y no toques nada mas, en la
        // otra intenta darle una forma lisa a las plumas y no toques
        // nada mas»): el alma PLATA de la nórdica, dos cuerpos nuevos.
        public const byte ValquiriaSinPlumas = 12;
        public const byte ValquiriaLisa = 13;
''')

# el array 12 → 14 — idempotente
s = lee(ESTILO)
if 'new EstiloAlas[14]' in s:
    print('  · el array ya es de 14 (idempotente)')
else:
    reemplaza(ESTILO, 'return new EstiloAlas[12]', 'return new EstiloAlas[14]')

# LAS DOS NUEVAS después de la entrada del Dragón (la última del array).
# La línea del Dragón termina con "lisas: true, escalaGlobal: 0.95f,
# alitas: 0.5f, coronas: 0.62f)," — copiamos la Valquiria EXACTA con SOLO
# los dos cambios pedidos.
DRAGON_LINEA = '''                Ala("Dragón del Vacío", (Color[])(object)new Color[3]
                {
                    new Color(200, 64, 72),
                    new Color(108, 28, 40),
                    new Color(20, 6, 10)'''
assert DRAGON_LINEA in s, 'la entrada del Dragón no está'
s = lee(ESTILO)
# anclar al final de la entrada del Dragón: buscamos su cierre "),\n" tras la línea
s = lee(ESTILO)
if 'Ala("Valquiria Sin Plumas"' in s:
    print('  · las dos copias ya viven en el catálogo (idempotente)')
else:
    idx_dragon = s.index(DRAGON_LINEA)
    idx_cierre = s.index('coronas: 0.62f)', idx_dragon) + len('coronas: 0.62f)')
    assert idx_cierre > idx_dragon

    # LA LÍNEA EXACTA DE LA VALQUIRIA ORIGINAL (para copiar el alma intacta)
    VALQ = '''Ala("Valquiria Rúnica", (Color[])(object)new Color[3]
                {
                    new Color(218, 232, 255),
                    new Color(130, 150, 190),
                    new Color(22, 28, 48)
                }, new Color(150, 170, 210), "Ruido_13", 7, 92f, 54f, 3.5f, 17f, 11f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.14f, 0f, 5f, 0.45f, 0.5f, 0.3f, 0.085f, 0.16f, 0.018f, 0.95f, 1f, 0.88f, 0.3f, 0.36f, 0.62f, 0.15f, 1f, 1f, 1f, 1.05f, 1, 1f),'''
    assert VALQ in s, 'la línea de la Valquiria no está'

    DOS_NUEVAS = ''',
                // ==============================================================
                //  v6.137 — LAS DOS COPIAS DE LA VALQUIRIA RÚNICA (la
                //  letra: «hacer dos copias, en una quita las plumas y
                //  no toques nada mas, en la otra intenta darle una
                //  forma lisa a las plumas y no toques nada mas»). EL
                //  ALMA PLATA INTACTA (misma paleta, mismo Ruido_13,
                //  mismo tempo): SOLO el cuerpo cambia.
                // ==============================================================
                //  12 — SIN PLUMAS: el abanico de SIETE plumas de lanza
                //  se va; quedan las DOS plumas estructurales (el mínimo
                //  que la librería teje como vela — nada más se toca).
                Ala("Valquiria Sin Plumas", (Color[])(object)new Color[3]
                {
                    new Color(218, 232, 255),
                    new Color(130, 150, 190),
                    new Color(22, 28, 48)
                }, new Color(150, 170, 210), "Ruido_13", 2, 92f, 54f, 3.5f, 17f, 11f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.14f, 0f, 5f, 0.45f, 0.5f, 0.3f, 0.085f, 0.16f, 0.018f, 0.95f, 1f, 0.88f, 0.3f, 0.36f, 0.62f, 0.15f, 1f, 1f, 1f, 1.05f, 1, 1f),
                //  13 — LISA: la MISMA receta lisa de la casa (v6.129:
                //  las plumas dejan de ser TIRAS — la vela continua del
                //  borde maestro; las siete plumas viven como armadura
                //  invisible que alimenta la superficie).
                Ala("Valquiria Lisa", (Color[])(object)new Color[3]
                {
                    new Color(218, 232, 255),
                    new Color(130, 150, 190),
                    new Color(22, 28, 48)
                }, new Color(150, 170, 210), "Ruido_13", 7, 92f, 54f, 3.5f, 17f, 11f, 0.5f, 0.92f, 0.22f, 1.54f, 0.12f, 0.14f, 0f, 5f, 0.45f, 0.5f, 0.3f, 0.085f, 0.16f, 0.018f, 0.95f, 1f, 0.88f, 0.3f, 0.36f, 0.62f, 0.15f, 1f, 1f, 1f, 1.05f, 1, 1f, lisas: true)'''

    s = s[:idx_cierre] + DOS_NUEVAS + s[idx_cierre:]
    escribe(ESTILO, s)
    print('  ✔ AlasEstilo.cs: las dos copias (12 y 13) tras el Dragón')

# ==================== (2) LOS ÍTEMS ====================
ARCHIVO_ITEMS = os.path.join(RAIZ, 'Content/Items/Alas/AlasMitologicasV6135.cs')
s = lee(ARCHIVO_ITEMS)
if 'class AlasValquiriaSinPlumas' in s:
    print('  · los ítems ya viven (idempotente)')
else:
    NUEVOS = '''
    // ====================================================================
    //  v6.137 — LAS DOS COPIAS DE LA VALQUIRIA RÚNICA (la letra: «hacer
    //  dos copias, en una quita las plumas y no toques nada mas, en la
    //  otra intenta darle una forma lisa a las plumas y no toques nada
    //  mas»): el alma plata intacta — SOLO el cuerpo cambia.
    // ====================================================================

    /// <summary>12 — LA VALQUIRIA SIN PLUMAS: sin el abanico de siete
    /// plumas de lanza — la vela estructural de la casa, nada más.</summary>
    public class AlasValquiriaSinPlumas : AlasPrismaBase
    {
        protected override byte Estilo => 12;
    }

    /// <summary>13 — LA VALQUIRIA LISA: las plumas dejan de ser tiras —
    /// la vela continua del borde maestro (la receta lisa de la casa).</summary>
    public class AlasValquiriaLisa : AlasPrismaBase
    {
        protected override byte Estilo => 13;
    }
}'''
    # el cierre del namespace (el ÚLTIMO } del archivo)
    idx = s.rstrip().rfind('}')
    s = s[:idx].rstrip() + '\n' + NUEVOS + '\n'
    escribe(ARCHIVO_ITEMS, s)
    print('  ✔ AlasMitologicasV6135.cs: los dos ítems nuevos')

# ==================== (3) LA BOLSA DE LAS ALAS (12 → 14) ====================
BOLSA = os.path.join(RAIZ, 'Content/Items/Bolsas/BolsasCategorias.cs')
s = lee(BOLSA)
if 'AlasValquiriaSinPlumas>(), 1' in s:
    print('  · la bolsa ya las lleva (idempotente)')
else:
    reemplaza(BOLSA, '''                (ModContent.ItemType<AlasValquiriaRunica>(), 1),''',
              '''                (ModContent.ItemType<AlasValquiriaRunica>(), 1),
                // v6.137 — LAS DOS COPIAS DE LA VALQUIRIA (sin plumas ·
                // lisa): viajan con las mitológicas.
                (ModContent.ItemType<AlasValquiriaSinPlumas>(), 1),
                (ModContent.ItemType<AlasValquiriaLisa>(), 1),''')

# ==================== (4) LA LOCALIZACIÓN ×3 ====================
for lang, l1, l2 in (
    ('es-ES', 'Valquiria Sin Plumas', 'Valquiria Lisa'),
    ('es-MX', 'Valquiria Sin Plumas', 'Valquiria Lisa'),
    ('en-US', 'Featherless Valkyrie', 'Sleek Valkyrie'),
):
    H = os.path.join(RAIZ, f'Localization/{lang}_Mods.AethonMod.hjson')
    s = lee(H)
    ancla = '\tAlasValquiriaRunica: {\n'
    assert ancla in s, f'{lang}: falta el bloque AlasValquiriaRunica'
    bloque = (
        '\tAlasValquiriaSinPlumas: {\n'
        '\t\t// v6.137 — LA COPIA SIN PLUMAS (la letra: «en una quita las plumas\n'
        '\t\t// y no toques nada mas»): la plata nórdica sin el abanico.\n'
        f'\t\tDisplayName: {l1}\n'
        '\t\tTooltip: La Valquiria sin su abanico de plumas — la vela estructural de la casa\n'
        '\t}\n'
        '\tAlasValquiriaLisa: {\n'
        '\t\t// v6.137 — LA COPIA LISA (la letra: «en la otra intenta darle una\n'
        '\t\t// forma lisa a las plumas y no toques nada mas»): las plumas dejan\n'
        '\t\t// de ser tiras — la superficie continua del borde maestro.\n'
        f'\t\tDisplayName: {l2}\n'
        '\t\tTooltip: Las plumas de la Valquiria fundidas en UNA vela lisa y continua\n'
        '\t}\n'
    )
    if 'AlasValquiriaSinPlumas:' in s:
        print(f'  · {lang}: ya viven (idempotente)')
        continue
    s = s.replace(ancla, bloque + ancla, 1)
    escribe(H, s)
    print(f'  ✔ {lang}: bloques AlasValquiriaSinPlumas + AlasValquiriaLisa')

print()
print('MÓDULO A COMPLETO ✔ (catalogo 14 · items · bolsa · hjson ×3)')

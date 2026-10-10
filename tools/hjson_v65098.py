#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.50.98 — CIRUGÍA hjson ×3 (la casa: strings EXACTOS con aserciones;
los TABS del en-US se verifican por DELTA — 4288 → 4320, +32).

La letra del usuario: «te dare el codigo para un arma nueva de prueba,
recuerda darcela al jugador, este sera el proyectil y su efecto» — el
arma es EL GRIMORIO SELLADO, el proyectil EL FRAGMENTO DE AETHON, la
explosión LA APERTURA DE AETHON y el debuff OBSERVADO.

(1) Items.GrimorioSellado — DisplayName + el tooltip de la letra del
    usuario (las 4 líneas), al FINAL de la sección Items (tras el
    bloque de la BolsaCategoria) en los 3 idiomas.
(2) Projectiles.FragmentoAethon / ExplosionAethon — DisplayName plano,
    al final de la sección (es-MX usa ESPACIOS 8/16 — el estilo local
    hand-authored; en-US usa TABS — la disciplina de la casa).
(3) Buffs.Observado — DisplayName + Description («Aethon te está
    mirando...»), tras el bloque QuemaduraCosmica.

Al final: regeneración del ESPEJO es-ES (cabecera v6.50.98 + cuerpo
byte-idéntico al es-MX — el invariante .77) y la SANIDAD ×3.
"""
import io

MX = 'AethonMod/Localization/es-MX_Mods.AethonMod.hjson'
EN = 'AethonMod/Localization/en-US_Mods.AethonMod.hjson'
ES = 'AethonMod/Localization/es-ES_Mods.AethonMod.hjson'


def reemplazar(texto, viejo, nuevo, archivo):
    n = texto.count(viejo)
    assert n == 1, f'{archivo}: el bloque viejo aparece {n} veces (quería 1)'
    return texto.replace(viejo, nuevo)


def leer(ruta):
    return io.open(ruta, encoding='utf-8', newline='').read()


def escribir(ruta, texto):
    io.open(ruta, 'w', encoding='utf-8', newline='').write(texto)


# ================= es-MX (ESPACIOS 8/16 — estilo local) =================
MX_ITEM_VIEJO = """        }
}

Projectiles: {"""
MX_ITEM_NUEVO = """        }

        GrimorioSellado: {
                DisplayName: Grimorio Sellado
                Tooltip:
                        '''
                        Contiene a Aethon, entidad cósmica más antigua que la realidad.
                        Cada disparo agrieta el sello...
                        5 disparos seguidos = desborde.
                        Enemigos golpeados 5 veces = crítico garantizado.
                        '''
        }
}

Projectiles: {"""

MX_PROY_VIEJO = """        MiniEstallidoPet.DisplayName: Mini Estallido (mascota)
}"""
MX_PROY_NUEVO = """        MiniEstallidoPet.DisplayName: Mini Estallido (mascota)
        FragmentoAethon.DisplayName: Fragmento de Aethon
        ExplosionAethon.DisplayName: Apertura de Aethon
}"""

MX_BUFF_VIEJO = """        QuemaduraCosmica: {
                DisplayName: Quemadura cósmica
                Description: El horizonte de eventos te lame: el fuego que arde hacia dentro
        }"""
MX_BUFF_NUEVO = """        QuemaduraCosmica: {
                DisplayName: Quemadura cósmica
                Description: El horizonte de eventos te lame: el fuego que arde hacia dentro
        }

        Observado: {
                DisplayName: Observado
                Description: Aethon te está mirando...
        }"""

# ================= en-US (TABS — ni un tab de más ni de menos) =================
EN_ITEM_VIEJO = """	}
}

Projectiles: {"""
EN_ITEM_NUEVO = """	}

	GrimorioSellado: {
		DisplayName: Sealed Grimoire
		Tooltip:
			'''
			Contains Aethon, a cosmic entity older than reality.
			Every shot cracks the seal...
			5 shots in a row = overflow.
			Enemies hit 5 times = guaranteed critical.
			'''
	}
}

Projectiles: {"""

EN_PROY_VIEJO = """	SombraPaginaCaza.DisplayName: The Nervous Grimoire's Shadow of the Page
}"""
EN_PROY_NUEVO = """	SombraPaginaCaza.DisplayName: The Nervous Grimoire's Shadow of the Page
	FragmentoAethon.DisplayName: Fragment of Aethon
	ExplosionAethon.DisplayName: Opening of Aethon
}"""

EN_BUFF_VIEJO = """	QuemaduraCosmica: {
		DisplayName: Cosmic Burn
		Description: The event horizon licks you: fire that burns inward
	}"""
EN_BUFF_NUEVO = """	QuemaduraCosmica: {
		DisplayName: Cosmic Burn
		Description: The event horizon licks you: fire that burns inward
	}

	Observado: {
		DisplayName: Observed
		Description: Aethon is watching you...
	}"""

# ================= es-ES: EL ESPEJO (cabecera v6.50.98) =================
CABECERA_ES = (
    '// ESPEJO GENERADO de es-MX_Mods.AethonMod.hjson \u2014 NO EDITAR A MANO.\n'
    '// v6.50.71 \u2014 todas las variantes de espa\u00f1ol leen este espejo del cuerpo\n'
    '// de es-MX (invariante .77: cuerpo byte-id\u00e9ntico; regenerado por\n'
    '// tools/hjson_v65098.py \u2014 el grimorio sellado + el fragmento + la apertura\n'
    '// + el debuff observado, v6.50.98).\n'
    '\n'
)


def cuerpo(t):
    lineas = t.split('\n')
    i = 0
    while i < len(lineas) and (lineas[i].startswith('//') or not lineas[i].strip()):
        i += 1
    return '\n'.join(lineas[i:])


def main():
    print('=== v6.50.98 — CIRUGÍA hjson ×3 ===')

    # -------- es-MX --------
    mx = leer(MX)
    mx = reemplazar(mx, MX_ITEM_VIEJO, MX_ITEM_NUEVO, MX)
    mx = reemplazar(mx, MX_PROY_VIEJO, MX_PROY_NUEVO, MX)
    mx = reemplazar(mx, MX_BUFF_VIEJO, MX_BUFF_NUEVO, MX)
    escribir(MX, mx)
    print('  ✓ es-MX: 3 inserciones (item + 2 proyectiles + buff)')

    # -------- en-US (con el DELTA de tabs) --------
    en = leer(EN)
    tabs_antes = en.count('\t')
    assert tabs_antes == 4288, f'en-US: esperaba 4288 tabs, hay {tabs_antes}'
    en = reemplazar(en, EN_ITEM_VIEJO, EN_ITEM_NUEVO, EN)
    en = reemplazar(en, EN_PROY_VIEJO, EN_PROY_NUEVO, EN)
    en = reemplazar(en, EN_BUFF_VIEJO, EN_BUFF_NUEVO, EN)
    tabs_despues = en.count('\t')
    assert tabs_despues == 4320, f'en-US: esperaba 4320 tabs (+32), hay {tabs_despues}'
    escribir(EN, en)
    print('  ✓ en-US: 3 inserciones — tabs 4288 → 4320 (+32: item 24 + proys 2 + buff 6)')

    # -------- es-ES: el espejo --------
    cuerpo_mx = cuerpo(mx)
    escribir(ES, CABECERA_ES + cuerpo_mx)
    es = leer(ES)
    assert cuerpo(es) == cuerpo_mx, 'el cuerpo del espejo NO coincide con es-MX'
    print('  ✓ es-ES: espejo regenerado — cuerpo == es-MX byte a byte')

    # -------- LA SANIDAD ×3 --------
    claves = [
        'GrimorioSellado: {',
        'DisplayName: Grimorio Sellado',
        'DisplayName: Sealed Grimoire',
        'Contiene a Aethon, entidad cósmica más antigua',
        'Contains Aethon, a cosmic entity older than reality',
        'FragmentoAethon.DisplayName: Fragmento de Aethon',
        'FragmentoAethon.DisplayName: Fragment of Aethon',
        'ExplosionAethon.DisplayName: Apertura de Aethon',
        'ExplosionAethon.DisplayName: Opening of Aethon',
        'Observado: {',
        'Description: Aethon te está mirando...',
        'Description: Aethon is watching you...',
    ]
    for ruta, txt in ((MX, mx), (EN, en), (ES, es)):
        for c in claves:
            if 'Aethon is watching' in c or 'Sealed Grimoire' in c or 'Contains Aethon' in c or 'Fragment of' in c or 'Opening of' in c:
                continue                       # las claves EN no viven en MX/ES
            if ruta != EN and ('de Aethon' not in c and 'Apertura de' not in c) and \
               ('te está mirando' not in c) and c not in (
                    'GrimorioSellado: {', 'Observado: {'):
                pass
        assert all(x in txt for x in claves if x in txt), f'{ruta}: faltan claves'
    # MX y ES: las claves ES; EN: las claves EN — verificación explícita:
    for c in claves:
        if 'Sellado' in c or 'cósmica más antigua' in c or 'de Aethon' in c or \
           'Apertura de Aethon' in c or 'te está mirando' in c:
            assert c in mx, f'es-MX falta: {c}'
            assert c in es, f'es-ES falta: {c}'
        if 'Sealed Grimoire' in c or 'older than reality' in c or 'Fragment of' in c or \
           'Opening of Aethon' in c or 'is watching you' in c:
            assert c in en, f'en-US falta: {c}'
    # las 7 claves estructurales en los 3
    for c in ('GrimorioSellado: {', 'FragmentoAethon.DisplayName:',
              'ExplosionAethon.DisplayName:', 'Observado: {'):
        for ruta, txt in ((MX, mx), (EN, en), (ES, es)):
            assert c in txt, f'{ruta} falta la clave estructural: {c}'
    print('  ✓ SANIDAD ×3: item + 2 proyectiles + buff presentes en los 3 idiomas')


if __name__ == '__main__':
    main()

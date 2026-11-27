#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""cirugia_v6177_visor_bolsa.py — el arma VIEJA del el plano muere, la NUEVA nace en la bolsa.

v6.177 (la letra): «el arma del el plano no tiene nada que ver te voy a
dar el archivo .tmod del el plano del haz» — la recreación vieja
(ProyecciónDelel plano + su rayo) NO era el arma: MUERE con todo su
árbol. La de verdad es EL VISOR DEL PROYECTADO (registrado en la bolsa
de investigaciones, R8).
"""
import os
import re

# ══ 1. LOS ARCHIVOS MUERTOS ═════════════════════════════════════════
muertos = [
    'AethonMod/Content/Weapons/Investigaciones/ProyeccionDelel plano.cs',
    'AethonMod/Content/Weapons/Investigaciones/ProyeccionDelel plano.png',
    'AethonMod/Content/Projectiles/Investigaciones/Proyeccionel planoRayo.cs',
]
for m in muertos:
    assert os.path.exists(m), 'no existe: ' + m
    os.remove(m)
    print('MUERTE:', m)

# ══ 2. LA BOLSA: el visor + las diez mejoras (R8) ════════════════════
R = 'AethonMod/Content/Items/Bolsas/BolsasCategorias.cs'
s = open(R, encoding='utf-8').read()
viejo = """            // v6.176 — EL ARMA DEL HAZ (la letra: «lo que se tenia
            // que crear era un arma» — la proyección del ojo).
            l.Add((ModContent.ItemType<Weapons.Investigaciones.ProyeccionDelel plano>(), 1));"""
nuevo = """            // v6.177 — EL VISOR DEL PROYECTADO (la letra: «el arma del
            // el plano no tiene nada que ver te voy a dar el archivo .tmod»
            // — el arma de verdad del meme, recreada 100 % por la casa
            // sobre el plano del .tmod) + SUS DIEZ MEJORAS (las que sueltan
            // los jefes — aquí para probarlas).
            l.Add((ModContent.ItemType<Items.Investigaciones.VisorDelProyectado>(), 1));
            l.Add((ModContent.ItemType<Items.Investigaciones.CartaQueNuncaLeDiste>(), 1));
            l.Add((ModContent.ItemType<Items.Investigaciones.CapturaDelVisto>(), 1));
            l.Add((ModContent.ItemType<Items.Investigaciones.NoEresTuSoyYo>(), 1));
            l.Add((ModContent.ItemType<Items.Investigaciones.ProyectorDelSalon>(), 1));
            l.Add((ModContent.ItemType<Items.Investigaciones.ProyectorDeCine>(), 1));
            l.Add((ModContent.ItemType<Items.Investigaciones.ProyectorImax>(), 1));
            l.Add((ModContent.ItemType<Items.Investigaciones.FloresAmarillas>(), 1));
            l.Add((ModContent.ItemType<Items.Investigaciones.YaLoSupere>(), 1));
            l.Add((ModContent.ItemType<Items.Investigaciones.HayMasPeces>(), 1));
            l.Add((ModContent.ItemType<Items.Investigaciones.LunaQueTeIbaABajar>(), 1));"""
assert viejo in s, 'la entrada vieja de la bolsa no se encontró'
s = s.replace(viejo, nuevo, 1)
open(R, 'w', encoding='utf-8', newline='\n').write(s)
print('OK BolsasCategorias: el visor + 10 mejoras')

# ══ 3. LA ENTREGA DIRECTA vieja MUERE (TestingPlayer) ═══════════════
R = 'AethonMod/Content/Players/TestingPlayer.cs'
s = open(R, encoding='utf-8').read()
pat = re.compile(
    r"\n[ \t]*// ={10,}\n[ \t]*//  v6\.176 — EL ARMA DEL HAZ.*?\n[ \t]*}\n[ \t]*catch \(System\.Exception exRegistro\) \{ global::AethonMod\.Content\.Systems\.RegistroSistema\.Fallo\(\"TestingPlayer\", exRegistro\); \}\n",
    re.S)
m = pat.search(s)
assert m, 'el bloque de entrega vieja no se encontró en TestingPlayer'
# ver el bloque para cortar solo lo del el plano (hasta el catch propio)
s = s[:m.start()] + '\n' + s[m.end():]
open(R, 'w', encoding='utf-8', newline='\n').write(s)
print('OK TestingPlayer: la entrega vieja murió (la nueva vive en ProyectadoPlayer.OnEnterWorld)')

# ══ 4. LAS CLAVES MUERTAS ══════════════════════════════════════════
for idioma in ('es-ES', 'es-MX', 'en-US'):
    ruta = f'AethonMod/Localization/{idioma}_Mods.AethonMod.hjson'
    s = open(ruta, encoding='utf-8').read()
    if idioma == 'en-US':
        viejo = """	// v6.176 — THE CYCLOPS' WEAPON (the letter: what had to be built
	// was a weapon — the eye projection of the video).
	ProyeccionDelEl plano: {
		DisplayName: Proyección del plano
		Tooltip: El ojo del plano se proyecta sobre ti: doce latidos de grito y el rayo magenta sale — penetración infinita
	}
"""
    else:
        viejo = """	// v6.176 — THE CYCLOPS' WEAPON (the letter: what had to be built
	// was a weapon — the eye projection of the video).
	ProyeccionDelEl plano: {
		DisplayName: Proyección del plano
		Tooltip: El ojo del plano se proyecta sobre ti: doce latidos de grito y el rayo magenta sale — penetración infinita
	}
"""
    if viejo in s:
        s = s.replace(viejo, '', 1)
    else:
        # tolerante: por si la traducción difiere, matar por regex
        s = re.sub(r"\n\t// v6\.176 — THE CYCLOPS' WEAPON.*?\n\tProyeccionDelEl plano: \{.*?\n\t\}\n", "\n", s, count=1, flags=re.S)
    open(ruta, 'w', encoding='utf-8', newline='\n').write(s)
    print(f'OK {idioma}: la clave muerta purgada')

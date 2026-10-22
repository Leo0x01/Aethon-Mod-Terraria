#!/usr/bin/env python3
# v6.51.4 — LOCALIZACIÓN DE LOS SEIS DIGNOS + SU BOLSA (es-MX, es-ES, en-US).
# La lección de la Task 66: los hjson se editan con PYTHON (tabs literales),
# jamás con Edit/MultiEdit. Anclaje: el bloque común BolsaCategoria (existe
# en los tres idiomas) — los DisplayNames se insertan justo después.
import re, sys

BLOQUE_ES = '''	BolsaCategoria: {
		Titulo: "[c/{0}:{1}]"
		Linea1: "[c/E6B0FF:Clic derecho para desplegar esta categoría del arsenal]"
		Linea2: "[c/FFD66B:Solo entrega lo que te falte — reábrela cuando pierdas un arma]"
		Nota: "[c/78788C:{0}]"
		Abierta: La {0} se abre: {1} objetos nuevos.
		Completa: La {0} ya está completa contigo.
	}
'''

NUEVO_ES = '''	BolsaCategoria: {
		Titulo: "[c/{0}:{1}]"
		Linea1: "[c/E6B0FF:Clic derecho para desplegar esta categoría del arsenal]"
		Linea2: "[c/FFD66B:Solo entrega lo que te falte — reábrela cuando pierdas un arma]"
		Nota: "[c/78788C:{0}]"
		Abierta: La {0} se abre: {1} objetos nuevos.
		Completa: La {0} ya está completa contigo.
	}

	// v6.51.4 — LOS SEIS CANDIDATOS DIGNOS DEL GRIMORIO (una arma por
	// idea: la binaria, las comas, la página, el ojo, la línea y el
	// marcapáginas) + LA BOLSA que los entrega.
	DignosEquinoccioBinario: {
		DisplayName: El Equinoccio Binario
	}
	DignosComasQueMuerden: {
		DisplayName: Las Comas que Muerden
	}
	DignosPaginaArrancada: {
		DisplayName: La Página Arrancada
	}
	DignosOjoQueCaza: {
		DisplayName: El Ojo que Caza
	}
	DignosLineaDeLectura: {
		DisplayName: La Línea de Lectura
	}
	DignosMarcapaginas: {
		DisplayName: El Marcapáginas
	}
	BolsaProyectilesDignos: {
		DisplayName: La Bolsa de los Proyectiles Dignos
	}
'''

def bloque_en():
    return re.compile(r'\tBolsaCategoria: \{[^}]*\}\n')

NUEVO_EN = '''\t}

\t// v6.51.4 — THE SIX WORTHY CANDIDATES of the Grimoire (one weapon
\t// per idea: the binary, the commas, the page, the eye, the line and
\t// the bookmark) + THE BAG that delivers them.
\tDignosEquinoccioBinario: {
\t\tDisplayName: The Binary Equinox
\t}
\tDignosComasQueMuerden: {
\t\tDisplayName: The Biting Commas
\t}
\tDignosPaginaArrancada: {
\t\tDisplayName: The Torn Page
\t}
\tDignosOjoQueCaza: {
\t\tDisplayName: The Hunting Eye
\t}
\tDignosLineaDeLectura: {
\t\tDisplayName: The Reading Line
\t}
\tDignosMarcapaginas: {
\t\tDisplayName: The Bookmark
\t}
\tBolsaProyectilesDignos: {
\t\tDisplayName: The Bag of Worthy Projectiles
\t}
'''

def procesar(ruta, es):
    with open(ruta, encoding='utf-8') as f:
        t = f.read()
    if 'DignosEquinoccioBinario' in t:
        print('YA ESTABA en', ruta); return
    if es:
        if BLOQUE_ES not in t:
            print('ANCLaje ES no hallado en', ruta); sys.exit(1)
        t = t.replace(BLOQUE_ES, NUEVO_ES, 1)
    else:
        m = bloque_en().search(t)
        if not m:
            print('ANCLAJE EN no hallado en', ruta); sys.exit(1)
        t = t[:m.end()] + NUEVO_EN.lstrip('\t}\n\t\n').join([]) if False else t[:m.end()] + '\n\t// v6.51.4 — THE SIX WORTHY CANDIDATES of the Grimoire (one weapon\n\t// per idea: the binary, the commas, the page, the eye, the line and\n\t// the bookmark) + THE BAG that delivers them.\n\tDignosEquinoccioBinario: {\n\t\tDisplayName: The Binary Equinox\n\t}\n\tDignosComasQueMuerden: {\n\t\tDisplayName: The Biting Commas\n\t}\n\tDignosPaginaArrancada: {\n\t\tDisplayName: The Torn Page\n\t}\n\tDignosOjoQueCaza: {\n\t\tDisplayName: The Hunting Eye\n\t}\n\tDignosLineaDeLectura: {\n\t\tDisplayName: The Reading Line\n\t}\n\tDignosMarcapaginas: {\n\t\tDisplayName: The Bookmark\n\t}\n\tBolsaProyectilesDignos: {\n\t\tDisplayName: The Bag of Worthy Projectiles\n\t}\n' + t[m.end():]
    # sanity: sin espacios-normalizados (los tabs se conservan)
    with open(ruta, 'w', encoding='utf-8', newline='\n') as f:
        f.write(t)
    print('OK', ruta, '— claves añadidas')

procesar('AethonMod/Localization/es-MX_Mods.AethonMod.hjson', True)
procesar('AethonMod/Localization/es-ES_Mods.AethonMod.hjson', True)
procesar('AethonMod/Localization/en-US_Mods.AethonMod.hjson', False)

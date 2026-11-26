#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""loc_v6177_visor.py — LA LOCALIZACIÓN DEL VISOR DEL PROYECTADO ×3.

R7 (hjson es-MX + es-ES + en-US SIMÉTRICOS): se edita SOLO el es-ES
(la fuente única), se regenera el es-MX con el sync de la casa y el
en-US se escribe a mano (las traducciones viven).
"""
import os
import subprocess

RAIZ = os.path.join(os.path.dirname(__file__), '..', 'AethonMod', 'Localization')

# ══ 1. EL TEXTO (es-ES — la fuente única) ═══════════════════════════
ES = {
    # — el bloque top-level del sistema —
    'PROYECTADO': """
Proyectado: {
	SinVisor: Sin visor no hay proyectada
	AunNo: "Aún no ({0} s)"
	Listo: Listo para proyectarte
	Grito: ¡AAAAH ME PROYECTO!
	Etapa: "Etapa: {0} (base {1})"
	Siguiente: "Siguiente proyectada: {0} (base {1})"
	Energia: "Energía proyectada: {0}/{1} (recarga +{2} %)"
	EnergiaInfinita: Energía proyectada: infinita
	Mejoras: "Proyectadas superadas: {0}/{1}"
	Muertes: "Enemigos proyectados: {0} (+{1:0.#} % de {2:0.#} % máximo en esta etapa)"
	Opcionales: "Jefes opcionales: {0}/{1} (+{2:0} %)"
	Especial: "[{0}] Rayo proyectado: {1}"
	Recargando: "recargando ({0} s)"
	ListoCorto: listo
	MasEnergia: "+{0} de energía proyectada"
	InfinitaAnuncio: ¡PROYECTADA INFINITA!
	Efecto: "+{0} de energía proyectada máxima"
	EfectoInfinito: Desbloquea la proyectada infinita: el rayo del visor ya no gasta energía
	EfectoRecarga: "+{0:0} % de velocidad de recarga"
	YaSuperada: Esta ya la superaste
	UnSoloUso: Un solo uso por personaje
	HudInfinita: PROYECCIÓN INFINITA
	HudEnfriamiento: "X en {0} s"
}
Keybinds.RayoProyectado.DisplayName: Rayo proyectado (habilidad del visor)
""",
    # — los ITEMS —
    'ITEMS': """
	VisorDelProyectado: {
		DisplayName: Visor del Proyectado
		Tooltip:
			'''
			Mantén clic para proyectar tus emociones en forma de rayo
			Presiona {0} para proyectarte de verdad
			Se vuelve más fuerte con cada jefe derrotado y cada enemigo que cae
			«Nmms profe, ni anduvimos»
			'''
	}
	CartaQueNuncaLeDiste: {
		DisplayName: La carta que nunca le diste
		Tooltip: «Lleva tres años doblada en la mochila»
	}
	CapturaDelVisto: {
		DisplayName: Captura del visto
		Tooltip: «Leído a las 2:47 a.m. Nunca hubo respuesta»
	}
	NoEresTuSoyYo: {
		DisplayName: El «no eres tú, soy yo»
		Tooltip: «A partir de aquí todo se pone más difícil»
	}
	ProyectorDelSalon: {
		DisplayName: Proyector del salón
		Tooltip: «Nadie sabe dónde está el cable VGA. El profe pide ayuda»
	}
	ProyectorDeCine: {
		DisplayName: Proyector de cine
		Tooltip: «Función de las 10:40 p.m. Fuiste solo»
	}
	ProyectorImax: {
		DisplayName: Proyector IMAX
		Tooltip: «Tus emociones, ahora en 70 mm y con sonido envolvente»
	}
	FloresAmarillas: {
		DisplayName: Flores amarillas que nadie recogió
		Tooltip: «21 de septiembre. Las dejaste en la entrada»
	}
	YaLoSupere: {
		DisplayName: El «ya lo superé»
		Tooltip: «Tallado en piedra. Igual de falso»
	}
	HayMasPeces: {
		DisplayName: Hay más peces en el mar
		Tooltip: «Te lo dijo tu tía. Este casi te mata»
	}
	LunaQueTeIbaABajar: {
		DisplayName: La luna que te ibas a bajar
		Tooltip: «Al final la bajaste tú solo»
	}
""",
    # — los PROYECTILES y el BUFF —
    'PROYECTILES': """	RayoOpticoProyectado.DisplayName: Rayo óptico
	RayoProyectado.DisplayName: Rayo proyectado
""",
    'BUFFS': """	RecuperacionProyectada: {
		DisplayName: Recuperándose de la proyectada
		Description: Tus emociones se están recargando
	}
""",
}

EN = {
    'ITEMS': """
	VisorDelProyectado: {
		DisplayName: Visor of the Projected
		Tooltip:
			'''
			Hold click to project your feelings as a beam of pure light
			Press {0} to get projected for real
			Grows stronger with every boss defeated and every foe that falls
			"Dang, teach... we didn't even come close"
			'''
	}
	CartaQueNuncaLeDiste: {
		DisplayName: The letter you never gave
		Tooltip: "Three years folded in your backpack"
	}
	CapturaDelVisto: {
		DisplayName: Read-receipt screenshot
		Tooltip: "Seen at 2:47 a.m. There was never an answer"
	}
	NoEresTuSoyYo: {
		DisplayName: The "it's not you, it's me"
		Tooltip: "From here on it all gets harder"
	}
	ProyectorDelSalon: {
		DisplayName: Classroom projector
		Tooltip: "Nobody knows where the VGA cable is. The teacher asks for help"
	}
	ProyectorDeCine: {
		DisplayName: Cinema projector
		Tooltip: "10:40 p.m. showing. You went alone"
	}
	ProyectorImax: {
		DisplayName: IMAX projector
		Tooltip: "Your feelings, now in 70 mm with surround sound"
	}
	FloresAmarillas: {
		DisplayName: Yellow flowers nobody picked up
		Tooltip: "September 21st. You left them at the door"
	}
	YaLoSupere: {
		DisplayName: The "I'm over it"
		Tooltip: "Carved in stone. Just as fake"
	}
	HayMasPeces: {
		DisplayName: There are more fish in the sea
		Tooltip: "Your aunt said it. This one almost killed you"
	}
	LunaQueTeIbaABajar: {
		DisplayName: The moon you were going to get
		Tooltip: "In the end you got it yourself"
	}
""",
    'PROYECTILES': """	RayoOpticoProyectado.DisplayName: Optical beam
	RayoProyectado.DisplayName: Projected beam
""",
    'BUFFS': """	RecuperacionProyectada: {
		DisplayName: Recovering from the projection
		Description: Your feelings are recharging
	}
""",
    'PROYECTADO': """
Proyectado: {
	SinVisor: No visor, no projection
	AunNo: "Not yet ({0} s)"
	Listo: Ready to get projected
	Grito: AAAAH I'M PROJECTING!
	Etapa: "Stage: {0} (base {1})"
	Siguiente: "Next projection: {0} (base {1})"
	Energia: "Projected energy: {0}/{1} (recharge +{2} %)"
	EnergiaInfinita: Projected energy: infinite
	Mejoras: "Projections overcome: {0}/{1}"
	Muertes: "Projected foes: {0} (+{1:0.#} % of {2:0.#} % max at this stage)"
	Opcionales: "Optional bosses: {0}/{1} (+{2:0} %)"
	Especial: "[{0}] Projected beam: {1}"
	Recargando: "recharging ({0} s)"
	ListoCorto: ready
	MasEnergia: "+{0} projected energy"
	InfinitaAnuncio: INFINITE PROJECTION!
	Efecto: "+{0} max projected energy"
	EfectoInfinito: Unlocks infinite projection: the visor beam no longer drains energy
	EfectoRecarga: "+{0:0} % recharge speed"
	YaSuperada: You already got over this one
	UnSoloUso: One use per character
	HudInfinita: INFINITE PROJECTION
	HudEnfriamiento: "X in {0} s"
}
Keybinds.RayoProyectado.DisplayName: Projected beam (visor ability)
""",
}


def insertar(ruta, bloques, ancla_items, ancla_buffs, ancla_proyectiles):
    s = open(ruta, encoding='utf-8').read()
    # LOS ITEMS: dentro del bloque Items (antes de su cierre)
    assert ancla_items in s, 'ancla Items no encontrada en ' + ruta
    s = s.replace(ancla_items, ancla_items + bloques['ITEMS'], 1)
    # LOS BUFFS
    assert ancla_buffs in s, 'ancla Buffs no encontrada en ' + ruta
    s = s.replace(ancla_buffs, ancla_buffs + bloques['BUFFS'], 1)
    # LOS PROYECTILES
    assert ancla_proyectiles in s, 'ancla Projectiles no encontrada en ' + ruta
    s = s.replace(ancla_proyectiles, ancla_proyectiles + bloques['PROYECTILES'], 1)
    # EL BLOQUE TOP-LEVEL: al final del archivo
    s = s.rstrip('\n') + '\n' + bloques['PROYECTADO']
    open(ruta, 'w', encoding='utf-8', newline='\n').write(s)


# Las anclas (los mismos puntos de inserción en los tres idiomas)
ANCLA_ITEMS = '	BolsaAuras: {'
ANCLA_BUFFS = 'Buffs: {'
ANCLA_PROYECTILES = 'Projectiles: {'

# ── es-ES (la fuente única) ───────────────────────────────────────
insertar(os.path.join(RAIZ, 'es-ES_Mods.AethonMod.hjson'), ES,
         ANCLA_ITEMS, ANCLA_BUFFS, ANCLA_PROYECTILES)
print('OK es-ES')

# ── es-MX: el sync de la casa lo regenera (espejo del es-ES) ───────
r = subprocess.run(['python3', 'tools/sync_es_mx_v6564.py'], cwd=os.path.join(
    os.path.dirname(__file__), '..'), capture_output=True, text=True)
print('sync:', (r.stdout + r.stderr).strip()[:200])

# ── en-US: a mano (las traducciones viven — R7) ────────────────────
insertar(os.path.join(RAIZ, 'en-US_Mods.AethonMod.hjson'), EN,
         ANCLA_ITEMS, ANCLA_BUFFS, ANCLA_PROYECTILES)
print('OK en-US')

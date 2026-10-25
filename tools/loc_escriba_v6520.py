#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.52.0 — LA LOCALIZACIÓN DEL CICLO DEL ESCRIBA (Task 84).

Genera los TRES archivos hjson NUEVOS (es-MX, es-ES espejo, en-US) en
Localization/Escriba/ — ARCHIVOS NUEVOS, cero edición de los hjson de
raíz (la regla del ciclo: «no toques nada del mod» + el descubrimiento
de la Task 84: TryGetCultureAndPrefixFromPath acepta MÚLTIPLES archivos
con el MISMO prefijo Mods.AethonMod — todos se fusionan en el runtime).

La paridad es-MX == es-ES == en-US (mismas CLAVES exactas, la regla de
la casa desde v6.50.130) garantizada DE FÁBRICA: este script es la
única fuente de verdad.
"""
import os

RAIZ = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Localization/Escriba"

# (clave, es, en) — el es-ES es ESPEJO del es-MX (español neutro).
K = []  # lista de (ruta, es, en)

def k(ruta, es, en):
    K.append((ruta, es, en))

# ============ ITEMS ============
k("Items.BolsaDelEscriba.DisplayName", "Bolsa del Escriba", "Scribe's Bag")
k("Items.BolsaDelEscriba.Tooltip",
  "El ciclo completo del Escriba en una bolsa: el arma del rayo masivo, "
  "la pluma viva con su árbol de inscripciones y la pluma que lo convoca. "
  "Ábrela con clic derecho (solo entrega lo que falte).",
  "The Scribe's full cycle in one bag: the massive beam weapon, the living "
  "quill with its inscription tree, and the quill that summons him. "
  "Right-click to open (only delivers what you're missing).")
k("Items.BolsaDelEscriba.Abierta", "Las palabras del Escriba despiertan en tu inventario.",
  "The Scribe's words awaken in your inventory.")

k("Items.PrimerTrazo.DisplayName", "El Primer Trazo", "The First Stroke")
k("Items.PrimerTrazo.Tooltip",
  "La pluma que escribió la primera línea del mundo, cargada de luz. "
  "MANTÉN el clic: cinco trazos convergen en UN rayo masivo de ruido de Perlin. "
  "A plena carga el daño se triplica.",
  "The quill that wrote the world's first line, loaded with light. "
  "HOLD the click: five strokes converge into ONE massive Perlin-noise beam. "
  "At full charge, damage triples.")
k("Items.PrimerTrazo.Prueba", "SIN COSTE DE MANÁ — arma de prueba: canaliza sin miedo.",
  "NO MANA COST — a test weapon: channel away without fear.")

k("Items.PlumaConsciente.DisplayName", "La Pluma Consciente", "The Conscious Quill")
k("Items.PlumaConsciente.Tooltip",
  "La última pluma del Escriba — con su conciencia atada dentro. Habla, "
  "recuerda y desbloquea sus propias palabras. Ábrelo con [P]: el árbol "
  "de inscripciones (Filigrana / Verso / Colofón). Cada jefe vencido con "
  "ella sube su nivel.",
  "The Scribe's last quill — with his consciousness bound inside. It speaks, "
  "remembers, and unlocks its own words. Open it with [P]: the inscription "
  "tree (Filigree / Verse / Colophon). Every boss felled while carrying it "
  "raises its level.")
k("Items.PlumaConsciente.Nivel", "Nivel de la pluma: {0}", "Quill level: {0}")
k("Items.PlumaConsciente.Ranura0", "Filigrana: {0}", "Filigree: {0}")
k("Items.PlumaConsciente.Ranura1", "Verso: {0}", "Verse: {0}")
k("Items.PlumaConsciente.Ranura2", "Colofón: {0}", "Colophon: {0}")
k("Items.PlumaConsciente.Arbol", "[P] — Árbol de inscripciones (sosteniendo la pluma)",
  "[P] — Inscription tree (while holding the quill)")

k("Items.PlumaConvocadora.DisplayName", "Pluma Convocadora", "Summoning Quill")
k("Items.PlumaConvocadora.Tooltip",
  "La pluma que llama a quien la sostentó por última vez. Invoca a EL "
  "ESCRIBA — el jefe demo de la coreografía (340.000 de vida, post-Señor "
  "de la Luna).",
  "The quill that calls its last wielder. Summons THE SCRIBE — the "
  "choreography demo boss (340,000 HP, post-Moon Lord).")

# ============ PROJECTILES ============
k("Projectiles.PrimerTrazoHeld.DisplayName", "El Primer Trazo", "The First Stroke")
k("Projectiles.PrimerTrazoFajo.DisplayName", "Trazo de luz", "Stroke of light")
k("Projectiles.TintaBolt.DisplayName", "Palabra de tinta", "Ink word")
k("Projectiles.EscribaGota.DisplayName", "Gota de tinta", "Ink drop")
k("Projectiles.EscribaPagina.DisplayName", "Página del Escriba", "Scribe's page")
k("Projectiles.EscribaPluma.DisplayName", "Pluma del Escriba", "Scribe's quill")
k("Projectiles.EscribaPared.DisplayName", "Pared de tinta", "Ink wall")
k("Projectiles.EscribaVerso.DisplayName", "El Verso", "The Verse")

# ============ NPCs ============
k("NPCs.EscribaJefe.DisplayName", "El Escriba, la Mano que Devoró el Libro",
  "The Scribe, the Hand that Devoured the Book")
k("NPCs.EscribaJefe.Bestiary",
  "La mano que escribió las páginas que Aethon devoró. No murió: se quedó "
  "a terminar el libro — y tú estás escrito en la última página.",
  "The hand that wrote the pages Aethon devoured. It didn't die: it stayed "
  "to finish the book — and you are written on the last page.")

# ============ KEYBINDS ============
k("Keybinds.EscribaArbol.DisplayName", "Árbol de inscripciones (Pluma Consciente)",
  "Inscription tree (Conscious Quill)")

# ============ UI.ARBOL ============
k("UI.Arbol.Titulo", "LAS INSCRIPCIONES DE LA PLUMA", "THE QUILL'S INSCRIPTIONS")
k("UI.Arbol.Nivel", "Nivel del arma: {0}", "Weapon level: {0}")
k("UI.Arbol.Ranura0", "FILIGRANA", "FILIGREE")
k("UI.Arbol.Ranura1", "VERSO", "VERSE")
k("UI.Arbol.Ranura2", "COLOFÓN", "COLOPHON")
k("UI.Arbol.Pista", "Señala una inscripción para leerla.", "Hover an inscription to read it.")
k("UI.Arbol.PistaHecho", "La gana el hecho: {0}", "Earned by the deed: {0}")
k("UI.Arbol.PistaRegalo", "La regala el Escriba al vencer: {0}", "The Scribe gifts it upon defeating: {0}")
k("UI.Arbol.PistaNacimiento", "La pluma ya nace sabiéndola.", "The quill is born knowing it.")

# ============ ESCRIBA (mensajes / conciencia) ============
k("Escriba.Anuncio", "El Escriba alza la pluma: la historia no ha terminado.",
  "The Scribe raises his quill: the story is not over.")
k("Escriba.BolsaEntregada", "La Bolsa del Escriba espera en tu inventario (clic derecho).",
  "The Scribe's Bag awaits in your inventory (right-click).")
k("Escriba.Encuentro1", "El Escriba: «…Así que tú eres quien lleva mi pluma.»",
  "The Scribe: \"...So you're the one carrying my quill.\"")
k("Escriba.Encuentro2", "El Escriba: «Yo escribí las páginas que tu libro devoró. Todas.»",
  "The Scribe: \"I wrote the pages your book devoured. Every one of them.\"")
k("Escriba.Encuentro3", "El Escriba: «Graba mis palabras en la pluma y verás de qué soy capaz.»",
  "The Scribe: \"Carve my words into the quill and you'll see what I can do.\"")
k("Escriba.Encuentro4", "El Escriba: «[P]. Elige bien: todo poder tiene su carga.»",
  "The Scribe: \"[P]. Choose well: every power carries its burden.\"")
k("Escriba.Susurro1", "El Escriba murmura: «Las líneas torcidas también terminan.»",
  "The Scribe murmurs: \"Crooked lines end too.\"")
k("Escriba.Susurro2", "El Escriba murmura: «La tinta nunca olvida lo que escribió.»",
  "The Scribe murmurs: \"Ink never forgets what it wrote.\"")
k("Escriba.Susurro3", "El Escriba murmura: «Ese enemigo… ya lo había descrito yo.»",
  "The Scribe murmurs: \"That enemy... I had described it already.\"")
k("Escriba.Susurro4", "El Escriba murmura: «Sigue. La página se llena sola.»",
  "The Scribe murmurs: \"Keep going. The page fills itself.\"")
k("Escriba.HechoCumplido", "HECHO CUMPLIDO — el Escriba desbloquea: {0}",
  "DEED FULFILLED — the Scribe unlocks: {0}")
k("Escriba.InscripcionDesbloqueada", "Nueva inscripción: {0}", "New inscription: {0}")
k("Escriba.PruebaVencida", "La pluma sube al nivel {0} — el arma recuerda a quien venció.",
  "The quill reaches level {0} — the weapon remembers who was felled.")
k("Escriba.AtaqueTrazo", "El Trazo", "The Stroke")
k("Escriba.AtaqueLluvia", "Lluvia de Versos", "Rain of Verses")
k("Escriba.AtaquePlumas", "Las Plumas", "The Quills")
k("Escriba.AtaqueBorron", "El Borrón", "The Blot")
k("Escriba.AtaqueCorregido", "El Trazo, Corregido", "The Stroke, Corrected")
k("Escriba.AtaqueJaula", "La Jaula de Tinta", "The Ink Cage")
k("Escriba.AtaqueVerso", "El Verso", "The Verse")
k("Escriba.AtaqueAnotacion", "La Anotación", "The Annotation")
k("Escriba.Transicion1", "La Corrección", "The Correction")
k("Escriba.Transicion2", "El Interlineado", "The Leading")

# LOS REGALOS (la reacción del Escriba a cada jefe, con su inscripción)
k("Escriba.Regalo.Subrayado",
  "El Escriba: «El Ojo cayó. Lo importante va SUBRAYADO.» (Subrayado desbloqueada)",
  "The Scribe: \"The Eye fell. The important parts get UNDERLINED.\" (Underlined unlocked)")
k("Escriba.Regalo.LetraCapital",
  "El Escriba: «El gusano/cerebro cayó. Todo título empieza con LETRA CAPITAL.» (Letra Capital desbloqueada)",
  "The Scribe: \"The worm/brain fell. Every title starts with a CAPITAL LETTER.\" (Capital Letter unlocked)")
k("Escriba.Regalo.TintaCorrosiva",
  "El Escriba: «El hueso viejo cayó. Mi tinta CORROE hasta eso.» (Tinta Corrosiva desbloqueada)",
  "The Scribe: \"The old bone fell. My ink CORRODES even that.\" (Corrosive Ink unlocked)")
k("Escriba.Regalo.Firma",
  "El Escriba: «El Muro cayó. Lo escrito con sangre lleva FIRMA.» (Firma desbloqueada)",
  "The Scribe: \"The Wall fell. What's written in blood bears a SIGNATURE.\" (Signature unlocked)")
k("Escriba.Regalo.VersoCiego",
  "El Escriba: «Las máquinas cayeron. Este VERSO CIEGO encuentra solo a quién leer.» (Verso Ciego desbloqueado)",
  "The Scribe: \"The machines fell. This BLIND VERSE finds its reader alone.\" (Blind Verse unlocked)")
k("Escriba.Regalo.RimaInterna",
  "El Escriba: «La planta cayó. Escucha: la RIMA corre por dentro del trazo.» (Rima Interna desbloqueada)",
  "The Scribe: \"The plant fell. Listen: the RHYME runs inside the stroke.\" (Internal Rhyme unlocked)")
k("Escriba.Regalo.Caligrafia",
  "El Escriba: «El guardián de piedra cayó. La CALIGRAFÍA es paciencia.» (Caligrafía desbloqueada)",
  "The Scribe: \"The stone guardian fell. CALLIGRAPHY is patience.\" (Calligraphy unlocked)")
k("Escriba.Regalo.Hemistiquio",
  "El Escriba: \"El fanático cayó. El HEMISTIQUIO atraviesa lo que divide.» (Hemistiquio desbloqueado)",
  "The Scribe: \"The fanatic fell. The HEMISTICH pierces what it divides.\" (Hemistich unlocked)")
k("Escriba.Regalo.TintaMadre",
  "El Escriba: «¡El SEÑOR LUNAR cayó! Esta es la TINTA MADRE — la de donde salieron todas.» (Tinta Madre desbloqueada)",
  "The Scribe: \"The MOON LORD fell! This is MOTHER INK — the one all others came from.\" (Mother Ink unlocked)")
k("Escriba.Regalo.ElColofon",
  "El Escriba: «…Me venciste con mi propia pluma. Toma EL COLOFÓN — la última palabra.» (El Colofón desbloqueado)",
  "The Scribe: \"...You defeated me with my own quill. Take THE COLOPHON — the last word.\" (The Colophon unlocked)")

# ============ HECHOS ============
k("Hechos.PrimerasPalabras.Nombre", "Primeras Palabras", "First Words")
k("Hechos.PrimerasPalabras.Descripcion", "Mata 20 enemigos llevando la pluma.", "Kill 20 enemies while carrying the quill.")
k("Hechos.ManoFirme.Nombre", "Mano Firme", "Steady Hand")
k("Hechos.ManoFirme.Descripcion", "Golpea 25 veces al mismo jefe sin cambiar de presa.", "Hit the same boss 25 times without switching prey.")
k("Hechos.AlMargen.Nombre", "Al Margen", "In the Margin")
k("Hechos.AlMargen.Descripcion", "Vence 5 jefes DISTINTOS llevando la pluma.", "Fell 5 DIFFERENT bosses while carrying the quill.")
k("Hechos.TintaSeca.Nombre", "Tinta Seca", "Dry Ink")
k("Hechos.TintaSeca.Descripcion", "Sostén la pluma 10 minutos en total.", "Hold the quill for 10 minutes total.")
k("Hechos.RachaViva.Nombre", "Racha Viva", "Living Streak")
k("Hechos.RachaViva.Descripcion", "Encadena 8 muertes separadas por menos de 3 segundos.", "Chain 8 kills less than 3 seconds apart.")

# ============ INSCRIPCIONES (18 × 4 claves) ============
def ins(clave, nombre, desc, poder, carga, en_nombre, en_desc, en_poder, en_carga):
    k(f"Inscripciones.{clave}.Nombre", nombre, en_nombre)
    k(f"Inscripciones.{clave}.Descripcion", desc, en_desc)
    k(f"Inscripciones.{clave}.Poder", "Poder: " + poder, "Power: " + en_poder)
    k(f"Inscripciones.{clave}.Carga", "Carga: " + carga, "Burden: " + en_carga)

ins("TintaLigera", "Tinta Ligera", "La tinta que fluye ligera.",
    "+10% de cadencia", "-3% de daño",
    "Light Ink", "The ink that flows lightly.",
    "+10% use speed", "-3% damage")
ins("PlumaAjena", "Pluma Ajena", "La pluma que aprendió a no gastarse.",
    "-40% de coste de maná", "-4% de daño",
    "Borrowed Quill", "The quill that learned not to waste itself.",
    "-40% mana cost", "-4% damage")
ins("MargenEstrecho", "Margen Estrecho", "El margen donde nada sobra.",
    "+4 de crítico", "-2% de daño",
    "Narrow Margin", "The margin where nothing is wasted.",
    "+4 crit", "-2% damage")
ins("Interlineado", "Interlineado", "Más líneas por página.",
    "+2 proyectiles", "-10% de daño",
    "Leading", "More lines per page.",
    "+2 projectiles", "-10% damage")
ins("LetraCapital", "Letra Capital", "La letra con la que nacen los títulos.",
    "+15% de cadencia", "+10% de maná",
    "Capital Letter", "The letter titles are born with.",
    "+15% use speed", "+10% mana")
ins("Caligrafia", "Caligrafía", "La escritura perfecta, medida y paciente.",
    "+20% de cadencia", "-3% de daño",
    "Calligraphy", "Perfect, measured, patient writing.",
    "+20% use speed", "-3% damage")
ins("VersoSencillo", "Verso Sencillo", "El verso que aprende todo escriba.",
    "+12% de daño", "+8% de maná",
    "Simple Verse", "The verse every scribe learns first.",
    "+12% damage", "+8% mana")
ins("VersoDoble", "Verso Doble", "El eco del verso: se dice dos veces.",
    "+1 proyectil", "-5% de daño",
    "Double Verse", "The verse's echo: said twice.",
    "+1 projectile", "-5% damage")
ins("TintaCorrosiva", "Tinta Corrosiva", "La tinta que corroe el hueso.",
    "+20% de daño", "+20% de maná",
    "Corrosive Ink", "The ink that corrodes bone.",
    "+20% damage", "+20% mana")
ins("VersoCiego", "Verso Ciego", "El verso que encuentra solo a quién leer.",
    "Los trazos persiguen", "-7% de daño",
    "Blind Verse", "The verse that finds its reader alone.",
    "Bolts home in", "-7% damage")
ins("RimaInterna", "Rima Interna", "La rima que corre por dentro del trazo.",
    "Estela de tinta viva", "+5% de daño",
    "Internal Rhyme", "The rhyme running inside the stroke.",
    "Living ink trail", "+5% damage")
ins("Hemistiquio", "Hemistiquio", "El verso partido que atraviesa.",
    "+2 de perforación", "+15% de maná",
    "Hemistich", "The split verse that pierces.",
    "+2 pierce", "+15% mana")
ins("ColofonHumilde", "Colofón Humilde", "El colofón en blanco: lo que todo libro lleva.",
    "+3 de crítico", "Sin carga",
    "Humble Colophon", "The blank colophon every book carries.",
    "+3 crit", "No burden")
ins("PuntoFinal", "Punto Final", "Lo que está casi muerto, MUERE.",
    "Ejecución (×2.5 bajo el 10% de vida)", "-5% de daño",
    "Full Stop", "What's almost dead, DIES.",
    "Execute (×2.5 under 10% HP)", "-5% damage")
ins("Subrayado", "Subrayado", "Lo importante, subrayado.",
    "+8 de crítico", "+12% de maná",
    "Underlined", "The important parts, underlined.",
    "+8 crit", "+12% mana")
ins("Firma", "Firma", "Lo escrito con sangre devuelve sangre.",
    "Robo de vida (4% del daño)", "-8% de daño",
    "Signature", "What's written in blood returns blood.",
    "Lifesteal (4% of damage)", "-8% damage")
ins("TintaMadre", "Tinta Madre", "La tinta de donde salieron TODAS.",
    "Buscapresas y +3 de perforación", "+30% de maná",
    "Mother Ink", "The ink all others came from.",
    "Homing and +3 pierce", "+30% mana")
ins("ElColofon", "El Colofón", "La última palabra del libro.",
    "+35% de daño y +2 proyectiles", "+35% de maná y -10% de cadencia",
    "The Colophon", "The book's last word.",
    "+35% damage and +2 projectiles", "+35% mana and -10% use speed")


# ==================================================================
#  EL EMISOR — tres archivos con la MISMA estructura de claves
# ==================================================================
def construir():
    """ruta jerárquica → dict anidado"""
    raiz = {}
    for ruta, es, en in K:
        partes = ruta.split(".")
        nodo = raiz
        for p in partes[:-1]:
            nodo = nodo.setdefault(p, {})
        hoja = nodo.setdefault("__hojas__", {})
        hoja[partes[-1]] = (es, en)
    return raiz


def emitir(nodo, nivel=1):
    """dict → hjson (con tabs, el estilo de la casa)."""
    lineas = []
    tab = "\t" * nivel
    # primero las hojas, luego las ramas (orden de inserción — estable)
    hojas = nodo.get("__hojas__", {})
    for clave, (es, en) in hojas.items():
        lineas.append(f"{tab}{clave}: {valor(es)}")
    for clave, sub in nodo.items():
        if clave == "__hojas__":
            continue
        lineas.append(f"{tab}{clave}: {{")
        lineas.extend(emitir(sub, nivel + 1))
        lineas.append(f"{tab}}}")
    return lineas


def valor(texto):
    """escapa un valor hjson de una línea."""
    return '"' + texto.replace("\\", "\\\\").replace('"', '\\"') + '"'


def generar(idioma, indice, cabecera):
    raiz = construir()
    lineas = [cabecera, ""] + emitir(raiz, 0)
    contenido = "\n".join(lineas).rstrip() + "\n"
    ruta = os.path.join(RAIZ, f"{idioma}_Mods.AethonMod.hjson")
    with open(ruta, "w", encoding="utf-8", newline="\n") as f:
        f.write(contenido)
    return ruta, contenido.count("\n")


if __name__ == "__main__":
    os.makedirs(RAIZ, exist_ok=True)
    base = (
        "// v6.52.0 — LA BOLSA DEL ESCRIBA: localización del ciclo nuevo\n"
        "// (Task 84). ARCHIVO NUEVO — los hjson de raíz NO se tocan (la\n"
        "// regla del ciclo). El prefijo del nombre (Mods.AethonMod) es el\n"
        "// MISMO que el de raíz: tML FUSIONA todos los archivos del idioma\n"
        "// (LocalizationLoader.TryGetCultureAndPrefixFromPath — verificado\n"
        "// contra el tModLoader.dll decompilado, Task 84).\n"
    )
    r1, n1 = generar("es-MX", 0, base + "// ESPAÑOL LATINOAMÉRICA (es-MX) — la fuente del ciclo.")
    r2, n2 = generar("es-ES", 0, base + "// ESPAÑOL ESPAÑA (es-ES) — ESPEJO EXACTO del es-MX (español neutro).")
    r3, n3 = generar("en-US", 0, base + "// ENGLISH (en-US).")
    print(f"{r1}: {n1} líneas, {len(K)} claves")
    print(f"{r2}: {n2} líneas")
    print(f"{r3}: {n3} líneas")
    # LA AUDITORÍA DE PARIDAD DE FÁBRICA: mismas claves en los tres.
    claves = sorted(ruta for ruta, _, _ in K)
    assert len(claves) == len(set(claves)), "¡CLAVES DUPLICADAS!"
    print(f"PARIDAD OK: {len(claves)} claves únicas × 3 idiomas")

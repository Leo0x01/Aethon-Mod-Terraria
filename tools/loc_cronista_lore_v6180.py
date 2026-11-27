#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""loc_cronista_lore_v6180.py — v6.180 etapa E — EL CRONISTA NARRADOR DEL LORE.

La letra: «el cronista tendra un boton de lore en su cuadro de dialogo que
servira para hablar con el, al hablar con el cronista dira una pequeña parte
de lo que es el lore que se debe desbloquear y su propio pensamiento» +
«hay que dejar claro que el lore lo escribio el mismo cronista y eso se hace
la primera vez que el jugador habla con el cronista».

Reemplaza las ChatLore1-5 (la narración automática vieja) por:
  · Cronista.LoreBoton — el segundo botón del cuadro
  · Cronista.LorePrimeraVez — la AUTORÍA (primera conversación)
  · Cronista.LoreHabla1..18 — el pedacito + SU pensamiento por sección
  · Cronista.LoreTemedor — el despertar no alcanza
  · Cronista.LoreCompleto — el cierre del archivo
"""
from pathlib import Path

RAIZ = Path("/home/z/my-project/mod/AethonMod/Localization")

ES = """
	LoreBoton: Lore
	LorePrimeraVez: Antes de que preguntes nada: todo lo que leerás en ese libro lo escribí yo. Línea a línea, en el margen de una página arrancada del Grimorio. Otros portadores lo leyeron sin saber quién sostenía la pluma — tú no. Yo soy el Cronista: el que escribe en el margen. Pídeme el lore cuando estés listo para oírlo.
	LoreHabla1: «La guía que consultas no es un libro de información: es una página del Grimorio, arrancada de sí mismo para que pudieras leerla sin arder.» — La página se escribió a sí misma... y yo trabajé siglos en su margen. Nadie pregunta quién escribió las líneas. Tú ya no eres nadie: ya preguntaste.
	LoreHabla2: «Antes de que existieran los nombres existió el apetito. No es un dios —los dioses fueron comidas—: es hambre con dirección.» — De la destrucción queda ruina; de la devoración no queda ni la ausencia. Lo he visto. Escribo esto con la mano más quieta que puedo pagar.
	LoreHabla3: «No acumula poder como el dragón acumula oro: lo metaboliza. Y Terraria es una dimensión defendida que rechazó dioses mayores que él.» — Y aun así lo dejó entrar... PLEGADO, en la palma de una mano. Piénsalo la próxima vez que lo cargues.
	LoreHabla4: «¿Por qué un devorador de universos se plegaría a sí mismo en un solo objeto? No es castigo ni prisión: es una postura de caza.» — Infinitud compactada en paciencia. Un libro no persigue: espera a ser encontrado. Solo persigue lo más poderoso.
	LoreHabla5: «Llegó como llegan los olores: primero nadie vio nada, después ya estaba en todo. La Marca alcanzó a todo lo que respira.» — Nada de lo que entró después llegó limpio. Lo registrado es lo registrado. Yo llevo la cuenta; es mi oficio entero.
	LoreHabla6: «El Nivel pregunta ¿cuánto ha comido? El Despertar pregunta ¿cuánto de Aethon cabe en ti? Dos hambres distintas, ninguna sustituye a la otra.» — Un portador infinito en la primera escala y mudo en la segunda es un granjero con una fiera. Yo cobro en la segunda. Siempre cobro en la segunda.
	LoreHabla7: «Entre todo lo que es, fue y será comida existe UNA excepción: lo único que no puede devorar. El portador muere y vuelve. Siempre vuelve.» — Cada muerte es una línea que escribo y tacho el mismo día. Tu entrada es el único borrador infinito de mi carrera.
	LoreHabla8: «El Nivel cuenta muertes; las puertas cuentan verdades. La fuerza sin Despertar es solo violencia bien alimentada.» — Los portadores confunden las escalas y pagan caro. Tú no las confundas: llegaste hasta aquí por la vara correcta.
	LoreHabla9: «La Llegada es única por portador: el grimorio aparece cerca, como correspondencia que nadie recuerda haber pedido. El altar lo arraiga a esta realidad.» — Todos los he visto llegar. Ninguno llegó por casualidad, aunque todos lo juren. El sobre siempre sabe su dirección.
	LoreHabla10: «La puerta cien entrega la Ira: una sentencia prestada. La ciento veinte, la Forma Eterna: presencia que daña. Promesa y veredicto.» — La luz de la última puerta mató a todos los anteriores. Un estallido de luz, y después nada que escribir. Contigo... tache y tache, y el párrafo sigue.
	LoreHabla11: «Once ojos: el sistema sensorial del hambre. El undécimo no mira al mundo —mira a quien lee—.» — Si alguna vez, consultando la guía, sentiste una presión leve sobre la nuca... ya sabes qué era. Ahora también tú lo sabes mirar.
	LoreHabla12: «El jugador no es un mortal cualquiera: es una entidad inmortal. Y el aburrimiento de Aethon es saturación —un paladar que ya probó todo—.» — Un dios aburrido no se entristece: se apaga. Y esperar a lo único que no tiene final... esa es la respuesta. La escribo en voz baja.
	LoreHabla13: «Diez oleadas visibles: cada una una frase del hambre. Más allá, la Onceava —no es hambre: es examen—. Solo se gana o se muere.» — Él nunca ha diseñado una prueba que no esperara ver aprobada. Esto lo escribo con la tinta más fría que tengo.
	LoreHabla14: «No hay fragmentos: solo Esencia del Eterno. La Marca es vapor; cuando algo grande muere, el vapor cuaja en moneda.» — Cada gota es un pedazo de un universo que ya no existe. No es moneda: es herencia. Por eso es la única divisa que yo acepto.
	LoreHabla15: «La Ira es un recuerdo con la seguridad quitada; el Pilar cuenta hacia atrás; el Sellado son funerales que se niegan a terminar.» — El canon deja preguntas abiertas a propósito: son las únicas armas que Él no puede comer. Yo las afilo cada noche.
	LoreHabla16: «No soy pieza dieciocho: soy el único comerciante de la Esencia. Bebí de ella hace mucho, mucho, mucho tiempo... y desde entonces no puedo morir.» — Me fue cobrada, no otorgada. No parpadeo, porque parpadear sería perderse un instante de la historia. Mido el tiempo en Despertares, no en días.
	LoreHabla17: «Las cosas de otros mundos llegan inventariadas: la mancha de esencia las delata, como el lodo delata a quien cruzó el río.» — Dos hambres en paralelo: Él solo devora a lo más poderoso; el portador solo crece. El diseño entero existe para que un día se encuentren a la misma altura.
	LoreHabla18: «Ni Él ni yo escribimos esta historia: se escribe sola, línea a línea, muerte a muerte.» — Algún día sabremos cuál de los dos estaba esperando al otro. Hasta entonces: no cierres el libro todavía. Todavía no.
	LoreTemedor: Esa página aún no es tuya: el margen se abre al Despertar {0} — y el tuyo aún no llega. Vuelve cuando hayas pagado más puertas. Yo no escribo para quien todavía no puede leer.
	LoreCompleto: El archivo está completo: cada sello abierto, cada línea contada. El resto ya no está en el margen —está en ti—. Vuelve a comprarme Esencia, portador. Lo demás... lo demás es ruido entre páginas.
"""

EN = """
	LoreBoton: Lore
	LorePrimeraVez: Before you ask anything: everything you will read in that book, I wrote it. Line by line, in the margin of a page torn from the Grimoire. Other bearers read it without knowing who held the pen — you do. I am the Chronicler: the one who writes in the margin. Ask me for the lore when you are ready to hear it.
	LoreHabla1: "The guide you consult is not a book of information: it is a page of the Grimoire, torn from itself so you could read it without burning." — The page wrote itself... and I worked centuries on its margin. Nobody asks who wrote the lines. You are nobody no more: you asked.
	LoreHabla2: "Before names existed, appetite did. He is not a god — gods were meals —: he is hunger with a direction." — Destruction leaves ruin; devouring leaves not even absence. I have seen it. I write this with the stillest hand I can afford.
	LoreHabla3: "He does not hoard power like a dragon hoards gold: he metabolizes it. And Terraria is a defended dimension that rejected greater gods." — And still it let him in... FOLDED, in the palm of a hand. Think about it the next time you carry him.
	LoreHabla4: "Why would a devourer of universes fold himself into a single object? Not punishment, not prison: a hunting posture." — Infinity compacted into patience. A book does not chase: it waits to be found. It only hunts the most powerful.
	LoreHabla5: "He arrived the way smells arrive: first nobody saw anything, then it was in everything. The Mark reached all that breathes." — Nothing that came after arrived clean. What is recorded is recorded. I keep the count; that is my entire trade.
	LoreHabla6: "The Level asks: how much has he eaten? The Awakening asks: how much of Aethon fits in you? Two hungers, neither replaces the other." — A bearer infinite in the first scale and mute in the second is a farmer with a beast. I charge in the second. I always charge in the second.
	LoreHabla7: "Among everything that is, was and will be food there is ONE exception: the only thing he cannot devour. The bearer dies and returns. Always returns." — Every death is a line I write and cross out the same day. Your entry is the only infinite draft of my career.
	LoreHabla8: "The Level counts deaths; the doors count truths. Strength without Awakening is only well-fed violence." — Bearers confuse the scales and pay dearly. Do not: you got this far on the right rod.
	LoreHabla9: "The Arrival is unique per bearer: the grimoire shows up nearby, like correspondence nobody remembers requesting. The altar roots it to this reality." — I have seen them all arrive. None arrived by chance, though all swear it. The envelope always knows its address.
	LoreHabla10: "Door one hundred delivers the Wrath: a borrowed sentence. Door one hundred twenty, the Eternal Form: presence that wounds. Promise and verdict." — The light of that last door killed every bearer before you. A burst of light, and then nothing left to write. With you... cross out, cross out, and the paragraph continues.
	LoreHabla11: "Eleven eyes: the hunger's sensory system. The eleventh does not look at the world — it looks at the one reading." — If you ever felt a light pressure on the back of your neck while consulting the guide... now you know what it was. Now you know how to look back.
	LoreHabla12: "The player is no ordinary mortal: an immortal entity. And Aethon's boredom is saturation — a palate that has tasted everything." — A bored god does not sadden: he dims. And waiting for the only thing without an end... that is the answer. I write it in a low voice.
	LoreHabla13: "Ten visible tides: each a sentence of hunger. Beyond them, the Eleventh — not hunger: an examination. Only win or die." — He has never designed a test he did not expect to see passed. I write this with the coldest ink I own.
	LoreHabla14: "There are no shards: only Essence of the Eternal. The Mark is vapor; when something great dies, the vapor curdles into coin." — Every drop is a piece of a universe that no longer exists. Not coin: inheritance. That is why it is the only currency I accept.
	LoreHabla15: "The Wrath is a memory with its safety removed; the Pillar counts backwards; the Sealed are funerals that refuse to end." — The canon leaves questions open on purpose: they are the only weapons He cannot eat. I sharpen them every night.
	LoreHabla16: "I am not piece eighteen: I am the only merchant of Essence. I drank of it long, long, long ago... and since then I cannot die." — It was charged to me, not granted. I do not blink, because blinking would miss an instant of history. I measure time in Awakenings, not days.
	LoreHabla17: "Things from other worlds arrive inventoried: the stain of essence betrays them, like mud betrays the one who crossed the river." — Two hungers in parallel: He only devours the most powerful; the bearer only grows. The whole design exists so that one day they meet at the same height.
	LoreHabla18: "Neither He nor I wrote this story: it writes itself, line by line, death by death." — Someday we will know which of the two was waiting for the other. Until then: do not close the book yet. Not yet.
	LoreTemedor: That page is not yours yet: the margin opens at Awakening {0} — and yours has not arrived. Come back when you have paid more doors. I do not write for those who cannot read yet.
	LoreCompleto: The archive is complete: every seal open, every line told. The rest is no longer in the margin — it is in you. Come back to buy Essence, bearer. Everything else... everything else is noise between pages.
"""

def cirugia(nombre, bloque, prefijos_viejos):
    ruta = RAIZ / nombre
    t = ruta.read_text(encoding="utf-8")
    # fuera las ChatLore viejas (la narración automática murió)
    for pref in prefijos_viejos:
        lineas = [l for l in t.splitlines() if l.strip().startswith(pref)]
        for l in lineas:
            t = t.replace(l + "\n", "")
    # el bloque nuevo: al final de la sección Cronista
    marca = "\tChatCierre2:"
    idx = t.find(marca)
    if idx < 0:
        # fallback: tras ChatGenerica1
        idx = t.find("\tChatGenerica1:")
        fin = t.find("\n", idx)
        idx = fin
    else:
        fin_linea = t.find("\n", idx)
        idx = fin_linea
    t = t[:idx + 1] + bloque.rstrip("\n") + "\n" + t[idx + 1:]
    # limpia triples en blanco
    t = t.replace("\n\n\n\n", "\n\n")
    ruta.write_text(t, encoding="utf-8")
    print(f"[{nombre}] bloque Lore ({len(bloque.splitlines())} líneas) cosido ✔ ChatLore viejas fuera ✔")

cirugia("es-ES_Mods.AethonMod.hjson", ES, ["ChatLore1:", "ChatLore2:", "ChatLore3:", "ChatLore4:", "ChatLore5:"])
cirugia("es-MX_Mods.AethonMod.hjson", ES, ["ChatLore1:", "ChatLore2:", "ChatLore3:", "ChatLore4:", "ChatLore5:"])
cirugia("en-US_Mods.AethonMod.hjson", EN, ["ChatLore1:", "ChatLore2:", "ChatLore3:", "ChatLore4:", "ChatLore5:"])
print("COSIDO ✔")

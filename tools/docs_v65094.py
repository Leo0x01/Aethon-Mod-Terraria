#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""docs_v65094.py — snapshot (título, resumen, próximos pasos, HISTORIAL
con la fila .93 restaurada + la .94) y README a la 6.50.94.

BUG HISTÓRICO CAZADO (patrón Task 82/.90): la fila v6.50.93 NUNCA existió
en la tabla HISTORIAL — la sesión .93 selló su item de PRÓXIMOS PASOS
pero no escribió la fila. Restaurada fiel de los mensajes de commit.
"""
import io

SNAP = 'STABLE-SNAPSHOT.md'
README = 'README.md'

# ---------------------------------------------------------------- 1. título
src = io.open(SNAP, encoding='utf-8').read()
viejo = '# AethonMod — ESTADO ACTUAL (v6.50.93)'
assert src.count(viejo) == 1
src = src.replace(viejo, '# AethonMod — ESTADO ACTUAL (v6.50.94)')

# ------------------------------------------------- 2. Última actualización
viejo_res = ('> Última actualización: v6.50.93 (LA OLEADA ES OTRA ENTIDAD: el Devorador '
             'de oleada ya NO se va')
i0 = src.index('> Última actualización: v6.50.93')
i1 = src.index('\n', src.index('EL ESTALLIDO ES EL ADIÓS'))
nuevo_res = """> Última actualización: v6.50.94 (LA LETRA DEL NERVIOSO que la .93 prometía
> y no cumplió en juego: EL RELOJ SIEMPRE — la gracia de los 10 s de la
> .92 era LA PAUSA que el usuario veía («el hambre se pausa»): muerta,
> Paso() suma SIEMPRE, el bocado sólo resta −1%; EL COMPÁS DE LA SOMBRA —
> un ataque cada 10 s (marcado al NACER) y NADA antes de 2 s de TERMINAR
> la anterior (marcado en OnKill): la digestión .93 marcaba a mitad del
> ataque y la sombra moría 84 t después — 0,6 s de pausa real era ESA la
> brecha del re-lanzamiento al matar; LA FUGA MUERTA — MODO_FUGA/IrAFuga/
> frases/purgados: al 100% el libro se queda flotando cerca cazando por
> sí mismo; LA CAZA DEL AUSENTE — sale del 25% con AFK 5 s O 30 s sin
> atacar con el libro, y mientras el dueño dispara (5 s) el espíritu
> calla sus sombras; EL FESTÍN EGOÍSTA — la XP de cada presa es DEL
> NERVIOSO (ShardLevelItem lo adopta, tooltip con SU nivel, sin Eterno ni
> latido: el jugador NADA); EL ESPÍRITU ÚNICO — SoyDuplicado() disuelve
> al segundo espíritu del dueño: jamás DOS libros flotando (la «copia» de
> la captura del usuario con el destello naciendo de la equivocada).
> .tmod 2.555.191 B md5 52004cee59ef7c79ef4da32c19ea593f, 384 entradas,
> auditoría TODO OK, headless 0 excepciones.)"""
src = src[:i0] + nuevo_res + src[i1:]

# ------------------------------------------------- 3. PRÓXIMOS PASOS: item .94
ancla_pp = '## 🚧 PRÓXIMOS PASOS SUGERIDOS (en orden)\n\n'
assert src.count(ancla_pp) == 1
item94 = ancla_pp + """1. **⏳ v6.50.94 LISTA — pendiente release** — EL RELOJ SIEMPRE + EL
   COMPÁS DE LA SOMBRA + LA FUGA MUERTA + EL FESTÍN EGOÍSTA + EL ESPÍRITU
   ÚNICO. La letra: «el hambre se pausa / su ataque sigue relanzandose al
   matar […] una vez cada 10 segundo o algo asi, o poniendo por codigo
   que no puede lanzar un ataque 2 segundos despues de terminar el
   primero / los proyectiles del libro no salen del libro cuando esta
   flotando, salen de una copia del sprite / esto de la fuga no tiene
   mucho sentido […] la fuga simplemente la quitamos el libro no se
   fuga, solo queda flotando cerca del jugador cazando por si mismo,
   esto de la caza por su cuenta es cuando el jugador esta en afk o
   simplemente lleva mucho tiempo sin atacar criaturas con el libro / el
   festin compartido no tiene sentido […] el libro debe ser egoista y
   comer toda la criatura loot incluido por eso el libro recibe la exp y
   el jugador nada». (1) LA GRACIA DE LOS 10 s MUERTA: Paso() suma
   SIEMPRE (1/3600 por tick plegado ldc.r4 0.00027777778) — UltimaComida
   y TICKS_GRACIA_COMIDA ELIMINADOS; el bocado resta −1% sin congelar
   nada. (2) EL COMPÁS: MarcarAtaque (600 t = 10 s) al nacer la sombra +
   SombraTerminada (120 t = 2 s) en OnKill — la digestión .93 (marcada
   en la ABSORCIÓN, a mitad del ataque) murió con su brecha de 0,6 s.
   (3) LA FUGA: MODO_FUGA/IrAFuga/bloque de huida/spawn/frases
   «Escapa»+«Fugitivo» ELIMINADOS — al 100% sigue cazando cerca del
   jugador. (4) LA CAZA DEL AUSENTE: TICKS_SIN_ATAQUE_LIBRO 1800 (30 s
   sin disparar con el libro TAMBIÉN lo saca, aunque el jugador se
   mueva) + TICKS_MARGEN_ATAQUE 300 (mientras el dueño dispara hace <5 s,
   JugadorAtacoReciente() calla las sombras). (5) EL FESTÍN EGOÍSTA:
   CobrarXPLibro cobra al NERVIOSO (ShardLevelItem lo adopta — nivel+XP
   por copia, tooltip con «Nivel N · XP x/y» vía Hambre.Nervioso.Nivel),
   sin MarcarGanancia ni SincronizarLibros: el jugador NADA. (6) EL
   ESPÍRITU ÚNICO: SoyDuplicado() cada 30 t — gana el MODO_CAZA, a igual
   modo el whoAmI más bajo: jamás dos libros (la copia de la captura
   murió por construcción). VERIFICACIÓN: oráculo 0/0 (306 .cs) · build
   real 0/0 · .tmod 2.555.191 B md5 52004cee59ef7c79ef4da32c19ea593f:
   384 entradas (el MISMO set de la .93), EOF exacto, blob diff
   whitelist exacto (dll/pdb/Info + 3 hjson), arte byte-idéntico (378),
   CECIL: UltimaComida/TICKS_GRACIA_COMIDA/PasoSinHambre/
   MarcarDigestion/IrAFuga/MODO_FUGA/«Fugitivo»/«Escapa» ELIMINADOS +
   MarcarAtaque/AtaqueListo/SombraTerminada/OnKill (600 y 120 plegados)
   + SoyDuplicado + Shoot/JugadorAtacoReciente (1800/300 plegados) +
   ShardLevelItem adopta al Nervioso + GrimorioHambriento (clase-ítem)/
   OleadaNPC/GrimorioFuriaSistema IL IDÉNTICOS a la .93 · hjson: es-ES
   espejo, 13 claves Nervioso (con «Nivel», sin fuga), 785 hojas es↔en,
   en-US tabs 4235, paquete == árbol ×3 · headless «Sandboxing
   v6.50.94 → Adding Recipes → Server started» 0 EXCEPCIONES.
   CHECKLIST DEL USUARIO (la prueba EN JUEGO): (a) con el libro QUIETO
   en el inventario el hambre sube SIEMPRE (un minuto 0→100%), coma o no
   coma — sin pausas de 10 s; (b) AFK 5 s (o 30 s sin disparar con el
   libro) con hambre ≥25% → «Si no me alimentas, salgo yo mismo a
   cazar»; (c) la sombra mata a una criatura y NO se relanza al instante
   — el siguiente ataque tarda 10 s (y jamás <2 s tras terminar el
   anterior); (d) al 100% el libro NO se escapa: sigue flotando y
   cazando; (e) UN solo libro flotando — si aparecía una copia junto a
   la mano, ya no: los proyectiles salen del libro REAL; (f) con el
   espíritu fuera, el jugador ataca con la mano VACÍA y la descarga nace
   del libro; (g) la presa comida NO deja loot y el NERVIOSO sube de
   nivel (tooltip «Nivel N · XP x/y») — el Grimorio del Eterno y el
   jugador NO reciben nada; (h) mientras el jugador dispara con el libro
   (<5 s), el espíritu NO lanza sombras por su cuenta.

"""
src = src.replace(ancla_pp, item94)

# --------------------------------- 4. HISTORIAL: fila .93 restaurada + fila .94
ancla_h = '## 📜 HISTORIAL DE ESTADO (contexto de versiones)\n| Versión | Estado | Notas |\n|---|---|---|\n'
assert src.count(ancla_h) == 1
fila93 = ("| **v6.50.93** | ✅ Build-verificada (oráculo 0/0 sobre 306 .cs, build real 0/0, "
          ".tmod 2.555.354 B md5 363932e16d8ce060736497bbfabcc2d1, 384 entradas (el MISMO set de la .92), "
          "EOF exacto, blob diff whitelist exacto (dll/pdb/Info + 3 hjson), arte byte-idéntico (378), "
          "CECIL: PasoSinHambre ELIMINADO de EstadoGrimorio + OrigenDelDisparo ×2 (base y Nervioso) + "
          "PosicionDelLibro + UMBRAL_CACERIA 0.25 plegada + MarcarDigestion/DigestionLista (ldc.i4.s 120) + "
          "IrAFuga en el espíritu + SelloVivo/_sellados/TTL_SELLO 900 + el chequeo 7|13 del préstamo de "
          "corrupción DENTRO del IL de PreAI + GrimorioFuriaSistema IL IDÉNTICO a la .92, hjson: es-ES "
          "espejo por contenido, 786 hojas es↔en, en-US tabs 4293, paquete==árbol ×3, headless "
          "«Sandboxing v6.50.93 → Adding Recipes → Server started» 0 excepciones), ✔ PUBLICADA (release "
          "408895980 + asset AethonMod.tmod 627773875 2.555.354 B, CDN verificado byte a byte md5 "
          "363932e1…, /releases/latest = v6.50.93), ⏳ en juego (la .94 corrige lo que la prueba reveló: "
          "el hambre aún se pausaba por la gracia, el re-lanzamiento al matar seguía, la copia del sprite "
          "junto a la mano) | LA LETRA: «en la oleada el devorador de mundo aparecio y se fue […] las "
          "versiones de oleada son entidades separadas de las originales […] el libro solo sale a cazar "
          "de 25% de hambre en adelante […] si el jugador ataque y el libro esta fuera sus ataques salen "
          "del libro no del jugador […] el nivel de hambre debe subir independientemente el libro case o "
          "no […] su ataque vuelve a lanzarce justo cuando mata a la criatura, esto no debe pasar». "
          "(1) EL DEVORADOR QUE SE FUE: la causa raíz en el decompile — al PARTIR al gusano vanilla MUTA "
          "el cuerpo en cabeza (SetDefaultsKeepPlayerInteraction(13)) y NPC.SetDefaults borra los globals "
          "(_globals=null): el sello moría con la instancia y la cabeza nueva era enterrada por su IA "
          "hasta active=false; CURA DOBLE: registro SelloVivo por whoAmI (TTL 15 s, re-adoptado en el "
          "propio SetDefaults) + préstamo de zona a TODA la mesa viva (incluidos los DevourerHead "
          "escupidos, 7|13 en PreAI). (2) UMBRAL_CACERIA 0.25 + IrAFuga al 100% estando fuera. (3) "
          "OrigenDelDisparo virtual + LA MANO VACÍA (ModifyItemDraw return false sin agregar nada). (4) "
          "PasoSinHambre eliminado (el reloj corría tras la gracia de 10 s). (5) LA DIGESTIÓN 120 t "
          "marcada en la absorción. LECCIONES: (1) un GlobalNPC NO sobrevive al cambio de type de su "
          "propio NPC — los sellos que sobreviven a una mutación viven en un REGISTRO externo por "
          "whoAmI; (2) los préstamos a vanilla no dependen del npc.target: TODA la mesa viva; (3) los "
          "const NPCID llegan PLEGADOS (ldc.i4.7): auditar por el literal DENTRO del bloque IL; (4) el "
          "cuerpo JSON de un release con newlines+unicode va por ARCHIVO (-d @file) |\n")
src = src.replace(ancla_h, ancla_h + fila93)
io.open(SNAP, 'w', encoding='utf-8', newline='\n').write(src)
print('STABLE-SNAPSHOT.md: título + resumen + item .94 + fila .93 restaurada')

# ---------------------------------------------------------------- 5. README
r = io.open(README, encoding='utf-8').read()
viejo_r = '> **Versión actual:** 6.50.93'
assert r.count(viejo_r) == 1
r = r.replace(viejo_r, '> **Versión actual:** 6.50.94')
io.open(README, 'w', encoding='utf-8', newline='\n').write(r)
print('README.md: versión 6.50.94')

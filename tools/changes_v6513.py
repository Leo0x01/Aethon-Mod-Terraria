# -*- coding: utf-8 -*-
# v6.51.3 — la entrada del CHANGES.md (la regla de la casa: Python sí, manos no)
import io

ENTRADA = u"""# v6.51.3 — EL DÍA Y LA NOCHE DE VERDAD + LA LENTE BAJO LOS FILTROS + LA MANADA DEL VAGABUNDO

**La letra del usuario**: «algo curioso, si hay mas de 2 vagabundos en la seleccion de personaje, deberian interactuar entre si, tambien mejora la animacion del vagabundo // los dialogos del grimorio dormido deben ser unicos y deben estar separados de los dialogos de los NPC, ya que vi un dialogo de un NPC en el grimorio dormido, ademas al aparecer todavia aparecen ovalados junto a la bruma // en el bestiario ya no esta el numero 42, pero el grimorio sigue sin estar centrado en su casilla, ademas creo que la etiqueta ✦ LEGENDARIO SUPREMO ✦ deberia estar mejor en el bestiario en la info del grimorio, por lo tanto quita ✦ LEGENDARIO SUPREMO ✦ de la ventana de info del grimorio, ademas el color de eso tambien deben incluir negro con pequeños brillos en su nombre, me acabo de dar cuenta que los soles runicos tienen como un brillo especial, bueno es como ese brillo, ademas crea un item cosmetico que sea solo ese brillo especial de los soles runicos, este brilo esta en los soles runicos de 3 anillos en adelante // la explosion del sol sigue cambiando la iluminacion o el flitro usado, lo que puedes hacer es modificar la explosion final del sol runico // los agujeros negros runicos no nacen correctamente donde tienen que nacer, aunque la animacion se hace este nace directamente en el enemigo, ademas agrega un poco de bruma en las animacions de nacimiento de agujeros negros runicos y de soles runicos, claro con la bruma del color correspondiente // al parecer no solo es la explosión final de los soles runicos, los agujeros negros runicos por completo o sea durante toda su vida cambia por completo el bioma creado para las oleadas, es como si quitaran el filtro usado para el bioma // al parecer las oleadas siempre son de noche, sabes que esto no es asi, las oleadas deben ser de dia y de noche, si el libro activa la oleada de dia es un eclipse como el que ya habiamos creado antes, si se activa de noche es luna de sangre teñida de morado, ademas recuerda como es cuando es de dia con el eclipse morado, busca como eran en commit anteriores, y sobre los jefes de las oleadas, estos son independientes por lo tanto no los afectan los biomas, ni el dia, ni la noche, ya que las oleadas son su propio ambiente, entiendes, trata de no olvidar ni modificar las mecanicas que ya existen y estan correctas, no solo en las oleadas, sino en todo el mod, nunca toques lo que ya funciona // la animacion de los minions cuando el grimorio esta fuera en su fase de hambre, esa animacion de orbitarlo como que no se ve bien, deberia ser mejorada, una mejor animacion de orbita, mas exacta o mejor dicho mejor animada y que un minion no se solape sobre otro y que no este desordenada, que sea todo como una danza sincronizada y bonita geometricamente hablando, incluso como atomos // es raro, a veces los minions del grimorio se desactivan solo, revisa eso, los minions deben persistir incluso si el grimorio deja el inventario, solo se deben desactivar si el jugador los quita usando su buff de minion // ahora los proyectiles del grimorio son horribles, necesitamos crear un proyectil digno para el grimorio, de momento deja el que ya teniamos antes, dime si tienes alguna idea o sugerencia para un proyectil o alguna forma de ataque basica».

## A. LAS OLEADAS VIVEN EL DÍA Y LA NOCHE — LA NOCHE PRESTADA MURIÓ

· **EL SÍNTOMA**: «al parecer las oleadas siempre son de noche… las oleadas deben ser de dia y de noche, si el libro activa la oleada de dia es un eclipse como el que ya habiamos creado antes, si se activa de noche es luna de sangre teñida de morado».
· **LA CAUSA**: la v6.51.2 interpretó «el bioma no debe cambiar por nada» como PRESTAR LA NOCHE a nivel de sistema (Main.dayTime=false mientras el festín viviera) — con eso CLAVÓ a las oleadas en la noche eterna.
· **LA CURA**: el préstamo sistema-level MUERE (PrestarNocheDelFestin/DevolverNocheDelFestin y sus llamadas — eliminados). El reloj del mundo sigue SU curso natural (amaneceres y atardeces dentro de una misma oleada) y el AMBIENTE YA SABÍA vestirse solo: **AmbienteOleadaSistema** (v6.50.74+, INTACTO — «no toques lo que ya funciona») tinta el cielo EN VIVO — de DÍA es el ECLIPSE morado (sol eclipse + cementerio + toque morado), de NOCHE la LUNA DE SANGRE morada, con cruce suave al amanecer/atardecer y su bruma cubriendo los astros. Los jefes de oleada SIGUEN independientes del bioma/día/noche (su préstamo por-AI de OleadaNPC — nacido v6.50.73 — NO se toca). La BRUMA AMBIENTE sigue respirando alrededor del portador todo el festín.

## B. EL FILTRO ASESINADO — LA LENTE COMPONE BAJO LOS FILTROS (la causa raíz, al IL)

· **EL SÍNTOMA**: «al parecer no solo es la explosión final de los soles rúnicos, los agujeros negros rúnicos por completo, o sea durante toda su vida, cambian por completo el bioma creado para las oleadas, es como si quitaran el filtro usado para el bioma».
· **LA CAUSA RAÍZ (decompilado Main.Draw de tML 2026.08.3.0)**: el mundo se renderiza en `Main.screenTarget` SOLO cuando hay filtros activos (`Filters.Scene.CanCapture()`); en ese caso `FilterManager.EndCapture(null, screenTarget, …)` compone screenTarget + **LA CADENA DE FILTROS DE PANTALLA** (el Graveyard del festín entre ellos) sobre el BACKBUFFER — y la lente gravitacional vieja (punto 36, DESPUÉS de EndCapture) copiaba `Main.screenTarget` — **el mundo SIN FILTRAR** — y lo volcaba A PANTALLA COMPLETA: el filtro del bioma quedaba BORRADO mientras cualquier fuente viviera (el agujero entero · la nova del sol — la onda StyleNova también era fuente).
· **LA CURA**: la distorsión se compone AHORA **ANTES** de la cadena de filtros — el hook nuevo `On_FilterManager.EndCapture` (ComponerLenteBajoFiltros) corre los pases A/B de distorsión sobre el screenTarget FRESCO y llama al orig CON LA COPIA LENSADA: los filtros de vanilla se aplican ENCIMA del mundo ya distorsionado. Ni la nova de un sol ni un agujero vivo pueden volver a apagar el eclipse ni la luna de sangre. De propina: el volcado del punto 36 ahora exige que el frame HAYA capturado (si el mundo se dibujó directo al backbuffer, screenTarget está rancio y JAMÁS se vuelca — la pantalla congelada latente que esperaba a pasar). RenderLens quedó partido en RecolectarFuentes / RenderPasesDeDistorsion / DibujarEncimaDeLaLente (mismo shader, mismo telescopio, cero cambios visuales fuera del fix).

## C. LA NOVA DEL SOL DOMADA

«modifica la explosion final del sol runico»: la luz de la onda StyleNova baja de 1,8 a 1,0 de intensidad (la StyleFire conserva la suya) — el fogonazo ya no LAVA de blanco el eclipse un segundo entero; respira DENTRO del bioma. (El apagón del filtro era la lente — ver B.)

## D. EL NACIMIENTO CONTINUO DEL AGUJERO + LA BRUMA DE NACIMIENTO

· **EL SÍNTOMA**: «los agujeros negros rúnicos no nacen correctamente donde tienen que nacer, aunque la animación se hace, este nace directamente en el enemigo».
· **LA CURA**: `AgujeroEnViajeProjectile` (nuevo) — el cordón umbilical del nacimiento: al cumplir el ritual, la SEMILLA del agujero nace EN EL PUNTO FOCAL (donde la ceremonia la gestó — encima del jugador) y VIAJA a su cita (acelera hacia la presa, ~1,5 s como mucho) llorando bruma violeta; al llegar SE ABRE: el agujero rúnico de siempre (cualquiera de los 10 — INTACTOS, cero cambios en sus clases) nace en su sitio con su estallido de niebla morada. Los 10 agujeros siguen siendo exactamente los que eran.
· **LA BRUMA DEL COLOR CORRESPONDIENTE** («agrega un poco de bruma en las animaciones de nacimiento»): la GESTACIÓN respira niebla alrededor del punto focal — DORADA para el sol, VIOLETA para el agujero; el sol estrena su vida exhalando un anillo de niebla dorada; la semilla del agujero viaja con su rastro morado y abre con su ráfaga.

## E. LOS DIÁLOGOS ÚNICOS DEL DORMIDO

· **EL SÍNTOMA**: «vi un diálogo de un NPC en el grimorio dormido».
· **LA CAUSA**: el Grimorio Dormido es townNPC (la ventana de diálogo de vanilla lo exige) y el GlobalNPC `PuebloChismesNPC` le robaba SU línea del sueño para ponerle un chisme o un reclamo del pueblo.
· **LA CURA**: los vecinos de VANILLA chismean; los libros del mod NUNCA — el repertorio exclusivo (Caida.Dormido1..8 + Presentación) manda.

## F. LOS ÓVALOS JUNTO A LA BRUMA — MUERTOS DOS VECES

· **En el CRÁTER**: el «vacío central» de LlegadaBrumaProjectile era DOS SoftGlow ESTIRADOS (×1,25/×0,92 — óvalos lisos y planos que la v6.51.1 dejó vivos al curar los puffs). Ahora es una CEBOLLA REDONDA: tres capas concéntricas con disimetrías propias (±7%) y un rim que RESPIRA — se lee como un globo de oscuridad con profundidad, jamás como una elipse dibujada.
· **En el BESTIARIO** (descubierto al IL: la casilla y el retrato dibujan al NPC vía UnlockableNPCEntryIcon → Main.DrawNPCDirect → PreDraw del ModNPC): el dummy del catálogo también corría LA BRUMA (que mide con Main.screenPosition del MUNDO — en el menú caía donde quería: el óvalo junto a la bruma de la casilla) y las ZZZ clipeadas. Ahora el libro posa LIMPIO en el catálogo: SOLO el cuerpo, CENTRADO en su casilla (vanilla centra a los noGravity en el centro exacto de la caja), con su tinte violeta de siempre.

## G. LA FICHA DEL GRIMORIO — LA ETIQUETA SE MUDÓ AL BESTIARIO

· **FUERA del tooltip del arma** (la letra: «quita ✦ LEGENDARIO SUPREMO ✦ de la ventana de info del grimorio»): la primera línea de la v6.51.2 MUERE. El nombre del arma sigue respirando su rareza violeta.
· **DENTRO de la ficha**: `EtiquetaSupremaInfoElement` (nueva) — el rótulo **✦ LEGENDARIO SUPREMO ✦ en NEGRO** (con su soplado violeta-oscuro para leerse sobre el panel), primera línea de la categoría del retrato, justo ENCIMA de las diez estrellas negras… y alrededor nacen y mueren LOS DESTELLOS DE 4 PUNTAS de los soles rúnicos de 3 anillos+ (DrawTwinkles: dos cápsulas cruzadas de blanco incandescente + el corazón dorado) — «me acabo de dar cuenta que los soles rúnicos tienen como un brillo especial… es como ese brillo».

## H. EL BRILLO DE LOS SOLES RÚNICOS — EL ÍTEM COSMÉTICO

`BrilloSolarItem` (nuevo, se entrega al entrar al mundo): **SOLO el brillo especial** — los destellos de 4 puntas flotando en la banda de siempre alrededor del portador (BrilloSolarHalo, la matemática de DrawTwinkles adaptada) + su luz cálida tenue. Nada de disco, ni anillos, ni aura: un trozo de sol caminando por el mundo. Icono de la casa (cápsulas cruzadas + corazón dorado) y hjson es-MX/es-ES/en-US.

## I. LA DANZA ATÓMICA — LA ÓRBITA DEL HAMBRE

· **EL SÍNTOMA**: «esa animación de orbitarlo como que no se ve bien… más exacta… y que un minion no se solape sobre otro y que no esté desordenada, que sea todo como una danza sincronizada y bonita geométricamente hablando, incluso como átomos».
· **LA CURA**: MUERE la rueda plana de anillos concéntricos (mismo plano — al apilarse radialmente se pisaban). Nace **EL ÁTOMO**: cada anillo es una ÓRBITA INCLINADA a su manera (k·0,55 rad) con PRECESIÓN lenta — los planos giran y se cruzan sin tocarse; el paso radial sube a 30 px (el ojo mide ~28: los anillos vecinos JAMÁS se pisan); TODO el sistema RESPIRA JUNTO (un solo latido); el giro es ARMÓNICO y contrarrotante (razones simples entre anillos — los cruces siempre al mismo compás). La escalera de puestos es la de la letra (5+k ojos por anillo). Y LA LLEGADA EXACTA: cerca del puesto, el ojo lo persigue proporcional puro — SIN inercia, SIN sobrepaso (el controlador viejo conservaba velocidad y CRUZABA el hueco) — cada ojo clavado en subpíxel a su órbita móvil.

## J. LOS MINIONS PERSISTEN — LA PROMESA COMPLETA

· **EL SÍNTOMA**: «es raro, a veces los minions del grimorio se desactivan solos… deben persistir incluso si el grimorio deja el inventario, solo se deben desactivar si el jugador los quita usando su buff de minion».
· **LA CAUSA**: el «techo vivo» de la v6.50.111 solo protegía al OJO ETERNO — el resto del escuadrón (Fragmento de Supernova, Living Pulsar, Nebula Jellyfish, Stellar Comet, la sierpe) seguía muriendo al primer tick en que el libro salía de la mano/barra (vanilla culla minions cada tick si los slots exceden maxMinions).
· **LA CURA**: el techo vivo cubre a TODO el escuadrón — mientras viva CUALQUIERA de los SEIS, lo vivo es LEGAL. El libro puede irse AL COFRE que los suyos siguen en el aire; la única despedida legítima es QUITAR EL BUFF.

## K. LA MANADA DEL VAGABUNDO — INTERACCIONES Y ANIMACIÓN

· **LA MANADA**: el singleton estático MURIÓ. Cada recuadro del seleccionador que posa un grimorio menor tiene SU PROPIO reloj de escape y SU PROPIO vagabundo (identidad: posición del recuadro) — con varios personajes con la mascota, la pantalla se llena de libros.
· **LAS INTERACCIONES (2+)**: EL DIRECTOR DE LA MANADA alterna la deambulación con actos de conjunto — **EL CORRO** (todos orbitan un centro común en puestos perfectamente repartidos, un solo compás, con la OLA DE GIROS DE CAMPANA sincronizados al despedirse), **LA PERSECUCIÓN** (el líder deambula y los demás lo persiguen en tren — el rol ROTA cada 4 s) y **EL CHOQUE** (dos libros: cargas frente a frente, topetazo con chispas y rebote, tres rondas). Y mientras UNO hace su acto solista (el título · el sol · la luna), los demás son ESPECTADORES: se acomodan en arco, MIRAN al actor, aletean y comentan con chispitas.
· **LA ANIMACIÓN MEJORADA**: el ALETEO DE VUELO (las páginas baten X±5% a 3 Hz cuando vuela rápido), el ATERRIZAJE CON BOTE (al llegar a la pausa la escala Y se aplasta y rebota amortiguado), el RASTRO PROPIO por libro (los fantasmas de un vagabundo ya no se mezclan con los de su hermano) y el tinte levemente distinto por libro (los gemelos se distinguen). Con UN solo vagabundo: TODO como siempre.

## L. EL PROYECTIL DE SIEMPRE — Y EL DIGNO PENDIENTE

«de momento deja el que ya teniamos antes»: la fusión-tentáculo de la v6.51.2 MUERE como disparo básico — vuelven LOS DISCOS DEL EQUINOCCIO (el 931 de la v6.51.1: el dorado y el violeta con sus marcas 9201/9202). El proyectil DIGNO queda como pendiente de diseño — las ideas van con el mensaje de la versión.

---

**Verificación**: oráculo 0 errores/0 warnings · -build REAL 0/0 ×2 · .tmod v6.51.3 (2.792.364 B, md5 122e40199bb4165bc147e67396b6d066): 395 ENTRADAS (379 rawimgs + el ícono nuevo), tabla+EOF EXACTOS, DLL 1.434.624 B con los 9 símbolos nuevos verificados dentro (AgujeroEnViajeProjectile · BrilloSolarHalo · EtiquetaSupremaInfoElement · ComponerLenteBajoFiltros · RecolectarFuentes · MANADA_CORRO · FASE_ESPECTADOR · AleteoVuelo · BoteAterrizaje) y PrestarNocheDelFestin AUSENTE · hjson es-MX empaquetado con BrilloSolarItem · servidor headless: Sandboxing + Finalizing de v6.51.3, Server started, 23.371 líneas CERO excepciones.

"""

with io.open('CHANGES.md', 'r', encoding='utf-8') as f:
    viejo = f.read()
with io.open('CHANGES.md', 'w', encoding='utf-8') as f:
    f.write(ENTRADA + viejo)
print("CHANGES.md — entrada v6.51.3 prepended")

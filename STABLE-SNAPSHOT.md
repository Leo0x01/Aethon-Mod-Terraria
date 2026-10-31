# v6.56.9 — LA PILA DE CAPAS GPU-UNIVERSAL + LA IMPLOSIÓN DE SOLES + LA AUDITORÍA EJECUTADA — EL CHECKLIST

> **PUBLICADA** — release [429618372](https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.56.9) + CDN verificado byte a byte (md5 `ba7f6bf57c23f02fee3617551986f3e3`, 6.364.617 B; asset id 693887337).

**Qué pasó** (la letra): «ahora se ve igual pero con un color diferente… lo que le faltan son varias capas de texturas con mas difuminado, glow entre demas efectos» + «los brillos de soles… aparecer lejos y contraerse hacia el centro pero sin dar vueltas, como una implosion» + «copia ese rc2 en varias armas con los cambios diseñados asi no dañamos el codigo original» + «cualquier gpu incluso los que no son dedicadas deben ver el shader» + «es momento de mejorar las librerias».

1. **LA PILA DE CAPAS (la cura de «una sola capa»)**: [ ] el RAYO DE PRUEBA A muestra BRUMA ambiental alrededor del conducto (violeta difuso que tiñe el fondo); [ ] ENVOLVENTE de glow con la paleta viajando (violeta→morado→rojo→magenta); [ ] el CUERPO de plasma del shader de siempre; [ ] la VENA NÚCLEO blanca-violeta en el centro; [ ] SOLES DE 4 PUNTAS corriendo por las márgenes hacia la punta. **Cómo debe verse: 5-6 capas distinguibles, glow que se extiende MUCHO más allá del núcleo saturado, bordes difuminados.**
2. **LAS TRES RECETAS (elige la ganadora)**: [ ] **A LA EQUILIBRADA** (elegante, el diseño base); [ ] **B EL TITÁN** (el más gordo y dramático — bruma y glow al máximo); [ ] **C EL SUEÑO** (todo fundido, máximo desenfoque, doble glow). El RC2 original queda INTACTO para comparar.
3. **CERO PICOS**: [ ] los bordes de todas las capas son LISOS (sin dientes de sierra ni peines) — la sim 1:1 dio 0 picos; si ves algo raro, captura y reporta.
4. **GPU-INTEGRADA**: [ ] en la máquina del jugador (sin GPU dedicada) la PILA SE VE COMPLETA — las capas usan el pipeline vanilla de Terraria (el .fx es un plus: si falla, la pila sigue).
5. **LA IMPLOSIÓN DE SOLES (la carga)**: [ ] los soles de 4 puntas APARECEN LEJOS y CAEN RECTOS hacia la boca (NADIE da vueltas/orbita); [ ] la caída ACELERA al acercarse; [ ] el enjambre se aprieta conforme la carga llena; [ ] al disparar salen ESCUPIDOS en abanico hacia el rayo.
6. **LA IGNICIÓN DE CINE (las copias)**: [ ] al disparar: EL MUNDO SE CONGELA un instante (los NPC quietos ~6 ticks — solo en un jugador) + kick + flash + baño de luz.
7. **LAS LIBRERÍAS**: [ ] TODO el mod se ve igual que siempre (la purga de 40 métodos muertos + BeamLib/BoltRenderer enterradas + helpers canónicos NO cambió NI UN efecto — solo borró lo muerto y unificó la lógica); [ ] el F8 sin errores nuevos.
8. **LOS CHECKLIST VIEJOS**: el de la .56.8 (paleta del conjuro, boca sin discos) y el de la .56.7 (cero picos en el RC2 original) siguen vigentes.

---

# v6.56.8 — LA CURA DE LA BANDA PLANA + LA PALETA DEL CONJURO + LA AUDITORÍA — EL CHECKLIST

> **PUBLICADA** — release [429441323](https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.56.8) + CDN verificado byte a byte (md5 `b04172a1f883eb9da60f29ce07b88b72`, 6.364.679 B; asset id 693270961).

**Qué pasó** (la letra): «bueno ahora esta igual, sin glow, sin desenfoque, es solo una sola capa y se ve simple, necesita mas capas pero aplicadas correctamente y sin picos, siempre analisa lo que haces para ver si crea picos o no» + «los colores del mod son principalmente morado, rojo, negro, no olvides lo que es el mod realmente» + «ademas estan los circulos planos… nada de circulos planos, si necesita efectos o shader usa las librerias existentes» + «no me diste el analisis que hicieste de las librerias» + «dale una mejora de shader».

1. **LA BANDA PLANA (la cura)**: [ ] el haz muestra el PLASMA DE TEXTURA de siempre (nubes/filamentos del fBm — NO una banda lisa de color plano); [ ] HAY GLOW alrededor (el halo difuso per-pixel); [ ] HAY DESENFOQUE en el borde (la falda pow³ fundiéndose); [ ] se ven VARIAS CAPAS (cuerpo + venas + núcleo + halo), no una sola.
2. **LA PALETA DEL CONJURO (morado · rojo · negro)**: [ ] el haz es VIOLETA/MORADO con VENAS ROJAS hirviendo (no amarillo/verde/cian arcoíris); [ ] el color VIAJA a lo largo del conducto (la boca y la punta en distinta fase del ciclo violeta→morado→rojo→magenta); [ ] la LUZ del mundo morada/roja; [ ] la corona rúnica con el aro rojo-dorado.
3. **SIN CÍRCULOS PLANOS**: [ ] la BOCA no tiene NINGÚN disco (los 4 glows apilados murieron — en las capturas se leía como disco con anillos concéntricos); [ ] la boca = corona rúnica (que ENCOGE con la carga: el indicador son los anillos) + soles de 4 puntas del accesorio + micro-arcos + LA ESTRELLA CONDUCTO (el destello de 4 puntas latiendo en la boca mientras dispara).
4. **SIN PICOS (la prueba del análisis siempre)**: [ ] el borde del haz LISO a toda potencia (la rugosidad medida en sim: 1.64 px de media — una curva no puede producir picos); [ ] el pulso viajero corriendo hacia la punta SIN sierra.
5. **EL FALLBACK (el seguro)**: [ ] si el shader no cargara en tu GPU, el haz se ve DIGNO igual (4 tiras suaves morado/rojo con núcleo blanco — sin peine de picos).
6. **LO QUE VIVE**: [ ] temblor sostenido + golpe de encendido; [ ] sonidos intactos (carga + bucle); [ ] destello de ignición; [ ] vuelo infinito del dueño; [ ] un solo RC2 a la vez.
7. **LA AUDITORÍA DE LIBRERÍAS (entregada)**: `AUDITORIA-LIBRERIAS.md` en el repo — 69 librerías/43.600 líneas: 2 muertas, 3 grupos de fusión, ~3.900 líneas recuperables, 3 librerías nuevas propuestas.
8. **REGRESIÓN**: [ ] las demás armas intactas; [ ] F8 presupuesto sano.

---

# v6.56.7 — EL RC2 VUELVE A SU TÉCNICA (607df36): LA CURA DE LOS PICOS — EL CHECKLIST

> **PUBLICADA** — release [429356315](https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.56.7) + CDN verificado byte a byte (md5 `b6f37c7f12d21fc14d175725b7782d2c`, 6.363.051 B; asset id 693010906).

**Qué pasó** (la letra): «primero borrar las variantes de rayo concentrado 2» + «ahora tiene picos y antes no, ¿por qué se hacen esos picos?, acaso es que se usan mal las texturas» + «el commit donde estaba correcta la técnica de haz es el 607df36… usa el código de 607df36 para reparar el problema de los picos y la falta de efecto» + «recuerda cual es el color del mod» + «los círculos rúnicos como en la versión actual» + «en la fase de carga también pusiste un círculo plano… usa los mismos anillos rúnicos» + «te olvidaste de usar los brillos de soles rúnicos del accesorio» + «dale una mejora de shaders» + «documentar la solución de los picos».

1. **LAS VARIANTES (muertas)**: [ ] la Bolsa de las Definitivas entrega SOLO el Rayo Concentrado 2; [ ] ni rastro de Prisma/Eclipse/Tormenta/Solar/Vórtice/Espectro/Sierpe/Cuántico/Nova (ni ítems ni iconos ni textos).
2. **LOS PICOS (LA CURA)**: [ ] el haz del RC2 tiene el borde ORGÁNICO de siempre — sin dientes, sin sierra, sin peines de blobs (la PILA de capas murió: nada de fajitas CPU encima del strip); [ ] al ENSANCHARSE el cono a plena potencia sigue liso (donde la sierra era peor); [ ] SIN franjas negras en los bordes (el lote aditivo de la .56.4 se mantiene).
3. **LA TÉCNICA DE 607df36 (la que funcionaba)**: [ ] EL COLOR DEL MOD: el gradiente PRISMÁTICO oil (magenta→naranja→amarillo→verde→cian) VIAJANDO a lo largo del haz — no el violeta-oro del juicio; [ ] LA BOCA: un bloom de VARIOS glows apilados con NÚCLEO BLANCO caliente (no un solo círculo); [ ] el plasma de las 4 texturas HIRVIENDO (la carne × el segundo ruido × la rampa + las chispas del doble AND); [ ] la PLUMA del extremo lejano (el cono se disipa a lo lejos, como siempre).
4. **LOS CÍRCULOS RÚNICOS (de la versión actual, al 53 %)**: [ ] la TRIPLE CORONA (dorado de 8 + violeta de 6 contrarrotando + blanco íntimo) pequeña alrededor de la boca; [ ] los DOS ANILLOS finos contrarrotando; [ ] LA CORONA ENCOGE con la carga (70→26 px) — EL INDICADOR DE CARGA SON LOS ANILLOS, no un disco plano; [ ] SIN círculo negro, SIN lente, SIN aguja.
5. **LOS SOLES RÚNICOS DEL ACCESORIO (ahora VISIBLES — el bug de coordenadas curado)**: [ ] durante la carga, destellos de 4 PUNTAS (cápsulas cruzadas + corazón dorado — LOS MISMOS de El brillo de soles rúnicos) reunirse y ACELERAR hacia el arma en la banda que encoge; [ ] AL DISPARAR: los destellos salen EXPULSADOS en abanico hacia el rayo.
6. **EL SHADER MEJORADO**: [ ] EL HALO difuso alrededor del haz (el desenfoque per-pixel — más ancho y suave que antes); [ ] LOS FILAMENTOS: cordones grandes serpentean bajo el hervor fino; [ ] EL NÚCLEO QUE LATE: la vena blanca respira (~1 s de ciclo); [ ] LA FRANJA PRISMÁTICA: el borde del halo con micro-desfase rojo/azul.
7. **LO QUE VIVE**: [ ] el temblor sostenido + el golpe de encendido; [ ] el sonido de carga y el bucle grave (intactos); [ ] los micro-arcos de la carga; [ ] el destello de la ignición; [ ] el vuelo infinito mientras dispara.
8. **REGRESIÓN**: [ ] las demás armas del mod intactas; [ ] F8: presupuesto sano; [ ] MP si pruebas: la mira del dueño, un solo RC2 a la vez.

---

# v6.56.6 — LA PURGA + LAS ALAS SIMÉTRICAS + EL RC2 DESNUDO + LAS 9 VARIANTES — EL CHECKLIST

> **PUBLICADA** — release [429218656](https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.56.6) + CDN verificado byte a byte (md5 `8bd69344bdefa480bd722021eabe6382`, 6.385.674 B; asset id 692603464).

**Qué pasó** (la letra): textos crudos («mods.aethonmod.nivel» en la barra de exp) · alas aún asimétricas con «esa ligera rotación en el sentido contrario» · «borra todas las nuevas armas y solo deja al rayo concentrado 2» · el RC2 sin círculo negro, sin lente, coronas −50%, más texturas/efectos y LOS MISMOS brillos del accesorio de soles rúnicos · «al menos 9 variantes más con diferentes efectos y técnicas vfx» · auditoría de librerías.

1. **LOS TEXTOS CRUDOS (la cura del fallback)**: [ ] con el juego EN INGLÉS el mod se lee ENTERO en español (la barra de exp dice «Grimorio · Nv X», no «Mods.AethonMod.HUD.Nivel»); [ ] con el juego en Español idéntico que antes; [ ] ningún texto crudo «Mods.AethonMod…» en ninguna parte (el en-US espejo es la red de seguridad del motor).
2. **LA PURGA**: [ ] en la bolsa de las Definitivas SOLO el RC2 y sus 9 variantes; [ ] la escalera/Prisma/Partidor/Réquiem/Ira/Índice/Pluma/Ojo GONE (ni en recetas ni en el bestiario raro); [ ] los personajes con las armas viejas las ven como items desaparecidos (esperado — la purga es la purga).
3. **LAS ALAS**: [ ] el par ESPEJO PERFECTO parado (nada de un ala más abierta que otra); [ ] SIN rotación rara en reposo (el rake de 5° murió); [ ] al correr/volar responden igual que antes (Spread abre, Flap acelera) pero SIN torcerse el par.
4. **EL RC2 DESNUDO**: [ ] SIN círculo negro sobre el personaje (el disco murió); [ ] SIN lente gravitacional (la curvatura del espacio murió); [ ] las coronas rúnicas VISIBLES pero pequeñas (~la mitad); [ ] LA CARGA: destellos de 4 PUNTAS (cápsulas cruzadas + corazón dorado — LOS MISMOS del accesorio El brillo de soles rúnicos) reuniéndose/acelerando hacia el arma; [ ] AL DISPARAR: los destellos salen EXPULSADOS en abanico hacia el rayo; [ ] EL HAZ: desenfoque difuso en los bordes + glow del gradiente + vena central BLANCA incandescente + soles de 4 puntas corriendo por dentro (muchas texturas, muchos efectos); [ ] el temblor sostenido mientras el rayo vive + el golpe de encendido.
5. **LAS 9 VARIANTES** (todas cargan/disparan/temblan igual; SOLO cambia la piel): [ ] **PRISMA** rojo+cian cremando el costado, gradiente arcoíris; [ ] **ECLIPSE** violeta oscuro exhalando bruma negra; [ ] **TORMENTA** rayos azules reptando por el conducto; [ ] **SOLAR** lenguas de fuego doradas brotando; [ ] **VÓRTICE** nebulosas cian-violeta orbitando; [ ] **ESPECTRO** dos fajas fantasma verdes escoltando; [ ] **SIERPE** el haz entero ondulando como serpiente ámbar; [ ] **CUÁNTICO** ramitas magenta/cian naciendo y muriendo; [ ] **NOVA** anillos rosas viajando hacia afuera; [ ] solo UN haz a la vez (el original y las variantes se excluyen entre sí).
6. **REGRESIÓN**: [ ] el RC2 original (variante 0) conserva SU juicio violeta-oro; [ ] los sonidos idénticos (carga + bucle — el bucle byte a byte intacto); [ ] las demás armas del mod intactas; [ ] F8: presupuesto sano.

---

# v6.56.5 — LA CURA DEL PITIDO + LA AUDITORÍA DE LOS 4 AGENTES — EL CHECKLIST

> **PUBLICADA** — release [428546845](https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.56.5) + CDN verificado byte a byte (md5 `6bfda3d02cdfc7bd438c43e7d3404484`, 6.344.773 B; asset id 690452563).

**Qué pasó** (la letra): «en el sonido de carga escucho un pitido al final» + «haz una auditoría en los últimos cambios en busca de errores no detectados».

1. **EL PITIDO DE LA CARGA**: [ ] al final de la carga (últimos ~0.45 s) ya NO suena ningún tono puro — solo el crescendo grave con un soplo de presión al final; [ ] el final del one-shot EXHALA (fade 60 ms — antes un golpecillo); [ ] el bucle del láser suena IGUAL que en la .56.4 (no se tocó).
2. **EL TEMBLOR (antes sub-píxel — invisible)**: [ ] al DISPARAR el RC2 se siente el golpe de encendido (~7 px); [ ] mientras MANTIENES el rayo el mundo RUMBLEA sutil (~2.75 px constantes — «un poco», sin marear); [ ] al apagarse muere solo.
3. **EL PORTAL DEL CONJURO**: [ ] el núcleo negro ahora es GRANDE (96-132 px de radio a plena potencia — antes salía a la mitad y su corona lo superaba: el eclipse se leía al revés).
4. **LAS ARMAS DE PICOS (la espina al revés)**: [ ] la COLUMNA de la Ira brilla en el PUNTO DE IMPACTO (antes se fundía a negro justo al llegar al suelo); [ ] la telegrafía y el CORTE del Partidor iluminan su mitad INFERIOR (donde muerde el juicio); [ ] el color sigue viajando en su dirección original y el hervor conserva su sentido; [ ] el Prisma/escalera/RC2 IGUAL que la .56.4.
5. **LAS ALAS RÚNICAS (Solares/Vacío/Eclipse/Juicio)**: [ ] las runas de las plumas gemelas giran en SENTIDO OPUESTO (el par se lee como un espejo — antes giraban al mismo sentido); [ ] al mirar a la IZQUIERDA el giro de las runas también se espeja; [ ] las plumas gemelas centellean y muestran EL MISMO glifo (antes diferían); [ ] el anillo central de 8 glifos REVERSA su orbital al girar el personaje.
6. **LA CARGA MUERE CON SU DUEÑO**: [ ] morir a mitad de carga corta el sonido de carga al instante ( antes seguía sonando 5 s huérfano).
7. **IDIOMAS/PAQUETE**: [ ] el mod sigue monolingüe (es-ES == es-MX, sin en-US); [ ] la descripción del navegador de mods dice «Localización ES (es-ES + es-MX)»; [ ] el .tmod pesa ~4.7 KB menos (los .fx fuente fuera; los shaders cargan igual).
8. **REGRESIÓN GENERAL**: [ ] el RC2 completo como la .56.4 (destellos, portal, haz sin franjas, sonido grave); [ ] las alas no-rúnicas como la .56.4; [ ] Réquiem/Ojo/Primer Trazo intactos; [ ] F8: presupuesto sano.

---


# v6.56.4 — EL CAMINO GPU DEL RC2 + EL ESPEJO COMPLETO DE LAS ALAS + EL PORTAL DEL CONJURO — EL CHECKLIST

> **PUBLICADA** — release [428511425](https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.56.4) + CDN verificado byte a byte (md5 `70b29ef940fa84dbac8c4ed18cfe92da`, 6.349.490 B; asset id 690309919).

**Qué pasó** (la letra): «solo deja español es_es» + «las alas no son simétricas y no cambian la dirección [al moverse] — todas las alas del mod» + «las armas con picos tienen aún más picos: adapta el código del rayo concentrado 2» + la aguja de carga «se ve horrible» (→ los brillos de los soles rúnicos), el círculo plano del RC2 (→ varios círculos rúnicos de agujeros negros), las franjas negras del haz, el sonido (carga generada + más grave + fade) y el temblor del mundo.

1. **LOS PICOS (la 4ª ronda — EL CAMINO GPU)**: [ ] el Rayo del ALBA liso al fluir; [ ] el VEREDICTO liso (con SU aberración); [ ] el JUICIO liso; [ ] el PRISMA: el látigo verlet sin quiebres; [ ] el PARTIDOR: la CARGA (senoide) y el CORTE lisos; [ ] la COLUMNA de la Ira lisa; [ ] el RC2 intacto (su camino GPU era el que se portó). Los conductos ahora pasan por el shader POR PÍXEL — el borde funde suave, sin sierra.
2. **LAS ALAS (todas — incluidas las rúnicas)**: [ ] el par ES SIMÉTRICO en reposo (el filo/lomo ya no cruza lados); [ ] al GIRAR a la izquierda el render es el ESPEJO exacto; [ ] al MOVERSE, el enjambre se INCLINA hacia atrás (correr barre la punta ~12 px; girar en plena carrera la mueve ~32-42 px); [ ] el centelleo es parejo entre alas; [ ] las runas de las rúnicas giran espejadas.
3. **EL RC2**: [ ] al USAR el arma, SUENA LA CARGA (crece y desemboca en el disparo — antes era silencio); [ ] durante la carga, BRILLOS DE SOL se reúnen a gran velocidad en el centro (la aguja de la «línea que avanza y retrocede» MUERE); [ ] al disparar, los brillos estallan hacia la dirección del rayo; [ ] en la boca del arma hay UN PORTAL: disco negro + VARIOS CÍRCULOS RÚNICOS (el círculo plano MUERE) + el espacio se CURVA alrededor (lente); [ ] el HAZ SIN FRANJAS NEGRAS al ensancharse; [ ] el sonido del láser es MÁS GRAVE, se mantiene, y se apaga con el rayo; [ ] el MUNDO TIEMBLA un poco mientras dispara (fuerte al encendido).
4. **LA ESCALERA**: [ ] cada peldaño conserva SU paleta y SU flujo (el alma de la .56.3) ahora sobre el cuerpo nuevo del shader; [ ] los peldaños también tienen su portal rúnico a su escala (coronas con el alma del peldaño).
5. **IDIOMAS**: [ ] el juego en «Español (España)» Y en «Español (Latinoamérica)» ven el mod EN ESPAÑOL (el es-MX es espejo del es-ES); [ ] el paquete en-US ya NO EXISTE (el mod es monolingüe — el juego en inglés mostrará claves sin traducir, aceptado).
6. **REGRESIÓN GENERAL**: [ ] Réquiem/Ojo/Primer Trazo/telégrafo de la Ira: como en su mejor época (la reversión de RayoStrip les devolvió el estado pre-regresión); [ ] MP si pruebas: todo lo anterior; [ ] F8: el presupuesto sano.

---

# v6.56.3 — LA RONDA DEL PERSONAJE NUEVO — EL CHECKLIST

> **PUBLICADA** — release [428376306](https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.56.3) + CDN verificado byte a byte (md5 `28d0bf10f831c24e9d968e283a2119e6`, 6.374.841 B).

**Qué pasó** (la letra): un personaje nuevo destapó la ronda — la llegada del grimorio jamás se mostraba en el mapa, las alas descentradas/rotadas/sin flip, el RC2 y el Ojo «no funcionaban» (el muro invisible de maná: costaban 100), picos en Partidor/Prisma/los 3 rayos, los 3 rayos idénticos, y el Testamento retirado.

1. **EL FARO DEL MAPA (la nueva estrella)**: [ ] con un personaje que AÚN NO recibe el grimorio, dejar pasar la primera noche → cuando la bruma se disipe, el minimapa y el mapa completo muestran el ICONO DEL GRIMORIO pulsando en su ubicación; [ ] al EXTENDER LA MANO el icono desaparece; [ ] si sales y vuelves a entrar al mundo, el icono sigue ahí mientras no lo tomes; [ ] el hover del icono dice «El Grimorio espera a su destinatario…».
2. **EL MURO DE MANÁ**: [ ] con el maná BAJO (o el personaje nuevo), el Rayo Concentrado II ahora SÍ dispara (cuesta 10); [ ] el Ojo del Juicio SÍ se coloca arriba y barre; [ ] TODAS las definitivas disparan con 20 de maná base.
3. **LAS ALAS**: [ ] centradas en el personaje (las 23 — incluidas las rúnicas); [ ] sin la inclinación rara; [ ] al girar a la IZQUIERDA las alas se espejan (cada garra abre hacia AFUERA); [ ] las runas de las alas rúnicas siguen las puntas al voltearse.
4. **LOS PICOS (la 3ª ronda)**: [ ] el Rayo del ALBA liso al fluir (era el peor); [ ] el VEREDICTO liso; [ ] el JUICIO liso; [ ] el PARTIDOR sin dientes en la CARGA (la senoide) y en el corte; [ ] el PRISMA sin quiebres angulares cuando latiguea (la espina se remuestrea suave); [ ] LA PUNTA de los rayos se estrecha (ya no corte plano).
5. **LAS ALMAS DE LA ESCALERA**: [ ] el Alba es DORADO-AMANECER; [ ] el Veredicto es ÁMBAR-VIOLETA; [ ] el Juicio conserva el arcoíris; [ ] hierve cada uno a su velocidad; [ ] el Veredicto tiene SU aberración de borde.
6. **EL TESTAMENTO, RETIRADO**: [ ] la bolsa de definitivas entrega 11 (ya no 12); [ ] nada lo referencia en ningún tooltip; [ ] si tenías uno en un cofre del mundo viejo, desaparece al recargar (esperado — el tipo murió).
7. **LA IRA Y EL RÉQUIEM SIN POLVOS**: [ ] la tormenta de la Ira cae con partículas de la casa (la lluvia ámbar viva, la convergencia al marcado en espiral); [ ] los anillos del Réquiem orbitan SIN la sierrita dorada del borde.
8. **REGRESIÓN GENERAL**: [ ] el Verbo Primordial en su movimiento VII (La Sinfonía) ahora SÍ dibuja su concierto (la excepción del client.log murió); [ ] el RC2 con su aguja y su láser intactos; [ ] MP si pruebas: todo lo anterior + el faro del mapa SOLO lo ve el destinatario.

---

# v6.56.2 — LA CURA QUE FALTÓ (la 6ª recursión del Partidor) — EL CHECKLIST

> **PUBLICADA** — release [427672847](https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.56.2) + CDN verificado byte a byte (md5 `22dfb5202c3399e610e7f07c9cb0eba1`, 6.377.522 B).

**Qué pasó**: la pasada de verificación tras publicar la .56.1 cazó la última
recursión del decompilador viva en `ReceiveExtraAI` del Partidor de Mundos
(la cura se aplicó a medias: el Send sí, el Receive no — el chequeo de
idempotencia del cirujano saltó una eliminación pura). En MP mataba el sync;
en SP dormía.

1. **MP (si pruebas con servidor)**: [ ] disparar el PARTIDOR DE MUNDOS ya no
   tumba nada (era el único de los 3 netImportant que conservaba la
   recursión del lado Receive); [ ] el corte a pantalla completa se ve igual
   en el cliente del dueño y en el servidor.

---

# v6.56.1 — LA RONDA DE CURACIÓN (LA AUDITORÍA DE LOS 4 AGENTES) — EL CHECKLIST

> **PUBLICADA** — release [427665989](https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.56.1) + CDN verificado byte a byte (md5 `65ef3f22c11150718760bb2845fa6f05`, 6.380.548 B).

**Qué pasó** (la letra): «Espero que hayas tomado en cuenta EFECTOS-Y-LIBRERIAS.md
ya que es fundamental» + «Quiero que también analices el código en busca de
errores no detectados antes». LA CAZA: 4 agentes en paralelo con el códice
como vara + verificación runtime contra los assemblies reales del juego.
Salieron 2 CRÍTICOS de multijugador (la recursión SendExtraAI del
decompilador y el ai[2] envenenado de las 3 armas nuevas), 6 ALTOS (la
sentencia doble del Testamento, el lote aditivo que lavaba los proyectiles,
la escalera sin exclusión cruzada, la aguja invisible del RC2, el GC del
láser, las miras del servidor), el derrame del es-MX (−636 líneas de
duplicados, paridad 1369=1369=1369 al parser REAL) y el códice puesto al
día (la lista de huérfanas era un boomerang de purga).

1. **EN JUEGO (SP — nada debe cambiar, todo debe MEJORAR)**: [ ] el RC2
   carga con SU AGUJA de puntería apuntando y creciendo (vivía invisible
   desde la .56.0); [ ] el RC2/Alba/Veredicto/Juicio/Prisma NO se pueden
   superponer (un solo haz a la vez, cruzado); [ ] un proyectil cualquiera
   (flecha, esbirro) en pantalla DURANTE un láser se ve NORMAL (el lote
   ya no lava); [ ] Ira/Partidor/Réquiem/Testamento/Primer Trazo idénticos
   a como los dejaste en la .56.0.
2. **LOS NOMBRES EN ESPAÑOL**: [ ] con el juego en Español (LatAm o
   España), las 3 armas nuevas + la escalera + las sentencias ya no
   muestran nombres en inglés; [ ] el aviso al entrar al mundo habla del
   COFRE del Arsenal (ya no «una por categoría»).
3. **MP (si pruebas con servidor)**: [ ] disparar Alba/Veredicto/Juicio/
   RC2/Partidor YA NO TUMBA nada (la recursión ExtraAI murió); [ ] el Ojo
   del Juicio se coloca y barre bien con CUALQUIER slot de jugador; [ ] el
   Testamento dicta UNA sola sentencia; [ ] el vuelo infinito del RC2 solo
   lo siente quien dispara; [ ] el haz apunta donde mira el DUEÑO.
4. **HIGIENE**: [ ] recargar el mod en juego (Mods → Reload) sin fugas
   raras; [ ] la bolsa de alas sigue entregando las 22; [ ] la marca del
   festín: mata un jefe marcado DESPUÉS de 6+ s por otra causa → la muerte
   es de esa causa (ya no hay cine eterno).

---

# v6.56.0 — LA CURA DE LOS CÍRCULOS PLANOS + LA ESCALERA DEL RAYO + LAS ALAS CENTRADAS + EL PULSO CONSTANTE — EL CHECKLIST

**Qué pasó** (la letra): «hay circulos planos al lanzar el ataque y al
cargarlo antes de lanzarlo y despues… si es polvo ya sabes de donde
conseguir el efecto» + «el sonido va y viene como si fueran ondas… hazlo
un pulso constante y luego acabe» + «recuerdas los colores que usa el
mod» + «las alas no estan centradas… se van hacia un lado o se rotan…
crea alas runicas, varios ejemplos» + «la ira del cielo no tiene
suficiente bruma, debe ser densa, el ataque atrae las nubes» + «el
partidor… ni la bruma ni las particulas… de un extremo de la pantalla
hasta el otro» + «el prisma… version inferior… crea 3 versiones
inferiores mas» + «el primer trazo tambien esta llena de picos,
investiga bien» + «crea al menos otras 3 armas definitivas, se
creativo». LA CURA: LA LEY DEL CERO-DUST (los polvos de vanilla eran
los círculos planos Y parte de los picos), el sonido v2 pulso constante,
los colores del Juicio, las alas centradas + 7 nuevas, la escalera de
5 niveles del Rayo Concentrado, el Partidor a pantalla completa, la Ira
densa y 3 armas del grimorio nuevas.

1. **LA PRUEBA DE LOS CÍRCULOS (LA QUE IMPORTA)**: [ ] agarrar EL RAYO
   CONCENTRADO II y cargarlo: ya NO hay discos/círculos planos — el foco
   es un bloom compacto con DOS ANILLOS DE CONTENCIÓN que se contraen
   (150→56) contrarrotando + micro-arcos; [ ] al lanzar y después del
   lanzamiento: CERO polvos cromáticos derivando — solo el canal vivo;
   [ ] EL SONIDO: un PULSO constante (inicia → se mantiene → acaba) —
   ya NO va y viene como ondas; [ ] LOS COLORES: el haz viste el
   gradiente del Juicio (la paleta del mod, la de la Ira).
2. **LA ESCALERA (bolsa de las definitivas — ahora 12)**: [ ] RAYO DEL
   ALBA / VEREDICTO / JUICIO: el mismo haz escalado (0.45/0.60/0.75) —
   el Juicio con aberración cromática; [ ] EL PRISMA REFORMADO (nivel
   0.85): ya NO son 7 haces — es LA ESPINA VIVA verlet vestida de
   plasma, con el color del Juicio viajando y cero polvos (la sentencia
   del Testamento sigue al usarlo); [ ] no se pueden lanzar dos haces a
   la vez (EscaleraDelRayo.HazLibre).
3. **LAS ALAS (bolsa 15→22)**: [ ] cualquier ala de SIEMPRE: centrada
   en la espalda (ya no se va a un lado) y SIGUEN la rotación del cuerpo
   (rotarse con un carro/minijuego); [ ] LAS 4 RÚNICAS (Solares, del
   Vacío, del Eclipse, del Juicio): runa en cada punta de pluma + anillo
   de 8 glifos contrarrotante — la del Juicio con el gradiente caminando;
   [ ] LAS 3 ANATOMÍAS (Agujas del Vacío, Cuchillas del Eclipse, Corolas
   del Alba): siluetas radicalmente distintas.
4. **EL PARTIDOR DE MUNDOS**: [ ] el corte CUBRE LA PANTALLA de extremo
   a extremo (techo→piso) SIN importar dónde esté el cursor o el jugador;
   [ ] mover al jugador/jefe durante el ataque: el ancla del corte NO se
   mueve; [ ] ya NO hay bruma ni partículas de polvo — en su lugar
   PULSOS viajeros por el canal + esquirlas-tira del corte.
5. **LA IRA DEL CIELO**: [ ] el cielo se CUBRE de bruma densa ANTES del
   juicio (racimos orgánicos convergiendo — el ataque ATRAE las nubes);
   [ ] de día Y de noche la bruma se ve.
6. **EL PRIMER TRAZO**: [ ] el trazo ya NO tiene picos de tinta — la
   carne de plasma con el remolino cruzado y la punta viva ardiendo.
7. **LAS 3 DEL GRIMORIO (bolsa de las definitivas)**: [ ] EL ÍNDICE DEL
   JUICIO: el libro real + 8 páginas fantasma que salen volando y
   cortan; [ ] LA PLUMA DEL VEREDICTO: el sello que se dibuja glifo a
   glifo y DETONA en 12 lanzas + triple onda; [ ] EL OJO DEL JUICIO: el
   ojo colosal en el cielo + el haz masivo de 7 capas.

---

# v6.55.0 — LA CURA DE LOS PICOS + EL RAYO CONCENTRADO II + LAS ALAS VECTORIALES — EL CHECKLIST

**Qué pasó** (la letra): «en ambas hay picos… y esos picos están en todas
las armas definitivas… es raro o una mala implementación de código» +
«no uses texturas originales del rayo de fusión, cambia su nombre a rayo
concentrado 2… el sonido debes recrearlo» + «nuestro proyecto no puede
tener ninguna referencia externa de ningún tipo» + «borra la bolsa de
investigación… deja la bolsa de definitiva y la bolsa de alas» + «las
alas… todas son rectángulos, crea formas vectoriales: plumas, cuchillas».
LA CURA: el diagnóstico por SIMULACIÓN numpy del pipeline completo (la
causa raíz de los picos demostrada), el arma renombrada 100 % casa
(shader propio + sonido sintetizado), las alas de tiras vectoriales y
la purga completa de lo externo.

1. **LA PRUEBA DE LOS PICOS (LA QUE IMPORTA)**: [ ] agarrar EL RAYO
   CONCENTRADO II y disparar contra un jefe: el haz con el BORDE LISO
   — el plasma hierve DENTRO pero la silueta ya NO tiene dientes de
   sierra; [ ] EL PRISMA: los 7 haces pequeños ondean SUAVES (ya no
   zigzaguean) y las chispas arden DENTRO del canal (no en el filo);
   [ ] EL PARTIDOR: la columna con los bordes limpios y las esquinas
   del perfil redondeadas (la saturación suave).
2. **EL RAYO CONCENTRADO II (antes la réplica)**: [ ] el NOMBRE nuevo
   en la bolsa de las definitivas (la 6ª arma); [ ] la carga: la boca
   de glows apilados + la AGUJA que apunta; [ ] el láser: 3500 px con
   el barrido cosenoidal, el kick cada 4 t, la música BAJA al 30 %;
   [ ] EL SONIDO NUEVO: el zumbido grave que late (¡el loop sintetizado
   — sin cortes al repetir!); [ ] el núcleo blanco SUAVE (el umbral ya
   no recorta fractales).
3. **LAS ALAS VECTORIALES**: [ ] equipar cualquier ala de la Bolsa de
   las Alas: las plumas tienen FORMA de pluma (raíz estrecha → vientre
   → PUNTA AFILADA) y el borde de fuga CÓNCAVO (la media luna) — ya NO
   son rectángulos; [ ] comparar DOS alas distintas (p.ej. Medianoche
   vs Plasma vs Orquídea): siluetas DIFERENTES (membranas anchas vs
   agujas vs pétalos); [ ] la punta se DESGARRA viva al aletear.
4. **LAS BOLSAS**: [ ] al entrar al mundo llegan: EL COFRE DEL ARSENAL
   (ahora con la Bolsa de las Alas DENTRO) + LA BOLSA DE LAS ALAS aparte
   + LA BOLSA DE LAS DEFINITIVAS aparte; [ ] la Bolsa de la Investigación
   YA NO EXISTE (ni en el cofre ni aparte).
5. **LA PURGA**: [ ] el mod carga limpio en el mundo (nada del material
   retirado); [ ] el Cofre del Arsenal despliega sus bolsas sin la de
   investigación.

---

# v6.54.7 — LA TRANSCENDENCIA VISUAL — EL CHECKLIST

**Qué pasó** (la letra): las armas seguían «a nivel principiante» para
ser ataques definitivos — «el prisma primordial no le llega ni a los
pies al rayo de fusión o a otros jefes con rayos destructores», y en
la Ira del Cielo «¿para qué fabricar óvalos planos como nubes si
tenemos las librerías de brumas?». LA CURA: el shader del rayo de
fusión DISECCIONADO en su fuente (4 texturas multiplicadas) y
reconstruido en CPU como LA PILA DE SEIS PASADAS TEXTURIZADAS + 10
técnicas nuevas de la investigación de los rayos destructores de los
mods populares + la librería de brumas donde correspondía.

1. **LO PRIMERO — EL PRISMA ES OTRO ARMA**: [ ] agarrar EL PRISMA y
   cargarlo: la AGUJA del telégrafo LATA acelerando con los CORREDORES
   recorriendo el canal; al encender: EL CHOMP elástico (se pasa ×1.14
   y vuelve muelleando) + flash + Kick.
2. **EL HAZ CENTRAL VIVO**: [ ] la CARNE DE RUIDO HIRVIENDO dentro del
   haz (dos ruidos cruzándose — ya NO es una línea con glow); [ ] el
   COLOR QUE VIAJA a lo largo del canal; [ ] BARRER el ratón: el haz SE
   REZAGA Y LATIGUEA (la espina verlet) y deja POSIMÁGENES del barrido;
   [ ] el FLICKER: nunca late igual dos veces; [ ] los bordes con el
   filo ROJO/CIAN (aberración); [ ] los ARCOS que trepan y se escapan
   del canal; [ ] LA PUNTA ARDE (nova a 12 Hz + aro + chispas con
   gravedad) y los PINCHOS DE DIFRACCIÓN alineados al haz en foco y
   punta; [ ] EL IRIS: los dos aros girando en contra que SE ABREN con
   la carga.
3. **LOS SIETE HACES**: [ ] cada haz de color ahora lleva SU PROPIA
   CARNE fluyendo (aura de su color + núcleo en banda).
4. **EL PARTIDOR DE MUNDOS**: [ ] la aguja del telegrafo ahora es de
   CARNE con corredores; [ ] la columna HIERVE HACIA ARRIBA (el calor
   de la herida) con aberración; [ ] a los 8 t EL RECHINIDO: flash,
   Kick y la cuarta onda + ESCOMBROS girando con gravedad; [ ] EL HUMO
   DE LA HERIDA sube de la base (bruma de verdad); [ ] las grietas con
   sus ECOS espectrales.
5. **EL RÉQUIEM ESTELAR — RÍOS DE BRONCE**: [ ] el ruido FLUYE ALREDEDOR
   del anillo (el bronce corre) SIN COSTURA (el círculo sigue cerrado);
   [ ] LOS PÉTALOS mordiendo al frente del anillo; [ ] antes de cada
   tañido la IMPLOSIÓN (chispas aspiradas al centro); [ ] al tañir, EL
   ALIENTO dorado (nube de bruma) respira; [ ] el cierre: ESTRELLAS de
   verdad cayendo con estela + el AURA QUE QUEDA SONANDO.
6. **LA IRA DEL CIELO — SIN ÓVALOS PLANOS**: [ ] el cielo cubierto con
   NUBES DE BRUMA de verdad (racimos orgánicos + bandas con paralaje);
   [ ] cada RELÁMPAGO AMBIENTAL ILUMINA las nubes cercanas; [ ] el
   anuncio: las CHISPAS CONVERGEN AL BLANCO antes del juicio; [ ] la
   caída envuelve su AURA DE CARNE; [ ] la columna con su pila + EL
   HUMO DEL JUICIO subiendo; [ ] el final: ESPIRAL TRIPLE + brasas.
7. **EL TESTAMENTO**: [ ] EL ALIENTO de tinta (puff violeta) bajo el
   libro; [ ] ARCOS saltando entre las runas; [ ] al dictar: LA ONDA
   expansiva + LA RÁFAGA de páginas volando; [ ] EL VÍNCULO ahora es
   una FAJA DE RUIDO con la carne corriendo del libro al arma.
8. **ESTABILIDAD**: [ ] ninguna de las cinco armas deja de dispararse
   jamás (el ciclo acotado + el failsafe de 1200 t siguen intactos);
   [ ] el juego no se cierra con ninguna (el contrato del lote se
   mantiene: cada PreDraw sale con el lote vivo).

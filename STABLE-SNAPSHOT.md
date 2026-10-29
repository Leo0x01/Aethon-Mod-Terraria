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

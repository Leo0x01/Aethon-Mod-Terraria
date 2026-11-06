# SNAPSHOT ESTABLE — v6.104 — EL AURA DE JUICIO + EL TENTÁCULO LISO + LA COREOGRAFÍA NUEVA

> **PUBLICADA — release 436248609 + CDN verificado byte a byte (md5 f74ae36c501b39e4aaf34463d8bd5716)** — .tmod v6.104 (11.515.390 B, 512 entradas, EOF exacto) — **EL PUERTO FIEL DEL WARBANNER**: el Aura de Juicio con la mecánica EXACTA del Warbanner of the Righteous (campo 720 px · daño ×0.85..×1.15 por cercanía · marca de quemadura con intensidad ×1→×3 y stacks 0..300 · golpes 2..19 ticks de 15 × mejor clase con 50 AP · Llamas del Juicio 30 t · repulsión de los pegados · 5 capas de bloom Goldenrod↔Orange + destellos + luciérnagas · el medidor Fuerza del Estandarte) con arte 100% de la casa · LA BOLSA DE LAS AURAS (bolsa 27: Aura de Juicio + Estandarte + las 5 Formas — el material de investigación de auras) · EL TENTÁCULO LISO (la sierra dorsal MUERE; piel lisa con brillo húmedo + ojos y colmillos DE LA FORJA que giran hacia la presa) · LAS ETIQUETAS SIN DESAJUSTE (el consagrado con UN gris humo en todo; el grimorio respirando EN VIVO con su peldaño; el Supremo respira con su traje) · LA COREOGRAFÍA NUEVA DEL DIOS (la Mirada COMÚN en P1 · el Renglón más separado + la MEZCLA 50% en sentido horario · LA MIRADA DEL CIELO 6V+6H con diagonal y relanzamiento en P2 · EL RAYO DE PRUEBA C con su LEY DE LOS DOS ANTERIORES en P3 · la danza reescrita) · LA MUERTE DE LAS TEXTURAS PLANAS (columnas, pilar, danza, lanza, los 44 rayos y la rejilla del fusionado TODO HazLib; los proyectiles pequeños con destellos de 4 puntas) · EL CALOR MUERTO en el arma-C — oráculo 0/0 (445 .cs), -build REAL 0/0, hjson ×3 idénticos, headless 0 EXCEPCIONES.

## LA LETRA (el usuario)

«veo que no entiendes el concepto del estandarte, investiga bien como funciona y como reacciona visualmente los enemigos, copia exactamente el codigo de Warbanner of the Righteous y sus textura, efectos vfx tecnica todo, en un nuevo item llamado Aura de Juicio y daselo al jugador, ademas todas estas cosas nuevas ponlas en una nueva bolsa, esto sera material de investigacion de auras / mejora aun mas las texturas de la sombra de la pagina 5, recuerda que es un tentaculo liso, con ojos y colmillos, si lo necesitas siempre puedes crear los ojos y los colmillos con la forja o el taller / en el caso de las etiquetas del grimorio y la etiqueta de consagrado, el color con los efectos a veces no coincide el de la barra de acceso rapido con el que se muestra en el inventario / muchos de sus ataques son texuras planas, solo un rectangulo y estan mal implementadas… cambia toda la coreagrafia de los jefes y mejora sus ataques… y al arma de rayo de prueba C quitale la turbulencia esa que es como calor»

## PARA VERIFICAR EN JUEGO (la checklist v6.104)

1. **EL AURA DE JUICIO**: llega DIRECTO al entrar al mundo (y en La Bolsa de las Auras, dentro del Cofre del Arsenal). Equípalo: el estandarte ondea a tu espalda (Balloon) y el aura dorada de 5 capas respira a tu alrededor. Acércale un enemigo: se MARCA y arde con las Llamas del Juicio (ícono dorado), la luz sobre él crece con la cercanía, las chispas salen EXPULSADAS de su flanco, y al acercarse MÁS se frena (y si corre a abrazarte, se VUELVE). El medidor «Fuerza del Estandarte» (abajo-izquierda) muestra −15%..+15% en tiempo real: pega al lado del enemigo y tu daño ARDE al máximo.
2. **LA SOMBRA DE LA PÁGINA 5**: el tentáculo sale LISO (sin sierra en el lomo), con el BRILLO HÚMEDO resbalando por la piel; los once OJOS DETALLADOS (iris carmesí→violeta, rendija vertical) giran hacia la presa y parpadean a ritmos distintos; la fauce muerde con COLMILLOS DE MARFIL DETALLADOS (encía violeta, punta pálida).
3. **LAS ETIQUETAS**: corrompe un arma en el altar — el nombre «(Consagrado)» se ve con EL MISMO gris humo en el tooltip del inventario Y en la barra rápida (inventario cerrado). Con el grimorio: el color del peldaño respira IGUAL en ambos lados.
4. **EL JEFE**: fase 1 — la MIRADA sale COMÚN (el rayo fucsia barriendo); fase 2 — la REJILLA DEL CIELO (6 verticales + 6 horizontales cayendo, arden, giran 45° derivando en diagonal y SE RELANZAN) y el RENGLÓN FINAL con los haces separados (la mitad de las veces con la MIRADA girando en el MISMO sentido); fase 3 — tras 2 de esos ataques, el RAYO DE PRUEBA C sale SIEMPRE (los 6 soles cayendo + el conducto con doble glow y aguja). LAS COLUMNAS Y RAYOS YA NO SON RECTÁNGULOS: hierven como los haces de las armas.
5. **EL ARMA-C**: el Rayo de Prueba C dispara LIMPIO — sin distorsión de calor en el aire.

---

# v6.101 — LA COPIA FIEL — EL ESTANDARTE 1:1 CON LA QUEMADURA DEL VACÍO EXTERIOR + LA SOMBRA DE LA PÁGINA V + EL ALTAR NUEVO

> **PUBLICADA — release 435948265 + CDN verificado byte a byte (md5 0525ee1a11d431d683ac3f0ab2e3b2b9)** — .tmod v6.101 (md5 `0525ee1a11d431d683ac3f0ab2e3b2b9`, 11.292.554 B, 496 entradas) — **LA COPIA FIEL**: el estandarte ya no es la «bendición dorada» de la v6.100 — ES EL EFECTO DEL ORIGINAL: radio pequeño (300 px), un aura que QUEMA con llamas negras y FRENA a los enemigos cuanto más se acercan, cada enemigo ardiendo CADA VEZ MÁS con su CAPA DE RESISTENCIA de meteoro entrando a la atmósfera (manto de partículas + cresta de choque + estelas de ablación, todo morado y negro) — y mientras un enemigo arde, el portador cobra el VACÍO EXTERIOR (+10% ataque, +10 defensa, −15% maná). **LA INVESTIGACIÓN DEL GRIMORIO**: el debuff QuemaduraVacioExterior + el buff VacioExterior + el motor completo (VacioGlobalNPC: DoT, freno y capa) viven del LADO DEL DEBUFF — cualquier fuente que aplique la quemadura hereda el paquete entero. **ADEMÁS**: la cura del client.log (el End() huérfano de La Última Palabra, línea 422 — la excepción silenciosa del Acto I), LA SOMBRA DE LA PÁGINA V (el tentáculo del abismo mordaz: once ojos rasgados que parpadean cada uno a su ritmo, la sierra doble de colmillos y LA FAUCE que jadea y tritura) y EL ALTAR NUEVO (6 marcos: el vórtice GIRANDO 60° por marco, las velas en contrafase, las runas pulsando, la chispa subiendo — QA VLM 8.5/10 «Ship it») — oráculo 0/0 (437 .cs), **-build REAL 0/0**, guardián **TODO VIVE ✔** (415 clases), .tmod 496 entradas EOF exacto, hjson ×3 paquete==fuente byte a byte, **headless 0 EXCEPCIONES** (Finalizing Content v6.101 → Server started).

## LA LETRA (el usuario)

«lo que tienes que hacer es copiarlo 1 a 1, ya que lo mas importante es el efecto, un radio de accion pequeño, un aura que quema a los enemigos y los vuelve mas lento cuanto mas se acercan al jugador, cada enemigo que entre al radio de efecto del estandarte se quema y visualmente se nota como cada vez se quema mas y como varias particulas crean algo asi como una capa de resistencia igual a la de los meteoritos entrando a la atmosfera y todo con energia morada y negra»

## EL EFECTO COPIADO (qué hace el estandarte AHORA)

1. **EL RADIO PEQUEÑO** — 300 px (18-19 tiles): la v6.100 abarcaba 720 — el original es un aura ÍNTIMA.
2. **LA QUEMADURA DEL VACÍO EXTERIOR** — cada impío que entra al radio arde con el debuff de LLAMAS NEGRAS (timer 180 del original: sigue ardiendo ya fuera); el daño escala ×1→×3 por cercanía (la técnica Remap del original) × la rampa del tiempo (cada segundo ardiendo lo enciende, hasta ×1.5) × el despertar del dueño.
3. **LA LENTITUD** — cuanto más se acercan, más lento caminan (×1 en el borde → ×0.55 pegado al cuerpo); vive en PostAI: pisa DESPUÉS de la IA y frena a caminantes Y voladores.
4. **EL PAGO AL DUEÑO** — mientras CUALQUIER enemigo arda con su quemadura, el portador cobra VACÍO EXTERIOR: +10% a TODO el ataque, +10 defensa, −15% maná — y él también «arde» (llamas negras de violeta lamiéndolo).
5. **LA CAPA DE RESISTENCIA** — el frente de ablación del meteoro: MANTO de 16 partículas alrededor del cuerpo (más brillante en la cara que avanza), CRESTA DE CHOQUE comprimida delante y ESTELAS que se desprenden hacia atrás — TODO morado y negro, creciendo con la rampa visual (2 s ardiendo = intensidad plena).

## PARA VERIFICAR EN JUEGO (la checklist)

1. **Actualiza a la v6.101** (el `.tmod` del release → carpeta Mods) — verifica en la lista de mods que diga **6.101**.
2. **EL ESTANDARTE**: equípalo y acércale un enemigo — arde con llamas negras (el ícono del debuff en su barra), CAMINA CADA VEZ MÁS LENTO al acercarse, y su capa de partículas moradas crece segundo a segundo; TU barra de buffs muestra VACÍO EXTERIOR mientras él arda.
3. **LA ÚLTIMA PALABRA**: úsala — el Acto I (la carga del libro) ya no lanza la excepción silenciosa del client.log.
4. **LA SOMBRA DE LA PÁGINA V**: llega DIRECTO al entrar al mundo — once ojos que parpadean a su ritmo, la sierra de colmillos, y LA FAUCE que se abre ENORME sobre la presa y tritura al morder.
5. **EL ALTAR**: colócalo — el vórtice GIRA, las dos velas bailan en contrafase, las runas pulsan y la chispa sube (6 marcos, ciclo de ~0.7 s).

## LA LECCIÓN (sellada en el código)

El efecto de un ítem de investigación vive DEL LADO DEL EFECTO (el debuff), no del arma que lo aplica: QuemaduraVacioExterior + VacioGlobalNPC forman el paquete completo (DoT, freno, capa, pago al dueño) — el estandarte solo lo APLICA. Cuando el grimorio llegue a sus niveles altos, aplicará la misma quemadura a todos los enemigos y heredará TODO sin escribir una línea nueva.

---

# v6.100.2 — LA ENTREGA COMPLETA — LA SOMBRA DE LA PÁGINA IV RE-FORJADA YA LLEGA DIRECTO

> **BUILD VERIFICADA — PENDIENTE PUBLICAR** — .tmod v6.100.2 (md5 `1d75b914c4117a237b09ece4c9b42747`, 11.286.228 B, 493 entradas) — **LA ENTREGA**: la v6.100 re-forjó completa la Sombra de la Página IV (la garganta del vórtice como boca girando con el rumbo, velo de ruido, anillo de tinta doble, icono re-forjado — 617 líneas de proyectil) pero solo la dejaba en la Bolsa de las Sombras. La regla de la casa es la regla: **lo re-forjado ES lo nuevo, y lo nuevo llega DIRECTO** — una sola línea en `ShardPlayer.EntregarRegaloDePruebas` (justo tras la Página II), la lista queda en **17 ítems directos** y TODA la creación nueva de la casa queda cubierta. **QUIRÚRGICO**: ni una coma de la interfaz de usuario se tocó — la cura v6.100.1 de la interfaz rota viaja INTACTA dentro de esta versión — oráculo 0/0 (423 .cs), **-build REAL 0/0**, **DLL DECOMPILADA** (la entrega lista las 17 con `SombraDeLaPagina4` tras `SombraDeLaPagina2` · GetItemDrawFrame VIVE · `MarcoVivo` x0 · `itemFrame` x0), guardián **TODO VIVE ✔** (411 clases), hjson es-ES == es-MX en claves y valores (1556 planas c/u) con 0 claves del español faltando en el en-US, headless **0 excepciones** (Selected AethonMod 6.100.2 → Mod Load Completed).

## LA LETRA (el usuario)

«asegurate de que le entregas al jugador las cosas nuevas, armas o cualquier cosa nueva que sea creada»

## LA AUDITORÍA DE LA ENTREGA (qué ya llegaba y qué faltaba)

**Ya llegaba DIRECTO (16 ítems):** las 5 armas nuevas de la v6.100 (Sello de Tinta · Parpadeo del Eterno · Corona de Plumas · Derrame de Tinta · La Última Palabra) + los 3 supervivientes de la purga (Coro de los Profundos · Pluma del Escriba Santo · Vástago del Eterno) + el Estandarte del Juicio + los históricos (Formas Ascendidas 2/3/4 · Aethon Menor · Halo Arcoíris · Brillo Solar · Sombra de la Página II).

**Faltaba (1):** LA SOMBRA DE LA PÁGINA IV — re-forjada COMPLETA en la v6.100 (el proyectil entero reescrito: la GARGANTA DEL VÓRTICE como boca girando con el rumbo del tentáculo, el VELO DE RUIDO sobre el cuerpo, la masa viva con sus ojos clavados en la presa, el ANILLO DE TINTA doble violeta+teal y el icono re-forjado) — pero solo vivía en la Bolsa de las Sombras. La bolsa sigue como vía de repuesto; lo nuevo llega DIRECTO.

## PARA VERIFICAR EN JUEGO (la checklist de la entrega)

1. **Actualiza a la v6.100.2** (VÍA 1 del COMPILACION.md: el `.tmod` del release → carpeta Mods) — verifica en la lista de mods que diga **6.100.2**.
2. **Entra a un mundo nuevo o con el inventario sin ella**: LA SOMBRA DE LA PÁGINA IV llega SOLA (con el mensaje dorado del regalo de pruebas) — igual que sus hermanas de creación reciente.
3. **La cura de la interfaz sigue viva**: entra con el estandarte equipado (o en la mano del cursor) — nada desaparece, no hay pantalla negra, el log queda limpio.
4. **El resto de la v6.100 intacto**: el aura del juicio, el altar asentado con sus dos velas, las cinco armas nuevas.

## LA LECCIÓN (sellada en el código)

La entrega directa no distingue entre «nacido» y «re-forjado»: si una versión le puso cuerpo nuevo a un ítem (proyectil reescrito + icono nuevo), ese ítem ES nuevo a efectos de la entrega — `EntregarRegaloDePruebas` es la lista viva de TODO lo que el jugador debe tener sin buscarlo.

---

# v6.100.1 — LA CURA DE LA INTERFAZ ROTA — EL ESTANDARTE YA NO REVIENTA LOS SLOTS (INCLUIDA EN LA v6.100.2)

> La cura viaja INTACTA dentro de la v6.100.2 — documentada aquí tal cual se forjó.

## EL BUG (la letra del usuario)

«hay un error muy raro, creo que con el estandarte, este error buguea por completo la interfaz de usuario haciendo que algunas cosas desaparezcan o la pantalla se vuelva negra, es muy raro»

### LA AUTOPSIA DEL client.log (5/11, 16:05→16:07)

- 16:05:30 — carga **AethonMod 6.100** (tras un reload desde la 6.99).
- 16:06:22 — entra al mundo con el estandarte equipado.
- 16:06:32→16:06:51 — **5 «Excepción silenciosa»** (tML las deduplica): `System.IndexOutOfRangeException` en `EstandarteDelJuicioItem.PreDrawInInventory` línea 92, llamadas desde `ItemSlot.Draw` por TRES vías: el inventario, el slot de accesorio (`AccessorySlotLoader.DrawRedirect`) y **el objeto en la mano del cursor** (`DrawInterface_38_MouseCarriedObject`).
- 16:07:08 — guarda y renuncia (la interfaz inservible).

**POR QUÉ SE ROMPÍA LA INTERFAZ ENTERA**: la excepción saltaba DENTRO del bucle de dibujado de las capas de interfaz (`Main.DrawInterface` → capa 27 del inventario) — tML la traga («silenciosa»), pero las capas que venían DETRÁS no se dibujan ese frame → elementos que desaparecen; según qué slot se dibujaba cuando reventaba, el daño cambiaba (a veces la pantalla entera en negro). CADA FRAME, mientras el ítem era visible.

### LA RAÍZ (verificada en el fuente de tModLoader, rama 1.4.4)

- `Main.itemFrame` es un array **VANILLA** (declarado `int[401]`, dimensionado por el juego base a ~5456) que tML **NUNCA redimensiona** — `ItemLoader.ResizeArrays` solo redimensiona `Main.itemAnimations` (el sistema de `Main.RegisterItemAnimation`… que el estandarte YA usaba para su animación).
- La v6.100 leía `Main.itemFrame[Type]` en `MarcoVivo` para el «marco vivo» — con Calamity y compañía, el Type del estandarte queda MUY por encima del tope vanilla → **fuera de rango**.

### LA CURA QUIRÚRGICA

- `PreDrawInInventory` dibuja el PARÁMETRO `frame` del hook (ItemSlot.Draw lo computa con `Main.itemAnimations` — el sistema que tML SÍ dimensiona para mods; escala 0.62 y el ajuste de 2 px INTACTOS).
- `PreDrawInWorld` usa `Main.GetItemDrawFrame` (el helper oficial — el mismo del Grimorio Hambriento).
- `MarcoVivo` (el lector del array venenoso) MUERE.

## LA LECCIÓN DE LA v6.100.1 (sellada en el código)

El marco animado de un ítem NUNCA se lee de `Main.itemFrame` (solo ítems vanilla): **llega en el parámetro `frame` del hook `PreDrawInInventory`** (ItemSlot.Draw lo computa con `Main.itemAnimations`) **o por `Main.GetItemDrawFrame(type, out tex, out frame)`** — el helper oficial, el mismo que el Grimorio Hambriento ya usaba. La casa ya conocía el patrón correcto; la v6.100 lo reinventó mal.

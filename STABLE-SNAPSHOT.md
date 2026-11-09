# SNAPSHOT ESTABLE — v6.120 — EL LLANO DE OBSIDIANA + EL PILAR DE LA SALIDA + LAS TRES BANDAS

> ⏳ **PENDIENTE DE PUBLICACIÓN** — .tmod v6.120 listo y verificado (15.943.130 B, 499 entradas, EOF exacto, md5 5bb475d8279bd092e6b3f18deec03514).

## LA LETRA (el usuario)

«una vez dentro del mundo interior no puedo salir, necesitas crear un pilar tipo prisma altar que permita salir, este se creara usando las herramientas del mod, la forja y el taller, ademas el pilar debe aparecer ya colocado en el mundo interior, y el mundo interior debe ser plano, solo un mundo plano de obsidiana, el fondo deben ser capas de tentaculos en loop y al menos 3 capas, usa la forja y el taller para crearlo» + «crea una banda sonora exclusiva para el mundo interior, crea 3 banda sonoras que sonaran aleatoriamente, manten el contexto del mundo interior del grimorio del eterno, como crees que seria el mundo interior de una entidad cosmica» + «la forma eterna debe poder activarse y ocultarse con las opciones del juego» + «las esencias del eterno solo llegan hasta pilas de 999… hacer que puedan acumularse hasta 9999» + «vuelve a copiar el codigo de las alas runicas del eclipse para reparar las alas runicas del vacio, luego has una auditoria del codigo copiado, revisa porque tiene picos, para investigar mejor el porque, puedes investigar los hazes que tambien tenian picos y ahora no, puedes usar la misma tecnica para las alas runicas del vacio» + «asegurarte de que cuando se cumpla esto, solo se active siempre que se consagran 1000 esencias» + «ademas no toques nada que este funcionando correctamente, recuerda, si funciona no lo toques».

## LA AUTOPSIA DEL «NO PUEDO SALIR»

El prisma viejo NUNCA nacía: `GenerarInterior` colocaba `WorldGen.PlaceObject` ANTES de generar el terreno — sobre el mundo recién limpiado, sin NI UN tile sólido debajo — y `TileObject.CanPlace` (leído en el decompile del tML 2026.09.3.1) valida el `AnchorBottom` que Style2xX hereda: sin sólido debajo, `CanPlace` devuelve false y PlaceObject FALLA EN SILENCIO. El interior nacía SIN PUERTA: quien entraba, no salía. LA CURA ES DE ORDEN: el llano nace primero y EL PILAR se coloca DESPUÉS — el obsidiana ya es sólido bajo su base, el ancla pasa y la puerta NACE en cada visita, al lado del punto de aparición.

## LA OBRA (v6.120)

- **EL PILAR PRISMA** — forjado por LA FORJA y EL TALLER (`tools/pilar_prisma_v6120.py`, PIL determinista, EL OJO QA VLM en 3 pasadas: FIX → FIX → **SHIP 7/9/8/8**): la columna santuario de 3×5 tiles (pedimento de amatista con sus dos velas, fuste esbelto facetado con sus tres anillos rúnicos, capitel con el prisma triangular bebiendo el haz dorado y el abanico en colores de la casa). Clic-derecho → LA VENTANA del prisma («tipo prisma altar»): VOLVER AL MUNDO REAL / INVOCAR A AETHON. MinPick 9999: nadie talla su salida. YA COLOCADO en cada visita.
- **EL LLANO DE OBSIDIANA** — `GenerarInterior` reescrito: UNA LÍNEA RECTA de obsidiana de borde a borde, SOLO obsidiana (colinas, plaza, geoda y vetas MURIERON). El interior de una entidad cósmica como LA PÁGINA NEGRA INFINITA: la carne congelada de lo devorado, la vastedad como diseño, el latido (PulsoCorazon) respirando el cielo entero.
- **EL FONDO EN LOOP** — las TRES capas de tentáculos son BANDAS PERIÓDICAS (900/760/640 px, 4+3+2 tentáculos por periodo) envueltas por el MÓDULO del paralaje: camines donde camines, el cuerpo del dios se repite SIN COSTURA — un LOOP de verdad. El brillo de los titanes corre en el mismo loop; el polvo estelar, las brumas y el abrazo del sol siguen vivos.
- **LAS TRES BANDAS EXCLUSIVAS** — 100% síntesis de la casa: «EL SUEÑO DEL VACÍO» (58 BPM, el dios dormido), «LA GARGANTA DE PÁGINAS» (92 BPM, el tragadero susurrante) y «EL CORAZÓN COLOSAL» (120 BPM, el órgano que todo lo mueve). LA MONEDA: una al azar al entrar y re-tiro cada ~2,5 min SIN repetir la inmediata — la estancia larga oye dos o tres; en el festín manda la oleada (BossHigh) como siempre.
- **LAS ALAS DEL VACÍO = LA COPIA EXACTA DEL ECLIPSE** — el repertorio estructural BYTE A BYTE (ruido, plumas, puntas, tempo, filos, núcleo), solo cambia la paleta del vacío y su anillo rúnico violeta. LA AUDITORÍA DE LOS PICOS: la sierra vivía en el CAMINO EXTRA (membrana + borde maestro + niebla inset + el balance de la vela), no en la geometría — la misma lección del FILO DE HazLib que curó los haces (borde a CERO EXACTO + smoothstep antes del borde + soft-add): se respeta VOLVIENDO al camino único del eclipse. LAS RUNAS vuelven a las puntas (nodo 8, escala completa 1.45+0.35·n) y las migas del vacío siguen cayendo. Tooltip ×3 reescrito.
- **LA FORMA ETERNA A LA VISTA** — el interruptor «Mostrar la Forma Eterna» del menú de configuración del mod (default ON) + el OJO de vanilla del hueco funcional la esconden y la muestran; SOLO muere el teatro (los stats y el aura del vacío son de juego). El escaneo de CosmeticPlayer respeta ambos.
- **LAS ESENCIAS A 9999** — las mil de la puerta caben en UNA pila. El drop (criaturas 0,1% · jefes 90/5/1), el tope del festín del portador y la lógica del altar VERIFICADOS SIN CAMBIO: la cuenta solo sube al consagrar, la puerta solo se abre a las MIL consagradas, el botón solo existe con la puerta abierta, la cuenta vive con el mundo.
- **LA CURA QUE SOLO EL HEADLESS VIO** — una línea huérfana en el en-US la toleró el -build y la cazó el servidor headless («malformed localization file» — el mod NO cargaba): extirpada, los TRES hjson parseados con el parser real y todo el gate re-corrido. El headless es OBLIGATORIO.

## VERIFICACIÓN (la cadena de la casa)

- oráculo verify.csproj: **0 errores / 0 warnings** (448 .cs contra el tML 2026.09.3.1 real).
- **-build REAL 0/0**: 499 entradas, 15.943.130 B, EOF exacto, 499/499 blobs inflan.
- **GUARDIÁN TODO VIVE ✔** (el pilar con su máscara en disco y en el .tmod).
- **AUDIT v6.120 TODO VERIFICADO ✔** — 100 checks: firmas VIVAS (PilarPrismaTile · LlanoY · EscenaInteriorGrimorio · MostrarFormaEterna · hideVisibleAccessory…), literales UTF-16 de las bandas, la tira 54×516 y su máscara 54×86, las 3 ogg, el repertorio del vacío == eclipse, el llano SOLO obsidiana, el pilar DESPUÉS del terreno, el drop intacto, la puerta solo a las mil, las esencias a 9999, keysets ×3 idénticos.
- **EL OJO del pilar: SHIP 7/9/8/8** (tres pasadas de refina, el ciclo de la casa).
- **headless 0 EXCEPCIONES** («Sandboxing: Aethon, the Eternal Grimoire v6.120» → Adding Recipes → Choose World).

## PARA VERIFICAR EN EL JUEGO

- [ ] **LA SALIDA EXISTE**: entra al mundo interior (el ítem o el botón del altar) — EL PILAR PRISMA ya está colocado al lado de donde apareces, con su luz de faro. Clic derecho → VOLVER AL MUNDO REAL (y INVOCAR A AETHON sigue vivo).
- [ ] **EL LLANO**: el interior es UNA planicie de obsidiana de borde a borde — sin colinas, sin plaza, sin geoda: solo el llano, su pilar, el eclipse y el latido.
- [ ] **EL LOOP DEL FONDO**: camina hacia un lado LARGO rato — las capas de tentáculos se repiten idénticas sin costura, los ojos siguen mirándote, los titanes verlet laten.
- [ ] **LAS TRES BANDAS**: entra varias veces (o quédate ~5 min) — suena EL SUEÑO DEL VACÍO, LA GARGANTA DE PÁGINAS o EL CORAZÓN COLOSAL, al azar, sin repetir la inmediata; en el festín manda la oleada.
- [ ] **LAS ALAS DEL VACÍO**: vístelas — el abanico del eclipse vestido de violeta, SIN PICOS, con las runas violetas EN LAS PUNTAS y las migas cayendo.
- [ ] **LA FORMA ETERNA**: «Opciones del juego → Configuración del mod → Mostrar la Forma Eterna» — apágala y el panteón visual se apaga (stats y aura intactos); enciéndela y vuelve. El ojo del hueco funcional también la esconde.
- [ ] **LAS ESENCIAS**: una pila llega a 9999 — consagra en el altar y la cuenta viva (X/1000) sube; a las MIL consagradas el botón IR AL MUNDO INTERIOR aparece (el drop sigue igual: 0,1% criaturas, jefes 1-10).
- [ ] **TODO LO DEMÁS INTACTO** (si funciona, no se toca): la devoración, el ítem que no se gasta, el ciclo de oleadas, las brumas, el altar de la consagración y sus secciones.

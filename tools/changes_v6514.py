#!/usr/bin/env python3
# v6.51.4 — la entrada del CHANGES.md (patrón de la casa: se inserta al inicio).
ENTRADA = '''# v6.51.4 — EL TYPO DEL VAGABUNDO + LAS BRUMAS DE VERDAD + LOS SEIS DIGNOS

**La letra del usuario**: «hay un error en la pantalla de seleccion de personajes // los jefes no tienen sprite en el bestiario // el grimorio dormido ya esta bien en el bestiario // el nombre "Grimorio del Eterno" en el bestiario: el mismo brillo que LEGENDARIO SUPREMO pero con mas brillo y particulas mas pequeñas // los agujeros negros runicos siguen sin nacer donde deben nacer, se hace la animacion pero aparecen en otro lugar // el BrilloSolar: el sprite en el inventario tiene un cuadrado de glow cortado muy feo, en el personaje se ve bien, arregla solo el inventario, y añadelo a la bolsa de comseticos // con respecto a las ultimas brumas que estas usando, las de la llegada y las del nacimiento de soles runicos y agujeros negros runicos, me pregunto, que brumas estas usando, teniamos ya una buena libreria de brumas completamente funcional que no ponen esos circulos planos... esos circulos planos estan mal, solo se debe cubrir con bruma, nada geometria plana // crea un arma nueva por cada una de estas ideas que diste y crea una nueva bolsa para darsela al jugador: LA BINARIA DIA/NOCHE (pareja dorado/violeta orbitando entre si; de dia golpea el dorado, de noche el violeta), LAS COMAS QUE MUERDEN (glifo rúnico viaja lento y recto; mantener clic = se queda quieto en el aire como coma; al soltar, TODAS las comas descargan latigazo hacia el enemigo más cercano), LA PÁGINA ARRANCADA (hoja girando como cuchilla, cara dorada y cara violeta; al clavarse flota un instante y la siguiente página la enciende), EL OJO QUE CAZA (mini-miradas que a mitad de camino abren el párpado y giran hacia la presa; impacto = parpadeo con flash)».

## A. EL ERROR DE LA SELECCIÓN DE PERSONAJE — EL TYPO DEL VAGABUNDO (cazado con el client.log)

· **EL SÍNTOMA**: «hay un error en la pantalla de selección de personajes».
· **LA CAUSA (el client.log del playtest, línea por línea)**: `ReLogic.Content.AssetLoadException: Asset could not be found: "Content\\Projectiles\\OjoEterne_Cerrado"` disparada desde `SeleccionGrimorioSistema.DibujarElLibro` — la manada del vagabundo (v6.51.3) pedía la textura del ojo cerrado con UN TYPO: `OjoEterne_Cerrado` sin la segunda "o". La textura real (`OjoEterno_Cerrado`) siempre existió — el vagabundo revientaba el dibujo del menú en cada parpadeo (excepción silenciosa: tML la traga, pero el libro del menú se dibuja roto y el log se llena).
· **LA CURA**: una letra. El asset pedido es ahora `AethonMod/Content/Projectiles/OjoEterno_Cerrado` (verificado a nivel binario: el typo NO viaja más en el DLL del .tmod; la ruta curada sí; la textura `.rawimg` empaquetada).

## B. LOS JEFES AL FIN TIENEN CARA — LAS POSES DEL BESTIARIO

· **EL SÍNTOMA**: «los jefes no tienen sprite en el bestiario».
· **LA CAUSA**: los cuatro jefes son 100% arte de código (sus PreDraw miden con `Main.screenPosition` del MUNDO) — el bestiario dibuja al dummy en su caja y el arte caía donde la última partida lo dejó: casillas VACÍAS. Y de propina, la danza de lotes (cerrar → aditivo → reabrir) devolvía un lote con parámetros del mundo en mitad de la UI.
· **LA CURA VANILLA (sin tocar a los jefes)**: `NPCID.Sets.NPCBestiaryDrawOffset[Type].CustomTexturePath` — el icono del bestiario dibuja una **POSE pre-renderizada** (el arte de cada jefe destilado en miniatura 112×112 por `tools/gen_poses_bestiario_v6514.py`: el SOL de tres anillos del Eterno, el ECLIPSE vivo del Fusionado, el sol MORIBUNDO del Oscuro, el LOBO-ESTRELLA del Cazador Astral) y JAMÁS llama a DrawNPCDirect — el PreDraw ya no corre en el catálogo. Cinturón de seguridad: `if (NPC.IsABestiaryIconDummy) return false;` en cada PreDraw. Los jefes EN EL MUNDO siguen siendo exactamente los que eran (cero cambios en su arte real).

## C. EL GRIMORIO DORMIDO — CONFIRMADO

«el grimorio dormido ya está bien en el bestiario» — confirmado por el usuario, no se toca («nunca toques lo que ya funciona»).

## D. EL NOMBRE DEL GRIMORIO ARDE — MÁS BRILLO, PARTÍCULAS MÁS CHICAS

· **LA LETRA**: «el mismo brillo que LEGENDARIO SUPREMO pero con más brillo y sus partículas más pequeñas».
· **LA CURA**: `NombreEternoGlobalItem` (GlobalItem nuevo, solo para el Grimorio del Eterno): el hook `PostDrawTooltipLine` corre DESPUÉS de que tML dibuja la línea del nombre (con su X/Y exactos) — ahí viven y mueren NUEVE destellos de 4 PUNTAS (la firma del rótulo ✦ LEGENDARIO SUPREMO ✦: dos cápsulas de blanco incandescente cruzadas + el corazón dorado), repartidos a lo largo del nombre con su deriva fija (deterministas, sin RNG), **MÁS BRILLANTES** (alfa 0.95 — el rótulo usaba 0.6-0.8) y **MÁS CHICOS** (3.4-5.4 px — el rótulo usaba 7-11). La primera línea de la ventana de info del Grimorio del Eterno VIVE.

## E. EL NACIMIENTO DEL AGUJERO — LA CURA DE LA CURA (nace DONDE la animación dijo)

· **EL SÍNTOMA**: «siguen sin nacer donde deben nacer, se hace la animación pero aparecen en otro lugar».
· **LA AUTOPSIA DE LA .51.3**: el viaje de la semilla funcionó, pero la APERTURA clavaba el agujero en el `Center` de la presa (DENTRO de su sprite — «nace directamente en el enemigo» otra vez) y la semilla era tan tenue que el vuelo casi no se veía.
· **LA CURA DE LA .51.4**: la semilla es ahora un NÚCLEO DE VACÍO CON BRUMA (BrumaFX — humo de verdad, nada de discos) que RESPLANDECE con luz propia (un foco violeta cruzando el bioma, del tamaño de lo que viene), FRENA al acercarse (el nacimiento se anuncia: a menos de 120 px el empuje se apaga suave, el tope de velocidad cae de 15,5 a 6) y para a la DISTANCIA LIMPIA del BORDE de la presa (`34 + clamp(ancho/2)` px — nunca dentro del cuerpo). El agujero SE ABRE EXACTAMENTE DONDE LA SEMILLA TERMINÓ SU VUELO: donde la animación lo dijo, ahí nace — imposible leerlo mal. Los 10 agujeros rúnicos siguen intactos.

## F. EL BRILLO SOLAR — EL CUADRADO CORTADO MUERE + VIAJA EN LA BOLSA

· **EL SÍNTOMA**: «el sprite en el inventario tiene un cuadrado de glow cortado muy feo; en el personaje se ve bien, arregla solo el inventario».
· **LA CAUSA**: el glow del icono sangraba fuera del frame 30×30 — el borde del canvas CORTABA el halo en línea recta (el «cuadrado»). 
· **LA CURA**: el icono regenerado (`tools/gen_icono_brillo_v6514.py`) — el destello vive ENTERO dentro del frame (contenido que respira con margen, cero píxeles tocando el borde). El halo EN EL PERSONAJE no se toca (ya estaba bien).
· **Y LA BOLSA**: `BrilloSolarItem` añadido a LA BOLSA DE LOS COSMÉTICOS («añádelo a la bolsa de cosméticos») — se entrega como sus hermanos cosméticos.

## G. LAS BRUMAS PLANAS — LA RESPUESTA Y LA CURA (solo bruma, cero geometría)

· **LA PREGUNTA**: «¿qué brumas estás usando? teníamos ya una buena librería de brumas completamente funcional que no ponen esos círculos planos… esos círculos planos están mal, solo se debe cubrir con bruma, nada de geometría plana».
· **LA RESPUESTA HONESTA**: los círculos NO eran una librería nueva — eran **SOFTGLOW** (el pincel de LUZ radial de VFXCore) usado como si fuera niebla: discos lisos y planos donde debía vivir BRUMA. La librería buena (la que viste funcionando: **BrumaFX**, los puffs de humo fBm que se desgarran de verdad — la que viste al Grimorio Dormido y a las fauces) ya estaba en la casa; las últimas llegadas y nacimientos simplemente NO la usaron.
· **LA PURGA TOTAL (todo lo que era disco plano ahora es bruma de la casa)**:
  - **LA LLEGADA** (LlegadaBrumaProjectile): la «cebolla redonda» de la .51.3 (TRES SoftGlow concéntricos) MUERE — el vacío central es ahora una NUBE MADRE GORDA (BrumaFX.Cloud, doble de puffs y alfa alta) anidada en el corazón + su hija violeta: la oscuridad la pone el HUMO.
  - **LA GESTACIÓN** (RayoConvergenteProjectile): la cuna del ritual RESPIRA niebla del color de lo que nace (ORO para el sol · VIOLETA para el agujero) que CRECE con la ceremonia — y el «vacío que oscurece» de los agujeros ya no es un disco liso: es UNA NUBE NEGRA-VIOLETA que se espesa con el ritual.
  - **EL NACIMIENTO**: `BrumaNacimientoFX` (proyectil nuevo) — la nube del COLOR de lo que nace (dorado para el sol, violeta para el agujero) que EXHALA, crece y se disuelve, con su aliento aditivo para la lectura nocturna. La usan el sol al echar a andar (más gorda cuanto más alto el tier — el GRAN SELLADO respira más) y la apertura del agujero.
  - **LA SEMILLA** (AgujeroEnViajeProjectile): el cuerpo de la semilla YA NO son discos — es UNA BOLA DE HUMO VIOLETA con su corazón de vacío y sus runas.
  - Los núcleos de LUZ que quedan (el corazón dorado del sol en gestación, el foco de la semilla) se ENCogieron (×2.4→×1.1): son la CHISPA dentro de la masa — la masa la pone la bruma.

## H. LOS SEIS DIGNOS — UN ARMA POR IDEA + SU BOLSA (22ª)

La letra: «crea un arma nueva por cada una de estas ideas que diste y crea una nueva bolsa para dársela al jugador». Las cuatro ideas presentadas + DOS hermanas del mismo folio (las seis de la presentación de la .51.3, una por idea) maduran en ARMAS COMPLETAS — cada una con su mecánica final de una pieza (nada de escaleras de nivel), sin maná, con receta de 5 madera (protocolo de pruebas), iconos pixel art de la casa (aprobados por el jurado visual 7/7):

1. **EL EQUINOCCIO BINARIO** (60 daño · la binaria día/noche): UN disparo = LA PAREJA — el disco dorado y el violeta ORBITÁNDOSE (radio 24 px, contrafase, 2.6 rad/s) con homing suave. El RELOJ DEL MUNDO decide quién guía: de DÍA el ORO va delante y el golpe es UN MINI ECLIPSE (estallido en área de 90 px + onda dorada); de NOCHE el VIOLETA guía, el disparo PERFORA 4 y cada mordida DRENA vida al dueño.
2. **LAS COMAS QUE MUERDEN** (48 daño · el glifo suspendido): la coma viaja lenta y recta ~26 ticks, FRENA y queda SUSPENDIDA en el aire (cabeceo + giro lento). Al SOLTAR el clic, TODAS las comas descargan EL LATIGAZO (embestida a 26 px/tick contra el enemigo más cercano, 1400 px de alcance, homing refrescado). La SEXTA coma en el mismo enemigo (ventana de 90 ticks) alinea LA FRASE… y **LA FRASE DETONA** (golpe ×3 + ocho glifos dorados en fila — «el libro escribe una frase y la frase muerde junta»).
3. **LA PÁGINA ARRANCADA** (85 daño · la hoja de dos caras): vuela GIRANDO como cuchilla (0.38 rad/tick, gravedad tenue) — una cara ORO y otra VIOLETA según la fase del giro. Al golpear SE CLAVA y queda FLOTANDO (180 ticks de mecha, el halo crece con la prisa)… y arde en EL VERSO (estallido en área de 110 px + seis glifos ascendentes). LA CADENA: al clavarse, si hay OTRA página clavada a menos de 200 px, LA MÁS CERCANA DETONA YA («la siguiente página la enciende — cadena de versos»).
4. **EL OJO QUE CAZA** (70 daño · la mirada del escuadrón): la mini-mirada (el arte aprobado del OjoEterno) viaja DORMIDA con homing medio; a los 40 ticks —o a 260 px de su presa— ABRE EL PÁRPADO: el iris (el del Grimorio Hambriento) se desliza y ROTA mirando EXACTAMENTE a su presa, y el tope de velocidad sube (12→16: el ojo fijó la mira). El impacto es EL PARPADEO: destello blanco con aro dorado cruzado + el chasquido del teletransporte de ojos.
5. **LA LÍNEA DE LECTURA** (55 daño · la frase cargada): mantén el clic para HOJEAR el libro (glifitos saltando en la boca del cañón, el vistazo de la frase creciendo); al SOLTAR, LA FRASE DISPARA — un RAYO de glifos dorados (58 px/tick, CABEZA violeta) que ATRAVIESA TODO en recta, cada enemigo UNA vez, daño ×1→×3 (0→2.5 s de hojeo) y la frase MÁS LARGA cuanto más hojeo.
6. **EL MARCAPÁGINAS** (40 daño · el dardo que marca): dardo-listón con homing suave que al golpear se CLAVA al enemigo (viaja con él, brillando 480 ticks). Al QUINTO marcador en el mismo blanco (ventana de un capítulo): **FIN DEL CAPÍTULO** — golpe ×4 y EL LIBRO SE CIERRA DE GOLPE encima (la portada oscura cayendo + el aro del portazo), tragándose TODOS los marcadores de esa página.

**LA BOLSA DE LOS PROYECTILES DIGNOS** (la 22ª — icono propio, el saco con la binaria estampada): entrega las SEIS y garantiza como sus hermanas (solo lo que falte, reabrible). Se entrega al entrar con el mod activo (TestingPlayer) junto a las demás.

**LOS SEIS DIGNOS + la bolsa + el BrilloSolar en la bolsa de cosméticos + las 4 poses del bestiario + la bruma de verdad + el typo muerto — TODO verificado**: oráculo 0 errores/0 warnings · build real 0/0 · .tmod 2.839.272 B (md5 158c7886f09521bac989eb2c6801495e): 406 entradas (395+11: 4 poses + 6 iconos + 1 bolsa), tabla+EOF+SHA1 exactos, DLL 1.460.736 B con los 10 símbolos nuevos y el typo AUSENTE (UTF-16 verificado), hjson es-MX/es-ES/en-US con las 7 claves ×3 · headless: Sandboxing+Finalizing v6.51.4, Server started, 0 excepciones.

'''

RUTA = 'CHANGES.md'
with open(RUTA, encoding='utf-8') as f:
    t = f.read()
if t.startswith('# v6.51.4'):
    print('La entrada ya existe'); raise SystemExit
with open(RUTA, 'w', encoding='utf-8', newline='\n') as f:
    f.write(ENTRADA + t)
print('CHANGES.md — entrada v6.51.4 insertada al inicio')

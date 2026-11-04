# v6.96 — LA CURA DEL HAMBRE MUERTA — EL CHECKLIST

> **PUBLICADA** — .tmod v6.96 FINAL (md5 `de75766479d33dfa094777c6d2fb1928`, 11.243.290 B) — oráculo 0 errores/0 warnings (415 .cs · 79 refs del tML 2026.07.3.0 real), **audit_v696 TODO VERIFICADO** (texturas de la casa VIVAS con dimensiones exactas + las copiadas AUSENTES · firmas v6.93/94/95/96 vivas en la DLL (UTF-8 y UTF-16) — incluidas `ArrojadoTirosSistema`, `MarcarPorRed`, `AvisarTiroDelPortador`, `HuellaDelTiro`, `LaHuellaVive` · 6 muertas ausentes · **0 caracteres chinos y 0 menciones IP en los 415 .cs + hjson** · hjson ×3 simétricos en árbol Y paquete · LICENSE propietaria dentro del .tmod), headless **Carga → Adding Recipes → Server started, 0 excepciones**.
>
> La entrega (v6.95+v6.96 juntas — la .95 quedó sin release): **EL SELLO DEL ARROJO CURADO** (la letra: «deje caer cosas para que el libro las devorara y no lo hace, el grimorio no se come nada» — la autopsia del tML: el tiro del jugador nace con contexto `ThrowItem` y el item del suelo es la instancia ORIGINAL del inventario; el clon que recibía `OnSpawn` moría reemplazado una instrucción después, así que la marca JAMÁS se ponía) + **EL ALTAR DICE «✦ No es apto»** + **LOS PARÉNTESIS (A)/(M) DE EJEMPLO MURIERON** + **EL DAÑO A TODO EL GRIMORIO CRECE +1% POR NIVEL** + el jefe aprendió las armas de la casa (Los Seis Renglones girando cada 60° · el Rayo Concentrado del Dios como definitivo · la Mirada del Dios como ataque medio raro) · la Mirada del Eterno con la paleta del libro · el dinero SIN sello (se devora directo, como las esencias) · el altar nuevo por el pipeline pixelart con LA FORJA (las tres herramientas unificadas).

## LA CURA DEL HAMBRE (v6.96 — el corazón de la letra)
1. **LA AUTOPSIA**: `Player.DropSelectedItem` viste el tiro con una fuente MISC de contexto `"ThrowItem"` — y el item que vive en el suelo es la INSTANCIA ORIGINAL del inventario (`Main.item[idx] = theItemWeDrop`): el clon que recibía `OnSpawn` moría reemplazado. El marcador viejo (mirando `EntitySource_DropAsItem`/padre-jugador) JAMÁS vio un solo plato: por eso el Eterno no comía NADA.
2. **LA CURA (tres dedos)**: (a) `OnSpawn` reconoce el contexto `"ThrowItem"` y anota la casilla del mundo con su HUELLA (tipo/pila/prefijo — el clon y el original son gemelos) · (b) `ArrojadoTirosSistema.PostUpdatePlayers` corre un instante después (el original ya reemplazó al clon) y marca al SUPERVIVIENTE — huella verificada: si la casilla cambió de dueño en el mismo tick, nadie come marcado por equivocación · (c) en MP, `MsgTiro=17` por la EcoRed avisa a la autoridad (el server busca el gemelo cerca del tirador).
3. **LAS CUATRO PUERTAS DE LA MARCA** (la letra: «solo come lo que el jugador suelta»): el nacimiento asistido (padre-jugador) · el arma arrojada que clava (solo si el proyectil salió de un jugador) · la bolsa llena (OverfullInventory) · el tiro de verdad (tecla T / arrastre — `"ThrowItem"`). Los drops de enemigos (Loot), cofres (ItemOpen) y el sistema (Misc de la casa) nacen SIN la marca: **el Eterno NI LOS PRUEBA**.
4. **EL DINERO SIN SELLO** (la letra: «el dinero no se consagra, simplemente se deja caer»): el altar lo rechaza («✦ No es apto»); cae delante del portador y el Eterno se traga la pila ENTERA al BANCO (retroactivo: los peldaños cobran de ahí hoy o mañana). El ✓ dorado del checklist lee `BancoMonedasDisponibles` (el banco vivo), no el campo muerto.
5. **EL LIBRO CONSAGRADO**: siempre devorable al soltarlo (la puerta del 20 manda: si ya está paga o el grimorio va al 20+, se queda en el suelo).

## LO DEMÁS DE LA LETRA (verificado en fuente y DLL)
- **EL ALTAR**: «✦ No es apto» para lo no consagrible — texto simple y bien escrito en los TRES idiomas.
- **LA LISTA DEL DESPERTAR**: los paréntesis (A)/(M) eran EJEMPLO de implementación — muertos; cada opción es UNA LÍNEA con SU sprite y SU nombre (las tres tintas intactas: verde cumplido · rojo falta · gris oscuro bloqueado).
- **EL DAÑO QUE CRECE**: la línea del hito muestra el ACUMULADO VIVO — **+1% por CADA nivel de Despertar** (`BonusDano`, lo mismo que el pipeline cobra en el clic y en los ojos); el paréntesis guarda el próximo hito del escuadrón (+10% del 20, +15% del 40…).
- **EL JEFE** (v6.95): Los Seis Renglones — 6 haces equidistantes cada 60° girando a su alrededor · el Rayo Concentrado del Dios como definitivo (la firma de la FURIA) · la Mirada del Dios como ataque medio raro (peso 1) — todo HazLib, cero-GC, el filo rgb=0.
- **LA MIRADA DEL ETERNO** (v6.95): la paleta del libro — espina fucsia del iris, velo morado, fogonazo de tinta azul-noche, dorado del broche.
- **LA NOTA FINAL** (v6.95): cortada en 6 líneas que fluyen abajo (la oración larguísima ya no estira la ventana).
- **EL ALTAR NUEVO** (v6.95): sprite por el pipeline pixelart (36×54) + LA FORJA (la pluma pxl + el molde sprite-gen + el alma pixelfx — las tres herramientas de los reels UNIDAS en un comando).

## EL PLAYTEST (la lista de la casa — verifica y REHACE)
(0) [ ] **LA DEVORACIÓN**: con el grimorio EN LA MANO, arroja una esencia — el Eterno SE LA COME al instante (la cura del sello).
(a) [ ] **EL ARROJO**: el botín de enemigos y cofres espera en el suelo SIN ser devorado; SOLO lo que ARROJAS TÚ se lo come.
(b) [ ] **EL DINERO**: arroja monedas — pila entera al banco, sin pasar por el altar; el altar las rechaza con «✦ No es apto».
(c) [ ] **EL LIBRO CONSAGRADO**: arrojado bajo el despertar 20 → devorado (la puerta queda paga); al 20+ se queda en el suelo.
(d) [ ] **LA LISTA**: abre la PROGRESIÓN — sin (A)/(M), una línea por opción con sprite y nombre, las tres tintas.
(e) [ ] **EL DAÑO**: sube un nivel de Despertar → la línea del hito muestra +1% más (acumulado: nivel 5 = +5%).
(f) [ ] **EL JEFE**: en su fight, los 6 renglones girando + el rayo concentrado definitivo + la mirada como ataque raro.

## PRÓXIMOS PASOS
- El playtest de arriba (la letra manda: verificar CADA punto en juego y rehacerlo hasta que funcione).
- EL RESTO DEL MOTOR DE PÍXEL (244 en el catálogo): el motor es de la casa — solo elegir.
- LOS CICLOS ANIMADOS del minigrimorio y Aethon desde una imagen base (el método del Vástago).

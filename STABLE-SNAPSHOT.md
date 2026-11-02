# v6.88 — LA GRAN SESIÓN DE LOS DIECISÉIS BUGS — EL CHECKLIST

> **PUBLICADA** — .tmod v6.88 (md5 `f8a7d50c8687156d6ad0ebb2574cf2ea`, 10.918.477 B) — 488 entradas, blobs 488/488 (30 stored planos), EOF EXACTO, la SEGUNDA música del festín dentro (2,06 MB), oráculo 0/0, -build REAL 0/0, headless 0 excepciones (Adding→Configuring→Finalizing→menú de mundos).

## LA LETRA (los dieciséis reportes del playtest, uno a uno)
1. **LA BRUMA EXPLICADA Y SEPARADA POR LIGAS**: la v6.87 lanzaba SIEMPRE la bruma del bastón — el usuario ordena: «el agujero negro runico con un solo anillo va junto a los agujeros negros de un solo anillo, el Ascendido va con los Ascendidos… el bruma normal solo debe lanzarse con los otros agujeros negros runicos normales de forma aleatoria; el bruma Ascendido, este solo debe lanzarse de forma aleatoria con los otros Ascendidos». HOY: el ritual lanza `BrumaBlackHole` (el del bastón, 1 anillo) con los NORMALES y `BrumaAscendido` (doble círculo rúnico) con los ASCENDIDOS (despertar 60+).
2. **LA VENTANA DE INFO DECÍA 50/50**: los tooltips vivían en la v6.51 (Sol 7, Agujeros 10, Supremo 45, Ira 50, «Despertar 50 ×1,25»). HOY: 15/30/90/100, el tope «100 de 100», el hito del escuadrón EN PORCENTAJE y VIVO, la cima de la Ira «Moon Lord 10/10 · Emperatriz 5/5 · Aethon 1/1».
3. **EL CALOR VIVÍA EN TODA LA PANTALLA MENOS EN EL ARMA**: la cápsula de la v6.85 (boca ×3,6 · aguja ×5 sobre un conducto de ~850 px) daba ~3.000 px — MÁS ANCHA QUE LA PANTALLA. HOY: la cápsula sigue la GEOMETRÍA REAL del conducto (×1,35/×1,9, el largo del dibujo) y el pintor respeta ×1,25·radio: el hervor vive en la franja del haz, y solo ahí.
4. **EL ALTAR SE DESLIGA DEL DESPERTAR**: la economía de puntos/canje del 30 % MUERE (AceptaRelleno/ValorEnPuntos/RegistrarOfrenda/ProgresoOfrendas/CreditoPuntosEsencias extintos de raíz). EL ALTAR ES EL CORRUPTOR: slot de arma limpia + botón CORROMPER; el nombre viste «(Corrupto)» EN NEGRO (la línea ItemName, no una línea aparte) y el velo morado-negro cubre el item EN EL INVENTARIO (PostDrawInventario — el sprite re-dibujado con la tinta que respira).
5. **EL RAYO PERLIN DE LOS MINIONS SE PERDÍA LEJOS**: el centro del proyectil es su ANCLA (la presa, hasta 1.500 px) y el culling de vanilla solo dibuja proyectiles con el centro en pantalla: presa fuera de la vista = rayo INVISIBLE (el daño golpeaba igual). `DrawScreenCheckFluff = 2600` (la casa ya usaba el truco en sus otros rayos — este era el único largo sin él).
6. **EL ESCUADRÓN APARECÍA EN CUALQUIER PERSONAJE**: LA GUARDIA DEL DUEÑO — el retrato solo se fotografía (SaveData) y solo resucita (OnEnterWorld) si el grimorio VIVE en el inventario de ESTE personaje. Sin Eterno: retrato vacío, nada revive.
7. **LA MASCOTA SE ACTIVABA CON LA TECLA B**: `Item.buffType` era el imán del QuickBuff — vanilla la encontraba en el inventario (equipada o no) y la usaba en cada pulsación. El buff lo aplica UseItem (como el Sol en Miniatura): solo con el CLIC (AethonMenor + GrimorioMenor).
8. **LA MURALLA EXPLOTABA ANTES DE LA ANIMACIÓN**: TRES cerrojos nuevos: (a) EL FESTÍN ES SAGRADO (estado 1/3: NINGÚN golpe mata a medias — la «muerte ajena» del segmento que revienta al pool ya no pasa); (b) HitEffect sin gores mientras dura (la rejilla de cadáver a pantalla completa muere); (c) la zona del festín calla: los segmentos ni golpean ni reciben (damage 0 + dontTakeDamage), los cercanos a ≤700 px pegan 0, y el nuevo FaucesGlobalProjectile apaga los proyectiles hostiles en vuelo. La cena del WoF NO teletransporta el cuerpo (el muro dibujado queda clavado en su columna; ojos y Hungry se enroscan alrededor).
9. **EL ORDEN DE LA ESCALA (LA WIKI)**: la caza 1-50 ya era canónica (v6.75 verificada) — lo que faltaba era REPARTIRLA: el DUQUE FISHRON entra como puerta (37, con sus armas de siempre), Aethon ABANDONA la 59 y custodia LA CIEN, y las REPETICIONES (Moon Lord ×2..×9 y Emperatriz ×2..×5) pueblan la 63-99. LA CIMA COMPUESTA (100): Aethon ×1 Y Moon Lord ×10 Y Emperatriz ×5 — y ahí nace LA IRA.
10. **EL HAZ NO SE LANZABA**: la v6.86 volvía a pedir los contadores DESPUÉS de subir (si no los tenías, NUNCA nacía). HOY: la puerta 100 cobra la caza ANTES de subir y la Ira vive con el despertar 100 (el escuadrón de 11 al máximo sigue siendo requisito del disparo — la letra v6.60).
11. **MONEDAS 100 COBRE → 100 PLATINO**: escala exponencial exacta g=10^(6/99) — el nivel 100 clava 100.000.000 cobre (100 P), ningún tramo plano (la v6.86 truncaba en 1 P desde el ~43).
12. **LA OFRENDA MINERAL VARIADA**: GruposMineralDe — MEZCLA de 2-3 minerales de la era (18 + nivel/5 por cabeza), pagos GRUPO A GRUPO (OfrendaMineralGrupo en el alma, ver 4 en el cable), cada grupo con sus ALTERNATIVOS (el mundo de estaño paga con estaño). Ninguna pila solitaria es una pared.
13. **EL HITO DEL ESCUADRÓN CADA 20 NIVELES**: MultEscuadron (20→+10 %, 40→+15 %, 60→+20 %, 80→+25 %, 100→+30 %) — VIVO (BonusHitoVivo lee el despertar del dueño cada tick; el ×1,25 horneado de la v6.51.9 MUERTO). En los hitos hjson + el tooltip del arma, EN PORCENTAJE y COMO HITO.
14. **LA SEGUNDA BANDA SONORA**: «LA OLEADA FINAL» (164 BPM, 78 compases): doble clímax con MODULACIÓN (la tercera de picardía de Mi bemol), el coro a OCHO voces, timbales de guerra, la sala épica (reverb de tráiler por FFT) y la campana tubular. La MONEDA: al activarse una oleada (flanco de FestinVisible) elige UNA de las dos para TODA la oleada — la próxima vuelve a tirar.
15. **EL ÁRBOL SE MUDA DE DUEÑO**: CERO vínculo con el despertar — los puntos nacen del NIVEL DEL LIBRO AZUL (3 + nivel/5) y los efectos solo viven mientras se LLEVA el arma. El Libro Azul (30×30, PIL): clic derecho abre el árbol, clic izquierdo dispara la Descarga (12 + nivel·2, maná 0), sube con las bajas de su portador (8 por nivel, tope 100 — las paga OnKill por lastInteraction). El Códice MUERE de raíz (la bolsa de Investigaciones entrega el Libro Azul en su sitio).
16. **LA AUDITORÍA DE LA VENTANA DE INFO**: todos los números de los tooltips ahora nacen de las FUNCIONES (no de literales viejos) — la alternativa mineral lista LA MEZCLA (una línea por grupo con su estado), El Cielo pinta la mezcla en la escalera y «los 50 peldaños» murió (CIEN).

## EL PLAYTEST (la lista de la casa)
(a) [ ] **LA BRUMA POR LIGAS**: con despertar 60+, el ritual combinado lanza la bruma y sale la ASCENDIDA (doble círculo: 8 teal + 6 blancas contrarroto); con despertar <60 sale la del BASTÓN (1 anillo).
(b) [ ] **EL CALOR EN SU SITIO**: dispara el Rayo C — el hervor SOLO alrededor del conducto; el resto de la pantalla QUIETO.
(c) [ ] **LA MESA VARIADA**: en un peldaño mineral (59+), la ventana lista la MEZCLA (2-3 metales) — soltar lingotes de cualquiera de la lista va llenando grupo a grupo.
(d) [ ] **EL ALTAR CORRUPTOR**: clic derecho en el altar con un arma limpia en el slot → CORROMPER → el nombre «(Corrupto)» EN NEGRO y el velo morado-negro en el inventario.
(e) [ ] **EL HAZ AL 100**: con despertar 100 y el escuadrón 11-al-máximo, LA IRA se dispara (la puerta 100 ya cobró Aethon+ML×10+EoL×5).
(f) [ ] **LAS DOS BANDAS**: activa varias oleadas — unas suenan «El Festín Desatado» (152) y otras «La Oleada Final» (164, el coro a 8 y la modulación), al azar.
(g) [ ] **EL LIBRO AZUL**: la bolsa de Investigaciones lo entrega; clic derecho abre el árbol (los puntos nacen de SU nivel); matar con él sube el arma (8 bajas/nivel) y el tooltip del grimorio YA NO mienta.

## PRÓXIMOS PASOS
- El playtest de arriba (la letra del usuario manda: verificar CADA entrega en juego y rehacerla hasta que funcione).
- El orden canónico 1-50 de la caza ya era wiki-verificado (v6.75); las REPETICIONES nuevas (63-99) piden su playtest de ritmo (¿cada 4 peldaños una puerta es el compás correcto?).
- La curva de monedas (g=10^(6/99)) pide playtest: los tramos 60-80 deben SENTIRSE más caros sin ser un muro.

---

# v6.87 — LA CURA DEL MOD QUE NO CARGABA — EL CHECKLIST

> **PUBLICADA** — .tmod v6.87 (md5 `f2b0e5cac4a48d642789fa0808cd38fb`, 8.851.448 B) — 487 entradas (+ el highlight del altar), blobs 487/487, EOF exacto, la música del festín NUEVA (1,9 MB), headless 0 excepciones (worldgen + Server started).

## LO QUE PASÓ (la autopsia con evidencia)
La v6.86 CAÍA AL CARGAR en el juego del usuario: `MissingResourceException: Content/Tiles/AltarOfrendasTile_Highlight` → «Se ha producido un error al cargar AethonMod v6.86 / Los mods se han desactivado automáticamente». El altar activó `HasOutlines` y tModLoader exige el PNG `_Highlight` por convención — nunca se creó. **El mod entero desactivado = TODO invisible**: ni el rayo C, ni las bolsas, ni el agujero nuevo, ni el altar, ni el árbol, ni el escuadrón. NADA estaba roto de verdad — nada pudo verse.

## LA ENTREGA v6.87
1. **LA CARGA**: el mod VUELVE A CARGAR (el highlight del altar creado — silueta amarilla 36×54) y `audit_assets.py` aprendió las CONVENCIONES (tiles con HasOutlines → _Highlight obligatorio; rutas literales verificadas contra disco): la lección v6.84+v6.86 cerrada para siempre.
2. **LA BRUMA DEL GRIMORIO**: cuando el ritual del grimorio lanza su agujero de bruma ALEATORIO, ahora es **SIEMPRE el del BASTÓN** (`BrumaBlackHoleProjectile` — el renderer con EL ANILLO RÚNICO v6.82), sin importar el despertar (antes, con 85+, servía al Ascendido SIN el anillo). Las otras 3 familias conservan sus ascendidos.
3. **EL FESTÍN DESATADO**: la banda sonora rehecha — **152 BPM animada y épica**: latido acelerando → kick/snare/hats + bajo ostinato + cuerdas staccato → CORNO + CORO → puente con arpegios → CLÍMAX con doble kick, arpegios 16avos y la campana inarmónica → cola que empalma con la intro. Síntesis saw-aditiva de la casa, mezcla calibrada por espectro.
4. **TODO LO DEMÁS (v6.85+v6.86) LLEGA POR FIN**: la escalera 100, el libro corrupto, el altar, el árbol, el escuadrón eterno, las bolsas (el Absoluto EN la de agujeros, los 7 frutos EN la de investigaciones), la cura del calor del rayo C (amplitud 16 px, solape ×4-5, 2 octavas) — verificado contra el .tmod.

## EL PLAYTEST (la lista de la casa)
(a) [ ] **EL CARTEL ROJO MURIÓ**: el mod carga limpio (sin «error al cargar») — la v6.86 no cargaba NADA.
(b) [ ] **LAS BOLSAS LLEGAN**: al entrar al mundo — El Cofre del Arsenal (con las 26 dentro: la de Agujeros Negros con EL ABSOLUTO, la de Investigaciones con los 7 frutos) + Alas + Definitivas + Investigaciones + la Llave del Despertar + 99 dummies. Clic derecho en cada bolsa: solo lo que falte.
(c) [ ] **EL RAYO C HIERVE**: dispara el Rayo de Prueba C — el aire alrededor del haz HIERVE (±16 px en la boca, la cura v6.85 que el usuario jamás pudo ver). F8 para el sello «campo ✓».
(d) [ ] **LA BRUMA DEL GRIMORIO CON ANILLO**: dispara el ritual combinado hasta que toque la BRUMA — el agujero nace con SU ANILLO RÚNICO (8 runas teal girando) aunque el despertar sea 85+.
(e) [ ] **LA MÚSICA NUEVA**: activa las oleadas de hambre (la Carnada) — la música ahora es un ASALTO épico a 152 BPM (percusión completa, corno, coro, clímax con doble kick) con la campana del grimorio y el latido del hambre de firma.
(f) [ ] **EL RESTO DE LA v6.86 POR FIN VISIBLE**: la escalera 100 en El Cielo, el libro corrupto (mazmorra/Muralla), el altar de ofrendas (colócalo: YA TIENE su resaltado del smart cursor — el sprite que faltaba), el códice del árbol, el escuadrón eterno.

---

# v6.86 — EL DESPERTAR DE LAS CIEN PUERTAS — EL CHECKLIST

> **PUBLICADA** — .tmod v6.86 (md5 `b50130bfdb6210c2ec9c10280bad39ec`, 9.068.405 B) — 486 entradas (6 assets nuevos), blobs 486/486, EOF exacto, 5 .fxc byte-idénticos a v6.85, música intacta, headless 0 excepciones.

## LA LETRA Y LA ENTREGA
1. **EL DESPERTAR 100**: la escalera realineada — cada arma y lingote OBTENIBLE en la era de su jefe. Deerclops y el Duque Fishron FUERA de las puertas (las armas del Duque siguen como ofrenda 37); la Emperatriz EN su propia puerta (38) con SUS armas; Aethon custodia la 59; el Señor de la Luna corona la 50 de vanilla.
2. **LA IRA (El Haz)** en el 100 + **Moon Lord 5 veces** y **Aethon 1 vez** (contadores por cazador; el legacy cobra 1 de las 5 si ya lo tenía muerto). El tooltip del peldaño 100 muestra la cuenta viva.
3. **EL LIBRO CORRUPTO** (la puerta del 20): 1% rompiendo libros en la mazmorra · 1% abriendo sus cofres · 50% la Muralla · 1 por jugador (quien levantó el suyo, no levanta otro) · el grimorio HABLA al recogerlo («déjalo caer al suelo») · lo devora del suelo con despertar < 20 → la puerta del 20 paga PARA SIEMPRE.
4. **EL ALTAR DE OFRENDAS** (Heroic Weapons): colócalo (20 piedra + 2 estrellas + 1 diamante), clic derecho → el panel. El RELLENO (lo que NO se necesita y NO está corrupto) se vuelve PUNTOS (1/10 de plata) → pagan hasta el 30% de las esencias del peldaño. El grimorio muestra sus puntos en el tooltip.
5. **EL ÁRBOL DE HABILIDADES** (Umbra/Path of Terraria): EL CÓDICE DE HABILIDADES (en la Bolsa de las Investigaciones) lo abre con clic derecho. Ventana con cadenas, paneo, zoom, reset doble-clic y refund conectado (BFS). Puntos: 3 + 1 cada 5 de despertar. 10 nodos (daño/vida/maná/crít/velocidad/defensa/maná−/regen/ +1 minion KEYSTONE).
6. **EL ESCUADRÓN ETERNO**: los ojos sobreviven a la MUERTE (orbitan al caído) y a la SALIDA (se re-invocan al volver al mundo, con nivel+mejoras+daño exactos). La 12.ª invocación FORTALECE A TODOS A LA VEZ (4 clics en toda la vida, antes 44).

## LOS ARCHIVOS (los nuevos)
`Content/Systems/DespertarGrimorio.cs` (LA ESCALERA 100 + la Ira con contadores + la API del altar) · `Content/Systems/DespertarGrimorioSistema.cs` (el libro en la mesa + la puerta del 20 + el canje del altar + la ruleta de los cofres) · `Content/Players/CazaJefesPlayer.cs` (VecesMoonLord/VecesAethon/YaTuvoLibro + legacy) · `Content/Globals/CazaJefesGlobalNPC.cs` (contadores de la Ira + la Muralla suelta el libro + Deerclops/Fishron fuera) · `Content/Globals/AlmaEternoGlobalItem.cs` (LibroCorruptoConsumido + ProgresoOfrendas + CreditoPuntosEsencias — persistencia v3) · `Content/Items/Altar/LibroCorrupto.cs` (+GlobalItem del pickup + GlobalTile de los libros) + `LibroCorrupto.png` · `Content/Tiles/AltarOfrendasTile.cs` + `Content/Items/Altar/AltarOfrendasItem.cs` + `AltarOfrendasItem.png` + `AltarOfrendasTile.png` · `Content/UI/AltarOfrendasUI.cs` · `Content/UI/ArbolHabilidadesUI.cs` + `ArbolHabilidades_Cadena.png` + `ArbolHabilidades_Nodo.png` · `Content/Players/SkillTreePlayer.cs` · `Content/Items/Investigaciones/CodiceHabilidades.cs` + `CodiceHabilidades.png` · `Content/Players/EscuadronPersistPlayer.cs` · `Content/Projectiles/OjoEternoMinion.cs` (sobrevive a la muerte) · `Content/Weapons/GrimoireEternal.cs` (LA MEJORA MASIVA + tooltips del libro/la Ira/los puntos) · `Content/Globals/DespertarRequisitosGlobalItem.cs` (el ✓ del libro y la Ira) · `Content/UI/CieloDespiertoUI.cs` (escalera de 100) · `Content/Items/Bolsas/BolsasCategorias.cs` (+altar +códice) · hjson ×3 (924 claves simétricas) · `build.txt` — 6.86.

## EL PLAYTEST (la lista de la casa)
(a) [ ] **EL DESPERTAR 100**: el tooltip del grimorio muestra el peldaño siguiente con su arma/mineral OBTENIBLE en su era (nada de adamantita con el Rey Gelatina vivo). El Cielo del Despertar: la escalera de 100 con rueda.
(b) [ ] **LAS PUERTAS**: Deerclops y el Duque NUNCA aparecen como presa; la Emperatriz en el 38 con sus armas; Aethon en el 59 (mata al dios: la puerta se paga).
(c) [ ] **LA IRA**: al despertar 100 el tooltip muestra «Moon Lord n/5 · Aethon n/1» — mata 5 veces al Señor de la Luna y 1 a Aethon → LA IRA DISPONIBLE y el Haz responde.
(d) [ ] **EL LIBRO CORRUPTO**: consíguelo (mazmorra o Muralla) — el grimorio HABLA al entrar al inventario; suéltalo: el grimorio lo devora (despertar < 20) y la puerta del 20 queda paga PARA SIEMPRE (el tooltip la marca ✓ aunque estés en el 3). El segundo libro: NO se puede levantar (1 por jugador).
(e) [ ] **EL ALTAR**: craftea (20 piedra + 2 estrellas + 1 diamante), colócalo, clic derecho → el panel con el slot de relleno. Mete cosas que NO necesites (una espada vieja LIMPIA, materiales) → OFRENDAR → +puntos (verde). El tooltip del grimorio: «Puntos de Ofrenda del altar: N». Con 25+ puntos: al abrir la mesa del peldaño, el canje paga esencias (hasta 30%).
(f) [ ] **EL ÁRBOL**: usa EL CÓDICE (clic derecho) → la ventana con cadenas. Asigna (clic), devuelve (clic derecho sin romper el árbol), panea, zookea, resetea (doble clic). Los stats se sienten (vida/daño/maná). Al re-entrar: los niveles del árbol SIGUEN (por jugador).
(g) [ ] **EL ESCUADRÓN ETERNO**: invoca los ojos, MUERE: los ojos te orbitan muerto (no desaparecen). SAL DEL MUNDO y vuelve: el escuadrón vuelve EXACTO (niveles, anillos, daño). Clic derecho en el buff: se despiden (y NO vuelven). Con el escuadrón lleno, UN clic de invocación fortalece a TODOS («¡EL ESCUADRÓN ENTERO SE FORTALECE!») — 4 clics y están al máximo.
(h) [ ] **LA MASCOTA SIGUE EN EL 20** y las mejoras de los ojos abren en 10/40/70/80 (el aviso lo dice).

---

# v6.85 — LA SESIÓN DE LAS NUEVE INVESTIGACIONES — EL CHECKLIST

> **PUBLICADA** — .tmod v6.85 (md5 `7e19111c733de9293f7b53516390568f`, 9.047.446 B) — 480 entradas (14 assets nuevos), blobs 480/480, EOF exacto, el .fxc del calor RECOMPILADO (5.290 B), la música del festín dentro (2,1 MB), headless 0 excepciones.

## LA LETRA Y LA ENTREGA
1. **EL CALOR DEL RAYO C, POR FÍN**: el F8 decía «ON · 1 fuentes» y no se veía NADA — el campo apenas existía (cuentas de glow sin solape cada 52 px + UNA octava a 30 FPS). Ahora: amplitud 16 px, el pintor solapa ×4-5 hasta SATURAR el campo, DOS octavas siempre, ruido más fino y vivo (blobs ~120 px). El F8 ahora solo promete lo que pintó («campo ✓» / «CAMPO SIN PINTAR»).
2. **LAS BOLSAS**: el Agujero Absoluto → Bolsa de Agujeros Negros · el Réquiem → Bolsa de los Clásicos Cósmicos · los clickers + LOS 5 ITEMS NUEVOS → **LA BOLSA DE LAS INVESTIGACIONES** (nueva, bolsa 26, en el Cofre y en la entrega directa).
3. **LOS CINCO ITEMS DE LAS INVESTIGACIONES** (todos 0 maná):
   - **EL RELICARIO DEL AVISPÓN** (R1 — Bombus Apis): accesorio, tecla **L**, 8 s de aura solar amarilla (+12 % daño/vel/crít; +10 % daño recibido y drenaje), 45 s de recarga.
   - **EL SOL EN MINIATURA** (R3 — MEAC/XiHe): minion persistente (sobrevive a la muerte y a salir de la partida; despida con clic derecho en su buff); láser dorado telegrafiado cada 45 ticks.
   - **EL COLMILLO DORADO** (R7 — los rayos del cielo): 3 rayos sobre los enemigos más cercanos al cursor · **clic derecho: EL JUICIO DEL COLMILLO** (6 rayos en anillo, 20 s).
   - **LA MIRADA DE SOLYN** (R8 — el rayo de energía): mantén el clic 0,75 s y suelta la ráfaga de 5.600 px que barre tras el ratón (núcleo rosa, espina dorada, filamentos dendríticos).
   - **LOS GRILLETES DE ANDRÓMEDA** (R9 — el campo de fuerza): el maná que gastas se vuelve escudo (cada 100 → +20, máx 100); al romperse una carga: +50 de maná y shatter con afterimages.
4. **EL GRIMORIO Y SU BRUMA**: ya lanzaba la clase nueva (misma que el bastón) desde v6.82 — el playtest lo confirmará (el usuario lo vio «viejo» porque la v6.83 crasheaba).
5. **LA MÚSICA DEL FESTÍN** (2:44 instrumental, 70 BPM): suena SOLO durante las oleadas, con crossfade.
6. **LA LEY DE LAS ESTATUAS**: presa de estatua devorada → +1 % de hambre (no −1 %) + la voz: «Esto es solo una ilusión… me da MÁS hambre».
7. **LA ESENCIA CORRUPTA**: sprite regenerado (núcleo con glow, 3 tonos, borde dentado, motas).
8. **EL ETERNO FLOTA**: guardado en el inventario (no en la mano) → se ve flotando a la espalda del jugador (visual puro — la caza sigue siendo solo de la fase de hambre).

## LOS ARCHIVOS (los nuevos)
`Content/VFX/CalorLib.cs` (amplificación + F8 honesto) · `CalorDistorsionShader.fx/.fxc` (octavas) · `Content/Projectiles/Definitivas/RayosPruebaProjectile.cs` (cápsula ×1,6) · `Content/Items/Bolsas/BolsasCategorias.cs` (+3 bolsas) · `Content/Players/TestingPlayer.cs` (+BolsaInvestigaciones) · `Content/Items/Investigaciones/RelicarioDelAvispon.cs` + `Content/Dusts/SolarGlowDust.cs` + `Content/Buffs/RelicarioCdBuff.cs` · `Content/Weapons/Investigaciones/ColmilloDoradoStaff.cs` + `Content/Projectiles/Investigaciones/AureumSkyStrike.cs` · `Content/Weapons/Investigaciones/SolEnMiniaturaStaff.cs` + `Content/Projectiles/Investigaciones/SolEnMiniaturaProj.cs` + `Content/Buffs/SolEnMiniaturaBuff.cs` · `Content/Weapons/Investigaciones/MiradaSolyn.cs` (arma+carga+rayo) · `Content/Items/Investigaciones/GrilletesDeAndromeda.cs` + `Content/Projectiles/Investigaciones/CampoAndromedaProj.cs` · `Content/Effects/FlareEstrella4Puntas.png` · `Content/Effects/AnilloHexagonal.png` · `Content/Effects/Procedural/RuidoDendritico.png` · `Content/Weapons/GrimorioFlotante.cs` · `Content/Weapons/GrimorioHambrientoNervioso.cs` (la ley de las estatuas) · `Content/Items/Altar/ItemCorrupto.png` (regenerado) · `Content/Systems/OleadaHambreMusica.cs` + `Music/OleadaHambre.ogg` · `AethonMod.cs` (el keybind) · hjson ×3 (las claves nuevas + Keybinds) · `build.txt` — 6.85.

## EL PLAYTEST (la lista de la casa)
(a) [ ] **EL RAYO C HIERVE** (el objetivo de 4 sesiones): dispara el arma C — el aire alrededor del conducto SE VE ONDULAR (±16 px en la boca). F8: la línea Calor dice «ON · 1 fuentes · campo ✓». A 30 FPS también (2 octavas).
(b) [ ] **LAS BOLSAS**: entra al mundo → el kit entrega EL COFRE DEL ARSENAL (con la Bolsa de las Investigaciones dentro), la Bolsa de las Alas, la de las Definitivas y LA DE LAS INVESTIGACIONES. Ábrelas: el Absoluto está en la de Agujeros Negros, el Réquiem en la de Clásicos Cósmicos, los clickers y los 5 items en la de Investigaciones.
(c) [ ] **EL RELICARIO DEL AVISPÓN**: equípalo, pulsa L → el aura amarilla con flares girando 8 s (+12 % en todo) y el debuff de 45 s.
(d) [ ] **EL SOL EN MINIATURA**: invócalo (0 maná) — orbita con sus 2 anillos dorados; telegrafía y dispara su láser. MUERE a propósito y SAL DEL MUNDO: al volver, el sol SIGUE AHÍ (persistencia). Clic derecho en su buff: se despide.
(e) [ ] **EL COLMILLO DORADO**: clic → 3 rayos dorados caen sobre los 3 enemigos más cercanos al cursor (presagio → bolt → resplandor). Clic derecho → EL JUICIO (6 rayos en anillo, cooldown 20 s).
(f) [ ] **LA MIRADA DE SOLYN**: mantén el clic (la carga estelar gira) y al soltar la carga: la RÁFAGA rosa/dorada de 5.600 px barre tras el ratón con su latido.
(g) [ ] **LOS GRILLETES**: equípalos, gasta maná → el campo hexagonal cian crece con las cargas (anillos rúnicos violetas contra-rotando). Recibe un golpe con escudo: shatter + 50 de maná.
(h) [ ] **LA BRUMA DEL GRIMORIO**: deja que el grimorio lance su agujero de bruma aleatorio — LLEVA SU ANILLO RÚNICO (desde v6.82; esta sesión lo verificó en el fuente).
(i) [ ] **LA MÚSICA**: en el festín de hambre suena la banda sonora (2:44, con el latido); al acabar el festín, crossfade de vuelta.
(j) [ ] **LA LEY DE LAS ESTATUAS**: con el grimorio hambriento, que devore presas de ESTATUA → el hambre SUBE 1 % (rojo) y el libro dice: «Esto es solo una ilusión… me da MÁS hambre».
(k) [ ] **LA ESENCIA CORRUPTA**: el sprite nuevo (núcleo brillante, borde dentado, motas) en las oleadas.
(l) [ ] **EL ETERNO FLOTA**: con el grimorio guardado (no en la mano), se ve flotando a la espalda del jugador (vaivén + halo). En la mano: como siempre.

---

# v6.84 — LA CURA DEL MOD QUE NO CARGABA (EL SPRITE DEL AGUJERO ABSOLUTO) — EL CHECKLIST

> **PUBLICADA** — .tmod v6.84 (md5 `8413bb28506ab82211017f244f556ec5`, 6.644.320 B) — 467 entradas (466 + el PNG nuevo), blobs compartidos byte a byte, EOF exacto, headless 0 excepciones, el .fxc SIN CAMBIOS (la cura es un asset puro).

## LA LETRA Y LA ENTREGA
1. **EL MOD VOLVERÁ A CARGAR**: la v6.83 salió sin el sprite del proyectil del Agujero Absoluto (`Content/Projectiles/Cosmic/AgujeroAbsolutoBlackHoleProjectile.png`) — tModLoader compilaba sin queja y luego caía en la carga (`MissingResourceException`) desactivando el mod entero. El sprite está: 76×76 con el perfil Absoluto (núcleo negro platino · photon ring cian con arco de oro blanco · halo violeta del vacío).
2. **LA RED DE SEGURIDAD NUEVA**: `audit_assets.py` — toda clase con textura automática (ModItem/ModProjectile/ModNPC/…) sin `override Texture` y sin ser abstract DEBE tener su PNG. Hoy: 437 clases, **0 faltantes**. La lección queda cazada para siempre: los iconos de los proyectiles nuevos también cuentan.

## LOS ARCHIVOS
- `AethonMod/Content/Projectiles/Cosmic/AgujeroAbsolutoBlackHoleProjectile.png` (NUEVO — el asset que faltaba).
- `AethonMod/build.txt` — 6.84. (Cero .cs tocados; el .fx/.fxc no cambian.)

## EL PLAYTEST (la lista de la casa)
(a) [ ] **EL MOD CARGA**: tModLoader → Mods → v6.84 activa, SIN el cartel rojo de «Se ha producido un error al cargar AethonMod».
(b) [ ] El Bastón del Agujero Absoluto: dispara su agujero de 4 coronas (daño 480, atracción 680px, Einstein 640px) — igual que como estaba pensado en v6.82.
(c) [ ] TODO lo demás como siempre: el aire que hierve del Rayo C (v6.82/6.83), la lente de los agujeros, el Compás Rúnico y la Furia Solar (clickers), el Requiem del Púlsar.

---

# v6.83 — LA CAPTURA FORZADA (EL CALOR Y LA LENTE HASTA EN RETRO) — EL CHECKLIST

> **PUBLICADA** — .tmod v6.83 (md5 `0c0b0feba423afaf2eca8e68cd602a05`, 6.640.392 B) — 466 entradas, EOF exacto, headless 0 excepciones, el .fxc SIN CAMBIOS (la cura es matemática C#).

## LA LETRA Y LA ENTREGA
1. **LA CAPTURA FORZADA**: dos ajustes de vídeo del juego (iluminación **Retro/Trippy** y **«Lighting Update Every Frame»**) mataban la captura de pantalla SIN aviso — ni calor, ni lente, ni el tinte del cementerio de vanilla, con CERO errores en el log. Ahora, en ese modo, el mod redirige el mundo a SU PROPIO render target (des antes del primer trazo) y compone en el punto 36: **el aire hierve y los agujeros distorsionan HASTA EN RETRO**.
2. **EL ANCLA DEL ZOOM DEL CAMPO**: con zoom ≠ 1 (4K/ultrawide con zoom forzado, o el slider) el campo de calor a ¼ quedaba desplazado hasta MEDIA PANTALLA del haz (v6.81 escalaba con ancla de esquina; la cámara real escala ALREDEDOR DEL CENTRO). Ahora el hervor va CENTRADO en el conducto a cualquier zoom.
3. **EL F8 QUE NO MIENTE**: la línea Calor ahora reporta la composición REAL — «ON · N fuentes» (la cadena de filtros), «ON · N fuentes · RETRO» (el pase del backbuffer), «SIN CAPTURA (¿iluminación Retro?)» (nada compone) o «¡RETRO FALLÓ!».

## LOS ARCHIVOS
- `AethonMod/Content/Effects/BlackHoleLensSystem.cs` — ModifyTransformMatrix (el bind del RT del modo sin captura, en la ventana limpia del frame), TerminarFrameRetro + BlitFullscreen (la composición completa del frame retro) y el consumo auto-verificado del flag.
- `AethonMod/Content/VFX/CalorLib.cs` — PintarCampo (el corazón compartido: sobres del estallido + campo a ¼), el ANCLA del zoom (los 3 tipos de fuente), PaseRetro (el pase manual con la matemática EXACTA de ScreenShaderData.Apply), HayCalorVivo y el Diagnostico honesto.
- `AethonMod/build.txt` — 6.83. (El .fx/.fxc NO cambian.)

## EL PLAYTEST (la lista de la casa)
(a) **Iluminación COLOR/BLANCA**: [ ] el Rayo C hierve como en v6.82 · [ ] F8 «Calor: ON · 1 fuentes» · [ ] CON ZOOM ≠ 1 el hervor sale CENTRADO en el conducto (antes: desplazado hasta media pantalla).
(b) **EL EXPERIMENTO QUE LO PRUEBA TODO**: [ ] cambia la iluminación a RETRO → dispara el Rayo C → **EL AIRE HIERVE IGUAL** (F8: «RETRO») · [ ] con un agujero negro en pantalla, el fondo SIGUE DISTORSIONÁNDOSE en retro · [ ] pantalla NUNCA negra ni congelada al entrar/salir de retro · [ ] el minimapa y el mapa (M) limpios · [ ] el modo foto sin corrupción.
(c) **«Lighting Update Every Frame» ON**: [ ] mismo resultado que retro (calor + lente vivos).
(d) **LO DEMÁS INTACTO**: [ ] la lente y el calor con iluminación normal, los agujeros, el grimorio, las 4 armas de v6.82 — todo como siempre.

---

# v6.82 — LA CURA DEL CALOR INVISIBLE + LAS CUATRO ARMAS NUEVAS — EL CHECKLIST

> **PUBLICADA** — .tmod v6.82 (md5 `c18a52b33d6336b5ae70a273ac54a065`, 6.638.549 B) — 466 entradas, blobs 466/466, EOF exacto, headless 0 excepciones.

## LA LETRA Y LA ENTREGA
1. **EL CALOR INVISIBLE CURADO** (el bug de unidades ÷300 en CalorDistorsionShader: el ruido venía en UV y se multiplicaba por px→UV — 0,04 px de hervor invisible; ahora ruido unitario × amplitud 9 px + pesos del campo arriba + frame ciego de UseGlobalOpacity curado + estallidos del Rayo C al alza). El .fxc recompilado con la cadena completa y validado contra el MojoShader REAL.
2. **EL ANILLO RÚNICO DE LA BRUMA**: 8 runas teal a 2,62·R (el estándar de los agujeros base que le faltaba).
3. **EL BASTÓN DEL AGUJERO ABSOLUTO**: 4 coronas rúnicas (la 4ª: 10 runas blanco-cian a 3,95·R), gradiente violeta→cian→oro, daño 480, atracción 680px, esfera ×1,14, Einstein 640px — más fuerte que los Supremos.
4. **LA CLASE CLICKER DE LA CASA** (standalone): EL COMPÁS RÚNICO (constelación cada 8 clics — 5 rayos de StormLib convergentes) + LA FURIA SOLAR (nova cada 12 clics — PyraLib.Estallido + flash + temblor). Radio de contrato dibujado con fade y brillo al cursor, ping de chispas por clic, MenuTick.
5. **EL REQUIEM DEL PÚLSAR** (arma cósmica): faro rotante 4 rev/s con haces dobles, estela Ribbon Comet, overbright ×1.4, pulsos de radio cada 12 ticks, colapso con CalorLib.Estallido (aberración R/G/B).

## LOS ARCHIVOS
- `AethonMod/Content/Effects/Shaders/CalorDistorsionShader.fx/.fxc` — la cura del bug de unidades (vkd3d→fx20_build→fx20_validate).
- `AethonMod/Content/VFX/CalorLib.cs` — AnchoPx 9,0 · pesos del campo · UseGlobalOpacity inmediato.
- `AethonMod/Content/Projectiles/Definitivas/RayosPruebaProjectile.cs` — estallidos 26/460 + 10/300.
- `AethonMod/Content/VFX/BrumaBlackHoleRenderer.cs` — DrawRuneCircle (el anillo que faltaba).
- `AethonMod/Content/VFX/SupremoBlackHoleRenderer.cs` — PerfilSupremo extendido (ConCuartoAnillo + RuneAbs) y el perfil Absoluto.
- `AethonMod/Content/VFX/AgujeroAbsolutoBlackHoleRenderer.cs` (NUEVO) — el shim del Absoluto.
- `AethonMod/Content/Projectiles/Cosmic/AgujeroAbsolutoBlackHoleProjectile.cs` (NUEVO) + `Weapons/Cosmic/AgujeroAbsolutoStaff.cs` (NUEVO) + su PNG.
- `AethonMod/Content/Weapons/Clicker/` (NUEVO: ClickerCasa.cs + CompasRunico.cs + FuriaSolarClicker.cs) + `Projectiles/Clicker/ClickerGolpeProjectile.cs` (NUEVO) + 2 PNG.
- `AethonMod/Content/Projectiles/Cosmic/RequiemPulsarProjectile.cs` (NUEVO) + `Weapons/Cosmic/RequiemDelPulsarStaff.cs` (NUEVO) + su PNG.
- `AethonMod/Content/Effects/BlackHoleLensSystem.cs` — el Absoluto en los 3 puntos de la lente.
- hjson ×3 (4 ítems + 3 DisplayNames), `tools/gen_iconos_v682.py` (los 4 iconos).

## EL PLAYTEST (la lista de la casa)
(a) Rayo C hirviendo de verdad · (b) Bruma con su anillo · (c) el Absoluto con 4 coronas · (d) los clickers con su círculo + ping + especiales de combo · (e) el púlsar con sus faros y su colapso.

---

# v6.81 — LA CURA DEL CATCH-22 — EL CHECKLIST

> **PUBLICADA** — release v6.81 + CDN verificado byte a byte (md5 `331eb08cb2de86ad05b4a9ceadcfed49`, 6.627.810 B).

**Qué pasó** (la letra): «aunque ya no hay errores grafico de pantalla negra, nunca vi que esto funcionara en el arma de prueba C» + «borra la Bolsa del Crecimiento y sus armas, son todas basura y nada creativa».

**La causa del calor invisible** (cazada contra el tML 2026.08 REAL descompilado — FilterManager + Main.DoDraw): `Main.DoDraw` decide la captura del mundo ANTES de dibujarlo (`CanCapture()` exige un filtro YA activo) y `EndCapture` — el único camino del hook del mod — SOLO se llama si ya había captura. El filtro del calor se activaba DENTRO del hook: **jamás podía encenderse solo**. La lente gravitacional sufría el MISMO encierro. La cura: la decisión vive en `PostUpdateEverything` (cero latencia), el filtro ahora también FUERZA la captura para la lente (con el campo vacío el shader copia pixel-perfect), puerta nueva contra las capturas ajenas del minimapa/foto, corrección de zoom del campo a ¼, y la línea CALOR en el F8.

1. **EL AIRE QUE HIERVE (el rayo C)**: [ ] al dispararlo, el aire alrededor del conducto HIERVE — denso en la boca, susurro en la aguja, subiendo (la convección); [ ] la distorsión respeta el mundo NÍTIDO bajo el haz; [ ] **POR PRIMERA VEZ** — esto nunca se había visto en el juego real (solo en la teoría de v6.78).
2. **LA ABERRACIÓN DEL ESTALLIDO**: [ ] en la IGNICIÓN (el arranque del láser) un anillo cromático rojo-afuera/azul-adentro alrededor del golpe; [ ] el ÚLTIMO SUSPIRO al morir el haz (un tercio de fuerza).
3. **LA LENTE FORZADA (los agujeros)**: [ ] cualquier agujero negro rúnico (Cosmic, Umbral, Bruma, Olvido, Supremo…) DISTORSIONA EL FONDO a su alrededor — incluso fuera del cementerio/tormenta (antes solo pasaba en biomas con filtro de vanilla); [ ] el Devorador (GravLens) también.
4. **EL F8**: [ ] línea nueva `Calor: Aire hirviente (CalorLib)` — «ON · 1 fuentes» con el rayo C, «ON · por lente» junto a un agujero, «off» en calma; [ ] la línea de AYUDA sigue siendo la última (gris).
5. **SALUD DEL RENDER**: [ ] el minimapa y el mapa grande (M) SIN distorsiones raras, sin parpadeos, sin frames fantasma; [ ] la pantalla JAMÁS se pone negra (la cura v6.79 intacta); [ ] FPS estables con el rayo + agujeros a la vez.
6. **LA PURGA**: [ ] al entrar al mundo NO llega la Bolsa del Crecimiento; [ ] en la Bolsa Maestra ya NO está; [ ] nada del Crecimiento en el juego; [ ] quien tuviera los ítems los ve como «missing item» (esperado — la purga es la purga).
7. **LO DEMÁS INTACTO**: [ ] el Equinoccio, los Ojos, LA IRA, los rayos de las sentencias, las 24 bolsas restantes, los sonidos — todo como en v6.80.

---

# v6.58 — EL ATAQUE SOBRE TODO EL MUNDO + LA IGNICIÓN A CIEGAS — EL CHECKLIST

> **PUBLICADA** — release [429662862](https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.58.0) + CDN verificado byte a byte (md5 `05bd91d1002b4881714d75acf3cf2ff0`, 6.365.818 B; asset id 694089669).

**Qué pasó** (la letra): «en el rayo C hay algo curioso, justo en el primer segundo de lanzar el rayo hay un pequeño destello blanco, eso no deberia pasar ese pequeño destello blanco de luz no deberia estar ahi, el destello es pequeño es solo de iluminación» + «algunas sprite del juego como las hojitas que caen de los arboles se ven sobre el ataque y eso no debe ser asi, el ataque debe estar sobre todo» + «todas las alas tienen picos, esos picos son intencionales o es un error como el de las armas».

1. **CERO DESTELLO BLANCO EN LA IGNICIÓN**: [ ] al DISPARAR el rayo C, NO aparece ningún destello blanco de luz (ni el mundo iluminándose blanco un instante, ni la estrella blanca de la boca) — el momento se lee solo con el TEMBLOR suave + el instante congelado + el estallido de soles.
2. **EL ATAQUE SOBRE TODO**: [ ] las HOJITAS que caen de los árboles (y cualquier polvo/gore/partícula del juego) ya NO se ven SOBRE el haz — el conducto pasa POR ENCIMA de todo el mundo; [ ] lanza el rayo junto a árboles con viento y las hojas cruzan POR DETRÁS del haz.
3. **EL HAZ SOBRE LA INTERFAZ (DECISIÓN)**: [ ] el haz ahora también pasa por ENCIMA del hotbar/corazones (es el punto «sobre todo» de la casa). Si lo prefieres por DEBAJO de la interfaz (que el hotbar se vea encima del haz): dilo — es un cambio de minutos.
4. **LO DEMÁS IGUAL QUE LA .57**: [ ] la tela blanca a todo el ancho, la punta de aguja, la muerte estrecha, los soles lejanos cayendo, el temblor suave, la corona del color del haz — TODO se ve igual (nada de la pila se tocó: solo se mudó de lugar en el frame).
5. **CERO PICOS (siempre)**: [ ] bordes lisos como siempre — la mudanza de pase NO añadió ni una capa (mismo volumen, mismo punto del frame distinto).
6. **EL RATÓN BIEN (la cura silenciosa)**: [ ] durante TODO el ataque el PUNTERO responde perfecto — el bug del puntero corrupto (los SetZoom_* del tML son remapeadores de ratón) se cazó ANTES de publicar, pero verifica que la cámara/mira/uso de items funciona normal con el haz activo.

**La respuesta a la pregunta de las alas**: los «picos» de TODAS las alas SON LAS PLUMAS — el diseño (abanico de 2-7 plumas vectoriales de punta afilada; las puntiagudas se llaman Agujas del Vacío y Cuchillas del Eclipse). NO es el bug de los quads de las armas (v6.56.6, curado en la .56.7). ¿Las quieres más redondas? Es un cambio de diseño — pedirlo y se afina.

---

# v6.57 — LA VUELTA DEL HAZ C: EL CORAZÓN DORADO MUERE, EL ALMA NACE Y LA MUERTE ESTRECHA — EL CHECKLIST

> **PUBLICADA** — release [429635856](https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.57.0) + CDN verificado byte a byte (md5 `4df379dd4870807e7e1001aaf9d5651c`, 6.365.240 B; asset id 693964616).

**Qué pasó** (la letra): «el mejor es el C, pero hay algo raro, son como circulos amarillos que aprecen a veces y son demasiado grandes, no deben aparecer» + «debe tener otra textura como esa pero que ocupe todo el haz en ancho» + «el pequeño brillo de soles runicos debe aparecer mas lejos del jugador, y a medida que se carga el ataque, estos caen hacia el centro» + «al lanzar el haz la pantalla debe temblar un poco, tal vez un temblor de unos pocos pixeles y con movimiento suaves» + «el color de los circulos runicos debe ser el mismo que el del haz C» + «al final del haz este como que se hace estrecho pero no lo sucifiente, al terminar de lanzar el haz este debe hacerse estrecho y luego desaparecer».

1. **CERO CÍRCULOS AMARILLOS (la ley del corazón dorado)**: [ ] en NINGÚN momento del ataque (carga, disparo, boca, márgenes del haz) aparece un círculo/blob AMARILLO grande — los soles son ahora ESTRELLAS BLANCAS de 4 puntas puras. Si ves algo amarillo y redondo: captura y reporta.
2. **EL ALMA — LA TELA BLANCA A TODO EL ANCHO (el haz C)**: [ ] la textura blanca que antes vivía SOLO en el centro ahora TIÑE EL HAZ DE PUNTA A PUNTA Y DE BORDE A BORDE (una tela blanca incandescente sobre el plasma); [ ] la vena central MÁS BRILLANTE sigue ardiendo encima (el «láser dentro de la explosión»).
3. **LA PUNTA DE AGUJA**: [ ] el extremo lejano del haz termina en PUNTA FINA (como una aguja/lanza) — ya NO es un extremo romo/cortado.
4. **LA MUERTE ESTRECHA**: [ ] al TERMINAR de lanzar (último segundo), TODO el haz SE ESTRECHA visiblemente hasta volverse un hilo delgado y LUEGO desaparece — como una boca que se cierra.
5. **LOS SOLES RÚNICOS DE LA CARGA**: [ ] nacen AHORA MUCHO MÁS LEJOS del jugador (del borde lejano del conjuro); [ ] caen RECTOS hacia el centro (sin orbitar); [ ] la caída ACELERA al final (el tirón) y el enjambre se aprieta con la carga; [ ] al disparar salen escupidos en abanico.
6. **EL TEMBLOR SUAVE DEL LANZAMIENTO**: [ ] al DISPARAR, la pantalla tiembla UN POCO — unos pocos píxeles, movimiento SUAVE (nada de sacudidas secas) + el instante congelado de cine; [ ] durante el resto del disparo el temblor es un susurro (ya no domina).
7. **LA CORONA DEL COLOR DEL HAZ**: [ ] los círculos rúnicos de la carga son VIOLETA/MORADO/ROJO/MAGENTA — LA MISMA paleta que viaja por el haz C (ya NO se ven blancos/cian ajenos).
8. **CERO PICOS (siempre)**: [ ] todos los bordes LISOS en carga, disparo Y muerte (la sim 1:1 dio 0 picos en el sobre en vida y en muerte); si algo se ve dentado: captura y reporta.

---

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

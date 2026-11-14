# AethonMod — Características e Inventario del Contenido

> 📜 **DOC HISTÓRICA** — Este documento describe una era anterior del
> proyecto. Para el estado vigente y las reglas actuales manda
> **[MAESTRO.md](MAESTRO.md)**; esta copia se conserva como referencia.


> **Inventario verificado contra el código de v6.50.30** (commit `1d26716`, 2026-09-28).
> Fuente de la verdad: el propio `Content/` (297 `.cs`, ~106.500 líneas). Cómo funciona cada
> cosa → `DISEÑO_DEL_MOD.md`. Estado/pendientes → `STABLE-SNAPSHOT.md`.

## Información general

- **Nombre**: Aethon, la Luz Primordial (`AethonMod` — el nombre de la CARPETA es el nombre del mod)
- **Versión**: 6.50.30 (en `AethonMod/build.txt`)
- **Target**: tModLoader 2026.07.3.0 (Terraria 1.4.4.9) · .NET 8 · `side = Both`
- **Repo**: https://github.com/Leo0x01/Aethon-Mod-Terraria
- **Registrado en tML**: 115 ModItem · 119 ModProjectile · 8 ModNPC · 9 ModBuff · 23 ModSystem ·
  1 ModTile · 1 ModBiome · 1 ModDust · 6 ModPlayer · 2 ModConfig · 6 Global · 3 PlayerDrawLayer
  (≈281 clases de contenido + soporte) · **297 archivos .cs** · 67 archivos VFX
- **Localización**: es-MX (idioma PRIMARIO) + en-US, hjson SIMÉTRICOS — y
  es-ES como ESPEJO GENERADO de es-MX (v6.50.71: TODAS las variantes de
  español caen en la nuestra; tools/sync_es_es_v65071.py)
- **Dependencias**: NINGUNA (`modReferences` vacío — auditoría v6.26)

---

## EL GRIMORIO DEL ETERNO (el corazón del mod)

- Arma **híbrida magia + invocación**, sin maná (regla de la casa), niveles **infinitos**,
  XP **por ítem individual** (persiste en el `.plr`).
- **Clic izq.**: proyectil vanilla Nightglow (931) con homing + bolts extra cada 3 niveles.
- **Clic der.**: invoca el **CosmicOrbMinion** (IA Terraprisma, nivel viaja en `ai[2]`).
- **Obtención**: 1 Fragmento Génesis + 5 lingotes de oro **o** platino (sin yunque) — o gratis en
  la Bolsa de los Fundacionales.
- **3 estados** (v6.46): sostenido = TODO el poder · barra rápida = come XP y conserva minions ·
  guardado = nada.
- **Fórmula de nivel**: `XP = 100 × nivel^1.5`. XP de mobs = `5 × estrellas²` del bestiario
  (×3 primera kill de especie, ×2 hardmode). Jefes: `5.000/25.000/100.000 + lifeMax/10 + ★ + 1.1·nivel·111`.
- **El hambre** (nivel 25+): 75 s sin matar = 1 momento (10 máx) → susurros con sabor a bioma →
  al 4º momento **LA FURIA** (ver DISEÑO §5). El libro se pone **celoso** si cambias de arma.
- **+1 nivel directo** por cada **esencia de jefe** devorada (ver abajo).

---

## ARMAS (~115, en 18 bolsas)

Todas sin maná (regla de la casa v6.26). Formato: **nombre `daño/cadencia`** — lo esencial.

### Los 5 RAYOS (bolsa 18 — el pipeline propio, cero sprites)
| Arma | Daño | Pipeline |
|---|---|---|
| **Bastón de Rayo Primordial** | 130/22 | `RayoParams.Tormenta` (1000 px, clima de vanilla 1.4.5): rayo instantáneo sobre el cursor, radial 100 + cadena 2 + Electrified |
| **Arco de Sobretensión** (Arc Surge portado) | 180/16 | `RayoParams.Arco(mano,blanco)`: 1 arco anclado a la mano + hasta 2 hermanos a NPCs en cono 60°, carmesí, 1 tick de vida |
| **Colmillo de Vena Trueno** (firma la referencia del dragón) | 155/30 | **Trío 1 naranja + 2 amarillos** con recada 4 t, parpadeo 50 %, caída telegrafiada 26 t, golpe al aterrizar (radial 130 + cadena 3) |
| **Cetro del Trueno Rúnico** | 95/26 | StormLib: rayos del cielo 700–980 px + salta 3 + electrifica |
| **Cetro del Trueno Perlin** | 130/22 | `PerlinBolt` (fBm 1D, meandro + Chaikin): arco continuo bastón→cursor que SIGUE al cursor 10 t |

### Por familia
- **Fundacionales (5)**: Grimorio `11/22` · Supernova `90/45` · PlasmaStorm `40/35` · PhoenixNova `70/50` · QuantumSplit `45/30`
- **Clásicos cósmicos (4)**: **El Sol** `80/50` (sol rojo gigante, shader propio) · Medusa Nebular `32/36` (summon) · Cometa Estelar `46/36` (summon) · Púlsar Vivo `38/36` (summon)
- **Agujeros negros (11)**: BlackHole `100/60` · Olvido/Cósmico/Umbral/Bruma `150/50` · sus 4 **Ascendidos** `200/50` · **Supremo** `300/50` · **Supremo Aurora** `300/50` — cada uno con su renderer de lente gravitacional propia
- **Soles rúnicos (20)**: Sol Rúnico 1..20 (daño `80+24·(tier−1)` → 820 en el 20; el 20 = **EL GRAN SELLADO** ×1.30)
- **Estrellas reales (19)**: Estrella de Neutrones `240/45` (starquakes) · Púlsar `280/50` (haces polares) · Enana Blanca `180/50` · Estrella Muerta `200/60` · Supergigante Roja `260/60` · **Magnetar** `320/55` (cadenas de rayo automáticas) · Ciclo Estelar · Sembrador Cementeral · Colapso Magnetar · Lágrimas de Sol Moribundo · Decreto de Eclipse · Cometa Errante · Nova Encadenada · Voz de Cuásar · Telar de Constelaciones · Lluvia de Meteoros · Abrazo de Nebulosa · Filo del Horizonte `230/38` · Rayo Gamma `300/70`
- **Armas de las librerías VFX (7)**: Sinfonía Primordial `120/32` · Tormenta Nebular `80/48` · Lanza del Alba `90/16` · **Eclipse Primordial `700/55`** (el total: creciente que apaga el día) · Cetro del Trueno Rúnico · **Desgarro en la Realidad `250/50`** · Cetro del Trueno Perlin
- **Bastones creativos (6)**: Reloj de Arena Cósmico `170/55` · Marea Gravitatoria `210/60` · Enjambre Prismático `190/45` · Péndulo del Juicio `250/65` · Coro Espectral `160/50` · **Ocaso de Aethon `150/14`** (arma suprema del patrón gauge: carga → 8 s ×3 → sobrecalentada)
- **Exhumados (2)**: Rencor Primordial `130/60` · Eminencia Atroz `150/20`
- **Dos formas (4)**: Sembrador Cementeral · Colapso Magnetar · Lágrimas de Sol Moribundo · Decreto de Eclipse (segunda forma de uso)
- **Desgarros (9)**: Desgarro en la Realidad · Sutura Cuántica `96/30` · Portal Dimensional `88/32` · Pliegue del Espacio `118/34` · Herida Eléctrica `74/26` · Corazón del Colapso `108/32` · Garganta del Vacío `102/34` · Umbral Roto `95/28` · Leviatán Espectral `86/30`
- **Códigos vivos (4, portados de demos web)**: Danza de Orbes `96/30` · Lente del Abismo `104/34` · Sol Vivo `100/30` · **La Sierpe Estelar `90/32`** (16 segmentos: cabeza + espina eslabón a eslabón — la referencia de estilo del jefe Aethon)
- **Sierpes (12, v6.38)**: La Sierpe Estelar (madre) · Ouroboros Astral `92` · Caravana Espectral `88` · Anguila Solar `98` · Ciempiés Rúnico `90` · Flagelo Estelar `96` · Víbora Genesíaca `100` · Boa de Eclipse `86` · Farol Guardián `102` · Cinta Aurora `96` · Manada Astral `90` · **Cría Estelar `30/30`** (summon)
- **Huéspedes (3, réplicas de estudio)**: Tomo de la Apatía Nula `63/8` (tentáculo del vacío con implosión) · Fragmento de Supernova `30/30` (summon) · Tajos Astrales `46/24`
- **Apuestas (6, v6.42 — ritmo/defensa)**: **Metronomo Pulsar `65/12`** (el ritmo) · Vela Solar `40/6` (cadencia creciente) · Guadaña del Desgarro `85/28` (melee) · Égida Nova `50/30` (melee, parry) · Eco Cuántico `70/24` (eco encadenado) · **Verbo Primordial `120/45`** (7 movimientos)
- **Ideas del grimorio (6, v6.50.19 — evolucionan con el nivel 6/12/20 del Grimorio)**: Folio Errante `46/24` · Pluma Primordial `34/17` · Sello Errante `30/30` · Lengua de Tinta `36/26` · Ojo de Texto `44/20` · Verso Vivo `24/28`
- **Tests (4)**: TestMagicRing, TestSparkle, ProjBeam, TestMagicRingV2 (`10/20`, herramientas VFX)

---

## LAS 18 BOLSAS (Content/Items/Bolsas/)

Semántica de **garantía** (solo lo que falte; reabrirla repone) + permanente + reabrible.
`TestingPlayer.OnEnterWorld` (1 jugador) entrega las 18 + 99 dummies.

1 **Probador** (kit: Fragmento, 100 barras de oro, LevelUpTester, BossSummonBag, SeerOrb, tests, Prisma, 5 altares, Carnada, 5 llamados) · 2 **Fundacionales** (5) · 3 **Clásicos Cósmicos** (4) · 4 **Agujeros Negros** (11) · 5 **Soles Rúnicos** (20) · 6 **Estrellas Reales** (19) · 7 **Armas de Librerías** (7) · 8 **Bastones Creativos** (6) · 9 **Exhumados** (2) · 10 **Cosméticos** (9) · 11 **Dos Formas** (4) · 12 **Desgarros** (9) · 13 **Códigos Vivos** (4) · 14 **Sierpes** (12) · 15 **Huéspedes** (3) · 16 **Apuestas** (6) · 17 **Ideas del Grimorio** (6) · 18 **Armas de Rayo** (5)

---

## JEFES (6) + EL TESTIGO

| Jefe | Vida | Daño | Invocador | Esencia | Arte |
|---|---|---|---|---|---|
| **Aethon, la Luz Primordial** (el final) | 2.400.000 | 95 | El Nombre de Aethon (solo día) | Esencia de Aethon | **100 % código** (eclipse de hueso: cabeza-orbe nocturno + anillo de oro + fauces en V + 46 vértebras + cola en el fondo del cielo) |
| Eco del Primer Portador | 180.000 | 50 | Sombra del Portador | Esencia del Primer Portador | 100 % código (duelista con **parry 20 %**) |
| Eco de la Arquera Estelar | 160.000 | 45 | Pluma de la Arquera | Esencia de la Arquera Estelar | 100 % código (el arco vivo se tensa) |
| El Guardián del Rift | 95.000 | 55 | Sello del Rift | Esencia del Guardián del Rift | 100 % código (teletransporte en desgarros) |
| El Titán Hueco | 42.000 | 30 | Cristal del Titán Hueco | Esencia del Titán Hueco | 100 % código (coloso andante) |
| **El Testigo** (NO jefe) | 200.000 (inmortal) | 0 | nace solo, siempre presente | — | 100 % código (orbe violeta) |

- Aethon: **5 fases** (Polvo Estelar → Nebulosa → Gravedad → Agujero Negro → Reconocimiento),
  estados NACIENDO/BAJO_TIERRA/**EMERGIENDO**/SUPERFICIE/**CARGA**/HUNDIÉNDOSE + cine de muerte
  (desarticulación cola→cabeza). Barra vanilla con icono propio (`_Head_Boss.png`, fix v6.50.26).
- Deerclops: **NO es jefe de oleada** — solo el **dado del 1 %** (v6.50.30).
- Los invocadores (5) son reutilizables, solo de día, sin recetas (vienen en bolsas).
- **Solo Aethon tiene icono de barra**; los otros 4 penden de `NPC.boss` (doc-rot conocido).

---

## LAS 12 ESENCIAS (devorar = +1 nivel + diálogo de "sabor" del jefe)

- **7 vanilla** (drop de jefes de LA FURIA, sin límite; el Testigo las vende a 10 de oro tras
  sobrevivir la oleada 10): Rey Gelatina · Ojo de Cthulhu · **Deerclops (solo dado 1 %)** ·
  Abeja Reina · Devorador de Mundos · Cerebro de Cthulhu · Skeletron.
- **5 del mod** (drop de cada jefe propio, **límite 10 por mundo** — contador persistente):
  Titán Hueco · Guardián del Rift · Arquera Estelar · Primer Portador · Aethon.
- Cada esencia: `+1 nivel completo` al grimorio + **voz del libro probando el alma** —
  **3 variantes por jefe, 12 jefes = 36 líneas por idioma** (v6.50.30, nunca repite la última).

---

## MINIONS (7) · BUFFS (9) · ACCESORIOS (3) · COSMÉTICOS (7)

- **Minions**: CosmicOrbMinion (del Grimorio) · LivingPulsar · StellarComet · NebulaJellyfish · Cría Estelar · Fragmento de Supernova (+ CosmicOrbBolt legacy).
- **Buffs**: 6 indicadores de minion + OcasoActivo (modo del arma) + Sobrecalentado (lockout del Ocaso) + Quemadura Cósmica (debuff).
- **Accesorios** (los "signos mágicos" de SigiloLib): Sello Génesis (8 runas girando) · Anillos Sol Rúnico · Anillos Horizonte.
- **Cosméticos**: Corona del Vacío · Corona Rúnica · Anillo Rúnico Dorsal · **Corona Rúnica Aura** · **Forma Ascendida** (drop de Aethon) · Brasa del Eclipse · Prisma de Paisajes.

---

## EL MUNDO

- **Altar Antiguo** (tile 3×2 interactivo): clic der → Fragmento Génesis. WorldGen: 3 por mundo nuevo, bajo tierra.
- **El Sagrario Hueco** (`HollowSanctumBiome`): sub-bioma cristalino subterráneo (activación 400 HP).

---

## SISTEMAS (23 ModSystem) — resumen

**El núcleo del grimorio**: ShardLevelSystem (XP) · ShardLevelItem (niveles por ítem) ·
GlobalNPCXP (paga la XP + drops) · ShardPlayer (hambre/celos/bonus) · ShardHUDSystem (barra dorada) ·
ShardSyncSystem + EcoRed (9 mensajes de red) · WeaponScaling (todas las fórmulas).
**LA FURIA**: GrimorioFuriaSistema (el motor de oleadas + el dado 1 %) · OleadaNPC (el sello,
GlobalNPC) · EsenciasModSistema (límite 10/mundo) · PresenciaNPCSystem (el Testigo siempre está).
**La voz**: EcoSistema (capa UI) · EcoLib (librería de voces dramáticas).
**VFX**: OndaSystem · BrumaSystem · OcasoSystem · CieloSystem · CalidadFps · DiagnosticoVFX
(overlay **F8**) · VFXCoreSystem (Janitor) · RayoSistema (el pozo de 24 rayos) · ColaSierpeSistema
(el fondo del jefe) · ParticleManager/Buffer/Presets (12 texturas).
**Globals** (6): ShardLevelItem · GlobalNPCXP · OleadaNPC · CosmicProjectileFX · TestAdvancedFX ·
TooltipToggleItem. **ModPlayers** (6): TestingPlayer · Shard · Sellos · Cosmetic · Ocaso · Apuestas.
**Config** (2): AethonConfig (cliente: XP/notis/debug) + AethonConfigServidor (servidor:
`EventoHambreGrimorio` ON por defecto, `XPMultiplier`).

## EL STACK VFX (67 archivos — TODO de la casa)

VFXCore (núcleo de quads, `Hash01` determinista, presupuesto 24 000 quads adaptativo) ·
**RayoLib** (puerto 1:1 del LightningGenerator de vanilla 1.4.5 + RayoSistema) ·
**RayoStrip** (tira de triángulos BasicEffect, 16 384 vértices, perfil gaussiano por vértice) ·
**StormLib** (árbol fractal + PerlinBolt fBm + pila de 6 pasadas) · BoltRenderer ·
LumenLib (luz) · EstelaLib (ribbons) · OndaLib (ondas de choque) · PyraLib (fuego) ·
RiftLib (desgarros) · SigiloLib (signos) · SierpesLib · TajoLib · EspectroLib · AuraLib (auras de
llama) · BrumaFX (humo flipbook) · AuraPortadorHalo · AethonSierpeArte (el arte del jefe final) ·
RuneSunRenderer + BlackHoleLensSystem (lente a pantalla completa) + ~20 renderers dedicados.
Detalle por librería → DISEÑO_DEL_MOD.md §10-11.

---

## LA ERA v6.90 (LA CONSAGRACIÓN — la ley de la ofrenda y el taller de efectos)

> **La fuente de la verdad**: `Content/` v6.90 + el checklist de STABLE-SNAPSHOT v6.90.

- **LA LEY DE LA OFRENDA**: el arma **O** los materiales — BASTA UNO DE LOS DOS (el grimorio se queda con la primera opción que alimentes) y TODO lo que come debe estar **CONSAGRADO por el altar** (las esencias exentas: ellas SON la esencia del Eterno).
- **EL ALTAR DE LA CONSAGRACIÓN** (antes «de la corrupción» — el renombrado cósmico): acepta ARMAS y MATERIALES; el arma gana +5 % a TODO + su etiqueta «(Consagrado)», el material SOLO la etiqueta. La ventana: el precio a la vista antes de pulsar, el resultado por tipo, la leyenda del sello. Sprite NUEVO (el método del reel: 2 rondas de borrador-render-mirar-refinar con VLM).
- **LAS REPETICIONES +1**: el Señor de la Luna ×2..×7 (+1 por repetición) y la cima ×10; contadores acumulativos (cada muerte cuenta para todas las puertas venideras).
- **EL HITO DEL ESCUADRÓN = DAÑO DE INVOVACIÓN** (+10/15/20/25/30 % cada 20 niveles — el rayo, los conjuros y LA IRA lo cobran vivo; el ×1,25 horneado escondido murió: la auditoría de las stats).
- **EL HITBOX DEL HAZ = SU SILUETA** (StripWidth en el punto del objetivo — si lo ves, te pega) y **EL VELO DE PERLIN** del ancho exacto (perfil V de la rampa horneado en el ruido).
- **LA MIRADA DEL ETERNO SIN PICOS** (4 causas curadas contra el original: el collar de bolas del bloom tileado, el fundido final invertido, el ruido crudo/lento, la raíz desalineada) y **EL RELICARIO QUE RESPONDE** (equipado o en la bolsa + el aura por fin VISIBLE en su lote aditivo propio).
- **LA TECLA B DESDE LA BOLSA**, **EL LIBRO DEL ETERNO** (1 % criaturas · 1 % cofres · 2 %/2 % en la mazmorra) y **EL TALLER DE EFECTOS** (la investigación de los reels: El Sigilo del Eterno · Las Llamas de Sombra · El Eclipse Vivo — 0 maná, LA BOLSA del Taller, bolsa 27).

## LA ERA v6.89 (EL DESPERTAR DE MAGIA PURA — verificado contra el código)

> **La fuente de la verdad**: `Content/` v6.89 + el checklist de STABLE-SNAPSHOT v6.89.

- **EL DESPERTAR** (la escalera rehecha): armas MÁGICAS/INVOCADOR por era (SIN látigos/melee/rango) — los datos EXACTOS 1.4.4 del propio juego decompilado; materiales de SUS recetas (el doble, redondeado, mínimo 20: «20 de oro O 20 rubíes»), UNA opción bloqueada por la mesa; consume UNA de las alternativas; SIN corrupción (el arma tal cual — la corrupción vive en el ALTAR). LA LISTA COMPLETA: §15 del DISEÑO_DEL_MOD (las CIEN filas).
- **LA ENTREGA DEL LIBRO**: al 19, el Eterno escupe su Libro Corrupto a la bolsa (la puerta del 20 nace con su llave).
- **LA ESENCIA DEL ETERNO**: el renombrado cósmico (antes «esencia corrupta» — la carta del bioma se devolvió).
- **EL NOMBRE CORRUPTO**: UN color (RarityCorrupta, negro tinta) en inventario Y barra rápida; EL PRECIO de corromper: esencias según calidad+daño+tipo+stats (altar y receta, la MISMA fórmula).
- **LA TECLA B**: las mascotas (Aethon Menor + Grimorio Menor) con buffType (sus RANURAS viven) + detour de QuickBuff — SOLO si están EQUIPADAS, jamás desde el fondo de la bolsa; la tecla como era (pociones igual).
- **EL HAZ**: la bruma del centro LLENA el interior del anillo íntimo (2,02 × rCorona) con el PUNTO DE LUZ exacto del nacimiento; el calor de la prueba C son CUATRO SEGMENTOS sobre la geometría real (solo el proyectel) + EL VELO DE PERLIN (capa nueva semitransparente al mismo ancho del cuerpo, encima de todo).
- **LAS ARMAS**: borradas las nuevas (quedan los clickers + La Mirada del Eterno + el Relicario + los Grilletes + el altar); el ÁRBOL vuelve al CÓDICE con la economía por despertar (3 + despertar/5). LA MIRADA DEL ETERNO: la geometría del original de la referencia (arranque redondo √(1,001−x²) — sin picos, fundidos por nodo, el muzzle con el factor mágico cos01(t·85)); EL RELICARIO: el detour de DrawInfernoRings con las texturas del original (destello y resplandor) y el CD naciendo al pulsar (45 s de ciclo).

## LA ERA v6.85-v6.88 (el inventario ACTUAL — verificado contra el código)

> **La fuente de la verdad**: `Content/` v6.88 (470 clases) + los checklist de
> `STABLE-SNAPSHOT.md`. Esta sección resume lo que la era añadió al inventario.

- **EL DESPERTAR 100** (`DespertarGrimorio`): escalera de armas alineada con la progresión
  de la wiki (cada arma y lingote OBTENIBLE en la era de su jefe) · LA CAZA con puertas de
  jefe (Duque Fishron en 37 · Aethon custodia la 100 con LA CIMA COMPUESTA: Aethon ×1 +
  Moon Lord ×10 + Emperatriz ×5 · repeticiones 63-99) · monedas 100 cobre → 100 platino
  (exponencial g=10^(6/99)) · LA OFRENDA MINERAL VARIADA (mezcla de 2-3 minerales de la
  era, 18+nivel/5 por cabeza) · LOS HITOS DEL ESCUADRÓN cada 20 niveles en porcentaje
  (20:+10 % · 40:+15 % · 60:+20 % · 80:+25 % · 100:+30 % — `MultEscuadron`).
- **EL LIBRO CORRUPTO** (la puerta del 20): 1% libros de mazmorra · 1% cofres · 50% Muralla
  de Carne · 1 por jugador para siempre · el grimorio lo devora del suelo (despertar < 20).
- **EL ALTAR DE LA CORRUPCIÓN** (v6.88: YA sin vínculo con el grimorio): corrompe ARMAS —
  el nombre viste «(Corrupto)» EN NEGRO + el velo morado-negro pulsante en el inventario
  (`PostDrawInInventory`) + el aura doble glow en el suelo. La economía de puntos murió.
- **EL LIBRO AZUL + EL ÁRBOL DE HABILIDADES** (v6.88: el árbol YA NO es del grimorio):
  arma de tinta azul con su perno `LibroAzulDescarga` (daño 12 + nivel·2) · los puntos del
  árbol nacen del NIVEL DEL LIBRO AZUL (nivel = 1 + bajas/8, tope 100; puntos = 3 +
  nivel/5) · los 10 nodos + KEYSTONE solo viven mientras se LLEVA el arma · el Códice de
  Habilidades fue BORRADO (su bolsa entrega el Libro Azul).
- **EL ESCUADRÓN ETERNO** (persistente + mejora masiva): los ojos sobreviven a la muerte y
  a salir de la partida — y v6.88 los guardó con LA GUARDIA DEL DUEÑO: la fotografía solo
  se guarda y solo resucita si ESTE personaje lleva su Grimorio del Eterno.
- **LOS CINCO ITEMS DE LAS INVESTIGACIONES** (v6.85): Relicario del Avispón (aura solar,
  tecla L) · Sol en Miniatura (minion dorado) · Colmillo Dorado (rayos del cielo) ·
  Mirada del Eterno (rayo canalizado) · Grilletes de Andrómeda (escudo por maná) — todos 0 maná.
- **LAS DOS BANDAS SONORAS DEL FESTÍN** (v6.88): «El Festín Desatado» (152 BPM) y «La
  Oleada Final» (164 BPM) — cada oleada tira una moneda y suena UNA de las dos al azar.
- **EL RITUAL POR LIGAS** (v6.88): la bruma NORMAL (la del bastón, 1 anillo) corre con los
  agujeros normales; la bruma ASCENDIDA (doble círculo rúnico) SOLO con los otros
  Ascendidos (despertar 60+). Dos tipos diferentes en ligas diferentes.
- **LA MASCOTA Y LA TECLA B** (v6.88): AethonMenor + GrimorioMenor ya SIN `Item.buffType` —
  el imán del QuickBuff murió: el buff nace solo con el CLIC del ítem.

## Testing

- **TestingPlayer**: al entrar al mundo (1 jugador) entrega las **18 bolsas** + 99 Training Dummies.
- **F8** = panel de diagnóstico VFX del probador (fps, presupuesto, lotes).
- Ítems de prueba (marcados "eliminar antes de release"): LevelUpTester (+10 niveles),
  CarnadaDelGrimorio (furia a la carta: clic der cicla 1..11), BossSummonBag (999 invocadores).
- **Cadena de verificación de la casa**: build headless 0/0 → `.tmod` auditado (tabla byte a byte) →
  DLL inspeccionada → servidor headless carga sin excepciones (COMPILACION.md).

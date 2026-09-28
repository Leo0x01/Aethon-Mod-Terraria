# AethonMod — Características e Inventario del Contenido

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
- **Localización**: es-ES (2.289 líneas) + en-US (2.290 líneas), hjson SIMÉTRICOS
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
| **Colmillo de Vena Trueno** (firma Coralite) | 155/30 | **Trío 1 naranja + 2 amarillos** con recada 4 t, parpadeo 50 %, caída telegrafiada 26 t, golpe al aterrizar (radial 130 + cadena 3) |
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

## Testing

- **TestingPlayer**: al entrar al mundo (1 jugador) entrega las **18 bolsas** + 99 Training Dummies.
- **F8** = panel de diagnóstico VFX del probador (fps, presupuesto, lotes).
- Ítems de prueba (marcados "eliminar antes de release"): LevelUpTester (+10 niveles),
  CarnadaDelGrimorio (furia a la carta: clic der cicla 1..11), BossSummonBag (999 invocadores).
- **Cadena de verificación de la casa**: build headless 0/0 → `.tmod` auditado (tabla byte a byte) →
  DLL inspeccionada → servidor headless carga sin excepciones (COMPILACION.md).

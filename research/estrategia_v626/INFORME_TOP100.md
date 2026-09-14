# INFORME TOP 100 MODS MÁS POPULARES DE tModLoader — v6.26
## Task 42-c · Agente: general-purpose (investigación top 100 mods populares)

**Misión:** "investigar los 100 mods más populares para ver qué se puede aprender" para
AethonMod (arsenal cósmico 100% procedural: soles rúnicos, agujeros negros, rayos, humo,
librerías VFX propias).

**Estrategia de presupuesto:** NO se re-investigó lo ya cubierto por los informes previos
(`research/humo_v625/INFORME_MODS_HUMO.md` 23 fuentes VFX/humo, `ANALISIS_HUECOS.md`
gap-analysis, `estrategia_v626/INFORME_BIOMAS.md`, `INFORME_DESGARRO_REALIDAD.md`,
`busquedas_web_42a/42b/42d/`). Este informe se centra en lo que esos informes NO dan:
**el mapa del mercado** (quién es popular, cuánto, por qué, y qué significa para nosotros).

### Método y fuentes
| Tipo | Detalle |
|---|---|
| **Scrape real del Steam Workshop (API + browse)** | Top-120 por suscriptores actuales → `top120_subscribers.tsv` (rank, subs, favoritos, lifetime, tags, tamaño); detalles de 21 "notables" de contenido/clases → `steam_details_notables.json`; 9 mods de nicho VFX → `steam_details_vfx.json`; 17 trending → `steam_details_trending.json`; descripciones de 16 mods clave → `desc_*.json` |
| **Búsquedas web** | ~38 del intento previo de esta misma task (JSONs 01-38 en esta carpeta) + **6 frescas** de este reintento: Wikszilla×2, Spirit Reforged, Stellamod, Shadows of Abaddon, mods estilo anime/Genshin (39-44) |
| **Informes previos del proyecto** | Se citan números ya verificados (Calamity particles, Everglow LUTs, WoTE shockwave, MEAC trails...) sin re-investigar |

> Convención: **subs = suscriptores ACTUALES** (los que lo tienen instalado hoy); lifetime =
> acumulados históricos. Números reales sin marca; **~X = estimación** documentada como tal.

---

# 0. NÚMEROS MACRO DEL ECOSISTEMA (calculados sobre el scrape real)

- **El top-120 suma 200,2 millones de suscriptores actuales.** El top-10 concentra 69,2M (35%).
- **QoL/Utilities: 82 mods = 137,3M subs (69% del volumen).** El top del workshop es una
  lista de comodidades, no de contenido.
- **"new content": 33 mods = 66,1M subs (33%).** El contenido vende profundo: Calamity sola
  9,74M y su ecosistema (mods con "Calamity"/"Infernum" en el nombre) **44,7M subs = 22% de
  TODO el top-120**.
- **Librerías/frameworks: 16 mods = 39,2M subs.** Luminance 4,5M, Subworld Library 5,0M,
  InnoVault 2,0M — una lib usada por un mod grande hereda su audiencia.
- **Audio: 10 mods = 28,3M.** Calamity Mod Music (9,28M) tiene el **95% de la base** del mod
  principal — la música como mod separado casi duplica el alcance total de la marca.
- **Traducciones/localización: 19 entradas = 33,7M subs (17% del top-120!)** — el
  segundo "producto" más grande del workshop es hacer accesible lo que ya existe.
- **Visual Tweaks: 26 mods = 26,2M.** La estética como standalone vende (Lights and
  Shadows 3,13M, Loot Beams 1,0M).
- **Tamaño no es freno:** mediana de los grandes mods de contenido ≈ **276 MB**; máximos:
  Lunar Veil Legacy **928 MB**, Stars Above 851 MB, Vanilla Calamity Music 910 MB.
- **Hecho de mercado 2026:** **Calamity CESÓ su desarrollo (ago-2026)** con >9M de
  suscriptores (Wikipedia; equipo de 30+ voluntarios según su Patreon) — hay una audiencia
  de millones de jugadores de contenido cósmico/endgame **sin sucesor**.

---

# 1. TABLA MAESTRA — LOS MÁS POPULARES (datos reales del Workshop, orden por subs)

## 1.1 El Top-120 oficial (scrape directo)

| # | Mod | Categoría | Subs | Qué aporta |
|---|---|---|---|---|
| 1 | Calamity Mod | CONT gigante | 9.74M | 27 jefes + 5 minibosses, >2000 ítems, biomas, endgame propio; mod de referencia |
| 2 | Calamity Mod Music | AUDIO | 9.28M | Banda sonora propia (obligatoria para Calamity) — el 95% de su base |
| 3 | Recipe Browser | QoL/TOOL | 8.33M | Buscador de recetas in-game universal (también de otros mods) |
| 4 | Boss Checklist | QoL | 8.20M | Checklist de progreso de jefes — LA capa de descubrimiento del ecosistema |
| 5 | Magic Storage | QoL/CONT | 7.12M | Red central de almacenamiento con búsqueda — resuelve el pain #1 del juego |
| 6 | absoluteAquarian Utilities | LIB | 6.74M | Lib de utilidades (dependencia heredada de Calamity/Catalyst) |
| 7 | AlchemistNPC Lite | QoL | 5.54M | NPCs tienda de pociones/buffs sin granja |
| 8 | Ore Excavator (Veinminer) | QoL | 4.75M | Minar vetas enteras de golpe |
| 9 | Subworld Library | LIB | 5.02M | Framework de submundos — usado por mods de historia/dimensiones |
| 10 | Luminance | LIB | 4.52M | Framework VFX/particles de WoTG (metaballs, primitivas, atlas) |
| 11 | Fargo's Mutant Mod | QoL/TOOL | 4.12M | NPC tienda + boss rush + utilidades de finales de partida |
| 12 | Quality of Terraria | QoL pack | 4.22M | Pack de decenas de QoL ajustables por config |
| 13 | Boss Cursor | QoL | 3.95M | Resalta jefes fuera de pantalla |
| 14 | Calamity's Vanities | CONT(vanity) | 3.56M | Mascotas/pets/muebles del universo Calamity |
| 15 | Wing Slot Extra | QoL | 3.24M | Slot de alas separado del acceso |
| 16 | Lights And Shadows | VFX | 3.13M | Motor de iluminación dinámica mejorado (luz que se mueve real) |
| 17 | Catalyst Mod | CONT addon | 2.77M | "New game+" para Calamity: superbosses (Astrageldon) |
| 18 | Calamity zh Translation Patch | TRAD | 3.12M | 汉化补丁 — la puerta de entrada del mercado chino |
| 19 | Census — Town NPC Checklist | QoL | 2.58M | Checklist de NPCs del pueblo (habitaciones/logros) |
| 20 | Thorium Mod | CONT Vanilla+ | 2.31M | 2.300+ ítems, 11 jefes, 3 clases nuevas (Bard: 9 sets, 56 acc, 30 buffs, 110 ítems) |
| 21 | Fargo's Souls Mod | CONT jefes | 2.36M | Encantamientos + Souls + Eternity Mode (remix de dificultad total) |
| 22 | Calamity: Wrath of the Gods | CONT jefes | 2.57M | 3 jefes con VFX extremos + personaje narrativo (lleva config de fotosensibilidad en la descripción) |
| 23 | Cheat Sheet | TOOL | 2.13M | Panel de trampas/utilidades de dev in-game |
| 24 | HERO's Mod | TOOL | 2.08M | El panel de administración clásico (cheats, spawns, world edit) |
| 25 | LuiAFK Reborn | QoL | 2.11M | QoL masivo de inventario/crafteo (auto-stack, etc.) |
| 26 | Auto Trash | QoL | 1.96M | Reglas de auto-basura de loot |
| 27 | Structure Helper | LIB | 2.00M | Framework de generación de estructuras |
| 28 | DAYBREAK | LIB (CN) | 2.25M | Framework chino de contenido/base de mods chinos |
| 29 | SilkyUI | LIB/UI (CN) | 2.32M | Framework de UI moderno (menús escalables) |
| 30 | Calamity Mod Infernum Mode | CONT remix | 2.05M | Re-imagina TODOS los patrones de jefes Calamity (difficulty remix como producto) |
| 31 | InnoVault | LIB (CN) | 2.00M | Lib base de Coralite (PRT particles, trails) — pública |
| 32 | Auto Reforge | QoL | 1.85M | Reforja en lote con reglas |
| 33 | Vanilla Calamity Mod Music | AUDIO | 1.69M | Versión alternativa de la OST |
| 34 | Basic Automated (Vein) Mining | QoL | 1.67M | Minado automatizado (competidor de Ore Excavator) |
| 35 | Colored Calamity Relics | VFX | 1.75M | Reliquias coloreadas — cosmético puro con 1,75M |
| 36 | Better Boss Health Bar | QoL/VFX | 1.64M | Barra de vida de jefes con info extra |
| 37 | High FPS Support | VFX/perf | 1.68M | Desbloquea >60 FPS — el "performance mod" del top |
| 38 | ArmamentDisplay | QoL | 1.56M | Muestra armas equipadas del jugador |
| 39 | Shorter Respawn Time | QoL | 1.55M | Menos tiempo de respawn |
| 40 | Calamity Overhaul (CN) | OVER/CONT | 1.61M | Rework melee estilo anime sobre Calamity (mod chino) |
| 41 | Which Mod Is This From? | QoL | 1.50M | Dice de qué mod es cada ítem — la capa de atribución |
| 42 | Max Stack Plus Extra | QoL | 1.35M | Stacks ampliados |
| 43 | Calamity: Hunt of the Old God | CONT addon | 1.60M | Contenido de caza/post-game para Calamity |
| 44 | Better Zoom | QoL | 1.30M | Zoom de cámara libre |
| 45 | Unofficial Calamity Whips | CLASS | 1.22M | Látigos para summoner en Calamity — clase como addon |
| 46 | The Stars Above | CONT | 1.11M | Starfarers (compañeros), >100 armas estilo anime/jRPG, 851 MB |
| 47 | More Accessory Slots | QoL | 1.24M | +Slots de accesorios |
| 48 | Terraria Overhaul | OVER | 0.99M | Remake de game-feel: ataques direccionales, dodge-roll cancel, clima, física |
| 49 | Autofish | QoL | 1.08M | Pesca automática |
| 50 | Fargo's Music Mod | AUDIO | 1.03M | OST propia para Fargo's |
| 51 | Calamity — Fargo's Souls DLC | CONT crossmod | 1.11M | Integración oficial entre las dos marcas |
| 52 | Dialogue Panel Rework | QoL/UI | 0.96M | Diálogos con panel mejorado |
| 53 | Calamity Ranger Expansion | CONT addon | 0.99M | Clase ranger ampliada para Calamity |
| 54 | Particle Library | LIB | 0.96M | Lib de partículas GPL usada por Redemption/SoA/LunarVeil |
| 55 | Loot Beams | VFX | 1.00M | Haces de luz estilo Diablo sobre loot raro |
| 56 | Shop Expander | QoL | 1.00M | Tiendas más grandes |
| 57 | Improved Movement Visuals | VFX | 0.89M | Animaciones de movimiento pulidas |
| 58 | Heart Crystal & Life Fruit Glow | VFX | 0.93M | Brillo sobre objetos coleccionables |
| 59 | HP Awareness | QoL | 0.80M | % de vida exacto de jefes/enemigos |
| 60 | Calamity Mod Extra Music | AUDIO | 0.86M | OST extra opcional |
| 61 | Begone, Evil! | QoL | 0.94M | Desactiva la expansión de biomas malos |
| 62 | Better Blending | VFX | 0.85M | Mezcla de sprites sin bordes duros |
| 63 | Angler Shop | QoL | 0.87M | Compra recompensas del pescador |
| 64 | Magic Storage 拼音搜索 (CN) | TRAD/QoL | 0.94M | Búsqueda pinyin sobre Magic Storage — un UX-layer chino con casi 1M |
| 65 | Gravitation Don't Flip Screen | QoL | 0.76M | No voltea pantalla al invertir gravedad |
| 66 | Magic Builder | QoL | 0.90M | Construcción asistida |
| 67 | Biome Titles | VFX | 0.84M | Cartelas cinematográficas al entrar en biomas |
| 68 | CoolerItemVisualEffect | VFX (CN) | 0.67M | Efectos visuales de ítems (glow/animación) |
| 69 | OmniSwing | QoL | 0.76M | Golpes en todas direcciones |
| 70 | Infernum Mode Music | AUDIO | 0.92M | OST del remix de dificultad |
| 71 | Fargo's zh Patch | TRAD | 0.80M | 汉化 de Fargo's |
| 72 | Smarter Cursor | QoL | 0.78M | Cursor inteligente |
| 73 | Antisocial | QoL | 0.75M | Accesorios "sociales" en cualquier slot |
| 74 | Bosses As NPCs | QoL/vanity | 0.74M | Jefes como NPC-town tras derrotarlos |
| 75 | LogSpiral's Library | LIB | 0.70M | Lib de primitivas/trails en espiral |
| 76 | Wombat's General Improvements | QoL | 0.77M | Pack de mejoras generales |
| 77 | Concise Mods List | QoL | 0.73M | Lista compacta de mods activos |
| 78 | Atmospheric Torches | VFX | 0.67M | Antorchas con partículas ambientales |
| 79 | Consolaria | CONT | 0.67M | Contenido de las versiones de consola (exclusivo de años perdidos) |
| 80 | zzp198's WeaponOut Lite | VFX/QoL | 0.66M | Ver el arma en la mano siempre (puerto mantenido) |
| 81 | Calamity Vanity zh | TRAD | 0.69M | 汉化 de vanities |
| 82 | Project tRU | TRAD (ru) | 0.84M | Infraestructura de declinación rusa para mods |
| 83 | Fancy Lighting | VFX | 0.59M | Iluminación suave tipo filtro |
| 84 | Terraria Ambience | AUDIO | 0.54M | Sonido ambiental (viento, cuevas, lluvia) |
| 85 | Spirit Classic | CONT | 0.54M | 1.300+ ítems, 8 jefes, 3 eventos, 3 biomas (versión clásica congelada) |
| 86 | Calamity Fables | CONT addon | 0.67M | Historia/cuentos de Calamity |
| 87 | Terraria Ambience API | LIB | 0.57M | API para el mod de ambiente |
| 88 | Call of Void | CONT | 0.70M | Mod de contenido temático void/eldritch |
| 89 | Summoner UI | QoL | 0.64M | UI de minions con slots |
| 90 | Shared World Map | QoL | 0.61M | Mapa compartido en multi |
| 91 | Syla's Resource Pack Library | LIB | 0.67M | Resource packs como mods |
| 92 | DPSExtreme | QoL | 0.59M | Medidor de DPS |
| 93 | Starlight River | CONT | 0.51M | Jefes multi-fase, VFX por vertex-buffer GPU, pixelation system — el "mod artístico" |
| 94 | tML Language Pack Fix | QoL | 0.66M | Arreglo de language packs |
| 95 | Armor Modifiers & Reforging | QoL | 0.53M | Prefijos también en armaduras |
| 96 | Friendly NPCs Don't Die | QoL | 0.60M | NPCs inmunes a eventos |
| 97 | WorldGen Previewer | QoL | 0.56M | Vista previa del mundo antes de crearlo |
| 98 | Impact Library | LIB | 0.53M | Lib de física/impacto |
| 99 | Helpful NPCs | QoL | 0.55M | NPCs con utilidades extra |
| 100 | Fargo's Best of Both Worlds | QoL/worldgen | 0.51M | Mundos con ambos biomas malos |
| 101 | Colored Damage Types | VFX | 0.54M | Colorea daño por tipo |
| 102 | MEAC — 原版内容重置 demo (CN) | OVER/CONT | 0.45M | "Reset de contenido vanilla" chino: reworks visuales/melee extremos (EoL rework, warps) |
| 103 | Fargo's Soul Mod Extras | CONT addon | 0.47M | Contenido extra para Souls |
| 104 | Realistic Sky | VFX | 0.54M | Cielo/nubes realistas con shaders |
| 105 | Recipe Browser && Magic Storage | QoL combo | 0.46M | Bundle de los dos QoL top |
| 106 | Mod of Redemption [BETA] | CONT | 0.42M | Exploración/dungeon focus, 13 jefes + 12 minibosses, 579 MB |
| 107 | No Pylon Restrictions | QoL | 0.53M | Pilones sin restricciones |
| 108 | Infernum Master/Legendary Patch | QoL | 0.44M | Ajustes de dificultad del remix |
| 109 | Secrets Of The Shadows (SOTS) | CONT | 0.43M | Exploración de mazmorras con secretos, 276 MB |
| 110 | Rainbow Master: Colored Relics | VFX | 0.54M | Reliquias arcoíris (CN/es) |
| 111 | Better Caves | QoL/worldgen | 0.52M | Cuevas más variadas |
| 112 | BossHealthBar (Boss血条) (CN) | QoL | 0.41M | Barra de jefe china |
| 113 | No More Tombs | QoL | 0.50M | Sin lápidas al morir |
| 114 | OreExcavator 汉化 | TRAD | 0.47M | Traducción del veinminer |
| 115 | LuiAFKReborn 汉化 | TRAD | 0.49M | Traducción de LuiAFK |
| 116 | Teleport All NPC Home | QoL | 0.47M | Teleporta NPCs a casa |
| 117 | Craftable Calamity Items | QoL | 0.44M | Hace crafteables ítems de drop |
| 118 | Compare Item Stats | QoL | 0.42M | Comparación de stats de ítems |
| 119 | Boss Intros | VFX | 0.43M | Intro cinematográfica de jefes |
| 120 | Calamity Flamethrowers | CONT addon | 0.38M | Lanzallamas para Calamity |

## 1.2 Más allá del top-120 — notables del nicho contenido/clases/VFX (datos reales)

| Mod | Categoría | Subs | Qué aporta |
|---|---|---|---|
| WeaponOut Lite (original) | VFX/QoL | 0.47M | Arma visible en mano — el clásico de "presencia visual" |
| Summoners' Association | CLASS/QoL | 0.38M | UI + calidad de vida summoner |
| The Clicker Class | CLASS | 0.34M | Clase de clic con upgrades/efe |
| The Amulet of Many Minions | CLASS | 0.33M | Mascotas de combate con IA variada |
| VFX+ | **VFX puro** | **0.31M** | **Rework del VFX (y SFX) de casi TODA arma mágica vanilla** — la prueba de mercado de nuestro nicho |
| Lunar Veil Legacy (CN) | CONT | 0.21M | 928 MB de contenido, partículas/shaders propios; Stellamod 2.0 en camino |
| Reduced Grinding | QoL | 0.21M | Menos grind de drops/eventos |
| DragonLens | TOOL dev | 0.19M | El "dev tool" moderno del ecosistema |
| LuiAFK DLC | QoL | 0.18M | Extensión de LuiAFK |
| Everglow 流光无际 [Demonstration] (CN) | CONT demo | 0.10M | Mod chino EXPLORACIÓN con el mejor VFX de humo del ecosistema (heat LUTs) |
| Verdant | CONT | 0.09M | Bioma/jefes de planta, artesanal |
| Orchid Mod | CLASS | 0.076M | Guardian (paredes/pavises) + Shapeshifter (formas) |
| Polarities | CONT | 0.056M | Mod de física/magnetismo, 6 jefes, muy "autor" |
| Calamity VFX+ | VFX addon | 0.040M | VFX+ aplicado a Calamity |
| Coralite 珊瑚石 (CN) | CONT | 0.044M | 7 jefes, armas voladoras, sistema PRT (InnoVault) |
| Exxo Avalon Origins | CONT | 0.038M | El contenido clásico de 1.2/1.3 |
| Ancients Awakened [BETA] | CONT | 0.034M | 23 encuentros de jefes (9 preHM, 4 HM, 13 post-ML) |
| VFX+ Boss Addon | VFX addon | 0.035M | VFX+ para jefes |
| Elements Awoken | CONT | 0.025M | Mod elemental clásico post-ML |
| W1K's Weapon Scaling | QoL/melee | 0.012M | Escala armas viejas al tier actual |
| Aequus | CONT | 0.007M | Contenido experimental compacto |
| The Enigma Mod | CONT | 0.006M | **Time Stop** como mecánica central |
| Vanilla Slash VFX | VFX | 0.005M | Rework visual de slashes |
| Crit VFX | VFX | 0.004M | VFX de críticos |
| Hellkate SFX+VFX | VFX | 0.003M | SFX+VFX de un jefe |
| Vibrant Reverie (W1K redux) | CONT | 0.005M | Melee anime redux |
| Ultra's Spirit Mod | CONT | 0.002M | Fork del Spirit clásico |
| Game Feel: Combat Feedback | VFX | **43** | Pitch abstracto "game feel" — **caso de fracaso** (lección 15) |
| Every weapon deserves to feel distinct | VFX | 550 | Pitch abstracto similar, mismo destino |

## 1.3 Fuera del Workshop / distribución alternativa (estimaciones documentadas)

| Mod | Categoría | Popularidad | Nota |
|---|---|---|---|
| Wikszilla | CONT/VFX | ~0,2-0,4M (est.) | Sin página Steam encontrada (4 búsquedas: API browse vacía, web confunde con wiki de kaijus). Referencia de DISEÑO (armas anime VFX-heavy), no competidor de mercado directo |
| Spirit Reforged (Revamped) | CONT | ~0,1-0,3M (est.) | Secuela/remaster del Spirit Mod — solo Mod Browser tML, no workshop |
| Shadows of Abaddon (ex-SacredTools) | CONT | ~0,5-1M histórico (est.) | 800+ ítems, 11 jefes, "Empress Den" — **1.3 solamente**, no portado a 1.4 (confirmado por foros) |
| Stellamod (Stars Above 2.0) | CONT | en desarrollo | Sucesor de The Stars Above; avances en GitHub/Discord ("90+ armas" en updates) |
| Tremor Remastered | CONT | ~0,5-1M histórico (est.) | El gigante de la era 1.3 del Mod Browser |

> Conteos de la tabla 1.1+1.2: **~150 mods documentados**, de los cuales **~120 con números
> reales** del workshop. La selección cubre el ~99% del volumen del top-120.

---

# 2. ANÁLISIS POR CATEGORÍA

## 2.1 QoL/Utilities — el 69% del mercado (137,3M subs, 82 mods)
El top-4 del workshop entero son QoL (Recipe Browser, Boss Checklist, Magic Storage,
SerousCommonLib). **Nada que aprender de diseño de contenido aquí, pero sí de distribución:**
los QoL universales son "instalados por defecto" por casi todo jugador moddeado — ser
compatible con ellos (recetas legibles por Recipe Browser, jefes registrados en Boss
Checklist, NPCs en Census, ítems con origen claro para WMITF) es **marketing gratis a
8M+ de instalaciones**.

## 2.2 Contenido gigante — 33 mods, 66,1M subs
- **Calamity (9,74M)**: el estándar. 27 jefes, >2000 ítems, biomas propios, 3 niveles de
  dificultad propios + remixes de terceros (Infernum 2,05M, Calamity Overhaul 1,61M,
  Catalyst 2,77M). **Cesió desarrollo en ago-2026** — vacuum de mercado.
- **Thorium (2,31M)**: la vía "Vanilla+": 11 jefes, 2.300+ ítems y **3 clases con recorrido
  COMPLETO** (Bard: 9 armaduras, 56 accesorios, 30 buffs, 110 ítems — documentado en su wiki).
- **Fargo's Souls (2,36M)**: la vía "sistemas + jefes de reto": Eternity Mode re-tunea el
  juego entero; Masochist como tier extra; boss rush en Mutant.
- **The Stars Above (1,11M)**: la vía "narrativa/jRPG": Starfarers (compañeros con
  personalidad), armas con kits activos, >100 armas, 851 MB.
- **Starlight River (0,51M)**: la vía "arte": jefes multi-fase, el mejor sistema de
  partículas GPU del ecosistema… y reviews marcadas por "severe performance issues and
  memory leaks" — **el límite del arte sin presupuesto de rendimiento**.

## 2.3 Overhauls de game-feel — Terraria Overhaul (0,99M), Calamity Overhaul (1,61M), MEAC (0,45M)
Overhaul NO añade contenido: remecha combate (ataques direccionales, dodge-roll con
cancelación de animación, charged attacks, clima/física) y llega a 1M. MEAC hace lo mismo
en chino para el melee visual (rework de la Emperatriz con warps de pantalla). **Lección:
la sensación de juego vende tanto como el contenido** — y es exactamente el terreno donde
nuestro arsenal procedural compite.

## 2.4 VFX puro — 26,2M subs en "visual tweaks", y la cola larga
Lights and Shadows (3,13M, motor de luz), Loot Beams (1M, un SOLO efecto), Better Blending
(0,85M), Biome Titles (0,84M), CoolerItemVisualEffect (0,67M), VFX+ (0,31M). **VFX+ es el
dato clave para nosotros: "rework visual de casi todas las armas mágicas vanilla" = 308k
subs sin una sola mecánica nueva.** Y el caso inverso: "Game Feel: Combat Feedback" (43
subs) demuestra que el MISMO producto con pitch abstracto muere. El mercado paga VFX
concretos, no conceptos.

## 2.5 Clases nuevas — Thorium-dentro-del-mod vs standalone
Clicker Class (0,34M), Summoners' Association (0,38M), Amulet of Many Minions (0,33M),
Unofficial Calamity Whips (1,22M — ¡clase como ADDON de marca!), Orchid (0,076M).
**Ninguna clase standalone supera a las clases DE un mod grande.** Las clases venden como
parte de un paquete con progresión completa (Bard) o como extensión de una marca
(Calamity Whips 1,22M).

## 2.6 Jefes/dificultad como producto
Fargo's Souls Eternity, Infernum (2,05M — un mod entero que SOLO remixa patrones de jefes
Calamity), Catalyst (2,77M — superbosses), Boss Intros (0,43M — solo la intro cinemática!),
Better Boss Health Bar (1,64M), Boss Cursor (3,95M). **"Jefes" es la categoría con más
productos satélite: la gente compra la EXPERIENCIA DE JEFE por piezas** (barra, cursor,
intro, remix, checklist).

## 2.7 Mods chinos — el segundo ecosistema completo
Dentro del top-120: DAYBREAK (2,25M), SilkyUI (2,32M), InnoVault (2,0M), MEAC (0,45M),
Calamity Overhaul (1,61M), CoolerItemVisualEffect, pinyin Magic Storage (0,94M) + 8
traducciones al chino (18M+ subs). Fuera: Lunar Veil (0,21M), Everglow (0,10M), Coralite
(0,044M). **Los chinos compiten en VFX/melee-animé al máximo nivel técnico** (Everglow y
Coralite tienen los mejores pipelines de partículas/humo vistos en humo_v625) y su mercado
es tan grande que una traducción de Calamity (3,12M) supera a Thorium entero.

## 2.8 Frameworks/librerías — 39,2M subs en 16 mods
Subworld Library (5,02M), Luminance (4,52M), InnoVault (2,0M), ParticleLibrary (0,96M),
LogSpiral's (0,70M). **Una librería no compite: se instala sola como dependencia.**
Luminance tiene 4,5M porque WoTG/Catalyst la exigen. Publicar nuestras libs VFX como mods
separados = instalar una "muestra gratuita" del motor gráfico de AethonMod en millones de
cargas de mods.

---

# 3. LOS ~35 MÁS RELEVANTES PARA AETHONMOD (qué hace mejor que nadie + qué adoptamos)

> Criterio de selección: mods de contenido/VFX/frameworks con lecciones directas para un
> arsenal cósmico 100% procedural con librerías VFX propias. Los números técnicos citados
> provienen de la investigación previa del proyecto (humo_v625/desgarro/biomas) — no se
> re-investigaron.

### 3.1 Calamity (9,74M) — el techo del género
**Mejor que nadie:** ecosistema de marca (15+ satélites = 44,7M subs), design de armas
(>2000 ítems con filosofía "wholly unique or meaningful upgrade"), DoG rift system
(grietas de realidad a 3 escalas: arma/telegraph/cielo). **Adoptamos:** el patrón
"ecosistema" (mods satélite propios), config de cap de partículas, y su filosofía de
arma (cada arma del arsenal = una mecánica, no un stat-stick).

### 3.2 Calamity Mod Music (9,28M) — el multiplicador silencioso
95% de la base del mod principal en un mod de audio separado. **Adoptamos:** "AethonMod
Music" como mod separado desde el día 1 (aunque arranque con 3-5 temas propios del
Sagrario/combate de Aethon).

### 3.3 Calamity: Wrath of the Gods (2,57M) — el mod de VFX como contenido
3 jefes con los VFX más extremos del ecosistema (metaballs de distorsión, shockwaves
full-screen). Su descripción EMPIEZA avisando de fotosensibilidad y config de tono.
**Adoptamos:** el aviso de fotosensibilidad en la descripción + slider de intensidad VFX;
el telegraph vertical de 4000px con falloff cuadrático (Lances) para LanzaAlba; shockwave
"de trabajo" con radio ideal 780px/15 ticks/smoothstep 0.01-0.09 para SupernovaProjectile.

### 3.4 Infernum Mode (2,05M) + Fargo's Eternity — el remix como producto
Un mod que SOLO reescribe patrones de jefes de OTRO mod tiene 2M. **Adoptamos:** idea de
"Modo Apoteosis" post-Aethon: mismos jefes, patrones remixeados + loot nuevo — segunda
jugabilidad con coste de contenido mínimo.

### 3.5 Thorium (2,31M) — la clase completa
Bard con contenido en TODOS los tiers (9 sets/56 acc/110 ítems). **Adoptamos:** regla de
"cobertura de progresión": cada rama del Fragmento Génesis (Distancia/Melé/Magia) necesita
un hito visible en cada banda de nivel vanilla (pre-hm, hm temprano/medio/tardío, post-ML).

### 3.6 The Stars Above (1,11M) — el arma como personaje
Starfarers + armas con kits y VFX estilo jRPG (es el pariente espiritual de nuestro
arsenal). 851 MB. **Adoptamos:** el patrón "arma con identidad": nombre + lore + kit de 2-3
comportamientos + VFX único; el Testigo puede vender "relatos" de cada arma del arsenal
(como los Starfarers narran).

### 3.7 Stellamod / Lunar Veil Legacy (0,21M, 928 MB) — el pipeline chino
ParticleSystem con cap 500, shader por partícula, ScreenTarget gestionado. **Adoptamos:**
su disciplina de caps duros + culling por pantalla (ya la tenemos en ParticleManager de
4000 structs — superado); su "legacy finalized + 2.0 en camino" = estrategia de liberar
una versión estable congelada mientras se desarrolla la siguiente.

### 3.8 Starlight River (0,51M) — arte GPU y su precio
Partículas por vertex buffer (10.000 máx), pixelation system RT a media resolución.
**Adoptamos:** su look "chunky" opcional (toggle de pixelación como estética del Sagrario);
su LECCIÓN negativa: presupuestar rendimiento (cap, pooling, sin fugas) o el review-bomb
te hunde.

### 3.9 Everglow [Demonstration] (0,10M) — la demo como producto
Un mod EXPLÍCITAMENTE etiquetado "[Demonstration]" con 96k subs. **Adoptamos:** publicar
el Arsenal Cósmico como "demo" jugable (nuestro STABLE-SNAPSHOT actual) mientras el bioma
y Aethon se cocinan — validation temprana + presencia en workshop.

### 3.10 MEAC (0,45M) — el rework visual de lo vanilla
Reset visual chino de contenido vanilla (EoL rework con warps de pantalla, 8-afterimage
trails con alpha 0.6−i/15 y squash 0.5/1.2). **Adoptamos:** el afterimage con squash como
estándar de todas las armas del arsenal (baratísimo: 8 draws, lee "rasguño de luz").

### 3.11 Coralite + InnoVault (0,044M / 2,0M) — la lib como distribución
El mod de contenido es pequeño pero SU LIBRERÍA (InnoVault) tiene 2M de subs porque otros
la requieren. **Adoptamos:** extraer BrumaFX/LumenLib/StormLib/EstelaLib a un mod
"AethonCore" público con README y ejemplos — instalar nuestro motor en millones de setups.

### 3.12 Terraria Overhaul (0,99M) — game-feel sin contenido
Dodge-roll cancel + ataques direccionales + charged. **Adoptamos:** micro-mecánicas de
game-feel para el Fragmento: "eco" (dash que deja afterimage y anula la animación del
bastón), cargas por mantener clic en las varas rúnicas (estilo charged bow).

### 3.13 VFX+ (0,31M) — la validación EXACTA de nuestro nicho
Rework VFX+SFX de casi toda arma mágica vanilla, 0 mecánicas nuevas, 308k subs.
**Adoptamos:** su pitch literal para nuestra descripción de workshop: "Rehace el VFX de
cada arma con shaders procedurales" + contador de armas reworkeadas. Su addon-model
(Calamity VFX+ 40k, Boss Addon 34k) = nuestros "packs" por bioma/clase.

### 3.14 WeaponOut Lite (0,47M) — presencia de arma
Ver el arma SIEMPRE. **Adoptamos:** DrawLayer de bastones rúnicos visibles en la espalda
(ya tenemos RuneCrownDrawLayer — extender a armas).

### 3.15 Lights and Shadows (3,13M) — luz dinámica
**Adoptamos:** luz emisiva REAL en soles/agujeros (ya AddLight; subir a "luz con color
animado por fase del arma" — p.ej. el agujero negro emite luz NEGRA absorbente con
deluminación circular — nadie lo hace).

### 3.16 Loot Beams (1,0M) — un solo efecto, 1M subs
**Adoptamos:** haces de loot cósmicos para drops del Sagrario (rayo vertical procedural con
nuestro BoltRenderer + identificación por rareza rúnica).

### 3.17 Biome Titles (0,84M) + Boss Intros (0,43M) — la cartela
**Adoptamos:** cartela "SAGRARIO HUECO" al entrar al bioma + intro de Aethon con título
de 2 líneas y flash — 2 sistemas <100 líneas cada uno, percepción de "mod grande".

### 3.18 Magic Storage (7,12M) + Recipe Browser (8,33M) — los reyes QoL
**Adoptamos:** integración (no competición): todas las recetas del arsenal limpias y
navegables; un "altar de resonancia" compatible con la red de Magic Storage.

### 3.19 Boss Checklist (8,20M) + Census (2,58M)
**Adoptamos:** registro de los 6 jefes + NPCs (Testigo, ecos) con iconos — es la tabla de
contenidos del mod ante 8M de jugadores.

### 3.20 Fargo's Mutant/Souls (4,12M/2,36M) — el meta-juego
Boss rush + souvenirs de TODOS los jefes. **Adoptamos:** "Oda del Testigo": boss rush de
los 6 jefes de AethonMod con remix final (todos a la vez, estilo Mutant).

### 3.21 Catalyst (2,77M) — el superboss como add-on
New game+ de Calamity. **Adoptamos:** "Aethon: Coda" — superboss post-Aethon (Aethon
Ascendido) en un mod satélite pequeño, igual que Catalyst separa su superboss.

### 3.22 Recipe Browser && Magic Storage bundle (0,46M) — el bundle
**Adoptamos:** pack de suscripción recomendado ("AethonMod + AethonCore + AethonMusic")
en la descripción, como hacen los top con sus collections.

### 3.23 Subworld Library (5,02M) — la puerta a dimensiones
**Adoptamos:** submundo "Biblioteca del Testigo" (una sala de estrellas con el códex de
armas) usando SubworldLibrary — el momento "wow" narrativo del mod.

### 3.24 Particle Library (0,96M) — lib GPL con 1M
Confirma el modelo lib-de-partículas. Nuestra diferencia: libs PROCEDURALES (cero PNG de
arte) — pitch único en el ecosistema.

### 3.25 Clicker Class (0,34M) — identidad de input
Toda una clase alrededor de UN input. **Adoptamos:** "runas de carga": mantener clic
carga el sol rúnico (ya semi-presente en StormRune) con UI radial procedural.

### 3.26 Orchid Mod (0,076M) — la clase audaz sin escala
Guardian/Shapeshifter complejísimos, 75k subs. **Lección:** mecánica de clase arriesgada
sin masa de contenido no escala — nuestra "absorción de armas" necesita masa para retener.

### 3.27 Enigma Mod (6k) — Time Stop
Congela enemigos/proyectiles/jugadores. **Adoptamos:** "Sinfonía Primordial" ya suena a
time-stop: fase final del arma = 60 ticks de tiempo congelado + shader cromático + dusts
suspendidos (coste bajo, momento memorable).

### 3.28 Polarities (0,056M) — el mod-autor
Física de magnetismo en TODO el mod. **Adoptamos:** su coherencia temática total: cada
arma del arsenal debe usar el MISMO lenguaje visual (nuestro sistema de paletas
VFXPalettes.cs) — identidad > cantidad.

### 3.29 Redemption (0,42M) + SOTS (0,43M) — exploración
Mazmorras con secretos. **Adoptamos:** el Sagrario con salas secretas selladas por
"selllos rúnicos" (mini-puzzles de luz) — contenido de exploración barato que alarga la
sesión sin nuevos jefes.

### 3.30 DragonLens (0,19M) — dev tool como producto
**Adoptamos:** nuestro "LevelUpTester" puede convertirse en herramienta pública
("AethonDevLens": spawner de VFX con sliders para testear las libs) — útil para OTROS
modders = visibilidad de nuestro motor.

### 3.31 Wikszilla (~est.) — el benchmark invisible
Armas con VFX estilo anime extremo, referencia recurrente de la comunidad. Sin página
Steam localizable (4 intentos documentados). **Adoptamos (de su reputación):** weapons
first, story never — cada trailer = un arma haciendo algo imposible.

### 3.32 High FPS Support (1,68M) — rendimiento como feature
**Adoptamos:** cap de partículas + "modo rendimiento" del BlackHoleLensSystem (reducción
de pases a 1) publicados como FEATURES en la descripción, no como opciones ocultas.

### 3.33 Better Boss Health Bar (1,64M)
**Adoptamos:** barra de Aethon con "escudos rúnicos" segmentados visibles (las fases como
anillos alrededor de la barra).

### 3.34 Consolaria (0,67M) — contenido perdido como oferta
Contenido de consola rescatado. **Adoptamos (meta):** rescate de features: nuestro códex
puede "archivar" armas de mods DESACTIVADOS (memoria de saves) — nadie lo hace.

### 3.35 Calamity's Vanities (3,56M) — el lore adorable
Pets/muebles = 3,56M. **Adoptamos:** mini-pets cósmicos (ya hay NebulaJellyfishMinion y
StellarComet — versiones vanity sin combate) + muebles rúnicos crafteables con Fragmentos
de Resonancia.

---

# 4. 15 LECCIONES ESTRATÉGICAS PARA AETHONMOD

1. **No compitas en QoL; compite en "VFX-first content".** El 69% del volumen del top-120
   es QoL — imbatible por un mod de contenido. Pero VFX+ demuestra (308k subs) que el
   rework visual puro de armas tiene mercado propio, y ahí NUESTRO motor procedural es
   top-3 del ecosistema. Posicionamiento literal para la descripción: *"Rehace cada arma
   cósmica con shaders y partículas 100% procedurales — cero sprites de arte."*
2. **Fragmenta la marca en módulos (modelo Calamity).** 15 de las 120 entradas del top
   llevan "Calamity" en el nombre (44,7M subs, 22% de todo el volumen). Estructura objetivo:
   `AethonCore` (libs VFX) + `AethonMod` (arsenal+bioma+jefes) + `AethonMusic` + después
   `Aethon: Coda` (superboss) y `AethonVanities`. Cada módulo suma suscriptores propios y
   degrada el riesgo (un módulo roto no tumba la marca).
3. **La música es un mod separado, no una feature.** Calamity Music = 9,28M subs (95% de la
   base del mod). Aunque el día 1 sean 3 pistas (Sagrario, combate, Aethon), el módulo
   existe, se suscribe solo y aparece en búsquedas.
4. **Aprovecha el vacío Calamity.** Cese de desarrollo confirmado (ago-2026, >9M de
   suscriptores huérfanos, Wikipedia/Patreon/Reddit). Acciones: (a) compatibilidad de
   escalado con ítems Calamity post-ML (nuestro WeaponScaling.cs ya es el hook), (b) en la
   descripción: "el arsenal endgame cósmico que tu mundo necesita ahora", (c) un
   "AethonMod × balance patch" ligero estilo Fargo's DLC (1,11M subs por ser el puente).
5. **Fotosensibilidad y caps como FEATURES vendidas.** WoTG pone el aviso en la primera
   línea de su descripción (2,57M subs); Calamity tiene cap de partículas configurable; tML
   2026 trae PhotosensitivityMode global. Nuestra lente full-screen y las tormentas de
   rayos EXIGEN: slider "Intensidad VFX" (0-100%), toggle de lente, cap de partículas y de
   pases del BlackHoleLensSystem. Anunciarlo en la descripción = confianza, no debilidad.
6. **El gigantismo es gratis; el lag es caro.** Mediana de los grandes = 276 MB; Lunar Veil
   = 928 MB sin pena. Pero Starlight River (el mejor arte GPU) arrastra reviews por fugas
   de memoria. Regla: cada efecto nuevo entra con su presupuesto (partículas máx, pases,
   targets) y su toggle; publicar "High FPS friendly" como Calamity Overhaul.
7. **El GIF de arma es el canal de adquisición #1.** Los tops del nicho (Stars Above,
   VFX+, Calamity Overhaul) venden con GIFs de armas en movimiento; ChippyGaming (1er
   YouTuber de Terraria, 1M+) es el amplificador histórico de mods. Entregable mínimo:
   3 GIFs de 8-10 s (Sol Rúnico N20, Agujero Negro con lente, Tormenta Nebular) + 6
   capturas comparadas vanilla-vs-Aethon en la página del workshop.
8. **Una clase/sistema solo retiene con cobertura de progresión completa.** Bard = 9 sets +
   56 accesorios + 110 ítems repartidos por TODA la partida; Clicker Class = 0,34M con
   decenas de upgrades. Nuestra rama del Fragmento necesita un hito POR BANDA (pre-HM,
   HM-temprano/medio/tardío, post-ML, post-Aethon) — 5 momentos de "wow" mínimos por rama.
9. **Publicar las libs VFX como mod separado = instalación gratuita en millones de setups.**
   Luminance 4,52M, SubworldLibrary 5,02M, InnoVault 2,0M, ParticleLibrary 0,96M — ninguna
   compitió: fueron dependencias. `AethonCore` (BrumaFX+Lumen+Storm+Estela+Onda, con README,
   3 ejemplos y wiki corta) es la muestra gratuita del motor. GPL NO (evitar el modelo
   copyleft de ParticleLibrary para no limitar adopción — MIT/informal).
10. **Integrarse con la capa de descubrimiento es marketing gratis a 8M+ instalaciones.**
    Boss Checklist (8,2M) → registrar los 6 jefes; Recipe Browser (8,3M) → recetas limpias;
    Census (2,58M) → Testigo/ECOs como town NPCs; WMITF (1,5M) → tooltips con origen claro.
    Es la tabla de contenidos de AethonMod ante casi todo el mercado.
11. **El mercado chino es el segundo mundo: zh-Hans desde el día 1.** 19 entradas del
    top-120 son localización (33,7M subs); una traducción de Calamity (3,12M) supera a
    Thorium. Acción barata: .hjson zh-Hans (podemos traducir 60-80 claves hoy) + título
    bilingüe en la página del workshop. Los VFX procedurales son idioma-agnóstico.
12. **El "difficulty remix" es un producto con 2M de subs (Infernum).** Diseñar YA el hook:
    tras vencer a Aethon, "Modo Apoteosis" — mismos jefes, patrones y VFX remixeados
    (soles que orbitan invertidos, agujeros que escupen lo absorbido). Reusa assets al 90%.
13. **Libera una DEMO vertical congelada antes del mod completo.** Everglow
    "[Demonstration]" = 96k subs; MEAC "demo" = 451k; Lunar Veil congeló su Legacy (210k)
    mientras desarrolla 2.0. Nuestro STABLE-SNAPSHOT (arsenal probado) puede publicarse
    como "AethonMod — Arsenal Demo" con las 14 armas del arsenal cósmico y el Grimorio.
14. **Nombra el pitch con el OBJETO, no con la sensación.** "Game Feel: Combat Feedback" =
    43 subs; "VFX+ reworks nearly every mage weapon" = 308k. La descripción del workshop
    debe decir QUÉ se rehace y CUÁNTO ("19 bastones de sol rúnico, 7 agujeros negros, 14
    armas procedurales, 8 shaders, 0 sprites de arte") — números concretos, no adjetivos.
15. **Los "productos satélite de experiencia" multiplican: cada pieza de la experiencia de
    jefe se vende por separado.** Barra (1,64M), cursor (3,95M), intro (0,43M), cartela de
    bioma (0,84M), haces de loot (1,0M). Paquete "presencia" para AethonMod en <300 líneas:
    cartela del Sagrario + intro de Aethon + barra segmentada por fases + loot beams
    rúnicos + luz dinámica del altar. Percepción de "mod grande" inmediata.

---

# 5. IDEAS DE CONTENIDO PRIORIZADAS (de la investigación al backlog)

## 5.1 Armamento/proyectiles (prioridad A — reusan libs existentes)
| Idea | Basada en | Coste | Impacto |
|---|---|---|---|
| **Shockwave "de trabajo"** reutilizable en cada impacto de arma (radio ideal 780px, vida 15 ticks, borde roto por ruido ±8 rad, disipación smoothstep 0.01→0.09) | WoTE PrismaticBurst | Bajo (ya hay OndaLib) | Alto — cada golpe se siente "premium" |
| **Afterimages con squash** como estándar de todos los proyectiles cósmicos (8 copias a −vel·0.8·i, alpha 0.6−i/15, squash Y 0.5 aliado/1.2 hostil) | MEAC | Muy bajo | Alto |
| **Lanza Alba con telegraph vertical** (columna 4000px, falloff cuadrático, advertencia 30 ticks antes del rayo) | WoTE EventideLances | Bajo (LanzaAlba existe) | Alto |
| **Time-stop en Sinfonía Primordial**: 60 ticks congelados + shader de aberración cromática + dusts suspendidos | Enigma Mod + Fargo's | Medio | Muy alto (momento firma) |
| **Charged cast**: mantener clic carga las varas rúnicas (anillo de runas procedural como UI de carga) | Overhaul charged attacks + Clicker | Medio | Alto |
| **Petrificación de la Medusa Nebular**: enemigos congelados en gris + crack de luz al romperse | MEAC/EoL | Bajo | Medio-alto |
| **Agujero negro "deluminador"**: además de distorsión, ABSORBE luz (círculo de oscuridad real) | nadie lo hace; Lights&Shadows inverso | Medio | Muy alto (únicos en el ecosistema) |
| **Loot beams rúnicos** verticales para drops del Sagrario | Loot Beams (1M subs) | Muy bajo | Alto |
| **Rail de rayo escalera** (cabeza/cuerda/cola con textura avanzando por el camino — modernizar el láser vanilla) | vanilla DelegateMethods | Medio | Alto |
| **Dash "Eco"** con afterimage que cancela la animación del arma | Overhaul dodge-roll | Bajo | Medio |

## 5.2 Sistemas (prioridad B — estructura de la marca)
1. **`AethonCore`** (libs VFX como mod-librería pública con 3 ejemplos + README) —
   estrategia de distribución §4.9.
2. **`AethonMusic`** separado (3 pistas: Sagrario, Combate cósmico, Aethon final) — §4.3.
3. **Integración Boss Checklist / Census / Recipe Browser** — §4.10 (día 1 del publish).
4. **Modo Apoteosis** (remix post-victoria, patrones invertidos) — §4.12.
5. **Subworld "Biblioteca del Testigo"** vía SubworldLibrary — sala de estrellas con el
   códex de armas y puertas a recuerdos — §3.23.
6. **Boss rush "Oda del Testigo"** — los 6 jefes en secuencia + final todos-a-la-vez —
   estilo Fargo's Mutant (4,12M).
7. **Cartela del Sagrario + intro cinemática de Aethon + barra de fases segmentada** —
   paquete "presencia" §4.15.
8. **Salas secretas selladas por runas** en el Sagrario (puzzles de luz) — exploración
   estilo Redemption/SOTS (0,42-0,43M c/u).
9. **zh-Hans + título bilingüe de workshop** — §4.11.
10. **`AethonDevLens`** (herramienta pública de sliders para testear las libs VFX) —
    utilidad para modders = marketing del motor — estilo DragonLens (0,19M).

## 5.3 Marketing/publishing (prioridad C — el día del publish)
- 3 GIFs (Sol Rúnico N20 / Agujero negro con lente / Tormenta Nebular) + 6 comparativas.
- Descripción con OBJETO+números: "19 bastones rúnicos, 7 agujeros negros, 14 armas,
  8 shaders procedurales, 0 sprites de arte".
- Aviso de fotosensibilidad + sliders en primera línea (estilo WoTG).
- Pack de suscripción: AethonCore + AethonMod + AethonMusic.
- Nota "compatibilidad post-ML con tus mods de contenido" para la audiencia huérfana de
  Calamity.

---

# 6. HALLAZGOS QUE CORRIGEN SUPUESTOS
- **El top-120 NO es una lista de mods de contenido**: es 69% QoL. Los "100 más populares"
  de cualquier lista clickbait (Calamity/Thorium/...) son los más populares DE CONTENIDO,
  no del workshop. Decisión estratégica: no medir nuestro éxito contra Recipe Browser.
- **Wikszilla no existe como página de workshop** (4 búsquedas: API vacía + web confunde
  con Wikizilla/kaijus) — es reputación de comunidad, no competencia de estantería.
- **Shadows of Abaddon = ex-SacredTools y está anclado en 1.3** (foro: "no portado a 1.4")
  — su hueco de "mod de jefes oscuro/eldritch 1.4" está LIBRE y Call of Void (0,70M) lo
  intenta sin líder claro.
- **Calamity cesó en agosto de 2026** — cualquier plan a 12 meses debe contar con un
  ecosistema Calamity "huérfano" (mods addon seguirán existiendo pero sin nodo central).

# 7. RUTAS DE EVIDENCIA
- Datos reales: `research/estrategia_v626/busquedas_web_42c/top120_subscribers.tsv`,
  `steam_details_raw.json`, `steam_details_notables.json`, `steam_details_vfx.json`,
  `steam_details_trending.json`, `desc_*.json` (16), `workshop_pop_p1.html`.
- Búsquedas previas del intento 42-c: JSONs `01_*.json`…`38_*.json` + `search_results*.txt`.
- Búsquedas frescas de este reintento: `39_wikszilla_terraria.json`, `40_spirit_reforged.json`,
  `41_stellamod.json`, `42_shadows_abaddon.json`, `43_genshin_style.json`, `44_wikszilla2.json`.
- No re-investigado (ver informes previos): técnicas de humo/partículas/trails
  (`humo_v625/INFORME_MODS_HUMO.md`), gap-analysis de libs (`humo_v625/ANALISIS_HUECOS.md`),
  biomas (`estrategia_v626/INFORME_BIOMAS.md`), desgarro de realidad
  (`estrategia_v626/INFORME_DESGARRO_REALIDAD.md`).

---
*Informe generado por Task 42-c (reintento). Presupuesto respetado: 6 búsquedas web nuevas
(39-44) + reutilización del scrape real del intento previo + informes previos del proyecto.*

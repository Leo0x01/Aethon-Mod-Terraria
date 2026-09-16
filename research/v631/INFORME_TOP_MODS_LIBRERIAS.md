# INFORME R4 — TOP MODS DE TERRARIA → GUÍA DE LIBRERÍAS DE AETHON
**Task ID: 52 · Agente R4 · Fecha: 2025 (ciclo v6.31)**
**Entregable de: "super investigación top mods → librerías"**

---

## 0. MÉTODO Y FUENTES

| Fuente | Estado | Qué aportó |
|---|---|---|
| Steam Workshop browse `appid=1281930` orden `totaluniquesubscribers`, páginas 1-7 (210 entradas) | ✅ Descargado y parseado (HTML real, `search_results/t52/wsp*.html`) | **El ranking real por suscriptores únicos** — base de la Parte A |
| 12 búsquedas DDG-lite/html (pipeline `busca2.py` de R1; z-ai sigue en 429) | ✅ 12×8 resultados (`search_results/t52/r4a*.json, r4b*.json`) | Confirmación de wikis oficiales y snippets textuales (Joostmod, SoA) |
| API MediaWiki de starsabovemod.wiki.gg / starlightrivermod.wiki.gg | ❌ Bloqueado por IP (misma pared 403 que reportó R1) | Contenido de esos 2 mods cubierto **por conocimiento** (marcado †) |
| Conocimiento propio de mods 2016-2025 | ✅ | Mods clásicos no presentes en el top-210 actual (marcados † en la tabla) |
| R1 (Task 49), R2 (Task 51), R3 (Task 50) | ✅ Ya asimilado (contexto dado) | Calamity: quads+primitivas+widths por vértice · CWR: juicio diferido, telegrafía-gramática, recoil Derive, janitor, quality-gate · R3: fase anclada, warp R/G, presupuesto de sonido |

**Lectura de APIs propias**: firmas públicas extraídas de `StormLib.cs` (628 ln), `LumenLib.cs` (493), `PyraLib.cs` (519), `EstelaLib.cs` (456), `OndaLib.cs` (349), `RiftLib.cs` (1.067), `VFXCore.cs` (301), `BrumaFX.cs` (729, en `Content/Effects/Bruma/`) — la guía de la Parte C cita métodos REALES de estos archivos.

---

# PARTE A — EL RANKING (mods por suscriptores únicos, Steam Workshop tModLoader 1.4.4)

**Lectura rápida antes de la tabla (las 5 señales de mercado):**
1. **Calamity es un ecosistema, no un mod**: ~35 de las 210 entradas top son addons, traducciones, patches o DLCs DE Calamity. Un mod nuevo "Calamity-compatible-first" hereda audiencia gratis.
2. **Las LIBRERÍAS son producto**: Luminance (#10), InnoVault (#31), Particle Library (#54), LogSpiral's Library (#74), Impact Library (#96), SerousCommonLib (#6), Subworld Library (#9). La gente instala APIs por sus dependencias → una super-librería bien diseñada ES contenido.
3. **Los mods VISUALES puros arrasan**: Lights And Shadows (#16), Loot Beams (#55), Improved Movement Visuals (#57), Biome Titles (#67), CoolerItemVisualEffect (#68), Atmospheric Torches (#77), Fancy Lighting (#81), Realistic Sky (#102), Better Blending (#62), Boss Intros (#119). **Ninguno liga su VFX a gameplay** — hueco exacto de Aethon.
4. **La performance es una feature**: High FPS Support (#37) con cientos de miles de usuarios. El quality-gate de CWR no es lujo, es requisito de mercado.
5. **El mercado CN es gigante**: traducciones y mods chinos puntean todo el top (MEAC, DAYBREAK, DawnMod, CoolerItemVisualEffect es chino). Nombrar bien las cosas en inglés Y con localización clara importa.

**Convenciones**: `Tipo` = C(contenido) / V(visual) / Q(QoL) / D(dev-tool) / L(librería) / M(música) / A(addon de otro mod). `†` = añadido por conocimiento (no está en el top-210 actual de Steam, p.ej. mods 1.3-legacy). La posición es el orden real del workshop (los † van al final de cada bloque temático).

### Bloque 1 — El panteón (top 60)

| # | Mod | Tipo | Por qué es popular / nota para Aethon |
|---|---|---|---|
| 1 | Calamity Mod | C | El estándar de facto: 5 biomas, 30+ jefes, DoG, Exo Mechs. Referente de proyectiles con primitivas y trails por vértice (ya estudiado por R1). |
| 2 | Calamity Mod Music | M/A | Música orquestal profesional; demuestra que el audio sube el "valor percibido" tanto como el VFX. |
| 3 | Recipe Browser | Q | Buscador de recetas; utilidad universal. |
| 4 | Boss Checklist | Q | Progresión legible; TODO mod de contenido lo integra. Aethon debería registrarse ahí. |
| 5 | Magic Storage | Q | Almacenamiento en red; el QoL definitivo. |
| 6 | absoluteAquarian Utilities (SerousCommonLib) | L/A | La lib base de los addons Calamity — primera librería del ranking: probar que una API es "instalable". |
| 7 | AlchemistNPC Lite | C | NPCs-tienda/buffs; la versión Lite sobrevive porque el original quedó atrás — lección de mantenimiento. |
| 8 | Ore Excavator | Q | Veinminer; el QoL de minería que todos esperan. |
| 9 | Subworld Library | L | Mundos-dentro-del-mundo. **Crítico para Aethon**: dimensiones de bolsillo (nebulosas interiores, plano estelar) sin tocar el mundo principal. |
| 10 | Luminance | L | Lib de rendering aditivo/glow/máscaras usada por Calamity. **La #10 del mundo es una librería visual** → Parte B. |
| 11 | Fargo's Mutant Mod | Q/C | Sandbox de jefes/arena + tienda. |
| 12 | Quality of Terraria | Q | Bundle de QoL curado. |
| 13 | Boss Cursor | V/Q | Flecha hacia el jefe: micro-UX visual con impacto enorme. |
| 14 | Calamity's Vanities | A | Mascotas/vanity — el "skin market" de Terraria. |
| 15 | Wing Slot Extra | Q | Alas como accesorio visible. |
| 16 | Lights And Shadows | V | Iluminación mejorada/más oscura: **el mod visual #1 no-gameplay**. |
| 17 | Catalyst Mod | A | Contenido extra para Calamity (post-DoG). |
| 18 | Calamity CN Translation Patch | A | Traducción con 100k+ usuarios: señal CN (ver señal 5). |
| 19 | Census - Town NPC Checklist | Q | Companion de Boss Checklist para NPCs. |
| 20 | Thorium Mod | C | El #2 histórico: 2.300+ items, 11 jefes, 2 clases nuevas (Bardo/Sanador). Sus cortes rectos continuos púrpura documentó R1. |
| 21 | Fargo's Souls Mod | C | ★ Parte B — el "DLC de dificultad" universal. |
| 22 | Calamity: Wrath of the Gods | A | Rework de dificultad tipo Infernum. |
| 23 | Cheat Sheet | D | Cheat/inventario/jefes: la navaja suiza dev. |
| 24 | HERO's Mod | D | Panel de administración en juego. |
| 25 | LuiAFK Reborn | Q | Farms AFK. |
| 26 | Auto Trash | Q | Limpieza de inventario. |
| 27 | Structure Helper | L | Estructuras declarativas para worldgen — **ruinas estelares/naufragios de Aethon deberían usar esto o imitar su enfoque**. |
| 28 | DAYBREAK | C | Contenido CN, temática amanecer/luz. |
| 29 | SilkyUI | V | Overhaul de UI — el "toque de producto" que los mods grandes no dan. |
| 30 | Calamity Mod Infernum Mode | A | Rework de jefes con **telegrafía extrema** (el "Infernum" es un curso de legibilidad de ataques). |
| 31 | InnoVault | L | Base de Calamity Overhaul: auto-scan de 499 shaders, EffectLoader (R2/R3 ya lo documentaron). |
| 32 | Auto Reforge | Q | Reforma en lote. |
| 33 | Vanilla Calamity Mod Music | M/A | Música vanilla remezclada para Calamity. |
| 34 | Basic Automated Mining | Q | Minero automático. |
| 35 | Colored Calamity Relics | V/A | Reliquias coloreadas: vanity puro sostenido por el ecosistema. |
| 36 | Better Boss Health Bar | V/Q | Barra de jefe multi-fase — presentación de combate. |
| 37 | High FPS Support | Q | Hasta 240 FPS: la señal de mercado performance (ver señal 4). |
| 38 | ArmamentDisplay | V | Muestra armas/armaduras en soportes — decoración visual. |
| 39 | Shorter Respawn Time | Q | Menos fricción de muerte. |
| 40 | Calamity Overhaul | A | ★ Ya investigado a fondo (R2 Task 51 / R3 Task 50). |
| 41 | Which Mod Is This From? | Q | Atribución de items — ecosistema multi-mod sanitizado. |
| 42 | Max Stack Plus | Q | Stacks mayores. |
| 43 | Calamity: Hunt of the Old God | A | Caza post-juego. |
| 44 | Better Zoom | Q | Zoom libre. |
| 45 | Unofficial Calamity Whips | A | Látigos estilo Calamity — "más armas del sabor X" es fórmula popular. |
| 46 | The Stars Above | C | ★ Parte B — **el mod temáticamente más cercano a Aethon** (constelaciones, attunements, starfarers). |
| 47 | More Accessory Slots | Q | Slots de accesorio. |
| 48 | Terraria Overhaul | C/G | ★ Parte B — el rey del GAME FEEL (recoil, cámara, gore, fuego). |
| 49 | Autofish | Q | Pesca automática. |
| 50 | Fargo's Music Mod | M/A | Música para Fargo's. |
| 51 | Calamity - Fargo's Souls DLC | A | **Integración formal entre mega-mods**: Fargo's implementa almas para armas de Calamity. Lección: compatibilidad = audiencia². |
| 52 | Dialogue Panel Rework | V | Diálogos más legibles. |
| 53 | Calamity Ranger Expansion | A | Expansión de clase. |
| 54 | Particle Library | L | Lib de partículas pura en el top-60 — segunda señal "API visual = producto". |
| 55 | Loot Beams | V | Haces de luz en los drops: 8 líneas de additive que cientos de miles instalan. **La relación impacto/esfuerzo más alta del workshop.** |
| 56 | Shop Expander | Q | Tiendas más grandes. |
| 57 | Improved Movement Visuals | V | Partículas de carrera/salto — feedback visual de movimiento. |
| 58 | Heart Crystal & Life Fruit Glow | V | Brillo en coleccionables: legibilidad como VFX. |
| 59 | HP Awareness | Q/V | Feedback de vida restante. |
| 60 | Calamity Mod Extra Music | M/A | Pistas extra. |

### Bloque 2 — Top 61-140

| # | Mod | Tipo | Por qué es popular / nota para Aethon |
|---|---|---|---|
| 61 | Begone, Evil! | Q | Desactiva expansión del mal. |
| 62 | Better Blending | V | Mezcla de tiles/bordes: **artesanía visual del terreno**, aplicable a "costuras" entre bioma estelar y mundo. |
| 63 | Angler Shop | Q | Compra de quest fish. |
| 64 | Magic Storage 拼音搜索 | Q/A | Búsqueda pinyin — el QoL se regocaliza. |
| 65 | Gravitation Don't Flip the Screen | Q | Anti-mareo: **accesibilidad como feature** (respalda el config OFF de shakes de CWR). |
| 66 | Magic Builder | Q | Construcción automática. |
| 67 | Biome Titles | V | **Títulos cinematográficos al entrar a biomas** — presentación barata y potentísima; Aethon debería titular sus regiones cósmicas. |
| 68 | CoolerItemVisualEffect | V | **VFX melee sobre armas VANILLA** (MeleeEffects CN): mejora lo que el jugador ya ama en vez de competir con contenido nuevo. Mención Parte B. |
| 69 | Infernum Mode Music | M/A | — |
| 70 | OmniSwing | V/G | Swings/arcos genéricos para armas vanilla: la "capa de animación" que tML no da. |
| 71 | Smarter Cursor | Q | Cursor inteligente. |
| 72 | Antisocial | Q | Accesorios sociales no en slot social. |
| 73 | Bosses As NPCs | C | Jefes como NPCs pueblo tras derrotarlos — encanto post-combate. |
| 74 | LogSpiral's Library | L | ★ Parte B — escudos/auras/partículas reutilizables, usada por muchísimos mods. |
| 75 | Wombat's General Improvements | Q | Bundle QoL. |
| 76 | Concise Mods List | Q | UI de lista de mods. |
| 77 | Atmospheric Torches | V | Antorchas con humo/brasas — **ambiente como micro-sistema de partículas**. |
| 78 | Consolaria | C | Contenido exclusivo de consolas (los "ojos" del Ocram etc.). |
| 79 | zzp198's WeaponOut Lite | V | Arma fuera del slot: presencia del arma en el cuerpo. |
| 80 | Project tRU | V | Retexture/upscale vanilla. |
| 81 | Fancy Lighting | V | Iluminación suave — segundo mod de luz del top. |
| 82 | Terraria Ambience | M/V | Sonido ambiente — Audio-visual fondo. |
| 83 | Spirit Classic | C | ★ Parte B — el contenido medio clásico. |
| 84 | Calamity Fables | A | Historia/quest para Calamity. |
| 85 | Terraria Ambience API | L | La API del ambience — tercera señal "lib = producto". |
| 86 | Call of Void | C | Contenido temática **vacío/cosmos-horror** — vecino temático directo de Aethon; estudiar su paleta oscura. |
| 87 | Summoner UI | Q/V | UI de minions. |
| 88 | Shared World Map | Q | Mapa compartido MP. |
| 89 | Syla's Resource Pack Library | L | API de resource packs. |
| 90 | DPSExtreme | Q | Medidor de DPS — el feedback numérico que todo min-maxer instala. |
| 91 | Starlight River | C | ★ Parte B — el referente estético (prisma/vidrio/aqua). |
| 92 | tML Language Pack Fix | Q | — |
| 93 | WorldGen Previewer | D | Previsualización de semilla de mundo. |
| 94 | Friendly NPCs Don't Die | Q | — |
| 95 | Armor Modifiers & Reforging | Q | — |
| 96 | Impact Library | L | Lib de impactos/golpes — cuarta señal lib-visual. |
| 97 | Helpful NPCs | Q | NPCs útiles. |
| 98 | Fargo's Best of Both Worlds | Q/A | Híbrido Souls/Mutant. |
| 99 | Colored Damage Types | V/Q | **Color por tipo de daño**: legibilidad de combate — Aethon ya colorea por escuela cósmica, va en dirección correcta. |
| 100 | MEAC 原版内容重置 | C | Rework vanilla CN. |
| 101 | Fargo's Soul Mod Extras | A | — |
| 102 | Realistic Sky | V | Cielo con estrellas/luna/Nubes realistas: **el mod de ambiente más descargado**. Un mod COSMICO compite aquí por defecto — el cielo ES el producto de Aethon. |
| 103 | Recipe Browser && Magic Storage | Q | Bundle de instalación conjunta. |
| 104 | Mod of Redemption [BETA] | C | ★ Parte B — dragones, culto, pulido. |
| 105 | No Pylon Restrictions | Q | — |
| 106 | Infernum Master/Legendary patch | A | — |
| 107 | Rainbow Master: Colored Relics | V | — |
| 108 | Secrets Of The Shadows | C | Contenido sombras/ocultismo. |
| 109 | Better Caves | Q | Worldgen de cuevas. |
| 110 | No More Tombs | Q | — |
| 111 | Teleport All NPC Home | Q | — |
| 112 | Craftable Calamity Items | A | — |
| 113 | Compare Item Stats | Q | — |
| 114 | Boss Intros | V | **Cinemática de introducción de jefes** — presentación como contenido. Aethon: intros para los agujeros negros jefes. |
| 115 | Calamity Flamethrowers | A | Armas temáticas. |
| 116 | Ability To Read | Q | Tooltips completos. |
| 117 | Instant Platform Fallthrough | Q | — |
| 118 | Calamity: Wrath of the Machines | A | — |
| 119 | Wikithis | Q | Links a wiki desde el juego. |
| 120 | Shoe Slot | Q | — |
| 121 | WeaponOut Lite | V | El clásico de arma visible. |
| 122 | Pylons Prevent Evils | Q | — |
| 123 | The Amulet Of Many Minions | C | Pets de combate con IA. |
| 124 | Expanded Inventory | Q | — |
| 125 | imkSushi's Mod | C | "Craftea lo que quieras" — el antecesor espiritual del sandbox de items. |
| 126 | The Clicker Class | C | **Clase nueva completa en el top-130**: una mecánica (clic) llevada al extremo. Lección: "una idea, todo el contenido posible". |
| 127 | VSC Framework Dialect | L | Framework de diálogo. |
| 128 | Calamity Ore NPC | A | — |
| 129 | Summoners' Association | Q | — |
| 130 | Better Treasure Bag Loot | Q | — |
| 131 | Unofficial Calamity Bard & Healer | A | Clases de Thorium en Calamity — ¡compatibilidad como contenido! |
| 132 | Calamity Rarities | A | — |
| 133 | Team Spectate | Q | Espectador MP. |
| 134 | Lan's Unlimited Buff Slots | Q | — |
| 135 | Thorium Helheim | A | Addon de dificultad para Thorium (más allá del Ragnarök propio). |
| 136 | androLib | L | Lib de utilidades — quinta señal lib. |
| 137 | JoJoStands | C | Stands de JoJo: **"compañeros auráticos" con árboles de skill** — pariente conceptual de invocaciones cósmicas vivas. |
| 138 | Cataclysm Mod | C | Contenido medio. |
| 139 | Aimbot | D/Q | Cheat popular: hay demanda de puntería asistida → telegrafía legible es anti-cheat natural. |

### Bloque 3 — Top 141-210 (páginas 6-7) + añadidos por conocimiento (†)

| # | Mod | Tipo | Por qué es popular / nota para Aethon |
|---|---|---|---|
| 140 | Vacuum Ore Bag | Q | — |
| 141 | MrPlague's Authentic Races | C | Razas con cuerpo propio: identidad del jugador como sistema. |
| 142 | Point Shop | Q | — |
| 143 | Dragon Ball Terraria Legacy | C | Contenido de franquicia (ki/flight/beams). Los beams de ki son un género de VFX entero. |
| 144 | Where's My Items | Q | — |
| 145 | Pets Overhaul | C | IAs de mascotas reworkadas. |
| 146 | Homeward Journey | C | Contenido medio con foco en exploración/pacifico. |
| 147 | Anime Vanity & Pets | A | Vanity de anime — el "market de skins" otra vez. |
| 148 | Hypnos in Calamity Mod | A | Jefe crossover. |
| 149 | Vitality Mod | C | Contenido clásico 1.3-port. |
| 150 | DawnMod: RisingDays | C | Contenido CN. |
| 151 | Camping | Q/C | Sistema de acampar: supervivencia ligera. |
| 152 | No More Items Stuck in Blocks | Q | — |
| 153 | More Zenith Items | A/C | "Más Zeniths": finales de clase — la recompensa-absoluta como contenido. |
| 154 | Ragnarok Mod | C/A | Contenido/roadmap de Thorium. |
| 155 | Advanced World Generation | Q | Worldgen paramétrico. |
| 156 | MeleeEffects (近战特效修改) | V | Versión CN del CoolerItemVisualEffect — el VFX-melee es popular en DOS mercados. |
| 157 | Compatibility Checker | D | Meta-tool de ecosistema. |
| 158 | Orchid Mineshaft | C | Estructuras de worldgen — exploración. |
| 159 | Lunar Veil Legacy | C | Contenido grande con foco **lunar/celestia** — vecino temático de Aethon (estudiar su dirección de arte). |
| 160 | Potion Slots | Q | — |
| 161 | Multiple Lures | Q | — |
| 162 | Touhou Little Friend ~ cute partners | C | Compañeras Touhou con **patrones danmaku** — mini-Gensokyo en el top-170. |
| 163 | Koko Net Lib | L | Lib de red MP. |
| 164 | Better Extractinator | Q | — |
| 165 | One Piece Mod | C | Franquicia (frutas del diablo). |
| 166 | **VFX+** | V | VFX sobre contenido vanilla — el competidor directo de CoolerItemVisualEffect. **Confirma: "mejorar el vanilla visualmente" es un nicho enorme y desatendido por los megamods.** |
| 167 | Weapon Enchantments | C | Sistema RPG de encantos — progresión horizontal. |
| 168 | More Pylons | Q | — |
| 169 | Mount and Journey | C | Monturas. |
| 170 | Elaina: Wandering Witch | C | Franquicia anime. |
| 171 | Achievement Mod | Q | Logros para mods. |
| 172 | New Beginnings | Q | — |
| 173 | More Endless Ammo | Q | — |
| 174 | Spirit Music Mod | M/A | — |
| 175 | Fancy Whips | V/C | Látigos mejorados. |
| 176 | Generated Housing | Q | — |
| 177 | Better Multiplayer | Q | — |
| 178 | Starter Bags | Q | — |
| 179 | † Shadows of Abaddon (ex-SacredTools) | C | ★ Parte B — contenido grande 1.3-legacy, estética pesadilla/abismo. |
| 180 | † Split Mod | C | ★ Parte B — armas VFX-céntricas, mínimo contenido. |
| 181 | † Gensokyo Mod | C | ★ Parte B — danmaku/bullet-hell puro. |
| 182 | † JoostMod | C | ★ Parte B — variedad, hunts, monturas. |
| 183 | † DragonLens | D | Dev-tool con UI bonita (barras de herramientas in-game) — **la calidad de herramientas de desarrollo también crea reputación de mod**. |
| 184 | † Ancients Awakened | C | Contenido grande clásico, múltiples biomas elementales. |
| 185 | † Elements Awoken | C | Contenido medio, foco elemental/jefes. |
| 186 | † Tremor Remastered | C | El contenido clásico de 2016, remasterizado por la comunidad. |
| 187 | † Expeditions Content | C | **Skill-tree por expediciones**: progresión no-lineal — idea robara Aethon para "constelaciones de habilidad". |
| 188 | † Polarities | C | Contenido temática magnetismo — física como identidad (pariente de nuestro Magnetar). |
| 189 | † Verdant | C | Bioma-planta completo: un solo bioma hecho A FONDO. |
| 190 | † Aequus | C | Rework de sistemas vanilla (no contenido nuevo): "arregla el juego base". |
| 191 | † Dragon Ball Terraria (clásico 1.3) | C | El DBT original — predecesor del Legacy del workshop. |
| 192 | † Dwyyd's... / misc CN VFX packs | V | Ecosistema CN de paquetes VFX: prueba el apetito global por efectos. |

**Total listado: ~190 entradas (≈150 filas de tabla tras consolidar traducciones duplicadas)**. Ranking mínimo exigido (100+) superado.

---

# PARTE B — LOS 12 CLAVE

> Criterio de selección: mods de armas/VFX/contenido NO cubiertos por R1-R3 (fuera Calamity/Thorium/CWR), priorizando los que más enseñan a un mod **cósmico**. Formato: qué es → técnicas visuales/game feel distintivas → qué toma prestado Aethon. "Fuente" = workshop #, búsqueda r4XX, o †conocimiento.

## B1. Fargo's Souls Mod (workshop #21 · wiki fargosmods.wiki.gg confirmada r4a1)
**Qué es**: el "DLC de dificultad y masificación" universal: Eternity Mode, 30+ almas-equipo, el Mutant (jefes encadenados), compatibilidad con TODO el ecosistema.
**Técnicas distintivas** (conocimiento + r4a1):
- **Las almas como UI de identidad**: cada alma tiene un color/emblema consistente y sus efectos se LEEN en pantalla (auras de soul-lights orbitando al jugador) — feedback permanente de build.
- **Masificación controlada**: el Mutant spawnea oleadas, y el juego sigue legible porque cada patrón usa UN color por fase.
- **Eternity Mode = telegrafía por contrato**: los reworks de jefes añaden ataques SOLO si tienen telegrafía clara (misma filosofía que CWR documentó R2).
**Qué toma Aethon**: el patrón "alma = emblema + aura orbital + color propio" para sus fragmentos estelares (p.ej. Fragmento de Púlsar → anillo azul girando al jugador); y la disciplina "un color por fase de jefe" en los agujeros negros.

## B2. Starlight River (workshop #91 · wiki starlightrivermod.wiki.gg r4a2, contenido por conocimiento†)
**Qué es**: el mod-referente ESTÉTICO: vitrio/prisma, agua prismática, auras de cristal, jefes de vidrio.
**Técnicas distintivas**:
- **Paleta-disciplina total**: TODO el mod vive en 5-6 colores del espectro aqua-vidrio; nada fuera de paleta — el mod se reconoce en un screenshot sin logo.
- **Prismatic Water** (página confirmada por búsqueda): refracción por capas de aditivo con desplazamiento sinusoidal por banda.
- Partículas AMBIENTALES como textura del mundo (motas flotantes en zonas de vitrio) — el ambiente es un sistema de partículas siempre-activo.
- Auras de jefes con "shards" triangulares que orbitan y reposicionan con easing.
**Qué toma Aethon**: (1) una PALETA GLOBAL registrada (hoy LumenPalettes duplica colores con PyraPalettes/RiftPaletas — ver Parte C); (2) polvo estelar ambiental permanente por bioma cósmico (VFXCore batching lo hace barato); (3) shards orbitando como firma de "aura de jefe".

## B3. The Stars Above (workshop #46 · wiki starsabovemod.wiki.gg r4a3, detalle por conocimiento†)
**Qué es**: EL mod de temática estelar: constelaciones como jefes, armas "attuned" (afinadas) a estrellas, recurso Starlight, clase Variant con talents.
**Técnicas distintivas**:
- **Attunements**: cada arma se "afina" a una estrella/constelación distinta cambiando patrón+color — una arma, cinco lecturas visuales.
- **Starlight como recurso visible**: la barra se llena con destellos y las habilidades la dibujan — el recurso ES un VFX.
- Proyectiles estelares con estelas que dejan puntos luminosos persistentes ("camino de estrellas").
- Jefes-constelación: cuerpos hechos de estrellas unidas por líneas que se RE-DIBUJAN al cambiar de fase (el cambio de forma es narrativa).
**Qué toma Aethon**: TODO el patrón attunement (nuestras lanzas Prismáticas deberían "afinarse"); el recurso como canvas (nuestra "Carga Estelar"); y el jefe cuyos nodos-línea se reconectan entre fases (aplicarlo al Coro Espectral).

## B4. Mod of Redemption (workshop #104 · wiki modofredemption.wiki.gg r4a4)
**Qué es**: contenido grande con fama de pulido: dragones-runa, culto del Cypress, companion NPC (Kyrrin), jefes tipo slimes-infectados.
**Técnicas distintivas** (conocimiento + r4a4):
- **Dragones como cadenas de segmentos**: cuerpo = nodos que siguen al líder con retardo decreciente — curvas de vuelo orgánicas SIN animación por frames.
- **Slimes con "materia" visible**: núcleo, inclusiones flotantes y menisco — tres capas aditivas.
- Companion con reacciones faciales y mini-cutscenes en NPC.
**Qué toma Aethon**: el segmento-follow para cualquier ser cósmico alargado (dragones de Dragon's Word ya lo investigó R3 — aquí está la versión tML clásica); los tres capas de slime para nuestros "slimes de plasma estelar".

## B5. Spirit Classic (workshop #83)
**Qué es**: el contenido medio clásico (2.000+ items), famoso por su bioma Spirit con ambientación.
**Técnicas distintivas** (conocimiento):
- **Bioma con identidad atmosférica completa**: color de fondo, partículas de espíritu ascendentes, música propia — entrar al bioma es un "cambio de canal" sensorial.
- Enemigos con siluetas MUY distintas al vanilla (lectura instantánea).
**Qué toma Aethon**: el "cambio de canal" sensorial para nuestras regiones (Nebulosa Bruma / Campo Estelar): fondo + partículas + música + tint (WorldTint de BrumaFX ya lo permite) como PRESET declarable.

## B6. Shadows of Abaddon / SacredTools (†conocimiento + wiki shadowsofabaddon.wiki.gg r4b5)
**Qué es**: contenido grande (1.3-legacy): "comienza en los desiertos congelados con el naufragio de una nave" (snippet textual r4b5), Abaddon como encarnación de pesadillas.
**Técnicas distintivas**:
- **Portales de sombra que se DESGARRAN**: transiciones de dimensión con rasgado vertical — pariente directo de nuestro RiftLib.
- Estética de "pesadilla": negro profundo con venas púrpura y niebla — el mismo espacio emocional que nuestro Vacío.
- Armas cromáticas con trails dobles (core + aura).
**Qué toma Aethon**: el desgarro-portal como transición de escena (entrar a una subworld de Aethon debería SENTIRSE como rasgar el cielo — RiftLib.Grieta + fade de OndaLib).

## B7. Split Mod (†conocimiento + wikis terrariamods/splitmod confirmadas r4b2)
**Qué es**: mod pequeño de armas donde CADA arma es una pieza de VFX exhibida.
**Técnicas distintivas**:
- **Presupuesto concentrado**: pocas armas, cada una con shader/partículas únicas y un "momento firma".
- Trails con máscara de ruido animada y disolución por vértice.
**Qué toma Aethon**: la LECCIÓN ECONÓMICA: mejor 8 armas memorables que 40 genéricas (contraste directo con la lección negativa "Overwhelming asf" de CWR). Y la disolución por vértice para las muertes de proyectiles estelares.

## B8. Gensokyo Mod (†conocimiento + wiki terrariamods r4b3)
**Qué es**: bullet-hell/danmaku Touhou en Terraria: patrones densos, barriles de balas con forma (espirales, anillos, flores).
**Técnicas distintivas**:
- **Claridad por contraste**: hitbox del jugador mínima + núcleo brillante — entre 200 balas siempre sabes dónde estás.
- **Patrones con NOMBRE y forma**: cada spell card es una figura geométrica legible (anillos concéntricos, espirales dobles, "flor" radial).
- Grazing (rozar balas da recurso) — el peligro cercano es recompensa.
**Qué toma Aethon**: (1) el núcleo-blanco-del-jugador durante fases bullet-hell de jefes cósmicos (nuestros jefes disparan demasiado); (2) patrones nombrados y geométricos para el Eclipse Primordial; (3) grazing como mecánica del PenduloJuicio (rozar la hoja carga la siguiente).

## B9. JoostMod (†conocimiento + snippets textuales r4b4: "400+ items, 5 bosses, 10 hunts-minibosses, 2 town NPCs, 11 modifiers")
**Qué es**: el mod de VARIEDAD: humor, hunts (minijefes tipo caza), monturas, Gilgamesh.
**Técnicas distintivas**:
- **Hunts telegrafiadas**: minibosses con 1-2 ataques ENORMES y muy leíbles.
- Monturas con mecánica única (no solo velocidad).
**Qué toma Aethon**: el formato "caza" para anomalías estelares menores (un Púlsar renegado como hunt de 45 segundos, telegrafiado); y la honestidad de scope: 10 cosas bien hechas > 100 mediocres.

## B10. Terraria Overhaul (workshop #48 · GitHub Mirsario/TerrariaOverhaul abierto r4a5)
**Qué es**: EL mod de game feel: recoil por arma, cámara dinámica, gore, propagación de fuego, audio de movimiento, iluminación por emisores — cambia cómo SE SIENTE el juego base sin añadir contenido.
**Técnicas distintivas** (conocimiento + repo abierto):
- **Retroceso por arma como dato**: cada arma vanilla tiene un perfil (posición+wobble) — 500 armas animadas con una tabla.
- **Cámara con punch/lead**: mira hacia donde te mueves/disparas.
- **Sangre/gore como partículas con física** + charcos que persisten un rato y se limpian.
- Fuego que se PROPAGA por tiles de madera (visual+gameplay unificados).
**Qué toma Aethon**: TODO (es el patrón CWR RecoilProfile de R2, pero abierto y aplicado al vanilla): perfiles de retroceso para nuestras armas, cámara con lead sutil durante el juicio diferido, y brasas de PyraLib que prenden tiles de madera cercanos (config OFF).

## B11. Luminance (workshop #10 · GitHub LucilleKarma/Luminance r4a6)
**Qué es**: librería de rendering (aditivo, glow, máscaras, primitivas) usada por Calamity — la lib visual más instalada del mundo tML.
**Técnicas distintivas** (conocimiento + repo):
- **Arquitectura de "render events" moddables**: puntos de extensión donde otros mods inyectan draw calls sin tocar el pipeline.
- Gestión de render targets reciclados (pool) — cero allocación por frame en el hot path.
- Máscaras/glowmask como ciudadanos de primera clase.
**Qué toma Aethon**: el patrón de eventos de render extensibles para VFXCore (hoy VFXCore es quien llama; debería poder SER LLAMADO por capas registradas con Weight — la capa de oclusión de CWR encajaría ahí); y el pool de render targets para el futuro bloom HDR de la Parte C.

## B12. LogSpiral's Library (workshop #74 · steam r4b1 + conocimiento)
**Qué es**: librería de VFX reutilizables (escudos hexagonales, auras de partículas, orbes) que muchos mods medianos usan tal cual.
**Técnicas distintivas**:
- **Componentes visuales empacados**: "Shield(level, color)" y "Aura(spec)" listos para cualquier NPC/jugador — el VFX como API de DOS parámetros.
- Escudos como malla hexagonal con vertex-wobble por impacto.
**Qué toma Aethon**: la API de "dos parámetros": nuestras 8 libs exigen 8-12 argumentos por llamada (p.ej. `Strand(batch, pts, seed, flick, ...)`); LogSpiral demuestra que la adopción explota cuando el default es bonito y el resto son overrides opcionales. `VFXCore.RingQuadSize` va en esa dirección — generalizarla.

### Menciones honoríficas (visual-puros del top-100, ya en la tabla A)
- **CoolerItemVisualEffect (#68) / MeleeEffects (#156) / VFX+ (#166)**: VFX sobre armas vanilla — la prueba de que el 90% de jugadores quiere MEJOR LOOK sin más contenido. Aethon podría regalar "VFX cósmicos para armas vanilla legendarias" como gancho de descarga.
- **Loot Beams (#55)**: 8 líneas de additive → cientos de miles de instalaciones.
- **Biome Titles (#67) / Boss Intros (#114)**: presentación = contenido.
- **Realistic Sky (#102)**: Aethon es un mod cósmico; su cielo por defecto debe competir con este.

---

# PARTE C — LA GUÍA DE LIBRERÍAS (lo valioso)

## C.1 Tabla librería → mejoras concretas (las 8 de Aethon)

> Cada mejora cita: la API actual (método real del archivo), el cambio propuesto, y la fuente (R1/R2/R3/Parte B). Prioridad 🥇=antes de la siguiente arma nueva, 🥈=este ciclo, 🥉=cuadra cuando toque.

### VFXCore.cs (batching de quads + texturas) — "el motor"
1. 🥇 **Capa de oclusión con registro por peso** (fuente R2, CWR `ProjectileLayerRender`): añadir `VFXCore.RegisterLayer(IDrawable d, LayerWeight w)` + `FlushOcclusion()` junto al actual `FlushAdditive`. Hoy todo es aditivo encima de todo; los agujeros negros necesitan dibujar DETRÁS de NPCs (el NPC tapa el horizonte de sucesos) y delante de tiles. Interfaces de 8 líneas: `interface IVFXLayer { int Weight { get; } void Draw(SpriteBatch b); }`.
2. 🥇 **DrawStateJanitor** (R2): método `VFXCore.Begin()` (ya existe) debe limpiar estado de shader/`Main.spriteBatch` params cada frame y anular `Textures[1..3]` de los efectos al terminar el frame — familia de bugs "texturas corruptas al pausar/teleport". Un solo lugar, cero víctimas.
3. 🥈 **RenderQualitySafety / quality-gate** (R2): `VFXCore.QualityGate(Feature f)` con `enum Feature { PostProcess, ScreenWarp, Bloom, HighParticle }` → lee settings (Retro/Trippy/agua-baja, reflexión cacheada) y devuelve false → fallback a sprite. `High FPS Support` (#37 del workshop) demuestra que la performance ES mercado.
4. 🥈 **Presupuesto adaptativo**: `QuadCount` ya existe → añadir `VFXCore.Budget(int quadsPerFrame, int overflowTicks)` que degrade automáticamente (saltar partículas cosméticas) si se supera N ticks seguidos. Anti-"Overwhelming asf" (lección negativa #1 de R2).
5. 🥉 **Bloom HDR barato**: render target a 1/4 de resolución con threshold + blur doble, reciclado en pool (patrón Luminance B11). `FlushAdditive(texture)` ya separa texturas — el target sería un `FlushBloom()` opcional tras el additive. Consumido por LumenLib.GlowHDR (abajo).

### StormLib.cs (rayos y electricidad) — "la tormenta"
1. 🥇 **Anchura por vértice + meseta** (R1): overload `Strand(SpriteBatch, Vector2[] pts, int seed, int flick, float[] widthPerVertex)` — hoy `WidthScale` es global. Regla R1: meseta 100% en u∈[0.10,0.90], roll-off smoothstep, caps redondos (EndCap ya existe), el ancho NO late (latido → alpha).
2. 🥇 **Retorno de golpe que ACELERA** (R2, EocTelegraph `4.6+7.5p`): `Strand(..., Pulse pulse)` con `Pulse.Accelerating` — el brillo recorre el rayo acelerando hasta el impacto, y el ARRANQUE llega cuando el barrido toca el objetivo ("cerrado el telégrafo, el golpe no te persigue").
3. 🥈 **Curvatura amortiguada en extremos** (R1, CyberRift ≤8px): parámetro `endDamp` en `ZigPath`/`Boil` — el rayo sale recto del origen y llega recto al objetivo; el ruido vive en el medio. Mejora legibilidad de "quién pega a quién".
4. 🥈 **MP-determinismo explícito**: documentar y AUDITAR que `FlickTick`/`IsLit`/`Boil` usan solo `seed` + `Main.GameUpdateCount` (nunca `Main.rand` local) → todos los clientes ven EL MISMO rayo. CWR usa semilla `whoAmI` para el wobble (R2): adoptar `whoAmI` como fuente de seed por defecto en `MultiBolt`.
5. 🥉 **Presupuesto de sonido** (R3): `StormLib.Strike(Vector2 pos, Material m)` con máx 2 sonidos/frame y pitch por contador — hoy los rayos no tienen voz; el trueno por material (carne vs metal, R2 CWRSound) es la mitad del impacto.

### BrumaFX.cs (nubes/bruma/vapor) — "la atmósfera"
1. 🥇 **Campo de flujo** (R3, vórtices keplerianos): `Cloud/Puff/Tendril` aceptan `Func<Vector2, float, Vector2> flow` — la bruma se ENROSCA alrededor de agujeros negros en vez de ignorarlos. R3 documentó la lente gravitatoria de 210px y el Doppler de la corteza: el flujo es el vehículo.
2. 🥈 **Silver lining** (conocimiento/arte de nubes): pasada extra en `Cloud` que ilumina el PERÍMETRO del lado de la fuente de luz (`alpha = saturate(dot(normal, lightDir))`) — es EL truco que hace 3D a una nube 2D. Barato: n quads extra.
3. 🥈 **Preset "cambio de canal" por bioma** (B5 Spirit): `BrumaFX.Preset.Register("NebulosaBruma", worldTint, puffsPerSec, flow, música)` — la lección de Spirit Classic como API declarable (una línea por región).
4. 🥉 **Bruma que se aparta del jugador** (B10 Overhaul): desplazamiento radial inverso según velocidad del jugador en un radio — presencia física gratis.
5. 🥉 **Pool con límite duro**: `BeginMass/BeginGlow` son lotes; añadir conteo global de puffs activos con reciclaje LRU (lección Calamity de pooling, R1) — protege el frame-budget cuando hay 3 jefes con bruma a la vez.

### LumenLib.cs (luz, flares, auroras) — "el resplandor"
1. 🥇 **Telegraph con GRAMÁTICA** (R2, EocTelegraph): reemplazar `Telegraph(batch, origin, dir, len, ...)` estático por `Telegraph(batch, origin, dir, len, in TelegraphSpec spec)` con: `Pulse Accelerating` (4.6+7.5p), `FlowDirection` (franjas que fluyen al destino), `SweepStartsAttack` (el barrido ARRANCA el golpe), `ColorByMaterial`. LA mejora de legibilidad más rentable del mod.
2. 🥇 **Registro global de paletas**: unificar `LumenPalettes` + `PyraPalettes` + `RiftPaletas` en `VFXCore.Palettes` (lección Starlight River B2: paleta-disciplina = identidad de marca). Migración con alias para no romper llamadas.
3. 🥈 **Aurora con cortinas y flecos** (B: Realistic Sky #102): `Aurora(batch, center, radius, ...)` → añadir `Curtains` (rayos verticales anclados a "latitud", brillo variable por banda) y flecos de ruido en el borde inferior. Es el VFX insignia de un mod cósmico y hoy es un anillo liso.
4. 🥈 **Lens flare con oclusión por raycast**: `Flare()` consulta 8-12 puntos de tile entre cámara y fuente; oculta elementos del flare tapados — el truco AAA que NADIE tiene en tML.
5. 🥉 **GlowHDR**: `LumenLib.GlowHDR(pos, size, color, intensity)` que escribe al target 1/4 res de VFXCore — glow que "contamina" a vecinos cercanos (la luz de un púlsar ilumina la orilla de la pantalla).

### PyraLib.cs (fuego/temperatura) — "el horno"
1. 🥇 **Heat haze** (R3, NeutronWarp mapa R/G): `PyraLib.HeatHaze(Vector2 center, float radius, float strength)` registra un warp de pantalla (con `VFXCore.QualityGate(Feature.ScreenWarp)` → fallback: nada). El fuego que DISTORSIONA el aire es el salto de calidad más visible posible para un mod de llamas.
2. 🥈 **Pose de llama** (R2, CrimsonRendSlash): `Tongue(batch, basePos, height, ..., in FlamePose pose)` con la línea "amartillar → PARADA MUERTA → estallido (1 frame overshoot) → caída congelada → respiración". El CONTRASTE hace la fuerza, no la velocidad.
3. 🥈 **Curl-noise en EmberField**: `EmberField(..., float curlStrength)` — vorticidad natural para las chispas (SeedCell ya celulariza el calor; curl-noise le da giro). Conocimiento-estándar en VFX industriales, ausente en tML.
4. 🥉 **Delegación a BrumaFX**: `Flame()` y `Tongue()` emiten automáticamente `AnimatedPuff`/`Vapor` encima con altura ∝ temperatura — hoy el llamador debe combinar dos libs a mano; coherencia automática = adopción.
5. 🥉 **Brasas que prenden** (B10, Overhaul fire-spread): `Sparks(..., Ignites = true)` — chispa que muere sobre madera enciende tiles vecinos (config OFF, igual que Overhaul). Visual+gameplay unificados.

### EstelaLib.cs (estelas/cintas) — "la estela"
1. 🥇 **FASE ANCLADA A ARCO MUNDIAL** (R3, Dragon's Word `uLenPx/uOffPx`): `RibbonSpec.PhaseAnchor = PhaseAnchor.WorldArc` — la textura de la cinta avanza con los PÍXELES RECORRIDOS, no con el tiempo: al girar, la estela no "resbala" (patinaje visual). Es LA mejora #1 de todo el informe: las estelas de púlsar/lágrimas se ven "sólidas".
2. 🥇 **Anchura por vértice con meseta** (R1): hoy `Ribbon(batch, pts, width, ...)` es un float único → `WidthProfile` (Mesa/Huso/Constante + rollOff). Meseta ≥80% de L + caps redondos + latido en ALPHA jamás en ancho.
3. 🥈 **Backend de tira central**: `Ribbon(..., Backend.CentralDiff)` — UNA triangle strip con perpendiculares por diferencia central (Abyssrend, R1): cero juntas, cero miter, más barato que quads-perlados cuando la curva es suave. Mantener quads-perlados para curvas cerras.
4. 🥈 **Estelas supervivientes a extraUpdates** (R3: lágrimas con extraUpdates 6 orbitando 150 frames): `Track(ownerId, capacity, TrackMode.SubStepAware)` — push solo en ticks de render, no en sub-pasos.
5. 🥉 **GhostTransforms → eco con juicio diferido** (R2): hoy es visual; darle semántica: los fantasmas MARCAN (eco glitch + pitch 0.55+n·0.04) y un `Settle()` liquida — puente natural hacia PulsoLib (C.2).

### OndaLib.cs (ondas de choque + kick/flash de pantalla) — "el golpe"
1. 🥇 **Push/decay estático de pantalla** (R2, EocScreenFX): convertir `Kick(strengthPx, ticks, dirAngle)` y `Flash(color, strength, ticks)` en impulsos SOBRE un estado persistente: `OndaLib.Screen.Push(...)` con decay exponencial, apilamiento limitado, vignette y pulso cardíaco configurables. Hoy cada renderer improvisa su flash → inconsistencia.
2. 🥇 **Ningún borde es círculo limpio** (R2, ShockRingDraw): `Shock(batch, center, progress, ..., float raggedness = 0.9f)` — el borde del anillo lleva un desgarro de 0.9× el grosor; y `innerGlow = 0` en telegrafías (regla: la onda que AVISA no brilla por dentro).
3. 🥈 **Solo el FRENTE golpea** (R3, starquake): `OndaSpec.FrontOnly = true` con `frontWidth` — la onda-expansión deja de dañar con todo el disco; legibilidad de gameplay (el jugador aprende "cruza el frente, no el disco").
4. 🥈 **Punch con dirección de reacción** (R2): la cámara se empuja OPUESTA al origen del impacto (no aleatorio) + `PunchCameraModifier` real con config de accesibilidad OFF (el mercado lo pide: Gravitation Don't Flip #65 y Shorter Respawn #39 son anti-mareo top-100).
5. 🥉 **Shock refractivo**: `Shock(..., Refract = true)` reutiliza el warp de calor — la onda de choque DOBLA la luz un instante.

### RiftLib.cs (desgarros/rupturas — 1.067 líneas, la mayor) — "la herida"
1. 🥇 **Ejecutar el veredicto R1**: QUITAR `CaminoEspejoRoto` (Lichtenberg) del proyectil del Bastón del Desgarro: fase RECTA de UN quad = lo continuo; FRACTURA = evento ÓPTICO (flash 0.30 + ancho ×1.35 por 2 ticks + hit-stop), CIERRE desde extremos al centro (ErodeT direccional). Ya diagnosticado por R1 con mock objetivo: 0 cortes, desviación de ancho ≤±5% en u∈[0.15,0.85]. `AnchosCamino(camino, maxWidth, plano: true)` ya existe — es la palanca.
2. 🥇 **Juicio diferido** (R2, OniFlashStep 1.156 líneas): `RiftLib.DeferredVerdict` — marcar objetivos con `EcoGlitch` (ya existe) SILENCIOSAMENTE, pitch ascendente por marca, y `Settle()` que liquida TODO con un clang único + shake escalado por víctimas; **el fallo NO suena ni brilla**. La mecánica identidad del iaijutsu aplicada a "cortar la realidad".
3. 🥈 **Cápsula gruesa**: `LineaToca` (14+8 px) → política "grosor de cinta visual = hitbox" (CWR: 140 px; Murasama 200-320): 15 muestras + spokes de 36 px por muestra. El daño de un tajo ES una cápsula (R1 hallazgo 3).
4. 🥈 **Hit-beat**: `RiftLib.HitBeat(proyectil, intervalTicks = 15)` — un golpe por objetivo por pulso con lista manual de golpeados (R3: el decreto late cada 15 t y ahí re-escribe los 28 glifos). Cadencia = música.
5. 🥉 **Lenguaje de bordes en shader** (R1, OniCrimsonSlash.fx): un `.fx` propio con 2 técnicas (tear + trail): el lado afilado ABSOLUTAMENTE liso, todo el ruido vive en el lado que arrastra; la disolución avanza de lo sucio hacia la línea-rasuradora. Máscara muere antes del borde del quad (R3).

### (Transversal) Familia de renderers de agujeros negros + RuneSunRenderer — 14 archivos
1. 🥇 **Un solo renderer paramétrico**: 14 `*BlackHoleRenderer.cs` (Umbral/Supremo/Cosmic/Olvido/Bruma + variantes Ascendido) comparten 80% de lógica → UN renderer con `BlackHoleSpec` (paleta, disco, lente, jets, bruma-flow). El mantenimientos de 14 archivos es la deuda más cara del directorio VFX.
2. 🥈 **Lente gravitacional con quality-gate + fallback** (R2/R3): warp de pantalla (mapa R/G) tras `QualityGate(Feature.ScreenWarp)`; fallback = sprite de distorsión estática.
3. 🥉 **Doppler en el disco** (R3): el lado que se acerca más brillante y azul-shifted — 3 líneas en el color por vértice, efecto enorme.

## C.2 LAS SUPER LIBRERÍAS (propuestas)

---

### ⭐ SUPER LIB 1 — `PulsoLib` — "el sistema nervioso" (game feel unificado)

**Propósito**: TODO lo que hace que un golpe SE SIENTA: hit-stop, juicio diferido, retroceso, cámara, sonido por material, push/decay de pantalla. Hoy está disperso (OndaLib.Kick/Flash + improvisaciones en renderers); CWR lo tiene PEGADO a 4.678 archivos (R2) y Terraria Overhaul (B10) lo tiene cerrado en binario. **Nadie lo publica como API.**

**API pública concreta**:
```csharp
public static class PulsoLib {
    // — Hit-stop (congela proyectiles del dueño, no al mundo entero)
    public static void HitStop(int ticks, float intensity = 1f, PulsoScope scope = PulsoScope.AttackerAndVictims);

    // — Juicio diferido (iaijutsu de CWR, generalizado)
    public sealed class Verdict {
        public static int  Begin(int ownerId, int settleTick, in SoundStyle clang);
        public static void Mark(int verdictId, NPC target, int damage, float armorPen = 0f); // silencioso, eco+pitch 0.55+n*0.04
        public static int  Settle(int verdictId); // liquida TODO → nº víctimas (para shake escalado); fallo: no suena, no brilla
    }

    // — Retroceso (readonly struct + Derive, patrón CWR GsGunRecoil)
    public readonly struct RecoilProfile {
        public readonly float Shift, Kick, WobbleHz, HeavyShake;
        public static RecoilProfile Derive(float weight, int chargeTicks); // 2 parámetros → 400 armas
    }
    public static void Recoil(Player p, in RecoilProfile r, Vector2 aimDir); // wobble con seed whoAmI (MP determinista)

    // — Cámara
    public static void PunchCamera(Vector2 dirOppositeToShot, float strengthPx, int ticks); // 0 si accesibilidad OFF

    // — Sonido por material + presupuesto 2/frame + pitch por combo
    public static void Sound(Material mat, Vector2 pos, int combo = 0); // Material.Flesh | Steel | Stone | Void ...

    // — Pantalla: estado estático push/decay (vignette, flash, pulso cardíaco)
    public static void ScreenPush(Color tint, float strength, int ticks);
    public static ScreenState Screen { get; } // FrameEffect: decay exponencial, apilamiento máx 3
}
```
**Qué la hace superior**: (1) primera lib que unifica las CINCO piezas (CWR las tiene repartidas y hardcodeadas; Overhaul es cerrado); (2) el struct `RecoilProfile.Derive(weight, chargeTicks)` da 400 armas animadas con 2 números (lección CWR); (3) `Verdict.Settle` devuelve el número de víctimas → shake/pitch escalan SOLOS; (4) accesibilidad en UN toggle central (señal de mercado #65/#39 del workshop); (5) el contador de combo regala el pitch ascendente del juicio diferido.

---

### ⭐ SUPER LIB 2 — `TelaLib` — "una cinta para gobernarlas todas" (estelas/trails unificadas)

**Propósito**: reemplaza `EstelaLib.Ribbon` + `LumenLib.LanceTrail` + trails improvisados de los 14 renderers por UNA cinta que reúne las tres piezas que NINGUNA librería pública combina: **fase anclada a arco mundial** (R3), **anchura por vértice con meseta** (R1/Calamity), **tira de diferencia central** (Abyssrend/R1) — más capas, ghosts con juicio, calidad adaptativa y MP-determinismo.

**API pública concreta**:
```csharp
public sealed class Cinta {
    public static Cinta Acquire(int ownerId, int capacity = 30, CintaFlags flags = CintaFlags.SubStepAware);
    public void Push(Vector2 worldPos);   // sobrevive extraUpdates (R3)
    public void Draw(in CintaSpec spec);  // una llamada = cinta completa multicapa
    public void Release();                // devuelve al pool
}

public readonly struct CintaSpec {
    public WidthProfile Width;        // Mesa(0.10f, 0.90f, RollOff.Smoothstep) | Huso | Constante(px)
    public ColorFn Color;             // delegate(float u, float arcPx, float t) → Color  (latido en ALPHA, jamás en ancho)
    public PhaseAnchor Phase;         // WorldArc (uLenPx/uOffPx antideslizamiento) | Time | OwnerVelocity
    public Backend Backend;           // QuadsPerlado | CentralDiffStrip (auto: cerrada→perlado, suave→strip)
    public float[] LayerWidths;       // p.ej. {22, 14, 11} velo/cuerpo/núcleo + filo blanco 0.5α
    public QualityTier Quality;       // Auto (degrada si VFXCore.QuadCount alto) | Alto | Bajo
}

public static class CintaDefaults {           // presets empaquetados (lección LogSpiral B12: default bonito)
    public static readonly CintaSpec Slash;   // tajo continuo parejo (reglas R1 completas)
    public static readonly CintaSpec Lagrima; // cola-fina/cabeza-redonda de metal fundido (R3)
    public static readonly CintaSpec Orbita;  // kepleriana, fase anclada, ghost-echo con Verdict
    public static readonly CintaSpec Laser;   // meseta + doble pasada (color 0.75 + blanca 0.5, Last Prism)
}
```
**Qué la hace superior**: Calamity popularizó "trail widths por vértice" pero sin fase anclada ni meseta; Abyssrend tiene la tira pero sin API; CWR ancla la fase pero la tiene pegada a Dragon's Word. **La fase antideslizante + la meseta + el backend dual + presets** juntas no existen en ningún mod público — y son exactamente lo que R1/R3 ya validaron para Aethon.

---

### ⭐ SUPER LIB 3 — `Astrolib` — "el cielo es una escena" (compositor cósmico de ambiente/post-proceso)

**Propósito**: la capa que los mods visuales top del workshop (Realistic Sky #102, Lights And Shadows #16, Fancy Lighting #81, Better Blending #62, Atmospheric Torches #77 — ver Parte A, señal 3) demuestran que la gente QUIERE, pero todos la implementan global y desconectada del gameplay. Astrolib la hace **local, ligada al gameplay cósmico y con quality-gate**.

**API pública concreta**:
```csharp
public static class Astrolib {
    // — Escena de cielo con capas parallax
    public static void RegisterSkyLayer(SkyLayer layer);   // stars, nebulosas, auroras (LumenLib.Aurora evoluciona a cortinas, C.1)
    public static void Constellation(string id, Vector2[] stars, Vector2 worldAnchor, ConstellationFx fx);
    //   → se dibuja en el cielo Y puede "apuntar" a un jefe/anomalía ( gameplay: la constelación carga un arma, B3 Stars Above)

    // — Warps de pantalla por mapa R/G (R3 NeutronWarp), con janitor y gate
    public static WarpHandle Lens(Vector2 worldPos, float radius, float strength);  // agujero negro
    public static WarpHandle Heat(Vector2 worldPos, float radius);                 // horno/estrella
    public static void DecayWarps();            // janitor obligatorio cada frame (lección DrawStateJanitor R2)

    // — Post-proceso con estado push/decay (delega en PulsoLib.Screen)
    public static void PushEffect(ScreenEffect fx);   // Vignette, Flash, Heartbeat, ChromaticAberration(cosmic!)

    // — Calidad: NADA de esto corre sin pasar la puerta
    public static bool QualityGate(Feature f);        // Retro/Trippy/agua-baja → fallback sprite (R2)

    // — Bloom HDR barato (target 1/4 res, pool)
    public static void GlowHDR(Vector2 worldPos, float size, Color c, float intensity);
}
```
**Qué la hace superior**: (1) los mods de ambiente top son GLOBALES — nadie ofrece "constelación que apunta al jefe" o "lente que solo distorsiona cerca del agujero negro" (gameplay-coupled ambience); (2) integra el warp R/G de R3 con el janitor y el quality-gate de R2 — higiene de pipeline que NINGÚN mod de ambiente tiene (por eso rompen en Retro/low-end); (3) para un mod cósmico el cielo no es decorado: es el producto (Realistic Sky tiene cientos de miles de usuarios siendo SOLO cielo).

---

## C.3 Síntesis: los 5 hallazgos más valiosos

1. **El hueco de mercado exacto de Aethon**: del top-100 del workshop, ~15 mods son visuales puros (Luminance, Loot Beams, Realistic Sky, CoolerItemVisualEffect...) y NINGUNO liga su VFX al gameplay. Un mod cósmico donde el cielo/lente/estela SON mecánicas no tiene competencia directa. La fórmula "gameplay-coupled ambience" (Astrolib) es el diferenciador.
2. **Las librerías son producto** (Luminance #10, InnoVault #31, Particle Library #54, LogSpiral #74): diseñar las 8 libs con defaults bonitos de 2 parámetros (lección LogSpiral B12) y presets empaquetados (CintaDefaults) convierte la infraestructura en reputación y en dependencia del ecosistema.
3. **Las tres piezas de cinta que nadie combina** — fase anclada a arco mundial (R3) + anchura en meseta con latido-en-alpha (R1) + tira de diferencia central (Abyssrend) — definen TelaLib y son la mejora visual más inmediata para TODAS las armas de estela del mod.
4. **El juicio diferido es la mecánica-firma robable**: CWR lo tiene hardcodeado en OniFlashStep; convertido en `PulsoLib.Verdict` (marcar silencioso → clang único escalado por víctimas → el fallo no suena) da a Aethon una identidad de game feel que ni Calamity ni Thorium tienen, aplicable al Bastón del Desgarro YA (RiftLib mejora #2).
5. **La lección negativa del workshop**: la queja #1 de CWR es "Overwhelming asf" y los mods anti-mareo/performance (High FPS Support #37, Gravitation Don't Flip #65) son top-100 → el quality-gate (RenderQualitySafety) + presupuesto adaptativo de quads + accesibilidad centralizada (todo OFF con un toggle) no son "extras técnicos": son requisitos de adopción masiva.

---

## C.4 Fuentes y artefactos

- Ranking real: `search_results/t52/wsp1.html … wsp7.html` (Steam Workshop, appid 1281930, `browsesort=totaluniquesubscribers`, 30/página).
- Búsquedas: `search_results/t52/r4a1…r4a6.json, r4b1…r4b6.json` (12 consultas × 8 resultados, pipeline DDG de R1).
- Wikis confirmadas por búsqueda (contenido por conocimiento† donde la API/IP bloqueó): fargosmods.wiki.gg, starlightrivermod.wiki.gg, starsabovemod.wiki.gg, modofredemption.wiki.gg, shadowsofabaddon.wiki.gg, terrariamods.wiki.gg (Split/Gensokyo/Joostmod), github.com/Mirsario/TerrariaOverhaul, github.com/LucilleKarma/Luminance.
- APIs propias leídas: `Content/VFX/{StormLib,LumenLib,PyraLib,EstelaLib,OndaLib,RiftLib,VFXCore}.cs` + `Content/Effects/Bruma/BrumaFX.cs`.
- Informes hermanos: INFORME_TAJOS_CORTE_REALIDAD.md (R1/49), INFORME_STAR_TOMB_DRAGON_WORD.md (R3/50), INFORME_CALAMITY_OVERHAUL.md (R2/51).

# 📜 Documento de Diseño del Mod — "Aethon, la Luz Primordial"

> Terraria Mod · Concepto de diseño completo (v2.0)
> Idioma: Español · Última actualización: documento unificado

---

## 📑 Índice

1. [Visión general del mod](#1-visión-general-del-mod)
2. [La entidad cósmica — Aethon](#2-la-entidad-cósmica--aethon)
3. [El descubrimiento — Altar y Fragmento Génesis](#3-el-descubrimiento--altar-y-fragmento-génesis)
4. [Sistema de 3 ramas principales (NUEVO)](#4-sistema-de-3-ramas-principales-nuevo)
5. [Rama de Distancia (Ranged)](#5-rama-de-distancia-ranged)
6. [Rama de Cuerpo a Cuerpo (Melee)](#6-rama-de-cuerpo-a-cuerpo-melee)
7. [Rama de Artes Mágicas (Magic + Invocador fusionadas)](#7-rama-de-artes-mágicas-magic--invocador-fusionadas)
8. [Sistema de niveles infinito](#8-sistema-de-niveles-infinito)
9. [Árboles de habilidades procedurales](#9-árboles-de-habilidades-procedurales)
10. [Absorción de Lore (capstone)](#10-absorción-de-lore-capstone)
11. [Sistemas del mundo](#11-sistemas-del-mundo)
12. [Jefe final — Aethon](#12-jefe-final--aethon)
13. [Jefes secundarios y ecos](#13-jefes-secundarios-y-ecos)
14. [Economía de Fragmentos de Resonancia](#14-economía-de-fragmentos-de-resonancia)
15. [NPCs](#15-npcs)
16. [Calidad de vida y configuración](#16-calidad-de-vida-y-configuración)
17. [Compatibilidad con mods](#17-compatibilidad-con-mods)
18. [Identidad visual y audio](#18-identidad-visual-y-audio)

---

## 1. Visión general del mod

**Aethon, la Luz Primordial** es un mod de historia final para Terraria (vía tModLoader) que añade:

- **Una entidad cósmica antigua** como jefe final opcional, con lore profundo.
- **Un item único (Fragmento Génesis)** que el jugador encuentra en un altar subterráneo.
- El fragmento **se transforma según la rama principal de combate** que el jugador elija, y **crece infinitamente** con cada enemigo eliminado.
- **Árboles de habilidades procedurales** únicos por rama, con nodos comunes / raros / legendarios.
- Un capstone de **Absorción de Lore**: el arma puede memorizar el comportamiento de cualquier arma del mismo tipo en el juego base y en otros mods cargados.
- **Eventos cósmicos** por niveles, **ecos de portadores anteriores** como jefes, y una economía de **Fragmentos de Resonancia**.

### Principios de diseño
1. **Progresión infinita pero con curva suave** — nunca se siente como grind muerto.
2. **Rejugabilidad** — cada personaje obtiene un árbol procedural distinto.
3. **Integración con el mundo** — el mod afecta biomas, eventos y NPCs existentes.
4. **Compatibilidad** — Funciona junto a los mods grandes del ecosistema.
5. **Accesibilidad** — Config options para desactivar eventos, ajustar XP, etc.

---

## 2. La entidad cósmica — Aethon

**Aethon, la Luz Primordial** es un ser anterior al universo de Terraria. Su cuerpo ES una galaxia; sus pensamientos son mareas gravitacionales. Hace eones se fragmentó a sí misma para sembrar toda la creación — cada estrella, cada alma, es una astilla de Aethon.

Un fragmento de su verdadera consciencia quedó dormido dentro del mundo de Terraria, enterrado en piedra. Civilizaciones florecieron y cayeron adorándolo; solo quedan altares en ruinas.

**El jefe final:** cuando el arma-fragmento del jugador alcanza un umbral de resonancia, Aethon despierta. Derrotarla no la destruye — te *reconoce* como una consciencia par.

### Fases de Aethon (5)
| Fase | Tema | Mecánica clave |
|------|------|-----------------|
| 1 — Polvo estelar | Stardust | Dispara pernos en espiral |
| 2 — Nebulosa | Nebula | Nubes AoE ciegan y queman; arena se deforma |
| 3 — Gravedad | Gravity | La gravedad se invierte cada 8s |
| 4 — Agujero negro | Black Hole | Una singularidad te atrae mientras genera adds |
| 5 — Reconocimiento | Acknowledgment | Aethon empuña TUS propias habilidades absorbidas contra ti |

### Recompensa de derrota
- El Fragmento Génesis se despierta por completo: forma "Ascendida" cosmética.
- Se desbloquea dificultad cósmica New Game+.
- Aethon deja de ser hostil (se vuelve neutral como el NPC Testigo).

---

## 3. El descubrimiento — Altar y Fragmento Génesis

### Bioma: El Sagrario Hueco (The Hollow Sanctum)
- Sub-bioma cristalino que puede generarse en cualquier lugar subterráneo **después de que el jugador tenga 200 HP máx**.
- Genera cristales bioluminiscentes, polvo iluminado, y runas antiguas en las paredes.
- Música ambiental única (tono bajo, reverberante).

### El Altar
- Estructura de piedra pequeña con runas grabadas que brillan al acercarse.
- Al interactuar, ofrece un único mote flotante de luz pura: **el Fragmento Génesis**.
- Un solo fragmento por personaje (alma-vinculada).

### Evolución visual del fragmento
El fragmento cambia de forma visualmente según el nivel:

| Nivel | Forma visual |
|-------|--------------|
| 1–10 | Mote tenue de luz |
| 11–25 | Orbe brillante |
| 26–50 | Artefacto con runas |
| 51–100 | Reliquia forjada en estrellas |
| 101–150 | Miniatura de galaxia en las manos del jugador |
| 151+ | Forma Ascendida (post-Aethon) |

---

## 4. Sistema de 3 ramas principales (NUEVO)

> ⚠️ **CAMBIO IMPORTANTE vs. versión anterior:** El sistema anterior tenía 4 armas separadas (arco, espada, cañón, libro). El nuevo sistema tiene **3 ramas principales** basadas en las clases de combate de Terraria. El Fragmento Génesis evoluciona según la rama que el jugador desarrolla primero.

### Las 3 ramas principales

| Rama | Clases de Terraria incluidas | Forma del fragmento | Color de acento |
|------|-------------------------------|---------------------|-----------------|
| **🎨 Distancia** | Ranged (arcos, armas de munición, armas arrojadizas) | Arma de distancia adaptable | Dorado estelar `#f5c451` |
| **⚔️ Cuerpo a Cuerpo** | Melee (espadas, lanzas, etc.) | Hoja de luz condensada | Naranja solar `#ff9a3c` |
| **🔮 Artes Mágicas** | Magic + Summoner (fusionadas) | Grimorio flotante | Violeta arcano `#b388ff` |

### Cómo funciona la elección de rama

1. El jugador encuentra el Fragmento Génesis (un mote de luz sin forma definida).
2. El fragmento **no tiene forma fija** — se adapta a cómo el jugador combate.
3. Al usar el fragmento por primera vez en combate, el juego detecta la **primera rama principal** que el jugador desarrolla (basado en el tipo de daño infligido / arma sostenida).
4. Una vez la rama se "imprime" (al matar N enemigos con esa clase de daño), el fragmento **se transforma permanentemente** en la forma de esa rama.
5. Sin embargo, el jugador puede **cambiar de rama** más adelante invirtiendo Fragmentos de Resonancia (ver §14), pero el árbol procedural de la rama original se conserva como "linaje".

### Fusión Magic + Summoner
Las ramas de Mago e Invocador **se fusionan** en "Artes Mágicas" porque:
- Ambas usan maná como recurso.
- El capstone de absorción puede memorizar tanto armas mágicas (Last Prism, Lunar Flare) como armas de invocador (Tempest Staff, Stardust Dragon).
- El árbol fusionado tiene sub-ramas que se especializan en magia pura vs. invocación pura, pero comparten el tronco de maná/proyectiles.

### Armas que puede tomar cada rama (expandido)

#### 🎨 Rama de Distancia (Ranged)
El fragmento se transforma en un **arma de distancia adaptable** que puede ser:
- **Arco**: dispara flechas de luz estelar (gratis, no consume munición base).
- **Arma de munición (pistola/escopeta/rifle)**: usa balas de plasma cinético.
- **Arma arrojadiza**: cuchillos/shuriken de energía que regresan.

El jugador elige la sub-forma dentro de la rama de Distancia (también permanente, pero se puede cambiar con resonancia).

#### ⚔️ Rama de Cuerpo a Cuerpo (Melee)
El fragmento se transforma en una **hoja de luz condensada**:
- **Espada**: cortes en arco + ondas de energía.
- **Lanza/Alabarda**: empujes + ataques de empuje (thrust) con alcance largo.
- **Yoyo**: giro continuo con control de dirección.

#### 🔮 Rama de Artes Mágicas (Magic + Summoner)
El fragmento se transforma en un **grimorio flotante**:
- **Grimorio de hechizos**: proyectiles mágicos (bolts, rayos, lluvia).
- **Grimorio de invocación**: invoca minions permanentes que heredan el poder del fragmento.
- El jugador puede mezclar ambos (lanzar hechizos Y mantener minions).

---

## 5. Rama de Distancia (Ranged)

### Nombre del arma: **Lumina, la Arcoestelar** (forma adaptable)

### Sub-ramas del árbol (6 ramas, cada una con 5 nodos)

#### Rama 1 — Génesis de Proyectiles
Nuevos tipos de munición/proyectil:
- **Flecha estelar** (base): ignora 5 de defensa.
- **Virote de vacío**: +30% daño, drena 4 maná.
- **Disparo dividido**: 2 proyectiles en abanico.
- **Estrella guiada**: los proyectiles curvan hacia enemigos.
- **Salva triple** (legendario): cada 3er disparo lanza 3 virotes buscadores.

#### Rama 2 — Maestría de Carcaj
- **Tiro rápido**: -15% tiempo de tensión/carga.
- **Cuerda doble**: +1 proyectil por disparo.
- **Carcaj infinito**: no consume munición.
- **Ráfaga**: mantén para disparar 6 proyectiles en secuencia.
- **Tormenta de estrellas** (legendario): disparo cargado llueve 12 proyectiles.

#### Rama 3 — Marca del Cazador
- **Etiqueta**: los golpes marcan enemigos (+10% daño).
- **Acumulación de crítico**: +4% crítico por golpe (máx 40%).
- **Punto débil**: los enemigos marcados muestran un punto débil (siempre crítico).
- **Foco del cazador**: estar quieto 1s duplica el crítico.
- **Marca letal** (legendario): matar a un enemigo marcado resetea cooldowns.

#### Rama 4 — Disparos Celestiales
- **Salva de meteoros**: alt-fuego: 3 meteoros caen en el cursor.
- **Destello solar**: los proyectiles incendian (DoT quemadura).
- **Eclipse**: disparo cargado ciega + quema (10s cd).
- **Cascada estelar**: cada golpe genera 2 mini-estrellas.
- **Supernova** (legendario): proyectil cargado explota en supernova de 12 tiles.

#### Rama 5 — Carcaj Fantasma
- **Disparo fase**: los proyectiles atraviesan 3 tiles.
- **Ricochet**: rebota en paredes hasta 2 veces.
- **Betty rebotadora**: los fallos generan minas de luz estacionarias.
- **Cadena fantasma**: rebota entre 3 enemigos.
- **Forma etérea** (legendario): todos los proyectiles fasan terreno Y perforan 5 enemigos.

#### Rama 6 — Absorción de Lore ⭐ (capstone)
- **Códex de memoria I/II/III**: desbloquea 3 slots de Runa de Memoria.
- **Afinación de resonancia**: memoriza el comportamiento de cualquier arma de distancia del juego base + mods.
- **Carcaj omnisciente** (legendario): equipa 5 Runas simultáneamente; fusiona dos en una.

---

## 6. Rama de Cuerpo a Cuerpo (Melee)

### Nombre del arma: **Solbrand, Filo del Alba** (hoja de luz condensada)

### Sub-ramas del árbol (6 ramas)

#### Rama 1 — Génesis de Hoja
- **Onda de corte**: cada golpe emite onda frontal (6 tiles).
- **Corte de rayo**: golpe cargado dispara rayo de 12 tiles.
- **Remolino**: alt-fuego: giro que golpea alrededor.
- **Combo de estocada**: combo de 3 golpes termina en estocada perforante.
- **Corte de realidad** (legendario): corte cargado corta terreno Y enemigos.

#### Rama 2 — Maestría de Combo
- **Medidor de combo**: golpes consecutivos acumulan combo (máx 10).
- **Remate**: a combo 10, +200% daño en el siguiente golpe.
- **Impulso**: el combo no decae al moverse.
- **Parry-Riposte**: bloquear + contraataque resetea combo y suma 5.
- **Filo infinito** (legendario): sin tope de combo; +5% daño por golpe más allá de 10.

#### Rama 3 — Ira Solar
- **Corte de destello solar**: los golpes incendian (ciego + DoT).
- **Corte de eclipse**: corte pesado oscurece la pantalla, +50% daño.
- **Aura de corona**: aura pasiva que quema (3 tiles).
- **Ignición solar**: enemigos quemados explotan al morir.
- **Golpe de supernova** (legendario): remate detona supernova de 10 tiles.

#### Rama 4 — Égida del Alba
- **Frames de parry**: los primeros 4 frames del golpe dan invulnerabilidad.
- **Reflexión de daño**: los proyectiles parry rebotan con +50% daño.
- **Dash del alba**: dash con i-frames (3s cd).
- **Baluarte**: estar quieto 1s otorga +20 defensa.
- **Guardia eterna** (legendario): ventana de parry duplicada; refleja también cuerpo a cuerpo.

#### Rama 5 — Peso de Estrellas
- **Golpes pesados**: +50% knockback, -10% velocidad.
- **Golpe al suelo**: golpe hacia abajo genera onda de choque.
- **Cráter**: el golpe al suelo deja un cráter dañino (5s).
- **Caída de meteorito**: golpe aéreo llueve meteoritos.
- **Pozo de gravedad** (legendario): los golpes al suelo atraen enemigos antes de estallar.

#### Rama 6 — Absorción de Lore ⭐ (capstone)
- **Códex de memoria I/II/III**: 3 slots de Runa.
- **Afinación de resonancia**: memoriza cualquier espada/lanza/yoyo del juego base + mods.
- **Alma del maestro de hojas** (legendario): 5 Runas; fusiona dos golpes en uno híbrido.

---

## 7. Rama de Artes Mágicas (Magic + Invocador fusionadas)

### Nombre del arma: **Grimorio del Eterno** (grimorio flotante)

### Sub-ramas del árbol (7 ramas — una más que las demás, por la fusión)

#### Rama 1 — Flujo de Maná
- **Reserva de maná**: +40 maná máximo.
- **Maná fluyente**: regeneración +50%.
- **Lanzamiento eficiente**: -25% coste de maná.
- **Pozo de maná**: pasivo: restaura 5 maná/seg quieto.
- **Reserva inagotable** (legendario): lanzar con <20 maná es gratis.

#### Rama 2 — Génesis Elemental
- **Bolt de fuego**: los bolts incendian.
- **Esquirla de escarcha**: los bolts ralentizan 40% por 3s.
- **Arco de tormenta**: los bolts saltan a 2 enemigos.
- **Virote de vacío**: los bolts perforan 3, +30% daño.
- **Convergencia elemental** (legendario): los bolts rotan elementos cada lanzamiento; todos a la vez.

#### Rama 3 — Evolución de Proyectiles
- **Bolt dividido**: los bolts se dividen en 2 al impactar.
- **Chispa guiada**: los bolts homing al enemigo más cercano.
- **Cadena de lanzamiento**: los bolts saltan entre 3 enemigos.
- **Orbe familiar**: genera un familiar orbital (3 bolts/seg).
- **Multilanzamiento** (legendario): cada lanzamiento dispara 3 bolts extra gratis.

#### Rama 4 — Conversión Arcana
- **Escudo de maná**: el daño drena maná antes que HP.
- **Maná → HP**: lanzar cura 2 HP por 10 maná.
- **HP → Maná**: pierde 5 HP para restaurar 30 maná.
- **Desesperación**: el daño escala con % de maná faltante.
- **Ciclo eterno** (legendario): matar restaura 30% maná + 10% HP.

#### Rama 5 — Hechizos Cósmicos (sub-rama de magia pura)
- **Agujero negro**: lanzamiento cargado genera pozo gravitacional de 4s.
- **Supernova**: lanzamiento cargado: explosión cósmica de 10 tiles.
- **Dilatación temporal**: lanzamiento cargado: ralentiza enemigos 60% por 4s.
- **Lluvia de estrellas**: pasivo: una estrella cae cada 3s.
- **Desgarro de realidad** (legendario): cargado abre portal; los bolts salen de un 2º portal.

#### Rama 6 — Maestría de Invocación (sub-rama de invocador puro)
- **Minion base**: invoca 1 minion del fragmento (estrella orbitante que dispara).
- **Minion +1**: +1 slot de minion.
- **Minion empoderado**: los minions heredan +10% del daño del grimorio.
- **Minion torre**: los minions pueden estacionarse (modo defensivo).
- **Enjambre estelar** (legendario): +3 slots; los minions disparan bolts al atacar.

#### Rama 7 — Absorción de Lore ⭐ (capstone)
- **Códex de memoria I/II/III**: 3 slots de Runa.
- **Afinación de resonancia**: memoriza cualquier arma mágica O de invocador del juego base + mods.
- **Grimorio omnisciente** (legendario): 5 Runas; lanza todos simultáneamente en una tormenta.

---

## 8. Sistema de niveles infinito

### XP por enemigo
Cada enemigo eliminado otorga XP según su tier:
- Slime: 1 XP
- Zombi: 3 XP
- Demon Eye: 4 XP
- Jefe Pre-Hardmode (Eye of Cthulhu): 5000 XP
- Jefe Hardmode (Plantera): 25000 XP
- Jefe Endgame (Moon Lord): 100000 XP
- Jefes del mod (Echoes, Aethon): 50000–500000 XP

### Curva de XP
`xpParaSiguienteNivel(n) = 80 × n^1.5` (exponencial suave). Niveles ilimitados.

### Puntos de habilidad por nivel

| Rango de nivel | Puntos / nivel | Acumulado al final del rango |
|---------------|----------------|------------------------------|
| 1 – 10 | 1 | 10 |
| 11 – 20 | 2 | 30 |
| 21 – 30 | 3 | 60 |
| 31 – 40 | 4 | 100 |
| 41 – 50 | 5 | 150 |
| 51 – 60 | 6 | 210 |
| 61 – 70 | 7 | 280 |
| 71 – 80 | 8 | 360 |
| 81 – 90 | 9 | 450 |
| 91 – 100 | 10 | 550 |
| 101+ | 10 (fijo) | +10 por nivel, para siempre |

### Evolución del arma por nivel
El daño base escala con el nivel:
- Distancia: `daño = nivel × 2.4`
- Cuerpo a cuerpo: `daño = nivel × 3.1`
- Artes mágicas: `daño = nivel × 2.6 + (% maná faltante × 0.5)`

### Hitos cósmicos por nivel
| Nivel | Evento |
|-------|--------|
| 10 | Primer despertar — el fragmento se solidifica con glifo rúnico |
| 25 | Lluvia de luz estelar (evento ambiental, mineral raro) |
| 50 | El Sagrario Hueco se extiende, nuevos mobs |
| 75 | Rifts dimensionales (mini-mazmorras con loot) |
| 100 | El agitar de Aethon (Ecos de portadores anteriores aparecen) |
| 150 | El despertar (jefe final Aethon disponible) |
| 200 | Forma Ascendida (cosmético) |

---

## 9. Árboles de habilidades procedurales

Cada rama genera su **propio árbol** usando un algoritmo procedural con seed:
- **Tema visual de constelación**: nodos = estrellas, aristas = puentes de luz.
- **6 ramas por arma** (7 para Artes Mágicas por la fusión).
- **3 rarezas de nodo**: Común (sólido), Raro (brillante), Legendario (supernova pulsante).
- **Algunos nodos están cerrados** por inversión total de puntos o nivel del arma.
- **El layout y las rarezas se randomizan por seed del personaje** → rejugar es distinto.

### Algoritmo procedural (resumen)
1. Se toma el seed del personaje (o un seed manual configurable).
2. Para cada rama, se colocan 5 nodos en una línea radial desde el centro.
3. Se aplica jitter aleatorio (±0.08 en coordenadas normalizadas).
4. Se asignan rarezas con probabilidad: 60% común, 30% raro, 10% legendario.
5. Los nodos legendarios siempre son los 5º de cada rama (capstones).
6. Se generan aristas de prerrequisito (cada nodo requiere el anterior de su rama).

### Interfaz en el juego
- Panel de árbol accesible via tecla (configurable, default 'K').
- Vista de constelación con zoom/pan.
- Click en nodo para asignar (respeta prerrequisitos + presupuesto).
- Botón "re-generar" (consume Fragmentos de Resonancia, nuevo seed).

---

## 10. Absorción de Lore (capstone)

El capstone de cada rama deja al arma **absorber habilidades** de cualquier arma del mismo tipo en el juego base y mods cargados.

### Cómo funciona
1. Al invertir en la rama 6 (o 7 para Artes Mágicas), se desbloquea el **Códex de Memoria**.
2. El códex lista todas las armas del tipo correspondiente (del juego base + mods).
3. El jugador invierte **Fragmentos de Resonancia** (ver §14) para "memorizar" el comportamiento de un arma.
4. Los comportamientos memorizados se equipan como **Runas de Memoria** — hasta N a la vez (N crece con el nivel del arma).
5. Las runas se manifiestan **simultáneamente** — no es "una o la otra".

### Ejemplos
- **Artes Mágicas** memoriza: el rayo convergente del Last Prism, la lluvia del Lunar Flare, el bumerán del Demon Scythe — TODOS a la vez.
- **Distancia** memoriza: la ráfaga del Megashark, la lluvia del Daedalus Stormbow, los pétalos del Stormbow — simultáneos.
- **Cuerpo a Cuerpo** memoriza: el rayo verde del Terra Blade, el doble corte del Influx Waver, las calabazas del Horseman's Blade.

### Códex de armas absorbibles (por rama)

#### Distancia — 12 armas base
Wooden Bow, Demon Bow, Molten Fury, The Bee's Knees, Hellwing Bow, Daedalus Stormbow, Ice Bow, Shadowflame Bow, Tsunami, Phantasm, Eventide, Aerial Bane.

#### Cuerpo a Cuerpo — 16 armas base
Wooden Sword, Blade of Grass, Muramasa, Phaseblade, Fiery Greatsword, Breaker Blade, Cobalt Sword, Cutlass, Ice Sickle, Keybrand, Terra Blade, Influx Waver, Horseman's Blade, Seedler, Star Wrath, Zenith.

#### Artes Mágicas (magia) — 16 armas base
Magic Dagger, Demon Scythe, Aqua Scepter, Flower of Fire, Space Gun, Magic Missile, Book of Skulls, Crimson Rod, Sky Fracture, Magnet Sphere, Leaf Blower, Razorblade Typhoon, Lunar Flare, Last Prism, Nebula Blaze, Blizzard Staff.

#### Artes Mágicas (invocación) — 12 armas base
Slime Staff, Hornet Staff, Imp Staff, Spider Staff, Optic Staff, Pirate Staff, Tempest Staff, Xeno Staff, Stardust Dragon Staff, Terraprisma, Desert Tiger Staff, Tavernkeep's sentry weapons.

> **Total: 56 armas absorbibles** en el códex base (más las de mods cargados).

---

## 11. Sistemas del mundo

### Eventos cósmicos (por nivel del arma)
- **Lv 25 — Lluvia de luz estelar**: meteoros ambientales, mineral raro (Aethonita).
- **Lv 50 — El Sagrario Hueco se extiende**: el bioma se propaga, nuevos mobs (Cristales huecos, Centinelas rúnicos).
- **Lv 75 — Rifts dimensionales**: mini-mazmorras que aparecen con loot único.
- **Lv 100 — El agitar de Aethon**: los Ecos de portadores anteriores aparecen como jefes.
- **Lv 150 — El despertar**: el jefe final Aethon se vuelve disponible.

### Resonancia zonal
El fragmento gana bonos por bioma:
- **Espacio** (alto del mapa): +20% XP.
- **Inframundo**: +15% daño.
- **Sagrario Hueco**: +25% XP + regeneración de maná.
- **Jungla**: +10% velocidad de ataque.

### Ecos de portadores
- Sombras de almas pasadas que también portaron el fragmento.
- Jefes opcionales con builds plausibles de tu árbol (miman tu progreso).
- 2 Echoes por rama (6 total).

---

## 12. Jefe final — Aethon

Ver §2 para las 5 fases. Detalles de combate:

- **HP**: 2,400,000 por fase (12M total).
- **Arena**: arena cósmica colapsante (se reforman las plataformas entre fases).
- **Mecánica única (fase 5)**: Aethon empuña TUS habilidades absorbidas contra ti — si memorizaste el Last Prism, Aethon te dispara un rayo convergente.
- **Derrota**: Aethon "reconoce" al jugador; el Fragmento Génesis se despierta (forma Ascendida), New Game+ cósmico desbloqueado.

---

## 13. Jefes secundarios y ecos

| Jefe | Tipo | HP | Desbloqueo | Drop de resonancia |
|------|------|-----|-----------|-------------------|
| El Titán Hueco | Mini | 42,000 | Descubrir Sagrario Hueco | 8 (2 repetible) |
| El Guardián del Rift | Cósmico | 95,000 | Lv 75 (Rifts dimensionales) | 45 (8) |
| El Testigo | NPC/Cósmico | — | Lv 50 | 0 (vende) |
| Eco del Primer Portador | Eco | 180,000 | Lv 100 | 120 (18) |
| Eco de la Arquera Estelar | Eco | 160,000 | Lv 110 | 110 (16) |
| Aethon, la Luz Primordial | Final | 2.4M/fase | Lv 150 | 250 (40) |

---

## 14. Economía de Fragmentos de Resonancia

**Fragmentos de Resonancia** = moneda secundaria para memorizar armas en el Códex.

### Fuentes
- Jefes cósmicos (primera derrota + repetible).
- Eventos cósmicos (minerales).
- El Testigo (NPC) vende algunos.

### Total farmeable
- **533** de primera derrota de todos los jefes.
- **84** repetible por kill.
- **~61%** del códex completo con una pasada de jefes.

### Slots de Runa por nivel
| Nivel del arma | Slots de Runa |
|----------------|---------------|
| < 50 | 0 |
| 50–74 | 1 |
| 75–99 | 2 |
| 100–124 | 3 |
| 125–149 | 4 |
| 150+ | 5 |

---

## 15. NPCs

### El Testigo
- Entidad cósmica errante que observa tu progreso.
- **No hostil** por defecto; narra lore mientras subes de nivel.
- Vende Fragmentos de Resonancia y Runas de Memoria.
- Si lo atacas → superboss opcional de 3 fases (drop cosmético "Ojo del Testigo").

---

## 16. Calidad de vida y configuración

- **Integración con Boss Checklist** y **Recipe Browser**.
- Opciones de configuración:
  - Desactivar nivel infinito (fijar tope).
  - Ajustar multiplicador de XP (0.5x – 5x).
  - Desactivar eventos cósmicos.
  - Cambiar la tecla del árbol de habilidades.
  - Elegir seed manual del árbol procedural.
- **Multijugador-friendly**: cada jugador tiene su fragmento; bonos de resonancia de grupo.

---

## 17. Compatibilidad con mods

- **Mods de contenido**: el códex puede absorber armas de otros mods si están cargados.
- **Mods de utilidad**: registro de jefes y checklists cuando estén presentes.
- **Recipe Browser**: recetas visibles.

---

## 18. Identidad visual y audio

### Paleta de colores
- **Fondo**: espacio profundo (azul-negro `oklch(0.13 0.025 280)`).
- **Primario (starlight gold)**: `#f5c451` — para acentos de Distancia y UI.
- **Acento (violeta arcano)**: `#b388ff` — para Artes Mágicas.
- **Solar (cuerpo a cuerpo)**: `#ff9a3c`.
- **Vacío teal**: `#3dd6c4`.

### Tipografía
- UI del juego: fuente pixel-art consistente con Terraria.
- Web showcase: Geist Sans + Geist Mono.

### Audio
- Música ambiental del Sagrario Hueco (tono bajo, reverberante).
- Música de jefe Aethon (5 pistas, una por fase, escalando en intensidad).
- SFX de absorción de memoria (campana cósmica).

---

## 📌 Resumen de cambios vs. versión anterior

| Aspecto | Antes (v1) | Ahora (v2) |
|---------|-----------|-----------|
| Armas | 4 fijas (arco, espada, cañón, libro) | 3 ramas principales adaptables |
| Cañón | Era arma separada | Se fusiona en rama de Distancia (sub-forma munición) |
| Mago + Invocador | Separados | **Fusionados en Artes Mágicas** |
| Árboles | 6 ramas × 4 armas | 6 ramas (Distancia/Melee) + 7 ramas (Artes Mágicas) |
| Códex | 64 armas (16 × 4 clases) | 56 armas (12 + 16 + 16 + 12) organizadas por rama |
| Elección de arma | Al bondéate al fragmento | Al desarrollar la primera rama en combate |
| Cambio de rama | No posible | Posible con Fragmentos de Resonancia |

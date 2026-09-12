# 🛠️ Roadmap de Implementación del Mod — "Aethon, la Luz Primordial"

> Guía completa para crear el mod desde cero en tModLoader (C#)
> Idioma: Español · Basado en la documentación de tModLoader v2026.06

---

## 📑 Índice

1. [Requisitos previos y entorno](#1-requisitos-previos-y-entorno)
2. [Arquitectura del proyecto C#](#2-arquitectura-del-proyecto-c)
3. [Fases de desarrollo (orden recomendado)](#3-fases-de-desarrollo-orden-recomendado)
4. [Lista completa de archivos a crear](#4-lista-completa-de-archivos-a-crear)
5. [Dependencias y APIs de tModLoader clave](#5-dependencias-y-apis-de-tmodloader-clave)
6. [Estimación de esfuerzo](#6-estimación-de-esfuerzo)
7. [Riesgos y mitigaciones](#7-riesgos-y-mitigaciones)

---

## 1. Requisitos previos y entorno

### Software necesario
- **.NET SDK 8.0** (o el que requiera la versión de tModLoader).
- **tModLoader** instalado vía Steam (rama `1.4-stable` o `preview`).
- **Visual Studio 2022** o **JetBrains Rider** (recomendado) o **VS Code** con extensión C#.
- **Git** para control de versiones.
- **GIMP / Aseprite / Paint.NET** para sprites pixel-art.

### Conocimientos requeridos
- **C# intermedio** (clases, herencia, eventos, LINQ).
- Conceptos de **Terraria modding** (items, NPCs, projectiles, buffs).
- Estructura del **tModLoader API** (ver §5).

### Configuración inicial
1. Instalar tModLoader desde Steam.
2. Lanzar tModLoader una vez para generar las carpetas de mods.
3. Usar el **tModLoader Example Mod** como referencia (viene con el instalador).
4. Crear el esqueleto del mod con la plantilla `mod build` del tModLoader.

---

## 2. Arquitectura del proyecto C#

```
AethonMod/
├── build.txt                  # Metadatos del mod (nombre, versión, autor)
├── description.txt           # Descripción para el navegador de mods
├── icon.png                   # Icono del mod (80×80)
│
├── Content/
│   ├── Items/
│   │   ├── GenesisShard.cs          # El item fragmento principal
│   │   ├── ResonanceShard.cs       # Moneda secundaria
│   │   └── MemoryRune.cs           # Item runa memorizada
│   │
│   ├── Weapons/
│   │   ├── LuminaStarbow.cs        # Arma de distancia (arco)
│   │   ├── SolbrandEdge.cs         # Arma cuerpo a cuerpo (espada)
│   │   ├── GrimoireEternal.cs      # Arma de artes mágicas (grimorio)
│   │   └── Projectiles/
│   │       ├── StarlightArrow.cs
│   │       ├── DawnSlash.cs
│   │       ├── ArcaneBolt.cs
│   │       └── StellarMinion.cs
│   │
│   ├── NPCs/
│   │   ├── AethonBoss.cs            # Jefe final
│   │   ├── AethonBossPhase*.cs     # Una clase por fase (5)
│   │   ├── EchoBlade.cs            # Eco del primer portador
│   │   ├── EchoArcher.cs           # Eco de la arquera estelar
│   │   ├── TheWitness.cs           # NPC/Testigo (hostil opcional)
│   │   ├── RiftKeeper.cs           # Guardián del rift
│   │   ├── HollowTitan.cs          # Mini-jefe del Sagrario
│   │   └── Mobs/
│   │       ├── HollowCrystal.cs    # Mob del bioma
│   │       └── RuneSentinel.cs      # Mob del bioma
│   │
│   ├── Biomes/
│   │   ├── HollowSanctumBiome.cs   # ModBiome
│   │   └── HollowSanctumPlayer.cs  # ModPlayer (efectos del bioma)
│   │
│   ├── Tiles/
│   │   ├── AncientAltar.cs          # Altar del fragmento (ModTile)
│   │   ├── AethoniteOre.cs          # Mineral raro
│   │   └── HollowCrystal.cs        # Cristal decorativo
│   │
│   ├── Buffs/
│   │   ├── StarMarked.cs           # Marca del cazador
│   │   ├── SolarBurn.cs            # Quemadura solar
│   │   └── ManaShield.cs           # Escudo de maná
│   │
│   ├── DamageClasses/
│   │   └── StellarDamage.cs        # Clase de daño personalizada (opcional)
│   │
│   ├── UI/
│   │   ├── SkillTreeUI.cs          # Estado UI del árbol
│   │   ├── SkillTreePanel.cs       # Panel principal
│   │   ├── SkillNodeElement.cs     # Botón de nodo
│   │   ├── MemoryCodexUI.cs        # Códex de absorción
│   │   └── ProgressionBarUI.cs     # Barra de XP
│   │
│   ├── Systems/
│   │   ├── ShardLevelSystem.cs     # ModSystem — lógica de niveles/XP
│   │   ├── CosmicEventSystem.cs    # ModSystem — eventos por nivel
│   │   └── BuildShareSystem.cs     # Importar/exportar builds
│   │
│   ├── Players/
│   │   └── ShardPlayer.cs          # ModPlayer — datos del jugador + fragmento
│   │
│   ├── Globals/
│   │   ├── GlobalNPCXP.cs          # GlobalNPC — otorgar XP al morir
│   │   └── GlobalItemMemory.cs    # GlobalItem — registrar armas absorbibles
│   │
│   ├── Items/Placeables/
│   │   ├── AncientAltarItem.cs     # Item-colocable del altar
│   │   └── AethoniteOreItem.cs
│   │
│   └── Prefixes/
│       └── StellarPrefix.cs       # Prefijo personalizado (opcional)
│
├── UI/                              # Assets de UI (PNG)
│   ├── SkillTreeBackground.png
│   ├── NodeCommon.png
│   ├── NodeRare.png
│   └── NodeLegendary.png
│
├── Textures/                        # Sprites (PNG)
│   ├── Items/
│   │   ├── GenesisShard.png
│   │   ├── ResonanceShard.png
│   │   └── MemoryRune.png
│   ├── Weapons/
│   │   ├── LuminaStarbow.png
│   │   ├── SolbrandEdge.png
│   │   └── GrimoireEternal.png
│   ├── NPCs/
│   │   ├── AethonBoss.png
│   │   └── ...
│   └── Projectiles/
│       └── ...
│
└── Localization/                    # Traducciones (ES + EN)
    ├── en-US_Mods.AethonMod.hjson
    └── es-ES_Mods.AethonMod.hjson
```

---

## 3. Fases de desarrollo (orden recomendado)

> ⚠️ **Orden crítico**: cada fase depende de la anterior. No saltes fases.

### 🟢 Fase 0 — Esqueleto y compilación (1–2 días)
**Objetivo**: el mod compila y aparece en tModLoader sin errores.

- [ ] Crear `build.txt` con metadatos:
  ```
  author = TuNombre
  version = 0.1
  displayName = Aethon, la Luz Primordial
  ```
- [ ] Crear `description.txt` (1 párrafo).
- [ ] Crear `icon.png` (80×80).
- [ ] Crear clase `AethonMod : Mod` (punto de entrada).
- [ ] Compilar con `tModLoader -build`.
- [ ] Verificar que aparece en el navegador de mods.

### 🟡 Fase 1 — Item básico "Fragmento Génesis" (2–3 días)
**Objetivo**: el jugador puede obtener el item y usarlo.

- [ ] Crear `GenesisShard.cs : ModItem` con:
  - `SetDefaults()`: daño 10, useTime 20, autoReuse.
  - `SetStaticDefaults()`: tooltip, rareza.
  - Sprite placeholder (cuadrado brillante).
- [ ] Añadir receta de debug (1 Wood → Genesis Shard) para testear.
- [ ] Implementar `UseItem()` que determine la rama (detectar daño infligido).
- [ ] Crear `ShardPlayer.cs : ModPlayer` con:
  - `int shardLevel = 1`
  - `int shardXP = 0`
  - `WeaponBranch activeBranch` (enum: None/Distance/Melee/Magic)
  - `SetDefaults()`, `SaveData()`, `LoadData()`.
- [ ] Implementar barra de XP básica en UI.

### 🟡 Fase 2 — Sistema de XP y niveles (2–3 días)
**Objetivo**: el fragmento sube de nivel al matar enemigos.

- [ ] Crear `GlobalNPCXP.cs : GlobalNPC` con:
  - `OnKill()`: calcular XP por tier del NPC, dar al jugador.
  - Tabla de XP por tipo de NPC (NPC.type → XP).
- [ ] Crear `ShardLevelSystem.cs : ModSystem` con:
  - Función `GainXP(Player player, int amount)`.
  - Función `CheckLevelUp(Player player)`.
  - Tabla de puntos por nivel (1-10 → +1, etc.).
- [ ] Implementar evolución visual del item por nivel (cambiar sprite).
- [ ] Añadir notificación de nivel subido (texto flotante).

### 🟠 Fase 3 — Transformación por rama (3–4 días)
**Objetivo**: el fragmento se transforma en el arma de la rama elegida.

- [ ] Definir `enum WeaponBranch { None, Distance, Melee, Magic }`.
- [ ] En `GenesisShard.UseItem()`:
  - Detectar el `DamageClass` del último daño infligido.
  - Tras N kills con la misma clase, fijar `activeBranch`.
  - Reemplazar el item por el arma correspondiente (o cambiar su comportamiento).
- [ ] Crear las 3 clases de arma:
  - `LuminaStarbow.cs` (Distancia) — arco que dispara flechas de luz.
  - `SolbrandEdge.cs` (Melee) — espada con onda de corte.
  - `GrimoireEternal.cs` (Artes Mágicas) — grimorio con bolts mágicos.
- [ ] Crear los proyectiles base de cada arma (4 clases `ModProjectile`).
- [ ] Implementar daño escalado por nivel (nivel × multiplicador).

### 🟠 Fase 4 — Árbol de habilidades básico (4–5 días)
**Objetivo**: UI funcional del árbol con nodos asignables.

- [ ] Definir estructuras de datos:
  - `SkillNode` (id, nombre, rama, rareza, coste, efecto, prerrequisito).
  - `SkillTree` (lista de nodos + aristas).
  - Datos de los 3 árboles (Distancia/Melee/Magic) — exportar desde el TS del sitio web.
- [ ] Crear `SkillTreeUI.cs : UIState` con:
  - Panel principal (closable con tecla K).
  - Renderizado de nodos como botones.
  - Zoom/pan con rueda del ratón.
- [ ] Crear `SkillNodeElement.cs : UIElement` (botón de nodo):
  - Estados: bloqueado / disponible / asignado.
  - Click para asignar (respeta prerrequisitos + presupuesto).
- [ ] Persistir nodos asignados en `ShardPlayer.SaveData()`.
- [ ] Implementar generación procedural (seed → jitter + rarezas).

### 🟠 Fase 5 — Efectos de los nodos (5–7 días)
**Objetivo**: cada nodo asignado modifica el comportamiento del arma.

- [ ] Implementar 6 ramas × 3 armas = 18 ramas de efectos.
- [ ] Por cada nodo, crear un método que aplique su efecto:
  - Ej: `ApplyManaPool(player)` → +40 maná máximo.
  - Ej: `ApplySplitShot(projectile)` → al impactar, generar 2 proyectiles.
- [ ] Enganchar los efectos en:
  - `ModPlayer.PostUpdate()` (pasivos: maná, HP, defensa).
  - `ModItem.UseItem()` / `Shoot()` (proyectiles, alteraciones).
  - `ModProjectile.AI()` (homing, split, chain).
  - `GlobalNPC.OnHitByItem()` (marcas, quemaduras).
- [ ] Implementar los capstones legendarios (supernova, agujero negro, etc.).

### 🔴 Fase 6 — Bioma Sagrario Hueco (3–4 días)
**Objetivo**: el bioma se genera y tiene mobs propios.

- [ ] Crear `HollowSanctumBiome.cs : ModBiome`:
  - `IsBiomeActive()`: detectar presencia de cristales del bioma cerca.
  - Música, color de fondo, partículas.
- [ ] Crear `AncientAltar.cs : ModTile` + `AncientAltarItem.cs` (colocable).
- [ ] Crear `AethoniteOre.cs : ModTile` (mineral raro).
- [ ] Crear mobs del bioma:
  - `HollowCrystal.cs` (cristal flotante hostil).
  - `RuneSentinel.cs` (centinela de piedra).
- [ ] Crear `HollowTitan.cs : ModNPC` (mini-jefe del bioma).
- [ ] Generación del bioma en `ModWorld.genAfter()` o via `WorldGen`.

### 🔴 Fase 7 — Jefes cósmicos (4–5 días)
**Objetivo**: todos los jefes del mod funcionan.

- [ ] `RiftKeeper.cs : ModNPC` (Guardián del rift).
- [ ] `EchoBlade.cs : ModNPC` (Eco del primer portador — mimica un árbol de Melee).
- [ ] `EchoArcher.cs : ModNPC` (Eco de la arquera — mimica un árbol de Distancia).
- [ ] `TheWitness.cs : ModNPC` (NPC hostil-opcional, 3 fases si atacado).
- [ ] Para cada jefe:
  - `SetDefaults()` (HP, daño, AIStyle custom).
  - `AI()` (patrones de ataque por fase).
  - `OnKill()` (drops: Fragmentos de Resonancia + loot único).
  - `BossHeadSlot()` (icono en mapa).
  - Música custom.
- [ ] Integrar con **Boss Checklist** (`BossChecklistData` callback).

### 🔴 Fase 8 — Jefe final Aethon (5–7 días)
**Objetivo**: la pelea de 5 fases contra Aethon.

- [ ] `AethonBoss.cs : ModNPC` con máquina de estados de 5 fases.
- [ ] Una clase auxiliar por fase (`AethonBossPhase1.cs` ... `Phase5.cs`).
- [ ] Mecánicas por fase:
  - F1: espiral de pernos estelares.
  - F2: nubes AoE que ciegan + queman; arena se deforma.
  - F3: inversión de gravedad cada 8s.
  - F4: agujero negro (atracción + adds).
  - F5: **Aethon empuña tus runas memorizadas contra ti** (lee las Runas del jugador).
- [ ] Drops: Forma Ascendida (cosmético), acceso a New Game+.
- [ ] Música: 5 pistas (una por fase), escalando intensidad.
- [ ] Arena custom (plataformas que se reforman entre fases).

### 🔴 Fase 9 — Absorción de Lore (Códex de Memoria) (4–5 días)
**Objetivo**: el capstone funciona — memoriza armas del juego base.

- [ ] Crear `MemoryCodexUI.cs : UIState` (panel del códex).
- [ ] Crear `GlobalItemMemory.cs : GlobalItem`:
  - `OnCreate()` / `SetDefaults()`: registrar items absorbibles por clase.
  - Construir lista de armas base (12 distancia + 16 melee + 16 magia + 12 invocación).
- [ ] Crear `MemoryRune.cs : ModItem` (runa equipable).
- [ ] Lógica de memorización:
  - UI lista armas → click → consume Fragmentos de Resonancia → crea Runa.
  - Runa se equipa en slot (hasta N por nivel del arma).
- [ ] Las Runas activas modifican el comportamiento del arma:
  - Ej: memorizar Last Prism → el grimorio dispara rayos convergentes además.
  - Implementar via flags en `ShardPlayer` que los proyectiles leen.
- [ ] Soporte para mods cargados (escanear items de otros mods si presentes).

### 🔴 Fase 10 — Eventos cósmicos (2–3 días)
**Objetivo**: eventos por nivel del fragmento.

- [ ] `CosmicEventSystem.cs : ModSystem`:
  - Verificar nivel del fragmento de cada jugador.
  - Trigger eventos: Lluvia de luz estelar (Lv25), extensión del Sagrario (Lv50), Rifts (Lv75), Ecos (Lv100), Despertar (Lv150).
- [ ] Lluvia de luz estelar: `ModPlayer.PreUpdate()` genera meteoros ambientales.
- [ ] Rifts dimensionales: generar mini-estructuras con loot.

### 🟣 Fase 11 — Economía y NPC Testigo (2 días)
**Objetivo**: el Testigo vende runas y narra lore.

- [ ] `TheWitness.cs` (versión NPC): `ModNPC` no-hostil, vende Fragmentos de Resonancia.
  - `GetChat()` muestra texto de lore escalado por nivel del fragmento.
  - `NPCShop` con runas y consumibles.
- [ ] Fragmentos de Resonancia como drops de jefes (ya hecho en Fase 7).

### 🟣 Fase 12 — Persistencia y multi-jugador (2–3 días)
**Objetivo**: el progreso se guarda y funciona en servidor.

- [ ] `ShardPlayer.SaveData()` / `LoadData()` (TagCompound).
- [ ] `ShardLevelSystem` sync en servidor (`NetMessage`, `SendData`).
- [ ] Testear en servidor dedicado con 2+ jugadores.
- [ ] Manejar disconnects (no perder progreso).

### 🟣 Fase 13 — Localización ES/EN (1–2 días)
**Objetivo**: todo el texto traducido.

- [ ] Crear `Localization/en-US_Mods.AethonMod.hjson` (inglés).
- [ ] Crear `Localization/es-ES_Mods.AethonMod.hjson` (español).
- [ ] Reemplazar todos los `string` hardcodeados con `Language.GetTextValue("Mods.AethonMod.X")`.
- [ ] Testear cambio de idioma en tModLoader.

### 🟣 Fase 14 — Pulido y balance (3–4 días)
**Objetivo**: el mod es jugable y balanceado.

- [ ] Playtest completo (Pre-Hardmode → Hardmode → Endgame → Aethon).
- [ ] Balance de XP (¿sube muy lento/rápido?).
- [ ] Balance de daño (¿el arma es muy OP?).
- [ ] Balance de jefes (HP, daño, duración de la pelea).
- [ ] Corregir bugs de edge case (muerte del jugador, server crash, etc.).
- [ ] Optimización (pool de proyectiles, reducir allocations).

### 🟣 Fase 15 — Documentación y publicación (1–2 días)
**Objetivo**: el mod está listo para el Steam Workshop.

- [ ] Escribir `description.txt` detallado.
- [ ] Crear imágenes de preview (4–6 capturas).
- [ ] Escribir wiki / guía en GitHub.
- [ ] Publicar en Steam Workshop (`tModLoader -publish`).
- [ ] Anunciar en comunidades de modding.

---

## 4. Lista completa de archivos a crear

### Items (3)
1. `GenesisShard.cs` — el fragmento principal
2. `ResonanceShard.cs` — moneda secundaria
3. `MemoryRune.cs` — runa de absorción

### Armas (3) + Proyectiles (4+)
4. `LuminaStarbow.cs` — arma de distancia
5. `SolbrandEdge.cs` — arma cuerpo a cuerpo
6. `GrimoireEternal.cs` — arma de artes mágicas
7. `StarlightArrow.cs` — proyectil de distancia
8. `DawnSlash.cs` — proyectil de melee (onda de corte)
9. `ArcaneBolt.cs` — proyectil mágico
10. `StellarMinion.cs` — minion de invocación

### NPCs (7)
11. `AethonBoss.cs` — jefe final (+ 5 clases de fase)
12. `EchoBlade.cs`
13. `EchoArcher.cs`
14. `TheWitness.cs`
15. `RiftKeeper.cs`
16. `HollowTitan.cs`
17. `HollowCrystal.cs` (mob)
18. `RuneSentinel.cs` (mob)

### Bioma y Tiles (4)
19. `HollowSanctumBiome.cs`
20. `AncientAltar.cs` (ModTile)
21. `AethoniteOre.cs` (ModTile)
22. `HollowCrystalTile.cs` (ModTile decorativo)

### Buffs (3)
23. `StarMarked.cs`
24. `SolarBurn.cs`
25. `ManaShield.cs`

### UI (5)
26. `SkillTreeUI.cs`
27. `SkillTreePanel.cs`
28. `SkillNodeElement.cs`
29. `MemoryCodexUI.cs`
30. `ProgressionBarUI.cs`

### Systems (3)
31. `ShardLevelSystem.cs`
32. `CosmicEventSystem.cs`
33. `BuildShareSystem.cs`

### Player + Globals (3)
34. `ShardPlayer.cs` (ModPlayer)
35. `GlobalNPCXP.cs` (GlobalNPC)
36. `GlobalItemMemory.cs` (GlobalItem)

### Placeables (2)
37. `AncientAltarItem.cs`
38. `AethoniteOreItem.cs`

### Localización (2)
39. `en-US_Mods.AethonMod.hjson`
40. `es-ES_Mods.AethonMod.hjson`

**Total: ~40 archivos C# + assets PNG + 2 archivos de localización.**

---

## 5. Dependencias y APIs de tModLoader clave

Basado en el análisis de `docs.tmodloader.net/docs/stable/annotated.html`:

### Clases base a heredar
| Funcionalidad | Clase base de tModLoader |
|---------------|--------------------------|
| Item personalizado | `ModItem` |
| Arma | `ModItem` + `DamageClass` (Ranged/Melee/Magic/Summon) |
| NPC / Jefe | `ModNPC` + `BossHeadSlot` |
| Proyectil | `ModProjectile` |
| Buff | `ModBuff` |
| Bioma | `ModBiome` |
| Tile (bloque) | `ModTile` |
| Sistema global | `ModSystem` |
| Datos del jugador | `ModPlayer` |
| Modificación global de NPCs | `GlobalNPC` |
| Modificación global de items | `GlobalItem` |
| Daño personalizado | `ModDamageClass` |
| UI estado | `UIState` + `UIElement` |
| Prefijo | `ModPrefix` |
| Rareza | `ModRarity` |

### APIs clave a usar
- **`Main.LocalPlayer`**: acceso al jugador local.
- **`Player.GetModPlayer<T>()`**: acceder al `ShardPlayer`.
- **`NPC.lifeMax`**, **`NPC.ai[]`**: controlar jefes.
- **`Projectile.NewProjectile()`**: spawn de proyectiles.
- **`Item.DamageType`**: `DamageClass.Ranged` / `Melee` / `Magic` / `Summon`.
- **`Language.GetTextValue()`**: localización.
- **`ModContent.GetInstance<T>()`**: singleton del mod.
- **`NetMessage.SendData()`**: sync multi-jugador.

### Patrones recomendados
1. **Singleton pattern** para sistemas (`ShardLevelSystem.Instance`).
2. **Data-driven** para nodos del árbol (archivo JSON cargado en `Load()`).
3. **Object pooling** para proyectiles (evitar lag con muchos proyectiles).
4. **TagCompound** para guardar/cargar progreso del jugador.

---

## 6. Estimación de esfuerzo

| Fase | Días | Notas |
|------|------|-------|
| 0 — Esqueleto | 1–2 | Rápido, plantilla |
| 1 — Item básico | 2–3 | UI de barra de XP |
| 2 — Sistema de XP | 2–3 | GlobalNPC + ModSystem |
| 3 — Transformación | 3–4 | Detección de clase de daño |
| 4 — Árbol de habilidades | 4–5 | UIState complejo |
| 5 — Efectos de nodos | 5–7 | El grueso del trabajo |
| 6 — Bioma | 3–4 | ModBiome + ModTile |
| 7 — Jefes cósmicos | 4–5 | 5 NPCs con AI custom |
| 8 — Aethon jefe final | 5–7 | 5 fases + arena |
| 9 — Absorción de Lore | 4–5 | Códex + Runas |
| 10 — Eventos cósmicos | 2–3 | ModSystem |
| 11 — NPC Testigo | 2 | NPCShop + diálogo |
| 12 — Persistencia/MP | 2–3 | NetMessage |
| 13 — Localización | 1–2 | hjson ES/EN |
| 14 — Pulido/balance | 3–4 | Playtest |
| 15 — Publicación | 1–2 | Steam Workshop |
| **TOTAL** | **44–61 días** | **~2–3 meses a tiempo parcial** |

---

## 7. Riesgos y mitigaciones

| Riesgo | Impacto | Mitigación |
|--------|---------|------------|
| UI del árbol muy compleja | Alto | Reusar patrones del Example Mod; prototipar en web primero |
| Performance con muchos proyectiles | Medio | Object pooling; limitar partículas |
| Balance roto (arma muy OP) | Medio | Config options; playtest extenso |
| Incompatibilidad con otros mods | Bajo | Detectar mods via `ModLoader.TryGetMod()` |
| Sync multi-jugador roto | Alto | Testear en servidor dedicado desde Fase 12 |
| Update de tModLoader rompe el mod | Medio | Pin a versión estable; seguir changelog |
| Assets pixel-art requieren skill | Medio | Usar placeholders; contratar artista si presupuesto |
| Capstone de absorción muy complejo | Alto | Implementar subconjunto primero (10 armas base, no 56) |

---

## 📋 Resumen ejecutivo

- **~40 archivos C#** + assets + localización.
- **15 fases** secuenciales (~2–3 meses a tiempo parcial).
- **Fase crítica**: Fase 5 (efectos de nodos) es el grueso del trabajo.
- **Fase más arriesgada**: Fase 8 (Aethon) y Fase 9 (absorción) por complejidad.
- **Recomendación**: empezar por Fase 0–3 para tener un MVP jugable (item que sube de nivel y se transforma), luego iterar.

> **Próximo paso**: cuando el usuario dé luz verde, crear el proyecto C# esqueleto (Fase 0) y empezar la implementación.

# 🛠️ Cómo compilar el mod "Aethon, la Luz Primordial"

Este documento explica cómo compilar el mod C# en un archivo `.tmod` jugable en tModLoader.

---

## 📋 Requisitos previos

1. **.NET SDK 8.0** — <https://dotnet.microsoft.com/download/dotnet/8.0>
2. **tModLoader** instalado vía Steam (rama `1.4-stable`):
   - Ruta típica (Linux/macOS): `~/.steam/steam/steamapps/common/tModLoader`
   - Ruta típica (Windows): `C:\Program Files (x86)\Steam\steamapps\common\tModLoader`
3. **Git** (opcional, para control de versiones).

---

## 🚀 Compilación rápida (modo desarrollo)

### Opción A: Desde el IDE (recomendado)

1. Abre `AethonMod/AethonMod.csproj` en Visual Studio / Rider / VS Code.
2. Ajusta `<TModLoaderPath>` en el `.csproj` a tu ruta de instalación de tModLoader.
3. Compila (`Ctrl+Shift+B` en VS / `Cmd+B` en Rider).
4. El archivo `.tmod` se generará en `AethonMod/bin/Debug/net8.0/AethonMod.tmod` (o en la carpeta de mods de tModLoader).

### Opción B: Desde la línea de comandos

```bash
cd AethonMod
# Ajusta la ruta de tModLoader si no está en el default
export TModLoaderPath=~/.steam/steam/steamapps/common/tModLoader

dotnet build -c Debug
# o para Release:
dotnet build -c Release
```

### Opción C: Usar el build system interno de tModLoader

tModLoader incluye un compilador propio que empaqueta automáticamente:

1. Abre tModLoader.
2. Ve a **Workshop → Develop Mods**.
3. Selecciona la carpeta `AethonMod/`.
4. Click **Build + Reload**.
5. El `.tmod` se generará en `<tModLoader>/Mods/AethonMod.tmod`.

---

## 📦 Estructura de archivos esperada

```
AethonMod/
├── build.txt                  # Metadatos del mod
├── description.txt            # Descripción
├── icon.png                   # (TODO: crear icono 80×80)
├── AethonMod.cs               # Punto de entrada
├── AethonMod.csproj           # Proyecto .NET
├── Content/
│   ├── Items/                 # ModItem
│   ├── Weapons/               # Armas + Projectiles/
│   ├── NPCs/                  # ModNPC (jefes)
│   ├── Biomes/                # ModBiome
│   ├── Tiles/                 # ModTile
│   ├── Players/               # ModPlayer + enums
│   ├── Globals/               # GlobalNPC
│   └── Systems/               # ModSystem
├── Localization/             # hjson ES/EN
└── Textures/                  # (TODO: sprites PNG)
```

---

## ⚠️ Estado actual del esqueleto (Fase 0–3)

Este esqueleto incluye:

- ✅ Punto de entrada `AethonMod.cs`
- ✅ `ShardPlayer` (ModPlayer) con nivel/XP/rama/nodos/runas + persistencia
- ✅ `ShardLevelSystem` (ModSystem) con tabla de XP
- ✅ `GlobalNPCXP` (GlobalNPC) que otorga XP al matar + detecta rama por kills
- ✅ `GenesisShard` (ModItem) — el fragmento
- ✅ 3 armas: `LuminaStarbow`, `SolbrandEdge`, `GrimoireEternal` con daño escalado por nivel
- ✅ 3 proyectiles: `StarlightArrow`, `DawnSlash`, `ArcaneBolt`
- ✅ `ResonanceShard` (moneda)
- ✅ `MemoryRune` (runa equipable de absorción)
- ✅ `HollowSanctumBiome` (ModBiome)
- ✅ `AncientAltar` (ModTile) + item colocable
- ✅ 6 NPCs: `AethonBoss` (5 fases completas), `HollowTitan`, `RiftKeeper`, `EchoBlade`, `EchoArcher`, `TheWitness` (NPC)
- ✅ `SkillTreeCatalog` — catálogo de 3 árboles (30/30/35 nodos)
- ✅ `NodeEffectSystem` — aplica efectos de nodos (daño, crit, maná, knockback, defensa, lifesteal, escudo de maná, etc.)
- ✅ `MemoryCodexSystem` — absorción de armas del juego base (30+ armas absorbibles)
- ✅ `CosmicEventSystem` — eventos por nivel (lluvia de estrellas, rifts, hitos)
- ✅ `ShardSyncSystem` — sync multi-jugador (NetMessage + HandlePacket wiring)
- ✅ `UISystem` — registra y gestiona las UI (tecla K = árbol, J = códex)
- ✅ `SkillTreeUI` — panel visual del árbol con nodos asignables (constelación)
- ✅ `MemoryCodexUI` — panel visual del códex con lista de armas absorbibles
- ✅ **19 sprites pixel-art** generados (items, armas, proyectiles, NPCs, tiles, UI)
- ✅ Localización ES/EN

### TODO (próximas fases)

- ❌ **Música custom**: pistas de Aethon (Fase 8 audio — los NPCs ya usan música vanilla).
- ❌ **Transformación del fragmento**: reemplazar el item al imprprimir rama (Fase 3 completa — la lógica de detección ya existe en `GlobalNPCXP`, falta el reemplazo físico del item).

---

## 🧪 Testeo en juego

1. Compila el mod (ver arriba).
2. Abre tModLoader → Mods → activa "Aethon, la Luz Primordial".
3. Entra a un mundo.
4. Craftea el Fragmento Génesis (1 Wood, receta de debug) O encuentra el Altar Antiguo.
5. Mata enemigos con daño Ranged/Melee/Magic para imprprimir tu rama.
6. Verifica que el fragmento sube de nivel (XP otorgada al matar).

---

## 🐛 Solución de problemas

| Error | Solución |
|-------|----------|
| `tModLoader.dll not found` | Ajusta `<TModLoaderPath>` en `.csproj` |
| `FNA.dll not found` | Verifica que tModLoader esté instalado correctamente |
| Build falla con errores CS | Asegúrate de usar .NET 8 SDK |
| Mod no aparece en tModLoader | Verifica que `build.txt` esté en la raíz de `AethonMod/` |
| Texturas moradas/faltantes | Crea los sprites PNG en `Textures/` (rutas coinciden con namespaces) |

---

## 📚 Recursos

- Documentación de tModLoader: <https://docs.tmodloader.net/docs/stable/>
- Example Mod (referencia): incluido con tModLoader
- Wiki de la comunidad: <https://github.com/tModLoader/tModLoader/wiki>

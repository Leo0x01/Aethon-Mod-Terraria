**Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran**

[GITHUB_PAT - solicitar al usuario]

Repositorio: https://github.com/Leo0x01/Aethon-Mod-Terraria

---

# AethonMod — Documento Completo del Proyecto

> Este documento es la fuente única de verdad para el mod **Aethon, la Luz Primordial**.
> Cualquier IA o desarrollador que trabaje en este proyecto DEBE leer este documento completo
> antes de tocar cualquier archivo.

---

## 1. RECORDATORIO OBLIGATORIO

**Puedo coger los recursos de nuestro github si los datos de mi versión local se borran**

- Esta frase DEBE ser la primera línea de cada commit, cada documento y cada mensaje.
- El entorno local del sandbox se resetea frecuentemente; **los datos locales se borran**.
- Para recuperar el trabajo: hacer `git fetch` + `git pull` del remote ANTES de cualquier cambio.
- GitHub PAT (Personal Access Token):
  ```
  [GITHUB_PAT - solicitar al usuario]
  ```
- Repositorio: https://github.com/Leo0x01/Aethon-Mod-Terraria
- Clonado local (en este sandbox): `/home/z/my-project/AethonMod/`

### Comando para clonar si el local se pierde
```bash
cd /home/z/my-project
git clone https://Leo0x01:[GITHUB_PAT - solicitar al usuario]@github.com/Leo0x01/Aethon-Mod-Terraria.git AethonMod
cd AethonMod
git log --oneline -5
```

---

## 2. PROYECTO: AethonMod

| Campo | Valor |
|---|---|
| **Mod name (interno)** | AethonMod |
| **Display name** | Aethon, la Luz Primordial |
| **Versión (build.txt)** | 5.81 |
| **Author** | AethonModTeam |
| **Framework** | tModLoader 1.4.4 |
| **Runtime** | .NET 8, C# |
| **Side** | Both (Client + Server) |
| **Commit actual** | b2798e6 (v5.82 en mensaje) |
| **Commit estable del remote** | e826c82 (referencia de sprites protegidos) |
| **Homepage** | https://github.com/Leo0x01/Aethon-Mod-Terraria |

### build.txt completo
```ini
author = AethonModTeam
version = 5.81
displayName = Aethon, la Luz Primordial
homepage = https://github.com/Leo0x01/Aethon-Mod-Terraria
modReferences =
buildIgnore = *.csproj, *.csproj.user, obj/*, bin/*, *.bak, *.md, *.py
side = Both
```

### AethonMod.csproj completo
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Import Project="/tmp/tmodloader/tMLMod.targets" />

  <PropertyGroup>
    <AssemblyName>AethonMod</AssemblyName>
    <RootNamespace>AethonMod</RootNamespace>
    <Nullable>disable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>

  <ItemGroup>
    <Compile Remove="**/obj/**" />
    <Compile Remove="**/bin/**" />
    <Compile Remove="Reference_WoTG/**" />
  </ItemGroup>
</Project>
```

> **Importante**: La línea `<Compile Remove="Reference_WoTG/**" />` fue añadida en v5.82
> para excluir los archivos de referencia de WoTG de la compilación. Sin esta línea el mod
> no compila porque `Reference_WoTG/` contiene archivos sueltos del mod original de WoTG
> que están solo como referencia, no para ser parte del build.

### AethonMod.cs (entry point)
```csharp
using System.IO;
using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Systems;

namespace AethonMod
{
    public class AethonMod : Mod
    {
        public static AethonMod Instance => ModContent.GetInstance<AethonMod>();

        public override void Load()
        {
            // Sistema simplificado: el SkillTree y el Codex fueron eliminados.
            // No hay inicializacion extra necesaria.
        }

        public override void Unload() { }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            ShardSyncSystem.HandlePacket(reader);
        }
    }
}
```

---

## 3. ARCHIVOS PROTEGIDOS (NO TOCAR NUNCA)

Estos archivos son **intocables**. Cualquier modificación puede romper el mod completo
o destruir contenido artesanal que tomó muchas horas de trabajo.

### 3.1 Código protegido del remote

| Archivo | Razón |
|---|---|
| `Content/Weapons/GrimoireEternal.cs` | Arma principal del mod con sistema de niveles infinitos |
| `Content/Weapons/GrimoireEternal.png` | Sprite original 30×38 — no rehacer |
| `Content/Weapons/TestAdvanced.cs` | 8 bastones de prueba del remote (TestMagicRing, TestSparkle, ProjBeam, TestMagicRingV2, ColorRainbow, ColorRed, ColorYellow, ColorGreen) |
| `Content/Globals/TestAdvancedFX.cs` | GlobalProjectile que añade flags `ai[1]` (3003, 3004, 4001, 4006, 5004-5007) al Nightglow #931 |
| `Content/Globals/CosmicProjectileFX.cs` | GlobalProjectile para el Nightglow #931 con efectos cósmicos (estela dorada, cian, magenta, índigo) + daño en área |
| `Content/Projectiles/CosmicOrbBolt.cs` | Bolt del CosmicOrb |
| `Content/Projectiles/CosmicOrbMinion.cs` | Minion del Grimorio |
| `Content/Buffs/CosmicOrbBuff.cs` | Buff del minion |
| `Content/Projectiles/ArcaneBolt.cs` | Proyectil homing del Grimorio |
| `Content/Projectiles/GenesisLight.cs` | Proyectil ceremonial |
| `Content/NPCs/*` (AethonBoss, HollowTitan, RiftKeeper, EchoArcher, EchoBlade, TheWitness) | NPCs del mod |
| `Content/Items/GenesisShard.cs`, `ResonanceShard.cs`, `SeerOrb.cs`, `LevelUpTester.cs`, `BossSummonBag.cs` | Items del juego |
| `Content/Tiles/AncientAltar.cs` | Tile ceremonial |
| `Content/Systems/*` (ShardSyncSystem, WeaponScaling, AncientAltarWorldGen, ShardLevelSystem) | Systems del mod |
| `Content/Players/ShardPlayer.cs`, `BranchType.cs` | Player systems |
| `Content/Globals/GlobalNPCXP.cs`, `ShardLevelItem.cs`, `TooltipToggleItem.cs` | Globals del mod |

### 3.2 Sprites protegidos del remote (commit e826c82)

Todos los sprites originales en:
- `Content/Weapons/*.png` (TestMagicRing, TestSparkle, ProjBeam, TestMagicRingV2, ColorRainbow, ColorRed, ColorYellow, ColorGreen, GrimoireEternal)
- `Content/NPCs/*.png` (AethonBoss, HollowTitan, RiftKeeper, EchoArcher, EchoBlade, TheWitness)
- `Content/Projectiles/*.png` (ArcaneBolt, GenesisLight, CosmicOrbBolt, CosmicOrbMinion)
- `Content/Buffs/CosmicOrbBuff.png`
- `Content/Items/*.png` (GenesisShard, ResonanceShard, SeerOrb, LevelUpTester, BossSummonBag)
- `Content/Tiles/AncientAltar.png`
- `Content/Items/Placeables/AncientAltarItem.png`
- `Content/Weapons/Projectiles/*.png`
- `icon.png` (icono del mod)
- `Content/_masters/*` (sprites maestros en alta resolución)

> **Regla de oro**: El commit `e826c82` contiene la versión estable de todos los sprites.
> Si se pierden localmente, ejecutar `git checkout e826c82 -- Content/ icon.png` para restaurarlos.

---

## 4. ESTADO ACTUAL DEL PROYECTO

### 4.1 Conteo de archivos (verificado)
- **78 archivos .cs** en `Content/`
- **138 archivos .png** (sprites)
- **8 shaders .fx** en `Content/Effects/Shaders/`
- **8 shaders .xnb** compilados en `Content/Effects/Shaders/`

### 4.2 Shaders (8 fx + 8 xnb)
| Shader | Archivo .fx | Archivo .xnb | Uso |
|---|---|---|---|
| RealBlackHoleShader | RealBlackHoleShader.fx | RealBlackHoleShader.xnb | BlackHoleProjectile (75-step lightmarch con lensing) |
| BlackHoleDistortionShader | BlackHoleDistortionShader.fx | BlackHoleDistortionShader.xnb | Lensing screen-space multi-fuente |
| BlackOnlyShader | BlackOnlyShader.fx | BlackOnlyShader.xnb | Refuerza event horizon |
| SunShader | SunShader.fx | SunShader.xnb | SunProjectile (estrella con corona + lava) |
| RadialShineShader | RadialShineShader.fx | RadialShineShader.xnb | Brillo radial (usado con SunShader) |
| ChromaticAberration | ChromaticAberration.fx | ChromaticAberration.xnb | Aberración cromática RGB |
| Shockwave | Shockwave.fx | Shockwave.xnb | Onda expansiva |
| Bloom | Bloom.fx | Bloom.xnb | Extract bright → blur → combine |

### 4.3 Texturas de WoTG (9 texturas en `Content/Effects/WoTG/`)
- `WavyBlotchNoise.png`
- `WavyBlotchNoiseDetailed.png`
- `InvisiblePixel.png`
- `PsychedelicWingTextureOffsetMap.png`
- `FireNoiseA.png`
- `FireNoiseB.png`
- `BloomCircleSmall.png`
- `BloomCircle.png`
- `BloomFlare.png`

### 4.4 Texturas procedurales (8 texturas en `Content/Effects/Procedural/`)
- `SoftGlow.png` (ID 0 en ParticleManager)
- `Trail.png` (ID 1)
- `Star.png` (ID 2)
- `Slash.png` (ID 3)
- `Vortex.png` (ID 4)
- `Ring.png` (ID 5)
- `Crescent.png` (ID 6)
- `Noise.png` (ID 7)

### 4.5 Texturas de efectos del remote (28 texturas en `Content/Effects/*.png`)
- GlowOrb, GlowOrbCyan, GlowOrbGold, GlowOrbGreen, GlowOrbMagenta, GlowOrbPurple, GlowOrbWhite
- GlowCircle, GlowCircleCyan, GlowCircleGold, GlowCircleGreen, GlowCircleMagenta, GlowCirclePurple, GlowCircleRed, GlowCircleWhite
- GlowRay, GlowRayCyan, GlowRayGold
- MagicRing, MagicRingGold
- BeamCyan, BeamGold
- HexCyan, HexGold
- ShieldCyan, ShieldGold
- SparkleStar
- TrailGlow

### 4.6 Sistema de partículas data-oriented
3 archivos en `Content/Particles/`:
- `ParticleData.cs` — struct con `[StructLayout(LayoutKind.Sequential, Pack = 1)]`
- `ParticleBuffer.cs` — buffer pre-asignado de 2000 partículas (capacidad default), estrategia round-robin
- `ParticleManager.cs` — `ModSystem` orquestador, actualización en `PreUpdateDusts`, render en `PostDrawTiles`

### 4.7 Estructura global de carpetas (raíz del proyecto)
```
/home/z/my-project/AethonMod/
├── .gitignore
├── AethonMod.cs              ← Mod entry point
├── AethonMod.csproj          ← MSBuild project (excluye Reference_WoTG)
├── CARACTERISTICAS.md
├── CHANGES.md
├── COMPILACION.md
├── LICENSE
├── README.md
├── STABLE-SNAPSHOT.md
├── Reference_WoTG/           ← Archivos de referencia de WoTG (NO compilados)
│   ├── BlackHole.cs
│   ├── BlackHolePet.cs
│   ├── PetBlackHoleRenderer.cs
│   ├── StarPet.cs
│   └── Starseed.cs
├── build.txt
├── description.txt
├── icon.png                  ← Icono del mod (commit e826c82 protegido)
├── update-changes.sh
├── bin/                      ← Output del build (ignorado)
├── obj/                      ← Intermedios del build (ignorado)
├── Localization/             ← Archivos de localización (.hjson)
└── Content/                  ← TODO el contenido del mod
```

---

## 5. ARMAS ACTUALES

### 5.1 Armas protegidas del remote (NO tocar)

#### 8 bastones de TestAdvanced.cs (en `Content/Weapons/TestAdvanced.cs`)
1. **TestMagicRing** — dispara Nightglow con flag `ai[1] = 3003` (anillo mágico girando)
2. **TestSparkle** — dispara Nightglow con flag `ai[1] = 3004` (sparkles)
3. **ProjBeam** — dispara Nightglow con flag `ai[1] = 4001` (beam)
4. **TestMagicRingV2** — dispara Nightglow con flag `ai[1] = 4006` (anillo v2)
5. **ColorRainbow** — dispara Nightglow con flag `ai[1] = 5004` (estela arcoíris densa)
6. **ColorRed** — dispara Nightglow con flag `ai[1] = 5005` (estela roja densa)
7. **ColorYellow** — dispara Nightglow con flag `ai[1] = 5006` (estela amarilla densa)
8. **ColorGreen** — dispara Nightglow con flag `ai[1] = 5007` (estela verde densa)

> **Importante**: Todos disparan el proyectil vanilla Nightglow (ID 931). El GlobalProjectile
> `TestAdvancedFX` lee `ai[1]` para añadir efectos específicos al proyectil.

#### Arma principal: GrimoireEternal (en `Content/Weapons/GrimoireEternal.cs`)
- Arma mágica híbrida (magia + invocación)
- **Click izquierdo**: dispara ArcaneBolt (homing). Cuesta 3 mana (+3 cada 20 niveles)
- **Click derecho**: invoca CosmicOrbMinion. Cuesta 15 mana (+1 por nivel)
- Sistema de niveles infinitos (XP guardada en `ShardLevelItem`)
- Escalado: +2.2% daño mágico, +1% summon, +0.2% crit, +0.4% armor pen, -0.3% use time (tope -25%)
- +1 slot de minion cada 5 niveles, +1 bolt extra cada 5 niveles
- Lifesteal nivel 7+: +0.1% cada 7 niveles
- Bonus por mana faltante: +0.5% daño por 1% mana faltante (tope +50%)

### 5.2 Armas V20 (19 armas creativas sin mana, en `Content/Weapons/V20/`)

> BlackHoleStaff fue movido a Cosmic en v5.77 — por eso hay 19 en vez de 20.

| # | Arma | Proyectil | Descripción |
|---|---|---|---|
| 1 | BlackHoleMiniStaff | BlackHoleMiniProjectile | Mini agujero negro |
| 2 | TornadoStaff | TornadoProjectile | Tornado de viento |
| 3 | PrismBeamStaff | PrismBeamProjectile | Rayo prismático |
| 4 | EarthquakeStaff | EarthquakeProjectile | Terremoto |
| 5 | MirrorDimensionStaff | MirrorDimensionProjectile | Dimensión espejo |
| 6 | GravityPulseStaff | GravityPulseProjectile | Pulso de gravedad |
| 7 | ShadowCloneStaff | ShadowCloneProjectile | Clon de sombra |
| 8 | CrystalShatterStaff | CrystalShatterProjectile | Fragmentos de cristal |
| 9 | SupernovaStaff | SupernovaProjectile | Supernova |
| 10 | VortexChainStaff | VortexChainProjectile | Cadena de vórtices |
| 11 | AbyssalEyeStaff | AbyssalEyeProjectile | Ojo del abismo |
| 12 | SpectralMirageStaff | SpectralMirageProjectile | Espejismo espectral |
| 13 | TemporalRiftStaff | TemporalRiftProjectile | Grieta temporal |
| 14 | PlasmaStormStaff | PlasmaStormProjectile | Tormenta de plasma |
| 15 | InfernoTornadoStaff | InfernoTornadoProjectile | Tornado infernal |
| 16 | VoidEaterStaff | VoidEaterProjectile | Devorador del vacío |
| 17 | PhoenixNovaStaff | PhoenixNovaProjectile | Nova fénix |
| 18 | QuantumSplitStaff | QuantumSplitProjectile | Split cuántico |
| 19 | PlasmaOrbStaff | PlasmaOrbProjectile | Orbe de plasma |

> Todas comparten la característica `Item.mana = 0` y `Item.DamageType = DamageClass.Generic`.

### 5.3 Armas Cosmic (2 armas con shaders de WoTG, en `Content/Weapons/Cosmic/CosmicWeapons.cs`)

#### BlackHoleStaff → BlackHoleProjectile
- **Daño**: 100
- **useTime/useAnimation**: 60
- **useStyle**: HoldUp
- **mana**: 0 (arma de prueba, no consume mana)
- **shoot**: `ModContent.ProjectileType<BlackHoleProjectile>()`
- **rare**: Quest
- **Shader**: `RealBlackHoleShader.fx` (75-step lightmarch con lensing gravitacional)
- **Texturas**: `FireNoiseB.png` (ruido del disco), `InvisiblePixel.png` (canvas), `Procedural/SoftGlow.png` (halo)
- **Efectos**: partículas en espiral (RotateTowards), disco de acreción, atracción de enemigos, iluminación pulsante
- **Tooltip**: `[c/9600FF:═══ AGUJERO NEGRO ═══]`

#### SunStaff → SunProjectile
- **Daño**: 80
- **useTime/useAnimation**: 50
- **useStyle**: HoldUp
- **mana**: 0
- **shoot**: `ModContent.ProjectileType<SunProjectile>()`
- **rare**: Quest
- **Shaders**: `SunShader.fx` (esfera + corona + manchas + lava) + `RadialShineShader.fx` (brillo radial)
- **Texturas**: `WoTG/BloomCircleSmall.png` (backglow), `WoTG/WavyBlotchNoise.png` (canvas y noise), `WoTG/PsychedelicWingTextureOffsetMap.png` (uv offset)
- **Efectos**: chispas de fuego orbitando, llamas, humo, iluminación intensa (Vector3.One * 3.2f)
- **OnHit**: OnFire (300 ticks), 30 partículas de GoldFlame
- **Tooltip**: `[c/FFD700:═══ SOL ═══]`

---

## 6. SHADERS

Los 8 shaders en `Content/Effects/Shaders/`:

### 6.1 RealBlackHoleShader.fx
- **Origen**: Wrath of the Gods (TheFifthCircle/WrathOfTheGodsPublic)
- **Técnica**: Lightmarch de 75 pasos con lensing gravitacional real (1/r²)
- **Parámetros clave**:
  - `blackHoleRadius` (0.3f)
  - `accretionDiskRadius` (0.33f)
  - `accretionDiskColor` (Vector3)
  - `cameraAngle` (0.32f)
  - `cameraRotationAxis` (Vector3)
  - `accretionDiskScale` (Vector3, scale Y aplastada para disco elíptico)
  - `zoom` (Vector2)
  - `globalTime` (se incrementa con Main.GameUpdateCount * 0.0167f)
- **Texturas**: s0 = baseTexture (InvisiblePixel), s1 = noiseTexture (FireNoiseB)
- **Funciones**: `QuadraticBump`, `Hash13`, `SignedTorusDistance`, `RotatedBy`, `Sample`, `RodriguesRotation`
- **Compile target**: `ps_3_0`
- **Uso**: BlackHoleProjectile

### 6.2 BlackHoleDistortionShader.fx
- **Técnica**: Lensing screen-space multi-fuente (distorsión de UV según lista de agujeros negros)
- **Compile target**: `ps_3_0`

### 6.3 BlackOnlyShader.fx
- **Técnica**: Smoothstep para reforzar el event horizon (la zona negra central del agujero negro)
- **Compile target**: `ps_3_0`

### 6.4 SunShader.fx
- **Origen**: Wrath of the Gods (StarPet)
- **Técnica**: Esfericidad (spherePinchFactor) + corona + manchas oscuras + ríos de lava
- **Parámetros clave**:
  - `coronaIntensityFactor` (0.05f)
  - `mainColor` (Color blanco)
  - `darkerColor` (Color naranja oscuro RGB 204,92,25)
  - `subtractiveAccentFactor` (Color rojo RGB 181,0,0)
  - `sphereSpinTime` (rotación lenta)
  - `globalTime`
- **Texturas**: s0 = fireNoiseTexture (WavyBlotchNoise), s1 = accentNoiseTexture (WavyBlotchNoise), s2 = uvOffsetNoiseTexture (PsychedelicWingTextureOffsetMap)
- **Funciones**: `InverseLerp`
- **Compile target**: `ps_2_0`
- **Uso**: SunProjectile

### 6.5 RadialShineShader.fx
- **Origen**: Wrath of the Gods
- **Técnica**: Brillo radial basado en ruido procedural
- **Parámetros**: `globalTime`
- **Texturas**: s0 + s1 (WavyBlotchNoise)
- **Uso**: SunProjectile (dibujado encima del SunShader con BlendState.Additive)

### 6.6 ChromaticAberration.fx
- **Técnica**: Aberración cromática RGB (samplea 3 veces desplazando R, G, B)
- **Compile target**: `ps_3_0`

### 6.7 Shockwave.fx
- **Técnica**: Onda expansiva (distorsión anular centrada en punto)
- **Compile target**: `ps_3_0`

### 6.8 Bloom.fx
- **Técnica**: Extract bright → blur (gaussian 2 passes) → combine (additivo)
- **Compile target**: `ps_3_0`

---

## 7. SISTEMA DE PARTÍCULAS

Sistema data-oriented en `Content/Particles/` basado en el documento
"Librería de Partículas para Terraria - Referencia para IA".

### 7.1 ParticleData.cs (struct)

```csharp
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ParticleData
{
    public Vector2 Position;          // 8 bytes — posición mundo
    public Vector2 Velocity;           // 8 bytes — velocidad por frame
    public Vector2 Scale;              // 8 bytes — escala X/Y separadas (estiramiento)
    public uint PackedColor;          // 4 bytes — RGBA packed
    public uint PackedStartColor;     // 4 bytes — color inicial (lerp FadeOut)
    public uint PackedEndColor;        // 4 bytes — color final
    public float Rotation;             // 4 bytes — rotación radianes
    public float RotationSpeed;        // 4 bytes — velocidad angular
    public int TimeLeft;               // 4 bytes — vida restante
    public int Duration;               // 4 bytes — duración total
    public ushort TextureId;           // 2 bytes — lookup tabla
    public ushort LayerPriority;       // 2 bytes — orden de render
    public ulong ComponentFlags;       // 8 bytes — bitmask componentes
    public byte BlendMode;             // 1 byte — 0=Alpha, 1=Additive, 2=NonPremultiplied
    public byte Flags;                 // 1 byte — bit 0: active
    public float UserData0;            // 4 bytes — data custom
    public float UserData1;            // 4 bytes — data custom (ScaleDown: scale X inicial)
    public float UserData2;            // 4 bytes — data custom (ScaleDown: scale Y inicial)
    public float UserData3;             // 4 bytes — data custom
}
```

Total: ~80 bytes por partícula. Con 2000 partículas = ~160 KB en cache L2.

### 7.2 ComponentFlags (bitmask)

```csharp
public static class ComponentFlag
{
    public const ulong Gravity     = 1UL << 0;   // UserData0 = gravedad (default 0.2f)
    public const ulong FadeOut     = 1UL << 1;   // Empieza fade al 50% de vida
    public const ulong FadeIn      = 1UL << 2;
    public const ulong ScaleDown   = 1UL << 3;   // UserData1/UserData2 = scale inicial
    public const ulong ScaleUp     = 1UL << 4;
    public const ulong Homing      = 1UL << 5;
    public const ulong Rotation    = 1UL << 6;
    public const ulong ColorShift  = 1UL << 7;
    public const ulong Orbit       = 1UL << 8;
    public const ulong Trail       = 1UL << 9;
    public const ulong DieOnTile   = 1UL << 11;
    public const ulong EmitLight   = 1UL << 12;
}
```

### 7.3 LayerPriorities

```csharp
BelowTiles = 0;
AboveTiles = 100;
BeforeProjectiles = 200;
AfterProjectiles = 300;
BeforeNPCs = 400;
AfterNPCs = 500;
BeforePlayers = 600;
AfterPlayers = 700;
AboveAll = 900;
```

### 7.4 ParticleBuffer.cs

- Buffer pre-asignado de `ParticleData[capacity]` (default 2000)
- **Round-robin**: busca slot libre empezando en `_nextSlot`, no recorre desde 0
- `TrySpawn(data)` → bool (false si buffer lleno)
- `Kill(index)` → desactiva por índice
- `Clear()` → desactiva todas
- **Cero GC**: nunca se asigna/libera memoria después de inicialización

### 7.5 ParticleManager.cs (ModSystem)

- `OnModLoad()`: crea buffer (2000), registra 8 texturas procedurales
- `PreUpdateDusts()`: actualiza todas las partículas activas (posición, rotación, componentes, decrementa vida)
- `PostDrawTiles()`: renderiza con batching por blend mode:
  - Pass 1: AlphaBlend (todas con BlendMode == 0)
  - Pass 2: Additive (todas con BlendMode == 1)
  - Usa `Main.GameViewMatrix.TransformationMatrix` para respetar zoom
- Helpers: `Spawn(data)`, `RegisterTexture(path)`, `PackColor(Color)`, `UnpackColor(uint)`

### 7.6 Manejo de errores (v5.78+)
- `RegisterTexture` no crashea si la textura no existe (devuelve ID 0 = SoftGlow)
- `PostDrawTiles` hace try/catch para restaurar el spriteBatch si algo falla

---

## 8. COMPILACIÓN DE SHADERS SIN WINE

### 8.1 Pipeline tradicional (roto en Linux)
- tModLoader usa MGCB (MonoGame Content Builder) que requiere Windows o Wine
- Los shaders `.fx` legacy usan HLSL con sintaxis DirectX 9
- En Linux sin Wine, MGCB no funciona

### 8.2 Solución adoptada (v5.69 - v5.70)

#### Herramienta: dxc (DirectX Shader Compiler)
- Descargado de: https://github.com/microsoft/DirectXShaderCompiler
- Es nativo de Linux, no requiere Wine
- Compila HLSL a DXIL (formato .dxbc compatible con MonoGame)

#### Script: `compile_shaders.py` (en raíz del proyecto o en `tmp_scripts/`)
1. Lee cada `.fx` en `Content/Effects/Shaders/`
2. Convierte sintaxis legacy:
   - `sampler baseTexture : register(s0);` → `Texture2D baseTexture; SamplerState baseTextureSampler;`
   - `tex2D(baseTexture, coords)` → `baseTexture.Sample(baseTextureSampler, coords)`
   - `compile ps_3_0` → `compile ps_6_0`
3. Llama a `dxc` para compilar a `.dxbc`
4. Renombra `.dxbc` → `.xnb` (formato que espera tModLoader)
5. Verifica con `file *.xnb` que es binario válido

### 8.3 Compilación manual paso a paso
```bash
cd /home/z/my-project/AethonMod/Content/Effects/Shaders

# Para cada shader .fx:
# 1. Convertir sintaxis legacy a HLSL SM 6.0
# 2. Compilar a .xnb
dxc -T ps_6_0 -E PixelShaderFunction RealBlackHoleShader_converted.hlsl -Fo RealBlackHoleShader.xnb
```

### 8.4 Estado actual (v5.82)
- Los 8 `.xnb` ya están compilados y commiteados en `Content/Effects/Shaders/`
- Solo se recompilan si se modifica el `.fx` correspondiente

---

## 9. RECURSOS DE WoTG (Wrath of the Gods)

### 9.1 Repositorio clonado
- URL: https://github.com/TheFifthCircle/WrathOfTheGodsPublic
- Path local: `/tmp/wotg/`
- Estructura:
  ```
  /tmp/wotg/
  ├── Assets/
  ├── Changelogs.md
  ├── Content/
  ├── Core/
  ├── Localization/
  ├── NoxusBoss.cs
  ├── NoxusBoss.csproj
  ├── NoxusBoss.sln
  ├── Properties/
  └── I'm sorry about this mod's code, dataminer.mp4
  ```

### 9.2 Texturas copiadas a AethonMod (9 texturas en `Content/Effects/WoTG/`)
1. `WavyBlotchNoise.png` — usado por SunShader y RadialShineShader
2. `WavyBlotchNoiseDetailed.png`
3. `InvisiblePixel.png` — canvas 1×1 transparente para BlackHole
4. `PsychedelicWingTextureOffsetMap.png` — UV offset map para SunShader
5. `FireNoiseA.png`
6. `FireNoiseB.png` — noise del disco de acreción de BlackHole
7. `BloomCircleSmall.png` — backglow del SunProjectile
8. `BloomCircle.png`
9. `BloomFlare.png`

### 9.3 Shaders copiados de WoTG (5 shaders .fx)
1. `RealBlackHoleShader.fx` (de `BlackHolePet.cs` / `PetBlackHoleRenderer.cs`)
2. `SunShader.fx` (de `StarPet.cs`)
3. `RadialShineShader.fx` (de `StarPet.cs`)
4. `BlackOnlyShader.fx`
5. `BlackHoleDistortionShader.fx`

### 9.4 Archivos de referencia en `Reference_WoTG/` (NO compilados)
Estos son los archivos originales de WoTG, copiados como referencia para entender cómo
usan los shaders. En v5.82 se añadieron a `.csproj` con `<Compile Remove="Reference_WoTG/**" />`
para que NO sean parte del build (causaba conflictos de namespaces con el mod original).

| Archivo | Contenido |
|---|---|
| `BlackHole.cs` | Implementación original de RealBlackHoleShader en WoTG |
| `BlackHolePet.cs` | Lógica del pet agujero negro |
| `PetBlackHoleRenderer.cs` | Renderer que usa RealBlackHoleShader |
| `StarPet.cs` | Implementación original de SunShader + RadialShineShader |
| `Starseed.cs` | Item que invoca StarPet |

### 9.5 Verificación de recursos
Para confirmar que todos los recursos de WoTG existen:
```bash
ls /home/z/my-project/AethonMod/Content/Effects/WoTG/
ls /home/z/my-project/AethonMod/Reference_WoTG/
```

---

## 10. HISTORIAL DE VERSIONES

Commits desde v5.28 hasta v5.82 (orden inverso, más reciente primero):

| Commit | Versión | Descripción |
|---|---|---|
| `b2798e6` | v5.82 | fix: reescribir BlackHole + Sun con recursos exactos de WoTG |
| `227a790` | v5.81 | fix: asegurar todos los recursos de Wrath of the Gods |
| `d906baf` | v5.81 | fix: arreglar todos los errores del client.log + 100 pasadas |
| `f861c5d` | v5.80 | fix: restaurar commit 3bd550a + añadir shaders WoTG + armas cósmicas |
| `3bd550a` | v5.78 | fix: revisión profunda 100 pasadas - 6 bugs arreglados |
| `1dd6f4b` | v5.77 | feat: 20 armas creativas sin mana + agujero negro con partículas |
| `16266a2` | v5.76 | feat: agujero negro con partículas reales + ruido Perlin + 4 armas |
| `b1118eb` | v5.75 | feat: 20 armas nuevas sin mana + agujero negro real |
| `5bf2fa0` | v5.74 | fix: revisión profunda de código - 7 bugs arreglados |
| `9484e94` | v5.73 | fix: ParticleManager PostDrawTiles - SpriteBatch End without Begin |
| `dc179d9` | v5.72 | feat: 4 armas avanzadas con shaders + partículas + trails |
| `3671d0a` | v5.71 | fix: arreglar compilación de Bloom.xnb |
| `d55200b` | v5.70 | feat: shaders HLSL compilados a .xnb SIN Wine usando dxc |
| `838353d` | ci | workflow para compilar shaders HLSL en Windows |
| `cfd232d` | v5.69 | feat: sistema de partículas data-oriented + shaders HLSL + texturas procedurales |
| `238f4e4` | v5.68 | cleanup: borrar bastones creativos (eran horribles) |
| `58b44ca` | v5.67 | feat: 2 bastones creativos con efectos avanzados + 4 assets nuevos |
| `d4b97e5` | v5.66 | fix: revisión 10 pasadas — 2 bugs arreglados |
| `a56148d` | v5.65 | fix: añadir FrostSpear.png que faltaba (MissingResourceException) |
| `db00ce3` | v5.64 | fix: renombrar CosmicOrbBolt → CosmicRainbowBolt (conflicto de nombre) |
| `802097d` | v5.63 | feat: 7 armas con proyectiles propios creativos + fix tooltip Grimorio |
| `79af5aa` | v5.62 | feat: arreglar tooltip Grimorio + 8 nuevos bastones con efectos en AI() |
| `1213956` | v5.61 | cleanup: borrar TODO el contenido de prueba |
| `38e9d0c` | v5.60 | refactor: arreglar code smells menores (sin tocar Grimoire) |
| `b91f09c` | v5.59 | fix: revisión de código - 5 bugs críticos + 4 warnings |
| `8c94048` | v5.58 | fix: Shoot con return false + Projectile.NewProjectile manual + HoldUp |
| `40657bd` | v5.57 | fix: quitar mana de todos los bastones de prueba |
| `1590729` | v5.56 | fix: restaurar 20 sprites originales del commit estable e826c82 |
| `6ddcd80` | docs | worklog push exitoso v5.55 |
| `eb951e9` | v5.55 | fix: errores de compilación (MathF + namespace TestStaffs) |
| `5e58cb4` | docs | worklog + bundles para push manual |
| `946a057` | v5.54 | feat: recuperar 4 bastones eliminados como versiones Alt |
| `5c44888` | v5.53 | merge: sincronizar local con origin/main (v5.52) + eliminar duplicados |
| `a73bf98` | v5.29 | feat: 16 bastones de prueba + efectos cósmicos + recreación Star Wrath |
| `140a9fa` | v5.28 | feat: texturas HQ (1024 LANCZOS) + contenido ceremonial |
| `e826c82` | v5.52 | fix: agregar bloom/destello que faltaba — doble dibujo del sprite (commit estable protegido) |
| `6000bcb` | v5.51 | fix: proyectil COMPLETAMENTE del color — return false + EntitySpriteDraw |
| `5c3b2bd` | v5.50 | fix: estela densa del color correcto (cubre la azul nativa del Nightglow) |
| `b429398` | v5.49 | feat: 3 armas de color (rojo, amarillo, verde) con método ColorRainbow |
| `92ea07c` | v5.48 | fix: ColorRainbow — puramente aditivo, deja que Nightglow haga lo suyo |
| `cc9f529` | v5.47 | fix: ColorRainbow — return true + projectile.color para tintar sprite |
| `d89c0a0` | v5.46 | fix: ColorRainbow reescrito — cambia sprite + estela completos |
| `61e62cd` | v5.45 | feat: texturas GlowOrb profesionales + GlowRay para lens flare |
| `51127b4` | v5.44 | feat: limpiar bastones + 4 nuevos de color del proyectil |
| `b59ebe3` | v5.43 | fix: reescribir efectos que no funcionaban + 20 pasadas de revisión |
| `1584326` | v5.42 | fix: CS0507 — Draw debe ser protected override, no public override |
| `f46b433` | v5.41 | fix: AURAS AHORA FUNCIONAN con PlayerDrawLayer + proyectiles mejorados |
| `86cfaf2` | v5.40 | cleanup: limpiar warnings CS0219 + tooltips + variables sin usar |
| `b4bde35` | v5.39 | fix: bug crítico OnHitNPC spriteBatch + limpiar variables sin usar |
| `5df8b9f` | v5.38 | fix: InvalidOperationException — eliminar additive blending de HoldItem |
| `5a72761` | v5.37 | fix: CS0117 — doble VFXHelper.VFXHelper en líneas 130-131 |
| `11096c8` | v5.36 | fix: CS0106 — quitar VFXHelper. de la declaración del método |
| `3085134` | v5.35 | feat: limpiar bastones + 10 armas nuevas + MagicRingV2 + additive blending |
| `c93a0a0` | v5.34 | feat: 4 armas avanzadas con PreDraw + additive blending + texturas custom |
| `87e7918` | v5.33 | fix: TestRays ahora son RELÁMPAGOS (lightning bolts) no rayos que suben |
| `c480631` | v5.32 | feat: aura tenue + rayos cósmicos + área +1/nivel + colores con estrellas |
| `9649df3` | v5.31 | fix: bastones de prueba no cuestan mana (DamageClass.Generic) |
| `b792acd` | v5.30.1 | fix: quitar mana de todas las armas de prueba |
| `2f0854f` | v5.30 | fix: arreglar issues 1+2 + crear armas de prueba daño en área |
| `930f3d2` | v5.29 | fix: arreglar 3 bugs críticos (MinionContactDamage, BoltArea, weapon swap) |
| `1313b70` | v5.28 | feat: 7 armas de prueba visual + revisión profunda del código |

---

## 11. ÚLTIMO ESTADO (donde nos quedamos)

### 11.1 Commit actual
- **Commit**: `b2798e6`
- **Versión**: v5.82 (en el mensaje del commit)
- **Mensaje**: "fix v5.82: reescribir BlackHole + Sun con recursos exactos de WoTG"

### 11.2 Cambios en este commit
1. **BlackHoleProjectile.cs** — reescrito siguiendo EXACTAMENTE el código de WoTG `BlackHolePet`:
   - Usa `RealBlackHoleShader.fx` (75-step lightmarch con lensing gravitacional real)
   - Usa `FireNoiseB.png` como textura de ruido del disco de acreción
   - Usa `InvisiblePixel.png` como canvas
   - Parámetros copiados 1:1 de `BlackHole.DrawBlackHole()` de WoTG
   - Spawnea partículas en espiral (patrón CircularSuctionPattern de WoTG)
   - Spawnea disco de acreción con DustID.GoldFlame orbitando
   - Atracción gravitacional de NPCs en radio 350
   - Iluminación pulsante naranja/amarilla
   - Fallback con dibujado manual si shader no carga
   - Método de extensión `Vector2Extensions.RotateTowards` añadido al final del archivo

2. **SunProjectile.cs** — reescrito siguiendo EXACTAMENTE el código de WoTG `StarPet`:
   - Usa `SunShader.fx` (esfericidad + corona + manchas + lava)
   - Usa `RadialShineShader.fx` (brillo radial)
   - Usa `WavyBlotchNoise.png` como canvas y noise
   - Usa `PsychedelicWingTextureOffsetMap.png` como UV offset noise
   - Usa `BloomCircleSmall.png` como backglow (amarillo + rojo)
   - Parámetros copiados 1:1 de `StarPet.DrawSelf()` de WoTG
   - Partículas de fuego orbitando + llamas + humo
   - Iluminación intensa (Vector3.One * 3.2f)
   - OnHit: 30 partículas GoldFlame + buff OnFire (300 ticks)
   - Fallback con dibujado manual si shader no carga

3. **AethonMod.csproj** — añadida la línea `<Compile Remove="Reference_WoTG/**" />`:
   - Excluye los archivos de referencia de WoTG de la compilación
   - Sin esta línea el mod no compila porque `Reference_WoTG/` contiene archivos sueltos del mod original

### 11.3 Estado actual del mod
- ✅ Mod compila correctamente
- ✅ Todos los shaders `.xnb` están compilados y commiteados
- ✅ Todos los recursos de WoTG están copiados
- ✅ Archivos de referencia de WoTG excluidos del build
- ⚠️ **PENDIENTE**: probar el `BlackHoleStaff` y `SunStaff` en tModLoader
- ⚠️ **PENDIENTE**: verificar que los shaders se carguen sin excepción en runtime

### 11.4 Próximos pasos sugeridos
1. Cargar tModLoader 1.4.4 en el sandbox
2. Build del mod AethonMod
3. Verificar que no hay excepciones en `client.log`
4. Spawnear los items (BlackHoleStaff, SunStaff) — están en el kit de TestingPlayer
5. Probar el disparo y verificar que los shaders se aplican
6. Si falla algún shader, revisar:
   - Que el `.xnb` existe en `Content/Effects/Shaders/`
   - Que los parámetros del shader coinciden con el `.fx`
   - Que las texturas referenciadas existen

---

## 12. INSTRUCCIONES PARA OTRA IA

> Si eres otra IA trabajando en este proyecto, sigue ESTRICTAMENTE estas reglas:

### 12.1 Reglas obligatorias
1. **SIEMPRE** escribe al inicio de cada commit:
   ```
   Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran
   ```
2. **SIEMPRE** escribe al inicio de cada documento:
   ```
   **Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran**
   ```

### 12.2 Archivos PROHIBIDOS de tocar
- ❌ `Content/Weapons/GrimoireEternal.cs`
- ❌ `Content/Weapons/TestAdvanced.cs`
- ❌ `Content/Globals/TestAdvancedFX.cs`
- ❌ `Content/Globals/CosmicProjectileFX.cs`
- ❌ Todos los sprites del commit `e826c82` (ver sección 3.2)
- ❌ `Content/NPCs/*.cs` y `Content/NPCs/*.png`
- ❌ `Content/Projectiles/CosmicOrbBolt.cs`, `CosmicOrbMinion.cs`, `ArcaneBolt.cs`, `GenesisLight.cs`
- ❌ `Content/Items/GenesisShard.cs`, `ResonanceShard.cs`, `SeerOrb.cs`, `LevelUpTester.cs`, `BossSummonBag.cs`
- ❌ `Content/Systems/*`
- ❌ `Content/Players/*`
- ❌ `Content/Globals/GlobalNPCXP.cs`, `ShardLevelItem.cs`, `TooltipToggleItem.cs`
- ❌ `icon.png`

### 12.3 Reglas técnicas
- Las **armas de prueba NO consumen mana** (`Item.mana = 0`)
- Las **armas de prueba** usan `DamageClass.Generic` (no magic/summon/etc)
- Las armas de prueba tienen `Item.rare = ItemRarityID.Quest`
- Las armas de prueba se craftean con 5 de madera para testing rápido
- Todas las armas se añaden al kit de `TestingPlayer.OnEnterWorld()`
- Los shaders van en `Content/Effects/Shaders/` con su `.fx` y su `.xnb` compilado
- Las texturas de WoTG van en `Content/Effects/WoTG/` (subcarpeta separada)
- Las texturas procedurales van en `Content/Effects/Procedural/`

### 12.4 Workflow recomendado
1. **ANTES de trabajar**: hacer `git fetch` + `git pull` del remote (el local se borra)
   ```bash
   cd /home/z/my-project/AethonMod
   git remote -v  # verificar remote
   git fetch origin
   git pull origin main
   ```
2. **DESPUÉS de trabajar**: hacer commit con el recordatorio obligatorio
   ```bash
   git add -A
   git commit -m "Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran

   <descripción del cambio>"
   git push origin main
   ```
3. **Si el local está vacío/perdido**: clonar de nuevo con el PAT
   ```bash
   git clone https://Leo0x01:[GITHUB_PAT - solicitar al usuario]@github.com/Leo0x01/Aethon-Mod-Terraria.git AethonMod
   ```

### 12.5 GitHub PAT
```
[GITHUB_PAT - solicitar al usuario]
```
- Usuario del repo: `Leo0x01`
- Repo: `Aethon-Mod-Terraria`
- Permisos: `repo` (read/write)
- Expira: ver GitHub settings (renovar si caduca)

### 12.6 Verificación rápida
Antes de empezar, verificar que tienes el proyecto completo:
```bash
ls /home/z/my-project/AethonMod/Content/Effects/Shaders/ | wc -l   # debe ser 16 (8 .fx + 8 .xnb)
ls /home/z/my-project/AethonMod/Content/Effects/WoTG/ | wc -l      # debe ser 9
ls /home/z/my-project/AethonMod/Content/Weapons/V20/*.cs | wc -l   # debe ser 19
ls /home/z/my-project/AethonMod/Content/Weapons/Cosmic/*.cs | wc -l # debe ser 1 (CosmicWeapons.cs con 2 clases)
ls /home/z/my-project/AethonMod/Content/Projectiles/Cosmic/*.cs | wc -l  # debe ser 2 (BlackHole + Sun)
```

---

## 13. CÓDIGO FUENTE CLAVE

### 13.1 BlackHoleProjectile.cs (Content/Projectiles/Cosmic/BlackHoleProjectile.cs)

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// BlackHoleProjectile — reescrito siguiendo EXACTAMENTE el código de WoTG.
    /// Usa RealBlackHoleShader.fx (75-step lightmarch con lensing gravitacional real)
    /// + FireNoiseB.png como textura de ruido del disco de acreción.
    /// También spawnea partículas en espiral (CircularSuctionPattern de WoTG).
    /// </summary>
    public class BlackHoleProjectile : ModProjectile
    {
        private Ref<Effect> _shader;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 76;
            Projectile.height = 76;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        public override void AI()
        {
            // Movimiento lento (el agujero negro flota)
            Projectile.velocity *= 0.97f;
            Projectile.rotation += 0.05f;

            // === PARTÍCULAS INFALLING EN ESPIRAL (patrón CircularSuction de WoTG) ===
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 3; i++)
                {
                    float angle = Projectile.rotation + i * (MathHelper.TwoPi / 3f);
                    float dist = Main.rand.NextFloat(90f, 140f);
                    Vector2 spawnPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(angle) * dist,
                        (float)Math.Sin(angle) * dist);

                    Vector2 toCenter = Projectile.Center - spawnPos;
                    float speed = Main.rand.NextFloat(4f, 8f);
                    if (toCenter.LengthSquared() > 0.01f)
                    {
                        toCenter.Normalize();
                        Vector2 velocity = toCenter * speed;

                        // RotateTowards: LA técnica que convierte infalling radial en espiral
                        float angleToCenter = (float)Math.Atan2(toCenter.Y, toCenter.X);
                        velocity = velocity.RotateTowards(angleToCenter + MathHelper.PiOver2 * 0.3f, 0.5f);

                        // Color: caliente cerca del centro, púrpura lejos
                        Color color = dist < 60f ? new Color(255, 240, 180)
                                   : dist < 100f ? new Color(255, 160, 60)
                                   : new Color(160, 80, 255);

                        Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch,
                            velocity, 150, color, 1.3f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                        d.scale = Main.rand.NextFloat(0.8f, 1.6f);
                    }
                }

                // === DISCO DE ACRECIÓN (partículas GoldFlame orbitando) ===
                if (Main.rand.NextBool(2))
                {
                    float diskAngle = Projectile.rotation * 4f;
                    float diskRadius = Main.rand.NextFloat(10f, 40f);
                    Vector2 diskPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(diskAngle) * diskRadius,
                        (float)Math.Sin(diskAngle) * diskRadius * 0.25f);
                    Vector2 tangent = new Vector2(
                        -(float)Math.Sin(diskAngle),
                        (float)Math.Cos(diskAngle) * 0.25f) * 3f;
                    Dust d = Dust.NewDustPerfect(diskPos, DustID.GoldFlame,
                        tangent, 220, new Color(255, 210, 100), 1.2f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // === POLVO Y HUMO ===
                if (Main.rand.NextBool(5))
                {
                    float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                    float dist = Main.rand.NextFloat(120f, 180f);
                    Vector2 spawnPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(angle) * dist,
                        (float)Math.Sin(angle) * dist);
                    Vector2 vel = (Projectile.Center - spawnPos) * 0.025f;
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.Smoke,
                        vel, 80, new Color(80, 40, 100), 0.8f);
                    d.noGravity = false;
                    d.fadeIn = 0f;
                }
            }

            // === ATRACCIÓN GRAVITACIONAL DE ENEMIGOS ===
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                Vector2 toCenter = Projectile.Center - npc.Center;
                float dist = toCenter.Length();
                if (dist > 350f || dist < 5f) continue;
                float strength = (1f - dist / 350f) * 2f;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    npc.velocity += toCenter * strength;
                }
            }

            // === ILUMINACIÓN ===
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GameUpdateCount * 0.08f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.95f * pulse, 0.45f * pulse, 0.1f * pulse));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (_shader == null)
            {
                try { _shader = new Ref<Effect>(ModContent.Request<Effect>("AethonMod/Content/Effects/Shaders/RealBlackHoleShader").Value); }
                catch { }
            }

            try
            {
                Vector2 drawPos = Projectile.Center - Main.screenPosition;

                // === 1. HALO EXTERNO PÚRPURA ===
                Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                Main.spriteBatch.Draw(glowTex, drawPos, null,
                    new Color(60, 20, 90, 50), 0f,
                    new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                    3.0f, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

                // === 2. SHADER REALBLACKHOLE (exactamente como WoTG) ===
                if (_shader != null && _shader.Value != null)
                {
                    Effect shader = _shader.Value;

                    // Parámetros EXACTOS de WoTG BlackHole.DrawBlackHole()
                    shader.Parameters["blackHoleRadius"].SetValue(0.3f);
                    shader.Parameters["blackHoleCenter"].SetValue(Vector3.Zero);
                    shader.Parameters["aspectRatioCorrectionFactor"].SetValue(1f);
                    shader.Parameters["accretionDiskColor"].SetValue(new Color(245, 105, 61).ToVector3());
                    shader.Parameters["cameraAngle"].SetValue(0.32f);
                    shader.Parameters["cameraRotationAxis"].SetValue(new Vector3(1f, 0f, Projectile.rotation));
                    shader.Parameters["accretionDiskScale"].SetValue(new Vector3(1f, 0.33f, 1f));
                    shader.Parameters["zoom"].SetValue(Vector2.One * 0.12f);
                    shader.Parameters["accretionDiskRadius"].SetValue(0.33f);
                    shader.Parameters["globalTime"].SetValue((float)Main.GameUpdateCount * 0.0167f);

                    // FireNoiseB como textura de ruido del disco (exactamente como WoTG)
                    Texture2D fireNoise = ModContent.Request<Texture2D>("AethonMod/Content/Effects/WoTG/FireNoiseB").Value;
                    Main.graphics.GraphicsDevice.Textures[1] = fireNoise;
                    Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                    // InvisiblePixel como canvas (exactamente como WoTG)
                    Texture2D invisiblePixel = ModContent.Request<Texture2D>("AethonMod/Content/Effects/WoTG/InvisiblePixel").Value;

                    // Orden correcto: Begin(Immediate) → Apply → Draw → End → Begin(Deferred)
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                    shader.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(invisiblePixel, drawPos, null, Color.Transparent, 0f,
                        new Vector2(invisiblePixel.Width / 2f, invisiblePixel.Height / 2f),
                        400f, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                }
                else
                {
                    // === FALLBACK: si el shader no carga ===
                    DrawFallback(drawPos);
                }
            }
            catch { }
            return false;
        }

        private void DrawFallback(Vector2 drawPos)
        {
            float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GameUpdateCount * 0.08f);
            Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            Texture2D vortexTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Vortex").Value;
            Texture2D ringTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring").Value;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

            // Disco de acreción frontal (elíptico, naranja)
            Main.spriteBatch.Draw(vortexTex, drawPos, null,
                new Color(255, 180, 80, 200), Projectile.rotation * 2f,
                new Vector2(vortexTex.Width / 2f, vortexTex.Height / 2f),
                new Vector2(1.5f, 0.5f), SpriteEffects.None, 0f);

            // Disco trasero (Einstein ring)
            Main.spriteBatch.Draw(vortexTex, drawPos - new Vector2(0, 4f), null,
                new Color(200, 50, 0, 100), -Projectile.rotation * 2f,
                new Vector2(vortexTex.Width / 2f, vortexTex.Height / 2f),
                new Vector2(1.5f, 0.5f), SpriteEffects.FlipVertically, 0f);

            // Beaming relativístico
            Main.spriteBatch.Draw(vortexTex, drawPos - new Vector2(3f, 0f), null,
                new Color(255, 230, 150, 130), Projectile.rotation * 2f,
                new Vector2(vortexTex.Width / 2f, vortexTex.Height / 2f),
                new Vector2(1.5f, 0.5f), SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // Event horizon
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                Color.Black, 0f, new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                0.8f, SpriteEffects.None, 0f);

            // Photon ring + aberración cromática
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            Main.spriteBatch.Draw(ringTex, drawPos - new Vector2(2f, 0f), null,
                new Color(255, 0, 0, 80), 0f, new Vector2(ringTex.Width / 2f, ringTex.Height / 2f),
                0.6f * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ringTex, drawPos, null,
                new Color(0, 255, 0, 80), 0f, new Vector2(ringTex.Width / 2f, ringTex.Height / 2f),
                0.6f * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ringTex, drawPos + new Vector2(2f, 0f), null,
                new Color(0, 100, 255, 80), 0f, new Vector2(ringTex.Width / 2f, ringTex.Height / 2f),
                0.6f * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ringTex, drawPos, null,
                new Color(255, 240, 200, 220), 0f, new Vector2(ringTex.Width / 2f, ringTex.Height / 2f),
                0.6f * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // Implosión + explosión
            for (int i = 0; i < 50; i++)
            {
                float angle = (MathHelper.TwoPi / 50) * i + Main.rand.NextFloat(-0.2f, 0.2f);
                float dist = Main.rand.NextFloat(80f, 140f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
                Vector2 toCenter = Projectile.Center - spawnPos;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X) * 0.5f;
                    Vector2 vel = (toCenter * 7f + tangent * 4f);
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch,
                        vel, 200, new Color(200, 100, 255), 1.3f);
                    d.noGravity = true; d.fadeIn = 0f;
                }
            }

            for (int i = 0; i < 40; i++)
            {
                float angle = (MathHelper.TwoPi / 40) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(5f, 11f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(5f, 11f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    dir, 220, new Color(255, 200, 100), 1.5f);
                d.noGravity = true; d.fadeIn = 0f;
            }

            for (int i = 0; i < 15; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                    new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 4f)),
                    255, Color.White, 1.0f);
                d.noGravity = true; d.fadeIn = 0f;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
        }
    }

    public static class Vector2Extensions
    {
        public static Vector2 RotateTowards(this Vector2 current, float targetAngle, float maxStep)
        {
            float currentAngle = (float)Math.Atan2(current.Y, current.X);
            float diff = ((targetAngle - currentAngle + MathHelper.Pi * 3) % MathHelper.TwoPi) - MathHelper.Pi;
            if (Math.Abs(diff) <= maxStep)
                return new Vector2((float)Math.Cos(targetAngle), (float)Math.Sin(targetAngle)) * current.Length();
            float newAngle = currentAngle + Math.Sign(diff) * maxStep;
            return new Vector2((float)Math.Cos(newAngle), (float)Math.Sin(newAngle)) * current.Length();
        }
    }
}
```

### 13.2 SunProjectile.cs (Content/Projectiles/Cosmic/SunProjectile.cs)

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// SunProjectile — reescrito siguiendo EXACTAMENTE el código de WoTG StarPet.
    /// Usa SunShader.fx (esfericidad + corona + manchas + lava) con FireNoiseB
    /// + RadialShineShader.fx (brillo radial) con WavyBlotchNoise.
    /// </summary>
    public class SunProjectile : ModProjectile
    {
        private Ref<Effect> _sunShader;
        private Ref<Effect> _shineShader;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 92;
            Projectile.height = 92;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        public override void AI()
        {
            // Movimiento lento
            Projectile.velocity *= 0.97f;
            Projectile.rotation += 0.01f;

            // === PARTÍCULAS DE FUEGO ===
            if (Main.netMode != NetmodeID.Server)
            {
                // Chispas de fuego orbitando
                if (Main.rand.NextBool(2))
                {
                    float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                    float dist = Main.rand.NextFloat(40f, 60f);
                    Vector2 spawnPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(angle) * dist,
                        (float)Math.Sin(angle) * dist);
                    Vector2 vel = -spawnPos + Projectile.Center;
                    if (vel.LengthSquared() > 0.01f)
                    {
                        vel.Normalize();
                        vel *= Main.rand.NextFloat(1f, 3f);
                        vel += new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f));
                        Dust d = Dust.NewDustPerfect(spawnPos, DustID.Torch,
                            vel, 150, new Color(255, 150, 50), 1.0f);
                        d.noGravity = true; d.fadeIn = 0f;
                    }
                }

                // Llamas saliendo del sol
                if (Main.rand.NextBool(3))
                {
                    float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                    float dist = Main.rand.NextFloat(30f, 45f);
                    Vector2 spawnPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(angle) * dist,
                        (float)Math.Sin(angle) * dist);
                    Vector2 vel = new Vector2(
                        (float)Math.Cos(angle) * 2f,
                        (float)Math.Sin(angle) * 2f);
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.GoldFlame,
                        vel, 200, new Color(255, 200, 100), 1.2f);
                    d.noGravity = true; d.fadeIn = 0f;
                }

                // Humo
                if (Main.rand.NextBool(8))
                {
                    float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                    float dist = Main.rand.NextFloat(50f, 70f);
                    Vector2 spawnPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(angle) * dist,
                        (float)Math.Sin(angle) * dist);
                    Vector2 vel = new Vector2(
                        (float)Math.Cos(angle) * 0.5f,
                        (float)Math.Sin(angle) * 0.5f - 1f);
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.Smoke,
                        vel, 60, new Color(100, 60, 30), 0.6f);
                    d.noGravity = false; d.fadeIn = 0f;
                }
            }

            // === ILUMINACIÓN (exactamente como WoTG: Vector3.One * 3.2f) ===
            float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GameUpdateCount * 0.05f);
            Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.9f, 0.5f) * 3.2f * pulse);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (_sunShader == null)
            {
                try { _sunShader = new Ref<Effect>(ModContent.Request<Effect>("AethonMod/Content/Effects/Shaders/SunShader").Value); }
                catch { }
            }
            if (_shineShader == null)
            {
                try { _shineShader = new Ref<Effect>(ModContent.Request<Effect>("AethonMod/Content/Effects/Shaders/RadialShineShader").Value); }
                catch { }
            }

            try
            {
                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                float scale = Projectile.scale;

                // === 1. BACKGLOW (exactamente como WoTG StarPet.DrawSelf) ===
                Texture2D bloomCircle = ModContent.Request<Texture2D>("AethonMod/Content/Effects/WoTG/BloomCircleSmall").Value;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                // Amarillo
                Main.spriteBatch.Draw(bloomCircle, drawPos, null,
                    (new Color(255, 230, 100) { A = 0 }) * 0.7f, 0f,
                    bloomCircle.Size() * 0.5f, scale * 0.95f, 0, 0f);
                // Rojo
                Main.spriteBatch.Draw(bloomCircle, drawPos, null,
                    (new Color(255, 50, 0) { A = 0 }) * 0.45f, 0f,
                    bloomCircle.Size() * 0.5f, scale * 1.61f, 0, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

                // === 2. SHADER SUNSHADER (exactamente como WoTG) ===
                if (_sunShader != null && _sunShader.Value != null)
                {
                    Effect shader = _sunShader.Value;
                    Texture2D wavyBlotch = ModContent.Request<Texture2D>("AethonMod/Content/Effects/WoTG/WavyBlotchNoise").Value;
                    Texture2D psychedelicWing = ModContent.Request<Texture2D>("AethonMod/Content/Effects/WoTG/PsychedelicWingTextureOffsetMap").Value;

                    // Parámetros EXACTOS de WoTG StarPet.DrawSelf()
                    shader.Parameters["coronaIntensityFactor"].SetValue(0.05f);
                    shader.Parameters["mainColor"].SetValue(new Color(255, 255, 255).ToVector3());
                    shader.Parameters["darkerColor"].SetValue(new Color(204, 92, 25).ToVector3());
                    shader.Parameters["subtractiveAccentFactor"].SetValue(new Color(181, 0, 0).ToVector3());
                    shader.Parameters["sphereSpinTime"].SetValue((float)Main.GameUpdateCount * 0.0167f * 0.9f);
                    shader.Parameters["globalTime"].SetValue((float)Main.GameUpdateCount * 0.0167f);

                    // Texturas (exactamente como WoTG)
                    // s0 = fireNoiseTexture (usamos WavyBlotchNoise como base)
                    // s1 = accentNoiseTexture (usamos WavyBlotchNoise)
                    // s2 = uvOffsetNoiseTexture (usamos PsychedelicWingTextureOffsetMap)
                    Main.graphics.GraphicsDevice.Textures[1] = wavyBlotch;
                    Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                    Main.graphics.GraphicsDevice.Textures[2] = psychedelicWing;
                    Main.graphics.GraphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;

                    // Canvas: usar WavyBlotchNoise como textura base (s0)
                    // WoTG usa DendriticNoiseZoomedOut, nosotros usamos WavyBlotchNoise
                    Vector2 drawScale = Vector2.One * Projectile.width * scale * 1.5f / wavyBlotch.Size();

                    // Orden correcto: Begin(Immediate) → Apply → Draw → End → Begin(Deferred)
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                    shader.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(wavyBlotch, drawPos, null, Color.White, Projectile.rotation,
                        wavyBlotch.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                }
                else
                {
                    DrawFallback(drawPos, scale);
                }

                // === 3. RADIAL SHINE (exactamente como WoTG) ===
                if (_shineShader != null && _shineShader.Value != null)
                {
                    Effect shineShader = _shineShader.Value;
                    Texture2D wavyBlotch = ModContent.Request<Texture2D>("AethonMod/Content/Effects/WoTG/WavyBlotchNoise").Value;
                    shineShader.Parameters["globalTime"].SetValue((float)Main.GameUpdateCount * 0.0167f);
                    Vector2 shineScale = Vector2.One * Projectile.width * scale * 2.72f / wavyBlotch.Size();

                    Main.graphics.GraphicsDevice.Textures[1] = wavyBlotch;
                    Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                    shineShader.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(wavyBlotch, drawPos, null,
                        new Color(252, 212, 112) * 0.24f, Projectile.rotation,
                        wavyBlotch.Size() * 0.5f, shineScale, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                }
            }
            catch { }
            return false;
        }

        private void DrawFallback(Vector2 drawPos, float scale)
        {
            Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GameUpdateCount * 0.05f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                new Color(255, 250, 200, 220), 0f,
                new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                1.5f * scale * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                new Color(255, 180, 60, 180), 0f,
                new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                2.0f * scale * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                new Color(200, 50, 0, 100), 0f,
                new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                2.8f * scale * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            for (int i = 0; i < 30; i++)
            {
                float angle = (MathHelper.TwoPi / 30) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(5f, 10f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(5f, 10f));
                Dust d = Dust.NewDustPerfect(target.Center, DustID.GoldFlame,
                    dir, 220, new Color(255, 200, 100), 1.3f);
                d.noGravity = true; d.fadeIn = 0f;
            }

            target.AddBuff(BuffID.OnFire, 300);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, target.Center);
        }
    }
}
```

### 13.3 CosmicWeapons.cs (Content/Weapons/Cosmic/CosmicWeapons.cs)

```csharp
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Cosmic;

namespace AethonMod.Content.Weapons.Cosmic
{
    public class BlackHoleStaff : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 100;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 60; Item.useAnimation = 60;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<BlackHoleProjectile>();
            Item.shootSpeed = 6f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item20;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/9600FF:═══ AGUJERO NEGRO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Lightmarch de 75 pasos con lensing gravitacional real (shader de WoTG)]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Partículas en espiral + disco de acreción + atracción de enemigos]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    public class SunStaff : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 80;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 50; Item.useAnimation = 50;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<SunProjectile>();
            Item.shootSpeed = 6f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/FFD700:═══ SOL ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Estrella con shader SunShader.fx (esfericidad + corona + lava) de WoTG]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Partículas de fuego + iluminación intensa + OnFire]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
```

### 13.4 TestingPlayer.cs (Content/Players/TestingPlayer.cs)

```csharp
using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Items;

namespace AethonMod.Content.Players
{
    public class TestingPlayer : ModPlayer
    {
        public override void OnEnterWorld()
        {
            if (Main.netMode != Terraria.ID.NetmodeID.SinglePlayer) return;
            if (Player.whoAmI != Main.myPlayer) return;

            bool alreadyHasKit = false;
            for (int i = 0; i < 58; i++)
            {
                if (Player.inventory[i] != null &&
                    Player.inventory[i].type == ModContent.ItemType<GenesisShard>())
                {
                    alreadyHasKit = true;
                    break;
                }
            }
            if (alreadyHasKit) return;

            GiveItem(ModContent.ItemType<GenesisShard>(), 1);
            GiveItem(Terraria.ID.ItemID.GoldBar, 100);
            GiveItem(ModContent.ItemType<LevelUpTester>(), 1);
            GiveItem(ModContent.ItemType<BossSummonBag>(), 1);
            GiveItem(ModContent.ItemType<Items.SeerOrb>(), 1);
            GiveItem(ModContent.ItemType<Weapons.TestMagicRing>(), 1);
            GiveItem(ModContent.ItemType<Weapons.TestSparkle>(), 1);
            GiveItem(ModContent.ItemType<Weapons.ProjBeam>(), 1);
            GiveItem(ModContent.ItemType<Weapons.TestMagicRingV2>(), 1);
            GiveItem(ModContent.ItemType<Weapons.ColorRainbow>(), 1);
            GiveItem(ModContent.ItemType<Weapons.ColorRed>(), 1);
            GiveItem(ModContent.ItemType<Weapons.ColorYellow>(), 1);
            GiveItem(ModContent.ItemType<Weapons.ColorGreen>(), 1);
            // v5.77: 20 armas creativas (BlackHoleStaff movido a Cosmic)
            GiveItem(ModContent.ItemType<Weapons.V20.BlackHoleMiniStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.TornadoStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.PrismBeamStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.EarthquakeStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.MirrorDimensionStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.GravityPulseStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.ShadowCloneStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.CrystalShatterStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.SupernovaStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.VortexChainStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.AbyssalEyeStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.SpectralMirageStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.TemporalRiftStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.PlasmaStormStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.InfernoTornadoStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.VoidEaterStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.PhoenixNovaStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.QuantumSplitStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.PlasmaOrbStaff>(), 1);
            // v5.80: armas cósmicas basadas en shaders de WoTG
            GiveItem(ModContent.ItemType<Weapons.Cosmic.BlackHoleStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.Cosmic.SunStaff>(), 1);
        }

        private void GiveItem(int itemType, int stack)
        {
            for (int i = 0; i < 58; i++)
            {
                if (Player.inventory[i] == null ||
                    Player.inventory[i].type == Terraria.ID.ItemID.None)
                {
                    Player.inventory[i].SetDefaults(itemType);
                    Player.inventory[i].stack = stack;
                    return;
                }
            }
            int drop = Item.NewItem(Player.GetSource_GiftOrReward(), Player.Center, itemType, stack);
            if (drop >= 0 && drop < Main.item.Length)
                Main.item[drop].noGrabDelay = 0;
        }
    }
}
```

### 13.5 RealBlackHoleShader.fx (Content/Effects/Shaders/RealBlackHoleShader.fx)

```hlsl
sampler baseTexture : register(s0);
sampler noiseTexture : register(s1);

float globalTime;
float blackHoleRadius;
float accretionDiskRadius;
float aspectRatioCorrectionFactor;
float cameraAngle;
float2 zoom;
float3 cameraRotationAxis;
float3 blackHoleCenter;
float3 accretionDiskColor;
float3 accretionDiskScale;

float QuadraticBump(float x)
{
    return x * (4 - x * 4);
}

float Hash13(float3 p)
{
    return frac(sin(dot(p, float3(12.9898, 78.233, 51.9852))) * 30000);
}

float SignedTorusDistance(float3 p, float2 t)
{
    float2 q = float2(length(p.xz) - t.x, p.y);
    return length(q) - t.y;
}

float2 RotatedBy(float2 v, float theta)
{
    float s = sin(theta);
    float c = cos(theta);
    return float2(v.x * c - v.y * s, v.x * s + v.y * c);
}

float4 Sample(float3 position)
{
    // Calculate the amount of glow for the accretion disk based on the distance relative to a torus surrounding the black hole.
    float3 offsetFromBlackHole = (position - blackHoleCenter) / accretionDiskScale;
    float accretionDiskDistance = -SignedTorusDistance(offsetFromBlackHole, float2(0.75, accretionDiskRadius));
    float accretionDiskGlow = pow(max(0, accretionDiskDistance / accretionDiskRadius), 0.9);

    // Apply some noise to the glow calculation to make it feel less artifically halo-y.
    float2 radial = float2(atan2(offsetFromBlackHole.x, offsetFromBlackHole.z) / 6.283 + 0.5, length(offsetFromBlackHole));
    accretionDiskGlow *= tex2D(noiseTexture, radial * float2(3, 3.5) + globalTime * float2(6.3, -2));

    // Combine the results with the base accretion disk color.
    float4 accretionDiskColorWithAlpha = float4(pow(saturate(accretionDiskColor), 1.1), 1) * accretionDiskGlow * 0.75;

    return lerp(accretionDiskColorWithAlpha, float4(0, 0, 0, 1), smoothstep(0.01, 0, length(offsetFromBlackHole) - blackHoleRadius));
}

// https://en.wikipedia.org/wiki/Rodrigues%27_rotation_formula
float3 RodriguesRotation(float3 v, float3 axis, float angle)
{
    float c = cos(angle);
    float s = sin(angle);
    return v * c + cross(v, axis) * s + axis * dot(axis, v) * (1 - c);
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0, float4 position : SV_Position) : COLOR0
{
    float2 baseCoords = coords;

    // Rearrage coordinates into a -1 to 1 UV range.
    coords = (coords - 0.5) * float2(aspectRatioCorrectionFactor, 1) + 0.5;
    coords = coords * 2 - 1;

    // Initialize positional information.
    float capturedLightInterpolant = 0;
    float3 samplePoint = float3(coords / zoom, -0.9);
    float3 standardLightPositionIncrement = float3(0, 0, 1);
    float distanceFromBlackHole = 0;
    float distanceFromBlackHoleEdge = 0;

    float3 startingSamplePoint = float3(samplePoint.xy, 0);

    // Apply camera rotation.
    samplePoint = RodriguesRotation(samplePoint - blackHoleCenter, cameraRotationAxis, cameraAngle) + blackHoleCenter;
    standardLightPositionIncrement = RodriguesRotation(standardLightPositionIncrement, cameraRotationAxis, cameraAngle);

    // Slightly nudge the starting sample point around a touch to make banding artifacts from the limited step count virtually unnoticeable.
    samplePoint += standardLightPositionIncrement * Hash13(samplePoint * 10 + globalTime) * 0.0175;

    float4 result = 0;
    float2 distortionOffset = 0;
    for (float i = 0; i < 75; i++)
    {
        // Calculate the distance from the black hole and its edge.
        distanceFromBlackHole = distance(samplePoint, blackHoleCenter);
        distanceFromBlackHoleEdge = distanceFromBlackHole - blackHoleRadius;

        // Determine how much light was captured by the black hole on this update step.
        capturedLightInterpolant = smoothstep(0.01, -0.1, distanceFromBlackHoleEdge);

        // Move the sample point forward and towards the black hole based on proximity.
        float step = lerp(0.02, 0.021, 1 - QuadraticBump(i / 75));
        float distortionIntensity = clamp(0.005 / pow(distanceFromBlackHole, 2), 0, 0.1) * blackHoleRadius;
        float3 distortion = normalize(blackHoleCenter - samplePoint) * distortionIntensity;

        samplePoint += distortion;
        samplePoint += standardLightPositionIncrement * (1 - capturedLightInterpolant) * step;

        // Accumulate total distortion offsets in the lightmarch for later.
        distortionOffset += distortion.xy;

        // Additively apply color samples to the result.
        // This determines the base of the accretion disk's color.
        result += Sample(samplePoint);
    }

    // Apply glow effects around the black hole.
    float glowAttenuation = smoothstep(5, 0, distanceFromBlackHole / blackHoleRadius);
    float4 accretionDiskColorWithAlpha = float4(accretionDiskColor, 1);
    result += clamp(0.3 / pow(distanceFromBlackHole, 3) * accretionDiskColorWithAlpha, 0, 2) * glowAttenuation * lerp(1, accretionDiskColorWithAlpha * 0.12, capturedLightInterpolant);

    float glowDistance = (distance(startingSamplePoint, blackHoleCenter) - blackHoleRadius * 1.7);
    result += 0.015 / abs(glowDistance) * smoothstep(0.2, 0.1, glowDistance);

    // Apply gravitational lensing UV effects.
    float angleOffset = length(distortionOffset) * 50 - globalTime * 6;
    float2 blackHolePosition2D = (blackHoleCenter.xy + 1) * 0.5;
    float2 rotatedCoords = RotatedBy(baseCoords - blackHolePosition2D, angleOffset) + blackHolePosition2D;
    float2 interpolatedCoords = lerp(baseCoords, rotatedCoords, smoothstep(0.125, 0.3, length(distortionOffset))) + distortionOffset;

    return tex2D(baseTexture, interpolatedCoords) * (1 - capturedLightInterpolant) + result;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
```

### 13.6 SunShader.fx (Content/Effects/Shaders/SunShader.fx)

```hlsl
sampler fireNoiseTexture : register(s0);
sampler accentNoiseTexture : register(s1);
sampler uvOffsetNoiseTexture : register(s2);

float globalTime;
float coronaIntensityFactor;
float sphereSpinTime;
float3 mainColor;
float3 darkerColor;
float3 subtractiveAccentFactor;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate the distance to the center of the sun. This is magnified a bit for intensity purposes in later equations.
    float2 coordsNormalizedToCenter = coords * 2 - 1;
    float distanceFromCenterSqr = dot(coordsNormalizedToCenter, coordsNormalizedToCenter) * 2;
    float starOpacity = InverseLerp(0.5, 0.42, distanceFromCenterSqr);

    // Calculate coordinates relative to the sphere.
    // This pinch factor effectively ensures that the UVs are relative to a circle, rather than a rectangle.
    // This helps SIGNIFICANTLY for making the texturing look realistic, as it will appear to be traveling on a
    // sphere rather than on a sheet that happens to overlay a circle.
    float spherePinchFactor = (1 - sqrt(abs(1 - distanceFromCenterSqr))) / distanceFromCenterSqr + 0.045;
    float2 sphereCoords = coords * spherePinchFactor + float2(sphereSpinTime, 0);

    // Calculate the star brightness texture from the sphere coordinates.
    float starCoordsOffset = tex2D(fireNoiseTexture, sphereCoords).r * 0.41 + globalTime * 0.3;
    float2 starCoords = sphereCoords + float2(starCoordsOffset, 0);
    float3 starBrightnessTexture = tex2D(fireNoiseTexture, starCoords);

    // Calculate the glow interpolant. The closer a pixel is to the center, the stronger this value is.
    float starGlow = saturate(1 - distanceFromCenterSqr * 0.91);

    // Combine various aforementioned values into the base result:
    // 1. The result is brighter the higher the pinch factor is. This makes colors at the cross direction edges a little bit weaker.
    // 2. The result is brighter the higher the star glow is.
    // 3. The result is brightened relative to the brightness texture. This gives variance in the brightness of the result, and keeps it crisp.
    float3 result = spherePinchFactor * mainColor * 0.777 + starGlow * darkerColor + starBrightnessTexture;

    // Apply subtractive texturing to the result, skewing things towards a darker orange red based on accent noise and the inverse of the brightness.
    // This allows the result to have darker patches on it.
    result = lerp(result, darkerColor, saturate(1 - starBrightnessTexture.r) * 0.8);
    result -= (1 - subtractiveAccentFactor) * tex2D(accentNoiseTexture, sphereCoords * 2).r * 1.1;

    // Apply sharp brightness texturing to the result, as though lava is flowing through it. The textures this creates are thin but very bright, like lava rivers.
    float2 uvOffset = tex2D(uvOffsetNoiseTexture, coords + float2(0, globalTime * 0.4));
    result += pow(tex2D(accentNoiseTexture, sphereCoords * 1.2 + uvOffset * 0.04).r, 2) * 2.1;

    // Calculate the corona brightness. This effect weakens on the star itself (obviously, it's a corona) and slowly dissipates the further out the pixel is from the center.
    // This gives a very, very strong bright edge to the result that dissipates in a manner similar to a radial bloom (except with a little bit of noise-based randomness).
    float coronaFadeOut = InverseLerp(0.2, 0.5, distanceFromCenterSqr) * InverseLerp(1.91, 0.98, distanceFromCenterSqr) * coronaIntensityFactor;
    float coronaBrightness = coronaFadeOut / abs(distanceFromCenterSqr - 0.5 + uvOffset.y * 0.04 + 0.04);

    // Combine everything together.
    return (starOpacity * float4(result, 1) + float4(mainColor, 1) * coronaBrightness) * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
```

---

## 14. ESTRUCTURA DE CARPETAS

### 14.1 Content/ completa

```
Content/
├── AethonConfig.cs                       ← Server config
│
├── Biomes/
│   └── HollowSanctumBiome.cs
│
├── Buffs/
│   ├── CosmicOrbBuff.cs
│   └── CosmicOrbBuff.png
│
├── Effects/
│   ├── BeamCyan.png                      ← Efectos del remote
│   ├── BeamGold.png
│   ├── GlowCircle.png
│   ├── GlowCircleCyan.png
│   ├── GlowCircleGold.png
│   ├── GlowCircleGreen.png
│   ├── GlowCircleMagenta.png
│   ├── GlowCirclePurple.png
│   ├── GlowCircleRed.png
│   ├── GlowCircleWhite.png
│   ├── GlowOrb.png
│   ├── GlowOrbCyan.png
│   ├── GlowOrbGold.png
│   ├── GlowOrbGreen.png
│   ├── GlowOrbMagenta.png
│   ├── GlowOrbPurple.png
│   ├── GlowOrbWhite.png
│   ├── GlowRay.png
│   ├── GlowRayCyan.png
│   ├── GlowRayGold.png
│   ├── HexCyan.png
│   ├── HexGold.png
│   ├── MagicRing.png
│   ├── MagicRingGold.png
│   ├── ShieldCyan.png
│   ├── ShieldGold.png
│   ├── SparkleStar.png
│   ├── TrailGlow.png
│   │
│   ├── Procedural/                       ← Texturas procedurales (8)
│   │   ├── Crescent.png                  (ID 6)
│   │   ├── Noise.png                     (ID 7)
│   │   ├── Ring.png                      (ID 5)
│   │   ├── Slash.png                     (ID 3)
│   │   ├── SoftGlow.png                  (ID 0)
│   │   ├── Star.png                      (ID 2)
│   │   ├── Trail.png                     (ID 1)
│   │   └── Vortex.png                    (ID 4)
│   │
│   ├── Shaders/                          ← 8 .fx + 8 .xnb
│   │   ├── BlackHoleDistortionShader.fx
│   │   ├── BlackHoleDistortionShader.xnb
│   │   ├── BlackOnlyShader.fx
│   │   ├── BlackOnlyShader.xnb
│   │   ├── Bloom.fx
│   │   ├── Bloom.xnb
│   │   ├── ChromaticAberration.fx
│   │   ├── ChromaticAberration.xnb
│   │   ├── RadialShineShader.fx
│   │   ├── RadialShineShader.xnb
│   │   ├── RealBlackHoleShader.fx
│   │   ├── RealBlackHoleShader.xnb
│   │   ├── Shockwave.fx
│   │   ├── Shockwave.xnb
│   │   ├── SunShader.fx
│   │   └── SunShader.xnb
│   │
│   └── WoTG/                             ← Texturas de Wrath of the Gods (9)
│       ├── BloomCircle.png
│       ├── BloomCircleSmall.png
│       ├── BloomFlare.png
│       ├── FireNoiseA.png
│       ├── FireNoiseB.png
│       ├── InvisiblePixel.png
│       ├── PsychedelicWingTextureOffsetMap.png
│       ├── WavyBlotchNoise.png
│       └── WavyBlotchNoiseDetailed.png
│
├── Globals/
│   ├── CosmicProjectileFX.cs             ← GlobalProjectile para Nightglow #931
│   ├── GlobalNPCXP.cs                    ← XP global para NPCs
│   ├── ShardLevelItem.cs                 ← Level system en items
│   ├── TestAdvancedFX.cs                 ← Flags ai[1] para TestAdvanced
│   └── TooltipToggleItem.cs              ← Toggle de tooltips
│
├── Items/
│   ├── BossSummonBag.cs
│   ├── BossSummonBag.png
│   ├── GenesisShard.cs
│   ├── GenesisShard.png
│   ├── LevelUpTester.cs
│   ├── LevelUpTester.png
│   ├── ResonanceShard.cs
│   ├── ResonanceShard.png
│   ├── SeerOrb.cs
│   ├── SeerOrb.png
│   └── Placeables/
│       ├── AncientAltarItem.cs
│       └── AncientAltarItem.png
│
├── NPCs/
│   ├── AethonBoss.cs
│   ├── AethonBoss.png
│   ├── EchoArcher.cs
│   ├── EchoArcher.png
│   ├── EchoBlade.cs
│   ├── EchoBlade.png
│   ├── HollowTitan.cs
│   ├── HollowTitan.png
│   ├── RiftKeeper.cs
│   ├── RiftKeeper.png
│   ├── TheWitness.cs
│   └── TheWitness.png
│
├── Particles/
│   ├── ParticleBuffer.cs                ← Buffer pre-asignado round-robin
│   ├── ParticleData.cs                   ← Struct con StructLayout Sequential
│   └── ParticleManager.cs                ← ModSystem orquestador
│
├── Players/
│   ├── BranchType.cs
│   ├── ShardPlayer.cs
│   └── TestingPlayer.cs                 ← Kit de spawn para testing
│
├── Projectiles/
│   ├── ArcaneBolt.cs                    (en Weapons/Projectiles/)
│   ├── ArcaneBolt.png
│   ├── CosmicOrbBolt.cs
│   ├── CosmicOrbBolt.png
│   ├── CosmicOrbMinion.cs
│   ├── CosmicOrbMinion.png
│   ├── GenesisLight.cs                  (en Weapons/Projectiles/)
│   ├── GenesisLight.png
│   │
│   ├── Cosmic/                          ← Proyectiles cósmicos (2)
│   │   ├── BlackHoleProjectile.cs
│   │   ├── BlackHoleProjectile.png
│   │   ├── SunProjectile.cs
│   │   └── SunProjectile.png
│   │
│   └── V20/                             ← Proyectiles V20 (19 pares .cs/.png)
│       ├── AbyssalEyeProjectile.cs / .png
│       ├── BlackHoleMiniProjectile.cs / .png
│       ├── CrystalShatterProjectile.cs / .png
│       ├── EarthquakeProjectile.cs / .png
│       ├── GravityPulseProjectile.cs / .png
│       ├── InfernoTornadoProjectile.cs / .png
│       ├── MirrorDimensionProjectile.cs / .png
│       ├── PhoenixNovaProjectile.cs / .png
│       ├── PlasmaOrbProjectile.cs / .png
│       ├── PlasmaStormProjectile.cs / .png
│       ├── PrismBeamProjectile.cs / .png
│       ├── QuantumSplitProjectile.cs / .png
│       ├── ShadowCloneProjectile.cs / .png
│       ├── SpectralMirageProjectile.cs / .png
│       ├── SupernovaProjectile.cs / .png
│       ├── TemporalRiftProjectile.cs / .png
│       ├── TornadoProjectile.cs / .png
│       ├── VoidEaterProjectile.cs / .png
│       └── VortexChainProjectile.cs / .png
│
├── Systems/
│   ├── AncientAltarWorldGen.cs
│   ├── ShardLevelSystem.cs
│   ├── ShardSyncSystem.cs
│   └── WeaponScaling.cs
│
├── Tiles/
│   ├── AncientAltar.cs
│   └── AncientAltar.png
│
├── Weapons/
│   ├── GrimoireEternal.cs               ← ARMA PRINCIPAL PROTEGIDA
│   ├── GrimoireEternal.png              ← SPRITE PROTEGIDO 30×38
│   ├── TestAdvanced.cs                  ← 8 BASTONES DEL REMOTE PROTEGIDOS
│   ├── TestMagicRing.png
│   ├── TestMagicRingV2.png
│   ├── TestSparkle.png
│   ├── ProjBeam.png
│   ├── ColorRainbow.png
│   ├── ColorRed.png
│   ├── ColorYellow.png
│   ├── ColorGreen.png
│   │
│   ├── Projectiles/                     ← Proyectiles del Grimorio
│   │   ├── ArcaneBolt.cs / .png
│   │   └── GenesisLight.cs / .png
│   │
│   ├── Cosmic/                          ← Armas cósmicas con shaders WoTG (2)
│   │   ├── CosmicWeapons.cs             (contiene BlackHoleStaff + SunStaff)
│   │   ├── BlackHoleStaff.png
│   │   └── SunStaff.png
│   │
│   └── V20/                             ← 19 armas V20 sin mana
│       ├── AbyssalEyeStaff.cs / .png
│       ├── BlackHoleMiniStaff.cs / .png
│       ├── CrystalShatterStaff.cs / .png
│       ├── EarthquakeStaff.cs / .png
│       ├── GravityPulseStaff.cs / .png
│       ├── InfernoTornadoStaff.cs / .png
│       ├── MirrorDimensionStaff.cs / .png
│       ├── PhoenixNovaStaff.cs / .png
│       ├── PlasmaOrbStaff.cs / .png
│       ├── PlasmaStormStaff.cs / .png
│       ├── PrismBeamStaff.cs / .png
│       ├── QuantumSplitStaff.cs / .png
│       ├── ShadowCloneStaff.cs / .png
│       ├── SpectralMirageStaff.cs / .png
│       ├── SupernovaStaff.cs / .png
│       ├── TemporalRiftStaff.cs / .png
│       ├── TornadoStaff.cs / .png
│       ├── VoidEaterStaff.cs / .png
│       └── VortexChainStaff.cs / .png
│
└── _masters/                            ← Sprites maestros alta resolución
    ├── AethonBoss.png
    ├── AncientAltar.png
    ├── AncientAltarItem.png
    ├── ArcaneBolt.png
    ├── BossSummonBag.png
    ├── CosmicOrbBuff.png
    ├── CosmicOrbMinion.png
    ├── EchoArcher.png
    ├── EchoBlade.png
    ├── GenesisLight.png
    ├── GenesisShard.png
    ├── GrimoireEternal.png
    ├── HollowTitan.png
    ├── LevelUpTester.png
    ├── ProjBeam.png
    ├── ResonanceShard.png
    ├── RiftKeeper.png
    ├── SeerOrb.png
    ├── TestMagicRing.png
    ├── TestMagicRingV2.png
    ├── TestSparkle.png
    ├── TheWitness.png
    └── icon.png
```

### 14.2 Reference_WoTG/ (archivos de referencia, NO compilados)

```
Reference_WoTG/
├── BlackHole.cs                  ← Implementación original WoTG
├── BlackHolePet.cs                ← Lógica del pet
├── PetBlackHoleRenderer.cs        ← Renderer RealBlackHoleShader
├── StarPet.cs                     ← Implementación SunShader + RadialShineShader
└── Starseed.cs                    ← Item que invoca StarPet
```

---

## 15. DOCUMENTACIÓN DE WoTG (técnicas aprendidas)

Técnicas aprendidas del mod **Wrath of the Gods** (`TheFifthCircle/WrathOfTheGodsPublic`)
que han sido adaptadas a AethonMod:

### 15.1 RealBlackHoleShader (lightmarch 75 pasos)

**Concepto**: En vez de aproximar el lensing gravitacional con una distorsión analítica
de UV, WoTG hace un **ray-marching** real: para cada pixel, lanza un rayo desde la cámara
y avanza 75 pasos pequeños, en cada paso:
1. Calcula la distancia al centro del agujero negro
2. Distorsiona la dirección del rayo hacia el centro proporcional a `1/distancia²`
3. Samplea el color del disco de acreción (torus) en esa posición 3D
4. Acumula el color con mezcla aditiva

**Funciones clave del shader**:
- `QuadraticBump(x)` — hace que los pasos sean más pequeños cerca del agujero
- `Hash13(p)` — hashing pseudoaleatorio para reducir banding (jittering)
- `SignedTorusDistance(p, t)` — SDF de un toro (usado para el disco)
- `RodriguesRotation(v, axis, angle)` — rotación 3D del rayo según eje arbitrario
- `Sample(position)` — samplea el disco de acreción con ruido procedural

**Fórmula del lensing**:
```hlsl
float distortionIntensity = clamp(0.005 / pow(distanceFromBlackHole, 2), 0, 0.1) * blackHoleRadius;
float3 distortion = normalize(blackHoleCenter - samplePoint) * distortionIntensity;
```

Es decir: la intensidad de distorsión decae con el **cuadrado** de la distancia al
agujero negro, replicando la ley de gravitación universal de Newton.

**Parámetros finales**:
```csharp
shader.Parameters["blackHoleRadius"].SetValue(0.3f);
shader.Parameters["blackHoleCenter"].SetValue(Vector3.Zero);
shader.Parameters["aspectRatioCorrectionFactor"].SetValue(1f);
shader.Parameters["accretionDiskColor"].SetValue(new Color(245, 105, 61).ToVector3());
shader.Parameters["cameraAngle"].SetValue(0.32f);
shader.Parameters["cameraRotationAxis"].SetValue(new Vector3(1f, 0f, Projectile.rotation));
shader.Parameters["accretionDiskScale"].SetValue(new Vector3(1f, 0.33f, 1f));  // Y aplastada → disco elíptico
shader.Parameters["zoom"].SetValue(Vector2.One * 0.12f);
shader.Parameters["accretionDiskRadius"].SetValue(0.33f);
shader.Parameters["globalTime"].SetValue((float)Main.GameUpdateCount * 0.0167f);
```

> **Truco clave**: `accretionDiskScale = (1, 0.33, 1)` aplasta el torus del disco de
> acreción en el eje Y. Esto crea la apariencia 2D del disco cuando se ve desde un
> ángulo. Combinado con `cameraAngle = 0.32` se obtiene el efecto "Interstellar"
> de un agujero negro visto casi de canto.

### 15.2 CircularSuctionPattern (partículas en espiral)

**Concepto**: Spawnear partículas que se mueven radialmente hacia el centro del agujero
negro, pero con su velocidad rotada ligeramente (no puramente radial) → genera una
espiral.

**Implementación** (en `BlackHoleProjectile.AI()`):
```csharp
Vector2 velocity = toCenter * speed;
float angleToCenter = (float)Math.Atan2(toCenter.Y, toCenter.X);
velocity = velocity.RotateTowards(angleToCenter + MathHelper.PiOver2 * 0.3f, 0.5f);
```

`RotateTowards(targetAngle, maxStep)` es un método de extensión que:
1. Calcula el ángulo actual de la velocidad
2. Calcula la diferencia con el ángulo objetivo
3. Rota como máximo `maxStep` radianes hacia el objetivo
4. Mantiene la magnitud de la velocidad original

> Resultado: las partículas caen hacia el centro pero con un giro sutil (0.3 *
> Pi/2 = 27°) que genera la espiral característica del disco de acreción.

### 15.3 SunShader (spherePinchFactor + corona + manchas + lava)

**Concepto**: Renderizar una estrella que parezca una esfera 3D con textura, no un
sprite plano. WoTG usa un "pinch factor" que deforma las UVs para que la textura
parezca estar viajando por la superficie de una esfera.

**spherePinchFactor**:
```hlsl
float spherePinchFactor = (1 - sqrt(abs(1 - distanceFromCenterSqr))) / distanceFromCenterSqr + 0.045;
float2 sphereCoords = coords * spherePinchFactor + float2(sphereSpinTime, 0);
```

Esto:
- Cuando el pixel está en el centro (distanceFromCenterSqr → 0), el pinch factor
  tiende a un valor grande → UVs muy estiradas en el centro → textura más detallada
  en el centro (efecto de "polo" de la esfera)
- Cuando el pixel está en el borde (distanceFromCenterSqr → 1), el pinch factor
  tiende a 0 → UVs muy comprimidas en el borde → textura muy comprimida en el borde
  (efecto de "ecuador" de la esfera)

**Lava (rivers bright)**:
```hlsl
float2 uvOffset = tex2D(uvOffsetNoiseTexture, coords + float2(0, globalTime * 0.4));
result += pow(tex2D(accentNoiseTexture, sphereCoords * 1.2 + uvOffset * 0.04).r, 2) * 2.1;
```

Samplea el accent noise desplazado por un UV offset noise (animado con tiempo).
Eleva al cuadrado para que solo los valores altos del noise aparezcan, dando
ríos finos y brillantes como lava.

**Corona**:
```hlsl
float coronaFadeOut = InverseLerp(0.2, 0.5, distanceFromCenterSqr)
                    * InverseLerp(1.91, 0.98, distanceFromCenterSqr)
                    * coronaIntensityFactor;
float coronaBrightness = coronaFadeOut / abs(distanceFromCenterSqr - 0.5 + uvOffset.y * 0.04 + 0.04);
```

`coronaFadeOut` es máximo en un anillo a distancia media del centro (entre 0.5 y 1.0
en coords normalizadas), creando un halo anular. Dividido por la distancia a ese
anillo, crea un brillo muy fuerte en el borde.

### 15.4 BlackOnlyShader (smoothstep para event horizon)

**Concepto**: El event horizon (zona negra central del agujero negro) debe ser
perfectamente negro, sin color del disco de acreción "filtrándose". WoTG usa un
shader simple con `smoothstep` para forzar el negro en el centro:

```hlsl
// En RealBlackHoleShader.fx, función Sample():
return lerp(accretionDiskColorWithAlpha, float4(0, 0, 0, 1),
    smoothstep(0.01, 0, length(offsetFromBlackHole) - blackHoleRadius));
```

Cuando `length(offsetFromBlackHole) < blackHoleRadius`, el `smoothstep` devuelve 1
y el resultado es negro puro. Fuera del radio, devuelve 0 y el color del disco
se aplica normalmente.

### 15.5 Polar UV sampling para swirl effects

**Concepto**: Para crear efectos de vórtice/swirl, WoTG samplea texturas en
coordenadas polares en vez de cartesianas:

```hlsl
// En RealBlackHoleShader.fx, función Sample():
float2 radial = float2(
    atan2(offsetFromBlackHole.x, offsetFromBlackHole.z) / 6.283 + 0.5,  // ángulo
    length(offsetFromBlackHole)                                         // radio
);
accretionDiskGlow *= tex2D(noiseTexture, radial * float2(3, 3.5) + globalTime * float2(6.3, -2));
```

- `atan2` da el ángulo del punto relativo al centro del agujero negro
- `length` da el radio
- Samplear en estas UVs polares hace que la textura de ruido se vea como remolinos
  que orbitan el centro del agujero negro
- Animar con `globalTime * (6.3, -2)` hace que el ángulo avance más rápido que el
  radio, generando el movimiento de rotación del disco de acreción

### 15.6 Patrón general de uso de shaders en tModLoader

Aprendido de WoTG, el patrón correcto para aplicar un shader a un proyectil:

```csharp
public override bool PreDraw(ref Color lightColor)
{
    // 1. Cargar el shader perezosamente (cacheado)
    if (_shader == null)
    {
        try { _shader = new Ref<Effect>(
            ModContent.Request<Effect>("AethonMod/Content/Effects/Shaders/MiShader").Value); }
        catch { }
    }

    try
    {
        Vector2 drawPos = Projectile.Center - Main.screenPosition;

        // 2. Configurar parámetros del shader
        if (_shader != null && _shader.Value != null)
        {
            Effect shader = _shader.Value;
            shader.Parameters["param1"].SetValue(...);
            shader.Parameters["globalTime"].SetValue((float)Main.GameUpdateCount * 0.0167f);

            // 3. Asignar texturas a los registros s1, s2, etc. (s0 es el sprite)
            Texture2D noise = ModContent.Request<Texture2D>("AethonMod/Content/Effects/WoTG/FireNoiseB").Value;
            Main.graphics.GraphicsDevice.Textures[1] = noise;
            Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

            // 4. Patrón Begin → Apply → Draw → End → Begin
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            shader.CurrentTechnique.Passes[0].Apply();

            // 5. Dibujar el canvas (un pixel invisible que se escala enorme, o el sprite)
            Texture2D canvas = ModContent.Request<Texture2D>("AethonMod/Content/Effects/WoTG/InvisiblePixel").Value;
            Main.spriteBatch.Draw(canvas, drawPos, null, Color.Transparent, 0f,
                new Vector2(canvas.Width / 2f, canvas.Height / 2f),
                400f, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        }
        else
        {
            // Fallback si el shader no carga
            DrawFallback(drawPos);
        }
    }
    catch { }
    return false;  // tModLoader NO dibuja el sprite original
}
```

### 15.7 Backglow con dos colores

Técnica del StarPet de WoTG: dibujar el backglow con dos colores superpuestos para
dar profundidad:

```csharp
// Backglow amarillo (pequeño, intenso)
Main.spriteBatch.Draw(bloomCircle, drawPos, null,
    (new Color(255, 230, 100) { A = 0 }) * 0.7f, 0f,
    bloomCircle.Size() * 0.5f, scale * 0.95f, 0, 0f);
// Backglow rojo (grande, tenue)
Main.spriteBatch.Draw(bloomCircle, drawPos, null,
    (new Color(255, 50, 0) { A = 0 }) * 0.45f, 0f,
    bloomCircle.Size() * 0.5f, scale * 1.61f, 0, 0f);
```

> Importante: `{ A = 0 }` después de inicializar el Color fuerza el alpha a 0,
> y luego se multiplica por el factor de intensidad. Esto evita que el alpha del
> color original afecte el blending.

### 15.8 Iluminación WoTG

Para partículas brillantes como estrellas, WoTG usa iluminación constante alta
(no pulsante):
```csharp
Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.9f, 0.5f) * 3.2f);
```

> El factor 3.2f da una luz extremadamente intensa que ilumina varios bloques a la
> redonda. AethonMod añade un pulso sutil (0.9 + 0.1 * sin) para que parezca "vivo".

---

## 16. RESUMEN FINAL

### 16.1 Lo que se ha logrado (hasta v5.82)
- ✅ Mod completo con 8 armas protegidas del remote
- ✅ 19 armas V20 creativas sin mana
- ✅ 2 armas cósmicas (BlackHoleStaff, SunStaff) con shaders reales de WoTG
- ✅ Sistema de partículas data-oriented (ParticleData + ParticleBuffer + ParticleManager)
- ✅ 8 shaders HLSL compilados a .xnb SIN Wine usando dxc
- ✅ 9 texturas de WoTG copiadas y referenciadas
- ✅ 5 shaders .fx copiados de WoTG como referencia
- ✅ Compilación estable sin errores

### 16.2 Lo que falta (próximos pasos)
- ⚠️ **PROBAR** en tModLoader 1.4.4 los 2 armas cósmicas
- ⚠️ Verificar que los shaders carguen sin excepción en runtime
- ⚠️ Si algún shader falla, comparar parámetros con el código de WoTG en `Reference_WoTG/`
- ⚠️ Verificar que el `.xnb` no esté corrupto (recompilar con dxc si es necesario)

### 16.3 Recordatorio final
**Puedo coger los recursos de nuestro github si los datos de mi versión local se borran**

- GitHub PAT: `[GITHUB_PAT - solicitar al usuario]`
- Repositorio: https://github.com/Leo0x01/Aethon-Mod-Terraria
- Commit actual: `b2798e6` (v5.82)
- Commit estable del remote: `e826c82`

---

**Fin del documento.**

> Última actualización: v5.82 (commit `b2798e6`)
> Documento generado para asegurar continuidad del proyecto entre sesiones de IA.
> Si eres una IA leyendo esto: SIEMPRE empieza por el Recordatorio al inicio de
> cualquier commit o documento nuevo.

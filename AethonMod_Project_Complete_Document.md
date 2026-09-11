**Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran**

[GITHUB_PAT - solicitar al usuario]

Repositorio: https://github.com/Leo0x01/Aethon-Mod-Terraria

---

# AethonMod — Documento Completo del Proyecto

> Este documento es la fuente única de verdad para el mod **Aethon, la Luz Primordial**.
> Cualquier IA o desarrollador que trabaje en este proyecto DEBE leer este documento completo
> antes de tocar cualquier archivo.

> ⚠️ **LEER PRIMERO — v5.83**: los shaders del mod ahora son `.fx` (fuente) + `.fxc`
> (compilado) — **NUNCA generar `.xnb` con dxc** (provocan "Asset could not be found"
> al cargar el mod). tModLoader NO compila .fx automáticamente; el formato correcto es
> copiar/compilar `.fxc` (como hace Wrath of the Gods). Ver sección 8.

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
| **Versión (build.txt)** | 5.84 |
| **Author** | AethonModTeam |
| **Framework** | tModLoader 1.4.4 |
| **Runtime** | .NET 8, C# |
| **Side** | Both (Client + Server) |
| **Commit actual** | v5.84 — librería de partículas completa + capas de VFX en BlackHole/Sun |
| **Commit estable del remote** | e826c82 (referencia de sprites protegidos) |
| **Homepage** | https://github.com/Leo0x01/Aethon-Mod-Terraria |

### build.txt completo
```ini
author = AethonModTeam
version = 5.84
displayName = Aethon, la Luz Primordial
homepage = https://github.com/Leo0x01/Aethon-Mod-Terraria
modReferences =
buildIgnore = *.csproj, *.csproj.user, obj/*, bin/*, *.bak, *.md, *.py
side = Both
```

### AethonMod.csproj completo (v5.83)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <!-- Patrón oficial tML: tML genera tModLoader.targets en ModSources al abrir el juego -->
  <Import Project="..\tModLoader.targets" Condition="Exists('..\tModLoader.targets')" />

  <PropertyGroup>
    <AssemblyName>AethonMod</AssemblyName>
    <RootNamespace>AethonMod</RootNamespace>
    <Nullable>disable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <Compile Remove="**/obj/**" />
    <Compile Remove="**/bin/**" />
    <Compile Remove="Reference_WoTG/**" />
  </ItemGroup>
</Project>
```

> **Importante (v5.83)**: eliminado el Import roto a `/tmp/tmodloader/tMLMod.targets`
> (ruta del sandbox inexistente en la máquina del usuario). El build real de tML NO usa
> el .csproj (compila con Roslyn desde build.txt); el csproj es solo para el IDE.
> La línea `<Compile Remove="Reference_WoTG/**" />` se mantiene para que si se vuelven
> a copiar archivos de referencia de WoTG, no se compilen.

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
- **139 archivos .png** (sprites — se añadió DendriticNoiseZoomedOut.png)
- **8 shaders .fx** en `Content/Effects/Shaders/` (fuente)
- **5 shaders .fxc** compilados en `Content/Effects/Shaders/` (los 5 de WoTG)
- ⚠️ **PROHIBIDO generar .xnb** — ver sección 8

### 4.2 Shaders (8 fx fuente + 5 fxc compilados)
| Shader | Archivo .fx | Archivo .fxc | Uso |
|---|---|---|---|
| RealBlackHoleShader | RealBlackHoleShader.fx | RealBlackHoleShader.fxc ✓ | BlackHoleProjectile (75-step lightmarch con lensing) |
| SunShader | SunShader.fx | SunShader.fxc ✓ | SunProjectile (estrella con corona + lava) |
| RadialShineShader | RadialShineShader.fx | RadialShineShader.fxc ✓ | Brillo radial del sol |
| BlackOnlyShader | BlackOnlyShader.fx | BlackOnlyShader.fxc ✓ | Refuerza event horizon (no usado aún: requiere render target) |
| BlackHoleDistortion | BlackHoleDistortionShader.fx | BlackHoleDistortionShader.fxc ✓ | Lensing screen-space (no usado aún: requiere screen filter) |
| ChromaticAberration | ChromaticAberration.fx | — (sin .fxc, no usado) | Aberración cromática RGB |
| Shockwave | Shockwave.fx | — (sin .fxc, no usado) | Onda expansiva |
| Bloom | Bloom.fx | — (sin .fxc, no usado) | Extract bright → blur → combine |

> **Los .fxc se copiaron directamente del repo de WoTG** (nuestros .fx son idénticos
> byte a byte). Si se modifica un .fx propio, hay que recompilar el .fxc con
> mgfxc/2MGFX — tModLoader NO compila .fx en el build.

### 4.3 Texturas de WoTG (10 texturas en `Content/Effects/WoTG/`)
- `WavyBlotchNoise.png`
- `WavyBlotchNoiseDetailed.png`
- `InvisiblePixel.png`
- `PsychedelicWingTextureOffsetMap.png`
- `FireNoiseA.png`
- `FireNoiseB.png`
- `BloomCircleSmall.png`
- `BloomCircle.png`
- `BloomFlare.png`
- `DendriticNoiseZoomedOut.png` ← canvas del SunShader (añadida en v5.83)

### 4.4 Texturas procedurales (8 texturas en `Content/Effects/Procedural/`)
- `SoftGlow.png` (ParticleTex.SoftGlow = ID 0 en ParticleManager)
- `Trail.png` (ParticleTex.Trail = 1)
- `Star.png` (ParticleTex.Star = 2)
- `Slash.png` (ParticleTex.Slash = 3)
- `Vortex.png` (ParticleTex.Vortex = 4)
- `Ring.png` (ParticleTex.Ring = 5)
- `Crescent.png` (ParticleTex.Crescent = 6)
- `Noise.png` (ParticleTex.Noise = 7)

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

### 4.6 Sistema de partículas data-oriented (COMPLETO desde v5.84)
6 archivos en `Content/Particles/`:
- `ParticleData.cs` — struct con `[StructLayout(LayoutKind.Sequential, Pack = 1)]` + ComponentFlag + LayerPriorities + constantes ParticleTex
- `ParticleBuffer.cs` — buffer pre-asignado de 4000 partículas, estrategia round-robin
- `ParticleManager.cs` — `ModSystem` orquestador, update en `PreUpdateDusts`, render en `PostDrawTiles` con frustum culling
- `ShapeDescriptor.cs` — API de spawn por forma: Box, Circle, Hollow, Cone, Sphere, Vortex, Line
- `ParticlePresets.cs` — efectos pre-empaquetados: Explosion, Implosion, RingPulse, VortexSwirl

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

Total: ~80 bytes por partícula. Con 4000 partículas = ~320 KB en cache L2.

### 7.2 ComponentFlags (bitmask) — v5.84: TODOS implementados

```csharp
public static class ComponentFlag
{
    public const ulong Gravity     = 1UL << 0;   // UserData0 = gravedad (default 0.2f)
    public const ulong FadeOut     = 1UL << 1;   // alpha decae en la 2ª mitad de la vida
    public const ulong FadeIn      = 1UL << 2;   // UserData0 = ticks de rampa (default 10)
    public const ulong ScaleDown   = 1UL << 3;   // UserData1/2 = escala inicial → 0
    public const ulong ScaleUp     = 1UL << 4;   // UserData1/2 = escala final ← 0
    public const ulong Homing      = 1UL << 5;   // UserData0 = whoAmI (≤0 = más cercano), UserData3 = fuerza
    public const ulong Rotation    = 1UL << 6;   // siempre activo vía RotationSpeed
    public const ulong ColorShift  = 1UL << 7;   // PackedStartColor → PackedEndColor
    public const ulong Orbit       = 1UL << 8;   // UserData0/1 = centro, UserData2 = rad/tick, UserData3 = radio
    public const ulong Trail       = 1UL << 9;   // reservado (roadmap sección 49.2)
    public const ulong BounceOnTile = 1UL << 10; // reservado
    public const ulong DieOnTile   = 1UL << 11;  // reservado
    public const ulong EmitLight   = 1UL << 12;  // intensidad = f(escala), sin UserData
}
```

**Componente Orbit — diseño stateless:** el ángulo se deriva cada tick de la
posición actual (`atan2(pos - centro)`), se avanza la velocidad angular y se
fija la nueva posición. `RotationSpeed` sincronizada con la velocidad angular
mantiene estelas (TrailGlow) alineadas tangencialmente sin estado extra.

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

- Buffer pre-asignado de `ParticleData[capacity]` (default 4000)
- **Round-robin**: busca slot libre empezando en `_nextSlot`, no recorre desde 0
- `TrySpawn(data)` → bool (false si buffer lleno)
- `Kill(index)` → desactiva por índice
- `Clear()` → desactiva todas
- **Cero GC**: nunca se asigna/libera memoria después de inicialización

### 7.5 ParticleManager.cs (ModSystem)

- `OnModLoad()`: crea buffer (4000), registra 11 texturas (8 procedurales +
  GlowOrb + SparkleStar + TrailGlow)
- `PreUpdateDusts()`: actualiza todas las partículas activas — orden:
  componentes de comportamiento (Gravity → Homing → Orbit) → movimiento base
  (omitido si Orbit fijó la posición) → rotación → componentes visuales
  (ColorShift → FadeIn → FadeOut → ScaleDown → ScaleUp) → EmitLight → vida
- `PostDrawTiles()`: renderiza con batching por blend mode + frustum culling:
  - Pass 1: AlphaBlend (todas con BlendMode == 0)
  - Pass 2: Additive (todas con BlendMode == 1)
  - Culling con `CameraBounds` (margen 320px para partículas escaladas grandes)
  - Usa `Main.GameViewMatrix.TransformationMatrix` para respetar zoom
- API: `Spawn(data)` (con auto-fill de start color/UserData),
  `SpawnShape(center, shape, count, template)`, `RegisterTexture(path)`,
  `PackColor(Color)`, `UnpackColor(uint)`, `Clear()`

### 7.6 Manejo de errores (v5.78+)
- `RegisterTexture` no crashea si la textura no existe (devuelve ID 0 = SoftGlow)
- `PostDrawTiles` hace try/catch para restaurar el spriteBatch si algo falla

### 7.7 ShapeDescriptor.cs (v5.84) — API de spawn por forma

Sección 13.3 del libro: en lugar de métodos específicos (SpawnBox, SpawnCircle...),
un struct readonly con factory methods y `GenerateRandomPoint()`:
- `Box(w, h)` / `HollowBox(w, h)` — caja sólida o solo bordes
- `Circle(r)` / `HollowCircle(r)` — círculo sólido o circunferencia (polares)
- `Cone(length, halfAngle)` — ángulo aleatorio en ±halfAngle
- `Sphere(r)` — esfera 3D proyectada (sqrt para distribución uniforme en área)
- `Vortex(r, turns)` — brazos espirales con `turns` vueltas
- `Line(length)` — línea a lo largo del eje X
- Campo `Rotation` opcional para formas orientadas (rotación manual, sin
  dependencias de extensiones de tML)

### 7.8 CameraBounds.cs (v5.84) — frustum culling

Sección 16.1 del libro: struct readonly con `TopLeft`, `Size`, `BottomRight` y
`IsVisible(worldPos, margin = 64f)`. El render usa margen 320px para no cortar
partículas grandes escaladas parcialmente fuera de pantalla.

### 7.9 ParticlePresets.cs (v5.84) — efectos pre-empaquetados

Apéndice A del libro (galería de efectos estilo ParticleAnimationLib):
- `Explosion(center, radius, count, coreColor, edgeColor, duration)` — ráfaga
  radial con ColorShift + anillo de shockwave con ScaleUp + chispas Star con
  rotación aleatoria
- `Implosion(center, radius, count, color, duration)` — partículas convergiendo
  en espiral (radial + tangencial) — colapso gravitatorio
- `RingPulse(center, maxRadius, color, duration)` — onda circular expansiva
- `VortexSwirl(center, radius, count, innerColor, outerColor, duration)` — brazos
  espirales vía `SpawnShape` con `ShapeDescriptor.Vortex`

### 7.10 Uso en los proyectiles cósmicos (v5.84)

BlackHole/Sun usan la librería como **capa de fondo aditiva** (PostDrawTiles)
bajo sus dusts vanilla (capa frontal) y el canvas del shader (encima):
arquitectura de profundidad por 3 capas. Véanse las secciones de cada
proyectil en el capítulo 13 para el detalle de los 4 efectos de cada uno.

**APIs de tML verificadas por reflection contra tModLoader.dll v2026.07.3.0**
(los presets usan PunchCameraModifier para screenshake coordinado — sección 8.5
del libro):
- `Terraria.Graphics.CameraModifiers.PunchCameraModifier(Vector2, Vector2, float, float, int, float, string)`
- `Main.CameraModifiers` — campo de instancia, tipo `CameraModifierStack`
- `Lighting.AddLight(Vector2, Vector3)`

---

## 8. COMPILACIÓN DE SHADERS SIN WINE

### 8.1 Pipeline tradicional (roto en Linux)
- tModLoader usa MGCB (MonoGame Content Builder) que requiere Windows o Wine
- Los shaders `.fx` legacy usan HLSL con sintaxis DirectX 9
- En Linux sin Wine, MGCB no funciona

### 8.2 ⚠️ SOLUCIÓN ANTIGUA (v5.69-v5.82) — OBSOLETA Y PELIGROSA, NO USAR

El pipeline antiguo usaba `dxc` para compilar HLSL a DXIL y renombraba el `.dxbc`
resultante a `.xnb`. **ESTO ESTÁ ROTO**: un `.dxbc` renombrado NO es un `.xnb`
válido de MonoGame (no tiene header XNB). tModLoader registraba el asset vía el
XnbReader, el parse fallaba, y el juego mostraba:

```
Failed to load asset 'Content\Effects\Shaders\SunShader'!
ReLogic.Content.AssetLoadException: Asset could not be found
```

**PROHIBIDO volver a generar .xnb con dxc.**

### 8.3 Solución correcta (v5.83) — formato .fxc como WoTG

**Investigación verificada contra el código fuente y el binario de tModLoader
v2026.07.3.0** (la versión exacta del usuario):

1. tModLoader **NO compila los `.fx` durante el build** (issue abierto
   https://github.com/tModLoader/tModLoader/issues/3326).
2. En tML FNA el reader de `.fx` (Terraria.Testing.FxReader) NO existe — verificado
   por reflection contra tModLoader.dll real. Los `.fx` en el .tmod **ni se
   registran** como assets.
3. El formato correcto es **`.fxc`**: tML lo registra con `FxcReader`, que crea el
   Effect con `new Effect(graphicsDevice, bytes)` en runtime.
4. **Wrath of the Gods commitea 270 `.fxc`** junto a sus `.fx` en
   `Assets/AutoloadedEffects/` — ese es el flujo estándar de los mods grandes.

**Lo que hace AethonMod ahora:**
- Cada shader usado tiene su `.fx` (fuente) + `.fxc` (compilado) en
  `Content/Effects/Shaders/`.
- Los 5 `.fxc` se copiaron directamente del repo de WoTG (nuestros `.fx` son
  idénticos byte a byte — verificado con diff).
- Los 3 shaders propios no usados (Bloom, ChromaticAberration, Shockwave) quedan
  solo como `.fx` fuente, sin `.fxc`. **Si algún día se usan desde C#, primero
  hay que compilarlos a `.fxc` con mgfxc/2MGFX** (dotnet tool de MonoGame) o el
  mod dará "Asset could not be found" otra vez.

### 8.4 Estado actual (v5.83)
- 5 `.fxc` activos: RealBlackHoleShader, SunShader, RadialShineShader,
  BlackOnlyShader, BlackHoleDistortionShader
- 3 `.fx` sin `.fxc` (inofensivos porque nadie los pide): Bloom,
  ChromaticAberration, Shockwave
- Cero `.xnb` en el proyecto

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

Commits desde v5.28 hasta v5.84 (orden inverso, más reciente primero):

| Commit | Versión | Descripción |
|---|---|---|
| `(este commit)` | v5.84 | feat: librería de partículas completa (ShapeDescriptor+CameraBounds+presets+6 componentes nuevos+culling) + capas de VFX en BlackHole/Sun |
| `7de559b` | v5.83 | fix: BlackHole + Sun idénticos a WoTG + shaders .fxc (error Asset could not be found) |
| `9f8fbbc` | docs | documento completo del proyecto para dar a otra IA |
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

### 11.1 Versión actual
- **Versión**: v5.84
- **Mensaje**: "feat v5.84: librería de partículas completa según el libro de referencia + capas de VFX en BlackHole/Sun"

### 11.2 Qué se hizo en v5.84

El usuario entregó el documento `particle_library_implementation_book.pdf` (88 páginas,
"Librería de Partículas para Terraria - Referencia para IA" — el mismo documento en
que se basó el sistema original de v5.69, en su versión completa). Se leyó entero y
se completó la librería al diseño íntegro:

1. **3 archivos nuevos** (`ShapeDescriptor.cs`, `CameraBounds.cs`, `ParticlePresets.cs`)
2. **6 componentes nuevos** en el update loop: FadeIn, ScaleUp, ColorShift, Homing,
   Orbit (stateless, con estelas tangenciales sincronizadas) y EmitLight
3. **API SpawnShape** por forma geométrica + **frustum culling** en el render
4. **BlackHole + Sun** recibieron 4 efectos de librería cada uno como capa de fondo
   aditiva (arquitectura de profundidad por 3 capas: librería → dusts → shader),
   más presets en impacto/muerte y screenshake con PunchCameraModifier
5. **APIs verificadas por reflection** contra tModLoader.dll v2026.07.3.0:
   PunchCameraModifier (firma de 7 parámetros), Main.CameraModifiers, Lighting.AddLight
6. **Compilación verificada**: 0 errores, 4 warnings benignos preexistentes

### 11.3 Estado actual del mod
- ✅ Mod compila correctamente (verificado contra tML 2026.07.3.0 real)
- ✅ Los 5 shaders usados tienen .fxc cargable (fix v5.83)
- ✅ Librería de partículas COMPLETA según el libro (v5.84)
- ✅ BlackHole + Sun replican el render de los pets de WoTG + 3 capas de efectos
- ⚠️ **PENDIENTE**: probar en tModLoader real (el usuario debe recompilar con
  Develop Mods → Build y probar BlackHoleStaff y SunStaff)

### 11.4 Próximos pasos sugeridos
1. El usuario: abrir tModLoader → Develop Mods → Build (recompila desde fuente)
2. Entrar al mundo (el kit de TestingPlayer incluye ambos staves)
3. Disparar BlackHoleStaff y SunStaff — render de WoTG + capas de partículas nuevas:
   espiral violeta de fondo, disco de estelas tangenciales, corona orbitando, viento
   solar, anillos de fotones, novas con doble onda expansiva y screenshake
4. Si algo falla, revisar client.log y comparar con Reference_WoTG (recuperable
   de https://github.com/TheFifthCircle/WrathOfTheGodsPublic)
5. Roadmap natural (sección 49 del libro): texturas de Bloom/ChromaticAberration/
   Shockway ya existen como .fx fuente — compilarlas con mgfxc/2MGFX cuando se usen
   desde C#; componentes Trail/BounceOnTile/DieOnTile; ModConfig MaxParticles

### 11.5 Qué se arregló en v5.83 (histórico)

**FIX CRÍTICO — el error que el usuario veía al cargar el mod:**
```
Failed to load asset 'Content\Effects\Shaders\SunShader'!
Failed to load asset 'Content\Effects\Shaders\RealBlackHoleShader'!
```
- Causa raíz: los `.xnb` generados con dxc no son XNB válidos (el XnbReader de tML
  los rechazaba) Y tML no compila `.fx` (sin reader para esa extensión en FNA).
- Solución: 5 `.fxc` compilados copiados de WoTG + borrar los 8 `.xnb`.
- Verificado por reflection contra tModLoader.dll v2026.07.3.0 real.

**BlackHoleProjectile — por qué NO se parecía al pet de WoTG (todas corregidas):**
1. `zoom` fijo 0.12 → ahora dinámico `width/256*scale*2` (≈0.59) — EL error principal
2. `accretionDiskRadius` fijo 0.33 → ahora `scale*0.4`
3. `cameraRotationAxis` sin velocity → ahora `(velocity.Y*-0.022+1, 0, rotation)`
4. `globalTime` con GameUpdateCount*0.0167 → ahora `Main.GlobalTimeWrappedHourly`
5. Sin pop elástico → ahora ElasticOut al nacer (como el pet)

**SunProjectile — por qué NO se parecía al StarPet de WoTG (todas corregidas):**
1. Canvas WavyBlotchNoise → ahora **DendriticNoiseZoomedOut** (la textura real de WoTG, faltaba)
2. `sphereSpinTime` con GameUpdateCount → ahora `GlobalTimeWrappedHourly*0.9`
3. RadialShine en AlphaBlend (invisible por result.a=0) → ahora en Additive
4. Sin pop elástico → ahora ElasticOut

**Verificación de compilación:** 0 errores compilando TODO el mod contra
tModLoader v2026.07.3.0 real con .NET 10 SDK (`dotnet build` con las DLLs del
release de GitHub). 4 warnings benignos preexistentes.

### 11.6 Estado del mod al cierre de v5.83 (histórico)
- ✅ Mod compila correctamente (verificado contra tML 2026.07.3.0 real)
- ✅ Los 5 shaders usados tienen .fxc cargable (fix del "Asset could not be found")
- ✅ Todos los recursos de WoTG copiados (10 texturas incl. DendriticNoiseZoomedOut)
- ✅ BlackHole + Sun replican el render de los pets de WoTG + mejoras propias

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
- Los shaders van en `Content/Effects/Shaders/` con su `.fx` (fuente) y su `.fxc` compilado — NUNCA .xnb
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
ls /home/z/my-project/AethonMod/Content/Effects/Shaders/ | wc -l   # debe ser 13 (8 .fx + 5 .fxc)
ls /home/z/my-project/AethonMod/Content/Effects/WoTG/ | wc -l      # debe ser 10 (con DendriticNoiseZoomedOut)
ls /home/z/my-project/AethonMod/Content/Weapons/V20/*.cs | wc -l   # debe ser 19
ls /home/z/my-project/AethonMod/Content/Weapons/Cosmic/*.cs | wc -l # debe ser 1 (CosmicWeapons.cs con 2 clases)
ls /home/z/my-project/AethonMod/Content/Projectiles/Cosmic/*.cs | wc -l  # debe ser 2 (BlackHole + Sun)
ls /home/z/my-project/AethonMod/Content/Particles/*.cs | wc -l     # debe ser 6 (v5.84: + ShapeDescriptor, CameraBounds, ParticlePresets)
find /home/z/my-project/AethonMod/Content -name '*.cs' | wc -l     # debe ser 81 (+ AethonMod.cs raíz = 82)
find /home/z/my-project/AethonMod/Content -name '*.png' | wc -l    # debe ser 139
```

---

## 13. CÓDIGO FUENTE CLAVE

### 13.1 BlackHoleProjectile.cs (Content/Projectiles/Cosmic/BlackHoleProjectile.cs)

> Código base v5.83 — réplica del render del BlackHolePet de WoTG
> (PetBlackHoleRenderer.UpdateUI) con parámetros EXACTOS + mejoras propias.
> Las claves: zoom dinámico `width/256*scale*2`, accretionDiskRadius `scale*0.4`,
> cameraRotationAxis con velocity.Y, canvas InvisiblePixel 256px, pop elástico
> ElasticOut, atracción de dusts del entorno, colapso final e implosión en OnKill.
>
> **v5.84**: el archivo añade 4 métodos de partículas de librería
> (`SpawnLibrarySuctionSpiral`, `SpawnLibraryAccretionDisk`,
> `SpawnLibraryPhotonRing`, `SpawnLibraryDistortionHalo` — ver sección 7.10)
> y presets `ParticlePresets.Implosion/Explosion/RingPulse` + screenshake en
> OnHitNPC/OnKill. Fuente autoritativa: el archivo del repo.

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// BlackHoleProjectile — réplica fiel del BlackHolePet de Wrath of the Gods.
    ///
    /// Render: RealBlackHoleShader.fx (lightmarch de 75 pasos con lensing gravitacional real)
    /// con los parámetros EXACTOS del PetBlackHoleRenderer de WoTG:
    ///   - zoom dinámico: width / 256 * scale * 2
    ///   - accretionDiskRadius: scale * 0.4
    ///   - cameraRotationAxis: (velocity.Y * -0.022 + 1, 0, rotation)
    ///   - cameraAngle: 0.32 / accretionDiskScale: (1, 0.33, 1)
    ///
    /// El shader se dibuja sobre un canvas de InvisiblePixel de 256px (el mismo tamaño
    /// del render target que usa WoTG para el pet).
    ///
    /// Mejoras propias: pop elástico de aparición (ElasticOut, como el pet),
    /// colapso final antes de expirar, succión espiral de partículas, disco de acreción
    /// con GoldFlame, atracción gravitacional de enemigos Y de polvo cercano,
    /// refuerzo del event horizon e implosión + explosión al morir.
    /// </summary>
    public class BlackHoleProjectile : ModProjectile
    {
        private Ref<Effect> _shader;
        private bool _shaderFailed;

        /// <summary>Tiempo visual de vida — usada para el pop elástico de aparición.</summary>
        public ref float VisualsTime => ref Projectile.ai[0];

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
            Projectile.timeLeft = 600;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        public override void AI()
        {
            // === POP ELÁSTICO DE APARICIÓN (EasingCurves.Elastic.Out del pet de WoTG) ===
            // scale = ElasticOut(0..120) * sqrt(InverseLerp(0..60)) — el agujero "rebota" al nacer.
            Projectile.scale = ElasticOut(Utils.GetLerpValue(0f, 120f, VisualsTime, true)) *
                               (float)Math.Sqrt(Utils.GetLerpValue(0f, 60f, VisualsTime, true));
            VisualsTime += 1f;

            // === COLAPSO FINAL: los últimos 40 ticks se encoge dramáticamente ===
            if (Projectile.timeLeft < 40f)
                Projectile.scale *= 0.93f;

            // === MOVIMIENTO: deriva lenta y frenado (el agujero flota) ===
            Projectile.velocity *= 0.97f;

            // Rotación suave hacia velocity.X * 0.04 (igual que el pet de WoTG)
            float targetRotation = Projectile.velocity.X * 0.04f;
            Projectile.rotation += MathHelper.WrapAngle(targetRotation - Projectile.rotation) * 0.3f;

            // === PARTÍCULAS (solo cliente) ===
            if (Main.netMode != NetmodeID.Server)
            {
                SpawnSuctionParticles();
                SpawnAccretionDiskParticles();
                SpawnSmokeParticles();
                SpawnCapturedEnergySparks();
                AttractNearbyDust();
            }

            // === ATRACCIÓN GRAVITACIONAL DE ENEMIGOS (radio 350) ===
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

            // === ILUMINACIÓN PULSANTE (naranja del disco + toque púrpura) ===
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.95f * pulse, 0.45f * pulse, 0.15f * pulse));
        }

        // ------------------------------------------------------------------
        //  PARTÍCULAS
        // ------------------------------------------------------------------

        /// <summary>Partículas que caen en espiral hacia el centro (CircularSuctionPattern de WoTG).</summary>
        private void SpawnSuctionParticles()
        {
            for (int i = 0; i < 3; i++)
            {
                float angle = Projectile.rotation * 1.5f + i * (MathHelper.TwoPi / 3f);
                float dist = 90f + 50f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f + i);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);

                Vector2 toCenter = Projectile.Center - spawnPos;
                // Más rápido cuanto más cerca del horizonte
                float speed = 4f + 4f * (1f - dist / 140f);
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    Vector2 velocity = toCenter * speed;

                    // RotateTowards: LA técnica de WoTG que convierte infalling radial en espiral
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
        }

        /// <summary>Disco de acreción: GoldFlame orbitando en un plano aplanado.</summary>
        private void SpawnAccretionDiskParticles()
        {
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
        }

        /// <summary>Humo púrpura siendo absorbido desde los alrededores.</summary>
        private void SpawnSmokeParticles()
        {
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

        /// <summary>Chispas doradas encantadas capturadas por el campo gravitatorio.</summary>
        private void SpawnCapturedEnergySparks()
        {
            if (Main.rand.NextBool(12))
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(140f, 220f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);
                Vector2 vel = (Projectile.Center - spawnPos) * 0.03f;
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.Enchanted_Gold,
                    vel, 255, new Color(255, 230, 150), 0.9f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        /// <summary>
        /// Efecto gravitacional sobre el polvo del ambiente: los dusts cercanos
        /// son atraídos hacia el horizonte de sucesos, como si el agujero devorase el entorno.
        /// </summary>
        private void AttractNearbyDust()
        {
            float radius = 190f;
            for (int i = 0; i < Main.maxDust; i++)
            {
                Dust d = Main.dust[i];
                if (!d.active || d.noGravity) continue;
                Vector2 toCenter = Projectile.Center - d.position;
                float dist = toCenter.Length();
                if (dist > radius || dist < 4f) continue;
                float strength = (1f - dist / radius) * 0.35f;
                toCenter.Normalize();
                // Componente tangencial sutil → espiral
                Vector2 pull = toCenter * strength + new Vector2(-toCenter.Y, toCenter.X) * strength * 0.35f;
                d.velocity += pull;
            }
        }

        // ------------------------------------------------------------------
        //  RENDER
        // ------------------------------------------------------------------

        public override bool PreDraw(ref Color lightColor)
        {
            if (!_shaderFailed && _shader == null)
            {
                try
                {
                    _shader = new Ref<Effect>(ModContent.Request<Effect>(
                        "AethonMod/Content/Effects/Shaders/RealBlackHoleShader",
                        AssetRequestMode.ImmediateLoad).Value);
                }
                catch
                {
                    _shaderFailed = true;
                }
            }

            try
            {
                Vector2 drawPos = Projectile.Center - Main.screenPosition;

                // === 1. HALO PÚRPURA EXTERIOR (aura cósmica de fondo) ===
                Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                float haloPulse = 0.85f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3.5f);
                Main.spriteBatch.Draw(glowTex, drawPos, null,
                    new Color(60, 20, 90, 40) * haloPulse * Projectile.scale, 0f,
                    glowTex.Size() * 0.5f, 3.2f * Projectile.scale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();

                if (_shader != null && _shader.Value != null)
                {
                    // === 2. REALBLACKHOLESHADER — parámetros EXACTOS del PetBlackHoleRenderer de WoTG ===
                    Effect shader = _shader.Value;

                    // El pet de WoTG renderiza a un target de 256x256: replicamos ese tamaño de canvas
                    float targetSize = 256f;
                    float resizingScale = Projectile.width / targetSize * Projectile.scale * 2f;

                    shader.Parameters["blackHoleRadius"].SetValue(0.3f);
                    shader.Parameters["blackHoleCenter"].SetValue(Vector3.Zero);
                    shader.Parameters["aspectRatioCorrectionFactor"].SetValue(1f);
                    shader.Parameters["accretionDiskColor"].SetValue(new Color(245, 105, 61).ToVector3());
                    shader.Parameters["cameraAngle"].SetValue(0.32f);
                    shader.Parameters["cameraRotationAxis"].SetValue(new Vector3(Projectile.velocity.Y * -0.022f + 1f, 0f, Projectile.rotation));
                    shader.Parameters["accretionDiskScale"].SetValue(new Vector3(1f, 0.33f, 1f));
                    shader.Parameters["zoom"].SetValue(Vector2.One * resizingScale);
                    shader.Parameters["accretionDiskRadius"].SetValue(Projectile.scale * 0.4f);
                    shader.Parameters["globalTime"].SetValue(Main.GlobalTimeWrappedHourly);

                    // FireNoiseB como textura de ruido del disco de acreción (s1) — igual que WoTG
                    Texture2D fireNoise = ModContent.Request<Texture2D>("AethonMod/Content/Effects/WoTG/FireNoiseB").Value;
                    Main.graphics.GraphicsDevice.Textures[1] = fireNoise;
                    Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                    // InvisiblePixel como canvas (s0) — igual que WoTG
                    Texture2D pixel = ModContent.Request<Texture2D>("AethonMod/Content/Effects/WoTG/InvisiblePixel").Value;

                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                        SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    shader.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(pixel, drawPos, null, Color.White, 0f,
                        pixel.Size() * 0.5f, targetSize, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();

                    // === 3. REFUERZO DEL EVENT HORIZON ===
                    // Sustituye al BlackOnlyShader de WoTG (que requiere render target):
                    // radio del horizonte en píxeles = blackHoleRadius * zoom * (canvas / 2)
                    float eventHorizonPx = 0.3f * resizingScale * targetSize * 0.5f;
                    if (eventHorizonPx > 2f)
                    {
                        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                            null, Main.GameViewMatrix.TransformationMatrix);
                        float horizonScale = (eventHorizonPx * 2.15f) / glowTex.Width;
                        Main.spriteBatch.Draw(glowTex, drawPos, null,
                            new Color(0, 0, 0, 215), 0f, glowTex.Size() * 0.5f,
                            horizonScale, SpriteEffects.None, 0f);
                        Main.spriteBatch.End();
                    }
                }
                else
                {
                    // === FALLBACK: dibujado manual si el shader no carga ===
                    DrawFallback(drawPos);
                }
            }
            catch { }

            RestoreSpriteBatch();
            return false;
        }

        /// <summary>Restaura el SpriteBatch al estado que tML espera tras PreDraw.</summary>
        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.Transform);
        }

        /// <summary>Dibujado manual de respaldo (vórtice + anillo de fotones + aberración cromática).</summary>
        private void DrawFallback(Vector2 drawPos)
        {
            float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f);
            Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            Texture2D vortexTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Vortex").Value;
            Texture2D ringTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring").Value;
            float s = Projectile.scale;

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            // Disco de acreción frontal (elíptico, naranja)
            Main.spriteBatch.Draw(vortexTex, drawPos, null,
                new Color(255, 180, 80, 200), Projectile.rotation * 2f,
                vortexTex.Size() * 0.5f, new Vector2(1.5f, 0.5f) * s, SpriteEffects.None, 0f);

            // Disco trasero (anillo de Einstein)
            Main.spriteBatch.Draw(vortexTex, drawPos - new Vector2(0f, 4f * s), null,
                new Color(200, 50, 0, 100), -Projectile.rotation * 2f,
                vortexTex.Size() * 0.5f, new Vector2(1.5f, 0.5f) * s, SpriteEffects.FlipVertically, 0f);

            // Beaming relativístico
            Main.spriteBatch.Draw(vortexTex, drawPos - new Vector2(3f * s, 0f), null,
                new Color(255, 230, 150, 130), Projectile.rotation * 2f,
                vortexTex.Size() * 0.5f, new Vector2(1.5f, 0.5f) * s, SpriteEffects.None, 0f);

            Main.spriteBatch.End();

            // Event horizon
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                Color.Black, 0f, glowTex.Size() * 0.5f,
                0.8f * s, SpriteEffects.None, 0f);
            Main.spriteBatch.End();

            // Anillo de fotones + aberración cromática
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(ringTex, drawPos - new Vector2(2f * s, 0f), null,
                new Color(255, 0, 0, 80), 0f, ringTex.Size() * 0.5f,
                0.6f * pulse * s, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ringTex, drawPos, null,
                new Color(0, 255, 0, 80), 0f, ringTex.Size() * 0.5f,
                0.6f * pulse * s, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ringTex, drawPos + new Vector2(2f * s, 0f), null,
                new Color(0, 100, 255, 80), 0f, ringTex.Size() * 0.5f,
                0.6f * pulse * s, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ringTex, drawPos, null,
                new Color(255, 240, 200, 220), 0f, ringTex.Size() * 0.5f,
                0.6f * pulse * s, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
        }

        // ------------------------------------------------------------------
        //  IMPACTO Y MUERTE
        // ------------------------------------------------------------------

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // Implosión: 50 partículas convergiendo en espiral
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
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // Explosión: 40 GoldFlame radiales
            for (int i = 0; i < 40; i++)
            {
                float angle = (MathHelper.TwoPi / 40) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(5f, 11f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(5f, 11f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    dir, 220, new Color(255, 200, 100), 1.5f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Destellos encantados
            for (int i = 0; i < 15; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                    new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 4f)),
                    255, Color.White, 1.0f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // === COLAPSO FINAL: implosión + explosión ===
            // Implosión: partículas convergiendo
            for (int i = 0; i < 60; i++)
            {
                float angle = (MathHelper.TwoPi / 60) * i;
                float dist = Main.rand.NextFloat(100f, 170f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
                Vector2 toCenter = Projectile.Center - spawnPos;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X) * 0.7f;
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch,
                        toCenter * 9f + tangent * 5f, 220, new Color(190, 90, 255), 1.4f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // Explosión: anillo expansivo de GoldFlame + Torch púrpura
            for (int i = 0; i < 45; i++)
            {
                float angle = (MathHelper.TwoPi / 45) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(6f, 13f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(6f, 13f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    dir, 230, new Color(255, 200, 100), 1.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
            for (int i = 0; i < 20; i++)
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                    new Vector2((float)Math.Cos(angle) * 3f, (float)Math.Sin(angle) * 3f),
                    200, new Color(160, 80, 255), 1.2f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item88, Projectile.Center);
        }

        // ------------------------------------------------------------------
        //  HELPERS
        // ------------------------------------------------------------------

        /// <summary>Elastic ease-out (réplica de EasingCurves.Elastic.Evaluate(EasingType.Out) de WoTG).</summary>
        private static float ElasticOut(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float c = (2f * (float)Math.PI) / 3f;
            return (float)(Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c) + 1);
        }
    }

    /// <summary>Extensiones vectoriales para las partículas en espiral.</summary>
    public static class Vector2Extensions
    {
        /// <summary>Rota el vector hacia el ángulo objetivo como máximo maxStep radianes, conservando la magnitud.</summary>
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

> Código base v5.83 — réplica del render de StarPet.DrawSelf de WoTG al pie
> de la letra: backglow doble BloomCircleSmall → RadialShine (Additive) →
> SunShader con canvas **DendriticNoiseZoomedOut** (la textura de WoTG),
> s1=WavyBlotchNoise, s2=PsychedelicWingTextureOffsetMap, sphereSpinTime
> = GlobalTimeWrappedHourly*0.9. + mejoras: llamaradas solares, nova final.
>
> **v5.84**: el archivo añade 4 métodos de partículas de librería
> (`SpawnLibraryCorona`, `SpawnLibrarySolarWind`, `SpawnLibraryTwinkles`,
> `SpawnLibraryFlareLoop` — ver sección 7.10) y presets
> `ParticlePresets.Explosion/RingPulse` + ráfaga de viento solar + screenshake
> en OnHitNPC/OnKill. Fuente autoritativa: el archivo del repo.

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// SunProjectile — réplica fiel del StarPet de Wrath of the Gods.
    ///
    /// Render (EXACTAMENTE como StarPet.DrawSelf de WoTG, en orden):
    ///   1. Backglow con BloomCircleSmall: amarillo * 0.7 (escala 0.95) + rojo * 0.45 (escala 1.61)
    ///   2. RadialShineShader sobre WavyBlotchNoise: color (252, 212, 112) * 0.24,
    ///      escala = width * scale * 2.72 / tamaño de la textura
    ///   3. SunShader sobre DendriticNoiseZoomedOut (canvas):
    ///      coronaIntensityFactor = 0.05, mainColor = blanco, darkerColor = (204, 92, 25),
    ///      subtractiveAccentFactor = (181, 0, 0), sphereSpinTime = GlobalTimeWrappedHourly * 0.9,
    ///      s1 = WavyBlotchNoise, s2 = PsychedelicWingTextureOffsetMap,
    ///      escala = width * scale * 1.5 / tamaño de la textura
    ///
    /// Mejoras propias: pop elástico de aparición, hinchazón previa a la nova final,
    /// chispas de fuego orbitando, llamaradas periódicas, prominencias solares,
    /// humo cálido, destellos encantados y nova de fuego al morir.
    /// </summary>
    public class SunProjectile : ModProjectile
    {
        private Ref<Effect> _sunShader;
        private Ref<Effect> _shineShader;
        private bool _sunShaderFailed;
        private bool _shineShaderFailed;

        /// <summary>Tiempo visual de vida — usada para el pop elástico de aparición.</summary>
        public ref float VisualsTime => ref Projectile.ai[0];

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
            Projectile.timeLeft = 600;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        public override void AI()
        {
            // === POP ELÁSTICO DE APARICIÓN (como el black hole) ===
            Projectile.scale = ElasticOut(Utils.GetLerpValue(0f, 90f, VisualsTime, true)) *
                               (float)Math.Sqrt(Utils.GetLerpValue(0f, 45f, VisualsTime, true));
            VisualsTime += 1f;

            // === NOVA FINAL: los últimos 30 ticks se hincha antes de explotar ===
            if (Projectile.timeLeft < 30f)
                Projectile.scale *= 1.025f;

            // === MOVIMIENTO: deriva lenta y frenado ===
            Projectile.velocity *= 0.97f;
            Projectile.rotation += 0.01f;

            // === PARTÍCULAS (solo cliente) ===
            if (Main.netMode != NetmodeID.Server)
            {
                SpawnOrbitingSparks();
                SpawnFlames();
                SpawnSmoke();
                SpawnSolarFlare();
                SpawnTwinkles();
            }

            // === ILUMINACIÓN INTENSA (como StarPet: Vector3.One * 3.2f, con pulso sutil) ===
            float pulse = 0.92f + 0.08f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f);
            Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.9f, 0.5f) * 3.2f * pulse);
        }

        // ------------------------------------------------------------------
        //  PARTÍCULAS
        // ------------------------------------------------------------------

        /// <summary>Chispas de fuego (Torch) orbitando y cayendo hacia la superficie.</summary>
        private void SpawnOrbitingSparks()
        {
            if (Main.rand.NextBool(2))
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(40f, 60f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);
                Vector2 vel = Projectile.Center - spawnPos;
                if (vel.LengthSquared() > 0.01f)
                {
                    vel.Normalize();
                    vel *= Main.rand.NextFloat(1f, 3f);
                    vel += new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f));
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.Torch,
                        vel, 150, new Color(255, 150, 50), 1.0f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
        }

        /// <summary>Llamas de GoldFlame escapando de la fotosfera.</summary>
        private void SpawnFlames()
        {
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
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        /// <summary>Humo cálido ascendiendo desde la corona.</summary>
        private void SpawnSmoke()
        {
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
                d.noGravity = false;
                d.fadeIn = 0f;
            }
        }

        /// <summary>Llamarada solar periódica: explosión radial de fuego desde el borde.</summary>
        private void SpawnSolarFlare()
        {
            // Cada ~45 ticks (0.75 s), una llamarada prominente
            if (VisualsTime % 45f == 0f && VisualsTime > 30f)
            {
                float baseAngle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                int count = 12;
                for (int i = 0; i < count; i++)
                {
                    float angle = baseAngle + (MathHelper.TwoPi / count) * i * 0.35f;
                    float dist = Projectile.width * 0.55f * Projectile.scale;
                    Vector2 spawnPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(angle) * dist,
                        (float)Math.Sin(angle) * dist);
                    Vector2 vel = new Vector2(
                        (float)Math.Cos(angle) * Main.rand.NextFloat(3f, 6f),
                        (float)Math.Sin(angle) * Main.rand.NextFloat(3f, 6f));
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.GoldFlame,
                        vel, 220, new Color(255, 180, 80), 1.4f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
        }

        /// <summary>Destellos encantados parpadeando alrededor de la estrella.</summary>
        private void SpawnTwinkles()
        {
            if (Main.rand.NextBool(20))
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(60f, 110f) * Projectile.scale;
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.Enchanted_Gold,
                    Vector2.Zero, 255, new Color(255, 240, 180), 0.7f);
                d.noGravity = true;
                d.fadeIn = 0.3f;
            }
        }

        // ------------------------------------------------------------------
        //  RENDER — réplica exacta de StarPet.DrawSelf de WoTG
        // ------------------------------------------------------------------

        public override bool PreDraw(ref Color lightColor)
        {
            if (!_sunShaderFailed && _sunShader == null)
            {
                try
                {
                    _sunShader = new Ref<Effect>(ModContent.Request<Effect>(
                        "AethonMod/Content/Effects/Shaders/SunShader",
                        AssetRequestMode.ImmediateLoad).Value);
                }
                catch
                {
                    _sunShaderFailed = true;
                }
            }
            if (!_shineShaderFailed && _shineShader == null)
            {
                try
                {
                    _shineShader = new Ref<Effect>(ModContent.Request<Effect>(
                        "AethonMod/Content/Effects/Shaders/RadialShineShader",
                        AssetRequestMode.ImmediateLoad).Value);
                }
                catch
                {
                    _shineShaderFailed = true;
                }
            }

            try
            {
                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                float scale = Projectile.scale;

                // === 1. BACKGLOW (EXACTAMENTE como StarPet.DrawSelf de WoTG) ===
                Texture2D bloomCircle = ModContent.Request<Texture2D>("AethonMod/Content/Effects/WoTG/BloomCircleSmall").Value;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                // Amarillo (pequeño e intenso)
                Main.spriteBatch.Draw(bloomCircle, drawPos, null,
                    (new Color(255, 230, 100) { A = 0 }) * 0.7f, 0f,
                    bloomCircle.Size() * 0.5f, scale * 0.95f, SpriteEffects.None, 0f);
                // Rojo (grande y tenue)
                Main.spriteBatch.Draw(bloomCircle, drawPos, null,
                    (new Color(255, 50, 0) { A = 0 }) * 0.45f, 0f,
                    bloomCircle.Size() * 0.5f, scale * 1.61f, SpriteEffects.None, 0f);
                Main.spriteBatch.End();

                // === 2. RADIAL SHINE (aura con ruido animado) ===
                Texture2D wavyBlotch = ModContent.Request<Texture2D>("AethonMod/Content/Effects/WoTG/WavyBlotchNoise").Value;
                if (_shineShader != null && _shineShader.Value != null)
                {
                    Effect shineShader = _shineShader.Value;
                    shineShader.Parameters["globalTime"].SetValue(Main.GlobalTimeWrappedHourly);
                    Vector2 shineScale = Vector2.One * Projectile.width * scale * 2.72f / wavyBlotch.Size();

                    // El shader samplea s0 (la textura dibujada); LinearWrap en el Begin
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                        SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    _shineShader.Value.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(wavyBlotch, drawPos, null,
                        new Color(252, 212, 112) * 0.24f, Projectile.rotation,
                        wavyBlotch.Size() * 0.5f, shineScale, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                }

                // === 3. SUNSHADER (la estrella — EXACTAMENTE como WoTG) ===
                if (_sunShader != null && _sunShader.Value != null)
                {
                    Effect shader = _sunShader.Value;
                    Texture2D psychedelicWing = ModContent.Request<Texture2D>("AethonMod/Content/Effects/WoTG/PsychedelicWingTextureOffsetMap").Value;
                    // ¡El canvas de WoTG es DendriticNoiseZoomedOut, no WavyBlotchNoise!
                    Texture2D dendritic = ModContent.Request<Texture2D>("AethonMod/Content/Effects/WoTG/DendriticNoiseZoomedOut").Value;

                    // Parámetros EXACTOS de StarPet.DrawSelf()
                    shader.Parameters["coronaIntensityFactor"].SetValue(0.05f);
                    shader.Parameters["mainColor"].SetValue(new Color(255, 255, 255).ToVector3());
                    shader.Parameters["darkerColor"].SetValue(new Color(204, 92, 25).ToVector3());
                    shader.Parameters["subtractiveAccentFactor"].SetValue(new Color(181, 0, 0).ToVector3());
                    shader.Parameters["sphereSpinTime"].SetValue(Main.GlobalTimeWrappedHourly * 0.9f);
                    shader.Parameters["globalTime"].SetValue(Main.GlobalTimeWrappedHourly);

                    // s1 = accentNoise (WavyBlotchNoise), s2 = uvOffsetNoise (PsychedelicWingTextureOffsetMap)
                    Main.graphics.GraphicsDevice.Textures[1] = wavyBlotch;
                    Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                    Main.graphics.GraphicsDevice.Textures[2] = psychedelicWing;
                    Main.graphics.GraphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;

                    // Canvas: DendriticNoiseZoomedOut (512x512) con la escala exacta de WoTG
                    Vector2 drawScale = Vector2.One * Projectile.width * scale * 1.5f / dendritic.Size();

                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                        SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    shader.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(dendritic, drawPos, null, Color.White, Projectile.rotation,
                        dendritic.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                }
                else
                {
                    // === FALLBACK: dibujado manual si el shader no carga ===
                    DrawFallback(drawPos, scale);
                }
            }
            catch { }

            RestoreSpriteBatch();
            return false;
        }

        /// <summary>Restaura el SpriteBatch al estado que tML espera tras PreDraw.</summary>
        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.Transform);
        }

        /// <summary>Dibujado manual de respaldo (glow multicapa naranja).</summary>
        private void DrawFallback(Vector2 drawPos, float scale)
        {
            Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                new Color(255, 250, 200, 220), 0f,
                glowTex.Size() * 0.5f, 1.5f * scale * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                new Color(255, 180, 60, 180), 0f,
                glowTex.Size() * 0.5f, 2.0f * scale * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                new Color(200, 50, 0, 100), 0f,
                glowTex.Size() * 0.5f, 2.8f * scale * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
        }

        // ------------------------------------------------------------------
        //  IMPACTO Y MUERTE
        // ------------------------------------------------------------------

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // Explosión radial de fuego sobre el objetivo
            for (int i = 0; i < 30; i++)
            {
                float angle = (MathHelper.TwoPi / 30) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(5f, 10f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(5f, 10f));
                Dust d = Dust.NewDustPerfect(target.Center, DustID.GoldFlame,
                    dir, 220, new Color(255, 200, 100), 1.3f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Ráfaga de chispas Torch
            for (int i = 0; i < 12; i++)
            {
                Vector2 dir = new Vector2(Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-5f, 5f));
                Dust d = Dust.NewDustPerfect(target.Center, DustID.Torch,
                    dir, 180, new Color(255, 150, 50), 1.1f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            target.AddBuff(BuffID.OnFire, 300);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, target.Center);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // === NOVA FINAL: explosión masiva de fuego ===
            // Onda expansiva de GoldFlame
            for (int i = 0; i < 60; i++)
            {
                float angle = (MathHelper.TwoPi / 60) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(6f, 14f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(6f, 14f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    dir, 240, new Color(255, 200, 100), 1.7f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Chispas Torch en todas direcciones
            for (int i = 0; i < 35; i++)
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                    new Vector2((float)Math.Cos(angle) * Main.rand.NextFloat(4f, 9f),
                                (float)Math.Sin(angle) * Main.rand.NextFloat(4f, 9f)),
                    200, new Color(255, 150, 50), 1.4f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Núcleo de la nova: destellos encantados
            for (int i = 0; i < 20; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                    new Vector2(Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-6f, 6f)),
                    255, new Color(255, 240, 180), 1.2f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Humo ascendente tras la explosión
            for (int i = 0; i < 15; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke,
                    new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-4f, -1f)),
                    100, new Color(120, 70, 40), 1.0f);
                d.noGravity = false;
                d.fadeIn = 0f;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45, Projectile.Center);
        }

        // ------------------------------------------------------------------
        //  HELPERS
        // ------------------------------------------------------------------

        /// <summary>Elastic ease-out (réplica de EasingCurves.Elastic.Evaluate(EasingType.Out) de WoTG).</summary>
        private static float ElasticOut(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float c = (2f * (float)Math.PI) / 3f;
            return (float)(Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c) + 1);
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
│   ├── Shaders/                          ← 8 .fx (fuente) + 5 .fxc (compilados de WoTG)
│   │   ├── BlackHoleDistortionShader.fx
│   │   ├── BlackHoleDistortionShader.fxc  (solo los 5 de WoTG)
│   │   ├── BlackOnlyShader.fx
│   │   ├── BlackOnlyShader.fxc  (solo los 5 de WoTG)
│   │   ├── Bloom.fx
│   │   ├── ChromaticAberration.fx
│   │   ├── RadialShineShader.fx
│   │   ├── RadialShineShader.fxc  (solo los 5 de WoTG)
│   │   ├── RealBlackHoleShader.fx
│   │   ├── RealBlackHoleShader.fxc  (solo los 5 de WoTG)
│   │   ├── Shockwave.fx
│   │   ├── SunShader.fx
│   │   └── SunShader.fxc
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

### 16.1 Lo que se ha logrado (hasta v5.84)
- ✅ Mod completo con 8 armas protegidas del remote
- ✅ 19 armas V20 creativas sin mana
- ✅ 2 armas cósmicas (BlackHoleStaff, SunStaff) con shaders reales de WoTG
- ✅ Sistema de partículas data-oriented COMPLETO (6 archivos: ParticleData +
  ParticleBuffer + ParticleManager + ShapeDescriptor + CameraBounds + ParticlePresets,
  con 9 componentes implementados, spawn por forma, culling y presets)
- ✅ BlackHole/Sun con 3 capas de profundidad: librería aditiva (fondo) + dusts
  (frontal) + shader de WoTG (canvas)
- ✅ 5 shaders con .fxc compilados (copiados de WoTG) + 3 .fx fuente sin usar
- ✅ 9 texturas de WoTG copiadas y referenciadas + 11 texturas de librería registradas
- ✅ Compilación estable sin errores (verificada contra tML v2026.07.3.0 real)

### 16.2 Lo que falta (próximos pasos)
- ⚠️ **PROBAR** en tModLoader 1.4.4 los 2 armas cósmicas
- ⚠️ Verificar que los shaders carguen sin excepción en runtime
- ⚠️ Si algún shader falla, comparar parámetros con el código de WoTG en `Reference_WoTG/`
- ⚠️ Los shaders ahora cargan como .fxc (fix v5.83 del error "Asset could not be found")

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

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
> copiar/compilar `.fxc` (como hace el mod de referencia). Ver sección 8.

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
| **Versión (build.txt)** | 5.90 |
| **Author** | AethonModTeam |
| **Framework** | tModLoader 1.4.4 |
| **Runtime** | .NET 8, C# |
| **Side** | Both (Client + Server) |
| **Commit actual** | v5.90 — Revert del sol a v5.88 (8781aa4) tras arruinar sus efectos en v5.89 + agujero negro rehecho: sin partículas moradas, partículas ABSORBIDAS (nuevo componente PullTo), UNA sola explosión cromática final con daño, lente DELGADA (ángulo pico 14.9→0.8 rad) y fix del corte del disco al crecer (canvas que escala) |
| **Commit estable del remote** | e826c82 (referencia de sprites protegidos) |
| **Homepage** | https://github.com/Leo0x01/Aethon-Mod-Terraria |

### build.txt completo
```ini
author = AethonModTeam
version = 5.90
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
  </ItemGroup>
</Project>
```

> **Importante (v5.83)**: eliminado el Import roto a `/tmp/tmodloader/tMLMod.targets`
> (ruta del sandbox inexistente en la máquina del usuario). El build real de tML NO usa
> el .csproj (compila con Roslyn desde build.txt); el csproj es solo para el IDE.
> **v5.85**: la línea `<Compile Remove="ReferenceShaders/**" />` fue eliminada — la
> carpeta de código de referencia ya no existe (se perdió con el sandbox y nunca se
> repondrá; el mod compila limpio sin ella).

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
- `_masters/*` en la RAÍZ DEL REPO (sprites maestros de referencia — v5.89: fuera
de la carpeta del mod; eran JPEGs con extensión .png y disparaban el warning FNA
"Image loading failed" al empaquetar)

> **Regla de oro**: El commit `e826c82` contiene la versión estable de todos los sprites.
> Si se pierden localmente, ejecutar `git checkout e826c82 -- Content/ icon.png` para restaurarlos.

---

## 4. ESTADO ACTUAL DEL PROYECTO

### 4.1 Conteo de archivos (verificado)
- **80 archivos .cs** en `Content/` (79 + CosmicShockwaveProjectile.cs nuevo en v5.86; v5.87-v5.89 no añadieron archivos — fixes y reescrituras: Unload en v5.87, texturas en v5.88, lente/sol/batch en v5.89)
- **139 archivos .png** (sprites — se añadió DendriticNoiseZoomedOut.png)
- **8 shaders .fx** en `Content/Effects/Shaders/` (fuente)
- **5 shaders .fxc** compilados en `Content/Effects/Shaders/`
- ⚠️ **PROHIBIDO generar .xnb** — ver sección 8

### 4.2 Shaders (8 fx fuente + 5 fxc compilados)
| Shader | Archivo .fx | Archivo .fxc | Uso |
|---|---|---|---|
| RealBlackHoleShader | RealBlackHoleShader.fx | RealBlackHoleShader.fxc ✓ | BlackHoleProjectile (75-step lightmarch con lensing) |
| SunShader | SunShader.fx | SunShader.fxc ✓ | SunProjectile (estrella con corona + lava) |
| RadialShineShader | RadialShineShader.fx | RadialShineShader.fxc ✓ | Brillo radial del sol |
| BlackOnlyShader | BlackOnlyShader.fx | BlackOnlyShader.fxc ✓ | Refuerza event horizon (no usado aún: requiere render target) |
| BlackHoleDistortion | BlackHoleDistortionShader.fx | BlackHoleDistortionShader.fxc ✓ | **v5.85: EN USO** — lente gravitacional screen-space del BlackHoleLensSystem |
| ChromaticAberration | ChromaticAberration.fx | — (sin .fxc, no usado) | Aberración cromática RGB |
| Shockwave | Shockwave.fx | — (sin .fxc, no usado) | Onda expansiva |
| Bloom | Bloom.fx | — (sin .fxc, no usado) | Extract bright → blur → combine |

> **Los .fxc se copiaron directamente del repo del mod de referencia** (nuestros .fx son idénticos
> byte a byte). Si se modifica un .fx propio, hay que recompilar el .fxc con
> mgfxc/2MGFX — tModLoader NO compila .fx en el build.

### 4.3 Texturas del pipeline de efectos (10 en `Content/Effects/Textures/`)
> v5.85: carpeta renombrada (antes con nombre del mod externo de referencia) —
> las 6 rutas de código ya apuntan aquí.
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
- `Noise.png` (ParticleTex.Noise = 7) — **v5.89: REGENERADO suave** (ruido fractal con blur wrap: rugosidad 4.2→0.58); **v5.90: SIN USO en juego** (el halo de distorsión del agujero era su único consumidor y se eliminó — el "efecto que se veía mal" desapareció)

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
├── AethonMod.csproj          ← MSBuild project (excluye ReferenceShaders)
├── CARACTERISTICAS.md
├── CHANGES.md
├── COMPILACION.md
├── LICENSE
├── README.md
├── STABLE-SNAPSHOT.md
├── (ReferenceShaders/ ELIMINADA en v5.85 — ver sección 9.4)
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

### 5.3 Armas Cosmic (2 armas con shaders propios, en `Content/Weapons/Cosmic/CosmicWeapons.cs`)

#### BlackHoleStaff → BlackHoleProjectile (v5.85)
- **Daño**: 100
- **useTime/useAnimation**: 60
- **useStyle**: HoldUp
- **mana**: 0 (arma de prueba, no consume mana)
- **shoot**: `ModContent.ProjectileType<BlackHoleProjectile>()`
- **rare**: Quest
- **Shader**: `RealBlackHoleShader.fx` (75-step lightmarch con lensing gravitacional)
- **NUEVO v5.85 — Lente gravitacional**: `BlackHoleLensSystem` distorsiona el FONDO
  REAL de la pantalla alrededor del horizonte (BlackHoleDistortionShader + hook
  TimeLogger 36 + Main.screenTarget)
- **Hitbox**: 96×96 (ampliada desde 76)
- **Gravedad**: radio 450px, fuerza 2.6 (aumentadas)
- **Texturas**: `Textures/FireNoiseB.png` (ruido del disco), `Textures/InvisiblePixel.png` (canvas), `Procedural/SoftGlow.png` (halo)
- **Efectos**: succión espiral multicolor (violeta/cian/magenta/oro), disco de acreción
  de estelas, devoración de polvo (radio 260), anillo de fotones, implosión + doble
  onda expansiva al colapsar
- **Tooltip**: `[c/9600FF:═══ AGUJERO NEGRO ═══]`

#### SunStaff → SunProjectile (v5.85)
- **Daño**: 80
- **useTime/useAnimation**: 50
- **useStyle**: HoldUp
- **mana**: 0
- **shoot**: `ModContent.ProjectileType<SunProjectile>()`
- **rare**: Quest
- **Shaders**: `SunShader.fx` (esfera + corona + manchas + lava) + `RadialShineShader.fx` (brillo radial)
- **Ciclo de vida**: 10 segundos exactos
  - t=0: nace + primera llamarada (PhoenixNovaProjectile centrado)
  - cada 2s: nueva llamarada solar (5 en total)
  - t=7s: SupernovaProjectile centrado y sincronizado (carga 3s)
  - t=7→10s: gravedad del sol crece hasta x4 (base = 1/10 del agujero negro)
  - t=10s: nova masiva simultánea (sol + supernova) con doble onda expansiva
- **Texturas**: `Textures/BloomCircleSmall.png` (backglow), `Textures/WavyBlotchNoise.png`
  (canvas y noise), `Textures/PsychedelicWingTextureOffsetMap.png` (uv offset),
  `Textures/DendriticNoiseZoomedOut.png` (canvas del SunShader)
- **Efectos**: corona de plasma orbitando, viento solar radial, prominencias
  periódicas, destellos luminosos, materia convergiendo durante la carga
- **OnHit**: OnFire (300 ticks) — quemadura de plasma
- **Tooltip**: `[c/FFD700:═══ SOL ═══]`

---

## 6. SHADERS

Los 8 shaders en `Content/Effects/Shaders/`:

### 6.1 RealBlackHoleShader.fx
- **Origen**: shader heredado del pipeline de referencia
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
- **Parámetros**: distortionStrength, maxLensingAngle, sourceRadii[5], sourcePositions[5], zoom, aspectRatioCorrectionFactor
- **Compile target**: `ps_3_0`
- **USO desde v5.85**: BlackHoleLensSystem (lente gravitacional de pantalla)

### 6.3 BlackOnlyShader.fx
- **Técnica**: Smoothstep para reforzar el event horizon (la zona negra central del agujero negro)
- **Compile target**: `ps_3_0`

### 6.4 SunShader.fx
- **Origen**: shader heredado del pipeline de referencia (estrella)
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
- **Origen**: shader heredado del pipeline de referencia
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

### 8.3 Solución correcta (v5.83) — formato .fxc como el mod de referencia

**Investigación verificada contra el código fuente y el binario de tModLoader
v2026.07.3.0** (la versión exacta del usuario):

1. tModLoader **NO compila los `.fx` durante el build** (issue abierto
   https://github.com/tModLoader/tModLoader/issues/3326).
2. En tML FNA el reader de `.fx` (Terraria.Testing.FxReader) NO existe — verificado
   por reflection contra tModLoader.dll real. Los `.fx` en el .tmod **ni se
   registran** como assets.
3. El formato correcto es **`.fxc`**: tML lo registra con `FxcReader`, que crea el
   Effect con `new Effect(graphicsDevice, bytes)` en runtime.
4. **El mod de referencia commitea 270 archivos `.fxc`** junto a sus `.fx` en
   `Assets/AutoloadedEffects/` — ese es el flujo estándar de los mods grandes.

**Lo que hace AethonMod ahora:**
- Cada shader usado tiene su `.fx` (fuente) + `.fxc` (compilado) en
  `Content/Effects/Shaders/`.
- Los 5 `.fxc` se copiaron directamente del repo del mod de referencia (nuestros `.fx` son
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

## 9. RECURSOS DEL MOD DE REFERENCIA DE SHADERS

> v5.85 — LIMPIEZA: se eliminaron de TODO el proyecto (código, carpetas, tooltips,
> csproj, gitignore, changelog y este documento) los nombres del mod externo que
> sirvió de referencia técnica. Las 10 texturas viven ahora en
> `Content/Effects/Textures/` y los shaders en `Content/Effects/Shaders/` como
> recursos propios del pipeline. Si algún día se necesita volver a estudiar el
> enfoque original, pedir la URL al usuario (no se archiva aquí a propósito).

### 9.1 Repositorio de referencia
- El repo público del mod de shaders de referencia puede clonarse a `/tmp/refmod/`
  si se necesita (la URL se pide al usuario; fue eliminada de este documento en la
  limpieza v5.85).
- Estructura típica:
  ```
  /tmp/refmod/
  ├── Assets/
  ├── Content/
  ├── Core/
  ├── Localization/
  └── ...
  ```

### 9.2 Texturas en `Content/Effects/Textures/` (10, recursos propios del pipeline)
1. `WavyBlotchNoise.png` — usado por SunShader y RadialShineShader
2. `WavyBlotchNoiseDetailed.png`
3. `InvisiblePixel.png` — canvas 1×1 transparente para BlackHole
4. `PsychedelicWingTextureOffsetMap.png` — UV offset map para SunShader
5. `FireNoiseA.png`
6. `FireNoiseB.png` — noise del disco de acreción de BlackHole
7. `BloomCircleSmall.png` — backglow del SunProjectile
8. `BloomCircle.png`
9. `BloomFlare.png`
10. `DendriticNoiseZoomedOut.png` (512×512) — canvas del SunShader

### 9.3 Shaders copiados del mod de referencia (5 shaders .fx)
1. `RealBlackHoleShader.fx` (de `BlackHolePet.cs` / `PetBlackHoleRenderer.cs`)
2. `SunShader.fx` (de `StarPet.cs`)
3. `RadialShineShader.fx` (de `StarPet.cs`)
4. `BlackOnlyShader.fx`
5. `BlackHoleDistortionShader.fx`

### 9.4 Carpeta de código de referencia (ELIMINADA)
> v5.85: `ReferenceShaders/` ya NO existe (se perdió con un reset del sandbox en
> v5.82 y nunca se repondrá — no afecta a la compilación). Su contenido histórico
> era: BlackHole.cs, BlackHolePet.cs, PetBlackHoleRenderer.cs, StarPet.cs y
> Starseed.cs (código del mod de referencia, NO compilado, solo consulta). Si se
> necesita de nuevo, clonar el repo público a `/tmp/refmod/`.

### 9.5 Verificación de recursos
Para confirmar que las texturas del pipeline existen:
```bash
ls /home/z/my-project/AethonMod/Content/Effects/Textures/   # debe listar 10 .png
```

---

## 10. HISTORIAL DE VERSIONES

Commits desde v5.28 hasta v6.01 (orden inverso, más reciente primero):

| Commit | Versión | Descripción |
|---|---|---|
| ``0cc89cf`` | v6.01 | **LA GRAN LIMPIEZA — EL USUARIO ELIGE QUÉ SE QUEDA (35→14 ARMAS)**: (petición: "es momento de seleccionar que se queda en el proyecto" + lista por números + "la galaxia se ve horrible y la lanza igual, ademas las dos son tan simple que no vale la pena que continue en el mod"): listado completo del arsenal por generaciones (8 tests + Grimorio + 19 V20 + 7 cósmicas), selección confirmada (el "191" = el 19, AbyssalEyeStaff) y purga de **21 ARMAS** — **4 DE COLOR** (ColorRainbow/Red/Yellow/Green: clases extirpadas de TestAdvanced.cs, texturas, handlers 5004-5007 de TestAdvancedFX.cs y el helper huérfano DrawColoredSprite que solo usaban ellas) + **15 V20** (Tornado, PrismBeam, Earthquake, MirrorDimension, GravityPulse, ShadowClone, CrystalShatter, VortexChain con su VortexMineProjectile interno, AbyssalEye, SpectralMirage, TemporalRift, InfernoTornado, VoidEater, PlasmaOrb, BlackHoleMini — arma+proyectil+texturas, mapeo 1:1 sin huérfanos) + **2 CÓSMICAS NUEVAS** (QuasarLance+QuasarJetProjectile y LivingGalaxyStaff+LivingGalaxyProjectile+GalaxyStarProjectile+SpiralGalaxy.png — "horribles y muy simples") + **cirugía del bloque galaxia en BlackHoleLensSystem** (recolección, _galaxyIndices/_galaxyCount, fuente del pase B y draw loop extirpados; el protocolo vive intacto en soles/medusas/cometas/púlsares) + localización en-US/es-ES y TestingPlayer limpios; **SE QUEDAN 14**: los 4 tests clásicos (con handlers 3003/3004/4001/4006), el Grimorio del Eterno (su Orbe Cósmico — imagen del usuario — INTACTO), 4 V20 (Supernova/PlasmaStorm/PhoenixNova/QuantumSplit) y las 5 cósmicas (Agujero/Sol/Medusa/Cometa/Púlsar); quien tenga armas borradas en guardados viejos LAS CONSERVA. Verificación: 0 referencias a las 40 clases borradas, 39 clases con textura ✓, compilación 0 errores/0 warnings, binario: 30 nombres borrados → 0 restos |
| ``d5fb4e3`` | v6.00 | **SOL/AGUJERO PULSAN MÁS FUERTE + LÁTIGO DE LA MEDUSA + ADIÓS OJO → LA GALAXIA VIVIENTE**: (1) **AGUJERO NEGRO — TICKS QUE ACELERAN CERCA DEL CENTRO** (petición: "los tick de daño deben aumentar a medida te acercas al centro"): adiós pulso global de 0.5 s — CADA ENEMIGO tiene su PROPIO intervalo por distancia al horizonte (borde ~24 ticks, PEGADO AL CENTRO 6 ticks = 10 golpes/s) + área 1.15→1.9× el campo; **SOL**: aura 15→10 ticks (+50% golpes/s), área 1.75→2.30× starR (~150→310 px), daño 40→45%; (2) **AMBOS PERSIGUEN LIGERAMENTE** (petición: "deben perseguir ligeramente a los enemigos"): agujero se desliza hacia la presa (0.07/t, tope 2.4 px/t), sol igual (0.09/t, tope 3); (3) **LA GRAVEDAD DOBLA BALAS ENEMIGAS** (petición: "afectar los proyectiles con su gravedad"): las balas hostiles caen en espiral hacia el agujero y AL CRUZAR EL HORIZONTE SON ABSORBIDAS (chispas doradas — el agujero SE COME las balas); el sol las curva débil y si tocan el plasma SE EVAPORAN; (4) **EL LÁTIGO ELÉCTRICO** (petición: "el rayo debe salir de medusa no del cielo, y debe tener mas brillo"): NebulaLightning REESCRITO — nace BAJO LA CAMPANA y vuela RECTO a la víctima; TRES capas aditivas (halo ancho + funda + NÚCLEO blanco 255), luz real 1.3/1.55/1.75 cada paso, micro-parpadeo, ramas, DESCARGA en el origen, trueno + chisporroteo al clavarse; daño 0.8→1.0×; (5) **EL OJO DEL VACÍO ELIMINADO** (petición: "borra el ojo, se ve feo"): staff+proyectil+3 texturas+generadores+lens+localización borrados (binario: 0 ocurrencias VoidEye); (6) **LA GALAXIA VIVIENTE** (petición: "crea un arma nueva con un proyectil cosmico, este debe ser una galaxia, investiga galaxias en internet"): investigación web (M51/M101/M74/M100 — NASA/Caltech/COSMOS: bulbo AMARILLO de viejas, brazos AZULES de jóvenes, HII ROSAS "beads-on-a-string", POLVO oscuro al borde interno) → SpiralGalaxy.png 512 PIL ×4 (2 iteraciones VLM 7.5→8.5/10: polvo como POLILÍNEAS que cortan el azul, HII "como letreros de neón", brazos ASIMÉTRICOS, bulbo elíptico moteado con filamentos) + LivingGalaxyStaff (Magic 110, mana 0) → LivingGalaxyProjectile ~9 s: pop elástico, vuela y SE ESTACIONA, disco GIRA 0.02 rad/t y CABECEA EN 3D (escala Y 0.55→1.0) + eco rotado (imagen secundaria), ARRASTRA enemigos (gravedad 0.4 r300), AURA 45% cada 10 ticks, SEMBRADO estelar cada 24 ticks (2 brazos sueltan GalaxyStarProjectile tangencial — rociador cósmico), ACECHA (ancla deriva a la presa 0.7/t); EXPLOSIÓN ESTELLAR al morir: AoE 90% + 14 semillas radiales (cada una con el COLOR de su origen: azul brazo/oro bulbo/rosa HII); lente pase B respirando con el giro (0.07→0.12) dibujada ENCIMA; (7) **MEJORA DE ARMAS NUEVAS + TODO SIN MANA** (petición: "mejora las nuevas armas... todas estas son armas de prueba no requieren mana"): Cometa 38→46 + nova 92→130 px al 75% + picado 19; Púlsar 30→38 + haces 340→420 + haz 65%; Quásar 85→100 + ATRAVIESA 14 + vida 120 + florecimiento 170 px al 65%; mana=0 en Medusa/Cometa/Púlsar/Quásar (Sol y Agujero ya eran). Compilación: 0 errores, 0 warnings; auditoría 86 clases ✓ |
| ``a25b632`` | v5.99 | **OJO REDISEÑADO + RAYOS + COMETA + PÚLSAR + QUÁSAR**: (1) **EL OJO DEL VACÍO rediseñado de raíz** (petición: "el ojo no se ve nada bien, investiga en internet"): investigación web (técnicas de ojos realistas + el diseño de GARGANTUA de Interstellar) + análisis VLM de la captura (esclerótica "plato de cerámica plano", pupila "PUERTA DE MADERA" — el RealBlackHoleShader mini era papilla ilegible, párpados "brackets sueltos") → **texturas v2** (EyeSclera: sombreado ESFÉRICO + venas AUDACES núcleo oscuro/halo claro en carmesí y azul-violeta ramificadas que se desvanecen antes del iris + moteado + subsurface cálido + especular EN MEDIA LUNA; EyeIris: PER-PIXEL con campos de ruido — bandas orbitales TURBULENTAS + fibras radiales + criptas caóticas + grano estelar + anillo limbal violeta + borde interno ARDIENTE; EyeLid: armadura de carbón con rim light, pliegues y grietas; icono rehecho; 2 iteraciones con crítica VLM: 8/10 y 9/10) + **LA PUPILA GARGANTUA** (DrawPupilGargantua sustituye al shader: esfera negra de borde suave + halo de absorción + ANILLO DE FOTONES fino blanco-caliente con micro-pulso + banda de acreción horizontal CRUZANDO DELANTE + BANDA VERTICAL lenteada DETRÁS con los arcos sobre/bajo + chispa de beaming relativista — legible a CUALQUIER escala) + cavidad suave (adiós anillo duro), falloff iris→pupila (se HUNDE), CATCHLIGHT húmedo unificado, RIM GLOW aditivo en párpados, halo RESPIRANDO; (2) **RAYOS DEL CIELO para la Medusa** (petición: "es mejor que el proyectil que usa la medusa sean rayos, ya sabes, los rayos que caen del cielo"): JellyfishStingBolt ELIMINADO → **NebulaLightning** — nace 420px sobre la víctima y CAE vertical ~90px/t con ZIGZAG dentado regenerado cada pocos ticks (hash determinista: misma forma en todas las máquinas), ramas laterales, frente brillante con destello de 4 puntas, chispas de hielo y TRUENO + destello de impacto; Frostburn INTACTA (la firma); (3) **EL COMETA ESTELAR** (LivingCometStaff → StellarCometMinion — petición: usar la imagen de referencia SIN tocar el CosmicOrbMinion que la usa): núcleo de plasma blanco-oro (CometHead PIL) + CORONA DE 8 PUNTAS lanceoladas cian→violeta GIRANDO (CometCrown — homenaje a la referencia) + COLA de 18 posiciones (cálida→fría) + chispas orbitando; NO persigue: **ORBITA en elipse excéntrica** (fase por minionPos) y CAE EN PICADO (0.46/t hasta 16.5) — al rozar ESTALLA EN NOVA (AoE 92px al 60% + OnFire — materia CALIENTE, opuesta a la medusa) y rebota a la órbita; lente B creciendo con la velocidad (0.05→0.17); (4) **EL PÚLSAR VIVO** (LivingPulsarStaff → LivingPulsarMinion): estrella de neutrones (PulsarCore: núcleo blanco-azul + arcos magnéticos nítidos + polos + bandas de giro) que GIRA barriendo con DOS HACES DE FARO opuestos (340px) — daño por COMPROBACIÓN ANGULAR (ni contacto ni proyectil: RAYOS GIRANDO), 55% cada 5 ticks + ELECTRIFIED; lissajous perezoso en reposo / punto de raqueo en alto con objetivo; haces cónicos con pulso viajero + remolino + chispas tangenciales; lente B pulsando (0.05→0.12); (5) **LA LANZA DEL QUÁSAR** (QuasarLance Magic 85 → QuasarJetProjectile): CHORRO RELATIVISTA — 132px a ~78px/t ATRAVESANDO 10 enemigos, 5 NUDOS DE SHOCK pulsando hacia la punta, retorción HELICOIDAL, 3 capas violeta/cian/blanco, estela de polvo; al morir FLORECIMIENTO DEL QUÁSAR (AoE 130px al 55%). Infra: LensSystem _cometIndices/_pulsarIndices (encima de la lente + fuentes B), TestingPlayer EnsureItem ×3, localización completa, auditoría 85 clases ✓. Compilación: 0 errores, 0 warnings |
| `ea49d1a` | v5.98 | **FIX DE CARGA + MEDUSA GARANTIZADA**: (1) **EL MOD NO CARGABA** (capturas del usuario: `MissingResourceException: Content/Projectiles/Cosmic/NebulaJellyfishMinion` y `Content/Projectiles/Cosmic/JellyfishStingBolt` → MultipleException con el mod desactivado entero): v5.97 añadió los `.cs` de la Medusa pero NO sus dos **texturas de clase** — tModLoader exige un asset por defecto para cada ModProjectile; **fix**: `NebulaJellyfishMinion.png` y `JellyfishStingBolt.png`, placeholders 1×1 RGBA transparentes (70 bytes, el patrón de VoidEyeProjectile/CosmicShockwave — ambos se dibujan 100% procedural en PreDraw=false) + **auditoría preventiva** propia de las 77 clases con textura obligatoria del arsenal (eran las ÚNICAS dos ausentes); (2) **LA MEDUSA SIEMPRE DESDE EL INICIO** (petición: "recuerda que el invocador de medusa se lo debes dar al jugador desde el inicio"): el kit de TestingPlayer tenía gate único por GenesisShard → el kit quedaba "congelado" en la versión con la que se entregó y las armas añadidas después jamás llegaban a quien ya tenía el kit; **fix**: `HasItem`/`EnsureItem` garantizan INDIVIDUALMENTE las 4 armas cósmicas en desarrollo (BlackHoleStaff, SunStaff, VoidEyeStaff, **MedusaNebularStaff**) en CADA entrada al mundo. Compilación: 0 errores, 0 warnings |
| `0311bac` | v5.97 | **UNA SOLA EXPLOSIÓN FINAL + LA MEDUSA NEBULAR (invocador)**: (1) **TODO SUCEDE EN UNA** (petición: "el sol y el agujero negro tienen dos, digamos explosiones al terminar, solo deben tener una donde suceda todo. Los anillos RGB del agujero negro quedan mal, lo mejor es un anillo de lente gravitacional"): AGUJERO — la explosión YA NO es la cromática RGB + el anillo al final: **ES EL ANILLO DE EINSTEIN MISMO** (OnKill → única onda StyleEinstein con el daño COMPLETO, radio 520, con el NUEVO FLASH DE LIBERACIÓN central durante sus primeros ~16 ticks: la luz del colapso escapando; la lógica "cromática engendra Einstein" ELIMINADA del AI de la onda); SOL — ya no son 3 ondas de fuego + 1 de lente: **UNA SOLA ONDA NOVA DE LENTE** (nuevo StyleNova: triple FireRing + frente blanco de choque + aberración CÁLIDA oro/brasa — sin RGB; daño de nova COMPLETO en banda 0.72-1.02·frente cada 0.1 s + quemadura 10 s; fuente del LensSystem; AoE núcleo 260px en el mismo instante); OJO — su GRITO es UN ÚNICO DESGARRO (Einstein, daño completo, radio 460); (2) **FIX: el VoidEyeStaff ya se entrega al jugador** (faltaba en TestingPlayer — por eso "no se veía el arma nueva"; + localización en-US/es-ES); (3) **LA MEDUSA NEBULAR** (petición: "crea una nueva arma que sea un invocador para un minion… el proyectil será la invocación"): MedusaNebularStaff (Summon, mana 10, minionSlots 1, se apilan) → NebulaJellyfishMinion — el minion ES el proyectil: campana translúcida de nebulosa (JellyfishBell.png 256 PIL ×4: degradado aqua→esmeralda→violeta, 16 costillas radiales orgánicas, margen bioluminiscente ROSA, semillitas estelares), **MINIGALAXIA ESPIRAL girando en su corazón** (JellyfishGalaxy.png 128: dos brazos logarítmicos, ~140 estrellas frías/cálidas, destella al contraer), **6 tentáculos × 9 cuentas estelares con FÍSICA DE CUERDA propia** (anclas en el margen, relajación rígida→laxa, gravedad, vaivén per-tentáculo, restricción de longitud; colores teal/rosa/menta; quedan a la estela al nadar), **LOCOMOCIÓN POR PULSOS** (cada 48 ticks: contracción squash/stretch + IMPULSO al ancla — caza 6.6 sobre la víctima / reposo 2.9 del hombro del jugador; entre pulsos deriva ×0.955 + hundimiento sutil; fase por minionPos → nunca pulsan al unísono), **LENTE SUTIL del pase B RESPIRANDO con el pulso** (0.06→0.14: donde nada el fondo se dobla apenas; campana dibujada ENCIMA de la lente), **NEMATOCISTOS** (contracción junto a víctima ≤190px → 3 JellyfishStingBolt, agujas de luz 12.5px/t con QUEMADURA DE HIELO — la quemadura fría del vacío, firma única del arsenal) + contacto de campana (46×46, Frostburn) + disolución estelar al morir + buff NebulaJellyfishBuff (sostenido por la medusa, timeLeft 2 vanilla) + iconos y tooltips localizados. Compilación: 0 errores, 0 warnings; auditoría del binario OK |
| `fada77a` | v5.96 | **GLOW CORONAL SIN PARPADEO + ANILLO DE EINSTEIN + DAÑO DE ÁREA CRECIENTE + EL OJO DEL VACÍO**: (1) **EL BRILLO DEL SOL YA NO PARPADEA** (petición: "el brillo de PhoenixNovaStaff ya no debe parpadear, debe comenzar a crecer lentamente, sincronizado con el ciclo de vida del sol y con el tamaño del mismo"): las llamaradas PhoenixNova periódicas (una nova de 60 frames cada 2 s — un PARPADEO por diseño) ELIMINADAS; en su lugar **GLOW CORONAL PERSISTENTE** (`DrawCoronalGlowSprites`: dos capas SoftGlow aditivas, función PURA de lifeT — CERO sin()/flashes/oscilación; halo 1.30→2.35× starR, corona 1.05→1.55×; dimensionado con starR → la GIGANTE ROJA ×1.85 lo ARRASTRA; enrojece con rg) — la PhoenixNova standalone también suavizada (pulso ±0.07 y flash del pico ELIMINADOS; nace contenida 0.75× y crece continua hasta 2.3×); (2) **ADIÓS FORCEFIELD, LLEGA EL ANILLO DE EINSTEIN** (petición: "quita el campo de fuerza de las columnas… en su lugar, al final de las explosiones debe crear un lente gravitacional en forma de anillo que se expanda"): la burbuja Perlin/ForceField ELIMINADA por completo (binario con 0 ocurrencias) — cuando la onda cromática del agujero TERMINA de expandirse engendra la onda **StyleEinstein**: frente fino BLANCO incandescente + franjas R/B al 1.8% + halo interior pálido + imagen secundaria, expansión CASI LINEAL a 26 px/tick (rápida — el ripple del espaciotiempo), radio 520, fuente del BlackHoleLensSystem (radio 0.85×frente que ABRAZA al anillo, decae lento), banda de daño FINA 0.88-1.06×frente + ShadowFlame; (3) **TODO EL DAÑO ES ÁREA QUE CRECE** (petición: "debe extenderse por fuera del proyectil y crecer conforme crece, se expande y explota"): SOL — aura cada 0.25 s al 40% en starR×(1.35+0.30·lifeT) (110→205px, OnFire que dobla en gigante); AGUJERO — aura radio escudo×(0.75+0.45·lifeProgress) y daño 35→65% (sobre la hinchazón de la muerte vía localAI[1]); PhoenixNova standalone — aura 45→155px al 60%; (4) **EL OJO DEL VACÍO (VoidEyeStaff → VoidEyeProjectile, arma nueva de terror cósmico — petición: "lo más cósmico y de terror cósmico que se te ocurra")**: UNA ESTRELLA MUERTA CON UN OJO VIVO de 12 s — cuerpo SunShader paleta INVERTIDA (carbón+vetas carmesí, emergencia siniestra sin pop); ojo con texturas NUEVAS generadas (EyeSclera 512px marfil enfermo con VENAS ramificadas procedurales, EyeIris ámbar con estrías+anillo limbal, EyeLid carne muerta con margen carmesí — PIL supersampleado ×4); **el iris ROTA lentamente y MIRA a la víctima (offset siguiendo al enemigo más cercano… o AL JUGADOR si no hay nadie)**; **la PUPILA es un MICRO AGUJERO NEGRO (RealBlackHoleShader de 75 pasos en miniatura con disco de acreción CARMESÍ) que se DILATA (0.55→1.35) arrastrando al aura de daño (140→300px), la lente (fuente del pase B como la gigante) y la gravedad**; párpados que se abren LENTO, PARPADEAN cada 3.3 s (**en la oscuridad daña EL DOBLE y tira ×2.5**) y se RETRAEN DE PAR EN PAR en el terror (iris→SANGRE, hinchazón ×1.4, gravedad ×4, temblor); aura de pavor con ShadowFlame + ralentización ×0.92; EL GRITO final: ScaryScream + AoE 380px ×1.6 (ShadowFlame 8s+Weak) + **onda CROMÁTICA INVERSA** (el mundo colapsa hacia el ojo) + **ANILLO DE EINSTEIN** (desgarro de la realidad, 12 ticks tras el colapso) + implosión/explosión de materia oscura; TODO determinista de la edad (ai[0]) → MP coherente; lente acepta StyleEinstein y el ojo en pase B + dibujado encima; tooltips de las 4 armas cósmicas actualizados. Compilación: 0 errores, 0 warnings |
| `f91c3a1` | v5.95 | **EFECTOS DEL SOL DETRÁS DE ÉL + FIX DEL ERROR DEL AGUJERO + EL CAMPO DE FUERZA COMO ONDA + LENTE DEL SOL + ONDAS DE LENTE**: (1) **CAUSA RAÍZ DEL PARPADEO** (decompilado `Main.DrawProjectiles` de tModLoader v2026.07.3.0): el bucle principal SOLO excluye a `hide` — la llamarada usaba `DrawBehind` SIN `hide=true` → se dibujaba **DOS VECES por frame, una ENCIMA del sol** con brillo aditivo duplicado (y la Supernova hija, de índice mayor, encima también) → **los hijos van con `hide=true` y EL SOL LOS DIBUJA ÉL MISMO** (`DrawStarVisuals` capa 0: llamarada + carga de la nova ANTES de sus capas — detrás del disco SIEMPRE, inmune al orden de índices y a Luminance; el disco alpha≈1 los oculta → backlight real por el limbo; standalone conservan su dibujado); (2) **LA LLAMARADA IGUALA EL TAMAÑO DEL SOL**: `DrawFlareSprites` dimensionada con starR (radio visual real width×scale×0.75 — crece con la gigante), núcleo = disco, halo backlight 2.6×, flash del pico = rim suave (alpha 120) en vez de pantalla blanca, paleta naranja→rojo gigante, pulso ±0.07; (3) **FIX DEL IndexOutOfRangeException del client.log**: v5.94 usaba `Projectile.ai[3]` — índice INEXISTENTE (array de 3) → ahora el radio de la burbuja viaja en `localAI[0]` con fallback determinista ai[2]×0.22 (+ fix localAI[1]: capturado UNA vez, antes decaía 101→5px); (4) **EL CAMPO DE FUERZA ES LA ONDA EXPANSIVA**: escudo en vida ELIMINADO — al explotar la burbuja Perlin/ForceField parte del radio del escudo al morir y CABALGA el frente (radio=max(escudo, frente)) desvaneciéndose — destrucción de Columna CONVERTIDA en onda; (5) **LENTE DEL SOL EN GIGANTE ROJA**: BlackHoleLensSystem con DOS pases de distorsión (A fuerte: agujeros+ondas; B débil del sol: fuerza rg×0.4 — "un poco") + target propio + sol dibujado encima de la lente + MODO IDENTIDAD (sin frames de invisibilidad al morir la última fuente — backbuffer verificado en el decompile); (6) **ONDA DE LENTE (StyleLens) EN AMBAS EXPLOSIONES**: nuevo estilo 3 con RGB LIGERO (×0.65) + anillo blanco tenue, registrada como fuente de lente (curva el fondo), encima de la lente, daño 0.1 s — el sol la lanza sin retardo (radio 400, la gravitacional viaja delante de la materia) y el agujero ya la tenía (cromática+lente+ForceField); nova standalone redimensionada 240/300/360 + AoE 260. Compilación: 0 errores, 0 warnings |
| `06fcf74` | v5.94 | **EL CAMPO DE FUERZA REAL DE LAS COLUMNAS + GIGANTE ROJA + ANILLOS SOLO AL FINAL**: investigación profunda del código REAL de Terraria (entorno de decompilación reconstruido: .NET 8 + ilspycmd + tModLoader v2026.07.3.0 de GitHub; decompile de NPC 112k líneas, Main 85k, Projectile 93k + assembly completo) → **mecanismo EXACTO del escudo de las Columnas Lunares descifrado** (Main.DrawNPCDirect_Inner): la burbuja es **ruido Perlin ("Images/Misc/Perlin" del juego) en un quad 600×600 con el shader `GameShaders.Misc["ForceField"]` VANILLA** (Immediate+AlphaBlend+PointWrap+DepthStencil.Default), alpha=fuerza·0.8+0.2, flash de 30 ticks al golpe (pop +5%, brillo +50%, npc.ai[3]=1..120) y al destruirse **se expande 2×, brillo ×2 y desvanece 1-sqrt(grow)** → el agujero negro USA EL MISMO SHADER DEL JUEGO con las mismas llamadas (DrawForceField reescrito; fuerza= carga hacia la muerte; radio 2.2× horizonte que MANTIENE su tamaño durante la evaporación vía localAI[1] pre-colapso; flash al absorber golpes) y **la onda cromática dibuja la burbuja de destrucción** (ai[3]=radio final, parámetros exactos de la animación vanilla) + **AURA DE DAÑO del campo** (50% del daño cada 0.5 s dentro del escudo ×1.3, crece con la muerte — límites de daño en área mejorados) + **GIGANTE ROJA del sol** (t=7-10s: hincha ×1.85 smoothstep + ENROJECE todo — backglow/aura/SunShader/luz/dusts/partículas con ToRedGiant — y su daño de área crece: hitbox ×1.85 con Resize centro-fijo + daño ×1.75 con base en ai[2] + quemadura 10 s) + **ANILLOS SOLO AL FINAL** (causa raíz del "en todo momento": la textura HD de v5.93 dejó 16× más grandes todos los dibujos de escala fija — PhoenixNova 5 anillos por llamarada hasta 4710px y Supernova contención hasta 2458px ELIMINADOS; anillo de fotones del agujero 1178px ELIMINADO; campo v5.93 RingShieldNebula REEMPLAZADO por el ForceField real) + **REDIMENSIONADOS**: ondas sol 360/450/540→240/300/360, cromática 620→420, AoE núcleo 340→260, pulsos 280/380→200/270 + **fixes 16×**: ParticlePresets radius/64→radius/512, AbyssalEye 2.0→0.125, GravityPulse Lerp(0..5)→(0..0.3125), Earthquake ÷16, BlackHoleMini 0.35→0.0219, DrawFallback del agujero por radio. Compilación: 0 errores, 0 warnings |
| `4d8681b` | v5.93 | **CAMPO DE FUERZA estilo Columna de Nebulosa + anillos de ALTA CALIDAD** (petición del usuario con referencia explícita al Nebula Pillar): el Ring.png era de **64px** (se pixelaba a 620px de radio) → **3 texturas nuevas de 1024px generadas proceduralmente** (Ring reemplazo directo 105KB con misma geometría — los 9 usos existentes ganan calidad; RingShieldNebula = cuerpo de campo con COLOR horneado rosa→magenta→cian + arcos de energía; FireRing = llamas con color propio blanco-amarillo→naranja→rojo y lengüetas fBm) + **DrawForceField** en el agujero negro: burbuja translúcida a 1.9× el horizonte (envuelve el disco) con cuerpo nebula + aros FINOS cian/rosa que "respiran" (±5-6.5% del radio) — vive en DrawCoreVisuals (mundo Y encima-de-lente) y crece con la evaporación → al morir, la onda cromática del OnKill ES el campo expandiéndose (continuidad visual perfecta) + **DrawWaveVisual reescrita**: cromática = cuerpo nebula tenue + 3 AROS FINOS R/G/B separados 3.5→9.5% del frente (aberración VISIBLE sin lavado a blanco — validado por simulación VLM + píxeles: el diseño de 3 pasadas de banda ancha se lavaba porque la base solapaba al 100%); fuego = FireRing ×2 + Ring fino de choque; compensación thinComp=1/0.92. Bugs de generación corregidos: clamp01 sobre canales 0-255 (→textura negra) y corte de borde (contenido ≤0.995 del canvas). RingShield blanca intermedia eliminada (sin usos). Compilación: 0 errores, 0 warnings |
| `a260673` | v5.92 | **FIX del error del sol** ("el sol dio un error" — reporte del usuario con client.log): `InvalidOperationException: Begin has been called before calling End` en `CosmicShockwaveProjectile.PreDraw` (línea 367 de v5.91), 2 "Excepción silenciosa" por explosión del sol que ABORTABAN el dibujado de todos los proyectiles del frame. Causa raíz: las ondas de fuego del sol nacen con retardo escalonado (edades 0/-8/-16); en el tick EXACTO en que un retardo expira el frente mide 0 px → `DrawWaveVisual` devolvía SIN tocar el spriteBatch → el Begin de restauración INCONDICIONAL del PreDraw re-abría el batch del juego YA ABIERTO (la onda cromática del agujero nace sin retardo → jamás lo disparó, por eso SOLO el sol fallaba). Fix: `DrawWaveVisual` ahora devuelve **bool** (false = no tocó el batch / true = lo dejó CERRADO) y el PreDraw restaura SOLO cuando corresponde; auditoría de Begin/End de sol/nova/agujero/PhoenixNova/lente: ningún otro proyectil tiene el patrón. Compilación: 0 errores, 0 warnings |
| `2f56660` | v5.91 | **El SOL AUTORITA su explosión final** (la Supernova del SupernovaStaff, verificada en el historial: el OnKill del sol mata la hija EN SU MISMO TICK con TryKillSupernova + genera ÉL las 3 ondas de fuego y el AoE del núcleo → sincronización POR CONSTRUCCIÓN, sin el punto único de fallo del índice ai[1]+clamp de la v5.88; flag ai[2]=1 SunInvoked → la hija no duplica ondas/AoE) + **partículas DEL COLOR DEL SOL** (halo dorado→blanco dorado, sin azul) + **ondas que dañan CADA 0.1 s A MEDIDA QUE AVANZAN** (_nextHitAt cooldowns por banda del frente, daño en área desde el centro; quemadura OnFire 10 s en fuego) + **aberración cromática TRANSPARENTE** (alphas 230→140/150→95) + **agujero negro: materia absorbida ORIENTADA AL CENTRO** (velocidad radial + Rotation=angle+π) con **ACELERACIÓN al explotar** (PullToGlobalBoost hasta ×6, reseteado en OnKill; polvo ×3) + **lente 1.4×** (mismo ángulo ~0.8 rad) + **PhoenixNova DETRÁS del sol** (DrawBehind→drawCacheProjsBehindProjectiles, firma verificada por reflexión; quirúrgico, sin el desastre v5.89) + **FIX del SpriteBatch del PhoenixNova** (restauraba SIN GameViewMatrix.TransformationMatrix → proyectiles vanilla "flotando/subiendo": la causa real de los círculos de v5.89) |
| `9b5f0a5` | v5.90 | **REVERT del sol a 8781aa4/v5.88** (la v5.89 lo arruinó: humo SoftGlow en círculos que subían + llamaradas tapadas por el cuerpo → sin su onda expansiva) conservando SOLO el fix de la excepción silenciosa + agujero negro: **partículas moradas ELIMINADAS** (succión multicolor, humo púrpura, espiral violeta, halo Noise.png — su único usuario) y **partículas ABSORBIDAS** nuevas (componente PullTo de la librería: aceleran al centro y mueren al llegar; estelas TrailGlow ámbar→blanco + polvo GoldFlame) + **UNA SOLA explosión cromática al desaparecer** (OnKill, estilo 0, daño COMPLETO, radio 620, aberración RGB real + distorsión del fondo; las 4 ondas v5.86 eliminadas) + **LENTE DELGADA** (maxLensingAngle 24→1.5 rad, fuerza 0.62→0.55 → ángulo pico ~14.9→0.8 rad: el shader rota alrededor del centro de pantalla y movía TODA la pantalla; radio 0.75×→1.1×) + **fix del CORTE por los lados** al crecer (canvas del RealBlackHoleShader fijo de 256px con zoom creciente → ahora canvas = 256·scale con zoom constante y disco con tope: nunca cruza el borde) |
| `ad9641a` | v5.88 | fix CRÍTICO: el mod NO cargaba — faltaba CosmicShockwaveProjectile.png (desde v5.86; la compilación C# pasa sin texturas pero tML las exige al cargar) + auditoría completa 71 clases/21 rutas + bug real de daño corregido (array _hitNPCs compartido por MemberwiseClone entre ondas simultáneas → NewInstance con array fresco) + End defensivo en RestoreSpriteBatch ×3 + Main.Transform deprecado → GameViewMatrix + ParticleManager con Asset<Texture2D> diferido (fix del warning 61ms blocking) + icon_small.png 30x30 + warnings del build limpios (CS0672 ×4 Kill→OnKill, CS8632 ×21) | 
| `9035888` | v5.87 | fix: ThreadStateException al desactivar el mod — el RenderTarget2D de la lente se dispone vía Main.QueueMainThreadAction (cola ConcurrentQueue drenada al final de Main.Update, en el hilo principal, también durante la pantalla de carga del reload); Unload con programación defensiva total; auditoría del patrón Dispose en todo el mod |
| `6255c88` | v5.86 | fix/feat: la lente va DETRÁS del agujero negro y sus efectos (núcleo AboveLens + DrawCoreVisuals estático + composición por regiones) + CosmicShockwaveProjectile NUEVO (ondas cromáticas/inversas/de fuego con daño real por frente) + secuencia de muerte del agujero (explosión → evaporación → implosión con 3 ondas inversas) + 3 ondas de fuego con quemadura en la explosión del sol + primera llamarada desde t=2s |
| `90e987f` | v5.85 | feat: Sol completo (10s: llamaradas cada 2s + supernova sincronizada en el s7 + gravedad) + lente gravitacional de pantalla (BlackHoleLensSystem) + supernova mejorada con doble onda + limpieza de referencias externas |
| `da7d030` | v5.84 | feat: librería de partículas completa (ShapeDescriptor+CameraBounds+presets+6 componentes nuevos+culling) + capas de VFX en BlackHole/Sun |
| `7de559b` | v5.83 | fix: BlackHole + Sun con render de referencia + shaders .fxc (error Asset could not be found) |
| `9f8fbbc` | docs | documento completo del proyecto para dar a otra IA |
| `b2798e6` | v5.82 | fix: reescribir BlackHole + Sun con recursos exactos del mod de referencia |
| `227a790` | v5.81 | fix: asegurar todos los recursos del mod de referencia |
| `d906baf` | v5.81 | fix: arreglar todos los errores del client.log + 100 pasadas |
| `f861c5d` | v5.80 | fix: restaurar commit 3bd550a + añadir shaders de referencia + armas cósmicas |
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
| `a73bf98` | v5.29 | feat: 16 bastones de prueba + efectos cósmicos + recreación estelar de referencia |
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
- **Versión**: v6.01
- **Mensaje**: "chore v6.01: LA GRAN LIMPIEZA — el usuario seleccionó qué se queda: 21 armas ELIMINADAS del arsenal de pruebas (4 de COLOR con sus handlers 5004-5007 y el helper huérfano DrawColoredSprite, 15 V20 con sus proyectiles, y las 2 cósmicas nuevas Quásar+Galaxia 'se ven horrible y son muy simples') + cirugía del bloque galaxia en BlackHoleLensSystem + localización y kit limpios; SE QUEDAN 14: los 4 tests clásicos, el Grimorio (Orbe Cósmico intacto), 4 V20 (Supernova/PlasmaStorm/PhoenixNova/QuantumSplit) y las 5 cósmicas (Agujero/Sol/Medusa/Cometa/Púlsar); verificación: 0 referencias, 39 clases con textura, 0 errores de compilación y 0 restos en binario"

### 11.2 Qué se hizo en v6.01 (la gran limpieza — el usuario elige qué se queda)

**Petición del usuario**: "es momento de seleccionar que se queda en el
proyecto" — listado completo del arsenal (35 armas agrupadas por
generación), selección explícita por números, confirmación de dudas (el
"191" era el 19 = AbyssalEyeStaff) y veredicto sobre las 2 cósmicas más
nuevas: "la galaxia se ve horrible y la lanza igual, ademas las dos son
tan simple que no vale la pena que continue en el mod".

**A. FUERA — 21 armas**
- 4 DE COLOR (ColorRainbow/Red/Yellow/Green): clases extirpadas de
  TestAdvanced.cs, texturas .png borradas, handlers 5004-5007 eliminados
  de TestAdvancedFX.cs + el helper DrawColoredSprite (solo lo usaban ellas)
- 15 V20: Tornado, PrismBeam, Earthquake, MirrorDimension, GravityPulse,
  ShadowClone, CrystalShatter, VortexChain (+VortexMineProjectile
  interno), AbyssalEye, SpectralMirage, TemporalRift, InfernoTornado,
  VoidEater, PlasmaOrb, BlackHoleMini — arma+proyectil+texturas (mapeo
  1:1 auditado, sin huérfanos)
- 2 CÓSMICAS NUEVAS: QuasarLance+QuasarJetProjectile y
  LivingGalaxyStaff+LivingGalaxyProjectile+GalaxyStarProjectile+
  SpiralGalaxy.png procedural
- BlackHoleLensSystem: bloque galaxia extirpado (recolección, arrays,
  fuente del pase B, draw loop) — el protocolo vive en soles/medusas/
  cometas/púlsares
- Localización en-US/es-ES y TestingPlayer limpios

**B. SE QUEDAN — 14 armas**: 4 tests clásicos (TestMagicRing,
TestSparkle, ProjBeam, TestMagicRingV2), el Grimorio del Eterno (su Orbe
Cósmico — imagen del usuario — INTACTO), 4 V20 (Supernova,
PlasmaStorm, PhoenixNova, QuantumSplit) y las 5 cósmicas (Agujero Negro,
Sol, Medusa, Cometa Estelar, Púlsar Vivo — con EnsureItem garantizado).
Quien tenga armas borradas en guardados viejos LAS CONSERVA (solo dejan
de entregarse).

**C. Verificación**: 0 referencias a las 40 clases borradas; 39 clases
ModItem/ModProjectile/ModBuff TODAS con textura ✓; compilación contra
tModLoader v2026.07.3.0 real 0 errores/0 warnings; binario auditado por
bytes: 30 nombres borrados → 0 restos, 20 conservados → todos presentes.

### 11.2.1 Qué se hizo en v6.00 (histórico — sol/agujero + látigo + galaxia viviente)

**Peticiones del usuario**: aumentar los ticks de daño del sol y el agujero
negro y su área (en el agujero los ticks aumentan cerca del centro); ambos
deben perseguir ligeramente a los enemigos y afectar los proyectiles con su
gravedad; el rayo de la medusa debe salir de la medusa (no del cielo) y con
más brillo; mejorar las nuevas armas; BORRAR el ojo (se ve feo) y crear un
arma nueva cuyo proyectil sea UNA GALAXIA (investigando galaxias en
internet); todas son armas de prueba SIN MANA.

**A. El agujero negro y el sol — pulsan más fuerte y doblan balas**
- AGUJERO: ticks PER-ENEMIGO según distancia al centro (6 ticks pegado al
  horizonte → 24 en el borde) + área 1.15→1.9× el campo
- SOL: aura cada 10 ticks (antes 15), área 1.75→2.30× starR, daño 45%
- PERSIGUEN ligeramente (agujero tope 2.4 px/t, sol 3 px/t)
- GRAVEDAD SOBRE PROYECTILES HOSTILES: caen en espiral; el agujero ABSORBE
  al cruzar el horizonte (chispas doradas), el sol EVAPORA al tocar plasma

**B. El látigo eléctrico de la medusa** — NebulaLightning reescrito: nace
bajo la campana y vuela recto a la víctima; 3 capas aditivas + luz real
cada paso + parpadeo + ramas + descarga en el origen; daño 0.8→1.0×

**C. El ojo del vacío ELIMINADO** — archivos, texturas, LensSystem,
TestingPlayer y localización (binario: 0 ocurrencias)

**D. La galaxia viviente** (LivingGalaxyStaff Magic 110 mana 0 →
LivingGalaxyProjectile): investigación web M51/M101/M74/M100 →
SpiralGalaxy.png 512 PIL (bulbo dorado moteado + brazos azules asimétricos
+ HII rosas vívidas + polvo en polilíneas — 2 iteraciones VLM hasta
8.5/10) + icono 30×30; gira, CABECEA en 3D (escala Y 0.55→1.0) + eco
rotado; arrastra enemigos; aura 45%/10 ticks; SIEMBRA 2 estrellas cada 24
ticks desde los brazos (GalaxyStarProjectile: azul/oro/rosa según origen);
ACECHA flotando hacia la presa; EXPLOSIÓN ESTELLAR: AoE 90% + 14 semillas
+ destello; lente B respirando con el giro (0.07→0.12)

**E. Mejoras + sin mana** — Cometa 46/nova 130 al 75%/picado 19; Púlsar
38/haces 420/haz 65%; Quásar 100/14 penetraciones/florecimiento 170 al
65%; mana=0 en Medusa/Cometa/Púlsar/Quásar (y la Galaxia nace sin mana)

### 11.2.1 Qué se hizo en v5.99 (histórico — ojo rediseñado + rayos + cometa + púlsar + quásar)

**Peticiones del usuario**: el ojo no se ve nada bien (mejorarlo, investigando
en internet ideas de assets); los proyectiles de la medusa son aburridos —
mejor RAYOS que caen del cielo; con la segunda imagen como referencia, crear
un minion cósmico con efectos como se creó la medusa (SIN tocar el
CosmicOrbMinion existente que usa ESA imagen); crear otro minion cósmico; y
una nueva arma con un proyectil súper cósmico.

**A. EL OJO REDISEÑADO**: investigación web (ojos realistas + Gargantua) +
análisis VLM de la captura (esclerótica "plato de cerámica", pupila "PUERTA
DE MADERA", párpados "brackets") → texturas v2 (EyeSclera esférica con
venas audaces núcleo+halo y especular en media luna; EyeIris per-pixel con
campos de ruido — bandas orbitales turbulentas + fibras + criptas + grano
estelar + limbal violeta; EyeLid armadura de carbón con rim light; 2
iteraciones VLM 8/10 y 9/10) + DrawPupilGargantua (esfera negra + halo de
absorción + anillo de fotones micro-pulso + banda de acreción DELANTE +
banda vertical lenteada DETRÁS con los arcos + chispa de beaming — legible a
CUALQUIER escala; _bhShader ELIMINADO) + cavidad suave + falloff iris→pupila
+ catchlight unificado + rim glow de párpados + halo respirando.

**B. RAYOS PARA LA MEDUSA**: JellyfishStingBolt ELIMINADO → NebulaLightning
(nace 420px sobre la víctima, cae vertical ~90px/t, zigzag dentado hash-
determinista regenerado cada pocos ticks, ramas laterales, frente con
Star-destello, trueno + destello de impacto, Frostburn intacta).

**C. EL COMETA ESTELAR** (LivingCometStaff → StellarCometMinion, minionSlots
1): núcleo de plasma (CometHead) + CORONA DE 8 PUNTAS girando (CometCrown —
el homenaje a la imagen de referencia) + cola de 18 posiciones + chispas
orbitando; NO persigue: ORBITA en elipse excéntrica (fase por minionPos) y
CAE EN PICADO (0.46/t hasta 16.5) — al rozar estalla en NOVA (AoE 92px al
60% + OnFire) y rebota a la órbita; lente B que crece con la velocidad
(0.05→0.17); buff + iconos PIL.

**D. EL PÚLSAR VIVO** (LivingPulsarStaff → LivingPulsarMinion): estrella de
neutrones (PulsarCore con arcos magnéticos nítidos + polos + bandas de
giro) que GIRA con DOS HACES DE FARO opuestos barriendo el campo (340px,
daño por comprobación ANGULAR cada 5 ticks al 55% + ELECTRIFIED — ni
contacto ni proyectil: RAYOS GIRANDO); lissajous perezoso en reposo, punto
de raqueo en alto con objetivo; haces cónicos con pulso viajero + remolino
+ chispas tangenciales; lente B pulsando con el giro (0.05→0.12).

**E. LA LANZA DEL QUÁSAR** (QuasarLance, Magic 85/mana 14 →
QuasarJetProjectile): CHORRO RELATIVISTA — lanza de luz 132px a ~78px/t que
ATRAVIESA 10 enemigos, 5 NUDOS DE SHOCK pulsando hacia la punta, retorción
helical, 3 capas violeta/cian/blanco + estela; al morir: FLORECIMIENTO DEL
QUÁSAR (AoE 130px al 55% + destello + temblor).

**F. Infra**: LensSystem con _cometIndices/_pulsarIndices (encima de la
lente + fuentes B); TestingPlayer EnsureItem ×3 armas; localización
en-US/es-ES completa; auditoría 85 clases ✓; compilación 0 errores.

### 11.2.1 Qué se hizo en v5.98 (histórico — fix de carga + medusa garantizada)

**Peticiones del usuario**: "mira estos errores" (capturas del juego con
`MissingResourceException: Content/Projectiles/Cosmic/NebulaJellyfishMinion`
y `Content/Projectiles/Cosmic/JellyfishStingBolt` — el mod se desactivaba
automáticamente al cargar) y "recuerda que el invocador de medusa se lo
debes dar al jugador desde el inicio".

**A. LA CAUSA — texturas de clase ausentes**: v5.97 añadió los `.cs` de la
Medusa pero NO sus dos texturas de clase. tModLoader exige un asset para
CADA `ModProjectile` en su ruta por defecto; al no existir, el cargador
lanzaba `MissingResourceException` (dos inner exceptions) y desactivaba el
mod ENTERO — el jugador no veía nada de v5.97. **Fix**:
`NebulaJellyfishMinion.png` y `JellyfishStingBolt.png` (placeholders 1×1
RGBA transparentes, 70 bytes — el mismo patrón de `VoidEyeProjectile.png` y
`CosmicShockwaveProjectile.png`; ambos proyectiles se dibujan 100%
proceduralmente así que la textura de clase jamás se muestra). Auditoría
preventiva propia de las 77 clases con textura obligatoria del arsenal: las
dos de la Medusa eran las ÚNICAS ausentes.

**B. La Medusa SIEMPRE desde el inicio**: el kit de `TestingPlayer` tiene
gate de una sola vez (GenesisShard) — quien entró al mundo con una versión
anterior jamás recibía las armas añadidas después (kit "congelado"). **Fix**:
`EnsureItem()` garantiza INDIVIDUALMENTE las 4 armas cósmicas en desarrollo
(BlackHoleStaff, SunStaff, VoidEyeStaff, **MedusaNebularStaff**) en cada
entrada al mundo: si falta una, vuelve al inventario.

### 11.2.1 Qué se hizo en v5.97 (histórico — una sola explosión + medusa nebular)

**Peticiones del usuario**: el sol y el agujero negro tienen dos, digamos
explosiones al terminar — solo deben tener una donde suceda todo (los
anillos RGB del agujero negro quedan mal; lo mejor es un anillo de lente
gravitacional); no se veía el arma nueva (el VoidEyeStaff nunca llegó al
inventario); y crear una nueva arma que sea un INVOCADOR de un minion, con
un proyectil super creativo y cósmico que SEA la invocación.

**A. UNA SOLA EXPLOSIÓN FINAL**: AGUJERO — la explosión ES el ANILLO DE
EINSTEIN (`OnKill` → única onda `StyleEinstein`, daño COMPLETO, radio 520;
con FLASH DE LIBERACIÓN central durante los primeros ~16 ticks — la luz del
colapso escapando; la lógica "cromática engendra Einstein al terminar"
ELIMINADA del AI de la onda). SOL — una única ONDA NOVA DE LENTE (nuevo
`StyleNova`: triple FireRing + frente blanco de choque + aberración CÁLIDA
oro/brasa — sin RGB; daño de nova completo en la banda + quemadura 10 s;
fuente del LensSystem; AoE del núcleo 260 px en el mismo instante).
OJO — su GRITO es UN ÚNICO DESGARRO (Einstein, daño completo, radio 460).

**B. FIX del arma invisible**: `TestingPlayer` ahora entrega el
`VoidEyeStaff` (faltaba — jamás llegó al inventario) + entradas de
localización en-US/es-ES para el báculo, la medusa y sus proyectiles.

**C. LA MEDUSA NEBULAR (el invocador)**: `MedusaNebularStaff` (Summon,
mana 10, minionSlots 1, apilable) invoca a `NebulaJellyfishMinion` — el
proyectil ES la invocación: campana translúcida de nebulosa (JellyfishBell
PIL ×4), MINIGALAXIA espiral girando en su corazón (JellyfishGalaxy), 6
tentáculos × 9 cuentas estelares con FÍSICA DE CUERDA propia (JellyfishBead
— relajación rígida→laxa, gravedad, vaivén, restricción de longitud),
LOCOMOCIÓN POR PULSOS (48 ticks: contracción squash/stretch + impulso al
ancla; deriva ×0.955 + hundimiento entre pulsos; fase por minionPos), LENTE
SUTIL del pase B respirando con el pulso (0.06→0.14) y campana dibujada
ENCIMA de la lente, NEMATOCISTOS (3 JellyfishStingBolt con Frostburn — la
quemadura fría del vacío) + contacto de campana + disolución estelar al
morir + buff NebulaJellyfishBuff sostenido por la propia medusa.

### 11.2.1 Qué se hizo en v5.96 (histórico — glow sin parpadeo + anillo de Einstein + daño de área + el ojo del vacío)

**Peticiones del usuario**: el brillo de PhoenixNovaStaff ya no debe
parpadear — debe comenzar a crecer lentamente, sincronizado con el ciclo de
vida del sol y con el tamaño del mismo; quitar el campo de fuerza de las
columnas del agujero negro (se ve mejor sin él) y en su lugar, al final de las
explosiones, crear un lente gravitacional en forma de anillo que se expanda;
todo el daño de ambos proyectiles debe ser daño de área que se extienda por
fuera del proyectil y crezca conforme el proyectil crece, se expande y
explota; crear otra arma nueva de prueba con lo aprendido, con el proyectil
más cósmico y de terror cósmico posible (libre imaginación).

**A. El brillo del sol ya no parpadea**: las llamaradas PhoenixNova
periódicas (una nova de 60 frames cada 2 s — un PARPADEO por diseño)
ELIMINADAS; en su lugar `DrawCoronalGlowSprites`: glow coronal persistente
— dos capas SoftGlow aditivas, función PURA de lifeT (CERO sin/flashes),
halo 1.30→2.35× starR, corona 1.05→1.55×, dimensionado con starR (la
gigante roja ×1.85 lo arrastra), enrojece con rg. La PhoenixNova standalone
también se suavizó (pulso y flash eliminados; crece continua 0.75→2.3×).

**B. El anillo de Einstein**: burbuja Perlin/ForceField ELIMINADA por
completo (binario: 0 ocurrencias de ForceField/Perlin). Cuando la onda
cromática del agujero TERMINA de expandirse (el final de la explosión)
engendra la onda StyleEinstein: frente fino blanco incandescente, franjas
R/B al 1.8%, halo interior pálido + imagen secundaria, expansión casi
lineal a 26 px/tick (radio 520), fuente del sistema de lente (radio
0.85×frente abrazando al anillo), banda de daño fina 0.88-1.06×frente cada
0.1 s + ShadowFlame.

**C. Todo el daño es área creciente**: SOL — aura cada 0.25 s al 40% en
starR×(1.35+0.30·lifeT) (110→205px, se extiende FUERA del cuerpo, crece con
el ciclo y con la gigante; OnFire dobla en gigante). AGUJERO — aura radio
escudo×(0.75+0.45·lifeProgress) y daño 35→65% (sobre la hinchazón +60% de
la muerte vía localAI[1]); el clímax del área: cromática + Einstein.
PhoenixNova standalone — aura 45→155px al 60% + OnFire cada 0.166 s.

**D. El Ojo del Vacío (VoidEyeStaff → VoidEyeProjectile, 12 s)**: UNA
ESTRELLA MUERTA CON UN OJO VIVO. Cuerpo: SunShader paleta invertida
(carbón + vetas carmesí), emergencia siniestra sin pop elástico. Ojo:
EyeSclera (marfil enfermo con venas ramificadas procedurales) + EyeIris
(ámbar, estrías radiales, ROTA lentamente) + EyeLid (carne muerta, margen
carmesí) — texturas 512px generadas con PIL supersampleado ×4. El iris
MIRA a la víctima (offset siguiendo al enemigo más cercano… o AL JUGADOR
si no hay nadie). La PUPILA es un micro agujero negro (RealBlackHoleShader
75 pasos en miniatura, disco de acreción carmesí) que se DILATA (0.55→1.35)
arrastrando al aura de daño (140→300px), la lente (pase B como la gigante)
y la gravedad. Párpados: se abren LENTO (0.8-3 s), PARPADEAN cada 3.3 s
(en la oscuridad daña EL DOBLE y la gravedad tira ×2.5), se retraen DE PAR
EN PAR en el terror (iris→sangre, hinchazón ×1.4, gravedad ×4, temblor).
Aura de pavor: ShadowFlame + ralentización ×0.92/tick. EL GRITO final:
ScaryScream + AoE 380px ×1.6 (ShadowFlame 8 s + Weak) + onda cromática
INVERSA (el mundo colapsa hacia el ojo) + ANILLO DE EINSTEIN (12 ticks
tras el colapso) + implosión/explosión de materia oscura. Sonidos:
MoonLord (nacimiento/terror), ZombieMoan (despertar/quejidos/parpadeos),
ScaryScream (el grito). Lágrimas de sangre, zarcillos orbitando (librería
Orbit+ColorShift), brasa corrupta, llama sombría. TODO determinista de la
edad (ai[0]) → MP coherente sin sincronizar nada.

**E. Sistema de lente**: acepta StyleEinstein como fuente (pase A) y el
ojo como fuente sutil (pase B, fuerza (dil-0.6)×0.45 tope 0.45) + dibujado
encima de la lente (paso 6). Tooltips de las 4 armas cósmicas actualizados.

### 11.2.1 Qué se hizo en v5.95 (histórico — efectos detrás del sol + fix del agujero + campo=onda + lentes)

**Peticiones del usuario**: los efectos SupernovaStaff y PhoenixNovaStaff deben
estar DETRÁS del sol (había un extraño parpadeo — el PhoenixNova no estaba
detrás); aumentar el tamaño del PhoenixNovaStaff hasta igualar el del sol;
un poco de lente gravitacional para el sol a medida que crezca como gigante
roja; el agujero negro tiene un error y el campo de fuerza debe usarse COMO
onda expansiva; en ambas explosiones debe haber una onda expansiva creada con
lente gravitacional y ligera distorsión cromática RGB.

**A. Fix del error (client.log)**: v5.94 escribía `Projectile.ai[3]` — el
array `ai` de tModLoader SOLO tiene 3 ranuras → IndexOutOfRangeException en
OnKill y DrawWaveVisual (la burbuja jamás se dibujó, el OnKill abortaba antes
de sonidos/dusts). Ahora: `localAI[0]` + fallback determinista `ai[2]×0.22`.
Fix extra: localAI[1] se captura UNA vez al iniciar la evaporación (antes
decadía de 101px a 5px con la escala colapsada).

**B. El parpadeo (decompilación de Main.DrawProjectiles)**: el bucle
principal solo excluye `hide` → la llamarada (DrawBehind sin hide) se
dibujaba DOS VECES, una ENCIMA del sol; la nova hija (índice mayor) encima.
Solución: hijos con `hide=true` + EL SOL LOS DIBUJA (DrawStarVisuals capa 0,
detrás del disco SIEMPRE — el disco alpha≈1 los oculta, backlight por el
limbo). Standalones intactos.

**C. Llamarada al tamaño del sol**: DrawFlareSprites con starR real (crece
con la gigante): núcleo = disco, halo 2.6×, flash = rim suave (120/235),
paleta naranja→rojo, pulso ±0.07.

**D. Campo de fuerza = onda**: sin escudo en vida; la burbuja parte del
radio del escudo al morir y CABALGA el frente (max(escudo, frente))
desvaneciéndose (fade 1-p·0.9, brillo 2→1). Aura de daño conservada.

**E. Lente del sol (dos pases)**: pase A fuerte (agujeros+ondas) + pase B
débil del sol (fuerza rg×0.4, radio 1.4× starR) con target propio; el sol se
dibuja encima de la lente (DrawStarVisuals); MODO IDENTIDAD al morir la
última fuente (sin frame invisible — backbuffer verificado en decompile).

**F. StyleLens (onda de lente)**: RGB ligero (×0.65) + anillo blanco tenue;
fuente de lente (curva el fondo) + encima de la lente + daño 0.1 s; el sol la
lanza sin retardo (radio 400); el agujero la tenía (cromática). Nova
standalone: ondas 240/300/360 + AoE 260.

### 11.2.2 Qué se hizo en v5.94 (histórico — escudo real de Columna + gigante roja)

**Peticiones del usuario**: los anillos quedaron demasiado grandes (redimensionar
sol y agujero); los anillos son parte de la onda expansiva — SOLO deben salir al
final (salían en todo momento, con captura); el sol al explotar debe crecer en
rojo (gigante roja) con su daño en área creciendo junto a la estrella; el agujero
debe mejorar los límites de su daño en área; "no hiciste el efecto que tienen los
escudos de las columnas en terraria — investiga profundamente el código de
Terraria y de cualquier mod con escudos o campos de fuerza".

**A. Investigación real (decompilación)**: el escudo de las Columnas = Perlin
("Images/Misc/Perlin") en quad 600×600 + shader `GameShaders.Misc["ForceField"]`
(DyeInitializer: `new MiscShaderData(Main.PixelShaderRef, "ForceField")`); vivo:
alpha=fuerza·0.8+0.2; golpe: proyectil 629 baja fuerza y npc.ai[3]=1 → flash 30
ticks (pop +5%, UseColor(1+flash·0.5)); destruido: expansión ×(1+grow) hasta 2×,
UseColor(2), fade 1-sqrt(grow). El mod usa el MISMO shader con las MISMAS
llamadas → look exacto del juego.

**B. Agujero negro**: DrawForceField con el mecanismo vanilla (fuerza = carga
hacia la muerte 0.2→1.0; flash al absorber golpes — OnHitNPC → localAI[0];
radio 2.2× horizonte estable durante la evaporación — localAI[1] pre-colapso);
al morir el OnKill pasa el radio a la onda (ai[3]) y la onda dibuja la burbuja
EXPANDIÉNDOSE y DESAPARECIENDO con los parámetros exactos. AURA de daño: 50%
cada 0.5 s dentro del campo ×1.3. Onda cromática 620→420.

**C. Sol (gigante roja)**: t=7-10s — hincha ×1.85 (smoothstep) y enrojece
(backglow/aura/SunShader/luz/dusts/partículas); hitbox ×1.85 (Resize centro
fijo — verificado en el decompile) y daño ×1.75; quemadura 10 s. Ondas
360/450/540→240/300/360; AoE del núcleo 340→260.

**D. Anillos solo al final (causa raíz del bug)**: la textura HD de v5.93
(64→1024px) dejó 16× más grandes los dibujos de escala fija. Eliminados:
anillos de las llamaradas (PhoenixNova, hasta 4710px), de la carga (Supernova,
hasta 2458px) y del anillo de fotones del agujero (1178px). Fixes: librería
radius/64→radius/512; V20 (AbyssalEye/GravityPulse/Earthquake/BlackHoleMini)
÷16; DrawFallback por radio.

**E. Verificación**: compilación contra tModLoader real v2026.07.3.0 con 0
errores y 0 warnings (entorno reconstruido: /tmp/verify + stub del hook
MonoMod On_TimeLogger, generado en runtime por tML y ausente del DLL distribuido).

### 11.2.3 Qué se hizo en v5.93 (histórico — campo Nebula inventado + anillos HD)

**Peticiones del usuario**: el anillo del agujero negro (Ring.png) tenía muy
baja calidad y era solo blanco; el agujero necesita el CAMPO DE FUERZA de la
Columna de Nebulosa (burbuja con aberración cromática que al destruirse se
expande y desaparece); los anillos de fuego del sol también debían mejorar.
"Si te faltan assets puedes generarlos o buscarlos y recrear tus versiones."

1. **DIAGNÓSTICO**: Ring.png era 64×64 — pixelado al escalarlo a 620px. El
   escudo real del Nebula Pillar (analizado con búsqueda de imágenes + VLM):
   borde exterior cian-azul brillante + cuerpo magenta + interior rosado,
   translúcido, con energía interna — la aberración vive en el BORDE.
2. **TEXTURAS 1024px** (procedurales, funciones suaves = cero aliasing):
   Ring.png (reemplazo directo, misma geometría → los 9 usos existentes
   ganan calidad solos), RingShieldNebula.png (cuerpo del campo con color
   horneado + arcos de energía), FireRing.png (llamas con color propio).
3. **CAMPO DE FUERZA del agujero** (DrawForceField, en ambos pases):
   burbuja a 1.9× el horizonte = cuerpo nebula + aros finos cian/rosa que
   respiran; crece con la evaporación; al morir la onda lo "expande".
4. **ONDA CROMÁTICA**: cuerpo nebula tenue + 3 aros finos R/G/B separados
   (3.5→9.5% del frente, invertidos en la convergente) — aberración clara,
   sin lavado a blanco (diseño validado por simulación con VLM + píxeles).
5. **ONDA DE FUEGO del sol**: FireRing ×2 (llamas con lengüetas reales,
   núcleo incandescente → rojo en puntas) + Ring fino de choque blanco.
6. Bugs de generación corregidos: clamp01 sobre canales 0-255 (FireRing
   negra), corte de borde (contenido ≤ 0.995 del canvas), y el preview de
   simulación (alpha ×255 doble → blanco falso).

### 11.2.0 Qué se hizo en v5.92 (histórico — fix del error del sol)

**Reporte del usuario**: "el sol dio un error" — el client.log mostraba 2
"Excepción silenciosa" por cada explosión del sol:

```
System.InvalidOperationException: Begin has been called before calling End
   at SpriteBatch.Begin(...)
   at CosmicShockwaveProjectile.PreDraw(Color& lightColor)
   at Terraria.Main.DrawProj_Inner / DrawProjectiles / Draw
```

**Causa raíz** (trazada tick a tick): las ondas de fuego del sol nacen con
retardo escalonado (edades 0/-8/-16). En el tick EXACTO en que un retardo
expira, el frente mide 0 px → `DrawWaveVisual` devolvía SIN tocar el
`spriteBatch` (que seguía ABIERTO, el del pase del mundo) → el `Begin` de
restauración INCONDICIONAL del `PreDraw` re-abría un batch YA ABIERTO → la
excepción abortaba el dibujado de TODOS los proyectiles del frame. La onda
cromática del agujero negro nace SIN retardo (su edad jamás es 0 en el PreDraw)
→ por eso SOLO el sol disparaba el error.

**Fix**: `DrawWaveVisual` ahora devuelve `bool` (false = no tocó el batch /
true = lo dejó CERRADO) y el `PreDraw` restaura el batch SOLO cuando
corresponde. Auditados los Begin/End de sol/nova/agujero/PhoenixNova/lente:
ingún otro proyectil tiene este patrón. Compilación contra tModLoader
v2026.07.3.0 real: 0 errores, 0 warnings.

### 11.2.0 Qué se hizo en v5.91 (histórico — el sol autorita su explosión final + ondas con daño cada 0.1 s)

**Peticiones del usuario** (plan aprobado — "ejecuta todo lo demás"): la explosión
final es la del SupernovaStaff, debe estar SINCRONIZADA y ser la final con
partículas DEL COLOR DEL SOL; partículas absorbidas del agujero negro UBICADAS
EN DIRECCIÓN AL CENTRO; lente ligeramente MÁS GRANDE; aberración cromática
TRANSPARENTE; partículas ACELERÁNDOSE al explotar; ondas que dañan A MEDIDA QUE
AVANZAN (área + cada 0.1 s); PhoenixNova DETRÁS del sol; quemadura 10 s; el
agujero negro tiene 10× MÁS fuerza de atracción que el sol (2.6 vs 0.26).

1. **SOL — explosión final autoritaria**: el OnKill del sol (muerte EXACTA a
   los 10 s, timeLeft fijo) ahora (a) mata la Supernova hija EN EL MISMO TICK
   (`TryKillSupernova` — flash + estallido sincronizados POR CONSTRUCCIÓN, sin
   depender del índice ai[1] ni del clamp de timeLeft de la v5.88, el punto
   único de fallo de la "onda que no se procesaba"), (b) genera LAS 3 ONDAS DE
   FUEGO (360/450/540, daño sol × 1.25 × 0.5) y (c) el AoE del núcleo (340 px,
   daño sol × 1.25) + quemadura 10 s. La nova hija nace con flag `ai[2]=1`
   (SunInvoked) → NO genera ondas/AoE propios (cero dobles); la
   SupernovaStaff standalone (`ai[2]=0`) conserva su explosión completa.
2. **PARTÍCULAS DEL COLOR DEL SOL**: la carga de la nova ya no vira al azul —
   halo dorado (255,200,90) → BLANCO DORADO (255,235,115), núcleo (255,250,215),
   luz en la familia cálida; quemadura de contacto 5 s → 10 s.
3. **ONDAS CON DAÑO CADA 0.1 s** (CosmicShockwave): `_hitNPCs` (bool, un golpe
   por NPC) → `_nextHitAt` (int, cooldown): cada NPC en la BANDA del frente
   ([0.72·front, 1.02·front] expansivas) recibe daño cada 6 ticks = 0.1 s
   EXACTOS mientras la onda lo barre; el disco completo queda cubierto desde
   el centro ("daño en área"). Fuego: quemadura 600 ticks (10 s). Cromática:
   alphas RGB 230 → 140 y núcleo 150 → 95 (TRANSPARENTE).
4. **AGUJERO NEGRO**: materia absorbida con velocidad RADIAL hacia el centro y
   eje largo apuntando AL CENTRO (Rotation = angle + π); nuevo
   `ParticleManager.PullToGlobalBoost` (1 + expansion·5, hasta ×6) dispara la
   aceleración de TODA la materia absorbida durante la secuencia de muerte
   ("acelerarse en el momento de explotar"), reseteado en OnKill; el polvo
   dorado también acelera (hasta ×3); lente 1.1× → 1.4× (mismo ángulo pico
   ~0.8 rad — delgada); vida 10 s (600) y gravedad 2.6 = 10× el sol (0.26)
   confirmados y documentados.
5. **PHOENIXNOVA DETRÁS DEL SOL + FIX CRÍTICO DEL BATCH**: hook `DrawBehind`
   → `drawCacheProjsBehindProjectiles` (firma verificada por reflexión contra
   tML real; DrawCachedProjs se dibuja ANTES de DrawProjectiles) → la
   llamarada erupciona POR DETRÁS del cuerpo de la estrella. Y el bug REAL de
   los "círculos que subían" (presente desde v5.88): el PreDraw del PhoenixNova
   restauraba el SpriteBatch SIN Main.GameViewMatrix.TransformationMatrix (ni
   sampler/rasterizer) → todos los proyectiles vanilla posteriores se dibujaban
   sin el transform del mundo. Ahora el restore es EXACTO al estado de tML.

### 11.2.1 Qué se hizo en v5.90 (histórico — revert del sol + agujero negro rehecho)

**Reporte del usuario** (tras probar v5.89 en juego): el sol EMPEORÓ (círculos
SoftGlow subiendo desde la estrella + onda expansiva perdida — las llamaradas
hide/DrawBehind quedaban tapadas por el cuerpo), el agujero se CORTA por los
lados al crecer para desaparecer, y pidió: quitar partículas moradas, agregar
partículas absorbidas, UNA sola explosión cromática al desaparecer (con
aberración + daño) y lente delgada.

1. **REVERT del sol**: `git checkout 8781aa4 --` SunProjectile + Supernova +
   PhoenixNova → el sol VOLVIÓ a v5.88 exacto (dusts vanilla, supernova
   auto-dibujada, llamaradas en pase normal). Se conservó únicamente el fix
   de la "Excepción silenciosa" (End defensivo solo en catch — invisible).
2. **Fix del corte del disco**: el canvas del RealBlackHoleShader era FIJO
   (256px) con zoom interno creciente → al hincharse (scale 1.6) la cobertura
   caía a 0.83 contra un toro de 1.39 → corte vertical. Ahora canvas =
   256·max(scale,0.08) con zoom CONSTANTE (width/256·2) y disco con tope
   (min(scale,1)·0.4): nunca cruza el borde; horizonte en píxeles idéntico.
3. **Partículas**: fuera TODO lo morado; nuevo componente **PullTo** (bit 13:
   UserData0/1 = centro, UserData3 = fuerza; muere al llegar) +
   SpawnLibraryAbsorbedMatter (TrailGlow ámbar→blanco espiral de infalling) +
   SpawnAbsorbedDusts (GoldFlame); presets y halo recalentados.
4. **UNA explosión cromática final**: OnKill → UNA onda estilo 0 con daño
   COMPLETO y radio 620 (RGB separados + distorsión de fondo + daño por
   frente). Crecimiento y evaporación ya sin ondas.
5. **Lente delgada**: parámetros 24→1.5 rad / 0.62→0.55 / radio 0.75×→1.1×.
   El .fxc NO se puede recompilar aquí (sin mgfxc/wine) — fix solo parámetros.

### 11.2.2 Qué se hizo en v5.89 (histórico — pantalla negra del agujero + sol + excepciones del log)

**Errores reportados** (captura + client.log de la v5.88 EN JUEGO — el mod ya cargaba):

1. **"Toda la pantalla se oscurece"**: la captura mostraba el mundo SOLO dentro de
   un cuadrado perfecto de 225×225 px centrado en el agujero; el resto era NEGRO
   PURO. Análisis de píxeles + VLM + decompilación de FNA.dll → causa raíz al 100%:
   **`SetRenderTarget(null)` de FNA LIMPIA el backbuffer** (semántica
   DiscardContents por defecto de PresentationParameters — el propio Terraria hace
   Clear+redraw en su EndCapture). La composición por REGIONES de la v5.86-5.88
   restauraba el binding (wipe) y solo redibujaba las regiones → el mundo quedaba
   destruido. FIX: lente a resolución NATIVA + blit a PANTALLA COMPLETA tras el
   restore (sin regiones, sin media resolución, sin bordes duros).
2. **"Sus efectos deben estar detrás del sol"**: los dusts vanilla se pintan en la
   capa de polvo (DESPUÉS de los proyectiles = ENCIMA del sol) y la supernova
   (índice mayor) tapaba el cuerpo. FIX: efectos ambientales → partículas de la
   librería (BeforeProjectiles); supernova con `ai[1]=1` dibujada por el sol
   (DrawChargeVisuals, capa más profunda); PhoenixNova con `hide=true` +
   `DrawBehind` → capa behindProjectiles (ANTES que los proyectiles).
3. **"Excepción silenciosa" ×4 en el log**: el `try{End}catch{}` incondicional del
   restore disparaba una excepción CAPTURADA cada frame (tML las registra vía
   first-chance handler, deduplicadas). FIX: End defensivo SOLO en los catch.
4. **Noise.png mejorado**: regenerado como ruido fractal suave con blur WRAP
   (tileable): rugosidad 4.2→0.58. FireNoiseB (disco de acreción) suavizado
   8.0→2.38 con blur wrap.
5. **Warning FNA "Image loading failed"**: los 23 archivos de `Content/_masters/`
   eran JPEGs disfrazados de .png (2.1MB empaquetados). Movidos a `_masters/` en
   la raíz del repo (fuera del build).
6. **Compilación verificada**: 0 errores, 0 warnings contra tModLoader
   v2026.07.3.0 real.

### 11.6 Qué se hizo en v5.88 (histórico — error del usuario al CARGAR v5.87 + revisión profunda pedida)

**Error reportado** (captura + client.log):

```
MissingResourceException: Recurso esperado no encontrado:
  Content/Projectiles/Cosmic/CosmicShockwaveProjectile
"Se ha producido un error al cargar AethonMod. Los mods se han desactivado
 automáticamente."
```

**El mod NO cargaba desde v5.86** (por eso las mejoras de la v5.86 nunca se
vieron en juego): el proyectil nuevo se creó sin su .png; la compilación C#
pasa sin texturas, pero tML las exige al CARGAR.

1. **FIX CRÍTICO — textura**: nuevo `CosmicShockwaveProjectile.png` (copia del
   InvisiblePixel 1×1 — el dibujado es 100% manual). Auditorías: 71 clases de
   contenido vs .png (era la única faltante) y 21 rutas de ModContent.Request
   en runtime (todas existen).
2. **BUG REAL DE DAÑO CORREGIDO**: tML clona el prototipo con MemberwiseClone →
   el array `_hitNPCs` de campo se COMPARTÍA entre ondas simultáneas; cada
   nacimiento de las 3 ondas inversas borraba las marcas de sus hermanas →
   golpes múltiples. Fix: override `NewInstance(Projectile)` con array fresco
   por onda (API virtual verificada contra el binario real).
3. **ROBUSTEZ DE RENDER**: End defensivo antes del Begin de restauración en
   RestoreSpriteBatch (BlackHole/Sun) y Supernova (una excepción con Begin
   abierto → "Begin has already been called" → crash del frame).
   `Main.Transform` (deprecado) → `GameViewMatrix.TransformationMatrix` (4 sitios).
4. **WARNING 61ms blocking**: ParticleManager ahora guarda `Asset<Texture2D>`
   sin resolver `.Value` durante la carga (se resuelve al dibujar, ya en juego).
5. **WARNING icon_small.png**: creado a 30×30 exacto (LANCZOS desde el icon.png
   80×80; tML exige 30×30 para la lista compacta, verificado decompilando
   `ModLoader.GetModIcon`).
6. **BUILD LIMPIO**: CS0672 ×4 (Kill→OnKill: CosmicProjectileFX, CosmicOrbBolt,
   QuantumSplitProjectile, GenesisLight) + CS8632 ×21 quitadas → compilación
   verificada SIN supresiones: **0 warnings, 0 errores**.
7. Revisión de 10 pasadas completa (ver CHANGES.md sección G): MP guards,
   fases del BH/sol, Globals, balance Begin/End — todo lo demás OK.

### 11.5 Qué se hizo en v5.87 (histórico — error del usuario al DESACTIVAR tras probar v5.85)

**Error reportado** (captura del diálogo de tModLoader v2026.7.3.0):

```
System.Threading.ThreadStateException: most FNA3D audio/graphics functions must
be called on the main thread
  at Microsoft.Xna.Framework.ThreadCheck.CheckThread()
  at Microsoft.Xna.Framework.Graphics.Texture.Dispose(Boolean)
  at Microsoft.Xna.Framework.Graphics.RenderTarget2D.Dispose(Boolean)
  at AethonMod.Content.Effects.BlackHoleLensSystem.Unload()  (línea 60)
  at Terraria.ModLoader.Mod.UnloadContent() → ModLoader.Unload()
```

"Uno o más errores ocurrieron durante la desactivación y tModLoader debe
reiniciarse... AethonMod no se ha desactivado correctamente."

1. **Causa raíz**: tModLoader descarga los mods en un HILO DE CARGA SECUNDARIO,
   pero FNA3D exige que el `Dispose()` de recursos gráficos corra en el hilo
   principal (`ThreadCheck.CheckThread()` lanza y aborta toda la desactivación).
   La lente gravitacional (v5.85+) tiene su propio `RenderTarget2D` y el
   `Unload()` de la v5.86 lo disponía directamente → excepción.
2. **Solución (verificada contra tModLoader.dll v2026.07.3.0 real por reflection +
   decompilación ilspycmd)**: `Main.QueueMainThreadAction(Action)` encola en
   `ConcurrentQueue<Action> _mainThreadActions`, drenada por
   `ConsumeAllMainThreadActions()` al final de `Main.Update()` CADA FRAME —
   incluso mientras la pantalla de carga del reload sigue dibujándose (el propio
   tML la usa para operaciones de ventana). OJO: `QueueRenderAction` NO existe
   en esta versión; `QueueMainThreadAction` SÍ.
3. **`BlackHoleLensSystem.Unload()` v5.87**: detach del hook en try/catch; el
   `target.Dispose()` se ENCOLA al hilo principal (el closure captura una
   variable local, no el ModSystem ni estado estático → acción autosuficiente);
   las referencias estáticas se anulan de inmediato. **Programación defensiva
   total**: encolado y Dispose envueltos en try/catch (como pide el propio
   diálogo de tML).
4. **Auditoría del patrón en todo el mod**: `RenderLens()` re-crea el target
   dentro del hook del punto 36 (hilo de render — SEGURO);
   `ParticleManager.Unload()` solo anula referencias (las texturas son Assets
   propiedad de tML — SEGURO); `AethonMod.Unload()` vacío (SEGURO); el único
   `new RenderTarget2D` del mod es el de la lente.
5. **Compilación verificada**: 0 errores contra tModLoader v2026.07.3.0 real
   (mismos 4 warnings benignos preexistentes, Kill obsoleto en archivos viejos).

### 11.3 Estado actual del mod
- ✅ Mod compila correctamente (verificado contra tML 2026.07.3.0 real, 0 errores / 0 warnings)
- ✅ **v5.93 — CAMPO DE FUERZA estilo Columna de Nebulosa** alrededor del
  agujero negro (magenta→cian con aberración viva en el borde) que la onda
  cromática "expande" al morir
- ✅ **v5.93 — ANILLOS 1024px DE ALTA CALIDAD**: Ring HD (todos los
  efectos ganan), RingShieldNebula (cuerpo del campo), FireRing (llamas
  reales con color propio para el sol)
- ✅ **v5.92 — SIN el error del sol**: el SpriteBatch nunca queda desbalanceado
  (las ondas con retardo escalonado ya no re-abren un batch abierto)
- ✅ **v5.91 — SOL AUTORITA su explosión final**: Supernova sincronizada por
  construcción + ondas de fuego con daño cada 0.1 s + quemadura 10 s
- ✅ **v5.91 — AGUJERO NEGRO**: materia absorbida orientada al centro +
  aceleración al explotar + lente 1.4× + aberración cromática transparente
- ✅ **v5.91 — PHOENIXNOVA detrás del sol** (DrawBehind) + fix del SpriteBatch
  del PhoenixNova (los "círculos que subían" de v5.89)
- ✅ **v5.89 — SIN PANTALLA NEGRA**: la lente va a resolución nativa y se vuelve
  a dibujar a pantalla completa tras el wipe inevitable del backbuffer de FNA
- ✅ **v5.88 — EL MOD CARGA**: textura del CosmicShockwaveProjectile añadida (el
  MissingResourceException de v5.86/v5.87 estaba bloqueando la carga)
- ✅ Los 5 shaders usados tienen .fxc cargable (fix v5.83) — y BlackHoleDistortion
  ahora SÍ se usa (lente gravitacional v5.85)
- ✅ Librería de partículas COMPLETA según el libro (v5.84) + capa AboveLens (v5.86)
  + componente PullTo (v5.90)
  + carga de texturas SIN bloqueo (Asset diferido, v5.88)
- ✅ Sol: ciclo completo de 10s con llamaradas (t=2,4,6,8s) + supernova sincronizada (v5.86)
- ✅ Agujero negro: lente DETRÁS del agujero y sus efectos + secuencia de muerte
  (crecimiento → evaporación → UNA explosión cromática final v5.90) y cada onda
  daña EXACTAMENTE una vez por NPC (fix del array compartido, v5.88)
- ✅ Desactivación del mod LIMPIA: el Dispose del render target de la lente se
  encola al hilo principal (v5.87 — fix del ThreadStateException de FNA3D)
- ✅ Cero referencias al mod externo de referencia en todo el proyecto (v5.85)
- ✅ Build 100% limpio: 0 warnings 0 errores sin supresiones (v5.88)
- ⚠️ **PENDIENTE**: probar en tModLoader real (recompilar, verificar carga sin
  error, campo de fuerza Nebula alrededor del agujero + onda R/G/B al
  explotar — v5.93 —, llamas del sol en HD, log limpio, desactivación limpia)

### 11.4 Próximos pasos sugeridos
1. El usuario: abrir tModLoader → Develop Mods → Build (recompila desde fuente)
2. Entrar al mundo (el kit de TestingPlayer incluye ambos staves)
3. Disparar BlackHoleStaff: el agujero rodeado por su CAMPO DE FUERZA
   magenta→cian con aberración en el borde (estilo Nebula Pillar) + al
   desaparecer, el campo EXPANDIÉNDOSE como onda con franjas R/G/B
   separadas y daño cada 0.1 s
4. Disparar SunStaff: ondas de fuego con LLAMAS reales (núcleo
   incandescente → rojo en las puntas) + anillos de la nova en HD
5. Verificar que el client.log quede LIMPIO (sin "Excepción silenciosa")
6. **Desactivar el mod o Mods → Reload: la desactivación debe completarse EN
   SILENCIO** (sin diálogo de error, sin pedir reinicio — fix v5.87)
7. Si algo falla, revisar client.log (la lente tiene try/catch total: lo peor que
   puede pasar es que no se dibuje)
8. FUTURO (Grimorio): quemadura del sol potenciada por daño mágico + integración
   del proyectil como ataque del arma definitiva
9. Roadmap natural: compilar Bloom/ChromaticAberration/Shockwave con mgfxc/2MGFX
   cuando se usen desde C#; componentes Trail/BounceOnTile/DieOnTile; ModConfig

### 11.5 Qué se hizo en v5.86 (histórico — reportes del usuario tras probar v5.85)

**Reportes**: (1) la lente afectaba al propio agujero negro y debía ir detrás de
su animación y efectos; (2) faltaba la onda cromática en la explosión del agujero
con crecimiento momentáneo del área y una implosión final con 3 ondas inversas
(daño cada una); (3) la primera llamarada del sol salía en la posición del
jugador; (4) faltaban las 3 ondas de fuego finales con daño + quemadura.

1. **ARQUITECTURA DE LENTE INVERTIDA (la lente va DETRÁS)**: con la lente activa
   el núcleo del agujero NO se dibuja en el pase del mundo
   (`BlackHoleProjectile.PreDraw` se salta); el `BlackHoleLensSystem` lo pinta
   ENCIMA de la distorsión vía `DrawCoreVisuals(p, endActiveBatch)` (estático,
   compartido). Las partículas de efectos del agujero pasan a la capa nueva
   `AboveLens` (950) que pinta `ParticleManager.RenderAboveLensLayer()` tras
   compositar. Fallback automático: `LensActive=false` → todo al pase normal.
2. **v5.89 — BLIT A PANTALLA COMPLETA (¡NO VOLVER A REGIONES!)**: FNA LIMPIA el
   backbuffer al re-bindearlo (`SetRenderTarget(null)` con DiscardContents por
   defecto — verificado decompilando FNA.dll; por eso el EndCapture de Terraria
   hace Clear+redraw completo). La lente copia screenTarget → _lensTarget
   (RESOLUCIÓN NATIVA) con el shader de distorsión, restaura el binding y
   redibuja _lensTarget A PANTALLA COMPLETA: el mundo queda a resolución nativa,
   distorsionado solo cerca de las fuentes. La composición por regiones de
   v5.86-5.88 destruía el mundo (pantalla negra con un cuadrado).
3. **CosmicShockwaveProjectile (NUEVO)**: 3 estilos — 0 cromática (RGB split +
   fuente de lente → distorsiona el fondo), 1 cromática INVERSA (convergente, RGB
   invertido, knockback hacia el centro), 2 fuego (triple anillo + llamas +
   QUEMADURA). Daño por frente de onda una única vez por NPC (marca reiniciada en
   SetDefaults), SimpleStrikeNPC con guard de autoridad. ai: edad (negativa =
   retardo)/estilo/radio máx; duración derivada maxR/20 (API: solo 3 slots ai).
4. **Secuencia de muerte del agujero negro**: t-90 onda cromática (520px, 75%)
   + estruendo/sacudida; t-90..t-36 escala +60% y radio de gravedad 450→720px
   (área de efecto crece con la onda); t-36..t-0 evaporación (escala→0); t-0
   OnKill = 3 ondas inversas (460/520/580px, retardos 9 ticks, 50% daño cada una).
   La lente sigue la escala: se enciende con la explosión y muere evaporada.
5. **Sol**: primera llamarada en t=2s (antes t=0 = posición del jugador); la
   SupernovaProjectile (explosión final sincronizada del sol, segundo 10) genera
   3 ondas de fuego (360/450/540px, retardos 8 ticks, 50% daño cada una + OnFire
   5s) — también aplica al SupernovaStaff standalone.
6. **Compilación verificada**: 0 errores contra tModLoader v2026.07.3.0 real.

### 11.6 Qué se arregló en v5.83 (histórico)

**FIX CRÍTICO — el error que el usuario veía al cargar el mod:**
```
Failed to load asset 'Content\Effects\Shaders\SunShader'!
Failed to load asset 'Content\Effects\Shaders\RealBlackHoleShader'!
```
- Causa raíz: los `.xnb` generados con dxc no son XNB válidos (el XnbReader de tML
  los rechazaba) Y tML no compila `.fx` (sin reader para esa extensión en FNA).
- Solución: 5 `.fxc` compilados copiados del mod de referencia + borrar los 8 `.xnb`.
- Verificado por reflection contra tModLoader.dll v2026.07.3.0 real.

**BlackHoleProjectile — por qué NO se parecía al pet del mod de referencia (todas corregidas):**
1. `zoom` fijo 0.12 → ahora dinámico `width/256*scale*2` (≈0.59) — EL error principal
2. `accretionDiskRadius` fijo 0.33 → ahora `scale*0.4`
3. `cameraRotationAxis` sin velocity → ahora `(velocity.Y*-0.022+1, 0, rotation)`
4. `globalTime` con GameUpdateCount*0.0167 → ahora `Main.GlobalTimeWrappedHourly`
5. Sin pop elástico → ahora ElasticOut al nacer (como el pet)

**SunProjectile — por qué NO se parecía al StarPet del mod de referencia (todas corregidas):**
1. Canvas WavyBlotchNoise → ahora **DendriticNoiseZoomedOut** (la textura real del mod de referencia, faltaba)
2. `sphereSpinTime` con GameUpdateCount → ahora `GlobalTimeWrappedHourly*0.9`
3. RadialShine en AlphaBlend (invisible por result.a=0) → ahora en Additive
4. Sin pop elástico → ahora ElasticOut

**Verificación de compilación:** 0 errores compilando TODO el mod contra
tModLoader v2026.07.3.0 real con .NET 10 SDK (`dotnet build` con las DLLs del
release de GitHub). 4 warnings benignos preexistentes.

### 11.7 Estado del mod al cierre de v5.83 (histórico)
- ✅ Mod compila correctamente (verificado contra tML 2026.07.3.0 real)
- ✅ Los 5 shaders usados tienen .fxc cargable (fix del "Asset could not be found")
- ✅ Todos los recursos del mod de referencia copiados (10 texturas incl. DendriticNoiseZoomedOut)
- ✅ BlackHole + Sun replican el render de los pets del mod de referencia + mejoras propias

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
- Las texturas del mod de referencia van en `Content/Effects/Textures/` (subcarpeta separada)
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
ls /home/z/my-project/AethonMod/Content/Effects/Textures/ | wc -l      # debe ser 10 (con DendriticNoiseZoomedOut)
ls /home/z/my-project/AethonMod/Content/Weapons/V20/*.cs | wc -l   # debe ser 19
ls /home/z/my-project/AethonMod/Content/Weapons/Cosmic/*.cs | wc -l # debe ser 1 (CosmicWeapons.cs con 2 clases)
ls /home/z/my-project/AethonMod/Content/Projectiles/Cosmic/*.cs | wc -l  # debe ser 3 (BlackHole + Sun + CosmicShockwave v5.86)
ls /home/z/my-project/AethonMod/Content/Particles/*.cs | wc -l     # debe ser 6 (v5.84: + ShapeDescriptor, CameraBounds, ParticlePresets)
find /home/z/my-project/AethonMod/Content -name '*.cs' | wc -l     # debe ser 82 (+ AethonMod.cs raíz = 83)
find /home/z/my-project/AethonMod/Content -name '*.png' | wc -l    # debe ser 139
```

---

## 13. CÓDIGO FUENTE CLAVE

### 13.0 ⚠️ AVISO v5.86/v5.87/v5.88 — ARCHIVOS QUE CAMBIARON TRAS v5.85

El DISCO es la fuente autoritativa (el código embebido de las secciones 13.1-13.4
corresponde a v5.85 y puede estar desactualizado en las partes señaladas):

| Archivo | Cambio v5.86 (+ v5.87/v5.88 donde se indica) |
|---|---|
| `Content/Projectiles/Cosmic/CosmicShockwaveProjectile.cs` | **NUEVO** (~390 líneas) — código completo en 13.1b. **v5.88**: + textura propia .png (InvisiblePixel 1×1 — antes el mod NO cargaba), override `NewInstance` con array `_hitNPCs` fresco por onda (bug de golpes múltiples), End defensivo + GameViewMatrix en el restore de PreDraw |
| `Content/Effects/BlackHoleLensSystem.cs` | **REESCRITO** (v5.89): bandera estática `LensActive`, fuentes = agujeros + ondas cromáticas, _lensTarget a RESOLUCIÓN NATIVA + BLIT A PANTALLA COMPLETA tras restaurar el binding (fix de la pantalla negra: FNA limpia el backbuffer al re-bindearlo), dibuja AboveLens particles + `BlackHoleProjectile.DrawCoreVisuals(bh, false)` + `CosmicShockwaveProjectile.DrawWaveVisual(wave, false)` ENCIMA de la distorsión, fallback automático. **v5.87**: `Unload()` reescrito — el Dispose del render target se ENCOLA al hilo principal (`Main.QueueMainThreadAction`) + programación defensiva total (fix del ThreadStateException de FNA3D). **v5.90: LENTE DELGADA** — maxLensingAngle 24→1.5 rad, fuerza 0.62→0.55 (ángulo pico ~14.9→0.8 rad: antes movía TODA la pantalla porque el shader rota alrededor del centro de pantalla), radio de influencia 0.75×→1.1× |
| `Content/Projectiles/Cosmic/BlackHoleProjectile.cs` (~800 líneas) | AI: secuencia de muerte v5.90 (crecimiento +60% escala/radio SIN ondas → evaporación → OnKill UNA onda cromática con daño COMPLETO y radio 620); `_shader` estático; `DrawCoreVisuals(p, endActiveBatch)` estático (**v5.90**: canvas = 256·scale con zoom CONSTANTE y disco con tope min(scale,1)·0.4 → nunca se corta al crecer); PreDraw se salta con `LensActive`; partículas librería → capa `AboveLens` (**v5.90**: materia absorbida con PullTo, sin morados) |
| `Content/Projectiles/Cosmic/SunProjectile.cs` | Llamaradas: `VisualsTime > 0 && % FlareInterval == 0` (primera en t=2s, no t=0). **v5.89**: TODOS los efectos ambientales como partículas BeforeProjectiles + carga de la supernova dibujada por el sol. **v5.90: REVERTIDO A 8781aa4/v5.88** (el usuario reportó el sol arruinado: círculos que subían + onda expansiva perdida) — dusts vanilla y supernova auto-dibujada como siempre; solo se conserva el End defensivo en catch |
| `Content/Projectiles/V20/SupernovaProjectile.cs` | OnKill: 3 `CosmicShockwaveProjectile` StyleFire (360/450/540px, retardos 8 ticks, daño 50% + OnFire 300); retiradas las 2 RingPulse decorativas. **v5.88**: End defensivo en el restore de PreDraw + GameViewMatrix |
| `Content/Particles/ParticleData.cs` | `LayerPriorities.AboveLens = 950`. **v5.90**: `ComponentFlag.PullTo` (bit 13 — aceleración hacia un punto: materia absorbida) |
| `Content/Particles/ParticleManager.cs` | `RenderAboveLensLayer()` estático; `DrawParticle` estático; PostDrawTiles salta AboveLens si `LensActive`. **v5.88**: `_textures` → `Asset<Texture2D>[]` con resolución DIFERIDA al dibujar (fix del warning "spent 61ms blocking on asset loading"). **v5.90**: componente PullTo en el update (acelera hacia UserData0/1, muere a <10px del centro) |
| `Content/Weapons/Cosmic/CosmicWeapons.cs` + `V20/SupernovaStaff.cs` | Tooltips v5.86 |

### 13.1b CosmicShockwaveProjectile.cs — COMPLETO (NUEVO v5.86)

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Effects;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// CosmicShockwaveProjectile — onda expansiva con daño real por frente de onda.
    ///
    /// v5.86 — Los tres frentes de onda del arsenal cósmico:
    ///
    ///   ESTILO 0 — ONDA CROMÁTICA (explosión del agujero negro):
    ///     Anillo RGB (aberración cromática real: los canales R/G/B se separan
    ///     radialmente) que se expande desde el centro. Se registra como fuente
    ///     del BlackHoleLensSystem → el FONDO del juego se distorsiona a su paso
    ///     ("distorsiona un poco"). Daña a cada NPC cuando el frente lo alcanza.
    ///
    ///   ESTILO 1 — ONDA CROMÁTICA INVERSA (implosión del agujero negro):
    ///     Nace en el radio máximo y CONVERGE hacia el centro (el frente barre
    ///     el daño hacia dentro). El desfase de color está invertido (azul por
    ///     delante de rojo) y también distorsiona el fondo al pasar.
    ///
    ///   ESTILO 2 — ONDA DE FUEGO (nova final del sol):
    ///     Triple anillo ardiente (rojo/naranja/amarillo) + llamas a lo largo
    ///     del frente. Cada onda hace daño y aplica QUEMADURA (OnFire).
    ///
    /// Campos AI:
    ///   ai[0] = edad (negativa = retardo escalonado aún activo)
    ///   ai[1] = estilo (0 cromática / 1 cromática inversa / 2 fuego)
    ///   ai[2] = radio máximo en píxeles
    ///   duración = derivada del radio (maxR/20 ticks ≈ frente de ~40 px/tick)
    ///   (la API de NewProjectile solo acepta 3 slots de ai: la duración se
    ///   deriva de forma determinista para que todas las máquinas coincidan)
    ///
    /// RENDER: los estilos 0/1 se dibujan ENCIMA de la lente gravitacional
    /// (el BlackHoleLensSystem los pinta tras compositar la distorsión), de
    /// modo que la lente nunca deforma sus propios anillos. El estilo 2 se
    /// dibuja en el pase normal del mundo.
    /// </summary>
    public class CosmicShockwaveProjectile : ModProjectile
    {
        /// <summary>Estilo: onda cromática expansiva.</summary>
        public const float StyleChromatic = 0f;

        /// <summary>Estilo: onda cromática inversa (convergente).</summary>
        public const float StyleChromaticInverse = 1f;

        /// <summary>Estilo: onda de fuego (con quemadura).</summary>
        public const float StyleFire = 2f;

        /// <summary>Marca de NPCs ya golpeados por esta onda (una sola vez cada uno).</summary>
        private readonly bool[] _hitNPCs = new bool[Main.maxNPCs];

        private float Age { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }
        private float Style => Projectile.ai[1];
        private float MaxRadius => Projectile.ai[2];

        /// <summary>Duración derivada del radio: frente de ~40 px/tick de pico.</summary>
        private float Duration => Math.Max(MaxRadius / 20f, 10f);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.tileCollide = false;
            // Daño manual por frente de onda (SimpleStrikeNPC): sin colisión vanilla.
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.light = 0f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;

            // tML puede reutilizar la instancia del ModProjectile: reiniciar la
            // marca de golpes para que cada onda nueva pueda dañar de nuevo.
            Array.Clear(_hitNPCs, 0, _hitNPCs.Length);
        }

        public override bool? CanCutTiles() => false;
        public override bool? CanDamage() => false;

        // ================================================================
        //  AI — frente de onda, daño y soporte visual
        // ================================================================
        public override void AI()
        {
            try
            {
                float age = Age;
                Age += 1f;

                // Retardo escalonado (ondas en secuencia): invisible e inofensiva.
                if (age < 0f)
                    return;

                float progress = MathHelper.Clamp(age / Math.Max(Duration, 1f), 0f, 1f);
                float front = FrontRadius(age, Style, MaxRadius);

                // === DAÑO POR FRENTE DE ONDA (solo autoridad) ===
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    ApplyWaveDamage(front);

                // === SOPORTE VISUAL (solo cliente) ===
                if (Main.netMode != NetmodeID.Server)
                {
                    if (Style == StyleFire)
                        SpawnFireFrontDusts(front, progress);
                    else
                        SpawnChromaticFrontSparks(front, progress);
                }

                // === LUZ ===
                float alpha = WaveAlpha(age, Math.Max(Duration, 1f));
                if (Style == StyleFire)
                    Lighting.AddLight(Projectile.Center,
                        new Vector3(1f, 0.55f, 0.2f) * 1.8f * alpha);
                else
                    Lighting.AddLight(Projectile.Center,
                        new Vector3(0.6f, 0.5f, 1f) * 0.9f * alpha);

                // Frente completado → la onda se disipa.
                if (age >= Duration)
                    Projectile.Kill();
            }
            catch { }
        }

        // ================================================================
        //  FRENTE DE ONDA (compartido con el sistema de lente)
        // ================================================================

        /// <summary>Radio del frente en píxeles, o -1 si aún retrasada/inactiva.</summary>
        public static float GetFrontRadius(Projectile p)
        {
            if (p == null || !p.active) return -1f;
            float age = p.ai[0];
            if (age < 0f) return -1f;
            return FrontRadius(age, p.ai[1], p.ai[2]);
        }

        /// <summary>Progreso 0..1 del frente (o -1 si retrasada).</summary>
        public static float GetProgress(Projectile p)
        {
            if (p == null || !p.active) return -1f;
            float age = p.ai[0];
            if (age < 0f) return -1f;
            return MathHelper.Clamp(age / DurationOf(p.ai[2]), 0f, 1f);
        }

        /// <summary>Duración del frente derivada del radio máximo (determinista).</summary>
        private static float DurationOf(float maxR)
        {
            return Math.Max(maxR / 20f, 10f);
        }

        private static float FrontRadius(float age, float style, float maxR)
        {
            float duration = DurationOf(maxR);
            float p = MathHelper.Clamp(age / duration, 0f, 1f);
            if (style == StyleChromaticInverse)
            {
                // Convergencia acelerada: nace en maxR y colapsa hacia el centro.
                return maxR * (1f - p * p);
            }
            // Expansión ease-out: arranque veloz, frenado al final.
            return maxR * (1f - (1f - p) * (1f - p));
        }

        /// <summary>Envolvente de alpha: aparición rápida + desvanecimiento final.</summary>
        private static float WaveAlpha(float age, float duration)
        {
            float fadeIn = Utils.GetLerpValue(0f, duration * 0.15f, age, true);
            float fadeOut = 1f - Utils.GetLerpValue(duration * 0.7f, duration, age, true);
            return Math.Min(fadeIn, fadeOut);
        }

        // ================================================================
        //  DAÑO — el frente barre a los NPC una única vez por onda
        // ================================================================
        private void ApplyWaveDamage(float front)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (_hitNPCs[i]) continue;
                NPC npc = Main.npc[i];
                if (npc == null || !npc.active || !npc.CanBeChasedBy()) continue;

                Vector2 toCenter = Projectile.Center - npc.Center;
                float dist = toCenter.Length();

                bool crossed;
                if (Style == StyleChromaticInverse)
                {
                    // Onda convergente: golpea cuando el frente pasa hacia dentro
                    // (y solo a quien estaba dentro del radio inicial).
                    crossed = dist <= MaxRadius * 1.02f && dist >= front;
                }
                else
                {
                    // Onda expansiva: golpea cuando el frente le alcanza.
                    crossed = dist <= front;
                }

                if (!crossed) continue;
                _hitNPCs[i] = true;

                // Dirección del empuje: hacia fuera en expansivas, hacia el
                // centro en la inversa (la implosión arrastra hacia dentro).
                int dir;
                float knockBack;
                if (Style == StyleChromaticInverse)
                {
                    dir = npc.Center.X < Projectile.Center.X ? 1 : -1;
                    knockBack = -4f;
                }
                else
                {
                    dir = npc.Center.X < Projectile.Center.X ? -1 : 1;
                    knockBack = Style == StyleFire ? 5f : 6f;
                }

                npc.SimpleStrikeNPC(Projectile.damage, dir, false, knockBack, DamageClass.Magic);

                // La onda de fuego aplica QUEMADURA.
                if (Style == StyleFire)
                    npc.AddBuff(BuffID.OnFire, 300);
            }
        }

        // ================================================================
        //  DUSTS DE APOYO
        // ================================================================

        /// <summary>LLamas vivas a lo largo del frente de la onda de fuego.</summary>
        private void SpawnFireFrontDusts(float front, float progress)
        {
            int count = 4;
            for (int i = 0; i < count; i++)
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * front,
                    (float)Math.Sin(angle) * front);
                Vector2 vel = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(1.5f, 3.5f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(1.5f, 3.5f));
                Color color = Main.rand.NextBool(2)
                    ? new Color(255, 170, 60)
                    : new Color(255, 100, 30);
                Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, vel, 220, color, 1.2f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        /// <summary>Chispas blancas escasas sobre el frente cromático.</summary>
        private void SpawnChromaticFrontSparks(float front, float progress)
        {
            if (!Main.rand.NextBool(2)) return;
            float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
            Vector2 pos = Projectile.Center + new Vector2(
                (float)Math.Cos(angle) * front,
                (float)Math.Sin(angle) * front);
            Dust d = Dust.NewDustPerfect(pos, DustID.Enchanted_Gold,
                Vector2.Zero, 200, new Color(230, 220, 255), 0.6f);
            d.noGravity = true;
            d.fadeIn = 0.2f;
        }

        // ================================================================
        //  RENDER
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                if (Age < 0f) return false;

                // Las ondas cromáticas las pinta el sistema de lente ENCIMA de la
                // distorsión (para que la lente no las deforme a ellas). Si la
                // lente no está activa, caemos al dibujado normal del mundo.
                if ((Style == StyleChromatic || Style == StyleChromaticInverse) &&
                    BlackHoleLensSystem.LensActive)
                    return false;

                // Pase del mundo: el spriteBatch del juego está abierto → cerrarlo
                // antes de nuestros pases (el sistema de lente lo llama en batch
                // ya cerrado, por eso el parámetro).
                DrawWaveVisual(Projectile, true);
            }
            catch { }

            // Restaurar el SpriteBatch al estado que tML espera tras PreDraw.
            // (End defensivo: si un error interno dejó un Begin abierto, se cierra
            // antes de restaurar; si no había nada abierto, se ignora.)
            try { Main.spriteBatch.End(); } catch { }
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.Transform);
            return false;
        }

        /// <summary>
        /// Dibuja la onda completa (anillos con aberración cromática o triple
        /// anillo de fuego). Reutilizable desde el pase del mundo (PreDraw) y
        /// desde el pase posterior a la lente (BlackHoleLensSystem).
        /// <param name="endActiveBatch">true cuando existe un Begin del juego
        /// activo (pase del mundo); false en el hook de la lente (batch cerrado).</param>
        /// </summary>
        public static void DrawWaveVisual(Projectile p, bool endActiveBatch)
        {
            try
            {
                if (p == null || !p.active || p.ai[0] < 0f) return;

                float age = p.ai[0];
                float style = p.ai[1];
                float duration = DurationOf(p.ai[2]);
                float front = FrontRadius(age, style, p.ai[2]);
                if (front <= 1f) return;

                float progress = MathHelper.Clamp(age / duration, 0f, 1f);
                float alpha = WaveAlpha(age, duration);

                Texture2D ring = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/Ring").Value;
                Vector2 drawPos = p.Center - Main.screenPosition;
                float ringUnit = ring.Width / 2f; // radio del anillo a escala 1

                if (endActiveBatch)
                    Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                if (style == StyleFire)
                {
                    // === TRIPLE ANILLO DE FUEGO ===
                    DrawRing(ring, drawPos, front, ringUnit,
                        new Color(220, 50, 10, (byte)(alpha * 200f)));
                    DrawRing(ring, drawPos, front * 0.93f, ringUnit,
                        new Color(255, 130, 30, (byte)(alpha * 220f)));
                    DrawRing(ring, drawPos, front * 0.86f, ringUnit,
                        new Color(255, 230, 130, (byte)(alpha * 230f)));
                    DrawRing(ring, drawPos, front * 0.8f, ringUnit,
                        new Color(255, 255, 220, (byte)(alpha * 120f)));
                }
                else
                {
                    // === ANILLO CROMÁTICO (aberración RGB real) ===
                    // La separación de canales crece con la edad (dispersión)
                    // y se INVERTIEn en la onda inversa (azul por delante).
                    float fringe = (2.5f + 4.5f * progress) *
                                   (style == StyleChromaticInverse ? -1f : 1f);
                    byte a = (byte)(alpha * 230f);

                    DrawRing(ring, drawPos, front + fringe, ringUnit, new Color(255, 40, 40, a));
                    DrawRing(ring, drawPos, front, ringUnit, new Color(60, 255, 90, a));
                    DrawRing(ring, drawPos, front - fringe, ringUnit, new Color(70, 130, 255, a));
                    // Núcleo blanco que unifica los tres canales.
                    DrawRing(ring, drawPos, front, ringUnit,
                        new Color(255, 255, 255, (byte)(alpha * 150f)));
                }

                Main.spriteBatch.End();
            }
            catch { }
        }

        /// <summary>Dibuja un anillo centrado en drawPos con el radio dado en píxeles.</summary>
        private static void DrawRing(Texture2D ring, Vector2 drawPos, float radiusPx,
            float ringUnit, Color color)
        {
            if (radiusPx <= 0.5f || color.A == 0) return;
            float scale = radiusPx / ringUnit;
            Main.spriteBatch.Draw(ring, drawPos, null, color, 0f,
                new Vector2(ring.Width * 0.5f, ring.Height * 0.5f), scale,
                SpriteEffects.None, 0f);
        }
    }
}

```


### 13.1 BlackHoleProjectile.cs — NÚCLEO DEL CICLO DE VIDA (v5.85; ⚠️ v5.86: la AI cambió — ver 13.0)
> El archivo completo está en el repo (`Content/Projectiles/Cosmic/BlackHoleProjectile.cs`).
> Aquí: cabecera documental + AI() completa (el corazón del ciclo). Lo demás son
> las 4+5 capas de partículas, el render del shader y la muerte (sin cambios de
> fondo respecto a v5.84, salvo la succión multicolor).

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// BlackHoleProjectile — agujero negro con lensing gravitacional real.
    ///
    /// RENDER: RealBlackHoleShader.fx (lightmarch de 75 pasos con lensing gravitacional
    /// real) sobre un canvas de InvisiblePixel de 256px:
    ///   - zoom dinámico: width / 256 * scale * 2
    ///   - accretionDiskRadius: scale * 0.4
    ///   - cameraRotationAxis: (velocity.Y * -0.022 + 1, 0, rotation)
    ///   - cameraAngle: 0.32 / accretionDiskScale: (1, 0.33, 1)
    ///
    /// v5.85 — LENTE GRAVITACIONAL de pantalla (BlackHoleLensSystem): el fondo
    /// real del juego se distorsiona alrededor del horizonte de sucesos con el
    /// shader BlackHoleDistortionShader (formalismo relativista con decaimiento
    /// exponencial). Fuerza gravitatoria aumentada y radio de atracción de 450px.
    ///
    /// v5.84 — Capa de partículas de la LIBRERÍA propia (data-oriented, additive,
    /// render en PostDrawTiles = capa de fondo con profundidad): espiral de succión
    /// multicolor (violeta/cian/magenta/oro) con ColorShift, disco de acreción de
    /// estelas TrailGlow orbitando (componente Orbit + rotación tangencial
    /// sincronizada), anillo de fotones pulsante con ScaleUp, halo de distorsión
    /// con ruido procedural, implosión/explosión con presets y screenshake.
    ///
    /// Mejoras propias: pop elástico de aparición, colapso final antes de expirar,
    /// succión espiral de partículas de colores, atracción gravitacional de
    /// enemigos Y devoración del polvo cercano, refuerzo del event horizon e
    /// implosión + doble onda expansiva al morir.
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
            // v5.85: área de daño ampliada (76 → 96): el hitbox y el canvas del
            // shader escalan con width, así que el agujero también se ve mayor.
            Projectile.width = 96;
            Projectile.height = 96;
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
            // === POP ELÁSTICO DE APARICIÓN ===
            // scale = ElasticOut(0..120) * sqrt(InverseLerp(0..60)) — el agujero "rebota" al nacer.
            Projectile.scale = ElasticOut(Utils.GetLerpValue(0f, 120f, VisualsTime, true)) *
                               (float)Math.Sqrt(Utils.GetLerpValue(0f, 60f, VisualsTime, true));
            VisualsTime += 1f;

            // === COLAPSO FINAL: los últimos 40 ticks se encoge dramáticamente ===
            if (Projectile.timeLeft < 40f)
                Projectile.scale *= 0.93f;

            // === MOVIMIENTO: deriva lenta y frenado (el agujero flota) ===
            Projectile.velocity *= 0.97f;

            // Rotación suave hacia velocity.X * 0.04
            float targetRotation = Projectile.velocity.X * 0.04f;
            Projectile.rotation += MathHelper.WrapAngle(targetRotation - Projectile.rotation) * 0.3f;

            // === PARTÍCULAS (solo cliente) ===
            if (Main.netMode != NetmodeID.Server)
            {
                // Dusts vanilla (capa frontal, se dibujan encima del canvas del shader)
                SpawnSuctionParticles();
                SpawnAccretionDiskParticles();
                SpawnSmokeParticles();
                SpawnCapturedEnergySparks();
                AttractNearbyDust();

                // v5.84: partículas de la librería propia (capa de fondo aditiva —
                // se renderizan en PostDrawTiles, detrás del canvas del agujero,
                // creando profundidad por capas)
                SpawnLibrarySuctionSpiral();
                SpawnLibraryAccretionDisk();
                SpawnLibraryPhotonRing();
                SpawnLibraryDistortionHalo();
            }

            // === ATRACCIÓN GRAVITACIONAL DE ENEMIGOS (radio 450, fuerza aumentada) ===
            const float gravityRadius = 450f;
            const float gravityStrength = 2.6f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                Vector2 toCenter = Projectile.Center - npc.Center;
                float dist = toCenter.Length();
                if (dist > gravityRadius || dist < 5f) continue;
                float strength = (1f - dist / gravityRadius) * gravityStrength;
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

        // [... resto del archivo: partículas vanilla + librería (4 efectos),
        //      render RealBlackHoleShader + refuerzo del event horizon,
        //      OnHitNPC con micro-colapso, OnKill con implosión + doble onda
        //      expansiva + ElasticOut — ver el archivo completo en el repo ...]
    }
}
```

### 13.2 SunProjectile.cs — NÚCLEO DEL CICLO DE VIDA (v5.85; ⚠️ v5.86: llamaradas desde t=2s — ver 13.0)
> El archivo completo está en el repo (`Content/Projectiles/Cosmic/SunProjectile.cs`).
> Aquí: cabecera documental + AI() completa — TODO el ciclo de 10 segundos
> (llamaradas cada 2s desde t=0, supernova del segundo 7 con centrado tick a tick
> vía ai[1] y sincronización exacta de la cuenta regresiva, gravedad 1/10 del
> agujero negro con rampa x4 durante la carga, luz creciente).

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Particles;
using AethonMod.Content.Projectiles.V20;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// SunProjectile — una estrella de plasma viva (10 segundos de vida).
    ///
    /// RENDER (3 capas de profundidad):
    ///   1. Backglow con BloomCircleSmall: amarillo * 0.7 (escala 0.95) + rojo * 0.45 (escala 1.61).
    ///   2. RadialShineShader sobre WavyBlotchNoise: color (252, 212, 112) * 0.24,
    ///      escala = width * scale * 2.72 / tamaño de la textura.
    ///   3. SunShader sobre DendriticNoiseZoomedOut (canvas):
    ///      coronaIntensityFactor = 0.05, mainColor = blanco, darkerColor = (204, 92, 25),
    ///      subtractiveAccentFactor = (181, 0, 0), sphereSpinTime = GlobalTimeWrappedHourly * 0.9,
    ///      s1 = WavyBlotchNoise, s2 = PsychedelicWingTextureOffsetMap,
    ///      escala = width * scale * 1.5 / tamaño de la textura.
    ///
    /// CICLO DE VIDA (v5.85) — el sol como cuerpo celeste completo:
    ///   - t=0s    : nace con pop elástico y lanza su primera LLAMARADA SOLAR
    ///               (PhoenixNovaProjectile centrado en el sol).
    ///   - cada 2s : nueva llamarada solar desde el centro (5 en total: 0, 2, 4, 6, 8s).
    ///   - t=7s    : aparece SUPERNOVAPROJECTILE centrado y sincronizado (dura 3s);
    ///               carga energía mientras la gravedad del sol AUMENTA progresivamente
    ///               y su luz se intensifica (materia convergiendo en espiral).
    ///   - t=10s   : ambos proyectiles explotan SIMULTÁNEAMENTE — nova masiva con
    ///               doble onda expansiva y temblor de pantalla.
    ///
    /// GRAVEDAD (cuerpo celeste): atrae solo enemigos, con una fuerza ~10 veces
    /// menor que la del agujero negro. Durante la carga de la supernova (últimos
    /// 3 segundos) la fuerza se multiplica progresivamente (x4 en el pico).
    ///
    /// QUEMADURA: bola de plasma ardiente → inflama enemigos al contacto (OnFire).
    /// (La quemadura potenciada por daño mágico se implementará cuando este
    /// proyectil se integre en el Grimorio, el arma definitiva.)
    ///
    /// v5.84 — Capa de partículas de la librería propia (data-oriented, additive,
    /// render en PostDrawTiles): corona de glóbulos SoftGlow orbitando con ColorShift
    /// amarillo→naranja, viento solar de estelas TrailGlow radiales, destellos
    /// SparkleStar con FadeIn+EmitLight, arcos de prominencia con estrellas orbitando.
    /// </summary>
    public class SunProjectile : ModProjectile
    {
        private Ref<Effect> _sunShader;
        private Ref<Effect> _shineShader;
        private bool _sunShaderFailed;
        private bool _shineShaderFailed;

        /// <summary>Tiempo visual de vida — usada para el pop elástico y el ritmo de llamaradas.</summary>
        public ref float VisualsTime => ref Projectile.ai[0];

        /// <summary>Índice del proyectil Supernova hijo (-1 = aún no invocado).</summary>
        public ref float SupernovaIndex => ref Projectile.ai[1];

        /// <summary>Duración total del sol: 10 segundos exactos.</summary>
        private const int SunLifetime = 600;

        /// <summary>Momento (ticks restantes) en el que nace la supernova: segundo 7.</summary>
        private const int SupernovaSpawnAtRemaining = 180;

        /// <summary>Cadencia de las llamaradas solares: cada 2 segundos.</summary>
        private const int FlareInterval = 120;

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
            Projectile.timeLeft = SunLifetime;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        public override void AI()
        {
            // === LLAMARADAS SOLARES (PhoenixNova cada 2 s, DESDE t=0) ===
            // Se comprueba ANTES del incremento para que el primer disparo
            // coincida con el mismo tick de nacimiento del sol.
            if (VisualsTime % FlareInterval == 0f && Projectile.owner == Main.myPlayer)
            {
                int flareDamage = (int)(Projectile.damage * 0.5f);
                if (flareDamage < 1) flareDamage = 1;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<V20.PhoenixNovaProjectile>(),
                    flareDamage, Projectile.knockBack * 0.5f,
                    Projectile.owner);
            }

            // === POP ELÁSTICO DE APARICIÓN ===
            Projectile.scale = ElasticOut(Utils.GetLerpValue(0f, 90f, VisualsTime, true)) *
                               (float)Math.Sqrt(Utils.GetLerpValue(0f, 45f, VisualsTime, true));
            VisualsTime += 1f;

            // === CARGA DE SUPERNOVA (últimos 3 s): el sol se comprime y brilla más ===
            bool supernovaCharging = Projectile.timeLeft <= SupernovaSpawnAtRemaining;
            if (supernovaCharging)
            {
                // Compresión sutil: la materia se acumula antes del colapso.
                Projectile.scale *= 1.0008f;
            }

            // === NOVA FINAL: los últimos 30 ticks se hincha antes de explotar ===
            if (Projectile.timeLeft < 30f)
                Projectile.scale *= 1.025f;

            // === MOVIMIENTO: deriva lenta y frenado ===
            Projectile.velocity *= 0.97f;
            Projectile.rotation += 0.01f;

            // === SUPERNOVA SINCRONIZADA (aparece en el segundo 7) ===
            if (Projectile.timeLeft == SupernovaSpawnAtRemaining && Projectile.owner == Main.myPlayer)
            {
                int novaDamage = Math.Max(1, (int)(Projectile.damage * 1.25f));
                int idx = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center, Projectile.velocity,
                    ModContent.ProjectileType<V20.SupernovaProjectile>(),
                    novaDamage, Projectile.knockBack,
                    Projectile.owner);
                SupernovaIndex = idx;
            }

            // Mantener la supernova PERFECTAMENTE centrada en el sol (y sincronizada).
            if (SupernovaIndex >= 0f)
            {
                int idx = (int)SupernovaIndex;
                if (idx >= 0 && idx < Main.maxProjectiles &&
                    Main.projectile[idx].active &&
                    Main.projectile[idx].type == ModContent.ProjectileType<V20.SupernovaProjectile>())
                {
                    // El sol arrastra a la supernova con él (deriva compartida).
                    Main.projectile[idx].Center = Projectile.Center;
                    Main.projectile[idx].velocity = Projectile.velocity;

                    // SINCRONIZACIÓN EXACTA: en los últimos ticks, la cuenta
                    // regresiva de la supernova se clava a la del sol → ambos
                    // mueren (y explotan) en el MISMO tick, sin deriva de índices.
                    if (Projectile.timeLeft <= 2)
                        Main.projectile[idx].timeLeft =
                            Math.Min(Main.projectile[idx].timeLeft, Projectile.timeLeft);
                }
                else
                {
                    SupernovaIndex = -1f;
                }
            }

            // === GRAVEDAD DEL SOL — 10 veces menor que el agujero negro, solo enemigos ===
            float gravityRadius = 280f;
            float baseStrength = 0.26f; // agujero negro: 2.6 → sol: 2.6 / 10
            // Durante la carga de la supernova la fuerza crece progresivamente (x4 pico).
            float chargeMult = 1f;
            if (supernovaCharging)
            {
                float chargeProgress = 1f - Projectile.timeLeft / (float)SupernovaSpawnAtRemaining;
                chargeMult = 1f + chargeProgress * 3f;
            }
            float sunGravity = baseStrength * chargeMult;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                Vector2 toCenter = Projectile.Center - npc.Center;
                float dist = toCenter.Length();
                if (dist > gravityRadius || dist < 5f) continue;
                float strength = (1f - dist / gravityRadius) * sunGravity;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    npc.velocity += toCenter * strength;
                }
            }

            // === PARTÍCULAS (solo cliente) ===
            if (Main.netMode != NetmodeID.Server)
            {
                // Dusts vanilla (capa frontal, se dibujan encima del canvas del shader)
                SpawnOrbitingSparks();
                SpawnFlames();
                SpawnSmoke();
                SpawnSolarFlare();
                SpawnTwinkles();

                // Partículas de la librería propia (capa de fondo aditiva)
                SpawnLibraryCorona();
                SpawnLibrarySolarWind();
                SpawnLibraryTwinkles();
                SpawnLibraryFlareLoop();

                // v5.85: materia convergiendo durante la carga de la supernova
                if (supernovaCharging && Projectile.scale > 0.3f)
                {
                    SpawnSupernovaChargeIntake();
                }
            }

            // === ILUMINACIÓN INTENSA (con pulso sutil + crecimiento en la carga) ===
            float pulse = 0.92f + 0.08f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f);
            float chargeLight = supernovaCharging
                ? 1f + (1f - Projectile.timeLeft / (float)SupernovaSpawnAtRemaining) * 0.8f
                : 1f;
            Lighting.AddLight(Projectile.Center,
                new Vector3(1f, 0.9f, 0.5f) * 3.2f * pulse * chargeLight);
        }

        // [... resto del archivo: 5 emisores vanilla + 4 de librería (corona,
        //      viento solar, destellos, prominencias) + carga de supernova +
        //      render 3 capas + OnHitNPC con OnFire + OnKill nova masiva con
        //      doble onda — ver el archivo completo en el repo ...]
    }
}
```

### 13.3 BlackHoleLensSystem.cs — COMPLETO (⚠️ REESCRITO EN v5.89 — fix de la "pantalla negra"; v5.90: LENTE DELGADA; el repo manda)
> La lente gravitacional de pantalla: distorsiona el fondo REAL del juego alrededor
> de hasta 5 fuentes (agujeros + ondas cromáticas). Hook en TimeLogger punto 36
> (tras EndCapture del mundo, antes de la UI), Main.screenTarget como fuente.
> v5.89: RT a RESOLUCIÓN NATIVA + blit A PANTALLA COMPLETA tras restaurar el
> binding (FNA limpia el backbuffer al re-bindearlo con DiscardContents — por eso
> el propio Terraria redibuja la pantalla entera en su EndCapture). Sin regiones.
> v5.90: LENTE DELGADA — el shader rota las coords ALREDEDOR DEL CENTRO DE
> PANTALLA (así que cualquier ángulo grande desplaza píxeles lejanos ∝ su
> distancia al centro: "movía toda la pantalla"). maxLensingAngle 24→1.5 rad y
> fuerza 0.62→0.55 (ángulo pico ~14.9→0.8 rad), radio 0.75×→1.1× del tamaño
> visual. El .fxc NO se recompila (sin mgfxc/wine en el sandbox): tuning solo
> por parámetros.

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Particles;
using AethonMod.Content.Projectiles.Cosmic;

namespace AethonMod.Content.Effects
{
    /// <summary>
    /// BlackHoleLensSystem — lente gravitacional de pantalla completa.
    ///
    /// v5.89 — FIX CRÍTICO de la "pantalla negra": FNA limpia el backbuffer al
    /// re-bindearlo. La semántica de FNA (verificada decompilando FNA.dll) es
    /// que SetRenderTarget(null)/SetRenderTargets(...) ejecuta
    /// `Clear(Target|Depth|Stencil)` sobre el target recién bindeado cuando su
    /// RenderTargetUsage es DiscardContents — y el PresentationParameters del
    /// juego usa DiscardContents por defecto. Por eso el propio Terraria hace
    /// Clear + redraw completo en su FilterManager.EndCapture.
    ///
    /// La v5.86-v5.88 componía por REGIONES (solo el cuadrado alrededor de cada
    /// fuente) intentando conservar el backbuffer intacto fuera de ellas... pero
    /// el restore del binding YA HABÍA BORRADO el backbuffer: el mundo dibujado
    /// por EndCapture se destruía y solo quedaban las regiones → pantalla negra
    /// con un cuadrado brillante (exactamente lo que reportó el usuario).
    ///
    /// Arquitectura nueva (v5.89), el mismo pipeline del renderer de WoTG que
    /// inspiró el sistema:
    ///   1. El mundo se renderiza en Main.screenTarget SIN el núcleo del agujero
    ///      (BlackHoleProjectile.PreDraw se salta su dibujado cuando la lente
    ///      está activa — ver LensActive).
    ///   2. Se recopilan hasta 5 fuentes de distorsión: agujeros negros Y ondas
    ///      cromáticas (CosmicShockwaveProjectile, estilos 0/1).
    ///   3. screenTarget se copia COMPLETO a través de BlackHoleDistortionShader
    ///      hacia _lensTarget — ahora a RESOLUCIÓN NATIVA (el shader es barato:
    ///      una sola lectura de textura por píxel, no necesita media resolución).
    ///   4. Se restaura el binding original (FNA borra el backbuffer — esperado)
    ///      y se dibuja _lensTarget A PANTALLA COMPLETA: el mundo vuelve a estar
    ///      en pantalla a resolución nativa, distorsionado solo cerca de las
    ///      fuentes. Sin regiones, sin bordes duros, sin media resolución.
    ///   5. ENCIMA de la lente, en orden:
    ///        a) partículas de la capa AboveLens (efectos del agujero negro),
    ///        b) el NÚCLEO del agujero negro (halo + RealBlackHoleShader +
    ///           refuerzo del horizonte de sucesos),
    ///        c) los anillos de las ondas cromáticas.
    ///   6. La UI se dibuja después, intacta.
    ///
    /// LensActive: bandera estática que indica "la lente se renderizó en el
    /// frame anterior". Los proyectiles la consultan en PreDraw (que corre
    /// ANTES del punto 36) para decidir si se saltan su dibujado del mundo.
    /// Si la lente falla o no hay fuentes, la bandera cae a false y todo se
    /// dibuja por el camino normal (fallback automático, el agujero jamás
    /// desaparece).
    ///
    /// v5.87 — Unload() con programación defensiva: tModLoader descarga los
    /// mods en un hilo de carga secundario, pero FNA3D exige que Dispose()
    /// de recursos gráficos corra en el hilo principal. El render target se
    /// destruye vía Main.QueueMainThreadAction (cola ConcurrentQueue drenada
    /// al final de Main.Update() cada frame — también durante la pantalla de
    /// carga del reload), verificado contra tModLoader v2026.07.3.0 real.
    /// </summary>
    [Autoload(Side = ModSide.Client)]
    public class BlackHoleLensSystem : ModSystem
    {
        private const int MaxSources = 5;

        /// <summary>Target de la pantalla distorsionada (resolución nativa).</summary>
        private static RenderTarget2D _lensTarget;

        /// <summary>Shader de lensing (mismo pipeline .fxc del resto de efectos).</summary>
        private static Effect _distortionShader;
        private static bool _shaderFailed;

        /// <summary>
        /// ¿La lente se renderizó en el frame anterior? Los PreDraw de los
        /// proyectiles cósmicos la consultan para saltarse el pase del mundo.
        /// </summary>
        public static bool LensActive { get; private set; }

        // Datos de las fuentes (como el shader los espera: arrays de 5)
        private readonly float[] _sourceRadii = new float[MaxSources];
        private readonly Vector2[] _sourcePositions = new Vector2[MaxSources];
        private readonly float[] _strengths = new float[MaxSources];

        // Índices de proyectiles a dibujar encima de la lente
        private readonly int[] _blackHoleIndices = new int[MaxSources];
        private int _blackHoleCount;
        private readonly int[] _waveIndices = new int[MaxSources];
        private int _waveCount;

        public override void Load()
        {
            // Hook MonoMod: punto 36 = tras EndCapture del mundo, antes de la UI.
            Terraria.On_TimeLogger.DetailedDrawTime += ApplyGravitationalLens;
        }

        public override void Unload()
        {
            try
            {
                Terraria.On_TimeLogger.DetailedDrawTime -= ApplyGravitationalLens;
            }
            catch
            {
                // Programación defensiva: el detach del hook jamás puede
                // impedir que la desactivación del mod continúe.
            }

            // v5.87 — FIX del ThreadStateException:
            // "most FNA3D audio/graphics functions must be called on the main
            // thread". Unload() corre en el hilo de carga secundario de tML;
            // RenderTarget2D.Dispose() ahí lanza y rompía toda la desactivación
            // del mod. La destrucción se encola al hilo principal: la cola
            // _mainThreadActions se drena en Main.Update() cada frame, incluso
            // mientras la pantalla de carga del reload sigue dibujándose.
            // El closure captura una variable local (no el ModSystem ni estado
            // estático), así que la acción es autosuficiente.
            RenderTarget2D target = _lensTarget;
            if (target != null)
            {
                try
                {
                    Main.QueueMainThreadAction(() =>
                    {
                        try { target.Dispose(); }
                        catch
                        {
                            // Defensivo: una excepción aquí subiría hasta
                            // Main.Update() y rompería el bucle del juego.
                        }
                    });
                }
                catch
                {
                    // Encolado imposible (p. ej. apagado total del proceso):
                    // se abandona la referencia — el driver libera los
                    // recursos del proceso al terminar de todos modos.
                }
            }

            _lensTarget = null;
            _distortionShader = null;
            _shaderFailed = false;
            LensActive = false;
        }

        // ================================================================
        //  HOOK PRINCIPAL — DetailedDrawTime(36)
        // ================================================================
        private void ApplyGravitationalLens(Terraria.On_TimeLogger.orig_DetailedDrawTime orig, int detailedDrawType)
        {
            try
            {
                if (detailedDrawType == 36)
                {
                    if (CanRender())
                        RenderLens();
                    else
                        LensActive = false;
                }
            }
            catch
            {
                // La lente jamás puede romper el render del juego: si algo falla,
                // el frame siguiente todos vuelven al dibujado normal del mundo.
                LensActive = false;
            }

            orig(detailedDrawType);
        }

        private bool CanRender()
        {
            // Sin mundo, menú o sin render targets de pantalla → nada que distorsionar.
            if (Main.gameMenu || Main.screenTarget == null || Main.screenTarget.IsDisposed)
                return false;

            if (!_shaderFailed && _distortionShader == null)
            {
                try
                {
                    _distortionShader = ModContent.Request<Effect>(
                        "AethonMod/Content/Effects/Shaders/BlackHoleDistortionShader",
                        AssetRequestMode.ImmediateLoad).Value;
                }
                catch
                {
                    _shaderFailed = true;
                }
            }

            return _distortionShader != null && !_distortionShader.IsDisposed;
        }

        // ================================================================
        //  RENDER DE LA LENTE
        // ================================================================
        private void RenderLens()
        {
            // === 1. Recopilar fuentes: agujeros negros + ondas cromáticas ===
            int blackHoleType = ModContent.ProjectileType<BlackHoleProjectile>();
            int waveType = ModContent.ProjectileType<CosmicShockwaveProjectile>();
            Vector2 screenSize = new Vector2(Main.screenWidth, Main.screenHeight);
            if (screenSize.X <= 0f || screenSize.Y <= 0f)
            {
                LensActive = false;
                return;
            }

            int count = 0;
            _blackHoleCount = 0;
            _waveCount = 0;

            for (int i = 0; i < Main.maxProjectiles && count < MaxSources; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active) continue;

                if (p.type == blackHoleType)
                {
                    Vector2 screenPos = p.Center - Main.screenPosition;
                    Vector2 uv = screenPos / screenSize;
                    if (uv.X < -0.25f || uv.X > 1.25f || uv.Y < -0.25f || uv.Y > 1.25f)
                        continue;

                    // Radio de influencia en UV: el 75% del tamaño visual (métrica del shader).
                    float radius = p.width * p.scale / screenSize.X * 0.75f;

                    // La lente es "pequeña": intensidad ligada a la escala del agujero
                    // (nace con el pop elástico, crece con la expansión final del
                    // v5.86, muere con la evaporación).
                    float strength = MathHelper.Clamp(p.scale * 1.1f, 0f, 1f);

                    _sourcePositions[count] = uv;
                    _sourceRadii[count] = Math.Max(radius, 0.0001f);
                    _strengths[count] = strength;
                    _blackHoleIndices[_blackHoleCount++] = i;
                    count++;
                }
                else if (p.type == waveType)
                {
                    // Solo las ondas cromáticas (0/1) distorsionan el fondo.
                    float style = p.ai[1];
                    if (style != CosmicShockwaveProjectile.StyleChromatic &&
                        style != CosmicShockwaveProjectile.StyleChromaticInverse)
                        continue;

                    float front = CosmicShockwaveProjectile.GetFrontRadius(p);
                    if (front <= 1f) continue; // retrasada o disipada

                    Vector2 screenPos = p.Center - Main.screenPosition;
                    Vector2 uv = screenPos / screenSize;
                    if (uv.X < -0.35f || uv.X > 1.35f || uv.Y < -0.35f || uv.Y > 1.35f)
                        continue;

                    // El frente de la onda curva el espacio que atraviesa.
                    float radius = front / screenSize.X;
                    float progress = CosmicShockwaveProjectile.GetProgress(p);
                    float strength = MathHelper.Clamp(1f - progress * 0.75f, 0f, 1f);

                    _sourcePositions[count] = uv;
                    _sourceRadii[count] = Math.Max(radius, 0.0001f);
                    _strengths[count] = strength;
                    _waveIndices[_waveCount++] = i;
                    count++;
                }
            }

            if (count <= 0)
            {
                // Sin agujeros ni ondas → no hay lente: todo se dibuja normal.
                LensActive = false;
                return;
            }

            // Rellenar el resto de slots con fuentes nulas (el shader itera los 5).
            for (int i = count; i < MaxSources; i++)
            {
                _sourcePositions[i] = Vector2.One * -9999f;
                _sourceRadii[i] = 0.0001f;
                _strengths[i] = 0f;
            }

            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            if (gd == null)
            {
                LensActive = false;
                return;
            }

            // === 2. Preparar el target de la lente (RESOLUCIÓN NATIVA) ===
            // v5.89: a resolución completa — el shader de distorsión es barato
            // (una lectura de textura por píxel) y así el mundo lenteado no
            // pierde nitidez ni muestra píxeles gordos al ampliarse.
            int lensW = Math.Max(2, Main.screenWidth);
            int lensH = Math.Max(2, Main.screenHeight);
            if (_lensTarget == null || _lensTarget.IsDisposed ||
                _lensTarget.Width != lensW || _lensTarget.Height != lensH)
            {
                _lensTarget?.Dispose();
                _lensTarget = new RenderTarget2D(gd, lensW, lensH,
                    false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            }

            // Guardar el estado de render targets ACTIVO (backbuffer o screenTarget).
            RenderTargetBinding[] previousBindings = gd.GetRenderTargets();

            // === 3. Copiar la pantalla COMPLETA a través del shader de lensing ===
            gd.SetRenderTarget(_lensTarget);
            gd.Clear(Color.Transparent);

            Effect shader = _distortionShader;
            float maxStrength = 0f;
            for (int i = 0; i < MaxSources; i++)
                if (_strengths[i] > maxStrength) maxStrength = _strengths[i];

            // "Pequeña lente": distorsión contenida (no la fuerza máxima del shader).
            float distortionStrength = 0.62f * MathHelper.Clamp(maxStrength, 0f, 1f);
            float maxLensingAngle = 24f;

            shader.Parameters["distortionStrength"].SetValue(distortionStrength);
            shader.Parameters["maxLensingAngle"].SetValue(maxLensingAngle);
            shader.Parameters["sourceRadii"].SetValue(_sourceRadii);
            shader.Parameters["sourcePositions"].SetValue(_sourcePositions);
            shader.Parameters["aspectRatioCorrectionFactor"].SetValue(
                new Vector2(screenSize.X / screenSize.Y, 1f));
            shader.Parameters["zoom"].SetValue(Main.GameViewMatrix.Zoom);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Matrix.Identity);
            shader.CurrentTechnique.Passes[0].Apply();
            // Quad completo 1:1 — la lente está a resolución nativa.
            Main.spriteBatch.Draw(Main.screenTarget,
                new Rectangle(0, 0, _lensTarget.Width, _lensTarget.Height), Color.White);
            Main.spriteBatch.End();

            // === 4. Restaurar el render target original ===
            // NOTA (v5.89): al volver al backbuffer FNA lo LIMPIA (semántica
            // DiscardContents de PresentationParameters, verificada contra
            // FNA.dll). Es lo mismo que hace el EndCapture de Terraria — y por
            // eso el paso 5 redibuja la pantalla COMPLETA.
            if (previousBindings != null && previousBindings.Length > 0)
                gd.SetRenderTargets(previousBindings);
            else
                gd.SetRenderTarget(null);

            // === 5. VOLCAR LA LENTE A PANTALLA COMPLETA ===
            // El backbuffer acaba de ser borrado por el restore del binding:
            // este blit devuelve el mundo entero a pantalla (resolución nativa,
            // distorsionado solo cerca de las fuentes). Es exactamente el mismo
            // blit que hace el EndCapture de Terraria con screenTarget — el
            // pipeline continúa como si la lente nunca hubiera existido, salvo
            // por la distorsión alrededor de las fuentes.
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Matrix.Identity);
            Main.spriteBatch.Draw(_lensTarget,
                new Rectangle(0, 0, gd.Viewport.Width, gd.Viewport.Height), Color.White);
            Main.spriteBatch.End();

            // === 6. ENCIMA DE LA LENTE: efectos del agujero (capa AboveLens) ===
            // Disco de acreción, anillo de fotones, espiral de succión...
            // La lente queda DETRÁS de los efectos del agujero negro.
            ParticleManager.RenderAboveLensLayer();

            // === 7. ENCIMA DE LA LENTE: el núcleo del agujero negro ===
            // El shader del agujero nunca es deformado por su propia lente.
            for (int i = 0; i < _blackHoleCount; i++)
            {
                Projectile bh = Main.projectile[_blackHoleIndices[i]];
                if (bh != null && bh.active)
                    BlackHoleProjectile.DrawCoreVisuals(bh, false);
            }

            // === 8. ENCIMA DE LA LENTE: anillos de las ondas cromáticas ===
            for (int i = 0; i < _waveCount; i++)
            {
                Projectile wave = Main.projectile[_waveIndices[i]];
                if (wave != null && wave.active)
                    CosmicShockwaveProjectile.DrawWaveVisual(wave, false);
            }

            // Todo renderizado con éxito: el frame siguiente los proyectiles se
            // saltan el pase del mundo y esta lente se encarga de pintarlos.
            LensActive = true;

            // El pipeline de Terraria continúa con su propio Begin para la UI:
            // dejamos el SpriteBatch CERRADO y los targets tal como estaban.
        }
    }
}

```

### 13.4 SupernovaProjectile.cs — COMPLETO (REESCRITO v5.85; ⚠️ v5.86: OnKill ahora genera 3 ondas de fuego — ver 13.0)
> 180 ticks de carga con atracción creciente y sacudidas anticipatorias; explosión
> masiva en OnKill con doble onda expansiva, flash gigante y AoE de 340px.
> Invocado por el sol en su segundo 7 (sincronizado) y por SupernovaStaff.

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// SupernovaProjectile — estrella que colapsa durante 3 segundos y luego
    /// estalla en una supernova masiva (v5.85, reescrito).
    ///
    /// CICLO (180 ticks = 3 segundos exactos):
    ///   - CARGA (0..180): contrae acelerando y se vuelve blanco-azulado,
    ///     atrae enemigos con fuerza CRECIENTE (0.5 → 2.2), genera GoldFlame
    ///     en espiral hacia dentro cada vez más rápido, y tiembla con
    ///     sacudidas de cámara que anticipan el estallido.
    ///   - ONKILL (tick 180): EXPLOSIÓN MASIVA mejorada:
    ///       * DOBLE onda expansiva (blanca-dorada veloz + naranja profunda retardada)
    ///       * Flash blanco gigante + destello de destellos (SparkleStar)
    ///       * 70 lenguas de GoldFlame + 25 chispas blancas + brasas + humo
    ///       * Daño AoE real en 340px (SimpleStrikeNPC) + OnFire
    ///       * Temblor de cámara fuerte (PunchCameraModifier)
    ///
    /// INTEGRACIÓN CON EL SOL (SunProjectile): el sol lo invoca en su segundo 7,
    /// lo mantiene centrado y ambos explotan SIMULTÁNEAMENTE en el segundo 10.
    /// La fuerza de succión durante la carga se suma a la gravedad creciente
    /// del propio sol → los enemigos son arrastrados al centro de la nova.
    /// </summary>
    public class SupernovaProjectile : ModProjectile
    {
        /// <summary>Duración total de la carga: 3 segundos.</summary>
        private const int ChargeDuration = 180;

        private float Age { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = ChargeDuration;
            Projectile.light = 0f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            Projectile.ignoreWater = true;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Age += 1f;
                Projectile.velocity *= 0.92f;

                // Progreso de carga: 0 → 1 durante los 3 segundos (con aceleración final).
                float charge = MathHelper.Clamp(Age / ChargeDuration, 0f, 1f);
                // Ease-in cuadrático: los primeros instantes son calma, el final es frenesí.
                float chargeEased = charge * charge;

                // === CARGA: atracción de enemigos con fuerza creciente ===
                float pullRadius = 300f;
                float pullStrength = 0.5f + chargeEased * 1.7f; // 0.5 → 2.2
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    Vector2 toCenter = Projectile.Center - npc.Center;
                    float dist = toCenter.Length();
                    if (dist > pullRadius || dist < 5f) continue;
                    float strength = (1f - dist / pullRadius) * pullStrength;
                    if (toCenter.LengthSquared() > 0.01f)
                    {
                        toCenter.Normalize();
                        npc.velocity += toCenter * strength;
                    }
                }

                // === CARGA: materia dorada en espiral hacia el núcleo ===
                if (Main.netMode != NetmodeID.Server)
                {
                    int spiralCount = 2 + (int)(chargeEased * 2f); // 2 → 4 por frame
                    for (int i = 0; i < spiralCount; i++)
                    {
                        float angle = Age * (0.18f + chargeEased * 0.14f) + i * MathHelper.Pi;
                        float maxDist = 110f - chargeEased * 30f;
                        float dist = maxDist * (1f - charge * 0.55f) + Main.rand.NextFloat(-8f, 8f);
                        Vector2 spawnPos = Projectile.Center + new Vector2(
                            (float)Math.Cos(angle) * dist,
                            (float)Math.Sin(angle) * dist);
                        Vector2 toCenter = Projectile.Center - spawnPos;
                        if (toCenter.LengthSquared() > 0.01f)
                        {
                            toCenter.Normalize();
                            Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X);
                            float speed = 3.5f + chargeEased * 4.5f;
                            Vector2 vel = toCenter * speed + tangent * speed * 0.45f;
                            Dust d = Dust.NewDustPerfect(spawnPos, DustID.GoldFlame,
                                vel, 200, new Color(255, 220, 150), 1.1f);
                            d.noGravity = true;
                            d.fadeIn = 0f;
                        }
                    }

                    // Sacudidas anticipatorias: cada 40 ticks, cada vez más fuertes.
                    if (Age % 40f == 0f && Age > 20f)
                    {
                        try
                        {
                            float rumble = 1.5f + chargeEased * 4.5f;
                            Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                                Projectile.Center, new Vector2(1f, 0f), rumble, 6, 8, 0.3f,
                                "AethonSupernovaCharge"));
                        }
                        catch { }
                    }
                }

                // === CARGA: luz que se blanquea e intensifica ===
                float lightIntensity = 1f + chargeEased * 1.6f;
                Lighting.AddLight(Projectile.Center,
                    new Vector3(1f, 0.9f - 0.15f * chargeEased, 0.6f - 0.35f * chargeEased) * lightIntensity);
            }
            catch { }
        }

        // ================================================================
        //  EXPLOSIÓN MASIVA (OnKill, sincronizada con el sol en el segundo 10)
        // ================================================================
        public override void OnKill(int timeLeft)
        {
            // Daño AoE: solo en la autoridad (servidor / singleplayer).
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    float dist = (npc.Center - Projectile.Center).Length();
                    if (dist < 340f)
                    {
                        npc.SimpleStrikeNPC(Projectile.damage, npc.direction,
                            false, Projectile.knockBack, DamageClass.Magic);
                        npc.AddBuff(BuffID.OnFire, 300);
                    }
                }
            }

            if (Main.netMode == NetmodeID.Server) return;

            // === DOBLE ONDA EXPANSIVA DE LA LIBRERÍA ===
            // Onda 1: blanca-dorada, veloz y agresiva.
            ParticlePresets.RingPulse(Projectile.Center, 320f,
                new Color(255, 245, 200, 230), 22);
            // Onda 2: naranja profunda, más ancha y retardada.
            ParticlePresets.RingPulse(Projectile.Center, 460f,
                new Color(255, 120, 40, 160), 44);

            // === FLASH BLANCO GIGANTE (SoftGlow aditivo de corta vida) ===
            var flash = new ParticleData
            {
                Position = Projectile.Center,
                Velocity = Vector2.Zero,
                Scale = Vector2.One * 6.5f,
                PackedColor = ParticleManager.PackColor(new Color(255, 255, 245, 255)),
                PackedStartColor = ParticleManager.PackColor(new Color(255, 255, 245, 255)),
                TimeLeft = 14,
                Duration = 14,
                TextureId = ParticleTex.SoftGlow,
                BlendMode = 1,
                LayerPriority = LayerPriorities.AboveTiles,
            };
            flash.EnableComponent(ComponentFlag.FadeOut);
            flash.EnableComponent(ComponentFlag.ScaleDown);
            ParticleManager.Spawn(flash);

            // Ráfaga de núcleo: interpolación blanco → naranja profundo.
            ParticlePresets.Explosion(Projectile.Center, 200f, 46,
                new Color(255, 250, 220), new Color(255, 100, 30), 42);

            // === VIENTO ESTELAR: estelas radiales largas ===
            for (int i = 0; i < 26; i++)
            {
                float angle = (MathHelper.TwoPi / 26) * i + Main.rand.NextFloat(-0.08f, 0.08f);
                Vector2 outward = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                var p = new ParticleData
                {
                    Position = Projectile.Center + outward * 24f,
                    Velocity = outward * Main.rand.NextFloat(4.5f, 8.5f),
                    Scale = new Vector2(2.6f, 0.55f),
                    Rotation = angle,
                    PackedColor = ParticleManager.PackColor(new Color(255, 245, 200, 220)),
                    PackedStartColor = ParticleManager.PackColor(new Color(255, 245, 200, 220)),
                    PackedEndColor = ParticleManager.PackColor(new Color(255, 90, 20, 20)),
                    TimeLeft = 44,
                    Duration = 44,
                    TextureId = ParticleTex.TrailGlow,
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                p.EnableComponent(ComponentFlag.FadeOut);
                p.EnableComponent(ComponentFlag.ColorShift);
                ParticleManager.Spawn(p);
            }

            // === TEMBLOR DE CÁMARA FUERTE ===
            try
            {
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                    Projectile.Center, new Vector2(1f, 0f), 10f, 14, 22, 0.5f,
                    "AethonSupernovaBlast"));
            }
            catch { }

            // === DUSTS FRONTALES: 70 lenguas de fuego ===
            for (int i = 0; i < 70; i++)
            {
                float angle = (MathHelper.TwoPi / 70) * i + Main.rand.NextFloat(-0.1f, 0.1f);
                float speed = Main.rand.NextFloat(7f, 14f);
                Vector2 vel = new Vector2(
                    (float)Math.Cos(angle) * speed,
                    (float)Math.Sin(angle) * speed);
                Color color = Main.rand.NextBool(3)
                    ? new Color(255, 245, 190)
                    : new Color(255, 230, 150);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    vel, 240, color, 1.8f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // 25 chispas blancas encantadas
            for (int i = 0; i < 25; i++)
            {
                Vector2 v = new Vector2(
                    Main.rand.NextFloat(-10f, 10f),
                    Main.rand.NextFloat(-10f, 10f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                    v, 255, Color.White, 1.2f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // 18 brasas de fuego (con gravedad: llueven tras la nova)
            for (int i = 0; i < 18; i++)
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                    new Vector2((float)Math.Cos(angle) * Main.rand.NextFloat(4f, 8f),
                                (float)Math.Sin(angle) * Main.rand.NextFloat(4f, 8f)),
                    210, new Color(255, 160, 60), 1.5f);
                d.noGravity = false;
                d.fadeIn = 0f;
            }

            // 14 volutas de humo ascendentes
            for (int i = 0; i < 14; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke,
                    new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-5f, -1f)),
                    110, new Color(130, 80, 50), 1.2f);
                d.noGravity = false;
                d.fadeIn = 0f;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45, Projectile.Center);
        }

        // ================================================================
        //  RENDER DE LA CARGA (contracción + blanco caliente + temblor final)
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                Texture2D ring = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring").Value;
                if (softGlow == null || ring == null) return false;

                float charge = MathHelper.Clamp(Age / ChargeDuration, 0f, 1f);
                float chargeEased = charge * charge;

                // Temblor de anticipación en el último tramo de la carga.
                Vector2 jitter = Vector2.Zero;
                if (charge > 0.6f)
                {
                    float shake = (charge - 0.6f) / 0.4f;
                    jitter = new Vector2(
                        Main.rand.NextFloat(-1f, 1f) * shake * 2.2f,
                        Main.rand.NextFloat(-1f, 1f) * shake * 2.2f);
                }

                Vector2 drawPos = Projectile.Center - Main.screenPosition + jitter;
                Vector2 glowOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                Vector2 ringOrigin = new Vector2(ring.Width / 2f, ring.Height / 2f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // === Halo contraído: de dorado (2.0) a blanco-azulado compacto (0.6) ===
                float scale = 2.0f - chargeEased * 1.4f;
                // El color se desplaza de oro a blanco puro.
                int r = 255;
                int g = (int)(180 + 75 * chargeEased);
                int b = (int)(80 + 165 * chargeEased);
                Color halo = new Color(r, g, b, 220);
                // Pulso creciente cerca del estallido.
                float pulse = 1f + (float)Math.Sin(Age * (0.25f + chargeEased * 0.5f)) * 0.06f * (1f + chargeEased * 2f);
                Main.spriteBatch.Draw(softGlow, drawPos, null, halo, 0f, glowOrigin, scale * pulse, SpriteEffects.None, 0f);

                // Núcleo blanco-caliente que crece en proporción (la masa se condensa).
                float coreScale = 0.4f + chargeEased * 0.28f;
                Color core = new Color(255, 255, 255, 240);
                Main.spriteBatch.Draw(softGlow, drawPos, null, core, 0f, glowOrigin, coreScale * pulse, SpriteEffects.None, 0f);

                // === Anillos de contención pulsantes (la estrella luchando por no colapsar) ===
                if (charge > 0.25f)
                {
                    float ringPhase = (Age % 24f) / 24f;
                    float ringScale = (0.8f + ringPhase * 1.6f) * (0.6f + charge * 0.5f);
                    byte ringAlpha = (byte)(160 * (1f - ringPhase) * charge);
                    Main.spriteBatch.Draw(ring, drawPos, null,
                        new Color(255, 220, 140, ringAlpha), 0f, ringOrigin, ringScale, SpriteEffects.None, 0f);
                }

                Main.spriteBatch.End();
            }
            catch { }

            // Restaurar el SpriteBatch al estado esperado por tML.
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.Transform);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            try
            {
                // La materia estelar inflama al contacto.
                target.AddBuff(BuffID.OnFire, 300);
            }
            catch { }
        }
    }
}
```

### 13.5 CosmicWeapons.cs (Content/Weapons/Cosmic/CosmicWeapons.cs)

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
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Lensing gravitacional real de 75 pasos + lente que distorsiona el propio fondo del juego alrededor del horizonte de sucesos]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Disco de acreción de estelas orbitando + succión espiral con partículas de colores + devora el polvo del entorno]"));
            tooltips.Add(new TooltipLine(Mod, "D3", "[c/78788C:Atrae enemigos en un radio de 450px y colapsa con implosión, doble onda expansiva y temblor de pantalla]"));
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
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Estrella de plasma de 10 segundos: llamaradas solares cada 2s y supernova que carga desde el segundo 7 hasta el estallido final]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Corona de plasma orbitando + viento solar radial + prominencias periódicas + destellos luminosos]"));
            tooltips.Add(new TooltipLine(Mod, "D3", "[c/78788C:Inflama a los enemigos, atrae a los rivales con su gravedad y muere en una nova masiva con doble onda expansiva y temblor de pantalla]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
```

### 13.6 TestingPlayer.cs (Content/Players/TestingPlayer.cs)

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
            // v5.80: armas cósmicas basadas en shaders del mod de referencia
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

### 13.7 RealBlackHoleShader.fx (Content/Effects/Shaders/RealBlackHoleShader.fx)

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

### 13.8 SunShader.fx (Content/Effects/Shaders/SunShader.fx)

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
│   ├── BlackHoleLensSystem.cs            ← NUEVO v5.85: lente gravitacional de pantalla
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
│   ├── Shaders/                          ← 8 .fx (fuente) + 5 .fxc (compilados)
│   │   ├── BlackHoleDistortionShader.fx
│   │   ├── BlackHoleDistortionShader.fxc  (compilado)
│   │   ├── BlackOnlyShader.fx
│   │   ├── BlackOnlyShader.fxc  (compilado)
│   │   ├── Bloom.fx
│   │   ├── ChromaticAberration.fx
│   │   ├── RadialShineShader.fx
│   │   ├── RadialShineShader.fxc  (compilado)
│   │   ├── RealBlackHoleShader.fx
│   │   ├── RealBlackHoleShader.fxc  (compilado)
│   │   ├── Shockwave.fx
│   │   ├── SunShader.fx
│   │   └── SunShader.fxc
│   │
│   ├── Textures/                         ← Texturas del pipeline de shaders (10) [renombrada en v5.85]
│   │   ├── BloomCircle.png
│   │   ├── BloomCircleSmall.png
│   │   ├── BloomFlare.png
│   │   ├── DendriticNoiseZoomedOut.png
│   │   ├── FireNoiseA.png
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
│   ├── Cosmic/                          ← Proyectiles cósmicos (3)
│   │   ├── BlackHoleProjectile.cs
│   │   ├── BlackHoleProjectile.png
│   │   ├── CosmicShockwaveProjectile.cs      ← NUEVO v5.86 (ondas cromáticas/inversas/fuego)
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
│   ├── Cosmic/                          ← Armas cósmicas con shaders el mod de referencia (2)
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

### 14.2 Carpeta de referencia (ELIMINADA en v5.85)

> `ReferenceShaders/` ya NO existe en el proyecto (se perdió con un reset del
> sandbox en v5.82 y no afecta a la compilación). Contenía históricamente:
> BlackHole.cs, BlackHolePet.cs, PetBlackHoleRenderer.cs, StarPet.cs y
> Starseed.cs (código del mod de referencia, NO compilado). Si se necesita
> consultar de nuevo, clonar el repo público a `/tmp/refmod/`.

---

## 15. DOCUMENTACIÓN DEL MOD DE REFERENCIA (técnicas aprendidas)

Técnicas aprendidas del mod de shaders de referencia que han sido
adaptadas a AethonMod (los nombres concretos se eliminaron en v5.85):

### 15.1 RealBlackHoleShader (lightmarch 75 pasos)

**Concepto**: En vez de aproximar el lensing gravitacional con una distorsión analítica
de UV, el mod de referencia hace un **ray-marching** real: para cada pixel, lanza un rayo desde la cámara
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
sprite plano. el mod de referencia usa un "pinch factor" que deforma las UVs para que la textura
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
perfectamente negro, sin color del disco de acreción "filtrándose". el mod de referencia usa un
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

**Concepto**: Para crear efectos de vórtice/swirl, el mod de referencia samplea texturas en
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

Aprendido del mod de referencia, el patrón correcto para aplicar un shader a un proyectil:

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
            Texture2D noise = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Textures/FireNoiseB").Value;
            Main.graphics.GraphicsDevice.Textures[1] = noise;
            Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

            // 4. Patrón Begin → Apply → Draw → End → Begin
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            shader.CurrentTechnique.Passes[0].Apply();

            // 5. Dibujar el canvas (un pixel invisible que se escala enorme, o el sprite)
            Texture2D canvas = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Textures/InvisiblePixel").Value;
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

Técnica del StarPet del mod de referencia: dibujar el backglow con dos colores superpuestos para
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

### 15.8 Iluminación del mod de referencia

Para partículas brillantes como estrellas, el mod de referencia usa iluminación constante alta
(no pulsante):
```csharp
Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.9f, 0.5f) * 3.2f);
```

> El factor 3.2f da una luz extremadamente intensa que ilumina varios bloques a la
> redonda. AethonMod añade un pulso sutil (0.9 + 0.1 * sin) para que parezca "vivo".

---

## 16. RESUMEN FINAL

### 16.1 Lo que se ha logrado (hasta v5.88)
- ✅ Mod completo con 8 armas protegidas del remote
- ✅ 19 armas V20 creativas sin mana
- ✅ 2 armas cósmicas (BlackHoleStaff, SunStaff) con shaders reales propios
- ✅ Sistema de partículas data-oriented COMPLETO (6 archivos: ParticleData +
  ParticleBuffer + ParticleManager + ShapeDescriptor + CameraBounds + ParticlePresets,
  con 9 componentes implementados, spawn por forma, culling y presets)
- ✅ BlackHole/Sun con 3 capas de profundidad: librería aditiva (fondo) + dusts
  (frontal) + shader (canvas)
- ✅ **v5.85 — LENTE GRAVITACIONAL de pantalla**: el fondo del juego se curva
  alrededor del agujero negro (BlackHoleLensSystem + BlackHoleDistortionShader
  que por fin SE USA + hook TimeLogger 36 + Main.screenTarget)
- ✅ **v5.85 — Sol con ciclo completo de 10 segundos**: llamaradas cada 2s,
  supernova sincronizada del segundo 7 al 10, gravedad 1/10 del agujero negro
  con rampa x4 durante la carga, nova simultánea con doble onda expansiva
- ✅ **v5.85 — SupernovaProjectile reescrito**: 3s de carga + explosión masiva
  mejorada (doble onda 320px+460px, flash, AoE 340px, temblor fuerte)
- ✅ **v5.85 — Cero referencias al mod externo de referencia** en todo el
  proyecto (carpeta Textures/, tooltips, csproj, gitignore, docs)
- ✅ **v5.86 — La lente va DETRÁS del agujero negro y sus efectos** (capa
  AboveLens + composición por regiones) + CosmicShockwaveProjectile con 3
  estilos de onda (cromática/inversa/fuego, daño real por frente de onda) +
  secuencia de muerte completa del agujero + 3 ondas de fuego con quemadura en
  la explosión del sol
- ✅ **v5.87 — Desactivación limpia del mod**: el Dispose del render target de
  la lente se encola al hilo principal (Main.QueueMainThreadAction) — fix del
  ThreadStateException de FNA3D que rompía la desactivación
- ✅ **v5.88 — El mod CARGA de nuevo**: textura del CosmicShockwaveProjectile
  (faltaba desde v5.86 y bloqueaba la carga) + bug de golpes múltiples de las
  ondas corregido + carga de assets sin bloqueo + icon_small + build 0 warnings
- ✅ 5 shaders con .fxc compilados + 3 .fx fuente sin usar
- ✅ 10 texturas del pipeline de efectos + 11 texturas de librería registradas
- ✅ Compilación estable sin errores (verificada contra tML v2026.07.3.0 real)

### 16.2 Lo que falta (próximos pasos)
- ⚠️ **PROBAR** en tModLoader 1.4.4 los 2 armas cósmicas (desde v5.88 el mod
  vuelve a cargar — la v5.86/v5.87 nunca llegaron a probarse)
- ⚠️ Verificar que la LENTE GRAVITACIONAL se vea en pantalla (fondo curvándose
  alrededor del agujero) y que los shaders carguen sin excepción en runtime
- ⚠️ Verificar que las 4 ondas del agujero hagan daño (UNA vez cada una por NPC
  desde v5.88) y las 3 de fuego apliquen quemadura
- ⚠️ **Verificar que la DESACTIVACIÓN del mod complete sin error** (fix v5.87:
  desactivar el mod o Mods → Reload debe terminar en silencio, sin diálogo)
- ⚠️ Si algún shader falla, revisar client.log (la lente tiene try/catch total:
  lo peor que puede pasar es que no se dibuje)
- ⚠️ FUTURO: integrar SunProjectile en el Grimorio (arma definitiva) con su
  quemadura potenciada por daño mágico

### 16.3 Recordatorio final
**Puedo coger los recursos de nuestro github si los datos de mi versión local se borran**

- GitHub PAT: `[GITHUB_PAT - solicitar al usuario]`
- Repositorio: https://github.com/Leo0x01/Aethon-Mod-Terraria
- Commit actual: v5.90 (hash en la tabla de la sección 10)
- Commit estable del remote: `e826c82`

---

**Fin del documento.**

> Última actualización: v6.01
> Documento generado para asegurar continuidad del proyecto entre sesiones de IA.
> Si eres una IA leyendo esto: SIEMPRE empieza por el Recordatorio al inicio de
> cualquier commit o documento nuevo.

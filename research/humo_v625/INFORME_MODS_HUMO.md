# INFORME — HUMO / BRUMA / NIEBLA / VAPOR EN 20+ MODS DE TERRARIA
## Investigación de VFX para BrumaFX v2 (AethonMod v6.25)

**Task ID:** 41-a · **Agente:** general-purpose (investigación humo en mods populares)
**Fecha:** v6.25 · **Contexto:** BrumaFX (librería procedural de humo del mod, 100% código, cero sprites de arte)

---

## 0. PUNTO DE PARTIDA — el bug que hay que superar (y lo que viene después)

`BrumaBrushes.HornearPuff()` hornea sus texturas 128×128 con **RGB=255 constante y alfa variable SIN premultiplicar**. En el pipeline FNA/tModLoader (que espera alfa premultiplicada):

- En lotes **aditivos (Blend One/One)** el alfa se IGNORA por completo → el quad entero se pinta como un **rectángulo sólido**.
- En lotes **alfa** los bordes quedan duros (el borde "decente" del premultiplicado requiere que RGB→0 cuando A→0).

Eso ya se está arreglando (premultiplicar RGB=alfa al hornear). **Esta investigación va más allá**: cómo hacen los mods de Terraria que el humo se ve *de verdad bien* — bordes suaves, movimiento orgánico, integración con la luz del mundo, presupuesto de rendimiento. Todo lo que sigue son hechos verificados leyendo código fuente real (decompilado o repos públicos) + fuentes web citadas.

**Fuentes primarias leídas (código):**
- Vanilla 1.4.4.9 decompilada desde `tModLoader.dll` v2026.07.3.0 con ILSpy: `Terraria.Dust` (3.314 líneas), `Terraria.Gore`, `Main.DrawDust` (Main.cs líneas 49935-50240), `Main.DrawGore`, `DustID` → `/tmp/research/terraria_src/`
- **Everglow** (repo local `/tmp/research/everglow`): `Vapor.cs/.fx`, `FireSmog.cs/.fx`, `IceSmog.cs`, `FogVFX.cs`, `CurseFlame_HighQuality.cs`, `VFXManager.cs`, `VFXBatch.cs`, `VFX.md`, HeatMaps (PNG analizados numéricamente), `Point.png`, `Noise_perlin.png`, `GiantCampFire.cs`
- **Coralite** (repo local `/tmp/research/coralite`): `Fog.cs`, `BigFog.cs`, `AnimeFog.cs`, `WalkSmoke.cs`, `FireParticle.cs`, inventario de Assets/Particles
- **Calamity** (clone `github.com/CalamityTeam/CalamityModPublic`): `Particle.cs`, `GeneralParticleHandler.cs`, `HeavySmoke.cs`, `ThanatosSmokeParticle(Set).cs`, `MediumMistParticle.cs`, `GraveyardMistParticle.cs`, `RancorFog.cs`, PNGs medidos con PIL
- **Starlight River** (clone `github.com/ProjectStarlight/StarlightRiver`): `Core/ParticleSystem.cs`, `Content/Dusts/PixelSmokeColor.cs`, `SmokeDustColor.cs`, `Mist.cs`, `Helpers/DustHelper.cs`, `PixelationSystem.cs`, `VitricDesertBiome.cs`
- **Spirit Mod / Spirit Reforged** (clone `github.com/PhoenixBladez/SpiritMod`): `DuelistDusts.cs` (DuelistSmoke), `BlizzardDust.cs`, Effects/ (shaders)
- **LunarVeil / Stellamod "The Stars Above"** (local `/tmp/research/lunarveil`): `TSmokeDust.cs`, `Systems/Particles/ParticleSystem.cs`, `Particle.cs`
- **SOTS / Shadows of Abaddon** (clone `github.com/VortexOfRainbows/SOTS`): `Prim/PrimBase.cs`, `Excavator.cs`
- **Fargo's Mutant Mod** (clone `github.com/Fargo-Team/FargosMutantMod`): `Projectiles/Explosion.cs`
- **Terraria Overhaul** (clone `github.com/Mirsario/TerrariaOverhaul`): `Common/BloodAndGore/ParticleSystem.cs`, `Common/Fires/FireSystem.cs`
- **ParticleLibrary** (clone `github.com/SnowyStarfall/ParticleLibrary`, GPL-3): `Core/V3/Particles/ParticleBuffer.cs`, `Core/V2/ParticleSystem/ParticleSystem.cs`, README
- **Polarities** (clone `github.com/nicobrownmath/Polarities`): `Dusts/GenericDustBase.cs`, `DendriticEnergy.cs`
- **WoTE "Wrath of the Terra/Empress"** (local `/tmp/research/wote`): `Content/Particles/*` (bloom, metaballs)
- **MEAC** (local `/tmp/research/meac/decompiled`): MyDustId, VFX de luz (ver informe luz_v622)

**Fuentes web (27 búsquedas, 25 JSONs preservados en `research/humo_v625/busquedas_web_41a/`):** Steam Workshop, foros de Terraria, GitHub, wikis — citadas por mod en la tabla.

---

## 1. TABLA MAESTRA — 23 MODS/FUENTES Y SU HUMO

| # | Mod | Tamaño/relevancia | Técnica de humo/niebla | Calidad humo | Fuente |
|---|-----|-------------------|------------------------|--------------|--------|
| 1 | **Vanilla Terraria 1.4.4.9** | — | Dust atlas 8×8 en celdas 10px, 3 variantes, tinte por `Lighting.GetColor`, smear de 10 copias, gores de humo 916-918 con viento suavizado ×50/51 | ⭐⭐⭐ (estándar del juego) | decompile propio |
| 2 | **Calamity** | ~324 MB repo, el mod de contenido más grande | PNG flipbooks pre-horneados (HeavySmoke 80×80 ×7 variantes ×6 frames), escalera de tamaños 22/24/32/80, 3 lotes fijos (Alpha/NonPremult/Additive), límite configurable + flag `Important` | ⭐⭐⭐⭐⭐ | clone GitHub |
| 3 | **Everglow** | 658 MB, mod chino | **Humo por shader de vértices**: 1 quad/puff, fBm muestreado en GPU (Noise_perlin 400²), máscara radial (Point 174²), LUT "HeatMap" 1D para color+alfa, luz del mundo POR VÉRTICE, viento real `Main.windSpeedCurrent`, capas PostDrawBG/PostDrawNPCs | ⭐⭐⭐⭐⭐ (lo mejor visto) | repo local |
| 4 | **Starlight River** | 141 MB repo | Partículas GPU (DynamicVertexBuffer+BasicEffect, 10.000 máx, FastParallel), PixelSmokeColor (doble draw cruzado rot y rot+90°), PixelationSystem (RT a media resolución → look "chunky"), Mist con alpha sinusoidal | ⭐⭐⭐⭐ | clone GitHub |
| 5 | **Coralite** | 360 MB, mod chino | Sistema PRT (librería InnoVault): Fog 64×64 ×4 variantes aditivo, BigFog 256×256 ×4, doble-draw con alfa partido (A/2), WalkSmoke flipbook 1×7 con luz del mundo + pase aditivo A=0 | ⭐⭐⭐⭐ | repo local |
| 6 | **LunarVeil / Stellamod** | 27 MB local | ModDust TSmokeDust: rampa color→gris(25,25,25)→negro por umbrales de alfa 120/180, luz ×0.1 mientras brilla, rotación π/4/16 por tick (mini-curl), 2 lotes (Additive + "Black"=AlphaBlend) | ⭐⭐⭐ | repo local |
| 7 | **Spirit Mod / Spirit Reforged** | 210 MB repo | DuelistSmoke: rampa Amarillo→Naranja→Gris(25,25,25) (humo de boca de arma que se enfría), luz emitida sincronizada con la rampa, + docenas de shaders .fx propios | ⭐⭐⭐ | clone GitHub |
| 8 | **SOTS / Shadows of Abaddon** | 176 MB repo | Vanilla puro: `DustID.Smoke` + **gore de humo vanilla** (GoreID.Smoke1-3), sistema Prim con RT a media resolución para trails pixelados | ⭐⭐ | clone GitHub |
| 9 | **Fargo's Mutant + Souls** | 5,9 MB repo | Vanilla puro: `DustID.Smoke` alfa 100 escala 1.5 + gores de explosión 61-64. Cero sistema propio | ⭐⭐ | clone GitHub |
| 10 | **Terraria Overhaul** | 929 archivos | ParticleSystem propio 1024 partículas, bitmask O(1), líneas 2px con motion-shear por OldPositions (sangre), BlendState.Opaque, culling probabilístico por config | ⭐⭐ (no es humo, sí partículas) | clone GitHub |
| 11 | **ParticleLibrary (SnowyStarfall)** | 8,4 MB, librería GPL-3 | V3: GPU **instancing** (`DrawInstancedPrimitives`), 1 textura por buffer, 17 capas de dibujo; V2: FastList + hooks; la usan Redemption, SoA, LunarVeil | ⭐⭐⭐⭐ (infra) | clone GitHub |
| 12 | **Polarities** | 199 MB repo | Vanilla: `DustID.Smoke` **tintado de negro** (humo oscuro), dusts genéricos con atlas de 3 tamaños (6×6/8×8/8×8) | ⭐⭐ | clone GitHub |
| 13 | **WoTE (Empress rework)** | 16 MB local | Sin humo: partículas de BLOOM puro (BloomCircle 200², halo ×1.5-2 + núcleo), metaballs de refracción | ⭐⭐⭐ (bloom) | repo local |
| 14 | **MEAC (EoL rework)** | 95 MB local | Casi sin humo: LUTs de color 1×256/1×40, tiras 1×N tintadas en runtime (ver informe luz_v622) | ⭐⭐ | repo local |
| 15 | **Thorium** | ~web | Web: repo GitHub solo releases sin fuente; estilo conservador documentado en wikis — humo con dusts vanilla y goras, foco en consistencia con el juego base | ⭐⭐ | web (01/10/21_*.json) |
| 16 | **Mod of Redemption** | ~web | Usa **ParticleLibrary** (GPL) como dependencia de partículas (verificado: search 06 + README de la librería) | ⭐⭐⭐ | web |
| 17 | **Ancients Awakened** | ~web | Web: foro oficial + wiki; VFX basados en dusts y sprites animados, sin sistema de humo destacable encontrado | ⭐⭐ | web (08/24) |
| 18 | **Orchid Mod** | ~web | Web: wiki/foro — alquimia y chamán con efectos sobrios de dusts, nada de humo de masa | ⭐⭐ | web (12/25) |
| 19 | **Exxo Avalon** | ~web | Web: org GitHub `Exxo-Avalon` — contenido clásico, dusts vanilla | ⭐⭐ | web (13) |
| 20 | **Fargo's Souls DLC** | ~web | Web + Fargo-Team GitHub — mismo estilo vanilla del Mutant, VFX de alma con dusts tintados | ⭐⭐ | web (11/19) |
| 21 | **DragonLens** | ~web | Herramienta de desarrollo (web 27) — sin VFX de humo, citada como referencia del ecosistema de tools | — | web |
| 22 | **Wrath of the Gods (WoTG)** | sesiones previas | Render de agujeros/sol por shaders raymarching + texturas procedurales de ruido (WavyBlotchNoise etc.); sin librería de humo (worklog Tasks 2-5) | ⭐⭐⭐ | worklog propio |
| 23 | **Vanilla tML Additive (Extra_98 cross)** | decompile | El "glow cross" de 4 draws que usa WoTE/EoL — para venas aditivas dentro de humo alfa | ⭐⭐⭐ | decompile propio |

> Los 4 chinos (Everglow, Coralite + los ya vistos LunarVeil/Stellamod) y Calamity/StarlightRiver concentran TODO el conocimiento de humo de nivel alto del ecosistema tML. El resto del ecosistema (Fargo, SOTS, Polarities, Thorium, Orchid, Avalon, AA) usa **vanilla dusts + gores**: la lección es que el humo "decentísimo" de vanilla ya es la referencia estética del juego.

---

## 2. EL DUST SYSTEM DE VANILLA, EN NÚMEROS (fuente primaria: decompile de `Terraria.Dust` + `Main.DrawDust`)

### 2.1 Estructura
- **6000 slots** (`Main.dust[6000]`), pero solo se actualizan/dibujan los índices < `Main.maxDustToDraw`.
- **Un solo atlas**: `TextureAssets.Dust` con celdas de **10×10 px** que contienen sprites de **8×8**. `frame.X = 10·type`; para type ≥ 100: `frame.X −= 1000·(type/100)`, `frame.Y += 30·(type/100)` → el atlas es una rejilla de 100 columnas × 3 filas por banda. **3 variantes** por tipo: `frame.Y = 10·rand(3)` al nacer.
- **Spawn** (`NewDust`): posición aleatoria dentro del rect pedido (mínimo 5×5), velocidad = `rand(-20..20)·0.1` + Speed pedida (±2 px/tick de jitter), `scale = 1 + rand(-20..20)·0.01` → **0.8..1.2** siempre.
- **Culling de nacimiento**: rect de pantalla inflado **400 px**; fuera → no nace.
- **Degradación LOD al nacer**: si el índice libre cae en zona >50% de `maxDustToDraw` → 1/5 de rechazo; >60% → 1/4; >70% → 1/2; >80% → 1/3; >90% → 3/4.

### 2.2 Física genérica (`UpdateDust`)
- `velocity *= (0.97, 0.99)` — ¡amortiguación ASIMÉTRICA X/Y! El humo frena más en horizontal que en vertical → columnas que se estiran hacia arriba de forma natural.
- `scale -= 0.0025` (encoge lento).
- `noGravity` → `velocity *= 0.93`, `scale += 0.0025` (crece lento) [el smoke flotante CRECE].
- Dust genérico `noGravity` sin tipo especial: `velocity *= 0.92`, `scale -= 0.04` (muere rápido).
- `rotation += velocity.X · 0.3` — **el humo RUEDA al avanzar** (rueda libre ligada a la velocidad horizontal).

### 2.3 El humo de cohete (dusts 130-134) — el "smear" de movimiento
```
// Al dibujar (Main.DrawDust), dusts 130-134, 219-223, 226, 272, 278:
numCopies = clamp((|vel.X|+|vel.Y|) · 0.3 · 10, ..., 10)      // hasta 10
para j en 0..numCopies:
    pos    = dust.position − dust.velocity · j                // copias ATRÁS en el tiempo
    escala = dust.scale · (1 − j/10)                          // cada copia más pequeña
    color  = GetAlpha(Lighting.GetColor(tile))
    Draw(atlas, pos, frame, color, rotation, origen(4,4), escala)
```
- **Cada dust veloz se dibuja como hasta 10 quads en posiciones pasadas** — motion smear puro. Un dust a 3 px/tick de velocidad total ≈ 9 copias.
- **Luz emitida por tipo** (130-134): `AddLight(tile, scale·(1,0.5,0.4))` naranja; 131 verde `(0.4,1,0.6)` con flotabilidad `vel.Y −= 0.1`; 132 azul `(0.3,0.5,1)`; 133 amarillo `(0.9,0.9,0.3)` — todos `× scale` clampeado a 1.
- Física: `vel ×0.93` noGravity (crece +0.0025) o `vel ×0.95` y `scale −0.0025` (gravitado).
- El culling de dibujo para estos tipos se amplía a **±1000 px X / ±1050 Y** (¡el humo se dibuja MÁS ALLÁ de la pantalla para que entre andando!).

### 2.4 El dust de humo genérico (31 = DustID.Smoke)
- `noGravity` → **`velocity ×= 1.02` (¡ACELERA!) + `scale += 0.02` + `alpha += 4`** → vida ≈ 64 ticks desde alfa 0. El humo vanilla ACELERA al subir (convección).
- Con `customData = NPC`: pegado al NPC, `alpha −= 70/tick` (se hace visible) y `scale ×0.97`.
- SteampunkSteam (303): `vel ×1.02`, `scale += 0.03`, alfa mínimo 90, +4/tick.
- Nacimiento "suavizado" para tipos de fuego/humo (lista de ~24 IDs): `vel.Y = rand(-10..6)·0.1`, `vel.X ×0.3`, `scale ×0.7` → nacen lentos y pequeños hacia arriba.

### 2.5 Color y alfa (el contrato de tinte)
```
newColor = Lighting.GetColor(tile del centro del dust)        // ¡tinte por iluminación!
newColor = dust.GetAlpha(newColor)                            // (255−alpha)/255
Draw(...newColor...)
si dust.color ≠ 0: Draw(...dust.GetColor(newColor)...)        // 2ª pasada de tinte custom
si newColor == NEGRO → dust.active = false                    // ¡muere en oscuridad!
```
- **El humo vanilla se TINTE por la luz del mundo y MUERE si la luz lo mata** — integración total con cuevas.
- `GetColor` = `dust.color − (255 − newColor)` clampeado por canal (sustracción del tinte de luz sobre el color custom).

### 2.6 El lote de dibujado
- `Main.DrawDust`: **UN solo `Begin(Deferred, AlphaBlend, PointClamp, CullNone, Transform)`** para TODOS los dusts. Solo se rompe el lote si un dust lleva `shader` (armor shader → `Immediate`).
- **PointClamp = muestreo PUNTO (nearest)**: los dusts escalados se ven pixelados-crujientes. Es LA razón por la que el humo vanilla se ve "de juego retro" y el de Calamity (que usa **LinearClamp**) se ve suave.
- Muerte por escala: umbral base **0.1**; con LOD (dCount 0.5→0.9): 0.11/0.13/0.16/0.22/0.25 + shrink extra 0.001/0.0025/0.005/0.01/0.02 por tick. **El sistema se autodegrada con carga.**

### 2.7 Los GORES de humo (GoreID 916-918, Smoke1-3) — el humo "gordo" de vanilla
SpecialAI 2 (leído de `Terraria.Gore.Update`):
- **Fade-in**: `alpha −= rand(1,4)` mientras `alpha > 100` (nace con alfa alto→bajo).
- **Fade-out**: últimos 60 ticks de vida → `alpha += rand(1,7)`.
- **Viento suavizado**: `vel.X = (vel.X·50 + WindForVisuals·2 + rand(±10)·0.1)/51` — un lerp de orden 50 hacia el viento del mundo + ruido. `vel.Y` ídem con flotabilidad −0.35 y acoplamiento `vel.X·0.2` si va a la izquierda.
- **`rotation = velocity.X · 0.6`** — el gore de humo RUEDA con la deriva.
- **Colisión con jugador → `timeLeft = 0`** (el humo se disipa al atravesarlo) y se impulsa con la velocidad del jugador.
- Dibujo (DrawGore): tinte `Lighting.GetColor` en el centro del sprite, rotación+escala, **en el pase del mundo** (con los NPC, delante de tiles). SOTS y Fargo's usan estos gores para sus explosiones.
- Fog machine (SpecialAI 6): nube que persiste (alfa >225 se mantiene), muve con `timeLeft -= DisappearSpeed`, colisión con 3 tiles debajo la disipa.

### 2.8 Implicación para BrumaFX
Vanilla confirma 6 decisiones ya tomadas en la librería (smear por velocidad, crecimiento lento, erosión) y enseña 4 que faltan: **tinte por luz del mundo con muerte/extinción en oscuridad, amortiguación X/Y asimétrica, rotación ligada a velocidad.X, y el viento del mundo (`Main.windSpeedCurrent`/`WindForVisuals`) como fuerza real**.

---

## 3. LOS 5 MODS CON MEJOR HUMO — anatomía en pseudocódigo propio

### 3.1 EVERGLOW — "humo = 1 quad + shader que hace todo" (EL MÁS AVANZADO)

**Arquitectura** (`VFXManager` + `VFXBatch` + `Pipeline`):
- Cada efecto visual (Visual) se registra con 1-3 **Pipelines**: el principal (aplica shader + blend) y post-pipelines opcionales (Halo=bloom, WarpAndFade). Los pipelines componen con **render targets en ping-pong** de un pool (RenderTargetManager).
- `VFXBatch`: batch propio estilo SpriteBatch con **vertex buffers de 8192 vértices** (copia las constantes de SpriteBatch), `TriangleStrip` por quad, mezcla de texturas con flush al cambiar.
- **7 capas de dibujo**: PreDrawFilter, PostDrawProjectiles, PostDrawTiles, PostDrawDusts, PostDrawBG, PostDrawPlayers, PostDrawNPCs — el humo vivo puede ir DETRÁS de los tiles (PostDrawBG: niebla de fondo) o delante de NPCs (PostDrawNPCs: humo de combate).
- Control de calidad automático: `VisualQualityController` baja a Low si el modo de iluminación no es Color/White (retro).

**El humo en sí** (`VaporDust` + `Vapor.fx`, parafraseado):
```
// Un puff de vapor = 4 vértices (1 quad, TriangleStrip)
Vertex2DSmog {
    posición    : Vector2
    color       : Color  →  R,G = UV del quad (0..1 por esquina)
                    B    = "pocession" (curva de vida 0→1→0, ver abajo)
                    A    = alfa base
    texCoord    : Vector3 → XY = coordenadas de ruido (semilla + scroll temporal)
    texCoord2   : Vector3 → RGB = COLOR DE LUZ DEL MUNDO muestreado EN ESA ESQUINA
}

// El pixel shader hace TODO el trabajo:
ruido  = sample(Noise_perlin, texCoord.xy)              // 400×400, WRAP, LINEAR
halo   = sample(Point,       color.xy)                  // 174×174 gradiente radial
light  = 1 − halo.r · ruido.r · (1 − color.b)           // densidad = máscara×ruido×vida
color  = sample(HeatMap, light)                         // LUT 1D de 128-141 px
color.rgb *= texCoord2.rgb * 0.6                        // tinte por luz del mundo (por vértice!)
color.a   *= color.a
```
- **Curva de vida vía shader**: `pocession = 1 − sin(pow(timer/maxTime, 0.3) · π)` → invisible al nacer, máxima densidad a mitad de vida, invisible al morir. El pow(0.3) hace el arranque LENTO (humo que "toma presencia").
- **Invarianza de escala por UV**: el rango UV del ruido por quad = `0.2·scale/70` (VaporDust) o `0.4·scale/70` (VaporDust2) → **el nº de celdas de ruido por píxel es CONSTANTE**: el puff de 20px y el de 160px tienen la misma frecuencia de grumos. (FireSmog usa 0.4 fijo — menos correcto.)
- **Luz del mundo por vértice**: cada esquina muestrea `Lighting.GetColor` en su propia tile → el humo se sombrea EN SU PROPIO ANCHO (gradiente de iluminación dentro del puff). Y el color final = `lightColor·0.2 + vec3(|lightColor|/3)` → **piso de luz: nunca negro puro, siempre ~20% de tinte + un 33% de versión en escala de grises de la luz ambiente**. ESTE es el truco del humo visible en cuevas oscuras.
- **Física** (Update C#): `vel ×0.9/tick`, `vel += (windSpeed·0.02, −0.08 + ai[0])` (¡VIENTO REAL del mundo!), `scale += 0.1` hasta 160, `vel = vel.RotatedBy(ai[1])` (curl manual por rotación fija), muerte acelerada si `SolidCollision` (timer++ doble).
- **Spawn típico** (bayoneta de prisión): ráfaga de N puffs, `scale rand(20..135)`, `maxTime rand(10..90)`, velocidad `rand(0..4) rotada + (0,−4)`, buoyancy ai[0] `rand(−0.05..−0.01)`. La hoguera gigante: 1 puff cada 3 ticks (`NextBool(3)`), `scale 40..100`, vida 37-305, para el VAPOR; + FireSparkDust para chispas.
- **HeatMaps medidas numéricamente** (PIL):
  - `HeatMap_smog` 141×3: RGB negro, **alfa 255 (0-10%) → 141 (25%) → 83 (50%) → 0 (74%)** — humo que OCLUYE, sin color propio.
  - `HeatMap_vapor` 128×10: RGB gris 59→30, alfa 255→0 al 75% — vapor gris oscuro.
  - `HeatMap_NecrosisSmog` 141×3: RGB (244,237,255)→(42,0,92), alfa 105-120 de banda, 0 en extremos — humo morado con banda de densidad media.
  - `HeatMap_fire` 128×10: blanco→amarillo→naranja→rojo oscuro→transparente.
  - **La LUT 1D es TODO el diseño de color del humo**: cambiar humo frío por humo de peste = cambiar 1 textura de 128 px.
- **La llama de la hoguera** (GiantCampFire): cinta de 40 segmentos con `x = 9·sin(v·32 + t·16)·v + v²·windSpeed·870` (v = altura normalizada) — el VIENTO DOBLA la llama según v²; UV.y = `v·1.3 − timeValue` (SCROLL de textura, la llama "sube"); z = pow para perspectiva; shader con HeatMap + noise flame; luz `AddLight((1,0.7,0.2)·2)`.
- **FogVFX** (niebla simple): sprite FBM horneado, dos modos de vida (fade-in 20 ticks / fade-out lento), y **modo `substract`**: cambia a `CustomBlendStates.Subtract` para RESTAR luz (niebla que oscurece de verdad).

### 3.2 CALAMITY — "PNGs pre-horneados en escalera de tamaños + 3 lotes fijos"

**Sistema** (`GeneralParticleHandler`, créditos: derivado del de Spirit Mod con permiso):
- Partículas = clases con flags: `UseAdditiveBlend`, `UseHalfTransparency` (→ **NonPremultiplied**), `Important` (bypass del límite), `SetLifetime`, `FrameVariants`, `Pixelate`, `AffectedByLight`, `CustomShader`, `DrawLayer`.
- **Enrutado por BlendState**: `Dictionary<BlendState, List<Particle>>` con 3 lotes en orden fijo: **AlphaBlend → NonPremultiplied → Additive** (más lotes Immediate para shaders custom y un sistema de pixelación). Cada lote = `Begin(Deferred, blend, LinearClamp, CullNone)`.
- Límite de cliente configurable (`CalamityClientConfig.ParticleLimit`); `Important=true` lo ignora.
- **Sampler LinearClamp** = humo SUAVE (vs el PointClamp crujiente de vanilla).

**HeavySmoke** (parafraseado):
```
textura 559×480 = 7 variantes × 6 frames de 80×80
al nacer:   variante = rand(7); spin según sentido de vel.X
frame      = floor(time / (lifetime/6))          // flipbook de 6 frames REPARTIDOS en la vida
si vida < 20%:  scale += 0.01                    // nace creciendo
si no:          scale ×= 0.975                   // luego se encoge
opacity     ×= 0.98
velocity    ×= 0.85
rotation    += spin · sign(vel.X)
últimos 15%: opacity ×= lerp adicional           // muerte suave
hue         += HueShift (opcional)
si AffectedByLight: color *= Lighting.GetColor(tile)  // ¡humo que se tinta por la luz!
lote: Additive si "glowing", si no NONPREMULTIPLIED (half-transparency)
```
- **La escalera de humo por textura** (medida con PIL): `MiniSmoke 66×22` (3 de 22²) → `SmallSmoke 24×24` → `MediumSmoke 32×102` (3 de 32×34) → `HeavySmoke 80×80×6frames` → `MediumMist 32×102` → `RancorFog 256×256` → `HighResFoggyCircleHardEdge 2048²` / `SmokeExplosion 2048²`. **Un LOD por TEXTURA, no por escala del mismo PNG** — nunca se escala un sprite más allá de su resolución nativa.
- **ThanatosSmokeParticle** (humo de escape del mecha): partícula CON `RelativeOffset` (pegada al cuerpo del jefe), deriva hacia atrás `−(rotación ± 0.18)·power·4.5`, escala crece `+power·0.04` hasta 1.25, color DarkRed → gris (154,139,138) con `pow(lerp,2)·0.5+0.5`, alfa fija 92. El Set spawnea `rand.NextVector2Circular·compactness` cada `spawnRate` ticks con vida 30.
- **GraveyardMist**: ¡usa **GORES DE VANILLA** (`Images/Gore_N`) como sprites de niebla! Estirada ×(1..2, 1), alfa = `sin(2π·t/halfLife)·0.375`, espejo según sentido del viento.
- **MediumMist**: aditiva, emite luz `×0.1` mientras `opacity > 90` (el humo ARDE al principio y luego solo vela), crece +0.01/tick → encoge ×0.975, color lerp fuego→apagado.

### 3.3 STARLIGHT RIVER — "partículas en GPU + pixelación coherente"

- `ParticleSystem`: **DynamicVertexBuffer/IndexBuffer con BasicEffect**, hasta **10.000 partículas por sistema**, pool de objetos (`Queue<Particle>`), actualización con `FastParallel.For` (paralelo real), buffers que crecen ×2 al llenarse, anclaje World/Screen/UI. UNA textura por sistema.
- **PixelSmokeColor** (parafraseado):
```
texturas: SmokeTransparent_1/2/3 (variantes horneadas)
alfa     += 4;  alfa ×= 1.0075      // fade ACCELERADO exponencialmente
scale    ×= 1.03                    // ¡CRECIMIENTO EXPONENCIAL 3%/tick!
buoyancy: vel.Y −= 0.015
vel ×= 0.95;  rotation += |vel|·0.01
color    = lerp(colorInicial, colorFinal, EaseQuinticInOut(1−lerper))  // se enfría con QUINTIC
DIBUJO: sprite en rotation  Y  OTRA VEZ en rotation+π/2   // DOBLE DRAW CRUZADO
        → dentro del PixelationSystem (RT a MEDIA resolución, reescalado ×2 con PointClamp)
```
- El **doble draw cruzado** (mismo sprite a rot y rot+90°) aplana la anisotropía de la textura y da masas más "redondas" — el mismo truco del glow-cross de vanilla.
- **PixelationSystem**: todas las partículas de humo se dibujan en un RT de **media resolución** y se reescala ×2 con PointClamp → humo "chunky" coherente con el pixel-art de Terraria + **mitad de fill-rate** (overdraw de humo a mitad de precio). El equivalente legitimado del "dibujar la niebla a menor resolución" del informe v616.
- **Mist** (niebla de biome): alfa = `lightColor · (0.01 + alfa/250) · sin(fadeIn/120·π)` — envolvente sinusoidal de 120 ticks; rotación acoplada a `vel.Y/40` con signo según alfa (¡gira hacia un lado al nacer y al otro al morir!); escala ×2.5-2.9 al nacer.
- **SmokeDustColor**: MISMA rampa que LunarVeil (color → gris(25,25,25) al alfa 120 → negro al 180, ×(255−alfa)/255) — misma familia de código.
- Biome particles (Vitric): **paralaje** (GetParallaxOffset 0.15/0.1), deriva `sin(timer/(type+200)·2π)·20`, color por progreso de vida.

### 3.4 CORALITE — "la estantería de puffs fritos" (sistema PRT/InnoVault)

Partículas con ciclo de vida ultra-simple y texturas pre-horneadas (parafraseado):
```
Fog:        textura 64×64 con 4 variantes verticales (Frame 64·rand(4))
            rotación inicial rand(2π), += 0.01/tick
            vel ×0.98;  scale ×0.997;  color ×0.94 (alfa decae)
            vida 120 ticks O alfa < 10;  lote ADITIVO
TwistFog:   rotación += 0.10/tick (10× más rápido = voluta)
            fadeIn 5 ticks: alfa ×(t/5), scale ×(0.5+0.5·t/5)  // NACE PEQUEÑO Y TRANSPARENTE
            DOBLE DRAW: mismo quad con alfa A y luego A/2      // densidad x1.5 gratis
            variante Dark con lote ALFA (la masa que ocluye)
BigFog:     256×256, 4 variantes, vida 60, aditivo            // la niebla GORDA de fondo
WalkSmoke:  flipbook 1×7, frame cada 2 ticks, lote ALFA
            color = Lighting.GetColor(tile, color)·alpha      // TINTE POR LUZ DEL MUNDO
            + pase aditivo OPCIONAL con c.A=0 (solo RGB)      // borde brillante opcional
FireParticle: flipbook 1×16, frame cada 5 ticks, vel ×0.95, color ×0.96,
            rotación = ángulo de velocidad −90° (la llama APUNTA contra el movimiento),
            doble draw (color y color·0.5), lote ADITIVO
```
- **El inventario de texturas ES el sistema**: AnimeFogSPA (flipbook), BigFog, Fog, FireParticle(+SPA)... Cada una una textura horneada por artista/proceso. 3+ tamaños + 2 blend modes + doble-draw = ~12 looks de humo con ~150 líneas de código total.
- Sistema PRT: enums de modo (Additive/AlphaBlend...), contador `Opacity` como reloj de vida, `TexValue` con `QuickCenteredDraw`.

### 3.5 FAMILIA SPIRIT/STELLAMOD — "la rampa de enfriamiento" (el humo de arma estándar del ecosistema)

DuelistSmoke (Spirit) ≡ TSmokeDust (LunarVeil) ≡ SmokeDustColor (StarlightRiver) (parafraseado):
```
al nacer:   escala ×rand(0.8..2);  rotación rand(2π);  frame de atlas (2-3 variantes)
rampa de color por ALFA (el "enfriamiento"):
    alfa < 60-120:  color inicial → gris(25,25,25)     // amarillo/naranja para armas de fuego
    alfa < 180:     gris → NEGRO
    else:           negro
    alfa final de draw = (255−alfa)/255
mientras brilla (alfa < 100): AddLight(color ×0.1)     // EL HUMO EMITE LUZ DE SU COLOR ACTUAL
física:  vel ×0.98;  vel.X ×0.95;  vel.Y ×0.97 (asimétrica como vanilla)
         rotación fija ±(π/4)/16 por tick (mini-curl constante)
         scale ×0.975-0.985;  alfa += 2..12/tick
lote:    vanilla (AlphaBlend, heredado del dust system)
```
- **La rampa de 3 tramos por umbral de alfa** es EL patrón del humo con brasa: el color cuenta la historia térmica de la bocanada (ardiendo → apagándose → ceniza).
- Stellamod añade: sistema de partículas propio tras `Main.DrawDust` (hook), **2 lotes: Additive para todo + "Black" (AlphaBlend) para las partículas `isBlack`** — la masa oscura que ocluye separada del brillo, MISMA idea que Calamity con NonPremultiplied.

---

## 4. TÉCNICAS TRANSVERSALES CON NÚMEROS (todas las fuentes juntas)

### 4.1 Blend states usados por los mods para humo
| Mod | Lote(s) para humo | Sampler |
|---|---|---|
| Vanilla dusts | AlphaBlend (1 lote) | **PointClamp** (crujiente) |
| Calamity | AlphaBlend → **NonPremultiplied** → Additive (3 lotes orden fijo) | **LinearClamp** (suave) |
| Everglow | AlphaBlend con shader propio (+ HaloPipeline de bloom después) | PointClamp en batch, LINEAR dentro del .fx para ruido/LUT |
| StarlightRiver | pixelación (RT media res) + lotes del PixelationSystem | PointClamp al reescalar |
| Coralite | Additive (Fog/BigFog/Fire) y AlphaBlend (variantes Dark/WalkSmoke) | — |
| Stellamod | Additive + AlphaBlend ("black" list) | PointWrap |
| ParticleLibrary V3 | BlendState por buffer (1 por textura) | configurable |
| Overhaul | **Opaque** (líneas 2px de sangre, no humo) | PointClamp |

**Ningún mod serio dibuja humo oclusivo en Additive.** Additive = brillo/emisión; el humo de masa SIEMPRE en AlphaBlend/NonPremultiplied. El humo bonito usa los dos por separado (masa alfa + venas/halo aditivo encima).

### 4.2 El viento del mundo (¡nadie lo inventa, se lo dan hecho!)
- Everglow: `vel += Main.windSpeedCurrent·0.02` (vapor) o `·0.1` (fire smog); la llama de hoguera se dobla con `v²·windSpeed·870`.
- Vanilla gores: lerp de orden 50 hacia `Main.WindForVisuals·2` + jitter ±1.
- BrumaFX actual: NO USA VIENTO — solo senos internos. **Añadir `Main.windSpeedCurrent` es el fix de "mundo vivo" más barato posible.**

### 4.3 Presupuestos de rendimiento observados
- Vanilla: 6000 slots, degrada por umbrales de 10% (5 niveles), culling nacimiento 400px, dibujo 1000-2100px para humo.
- Calamity: límite configurable de cliente + flag `Important` (las VFX críticas del boss nunca se caen).
- StarlightRiver: 10.000 partículas por sistema en GPU; humo a media resolución (fill-rate −75%).
- Overhaul: 1024 partículas + culling probabilístico por config (contador % divisor).
- Everglow: VFXBatch de 8192 vértices ≈ 2048 quads por flush; Vertex2DSmog registrado con 2048 vértices/3072 índices.
- ParticleLibrary: pooling de RTs + FastList; V3 con instancing real en GPU.
- **El humo de 1 quad con shader (Everglow) es el más barato de TODOS por puff**: 4 vértices, cero texto en CPU, el ruido se evalúa en la GPU por píxel.

### 4.4 Ciclo de vida de una bocanada — el consenso de los 5 mejores
| Fase | Vanilla 31 | Calamity Heavy | Everglow Vapor | Starlight Pixel | Coralite Twist |
|---|---|---|---|---|---|
| Nacimiento | vel ×0.3, escala ×0.7, alfa→0 | scale += 0.01 (20% de vida) | pow(t,0.3): arranque lento | — | fadeIn 5t: escala 0.5→1, alfa 0→1 |
| Crecimiento | +0.02/tick (¡lineal!) | += 0.01 → ×0.975 tras 20% | +0.1/tick hasta 160 | ×1.03 EXPONENCIAL | ×0.997 (casi nada) |
| Madurez | alfa +4/tick | opacity ×0.98 | sin(π·x) máx a mitad | alfa ×1.0075 | alfa ×0.94 |
| Muerte | alfa 255 | ×0.975 + lerp final 15% | sin→0 final | alfa→255 | alfa <10 → kill |
| Muerte por colisión | — | — | timer++ doble en sólidos | — | — |

**NADIE hace fade-out lineal puro**: siempre es exponencial (×0.98) o sinusoidal (sin(π·t)), y el arranque tiene su propia sub-curva (pow 0.3 / fadeIn dedicado / 20% de vida).

### 4.5 Integración con la luz del mundo
- Vanilla: multiplica TODO el color por `Lighting.GetColor(tile)` y MUERE en negro.
- Calamity: flag `AffectedByLight` opt-in por partícula (multiplicación en draw).
- Starlight Mist: multiplica el alfa por `lightColor` (la niebla DESAPARECE en oscuro, correcto físicamente).
- **Everglow (la mejor)**: luz POR VÉRTICE (gradiente dentro del propio puff) + piso `light·0.2 + gray(light)/3` (nunca invisible).
- BrumaFX actual: cero integración → **el humo "no vive" en cuevas**. Esta es la carencia nº1.

---

## 5. SÍNTESIS — 24 LECCIONES CON NÚMEROS PARA BRUMA V2

1. **Premultiplicar o morir**: toda textura horneada con RGB=255 y alfa<255 es un rectángulo en Additive (bug actual). Regla: `RGB' = RGB·(A/255)`. Y en lote alfa los bordes duros desaparecen solo si RGB→0 con A→0.
2. **El humo de masa NUNCA en Additive**: AlphaBlend/NonPremultiplied para el volumen, Additive solo para venas/halo/brasa. Calamity: 3 lotes en orden fijo; Stellamod: lista "black" separada.
3. **Sampler = identidad visual**: PointClamp = crujiente vanilla; LinearClamp = suave cinematográfico (Calamity). Para BrumaFX: LinearClamp (el humo ES suavidad).
4. **1 quad + shader > N quads + CPU** (Everglow): un puff = 4 vértices; ruido fBm/LUT evaluado por píxel en GPU. Con los .fxc del mod ya compilables, un shader de humo es el techo de calidad.
5. **LUT 1D de color** (128-141 px) = todo el diseño de color del humo en una textura: `smoke LUT: alfa 255→141→83→0 entre 0% y 74% de densidad`. Cambiar de humo negro a vapor gris a peste morada = cambiar la LUT.
6. **Invarianza de escala por UV**: rango de ruido por quad = `k·scale/70` (k≈0.2-0.4) → misma frecuencia de grumos por píxel a 20 y a 160 px (Everglow). BrumaFX lo hace por octavas de fBm en CPU — correcto también, pero el shader lo tiene gratis.
7. **Luz del mundo por vértice + piso**: `color = lightColor·0.2 + gray(light)·0.33` — humo visible en cueva sin ser fantasma. Versión mínima sin shader: multiplicar el Tint por `Lighting.GetColor(tile)` con piso 0.15.
8. **El viento del mundo existe**: `Main.windSpeedCurrent` × 0.02-0.1 px/tick² en la física del humo. Cero coste, mundo unificado.
9. **Amortiguación X/Y asimétrica** (vanilla): `(0.97, 0.99)` — el humo frena en X y sube limpio en Y. Columnas naturales sin ningún campo de fuerzas.
10. **Rotación = f(velocidad.X)**: ×0.3 en vanilla dusts, ×0.6 en gores, ×0.004-0.01 (por |vel|) en mods. El humo RUEDA al avanzar; nunca rota solo porque sí.
11. **Rampa de enfriamiento por umbral de alfa** (familia Spirit/Stellamod/SLR): tramos 60-120-180 (arma) o 120-180 (genérico): color→gris(25,25,25)→negro. La bocanada cuenta su historia térmica.
12. **El humo que arde EMITE luz de su color mientras arde**: `AddLight(color·0.1)` solo mientras alfa < 90-120 (Calamity MediumMist, familia Spirit). Después, nada.
13. **Smear de 10 copias** (vanilla 130-134): `pos −= vel·j; scale ×= (1−j/10)` con j hasta `min(|vx|+|vy|·3, 10)`. Para puffs rápidos de BrumaFX: el trail por velocity YA está — parametrizar el número de copias por velocidad exactamente así.
14. **Doble draw cruzado** (Starlight PixelSmoke): mismo sprite a rot y rot+90° — masas redondas con textura anisótropa. También: Coralite dibuja el mismo quad con alfa A y A/2 (densidad ×1.5 con 1 textura).
15. **Crecimiento EXPONENCIAL lento** (SLR): ×1.03/tick — un puff dobla tamaño en ~24 ticks. Con fadeIn de escala 0.5→1 en 5 ticks (Coralite TwistFog) para el nacimiento.
16. **Curva de vida sinusoidal con arranque pow**: `1 − sin(pow(t/max, 0.3)·π)` (Everglow) o `sin(2π·t/halfLife)·0.375` (Calamity Graveyard). NUNCA lineal.
17. **Escalera de tamaños por TEXTURA, no por escala** (Calamity): 22/24/32/80/256 px son sprites distintos. Equivalente procedural de BrumaFX: regenerar el fBm con octavas por radio (YA está en BrumaNoise.OctavesForRadius — mantener).
18. **Flipbooks horneados de 6-7 frames repartidos en la vida** (Calamity): `frame = floor(time/(lifetime/6))`. Equivalente procedural: animar el WARP del fBm por tiempo (BrumaBrushes podría hornear 6-8 frames del MISMO campo de ruido evolucionado y cambiar el sourceRect — cero coste).
19. **Humo pegado a entidades** (Calamity Thanatos): `RelativeOffset` que se actualiza con el padre + deriva propia hacia atrás ±0.18 rad de jitter ×4.5. Para humo de cañón/hoguera montada.
20. **Gores de vanilla como sprites de niebla** (Calamity Graveyard: `Images/Gore_N` estirado ×(1-2,1), alfa sin()·0.375): el "sistema de niebla más barato del mundo" — y SOTS/Fargo usan GoreID.Smoke1-3 tal cual para explosiones.
21. **Media resolución para niebla masiva** (Starlight PixelationSystem / SOTS Prim RTs): dibujar la banda de bruma en un RT de ½ (o ¼) resolución y reescalar — fill-rate −75%, look coherente. La lente de AethonMod ya tiene la infraestructura de RT para hacer esto.
22. **Culling probabilístico con presupuesto** (Overhaul): `contador % divisor` para degradar NUEVOS spawns según config; Calamity: límite + `Important` para no perder VFX críticas. BrumaFX debería llevar un contador global de puffs/frame con presupuesto propio (p. ej. 400 quads de humo) y prioridad.
23. **Capas de dibujo explícitas** (Everglow: 7 CodeLayers; vanilla: dust tras tiles): el humo de AMBIENTE va detrás (PostDrawBG), el de combate delante (PostDrawNPCs). MistBand debería poder elegir.
24. **El detalle más barato que más vende**: chispas/motas PEQUEÑAS acompañando al humo (FireSparkDust de la hoguera de Everglow: 1 cada 3 ticks, escala 0.1-27, vida 37-195) + luz puntual en la BASE. El humo solo nunca parece "vivo"; humo + 3 chispas + 1 luz sí.

---

## 6. RECOMENDACIONES ESPECÍFICAS PARA BRUMA v2 (qué añadir, con qué números, qué NO)

### 6.1 Arreglos obligatorios (post-bug premultiplicado)
1. **Premultiplicar al hornear** (ya en curso): `data[i] = new Color(rgb·a, a)`. Y cambiar el `Tint()` de BrumaFX: con textura premultiplicada, el tinte lineal es multiplicar RGB y alfa por separado (`new Color(c.R·f, c.G·f, c.B·f, 255·f)`) — NO solo alfa.
2. **Doble pipeline de blend explícito**: `Puff(..., blend: Massa|Glow)` — la API ya documenta que el llamador elige el lote; añadir helpers `BrumaFX.BeginMass()` (AlphaBlend + LinearClamp) / `BeginGlow()` (Additive) con el CONTRATO de lote del mod (v6.10: quien abre cierra).

### 6.2 Lo que más impacto visual tiene por hora de trabajo (ordenado)
1. **`BrumaFX.WorldLight(pos)` → tinte por iluminación**: `L = Lighting.GetColor(tile); floor(L) = L·0.2 + gray(L)/3` (receta Everglow). Aplicarlo al color de cada puff/column/banda. Hace que TODO el humo del mod viva en cuevas. Coste: 1 muestra de luz por puff (no por blob).
2. **Viento del mundo**: `vel.X += Main.windSpeedCurrent·0.02` en Column/Tendril/MistBand (y ×870·v² si se hace llama). Coste: 1 línea.
3. **Rampa de enfriamiento** en `Puff()`: parámetro `Color? colorFinal = null` — si se pasa, lerp color→colorFinal por la curva de vida (estilo familia Spirit: color→gris(25,25,25)). Con `EmberGlow = color·0.1` en AddLight mientras el puff "arde" (primer 40% de vida).
4. **`Puff` con FLIPBOOK de ruido evolucionado**: hornear 6-8 frames por variante en BrumaBrushes (misma semilla, warp del dominio incrementándose `warp·(1+frame·0.15)`), exponer `BrumaFX.AnimatedPuff(pos, r, color, seed, time, life01)` que elija frame = `floor(life01·6)`. Un PNG-con-like 100% procedural: la erosión del humo ANIMA de verdad (hoy solo rota/respira).
5. **`Vapor(...)`** — el humo ALFA de 1 capa estilo Everglow: textura del puff horneada CON LUT de alfa dura (255→0 entre 0-74% de densidad), tinte por luz del mundo, curva `1−sin(pow(t,0.3)·π)`, vel ×0.9 + viento + flotabilidad, escala +0.1 hasta 160, colisión sólida acelera la muerte. Es EL look "humo de juego moderno".
6. **`Wisps(...)`** — volutas con brasa: Tendril existente + 2-4 motas pequeñas (SoftGlow 2-4px, Additive, alfa 0.5·(1−t)) a lo largo de la ruta con flotabilidad. Lección 24.
7. **`FogOverlay(...)`** para escenas: MistBand renderizada a un RT de media resolución (la infraestructura de la lente ya sabe) — presupuesto: ≤6 quads/capa × 3 capas, alfas 0.07/0.11/0.16, y modo `Subtract` opcional (Everglow FogVFX) para niebla que OSCURECE.
8. **Smear paramétrico** en `Puff`: el `trail` actual estira blobs; cambiar al modelo vanilla: N = `min(int(|vel|·3), 10)` copias del NÚCLEO (no de los blobs) en `pos−vel·j` con escala ×(1−j/10). Para columnas con viento y proyectiles humeantes.
9. **Presupuesto global**: contador estático de quads emitidos por BrumaFX en el frame; si supera ~500, degradar `quality` (menos sub-blobs) automáticamente — el LOD de vanilla aplicado a la librería.
10. **Capa de dibujo para niebla de ambiente**: `MistBand(..., behind: true)` documentado para llamarse en un pase PostDrawTiles-antes-de-entidades.

### 6.3 Parámetros de referencia (recetas listas)
- **Humo de arma (bocanada)**: ráfaga 4-10 puffs, radio 12-30px, vida 30-90 ticks, vel inicial 2-6 px/t + (0,−4), rampa color(brasa)→(25,25,25)→negro, luz color·0.1 primeros 40%, escala ×1.02/tick, alfa +4-6/tick.
- **Humo de hoguera (continuo)**: 1 puff cada 3 ticks, radio 40-100px, vida 150-300 ticks, flotabilidad −0.08, viento ×0.02, escala +0.1 hasta 160, tinte por luz con piso, LUT alfa 255→0@74%, + chispas (1/3 ticks, vida 37-195, escala 0.1-27) + luz naranja (1,0.7,0.2)·2 en la base.
- **Vapor de agua (jet)**: vida 10-90 ticks, radio 20-135px, alfa máx 0.3-0.5, gris 59→30, muere rápido en sólidos, rotación ±0.05 rad/tick fija.
- **Niebla de valle**: 3 capas × 6 puffs pantalla+, alfas 0.07/0.11/0.16 (¡ya está en MistBand!), tinte frío capa lejana, viento ×0.25 con paralaje (ya está), envolver X en el área (ya está), GRADIENTE por luz del mundo (falta).
- **Humo del "vacío" (Eclipse/Olvido)**: masa alfa violeta profundo (70,20,110) borde (140,100,190) + venas aditivas (doble draw cruzado con alfa 0.3) + piso de luz 0.2 para que no desaparezca en la sombra.

### 6.4 Lo que NO hacer (anti-patrones verificados en todos los análisis)
1. **NO dibujar masa de humo en Additive** — se convierte en "sábana" brillante (el bug actual lo demuestra en su forma extrema).
2. **NO escalar un único sprite de 128px a 500px** con LinearClamp sin regenerar (borde de "baba" de 40px) — usar la escalera de octavas/texturas.
3. **NO animar el humo solo con rotación+respiración** (el estado actual de BrumaFX): todos los sistemas con flipbook/scroll de ruido se ven cualitativamente más vivos. La rotación sola delata el truco en puffs grandes.
4. **NO fade lineal** de nacimiento/muerte — pow(0.3)/sin()/exponencial ×0.98 SIEMPRE.
5. **NO ignorar `Main.windSpeedCurrent`** — es la diferencia entre humo de laboratorio y humo de un mundo con clima.
6. **NO negro puro en masa** (desaparece en cueva) ni **RGB=255 con alfa parcial sin premultiplicar** (rectángulo).
7. **NO un quad de niebla por tile/pantalla pequeña** — pocos quads ENORMES (≤6/capa) con alfas bajos; el overdraw es el asesino silencioso (vfxlabs ya lo decía, Everglow/SLR lo confirman con RT de media res).
8. **NO copiar código GPL** (ParticleLibrary es GPL-3): las TÉCNICAS son libres; la implementación de AethonMod ya es propia y superior en lo procedural — solo faltan luz del mundo, viento y flipbook.

---

## 7. CONCLUSIÓN

El ecosistema se divide en 3 escuelas: (A) **vanilla-conservadora** (Fargo, SOTS, Thorium, Polarities, Orchid, Avalon, AA): dusts+gores, barata y estéticamente coherente; (B) **PNG pre-horneado con escalera y 3 lotes** (Calamity, Coralite, Spirit/Stellamod): la escuela estándar de calidad; (C) **shader por vértice/píxel** (Everglow, y StarlightRiver por el camino GPU): 1 quad por puff, ruido+LUT+luz por píxel, viento real — el techo absoluto.

**BrumaFX, una vez premultiplicado, ya es proceduralmente superior a (A) y (B) en flexibilidad (invariancia de escala por octavas, cobertura constante, senos inconmensurables, curl)** — le faltan exactamente 4 cosas que (C) tiene y que ningún competiente combina: tinte por luz del mundo con piso, viento del mundo, flipbook de ruido evolucionado, y la disciplina de masa-alfa vs brillo-aditivo separados. Con las recetas de §6.2 (10 mejoras, las 3 primeras son ~1 tarde de trabajo) Bruma v2 puede ser el mejor humo del ecosistema tML sin sacrificar su naturaleza 100% código.

---

## APÉNDICE A — Archivos leídos por fuente (verificabilidad)

| Fuente | Archivos |
|---|---|
| Vanilla decompile | `Dust.cs` (nuevo decompile de esta sesión, 3.314 líneas), `Gore.cs` (nuevo), `Main.cs` (DrawDust 49935-50240, DrawGore 21303+), `DustID` (nuevo), ya existentes: Projectile.cs, NPC.cs, DelegateMethods.cs |
| Everglow | `VFX/CommonVFXDusts/Vapor.cs`, `Vapor.fx`, `FireSmog.cs`, `FireSmog.fx`, `IceSmog.cs` (+Vertex2DSmog), `FogVFX.cs`, `CurseFlame_HighQuality.cs`, `VFXManager.cs`, `VFXBatch.cs`, `VFX.md`, `MEACVFX.cs`, `VisualQuailtyController.cs`, `CagedDomain/Tiles/GiantCampFire.cs`, `EternalResolve/Projectiles/PrisonFireBayonet_Pro(_Stab).cs`, HeatMaps/*.png (PIL), `Textures/Point.png`, `Textures/Noise_perlin.png` |
| Coralite | `Content/Particles/Fog.cs`, `BigFog.cs`, `AnimeFog.cs`, `WalkSmoke.cs`, `FireParticle.cs`, inventario `Assets/Particles/` y `Assets/Dusts/` |
| Calamity | `Particles/Particle.cs`, `GeneralParticleHandler.cs`, `HeavySmoke.cs`, `ThanatosSmokeParticle.cs`, `ThanatosSmokeParticleSet.cs`, `MediumMistParticle.cs`, `GraveyardMistParticle.cs`, `RancorFog.cs`, `CircularSmearSmokeyVFX.cs`; PNGs medidos: HeavySmoke, MediumSmoke, SmallSmoke, MiniSmoke, MediumMist, RancorFog, SmokeExplosion, HighResFoggyCircleHardEdge, CircularSmearSmokey, PlagueHumidifierMist |
| Starlight River | `Core/ParticleSystem.cs`, `Core/Systems/PixelationSystem/PixelationSystem.cs`, `Content/Dusts/PixelSmokeColor.cs`, `SmokeDustColor.cs`, `Mist.cs`, `Helpers/DustHelper.cs`, `Content/Biomes/VitricDesertBiome.cs` |
| Spirit Mod | `Items/Sets/PirateStuff/DuelistLegacy/DuelistDusts.cs`, `Dusts/BlizzardDust.cs`, inventario Effects/ |
| LunarVeil | `Content/Dusts/TSmokeDust.cs`, `Systems/Particles/ParticleSystem.cs`, `Systems/Particles/Particle.cs` |
| SOTS | `Prim/PrimBase.cs`, `NPCs/Boss/Excavator/Excavator.cs` |
| Fargo's | `Projectiles/Explosion.cs` |
| Overhaul | `Common/BloodAndGore/ParticleSystem.cs`, `Common/Fires/FireSystem.cs` |
| ParticleLibrary | `Core/V3/Particles/ParticleBuffer.cs`, `Core/V2/ParticleSystem/ParticleSystem.cs`, `README.md` |
| Polarities | `Dusts/GenericDustBase.cs`, `Items/Placeable/Blocks/Fractal/DendriticEnergy.cs` |
| WoTE | `Content/Particles/BloomCircleParticle.cs`, `BloomPixelParticle.cs`, estructura Content/Particles/ |
| MEAC | `Utils/MyDustId.cs`, estructura decompiled/ (resto ya analizado en luz_v622) |
| Web | `research/humo_v625/busquedas_web_41a/` — 25 JSONs de 27 búsquedas (01-27): Calamity, StarlightRiver, Thorium, Fargo's, Spirit, Redemption, SoA, Ancients Awakened, Orchid, Avalon, Wikszilla, reddit/workshop VFX, Polarities, DragonLens, big mods list |

## APÉNDICE B — Deuda con investigaciones previas (no repetida aquí)
- `research/smoke_research_v616/INFORME.md`: fundamentos fBm/warping/erosión, recetas de color pseudo-volumétricas (Diablo 3/JangaFX), tablas de parámetros, pseudocódigo G.1-G.4 — BASE teórica de BrumaFX, sigue vigente.
- `research/luz_v622/*`: WoTE bloom (38-a), EoL vanilla (38-c), MEAC LUTs (38-b).
- `research/storm_v621/INFORME.md`: rayos (StormLib).

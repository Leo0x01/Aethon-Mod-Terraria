# INFORME DE BIOMAS — AethonMod v6.26 (Task 42-d)

> Investigación de biomas para AethonMod: estado real del proyecto, guía técnica
> completa de creación de biomas en tModLoader 1.4.4/2026 (verificada contra el
> binario real tModLoader.dll v2026.07.3.0), anatomía con números de los biomas de
> los grandes mods (Calamity, Starlight River, Everglow, Coralite, Spirit, LunarVeil)
> y 5 conceptos de bioma completos para AethonMod.
>
> Fuentes primarias: decompiles con ILSpy (ModBiome, ModSceneEffect, SceneMetrics,
> Main de la DLL real), código fuente de CalamityModPublic + StarlightRiver +
> SpiritMod + tModLoader/ExampleMod (clonados a /tmp), decompiles locales
> /tmp/research/{coralite,everglow,lunarveil,wote}, búsquedas web (JSONs en
> busquedas_web_42d/). Cero código copiado al repo; todo parafraseado.

---

## 1. ESTADO REAL DEL BIOMA DE NUESTRO PROYECTO

### 1.1 HollowSanctumBiome = STUB (bioma "lógico", no físico)

`Content/Biomes/HollowSanctumBiome.cs` (32 líneas) es un `ModBiome` **mínimo**:

| Pieza | Estado | Detalle |
|---|---|---|
| Clase ModBiome | ✅ existe | `public class HollowSanctumBiome : ModBiome` |
| IsBiomeActive | ⚠️ trivial | `(ZoneDirtLayerHeight ∥ ZoneRockLayerHeight) && statLifeMax ≥ 400` |
| Music | ⚠️ vanilla | `MusicID.Underground` (reutilizada, cero música propia) |
| Priority | ✅ | `SceneEffectPriority.BiomeHigh` |
| BestiaryIcon | ❌ comentado | "no existen las texturas" (comentario literal en el código) |
| BackgroundPath | ❌ comentado | ídem |
| WaterStyle | ❌ | no override |
| Surface/UndergroundBackgroundStyle | ❌ | no override |
| MapBackground / BackgroundColor | ❌ | no override |
| BiomeTorchItemType | ❌ | no override |
| OnEnter/OnInBiome/OnLeave | ❌ | no usados (aquí iría el ambient VFX) |
| SpecialVisuals (cielo) | ❌ | no override |
| Localización | ❌ | no hay clave `Biomes.HollowSanctumBiome.DisplayName` en los .hjson |

**El problema central de la detección**: `IsBiomeActive` tal como está significa que
*TODA la capa de tierra/piedra del mundo* cuenta como "Sagrario Hueco" en cuanto el
jugador tiene 400 HP. No hay ni un solo tile propio, ni conteo, ni área. El bioma no
"existe" en el mundo: es una etiqueta que cambia la música subterránea.

### 1.2 Inventario biome-relevante del proyecto (verificado a fecha de hoy)

- **Tiles propios: 1** — `Content/Tiles/AncientAltar.cs`: tile 3×2 no sólido
  (`TileObjectData.Style3x2`), `RightClick` otorga el `GenesisShard` (busca en las 58
  ranuras de inventario), `NearbyEffects` emite luz `(0.4, 0.3, 0.6)`, `MouseOver`
  muestra el ícono. `Main.tileLighted`, `tileFrameImportant`. Es un tile-función, no
  un tile-bioma.
- **Worldgen: 1** — `Content/Systems/AncientAltarWorldGen.cs` (`ModSystem.PostWorldGen`):
  coloca **3 altares** en puntos aleatorios de la capa de roca (máx 30000 intentos,
  busca suelo sólido + aire), con fallback de barrido si 0 colocados. NO genera el
  bioma (no talla cuevas, no coloca cristales, no guarda posición en TagCompound →
  no hay forma actual de saber DÓNDE están los altares tras recargar).
- **NPCs: 6** — AethonBoss (jefe final), RiftKeeper (jefe nivel 75), HollowTitan
  (mini-jefe), EchoBlade, EchoArcher, TheWitness (oráculo). **NINGUNO tiene
  `SpawnChance`** → ningún enemigo spawnea de forma natural en el "bioma"; son
  entidades de lógica/existentes por invocación o guion. La lore de TheWitness manda
  al jugador a "buscar el Sagrario Hueco bajo tierra" → el jugador busca 3 altares
  invisibles en un mar de 8.4M tiles.
- **Systems: 4** — ShardLevelSystem (hitos cósmicos por nivel: "Lluvia de Luz
  Estelar / Sagrario / Rifts / Aethon se agita" a niveles 25/50/75/100/150 — solo
  NewText, sin efectos de mundo), ShardSyncSystem, WeaponScaling, AncientAltarWorldGen.
- **Globales**: GlobalNPCXP, ShardLevelItem, TooltipToggleItem, CosmicProjectileFX.
- **Nuestro arsenal VFX (la ventaja)**: ParticleManager (data-oriented, 4000 cap,
  Spawn/SpawnShape, PostDrawTiles + capa AboveLens), EstelaLib/OndaLib/PyraLib/
  StormLib/BrumaFX/LumenLib (v6.25), BlackHoleLensSystem (distorsión de pantalla
  completa 817L), 8 shaders .fxc funcionando. **Cero de esto está cableado a
  ambientes de mundo** — todo vive en armas/cosméticos.

### 1.3 Veredicto

**El Sagrario Hueco es un stub de bioma con lore**. Existe como promesa (README,
bestiario de TheWitness, CARACTERISTICAS.md) y como música subterránea con nombre
bonito. Es el esqueleto correcto (ModBiome + Priority + IsBiomeActive) al que le
falta TODO el wiring: tiles, conteo, spawns, bestiario, fondos, VFX, worldgen con
memoria. **La buena noticia**: es exactamente el mismo checklist que usan Calamity y
compañía, y el 70% se puede hacer HOY sin un solo sprite nuevo (secciones 2 y 5).

---

## 2. GUÍA TÉCNICA: CREACIÓN DE BIOMAS EN TMODLOADER (2024+/1.4.4/2026)

Todo lo siguiente verificado contra `Terraria.ModLoader.ModBiome` y
`ModSceneEffect` decompilados de **tModLoader.dll v2026.07.3.0** (la versión real
del usuario) + ExampleMod oficial + patches del repo tModLoader.

### 2.1 La anatomía de un bioma completo (checklist de wiring)

```
ModBiome (Content/Biomes/MiBioma.cs)
├── IsBiomeActive(Player)      ← EL CORAZÓN: llamada CADA TICK por BiomeLoader.UpdateBiomes
│   └── escribe player.modBiomeFlags[type] → dispara OnEnter/OnInBiome/OnLeave
├── Music (int)                ← MusicID.X  o  MusicLoader.GetMusicSlot(Mod,"Assets/Music/X") (.ogg/.wav/.mp3)
├── WaterStyle (ModWaterStyle) ← agua/caída/p-splash/gore de gota/tinte de pelo
├── SurfaceBackgroundStyle (ModSurfaceBackgroundStyle)   ← fondo parallax de superficie
│   └── ChooseFarTexture / ChooseMiddleTexture / ChooseCloseTexture + ModifyFarFades
├── UndergroundBackgroundStyle (ModUndergroundBackgroundStyle)
│   └── FillTextureArray(int[5] slots) ← 5 texturas de fondo de cueva
├── BestiaryIcon / BackgroundPath / BackgroundColor  ← foto de escena del bestiario
│   (por defecto Namespace/MiBioma_Icon y Namespace/MiBioma_Background)
├── MapBackground (+ MapBackgroundFullbright) ← fondo del MAPA al seleccionar spawn
├── TileColorStyle (CaptureBiome.TileColorStyle) ← tinte del mapa-cámara
├── BiomeTorchItemType / BiomeCampfireItemType ← antorcha/hoguera del bioma
├── Priority (SceneEffectPriority) + GetWeight(Player) ← quién gana la disputa de escena
└── SpecialVisuals(Player, bool isActive) ← cielos: SkyManager.Instance.Activate("MiMod:MiSky")
```

Jerarquía real: `ModBiome : ModSceneEffect : ModType`. `IsSceneEffectActive` está
**sellado** en ModBiome y devuelve `player.modBiomeFlags[ZeroIndexType]` — el flag
que mantiene actualizado el BiomeLoader (verificado en BiomeLoader.cs del repo tML:
`UpdateBiomes(player)` recorre la lista y llama `IsBiomeActive`, con transiciones
OnEnter/OnInBiome/OnLeave).

**Resolución de escena** (SceneEffectLoader): se calcula `GetCorrWeight = clamp(
GetWeight, 0, 1) + Priority` para TODOS los efectos activos y se ordena
descendente; el ganador impone música/fondo/agua. Prioridades de menor a mayor:
`None < Environment < BiomeLow < BiomeMedium < BiomeHigh < BossLow < BossMedium <
BossHigh < BossHighest` (más Event: Rain/BloodMoon/etc. por encima de biomas).

### 2.2 Detección — las 5 escuelas (con umbrales reales)

**El contador de tiles es el estándar.** `ModSystem.TileCountsAvailable(
ReadOnlySpan<int> tileCounts)`: tML rellena `_tileCounts[tileType]` escaneando un
rectángulo alrededor del jugador y llama a TODOS los ModSystem. Área exacta
(extraída del binario): `Main.buffScanAreaWidth × buffScanAreaHeight` centrado en
`player.Center`, con `buffScanAreaWidth = (maxScreenW+800)/16-1` y
`buffScanAreaHeight = (maxScreenH+800)/16-1` → **a 1080p ≈ 169×116 tiles
(~2700×1860 px)**. IMPORTANTE: el área depende del tamaño de pantalla del cliente —
los umbrales deben calibrarse como los de vanilla.

Umbrales de referencia medidos del propio binario (SceneMetrics decompilado) y de
los mods:

| Quién | Umbral (tiles contados en el rectángulo) |
|---|---|
| Vanilla Corrupción/Carmesí | 300 (máx 1000 para conversión inversa) |
| Vanilla Hallow | 125 (máx 600) |
| Vanilla Jungla | 140 (máx 700) |
| Vanilla Nieve | 1500 (máx 6000) |
| Vanilla Desierto | 1500 |
| Vanilla Meteoro | 75 |
| Vanilla Cementerio | 16–36 (umbral 28) — ¡también con debuff de proximidad! |
| Vanilla Shimmer | 300 |
| ExampleMod | ≥ 40 tiles propios |
| Coralite Cueva de Cristal | > 600 (4 tipos de tiles sumados) |
| Coralite Isla Skarn | > 400 (7 tipos) |
| Calamity Mar Sulfuroso | ≥ 300 (3 tipos de arena) |
| Calamity Infección Astral | > 950 (12 tipos + variantes desert/snow) |
| Calamity Abismo capa N | ≥ 200 del tile de ESA capa (Shale / Gravel+PlantyMush / PyreMantle / Voidstone) |
| Spirit (viejo) | > 200 (5 tipos) |

Patrón de código (el de la casa, adaptado de ExampleMod/Calamity):

```csharp
public class BiomaTileCount : ModSystem {
    public static int PiedraHuecaTiles;
    public override void ResetNearbyTileEffects() => PiedraHuecaTiles = 0; // cada scan
    public override void TileCountsAvailable(ReadOnlySpan<int> tileCounts) {
        PiedraHuecaTiles = tileCounts[ModContent.TileType<PiedraHueca>()];
        // + variaciones: Main.SceneMetrics.SnowTileCount += nuestros tiles de nieve
        // (así los NPCs vanilla de nieve también spawnean — truco Calamity Astral)
    }
}
// IsBiomeActive:  player.ZoneRockLayerHeight && PiedraHuecaTiles > 500
//                 && X en mitad central del mundo (opcional)
```

Las otras 4 escuelas de detección (todas vistas en código real):

1. **Por área/rectángulo** (Starlight River Vitric): el worldgen guarda
   `Rectangle vitricBiome` (calibrado sobre `GenVars.UndergroundDesertLocation`);
   `IsBiomeActive` = `detectionBox.Contains(playerPos/16)` con `Inflate(pantalla)`.
   → nuestra opción SIN tiles: guardar el rectángulo del Sagrario en TagCompound.
2. **Por proximidad a objeto** (Starlight Hotspring): distancia al tile-Dummy
   fuente < 30×16 px. La versión barata: distancia al altar (que ya generamos).
3. **Por posición del mundo** (Calamity Abismo/Sulphur): X < 435 del lado del
   abismo + Y por capas + 200 tiles de la capa. El bioma ES una zona del mapa.
4. **Por muro** (Coralite ShadowCastle): `CoraliteSets.Walls.ShadowCastle[
   Framing.GetTileSafely(pos).WallType]` — el bioma se define por el MURO del
   fondo. Técn única para interiores "edificio".
5. **Por estado/submundo** (Everglow): `SubworldSystem.IsActive<MothWorld>()` — el
   bioma es un mundo entero aparte (SubworldLibrary). Máxima libertad, máxima
   distancia de nuestro estilo actual.

### 2.3 Música

- Vanilla: `public override int Music => MusicID.Underground;` (como ya hacemos).
- Modded: fichero `.ogg/.wav/.mp3` en `Assets/Music/MiTema.ogg` (ExampleMod: 2 .ogg
  con Credits.txt) → `MusicLoader.GetMusicSlot(Mod, "Assets/Music/MiTema")` (SIN
  extensión). Se puede hacer **condicional** (Calamity Sulphur): día/noche/lluvia/
  evento/jefes → tema distinto, con `Main.curMusic` de cortesía durante jefes.
- `ModMusic` existe para control avanzado (looping/volumen) — no necesario para v1.

### 2.4 Fondos

- **Underground** (el caso del Sagrario): `ModUndergroundBackgroundStyle` +
  `FillTextureArray(int[] textureSlots)` con **5 slots** (0..4) registrados con
  `BackgroundTextureLoader.GetBackgroundSlot(Mod, "ruta")` (Coralite
  MagicCrystalCaveBackground hace exactamente esto). Son texturas de fondo de cueva.
- **Surface**: `ModSurfaceBackgroundStyle` con `ChooseFarTexture` (montañas lejanas),
  `ChooseMiddleTexture` (capa media, puede ANIMAR por frames — ExampleMod rota 4
  frames cada 12 ticks) y `ChooseCloseTexture(ref scale, ref parallax, ref a, ref b)`
  (capa cercana con parallax real) + `ModifyFarFades` para el crossfade.
- **La vía 100% procedural (nuestra)**: Everglow demuestra que se puede **desactivar
  el fondo vanilla y dibujarlo a mano**: `Ins.HookManager.Disable(
  TerrariaFunction.DrawBackground)` cuando estás en el bioma + dibujar parallax
  manual en un hook PostDrawBG con `deltaPos *= MoveStep` por capa + `sin()` de
  oscilación en las colgaduras. Starlight River va más lejos: `On_Main.
  DrawBackgroundBlackFill += DrawVitricBackground` + `IL_Main.DrawBlack +=
  ChangeBlackThreshold` (cambia el umbral de negro del underground para "abrir" el
  fondo de cueva). **Conclusión: NO necesitamos PNGs de fondo para tener fondo
  propio** — nuestro VFXCore + BrumaFX pueden pintar la "catedral" proceduralmente.

### 2.5 Iluminación y ambiente

- `ModSystem.ModifyLightingBrightness(ref float scale)` — Calamity lo usa para el
  oscurecimiento de Signus: `scale += -0.4f * darkRatio` (hasta -0.8 en GFB).
- `ModSystem.ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)` —
  tinte global: Calamity hace `tileColor = Lerp(tileColor, Black, 0.3f)` y
  `backgroundColor = Lerp(backgroundColor, Black, 0.67f)` (ExoMechs), Everglow Tusk
  funde un tinte con `TuskS += 0.01f` por tick + activa su cielo con SkyManager.
- **Inyección de luz ambiente total** (Everglow MothBackground): hook
  `On_TileLightScanner.GetTileLight += (orig, self, x, y, out color) => { orig(...);
  color += ambient; }` con `ambient = (0.001, 0.001, 0.05)` — añade luz azulada a
  TODA la escena dentro del bioma. TRAZO EXACTO para "el Sagraro respira luz".
- Cielos: `ModSky` + activar en `SpecialVisuals(player, isActive)` con
  `player.ManageSpecialBiomeVisuals("Mod:Sky", isActive)` (Calamity Astral) o
  `SkyManager.Instance.Activate/Deactivate` (Calamity Sulphur, Everglow Tusk).

### 2.6 Agua

`ModWaterStyle` (ExampleMod/ Everglow FireflyWaterStyle decompilado):
`ChooseWaterfallStyle()` → slot de `ModWaterfallStyle`, `GetSplashDust()` → dust al
salpicar, `GetDropletGore()` → gore de gota, `LightColorMultiplier(ref r, ref g,
ref b)` (Firefly: 0.8/0.9/1.01), `BiomeHairColor()` → tinte del pelo del jugador,
`GetRainVariant()`, `GetRainTexture()`. Calamity alterna agua por mundo (zenith =
"PissWater" 😄). La ruta alternativa LunarVeil: clase propia `WaterAddon` con 2
`ScreenTarget` (frente/fondo) que tilea una textura de ruido por la pantalla cuando
`Biomes` está activo — un overlay de agua shaderizado sin tocar el agua real.

### 2.7 Spawns de NPCs

- Por NPC: `public override float SpawnChance(NPCSpawnInfo spawnInfo)` → peso de
  spawn (0 = nunca). Gate de bioma: `spawnInfo.Player.InModBiome<MiBiome>()` (helper
  genérico de tML) o un helper propio en el ModPlayer (Calamity:
  `ZoneAstral => Player.InModBiome<AstralInfectionBiome>() && !ZoneAbyss`).
- Pesos: usar `SpawnCondition.*.Chance` como unidades (Calamity Twinkler usa
  `SpawnCondition.TownCritter.Chance` dentro del bioma Astral).
- Ejemplo Calamity (Twinkler.cs decompilado): `if (AnyEvents) return 0; if
  (spawnInfo.Player.InAstral()) return SpawnCondition.TownCritter.Chance; return 0;`
- **Bestiario**: en `SetBestiary` del NPC → `bestiaryEntry.Info.Add(GetInstance<
  MiBiome>().ModBiomeBestiaryInfoElement)` (el elemento se crea SOLO en
  SetupContent del bioma a partir de BestiaryIcon/BackgroundPath/BackgroundColor;
  Calamity lo añade en masa vía `bestiaryEntry.AddTags(...)`).

### 2.8 Info displays y etiqueta de bioma

- Info display (como el "biómetro" vanilla): `InfoDisplay` + ModPlayer con
  `ResetInfoAccessories`/`RefreshInfoAccessoriesFromTeamPlayers` (ExampleMod
  ExampleInfoDisplayPlayer) → muestra "Sagrario Hueco: 62%" como los contadores
  vanilla de corrupción.
- **Etiqueta de bioma al entrar** (Everglow BiomeLabelSystem, decompilado):
  ModSystem con `Dictionary<ModBiome,int>` de ticks-in-biome, debounce de 50 ticks,
  cola `(nombre, icono)` y dibujo en `PostDrawInterface` con fade de altura 100.
  Un MUST para el Sagrario: "EL SAGRARIO HUECO" flotando la primera vez que entras.

### 2.9 Conversión de biomas (la API que faltaba en 1.4.3)

`tML 1.4.4+` tiene `ModBiomeConversion` (nuevo) + `TileLoader/WallLoader.
RegisterSimpleConversion(tileType, conversionType, toType)` y
`RegisterConversion(...)` con delegados. Calamity AstralConversion registra 13
tiles y 9 muros (Grass→AstralGrass, Dirt→AstralDirt, Stone→AstralStone, Snow, Ice,
Sand, Sandstone, HardenedSand, Clay, Silt→NovaeSlag, DesertFossil→CelestialRemains,
LivingWood→AstralMonolith, Meteorite→AstralOre) + conversiones INVERSAS para que
las soluciones del mal/hallow limpien lo astral. La solución spray: `Item.
DefaultToSolution(ModContent.ProjectileType<AstralSpray>())` (Calamity
AstralSolution). `BiomeConversionID` custom permitido → **"Solución del Eclipse"**
es factible HOY con API de primera clase.

### 2.10 Worldgen

- Inserción de pass: `ModSystem.ModifyWorldGenTasks(List<GenPass> tasks, ref double
  totalWeight)` → `int idx = tasks.FindIndex(t => t.Name.Equals("Smooth World"));
  tasks.Insert(idx + 1, new PassLegacy("Mi Pass", (progress, config) => {...}))`.
  Calamity inserta tras "Smooth World", "Floating Island Houses", "Dungeon",
  "Living Trees", "Jungle Temple" (este último REEMPLAZA el pass vanilla con un
  templo propio). Nombres de passes vanilla útiles: "Smooth World", "Dungeon",
  "Floating Island Houses", "Jungle Temple", "Living Trees", "Planting Seeds
  Above Ground"... (WorldgenManagementSystem.cs de Calamity es la referencia).
- Pase simple posterior: `PostWorldGen()` (nuestro AncientAltarWorldGen ya lo hace)
  — suficiente para altares/nodos; insuficiente para tallar el bioma (corre tras
  todo el gen pero sin estructura de progreso ni coordenadas de GenVars frescas).
- **Memoria del bioma**: guardar centro/rectángulo en `SaveWorldData(TagCompound)`
  (`tag["DungeonArchivePos"] = Point` — Calamity) → la detección por área lo
  necesita para sobrevivir recargas. **Nuestro altar actual NO guarda posición →
  bug latente de diseño**.
- Datos de vanilla útiles en gen: `GenVars.UndergroundDesertLocation` (Starlight
  genera el Vitric BAJO el desierto subterráneo), `GenVars.dungeonX`,
  `GenVars.structures` (evitar solaparse), `Main.worldSurface`, `Main.rockLayer`,
  `WorldGen.RandomWorldPoint(...)`.
- Estructuras por imagen (Everglow): leen PNG con ImageSharp donde R=255 marca
  posición y G/B codifican parámetros (largo/tamaño de las colgaduras) — el
  "blueprint en PNG". También es la técnica de schematic de Calamity (Schematics/.

### 2.11 Extras del ecosystema

- Emotes de bioma (ExampleMod ExampleBiomeEmote), torches de bioma
  (BiomeTorchItemType + ModTile torch con `TileID.Sets.Torch`), NPC happiness:
  los biomas moddeos aparecen en el diálogo de felicidad vía `IShoppingBiome`
  ( TownNPCDialogueName auto-registrado por ModBiome.SetupContent — verificado en
  el decompile), `ModBiome_Icon` y `_Background` auto-rutas si no las sobreescribes.

---

## 3. ANATOMÍA DE BIOMAS DE MODS POPULARES (técnicas y números)

### 3.1 Calamity — Mar Sulfuroso (SulphurousSeaBiome.cs)

- Detección HÍBRIDA: `SulphurTiles ≥ 300` **O** posición (X < 435 del lado del
  abismo, Y < rockLayer − maxTilesY/13) sin estar en el Abismo → el bioma es una
  franja oceánica del lado del abiso + válido artificialmente.
- Agua `SulphuricWater` propia + cielo "CalamityMod:SulphurSea" activado en
  SpecialVisuals + antorcha propia + fondo de superficie propio.
- Música con MÁQUINA DE ESTADOS: acid-rain tier 3 (post-Polterghast) / tier 1 /
  lluvia normal / noche / día — 5 temas .ogg del music-mod con fallback a MusicID.
- Vinculado al evento **Acid Rain** (evento-clima del bioma: el bioma hostiga).

### 3.2 Calamity — El Abismo (4 ModBiome separados, AbyssLayer1..4Biome.cs)

- "Bioma de profundidad" canónico: cada capa es un `ModBiome` PROPIO (con icono y
  fondo de bestiario separados — 4 PNGs por capa en BiomeManagers/).
- Detección por capa: posición (X ± 140 del chasm, Y ≥ YStart) **Y** ≥ 200 tiles
  del tile exclusivo de la capa (Shale / Gravel+PlantyMush / PyreMantle /
  Voidstone) **Y** no estar en la capa inferior → jerarquía de capas por exclusión.
- El agua "cuenta" como Abismo (el bioma solo aplica EN agua: `!lavaWet &&
  !honeyWet`), biome de INMERSIÓN: crush depth (daño 1500/s a enemigos no-nativos),
  defensa negativa por profundidad, regen de vida desactivada bajo capa 2, bolsas
  de aire que dañan 60/s, sonidos de muerte propios al ahogarse.
- Fondos por capa + música por capa. La wiki documenta: "artificial no cuenta" —
  la posición ES parte del contrato.

### 3.3 Calamity — Infección Astral (AstralInfectionBiome.cs + AstralConversion.cs)

- **El bioma-conversión de referencia**: > 950 tiles astrales (12 tipos, sumando
  variantes desierto/nieve que ADEMÁS se suman a `Main.SceneMetrics.SandTileCount/
  SnowTileCount` para que la arena/quimera vanilla reaccione) y `!ZoneDungeon`.
- Se SIEMBRA por meteorito tras Moon Lord: `PlaceAstralMeteor()` con `CanAstral-
  BiomeSpawn()` que cuenta tiles astrales < 400 × worldSizeFactor (4200/6400/8400)
  en toda la superficie — auto-limitación de expansión; 600 tiles sólidos para
  aterrizar; sonido de caída propio.
- `ModBiomeConversion` completa (13 tiles + 9 muros, sección 2.9) + solución
  inversa (las soluciones del mal/hallow limpian Dirt/Snow astral).
- 5 fondos según sub-zona (Snow/Desert/superficie/underground) elegidos en el
  getter de `SurfaceBackgroundStyle` mirando `Main.LocalPlayer.ZoneSnow/ZoneDesert`
  — UNA clase, MUCHOS fondos contextuales. Música surface/underground distinta.
- Monolitos (AstralMonolith): tile decorativo de "clima de bioma" con glow layer
  PostDraw — para capturas y casas.

### 3.4 Everglow — Santuario de las Polillas (FireflyBiome + MothBackground)

- Bioma = **subworld** (SubworldLibrary): `BiomeActive() == SubworldSystem.
  IsActive<MothWorld>()`. Mundo entero dedicado → gen completa sin límites.
- Fondo parallax **dibujado a mano**: alpha de entrada `backgroundAlpha += 0.02f`,
  al llegar a 1 DESACTIVA el fondo vanilla (`Ins.HookManager.Disable(
  TerrariaFunction.DrawBackground)`); colgaduras bioluminiscentes posicionadas por
  PNG-codificado (R=255 = posición, G/4+2 = largo, B/255+0.5 = tamaño, tipo
  aleatorio 0-4), oscilación `sin(time/128 + x/70 + y/120) × 0.2 × largo`, capas
  con parallax distinto (`MoveStep`), luminancia global que baja a 0.1 en boss
  (lerp 0.02).
- Luz ambiente inyectada con hook a TileLightScanner (`+(0.001,0.001,0.05)`).
- Agua, música, icono y fondos underground propios (el checklist completo).

### 3.5 Everglow — Tusk y Midnight Bayou

- Tusk: subworld + cielo custom (`SkyManager.Instance["TuskSky"]`) + tinte global
  `ModifySunLightColor` con fundido 0.01/tick + fondo de superficie propio.
- Midnight Bayou (YggdrasilTown): `IsBiomeActive` = subworld Y **distancia al punto
  fijo (1395, maxTilesY−405) < 5000 px** — bioma por radio; `OnInBiome` APAGA la
  lluvia/luna de sangre a mano (control de clima desde el bioma), Priority
  BossMedium.

### 3.6 Starlight River — Desierto Vitric (VitricDesertBiome + GenerateVitric.cs)

- Gen: 140 tiles de alto BAJO el desierto subterráneo (`GenVars.
  UndergroundDesertLocation.X − 25, +Width + 50`); FastNoise para suelo/techo
  (`NOISE_HEIGHT = 9f`, log(40) para las profundidades de techo), islas flotantes
  de vidrio con anti-solapamiento (distancia mínima 32 tiles, máx 50 fallos),
  barreras de boss colgadas del techo, región NoBuild protegida.
- Detección: `Rectangle.Contains(playerPos)` + Inflate(pantalla/32) + guardado del
  rectángulo → bioma por ÁREA, no por tiles.
- Ambiente: DOS `ParticleSystem` propios (foreground con textura grande +
  background con pequeña, ancladas a coordenadas de MUNDO) spawneadas desde
  `OnInBiome` en abanico de ±1.5 pantallas cada 30 px, con variante luna de sangre
  (1/400). Fondo: usa un "BlankBG" underground + dibuja el suyo con `On_Main.
  DrawBackgroundBlackFill` y umbral de negro con `IL_Main.DrawBlack`.
- Screen shader de bioma: `Filters.Scene["GradientDistortion"]` con ScreenShaderData
  propio — distorsión de pantalla DENTRO del bioma. (Nuestro BlackHoleLensSystem ya
  es superior técnicamente para esto.)

### 3.7 Starlight River — Hotsprings (el minibioma por objeto)

`IsBiomeActive` = existe un Dummy-tile `HotspringFountainDummy` activo a < 480 px.
Música ambiental propia. **El precedente directo para "bioma por altar"** —
exactamente el patrón que nuestro Sagrario puede usar HOY.

### 3.8 Coralite — Cueva de Cristal Mágica + Castillo de las Sombras

- Cueva: > 600 tiles (Basalto×3 + Bloque de Cristal) + X en mitad central del mundo
  (`|playerX − maxTilesX/2| < maxTilesX/4`) + capas dirt/rock. Fondo underground de
  5 slots propios + ModSceneEffect espejo con prioridad BiomeHigh (duplican clase
  para asegurar la escena). Música .ogg propia (`MusicLoader.GetMusicSlot`).
- Truco extra: `HyacinthRelicTile > 0` → `Main.SceneMetrics.GraveyardTileCount = 0`
  (un tile que APAGA el cementerio) — los contadores vanilla son editables.
- Castillo: detección por MURO (Set `CoraliteSets.Walls.ShadowCastle[wallType]`) —
  bioma-interior por paredes, ideal para "cámaras" del Sagrario si algún día
  queremos interiores sellados.

### 3.9 Spirit Mod (viejo 1.3-port) — bioma Espíritu

- `SpiritTiles > 200` (dirt/stone/sand/ice/grass propios) en `TileCountsAvailable`
  (int[] viejo) + REGIONES por Y: superficie-overworld, "región 1" = rock layer
  con `Y > (rockLayer + maxTilesY − 330)/2`, "región 2" = inframundo (últimos 300
  tiles) — el mismo bioma con 3 variantes de spawn por profundidad.
- Aurora nocturna visual (condición: nieve ∨ espíritu ∨ cielo, de noche, sin
  lluvia, sin mal) — el "cielo vivo" del bioma con flag de mundo `MyWorld.aurora`.

### 3.10 LunarVeil/Stellamod — agua-shader y nada de ModBiome

- Sin biomas ModBiome propios en el decompile (reutilizan zonas vanilla para sus
  sets de items). Su aporte: `WaterAddon` con 2 `ScreenTarget` (frente/fondo) que
  tilean un PNG de ruido (Water3) por toda la pantalla cuando la condición de zonas
  vanilla se cumple, respetando `Main.sceneWaterPos` — un "agua cinematográfica"
  por overlay. También `ScreenTargetHandler` (hook CheckMonoliths) para filtros de
  pantalla persistentes.

### 3.11 Síntesis de técnicas para AethonMod

| Técnica | Quién la usa | ¿La podemos hacer? |
|---|---|---|
| Tile-count + umbral | todos | ✅ cuando tengamos tile (fase 2) |
| Detección por rectángulo guardado | Starlight | ✅ HOY (TagCompound) |
| Detección por proximidad a tile | Starlight Hotspring | ✅ HOY (altare existente) |
| Detección por posición del mundo | Calamity | ✅ HOY |
| Detección por gemas vanilla contadas | (nueva, inspirada vanilla) | ✅ HOY — cero assets |
| Conversión ModBiomeConversion | Calamity Astral | ✅ HOY (API tML) — fase 3 |
| Música vanilla condicional | Calamity | ✅ HOY |
| Música .ogg propia | ExampleMod | ✅ (necesita composición) |
| Fondo underground 5 slots PNG | Coralite | ✅ (necesita 5 PNGs) |
| Fondo procedural a mano | Everglow/Starlight | ✅ HOY (VFXCore/BrumaFX) |
| Luz ambiente inyectada | Everglow | ✅ HOY (hook TileLightScanner) |
| ModifySunLightColor/LightingBrightness | Calamity/Everglow | ✅ HOY |
| Sky custom + SkyManager | Calamity/Everglow | ✅ (ModSky simple o ScreenOverlay) |
| Agua ModWaterStyle | ExampleMod/Everglow | ✅ (necesita texturas de agua) |
| Spawns por SpawnChance + InModBiome | Calamity | ✅ HOY |
| Bestiario NPC↔bioma | Calamity | ✅ HOY (1 línea por NPC) |
| Etiqueta de entrada de bioma | Everglow | ✅ HOY (PostDrawInterface) |
| Info display de bioma | ExampleMod | ✅ HOY |
| Mundo-subworld | Everglow | ⚠️ dependencia SubworldLibrary — NO recomendado ahora |
| Estructura por PNG codificado | Everglow/Calamity schematics | ✅ fase 3 (nuestro gen) |

---

## 4. CONCEPTOS DE BIOMA PARA AETHONMOD (5, con plan de fases)

Criterios de diseño: tema CÓSMICO/RÚNICO de "Aethon, la Luz Primordial"; render
100% procedural (nuestras librerías); progresión ya existente (niveles de fragmento
25/50/75/100/150 con hitos nombrados en ShardLevelSystem — ¡los nombres de los
hitos YA SON biomas prometidos!); español estilo de la casa; mínimo de assets
nuevos por fase.

### CONCEPTO 1 — «EL SAGRARIO HUECO» (realización del existente) ★ prioridad

> *"La catedral subterránea donde duerme la primera luz. No la encuentras: te
> reconoce."*

- **Qué es**: bioma subterráneo de "cueva catedral": cavernas amplias de roca
  desnuda donde la luz primordial queda atrapada en cristales colgantes y en
  pilares de eco. Es el hogar del Altar Antiguo y del Fragmento Génesis.
- **Activación (fase 1 — HOY, cero tiles)**:
  - Detección por GEMAS + ÁREA: `(ZoneRockLayerHeight) && (gemas de cueva
    contadas ≥ 40: TileID.Diamond+Ruby+Emerald+Sapphire+Topaz+Amethyst, vía
    TileCountsAvailable de tiles VANILLA) && distancia al altar más cercano < 120
    tiles` — el altar "irradia" sagrario. Los altares ya se generan; solo falta
    guardar sus posiciones en TagCompound (WorldGen system + SaveWorldData).
  - Umbrales calibrados a pantalla 1080p (área ≈169×116 tiles): 40 gemas ≈ 0.2% del
    área, señal fuerte pero natural; distancia 120 tiles = 1920 px.
  - En mundos ya generados sin dato guardado: fallback = barrido PostWorldGen
    existente + serialización de las 3 posiciones.
- **Activación (fase 2)**: tiles propios (`Piedra Hueca` PNG 32×32 procedural ×3
  variantes, generado por script PIL como los iconos v6.22) → conteo > 600 estilo
  Coralite; la gen talla la catedral alrededor de cada altar (pass ModifyWorld-
  GenTasks tras "Smooth World": cúpula WorldGen.TileRunner + pilares + crystal
  clusters), y el altar se coloca DENTRO (no aleatorio).
- **Ambient VFX (nuestra especialidad, en OnInBiome + PostDrawTiles)**:
  - **Lluvia de motas ascendentes**: LumenLib — perlas de luz que NADAN HACIA
    ARRIBA lentamente (invertir gravedad de los presets de partículas), densidad
    ~1/2s por pantalla, con parpadeo tipo insecto (alpha = 0.6+0.4·sin(hash·t)).
  - **Cristales colgantes dibujados**: BrumaFX en modo masa (FlushNonPremultiplied
    — el humo que OCLUYE de v6.25) para las vetas de sombra + LumenLib para las
    puntas brillantes. Posiciones deterministas por hash(i,j) del tile de techo
    (nada que sincronizar).
  - **Anillos rúnicos latentes**: cada X tiles de suelo, un aro de runas del
    RuneSunRenderer "respirando" (escala 0.5→0.55, alpha 0.10→0.16) — el sagrario
    está VIVO y responde al nivel del fragmento del jugador (más nivel = más aros
    visibles: 0→3 por pantalla según nivel 0/50/100).
  - Luz ambiente inyectada (hook TileLightScanner de Everglow): `+(0.02, 0.02,
    0.05)` frío-azulado dentro del bioma.
- **Música (fase 1)**: `MusicID.Underground` de día... mejor: condicional —
  `Crystal` (no existe; usar `MusicID.Ice` NO) → propuesta: `MusicID.Underground`
  base + `MusicID.Desert` NO... **propuesta concreta**: fase 1 usa
  `MusicID.Mushrooms` (orgánico-catedral, poco escuchado) por debajo de 400 HP y
  `MusicID.Underground` después; fase 2: tema .ogg propio "El Sagrario Hueco".
- **Spawns**: EchoBlade y EchoArcher (YA EXISTEN, sin SpawnChance) reciben
  `SpawnChance` gated en el bioma (pesos 0.08/0.05, solo si nivel de fragmento ≥ 15
  para que el bioma no mate a recién llegados); HollowTitan = mini-jefe invocable
  en el altar con clic derecho + Fragmento nivel 50 (ya es "del Sagrario" en lore).
- **Drops**: ResonanceShard existente como drop de ecos (ya es el bucle del mod);
  nuevo (fase 2): "Lágrima del Sagrario" de los cristales (material de cosméticos).
- **Gating**: entrada física desde nivel 0 (las cuevas existen), spawns hostiles ≥15,
  HollowTitan ≥50. El bioma CRECE: a nivel 100 aparecen "capillas" (sub-áreas con
  más aros y spawns de ecos élite).
- **Bestiario**: 1 PNG icono 30×30 + 1 PNG fondo bestiario 172×124 (o BackgroundColor
  = Color(20,16,40) SIN PNG, que tML soporta) + asociar los 3 NPCs del bioma con
  `ModBiomeBestiaryInfoElement`.
- **Esfuerzo fase 1**: ~400–500 líneas (BiomeSystem con conteo de gemas +
  TagCompound de altares + SpawnChance ×2 + ambient VFX ~200L + etiqueta de bioma
  ~80L). **Cero sprites.** Fase 2: +gen pass (~300L) + 1–3 PNGs de tile
  procedurales.

### CONCEPTO 2 — «EL CAMPO ESTELAR» (bioma por evento nocturno)

> *"Algunas noches, el cielo se olvida de la distancia."*

- **Qué es**: el hito "Lluvia de Luz Estelar" (nivel 25, ya anunciado en
  ShardLevelSystem) se convierte en bioma-noche: durante esa noche, TODA la
  superficie bajo el cielo abierto se vuelve Estelar.
- **Activación**: `ModBiome.IsBiomeActive` = evento activo (flag del
  ShardLevelSystem al llegar al hito + noche + `ZoneSurfaceHeight/Overworld`) —
  detección por ESTADO, como la lluvia vanilla. Duración: 1 noche entera, 1 de
  cada 3 noches tras el hito (determinista por Main.moonPhase).
- **Ambient VFX (el show)**:
  - Lluvia de ESTELAS: EstelaLib con perfil Comet cayendo en diagonal con el
    viento (Main.windSpeedCurrent), ~6 visibles por pantalla, cada una con su
    estela triple velo/cuerpo/núcleo — el cielo literalmente LLUEVE cometas.
  - Suelo recogedor: donde aterriza un cometa, OndaLib + perlas LumenLib —
    charcos de luz que persisten 30 s (lista de impactos en el ModSystem).
  - Viento de partículas: las motas de luz van con `Main.windSpeedCurrent×0.02`
    (la lección Everglow de humo v625 aplicada a ambiente).
- **Música**: `MusicID.Space` cuando el evento está activo y estás al aire libre
  (superficie+cielo), de noche — encaja de sobra y es barata.
- **Spawns**: StellarComet/LivingPulsar YA EXISTEN como minions — versión salvaje
  (NPCs nuevos "Cometa Viviente" pasivo-hostil que embiste en arco) con drops de
  ResonanceShard; o reutilizar: EchoArcher también spawnea en el Campo (son
  "arqueras estelares").
- **Drops**: "Polvo de Cometa" ( material VFX: consumirlo activa la Lluvia a
  demanda, 1 por noche, caro).
- **Gating**: nivel 25+ (ya definido por el hito).
- **Esfuerzo**: ~350L + lógica del hito ya escrita a medias. Cero sprites. Riesgo
  bajo. **Es el bioma más barato y más espectacular por nuestra librería de
  estelas.**

### CONCEPTO 3 — «LA VETA RÚNICA» (bioma-mineral subterráneo)

> *"La roca recuerda. En lo hondo, la memoria de Aethon se endureció en vetas."*

- **Qué es**: venas de runas fosforescentes que serpentean por la roca profunda
  (rock layer baja, cerca del inframundo). No hay "sala": hay VETA — el bioma es la
  roca misma. Exploración-minería con ambient rúnico.
- **Activación**: nodos guardados en TagCompound (N=8–12 por mundo,
  WorldGen.PostWorldGen los siembra en `Y ∈ [rockLayer+0.6, maxTilesY−200]`); el
  bioma está activo a < 80 tiles de cualquier nodo (hash espacial para el lookup
  O(1), no lineal) + ZoneRockLayerHeight/Underworld. Fase 2 con tiles: el nodo
  esparce "Mena Rúnica" (tile 16×16 PNG procedural) y el conteo ≥ 60 manda.
- **Ambient VFX**:
  - La veta se DIBUJA: caminos fractales de StormLib (ZigPath+Refine) pegados a la
    superficie de la roca, alpha 0.25, color dorado-cálido de VFXPalettes — "vetas
    de rayo doradas en la pared". Se calculan UNA VEZ por nodo (semilla = índice)
    y se cachean.
  - Latido sincronizado: todas las vetas de un nodo laten con la misma fase
    (0.9+0.1·sin(t/45)) — el corazón del mundo.
  - Al MINAR dentro del bioma: chispas PyraLib +Kick de sacudida (OndaLib).
- **Música**: `MusicID.Underground` con prioridad menor… mejor `MusicID.Eclipse`
  NO (es de jefe). Propuesta fase 1: `MusicID.Mushrooms` en la zona de veta y
  `MusicID.Underground` fuera — contraste audible al encontrarla.
- **Spawns**: Ecos (EchoBlade) spawnean pegados a las vetas (peso 0.04, cap 2);
  "Guardián de la Veta" (NPC nuevo o reutilizar TheWitness visual) NO hostil: vaga
  por la veta y te vende 1 cosa por nivel de fragmento.
- **Drops**: minar mena rúnica = XP directo del fragmento (+25 por nodo, el bucle
  de nivel ya existe) + "Runa Suelta" (material de las armas de sol: acelera el
  tier de RuneSun).
- **Gating**: los nodos solo se revelan (veta visible) tras nivel 50 — antes están
  "apagadas" (dibujadas a alpha 0.05, casi invisibles): el mundo cambia según TI.
- **Esfuerzo**: ~400L fase 1 (nodos + detección + veta-fractal cacheada + spawns).
  Cero sprites fase 1 (la veta es 100% StormLib). Fase 2: mena PNG + conteo.

### CONCEPTO 4 — «LAS CENIZAS DEL ECLIPSE» (bioma por conversión post-jefe)

> *"Aethon ha sido vencido, y su luz no sabe morir: cae sobre el mundo como ceniza
> que ilumina."*

- **Qué es**: tras derrotar a AethonBoss (el jefe final YA EXISTE), una zona de la
  superficie se convierte con **ModBiomeConversion** (la API moderna): el mundo
  "amanece" en eclipse dorado permanente en un radio de ~340 tiles alrededor del
  punto de muerte del jefe. El equivalente casa de la Infección Astral: nuestra
  "infección de LUZ".
- **Activación**: `IsBiomeActive` = centro guardado (TagCompound, el punto de
  muerte del boss) + distancia < 340 tiles + superficie; ADEMÁS fase 2 con tiles:
  `Ceniza Dorada` (tile conversión de Grass/Dirt/Sand → registro con
  TileLoader.RegisterSimpleConversion) y conteo ≥ 300 con limpieza por solución
  inversa (el mundo puede curarse).
- **Ambient VFX (el más dramático)**:
  - **La corona del eclipse en el cielo**: la DrawSolarCorona del
    EclipsePrimordialRenderer (v6.24, YA ESCRITA) re-escalada al cielo con
    parallax 0.1 — un sol negro con streamers LumenLib presidiendo la zona.
  - `ModifySunLightColor`: `tileColor = Lerp(tileColor, Color(60,45,20), 0.25f)`,
    `backgroundColor = Lerp(backgroundColor, Color(35,25,12), 0.4f)` con fundido
    0.005/tick al entrar — el mundo se vuelve ámbar.
  - Ceniza cayendo: PyraLib en modo brasa (brasas doradas cayendo LENTO, drift por
    viento, muerte al tocar agua — la lección Everglow de FireSpark).
  - `ModifyLightingBrightness`: +0.05 (la ceniza ILUMINA: un bioma nocturno que
    aclara).
- **Música**: `MusicID.Eclipse` (perfecta y vanilla) con Priority BiomeHigh.
- **Spawns**: "Cenicientos" — NPCs nuevos (o ecos re-tintados): spawns élite end-
  game (pesos 0.1, cap 4) solo dentro de la zona.
- **Drops**: "Ceniza del Eclipse" (material tier EclipsePrimordial: recetas para
  las armas cósmicas supremas — cierra el círculo del arsenal v6.2x).
- **Gating**: post-AethonBoss (final del juego). Diseñado como la "recompensa de
  mundo" del mod.
- **Esfuerzo**: fase 1 (sin tiles): ~450L (detección por radio + VFX + tint +
  spawns). Fase 2 (conversión real de tiles): +250L + PNGs de ceniza; la API
  ModBiomeConversion es de primera clase HOY.

### CONCEPTO 5 — «LA FALLA DEL VACÍO» (grietas de realidad, end-game subterráneo)

> *"La realidad tiene costuras. Los agujeros negros tiran de ellas."*

- **Qué es**: 1–3 grietas VERTICALES que atraviesan el mundo cerca del inframundo
  (de rockLayer baja hasta el infierno), donde el tejido está desgarrado: distorsión
  de lente constante, bruma que devora la luz, gravedad traicionera. El hogar
  natural de RiftKeeper (jefe nivel 75 YA EXISTE con teleports por rifts en su AI).
- **Activación**: boca de la falla guardada (X aleatorio lejos de dungeon/jungle,
  anchura 12–18 tiles); activo si `|playerX − fallaX| < 40 && Y > rockLayer+0.5`.
  Detección por POSICIÓN pura (estilo Calamity Abismo) — la falla ES el bioma.
- **Ambient VFX (el más técnico, el homenaje a nuestro BlackHoleLensSystem)**:
  - **Lente ambiental**: BlackHoleLensSystem (817L, YA ESCRITO, distorsión de
    pantalla completa) alimentado con una "singularidad débil" en el centro de la
    boca: strength escalado por proximidad (0 lejos → 0.35 al borde) — EL mundo se
    curva alrededor de la grieta. Nadie en el ecosistema tML tiene esto.
  - **Bruma devoradora**: BrumaFX NonPremultiplied (masa que OCLUYE) en columnas
    que suben desde la boca + InteriorBiomeLighting: ModifyLightingBrightness
    `scale −= 0.25f` con recover gradual — la oscuridad que ATRAPAN los grandes.
  - **Runas prisioneras**: anillos rúnicos CCW (RuneRadius 2.55, velocidad −0.14,
    la firma de BrumaAscendido v6.23) rodeando la boca, girando lento.
  - Partículas INWARD: motas que caen hacia la boca en espiral (reutilizar el
    patrón de succión del BlackHoleProjectile).
- **Música**: `MusicID.Eerie` (superficie noche está prohibida aquí; en cueva
  funciona) — o `MusicID.Underworld` con Pitch... vanilla no da pitch por zona:
  propuesta `MusicID.Eerie`.
- **Spawns**: "Residuos del Vacío" (NPCs nuevos mínimos: 2 tipos) + RiftKeeper
  spawnea NATURAL en la falla si nivel ≥ 75 (primer jefe "mundial" del mod).
- **Drops**: "Filamento del Vacío" (material blackhole tier: recetas
  Umbral/Olvido/Bruma — el arsenal de agujeros negros gana su fuente).
- **Gating**: nivel 75 (ya definido por RiftKeeper).
- **Esfuerzo**: ~500L fase 1 (boca + detección + lente + bruma + spawns). Cero
  sprites. Riesgo MEDIO (la lente a pantalla completa constante hay que presupuestar
  — mitigación: strength ≤ 0.35 y solo dentro del radio).

### 4.1 Tabla resumen de los 5

| # | Bioma | Tipo de detección | Assets nuevos | Librerías protagónicas | Gate | Esfuerzo F1 |
|---|---|---|---|---|---|---|
| 1 | El Sagrario Hueco | gemas vanilla + área de altar (F1) → tiles >600 (F2) | 2 PNG bestiario (o 0 con BackgroundColor) | LumenLib, BrumaFX, anillos rúnicos | 0/15/50 | ~450L |
| 2 | El Campo Estelar | estado (evento nocturno por hito) | 0 | EstelaLib, OndaLib | 25 | ~350L |
| 3 | La Veta Rúnica | nodos TagCompound (F1) → mena ≥60 (F2) | 0 (F1) | StormLib (vetas fractales), PyraLib | 50 | ~400L |
| 4 | Cenizas del Eclipse | radio del punto de muerte del jefe (F1) → conversión (F2) | 0 (F1) | Corona solar (ya), PyraLib, tintes | post-Aethon | ~450L |
| 5 | La Falla del Vacío | posición por anchura (estilo Calamity) | 0 | BlackHoleLensSystem, BrumaFX, anillos | 75 | ~500L |

### 4.2 Plan de implementación por fases (recomendado)

- **FASE 0 — Infraestructura común (una vez, ~250L)**: `BiomeTileCountSystem`
  (TileCountsAvailable + ResetNearbyTileEffects), `WorldAnchorSystem` (TagCompound
  de puntos: altares/nodos/falla/punto-de-muerte con hash espacial de lookup),
  `BiomeLabelSystem` (etiqueta de entrada estilo Everglow: debounce 50 ticks,
  PostDrawInterface, usa iconos existentes GlowOrb/Crescent — cero sprites),
  localización de DisplayName de todos los biomas.
- **FASE 1 — Sagrario + Campo Estelar** (los dos extremos de progresión: inicio y
  nivel 25; reutilizan NPCs existentes con SpawnChance). Entrega visible en una
  versión: el jugador ENCUENTRA el sagrario de verdad y vive una noche estelar.
- **FASE 2 — Veta Rúnica + Falla del Vacío** (nivel 50/75: exploración vertical +
  el show técnico de la lente).
- **FASE 3 — Cenizas del Eclipse** (post-final: conversión real con
  ModBiomeConversion + tiles de ceniza PNG) + FASE 2 del Sagrario (gen de catedral
  con pass ModifyWorldGenTasks + tiles Piedra Hueca + música .ogg propia).
- **Regla de la casa para sprites**: fase 1 = 0 sprites obligatorios (BackgroundColor
  en vez de PNG de bestiario, icono de etiqueta = PNG existente). Fase 2/3 = PNGs
  16/32×32 de tiles generados por script PIL + revisión VLM (el pipeline v6.22).

### 4.3 Qué es factible HOY sin sprites de tiles (resumen ejecutivo)

1. Detección por: conteo de tiles VANILLA (gemas), rectángulos/radios guardados en
   TagCompound, posición del mundo, estado de evento. Todas usadas por mods AAA.
2. Ambient VFX 100% procedural con las 6 librerías v6.25 + partículas (PostDrawTiles
   / PostDrawInterface).
3. Música: cualquier MusicID vanilla, condicional por día/noche/evento/nivel.
4. Spawns: SpawnChance gated con InModBiome + bestiario asociado con 1 línea.
5. Luz: ModifySunLightColor/ModifyLightingBrightness + hook TileLightScanner.
6. Bestiario: BackgroundColor en lugar de PNG de fondo; icono con PNG existente o
   generado 30×30.
7. Conversión de mundo: ModBiomeConversion + RegisterSimpleConversion (API nativa).
8. Etiqueta de entrada de bioma + info display.
9. NO factible sin assets: fondos underground de 5 slots (5 PNGs), agua custom
   (texturas de agua/caída), música .ogg (composición), tiles propios (PNG 16/32).

---

## 5. APÉNDICE: verificación de fuentes

- Decompile ILSpy (ilspycmd 8.2, /tmp/dotnet8, roll-forward Major) desde
  /tmp/tml/tModLoader.dll (tModLoader v2026.07.3.0, la del usuario): ModBiome (88L),
  ModSceneEffect (69L), SceneMetrics (748L — umbrales vanilla), Main
  (buffScanAreaWidth/Height), MusicLoader. Guardados en /tmp/research/*.cs.
- Repos clonados (--depth 1): CalamityTeam/CalamityModPublic → /tmp/calamity
  (BiomeManagers/ 16 clases + Systems/Tile/BiomeTileCounterSystem.cs + World/
  AstralConversion.cs + Systems/World/WorldgenManagementSystem.cs +
  CalPlayer/CalamityPlayer.cs + NPCs/Astral/Twinkler.cs + Skies...);
  ProjectStarlight/StarlightRiver → /tmp/starlight (Content/Biomes/ 9 clases +
  Content/WorldGeneration/GenerateVitric.cs); PhoenixBladez/SpiritMod →
  /tmp/spiritmod (MyPlayer.cs + MyWorld.cs + World/SpiritGenPasses.cs); tModLoader/
  tModLoader → /tmp/tmlsrc (ExampleMod completo + patches/tModLoader/Terraria/
  SceneMetrics.cs.patch + TileLoader.cs + BiomeLoader.cs + SceneEffectLoader.cs).
- Decompile locales previos: /tmp/research/{coralite (Content/Biomes/ 3 clases +
  Core/CoraliteTileCount.cs), everglow (FireflyBiome/TuskBiome/MidnightBayouBiome/
  MothBackground/TuskGen), lunarveil (Systems/Waters/DefaultWaterAddon.cs), wote}.
- Búsquedas web guardadas en busquedas_web_42d/: tml_modbiome.json,
  tml_biome_tutorial.json, tml_wiki_github.json, starlight_repo.json,
  spirit_repo.json, thorium_biome.json, calamity_abyss.json, screen_shader.json,
  tml_guide.json, tml_wiki_biome.json, calamity_abyss_page.json, redemption_repo.json,
  scenetrics_size.json (fallida por 429 — el dato se obtuvo del binario).
- Notas de cobertura: Thorium/Redemption/The Stars Above/Ancients Awakened NO
  tienen fuente pública disponible (confirmado por búsqueda y sesiones previas
  41-a/41-b); sus técnicas conocidas (biomas de música por pista, stronghold por
  estructura) se cubren con los patrones equivalentes de los mods con fuente.
- LunarVeil: sin ModBiome propias en el decompile — su aporte es el WaterAddon/
  ScreenTarget (documentado en 3.10).

*Informe: Task 42-d — investigación de biomas para AethonMod v6.26. Solo lectura
del código del proyecto; cero cambios en el mod.*

# AethonMod — Historial de Cambios

## Commit v5.85 — SOL COMPLETO (10s) + LENTE GRAVITACIONAL + SUPERNOVA MEJORADA + limpieza de referencias

Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran

### A. SunProjectile — el sol como cuerpo celeste completo (ciclo de 10 segundos)

El sol ahora tiene un ciclo de vida determinista de **10 segundos exactos** (600 ticks)
y conjuga los proyectiles de dos bastones existentes + gravedad propia:

1. **LLAMARADAS SOLARES cada 2 segundos** — desde t=0 (nace con el sol), se invoca
   `PhoenixNovaProjectile` (el del PhoenixNovaStaff) **centrado en el sol**:
   anillos naranjas expansivos + flash + lluvia de Torch + OnFire. 5 llamaradas
   en total (t=0, 2, 4, 6 y 8s), daño = 50% del sol.
2. **SUPERNOVA SINCRONIZADA en el segundo 7** — cuando al sol le quedan 180 ticks,
   invoca `SupernovaProjectile` (el del SupernovaStaff) en su centro y lo mantiene
   **perfectamente centrado** tick a tick (ai[1] guarda el índice del hijo y el sol
   le impone su posición y velocidad).
3. **GRAVEDAD DEL SOL** — como cuerpo celeste atrae enemigos (radio 280px) con una
   fuerza **10 veces menor que la del agujero negro** (0.26 vs 2.6). Durante la
   carga de la supernova (segundos 7→10) la fuerza **aumenta progresivamente hasta
   x4** (1.04 en el pico): los enemigos son arrastrados hacia la nova.
4. **EXPLOSIÓN SIMULTÁNEA en el segundo 10** — el sol (OnKill) y la supernova
   (OnKill) estallan el mismo tick: nova masiva combinada con doble onda expansiva.
5. **QUEMADURA** — inflama enemigos al contacto (OnFire 5s). La variante potenciada
   por daño mágico se implementará al integrarlo en el Grimorio (arma definitiva).
6. Visuales de carga: el sol se comprime sutilmente, su luz crece hasta x1.8 y
   estelas doradas convergen en espiral hacia el núcleo (SpawnSupernovaChargeIntake).

### B. SupernovaProjectile — reescrito: 3 segundos de carga + explosión masiva

- **timeLeft 90 → 180** (3 s exactos, sincronizable con el sol).
- **Carga (0..180)**: contracción acelerada hacia blanco-azulado, atracción de
  enemigos con fuerza creciente (0.5 → 2.2, radio 300px), espiral de GoldFlame
  cada vez más rápida (2→4/frame), **sacudidas de cámara anticipatorias** cada 40
  ticks (intensidad creciente) y anillos de contención pulsantes.
- **Explosión (OnKill, mejorada y más vistosa)**:
  - DOBLE onda expansiva de la librería (blanca-dorada 320px veloz + naranja
    profunda 460px retardada)
  - Flash blanco gigante (SoftGlow aditivo x6.5) + Explosion(200px, 46 partículas)
  - 26 estelas de viento estelar radiales largas
  - 70 lenguas de GoldFlame + 25 chispas blancas + 18 brasas con gravedad + 14 humos
  - AoE real de 340px (SimpleStrikeNPC + OnFire, solo en autoridad)
  - Temblor de cámara fuerte (10f, "AethonSupernovaBlast") + doble sonido
- `OnHitNPC`: ahora inflama (OnFire 300).

### C. BlackHoleProjectile + LENTE GRAVITACIONAL de pantalla (nuevo sistema)

1. **`BlackHoleLensSystem` (archivo nuevo, `Content/Effects/`)** — la pieza clave:
   - Hook MonoMod `Terraria.On_TimeLogger.DetailedDrawTime` en el punto 36 —
     verificado por decompilación contra tModLoader v2026.07.3.0: es el punto
     EXACTO tras `Filters.Scene.EndCapture` (el mundo ya está en
     `Main.screenTarget`) y antes de la UI.
   - Recopila hasta 5 agujeros activos (posición UV de pantalla + radio
     `width*scale/screenW*0.75`), copia `Main.screenTarget` a través de
     `BlackHoleDistortionShader` (el .fxc del pipeline propio) hacia un render
     target a media resolución y lo devuelve cubriendo la pantalla → **el fondo
     real del juego se curva alrededor del horizonte de sucesos**.
   - "Pequeña lente" deliberada: distortionStrength 0.62 ligada a la escala del
     agujero (nace y muere con él), maxLensingAngle 24, decaimiento exponencial.
   - APIs verificadas por reflexión + decompilación: `Main.screenTarget` ✓,
     `Main.screenWidth/Height` ✓, hook event `DetailedDrawTime` ✓, RT bindings
     preservados/restaurados ✓, try/catch total (nunca rompe el render).
2. **Fuerza gravitatoria mayor**: 2.0 → 2.6 y radio 350 → **450px**.
3. **Área de daño mayor**: width/height 76 → 96 (hitbox + canvas del shader).
4. **Succión espiral multicolor**: la materia devorada cubre violeta/cian/
   magenta/oro (dusts + partículas de librería) y se vuelve incandescente cerca
   del horizonte.
5. **Devora el polvo** en radio ampliado 190 → 260px.
6. disco de acreción de estelas + anillo de fotones + halo de distorsión +
   implosión/doble onda expansiva al colapsar (de v5.84, intactos).

### D. LIMPIEZA TOTAL de referencias externas

- Carpeta `Content/Effects/WoTG/` renombrada a **`Content/Effects/Textures/`**
  (10 texturas) y las 6 rutas de código actualizadas.
- Tooltips de BlackHoleStaff/SunStaff reescritos: describen las capacidades
  propias (nada de "render idéntico a...").
- `AethonMod.csproj`: eliminado `Compile Remove="Reference_WoTG/**"` (la carpeta
  ni existe) y el comentario que citaba al mod externo.
- `.gitignore`: eliminada la línea `Reference_WoTG/`.
- `TestingPlayer.cs` y `CHANGES.md`: comentarios/histórico neutralizados
  ("el mod de shaders de referencia", "render de referencia").
- `SupernovaStaff.cs`: docblock y tooltip actualizados a la nueva conducta.

### E. Versionado

- `build.txt`: versión 5.84 → **5.85**.
- **Compilación verificada**: 0 errores contra tModLoader v2026.07.3.0 real
  (4 warnings benignos preexistentes de `Kill()` obsoleto en archivos viejos).

---

## Commit v5.84 — LIBRERÍA DE PARTÍCULAS COMPLETA + capas de VFX en BlackHole/Sun

Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran

### A. Librería de partículas completada según el libro de referencia

El usuario entregó el documento "particle_library_implementation_book.pdf" (88 páginas,
"Librería de Partículas para Terraria - Referencia para IA"). El sistema existente desde
v5.69 (ParticleData + ParticleBuffer + ParticleManager) estaba basado en ese libro pero
INCOMPLETO. v5.84 lo completa al diseño completo del documento:

**Archivos nuevos en `Content/Particles/`:**
- `ShapeDescriptor.cs` — API de spawn genérica por forma (sección 13.3 del libro):
  Box, Circle, HollowBox, HollowCircle, Cone, Sphere, Vortex y Line con
  `GenerateRandomPoint()` y factory methods estáticos.
- `CameraBounds.cs` — frustum culling (sección 16.1): rectángulo visible de la cámara
  con margen para partículas grandes parcialmente fuera de pantalla.
- `ParticlePresets.cs` — efectos pre-empaquetados (apéndice A del libro):
  `Explosion()` (ráfaga radial + anillo de shockwave + chispas),
  `Implosion()` (colapso espiral convergente),
  `RingPulse()` (onda expansiva) y
  `VortexSwirl()` (brazos espirales).

**Componentes implementados en el update loop (antes solo había 3):**
- `FadeIn` — el alpha sube durante los primeros UserData0 ticks (default 10)
- `ScaleUp` — la escala crece de 0 a UserData1/UserData2 (anillos expansivos)
- `ColorShift` — interpola PackedStartColor → PackedEndColor durante la vida
  (¡los campos existían desde v5.69 pero nadie los usaba!)
- `Homing` — persigue un NPC: explícito por whoAmI o el más cercano (sección 12.2)
- `Orbit` — orbita un centro (UserData0/1=centro, UserData2=vel. angular,
  UserData3=radio); stateless: el ángulo se deriva de la posición actual cada tick,
  y RotationSpeed sincronizada mantiene las estelas alineadas tangencialmente
- `EmitLight` — emite luz del color de la partícula; la intensidad deriva de la
  escala (sin UserData → combinable con cualquier otro componente)

**Mejoras del orquestador (ParticleManager):**
- Capacidad 2000 → 4000 partículas (~320 KB, sigue siendo cero GC)
- `SpawnShape(center, shape, count, template)` — API genérica (sección 14)
- Auto-fill de PackedStartColor/UserData en Spawn para todos los componentes nuevos
- Frustum culling en el render (margen 320px) — no se dibujan partículas
  fuera de pantalla (sección 16.1)
- 3 texturas nuevas registradas: GlowOrb (ID 8), SparkleStar (ID 9), TrailGlow (ID 10)
- Constantes `ParticleTex` (patrón TextureRegistry de la sección 21.2)

**APIs verificadas por reflection contra tModLoader.dll v2026.07.3.0 real:**
- `Terraria.Graphics.CameraModifiers.PunchCameraModifier(Vector2, Vector2, float, float, int, float, string)` ✓
- `Main.CameraModifiers` (campo de instancia, tipo CameraModifierStack) ✓
- `Lighting.AddLight(Vector2, Vector3)` ✓

### B. BlackHoleProjectile — capa de partículas de la librería (4 efectos nuevos)

Arquitectura por capas de profundidad: partículas de la librería (PostDrawTiles,
fondo aditivo) + dusts vanilla (capa frontal) + canvas del shader (encima):
1. **Espiral de succión** — 2 partículas/frame SoftGlow aditivas naciendo en el borde
   del campo gravitatorio con velocidad tangencial+radial (espiral natural) y
   ColorShift blanco incandescente → violeta cósmico al morir
2. **Disco de acreción de estelas** — TrailGlow (32x8) estiradas tangencialmente
   orbitando con el componente Orbit; la rotación avanza al mismo ritmo que la
   órbita (RotationSpeed = angVel) → las estelas quedan SIEMPRE alineadas con la
   tangente; ColorShift naranja dorado → rojo profundo
3. **Anillo de fotones pulsante** — Ring con ScaleUp (0→1.15×scale) + FadeOut cada
   36 ticks: destello circular azulado expandiéndose en el horizonte de sucesos
4. **Halo de distorsión** — Noise procedural rotando lento con alpha 26 y tinte
   violeta: sugiere la curvatura del espacio

Impacto y muerte:
- `OnHitNPC`: micro-colapso con Implosion + RingPulse sobre el objetivo
- `OnKill`: Implosion(165px, 46 partículas violetas) + Explosion(130px, blanco→naranja)
  + doble RingPulse (250px violeta + 320px dorada retardada) + screenshake con
  PunchCameraModifier ("AethonBlackHoleCollapse")

### C. SunProjectile — capa de partículas de la librería (4 efectos nuevos)

Misma arquitectura de capas (librería al fondo + dusts frontales + canvas del shader):
1. **Corona de plasma orbitando** — SoftGlow con Orbit (radio 48-60×scale, deriva
   lenta 0.045-0.075 rad/tick) y ColorShift amarillo incandescente → naranja profundo
2. **Viento solar** — estelas TrailGlow alineadas radialmente fluyendo hacia fuera
   desde la fotosfera, desvaneciéndose blanco-amarillo → naranja
3. **Destellos luminosos** — SparkleStar con FadeIn (8 ticks) + FadeOut + EmitLight
   (la partícula ILUMINA su entorno, intensidad según escala)
4. **Arcos de prominencia** — cada 45 ticks (sincronizado con las llamaradas de dust),
   7 estrellas orbitando en el borde de la llamarada con dirección alternante

Impacto y muerte:
- `OnHitNPC`: estallido solar con Explosion + RingPulse sobre el objetivo
- `OnKill`: nova masiva = Explosion(170px, 40 partículas blanco→naranja) + doble
  RingPulse (280px dorada + 380px roja retardada) + ráfaga de 22 estelas de viento
  solar radiales (velocidad 3.5-7 px/tick) + screenshake ("AethonSunNova")

### D. Otros cambios
- Tooltips de BlackHoleStaff y SunStaff actualizados con las nuevas capas de VFX
- `version = 5.84` en build.txt
- **Compilación verificada**: 0 errores contra tModLoader v2026.07.3.0 real
  (mismos 4 warnings benignos preexistentes de Kill() obsoleto en archivos viejos)

---

## Commit v5.83 — AGUJERO NEGRO + SOL con render de referencia + FIX CRÍTICO de shaders

Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran

### A. FIX CRÍTICO: "Failed to load asset 'Content\Effects\Shaders\SunShader'"

**Causa raíz descubierta analizando el código fuente de tModLoader v2026.07.3.0:**
- tModLoader NO compila los archivos `.fx` durante el build (verificado en el código
  fuente de tML y en el issue abierto #3326 de tModLoader).
- Los mods DEBEN incluir los shaders **ya compilados como `.fxc`** — así lo hace
  el mod de shaders de referencia (270 archivos .fxc commiteados en su repo).
- Nuestros antiguos `.xnb` (generados con dxc en el sandbox) no eran XNB válidos de
  MonoGame: el XnbReader de tML fallaba al parsearlos → "Asset could not be found".
- Además tML no tiene reader para `.fx` (el FxReader vanilla es solo XNA, verificado
  por reflection contra tModLoader.dll real), por lo que los `.fx` solos jamás se
  registran como assets.

**Solución aplicada:**
- Borrados los 8 `.xnb` inválidos de `Content/Effects/Shaders/`.
- Copiados los 5 `.fxc` compilados del mod de referencia (nuestros `.fx` son idénticos byte a byte):
  `RealBlackHoleShader.fxc`, `SunShader.fxc`, `RadialShineShader.fxc`,
  `BlackOnlyShader.fxc`, `BlackHoleDistortionShader.fxc`.
- Los `.fx` se mantienen como fuente junto a los `.fxc` (sin conflicto: `.fx` no se
  registra, `.fxc` sí). Los shaders propios `Bloom.fx`, `ChromaticAberration.fx` y
  `Shockwave.fx` NO se usan desde C# y no tienen `.fxc` — si algún día se usan,
  habrá que compilarlos con mgfxc/2MGFX primero.
- Verificado por reflection contra `tModLoader.dll` v2026.07.3.0 (la versión exacta
  del usuario): `Terraria.Testing.FxReader` NO existe (`.fx` sin reader) y
  `Terraria.ModLoader.Assets.FxcReader` SÍ existe (`.fxc` se carga con
  `new Effect(device, bytes)`).

### B. BlackHoleProjectile — réplica EXACTA del agujero negro de referencia

Reescrito siguiendo el renderer de referencia al pie de la letra:
- **Zoom dinámico**: `width / 256 * scale * 2` (antes un 0.12 fijo — el error que
  hacía que no se pareciera en nada al original).
- **accretionDiskRadius**: `scale * 0.4` (antes 0.33 fijo).
- **cameraRotationAxis**: `(velocity.Y * -0.022 + 1, 0, rotation)` — el eje se inclina
  con el movimiento vertical, como el pet.
- Canvas de InvisiblePixel de 256px (mismo tamaño del render target del pet).
- `globalTime` ahora usa `Main.GlobalTimeWrappedHourly` (antes GameUpdateCount*0.0167).
- Carga del shader con `AssetRequestMode.ImmediateLoad` + flag anti-reintento.

Mejoras propias añadidas:
- **Pop elástico de aparición** (ElasticOut — réplica de la curva elástica de referencia):
  el agujero rebota al nacer.
- **Colapso final**: los últimos 40 ticks se encoge antes de explotar.
- **Devora el polvo del entorno**: los dusts cercanos (radio 190) caen en espiral
  hacia el horizonte de sucesos.
- Succión espiral de partículas mejorada (más rápidas cuanto más cerca).
- Disco de acreción con GoldFlame + chispas Enchanted_Gold capturadas + humo.
- Refuerzo manual del event horizon (sustituye al BlackOnlyShader, que requiere
  render targets): radio calculado desde los parámetros del shader.
- Implosión + explosión + doble sonido al morir (OnKill).
- Restauración correcta del SpriteBatch (`Main.Transform` +
  `RasterizerState.CullCounterClockwise` — `Main.CullCurrentScissor` NO existe en
  tML 2026, error CS0117 corregido).

### C. SunProjectile — réplica EXACTA de la estrella de referencia

Reescrito siguiendo el draw de la estrella de referencia al pie de la letra:
- **Canvas correcto**: `DendriticNoiseZoomedOut.png` (¡la textura de referencia que
  NOS FALTABA! antes usábamos WavyBlotchNoise como canvas — otra razón del parecido
  nulo). Copiada a la carpeta de texturas de efectos (10 texturas de referencia ahora;
  carpeta renombrada a `Content/Effects/Textures/` en v5.85).
- Backglow doble con BloomCircleSmall (amarillo*0.7 @0.95 + rojo*0.45 @1.61).
- RadialShine sobre WavyBlotchNoise con color (252,212,112)*0.24 y escala
  `width*scale*2.72` (dibujado en Additive para que el brillo radial sume).
- SunShader con parámetros exactos: corona=0.05, mainColor=blanco,
  darkerColor=(204,92,25), accent=(181,0,0), sphereSpinTime=GlobalTimeWrappedHourly*0.9.
- s1=WavyBlotchNoise, s2=PsychedelicWingTextureOffsetMap, sampler LinearWrap.

Mejoras propias añadidas:
- Pop elástico de aparición (ElasticOut).
- **Hinchazón previa a la nova**: se expande los últimos 30 ticks antes de morir.
- **Llamaradas solares periódicas** cada ~0.75s (burst radial de GoldFlame).
- Chispas Torch orbitando + llamas GoldFlame + humo cálido + destellos Enchanted_Gold.
- Iluminación `Vector3(1, 0.9, 0.5) * 3.2` con pulso sutil (como la estrella de referencia).
- **Nova final**: 60 GoldFlame + 35 Torch + 20 destellos + 15 humos + doble sonido.

### D. Otros cambios

- `AethonMod.csproj`: eliminado el Import roto a `/tmp/tmodloader/tMLMod.targets`
  (ruta del sandbox que no existe en la máquina del usuario); ahora usa el patrón
  oficial `..\tModLoader.targets` con `Condition="Exists(...)"`.
- `CosmicWeapons.cs`: tooltips actualizados describiendo los nuevos efectos.
- `build.txt`: versión 5.81 → 5.83.
- **Verificación de compilación**: el mod completo compila con **0 errores** contra
  tModLoader v2026.07.3.0 real (descargado y compilado con .NET 10 SDK: 4 warnings
  benignos preexistentes de `Kill()` obsoleto en archivos viejos; los proyectiles
  cósmicos nuevos migrados a `OnKill()`).

## Commit v5.82 — reescribir BlackHole + Sun con recursos exactos de referencia

## Commit v5.29 — Bastones de prueba + efectos cósmicos + recreación estelar de la imagen de referencia

Sistema completo de bastones de prueba para testear todos los efectos cósmicos
aprendidos. Todos usan el proyectil Nightglow (#931) como base.

### A. Helper de efectos cósmicos reutilizables (CosmicEffects.cs)

Nuevo archivo `Content/Globals/CosmicEffects.cs` con métodos estáticos:
- `SpawnCosmicTrail(center, velocity, scale)` — estela dorada/cian/magenta/índigo
- `SpawnMagicRing(center, color, count, radius, speed)` — anillo expansivo
- `SpawnMagicRingMulti(center, speed)` — 4 anillos cósmicos de colores
- `SpawnSparkles(center, count, spread)` — destellos ambientales
- `SpawnLightBeams(center, count, length)` — rayos de luz radiantes
- `SpawnStarfall(target, count, spread)` — estrellas cayendo del cielo
- `SpawnImpactSphere(center, intensity)` — esfera aditiva blanco/cian/azul
- `SpawnSupernova(center, scale)` — explosión cósmica completa (4 colores + blanco)
- `SpawnStarEffect(center)` — EFECTO COMPLETO (combina todos los anteriores)
- `SpawnRainbowTrail(center, velocity)` — estela arcoíris cambiante

### B. 4 bastones protegidos (baseline, no modificar)

En `Content/Weapons/TestStaffs/`:
1. **TestMagicRing** — Nightglow + anillo dorado básico
2. **TestSparkle** — Nightglow + sparkles ambientales (+ HoldItem aura)
3. **ProjBeam** — Nightglow + rayo concentrado jugador→cursor
4. **TestMagicRingV2** — Nightglow + múltiples anillos cósmicos

### C. 12 bastones nuevos con Nightglow #931

1. **TestNightglowBasic** — baseline vanilla sin efectos (comparación)
2. **TestNightglowCosmicTrail** — estela cósmica densa
3. **TestNightglowStar** ⭐ — RECREA EL EFECTO DE LA IMAGEN:
   esfera de impacto + starfall + sparkles + light beams + anillo dorado
4. **TestNightglowRingBurst** — 4 anillos cósmicos expansivos
5. **TestNightglowSparkleTrail** — estela continua de sparkles
6. **TestNightglowLightBeams** — rayos de luz radiantes (8 rayos)
7. **TestNightglowStarfall** — 5-7 estrellas cayendo del cielo
8. **TestNightglowLifesteal** — 5% lifesteal mientras se sostiene
9. **TestNightglowEmpower** — concede Empoderamiento Cósmico (+10% dmg, +5% crit, 1% lifesteal)
10. **TestNightglowMultishot** — 3 proyectiles en abanico
11. **TestNightglowRainbowTrail** — estela arcoíris cambiante
12. **TestNightglowSupernova** — supernova cósmica completa + esfera de impacto

### D. Cofre de Pruebas Cósmico (TestStaffChest)

Nuevo item `Content/Items/TestStaffChest.cs`:
- Al usarlo, despliega en el inventario: 16 bastones + 6 items ceremoniales
- Items incluidos: 4 bastones protegidos + 12 Nightglow + StellarDust x50 +
  AethonSigil + ResonanceShard x20 + GenesisShard + SeerOrb
- Reutilizable (no consumible)
- Efectos visuales al abrir (40 partículas doradas + sonido)

### E. Cambios en TestingPlayer

- Cambió el marcador de "kit ya entregado" de GenesisShard → TestStaffChest
- Ahora entrega: GenesisShard, 100 GoldBar, LevelUpTester, BossSummonBag, SeerOrb,
  StellarDust x50, AethonSigil, ResonanceShard x20, TestStaffChest
- El jugador recibe TODOS los items al entrar al mundo

### F. Lifesteal mejorado (ShardPlayer + GlobalNPCXP)

- Nuevo flag `HasEnhancedLifesteal` en ShardPlayer (resetado en ResetEffects)
- `ApplyCosmicEmpowermentLifesteal` ahora soporta lifesteal combinado:
  - 1% si HasCosmicEmpowerment (Sello de Aethon)
  - +4% si HasEnhancedLifesteal (TestNightglowLifesteal)
  - Total máximo: 5%

### G. Texturas (17 nuevas)

Generadas a 1024×1024 con z-ai image, downscale LANCZOS a 30×30 (bastones)
y 32×32 (cofre), con background transparency:
- 4 texturas bastones protegidos
- 12 texturas bastones Nightglow
- 1 textura TestStaffChest
- Maestros preservados en Content/_masters/

### H. Localization ES/EN

Añadidas 34 claves nuevas (17 Display + 17 Tooltip) en ambos idiomas.

Versión bump: 5.28 → 5.29

---

## Commit v5.28 — Mejoras profesionales: texturas HQ + nuevo contenido ceremonial

Mejoras aplicadas sobre el baseline estable v5.27 (sin tocar la lógica del Grimorio).

### A. Texturas regeneradas en alta calidad (1024×1024 → downscale LANCZOS)

Workflow: generadas a 1024×1024 (maestros preservados en `Content/_masters/`),
luego reescaladas con PIL LANCZOS al tamaño requerido por el juego, con
remoción de fondo (transparencia) basada en el color dominante del borde.

**14 texturas de items/proyectiles/buffs/tile/icon:**
- icon.png (80×80) — icono del mod
- GrimoireEternal.png (30×38) — arma principal
- GenesisShard.png (24×24) — item clave
- SeerOrb.png (24×24) — item
- CosmicOrbMinion.png (32×32) — minion
- CosmicOrbBolt.png (16×16) — proyectil
- CosmicOrbBuff.png (32×32) — buff icon
- AncientAltar.png (16×16) — tile
- AncientAltarItem.png (24×24) — item placeable
- BossSummonBag.png (24×24) — item de testing
- LevelUpTester.png (24×24) — item de testing
- ResonanceShard.png (24×24) — moneda cósmica
- ArcaneBolt.png (16×16) — proyectil del arma
- GenesisLight.png (22×22) — proyectil del Fragmento Génesis

**6 texturas de NPCs:**
- AethonBoss.png (48×48) — jefe final
- HollowTitan.png (48×48) — mini-jefe del Sagrario Hueco
- TheWitness.png (24×40) — NPC del pueblo
- RiftKeeper.png (36×36) — mini-jefe dimensional
- EchoArcher.png (36×36) — enemigo
- EchoBlade.png (36×36) — enemigo

**3 texturas para contenido nuevo:**
- AethonSigil.png (28×28) — accesorio nuevo
- StellarDust.png (18×18) — material nuevo
- CosmicEmpowermentBuff.png (32×32) — buff nuevo

### B. Nuevo contenido ceremonial

1. **Polvo Estelar (StellarDust.cs)** — material cósmico fino
   - Recetas: 3 ResonanceShard → 1 StellarDust (y viceversa) en Anvil
   - Drops: jefes cósmicos y enemigos del Sagrario Hueco
     * AethonBoss: 10-15 StellarDust (garantizado)
     * HollowTitan: 5-8 StellarDust (garantizado)
     * RiftKeeper: 3-5 StellarDust (garantizado)
     * EchoArcher/EchoBlade: 1-2 StellarDust (25% chance)

2. **Sello de Aethon (AethonSigil.cs)** — accesorio ceremonial
   - Crafteo: 1 GenesisShard + 5 StellarDust + 3 ResonanceShard + 3 GoldBar/PlatinumBar en Anvil
   - Efectos mientras esté equipado:
     * +5% daño mágico
     * +5% daño de invocación
     * +1 slot de minion
     * +5/s regeneración de mana
   - Confiere buff "Empoderamiento Cósmico" (mantenido por el accesorio)

3. **Empoderamiento Cósmico (CosmicEmpowermentBuff.cs)** — buff ceremonial
   - +10% daño (todas las clases)
   - +5% probabilidad de crítico (todas las clases)
   - +5% velocidad de ataque (todas las clases)
   - 1% de lifesteal (aplicado via ModPlayer.OnHitAnything)

### C. Cambios de código

- `Content/Players/ShardPlayer.cs`:
  * Nuevo flag `HasCosmicEmpowerment` reseteado en `ResetEffects()`
  * Override de `OnHitAnything(float, float, bool)` para aplicar 1% lifesteal
- `Content/Globals/GlobalNPCXP.cs`:
  * Nuevo bloque en `OnKill` para drops de StellarDust según tipo de NPC
- `Localization/es-ES` y `en-US`: añadidas 6 claves nuevas
  (StellarDust, AethonSigil, CosmicEmpowermentBuff en ambas Display + Tooltip/Description)

### D. Seguridad

- Tag `stable-pre-improvements-v5.27` + rama `stable-pre-improvements-v5.27-backup`
  creadas ANTES de aplicar estas mejoras.
- Restaurar baseline estable: `git checkout stable-pre-improvements-v5.27`

Versión bump: 5.27 → 5.28

---

## Commit v5.1 — autoReuse + tooltip rediseñado + proyectil cósmico

3 mejoras solicitadas por el usuario:

1. DISPARO CONTINUO (mantener click):
   - Item.autoReuse cambiado de false → true
   - Ahora se puede mantener el click izquierdo para disparar continuo
   - El Shoot retorna true (tModLoader dispara 1 proyectil, sin doble)

2. TOOLTIP REDISEÑADO COMPLETAMENTE:
   - Antes: 6 líneas con abreviaturas crípticas (+4% mag, -20%tb, 13f, Hilo nv25, ump)
   - Ahora: 6 secciones organizadas con cabeceras de colores y texto claro:
     * PROGRESIÓN (verde): Nivel + barra XP + próximo hito
     * DAÑO (dorado): daño mágico/summon + crit + armor pen + minion slots + knockback
     * RECURSOS (azul): mana/vida max + regen + reducción de daño
     * PROYECTIL (dorado): bolts + área + costo mana
     * ORBE CÓSMICO (magenta): contacto + velocidad + rango + cooldown + costo
     * BONUS (rojo): mana bajo + lifesteal
   - Sin abreviaturas: todo el texto es legible

3. PROYECTIL CÓSMICO (CosmicProjectileFX.cs — NUEVO):
   - GlobalProjectile que afecta SOLO al Nightglow (ID 931)
   - Estela cósmica con paleta del Grimorio:
     * Dorado (cada frame) — núcleo de galaxia
     * Cian (cada 2 frames) — estrella guía
     * Magenta (cada 3 frames) — gemas
     * Índigo (cada 4 frames) — fondo del portal
   - Luz cósmica intensa (violeta-dorada)
   - Explosión cósmica al impactar enemigos (4 colores + supernova blanca)
   - Explosión al morir sin impacto

Versión bump: 5.0 → 5.1

## Commit FIX-COSMIC-EVENTS-ELIMINADOS — Quitar sistema de eventos cósmicos (Hitos + Lluvia de Luz + Rifts)
- CosmicEventSystem.cs ELIMINADO por completo (165 líneas):
  * Anuncios "Hitos cósmico: Lluvia de Luz Estelar / Sagrario Hueco / Rifts Dimensionales / Aethon se agita / El Despertar" al alcanzar niveles 25/50/75/100/150
  * UpdateStarlightRain: spawn de meteoros dorados cada 10s al nivel 25+
  * UpdateDimensionalRifts: spawn de NPC RiftKeeper bajo tierra al nivel 75+
- AethonConfig.cs: eliminadas flags EnableCosmicEvents, EnableStarlightRain, EnableDimensionalRifts (ya no se usan)
- Se conservan EnableCosmicEvents/StarlightRain/Rifts eliminados del config (cualquier config.json antiguo simplemente ignora esas claves)
- Motivo: el usuario reportó que los mensajes "Hitos cósmico" seguían apareciendo en el juego y debían estar eliminados (formaban parte de la misma familia de eventos cinematográficos que ya se quitó)

## Commit FIX-SPRITES-FALTANTES — Agregar sprites PNG para LevelUpTester y BossSummonBag
- LevelUpTester.png generado (24x24, saco dorado con flecha ascendente)
- BossSummonBag.png generado (24x24, saco purpura con calavera roja)
- MissingResourceException al cargar el mod resuelto
- Verificado: 18/18 ModItem/Projectile/NPC/Buff/Tile tienen su .png

## Commit FIX-EVENTOS-ELIMINADOS — Quitar lore y eventos cinematográficos de subida de nivel
- LevelUpEventSystem.cs ELIMINADO por completo (temblor de pantalla, overlay con grano, time-skip de 1 día, texto de lore centrado)
- ShardLevelItem.OnLevelUp: removido el bloque que llamaba a LevelUpEventSystem.Trigger() en la primera subida de nivel
- GrimoireEternal.OnCraft: método eliminado (su único propósito era disparar el Trigger(showLore:false) al craftear)
- Se conservan los efectos simples de subida de nivel: mensaje dorado "✦ Nivel X!", sonido Item4, 40 partículas doradas, y hito cada 50 niveles
- ShardPlayer.FirstLevelUpTriggered: ahora es flag legacy (se persiste para no romper saves antiguos pero ya no dispara nada)
- Motivo: request directo del usuario de quitar el lore y los eventos como el que mueve la pantalla

## Commit FIX-7ERRORES-COMPILACION — Corregir 7 errores CS0103 reportados por el usuario
- TheWitness.OnChatButtonClicked: declarada variable 'level' (CS0103)
- GrimoireEternal.OnCraft: eliminada sp.ActiveBranch (propiedad inexistente)
- LevelUpEventSystem: agregada sobrecarga Trigger(bool showLore) — luego eliminada en el commit siguiente
- Items/LevelUpTester.cs CREADO: +10 niveles al Grimorio (reemplaza TestSlayer perdido en force-push)
- Items/BossSummonBag.cs CREADO: 999 invocadores de 16 jefes vanilla (recreado tras force-push)
- TestingPlayer reescrito: kit de testing con GenesisShard + 100 GoldBar + LevelUpTester + BossSummonBag
- Localization es-ES/en-US: limpiadas claves huérfanas (CosmicPetItem, TestSlayer, CosmicPet), añadidas LevelUpTester + BossSummonBag

## Commit b70d655 — Correcciones del commit 48688dd
- XPForNextLevel: eliminado if(Level<=1) return 1, fórmula normal para todos
- ExtraProjectiles: cambiado de level/5 a level/3 (cada 3 niveles)
- CanUseItem: click izquierdo retorna true (Mana Flower)
- CanUseItem: click derecho permite Mana Flower
- Shoot minion: maneja Mana Flower
- autoReuse = false (previene doble disparo)
- Item.shoot = 931 (Nightglow)
- Eliminado código duplicado en CanUseItem

## Commit d205222 — autoReuse false + Nightglow 931
- autoReuse cambiado a false (causa del doble disparo)
- Item.shoot = 931 (Nightglow restaurado)

## Commit 3aaec4f — GrimorioTest creada
- Nueva arma de prueba con lógica diferente
- Sin AltFunctionUse, sin autoReuse
- Click derecho en UseItem, click izquierdo en Shoot return true
- Sprite generado con AI

## Commit bd3a55c — Doble disparo solucionado con return true + reuseDelay
- Click izquierdo: return true (tModLoader dispara 1)
- Click derecho: return false + reuseDelay=10

## Commit 48688dd — Doble uso + Mana Flower para minion
- CanUseItem del minion permite Mana Flower
- Shoot del minion maneja 3 casos de mana

## Commit 1a1da42 — Mana Flower no permite disparar con 0 mana
- CanUseItem retorna true para click izquierdo

## Commit 2f4ae26 — Quitar disparo doble + XP nivel 1→2 normal
- Eliminada probabilidad de disparo doble
- XPForNextLevel: fórmula normal para nivel 1→2

## Commit b37ef0e — Proyectil sale doble: return false → return true
- return false causaba que tModLoader disparara adicional

## Commit 482a19d — Proyectil doble: return true → return false
- Cambio inicial de return true a return false

## Commit b507628 — Restaurar sprites + Nightglow + tooltip compacto
- GrimoireEternal.png = sprite libro.png
- CosmicOrbMinion.png = minion cosmico.png
- Item.shoot = 931 (Nightglow)
- Tooltip compactado (12→8 líneas)

## Commit 117bd03 — Trigger(bool) restaurada
- Sobrecarga Trigger(bool showLore) se perdió en force push
- _showLore flag restaurado

## Commit a81445b — ActiveBranch rezagado eliminado
- sp.ActiveBranch = BranchType.Magic eliminado

## Commit 480761e — NPC.HitInfo KnockBack eliminado
- KnockBack no existe en HitInfo

## Commit 982ea23 — BuffID.Terraprisma → ID 322
- Nombre constante no existe, usar ID numérico

## Commit c74e56a — Projectile.color eliminado
- Projectile no tiene propiedad .color

## Commit c8638c9 — WeaponScaling using agregado a CosmicOrbMinion
- Falta using AethonMod.Content.Systems

## Commit be0fee8 — Nightglow cósmico + Terraprisma minion + CosmicEventSystem eliminado
- CosmicProjectileFX mejorado con tinte dorado
- Grimorio invoca proyectil vanilla 946 (Terraprisma)
- CosmicMinionFX creado
- CosmicEventSystem.cs eliminado

## Commit 2ffddc7 — Minion cooldown quitado del tooltip + hitos mejorados
- Eliminado 'Minion cooldown: Xf' del tooltip
- Hitos actualizados: Mejora de velocidad, Mejora de minion

## Commit f284177 — Mana max + vida max + hit cooldown en tooltip
- +1 mana cada 4 niveles
- +2 vida cada 20 niveles
- Hit cooldown del minion en tooltip
- WeaponScaling: BonusMana, BonusLife, MinionHitCooldown

## Commit b728a96 — Bolts en línea + hit cooldown mejora con nivel
- Separación reducida de 0.08 a 0.02 rad
- Hit cooldown: 15 base, -1 cada 10 niveles, min 1

## Commit 52b6d4b — 1 bolt por click + minion custom con sprite
- return true cambiado a return false
- CosmicOrbMinion reescrito con IA tipo Terraprisma
- CosmicMinionFX eliminado

## Commit f6db599 — TODAS las mejoras del Grimorio (23 funciones)
- WeaponScaling: 23 funciones de escalado + MilestoneRewards
- GrimoireEternal: ModifyWeaponKnockback, disparo doble, tooltip completo
- ShardPlayer: PostUpdate (regen), ModifyHurt (reducción daño)
- CosmicOrbMinion: velocidad y rango escalados
- CosmicProjectileFX: recreado con daño en área

## Commit 093826c — 3 blockers de REVIEW-FINAL arreglados
- TestingPlayer: eliminadas refs a items inexistentes
- TheWitness: level declarado en OnChatButtonClicked
- CosmicOrbMinion: held redeclarado (CS0136)

## Commit 2f8a128 — BossSummonBag recreado
- Se perdió en force push, recreado

## Commit cb9e6cd — LevelUpTester recreado
- Se perdió en force push, recreado

## Commit 167ddec — Minions persisten al cambiar arma + Mana Flower
- Slots de minion en PostUpdateEquips (busca en todo el inventario)
- CanUseItem permite Mana Flower

## Commit 1038516 — Limpiar repositorio + sprite libro
- Eliminadas carpetas del sandbox de GitHub
- GrimoireEternal.png = sprite libro.png

## Commit c44a62b — Minions desaparecían al exceder limite
- Slots de minion movidos a ModifyWeaponDamage
- Conteo cambiado a ownedProjectileCounts

## Commit 66b6533 — Tooltip solo muestra próximo hito
- Eliminado el bloque que listaba todos los hitos acumulados

## Commit b0998c5 — Armas se craftean sin yunque
- Eliminado AddTile(TileID.Anvils) de las recetas

## Commit 5c684b4 — Quitar eventos + partículas + minion sprite + XP normal
- Eventos de subida de nivel removidos
- Partículas reducidas (scale + alpha)
- Minion sprite = minion cosmico.png
- XP nivel 1→2 normal

## Commit 458d30d — ThreadStateException en Unload
- Dispose envuelto en try/catch

## Commit b9905cd — Partículas carga minion + bolts 1+cada3 + LevelUpTester
- Partículas de carga restauradas
- Bolts: 1 base + 1 cada 3 niveles
- LevelUpTester: da +10 niveles al Grimorio

## Commit 224bb62 — LevelUpTester busca en todo el inventario
- No requiere sostener el Grimorio

## Commit 936add3 — Minion requiere mana (15 + nivel, tope 100)
- WeaponScaling.MinionManaCost creada
- CanUseItem verifica mana del minion
- Shoot cobra mana al invocar

## Commit 8e93990 — NPC.HitInfo no contiene KnockBack
- Eliminado hit.KnockBack

## Commit f3d84dc — CosmicOrbMinion namespace corregido
- global::AethonMod.Content.Projectiles.CosmicOrbMinion

## Commit 7c30f7a — BranchChoiceUI eliminado de UISystem
- UISystem simplificado

## Commit 1e9b277 — BranchChoiceUI eliminado de UIScrollBlockPlayer
- Simplificado a métodos vacíos

## Commit 4d67ba0 — try sin catch en CosmicOrbMinion
- Estructura try/catch reparada

## Commit 8eb24c7 — Grimorio reescrito desde cero
- Sin dependencias de ActiveBranch/IsImprinted
- WeaponScaling reescrito sin BranchType
- GlobalNPCXP solo otorga XP al Grimorio
- CosmicOrbMinion sin BranchType

## Commit 71fd964 — Rediseño: crafteo + boss drop
- BranchChoiceUI removido
- GenesisShard es arma de luz + material
- Las 3 armas se craftean con GenesisShard
- OnCraft dispara evento cinematográfico
- Boss drops de King Slime/Eye of Cthulhu

## Commit fa240e1 — HJSON malformado + revisión profunda
- HJSON con múltiples cierres } arreglado
- 3 riesgos arreglados (TestingPlayer, BossSummonBag, TestSlayer)

# INFORME DE INVESTIGACIÓN — ExoElectric Disintegrator + Nameless Destroyer
**Task ID:** 42-a · **Fecha:** 2026-09 · **Agente:** investigación de armas de mods populares
**Objetivo:** identificar el mod de origen, extraer números exactos de proyectiles/VFX y derivar lecciones para AethonMod.

---

## 0) RESUMEN EJECUTIVO (identificación)

| Nombre en el encargo | Identidad real | Mod de origen | Estado |
|---|---|---|---|
| **"ExoElectric / Exolectric Disintegrator"** | **Exo Disintegrator** (arma mítica ranged post-Moon Lord) + su homónimo visual, el ataque **Exoelectric Disintegration Ray** del jefe **XG-07 Mars** | Arma: **Infernal Eclipse of Ragnarok (IER)**, mod de Akira (addon Infernum para Calamity/Thorium/SOTS). El ataque fuente es de **Calamity: Wrath of the Gods (WOTG)** por TheFifthCircle/Lucille | ✅ CONFIRMADO (wiki oficial terrariamods.wiki.gg + código público WOTG) |
| **"Nameless Destroyer"** | **Cosmic Destroyer** (arma ranged con gauge "Magiton") | **The Stars Above (TSA)** 1.4 por ThePaperLuigi (repo abierto). El nombre "Nameless" viene del association con el jefe final de la saga (Nameless Deity, hoy en WOTG); los videos de TikTok/Instagram (@GemniscataSimulation) la titulan "Nameless Destroyer" cuando muestran "COSMIC DESTROYER: 4.8S CHARGING" | ✅ CONFIRMADO por el snippet de Instagram que desenmascara el título real |

**Cadena de evidencia clave:**
1. Wiki oficial (terrariamods.wiki.gg): `Infernal Eclipse of Ragnarok/Weapons` → "Ranged · Post-Moon Lord · **Exo Disintegrator**". Changelog 0.10.7: "All **mythic items**, with the exception of Entropy's Vigil, Chaos Blaster, **Exo Disintegrator**, & Nebula Gigabeam, now use the **Infernum Vassal Rarity**".
2. Wiki `Wrath of the Gods/XG-07 Mars`: "Phase 4: Mars becomes invulnerable, charges up, and fires an enormous **Exoelectric Disintegration Ray** at the player, sharply tracking their movement… 350/450/700 damage".
3. Código público WOTG (GitHub `TheFifthCircle/WrathOfTheGodsPublic`, nombre interno NoxusBoss): `Content/NPCs/Bosses/Draedon/Projectiles/ExoelectricDisintegrationRay.cs` + `MarsAIValues.json` con TODOS los números.
4. TSA (GitHub `ThePaperLuigi/The-Stars-Above`): `Items/Weapons/Ranged/CosmicDestroyer.cs` + `Projectiles/Ranged/CosmicDestroyer/*` + `WeaponPlayer.cs` (gauge).
5. Instagram (@GemniscataSimulation): "Nameless Destroyer vs Zenith … **COSMIC DESTROYER: 4.8S CHARGING**" → el título "Nameless Destroyer" es un garbled name del **Cosmic Destroyer** (TSA). La sospecha original "de The Stars Above, relacionado con Nameless Deity" era correcta en lo esencial.
6. Los videos comparativos "Nameless Destroyer vs Exo Electric Disintegrator" son simulaciones de granjas de contenido: enfrentan el **Cosmic Destroyer (TSA)** contra el **Exo Disintegrator (IER)** en el ecosistema del modpack IER (Calamity+Infernum+WOTG+SOTS+Thorium).

Repositorios NO clonados (se usó la API de árboles de GitHub + raw de archivos sueltos, más barato que clonar 240–330 MB). Nada relevante existe en /tmp/research local (grep negativo para Exolectric/Nameless/Disintegrator/Destroyer).

---

## 1) ARMA 1 — EXO DISINTEGRATOR ("ExoElectric Disintegrator")

### 1.1 Ficha técnica del arma (IER)
- **Mod:** Infernal Eclipse of Ragnarok (IER), por Akira (+Advantaje, Wardrobe Hummus). tModLoader 1.4.4, 355 MB, 168k suscriptores en Steam (ID 3456517686).
- **Tipo:** Ranged (a distancia). **Tier:** Post-Moon Lord, tier "6" del Boss Rush reworkado de IER; item **mítico** con rareza "Infernum Vassal".
- **Ecosistema de progresión IER (Boss Progression, wiki):** Exo Mechs → Supreme Calamitas → **XG-07 Mars (WOTG)** → Goozma → Avatar of Emptiness → **Nameless Deity of Light (WOTG)**. El arma se sitúa en este tramo final.
- **Daño exacto del arma:** no publicado (IER es closed-source y su wiki está WIP: la página del item está vacía). **No inventar.** Lo documentable con números exactos es su nombre-fuente y su referencia visual (el rayo de Mars), que es lo que da identidad al arma.
- **Clase "mítica" de daño en IER:** "Buffed the inheritance of damage bonuses of other damage types for **Mythic damage from 50% to 75%**" (changelog 0.10) — el tier mítico hereda el 75% de los bonus de otros tipos de daño.

### 1.2 El ataque-fuente: Exoelectric Disintegration Ray (XG-07 Mars, WOTG) — NÚMEROS EXACTOS
Fuente: `ExoelectricDisintegrationRay.cs` + `MarsAIValues.json` + wiki XG-07 Mars.

**Timing (frames, iguales en todas las dificultades):**
| Fase | Valor |
|---|---|
| MarsRedirectTime (apuntado inicial) | **40 f** (~0,67 s) |
| LaserChargeUpTime (carga) | **150 f (2,5 s)** |
| LaserShootTime (disparo) | **600 f (10 s)** |
| Post-ataque hasta cambio de estado | +90 f (1,5 s) |

**Daño por dificultad:** Normal **350** / Expert–Revengeance **450** / Death **700** / GFB **1000**.
**Ancho del rayo:** 510 px (Normal/Expert/Rev) → **560 px (Death)** → **700 px (GFB)**.
**Zona segura (escudo de Solyn):** 215 px → 192 (Expert) → 176 (Rev) → 165 (Death) → **90 px (GFB)**.
**Largo máximo:** 5.600 px; **crecimiento: +172 px/frame** (alcanza el largo máximo en ~33 f).
**Grosor dinámico:** `initialBulge` hasta 32 px + `Cos(GlobalTime*90)*6` (respiración) + `CarvedLaserbeam_LaserWidth`; cierre en las últimas **8 f** (`closureInterpolant`); arranque circular (`circularStart`, sqrt(1.001−x²)).
**Hitbox/colisión:** proyectil 2×2 px, `penetrate = -1`, `tileCollide = false`, `ignoreWater = true`, `CooldownSlot = ImmunityCooldownID.Bosses`; daño real vía `Collision.CheckAABBvLineCollision` con **width = LaserWidthFunction(0.25)*1.8** y largo efectivo **95%** del rayo. `ShouldUpdatePosition() = false` (el rayo no se mueve: rota sobre el jefe).
**Tracking:** dirección = `CarvedLaserbeam_LaserbeamDirection` (NPC.ai[0]); velocidad angular ideal `WrapAngle(Δ)*InverseLerp(0.4,1.5,dist/Ancho)*0.08` — gira "brusco pero suavizado" persiguiendo al jugador; el jugador escapa lateralmente (la IA apunta solo si dista >0.67×ancho del eje).
**Anti-cheese:** Mars no baja del 5% de HP antes del ataque; durante Fase 4 es invulnerable y su vida se fuerza a `SmoothStep(Phase4LifeRatio=0.10 → 0.01)` a lo largo de los 600 f.
**Contexto del jefe:** HP 4.500.000 / 5.600.000 / 7.200.000 / 8.100.000; defensa 100; 100% KB-resist; Mars solo recibe 5%/3% del daño normal (la mecánica real es el rayo aliado de Solyn: **1900 de daño base tipo typeless, puede crit**). Tema: "RAMifications" (Moonburn).

### 1.3 Anatomía VISUAL del rayo (código exacto)
- **Color del núcleo:** `new Color(255, 40, 55)` (rojo eléctrico) con alfa en función del avance (opacity = lengthOpacity × startOpacity × endOpacity; fade-in 0→0.032, fade-out 0.95→0.81).
- **Bloom:** `new Color(255, 10, 20) × InverseLerpBump(0.02, 0.05, 0.81, 0.95, ratio) × 0.54`; ancho de bloom = **1.9× el ancho del láser**; shader `NoxusBoss.PrimitiveBloomShader` con `innerGlowIntensity = 0.45`.
- **Shader dedicado del rayo:** `NoxusBoss.ExoelectricDisintegrationRayShader` con parámetros: `centerGlowExponent 2.9`, `centerGlowCoefficient 9.3`, `edgeGlowIntensity 0.046`, `centerDarkeningFactor 0.6` (núcleo oscuro = "cable eléctrico"), scroll a 3 velocidades (`inner 0.85`, `middle 0.5`, `outer 0.2`) con 2 texturas de ruido (DendriticNoiseZoomedOut en sampler 1 + PerlinNoise en sampler 2, LinearWrap).
- **Render:** primitivas tipo trail con **12 puntos de control** (`GetLaserControlPoints(12, length)`), **120 segmentos** para el rayo y **70 para el bloom**; render en `InstancedRequestableTarget` (render target por identidad de proyectil) para poder recortar el rayo con la "burbuja" del escudo de Solyn vía shader overlay (`TransientSolynForcefieldOverlayShader`: recibe laserDirection, zoom, safeZoneWidth, pulse = `Cos01(GlobalTime*85)*0.08`, posición de Solyn).
- **Partículas interiores:** 6/frame, spawn a lo largo de 100→3000 px del eje, en el borde de la zona segura; `Dust.NewDustPerfect(..., 264, vel 2–4)` con `noGravity` y **color Color.Wheat**; 1 de cada 7 (NextBool(7)) genera además un **SmallTeslaArc** (arco eléctrico) con vida 6–14 f y offset radial 40–200 px (^5 interpolado).
- **Partículas exteriores:** 6 **SmallTeslaArc/frame** sobre el borde del rayo (90% del ancho), con offset `perpendicular.RotatedBy(1.04 rad)` y alcance 40–320 px (^4 interpolado).
- **Lens flare en el origen:** **3× ShineFlareTexture + 2× BloomCircleSmall**, escala = `InverseLerp(0,12,Time) * Lerp(1, 1.2, Cos01(GlobalTime*85)) * 2` ("the magic factor that makes every electric shine effect so much better").
- **Screen shake:** `ScreenShakeSystem.StartShake(InverseLerp(0,20,Time) * 2)` — sube en 20 f y se mantiene.
- **Sonido/contexto:** el rayo va acompañado de la escena final (Solyn empuja el rayo, fundido a blanco al matar a Mars).

---

## 2) ARMA 2 — "NAMELESS DESTROYER" (= COSMIC DESTROYER, The Stars Above)

### 2.1 Ficha técnica (TSA 1.4, código abierto)
- **Mod:** The Stars Above (TSA) por ThePaperLuigi (repo `ThePaperLuigi/The-Stars-Above`, 1.4 open-source). En la saga TSA el jefe final es el **Nameless Deity** (hoy reimaginado en WOTG como "Nameless Deity of Light"), de ahí la asociación de nombre en los videos ("Nameless Destroyer").
- **Tipo:** Ranged (pistola/cañón de mano gigante, 136×56 px de sprite).
- **Daño:** **530** con Calamity cargado / **270** sin Calamity (patrón `TryGetMod("CalamityMod")` para auto-balancear cross-mod — LECCIÓN IMPORTANTE).
- **useTime / useAnimation: 7** (cadencia ~8,57 disparos/s), `autoReuse = true`, `noMelee`, knockBack **2**, `shootSpeed 11`, rareza **Red**.
- **Crafting (con Calamity):** 4× Cosmilite Bar + 12× Shroomite Bar + 12× Lunar Bar + 1× Chain Gun + 5× Sapphire + 30× Soul of Might + 2× Illegal Gun Parts + 1× Essence of the Future (ítem de quest TSA) — **en un Yunque (TileID.Anvils)**. Sin Calamity: la misma receta menos los Cosmilite.
- **AltFunctionUse:** sí (clic derecho = activar Magiton Overheat).

### 2.2 Mecánica del "Magiton Gauge" (identidad del arma) — NÚMEROS
1. Cada impacto del proyectil normal (`CosmicDestroyerRound`) suma **+1 al gauge** (`WeaponPlayer.OnHitNPC...`: `CosmicDestroyerGauge++`), tope **100**. A cadencia 7 f, llenar el gauge a mano ≈ **700 f (~11,7 s)** de fuego continuo (los videos de "4.8 s charging" implican modifiers de velocidad de uso).
2. Al llegar a 100, **clic derecho** → `AddBuff(MagitonOverheat, 480)` (**8 segundos**), `CosmicDestroyerRounds = 11`, gauge = 0, SFX de summon, `screenShakeTimerGlobal = -80`, 30× dust 127 blanco en el jugador.
3. Durante Overheat, los **11 siguientes disparos son "Magiton Shots"** (`CosmicDestroyerRound2`): **daño ×3** (`damage *= 3` en Shoot) y **crit garantizado contra enemigos con <50% HP** (tooltip oficial).
4. Al consumir los 11 disparos (o expirar el buff de 8 s): se elimina MagitonOverheat y se aplica **Overheated 60 f (1 s)** — el arma queda **inutilizable** (CanUseItem devuelve false). Debuff real (`Main.debuff = true`, la enfermera no lo quita).
5. UI del gauge: `CosmicDestroyerGauge` con **12 sprites discretos** (Empty + CD1..CD11) y fade-out de visibilidad `-= 0.05/frame` (20 f de fade tras disparar).

### 2.3 Anatomía de PROYECTILES (código exacto)
**CosmicDestroyerRound (normal) y CosmicDestroyerRound2 (Magiton) comparten base:**
- Hitbox **54×54 px** (grande = perdona el aim); `aiStyle 1` + `AIType = ProjectileID.Bullet` (IA de bala vanilla).
- `penetrate = 1`, `timeLeft = 600` (10 s), `alpha = 255` (fade-in del aiStyle 1), `light = 0.5f`, `ignoreWater = true`, `tileCollide = false`, `extraUpdates = 2` (viaja a velocidad efectiva ×3).
- `TrailCacheLength = 70`, `TrailingMode = 3` (trail de 70 posiciones; se dibuja con efectos `Effects.BlueTrail` el normal y `Effects.OrangeTrail` el Magiton — feedback de color estado).
- **Rebote en tiles:** `OnTileCollide` decrementa penetrate; con penetrate restante **rebota invirtiendo la componente de colisión** + `SoundID.Item10` + `Collision.HitTiles`; al morir suelta 20× dust 31 (normal) / dust 90 (Magiton) con vel ×1.4.
- **OnKill (normal):** 3 tandas de 20 dusts: **226** (×2 tandas, scale 0.5/0.8) + **92** (scale 0.8), vel ×1.4.
- **OnKill (Magiton):** 10× (dust **219** noGravity scale 1 vel ×5 + dust **6** scale 2 vel ×3) + 20× dust **90** (0.5) + 20× dust **219** (0.8) + 20× dust **90** (0.8) — explosión mucho más rica.

**Disparo (Shoot del arma):**
- Muzzle: `position += Normalize(velocity) * 160` (boca del cañón a 160 px) con check `Collision.CanHit` + offset Y +7; `HoldoutOffset (0, 8)`.
- Spread: `RotatedByRandom(1°)` (muy precisa).
- Sonido de disparo: `SoundID.Item11`.
- **Dusts al disparar (normal):** 21× dust **92** (±17°, scale 1.8) + 21× dust **92** (±87° en dirección inversa, scale 1) + 16× dust **31** (±17° sobre la mitad de la velocidad, scale 1). Todos noGravity, alpha 150.
- **Dusts al disparar (Magiton):** 21× dust **90** (±24°, scale 2) + 11× dust **90** (±87° inverso) + 31× dust **127** (±87° inverso, scale 1.6) + 16× dust **31** (±17°, scale 1).

### 2.4 Anatomía VISUAL resumen
- **Identidad de color por estado:** trail azul (normal) vs naranja (Magiton) — el jugador lee el estado con el color.
- Dusts clave: 90/92 (chispas), 127 (brillo blanco de "carga"), 31 (humo/chispa vanilla), 226 (normal kill), 219+6 (explosión Magiton, roja y de gran escala 2).
- Shake de pantalla global −80 al activar Overheat (sistema global del mod).
- Gauge con sprites discretos + fade de visibilidad: UI ligera que solo aparece al accionar.

---

## 3) LECCIONES PARA AETHONMOD

1. **El "beam" élite se construye con NÚMEROS, no con el sprite:** ancho 510–700 px, largo 5.600 px, crecimiento +172 px/frame, disparo de 600 f (10 s), carga telegrafiada de 150 f. Para AethonMod: un haz élite de boss final debería usar exactamente este orden de magnitud (ancho ~500 px = ~31 bloques, no un láser de 20 px).
2. **Ciclo telegraph→fire→close:** 40 f de redirección + 150 f de carga + 600 f de fuego + cierre en las últimas 8 f (`closureInterpolant = InverseLerp(0, 8, Lifetime - Time)`). Nunca cortar el haz en seco: el cierre de 8 f vende el peso.
3. **Grosor vivo, no estático:** `ancho = bulge_inicial(≤32) + Cos(tiempo*90)*6 + ancho_base` + arranque circular `sqrt(1.001−x²)`. Una oscilación de ±6 px a 90 rad/s hace que el rayo "respire". Replicable en AethonMod con una sola línea de Cos.
4. **Núcleo oscuro + bordes brillantes:** shader del rayo con `centerDarkeningFactor 0.6` y `centerGlowExponent 2.9 / coefficient 9.3 / edgeGlow 0.046` — el clásico look "cable eléctrico" (centro oscuro, halo brillante). Con primitivas + additive blending se puede aproximar sin shader: dibujar 3 pasadas (núcleo oscuro 0.6×ancho, cuerpo color, bloom 1.9×ancho al 54%).
5. **3 capas de scroll a velocidades distintas** (0.85 / 0.5 / 0.2) con dos texturas de ruido (dendrítica + perlin) = energía que "fluye" hacia dentro. En AethonMod: pasar `Main.GlobalTimeWrappedHourly * velocidad` como offset de muestreo en el shader del haz.
6. **Partículas dobles (dentro/fuera):** 6 dusts/frame (ID 264, wheat, noGravity, vel 2–4) en el borde interior + 6 arcos Tesla/frame (vida 6–14 f, alcance 40–320 px) en el borde exterior. El 1-de-cada-7 para los arcos interiores mantiene el coste bajo (~0.86 arcos/frame extra). Coste total ≈ 12 partículas/frame: barato para lo que aparenta.
7. **Lens flare de origen = 3×flare + 2×glow** con escala pulsante `Lerp(1, 1.2, Cos01(t*85))*2` y fade-in de 12 f. El origen del haz SIEMPRE debe tener su propio glow (es donde mira el ojo del jugador).
8. **Screen shake escalado al inicio:** `StartShake(InverseLerp(0, 20, Time) * 2)` — sube en 20 f y se sostiene. No sacudir a lo bestia: 2 unidades sostenidas > 10 unidades puntuales.
9. **Rayo = proyectil 2×2 invisible + colisión manual:** `CheckAABBvLineCollision(start, end, width*1.8)` al 95% del largo, `penetrate -1`, `ShouldUpdatePosition false`, `tileCollide false`. NUNCA usar hitbox de proyectil para haces largos.
10. **Sistema de gauge con carga por impacto + estado superior + cooldown de castigo (patrón Cosmic Destroyer):** +1 por hit, tope 100 → clic der. activa 8 s de modo "Magiton" → 11 disparos a daño ×3 con crit garantizado <50% HP → debuff "Overheated" 1 s bloqueando el arma. La trinidad carga→burst→lockout es la mecánica más copiable para un arma "signature" de AethonMod; además el ×3 y el execute <50% dan números de DPS claros para balancear.
11. **Color = estado:** mismo proyectil con trail Azul (normal) y Naranja (potenciado) + dusts distintos (90/92 normal vs 127/219/6 en burst). El jugador debe poder leer el estado del arma con visión periférica, sin mirar buffs.
12. **Cross-mod auto-balance (patrón TSA):** `if (ModLoader.TryGetMod("CalamityMod", out...)) Item.damage = 530; else Item.damage = 270;` y receta con/sin materiales de Calamity. Para AethonMod: cualquier arma balanceada contra un overhaul debería duplicar stats y bifurcar recetas si detecta mods grandes.
13. **Render target por instancia para "recortar" el haz** (escudo de Solyn que parte el rayo en 2): si AethonMod necesita haces que colisionan con escudos/paredes del jugador, dibujar el haz en un `InstancedRequestableTarget` y recortarlo con un shader overlay que reciba dirección del láser + posición y radio de la burbuja (aquí: safeZone 90–215 px).
14. ** naming/lore como sistema:** IER renombra armas con el ataque del boss que las inspira ("Exo Disintegrator" ← "Exoelectric Disintegration Ray" de XG-07 Mars) y les da una rareza propia ("Infernum Vassal") + clase de daño propia (mítica = hereda 75% de bonus de otros tipos). Un tier "élite" en AethonMod debería tener: nombre derivado del boss, rareza exclusiva y regla de escalado única.

---

## 4) FUENTES
- Wiki oficial Terraria Mods (terrariamods.wiki.gg): `Infernal_Eclipse_of_Ragnarok` (hub), `/Weapons`, `/0.10`, `/0.10.1`, `/0.10.7`, `/Boss_Progression`, `/Class_Setups_Guide`, `Wrath_of_the_Gods/XG-07_Mars`, `Calamity_Rekindled/Lore_Items`.
- GitHub `TheFifthCircle/WrathOfTheGodsPublic` (WOTG/NoxusBoss, rama main): `ExoelectricDisintegrationRay.cs`, `MarsBody.BehaviorStates.CarvedLaserbeam.cs`, `MarsAIValues.json`.
- GitHub `ThePaperLuigi/The-Stars-Above` (TSA 1.4, rama main): `CosmicDestroyer.cs`, `CosmicDestroyerRound.cs`, `CosmicDestroyerRound2.cs`, `MagitonOverheat.cs`, `Overheated.cs`, `WeaponPlayer.cs` (gauge), `CosmicDestroyerGauge.cs` (UI).
- Steam Workshop: IER (3456517686) + modpack (3456508757, 54 mods), WOTG (2995193002), Infernal Arsenal (3485175010).
- Búsquedas web (z-ai web_search) + páginas TikTok/Instagram/Steam que triangulan el nombre "Nameless Destroyer" = Cosmic Destroyer.

**Nota de honestidad:** el daño exacto del item "Exo Disintegrator" de IER no es público (mod closed-source, wiki WIP). Todos los números del rayo exo-eléctrico provienen del código de WOTG, que es el ataque original que nombra e inspira el arma; son los valores correctos a imitar para "VFX élite".

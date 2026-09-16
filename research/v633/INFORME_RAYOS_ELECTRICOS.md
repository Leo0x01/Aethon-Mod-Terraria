# INFORME: RAYOS ELÉCTRICOS EN JUEGOS Y MODS — MEJORAS PARA StormLib

**Task ID:** 54 · **Fecha:** v633 · **Alcance:** investigación web de algoritmos y código real de rayos 2D (bolt chains) para mejorar `AethonMod/Content/VFX/StormLib.cs` (tModLoader 1.4.4, C#, SpriteBatch).

---

## 0. Estado actual de StormLib (línea base)

Métodos públicos existentes: `FlickTick(hz=15)`, `IsLit(aliveChance=0.62)`, `DeathGrow`, `ZigPath(segments=12, amp=24px)` con envolvente `sin^0.8` y tope `amp ≤ len×0.30`, `Boil(amp=3px)`, `ForkTree(branchScale=0.5, maxBranches=3)` (longitud rama 0.14–0.30×tronco, spread 0.45–0.90 rad, 7 pasos, rot ±0.3 rad con autocorrección ×0.3), `Refine(ampScale=0.5)`, `PathLength`, `Strand`, `ChainBolt`, `Bolt`, `MultiBolt`, `ArcRing`, `ImpactFlash`, `EndCap`, `AddLightAlong`.

Es decir: generación por **jitter uniforme** (no fractal), forks **posteriores** al path (no integrados), flicker binario 15 Hz / 62%, y capas de pintado propias. Las 5 técnicas de abajo atacan justo lo que falta.

---

## 1. TABLA DE TÉCNICAS CON NÚMEROS

| # | Técnica / Fuente | Números exactos |
|---|---|---|
| 1 | **Midpoint displacement (fractal)** — drilian (Procyon) | Empieza con 1 segmento; **5 generaciones** → 32 segmentos; `offsetAmount` inicial = máximoOffset y **se divide a la mitad cada generación** (`offsetAmount /= 2`); desplazamiento del punto medio por la **perpendicular** con `RandomFloat(-offset, +offset)` |
| 2 | **Midpoint displacement** — Digital Ruby (Unity) | `Generations = 6` (rango 0–8) → 64 segmentos; `ChaosFactor = 0.15` → offset inicial = **len × 0.15**; `Duration = 0.05 s` → **regeneración a 20 Hz**; distancia = `rand*offset*2 − offset`; textura animada en grid **Rows×Columns** con modo PingPong |
| 3 | **Forks durante subdivisión** — Habrador (basado en drilian) | `generations = 4`; `maximumOffset = 60`; **`splitProbability = 0.8`** por segmento y generación; ángulo fork = `Random(-30°, 30°)` y luego **±20° extra** (total **20°–50°**); longitud fork **×0.6**; ancho rama **0.3** vs tronco **0.8** (37.5%) |
| 4 | **Forks durante subdivisión** — drilian | `splitEnd = Rotate(direction, ánguloPequeño) × lengthScale + midPoint` con **lengthScale 0.7**; los forks se pintan **más tenues** (solo el tronco a brillo completo); solo en generaciones alternas |
| 5 | **Animación doble rayo** — drilian | **2 bolts entrelazados**; cada bolt vive **1/3 s (20 frames)**; se regenera uno cada **1/6 s (10 frames = 6 Hz)**; el viejo se desvanece a **50%** antes de morir; **jitter por frame** de cada punto (pequeño) y **el doble en los puntos de subdivisión**; endpoints móviles permitidos |
| 6 | **Flicker por frames de textura** — vanilla Terraria (`AI_176_EdgyLightning`, zap del Thunder Zapper) | Homing a objetivo en **400 px**, velocidad **10 px/tick**; **frame aleatorio cada 3 ticks = 20 Hz**; muere si no hay objetivo |
| 7 | **Rayo vanilla estilo martiano** — vanilla ProjectileID **466 "Lightning Orb Arc"** (decompilado; mismo patrón copiado por Calamity RedLightning y Fargo CopperLightning) | Dirección nueva cada **`extraUpdates×2` ticks** vía `UnifiedRandom` encadenado por `ai[1]`; ángulo = `(rand%100)/100 × 2π`; **Y forzada negativa** (solo "baja" en su marco local); rechaza si `Y > −0.02`; **deriva horizontal acumulada `localAI[0]` limitada a ±40 px**; al tocar: `velocity = 0` (queda electrocutando); `extraUpdates = 20` (Calamity) / `4` (Fargo); trail `oldPos` de **20** (Calamity) / **60** (SystemBane) puntos |
| 8 | **Chain lightning** — Fargo's Souls CopperLightning | `extraUpdates=4`, `timeLeft = 120×5`; al golpear: **rota velocidad 2π aleatoria**, re-encadena al NPC más cercano en **1000 px**, daño **×0.8 por rebote**, aplica **`BuffID.Electrified` 120 ticks (2 s)**; luz `AddLight(0.3, 0.45, 0.5)`; si velocidad < 4 px: relanza a **20 px en dirección ±45°** |
| 9 | **3 capas glow/mid/core** — Calamity RedLightning (Storm Weaver) | Escalas **0.6 / 0.4 / 0.2**; colores **#DB683A (219,104,58) / #FF7E38 (255,126,56) / #FF8080 (255,128,128)**, todos **×0.5 alpha**; dibujo con `Utils.DrawLaser` + `DelegateMethods.LightningLaserDraw`; luz `(0.8, 0.25, 0.15)`; polvo RedTorch escala **1.7 y 1.2**, `noGravity`, 1 de cada 5 chispa extra |
| 10 | **2 capas cian/blanco** — Calamity SystemBaneLightning | Outer: **Cyan ×0.35 alpha, escala 0.5**; Inner: **White ×0.75 alpha, escala 0.3**; trail de **60** puntos; giro hacia objetivo **0.25 rad/tick**; jitter `RotatedByRandom(0.52 rad)` 1-in-10; durante homing `RotatedByRandom(0.9)` 1-in-5 |
| 11 | **Partícula trueno con shake** — Calamity ThunderBoltVFX | **Additive blend**; `Shake = Vector2.One.RotatedByRandom(2π) × (1 − Time/Lifetime) × ShakePower` (**el shake decae lineal a 0**); fade **×0.95/tick** tras t>10 (`1 − 0.05×clamp((Time−10)/10)`); `Squish.X ×= fadeFactor` (**adelgaza**); **doble pasada**: glow `Color×Opacity×0.6` + core `Lerp(White→Color, t)×Opacity`; luz `Color.ToVector3() × 3`; flip según `GlobalTimeWrappedHourly % 30 < 15` |
| 12 | **Partícula bolt** — Calamity BoltParticle | **3 variantes de frame** aleatorias; `Stretch = (0.5, 1.6)`; shrink `Scale ×= 0.95/tick`; color `Lerp(inicial, Transparent, t³)`; fadeIn con Lerp **+0.2 / −0.21** (crece y encoge); gravedad `Y += 0.25` si `|v| < 12`; pasada extra `Lerp(col, White, 0.8)` a escala **×0.8** si `glowCenter` |
| 13 | **Shader de distorsión** — Calamity HeavenlyGaleLightningShader.fx | `distortion = lerp(-1,1, tex2D(noise, coords + float2(0, uTime × sign(y>0.5?-1:1) × 1.81)).r)` → **noise scroll a 1.81 u/s en direcciones OPUESTAS arriba/abajo**; `opacity = pow(sin((y + distortion×0.15)×π), distortion×3.95 + 7)`; salida `color × pow(opacity, 0.25) + opacity` (glow + core aditivo) |
| 14 | **Glow por blending aditivo** — NVIDIA GPU Gems cap. 21 | Glow suave **encima** de la escena con **alpha blending aditivo**; fuentes brillantes difuminadas (downsample+blur) y reinyectadas; evita clumping |
| 15 | **Strip de vértices con normales** — drilian | Por cada punto del rayo, **2 vértices desplazados ±ancho a lo largo de la normal** (normal = perpendicular a la **media de las direcciones de los 2 segmentos adyacentes**) → ribbon continuo **sin puntos brillantes en las juntas**; alternativa offscreen: **max blending** (`D3DBLENDOP_MAX`) |
| 16 | **Textura láser en 3 frames** — vanilla `DelegateMethods.LightningLaserDraw` (usada por `Utils.DrawLaser`) | Textura de **21 px** de ancho: frame inicio `(0,0,21,8)`, medio `(0,8,21,6)`, fin `(0,14,21,8)`; `color = c_1 × f_1`; origen en el centro superior |
| 17 | **Aura de rayo vanilla** — `AI_137_LightningAura` (sentry DD2) | Zap cada **30 ticks (2 Hz)**; frame de animación cada **8 ticks (7.5 Hz)**; altura mínima **4 tiles** (T1) / **8** (T3), máxima **10** (T1) / **14** (T3); ancho tope 999; sonido `DD2_LightningAuraZap` al conectar; `knockBack = 0` |
| 18 | **Debuffs eléctricos vanilla** | `Electrified` (Buff ID 144): **4 HP/s quieto** (1 daño cada 0.25 s) vs **~16 HP/s moviéndose en horizontal** — castigo por moverse; Calamity escala a 16/60 HP/s; en 1.4.5 los rayos de tormenta hacen 200/300/400 (Clásico/Experto/Maestro, nerfeados en el parche) |

---

## 2. LAS 5 TÉCNICAS GANADORAS PARA StormLib (priorizadas)

### 🥇 T1 — Subdivisión fractal midpoint-displacement (núcleo de generación)
**Por qué:** el jitter uniforme de `ZigPath` da ruido mono-escala; el rayo real es fractal (grandes lazadas + micro-detalle). Todos los clásicos (drilian, DigitalRuby, Habrador) usan lo mismo y es baratísimo.

**Método nuevo recomendado:**
```csharp
public static Vector2[] FractalPath(Vector2 start, Vector2 end, int seed, int flick,
    int generations = 5, float chaos = 0.15f)
```
**Parámetros exactos:**
- `generations = 5` (→ 32 segmentos; rango útil 4–6; hoy StormLib usa 12+1 puntos)
- offset inicial `= len × chaos` con **`chaos = 0.15`** (DigitalRuby); permitir hasta 0.25–0.30 coherente con el tope actual `amp ≤ len×0.30`
- cada generación: `offsetAmount *= 0.5f`
- punto medio = `(a+b)/2 + perpendicular × Hash01±offset` (uniforme en `[−offset, +offset]`)
- **anclajes exactos** en 0 y N (como ya hace ZigPath)
- Combina perfecto con `Boil` (jitter temporal encima) y `DeathGrow`.

### 🥈 T2 — Forks integrados en la subdivisión, con herencia de anchura/brillo
**Por qué:** el `ForkTree` actual genera ramas con paseo propio, pero en la naturaleza las ramas nacen *durante* la subdivisión y heredan el detalle fino de las generaciones siguientes.

**Cambios:**
- Al partir cada segmento, con probabilidad **`forkChance = 0.25–0.35` por segmento** (drilian usa generaciones alternas; Habrador 0.8 es para estética de arma), crear rama: `splitEnd = midPoint + Rotate(dir, ángulo) × len × 0.6` (Habrador) o **×0.7** (drilian)
- **Ángulo:** `Random(-30°, 30°) ± 20°` → total **20°–50° (0.35–0.87 rad)** — coincide con el spread actual 0.45–0.90 rad ✓
- **Ancho rama ×0.5** del tronco en ese punto (Habrador: 0.3 vs 0.8) y **alpha ×0.4** — "solo el tronco conecta con el objetivo, solo él va a brillo completo" (drilian)
- Tope de ramas por rayo: **3–5** (presupuesto de quads); profundidad de subdivisión de rama: **máx 2 generaciones menos** que el tronco
- Método nuevo: `ForkTree(Vector2[] trunk, ..., bool fromFractal)` o parámetro `forkChance` en `FractalPath`.

### 🥉 T3 — Animación a doble rayo entrelazado (regeneración + fade, no parpadeo binario)
**Por qué:** `FlickTick(hz=15) + IsLit(0.62)` apaga el rayo entero al azar; el estándar de la industria es **dos rayos desfasados que se relevan**, siempre hay uno visible → sensación de corriente continua sin "blink".

**Parámetros exactos (drilian, a 60 TPS):**
- Bolt A y Bolt B con seeds distintos; **regeneración (nuevo path FractalPath) cada 10 ticks = 6 Hz**, alternando A/B (desfase de medio ciclo)
- Cada bolt vive **20 ticks (1/3 s)**: brillo **100% → 50%** lineal los primeros 10 ticks tras ser relevado
- **Boil ±3 px/tick en todos los puntos** (ya existe) y **±6 px en los puntos de subdivisión** (jitter doble en midpoints — "hierve más donde se partió")
- DigitalRuby/vanilla confirman 20 Hz como techo: si se quiere más nervio, alternar frame de textura cada **3 ticks (20 Hz)** como `AI_176_EdgyLightning`
- Método nuevo: `Bolt(..., bool dual)` o `StormArc(start, end, seed)` que pinte A+B con fases `tick/10` y `tick/10 + 5`.

### 🏅 T4 — Pintado en 3 capas glow/mid/core con strip de normales (matar los "puntos" en las juntas)
**Por qué:** Calamity usa 2–3 pasadas de escala/color; drilian documenta el defecto de dibujar quads por segmento con textura glow: **los solapes en las articulaciones se ven como puntitos brillantes**. El ribbon continuo (triangle strip) con normal media lo elimina.

**Parámetros exactos:**
- Capas (receta eléctrica azul, extrapolando Calamity rojo/cian):
  - **Glow:** escala ×1.0, color **#1E50A8**, alpha **0.30–0.35**
  - **Mid:** escala ×0.5, color **#5EB3FF**, alpha **0.55**
  - **Core:** escala ×0.22, color **#FFFFFF**, alpha **0.90**
  - (Calamity Red usa 0.6/0.4/0.2 con #DB683A/#FF7E38/#FF8080 ×0.5; SystemBane usa 2 capas 0.5/0.3 con Cyan×0.35 + White×0.75)
- **Normal por vértice:** perpendicular a `(dir[i-1→i] + dir[i→i+1]) / 2`; 2 vértices por punto a ±ancho/2 → ribbon; o reutilizar `Utils.DrawLaser` + `DelegateMethods.f_1/c_1` con textura 21 px (frames 8/6/8) como vanilla
- Ancho **decrece hacia la punta** de las ramas (×0.5 en fork) y con `DeathGrow`
- Luz dinámica: `AddLightAlong(..., 0.4f, 0.65f, 1.0f)` ×1.5–3 (Calamity usa color×3 en partículas)

### 🏅 T5 — Partículas de impacto con shake decaído + squish + doble pasada
**Por qué:** para `ImpactFlash`/`ArcRing`/`EndCap`; es la receta de Calamity `ThunderBoltVFX`/`BoltParticle`, muy barata y muy vistosa.

**Parámetros exactos:**
- `Shake = Vector2.One.RotatedByRandom(2π) × (1 − lifeT) × power` — **el temblor decae lineal a 0** (al revés de un oscilador constante)
- Fade: `Opacity ×= (1 − 0.05 × clamp((t−10)/10, 0, 1))` ≈ **×0.95/tick tras el tick 10**
- **`Squish.X ×= fadeFactor`** → la partícula se adelgaza hasta un hilo antes de morir
- **Doble pasada:** glow `Color × Opacity × 0.6` + core `Lerp(White, Color, lifeT) × Opacity`
- **3 variantes de sprite** elegidas al azar + flip según `GlobalTimeWrappedHourly % 30 < 15`
- Stretch inicial **(0.5, 1.6)**; luz `Color.ToVector3() × 3`
- Método nuevo: `SparkBurst(pos, color, size, count)` en StormLib reutilizando `ImpactFlash`.

**Bonus (si algún día hay shader):** HeavenlyGaleLightningShader (distorsión por noise-texture a 1.81 u/s en sentidos opuestos, warp 0.15, exponente `distortion×3.95+7`, salida `color×op^0.25 + op`) — mientras tanto se puede aproximar modulando ancho/alpha por `VFXCore.Hash01` a lo largo del path en 2 bandas.

---

## 3. CITAS DE CÓDIGO REAL (con URL)

**A. drilian — pseudocódigo canónico (2009):**
> ```
> segmentList.Add(new Segment(startPoint, endPoint));
> offsetAmount = maximumOffset;
> for each generation (some number of generations)
>     for each segment ...:
>         midPoint = Average(startpoint, endPoint);
>         midPoint += Perpendicular(Normalize(endPoint-startPoint))*RandomFloat(-offsetAmount,offsetAmount);
>         segmentList.Add(new Segment(startPoint, midPoint));
>         segmentList.Add(new Segment(midPoint, endPoint));
>     offsetAmount /= 2; // Each subsequent generation offsets at max half as much
> ```
> Fork: `splitEnd = Rotate(direction, randomSmallAngle)*lengthScale + midPoint; // 0.7 is a good value` · Animación: "every 1/3rd of a second, one of the bolts expires, but each bolt's cycle is 1/6th of a second off" (Frame 0/10/20/30 a 60 fps) · Render: "create a vertex strip ... moving each of them along the 2D vertex normals".
> URL: https://drilian.com/posts/2009.02.25-lightning-bolts/ (artículo original; el link viejo `/2009/02/25/lightning-hilarity...` ahora redirige aquí; cita textual obtenida 2026-09-16).

**B. Digital Ruby (Unity), `LightningBoltScript.cs`:**
> ```csharp
> [Range(0, 8)] public int Generations = 6;
> [Range(0.01f, 1.0f)] public float Duration = 0.05f;
> [Range(0.0f, 1.0f)] public float ChaosFactor = 0.15f;
> ...
> if (offsetAmount <= 0.0f) offsetAmount = (end - start).magnitude * ChaosFactor;
> while (generation-- > 0) { ... midPoint += randomVector; ... offsetAmount *= 0.5f; }
> ```
> URL (copia local, c) 2016 Digital Ruby, LLC): `research/v633/src/gamedev/LightningBoltScript.cs` — repo original: https://github.com/DigitalRuby/LightningBolt

**C. Habrador (Unity, "cool lightning from drilian"):**
> ```csharp
> int generations = 4; float maximumOffset = 60f; float splitProbability = 0.8f;
> float width = 0.8f; float smallWidth = 0.3f;
> ...
> if (Random.Range(0f, 1f) < splitProb) {
>     float angle = Random.Range(-30f, 30f);
>     if (angle < 0f) angle -= 20f; else angle += 20f;
>     Vector3 newEndPoint = RotatePointAroundPivot(endPoint, midPoint, new Vector3(angle, 0f, 0f));
>     ... newEndPoint = midPoint + dir * (newEndPoint - midPoint).magnitude * 0.6f;
> ```
> Copia local: `research/v633/src/gamedev/Habrador_Lightning.cs` (header cita `http://drilian.com/2009/02/25/lightning-bolts/`).

**D. Calamity Mod — RedLightning.cs (Storm Weaver), 3 capas:**
> ```csharp
> for (int i = 0; i < 3; i++) {
>     if (i == 0)      { lightningScale = new Vector2(Projectile.scale) * 0.6f; DelegateMethods.c_1 = new Color(219, 104, 58, 0) * 0.5f; }
>     else if (i == 1) { lightningScale = new Vector2(Projectile.scale) * 0.4f; DelegateMethods.c_1 = new Color(255, 126, 56, 0) * 0.5f; }
>     else             { lightningScale = new Vector2(Projectile.scale) * 0.2f; DelegateMethods.c_1 = new Color(255, 128, 128, 0) * 0.5f; }
>     ... Utils.DrawLaser(Main.spriteBatch, tex3, start, end2, lightningScale,
>         new Utils.LaserLineFraming(DelegateMethods.LightningLaserDraw));
> ```
> URL: https://raw.githubusercontent.com/CalamityTeam/CalamityModPublic/master/Projectiles/Boss/RedLightning.cs (verificado: descargado y leído íntegro; `extraUpdates=20`, trail 20, drift ±40 px).

**E. Calamity Mod — SystemBaneLightning.cs, 2 capas + homing:**
> ```csharp
> public const float OuterLightningScale = 0.5f;  public const float InnerLightningScale = 0.3f;
> public const float OuterLightningOpacity = 0.35f; public const float InnerLightningOpacity = 0.75f;
> public static readonly Color OuterLightningColor = Color.Cyan; public static readonly Color InnerLightningColor = Color.White;
> ...
> float updatedVelocityDirection = Projectile.velocity.ToRotation().AngleTowards(Projectile.AngleTo(target.Center), 0.25f);
> if (Main.rand.NextBool(5)) Projectile.velocity = Projectile.velocity.RotatedByRandom(0.9f);
> if (Main.rand.NextBool(10)) { Projectile.velocity = Projectile.velocity.RotatedByRandom(0.52f); }
> ```
> URL: https://raw.githubusercontent.com/CalamityTeam/CalamityModPublic/master/Projectiles/DraedonsArsenal/SystemBaneLightning.cs

**F. Calamity Mod — ThunderBoltVFX.cs (partícula con shake):**
> ```csharp
> Vector2 Shake = Vector2.One.RotatedByRandom(MathHelper.TwoPi) * (1 - (Time / (float)Lifetime)) * ShakePower;
> Color drawColor = Color.Lerp(Color.White, Color, (Time / (float)Lifetime));
> spriteBatch.Draw(tex, Position + Shake - Main.screenPosition, null, Color * Opacity * 0.6f, ...);
> spriteBatch.Draw(tex, Position - Main.screenPosition, null, drawColor * Opacity, ...);
> // Update: float fadeFactor = 1f - 0.05f * MathHelper.Clamp((Time - 10) / 10f, 0f, 1f); Opacity *= fadeFactor; Squish.X *= fadeFactor;
> ```
> URL: https://raw.githubusercontent.com/CalamityTeam/CalamityModPublic/master/Particles/ThunderBoltVFX.cs

**G. Calamity Mod — HeavenlyGaleLightningShader.fx (pixel shader del rayo):**
> ```hlsl
> float distortion = lerp(-1, 1, tex2D(uImage1, coords + float2(0, uTime * sign(coords.y > 0.5 ? -1 : 1) * 1.81)).r);
> float opacity = pow(sin((coords.y + distortion * 0.15) * 3.141), distortion * 3.95 + 7);
> return color * pow(opacity, 0.25) + opacity;
> ```
> URL: https://raw.githubusercontent.com/CalamityTeam/CalamityModPublic/master/Effects/HeavenlyGaleLightningShader.fx

**H. Vanilla Terraria (decompilado) — Projectile.cs, proyectil 466 "Lightning Orb Arc":**
> ```csharp
> UnifiedRandom unifiedRandom = new UnifiedRandom((int)this.ai[1]);
> ...
> int num779 = unifiedRandom.Next(); this.ai[1] = num779; num779 %= 100;
> float f = (float)num779 / 100f * (MathF.PI * 2f);
> Vector2 vector92 = f.ToRotationVector2();
> if (vector92.Y > 0f) vector92.Y *= -1f;
> if (vector92.Y > -0.02f) flag34 = true;
> if (vector92.X * (float)(extraUpdates + 1) * 2f * num777 + localAI[0] > 40f) flag34 = true;  // deriva ±40 px
> ```
> Copia local: `research/v633/src/vanilla/Projectile.cs` (líneas ~28105+). ID 466 = "Lightning Orb Arc" (lista tshock: https://tshock.readme.io/docs/projectile-list). Mismo patrón en Fargo's Souls `CopperLightning.cs` (local: `research/v633/src/fargo/`) con `BuffID.Electrified, 120` y re-encadena a 1000 px.

**I. Vanilla Terraria — DelegateMethods.LightningLaserDraw (framing de textura láser/rayo):**
> ```csharp
> color = c_1 * f_1;
> case 0: frame = new Rectangle(0, 0, 21, 8); ...      // inicio
> case 1: frame = new Rectangle(0, 8, 21, 6); ...      // medio
> case 2: distCovered = 8f; frame = new Rectangle(0, 14, 21, 8); // fin
> ```
> Copia local: `research/v633/src/vanilla/DelegateMethods.cs` (línea 829+). Es LA vía vanilla para dibujar tramos de rayo con textura (`Utils.DrawLaser`).

**J. Vanilla Terraria — AI_176_EdgyLightning (zap del Thunder Zapper) y AI_137_LightningAura (sentry DD2):** homing 400 px a 10 px/tick con frame aleatorio cada 3 ticks; aura con zap cada 30 ticks y animación cada 8 ticks, altura 4–10 tiles (T1) / 8–14 (T3). Copia local: `research/v633/src/vanilla/Projectile.cs` (líneas 36924+ y 57955+).

**Fuentes secundarias (snippets, páginas no accesibles por Cloudflare):** gamedev.stackexchange.com/questions/71397 "How can I generate a lightning bolt effect?" (referencia clásica; cuerpo bloqueado a curl) · NVIDIA GPU Gems cap. 21 "Real-Time Glow" (https://developer.nvidia.com/gpugems/gpugems/chapter-21-real-time-glow — glow aditivo sobre la escena) · wiki Terraria (Electrified 4→16 HP/s moviéndose; Thunder Zapper use time 17; 1.4.5 rayos de tormenta 200/300/400; wiki.gg/fandom devuelven 403 a curl — datos tomados de snippets de búsqueda).

---

## 4. Notas de integración y riesgos

1. **Presupuesto de quads:** 5 generaciones (32 tramos) × 3 capas × 2 bolts duales = ~192 draws/rayo; con `MultiBolt` vigilar el tope de 24000 quads/frame de VFXCore v2 — bajar a 4 generaciones (16 tramos) si hace falta.
2. **Determinismo:** mantener `VFXCore.Hash01(seed, flick, ...)` como RNG (regla del proyecto: nada de `Main.rand` en visual) — el midpoint displacement solo necesita 1 hash por punto medio.
3. **MP:** el path se deriva de (start, end, seed, flick) como hoy — sin estado extra; el dual-bolt añade solo la fase `tick/10`, también derivable.
4. **Regresión visual:** el zigzag actual (`ZigPath` + `Boil`) queda como fallback/estilo "serpenteo"; `FractalPath` es aditivo, no rompe API.
5. Los archivos en `research/v633/src/` son **solo referencia** (lección v6.32: nada de .cs de investigación dentro de la carpeta del mod).

*Informe generado por el agente de investigación Task 54. Búsquedas: 11 · páginas/código leído: drilian (íntegro), Calamity ×5 archivos (íntegros), vanilla decompilado ×3 secciones, más material local reutilizado de research/v633/src (gamedev, fargo, spirit, starlight, coralite, cow).*

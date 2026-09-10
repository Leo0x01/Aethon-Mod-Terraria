# Black Hole / Singularity Visual Effects in tModLoader

Research summary based on actual source code from:
- **CalamityTeam/CalamityModPublic** (Calamity Mod)
- **TheFifthCircle/WrathOfTheGodsPublic** (Wrath of the Gods - the gold standard for black holes in Terraria)
- **SnowyStarfall/ParticleLibrary** (Particle system library)

All `.fx` shader files and `.cs` code examples are in this folder. The
Wrath of the Gods (WoTG) Nameless Deity boss's black hole is by far the
most advanced example of a real-time raymarched black hole ever shipped
in a Terraria mod.

---

## TL;DR — Technique Map

| Effect you want | File to read | Technique |
|---|---|---|
| **Real gravitational lensing + accretion disk** | `RealBlackHoleShader.fx` + `NamelessBlackHoleRenderer.cs` | 75-step lightmarch in a pixel shader, samples `Main.screenTarget`, returns lensed background + glowing torus |
| **Cheap screen-space lensing (multi-source)** | `BlackHoleDistortionShader.fx` + `PetBlackHoleRenderer.cs` | Per-pixel rotational UV distortion around N black-hole positions, exponential falloff |
| **Spaghettification (pixels stretch TOWARD the hole)** | `AvatarRiftSpaghettificationShader.fx` | `lerp(coords, centerUV, -intensity * localIntensity)` — negative lerp pulls UVs outward, creating stretch |
| **Swirling dark spiral overlay** | `SuctionSpiralShader.fx` | Polar UV rotation + Perlin noise + subtractive blend |
| **Event horizon (dark center, no black circle)** | `BlackOnlyShader.fx` + `RealBlackHoleShader.fx` | `capturedLightInterpolant = smoothstep(0.01, -0.1, distEdge)` accumulates during lightmarch, then `result * (1 - capturedLightInterpolant)` blackens the absorbed region |
| **Chromatic aberration near the hole** | `ChromaticAberrationShader.fx` | R/G/B channels sampled at 3 different offset positions, offset inversely proportional to distance from impact point |
| **Generic screen distortion (any particle-driven)** | `ScreenDistortionShader.fx` + `ArbitraryScreenDistortionSystem.cs` | Distortion target where R=angle, G=magnitude, B=timespeed. Exclusion target masks out things you don't want distorted |
| **Polar-coord swirl edge** | `AvatarRiftShapeShader.fx` | Two counter-rotating noise samples in polar UV space; edge defined by erasure threshold |
| **Vortex / portal swirl (cheapest)** | `Calamity_ExoVortexShader.fx` | `swirlRotation = length(centeredCoords) * 17.2 - uTime * 6`, builds 2x2 rotation matrix per pixel |
| **Particle-driven infalling matter** | `CircularSuctionParticle.cs`, `ConvergingSupernovaEnergy.cs`, `Calamity_SealedSingularityRock.cs` | Spawn at random point on a circle, accelerate toward center, optionally rotate velocity toward centerline |
| **Stretched motion-blur particle (accretion streams)** | `Calamity_SquishyLightParticle.cs` | Particle texture is stretched along velocity direction by `(Scale - Scale*squish*0.3, Scale*squish)` |

---

## 1. REAL BLACK HOLE (the WoTG Nameless Deity approach)

This is the implementation you should clone for a high-end black hole.

### Architecture

1. **Invisible projectile** (`BlackHoleHostile.cs`) — only carries position,
   scale, lifetime. Its `PreDraw` draws a single `ChromaticBurst` texture
   that pulses and rotates as a "suck" overlay. The real visual happens
   elsewhere.
2. **Renderer system** (`NamelessBlackHoleRenderer.cs`) — hooks
   `On_TimeLogger.DetailedDrawTime` at index 36 (which fires after the
   main scene is rendered to `Main.screenTarget`). It allocates a
   `DownscaleOptimizedScreenTarget` at 0.385x screen resolution for
   performance, then runs the shader.
3. **Shader** (`RealBlackHoleShader.fx`) — a 75-iteration lightmarch
   that performs true gravitational lensing.

### The shader, line by line

```hlsl
// RealBlackHoleShader.fx  (paraphrased, see file for full version)

float4 PixelShaderFunction(...) : COLOR0
{
    // 1. Repack coords to -1..1 range, correct for aspect ratio
    coords = (coords - 0.5) * float2(aspectRatioCorrectionFactor, 1) + 0.5;
    coords = coords * 2 - 1;

    // 2. Where are we starting in 3D space?
    float3 samplePoint = float3(coords / zoom, -0.9);
    float3 stepDir     = float3(0, 0, 1);

    // 3. Rotate the whole space by the camera angle (Rodrigues' formula)
    samplePoint = RodriguesRotation(samplePoint - blackHoleCenter,
                                    cameraRotationAxis, cameraAngle) + blackHoleCenter;

    // 4. Tiny dither to hide banding from the 75-step march
    samplePoint += stepDir * Hash13(samplePoint * 10 + globalTime) * 0.0175;

    float4 result = 0;
    float2 distortionOffset = 0;
    float capturedLight = 0;

    // 5. THE LIGHTMARCH — 75 steps
    for (float i = 0; i < 75; i++)
    {
        float distFromCenter     = distance(samplePoint, blackHoleCenter);
        float distFromEdge       = distFromCenter - blackHoleRadius;

        // How much light has been absorbed on this step?
        capturedLight = smoothstep(0.01, -0.1, distFromEdge);

        // STEP FORWARD along the camera direction (unless absorbed)
        float step = lerp(0.02, 0.021, 1 - QuadraticBump(i / 75));
        samplePoint += stepDir * (1 - capturedLight) * step;

        // GRAVITATIONAL LENSING — bend the ray toward the black hole
        // (the 1/r^2 falloff is what creates real lensing)
        float bend = clamp(0.005 / pow(distFromCenter, 2), 0, 0.1) * blackHoleRadius;
        float3 distortion = normalize(blackHoleCenter - samplePoint) * bend;
        samplePoint += distortion;
        distortionOffset += distortion.xy;   // remember this for UV warping

        // ACCRETION DISK — sample the torus, additively accumulate
        result += Sample(samplePoint);
    }

    // 6. Outer photon-sphere glow
    float glow = smoothstep(5, 0, distFromCenter / blackHoleRadius);
    result += clamp(0.3 / pow(distFromCenter, 3) * float4(accretionDiskColor,1), 0, 2)
              * glow * lerp(1, float4(accretionDiskColor,1)*0.12, capturedLight);

    // 7. APPLY LENSING TO THE BACKGROUND SCREEN TEXTURE
    // Rotate the background UVs around the black hole position by an angle
    // proportional to accumulated distortion — this is what makes the
    // background appear to swirl.
    float angleOffset = length(distortionOffset) * 50 - globalTime * 6;
    float2 bhPos2D = (blackHoleCenter.xy + 1) * 0.5;
    float2 rotated = RotatedBy(baseCoords - bhPos2D, angleOffset) + bhPos2D;
    float2 finalUV = lerp(baseCoords, rotated,
                         smoothstep(0.125, 0.3, length(distortionOffset)))
                    + distortionOffset;

    // 8. Final composite: lensed background everywhere except where light was captured
    return tex2D(baseTexture, finalUV) * (1 - capturedLight) + result;
}
```

The accretion disk sample function uses a **torus distance field**:

```hlsl
float4 Sample(float3 position)
{
    float3 offset = (position - blackHoleCenter) / accretionDiskScale;
    float torusDist = -SignedTorusDistance(offset, float2(0.75, accretionDiskRadius));
    float glow = pow(max(0, torusDist / accretionDiskRadius), 0.9);

    // Polar UV on the torus for the noise (creates the swirling streaks)
    float2 radial = float2(atan2(offset.x, offset.z) / 6.283 + 0.5,
                            length(offset));
    glow *= tex2D(noiseTexture, radial * float2(3, 3.5)
                                 + globalTime * float2(6.3, -2));

    float4 col = float4(pow(saturate(accretionDiskColor), 1.1), 1) * glow * 0.75;

    // Black out the event horizon (this is what makes the center dark
    // WITHOUT drawing a black circle — anything inside blackHoleRadius
    // gets lerped to (0,0,0,1))
    return lerp(col, float4(0,0,0,1),
                smoothstep(0.01, 0, length(offset) - blackHoleRadius));
}
```

### Host-side setup (C#)

From `NamelessBlackHoleRenderer.cs`:

```csharp
BlackHoleTarget ??= new DownscaleOptimizedScreenTarget(0.385f, PrepareBlackHoleTargetAction);

Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                       SamplerState.PointClamp, DepthStencilState.None,
                       CullOnlyScreen, null, Matrix.Identity);
BlackHoleTarget.Render(Color.White, 0);
Main.spriteBatch.End();

// -- inside PrepareBlackHoleTargetAction --
Vector2 screenSize = ViewportSize;
Vector2 actualScreenSize = new(Main.screenWidth, Main.screenHeight);
Vector3 bhUV = new((bh.Center - Main.screenPosition) / actualScreenSize, 0f);
float aspect = screenSize.X / screenSize.Y;
float resizeScale = bh.width / actualScreenSize.X * bh.scale * 2f;
Vector2 zoom = Main.GameViewMatrix.Zoom * resizeScale;

// Align the shader's internal UV with the actual screen UV
bhUV = (bhUV - new Vector3(0.5f, 0.5f, 0)) * new Vector3(aspect, 1, 1) + new Vector3(0.5f,0.5f,0);
bhUV = bhUV * 2 - new Vector3(1, 1, 0);
bhUV /= new Vector3(zoom / Main.GameViewMatrix.Zoom, 1);

var s = ShaderManager.GetShader("NoxusBoss.RealBlackHoleShader");
s.TrySetParameter("blackHoleRadius", 0.3f);
s.TrySetParameter("blackHoleCenter", bhUV);
s.TrySetParameter("aspectRatioCorrectionFactor", aspect);
s.TrySetParameter("accretionDiskColor", new Color(245, 105, 61).ToVector3()); // orange
s.TrySetParameter("cameraAngle", 0.32f);
s.TrySetParameter("cameraRotationAxis", new Vector3(1f, 0f, bh.rotation));
s.TrySetParameter("accretionDiskScale", new Vector3(1f, 0.2f, 1f));   // squishes the disk
s.TrySetParameter("zoom", zoom);
s.TrySetParameter("accretionDiskRadius", bh.scale * 0.33f);
s.SetTexture(FireNoiseB, 1, SamplerState.LinearWrap);  // the noise for the disk
s.Apply();

// Draw the screen target THROUGH the shader. This is what causes the
// background to appear lensed.
Vector2 drawPos = screenSize * 0.5f;
Main.spriteBatch.Draw(Main.screenTarget, drawPos, null, Color.White, 0f,
                      Main.screenTarget.Size() * 0.5f,
                      screenSize / Main.screenTarget.Size(), 0, 0f);
```

**Critical implementation notes:**
- The shader reads `Main.screenTarget` as `baseTexture` (sampler s0). This
  means **the lensing effect distorts everything already drawn on screen**
  (tiles, NPCs, players, projectiles). The accretion disk and event
  horizon are composited ON TOP of the lensed background.
- The black hole position is computed in screen UV space, not world
  space — so the visual follows the projectile on screen even when the
  camera moves.
- The downscale to 0.385x is critical for performance: a 75-step
  lightmarch is expensive, and at full 1080p that's 16M pixels × 75
  iterations. At 0.385x you cut that to ~2.4M effective pixels.
- The `accretionDiskScale = (1, 0.2, 1)` squishes the torus on the Y
  axis — this is what makes the disk look like a thin ring viewed at an
  angle rather than a sphere.

---

## 2. CHEAPER SCREEN-SPACE LENSING (the pet black hole approach)

If a full lightmarch is too expensive (e.g. you want 5 black holes at
once), use `BlackHoleDistortionShader.fx`:

```hlsl
float CalculateGravitationalLensingAngle(float radius, float2 coords, float2 srcPos)
{
    float2 zoomedCoords = (coords - 0.5) * aspectRatioCorrectionFactor / zoom + 0.5;
    float2 correctedSrc = (srcPos - 0.5) * aspectRatioCorrectionFactor + 0.5;
    float dist = distance(zoomedCoords, correctedSrc);
    // Exponential decay — lensing falls off quickly with distance
    return distortionStrength * maxLensingAngle * exp(-dist / radius * 2);
}

float4 PixelShaderFunction(...)
{
    float2 distortedCoords = coords;
    for (int i = 0; i < 5; i++)           // up to 5 black holes
    {
        float angle = CalculateGravitationalLensingAngle(sourceRadii[i], coords, sourcePositions[i]);
        distortedCoords = RotatedBy(distortedCoords - 0.5, angle) + 0.5;
    }
    return tex2D(baseTexture, distortedCoords);
}
```

This is applied as a `ManagedScreenFilter` (a Terraria
`Terraria.Graphics.Effects.Filter`):

```csharp
ManagedScreenFilter f = ShaderManager.GetFilter("NoxusBoss.BlackHoleDistortionShader");
f.TrySetParameter("sourceRadii", blackHoleRadii);            // float[5]
f.TrySetParameter("sourcePositions", blackHolePoints);        // Vector2[5] in screen UV
f.TrySetParameter("distortionStrength", 1f);
f.TrySetParameter("aspectRatioCorrectionFactor", new Vector2(screenW/screenH, 1f));
f.TrySetParameter("maxLensingAngle", 28.3f);
f.TrySetParameter("zoom", Main.GameViewMatrix.Zoom);
f.SetTexture(PsychedelicWingTextureOffsetMap, 1, SamplerState.LinearWrap);
f.Activate();
```

The exponential `exp(-dist/radius*2)` decay is a good substitute for the
real `1/r^2` lensing curve and it's stable numerically.

---

## 3. SPAGHETTIFICATION (pixels stretch toward the hole)

From `AvatarRiftSpaghettificationShader.fx`:

```hlsl
float4 PixelShaderFunction(...)
{
    float dist = distance(position.xy, distortionPosition) / zoom;
    float localIntensity = smoothstep(distortionRadius, 0, dist);
    // Sharp nonlinear ramp so only pixels very close to the rift stretch hard
    localIntensity = exp(pow(localIntensity, 2)) - 1;

    // NEGATIVE lerp factor — pulls UV AWAY from the rift center,
    // which makes pixels APPEAR to be pulled TOWARD it (stretch effect)
    float2 distortedCoords = lerp(coords, distortionPosition / screenSize,
                                  -distortionIntensity * localIntensity);

    float4 baseColor = tex2D(screenTexture, coords);
    float4 riftColor = tex2D(noxusRiftTexture, coords);          // the rift's own render target
    float4 distortedColor = tex2D(screenTexture, distortedCoords);
    float4 distortedRift = tex2D(noxusRiftTexture, distortedCoords);

    // Where both the original and distorted pixels match the rift color,
    // fall back to the un-lensed baseColor — prevents infinite smearing
    // inside the rift itself.
    bool inRift = length(distortedColor - distortedRift) <= 0.02
               && length(baseColor - riftColor) <= 0.02;
    return lerp(distortedColor, baseColor, inRift);
}
```

**This is the technique to use if you want a "matter being pulled into
the hole" effect on the entire screen.** Combine with the RealBlackHole
shader for full effect.

---

## 4. EVENT HORIZON WITHOUT A BLACK CIRCLE

There are two layered techniques used in WoTG:

### Technique A: Accumulated light absorption in the lightmarch
Already covered above — `capturedLight = smoothstep(0.01, -0.1, distFromEdge)`
incrementally darkens pixels that fall within the event horizon. At the
end, the lensed background is multiplied by `(1 - capturedLight)` so it
goes black where the light was absorbed.

### Technique B: The `BlackOnlyShader` (used for the pet)
This is a separate post-pass applied to the black hole's own render
target. It uses the brightness of the source pixel as a mask:

```hlsl
float4 PixelShaderFunction(...)
{
    float4 color = tex2D(baseTexture, coords);
    float brightness = dot(color.rgb, 0.333);
    // Output pure black wherever the source was already dark (< 0.3 brightness)
    return float4(0, 0, 0, 1) * sampleColor * color.a
           * smoothstep(0.3, 0.1, brightness);
}
```

This means anything inside the event horizon (which is dark) gets
reinforced to pure black, with a soft alpha falloff. Applied as a
second draw call after the main black hole render — see
`PetBlackHoleRenderer.cs`.

```csharp
// First pass: the black hole shader renders accretion disk + lensing
Main.spriteBatch.Draw(data.Target, drawPosition, Color.White);

// Second pass: blacken anything dark (reinforces the event horizon)
ManagedShader blackShader = ShaderManager.GetShader("NoxusBoss.BlackOnlyShader");
blackShader.Apply();
Main.spriteBatch.Draw(data.Target, drawPosition, Color.White);

// Third pass: optionally re-apply an armor dye for color tinting
GameShaders.Armor.Apply(data.ShaderIndex, data.BlackHole, fakeDrawData);
```

---

## 5. CHROMATIC ABERRATION IN TERRARIA

From `ChromaticAberrationShader.fx`:

```hlsl
sampler baseTexture : register(s0);
float splitIntensity;
float2 impactPoint;

float4 PixelShaderFunction(...)
{
    float4 color = tex2D(baseTexture, coords);
    // Split strength is INVERSELY proportional to distance from impact —
    // strongest right at the black hole, fading out at the edges.
    float splitDistance = splitIntensity * 0.048 / (distance(coords, impactPoint) + 1);

    // 3 channels, 3 different offset directions (120 degrees apart)
    color.r = tex2D(baseTexture, coords + float2(-0.707, -0.707) * splitDistance).r;
    color.g = tex2D(baseTexture, coords + float2( 0.707, -0.707) * splitDistance).g;
    color.b = tex2D(baseTexture, coords + float2( 0,      1)     * splitDistance).b;
    return color;
}
```

**Activation pattern (one-line call from anywhere):**

```csharp
GeneralScreenEffectSystem.ChromaticAberration.Start(
    sourcePosition: NPC.Center,    // world-space origin of the effect
    intensityFactor: 1.5f,         // multiplies splitIntensity * 0.0004
    duration:        54            // frames
);
```

The `GeneralScreenEffectSystem` (`/Core/Graphics/GeneralScreenEffects/`)
manages the timer, intensity decay (`InverseLerp(Duration, 1, Timer) *
IntensityFactor`) and photosensitivity check. Copy this system verbatim
if you want a clean one-shot API for any screen filter.

---

## 6. SWIRLING / INFALLING PARTICLE MOTION

### Option A: The `CircularSuctionParticle` (WoTG, best quality)

```csharp
public override void Update()
{
    Vector2 target = FollowEntity.Center;

    // Particles shrink as they get close
    if (Position.WithinRange(target, 180f))
        Scale *= 0.84f;

    // 1. ACCELERATE TOWARD CENTER (gravity)
    // Time scales the pull so the longer a particle has been alive,
    // the harder it gets sucked in (mimics real infall acceleration)
    Velocity += Position.SafeDirectionTo(target) * (Time * 0.026f + 0.67f);

    // 2. ROTATE VELOCITY TOWARD CENTER-LINE
    // This is THE technique that turns radial infall into a spiral.
    // RotateTowards rotates the velocity vector toward the direction
    // pointing at the center, by at most 0.09 radians per frame.
    // The result: particles entering at an angle curve inward as they
    // fall, drawing a spiral.
    Velocity = Velocity.RotateTowards(Position.AngleTo(target), 0.09f);

    Opacity = InverseLerp(0f, 12f, Time).Squared();
    Rotation = Velocity.ToRotation();   // align texture with motion
}

public override void Draw(SpriteBatch spriteBatch)
{
    // STRETCH the particle along its velocity vector — this creates
    // motion-blur streaks that look like infalling plasma streams.
    Vector2 stretch = new Vector2(
        1f + Velocity.Length() * 0.04f,    // X grows with speed (motion blur)
        0.7f - Velocity.Length() * 0.01f); // Y shrinks slightly

    // Drawn additively with a strong bloom texture
    spriteBatch.Draw(BloomTexture, ..., Scale * stretch * 0.9f, ...);
    spriteBatch.Draw(Texture,      ..., Scale * stretch, ...);
}
```

**Spawn pattern** (from `CrushStarIntoQuasar`):

```csharp
// Spawn particles on a circle that rotates over time.
// The rotating spawn angle is what creates the pinwheel spiral pattern
// — each particle individually curves inward, but as a group they form
// a swirling vortex.
float angle = TwoPi * AITimer / 30f;
Vector2 spawnPos = plasmaSpawnCenter
                + angle.ToRotationVector2() * Main.rand.NextFloat(600f, 700f);

// Initial velocity points TOWARD the center
Vector2 velocity = (plasmaSpawnCenter - spawnPos).SafeNormalize(Vector2.UnitY)
                * plasmaShootSpeed;
```

### Option B: Calamity `SquishyLightParticle` (cheaper, more cartoonish)

Used by `EnormousConsumingVortex` to make the exo-vortex's swirly trails:

```csharp
public override void Update()
{
    // Speeds UP for the first 34% of life, then slows DOWN — gives a
    // "burst then settle" feeling.
    Velocity *= (LifetimeCompletion >= 0.34f) ? 0.93f : 1.02f;
    Opacity = (float)Math.Sin(LifetimeCompletion * MathHelper.Pi);
    Scale *= 0.95f;
}

public override void CustomDraw(SpriteBatch spriteBatch)
{
    // Stretch factor based on velocity (capped to MaxSquish)
    float squish = MathHelper.Clamp(Velocity.Length() / 10f * SquishStrenght,
                                    1f, MaxSquish);
    float rot = Velocity.ToRotation() + MathHelper.PiOver2;

    // X compresses, Y elongates — particle becomes a streak
    Vector2 scale = new Vector2(Scale - Scale * squish * 0.3f, Scale * squish);

    // 3 layered draws for glow + core
    spriteBatch.Draw(bloomTex, drawPosition, null, Color * Opacity * 0.8f,
                    rot, bloomTex.Size()/2f, scale * 2 * properBloomSize, ...);
    spriteBatch.Draw(tex, drawPosition, null, Color * Opacity * 0.8f,
                    rot, origin, scale * 1.1f, ...);
    spriteBatch.Draw(tex, drawPosition, null, Color.White * Opacity * 0.9f,
                    rot, origin, scale, ...);
}
```

### Option C: Calamity's `SealedSingularityRock` (chunk-style infalling debris)

```csharp
// Spawn at a random point on a circle around the hole
Vector2 spawnPos = bh.Center + new Vector2(380, 0).RotatedByRandom(MathHelper.TwoPi);

public override void AI()
{
    // Simple linear acceleration toward center (no orbital motion here —
    // the visual swirl comes from the spawn pattern being random per rock)
    Projectile.velocity += Projectile.DirectionTo(goal.Center) * 0.4f;
    Projectile.rotation += 0.1f;

    // Die when close enough
    if (Projectile.Distance(goal.Center) < 16) Projectile.Kill();
}
```

This is the simplest possible infalling-matter pattern. Good for chunky
debris, not great for plasma. Pair with dust:

```csharp
// Spawn purple dust that ALSO moves toward the hole
Vector2 v = new Vector2(380, 0).RotatedByRandom(MathHelper.TwoPi);
Dust.NewDustPerfect(bh.Center + v, DustID.Clentaminator_Purple, -v / 100f);
```

---

## 7. ACCRETION DISK WITHOUT A SHADER (texture + particle approach)

If you can't ship a `.fx` shader (e.g. for a smaller mod), Calamity's
`StratusBlackHole.cs` is the reference. It uses **layered BloomCircle
textures with the `OtherworldBarrierDistortion` shader** for a polar-coord
swirl effect:

```csharp
private static void DrawAura(StratusBlackHole mproj, Matrix matrix)
{
    Vector2 drawPosition = mproj.Projectile.Center - Main.screenPosition;

    // 1. Outer aura ring — uses the Neurons noise texture for distortion
    Main.spriteBatch.EnterShaderRegion(matrix: matrix);
    GameShaders.Misc["CalamityMod:OtherworldBarrierDistortion"].UseOpacity(1f);
    GameShaders.Misc["CalamityMod:OtherworldBarrierDistortion"].UseSaturation(0.2f);
    GameShaders.Misc["CalamityMod:OtherworldBarrierDistortion"]
        .SetShaderTexture(ModContent.Request<Texture2D>(
            "CalamityMod/ExtraTextures/GreyscaleGradients/MeltyNoiseHighContrast"), 1);
    GameShaders.Misc["CalamityMod:OtherworldBarrierDistortion"].Apply();

    var tex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
    Main.EntitySpriteDraw(tex, drawPosition, null,
        Color.SkyBlue * 0.5f * mproj.Projectile.Opacity, 0,
        tex.Size() / 2f,
        1200f * mproj.Projectile.Opacity / tex.Width, 0, 0);
}
```

The `OtherworldBarrierDistortion.fx` shader does the swirl in **polar
coordinates**:

```hlsl
float2 drift = float2(0.23 * uTime * uSaturation, uTime * uSaturation);
float2 offsetFromCenter = (coords - 0.5);

float angle = atan2(offsetFromCenter.y, offsetFromCenter.x) / (2 * 3.1415926);
float dist  = length(offsetFromCenter);
float2 polarUV = float2(angle, dist);

// Sample noise in POLAR coordinates — this is what makes it swirl.
float offset = tex2D(uImage1, polarUV + drift).x;

// Push the UV along the radial direction by the noise value
float2 modifiedCoords = coords + offsetFromCenter * offset * uOpacity;
return tex2D(uImage0, modifiedCoords) * sampleColor * mask;
```

**The key insight:** by sampling the noise texture in polar coordinates
`(angle, distance)`, and drifting the angle component at 0.23x the rate
of the distance component, the noise appears to spiral around the center.
The shader then uses the noise value as a radial offset, distorting the
underlying BloomCircle texture inward/outward — creating the appearance
of swirling plasma.

---

## 8. SWIRL SHADER (the simplest vortex)

`Calamity_ExoVortexShader.fx` — 8 lines of HLSL for a complete vortex:

```hlsl
float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 centered = coords - 0.5;
    // Rotation angle grows with distance from center, animates over time
    float swirlAngle = length(centered) * 17.2 - uTime * 6;
    float fade = 1 - saturate(length(centered) * 1.8);

    float s = sin(swirlAngle), c = sin(swirlAngle + 1.5707);
    float2x2 rot = float2x2(c, -s, s, c);
    float2 swirlCoords = mul(centered, rot) + 0.5;

    return tex2D(uImage0, swirlCoords) * float4(sampleColor.rgb, uOpacity) * fade;
}
```

Used by the `ExoVortex` and `EnormousConsumingVortex` projectiles — they
apply this shader to a `BlobbyNoise` texture and draw 5 offset copies in
different palette colors for the multi-hue vortex effect.

---

## 9. SCREEN DISTORTION VIA GENERIC TARGET (the do-anything system)

WoTG has an `ArbitraryScreenDistortionSystem` that lets any particle
queue a distortion contribution. The shader is dead simple:

```hlsl
// ScreenDistortionShader.fx
float4 PixelShaderFunction(...)
{
    float4 data = tex2D(distortionTexture, coords);
    // R channel = rotation angle (0..1 -> 0..2pi)
    float angle = data.r * 6.2831853 + globalTime * (1 - data.b) * 3;
    // G channel = magnitude (max 0.12 of screen)
    float2 offset = float2(cos(angle), sin(angle)) * data.g * 0.12;

    // Optional exclusion mask — areas where alpha=1 are NOT distorted
    offset *= smoothstep(1, 0, tex2D(exclusionTexture, coords + offset).a);
    offset *= smoothstep(1, 0, tex2D(exclusionTexture, coords).a);

    return tex2D(screenTexture, coords + offset);
}
```

Usage from a particle (see `LinearDistortionParticle.cs`):

```csharp
public override void Draw(SpriteBatch spriteBatch)
{
    ArbitraryScreenDistortionSystem.QueueDistortionAction(() =>
    {
        Texture2D tex = GennedAssets.Textures.Extra.RadialDistortion.Value;
        Vector2 drawPos = Position - Main.screenPosition;
        Color c = Color.White;
        c.G = (byte)(c.G * Opacity);     // green channel = magnitude
        Vector2 scale = Scale / tex.Size();

        Main.spriteBatch.Draw(tex, drawPos, null, c, Rotation,
            tex.Size() * 0.5f, scale, 0, 0f);
    });
}
```

The system maintains two render targets (`DistortionTarget` and
`DistortionExclusionTarget`). Anything drawn into them via
`QueueDistortionAction` / `QueueDistortionExclusionAction` during the
current frame becomes a distortion in the next pass.

**Why this matters for black holes:** you can have infalling particles
that ALSO distort the screen as they fall in, by queueing distortion
draws alongside their normal additive draws. See `CircularSuctionParticle`
for a worked example — it both draws itself visually AND queues an
exclusion mask (so it isn't doubly distorted by the screen filter).

---

## 10. THE COMPLETE STACK (what to actually build)

For a high-quality black hole, build this stack:

1. **Invisible projectile** carries position/scale/lifetime. Projectile
   `width` defines the lensing radius. No texture needed.
2. **Renderer system** hooks `On_TimeLogger.DetailedDrawTime` at index
   `36` (this is where screen-target post-processing happens in vanilla).
   Allocates a `DownscaleOptimizedScreenTarget` at 0.385x. Runs the
   `RealBlackHoleShader` against `Main.screenTarget`.
3. **Shader** does the 75-step lightmarch + torus accretion disk + UV
   lensing of the background.
4. **Particle spawner** (in projectile AI): every few frames spawn
   `CircularSuctionParticle`s on a rotating circle around the hole.
5. **Screen filter** for chromatic aberration, triggered by
   `GeneralScreenEffectSystem.ChromaticAberration.Start(center, 1.5f, 54)`
   when the hole spawns and every few seconds while alive.
6. **Optional `BlackOnlyShader` second pass** if you want the event
   horizon to be deep pure black rather than a darkened lensed image.
7. **LoopedSoundInstance** with a start sound + a loop sound for the
   "whirr" — see `BlackHoleHostile.cs`'s `BrrrrrSound`. Update volume
   based on contraction factor so it fades out as the hole dies.

---

## Files in this folder

### `shaders/`
| File | What it does |
|---|---|
| `RealBlackHoleShader.fx` | Full raymarched black hole (lensing + accretion disk + event horizon) |
| `BlackHoleDistortionShader.fx` | Cheap screen-space lensing for up to 5 holes |
| `AvatarRiftSpaghettificationShader.fx` | Stretch distortion toward a point |
| `SuctionSpiralShader.fx` | Swirling dark spiral overlay |
| `ScreenDistortionShader.fx` | Generic R/G/B distortion target |
| `BlackOnlyShader.fx` | Reinforce dark pixels to pure black (event horizon) |
| `AvatarRiftShapeShader.fx` | Polar-coord swirl portal edge |
| `ChromaticAberrationShader.fx` | RGB split based on distance from impact |
| `Calamity_ExoVortexShader.fx` | 8-line swirl shader |
| `Calamity_OtherworldBarrierDistortion.fx` | Polar-coord noise distortion |

### `code_examples/`
| File | What it does |
|---|---|
| `NamelessBlackHoleRenderer.cs` | Host-side render target + shader apply |
| `BlackHoleHostile.cs` | Invisible projectile, growing scale, suck overlay |
| `BlackHole.cs` | Static helper to draw a black hole at any position |
| `PetBlackHoleRenderer.cs` | Multi-instance render targets with dye support + screen-space lensing filter |
| `CircularSuctionParticle.cs` | The reference infalling particle (accel + rotate-toward-center + stretch) |
| `LinearDistortionParticle.cs` | Distortion-queue particle using RadialDistortion texture |
| `ArbitraryScreenDistortionSystem.cs` | Render target manager for the distortion system |
| `GeneralScreenEffectSystem.cs` | One-shot API for chromatic aberration / radial blur / contrast |
| `ConvergingSupernovaEnergy.cs` | Spiraling projectile (rotating spawn + exponential accel) |
| `Calamity_StratusBlackHole.cs` | Layered texture + polar distortion approach |
| `Calamity_SealedSingularityProjectile.cs` | The singularity bomb (3-state AI: throw → suck → detonate) |
| `Calamity_SealedSingularityRock.cs` | Simple chunk infalling debris |
| `Calamity_ExoVortex2.cs` | Multi-colored vortex with primitive trail |
| `Calamity_SquishyLightParticle.cs` | Stretched motion-blur particle |

---

## Recommended implementation order

1. Start with **Calamity's `StratusBlackHole`** approach — it ships in
   one file with a known-good shader. Get the polar-coord swirl working.
2. Add **`CircularSuctionParticle`**-style infalling particles. The
   visual upgrade is immediate and dramatic.
3. Add **`ChromaticAberration`** + **`RadialBlur`** via the
   `GeneralScreenEffectSystem` API (copy both files).
4. Replace the layered-texture approach with the **`RealBlackHoleShader`
   lightmarch** for the final boss version. Budget for ~3ms GPU time at
   1080p (which is why the 0.385x downscale matters).
5. Add **spaghettification** (`AvatarRiftSpaghettificationShader`) as
   a screen filter when the player is within X tiles of the hole.

## Known pitfalls

- The shader **must** read from `Main.screenTarget` (or an equivalent
  pre-rendered target) — if you apply it before the scene is drawn you
  get a black screen.
- `On_TimeLogger.DetailedDrawTime` with `detailedDrawType == 36` is the
  correct hook point. Other indices fire too early or too late. (If you
  don't want to depend on this hook, you can also use a
  `ManagedScreenFilter` activated each frame from `PostUpdateEverything`.)
- Render targets must be re-requested when the screen size changes. Use
  `InstancedRequestableTarget` or `ManagedRenderTarget` (both from
  Luminance) which handle this automatically.
- `Main.spriteBatch.Begin(..., SpriteSortMode.Immediate, ...)` is required
  to apply shaders to individual draws. The default `Deferred` mode
  batches draws and ignores shaders until `End()`.
- For multiplayer: the black hole **projectile** should be `netImportant`
  so all clients see it. The visual rendering is client-side only (in a
  `[Autoload(Side = ModSide.Client)]` system) — never trust visual state
  from the network.
- Watch out for `ProjectileID.Sets.DrawScreenCheckFluff` — set it to a
  large value (e.g. 750) or your projectile will stop rendering when off-
  screen, which kills the screen-space lensing effect.

## External references (still useful)

- tModLoader `[TUTORIAL] Shockwave effect` thread on
  `forums.terraria.org` (Jul 2019) — same underlying technique as
  `BlackHoleDistortionShader`, useful if you want a self-contained
  example without Luminance dependencies.
- `tModLoader Terraria.Graphics.Effects.Filter` docs at
  `docs.tmodloader.net` — the vanilla `Filter` / `ScreenShaderData`
  system that `ManagedScreenFilter` wraps.
- Reddit threads confirming players associate "black hole in background"
  visuals with the **Avatar of Emptiness / Nameless Deity** fight —
  these are the exact effects whose source is documented above.

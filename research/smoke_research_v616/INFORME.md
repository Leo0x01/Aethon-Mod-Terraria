# INFORME — Renderizado PROCEDURAL de humo, niebla y bruma en 2D
## Contexto: AethonMod (tModLoader 1.4.4, C#/MonoGame-FNA, SpriteBatch)

**Task ID:** 29 · **Agente:** general-purpose (investigación web)
**Fuentes principales leídas COMPLETAS** (guardadas en `/tmp/smoke_research/`):

| # | Fuente | Qué aporta |
|---|--------|-----------|
| 1 | **JangaFX — "Exploring and Modernizing The VFX Methods of Diablo 3"** (Nick Seavert, con aportes directos de Julian Love, GDC 2013) — `jangafx_diablo.html` | LA técnica canónica de humo de juego: ruido multiplicado en scroll, "Scale by Mids", reglas anti-fase, humo pseudo-volumétrico con gradiente pintado, rim lighting |
| 2 | **The Book of Shaders** cap. 11 (noise) y cap. 13 (fBm) — `bookofshaders11/13.html` | Fórmulas exactas de value noise (quintic) y fBm (octavas/lacunaridad/ganancia), turbulent/ridged |
| 3 | **Inigo Quilez** — artículos fBm, warping, morenoise — `iq_*.html` | Hurst H, auto-similitud, band-limiting para LOD, domain warping `f(p)=fbm(p+4·fbm(p))` (el look "humo vivo"), quintic `6w⁵-15w⁴+10w³` |
| 4 | **VFXDoc — Alpha Erosion** — `vfxdoc_erosion.html` | Erosión con step/smoothstep, rango de umbral `[-feather, 1+feather]`, importar LINEAL, dithered erosion |
| 5 | **VFXlabs — Fog of War** y **Scalable VFX** (Robin Wouters) — `vfxlabs_*.html` | Overdraw de niebla, triple textura (main+distort+dissolve), muestrear mip alta para blur, grosor constante al escalar |
| 6 | **realtimevfx.com** — hilo "Smoke VFX" — `rtvfx_smoke.json` | Confirmación de la técnica de capas de ruido en pan + máscara esférica |
| 7 | **Terraria VANILLA** (decompilado con ilspycmd desde tModLoader.dll v2026.07.3.0) — `Terraria_Dust_decompiled.cs`, `Terraria_Main_decompiled.cs` | Cómo dibuja y actualiza el humo el juego anfitrión (ver §E) |
| 8 | **Calamity Mod** (repo público LucilleKarma/CalamityMod) — `calamity_*.cs` | Sistema de partículas real de un mod de referencia: flipbooks PNG, 3 lotes de blending, curvas de escala/opacidad |
| 9 | **Starlight River** (repo público ProjectStarlight/StarlightRiver) — `starlight_particlesystem.cs` | El mod de tML más premiado por visuales: partículas por VERTEX BUFFER en GPU + 30 shaders |
| 10 | **ParticleLibrary** (SnowyStarfall) — `particlelibrary_readme.md` | Librería GPL alternativa al sistema de dusts (usada por Redemption, Shadows of Abaddon, Lunar Veil) |
| 11 | Código local de AethonMod (`VFXCore.cs`, `ParticleData.cs`, renderers) | Alineación exacta de las recetas con la infraestructura existente |

---

## A) FUNDAMENTOS — cómo se genera humo procedural 2D de calidad

La cadena universal que aparece en TODAS las fuentes (Diablo 3, IQ, Book of Shaders, Calamity, VFXDoc) es:

```
FORMA      = máscara radial/soft (dónde hay humo)
DETALLE    = fBm (value/Perlin noise sumado en octavas) × máscara
MOVIMIENTO = scroll/rotación/advención de las coordenadas del ruido (no del sprite)
PROFUNDIDAD= varias capas de puffs con parallax/escala/opacidad distintas
DESFASE    = aleatoriedad determinista POR PARTÍCULA (semilla) para que nunca se vea el loop
```

### La matemática del humo (fBm — Fractal Brownian Motion)

Un **value noise 2D**: valores aleatorios en los nodos de una rejilla, interpolados bilinealmente con la **quintic de Perlin** `u(t)=6t⁵−15t⁴+10t³` (continuidad C2: sin "arrugas" en la derivada; el smoothstep cúbico `3t²−2t³` basta para texturas difusas, IQ recomienda quintic).

El **fBm** suma octavas de ese noise:

```glsl
// Book of Shaders 13 / IQ
float fBm(vec2 p) {
    float v = 0.0, a = 0.5;      // amplitud inicial
    vec2  freq = vec2(1.0);      // frecuencia inicial
    for (int i = 0; i < OCTAVES; i++) {
        v += a * noise(p * freq);
        freq *= lacunarity;      // típicamente 2.0
        a   *= gain;             // típicamente 0.5 ("persistence")
    }
    return v;
}
```

- **Lacunaridad** = cuánto sube la frecuencia por octava (2.0 = cada octava tiene el doble de grumos).
- **Ganancia/persistencia** = cuánto baja la amplitud (0.5 = cada octava pesa la mitad). Relación con el exponente de Hurst H: ganancia = 0.5^H → H=0.5 (aspecto "normal"), H alto = suave/almohadillado, H bajo = rugoso (IQ: es el parámetro que controla la memoria del proceso).
- **El resultado es AUTO-SIMILAR**: una porción ampliada se parece al todo. Esta propiedad ES la invariancia de escala de la que hablamos en §B.
- IQ subraya: el fBm permite **band-limiting** (limitar por banda de frecuencia) → LOD barato: cortas octavas altas cuando el detalle cae bajo el píxel.
- Variantes: **turbulence** (`abs(noise)` → valles afilados, buen humo denso), **ridged** (`1−abs(n)` al cuadrado → crestas, buen humo con filamentos), **domain warping** (§C, el look "mágico").

### Movimiento: 3 niveles de sofisticación (todos viables en SpriteBatch)

1. **Scroll de UVs (Diablo 3)** — 2-3 capas de ruido desplazándose en direcciones distintas, MULTIPLICADAS: `A = (Tex1.A × Tex2.A) × Tex3.A × 2`. Con velocidades parecidas (clamp [0.1..1]) el ruido parece MORFAR en vez de deslizar. Reglas críticas del artículo de JangaFX:
   - UV scales en **potencias exactas de 2** (0.25/0.5/1/2) — con 0.8/1.0/1.2 hay "phasing" pulsante.
   - **"Scale by Mids"** (cita literal de Julian Love): *"The more you multiply, the more you want to constrain the range of your noise. Once you have a lot of black in your texture, it eats up a lot of action."* → re-centrar el ruido en gris medio (rango ±m alrededor de 0.5) antes de multiplicar.
   - NO multiplicar la MÁSCARA por 2 (solo los ruidos en pan); la máscara debe estar BLURREADA — si tiene detalle fino se ve estático cuando el ruido pasa por encima.
   - Posición y velocidad de scroll **aleatorias por partícula** (semilla) para evitar reconocimiento de patrón.
2. **Flipbook de fBm evolutivo (Calamity)** — N frames horneados donde el ruido cambia entre frame y frame (se anima como sprite sheet). Coste cero en runtime. `HeavySmoke` de Calamity: 7 variantes × 6 frames de 80×80.
3. **Advección por curl noise (nivel pro)** — el humo se mueve como un fluido sin divergencia. En 2D: campo de velocidad derivado de un potencial escalar ψ (tu fBm): `v = (∂ψ/∂y, −∂ψ/∂x)`. Garantiza remolinos que no se comprimen ni se estiran ("divergence-free"). Cada partícula consulta el campo con sus coordenadas → las vecinas se mueven coherentemente y aparecen VOLutas. Es lo que usa Unity VFX Graph ("Perlin Curl Noise") y es el truco estándar para movimiento "fluido" sin simulación de fluidos.

### Tabla de parámetros típicos (consenso de las fuentes)

| Parámetro | Rango típico | Valor por defecto | Notas |
|---|---|---|---|
| Octavas | 2–8 | **4–5** para puff; 2–3 para humo pequeño (<30px); 6–8 para niebla/monstruos gigantes | >6 sin ajustar lacunaridad no aporta (BoS) |
| Lacunaridad | 1.8–2.5 | **2.0** | >2 = detalle más "polvoriento"; <2 = más suave |
| Ganancia (persistencia) | 0.3–0.6 | **0.5** | 0.35 = grumoso/bloqueado (estilizado); 0.6 = turbio |
| Frecuencia base (celdas por sprite 128px) | 2–8 | **4** | 4 celdas = grumos de ~32px = escala de "bola de humo" |
| Warp de dominio | 0–6 | **2–4** | >6 se vuelve psicodélico |
| Velocidad de evolución del ruido | 0.05–0.3 UV/s | **0.1–0.15** | Debe ser lenta: el humo real evoluciona en segundos |
| Velocidad de scroll relativa entre capas | 0.5–2× | similar entre sí, clamp mínimo >0 | Velocidades MUY distintas rompen la ilusión de morphing (jangafx Fig. 8) |
| nº partículas de una columna de humo | 7 (D3 arcane orb) – 60 (D3 humo, 54 en réplica) | **8–24** en 2D | Pocos sprites GRANDES y blandos > muchos pequeños |
| Radio del puff vs. emisor | 1.5–4× | 2.5× | La columna debe solaparse: spacing ≤ 0.6·radio |
| Overlap entre puffs vecinos | 30–60% | 50% | Menos = "bolas"; más = masa uniforme sin textura |
| Emisión (partículas/s) | 4–16 | 8 | Con vida 1.5–4 s ⇒ 12–64 activas por columna |

---

## B) ESCALA INVARIANTE — que el mismo puff luzca a 10px y a 500px

Esto es lo más delicado y donde la mayoría de implementaciones fallan. Cinco técnicas concretas, todas compatibles con quads de SpriteBatch:

1. **Selección de octavas según tamaño en pantalla (band-limiting de IQ).**
   El ojo necesita que el detalle más fino mida ~2–3 px, ni más ni menos. Si el puff mide `R` px:
   `octavas = clamp(round(log2(R / 6)), 2, 7)` → a R=12px: 2 octavas (solo la forma); a R=384px: 6 octavas (grumos + micro-detalle). Con la misma semilla las octavas bajas SON LAS MISMAS → al crecer, el puff "gana detalle" en vez de estirarse. Esto es exactamente el LOD por mip de vfxlabs y el auto-similar del fBm de IQ.

2. **Densidad de sub-blobs proporcional al PERÍMETRO, no al área.**
   Lo que lee el ojo como "calidad de humo" es la densidad de IRREGULARIDADES EN EL BORDE. Manteniendo `n ≈ k·R` (k≈0.4–0.6 blobs por píxel de borde) el borde tiene la misma "frecuencia de mordiscos" a cualquier escala. vfxlabs resolvió el mismo problema (grosor de aro constante al escalar) re-generando la geometría; aquí el equivalente es re-componer los sub-blobs en vez de escalar el compuesto.

3. **Presupuesto de alfa por blob (¡el punto que casi todos olvidan!).**
   - Con **blending aditivo**: brillo total = Σ alfas = n·a. Si n ∝ R y a es fijo, un puff grande se vuelve 40× más brillante. FIX: `a = A_total / n` (presupuesto total constante, p. ej. A_total≈0.9).
   - Con **blending alfa**: cobertura total = `1−(1−a)^n`. FIX: `a = 1 − (1−A_total)^(1/n)`. Así un puff de 6 blobs y otro de 40 tienen la MISMA densidad óptica global.

4. **Texturas horneadas a densidad de texel fija.**
   Generar cada sub-blob de la MISMA resolución efectiva (p. ej. el gradiente radial siempre con caída de ~24px de ancho de borde, sea un blob de 40 o de 400px: generas la textura a tamaño ∝ blob o usas varias texturas LOD 64/128/256). Un SoftGlow escalado ×12 tiene un borde de 40px de "baba" — eso delata el truco. (Lección ya aprendida en AethonMod v6.11 con las cápsulas del Oblivion: el tamaño 2·sigma debía ser el TOTAL del quad.)

5. **Rotaciones y fases independientes de la escala.**
   La rotación debe ser en radianes/seg constante (NO proporcional al tamaño) y las fases de "respiración" por blob independientes (hash por índice). Un puff gigante que respira en coro sincronizado se ve como un pulmón, no como humo.

**Regla mental final:** *escalar el SEMILLA-espacio, no el sprite-espacio.* Un puff 10× más grande = mismos offsets relativos (×1.0), más octavas de detalle, más sub-blobs en el anillo, alfa por blob menor. La forma global permanece reconocible como "el mismo tipo de humo".

---

## C) COMPOSICIÓN DE PUFFS — blobs suaves + ruido

### Camino 1 (composición de sub-blobs, estilo actual del mod)

Un puff convincente = **1 núcleo + N blobs de borde**:
- **Núcleo**: un blob de radio 0.55·R en el centro, alfa moderado — da la MASA.
- **N blobs** (n ∝ R, §B.2) en un anillo de radios 0.35–0.85·R (distribución por hash determinista, NO uniforme en cuadrícula), radios 0.2–0.5·R, **rotación individual lenta** (el humo rueda: Terraria vanilla usa `rotation += velocity.X · 0.3`), **respiración desfasada** por blob (±10% del radio, ~0.5 Hz), y deriva lenta hacia afuera-arriba (el humo se expande al envejecer).
- Alfas: ver §B.3. Color: núcleo ligeramente más oscuro/brillante que el borde según receta (§F).
- **La estría de movimiento**: cuando el puff viaja, estirar los blobs del borde trasero a lo largo de −velocidad (factor 1+|v|·k, máx 2×) — ES la técnica de Vanilla para los dusts 130-134/219-223 (10 copias en `pos − vel·j`, escala ×(1−j/10)) y el "motion smear" de animación 2D (canmom). Da el aspecto "pintado con brocha".

### Camino 2 (textura runtime generada con fBm — recomendado para puffs grandes)

Hornear UNA VEZ (carga del mod, o caché por semilla) una textura 128×128:

```
alfa(u,v) = falloffRadial(d) × fBm(uv · 4, semilla)      con domain warp opcional
falloffRadial: 1 en d<0.55 → 0 en d=1 (smoothstep)  → plató interior + caída suave
```

Puntos de la fuente JangaFX/Diablo 3 aplicados al horneado:
- **Scale by Mids en el ruido**: `n' = 0.5 + (n−0.5)·m` con m≈1.6 antes de multiplicar por el falloff — evita que el negro del ruido se coma la forma.
- El falloff (máscara) **suave/blurreado**, el ruido el que tenga detalle.
- Para ANIMARLA sin shaders: hornear **6–8 frames** del fBm evolutivo (sumando el tiempo como desplazamiento del dominio del ruido, `uv·4 + t·dir`) → flipbook al estilo Calamity `HeavySmoke`, con `frame = (int)(t·fps) % frames` + variante por semilla. El morphing entre frames del fBm con offsets iguales a potencias de 2 de frecuencia NO produce saltos (regla anti-phasing).
- Guardar en RGB el valor PREMULTIPLICADO y A=255 (el truco OblivionBlob v6.11 del propio mod): el blending aditivo queda LINEAL y el falloff funciona; para componer con alfa usar `BlendState.NonPremultiplied` (One, InverseSourceAlpha) que es la matemática correcta para texturas premultiplicadas — con `AlphaBlend` clásica el alfa se aplicaría dos veces (rgb·a², el bug v6.11).

### Camino 3 (híbrido, el que recomiendo para la librería)

Puff = **textura fBm horneada** (forma orgánica) dibujada como QUAD + **3–8 sub-blobs SoftGlow** solo en el BORDE (donde se cruzan el anillo y la dirección de movimiento). Los blobs borran la sensación de "imagen repetida", la textura da el detalle interno, y el coste es 4–9 quads. Con la lente global de distorsión encima, la textura además se deforma sutilmente → humo "vivo" casi gratis.

---

## D) NIEBLA / BRUMA — capas, gradientes, deriva

Lo que hacen Dead Cells (hilo de Unity), Ori y el vfxlabs Fog of War se reduce al mismo patrón:

1. **3–5 CAPAS con parallax** (de atrás a delante):
   - capa lejana: puffs 2–3× el alto de pantalla, opacidad 4–8%, deriva lenta (0.1–0.3× viento), tinte frío (azul-gris), DIBUJADA PRIMERO.
   - capa media: puffs ~1 pantalla, opacidad 8–15%, deriva 0.5×.
   - capa cercana: puffs 1.5–2 pantallas, opacidad 12–20%, deriva 1× (viento real), tinte neutro, ocasionalmente DELANTE del jugador (con alfa 5–10% para no ensuciar).
   La clave del efecto: **cada capa se mueve a velocidad distinta** → paralaje = profundidad. En Terraria: multiplicar el desplazamiento por `(1 − depthFactor)` del movimiento de cámara del frame.

2. **Gradiente vertical de densidad.** La niebla real se acumula: bruma de valle = densa abajo (alfa ∝ smoothstep desde el suelo), techo de nubes = densa arriba. En quads: cada puff de la banda baja su alfa final × rampa vertical — o más barato: una banda inferior de puffs pequeños superpuestos y una superior de puffs enormes y casi invisibles.

3. **Deriva = seno + ruido** (nunca lineal pura):
   `x(t) = viento·t + A·sin(t·f1 + φ) + B·sin(t·f2 + φ·2.7)` con f2 ≈ 2.3·f1 (inconmensurables → nunca repite) y A ≈ 20–60px, B ≈ A·0.4. El segundo seno rompe el "vaivén de péndulo". Añadir a nivel de PUFF otro seno pequeño individual desfasado.

4. **Opacidad total acotada (overdraw).** El asesino de rendimiento de la niebla es el overdraw de partículas transparentes grandes (vfxlabs lo repite tres veces). En Terraria con SpriteBatch: mantener ≤ 12–16 quads de niebla por capa en pantalla, y presupuesto de alfa ACUMULADO de la banda ≤ 0.5–0.7 (más allá, el mundo detrás se emborrona y el gameplay se oculta). El artículo de vfxlabs lo resolvió con render-texture + mip alta (blur gratis); el equivalente aquí: dibujar la niebla a UNA resolución menor si hiciera falta (la lente global ya funciona así).

5. **El trío de texturas de vfxlabs** (main + distort + dissolve): para bruma estática de escenario; el "distort" ya lo da la lente global del mod; el "dissolve" = erosión de alfa (§G, `SmoothstepRange`) cuando la niebla se disipa al acercarse el jugador.

6. **Dioses/rayos de luz (opcional)**: 3–6 quads alargados muy tenues (SoftGlow estirado ×(0.15, 3)) con blending aditivo, orientados según la fuente de luz, con alfa × (1 − densidad de niebla alrededor) — barato y vende MUCHO la bruma.

---

## E) LO QUE HACEN LOS MODS DE TERRARIA (hechos verificados)

### Vanilla (decompilado desde tModLoader.dll 2026.07.3.0 — archivos adjuntos)

- **Dibujado** (`Main.DrawDust`): su propio `spriteBatch.Begin(Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, …, Transform)`; cada dust = 1 quad con rotación, origen el centro de la celda 8×8, color = `Lighting.GetColor(tile)` (¡el humo vanilla se TINTE por la iluminación de tiles: oscuro en cueva!) × `GetAlpha` = `(255−alpha)/255`. Si el color resulta negro, el dust MUERE (auto-fade en oscuridad).
- **Atlas 8×8 con 3 variantes** elegidas al azar en el spawn (`dust.frame` aleatorio de 3) — el "sprite sheet" más pequeño posible.
- **ESTRELLAS DE MOVIMIENTO horneadas**: los dusts 130–134 (fuego de cohete), 219–223, 226, 272 y 278 se dibujan como HASTA 10 COPIAS en `position − velocity·j` con `scale·(1−j/10)` — la técnica de smear de motion del juego anfitrión, pura y simple.
- **Comportamiento de humo genérico** (`Dust.UpdateDust`): `velocity *= (0.97, 0.99)`; el smoke crece `scale += 0.0025` (noGravity) o se encoge 0.01–0.04/tick; fade `alpha += 4..6` por tick hasta 255; rotación por `velocity.X · 0.3` (rueda al avanzar).
- **LOD real**: cuando el número de dusts supera 50/60/70/80/90% de `maxDustToDraw`, los umbrales de muerte de escala se disparan (0.001 → 0.02) — el sistema se autodegrada con carga.
- **Cap global** de dusts + `noLight`/`noGravity` como palancas de comportamiento.

### Calamity (repo público, archivos adjuntos)

- Sistema **GeneralParticleHandler** con **clases de partícula** (`Particle` base con `Update()`, `CustomDraw()`, flags `UseAdditiveBlend`, `UseHalfTransparency`, `SetLifetime`, `FrameVariants`, `Important`).
- **TRES lotes de blending por frame en orden fijo**: (1) AlphaBlend, (2) NonPremultiplied ("half transparency" — la clave para humo denso no emisivo), (3) Additive; cada uno con su `Begin/End` y restaurando el batch vanilla al final.
- **Humo = PNG flipbooks**: `HeavySmoke.png` = 7 variantes × 6 frames de animación de 80×80 (80px es su "blob" de referencia); `MediumSmoke/SmallSmoke/MiniSmoke/HeavySmoke/MediumMist` = escalera de tamaños pre-horneados (¡LOD por textura!, no por escala del mismo PNG).
- **Curvas típicas de su humo**: escala += 0.01/tick mientras vida < 20–84%, luego `scale *= 0.975`; `opacity *= 0.98`; `velocity *= 0.85` + flotabilidad `−0.08·UnitY`; spin ligado al SIGNO de `velocity.X`; lerp de 2 colores (colorStart→colorFade) con curva cuadrática `1−t²`; variante aleatoria al nacer.
- **Límite configurable de partículas** en su config de cliente + bypass `Important` para VFX críticos.
- Mist (`MediumMistParticle`): aditivo, LERP de color fuego→desvanecido, emite luz real (`Lighting.AddLight` ×0.1).

### Starlight River (repo público — el mod de tML referencia en visuales)

- **Partículas en GPU**: `ParticleSystem` con `DynamicVertexBuffer`/`DynamicIndexBuffer` de `VertexPositionColorTexture` + `BasicEffect`, actualización paralela con `FastParallel.For`, pool de objetos, 10.000 partículas máx, anclaje World/Screen/UI. NO usa SpriteBatch para sus partículas.
- Biblioteca de **~30 shaders .xnb** (Distort, Blur, Shockwave, Primitives, WaterEffect…) + `PrimitiveDrawing.cs` para cintas/trails con vértices custom.
- Es decir: el techo de calidad en tML = shaders + vértices; pero exige infraestructura propia.

### ParticleLibrary (SnowyStarfall, GPL)

- API de partículas "alternativa a Terraria's dust and particles", performante y customizable; la usan **Mod of Redemption, Shadows of Abaddon y Lunar Veil**. Librería de dependencia (modReferences), GPL-3 (¡atención a la licencia si se cogiera código!).

### Implicación para AethonMod

El mod YA está en el camino menos común y más potente: **100% código, cero PNGs de humo**. Las lecciones de los demás traducidas a su contexto:
- De Calamity → la **escalera de tamaños pre-horneados** (generar texturas fBm en 3–4 tamaños LOD, no escalar una sola) y el **lote NonPremultiplied** para humo denso.
- De Vanilla → el **tinte por iluminación del mundo** (`Lighting.GetColor`) para que el humo "viva" en cuevas oscuras, y el **smear por velocidad** para puffs rápidos.
- De D3 → el **morphing por semilla** y el control de MIDS.
- De Starlight River → ni un solo quad de más: todo a buffers (el mod ya lo hace con `_quads` en VFXCore).

---

## F) RECETAS DE COLOR

### El modelo pseudo-volumétrico de Diablo 3 (verificado en el artículo de JangaFX)

- **Humo = GRADIENTE pintado de brillante (lado iluminado) a oscuro (lado en sombra), enmascarado por el alfa del ruido**. La dirección del gradiente apunta a la fuente de luz (arriba en D3, sol fijo). El gradiente NO necesita sprite sheet de variantes porque la máscara de ruido aleatoria por partícula ya rompe el patrón.
- **Rotación aleatoria ligera** de los sprites (D3 lo hace ±, ~54 partículas) para que el gradiente no quede clavado — pero limitada, porque la sombra debe seguir caída "hacia abajo".
- **Rim/back lighting** cuando la luz está DETRÁS del humo: los bordes superiores se encienden (emisivo) — es el "edge light"/"kicker" de Gurney y el toque final del artículo de JangaFX.

### Recetas concretas (rampa núcleo→borde, en términos del mod)

| Receta | Núcleo | Borde | Blend | Notas |
|---|---|---|---|---|
| Humo frío (niebla) | RGB 150–180, alfa 0.5 | RGB 200–230, alfa 0.25 | **Alfa** | Borde ligeramente MÁS claro (más luz atraviesa lo fino). Tinte azul-gris (B+15 sobre R) |
| Humo denso (sombra) | RGB 25–45 | RGB 70–90 | **Alfa (NonPremult)** | Necesita rim light (0.3–0.5 del color de luz ambiente) para leerse sobre fondo oscuro; si no, silueta negra invisible |
| Humo iluminado por fuego | Base 60–80 + EMBERS: gradiente inferior naranja (255,140,50)→gris | gris frío arriba | **Alfa + destellos aditivos** | El 20% inferior del puff con tinte cálido decayendo (distancia al foco de luz²) |
| Humo mágico (Olvido-style) | Violeta profundo (70,20,110) | Dorado/violeta claro en borde | **Aditivo para venas + alfa para volumen** | El patrón del mod ya validado: VOLUMEN oscuro + TRAZOS claros |
| Vapor (jet corto) | Blanco 240, alfa 0.6 → 0.2 en 0.5s | — | **Alfa** | Vida corta, escala ×3 rápida, sin ruido apenas |
| Humo tóxico | Verde apagado (90,140,70) núcleo, (140,200,110) borde | | **Alfa** | + partículas mota pequeñas elevándose |
| Humo de peste/jardín (PlagueHumidifier) | | | | Calamity usa mist aditiva con lerp colorFire→colorFade |

### Las 5 leyes de color del humo en juego 2D

1. **Humo ALFA = oclusión** (oscurece), **humo ADITIVO = luz** (brilla). Un humo realista usa los DOS: masa alfa + borde/iras aditivas. Calamidad lo separa por lotes; D3 con "Blend-Add" (= premultiplicado: fondo negro tras lo emisivo — exactamente el BlackDisk+glow del mod).
2. **El humo se oscurece al engrosar** (más scattering-out): los blobs del NÚCLEO con luminancia menor que el borde → código: `lum = lerp(edge, core, densidadLocal)`.
3. **Nunca negro puro en masa**: sobre fondo oscuro desaparece; sobre claro se ve recortado. Mínimo ~25/255 + rim.
4. **Tinte por iluminación del mundo** (vanilla): multiplicar el color del humo por `Lighting.GetColor(tile)` con piso 0.15 — vende integración brutal en Terraria por 0 coste.
5. **El alfa manda sobre el RGB**: con el truco premultiplicado del mod (RGB pleno + alfa=opacidad, v6.12/v6.15: `Tint(c,f)` = rgb pleno + alfa=f) el brillo queda LINEAL — el `Color*f` cuadrático apaga (lección v6.15 del worklog).

---

## G) PSEUDOCÓDIGO C# — adaptado a quads de SpriteBatch

Alineado con `VFXCore` (buffer de `GlowQuad`, semántica de escala en píxeles finales, `Hash01` determinista). Todo determinista ⇒ seguro en multijugador sin sincronizar nada.

### G.1 — Ruido: hash → value noise → fBm → curl (clase única)

```csharp
/// <summary>Ruido 2D determinista para bruma: hash + value noise + fBm + curl.
/// Mismo contrato que VFXCore.Hash01: MISMA secuencia en todas las máquinas.</summary>
public static class SmokeNoise
{
    // ---- hash entero determinista [0,1) (idéntico espíritu a VFXCore.Hash01) ----
    public static float Hash(int x, int y, int seed)
    {
        int h = unchecked(seed * 374761393 + x * 668265263 + y * 1911520717);
        h = unchecked(h ^ (h >> 13));
        h = unchecked(h * 1274126177);
        h = unchecked(h ^ (h >> 16));
        return (h & 0xFFFFFF) / 16777216f;
    }

    // ---- quintic de Perlin: C2 continua, sin "arrugas" en la derivada ----
    static float Quintic(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

    // ---- value noise 2D: 4 esquinas hasheadas + interpolación quintic ----
    public static float Value(float x, float y, int seed)
    {
        int xi = (int)MathF.Floor(x), yi = (int)MathF.Floor(y);
        float tx = Quintic(x - xi), ty = Quintic(y - yi);
        float a = Hash(xi,     yi,     seed);
        float b = Hash(xi + 1, yi,     seed);
        float c = Hash(xi,     yi + 1, seed);
        float d = Hash(xi + 1, yi + 1, seed);
        float ab = a + (b - a) * tx;
        float cd = c + (d - c) * tx;
        return ab + (cd - ab) * ty;                      // [0,1)
    }

    /// <summary>fBm normalizado [0,1]. octaves = LOD (§B.1): 2-3 puffs pequeños,
    /// 4-5 puffs normales, 6-7 niebla gigante. lacunarity 2, gain 0.5 por defecto.</summary>
    public static float Fbm(float x, float y, int seed, int octaves = 5,
                            float lacunarity = 2f, float gain = 0.5f)
    {
        float sum = 0f, amp = 0.5f, fx = 1f, fy = 1f, norm = 0f;
        for (int i = 0; i < octaves; i++)
        {
            sum  += amp * Value(x * fx, y * fy, seed + i * 101);
            norm += amp;
            amp  *= gain;
            fx   *= lacunarity;                          // regla D3: potencias exactas
            fy   *= lacunarity;                          // de 2 ⇒ sin phasing
        }
        return sum / norm;                               // [0,1]
    }

    /// <summary>Octavas óptimas para un radio de pantalla dado (§B.1):
    /// detalle más fino ≈ 3 px SIEMPRE. log2(R/6): R=12→2, R=48→3, R=192→5.</summary>
    public static int OctavesForRadius(float radiusPx)
        => Math.Clamp((int)MathF.Round(MathF.Log2(MathF.Max(radiusPx, 8f) / 6f)), 2, 7);

    /// <summary>Domain warping (IQ) — el look "humo vivo": evalúa el fBm en un
    /// dominio distorsionado por OTRO fBm. warp 2-4.</summary>
    public static float WarpedFbm(float x, float y, int seed, float warp = 3f)
    {
        float qx = Fbm(x,        y,        seed);
        float qy = Fbm(x + 5.2f, y + 1.3f, seed);        // offsets "mágicos" de IQ:
        return Fbm(x + warp * qx, y + warp * qy,         // solo decorrelacionan llamadas
                   seed + 7);
    }

    /// <summary>Curl 2D: velocidad SIN DIVERGENCIA desde un potencial escalar ψ=fBm.
    /// v = (∂ψ/∂y, −∂ψ/∂x) → remolinos que no se comprimen (§A, nivel 3).</summary>
    public static Vector2 Curl(float x, float y, int seed, float eps = 0.15f)
    {
        float dy = Fbm(x, y + eps, seed) - Fbm(x, y - eps, seed);
        float dx = Fbm(x + eps, y, seed) - Fbm(x - eps, y, seed);
        return new Vector2(dy, -dx) * (0.5f / eps);
    }

    // ---- utilidades de forma ----
    public static float Smoothstep(float e0, float e1, float t)
    { t = Math.Clamp((t - e0) / (e1 - e0), 0f, 1f); return t * t * (3f - 2f * t); }

    /// <summary>Erosión de alfa (VFXDoc): disuelve el borde con plumas,
    /// el ruido decide QUÉ muere antes. erosion 0=solido, 1=desvanecido.</summary>
    public static float Erode(float alpha, float noise01, float erosion, float feather = 0.15f)
    {
        // rango completo: el threshold debe poder ir MÁS ALLÁ de [0,1] (VFXDoc)
        float lo = erosion * (1f + feather) - feather;
        float hi = lo + feather;
        return Smoothstep(lo, hi, alpha * (0.35f + 0.65f * noise01));
    }
}
```

### G.2 — Textura de puff 128×128 horneada con SetData (falloff radial × fBm)

```csharp
public static class PuffBakery
{
    // Caché por semilla+estilo: se hornea UNA vez (carga o primer uso).
    static readonly Dictionary<int, Texture2D> _cache = new();

    /// <summary>Puff 128×128: alfa = falloffRadial × fBm(warp). RGB PREMULTIPLICADO
    /// y A=alfa (el truco v6.11: aditivo lineal + NonPremultiplied correcto).
    /// frames>1 ⇒ flipbook fBm evolutivo (estilo Calamity HeavySmoke) — el
    /// desplazamiento temporal usa offsets iguales a la frecuencia (potencias
    /// de 2) para que el morphing sea continuo (regla anti-phasing D3).</summary>
    public static Texture2D Get(GraphicsDevice gd, int seed, int frames = 1, bool warped = true)
    {
        int key = seed * 31 + frames * 7 + (warped ? 1 : 0);
        if (_cache.TryGetValue(key, out var t)) return t;

        const int S = 128;
        var data = new Color[S * S * frames];
        Vector2 c = new(S * 0.5f, S * 0.5f);

        for (int f = 0; f < frames; f++)
        for (int j = 0; j < S; j++)
        for (int i = 0; i < S; i++)
        {
            Vector2 p = new(i + 0.5f, j + 0.5f);
            float d = Vector2.Distance(p, c) / (S * 0.5f);          // 0 centro → 1 borde

            // MÁSCARA: plató 55% + caída suave (la máscara SIEMPRE blandita — D3)
            float falloff = 1f - SmokeNoise.Smoothstep(0.55f, 1.0f, d);

            // RUIDO: 4 celdas base; el frame avanza el dominio con la misma
            // frecuencia ⇒ morph continuo; warp opcional para organicidad.
            float u = i * 4f / S, v = j * 4f / S;
            float dt = f * 0.25f;                                    // paso temporal
            float n = warped
                ? SmokeNoise.WarpedFbm(u + dt, v - dt * 0.5f, seed)
                : SmokeNoise.Fbm(u + dt, v - dt * 0.5f, seed, 5);

            // SCALE BY MIDS (Julian Love): re-centrar en 0.5 ANTES de multiplicar;
            // el negro no se come la acción.
            n = 0.5f + (n - 0.5f) * 1.6f;

            float a = Math.Clamp(falloff * n * 2f, 0f, 1f);          // ×2 solo al ruido
            byte A = (byte)(a * 255f);
            // Premultiplicado: RGB = nivel gris × alfa ⇒ aditivo LINEAL y
            // NonPremultiplied-blend correcto. El alfa real queda disponible
            // para usar AlphaBlend clásica si se quiere (rgb·a² suaviza doble,
            // de hecho útil en niebla).
            byte L = A;                                              // gris = alfa
            data[(f * S + j) * S + i] = new Color(L, L, L, A);
        }

        var tex = new Texture2D(gd, S, S * frames);
        tex.SetData(data);
        _cache[key] = tex;
        return tex;
    }

    /// <summary>SourceRect del frame f del flipbook.</summary>
    public static Rectangle Frame(Texture2D tex, int frame)
        => new(0, frame * tex.Width, tex.Width, tex.Width);
}
```

### G.3 — DrawSmokePuff: composición invariante de escala

```csharp
/// <summary>Un puff de humo/niebla compuesto por cuadros. TODO determinista:
/// misma semilla ⇒ mismo humo en todas las máquinas (multiplayer-safe).</summary>
public static class Mist
{
    /// <param name="center">Coords de MUNDO (VFXCore resta screenPosition al volcar).</param>
    /// <param name="radius">Radio DESEADO en px de pantalla.</param>
    /// <param name="color">Tinte (la A del Color se ignora; el presupuesto se calcula).</param>
    /// <param name="seed">Semilla determinista del puff.</param>
    /// <param name="time">Tiempo de vida del puff en segundos (crece, erosiona, morfea).</param>
    /// <param name="velocity">Velocidad actual (para la ESTRÍA de movimiento).</param>
    /// <param name="additive">True = humo luminoso; False = humo que ocluye (lote NonPremult).</param>
    public static void DrawSmokePuff(Vector2 center, float radius, Color color, int seed,
                                     float time, Vector2 velocity, bool additive = false)
    {
        // 0) textura horneada (caché por semilla; en producción: escalera LOD
        //    64/128/256 según OctavesForRadius(radius))
        Texture2D puff = PuffBakery.Get(Main.graphics.GraphicsDevice, seed & 1023, frames: 1);

        // 1) NÚCLEO — la masa (textura fBm); crece al envejecer; rueda lento;
        //    deriva orgánica: curl noise en coords de mundo (remolinos §A nivel 3)
        float grow = 1f + 0.35f * MathF.Min(time * 0.5f, 1f);
        float coreR = radius * 0.62f * grow;
        float rot = time * 0.12f * (seed % 2 == 0 ? 1f : -1f);
        Vector2 swirl = SmokeNoise.Curl(center.X * 0.004f, center.Y * 0.004f, seed) * 6f;

        // 2) BLOBS DE BORDE — n ∝ PERÍMETRO (§B.2); presupuesto de alfa
        //    de cobertura CONSTANTE (§B.3) para invariancia de escala
        int n = Math.Max(3, (int)(radius * 0.5f));
        float aTotal = 0.75f;
        float aBlob = 1f - MathF.Pow(1f - aTotal, 1f / (n + 1));
        byte A = (byte)(aBlob * 255f);
        var baseCol = new Color(color.R, color.G, color.B, A);

        VFXCore.Quad(center + swirl * 0.3f, baseCol, new Vector2(coreR * 2f, coreR * 2f), rot, puff);

        for (int i = 0; i < n; i++)
        {
            float h1 = VFXCore.Hash01(seed, i, 1);                  // ángulo
            float h2 = VFXCore.Hash01(seed, i, 2);                  // radio del anillo
            float h3 = VFXCore.Hash01(seed, i, 3);                  // tamaño
            float h4 = VFXCore.Hash01(seed, i, 4);                  // fase temporal

            float ang = h1 * MathHelper.TwoPi;
            float ring = (0.45f + 0.4f * h2) * radius * grow;
            Vector2 dir = new(MathF.Cos(ang), MathF.Sin(ang) * 0.85f);

            // respiración DESFASADA por blob (nunca en coro)
            float breathe = 1f + 0.12f * MathF.Sin(time * 0.8f + h4 * MathHelper.TwoPi);
            float blobR = radius * (0.28f + 0.22f * h3) * breathe;

            // ESTRÍA de movimiento (técnica vanilla 130-134): estirar los blobs
            // TRASEROS a lo largo de −velocidad; máx 2× (smear de animación 2D)
            float trail = Math.Clamp(velocity.Length() * 0.02f, 0f, 1f);
            float stretch = 1f + trail * (1f - MathF.Abs(MathF.Sin(ang))) * 1.1f;

            float brot = time * (0.2f + 0.25f * h1) * (h2 > 0.5f ? 1f : -1f);

            VFXCore.Quad(center + swirl * 0.3f + dir * ring, baseCol,
                         new Vector2(blobR * 2f * stretch, blobR * 2f), brot, puff);
        }
        // VOLCAR: VFXCore.FlushAdditive() si luminoso; para humo QUE OCLUYE,
        // un FlushNonPremultiplied() gemelo con BlendState.NonPremultiplied
        // (texturas ya premultiplicadas ⇒ composición física correcta).
    }
}
```

### G.4 — Banda de bruma (niebla de escenario en capas)

```csharp
/// <summary>Bruma de fondo en capas con paralaje + gradiente vertical + deriva.</summary>
public static void DrawMistBand(int seed, float time, float windPxPerSec)
{
    var screen = new Rectangle((int)Main.screenPosition.X, (int)Main.screenPosition.Y,
                               Main.screenWidth, Main.screenHeight);

    float[] alphas = { 0.05f, 0.10f, 0.16f };          // presupuesto por capa (§D.4)

    for (int layer = 0; layer < 3; layer++)            // 3 capas: lejos → cerca
    {
        float depth = 1f - layer / 3f;                 // 1=lejos … 0.33=cerca
        float alpha = alphas[layer];
        float size = screen.Height * (1.4f + layer * 0.5f);
        int count = 6;                                 // ≤6 quads/capa: overdraw a raya

        for (int k = 0; k < count; k++)
        {
            float h = VFXCore.Hash01(seed + layer, k, 9);
            // deriva = viento (parallax por capa) + 2 senos INCONMENSURABLES (§D.3)
            float t = time * (0.4f + 0.2f * layer);
            float x = screen.X + (k + 0.5f) * screen.Width / count
                    + windPxPerSec * time * (1f - depth)             // parallax
                    + 48f * MathF.Sin(t * 0.31f + h * 6.28f)
                    + 20f * MathF.Sin(t * 0.71f + h * 12.56f);
            // envolver en el ancho de pantalla (la niebla nunca termina)
            x = screen.X + ((x - screen.X) % screen.Width + screen.Width) % screen.Width;

            // gradiente vertical: densa ABAJO (bruma de valle, §D.2)
            float yFrac = 0.55f + 0.35f * h;
            float y = screen.Y + screen.Height * yFrac;
            float vFade = MathF.Pow(yFrac, 1.5f);      // más alto ⇒ más tenue

            byte A = (byte)(alpha * vFade * 255f);
            var col = layer == 2
                ? new Color(210, 220, 230, A)          // cerca = neutro
                : new Color(185, 205, 225, A);         // lejos = frío

            Mist.DrawSmokePuff(new Vector2(x, y), size * 0.5f, col,
                                seed + layer * 100 + k, time + h * 10f,
                                Vector2.UnitX * windPxPerSec * 0.2f, additive: false);
        }
    }
}
```

**Notas de integración con la infraestructura existente:**
- `VFXCore.Quad` ya cumple la semántica (escala = px finales, centro = origen, `Immediate` permite mezclar texturas) — el puff horneado se pasa como `Texture` del quad.
- Falta un volcado gemelo `FlushNonPremultiplied()` (Begin con `BlendState.NonPremultiplied, SamplerState.LinearClamp`, mismo contrato cerrado→abierto→cerrado a prueba de hooks de otros mods que ya usa el mod) para el humo QUE OCLUYE — el aditivo existente sirve para humo luminoso/venas.
- Tinte por iluminación del mundo (opcional, §F.4): `var light = Lighting.GetColor((int)(center.X/16f), (int)(center.Y/16f)); col = Color.Lerp(col, col.MultiplyRGBA(light), 0.8f)` con piso de luminancia 0.15.
- La erosión de alfa (G.1 `Erode`) se usa en la MUERTE del puff: en vez de fade uniforme, el alfa final se erosiona con el ruido (VFXDoc) ⇒ el humo "se deshace en volutas" en vez de desvanecerse como fantasma.

---

## H) LAS 10 DECISIONES DE DISEÑO MÁS IMPORTANTES (para la librería de bruma)

1. **Dos lotes de blending, no uno**: humo QUE OCLUYE (`NonPremultiplied` + texturas premultiplicadas horneadas) y humo LUMINOSO (aditivo, el existente). Calamity usa exactamente 3 lotes en orden fijo; D3 usa "Blend-Add". El humo creíble necesita masa Y luz.
2. **Texturas nacidas de código, escalera LOD**: hornear puffs fBm en 3–4 tamaños (64/128/256) × N semillas × 6–8 frames de flipbook, al estilo Calamity pero sin PNGs. NUNCA escalar una textura más allá de ~2× (el borde de 24px se vuelve baba — lección v6.11).
3. **Invariancia de escala por construcción** (§B): octavas según radio (`log2(R/6)`), blobs ∝ perímetro, presupuesto de alfa con `1−(1−A)^(1/n)`. Regla: *escalar el espacio de la semilla, no el sprite*.
4. **Movimiento = semilla + senos inconmensurables + curl opcional**: deriva con `sin(0.31t)+sin(0.71t)` (nunca un solo seno: péndulo), fases por hash por blob (nunca en coro), y `SmokeNoise.Curl` para las columnas protagonistas (remolinos sin divergencia = "fluido" sin simular fluidos).
5. **Morphing determinista anti-bucle** (reglas D3): offsets de scroll en potencias exactas de 2, velocidades similares con clamp mínimo >0, posición/velocidad aleatoria POR SEMILLA, "Scale by Mids" (`0.5+(n−0.5)·1.6`) antes de multiplicar ruido×máscara. Objetivo: "poder quedarse mirándolo como una hoguera" (Julian Love).
6. **La máscara blandita y el ruido con detalle**: el falloff radial siempre smooth/plató (si la máscara tiene detalle se ve ESTÁTICO cuando el ruido pasa); el detalle vive en el fBm; multiplicar ×2 SOLO al ruido, jamás a la máscara.
7. **Presupuesto de quads por puff: 4–9** (núcleo + borde) y por banda de niebla: ≤ 16–18 totales. Con el overdraw como asesino (vfxlabs×3), el presupuesto ES el diseño: puffs grandes y pocos, vida 1.5–4 s, emisión 4–16/s. Vanilla se autodegrada por conteo (LOD 50–90%) — copiar esa idea: si `QuadCount` supera umbral, acelerar la muerte de los puffs viejos.
8. **Muerte por EROSIÓN, no por fade**: `Erode()` con plumas (VFXDoc) — el humo se disuelve en grumos donde el ruido manda. Nacimiento inverso: crecer de 0.4→1.0·R en ~0.3 s (nunca aparecer de golpe ni a tamaño final).
9. **Color: gradiente iluminado→sombra + tinte por Lighting.GetColor** (§F): el humo pertenece al mundo — se oscurece en cuevas, se enciende junto al fuego (20% inferior cálido), rim cuando la luz está detrás. Nunca negro puro en masa (mín ~25 + rim). Emplear la lección v6.15: tinte con RGB pleno y el alfa aparte (lineal), no `Color·f`.
10. **Todo determinista por semilla** (`VFXCore.Hash01` ya existe): misma secuencia en cliente/multijugador, cero sincronización, cero RNG de red. La semilla se deriva del emisor (p. ej. `whoAmI·31+spawnIndex`) ⇒ la columna de humo del proyectil 41 es SIEMPRE la misma columna, y eso además la hace reconocible como "firma" del arma.

---

## Apéndice — hallazgos verificados de vanilla Terraria (decompilación)

```
DrawDust():    spriteBatch.Begin(Deferred, AlphaBlend, PointClamp, None, CullNone, null, Transform)
Genérico:      Draw(tex, pos−screen, frame, Lighting.GetColor(tile)·GetAlpha, rot, (4,4), scale)
GetAlpha:      (255−alpha)/255;  humo denso (16 y familia): Color(…, 25) — alfa BASE 230
Trail dusts:   tipos 130-134, 219-223, 226, 272, 278 → hasta 10 copias en pos−vel·j,
               scale·(1−j/10)  ⇒ smear de movimiento horneado
Update smoke:  vel *= (0.97,0.99)  ·  scale += 0.0025 (noGravity) / −= 0.01..0.04  ·  alpha += 4..6
Atlas:         8×8 por celda, 3 variantes random al spawn (dust.frame)
LOD:           dust>50..90% de maxDustToDraw → umbral de muerte 0.001→0.02 (autodegradación)
Muerte extra:  si Lighting.GetColor == Black → dust.active = false (fade en oscuridad)
```

**FIN DEL INFORME** — archivos fuente en `/tmp/smoke_research/` (34 JSONs de búsqueda + 12 páginas completas + 6 fuentes de código + 2 decompilaciones).

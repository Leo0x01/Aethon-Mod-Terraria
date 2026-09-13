# Matemática de los Agujeros Negros — Investigación para el Render

> Investigación en internet (búsquedas web + artículos: ISCO/Photon sphere
> Wikipedia, "Raytracing a Black Hole with WebGPU", "Approximate Black Hole
> Renderings with Ray Tracing", "Relativistic beaming") aplicada al
> **CrimsonBlackHoleProjectile v6.05** — el método pedido por el usuario:
> *copia exacta del agujero funcional → modificar parámetros*.

---

## 1. Las fórmulas fundamentales (Schwarzschild, sin rotación)

| Concepto | Fórmula | Valor relativo |
|---|---|---|
| Radio de Schwarzschild | `r_s = 2GM/c²` | 1 `r_s` |
| Esfera de fotones | `r_ph = 1.5·r_s` | 1.5 `r_s` |
| Sombra aparente (a un observador lejano) | `R_sh = (√27/2)·r_s ≈ 2.598·r_s` | ~2.6 `r_s` |
| ISCO (borde interno del disco) | `r_isco = 3·r_s = 6GM/c²` | 3 `r_s` |
| Velocidad orbital kepleriana | `v(r) = √(GM/r)` | en el ISCO: `c/√6 ≈ 0.408c` |

**Claves para el render:**
- El disco NO puede existir por dentro del ISCO: la materia cae
  irremediablemente → el borde interno del disco nace pegado a la sombra.
- La **sombra** que se ve es MÁS GRANDE que el horizonte (2.6×) porque es la
  imagen lensada de la esfera de fotones.

## 2. Lente gravitacional

- Desviación de un rayo que pasa a impacto `b` (campo débil, Einstein):
  `α = 4GM/(c²·b)` — ¡el DOBLE de lo que predice Newton! (por el espaciotiempo curvado).
- **Anillo de Einstein**: si la fuente, el agujero y el observador se alinean,
  la imagen del fondo se estira en un anillo perfecto.
- En un render por marcha de rayos: en cada paso se curva el rayo hacia el
  centro con fuerza `~ 1/r²` (aproximación newtoniana de la geodésica) — es
  EXACTAMENTE lo que hace nuestro `RealBlackHoleShader.fxc`:
  `intensidadCurva = clamp(0.005/r², 0, 0.1) · blackHoleRadius`.
- El disco trasero se ve ARRIBA y ABAJO de la sombra (la luz de la parte de
  atrás se dobla sobre el agujero) → el clásico "Gargantua" de Interstellar
  (James, von Tunzelmann, Franklin & Thorne, 2015, "Gravitational lensing by
  spinning black holes in astrophysics, and in the movie Interstellar").

## 3. Disco de acreción

- Perfil de temperatura (Shakura–Sunyaev): `T(r) ∝ r^(−3/4)` → el borde
  INTERIOR es el más caliente (blanco) y se enfría hacia fuera (rojo).
- La paleta de la referencia replica esto: núcleo blanco-rosado → magenta →
  carmesí profundo.
- Turbulencia: el plasma "hierve" — nuestro shader lo hace con ruido
  (`FireNoiseB`) muestreado en coordenadas polares desplazándose con el
  tiempo: `polar·(3, 3.5) + t·(6.3, −2)`.

## 4. Doppler beaming (relativístico)

- Factor Doppler relativístico: `δ = 1/(γ·(1 − β·cos θ))`, con `β = v/c` y
  `γ = 1/√(1−β²)`.
- Brillo observado: `I_obs = δ³ · I_emit` (bolométrico: `δ⁴`).
- Con `β ≈ 0.4` (kepleriano en el ISCO):
  - Lado que se ACERCA (θ≈0): `δ ≈ 1.53` → `δ³ ≈ 3.6×` más brillante.
  - Lado que se ALEJA (θ≈π): `δ ≈ 0.65` → `δ³ ≈ 0.28×` (≈3.5× más tenue).
  - Contraste total ≈ 13× — por eso en la foto del M87 un lado domina.
- Aplicación en v6.05: velo aditivo blanco-rosado sobre el lado izquierdo
  (el que se acerca, como en la referencia) + brasa carmesí tenue en el
  derecho.

## 5. Mapeo de fórmulas → parámetros del shader (v6.05)

| Física real | Parámetro del RealBlackHoleShader | Funcional | Carmesí v6.05 |
|---|---|---|---|
| Radio de la sombra | `blackHoleRadius` | 0.30 | **0.25** (agujero un poco más pequeño) |
| Grosor del disco (borde interno ≈ ISCO) | `accretionDiskRadius` (tubo del toro, anillo mayor fijo 0.75) | 0.40 | **0.48** (disco más grande; borde interno 0.27 ≈ pegado a la sombra 0.25, como el ISCO) |
| Borde exterior del disco | 0.75 + tubo | 1.15 = 3.8× sombra | **1.23 ≈ 4.9× sombra** (referencia: ~3.5-4×) |
| Paleta T(r) ∝ r^(−3/4) | `accretionDiskColor` | (245,105,61) naranja | **(255,45,100) carmesí-fucsia** |
| Inclinación del disco | `cameraAngle` | 0.32 rad (~18°) | **0.30 rad (~17°)** (referencia ~15-20°) |
| Grosor vertical de la banda | `accretionDiskScale.y` | 0.33 | **0.28** (banda fina casi de canto) |
| Turbulencia del plasma | `globalTime` | t | **t × 1.35** (hierve más vivo) |
| Precesión del plano | `cameraRotationAxis` | fijo + velocity | **+ oscilación ±0.05/±0.06 rad a 2 frecuencias** |
| Anillo de fotones | `distGlow = 1.7·r_h` (interno del shader) | — | **+ refuerzo Ring rosa pálido pulsante** |
| Doppler δ³ | — | — | **+ velos aditivos (izq. brillante / der. tenue)** |

## 6. ¿Por qué el funcional "no tiene assets"?

Porque es **~100% código**:
- El núcleo visual es `RealBlackHoleShader.fxc`: una **marcha de luz de 75
  pasos** que integra la curvatura 1/r² por píxel — no hay ninguna imagen.
- El "lienzo" es `InvisiblePixel.png`: un píxel transparente 1×1 escalado
  (solo el quad donde corre el shader).
- El ruido del disco es `FireNoiseB.png` (textura de ruido genérica).
- Las partículas son dusts VANILLA de Terraria (GoldFlame, etc.).
- La distorsión del fondo es otro shader (`BlackHoleDistortionShader.fxc`)
  en `BlackHoleLensSystem`.
- Las texturas "procedurales" (SoftGlow, Vortex, Ring) solo se usan en el
  FALLBACK (si el shader no carga) y en refuerzos.

## Fuentes consultadas

- Wikipedia: *Innermost stable circular orbit*, *Photon sphere*,
  *Relativistic beaming*, *Accretion disk*.
- *Raytracing a Black Hole with WebGPU* (physics + shader math).
- *Visualizing Black Holes with General Relativistic Ray Tracing* (HLSL).
- *Approximate Black Hole Renderings with Ray Tracing* (stanford edu PDF).
- *Gravitational lensing by spinning black holes in astrophysics, and in the
  movie Interstellar* — James, von Tunzelmann, Franklin & Thorne (2015).
- r/AskPhysics + astronomy.stackexchange: "Why is one side of the black hole
  brighter?" (Doppler beaming).

---

# v6.09 — EL RENDER ANALÍTICO POR CAPAS (la "otra librería con las físicas correctas")

> Petición del usuario: "investiga más sobre agujeros negros, investiga las
> matemáticas de cómo crear un agujero negro, crea otra librería de ser
> necesario con las físicas correctas, que sea igual que la referencia —
> super igual, aunque tenga que ser gigante". El shader de marcha de rayos
> (RealBlackHoleShader) produce un toro lensado genérico que NO replica la
> referencia; se reemplaza por un render ANALÍTICO por capas con la
> geometría de oclusión exacta de un disco delgado inclinado.

## 1. Investigación adicional (fuentes)

- **James, von Tunzelmann, Franklin & Thorne (2015)**, "Gravitational
  lensing by spinning black holes in astrophysics, and in the movie
  Interstellar" (Class. Quantum Grav. 32 065001, arXiv:1502.03808) — el
  paper de DNEG (DNGR): el disco se modela como un ANILLO PLANO inclinado y
  la cámara cerca; las imágenes directa + secundaria (eco) del disco se
  componen sobre la sombra.
- **Luminet (1979)**, "Image of a spherical black hole with thin accretion
  disk" (A&A 75 228) — el clásico: imagen directa (delante/detrás según el
  semiplano) + imagen secundaria lensada abrazando la sombra.
- **UNLV/physics**: sombra = (√27/2)·r_s ≈ 2.598·r_s (confirmado).

## 2. La geometría de oclusión (LO QUE FALTABA)

Un disco delgado plano visto con inclinación i (desde face-on):

```
punto(r, φ):   x = r·cos φ          (eje mayor en pantalla)
               y = r·sin φ·cos i    (eje menor; i≈69° → b/a = 0.345)
profundidad:   z = r·sin φ·sin i    (>0 = lado CERCANO, abajo en pantalla)
```

- **Lado cercano (sin φ > 0)**: se dibuja ENCIMA de la esfera → el disco
  CRUZA POR DELANTE de su cara inferior (medido en la referencia: el cruce
  empieza a +0.76·R_sh bajo el centro).
- **Lado lejano (sin φ < 0)**: se dibuja DETRÁS → la esfera negra lo
  OCULTA dentro de su silueta circular (la parte del arco lejano con
  proyección |p| < R_sh desaparece tras la bola).
- El ORDEN de dibujado (lejano → esfera → cercano) produce el "wrap"
  Gargantua SIN calcular lensing: es la oclusión geométrica real.
- El **eco lensado** (imagen secundaria de Luminet) se pinta como anillos
  circulares concéntricos a 1.7·R_sh abrazando la silueta.

## 3. Mediciones píxel-exactas de la referencia (347×173, R_sh≈14px)

| Estructura | Radio (×R_sh) | Medida |
|---|---|---|
| Esfera negra | 1.0 | negro #000000 (60,20,30 con sangrado de brillo) |
| Anillo de fotones (banda sólida) | 1.16–1.49 | blanco saturado 255, doble pico en 1.28/1.34 |
| Eco lensado | 1.68–1.96 | blanco-rosa 80–220 decreciente |
| Foso oscuro (moat) | 2.1–2.9 | brillo 50–80 (el lado lejano nace en 2.7) |
| Borde interno caliente | 2.2 (SOLO lado cercano) | cruce frontal a +0.76·R_sh |
| Pico del disco | 4.6 | magenta (255,105,255) saturado 255 |
| Borde exterior | 6.5 | carmesí apagado, fade a fondo |
| Elipse b/a | 0.345 | inclinación ≈ 69.8° desde face-on |
| Doppler | izq +30% | contraste moderado |
| Rayos cian | 2.1 → 8.5-12·R_sh | fondo, 18 rayos radiales |

Perfil radial medido (brillo normalizado):
`2.55:0.30 → 3.2:0.55 → 3.8:0.80 → 4.6:1.0 → 5.3:0.82 → 5.5:0.52 → 5.9:0.22 → 6.5:0.08`
Rampa cromática: `(255,214,240) → (255,42,122) → (255,66,168) → (255,88,232) → (255,105,255) → (255,78,212) → (224,32,84) → (142,22,54)`

## 4. La librería nueva (2 archivos)

- **`Content/VFX/BlackHolePhysics.cs`** — la matemática GR pura:
  r_s, esfera de fotones (1.5 r_s), sombra (√27/2 r_s), ISCO (3 r_s),
  velocidad kepleriana v=√(GM/r)=c·√(r_s/2r), ω∝r^(−3/2), Doppler δ y δ³,
  Shakura–Sunyaev T∝r^(−3/4) e I∝r^(−3), corrimiento al rojo g=√(1−r_s/r),
  proyección del disco inclinado + test de lado cercano/lejano.
- **`Content/VFX/CrimsonBlackHoleRenderer.cs`** — el render por capas
  calibrado 1:1 con el prototipo Python (EMA 17/255 contra la referencia):
  7 capas (ambiental+rayos → disco lejano → halo fotónico → esfera negra
  opaca → anillos de fotones con rungs → disco cercano → bloom).

## 5. Validación del prototipo (tools/mock_blackhole_v609.py)

- El prototipo usa las TEXTURAS REALES del mod (SoftGlow/Ring/GlowRay/
  BlackDisk) con muestreo bilinear + supersampling 2× (= LinearClamp+GPU).
- Error medio absoluto del perfil horizontal (23 puntos, 1.0→6.5 R_sh):
  **17/255** — el cuerpo del disco calca la referencia (pico 212 vs 207).
- Elipse de 32 líneas de corriente × cápsulas solapadas ×2.2 → banda DENSA
  continua (la petición "disco más denso").
- Escala gigante autorizada: R_sh = 38px (esfera 76px) → disco 494px de
  ancho total a escala 1 (≈53% del ancho de la referencia, igual que ella).

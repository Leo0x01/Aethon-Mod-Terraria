# INFORME v6.39 — LOS RAYOS ELÉCTRICOS Y LOS DESGARROS DE REALIDAD

**La queja del usuario (textual):** "los desgarros están mal y cuando usas rayos
eléctricos para los desgarros tienen un error gráfico: líneas intermitentes de un
color más oscuro o claro, como si le pusieras brillo y desenfoque pero este se
corta por secciones… el problema puede estar en la librería o cómo se implementan
los rayos con la librería… si la realidad se desgarra no sería una fea línea recta."

---

## A. LA REVISIÓN CAPA POR CAPA (medida sobre los archivos reales)

Se auditaron las TRES capas del pipeline de rayos de la casa
(`StormLib` v6.21 → texturas `Bolt*` → lotes `BlendState.Additive`),
más la capa del desgarro (`RiftLib` v6.31 → texturas `RiftTaper*`).

### Capa 0 — LAS TEXTURAS (el hallazgo crítico, medido con numpy)

| Textura | RGB | Alfa | Perfil a lo largo (eje X) |
|---|---|---|---|
| SoftGlow (v5.x, la de siempre) | **perfil horneado** (0..255) | perfil | radial (correcto para puntos) |
| GlowOrb / GlowCircle / GlowRay | **perfil horneado** | perfil | — |
| **BoltHalo** (v6.21) | **blanco puro (255)** | perfil | modulado (mood 0.85..1.0) |
| **BoltCore** (v6.21) | **blanco puro (255)** | perfil | **muy modulado**: grietas por bloques de 16 px, nodos cada 34-60 px, serpenteo del eje |
| **BoltChain** (v6.21) | **blanco puro (255)** | perfil | cuentas discretas |
| **BoltImpact** (v6.21) | **blanco puro (255)** | perfil | radial |
| **RiftTaperVelo/Núcleo** (v6.31) | **blanco puro en el cuerpo** | perfil | meseta uniforme (bien) |

**El bug raíz nº 1 — LA SEMÁNTICA DEL LOTE ADITIVO.**
`BlendState.Additive` de XNA/FNA es `(SrcBlend=One, DstBlend=One)`: el color
fuente se SUMA sin pasar por el alfa. El pipeline real es:

```
aporte.rgb = textura.rgb × tinte.rgb          // ¡el alfa de AMBOS es ignorado!
```

Consecuencias medidas en TODO lo dibujado por StormLib (rayos de la herida
eléctrica, tormenta del magnetar, rayos del olvido, corona de arcos…):

1. **BoltCore/BoltHalo/BoltChain/BoltImpact dibujan RECTÁNGULOS SÓLIDOS**:
   su filamento vive en el canal alfa → invisible en aditivo. El "zigzag
   luminoso" es en realidad una banda sólida con esquinas duras.
2. **`StormLib.Tint()` ponía la intensidad en el ALFA del tinte** → también
   ignorada → las 3 capas (halo 0.30 / cuerpo 0.85 / núcleo 1.0) salen TODAS a
   brillo máximo → apilamiento descontrolado donde se solapan.
3. **RiftTaperVelo/Núcleo** (el brillo del desgarro) dibujan su cuerpo como
   banda sólida → el desgarro lee "línea dura", no "herida de luz".

Las texturas v5.x de la casa (SoftGlow etc.) SIEMPRE hornearon el perfil en RGB
precisamente por esto — la convención correcta existe en el proyecto desde el
principio; StormLib v6.21 y las RiftTaper v6.31 la olvidaron.

**El bug raíz nº 2 — EL PERFIL A LO LARGO DEL EJE X.**
Aunque el alfa hubiera funcionado, BoltCore lleva grietas (bloques de 16 px),
nodos (gauss cada 34-60 px) y serpenteo horneados a lo ancho de los 256 px.
StrandImpl divide el rayo en sub-segmentos de ≤42 px y **cada sub-segmento
estira la textura COMPLETA** → cada sección repite TODO el patrón de
modulación → "brillo y desenfoque QUE SE CORTA POR SECCIONES" (la frase exacta
del usuario, explicada).

### Capa 1 — LA GEOMETRÍA (sana)

`ZigPath` (jitter perpendicular con envolvente senoidal, anclajes exactos),
`Refine` (midpoint con sesgo cúbico), `FractalPath` (midpoint displacement
canónico con offset/2 por generación), `Boil`, `ForkTree` (ramas con
auto-corrección de curvatura): todo correcto y determinista. La geometría
NUNCA fue el problema.

### Capa 2 — EL PINTADO (StrandImpl, PintaArco, SeekArc, BoltRenderer)

1. **StrandImpl solapa cada quad `subLen + w*2.0`** (extiende w por extremo):
   en aditivo, cada junta recibe el DOBLE de brillo → "cuentas brillantes"
   periódicas → las "líneas intermitentes de un color más claro".
2. **El crackle es POR SUB-SEGMENTO** (`Hash01(seed, flick, k*41+17)`): brillo
   aleatorio duro que cambia a saltos cada 42 px → las "de un color más
   oscuro".
3. **Rotación por sub-segmento propio** (no la normal media): en las juntas
   giradas los solapes forman lentes de doble brillo.
4. **PintaArco/SeekArc (los arcos de la herida eléctrica) usan SoftGlow
   estirado a lo largo**: SoftGlow es RADIAL → funde a 0 en los DOS extremos de
   cada quad → franjas oscuras en cada junta, compensadas con solapes
   `sl + w*1.6/0.8/0.4` → doble brillo donde alcanza, oscuridad donde no →
   **EL artefacto exacto reportado** ("como si le pusieras brillo y desenfoque
   pero se corta por secciones"). La "normal media" de v6.33 alineó las rotaciones
   pero el fallo de textura+solape quedó intacto.
5. **BoltRenderer (1ª generación)**: SoftGlow + solape `segLen + width` → el
   mismo banding en todo lo que lo usa (coronas rúnicas, látigo de la medusa…).

### Capa 3 — EL DESGARRO (RiftLib)

`Tear`/`TearVacio` dibujan UN SOLO quad estirado de punta a punta →
**"una fea línea recta"** (la frase del usuario). La v6.31 eligió "la línea
permanece recta" para matar la discontinuidad del huso; el precio fue la
rectitud. `HeridaElectrica` (v6.33) es también un quad recto + StormArc + 5
ramas cortas.

---

## B. LA INVESTIGACIÓN EXTERNA (web, esta sesión)

Búsquedas realizadas (z-ai web_search ×11 + lecturas): midpoint displacement
2D lightning (Wikipedia diamond-square, stevelosh, craftofcoding, gamedev.se),
electricity arc methods (realtimevfx), overlapping additive sprites (Foro
Construct.net: *"letting the sprites overlap will create double lighting
effect and looks bad"* — confirmación directa del mecanismo de las "cuentas"),
trail discontinuities (Foros Unity: los trails necesitan VÉRTICES COMPARTIDOS
y UV continua), LIGHTNING de Shadertoy (capas core+glow, flicker con ruido),
rayos de tModLoader (foros Terraria/Thorium).

**Síntesis de la técnica canónica de los 2D de nivel alto (consenso de todas
las fuentes + práctica de los mods grandes):**

1. **La nitidez vive en la GEOMETRÍA, nunca en la textura**: camino fractal
   multi-escala (midpoint displacement, offset/2 por generación) + regeneración
   a 6-15 Hz + probabilidad de apagado entero (0.62 en nuestra casa, correcto).
2. **La textura del filamento es UNA BANDA UNIFORME a lo largo** (solo cae a
   través del ancho) y **PREMULTIPLICADA (perfil en RGB)** para que el lote
   aditivo la respete: es LA lección XNA/FNA ("additive ignores alpha —
   premultiply your textures").
3. **El ribbon es continuo**: quads de borde a borde con la NORMAL MEDIA en las
   juntas (miter) — cero solapes apilables, cero huecos. El brillo es CONSTANTE
   de punta a punta; la variación de brillo (si la hay) es de BAJA FRECUENCIA
   e interpolada, nunca por sección dura.
4. **El look "eléctrico" se compone por CAPAS DE ANCHO** (halo ×2 / cuerpo ×1 /
   núcleo ×0.26 casi blanco) + ramas + pelos + parpadeo — no por ruido horneado
   en la textura.
5. **Un desgarro de realidad NO es una recta**: es un borde RASGADO — dientes
   multi-escala congelados (la herida está abierta, no vibra como un guscano)
   + micro-deriva lenta (≤0.3 Hz) para que viva. Los extremos quedan clavados.

---

## C. EL FIX v6.39 (lo implementado)

| # | Fix | Dónde |
|---|---|---|
| 1 | **Texturas de tormenta regeneradas**: perfil horneado en RGB (premultiplicado, la convención SoftGlow de la casa), **uniformes a lo largo** (solo 3 px de fundido en los extremos para el antialias) | `tools/gen_bolts_v637.py` → BoltHalo/BoltCore/BoltChain/BoltImpact |
| 2 | **RiftTaper Velo/Cuerpo/Núcleo rebornadas**: RGB = perfil×estructura (el brillo del desgarro recupera sus labios y su filo en el lote aditivo) | `tools/gen_bolts_v637.py` (lee los PNG v6.31 y hornea) |
| 3 | **`StormLib.Tint` premultiplicado** (rgb×f, alfa=f — el Tint v6.25 de RiftLib): la intensidad VUELVE a funcionar en aditivo | StormLib.cs |
| 4 | **`StrandImpl` v6.39**: normal media en juntas + largo EXACTO proyectado (+1.2 px de margen AA, sin solapes de ancho) + **crackle POR PUNTO interpolado** (brillo continuo, nunca por sección) | StormLib.cs |
| 5 | **`PintaArco`/`SeekArc` con la banda uniforme** y largos exactos (muere el SoftGlow-estirado y su compensación por solape) | StormLib.cs |
| 6 | **`BoltRenderer` (1ª gen) con texturas de banda** + normal media + largo exacto | BoltRenderer.cs |
| 7 | **`RiftLib.CaminoDesgarro`**: el borde RASGADO determinista (3 octavas congeladas por semilla + deriva lenta, anclajes exactos) | RiftLib.cs |
| 8 | **`Tear`/`TearVacio` dibujan POR EL CAMINO rasgado** (segmentos con solape adaptativo y perlas de la cadena gemela v6.31 — la continuidad queda intacta): todos los desgarros del mod dejan de ser rectos DE GOLPE (RealityTear, Rencor, VozCuasar, Ocaso) | RiftLib.cs |
| 9 | **`CaminoVibracion` + el rasgado**: la vibración es la onda estacionaria SOBRE el borde rasgado | RiftLib.cs |
| 10 | **`HeridaElectrica` rasgada de punta a punta**: vacío + labios por camino, arcos voltaicos CORRIENDO POR DENTRO del rasgado (entre puntos del camino), aberración/estática/ramas sobre el camino | RiftLib.Portales.cs |

**La garantía de continuidad (por qué esto no repite el error v6.28):** el
rasgado NO regenera su forma por frame — el patrón de dientes está CONGELADO
por semilla (una herida abierta no se re-teje); solo arrastra una deriva lenta
de ±15% a 0.3 Hz. La línea de daño (cápsula `LineaToca`) sigue midiendo sobre
la recta: la desviación visual (≤ 0.45·maxWidth px) cabe holgada dentro del
radio de daño (maxWidth + 8).

**Coste:** Tear pasa de 3 quads a ~3×(10-14) quads + perlas — dentro del
presupuesto de la casa (los desgarros son 1-2 por pantalla).

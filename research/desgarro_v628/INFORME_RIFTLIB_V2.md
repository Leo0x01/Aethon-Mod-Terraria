# INFORME RIFTLIB V2 — LA LÍNEA CONTINUA ANTI-HUECOS + EL STAGING RECTO→FRACTURA
## Task RIFT-RESEARCH · Agente: general-purpose (investigación desgarros, ronda 2)

**El problema concreto que motiva esta ronda:** el desgarro de RiftLib v6.26 se renderiza
como QUADS SEGMENTADOS con huecos e interrupciones azules entre segmentos, en vez de una
línea luminosa continua. Este informe encuentra la causa raíz MEDIDA en píxeles, la receta
exacta que usa el ecosistema para líneas continuas, y el staging recto→fractura con pico
de daño que pide el usuario. Continúa (no repite) al INFORME_DESGARRO_REALIDAD.md de la
carpeta `estrategia_v626` (Task 42-b: Calamity DoG/MEAC/WoTE, escuelas de daño, contrato
v1). Aquí se investiga lo que v1 NO cubrió: la geometría de la línea sin huecos y el
timing de la fractura.

### Método
| Tipo | Fuentes |
|---|---|
| **Código fuente leído línea a línea** | CalamityModPublic (clone local /tmp/calamity): `Graphics/Primitives/PrimitiveRenderer.cs` (886 L) + `PrimitiveSettings.cs` (315 L) + `VertexPosition2DColorTexture.cs` + `Projectiles/Magic/HyperdeathRiftScepterBeam.cs` + `Projectiles/Melee/DeathsAscensionRift.cs` + shaders `StandardPrimitiveShader.fx` |
| **Medidas de píxeles (PIL)** | 5 texturas de Calamity (`BloomLineThick`, `LineThick`, `BloomCircle`, `ScarletDevilStreak`, `SylvestaffStreak`) vs 3 de casa (`TrailGlow`, `Trail`, `LumenBlade`) → `MEDIDAS_TEXTURAS.txt` |
| **Búsquedas web (17 consultas + 8 páginas leídas)** | JSONs en `busquedas_web/`: beyond-fx (hell rift Diablo), 80.lv (Desolus), infinitecanvas lección 12 (polylines/miter), MonoGame/XNA line-drawing (4 fuentes), wiki Calamity (Dimension-Tearing Disk), wiki WoTG (Rift Eclipse), Riot VFX style guide, física de fractura del vidrio (ceramics.org, structuremag), Genshin abyss, Apex Wraith |
| **Repo GitHub** | absoluteAquarian/GraphicsLib (librería de primitivas GPU para tML) |

> REGLA DE HONESTIDAD (la de la casa): todo código ajeno se cita como paráfrasis/pseudocódigo
> propio; los NÚMEROS (medidas, factores, defaults) son datos y se citan con su origen.

---

# SECCIÓN A — QUÉ HACEN LOS MODS DE TERRARIA (hallazgos nuevos)

## A.1 Calamity `PrimitiveRenderer` — EL estándar de líneas del ecosistema

Calamity NO dibuja sus ribbons con quads por segmento: **construye una malla de vértices y
la envía como `TriangleStrip` — los vértices son COMPARTIDOS entre segmentos, así que
geometría sin huecos POR CONSTRUCCIÓN** (no hay "junta" que fallar). Datos exactos:

- **Topología**: `PrimitiveTopology.TriangleStrip` por defecto; `TriangleList` solo si
  hay caps (entonces añade un vértice central en cada extremo → triángulo-tapa).
- **JoinStyle** (cómo expandir cada punto a izquierda/derecha): `Flat` (normal del propio
  segmento — el "legacy"), `Smooth` (media de las 3 normales vecinas ÷3 — **el default**)
  y `Miter` con `JoinMiterLimit = 4` por defecto (¡el mismo límite 4:1 del estándar SVG!).
- **La matemática del miter** (leída del código, es la receta de la Sección B):
  `miter = normalize(n_prev + n_next)`; `miterLen = halfWidth / dot(miter, n_next)`;
  clamp a `±halfWidth · MiterLimit`. Si el giro es demasiado cerrado, degrada a bisel.
- **Anchura por VÉRTICE, no por segmento**: `widthAtVertex = WidthFunction(completionRatio)`
  — la anchura se evalúa en el PUNTO compartido, así dos segmentos contiguos usan la MISMA
  anchura en la junta (cero escalones de grosor).
- **UVs**: modo `Normalized` (U 0→1 a lo largo de toda la línea) o modo `Distance` (una
  textura completa cada `TextureCycleLength` px + `TextureScrollOffset` para scrollear).
  La V se calcula `0.5 ± effectiveHalfWidth·0.5` — el perfil transversal se conserva a
  cualquier anchura.
- **Suavizado** Catmull-Rom por defecto (opciones Cardinal/Hermite/Bezier/Linear),
  guardas anti-anchura-degenerada con `Epsilon`.
- Incluye **wireframe de debug** (verde lima) — idea robable para validar RiftLib v2.

**GraphicsLib** (absoluteAquarian, GitHub) es la generalización de esto como librería
GPU de primitivas para tML — confirma que "malla de vértices" es LA vía seria del ecosistema.

## A.2 Calamity `HyperdeathRiftScepterBeam` — EL beam de desgarro, 100% SpriteBatch

El cetro "Hyperdeath Rift" (post-DoG) dispara un haz de **3000 px dibujado con UN SOLO
`EntitySpriteDraw`** — un único quad estirado con la textura `BloomLineThick` (1960²),
rotación `dir + π/2`, origen `(Width/2, Height)` (anclado en el punto de ruptura).
**Cero segmentos = cero juntas = cero huecos.** El "look desgarro" lo construye por CAPAS:

| Capa | Técnica | Números |
|---|---|---|
| Núcleo color | 1 quad `BloomLineThick` tintado (color con `A=0`) | thickness 0.09, opacidad 0.35 |
| **Vacío negro dentro** | `4·scale` quads `LineThick` NEGROS cada vez más estrechos y más opacos, encima del núcleo | thickness ×`(0.8 − 0.15·t)`, alpha `(0.2 + 0.15·t)` → **el centro lee NEGRO y el borde queda brillando** |
| Respiración | grosor ×`Remap(sin(t·4/π), −1, 1, 0.8, 1.1)` | **±15%** |
| Telegafía → golpe | `attackTime = 10` ticks sin daño; al atacar `laserFX = 2.5` (pico) que decae `Lerp(_, 0, 0.07)` | daño solo si `doneAttack && laserFX ≥ 1` |
| Hitbox | `CheckAABBvLineCollision(start, end, 70·scale)` | cápsula de **70 px·scale** |
| Extras | screenshake 3 (si jugador <1600 px), `DrawScreenCheckFluff = 10000`, modo fotosensibilidad baja opacidad a 0.2 | — |

**Truco de blending premultiplicado** (leído del código): en el batch premultiplicado de
entidades, un color `RGB>0, A=0` **AÑADE luz** (fuente=One) y un `RGB=0, A>0` ** oscurece**
— así el MISMO pase pinta el glow y el vacío negro interior. Casa ya tiene su contrato de
2 lotes (NonPremultiplied para el vacío + aditivo para la luz): ambas vías son válidas.

## A.3 Las texturas de línea de Calamity — MEDIDAS (la prueba del delito)

Medidas PIL (medias de columna; ver `MEDIDAS_TEXTURAS.txt`):

| Textura | Tamaño | col0 / colmid / collast (eje X) | Veredicto |
|---|---|---|---|
| `BloomLineThick` | 1960² | PERFECTAMENTE uniforme en su eje largo (250,250,250 en TODA la columna) | ✓ anti-huecos |
| `LineThick` | 1960² | uniforme; banda dura (~30% del ancho, alpha 255) sobre alpha 0 | ✓ núcleo razor |
| `ScarletDevilStreak` | 256² | 14 / 17 / 14 → uniforme a lo largo, gradiente SOLO transversal | ✓ anti-huecos |
| `SylvestaffStreak` | 256² | 12 / 18 / 13 → ídem | ✓ anti-huecos |

**Todas son MÁSCARAS GRISACES (el color lo pone el tinte por vértice), nunca llevan hueco
horneado.** El perfil transversal del `BloomLineThick` (muestreado): 0→19→60→205→**250**
→194→51→0 — gaussiana con pico 250/255.

## A.4 Funciones de anchura concretas (patrones para nuestros labios)

- `Phaseslayer`: `127·(1−completionRatio)·0.8` — taper lineal (gorda en raíz, aguja en punta).
- **`DynamicPursuerElectricity`** (¡el "nebula beam" del **Dimension-Tearing Disk**, arma
  rogue de 725 dmg que "tears space", drop del Devorador de Dioss — wiki):
  `Lerp(4, 7, sin(4π·f)·0.5+0.5) · sin(π·f)` — **la anchura OSCILA 4 veces a lo largo del
  camino** (look nebuloso/eléctrico) con envelope sin(π·f) en las puntas.
- `Elumphant`: respiración temporal `sin(t·9 − f·6)` + taper `pow(1−f, 2)` + suavizado del
  arranque (cutPoint 0.1).
- `MagnusBeam`: constante `24·scale`.
- `ScarletDevil`: `Lerp(0, 110, GetLerpValue(0, 0.1, f)) · clamp(1−pow(f, 0.4), 0.37, 1)`
  — arranque rápido (0→0.1) y caída suave.

## A.5 `DeathsAscensionRift` — el desgarro-metaball

La "Ascensión de la Muerte" abre un rift VERTICAL con metaballs: 20 pares de bolas a lo
largo de una línea (offset `i·5` px), escala `Lerp(90, 10, i/20)` con ruido de tamaño
±10→0 y posición ±20, escalando TODAS dentro durante los últimos 10 ticks de vida
(`GetLerpValue`). **Un desgarro que se "infla" de dentro afuera con gradiente de tamaño
gordo→fino.** (Casa: vía fuente-LÍNEA en la lente, T14 del informe v1.)

## A.6 WoTG y el ecosistema (verificación web)

- Wiki oficial de Wrath of the Gods: existe contenido **"Rift Eclipse"** (página aún vacía
  en wiki.gg — el evento del Nameless Deity; su "Reality Shatter" ya está en el informe v1).
- Hilo de foros "tModLoader — Complicated question about visual effects" (ago-2023):
  la comunidad pide "fluid effects / tears in space" — demanda confirmada del look.
- Negativo (v1 confirmado): Starlight River no tiene contenido rift propio.

---

# SECCIÓN B — LA RECETA TÉCNICA: LÍNEA CONTINUA ANTI-HUECOS EN SPRITEBATCH

## B.0 Diagnóstico de NUESTRO bug (medido, no adivinado)

1. **La textura es la causa raíz.** `TrailGlow.png` (32×8) tiene una RAMPA DE ALPHA en el
   eje LONGITUD: 3→7→15→…→204 de izquierda a derecha, y encima es **CIAN pura (0,255,255)**.
   Cada segmento se dibuja con esta textura → el inicio de cada quad es casi transparente
   y teñido de cian: **hueco oscuro + interrupción azul en CADA junta**. Las otras texturas
   de línea de casa (`Trail.png` 255→3, `LumenBlade.png` 0→222→0) también degradan a lo
   largo — ninguna es uniforme en longitud. El solape `len + w·0.5` del código actual NO
   puede arreglarlo: el problema está DENTRO de la textura, no en la geometría.
2. **Anchura por centro de segmento** (`lens` evaluado en el midpoint de cada segmento):
   dos segmentos contiguos usan anchuras distintas en su vértice compartido → escalones.
3. **Quads = rectángulos rotados**: en un camino angulado dejan cuñas sin cubrir (sin miter).

## B.1 EL 80% DEL FIX — el contrato de textura "labio" (2 texturas nuevas, 100% procedurales)

Generar (casa ya hornea PNGs en runtime/on-build):

- **`RiftLip.png` (128×16, blanco/gris, alpha 255)**: luminancia **UNIFORME en X** (col0 =
  colmid = collast, verificar con el mismo script de MEDIDAS) y gradiente gaussiano SOLO en
  Y con pico ≈250/255 (perfil normalizado 0/19/60/205/250/194/51/0 — el de BloomLineThick).
  **SIN color horneado** — el tinte de la paleta lo pone la capa (adiós cian fantasma).
- **`RiftCore.png` (64×16)**: banda dura (núcleo sólido ~30-40% del ancho, alpha 0 fuera) —
  para el razor del núcleo y los bordes del vacío (el papel de `LineThick`).

Regla de oro verificada en las 5 texturas de Calamity: **uniforme a lo largo, gradiente
solo a lo ancho, color=0 horneado**. Con eso, los quads pueden tocarse "butt" (sin solape)
y NO puede existir hueco: la textura vale 250/255 en el borde exacto del quad.

## B.2 La fase RECTA = UN SOLO QUAD (la respuesta literal de Calamity)

Mientras el desgarro es una línea recta (telegrafía→apertura→sostenido ANTES de fracturar),
**no hay ninguna razón para segmentar**: `segs = clamp(len/48, 8, 24)` actual son 8-24
quads para una recta. Con la textura B.1, el helper `Quad()` DE CASA ya sirve tal cual —
solo cambia la llamada:

```
// La línea entera como UN quad (patrón HyperdeathBeam):
Quad(batch, RiftLip, origin + dir·(length·0.5f),       // centro = punto medio
     new Vector2(length, w),                            // TODO el largo en X
     rot, Tint(cuerpo, 0.60f·intensity));              // rot = Atan2(dir)
```
(620 px de desgarro = 1 quad por capa y lado; Calamity estira 3000 px sin pestaña.)

## B.3 La fase FRACTURADA = segmentos con juntas miter (la matemática, 2 fuentes coincidentes)

Para el camino jagged (Lichtenberg de `CaminoGrieta`), cada segmento se expande con la
normale del VÉRTICE compartido, no del segmento:

```
// en cada punto interior i del camino:
n_prev, n_next = normales de los segmentos (i-1→i) e (i→i+1)
miter    = normalize(n_prev + n_next)          // bisectriz
miterLen = halfWidth / max(|dot(miter, n_next)|, ε)   // ±(el coseno del semiangulo)
miterLen = clamp(miterLen, ±halfWidth·4)       // MiterLimit 4 (Calamity Y estándar SVG)
izq[i] = p[i] − miter·miterLen ; der[i] = p[i] + miter·miterLen
// el ancho w[i] se evalúa EN el vértice (lens(f) con f = arco/longitud total)
```

**Renderizado puro SpriteBatch de esas juntas** (SpriteBatch no sabe dibujar cuads
cizallados): dos técnicas combinadas, ambas baratas —

1. **SOLAPE por extensión**: cada quad mide `dist + w` (medio ancho extra por cada extremo,
   a lo largo de SU dirección). Cubre giros de hasta ~53° sin cuña visible. Con textura
   uniforme, el solape dobla el brillo solo en aditivo (ver B.6).
2. **PERLA DE JUNTA**: en cada vértice interior con giro > ~30°, 1 quad `RiftLip` CUADRADO
   (o SoftGlow pequeño) de lado `w`, alfa = pico de la capa. Es el "round join" del estándar
   (regl-gpu-lines gasta hasta 134 vértices/segmento a resolución 32 para redondear; en
   sprites cuesta 1 quad). En un glow aditivo las perlas leen como "cuentas de luz" —
   estético, no parche.

Coste total fase fracturada: `segs` quads por capa + `(segs−2)` perlas ≈ 2× el presupuesto
actual — dentro del contrato de rendimiento de la casa (~60 quads por desgarro).

**Camino definitivo (v3, documentado):** `DrawUserPrimitives`/TriangleStrip con el batch
del llamador (lo que hacen Calamity y GraphicsLib) — sigue siendo 100% código sin assets;
requiere vértice propio + matriz. NO necesario para arreglar el bug.

## B.4 Continuidad de COLOR

- El tinte va por CAPA (constante a lo largo de la línea) — como ya hace `Tear()`. Nunca
  muestrear la paleta por segmento (bandas de color).
- Los degradados a lo largo (raíz→punta) solo via ANCHURA (taper) o alfa de capa, no via
  textura U.
- La aberración R/B (±2 px, canal puro) está bien PERO solo en el sostenido (LOD actual ✓):
  en apertura/fractura el doble-draw por canal engorda las juntas.

## B.5 Scrolleo del interior SIN seams

Con textura uniforme la U es irrelevante → sin seams posibles. Para el flujo interior
(estrellas), la vía MEAC (v1, §1.2.1): **rectángulo fuente animado en espacio de textura**
(`sourceRect.X −= 2·ticks`) en vez de depender del wrap del sampler; o posiciones de las
estrellas desplazadas sobre el eje (casa ya lo hace así ✓).

## B.6 Brillo en los solapes (aditivo)

- El solape B.3.1 dobla el aporte en la zona: para el NÚCLEO razor usa `RiftCore` (banda
  dura) con juntas EXACTAS (butt) + perlas solo en vértices con giro — cero doble-adición.
- Para velos difusos el doble-aporte es casi invisible (gaussiana²); si molesta: perla con
  alfa ×0.85 del pico de capa.
- El vacío (negro, lote NonPremultiplied) se solapa sin pena: alpha 0.94 sobre 0.94 ≈ 0.94.

## B.7 Anti-aliasing

Del estándar WebGL/OpenGL ("Drawing Antialiased Lines" vía infinitecanvas L12): basta un
**feather de 1 px** (expandir `w → w+1` y suavizar en el borde) — el gradiente gaussiano
de RiftLip ya lo trae de serie.

---

# SECCIÓN C — TEORÍA DEL COLOR + STAGING DEL DESGARRO (AAA y casos)

## C.1 El convenio de color universal del "tear" (4 fuentes convergentes)

| Fuente | Receta |
|---|---|
| **Genshin (Abismo)** | rasgaduras VERTICALES negras con labio blanco-azulado; el Abismo es "espacio vacío de vida y luz" — interior negro absoluto + filo claro |
| **LoL (Vacío, Vel'Koz)** | familia violeta/púrpura con NÚCLEOS brillantes magenta; guía Riot: el color comunica gameplay primero (claridad > clutter) |
| **Doom Eternal (portales infierno)** | anillo de brasas alrededor, interior NEGRO "carne" — borde caliente, centro vacío |
| **Calamity DoG** (v1 + código) | Twilight (147,24,204) + LightBlue (0,221,250) + Fuchsia (255,0,255); borde del metaball (136,26,186); y el Hyperdeath beam: núcleo NEGRO estrecho DENTRO del glow magenta |

**Convergencia: interior desaturado/oscuro + borde saturado + razor blanco-central.** La
paleta `Vacio` de casa (150,80,255 → 60,80,220 → 80,200,255 → 235,245,255) YA cumple el
convenio del ecosistema. Apex (Wraith) añade la 4ª escuela: el MUNDO reacciona (pantalla
en escala de grises durante el vacío) — casa: `RiftMundoSystem` oscurece ✓.

## C.2 EL STAGING DE 4 TIEMPOS (con la FRACTURA como beat nuevo)

Referencias de timing: tutorial hell-rift de Beyond-FX (Diablo) lista **"Anticipation" como
fase propia** antes de los sistemas de partículas, y **"Parallax Effect"** como fase de
profundidad posterior; física del vidrio (ceramics.org / structuremag): **las grietas se
propagan a 1.458–1.500 m/s** (Slow Mo Guys a 78.000 fps; ~3.000–4.800 km/h según vidrio) —
a escala de juego, UNA GRIETA ES UN EVENTO DE 1 FRAME. Principios de timing VFX
(vfxapprentice/sunstrike): anticipación→impacto→follow-through, el impacto se resuelve en
1-2 frames ("single-frame visuals sell impact"), el timing se planea ANTES del arte.

| Beat | Duración | Visual | Daño |
|---|---|---|---|
| **1. TELEGRAFO** (anticipation) | 10-20 t | estrella creciente, anillo implosionando 90→0, pitch sube −0.75→0 (DoG), línea-fantasma fina | 0 |
| **2. EL GOLPE (apertura)** | 3-4 t (MEAC 0.333/t) | la línea RECTA se abre de golpe: flash, kick perpendicular, estrella 4 puntas | 100% (línea entera) |
| **3. SOSTENIDO** (la línea viva) | 60-120 t | respiración ±6-15%, estrellas fluyendo, aberración R/B, oscurecer del mundo | 100% (escuela A: CheckAABBvLine) |
| **3.5 VIBRACIÓN** (NUEVO — pre-fractura) | 10-20 t | la recta developa una onda estacionaria: amplitud 0→3 px a 8-12 Hz — el "cristal que sufre" | 100% |
| **4. FRACTURA (shatter)** | **1-2 t** (física del vidrio) | la línea SE QUIEBRA al camino Lichtenberg jagged en 1 frame + shards (CrackParticle: PolyOut(4), congelados 1 tick) + screenshake 6-8 | **PICO ×1.5-2 durante 4-8 t** |
| **5. CIERRE** | 20-30 t | la grieta jagged persiste y respira (±8%), taper→0, shards se apagan | OFF 8 t antes del final visual (MEAC) |

**Por qué el daño sube en la fractura:** es el principio anticipación→impacto llevado al
interior del efecto: el jugador APRENDE la línea recta (beat 3), la vibración le avisa
(beat 3.5), y el quiebre ES el segundo impacto (el que la física dice que es instantáneo).
Calamity ata el daño al pico visual igual (`laserFX 2.5` al atacar; `canDamage` solo con
`laserFX ≥ 1`); MEAC ata el cese de daño al final visual (−8 t). El pico de fractura pide
además inmunidad local corta (12-15 t) para no re-machacar al ya golpeado.

## C.3 Detalles de aaa que faltan por copiar (baratos)

- **Paralaje interior** (Beyond-FX lo lista como fase; DoG usa 1/30 vs 0.9/30 en X/Y): casa
  ya tiene paralaje X≠Y en las estrellas ✓ — añadir 2 capas de estrellas (cercanas 1.0,
  lejanas 0.9) para profundidad.
- **Columna de luz como telegrafía de spawn** (Doom hell rifts → LumenLib.Lance).
- **El mundo en escala de grises** (Apex) — versión suave: el `RiftMundoSystem` ya mata el
  verde antes que el azul ✓ (v6.26); opcional endurecer a −70% saturación durante la
  fractura (2-3 t).

---

# SECCIÓN D — LECCIONES CONCRETAS PARA RIFTLIB v2 (con números)

1. **TEXTURA ANTI-HUECOS**: generar `RiftLip.png` 128×16 — uniforme en X (col0=colmid=
   collast, verificación PIL automática en el generador), gaussiana en Y pico 250/255,
   RGB blanco/gris SIN color horneado. Es el 80% del fix. [B.1, A.3]
2. **KILL THE CIAN**: `TrailGlow.png` (rampa 3→204 + RGB (0,255,255)) queda BANEADA para
   líneas estáticas — solo para estelas que se desvanecen (su caso de uso real). [B.0]
3. **RECTA = 1 QUAD**: en fases Telegrafo/Apertura/Sostenido-vivo, TODO el desgarro es UN
   solo draw por capa con `Quad(batch, RiftLip, mid, (length, w), rot, …)` (620 px como los
   3000 de Calamity). Elimina `segs` para la recta: −90% de quads. [B.2, A.2]
4. **ANCHURA EN EL VÉRTICE**: evaluar `lens(f)` en los extremos compartidos de cada
   segmento, jamás en centros de segmento (mata los escalones de grosor). [A.1, B.3]
5. **MITER CON LÍMITE 4**: `miter=norm(n1+n2)`, `miterLen=halfW/dot(miter,n2)`, clamp
   `±halfW·4`; más allá, bisel. [A.1, B.3, infinitecanvas L12]
6. **SOLAPE + PERLA**: quads `dist + w` de extensión cubren giros ≤53°; para giros mayores,
   1 perla `RiftLip` cuadrada de lado `w` en el vértice (round join). Presupuesto ≈2× el
   actual (~60 quads por desgarro fracturado). [B.3]
7. **JUNTAS EXACTAS PARA EL NÚCLEO**: el razor (`RiftCore`, banda dura) va butt (sin solape)
   + perlas solo en giros — evita doble-adición en aditivo. Velos difusos: solape libre
   (gaussiana²≈invisible) o perla ×0.85. [B.6]
8. **EL VACÍO DENTRO DEL GLOW**: banda negra 0.62·w de casa está EN el rango del ecosistema
   (Calamity: capas negras 0.725-0.8·w ×4 con alfas 0.2-0.35 crecientes). Mantener 0.62 y
   añadir 2ª capa negra ×0.8 alfa 0.3 en el beat de fractura. [A.2]
9. **RESPIRACIÓN ±15%**: `Remap(sin(t·4/π), −1, 1, 0.8, 1.1)` del Hyperdeath como opción
   máxima; la casa usa ±6-8% (Breathe) — subir a ±10% en sostenido para "línea viva". [A.2]
10. **STAGING 4+1**: Telegrafo 10-20 t (0 daño) → Apertura 3-4 t (MEAC 0.333/t ✓ casa) →
    Sostenido → Vibración 10-20 t (amplitud 0→3 px, 8-12 Hz) → **Fractura 1-2 t** → Cierre
    con daño OFF 8 t antes del final. [C.2]
11. **PICO DE DAÑO EN LA FRACTURA**: ×1.5-2 durante 4-8 t, inmunidad local 12-15 t,
    screenshake 6-8 (rango DoG 6-14), shards CrackParticle-style (PolyOut(4), congelados
    1 tick, alpha sin(π/2+t·π/2)). [C.2, v1 §1.1.5]
12. **GROSOR OSCILANTE = NEBULOSA**: para el interior de la fractura, anchura
    `Lerp(4,7,sin(4π·f)·0.5+0.5)·sin(π·f)` (DynamicPursuer — el beam del Dimension-Tearing
    Disk de 725 dmg). [A.4]
13. **PALETA**: mantener `Vacio` (cumple el convenio violeta→cian→blanco del ecosistema);
    en fractura, desplazar 1 tick a paleta más BLANCA (el pico de energía) y volver. [C.1]
14. **HITBOX CONSTANTE**: cápsula `CheckAABBvLineCollision` con radio `max(0.5·w, 4)` (casa
    ✓) — Calamity usa 70·scale para un beam de 3000 px (proporción ~1:43 largo:radio). [A.2]
15. **PERF**: `DrawScreenCheckFluff = 10000` para desgarros de pantalla (vía ya usada por el
    proyectil); budget total con fractura ≈ 60-80 quads + 16-28 estrellas + shards — lejos
    del techo de la casa (1-2k). Fotosensibilidad: config ×0.6 (DoG) / ×0.2 (Hyperdeath). [A.2]
16. **DEBUG WIREFRAME** (robo directo de Calamity): flag que dibuja los quads en verde
    lima para validar juntas mitre en desarrollo — 10 líneas de código. [A.1]

---

# SECCIÓN E — URLs CONSULTADAS (y fuentes locales)

**Terraria / tML:**
- https://calamitymod.wiki.gg/wiki/Dimension-Tearing_Disk (arma "tears space", 725 dmg, nebula beam 70%)
- https://terrariamods.wiki.gg/wiki/Wrath_of_the_Gods (addon Nameless Deity / Avatar of Emptiness; contenido "Rift Eclipse")
- https://github.com/CalamityTeam/CalamityModPublic (clone local: PrimitiveRenderer, HyperdeathRiftScepterBeam, DeathsAscensionRift, texturas medidas)
- https://github.com/absoluteAquarian/GraphicsLib (librería primitivas GPU tML)
- https://forums.terraria.org — "tModLoader — Complicated question about visual effects" (ago-2023, demanda de "tears in space")
- https://docs.tmodloader.net (ModProjectile: PreDraw/diagonal lasers)

**Técnica de líneas continuas (XNA/MonoGame/WebGL):**
- https://infinitecanvas.cc/guide/lesson-012.html (LECCIÓN MAESTRA: extrusión, linejoin miter/bevel/round, strokeMiterlimit 4, AA feather 1px, regl-gpu-lines 134 verts/segmento, 9 vértices fijos en shader)
- https://community.monogame.net/t/line-drawing/6962 (1×1 estirado con SpriteBatch)
- https://stackoverflow.com/questions/270138/how-do-i-draw-lines-using-xna
- https://gamedev.stackexchange.com/questions/26013/drawing-a-texture-line-between-two-vectors-in-xna-wp7 (Cloudflare-blocked, citado por snippet)
- https://www.david-amador.com/2010/01/drawing-lines-in-xna (LineBatch, 1×1 blanco estirado)
- https://bayinx.wordpress.com/2011/11/07/how-to-draw-lines-circles-and-polygons-using-spritebatch-in-xna
- https://realtimevfx.com — "[Niagara 4.25] Ribbon Trail Mini Tutorial" (ribbons = strip continuo, no quads sueltos)

**VFX de juego / color / staging:**
- https://blog.beyond-fx.com/articles/level-up-episode-3-hell-rift-effects-tutorial (hell rift Diablo: fases con "Anticipation" y "Parallax Effect" explícitas)
- https://www.youtube.com/watch?v=T_FD9zNjNVE (video del mismo tutorial)
- https://80.lv/articles/a-reality-breaking-portal-effect-made-in-unity/ (Desolus de Mark Mayers — "space-shattering portal")
- https://nexus.leagueoflegends.com — "/dev: League's VFX Style Guide" (claridad > clutter; color = gameplay)
- https://www.vfxapprentice.com — "How to master good timing in VFX and animation" (timing antes del arte)
- https://sunstrikestudios.com — "Timing in Animation: Principles, Charts & Game-Ready" (single-frame visuals)
- https://apexlegends.fandom.com / apexlegends.wiki.gg (Wraith: mundo en escala de grises en el vacío)
- https://genshin-impact.fandom.com/wiki/Abyss (vacío sin vida ni luz)
- https://leagueoflegends.fandom.com (Vel'Koz / Vacío)
- https://doom.fandom.com (portales/hell rifts Doom Eternal)

**Física de fractura:**
- https://ceramics.org — "Video: Speed of cracks in glass" (1.458 m/s medidos a 78.000 fps)
- https://www.structuremag.org — "Crack Patterns Tell the Story of Glass Breakage"
- https://www.researchgate.net — "Fast Fracture in Tempered Glass" (~1.500 m/s máximo)

**Fuentes locales del proyecto:** `/tmp/calamity` (clone Calamity público),
`research/estrategia_v626/INFORME_DESGARRO_REALIDAD.md` (v1, Task 42-b),
`MEDIDAS_TEXTURAS.txt` y `busquedas_web/` (esta carpeta).

---

# RESUMEN EJECUTIVO (para el agente principal)

**El bug de los huecos es la TEXTURA, no la geometría**: `TrailGlow.png` degrada su alpha
3→204 A LO LARGO y es cian → cada junta = hueco + mancha azul. La solución verificada en
el ecosistema (Calamity, medida en píxeles): textura UNIFORME a lo largo + gradiente solo
transversal + sin color horneado (2 texturas nuevas procedurales: RiftLip gaussiana y
RiftCore banda dura), la fase RECTA dibujada como UN SOLO quad estirado (Calamity estira
3000 px en un draw), y la fase FRACTURADA con segmentos miter (`miterLen = halfW/dot(m,n)`,
límite 4, anchura evaluada en el vértice) + perlas de junta en los giros. El staging que
pide el usuario tiene respaldo físico y de diseño: telegrafo → golpe 3-4 t → línea viva →
**vibración 10-20 t (8-12 Hz, 0→3 px) → FRACTURA EN 1-2 FRAMES (el vidrio se agrieta a
1.458 m/s: un frame) con pico de daño ×1.5-2 e inmunidad local 12-15 t** → grieta
persistente con el daño OFF 8 t antes de morir (MEAC). Paleta casa ✓ ya conviene con el
ecosistema (violeta/cian/blanco, interior negro 0.62·w dentro del rango Calamity 0.7-0.8).

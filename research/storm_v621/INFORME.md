# INFORME DE INVESTIGACIÓN — v6.21 · LA TORMENTA
## Rayos reales: estudio profundo y metódico de 4 mods de referencia

**Fecha:** v6.21 · **Método:** clonado íntegro de los repositorios fuente + lectura
de código línea a línea + análisis VLM de las texturas de rayo reales + síntesis
propietaria (re-implementación 100% propia, sin copiar código ni citar fuentes
externas en los archivos del mod).

**Repos estudiados (clonados en `/tmp/research/`):**

| Mod | Repositorio | Tamaño | Archivos con rayos |
|---|---|---|---|
| Coralite | CoraIite/Coralite-Mod | 360 MB | 78 |
| Everglow | CycloneClub/Everglow | 658 MB | 34 |
| Wrath of the Empress | LucilleKarma/WoTE | 16 MB | 25 (bolts/lanzas) |
| Lunar Veil (linaje Stellamod) | AzaleaThePhantomWitch/LunarVeil | 27 MB | 3 + 23 shaders |

Los 4 informes completos de los agentes de investigación viven en el worklog
(Task IDs 37-a … 37-d). Este documento es la SÍNTESIS operativa: qué técnicas
adoptamos, con qué parámetros medidos, y cómo se materializan en NUESTRAS
librerías.

---

## 1. EL HALLAZGO CENTRAL: por qué nuestros rayos no parecían rayos

Nuestro `LightningCore` (v6.18) dibuja segmentos con la textura **SoftGlow**
(radial, suave, borrosa) estirada como cápsula. El resultado es una "tira de
energía difusa" — correcta para arcos de agujero negro, PERO un rayo de verdad
es LO CONTRARIO: **un filamento BLANCO nítido y delgado dentro de un halo
tenue**, con grietas y segmentos que se mueven.

Análisis VLM de las texturas de rayo reales del mod de referencia (el jefe
eléctrico de Coralite, `ThunderTrail`/`LightingBody`):

| Textura real | Perfil medido |
|---|---|
| Halo (cuerpo blando) | banda gaussiana SUAVE: núcleo ~20-25% del ancho, cae a ~20% de brillo al 50% del ancho, negro en los bordes; bordes ondulantes de baja frecuencia a lo largo |
| Núcleo (el rayo) | banda de ALTO CONTRASTE: el núcleo blanco (30-40% del ancho) **se desplaza verticalmente a lo largo de la tira** (el filamento ondula DENTRO de la textura), caída brutal (negro al 60-70%), **grietas/segmentos de alta frecuencia a lo largo** |
| Cadena | óvalos brillantes discretos espaciados regularmente unidos por un puente tenue |

**Conclusión de diseño:** la nitidez NO vive en la geometría — vive en la
TEXTURA. Necesitamos texturas de banda con perfil de filamento (no glows
radiales) y variación de alta frecuencia a lo largo.

## 2. LAS 12 LECCIONES SINTETIZADAS (de los 4 mods)

1. **PARPADEO CON APAGADO INTERMITENTE (~15 Hz, ~40-50% de frames apagado)** —
   el factor #1 del look real. El rayo se re-genera cada 4-6 ticks Y además
   tiene probabilidad de no dibujarse un frame entero.
2. **EXTREMOS PERFECTAMENTE ANCLADOS** — el zigzag es solo interior; origen y
   destino se tocan EXACTO. El desplazamiento se anula cerca de los anclajes
   (envolvente o fórmula `(1-(2t-1)^6)`).
3. **JITTER PERPENDICULAR + DISPERSIÓN 2D** sobre una polilínea base (no solo
   perpendicular: también una dispersión circular pequeña por punto).
4. **SUPERPOSICIÓN MULTI-FILAMENTO** — 2-3 rayos paralelos con 2 colores de la
   paleta y semillas independientes (más rico y más barato que un solo path
   ramificado).
5. **DOBLE/TIPLE TIRA: halo ancho de color + cuerpo + NÚCLEO BLANCO a ~¼ del
   ancho** — el filamento caliente SIEMPRE blanco.
6. **CRACKLE POR SEGMENTO** — brillo no uniforme a lo largo (variación de alta
   frecuencia en la textura y/o por sub-segmento), con "nodos" más brillantes.
7. **MUERTE VIOLENTA** — al disolverse, el rango de jitter y la dispersión
   CRECEN (el rayo "revienta") mientras el alpha baja con easing cuadrático.
8. **NACIMIENTO POR CRECIMIENTO** — el rayo se materializa a lo largo de su
   trayectoria con ancho creciente; nunca aparece completo de golpe.
9. **IMPACTO = FLASH MULTI-DRAW** — redibujar la misma geometría N veces
   decrecientes + cruz de luz (2 orientaciones × 2 escalas) + gorros de glow a
   2 escalas apiladas en los extremos.
10. **TAPER** — funciones de ancho a lo largo: senoidal (grueso al centro),
    lineal, raíz (grueso en el impacto). El ancho es un CONTRATO (delegado/enum).
11. **LUZ ESTRANGULADA** — `Lighting.AddLight` solo cada N píxeles del camino y
    no cada frame; el daño SIEMPRE en la LÍNEA RECTA (el jitter es cosmético) →
    determinismo MP gratis.
12. **TELEGRAPH** — línea fina de aviso + anillo objetivo antes del golpe; el
    daño y el movimiento se activan al terminar el telegraph.

## 3. QUÉ ADOPTAMOS DE CADA MOD (y qué NO)

| Fuente | Adoptamos | Descartamos (por nuestra pila SpriteBatch) |
|---|---|---|
| Coralite | flicker 50% a 15 Hz · jitter perpendicular+circular · multi-filamento · muerte expansiva · gorros 2 escalas · textura filamento · daño en línea recta | vertex strips TriangleStrip con struct propio (nuestros quads ya funcionan y evitan el bug de batch) |
| Everglow | árbol de ramas con AUTO-CORRECCIÓN de curvatura (`rot -= totalRot*0.3`) · "hervir" de todos los puntos · flash multi-draw · ramas ×0.5 de ancho prob 1/9 | pipelines de post-proceso/bloom de pantalla (peso) · compilación automática de .fx |
| WoTE | cruz de luz 4-draw en impactos · telegraph + gate de daño · paletas por datos | render targets de pixelación · shaders de primitivas |
| Lunar Veil | sándwich de batch Immediate (valida nuestro contrato v6.10) · puntas redondeadas (endcaps) · extraUpdates para densidad | reflexión sobre MiscShaderData · IL hooks |

## 4. MATERIALIALIZACIÓN: LAS PIEZAS NUEVAS DE v6.21

### 4.1 Texturas procedurales nuevas (`tools/gen_storm_textures_v621.py`)

| Archivo | Tamaño | Diseño (RGB blanco + alpha = perfil) |
|---|---|---|
| `Effects/Procedural/BoltHalo.png` | 256×64 | banda gaussiana suave (σ≈0.28), bordes ondulantes de baja frecuencia a lo largo |
| `Effects/Procedural/BoltCore.png` | 256×64 | EL FILAMENTO: núcleo blanco que **serpentea verticalmente** (paseo aleatorio con reversión a la media, acotado al 30% central), caída gaussiana estrecha (σ≈0.16), **grietas de alta frecuencia** a lo largo (brillo 0.55..1.0) + nodos brillantes |
| `Effects/Procedural/BoltChain.png` | 256×64 | cadena: óvalos gaussianos discretos (~cada 36 px) sobre puente tenue (σ_v≈0.12) |
| `Effects/Procedural/BoltImpact.png` | 96×96 | estallido: punto caliente central + 7 rayos radiales finos de longitudes desiguales |

### 4.2 La nueva librería `Content/VFX/StormLib.cs` — "LA SEGUNDA GENERACIÓN"

`LightningCore` (v6.18) queda intacta para la familia de agujeros negros.
`StormLib` nace de esta investigación y es la librería de rayos de verdad:

- **Reloj**: `FlickTick(time, hz=15)` + `IsLit(seed, flick, 0.62)` (lección 1).
- **Generación determinista** (hash propio, sin Main.rand → mismo rayo en todas
  las máquinas):
  - `ZigPath` — perpendicular + dispersión circular + envolvente, extremos
    anclados (lecciones 2-3), con `amp` multiplicable por la muerte (lección 7).
  - `Boil` — deriva de TODOS los puntos por tick (Everglow).
  - `ForkTree` — ramas con auto-corrección de curvatura, ancho ×0.5, prob 1/9
    (Everglow) → lista de strands.
  - `MultiStrand` — 2-3 filamentos paralelos, 2 colores (lección 4).
- **Render** (contrato LightningCore: batch ABIERTO aditivo, anidable):
  - `Strand` — triple capa por sub-segmento (~40 px): halo (×2.4 ancho,
    BoltHalo) + cuerpo (BoltCore) + núcleo blanco (×0.30, BoltCore); brillo de
    crackle por sub-segmento; taper por enum (lecciones 5-6, 10).
  - `Bolt` / `MultiBolt` / `ChainBolt` — atajos.
  - `ImpactFlash` — cruz de luz 4-draw + destello apilado (lección 9).
  - `EndCap` — gorros a 2 escalas.
  - `ArcRing` — arco eléctrico re-implementado con las texturas nuevas.
- **Escena**: `AddLightAlong` (cada 48 px, estrangulada — lección 11).

### 4.3 El arma reconstruida: `RunicLightning` → RAYO DEL CIELO

El v6.19 disparaba una línea horizontal borrosa. v6.21 lo reconstruye como un
**RAYO DE VERDAD que CAE DEL CIELO** sobre el cursor (la firma del jefe
eléctrico de Coralite + el hechizo de trueno de Everglow):

1. **TELEGRAPH (10 ticks)** — línea fina del cielo al objetivo + anillo
   pulsante (lección 12; el daño llega SOLO al final del telegraph).
2. **EL GOLPE** — daño en la LÍNEA RECTA cielo→suelo (columna) + estallido
   radial en el impacto + CADENA a 3 enemigos (60%) + Electrified.
3. **EL RAYO** — MultiBolt de 3 filamentos (oro + azul estelar + blanco) con
   ForkTree de ramas, flicker 15 Hz con apagado, `DeathGrow` que REVIENTA el
   jitter al morir, flash de impacto apilado + cruz de luz + onda de choque +
   estallido de chispas + cámara + trueno grave (zap con pitch −0.5) + zaps.
4. **SIN MANÁ** (todos los bastones del mod son de prueba — regla del usuario).

### 4.4 El fix de los soles rúnicos (bug v6.20 residual)

`RuneSunRenderer` usaba `SpriteSortMode.Deferred` en sus `Begin*` → **el pase
de shader se ignora** (el batch enlaza su propio efecto al hacer flush) → el
`DendriticNoise` se pintaba CRUDO: "un cuadrado con textura, sin animación"
(exacto lo que reportó el usuario). El sol original usa `Immediate` en TODOS
sus pases. Fix: los dos helpers pasan a `SpriteSortMode.Immediate` con
`LinearWrap` (idénticos al sol original) → el disco de plasma vuelve a ser una
esfera animada y el aura vuelve a ser el resplandor de ruido.

## 5. REGLA DE HONESTIDAD

Ninguna línea de código de los 4 mods se copia. Las técnicas (jitter
perpendicular, flicker, multi-filamento, auto-corrección, tapers, perfiles de
textura) son ALGORITMOS re-implementados desde cero sobre nuestra pila
(SpriteBatch + texturas procedurales + hash determinista). Los archivos del mod
no citan fuentes externas: todo es creación propia de AethonMod.

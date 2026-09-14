# INFORME DE INVESTIGACIÓN — MEAC (Emperatriz de la Luz rework)
## v6.22 · El mod chino "MEAC - 原版内容重置 demo" (yiyang233) — adquirido y analizado

**Método:** descarga del .tmod archivado (modsbase/obstorage) + parser propio
del formato binario (header: `tMOD` + str7 versión + SHA1 20B + firma 256B +
uint32 dataLen + str7 nombre + str7 versión + uint32 fileCount + tabla
[path str7, int32 size, int32 csize] + blobs DEFLATE crudos concatenados) +
decompilación ILSpy del MEAC.dll (253 archivos C#) + conversión de las
.rawimg (int32 w, int32 h, RGBA) + análisis numérico de paletas + VLM.

**NOTA LEGAL/DE DISEÑO:** como con todos los mods estudiados, NADA de este
código se copia al proyecto — se extraen las TÉCNICAS (parafraseadas abajo)
y se re-implementan 100% con nuestra pila. El decompile queda en /tmp (no
entra al repo).

---

## 1. QUÉ ES MEAC

Rework de contenido vanilla ("原版内容重置" = "vanilla content reset"), famoso
por su Emperatriz de la Luz ("羲和" Astraeus) con efectos de luz espectaculares.
183 archivos: MEAC.dll (659 KB), shaders .xnb (Fire, RainbowLaser, KnifeLight,
Sun, DrawPrim, Colorize, Screen/Cosmic, Screen/EoLWarp...) e imágenes .rawimg.

## 2. LAS 12 LECCIONES DE LUZ (del código decompilado)

1. **EL COLOR ES UN LUT DE 1×256 MUESTREADO** — `rainbow.rawimg`: un
   arcoíris HSL S=100% L=50% completo (255,0,93 → 219,0,255 → 0,53,255 →
   0,255,250 → 0,255,58 → 220,255,0 → 255,101,0). El color de cada
   proyectil = muestrear la tira por hue. Validación NUMÉRICA de nuestro
   `LumenLib.Hue(h, 0.5, 1.0)` — misma matemática.
2. **EL HUE VIVE EN ai[0] EN GRADOS** — las lanzas/láseres nacen con
   `ai[0] = Main.timeForVisualEffects * 3 % 360` (3°/tick = 180°/s: el
   arcoíris de la volea ENTERA cicla rápido) y cada proyectil pinta
   `HsvToRgb(ai[0] % 360)`. Nuestro `Drift()` es la versión lenta (0.2-0.6
   hue/s de WoTE) — MEAC confirma el rango rápido para voleas sincronizadas.
3. **LA LANZA = CUERPO SPRITE + TRIÁNGULO DE 2500px** — además del sprite
   alargado, un `DrawUserPrimitives` con 2 vértices en el proyectil (±2.5px
   perpendiculares, color pleno) y 1 vértice a 2500px delante
   (`Color.Transparent`) — el "laser body" de un solo triángulo con el
   MagicPixel + el shader Colorize. Nuestro `LumenLib.Ray` reproduce el
   concepto con quads de bloom estirados (mismo look, sin primitivas).
4. **8 AFTERIMAGES CON SQUASH** — la estela = 8 copias del sprite a
   `-velocity * 0.8 * i` con alpha `(0.6 - i/15)` y escala Y escurrida
   (`1.2` hostil / `0.5` aliado) — las copias se APLASTAN al 50% en Y:
   leen como "rasguño de luz" en vez de "cola de sprites". LumenLib ya
   tenía el growth ×1.4 de la Emperatriz vanilla; el squash Y es la
   variante MEAC.
5. **DOBLE DIBUJADO SIEMPRE** — cuerpo en pase aditivo + `DrawBloom()`:
   la MISMA textura otra vez con `A = 0` (color puro, alpha 0) en aditivo
   — el truco del "bloom copy" barato que dobla la sensación emisiva sin
   textura de glow. Es exactamente nuestra DOBLE PASADA universal.
6. **EL FUEGO ES UN LUT DE 40 NIVELES** — `Fire_Color.rawimg` (1×40):
   transparente → (255,102,0) → (255,186,0) → (250,244,79). Solo 3-4
   niveles visibles (¡frente a nuestros 37!) — MEAC simplifica; nuestra
   curva de 37 con puntos de control es MÁS RICA que la suya.
7. **LAS TIRAS 1×N SON LA TEXTURA UNIVERSAL** — Laser/Star/Trail/Ball =
   tiras negras RGB(0,0,0) de perfil plano (alpha 255 en el cuerpo, 0 en
   los 8 px extremos): TODO el color lo pone el shader Colorize/tinte en
   runtime. Lección: perfiles DUROS (no gaussianos) + tinte = el look
   "cuchillo de luz" nítido de MEAC (nuestro BoltCore ya usa caída dura).
8. **WARP DE PANTALLA** — `GlobalVisuals/EoLWarp`: un GlobalVisual con
   screen targets (RT1) que re-dibuja la pantalla con el shader
   Screen/EoLWarp y un cross-fade gris (`Color.White * gray` /
   `Color.White * (1-gray)`) — la distorsión de cristal alrededor del
   jefe. (Nosotros YA tenemos BlackHoleLensSystem para esto.)
9. **EL JEFE SE DIBUJA A PUNTOS CLAMP** — el NPC usa
   `SamplerState.PointClamp` + `DepthStencilState.Default` en sus pases
   y `Main.timeForVisualEffects / 3` para el frame de alas (frame cada 3
   ticks) + DrawData oficial para las alas tras el cuerpo.
10. **EFECTOS COMO ENTIDADES** — `VisualEffect.Create<EOLEff>(pos, vel)`
    con `owner = npc.whoAmI`: un pool de efectos visuales ligados al dueño
    (nuestro ParticleManager es equivalente y más rico).
11. **LAS VOLEAS NACEN CON HUE DECALENDO** — las lanzas de la ráfaga
    reciben `Main.timeForVisualEffects * 0.02f` como hue de arranque
    (offset temporal) → cada lanza de la volea tiene el color JUSTO
    desfasado del vecino (el "abanico arcoíris").
12. **PANTALLA Y CIELO PARTICIPAN** — carpeta Sky/ + shaders Screen/ —
    el cielo cambia con el jefe y hay 12+ shaders .fx propios (Fire,
    RainbowLaser, KnifeLight, Sun, DrawPrim, Colorize, Warp...).

## 3. VEREDICTO PARA LumenLib

Nuestra librería de luz v6.22 ya cubre el 90% del vocabulario de MEAC
(hue-HSL por identidad con drift, doble pasada, bloom apilado, estelas de
fantasmas, rayos estirados, telegraph) — y en DOS puntos somos más ricos:
la paleta de fuego de 37 niveles (vs su LUT de 3-4) y el bloom apilado
invertido de 4 capas (vs su simple doble-dibujado). Lo único que MEAC tiene
que nosotros tratamos de otra forma: el warp de pantalla (nosotros lo
hacemos con la lente gravitacional del BlackHoleLensSystem, aplicada a
todos los agujeros) y las primitivas de triángulo (nuestros quads rotados
evitan el riesgo de estado de GPU).

SIN COPIAR NADA: todas las técnicas re-implementadas con nuestra pila.

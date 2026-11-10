# LA SUPERFORJA — Las 3 herramientas de los reels + las 7 nuevas de la investigación, todas en una

> **La letra**: «une las 3 herramientas en una, ademas busca mas herramientas similares y de utilidad que creas que nos puedan ser utiles y crea una super herramienta con todas ellas»

---

## v6.121 — LA FUSIÓN: LA FORJA + EL TALLER = LA SUPERFORJA (una sola)

> **La letra**: «une La Forja y El Taller — que se mencionen por separado es molesto» +
> «dime para que sirve cada una».

**PARA QUÉ SERVÍA CADA UNA** (la respuesta):

| | Qué era | Qué hacía |
|---|---|---|
| **LA FORJA** (v6.95, `taller_pixelart.py`) | La cadena CORTA de los 3 reels | LA PLUMA (pxl: el borrador IA con la paleta del libro) → EL MOLDE (sprite-gen: cutout, rejilla, paleta, contorno Terraria, highlight) → EL ALMA (pixelfx: el efecto de píxel). Forjó el Altar de la Caída y las 20 armas dignas. |
| **EL TALLER** (v6.98, `superforja.py`) | La Forja AGRANDADA — LA SUPERFORJA | El mismo corazón (la cadena corta VIVE dentro: el comando `forjar`) rodeado de las SIETE herramientas nuevas de la investigación: EL PUENTE, EL ESPEJO, EL TAMBOR, LA RAMPA, EL ATLAS, LA REFINA, EL OJO, EL CORTE. |

**LA FUSIÓN**: desde la v6.121 hay **UNA SOLA HERRAMIENTA — LA SUPERFORJA**
(`tools/superforja.py`). `tools/taller_pixelart.py` YA NO es una herramienta
separada: es el **ALIAS DE COMPATIBILIDAD** — las recetas viejas que llaman «la
forja» o «el taller» siguen funcionando y aterrizan en la Superforja (con su
carpeta `forja_<n>` respetada). Un nombre, una casa, cero pérdida.

---

## v6.121 — EL ALMA ES PIXELFX DE VERDAD (copiado y adaptado)

> **La letra**: «revisa que la forja o el taller tenga el codigo de este repo
> (github.com/patch-gg/pixelfx), asegurate de que no pierda calidad de ninguna
> manera, copia y adapta, y tambien mejora para nuestro juego».

**LO COPIADO Y ADAPTADO — la arquitectura de los 244 efectos** (su corazón):
1. **EL DOBLE BÚFER**: un búfer de COLOR y un búfer de LUZ — la luz se compone
   **EN ADITIVO** (premultiplied alpha: rgb·a se SUMA, el alfa se MAXEA): el
   brillo respira ALREDEDOR del sprite SIN lavarlo jamás.
2. **EL LIENZO EXTENDIDO**: los efectos viven en el espacio LOCAL alrededor
   del origen (pad de 26 px) — coronas, halos y anillos respiran fuera del
   bounding box.
3. **draw(p, t, o)**: cada efecto, UN TROCITO DE CÓDIGO determinista que
   pinta el frame t — la firma de su API.
4. **CERO BLUR**: pixel PURO — la luz es CRISP (la lección del pilar).

**LO MEJORADO PARA NUESTRO JUEGO**: los 8 efectos nuevos salen de SUS
categorías, vestidos con la paleta del vacío (violeta 150,84,235 · oro
255,214,130):

```
LOS 17 EFECTOS DEL ALMA (python3 tools/superforja.py alma --efecto <nombre>):
  los 9 de siempre: brasa · humo · glitch · consagracion · fuego · hielo ·
                    rayo · escudo · portal           (INTACTOS, píxel a píxel)
  los 8 de pixelfx: eclipse (shadow: LA FIRMA DEL MOD) · sigilo (hex sigil) ·
                    agujero (black hole) · orbe (arcane orb) ·
                    disolucion (dissolve burn) · aura (halo) ·
                    destello (hit flash) · runas (magic circle)
  QA VLM: SHIP — «eclipse, orbe y destello leen muy bien, paleta
          violeta+oro consistente y píxeles limpios»
```

---

**El comando**: `python3 tools/superforja.py <comando> — TODO local, sin nube (la regla de sprite-gen).**

---

## EL MAPA — 12 comandos, una casa

```
LA CADENA DE LA SUPERFORJA (el comando `cadena` — TODO en uno):

  LA PLUMA → EL PUENTE → EL MOLDE → EL ESPEJO → EL ALMA → EL ATLAS → EL MONTAJE → EL OJO
  (borrador   (la rejilla   (cutout →    (la tira     (el      (el       (todo      (la nota
   IA con QA   nativa        rejilla →    animada      efecto   empaque    junto      y los
   + la refina restaurada)   paleta →     procedimen-  de       + mani-    para el   defectos:
   quirúrgica)                contorno →   tal)         píxel    fiesto)   ojo)      listos
                              highlight)
```

| Comando | La idea (de dónde viene) | Qué hace |
|---|---|---|
| `pluma` | **pxl** (reel 3 — pixel art as code) | El borrador IA con la paleta del LIBRO y el fondo croma; el método «pintar → mirar → refinar» |
| `refina` | el paso «refinar» de pxl, ahora **quirúrgico** | La EDICIÓN DIRIGIDA: «la hoja más larga» cambia SOLO eso — el resto del dibujo queda intacto (image-edit) |
| `puente` | **restore-pixel-art / fix-pixelart** + los escaladores xBR-hqx de la investigación | Detecta la REJILLA NATIVA del render IA (la autocorrelación de bordes + la pureza de celdas) y restaura el pixel art REAL píxel por píxel · + EL ESCALÓN 2× de la casa (los bordes que se conservan) |
| `molde` | **sprite-gen** (reel 2 — una imagen → sprites jugables) | cutout flood-fill → rejilla por celdas → paleta del LIBRO → contorno Terraria → halo del conjuro → highlight tML |
| `espejo` | **framelet** (static sprites → sprite sheet) + la técnica del Vástago del Eterno (.91) | El sprite estático → TIRA ANIMADA procedimental: `vida` (respirar+vaivén) · `respirar` (squash&stretch) · `flotar` (bob+inclinación) · `latir` (pulso de brillo) · `vaivén` (balanceo) → tira vertical tML + manifiesto + GIF |
| `alma` | **pixelfx** (reel 1 — 244 efectos, cada uno un trocito de código) + los 25 VFX libres de la investigación | El preview VIVO con **9 efectos**: brasa · humo · glitch · consagración · **fuego** · **hielo** · **rayo** · **escudo** · **portal** |
| `tambor` | los generadores de **dithering** de la investigación | Bayer 4×4/8×8 ordenado y Floyd-Steinberg por difusión de error, a la paleta de la casa o a N colores de la imagen |
| `rampa` | **palette.joannahosking / Huetrivo** (la ley del hue-shift) | Del color base a la RAMPA COMPLETA: sombras al frío, luces al cálido, máximo N° — PNG de muestras + HEX listos para el prompt |
| `atlas` | **MaxRects** (soimy/maxrects-packer · PyTexturePacker) | Frames sueltos → TIRA VERTICAL tML (la convención del juego) + ATLAS COMPACTO (guillotina por mejor lado) + DETECCIÓN DE DUPLICADOS por huella + manifiesto JSON |
| `ojo` | el QA VLM de la casa, ahora DENTRO de la cadena | El paso «mirar»: nota 0-10 y defectos concretos sobre el montaje — la entrada perfecta para `refina` |
| `forjar` | LA FORJA v6.95 (la cadena corta, compatibilidad) | pluma → molde → alma → montaje (lo que forjó el altar y las 20 armas) |
| `corta` | **EL CORTE** (v6.98 — la ley de la transparencia) | EL CUTOUT UNIVERSAL: CUALQUIER fondo se va (no solo el croma verde — el azul-noche `#183078` y el casi-negro de un render también): el color se huele en el ANILLO del borde · LA INUNDACIÓN CON CORREA (BFS global + degradados locales, con correa — nunca come el dibujo) · EL DES-FRINGE de 2 anillos (el borde anti-aliased des-mezclado: sin halos jamás) · EL PEINE (islas diminutas oscuras barridas) · `--cielo` (los fondos estrellados: las islas brillantes rodeadas de nada también se van) · `--frames N` (tiras de N marcos — el altar) — y LA CADENA termina SIEMPRE aquí: nada sale de la forja con fondo |
| `cadena` | **LA SUPERFORJA** (la unión total) | TODO: pluma → puente → molde → espejo → alma → atlas → montaje → ojo |

---

## LA LEY DE LA TRANSPARENCIA (v6.98) — EL CORTE

```bash
# el sprite que salió con fondo oscuro (la letra: «todos los sprite
# deben tener fondo transparente»):
python3 tools/superforja.py corta --imagen sprite.png --preview
#   → fondo #183078 (cobertura del borde 98%) · quitado 78,1%
# la tira de N frames (el altar 36×216):
python3 tools/superforja.py corta --imagen altar.png --frames 4 --preview
# el fondo estrellado (las estrellitas flotantes también se van):
python3 tools/superforja.py corta --imagen libro.png --tolerancia 34 --cielo
```

LAS TRES LEYES: (1) el fondo se huele en el anillo del borde — la moda
de sus celdas cuantizadas, sea del color que sea; (2) la inundación
con correa — un píxel entra si se parece al fondo (≤ tolerancia) O a su
vecino ya aceptado (≤ 11, los degradados) SIN soltarse del fondo
(2,2× la tolerancia — jamás se come el dibujo); (3) el des-fringe — el
anillo de 2 píxeles que toca el fondo se des-mezcla: cuánto SUJETO
lleva el color observado es su alfa, y su color es el sujeto puro.

## EL CICLO DE LA CASA — pintar → mirar → refinar → re-forjar

```bash
# 1) LA CADENA COMPLETA: de cero al QA (el arma nueva de la casa)
python3 tools/superforja.py cadena \
    --nombre mialema --prompt "una espada ritual de tinta azul-noche" \
    --ancho 36 --alto 44 --efecto fuego --animacion vida --frames 6 --qa

# 2) EL OJO dijo «ruido en el fondo» → LA REFINA quirúrgica (no se regenera NADA)
python3 tools/superforja.py refina --nombre mialema \
    --nota "remove background noise, clean solid green background"

# 3) re-forjar con el borrador refinado → el QA sube (verificado: 7/7/6/8 → 9/8/7/9)
python3 tools/superforja.py cadena --nombre mialema2 \
    --borrador /tmp/superforja_mialema/borrador_refinado.png \
    --ancho 36 --alto 44 --efecto fuego --animacion vida --frames 6 --qa
```

## LAS HERRAMIENTAS SUERTAS (una por una)

```bash
# LA RAMPA — la paleta de las tres casas antes de pintar nada
python3 tools/superforja.py rampa --base "#D89030" --pasos 7 --hue 24
#   → #380D04 #57220B #763F14 #956321 #B48335 #D3A54D #F2C96A (dorado: marrón→oro)

# EL TAMBOR — el velo de los degradados en paleta contada
python3 tools/superforja.py tambor --imagen cielo.png --matriz bayer8
python3 tools/superforja.py tambor --imagen cielo.png --matriz floyd --colores 6

# EL PUENTE — el render IA viene con la rejilla SUCIA: se restaura solo
python3 tools/superforja.py puente --nombre x [--x2 2]     # + el escalón 2^2

# EL ESPEJO — el objeto mágico que late, la mascota que respira
python3 tools/superforja.py espejo --nombre x --animacion latir --frames 4
#   → tira.png (tML AnimationFrameHeight) + manifiesto + ciclo.gif

# EL ALMA — los 5 efectos nuevos de la investigación VFX
python3 tools/superforja.py alma --nombre x --efecto portal --tira

# EL ATLAS — los frames de la bolsa: tira del juego + atlas compacto + duplicados fundidos
python3 tools/superforja.py atlas --nombre x --maxrects

# LA PLUMA sola + LA REFINA inmediata
python3 tools/superforja.py pluma --nombre x --prompt "..." --refinar "más dorado"
```

---

## LAS VERIFICACIONES DE LA SESIÓN (todo probado de verdad)

- **EL PUENTE — 7/7 pruebas**: x3/x4/x6/x8 limpios detectados exactos · el x3 con bordes sucios (patológico y realista) detectado · el nativo x1 NO detecta nada (correcto) · la restauración del sucio realista recupera el original **PÍXEL POR PÍXEL (0.0% distintos)**.
- **LA RAMPA**: dorado `#380D04→#F2C96A` · teal `#103138→#8BF2A2` · violeta `#1A0838→#DC75F2` — el hue-shift acotado que la primera versión no tenía (curada en vivo).
- **EL TAMBOR**: bayer4 / bayer8 / floyd sobre el gradiente tinta→fucsia, a los 12 colores de la casa.
- **EL ESPEJO**: ciclo `vida` de 6 frames + `ciclo.gif` — QA VLM: «el vaivén y el squash se perciben naturales, silueta consistente».
- **EL ALMA (los 9)**: fuego y portal re-diseñados tras el primer QA (5.5/10 «parece ruido») → **7/10** «la frontera ondula claramente, la espiral gira continua y coherente» → ajuste final (el objeto legible bajo las llamas, más teal en el portal).
- **LA REFINA**: la llamada directa al endpoint de la casa (`images: [{url}]` — el CLI manda `image` y la API de esta era lo rechaza: el formato documentado por la skill de edición).
- **EL CICLO COMPLETO**: el cáliz dorado de cero (QA 7/7/6/8, «ruido en el fondo») → LA REFINA → re-forja (QA **9/8/7/9**) — el método pxl hecho herramienta.
- **EL OJO**: el veredicto limpio extraído del JSON (antes el .txt guardaba el stdout crudo).
- **EL CARTEL** (todo junto): QA VLM 8/10 «muy coherente y útil: cubre el pipeline real de un mod de Terraria; lo mejor, los efectos de alma y las rampas bien calibradas».

## LA LEY DE LA CASA (por qué cada pieza es nuestra)

Cada herramienta recrea **la IDEA**, no el código ni las texturas de nadie: la autocorrelación de bordes y la pureza de celdas de EL PUENTE · el escalón de bordes conservados · la guillotina por mejor lado de EL ATLAS · las matrices Bayer y la difusión de error de EL TAMBOR · el hue-shift de LA RAMPA — todas escritas aquí, en Python de la casa, sobre las funciones que ya produjeron los sprites vivos del mod (`gen_altar_v695.py`: la fuente de verdad). El mod sigue siendo 100% propiedad exclusiva (LICENSE propietaria).

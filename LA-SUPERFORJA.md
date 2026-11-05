# LA SUPERFORJA — Las 3 herramientas de los reels + las 7 nuevas de la investigación, todas en una

> **La letra**: «une las 3 herramientas en una, ademas busca mas herramientas similares y de utilidad que creas que nos puedan ser utiles y crea una super herramienta con todas ellas»

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
| `cadena` | **LA SUPERFORJA** (la unión total) | TODO: pluma → puente → molde → espejo → alma → atlas → montaje → ojo |

---

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

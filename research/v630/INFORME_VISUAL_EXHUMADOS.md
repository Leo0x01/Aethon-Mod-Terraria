# INFORME VISUAL — LOS DOS EXHUMADOS (v6.30)

> Medido DIRECTAMENTE de los sprites/GIFs oficiales del wiki de Calamity
> (calamitymod.wiki.gg), con PIL/numpy — no adivinado. Referencias descargadas
> en `research/v630/ref/`. Este informe corrige los ERRORES DE PALETA de v6.29.

---

## A. RANCOR — los colores EXACTOS

### A.1 El haz ("The Angy Beam") — medido en el frame 152 del GIF demo (652×350)
- **NÚCLEO: BLANCO PURO (255,255,255)** — sólido, ~230 px de ancho en pantalla
  de 652 px (~35% de la pantalla). El haz cruza TODO el ancho (x 240→613):
  **PERFORACIÓN INFINITA** visual.
- **BORDES/GLow: ROSA-MAGENTA** — media medida **(204, 77, 112)**, rango
  (168..232, 70..134, 91..143). Es PINK/ROSE, NO carmesí-ámbar.
- **v6.29 FALLÓ**: usamos paleta carmesí-ámbar (120,8,30)→(255,140,60). El
  original es BLANCO + ROSA. CORREGIR: paleta Rancor = [ (140,20,60) bordes
  externos, (204,77,112) glow, (255,180,200) labio, (255,255,255) núcleo ].

### A.2 El círculo mágico (Rancor_Magic_Circle.png, 114×114)
- **ESCALA DE GRISES/BLANCO**: (224,224,224) 33% + (192,192,192) 40% +
  (160) 12% + (96) 8% — se dibuja aditivo → **glow BLANCO-PLATA**.
- Densidad radial creciente hasta el borde (r=44): anillo exterior denso +
  estructura interna completa (círculo de transmutación FMA con anillos,
  runas y pentagrama).
- Diámetro en juego ≈ 114 px de sprite (visible a distancia fija del jugador).

### A.3 Los brazos (Rancor_Arms.png, 166×120)
- **SILHOUETTES NEGRAS/ROJAS OSCURAS**: NEGRO (0,0,0) 31% + (32,0,0) 22% +
  (64,32,32) 17% + (64,32,64) 12% + toques (160,32,64) 3%.
- **v6.29 FALLÓ**: los hicimos de "hueso blanco (255,243,228) + halo
  carmesí". Los originales son **BRAZOS ESQUELÉTICOS OSCUROS** (manos-garra
  negras con bordes rojo oscuro) — emergen del tile como sombras.

### A.4 Las ascuas (Rancor_Cinder.png, 4×18)
- Sprite vertical degradado: (224,224,192) punta pálida → (224,160,32) ámbar
  → (224,64,0) base naranja-roja. **Chispas ámbar/naranja** que vuelan.

### A.5 El libro (icono, 66×82)
- Magenta-rosa (192,128,160) 15% + magenta oscuro (96,0,96) 11% + carmesí
  (224,0,32) 5% + bordes negros. Tomo VIOLETA-ROSA con detalles carmesí.

### A.6 Sonidos medidos en el wiki
- Use / Laser Beam / Laser Loop / Arms — hay SONIDO DE LAZO durante el haz
  (loop continuo mientras dispara) + sonido propio de brazos.

---

## B. GRUESOME EMINENCE — los colores EXACTOS

### B.1 La congregación (Spirit_Congregation.gif, 134×142, 18 frames)
- **MASA OSCURA**: NEGRO (0,0,0) **45.8%** + violeta oscuro (32,0,32) 26.4%
  + rojo oscuro (32,0,0) 10.4% → el 83% del sprite es OSCURO.
- **CARAS/OJOS ROJO-NARANJA BRILLANTES**: (253,74,60) + (224,64,32) 5.2% +
  rojo puro (192-224,0,0) ~4.5% — puntos/calaveras brillando DENTRO de la
  masa oscura. Media del sprite: RGB (61,13,32) — casi negro.
- **Acentos menores**: blanco pálido (192,224,224) 0.6% + rosa (224,64,160)
  0.1% (destellos ocasionales).
- Clusters de rojos en frame 9: cara mayor en zona superior-derecha
  (x 85-122, y 30-65) + segunda cara a la izquierda (x 14-24, y 66-75) +
  rests abajo — **VARIAS CARAS brillando dentro de la masa**.
- **v6.29 FALLÓ**: la hicimos "pálida (208,222,226) + interior (84,64,104)"
  con ojos blancos — la original es una MASA NEGRA con caras ROJAS.

### B.2 La abominación final (frames 400-560 del demo, 516×350)
- Los píxeles rojos se concentran en un cluster compacto (~120-250 px de
  región) = UNA CARA grande central cuando está completa; el resto del
  cuerpo sigue siendo masa oscura con las caras menores.

### B.3 El icono/holdout (42×74)
- Cabeza oscura rojo-profundo (32,0,0)+(64,0,0) ~50% + detalles TAN/MARRÓN
  (128,96,64)+(96,64,32) ~20% — es la "Cabeza de Dismas" (Darkest Dungeon):
  una cabeza momificada/separada con cuero viejo.
- **El ítem se sostiene EN LA MANO (holdout)** — se ve la cabeza en la mano
  del jugador mientras se canaliza.

### B.4 Comportamiento medido del demo (623 frames)
- La congregación "loose" (primeros ~380 frames): se mueve ERRÁTICA, con
  sacudidas; solo ~350-400 px rojos visibles (caras dispersas parpadeando).
- Al completarse (frame 400+): los rojos se concentran — LA CARA MAYOR se
  forma en el centro de la masa compacta. Es entonces controlable.

---

## C. SUPREME CALAMITAS "ENCHANTED/EXHUME" — contexto de paleta

- Los ítems "exhumed" nacen del encantamiento EXHUME de la Brimstone Witch:
  Burning Sea → Rancor; Ghastly Visage → Gruesome Eminence.
- Paleta dominante de la fase/temática: CARMESÍ + NEGRO + BLANCO HUESO +
  MAGENTA (la bruja de la piedra de azufre). Los dos ítems heredan eso:
  Rancor = blanco+rosa; Eminencia = negro+rojo.

---

## D. REGLAS DE RAMIFICACIÓN LICHTENBERG / ESPEJO ROTO (números)

### D.1 Figuras de Lichtenberg (descarga en dieléctrico)
- **Rama principal**: recta-ish con desvíos pequeños acumulativos.
- **Ramificaciones laterales**: salen con ángulo **25°-60°** del canal
  principal (moda ~40°), LONGITUD ~0.25-0.45× del canal padre, ANCHURA
  ~0.55-0.7× del padre en la base, afinándose a aguja.
- **Sub-ramas**: de las ramas, con ~0.5× de sus dimensiones, ángulo similar.
- **Densidad**: una rama cada 2-4 segmentos del canal principal, alternando
  lados (como los rayos reales).
- **Micro-fallas**: quiebres bruscos (±20°-40°) ocasionales en el canal.

### D.2 Vidrio/espejo roto (patrón de impacto)
- **Grietas RADIALES**: 5-9 desde el punto de impacto, ángulos casi
  equiespaciados con jitter ±15°.
- **Grietas TANGENCIALES/CONCÉNTRICAS**: conectan radiales adyacentes a
  ~0.4-0.7× de la distancia al impacto (la "telaraña").
- **ANCHO**: las grietas de vidrio NO afinan a cero rápido — el canal
  mantiene ~40-70% de su anchura hasta cerca de la punta (taper suave
  exponente 0.35-0.5, NO 0.9).
- **La rotura se propaga a 1458-1500 m/s**: el evento visual del espejo
  rompiéndose es 1-2 frames, con VIBRACIÓN previa visible.

### D.3 Continuidad de líneas encadenadas (lecciones regl-gpu-lines)
- El estándar de "grosor variable en quads": anchura EN LOS VÉRTICES
  compartidos (promedio de segmentos adyacentes) — sin escalones.
- **Solape necesario por giro**: extensión más allá del vértice =
  (w/2)·tan(θ/2) por lado; con extensión = w completa se cubren giros
  hasta 2·atan(2) ≈ **126°**. Con w·0.9 solo ~90°. Con w·0.35 solo ~53°.
- **Perla (round-join)**: disco de diámetro = max(w_i, w_{i+1}) en CADA
  vértice interior — cubre el cuño exterior en giros fuertes.
- **Textura del segmento**: PERFIL UNIFORME a lo largo del eje (sin rampas
  de alfa horneadas a lo largo) — las rampas de longitud SOLO en quads
  ÚNICOS (el desgarro recto).
- **El pase de vacío (oscuro) debe tener el MISMO o mayor solape que el
  pase de luz**: si el negro tiene menos solape que la luz, los huecos del
  negro se leen como CORTES de la grieta.

---

## E. DIAGNÓSTICO DE LOS CORTES DE LA FASE 2 (v6.29, medible)

1. **`CaminoGrieta` con curvatura 8** produce giros consecutivos de hasta
   ~2i·8·0.25° = 52° a i=26 + kinks de ±34° → **giros de hasta ~86°**.
   El solape de la luz es len+w·0.9 (cubre ≤~90° justo) pero el de la
   PERLA es w·1.15 con la rotación del segmento ACTUAL — en giros >70° el
   cuño exterior queda >1.5px sin cubrir. **Y el VACÍO solo solapa
   len+w·0.35 (cubre ≤~53°)** → huecos negros visibles = CORTES.
2. **El taper (1-t)^0.9** mata la mitad distal: a t=0.75 w=14·0.27≈3.8px,
   a t=0.9 w≈1.5px, y los segmentos con w<0.4 se OMITEN (`continue`) →
   la cola se rompe en trazos sueltos = **DASHES**.
3. **`vida = (1-progress)^0.8`** en la grieta viva llega a 0.22 al final →
   alfas 0.066/0.13/0.20 — la cola fina+tenue se percibe como interrumpida.

**REGLA v6.30**: solape de vacío ≥ solape de luz; perla de diámetro
max(w_i,w_{i+1}) en TODOS los vértices (también punta y raíz con media
perla); taper exponente ≤0.5 con suelo de anchura 25%; NUNCA omitir
segmentos (clamp 0.6px mínimo); giros limitados (curvatura moderada
4-5, kink ±25°) + ramificaciones propias en vez de giros extremos.

---

## F. LECCIONES ACCIONABLES (para implementar)

1. **Rancor**: paleta del haz = blanco puro + rosa (204,77,112); círculo
   BLANCO-PLATA (grises aditivos); brazos NEGROS con borde rojo oscuro;
   ascuas ámbar; sonido en LOOP durante el haz.
2. **Eminencia**: masa NEGRA/violeta oscuro (alpha pass) con caras
   ROJO-NARANJA (253,74,60) brillando dentro (additive); al completarse,
   UNA cara mayor central; el ítem visible en la mano (holdout).
3. **Espejo roto del desgarro**: canal principal Lichtenberg moderado +
   ramas laterales (25-60°, 0.25-0.45× largo, 0.6× ancho) alternando lados
   cada 2-4 segmentos + sub-ramas 0.5× + arcos tangenciales conectando
   radiales cerca del origen. TODO con la cadena continua v6.30 (regla E).
4. **Sin proyectiles que caen**: la fractura NO suelta shards — la
   "esquirla" ES el patrón de grietas ramificado dibujado.
5. **Daño**: el espejo completo (canal + ramas) golpea en la fractura
   (×2.2 el canal, ×1.6 las ramas), nada más cae del cielo.

## Fuentes
- https://calamitymod.wiki.gg/wiki/Rancor (mecánica + sprites + sonidos)
- https://calamitymod.wiki.gg/wiki/Gruesome_Eminence (mecánica + sprites)
- Sprites medidos: Rancor.png, Rancor_Arms.png, Rancor_Cinder.png,
  Rancor_Magic_Circle.png, Gruesome_Eminence.png, Gruesome_Eminence_Holdout.png,
  Spirit_Congregation.gif (18 frames), Rancor_(demo).gif (275 frames),
  Gruesome_Eminence_(demo).gif (623 frames) — todo en research/v630/ref/.
- tML 1.4.4 NPC.cs.patch (CanBeChasedBy/immortal/TargetDummy — GitHub).

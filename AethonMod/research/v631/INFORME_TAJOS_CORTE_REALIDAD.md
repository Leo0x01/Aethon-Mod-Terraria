# INFORME R1 — TAJOS: EL CORTE DE REALIDAD CONTINUO Y PAREJO
**Task ID 49 · Agente de investigación (sin código de producción) · v6.31**

Petición del usuario: el Bastón del Desgarro (corte de realidad) se ve **discontinuo**;
hay que **QUITAR el efecto espejo-roto** (ramas Lichtenberg + arcos telaraña de v6.30)
y lograr un corte **CONTINUO y PAREJO** (anchura uniforme, sin interrupciones).

Método: 54 búsquedas web (29 de la primera pasada b01-b29 + 25 nuevas b30-b54; el
servicio z-ai `web_search` permaneció en 429 durante toda la sesión → pipeline de
respaldo DDG-lite/DDG-html/Bing, scripts en `busquedas/`), 12 páginas leídas
(`search_results/t49/pages/`), 15 fuentes descargadas y leídas línea a línea
(`src_cal/` Calamity, `src_cwr/` Calamity Overhaul chino, `fx_cwr/` shaders,
`terraria_projectile.cs` decompile vanilla) + lectura del código actual
(`RealityTearProjectile.cs`, `RiftLib.cs`, `DesgarroRealityStaff.cs`).

---

## (A) TABLA DE ARMAS QUE CORTAN/DESGARRAN LA REALIDAD

| Arma (mod) | Concepto "corte realidad" | Técnica VISUAL | Técnica de DAÑO | Números clave |
|---|---|---|---|---|
| **Reality Slasher** (Thorium, radiante post-ML) | "Rapidly spins a reality slicing scythe… enemies are rapidly torn from reality" | **Cortes rectos púrpura CONTINUOS** (horizontales o verticales) sobre el enemigo más cercano, 3 por uso — la referencia visual MÁS cercana a lo que pide el usuario | La guadaña gira alrededor (aura) + los cortes rectos pegan daño adicional | 170 dmg, use 22, auto; cortes ×3 por uso, H o V; debuff Light Curse 5 s |
| **Reality Rupture** (Calamity, rogue post-ML, upgrade de Spear of Destiny) | "reality shattering lance" (stealth) | Lanza/estrellas estándar (no es línea) | Alterna 3 lanzas teledirigidas (perfora ×2) y 1 lanza perforante | Lance ×400% base, −5% daño por golpe; stealth ×390% + explosión |
| **Murasama** (Calamity, true melee post-ML) | "high-frequency blade that can cleanly slice through even the mightiest foe" | **Spritesheet de 14 frames** (1 frame cada 3 ticks → ciclo de 42 ticks); bucle baja→sube→GIANTE ×2; sprite 216×216 centrado a 80 px del jugador; luz roja ×2-3.5 | **El hitbox ES una línea gruesa**: AABBvLineCollision del centro del jugador a `velocity·8.6` (normal) o `·11.35` (golpe grande), **ancho 200-320 px** | 2200 dmg, crit 65%, use 25; `idStaticNPCHitCooldown = 6`; Slash3 = daño ×2; golpe 3º inmunidad 8 ticks |
| **Exoblade** (Calamity, melee post-ML) | Espada de energía que "desgarra" con haces que cortan | Beams con **primitive trail de 30 puntos** (TrailingMode 2) + al impactar **slash largo perforante infinito** (hitbox 512×24); dos pasadas de trail (color + blanca ×0.8) | 4 beams que buscan tras 0.4 s (35% daño); dash 0.82 s / 65 tiles → 5 slashes ×150%; siguiente swing +50% tamaño + explosión ×180% | Trail `MaxWidth=30`; perfil ancho `sin(acos(1−lerp(0,0.15,u)))·lerp(1,0.4,u)`; `localNPCHitCooldown = MaxUpdates·12`; timeLeft slash 35 con MaxUpdates 2 |
| **Onikiri 鬼切** (Calamity Overhaul/CWR, legendaria) | "斩缝" — la **COSTURA/SURCO que el filo deja en el aire**: cada golpe abre una rendija | **RiftDef paramétrica** (arc= círculo 3D inclinado, o línea) dibujada como banda con shader: 4 colores (blanco-caliente→carmesí→rojo profundo→casi negro), revelado **cabeza-primero** mientras la hoja barre, overshoot 1.05 al 62%, hit-stop, tinta | Ventana de daño = `GatherFrames→(Gather+Sweep+Hold+1)`; **un golpe por objetivo por golpe/combo** (lista HitTargets); radios: banda `max(32, ancho)` + radios ("spokes") de 36 px desde el centro | 5 beats, `BeatGap=[10,10,13,15,24]`; windup `[0.90,0.90,1.10,1.40,1.70]` rad; daño ×1/×1.3/×1.6; hit-stop `[2,2,3,4,5]`; punch de cámara `[1.5,1.5,2,4,6]`; colisión: 15 muestras a lo largo; grass-cut: 9 muestras |
| **CyberRiftSlash 次元斩** (CWR, teletransporte SHPC) | Corredor de teletransporte = **un tajo dimensional continuo** | **Trail primitivo continuo** casi recto: micro-arco ≤8 px amortiguado en los extremos, `segs=clamp(L/60,8,18)`; ancho `56·sin(πu)^0.45` con suelo 0.18; shader aditivo | (es movilidad, no daño — pero es EL patrón de línea continua del mod) | Vida 30 ticks (0.5 s): extensión 0→0.32 (ease `1−(1−t)^2.4`), pulso de impacto 0.32→0.5 (×1.30 de ancho), retracción cola→cabeza 0.5→1 |
| **Arkhalis / Terragrim** (vanilla) | "series of blurred slashes… appears as a sword but does not present an actual blade" | **No hay hoja**: ráfaga de sprites de cuchillada hacia el cursor; el jugador dibuja la espada estática | aiStyle **75**; caja de daño **68×64** centrada a **52 px** delante del jugador (recreada por CWR: `BoxForward=52`); cambio de pose del filo cada **5 frames** (`FlashInterval`), spread máx **0.7 rad** | **5 i-frames → 12 golpes/s**; 20 armor pen (Arkhalis); ignora modificadores de tamaño/velocidad; daña incluso detrás del jugador |
| **Zenith / Terra Blade** (vanilla, referencia) | Arcos de espadas voladoras | Proyectiles-esprite que siguen curvas paramétricas de swing | Colisión por hitbox de sprite estándar | (contexto; no es "línea") |
| **Fargo's Souls** | (Eternity Mode es dificultad, no arma-corte) | Búsqueda sin hallazgo específico de arma "reality-cutter"; sus jefes finales usan cortes de pantalla | — | Limitado: no hay arma análoga documentada |
| **Starlight River** | Armas con primitivos propios | Repos con primitivos/TrailLib-like (fuentes 404 en las 3 tentativas: `PrimitiveRenderer`-style) | — | Limitado: no se pudieron bajar fuentes (404); solo confirmación de arquitectura de primitivos |

**Conclusión (A):** nadie implementa "corte de realidad" como **ramas**: los
referentes de primera línea (Thorium Reality Slasher, CWR CyberRift/Onikiri,
Murasama) usan **UNA SOLA LÍNEA/RENIDA CONTINUA** (recta o arco único) y toda la
lectura de "realidad cortada" viene de **anchura, color, luz y timing**, no de
fragmentación. La fragmentación (Lichtenberg) es exactamente lo que el usuario
percibe como **discontinuo**.

---

## (B) MECÁNICA DEL "PROYECTIL-ES-TAJO" EN tModLoader

1. **El hitbox del proyectil NO es el sprite: es una LÍNEA/CÁPSULA.** Patrón
   Calamity (Murasama) y CWR (OniSlash) idéntico:
   ```csharp
   Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
       start, end, lineWidth, ref _);
   ```
   - Murasama: start = centro del jugador, end = `centro + velocity·8.6`,
     **lineWidth 200-320** (¡el "corte" daña en una franja GRUESA aunque el
     dibujo sea fino!).
   - OniSlash: 15 muestras a lo largo de la banda (u 0.05→0.95), por segmento
     `thickWorld = max(32, band.Width)` + "radios" de 36 px desde el eje al
     centro (para que pegue pegado al jugador). El **corte de hierba** usa 9
     muestras con `Utils.PlotTileLine(prev, band.Center, max(30, w·0.8))`.
   - Nuestro `RiftLib.LineaToca` ya es esta escuela (A) con radio
     `max(width·0.5, 4)` + inflado AABB. ✔

2. **AIType:** el vanilla Arkhalis/Terragrim es **aiStyle 75** (flurry corto); no
   se reutiliza para tajos largos. Los mods modernos NO usan aiStyle: usan
   **holdout propio** (`heldProj`, `timeLeft=2` auto-renovado, channel) que vive
   pegado al jugador (Murasama: `Center = puntoRotado + velocity·80·scale`) o un
   **proyectil estático de área** clavado en el mundo (nuestro modelo). El
   "swing arc" de Murasama es un **bucle de 3 poses** (frames 10→0→6) con daño
   activo SOLO en los frames-key (`CanDamage() => Slashing ? null : false`).

3. **Cadencia de daño (los 3 sistemas de inmunidad vistos):**
   - `usesIDStaticNPCImmunity + idStaticNPCHitCooldown = 6` (Murasama: cada
     6 ticks por proyectil-ID — 10 golpes/s por objetivo).
   - `usesLocalNPCImmunity + localNPCHitCooldown = MaxUpdates·12` (Exoblade).
   - **Lista manual por golpe** `HitTargets` + `EngineHitCooldown = 2` de
     respaldo (OniSlash: un objetivo recibe EXACTAMENTE 1 golpe por beat; el
     motor solo evita el doble-registro del mismo frame).
   - Vanilla Arkhalis: 5 i-frames globales = 12 golpes/s.
   - Nuestro DoT cada 3 ticks (×0.07/×0.10) está en el rango alto de frecuencia
     (20 golpes/s) — correcto para "herida que sigue doliendo".

4. **Ventana de daño separada de la vida visual:** OniSlash define
   `DamageStart = GatherFrames` y `DamageEnd = Gather+Sweep+Hold+1`; después la
   rendija sigue VIVA visualmente pero ya no daña. Nuestro contrato actual
   (`Daña(progress) => progress < 0.90`) es el mismo patrón. ✔

5. **Dibujo del tajo: 3 escuelas.**
   - **Spritesheet frameado** (Murasama 14 frames @3 ticks, Arkhalis vanilla):
     cero matemáticas, cero continuidad garantizada — la continuidad la pone el
     artista. NO aplica a nosotros (VFX 100% código).
   - **Un solo quad estirado** (Last Prism vanilla / `ExampleLastPrismBeam`
     tML): `Utils.DrawLaser` con `LaserLineFraming` que enmascara el tiling; 2
     pasadas (exterior color opacidad 0.75 α=64 + **interior BLANCA a media
     escala** opacidad 0.1 = "centro cegador"). Es exactamente nuestra
     `RiftLib.Tear` (3 quads RiftTaper*).
   - **Primitive trail / triangle strip** (Exobeam, CyberRift, Abyssrend,
     OniSlash): `DrawUserPrimitives(PrimitiveType.TriangleStrip, bars, 0, n-2)`
     con vértices `path[i] ± perp·w(u)` — la línea es UNA tira sin juntas.
     Abyssrend calcula la dirección por **diferencia central**
     (`path[i+1]−path[i−1]`) → normales suaves sin picos de miter.
   - **Regla de oro CWR para el ancho** (`RiftDef`): `BandMax ≈ 0.4·R` de la
     hoja, pico en `PeakU` (0..1), potencia de entrada `PowIn` (grande = entra
     en punta) y de salida `PowOut` (pequeño = desgarra gordo). El ancho vive en
     los VÉRTICES, compartido entre segmentos (igual que nuestro `AnchosCamino`).

6. **TrailCache:** para tajos que SE MUEVEN, `ProjectileID.Sets.TrailCacheLength
   = 30` + `TrailingMode = 2` (Exobeam) y el trail se dibuja sobre `oldPos`.

---

## (C) REGLAS DE LÍNEAS CONTINUAS EN SpriteBatch/XNA — CON NÚMEROS

Recopilado de: Shtille (polyline rendering 1-5), infinitecanvas.cc lesson-12
(regl-gpu-lines), mhalber/Lines, Matt DesLauriers "Drawing Lines is Hard",
Catmull-Rom, Godot Line2D (join modes), 博客园 刀光拖尾, bilibili 顶点绘制,
+ los fuentes CWR/Calamity + nuestras lecciones v6.28-v6.30 medidas:

1. **Un quad por segmento con SOLAPE COMPLETO `len + wmax`.** Dos segmentos que
   comparten vértice se dibujan con largo `len + max(wa,wb)`; cubre giros de
   hasta ~126° sin hueco (regla regl-gpu-lines/shtille; ya es nuestro contrato
   v6.30). Para giro arbitrario → perla (regla 2).
2. **PERLA en CADA vértice** (round join): círculo/quad de diámetro
   `max(wa,wb)·1.15..1.25` en cada punto compartido + **perla de punta** al
   final (round cap). El vacío/negro lleva SU perla (α 0.94) o el hueco se lee
   como CORTE. (Nuestro v6.30: perlas en todos los vértices ✔.)
3. **El ancho se evalúa en los VÉRTICES compartidos, jamás en centros de
   segmento** (lección Calamity WidthFunction): escalones de ancho = bordes
   vistos = "cortes" percibidos.
4. **PERFIL DE ANCHO a lo largo — el corazón del "PAREJO":**
   - **PROHIBIDO** el taper lineal a 0 (aguja): mata la cola y rompe la cadena
     (medido en v6.29: era 1 de las 3 causas de los cortes).
   - **Perfil de meseta con extremos suaves** (lo que usan los que ganan):
     CyberRift: `w(u) = W·pow(max(sin(π·u), 0.18), 0.45)` — cuerpo plano al
     ~70-80% del centro, recogida MÍNIMA (suelo 18%) en los últimos ~10% de cada
     extremo. Con W=56 px base.
   - OniSlash: banda asimétrica `pow` de entrada/salida con `PeakU` desplazado
     al frente (la fuerza del golpe define dónde está lo gordo).
   - **Parejo estricto** (lo que pide el usuario): constante 100% del cuerpo +
     caps redondos; solo un roll-off de **0.10·L en cada extremo** con
     smoothstep. NUNCA `sin(πu)` puro (forma de huso = "no parejo").
5. **Curvatura máxima por segmento:** con solape `len+w` el giro seguro es
   ≤126°/segmento; en la práctica CWR mantiene el canal casi recto: **micro-arco
   ≤ `clamp(L·0.012, 2, 8)` px** con `endpointDamp = SmoothStep(0,1,min(k,1−k)·4)`
   (extremos CLAVADOS rectos — "走廊少弯，防菱形像素": el corredor casi no se
   dobla para evitar píxeles de rombo). Segmentos: `clamp(L/60, 8, 18)`; línea
   OniSlash: 14 slices; arco: 28 slices; máx 32.
6. **Una sola tira = cero juntas** (Abyssrend `DrawPathStrip`): triangle strip
   con perpendiculares por diferencia central — la alternativa a
   quads+perlas cuando se puede usar `DrawUserPrimitives`.
7. **Nada de `depth`/z-fighting entre capas propias** (shtille: `glDepthMask(false)`):
   en SpriteBatch equivale a dibujar TODAS las capas del corte en el MISMO lote
   y mismo orden (nuestros 2 lotes del contrato: NonPremultiplied para el vacío,
   Additive para la luz ✔).
8. **Sin variación de alpha POR SEGMENTO**: el pulso/beat debe ser GLOBAL o
   función suave de u; un `sin(k·1.7)` por índice de segmento produce
   "perlas-oscuras" alternadas que se leen como cuentas separadas (nuestro beat
   actual de `Grieta` usa `k·1.7` — candidato a suavizar).
9. **Doble pasada anidada para "afilado"** (las 3 implementaciones de referencia):
   - Last Prism: exterior opacidad 0.75 (α=64) + interior BLANCA ×0.5 escala.
   - Exobeam: trail color + trail blanca ×0.8 de ancho.
   - Onikiri: 4 colores — caliente `(1.55,1.34,1.10)`, carmesí `(1.32,0.17,0.10)`,
     profundo `(0.58,0.045,0.065)`, vacío `(0.055,0.018,0.030)`.
   - Nuestro contrato actual: velo ×1.6 α0.30 / cuerpo ×1.0 α0.60 / núcleo ×0.8
     α0.90 — MISMA proporción (≈1.6/1.0/0.8). ✔ Mantener.
10. **El lenguaje de bordes** (cabecera `OniCrimsonSlash.fx`, traducida): *"la
    nitidez viene del borde*: el lado afilado (h=0) permanece ABSOLUTAMENTE liso
    mientras el trazo vive — el ruido, las roturas orgánicas y las cerdas de
    pincel viven SOLO en el lado que arrastra (h=1); la disolución avanza del
    lado sucio hacia la línea-rasuradora, que permanece limpia hasta el final".*
    → Para un corte PAREJO: **cero ruido en los bordes del corte**; si se quiere
    textura, va en el INTERIOR (estrellas/anomalías), jamás en el filo.
11. **Cometa/retracción (cola→cabeza):** `uTailErode`: mientras el frente sigue
    revelándose, el arranque ya se evapora (CyberRift fase 3: `visibleStart`
    sube 0→1 y `fadeAlpha 1→0`). Para una HERIDA persistente no se usa; para el
    CIERRE sí es la mejor lectura ("la realidad sana comiéndose el corte desde
    los extremos").
12. **Muestreo bilineal + premultiplicado** en los mocks (lección v6.30): el
    juego con SpriteBatch muestrea Lineal; los escalones duros del mock eran un
    bug del propio mock.

---

## (D) RECOMENDACIÓN CONCRETA PARA EL DESGARRO CONTINUO v6.31

**Diagnóstico:** el desgarro se lee discontinuo porque las fases
FRACTURA/GRIETA/CIERRE (110 de los 193 ticks, el 57% de la vida) dibujan
`CaminoEspejoRoto` = canal Lichtenberg + ramas + arcos = **geometría
fragmentada por diseño**. Además el canal madre usa `CaminoGrieta` (curvatura
4.5°, kinks ±0.6 rad, taper `(1−t)^0.45`) = ancho decreciente y quiebres. El
usuario quiere LO CONTRARIO: una sola línea pareja. La fase RECTA (52 ticks, UN
quad) ya es continua y fue verificada en v6.28 — **extender ese contrato a toda
la vida del proyectil**.

### D.1 — QUITAR (lo que se elimina)
1. En `RealityTearProjectile`: eliminar `_espejoMundo`/`_espejoPantalla`,
   `CaminoEspejoRoto`, `GolpearRamillete`, `DibujarRamillete` (las llamadas;
   RiftLib puede conservar la API para otros usos, pero el Bastón no la usa).
2. `CaminoGrieta` con curvatura/kinks como canal del desgarro: FUERA. La única
   curvatura permitida = la onda estacionaria de VIBRACIÓN (amplitud 3.5 px,
   2 nodos, ~10 Hz — ya existe y está bien).
3. Sin shards, sin ramas, sin arcos, sin sub-ramas (confirmado: nada cae, nada
   se ramifica).

### D.2 — EL CORTE (geometría)
- **Toda la vida = la LÍNEA RECTA de UN QUAD** (`RiftLib.Tear`/`TearVacio`),
  620 px, de punta a punta, atravesando paredes (sin cambios de contrato).
- Durante VIBRACIÓN: cadena `CaminoVibracion` (que ya dibuja con
  `Grieta`+perlas) pero con **ANCHO PLANO** (ver D.3); amplitud 3.5 px máx
  (≤ la micro-curvatura CWR de 8 px).
- Tras la FRACTURA el corte **se queda recto** (la "fractura" es un evento de
  luz/golpe, no de geometría — D.5).

### D.3 — ANCHURAS (el "PAREJO", con números)
| Capa | Ancho (px) | Alpha | Nota |
|---|---|---|---|
| VACÍO (BlackDisk, lote NonPremultiplied) | **14** constante + perlas Ø 16 | 0.94 | quad alto 0.62·w+0.8 (actual) |
| VELO (RiftLip violeta 150,80,255) | **22** (×1.6) | 0.30 | halo integrador |
| CUERPO (RiftLip carmesí 255,60,130) | **14** (×1.0) | 0.60 | el labio |
| NÚCLEO (RiftCore blanco-cian 235,245,255) | **11** (×0.8) | 0.90 | el filo razor |

- **Perfil longitudinal nuevo (crítico):** sustituir el taper actual
  `(1−t)^0.45+suelo 25%` por **meseta**: ancho 100% en u∈[0.10, 0.90],
  roll-off smoothstep en los extremos (u<0.10 sube 0→1, u>0.90 baja 1→0),
  caps redondos (perlas de punta Ø=w) — CyberRift `pow(max(sin,0.18),0.45)`
  si se quiere algo de vida, pero MESETA ≥ 80% de L.
- **El ancho NO respira**: la respiración (±8%) pasa a la ALPHA/brillo solo.
  Un ancho que late = "no parejo" (lección borde CWR: el filo permanece liso).
- Pulso de FRACTURA: ancho ×**1.35** durante **2** ticks (CyberRift boost
  1.30; nuestro kick actual ×1.3 de cámara ya existe) y vuelve a 14 — UNA
  inhalación, no una oscilación.

### D.4 — COLORES (mantener paleta, añadir el 4º plano)
- Violeta `(150,80,255)` velo · Carmesí `(255,60,130)` cuerpo · Blanco-cian
  `(235,245,255)` núcleo — + **nuevo filo interior blanco puro ×0.5 de ancho,
  α 0.10-0.15** (la lección Last Prism: "centro cegador" blando; Exobeam hace
  lo mismo con su mini-trail blanca ×0.8).
- Estrellas del vacío y chispas de anomalía: SOLO dentro del canal (interior),
  jamás tocando los bordes (lenguaje de bordes CWR: el filo queda limpio).

### D.5 — TIMINGS (193 ticks totales, mismas fases, nuevos significados)
| Fase | Ticks | Visual | Daño |
|---|---|---|---|
| TELÉGRAFO | 12 | estrella creciendo + anillo implosionando + mundo 0→0.22 | 0 |
| APERTURA | 3 | **la línea nace YA a 620 px** con flash 0.22 | **×1.0** una vez (cápsula 14+8) |
| RECTO | 52 | línea pareja viva; estrellas fluyen; α respira ±8% 2.2 Hz | DoT ×0.07 cada 3 ticks |
| VIBRACIÓN | 16 | onda estacionaria 0→3.5 px ~10 Hz (ancho plano) | DoT ×0.07 cada 3 ticks |
| FRACTURA | 2 | **CLÍMAX ÓPTICO**: flash 0.30 + ancho ×1.35 + intensidad 1.25 + kick ×1.3 + hit-stop 2 ticks | **×2.2** UNA vez (la herida se profundiza) |
| CORTE VIVO | 98 | la MISMA línea recta pareja, más intensa (herida abierta), α 0.90±10% 4.4 Hz | DoT ×0.10 cada 3 ticks |
| CIERRE | 10 | **se cierra desde los EXTREMOS hacia el centro** (ErodeT direccional CWR: "la realidad sana comiéndose el corte") — NO menguando el ancho | 0 (cesa 8 ticks antes) |

- Cadencia alternativa si se quiere golpear más "por pulsos" como Murasama:
  sustituir el DoT cada 3 ticks por inmunidad estática **6 ticks** (10 golpes/s)
  — mantener el actual está bien (20/s con factores pequeños).
- La VIDA del ancho JAMÁS baja de 0.90 durante RECTO→CORTE VIVO (regla v6.30:
  se LEA continua; el cierre solo en los últimos 10 ticks y por longitud).

### D.6 — VERIFICACIÓN
- Mock 1:1 numérico (como `mock_espejo_v630.py`): contar cortes (huecos donde
  la cobertura alfa < umbral) a lo largo de u∈[0,1] con 5 semillas → objetivo
  **0 cortes** y desviación de ancho ≤ ±5% en u∈[0.15,0.85] (el "parejo"
  medible). Puntos de muestreo: cada 2 px a lo largo de la línea central.
- Test en juego: contra las 99 dummies, el corte debe leerse como UNA raya
  violeta-carmesí continua de punta a punta durante toda la secuencia.

### D.7 — LO QUE NO CAMBIA
- Contrato de 2 lotes (NonPremultiplied → Additive) y el guard de PreDraw. ✔
- Determinismo MP (semilla = identity; daño solo server/SP; visual cliente). ✔
- `EsObjetivo`/dummies, Electrified 90-120, escuela A de daño por cápsula. ✔
- `DesgarroRealityStaff.cs` salvo tooltips (mencionar "corte continuo y parejo"
  en vez de "se fractura como un espejo").

---

## FUENTES PRINCIPALES
- Thorium Reality Slasher: https://thoriummod.wiki.gg/wiki/Reality_Slasher
- Calamity Reality Rupture: https://calamitymod.wiki.gg/wiki/Reality_Rupture
- Calamity Murasama: https://calamitymod.wiki.gg/wiki/Murasama (+ fuente
  `src_cal/murasama_slash.cs`, `murasama_item.cs`)
- Calamity Exoblade: https://calamitymod.wiki.gg/wiki/Exoblade (+ fuentes
  `src_cal/exobeam.cs`, `exobeam_slash.cs`, `exoblade_item.cs`)
- tML ExampleLastPrismBeam/Holdout: github tModLoader 1.4.5 (`src_cal/ex_lastprismbeam.cs`)
- Calamity Overhaul (CWR, chino): Onikiri 狼切 — `OniSlash.cs` + `OniSlashRenderer.cs`
  (RiftDef/RiftBand/BandWidth), `CyberRiftSlashProj.cs`, `GsTerragrim.cs` +
  `GsOdditiesFlurryHeldBase.cs` (aiStyle 75 recreado), `AbyssrendFX.cs`
  (DrawPathStrip), shaders `fx_cwr/OniCrimsonSlash.fx`, `CyberRiftSlash.fx`, etc.
  Sitio oficial: https://calamity-overhaul.cc/cn/legend/onikiri/
- Vanilla Arkhalis/Terragrim: https://terraria.wiki.gg/wiki/Arkhalis ,
  https://terraria.wiki.gg/wiki/Terragrim
- Líneas continuas: https://shtille.github.io/blog/polyline-rendering (serie 1-5),
  https://infinitecanvas.cc/guide/lesson-012 (regl-gpu-lines),
  https://github.com/mhalber/Lines , https://mattdesl.svbtle.com/drawing-lines-is-hard ,
  https://stackoverflow.com/questions/687173/ , Godot Line2D docs (joint modes),
  Catmull-Rom (Wikipedia/KipuHub/qroph).
- Chino: 博客园 《算法与游戏实战技术之刀光拖尾实现》 (cnblogs.com/booth666/p/14994050.html
  — B-spline + tira de triángulos + fade temporal), bilibili BV19GsMePEue
  《附源码 顶点绘制教程 弹幕拖尾》, Kyle's Blog 泰拉瑞亚Mod开发指南
  (cyborg2077.github.io), 灾厄中文维基 鬼妖村正 (calamity.huijiwiki.com — 403).
- Búsquedas: `research/v631/busquedas/b01..b54_*.json` (54 consultas).

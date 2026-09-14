# INFORME — Técnicas de luz de la Emperatriz de la Luz VANILLA (Terraria 1.4.4.9)

**Task ID:** 38-c · **Agente:** general-purpose (investigación EoL vanilla) · **Fecha:** sandbox actual
**Objetivo:** extraer del código vanilla real las técnicas de luz/prisma de la Emperatriz de la Luz (NPC + todos sus proyectiles) para la librería de luz de AethonMod ("como los mods que reworkean a la Emperatriz").

---

## 1. FUENTES USADAS (y cómo se obtuvieron)

| Fuente | Qué aporta | Fiabilidad |
|---|---|---|
| **tModLoader.dll v2026.07.3.0 decompilado con ILSpy 8.2** (el zip de tML ya estaba en `/tmp/tModLoader.zip`; dll en `/tmp/tml/tModLoader.dll`) | Es el ensamblado real del juego: **Terraria 1.4.4.9 recompilado + parches tML**. El código vanilla de la Emperatriz está íntegro. Se extrajeron tipos completos: `Terraria.Projectile` (93k líneas), `Terraria.NPC` (112k), `Terraria.Main` (84k), `Terraria.ID.ProjectileID/NPCID`, `Terraria.DelegateMethods`, `Terraria.Graphics.EmpressBladeDrawer/LightDiscDrawer/RainbowRodDrawer` | ★★★★★ — es literalmente el código que ejecuta el juego |
| Búsqueda web (z-ai web_search) | Confirmación de que NO existe repo público del dump 1.4.4 fácilmente clonable (repos tipo "Terraria-Source" son 1.4.0.5 o incompletos); wikis (terraria.wiki.gg Empress of Light) para nombres de ataques | ★★★ corroboración |
| **Coralite** (`/tmp/research/coralite`) | Comprobado: NO reworkea a la Emperatriz (solo tiene el ítem `HolyLacewing` que invoca al boss vanilla + partícula `RainbowHalo`). Everglow: sin contenido EoL | ★★ complementario |

**Copia de trabajo del decompile:** `/tmp/research/terraria_src/` (Projectile.cs, NPC.cs, Main.cs, ProjectileID.cs, NPCID.cs, DelegateMethods.cs, empress_ai.txt, EmpressBladeDrawer.cs...). El decompile NO se copia al repo (copyright); este informe lo **parafrasea en pseudocódigo propio**.

### 1.1 Mapa de entidades (nombres internos REALES 1.4.4)

> Nota: los nombres que circulaban ("HallowBossRay", "HallowBossDagger", "PrismaticLance", "HallowBossRainbowRepeat", "HallowBossLastLightWave") **NO existen** en 1.4.4. Los reales son:

| ID | Nombre interno | Qué es |
|---|---|---|
| NPC **636** | `HallowBoss` (aiStyle **120**, `AI_120_HallowBoss`) | La Emperatriz. 70000 HP, 100×100 px, sin gravedad/colisión |
| NPC **661** | `EmpressButterfly` | El ala de encaje prismática (critter que la invoca) |
| PROJ **871** | `HallowBossSplitShotCore` (aiStyle 172) | "Director" invisible del abanico radial de píldoras (Prismatic Bolts fase 2) |
| PROJ **872** | `HallowBossLastingRainbow` (aiStyle 173) | **Everlasting Rainbow**: estelas curvas del arcoíris (timeLeft 660) |
| PROJ **873** | `HallowBossRainbowStreak` (aiStyle 171) | **Prismatic Bolts**: dardos-estela prismáticos homing (timeLeft 200) |
| PROJ **874** | `HallowBossDeathAurora` (aiStyle 0) | **Aurora vertical de 15 bandas** (spawn, ataque 3 y muerte) |
| PROJ **919** | `FairyQueenLance` (aiStyle 179) | **Ethereal Lance / Radiance**: las lanzas (¡los muros de Radiance son LANZAS!) |
| PROJ **923** | `FairyQueenSunDance` (aiStyle 180) | **Sun Dance**: rayos radiales de 800 px |
| PROJ **924** | `FairyQueenHymn` (aiStyle 186) | Sin uso por la Emperatriz (familia armas princesa) |
| PROJ **931** | `Nightglow` (aiStyle 171) | Versión amiga del streak (mismo aiStyle que 873) |
| PROJ **932** | `FairyQueenRangedItemShot` (aiStyle 181) | Flecha prismática (arco de la Emperatriz) |
| PROJ **946** | `EmpressBlade` (aiStyle 156) | **Terraprisma**: espadas siervas prismáticas |
| Texturas ref. | `Extra[178]` = veta/estela larga blanca (haz de la lanza); `Extra[98]` = glow suave (cruz de brillo del streak); `Projectile_923` = hoja 1×2 (frame0 glow / frame1 núcleo); `Projectile_872` = hoja partida en 2 mitades horizontales (cabeza | segmento de estela) | |

---

## 2. EL MOTOR DE COLOR PRISMÁTICO (la base de TODO)

### 2.1 `Main.hslToRgb(hue, 1, L)` — el arcoíris de la Emperatriz es HSL puro
La Emperatriz **NO usa `Main.DiscoR/G/B`** para sus armas (eso es del Rainbow Rod/disco ball). Usa **HSL con hue explícito**:

```
color = hslToRgb(hue_normalizado % 1, saturación=1, luminosidad)
```

Luminosidades observadas: **0.5** (color pleno: 873/872/946/orbit halos), **0.85** (luz emitida por SunDance), **1.0** (núcleo del rayo = color a plena luz), **0.3→0.66** (capas exteriores del rayo, se aclaran con el progreso). El hue siempre se pasa `% 1f` (wrap).

### 2.2 Hue por identidad/patrón (cómo cada proyectil "elige" su color)
- **873 Prismatic Bolt (volley fase 1):** hue = `progreso_ataque/60` → la ráfaga ENTERA recorre el arcoíris de inicio a fin.
- **873 (espiral experto):** hue = `(t-10)/50` → ídem, sobre 50 ticks.
- **872 Everlasting Rainbow:** hue = `índice/13` → 13 estelas, 13 hues espaciados uniformemente (arcoíris simultáneo espacial).
- **923 Sun Dance:** hue = `ángulo_inicial/2π + progreso/180` → mezcla espacial+temporal (los rayos van cambiando de color mientras giran).
- **919 FairyQueenLance:** color vía `hue = (ai[1]+0.5) % 1`, con ai[1] = progreso del volley (t/100).
- **946 Terraprisma:** hue = `(identity % nº_minions)/nº_minions + (GlobalTime % 3)/3` → **cada espada tiene un hue distinto Y todos rotan de color cada 3 s**.
- **Last Prism (láseres):** hue = `índice_láser/6` → 6 haces a 60° de hue de distancia.

**LECCIÓN MAESTRA:** el hue se deriva SIEMPRE de algo con significado (progreso temporal, índice en el grupo, ángulo, identidad persistente). Nunca es aleatorio.

### 2.3 `GetFairyQueenWeaponsColor(alfaMult, lerpToWhite, hueOverride?)` — la función-tirana
Es EL proveedor de color de las armas de la reina hada (Terraprisma, arco, Nightglow, flechas):
```
hue   = ai[1] (o hueOverride si viene dado)
hue   = (hue + 0.5) % 1
color = hslToRgb(hue, 1, 0.5) * Opacidad
si lerpToWhite > 0: color = lerp(color, Blanco, lerpToWhite)
color.A *= alfaMult            // uso habitual: alfaMult 0.5 para "halo", 1.0 cuerpo
```
Con ~35 **easter eggs por nombre de jugador** (Cenx, Crowno, Yoraiz0r...): paletas personales que re-mapean hue/luminosidad (p.ej. Cenx = lerp(verde(0.3,1,0.2), HotPink) con doble SmoothStep). *No copiables literalmente (contenido del juego) pero el PATRÓN "paleta por identidad del dueño" sí es extrapolable.*

### 2.4 `Main.DiscoR/G/B` — el "disco" global (para completar)
`DoUpdate_AnimateDiscoRGB()`: 6 fases (G↑, R↓, B↑, G↓, R↑, B↓) a **+7 por tick** → ciclo completo ≈ **219 ticks (~3.6 s)**. Se usa en: Rainbow Rod (79: luz = disco/255), tipo 251 (luz = (disco+1)/2, versión pastel), tipo 502 (luz = (0.5+disco/255)/2, versión suave), dust 66 multicolor del cetro arcoíris (color = disco, escala 2.5). La Emperatriz NO lo usa — pero es la alternativa "color global gratis" si no quieres gestionar hues.

### 2.5 Enfurecida (día): `OurFavoriteColor = (255, 231, 69)` (dorado)
`NPC.ShouldEmpressBeEnraged()` = `Main.dayTime` (o elevada sobre superficie en mundo remix). Cuando está enfurecida:
- TODOS los colores de armas se sustituyen por `lerp(Blanco, OurFavoriteColor, rampa de Main.time 0→60 ticks)` (las armas amanecen doradas).
- Todos los daños = **9999** e `IsDamageDodgable() = false` (el famoso one-shot diurno, sin i-frames de esquive).
- El NPC en sí: contacto también 9999.

---

## 3. TÉCNICAS POR PROYECTIL (pseudocódigo propio)

### 3.1 `FairyQueenLance` (919) — EL PATRÓN "LANZA PRISMÁTICA" (el que pide el usuario)
**AI (aiStyle 179):**
```
t = localAI[0]++
fase telegrafía: t < 60 → velocidad = 0; alpha = lerp(255→0 en 20 ticks)  // el aviso se hace VISIBLE
fase disparo:   t ≥ 60 → velocidad = dirección(ai[0]) × 40 px/t
                 cada 3 ticks: dust(267 "hada") en el centro, color = lerp(color_hue, Blanco, rnd*0.4),
                 escala ×1.5, fadeIn=1, sin luz propia (noLightEmittence)
muerte: t ≥ 360
rotación = ai[0] (ángulo fijo guardado al spawn)
```
**Draw (5 capas):**
```
haz_telegrafía (solo t∈[10,55]): textura raya larga (Extra[178]) estirada a 3600 px de largo,
    origen en borde-izq-centro (0, 0.5·alto) → crece DESDE la lanza en su dirección;
    2 pasadas: núcleo brillante a MEDIA longitud (escala (0.5·L, 2)) + velo tenue (alpha ×0.3) a LONGITUD COMPLETA;
    color = hslToRgb(hue,1,0.5) con A=0 (aditivo visual); brillo = lerp(t,60→55) × lerp(t,0→10)
fantasmas (solo si velocidad > 0): 6 copias a -120·k px por detrás (k=1..1/6), alpha (1-k),
    escala fija; + 6 copias BLANCAS ×0.15 alpha a 0.85 escala (núcleo blanco del fantasma)
anillo: 4 copias a 2 px del centro en círculo (shimmer), color hue, alpha÷2
cuerpo: sprite blanco alpha÷2 + PASADA DE COLOR a 1.1× escala (el glow coloreado SIEMPRE más grande)
crecimiento: escala = lerp(0.7→1 entre t=55..60) → la lanza "se hincha" justo antes de lanzarse
```
**El muro de Radiance (ataque 7 del NPC):** NO es un beam — son **hasta 13 lanzas por línea** formando líneas de 1950 px (normal) / **18 lanzas en 975 px** en experto (muro más corto pero más denso), en 4-6 orientaciones sucesivas (vertical, diagonal, apuntando al jugador...), con lead de 90 ticks sobre la velocidad del jugador. Muro de luz = enjambre de lanzas.

### 3.2 `FairyQueenSunDance` (923) — rayos radiales (Sun Dance)
**AI:** lifetime 180; `alpha -= 15/tick` (fade-in 17 ticks); `escala = pop_in(0→20t) × fade_out(últimos 60t)`; sigue al boss (Center = boss.Center); `rotación = ángulo_inicial + rampa(+π/9 entre t=50→180)`; **emite luz física**:
```
color_luz = hslToRgb((ángulo/2π + t/180) % 1, 1, 0.85) × escala   // ¡L=0.85!
v3 = color_luz.ToVector3()
para f en 0..1 paso 1/12:                    // 13 muestras
    tile = (centro + dir · 800·escala · f).ToTile()
    CastLightOpen(tile)                      // solo ilumina tiles NO sólidos (atraviesa muros sin parar)
```
**Draw (4 capas, textura 1×2: frame0=glow, frame1=núcleo):**
```
origen  = tamaño_frame × (0.03, 0.5)          // casi el borde izquierdo → el rayo sale del centro
grosor  = (1, lerp(0.25→0.7, t 40→60)) · escala   // el rayo ENGORDA durante el ataque
hue     = ángulo/2π + t/180
capa1:  frame1, color hslToRgb(hue+0.3, 1, 0.3→0.66), escala ×1.2, A÷2, lerp a blanco 10%
capa2:  frame1, color hslToRgb(hue+0.15, 1, 0.3→0.5), escala ×1.1, A÷2, lerp a blanco 10%
capa3:  frame0 (glow), color hslToRgb(hue,1,1) ×brillo, alpha ×0.5        // blanco-ciclo núcleo
capa4:  frame1 (núcleo), color ídem ×lerp(40→60,t) a plena fuerza
enfurecida → capas1-2 se pintan con OurFavoriteColor
```
**Hitbox anidada (3 segmentos):** 510 px/grosor 70·escala · 660 px/grosor 42·escala · 800 px/grosor 7·escala — el daño "cónico" del rayo. Se dibuja en capa **detrás de los NPCs**.

### 3.3 `HallowBossLastingRainbow` (872) — las ondas/estelas curvas (Everlasting Rainbow)
**AI:** timeLeft 660; `Opacidad = fade_in(primeros 60) × fade_out(últimos 60)`; **curvatura progresiva**: `velocity = velocity.RotatedBy(ai[0])` cada tick, con `ai[0]` creciendo de 0 a π/360 en 30 ticks → primero recto, luego curva cada vez más (la "ola"). Spawn: **13 estelas** alrededor de la mano, ángulos π/2 + 2π·(i/13) + offset aleatorio, velocidad 8 px/t hacia fuera, **hue = i/13**.
**Draw:**
```
textura partida en 2 mitades horizontales: [mitad izq = CABEZA | mitad der = SEGMENTO]
cabeza:  mitad izquierda, Blanco·Opacidad con A=0, escala 0.9
estela:  TrailCacheLength = 120 (¡la mayor de todo el juego; el siguiente máximo es 80!)
         79 fantasmas (índices 79→0 del búfer), cada uno con el SEGMENTO (mitad derecha),
         color = hslToRgb(hue,1,0.5)·0.4 con A×0.6
```
**Colisión:** recorre `oldPos` (los primeros 80, paso 2) con rect 30×30 — la estela ENTERA hace daño.

### 3.4 `HallowBossRainbowStreak` (873) / `Nightglow` (931) — dardos prismáticos homing
**AI:** `Opacidad = lerp(240→220, timeLeft)` (fade-in 20 ticks). Fase "a la deriva" (timeLeft>140): `velocity ×= 0.98` y **ondulación** `velocity = RotatedBy(cos(whoAmI%6/6 + x/320 + y/160) · 2π·0.125/30)` → deriva serpenteante pseudo-única por identidad y posición. Fase homing (timeLeft<30): `velocity = SmoothStep(velocity, dirección_al_target×30, lerp(0.05→0.1))`.
**Draw:**
```
estela:      39 fantasmas (índices 39→0 del búfer, TrailCacheLength 60), escala lerp(1→1.4, j/40) —
             ¡los fantasmas CRECEN hacia atrás! — color = hslToRgb(hue,1,0.5) (873) o FairyQueenColor (931),
             alpha A÷2 × lerp(0→20, timeLeft) × (dist/90)
glow cuerpo: el sprite OTRA VEZ a 0.9 escala con color hue A=0 (tint aditivo)
cruz glow:   textura Extra[98] ×4: vertical escala (0.5,5) y horizontal (0.5,2), cada una a brillo
             pleno y a ×0.6 — una CRUZ de brillo alargada orientada a la velocidad,
             con pulso cos ±20% (periodo ~0.5 s ×3) y fade in(15-30t)/out(240-200t) ×0.8
muerte:      dusts 267 a lo largo de oldPos con color hue, cada uno CLONADO en blanco
             (scale÷2, fadeIn×0.85) — la técnica "dust + clon blanco" del hada
```

### 3.5 `HallowBossSplitShotCore` (871) — el abanico radial (director invisible)
Un solo proyectil "core" que **no se mueve** y calcula 6 "tormentas" paramétricas:
```
por tormenta i (0..5):
    ángulo_inicial = i·π/3 - π/2 + i·π/5        // 6 direcciones + desalineación extra
    ángulo_por_bala = 2π/3                        // 3 balas por tormenta, a 120°
    rango_total = 500 px, tamaño_bala = 16×16
    progreso_i(t) = rampa(localAI[0], i·10 → 90+i·10)   // cada tormenta se DESPLIEGA escalonada
posición_bala = centro + UnitX.RotatedBy(áng) · 500 · progreso   // brotan en espiral
```
**Draw:** textura 1×4 (anillo de glow animado, frame = `(j + i·6 + t/4) % 4`); cada bala = 2 pasadas: **color hue (a 1.3× escala) + BLANCO (a 1×)** — SIEMPRE doble pasada color+núcleo. Brillo por bala: `rampa(0→0.1, progreso) × rampa(1→0.8, progreso)` (aparece brillante, se apaga al final). Entrada: escala lerp(5→1) y opacidad 1-(1-t²) durante 90 ticks (implosión de recogida). Colisión = las 18 balas como rects centrados 16×16.

### 3.6 `HallowBossDeathAurora` (874) — la AURORA de pantalla (efecto wide)
Un proyectile estático (timeLeft 210) que dibuja **15 bandas verticales de arcoíris** en su posición:
```
fase = (GlobalTime % 10) / 10                          // ciclo de 10 s
por banda i (0..14):
    offsetX[i] = sin(fase·2π + π/2 + i/2) · (300 - i·3)   // cabeceo lento decreciente
    offsetY[i] = sin(fase·4π + π/3 + i)·30 - i·3          // vaivén doble frecuencia + deriva ascendente
    hue_a[i]   = i/15·2 + fase                            // 2 vueltas de arcoíris en 15 bandas
    hue_b[i]   = (sin(fase·2π+i/2)·0.5+0.5)·0.6 + fase     // variante "ondulada"
    alpha[i]   = fade_in(60) × fade_out(últimos 60) · lerp(0.2→0.5) con A÷4
    escala[i]  = (0.8 + (i+1)·(1-0.8)/15)·0.3             // bandas crecen con i
    dibujar(textura_streak, pos+offset, color hslToRgb(hue,1,0.5), 
            rot = π/2 + sin·(π/4)·(-0.3) + π·i,           // ¡cada banda espejada 180° respecto a la anterior!
            escala × (3, 6))                               // MUY estirada verticalmente
```
Aparece: al spawn del boss (decorativa, daño 0), en el ataque 3 (sobre el jugador cada 180 ticks, daño 40) — y visualmente es la misma técnica que la "death aurora".

### 3.7 `EmpressBlade` (946) — Terraprisma (ribbon + fantasmas)
**Hue por espada:** `(identity % maxMinions)/maxMinions + (GlobalTime % 3)/3`, y el ribbon usa ColorStart=hue, ColorEnd=hue+0.5 (¡gradiente de media vuelta de arcoíris a lo largo de la estela!).
**Draw:**
```
A) RIBBON de vértices (EmpressBladeDrawer): tira de triángulos sobre oldPos/oldRot (20 posiciones),
   color = lerp(Start, End, rampa(0→0.7, progreso)) × (1 - rampa(0→0.98, progreso)), A÷2,
   ancho constante 36 px, shader misc "EmpressBlade" (saturación/alpha via UseShaderSpecificData(1,0,0,0.6))
B) fantasmas de sprite: 6 a -120·k px detrás ×velocidad, color hue, alpha (1-k)·0.3·Opacidad
C) anillo: 4 copias a 4 px del centro
D) cuerpo: blanco A×0.7 + color hue A÷2
```
**Idle orbit (AI_156):** posición = `dir·(índice·-6-16), -15·gravDir` sobre el hombro + wobble circular 4 px (velocidad angular distinta por índice), rotación girando 2π/60·dir. **No emite luz de mundo significativa** (en el decompile, el proveedor de color de esa AI devuelve Transparent para 946 — todo su color vive en sprites; el Terraprisma NO ilumina el terreno). Ataque: sale disparado al target (40 ticks de ciclo), vuelve con `Center.MoveTowards(idle, 32)`.

### 3.8 Last Prism (632) — el haz prisma (referencia de "rayos largos")
```
por cada uno de los 6 láseres: hue = índice/6, color = hslToRgb(hue, 1, 0.5)
LUZ a lo largo del haz: v3 = color·0.3; PlotTileLine(centro, centro+velocidad·largo, ancho, CastLight)
   → ilumina TODOS los tiles de la línea (CastLight no para en sólidos) con el color del láser
pulso de largo: ±0.1·sin(GlobalTime·20)
punta del haz: 2 dusts/tick (267) color del láser, clonados a blanco (scale÷2); hue jitter
   lerp(color_láser, hslToRgb(hue_disco±0.4, 1, 0.75), escala/1.4)  — chispas multicolor
shader de agua (WaterDistortion) alimentado por el haz  → refracción del terreno
```

---

## 4. EL NPC (636) — anatomía del jefe de luz

**Constantes:** 70000 HP, damage 80→9999 (día), defense 50 (×1.2 en fase 2), width/height 100, Opacity arranca en **0** (fade-in de 180 ticks al spawn), `dontTakeDamage` durante no-ataque (¡solo recibe daño durante los ataques: `dontTakeDamage = !flag6`!).

**Estados (ai[0])**: 0=spawn (aurora 874 + lluvia de dusts arcoíris 180 ticks) · 1=selector cíclico (n/10 o n/9 ataques, cambia a fase 2 bajo 50% HP) · 2=bolts (873 cada 3 ticks ×60, hue=t/60) · 3=aurora sobre el jugador · 4=lances (919 cada 4 ticks ×100 con lead 90t) · 5=everlasting rainbow (13×872) · 6=sun dance (6-8×923 cada 60t ×3 oleadas) · 7=radiance (muros de 919) · 8/9= dashes laterales (±550 px del jugador) · 10=transición fase 2 (teleport sobre el jugador -250 px) · 11=muro experto · 12=espiral experto (873 hue=(t-10)/50) · 13=despawn (alpha ±5/tick + dusts).

**LUZ DEL NPC — la única que emite:**
```
Lighting.AddLight(Center, Vector3.One × Opacity)     // BLANCO puro, intensidad = visibilidad (0→1)
```
**Tintado del sprite (GetAlpha para 636):** `lerp(color_luz_del_mundo, Blanco, 0.25) × Opacidad` — el sprite se aclara un 25% hacia blanco SIEMPRE (look "de luz") y se multiplica por la opacidad global. Sus 2 frames = normal / fase 2 (enraged, frame.Y = alto).

**`DoMagicEffect(punto, tipo, progreso)` — el "aura de hechizo" con dusts (267 y 86-92):**
```
tipo 1 (corona, 60 ticks): 2 dusts/tick en anillo radio 4, color hslToRgb(rnd, 1, 0.5), fadeIn 2
tipo 2/4 (ojos ±55,-20): 4 dusts orbitando (2π·i/4 + π/4)·8 px, hue = i/5 + 0.5·(tipo4) + progreso·0.5,
      A÷2 + alpha 127, escala 3·campana(progreso)        // los ojos ARCOÍRIS girando
tipo 3 (mano): radio 30, fadeIn 2.5
tipo 5 (dash): dusts 86-92 con customData=NPC (¡los dusts persiguen al boss!), al inicio blancos
      (255,255,255,80)·0.3 ascendiendo; luego hue = progreso·2 con velocidad ×3 + boss·1
```
**Hit/dusts de daño:** por cada punto de vida (dmg/lifeMax·100) un dust 67 o 69 (los "destellos de cristal"). **Muerte:** 50 dusts + 9 gores (1262-1268: fragmentos de alas).

---

## 5. TABLA DE LECCIONES (con números concretos)

| # | Lección | Parámetros exactos |
|---|---|---|
| 1 | **Color = HSL puro con hue significativo** | hslToRgb(hue%1, 1, 0.5) cuerpo; L=0.85 luz emitida; L=1.0 núcleos; L=0.3-0.66 capas externas. Hue de: progreso/tick total (bolts t/60), índice/n (13 estelas → i/13), ángulo/2π (rays), identity%n+tiempo/3 (Terraprisma) |
| 2 | **Doble pasada color+núcleo blanco** | TODO se dibuja 2 veces: capa de color (hue, A÷2, escala ×1.1-1.4) + capa blanca (A÷2, escala ×1.0). El color SIEMPRE es más grande que el blanco (halo) |
| 3 | **Afterimages por oldPos con fantasmas que crecen** | 873: 39 fantasmas (índice 39→0, TrailCache 60)/escala lerp(1→1.4 con j/40) · 872: 79 fantasmas/TrailCache 120 (récord del juego) · 919/946: 6 fantasmas manuales a -120·k px, alpha (1-k) · fade lineal dist/(cache×1.5) |
| 4 | **Telegrafía con haz** | Lanza: 3600 px de raya estirada (origen en borde), 2 pasadas (media longitud brillante + completa ×0.3), visible t∈[10,55], A=0 (aditivo) — luego vuela a 40 px/t con 6 fantasmas |
| 5 | **El glow del cuerpo = sprite repetido + cruz de brillo** | 873: sprite tintado A=0 a 0.9× + textura glow ×4 dibujos (vertical (0.5,5)·p, horizontal (0.5,2)·p, y ×0.6 cada una) con pulso cos ±20% |
| 6 | **Luz de mundo = muestras, no posición única** | SunDance: 13 muestras cada 800/12 px con CastLightOpen (solo aire) color L=0.85 ×escala · LastPrism: PlotTileLine color×0.3 todo el haz (a través de sólidos) · NPC: 1 luz blanca = Opacidad |
| 7 | **El arcoíris espacial** | 13 estelas con hue=i/13 simultáneas = arcoíris PERFECTO visible en un solo frame; alternativa temporal: hue=t/60 en ráfagas |
| 8 | **Enrage = cambio de PALETA, no de efecto** | Blanco→(255,231,69) dorado con rampa 60 ticks; damage 9999 + no-dodgeable; defense ×1.2 |
| 9 | **Fade-in/out simétrico en tiempo de vida** | Patrón `GetLerpValue(0,60,timeLeft) × GetLerpData(total,total-60,timeLeft)` en TODOS (aparece y muere suave). alpha -= 15/tick (17 ticks de fade) |
| 10 | **Los "rayos" son sprites estirados + hitbox anidada** | SunDance: grosor lerp(0.25→0.7), 4 capas (2 color ×1.2/×1.1 + glow ×0.5 + núcleo), hitbox 3 tramos (510/70, 660/42, 800/7 px·escala), dibujado detrás de NPCs |
| 11 | **Aurora de pantalla = 15 bandas espejadas** | rot=π/2±vaivén+π·i (alterna 180°), escala (3,6), 2 vueltas de hue en 15 bandas, hue_ondulado opcional (sin·0.6), A÷4, sinos de 2 frecuencias (fase·2π y 4π) |
| 12 | **Ribbon de vértices para estelas premium** | EmpressBladeDrawer: tira sobre oldPos, ancho 36 px, gradiente hue→hue+0.5 a lo largo, fade (1-rampa 0.98), A÷2, shader misc con UseShaderSpecificData(1,0,0,0.6) |
| 13 | **Dust 267 "hada" + clon blanco** | Siempre: dust color hue (escala ×1.2-1.5, fadeIn 0.6-2.5, noGravity) + Dust.CloneDust→blanco (escala ÷2, fadeIn ×0.85). Usado en TODO (spawn, muerte, dash, lances) |
| 14 | **Director invisible** | 871/923 son "proyectiles núcleo" invisibles que calculan N sub-balas paramétricas (18 balas / 6 tormentas) — un solo netID para todo el patrón |
| 15 | **La boss entera emite BLANCO** | Luz = Vector3.One×Opacity (no prismática en mundo); el prisma vive en sprites/dusts; su sprite se aclara lerp(luz, blanco, 0.25) |

---

## 6. APLICACIÓN DIRECTA A LA LIBRERÍA DE LUZ DE AETHONMOD

1. **Un solo helper de color:** `ColorPrismatico(hue, L=0.5, alfaMult=0.5, lerpBlanco=0.1)` replicando la doble pasada (los VFX del mod ya hacen glow doble — ahora con HSL en vez de paletas fijas).
2. **El patrón Lanza** (para lanzas/rayas de las armas ascendidas): telegrafía 60 ticks con raya estirada (media longitud brillante + completa tenue) → disparo 40 px/t + 6 fantasmas -120 px + anillo 4×2 px + cuerpo blanco/1.1× color.
3. **Rayos radiales** (Sun Dance): sprite estriado 1×2, grosor animado 0.25→0.7, 4 capas, y **luz de mundo muestreada** cada 1/12 del largo con L=0.85 — reemplaza el `AddLight` puntual actual por `PlotTileLine`/muestreo.
4. **Ondas curvas** (872): búfer de 120 posiciones + 79 fantasmas con SEGMENTO de textura + cabeza blanca; curvatura `RotatedBy(ε creciente)` — esto ES la "onda de luz" que el usuario quiere para las ondas cromáticas de Aethon.
5. **Hue por identidad** en las espadas siervas/orbitales de coronas: identity%n/n + GlobalTime/3.
6. **Aurora de pantalla** (para muertes de bosses): 15 bandas espejadas — combinable con el CosmicShockwave existente como "aurora" final.

## 7. NOTAS DE RESPALDO (otros mods)
- **Coralite**: sin rework de la Emperatriz. Partícula `RainbowHalo`: aditiva, rota +0.05/tick, escala ×1.04, color ×0.86 tras 8 ticks, muere a 18 — el "halo arcoíris" más simple posible (patrón útil para bursts).
- **Everglow/LunarVeil**: sin contenido EoL relevante localizado.

*Todo el código de este informe está parafraseado a pseudocódigo propio a partir del decompile con ILSpy; no se copia código literal al repo (el dump completo queda en /tmp/research/terraria_src para consulta de esta sesión).*

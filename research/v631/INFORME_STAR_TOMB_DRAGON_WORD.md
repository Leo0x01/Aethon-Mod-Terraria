# INFORME R3 — LIGHT OF THE STAR TOMB (星墓之光) + DRAGON'S WORD (龙言)
**Task ID 50 · Agente R3 (investigación, sin código de producción) · v6.31**

Petición: investigar a fondo DOS armas del mod **Calamity Overhaul (CWR / 灾厄重铸)**,
cada una con **DOS formas de uso** (= 4 fichas), para que otro programador cree 4 armas
nuevas en Aethon (temática cosmos/estrellas/soles/agujeros negros/desgarros/eclipse)
copiando proyectiles/animación/técnica/uso.

Método: **el código fuente completo de ambas armas se descargó del repo oficial**
(`github.com/hocha113/CalamityOverhaul`, rama `main`, v0.9209 — árbol completo en
`search_results/repo_tree.json` con 8.068 entradas) y se leyó **línea a línea**:
ítem + holdout + 3 proyectiles + 1 partícula propia + 4 shaders .fx completos
(`DragonsWordFX`, `NeutronPulsar`, `NeutronPulseBeam`, `NeutronWarp`) + 8 tipos de
partículas PRT + 2 debuffs + BaseHeldGun (la base holdout) + localización zh/en +
wiki oficial CN/EN (4 páginas) + changelog 0.9205 + sprites medidos con PIL.
El servicio z-ai `web_search` siguió en 429/timeout (igual que en R1); las búsquedas
de respaldo (Bing/DDG) devolvieron basura localizada, pero **no hizo falta**: el
código + wiki oficial cubren el 100 % de lo pedido. 24 archivos fuente en
`search_results/t50/`.

---

## 0) IDENTIDAD DE LAS DOS ARMAS (resumen ejecutivo)

| | **Light of the Star Tomb · 星墓之光** | **Dragon's Word · 龙言** |
|---|---|---|
| Clase | **Magia** (varita/wand, holdout estilo "pistola a una mano") | **Magia** (tomo endgame) |
| Tier | Endgame absoluto — línea de armas de **neutron stars** (post-SCal; la línea completa: 洛希之弦 Roche's String, **星墓之光**, 黑域斩切, 恒星猎手, 黑洞使者) | Endgame absoluto — se craftea con **Subsuming Vortex + 39 Yharon Soul Fragment + Rock** (post-Yharon + Boss Rush) |
| Daño base | **355** | **682** |
| useTime / mana / crit | 26 / 16 por siembra / +6 % | 60 / 80 por casteo / — |
| Fabricación | 11 × NeutronStarIngot en la forja de Draedon (sin Calamity: Ancient Manipulator). El lingote = 1 barra de CADA era del juego + 13 Shadowspec | SubsumingVortex + 39 × YharonSoulFragment + Rock, forja de Draedon (requiere Calamity) |
| Forma 1 (clic izq.) | **Sembrar un Pulsar** que frena, ancla en el cursor y barre el campo con 2 haces de faro | **3 Lágrimas de Dragón** que orbitan 2,5 s, luego persiguen y estallan |
| Forma 2 (clic der.) | **Frenado magnético** (mantener) → **Starquake** (soltar): cosecha | **Círculo del Decreto del Dragón**: área que crece mientras se paga maná y ejecuta enemigos cada 0,25 s |
| Diseño | "Las dos teclas son recursos mutuos": la estrella es el SUSTENTO, el starquake es la COSECHA | Un solo círculo a la vez; mientras vive bloquea ambos casteos |
| Lore | "El psíquico quería aplastar estrellas en agujeros negros, pero solo produjo restos congelados en materia degenerada. Los nómadas llaman a ese rincón del cielo la Tumba de Estrellas" | "Un dragón solo puede derramar una lágrima una vez en su vida" |

**Los dos nombres en chino** (importante para futuras búsquedas): 星墓之光 (no "星辰墓"),
龙言 (no "龙语" ni "龙之语" — "龙言" = literalmente "palabra-dragón"). Nombres internos
de clase: `NeutronWand`/`NeutronWandHeld`/`NeutronPulsar`/`NeutronStarquake` y
`DragonsWord`/`DragonsWordProj`/`DragonsWordMouse`/`DragonsWordCut`.

### El patrón técnico común (CLAVE para Aethon)

1. **Proyectil invisible + VFX 100 % procedural.** TODOS los proyectiles de ambas
   armas usan `Texture = "InnoVault/Assets/placeholder"` que es un **PNG de 1×1 px
   TOTALMENTE TRANSPARENTE** (verificado descargándolo). El proyectil es pura
   lógica/hitbox; todo el dibujo son shaders sobre quads (`placeholder2` = 1×1
   blanco) + partículas PRT. `PreDraw() => false` donde hace falta.
2. **Holdout de dos botones.** El ítem tiene `noUseGraphic = true` +
   `ItemUseStyleID.Shoot`; `Shoot()` solo hace `SpawnHeldProj<T>()` que crea UN
   proyectil "held" pegado al jugador (`heldProj`, `timeLeft = 2` auto-renovado,
   se autodestruye al soltar salvo `StayAlive()`). Ese holdout decide en su `AI()`
   qué pasa con clic izquierdo (`WantsFireLeft`) o derecho (`WantsFireRight` =
   `CanRightClick && DownRight && !DownLeft`). El arco de arma se dibuja con
   `GunDraw` y un spritesheet vertical animado a 5 ticks/frame.
3. **Hitbox custom por `Colliding()`**: la caja del proyectil se pone GIGANTE
   (1240 px en el neutron; 122 en el decreto) **solo para que el motor llame a
   `Colliding`**, y dentro se hace la prueba real: círculo núcleo + **2 líneas
   (`Collision.CheckAABBvLineCollision`) por los dos polos magnéticos** o un
   anillo de impacto con vacío central.
4. **Shaders .fx ps_3_0 con disciplina documentada** (en los propios comentarios
   del archivo): (a) salida **premultiplicada** consumida con `BlendState.AlphaBlend`,
   o aditiva; (b) **el ángulo polar JAMÁS entra en ruido** (`atan2` solo en
   `floor/frac` discretos, o `cos(k·θ)`, o el vector dirección rotado — si no,
   salta una línea dura en el semieje −x); (c) toda máscara debe **cerrar antes
   del borde del quad** (si no, el tiling/blend corta un rectángulo brillante);
   (d) "recta aritmética sin ramas dinámicas".
5. **Sistema de partículas PRT** (InnoVault): pooled, con `Configure(...)` fluido,
   modos de dibujo (Additive/NonPremultiplied/AlphaBlend), y partículas que son
   mini-sistemas completos (vórtices keplerianos, anillos con perlas de Baily…).
6. **Warp de pantalla por mapa de desplazamiento** (`NeutronWarp.fx`): un solo
   quad a pantalla con el shader como `BlendState` del SpriteBatch dibuja un
   píxel estirado; el shader escribe R=dirección, G=intensidad; **la pluma del
   borde va en G, nunca en A** (el consumidor no lee A). 5 técnicas:
   GravitationalVortex / ShockwaveRing / RelativisticJet / GravitationalLens /
   KamuiLine.
7. **MP sincronizado**: `netImportant`, `SendExtraAI/ReceiveExtraAI` para el estado
   de carga/fase, spawn de daño solo en `IsOwnedByLocalPlayer()`, sonidos/FX
   "self-played per-end by synced state", presupuesto de sonido (máx 2
   `DragonsWordCut` por frame global).

---

## FICHA 1/4 — STAR TOMB, FORMA 1: SEMBRAR PÚLSAR (clic izquierdo)

### Cómo se activa
- Mantener/soltar clic **izquierdo** con el holdout activo. Cadencia = `Item.useTime
  (26) / AttackSpeed`, `autoReuse`. **Coste: 16 maná por siembra** ( PayMana() ).
  Al soltar el botón el holdout sobrevive mientras `quakeCharge > 0` o
  `muzzleFlash > 0` (transiciones limpias).
- El ítem NO dispara directamente: `Shoot()` → `BaseHeldGun.SpawnHeldProj<NeutronWandHeld>()`
  (un solo holdout; `CanUseItem` lo exige).

### Identidad del proyectil `NeutronPulsar` (脉冲星)
| Campo | Valor |
|---|---|
| Daño | `WeaponDamage × 0.62` = **220** (con 355 base) |
| Vida | **660 ticks (11 s)** — coincide con el wiki "unos 11 s" |
| Fase de frenado | **26 ticks**, `velocity *= 0.885` por frame; `TravelFactor = 7.375` (serie geométrica k·(1−kⁿ)/(1−k) — la holdout **resuelve la velocidad inicial = distancia_al_cursor / 7.375**, clamp 8..64 px/tick, para que la estrella **aterrice exactamente en el cursor**) |
| Spin-up | 120 ticks: `SpinRate = lerp(0.03, 0.135, t²)` rad/tick tras anclar (×(1−0.85·quake) si la están frenando) |
| Anclaje | `ShouldUpdatePosition() => !Anchored` → a los 26 ticks `velocity = 0` para siempre |
| penetrate | −1 (infinito) · `localNPCHitCooldown = 10` (un golpe por objetivo cada 10 ticks) |
| **ArmorPenetration** | **80** |
| ai[0] | fase del eje magnético al nacer (`Main.rand.NextFloat(TwoPi)`), viaja en el paquete de spawn |
| Caja | 1240×1240 (solo para que `Colliding` corra) |
| Tope | **3 estrellas** (MaxPulsars). Sembrar una 4ª → la MÁS VIEJA sufre `TriggerQuake(0.35, forced:true)` ("la siembra nunca es un cast desperdiciado") y muere al terminar su superación |

### Colisión (cápsula núcleo + 2 líneas-polares)
```csharp
core:  CircleIntersectsRectangle(center, 26f, hitbox)               // siempre
beams: CheckAABBvLineCollision(..., center, center ± axis*(len), 17f)
       len = 560 * BeamReach * 0.86 ;  rad = 17 * (glitch ? 2.2 : 1)
```
El eje es `MagAxis = spinPhase + MagPhase` (el eje magnético está DESALINEADO del
eje de spin → los haces barren como un faro). **Envolvente de faro**:
`BeamPhase = |cos(spinPhase)|^5` (pico dos veces por vuelta — los dos polos).
`BeamReach = (1 − quake·0.55) · (glitch ? 1.55 : 1)`, `BeamHalfWidth = 30 · (1 −
quake·0.2) · (glitch ? 2.2 : 1)`. Mientras vuela (no anclada) SOLO daña con el
núcleo (26 px). FadeIn de 8 ticks; no daña antes de FadeIn < 0.2.

### Daño y debuff
- Golpe → **VoidErosion (虚空侵蚀) 1200 ticks = 20 s** + 5 partículas de impacto.
- Con debuff activo el NPC "se desintegra" (bool en CWR-NPC data).
- Gravedad débil de arrastre (solo anclada): radio **220 px** (+120 en glitch),
  tira de NO-boss con `velocity += dir · 2.6 · (1−d/R)² · knockBackResist`,
  mínimo 24 px para no aplastar contra el centro.

### ANIMACIÓN — exactamente cómo se ve
**Paleta (constantes del código):**
| Nombre | Vector3 | RGB | Uso |
|---|---|---|---|
| ColHot | (0.78, 0.84, 1.00) | **(199,214,255)** | núcleo degenerado blanco-azul |
| ColMain | (0.54, 0.31, 1.00) | **(138, 79,255)** | violeta neutrón |
| ColBeam | (0.47, 0.71, 1.00) | **(120,181,255)** | azul frío de la magnetosfera |
| ColDeep | (0.12, 0.10, 0.50) | **(31, 26,128)** | base profunda de la corteza |
| ParticleViolet / Blue / Hot | — | (138,80,255) / (120,180,255) / (199,215,255) | partículas |

**Cuerpo** (shader `NeutronPulsar.fx`): un quad de **260 px** (×1.18 en glitch) con
DOS técnicas:
- **`Crust`** (AlphaBlend premult) — "la estrella de neutrones NO es un agujero
  negro: tiene corteza dura, magnetosfera y spin rapidísimo; el cuerpo es un
  sólido brillante, no un hueco": placas Voronoi (`Extra_193.png`, 256×256 gris,
  2 octavas 1.5×/4.1×) = **placas tectónicas**; las costuras entre placas =
  grietas de corteza que **se encienden a blanco según `uQuake`** (calor =
  `quake·1.2 + glitch·0.8`, crack color lerp violeta×1.35→hot×2.3); **efecto
  Doppler**: `1 + cos(θ−spin)·(0.26+spinRate·0.62)` — el lado que se acerca al
  jugador brilla más y el brillo BARRE con la rotación; **limb brightening** en
  0.78R..1.0R (la lente comprime la corteza del fondo sobre el borde);
  **compresión gravitatoria** de coordenadas `comp = 1/(z·0.34+0.16)` (la textura
  se apila más densa hacia el borde — "ves la parte de atrás"). Radio visible
  `uRadius = 0.085` del semi-quad ≈ **22 px de radio** (núcleo). Durante el vuelo
  se estira `squash = 1+clamp(v/30, 0, 0.55)` a lo largo de la velocidad.
- **`Field`** (Additive) — la magnetosfera: **anillo de fotones** en r=1.42R
  (gaussiana exp(−((r−1.42)·9)²)); **líneas de campo dipolares reales**
  `r = L·sin²(latitud)` — iso-L dibujadas con `frac(shell·2.1 − t·0.3)` (se
  retuercen y comprimen ×(1+quake·0.8) + ruido durante el frenado), jaula entre
  0.98R y 4.4R→2.1R; **casquetes polares** `|cos(θ−magAngle)|^24` = las dos
  manchas calientes DONDE NACEN LOS HACES; **halo de estrés** exp(−(r−1)²·1.96)·quake.

**Haces** (shader `NeutronPulseBeam.fx`, `DrawUserPrimitives(TriangleStrip, 4 verts)`
— **UN QUAD por polo**, raíz enterrada 14 px dentro de la corteza):
- Concepto: **"cono HUECO, no barra sólida"** — el plasma es ópticamente delgado,
  la PARED del cono se ve más brillante que el eje: `wall` en d∈[0.3..1.06],
  `core = exp(−d²·2.6)·0.42`, **eje caliente finísimo** `exp(−d²·90)·(0.5+glitch·0.7)`.
- Cono: `coneHalf = lerp(0.10, 0.86, along^0.68)` — nace de 3 px en el casquete
  polar y se abre hasta ~26 px de semiancho en la punta; **la estrechez la hace
  el shader, los vértices van a ancho constante** (comentario: "si no, dos
  estrechamientos estrangulan el haz en una línea").
- Punta rota por ruido (ventana de rotura cierra en along=0.86 antes del borde
  del quad), **nudos de choque** viajando por el haz (`frac(along·3.2 − t·1.25)`),
  caída ~1/(1+along·2.1), raíz suavizada 0→0.09.
- Intensidad global × `(0.35 + BeamPhase·0.65)` → **el haz LATE con el faro**.

**Warp de lente gravitatoria** (`NeutronWarpHelper.DrawWarp(…, "GravitationalLens")`):
siempre activo sobre la estrella: intensidad `(0.16 + glitch·0.3 + quake·0.22)·FadeIn`,
tamaño **210 px (+130 en glitch)**. El shader: deflexión GR `1/(r²+0.08)`, anillos
de Fresnel `sin(r·10π)`, anillo de Einstein en 0.42R, centelleo `sin(3θ+t·4)`,
**dirección radial hacia DENTRO** (lente que concentra, no explota).

**Partículas** (PRT, todas medidas):
| Emisor | Cuándo | Qué |
|---|---|---|
| Estela de vuelo | mientras frena | 1..5/frame `PRT_HeavenfallStar` (estrella 4 puntas `StarTexture_White`, estirada (0.2, 1.6)×, escala ×0.95, muere pow³, gravedad si v<12) |
| Anclaje (1 vez) | tick 26 | `PRT_StarPulseRing` (anillo `DiffusionCircle4` 0.2→1.5 en 22 ticks + bloom + estrella central 39 px/escala) + 14 estrellas radiales v 3..8 |
| Acreción | cada 6 ticks | `PRT_GravityVortex`: **órbita kepleriana real** (`ω = 0.08/max(r·0.01, 0.3)`, r ×0.97 − 0.3/frame, min 2) desde radio 70..140 px, vida 34..52, **corrimiento al azul cerca del centro** lerp→(180,200,255) |
| Flujo polar | cada 3 ticks | `PRT_Spark` (estela `Extra_98` 72×72, doble dibujo ×(0.5,1.6) y ×0.45 ancho) lanzada a lo largo del eje magnético ±(26..46 px), v 5..12 ×(1+glitch) |
| Fuga de corteza | si quake>0.25, cada 2 ticks | `PRT_SpaceFracture`: línea `LightBeam` fina (0.05, 0.18)×, rotación con velocidad angular, **"aparece rápido, muere en pico"** fadeIn ×8 / fadeOut pow2.5, color se apaga a (60,20,80) |
| Muerte natural | OnKill | sonido Item122 + anillo 0.35→2.1 + 20 SpaceFracture + 10 vórtices (la corteza "evapora" — sobrevive a su cuerpo) |

**Iluminación**: `Lighting.AddLight(center, ColMain · (0.55 + BeamPhase·0.5 + Glitch01·0.8))`
— la luz VIOLETA late con el faro.

**Sonidos**: siembra Item4 (pitch −0.55) + Item88 (−0.35); anclaje Item29
(vol 0.55, pitch 0.35). Sacudida de pantalla al sembrar: **2.2**.

**Muzzle flash** (en la holdout): 6 ticks, textura `StarTexture` **estirada
(0.55, 0.13)·vida a lo largo del ángulo de disparo** (¡una cruz/estrella aplastada
en un destello direccional!), doble pasada azul + blanco-caliente×0.45, lote
Additive propio. + 9 `PRT_Spark` cono ±0.35 rad, v 3..11, lerp violeta→hot.

### El ítem (NeutronWand)
- Sprite **78×960 = 12 frames de 78×80** animados a **5 ticks/frame**
  (`DrawAnimationVertical(5, 12)`; la holdout cicla `ClockFrame(frame, 5, 11)`).
  Paleta medida: grafito/negro + blancos + acentos **magenta (200,79,159)**.
- Stats: 32×32, daño 355, mágico, use 26, `Shoot` = NeutronPulsar (pero el Shoot
  real solo spawnea la holdout), `shootSpeed 15`, `crit 6`, `autoReuse`,
  `noMelee`, `noUseGraphic`, rare Roja, compra 15 plat 3 oro 5 plata.
- Pose de la holdout (estilo pistola a una mano): `HandIdleDistance (52, −20)`,
  `GunPressure 0.32`, `ControlForce 0.08`, `RecoilRetroForceMagnitude 9`,
  `RecoilOffsetRecoverValue 0.62`, `AlwaysAimPose`, `Onehanded`,
  `MuzzleForwardOffset 20` — **la varita se comporta como una pistola mágica**
  (apunta siempre al ratón, retrocede al sembrar).

---

## FICHA 2/4 — STAR TOMB, FORMA 2: FRENO MAGNÉTICO → STARQUAKE (clic derecho mantener/soltar)

### Cómo se activa
1. **Mantener clic derecho**: si hay ≥1 estrella "frenable" en campo
   (`CanBrake = !Glitching && !forcedOut`), `quakeCharge` sube **1/55 por tick**
   (**55 ticks ≈ 0.92 s** para llenar — el wiki dice "≈1 s"). Si no hay estrellas,
   la carga se resetea (la dependencia entre teclas se auto-explica).
2. Cada frame carga TODAS las estrellas propias: `pulsar.DriveQuake(quakeCharge)`.
3. **Soltar**: si `quakeCharge > 0.06` → `ReleaseQuake()`: cada estrella ejecuta
   `TriggerQuake(power)`. Si sueltas sin cargar, la carga se desvanece sola.
4. **La forma 2 NO gasta maná** (el wiki lo confirma: solo la siembra cuesta).

### El proyectil `NeutronStarquake` (星震)
| Campo | Valor |
|---|---|
| Daño | `Projectile.damage(pulsar=220) × (1.4 + power·1.1)` = **308 (power 0) … 550 (power 1)** |
| Vida | **34 ticks** |
| Radio de impacto | `Reach = (250 + 330·power) · EaseOutCubic(progress)` → **hasta 580 px** |
| Fade | `1 − EaseInQuad(clamp((p−0.35)/0.65))` — pleno hasta el 35 % de vida |
| penetrate | −1 · `localNPCHitCooldown = 8` · **ArmorPenetration 100** |
| ai[0] | Power (0..1), viaja en el spawn |
| Knockback | `pulsar.knockBack × 2` |

**Colisión = SOLO EL FRENTE del anillo** (comentario: "solo se juzga la circunferencia
del frente, el vacío central no golpea dos veces"): si el objetivo toca el círculo
`reach` → hit; pero si además está DENTRO de `reach·0.45` (vacío central), solo
cuenta durante los primeros 8 ticks. **Come proyectiles hostiles**: cualquier
proyectil enemigo a < `Reach·0.7` es `Kill()` (la reconexión magnética "los
destroza").

**Debuff**: **VoidErosion 1800 ticks = 30 s** + 6 estrellas de impacto.

### El disparador en la estrella (`TriggerQuake(power, forced)`)
- `glitchTimer = 150 · (0.55 + power·0.45)` = **82..150 ticks de VENTANA DE
  SUPERACIÓN (overclock)** para la estrella superviviente.
- Sonidos: **Item62** (vol 0.9, pitch −0.45) + **Item94** (vol 0.7, pitch
  0.35+0.3·power). Sacudida: **4 + 4·power**.
- **Durante el overclock** (Glitch01 = glitchTimer/150 → 1..0):
  - `SpinRate = 0.32` fijo (**≈4,7× el spin máximo normal** — 3 rev/s);
  - `BeamReach ×1.55` (haz de **868 px**), `BeamHalfWidth ×2.2` (66 px),
    radio de golpe ×2.2 (37,4 px);
  - **`ModifyHitNPC: daño ×1.6`** (la estrella golpea más fuerte);
  - radio de arrastre 220→**340 px**;
  - `uGlitch` alimenta todos los shaders (corteza agrietada blanca, casquetes
    ×3, jaula retorcida, warp de lente 340 px con intensidad +0.3).
- Al morir por desborde (forcedOut) la estrella se mata al terminar el overclock.

### ANIMACIÓN del starquake
- **Carga (frenado)** — se VE en las estrellas y en la boca del arma:
  - las estrellas: spin visible decae (×0.15), el haz se acorta ×(1−0.55·quake)
    y se estrecha, la corteza se agrieta encendiéndose (`uQuake`), la jaula de
    líneas se COMPRIME y ENREDA (×(1+0.8·quake) + ruido), chispas de fuga por
    las grietas cada 2 ticks si quake>0.25;
  - la holdout: **tether de frenado** cada 2 ticks en la boca del arma —
    `PRT_GravityVortex` con radio **34..80 px** según carga, color lerp
    azul→hot ("la lectura de carga crece ESTRUCTURAL, no en una bola de luz").
  - sonido de inicio de carga: Item77 pitch −0.35.
- **Estallido**: sonido DD2_ExplosiveTrapExplode vol 1.0 pitch −0.5;
  `PRT_StarPulseRing` blanco-caliente "2 ticks de sobre-exposición" escala
  2.6+1.4·power; **rayos de reconexión**: 10+8·power radios, cada uno con 4
  SpaceFracture en abanico (18, 31, 44, 57 px del centro, v 9..20 ×(0.7+0.7·power),
  color hot→violeta a lo largo del rayo).
- **Frente visible**: 6 estrellas/frame los primeros 6 ticks, luego 2/frame,
  SIEMPRE sobre el frente (radio·0.86..1.04) — "para que la superficie de choque
  se vea dónde está".
- **El cuerpo del starquake**: reutiliza el shader `NeutronPulsar.fx` técnica
  **`Field`** (¡la jaula magnética entera EXPULSADA!): quad de lado
  `max(Reach·2.9, 90)` px, `uQuake = 1−0.6·progress`, `uGlitch = 1`,
  `uSpinRate = 1`, `uFade ×(1.15+0.5·power)` — el casquete polar y el anillo de
  fotones se abren con el frente.
- **Warp**: `ShockwaveRing` de tamaño `max(Reach·2.4, 120)`, intensidad
  `0.5·fade + 0.2·power`. El shader: **3 frentes concéntricos** (principal en
  `progress·1.2` + reflejo ×0.6 + tercero ×0.3, anchos que crecen con el
  progreso), residuo de colapso central `(1−progress)`, **ripples de alta
  frecuencia** `sin(r·28−t·7)`, y ruido azimutal alimentado con el **vector
  dirección rotado** (lección de costura documentada en el propio .fx).

### Números de la economía completa (minijuego interno)
- Siembra: 220 dmg, 16 maná, 26 ticks CD, hasta 3 estrellas, 11 s de vida.
- Sustain de una estrella anclada: núcleo 26 px + 2 líneas 560×17 px con i-frames
  10 ticks → **hasta ~6 golpes/s** por objetivo si el faro le pasa encima.
- Cosecha: 308..550 dmg en anillo de hasta 580 px + 30 s de debuff + come balas
  + deja a las supervivientes 1,4..2,5 s a ×1.6 con haces de 868×37 px.
- Cadencia real de cosecha: carga 55 ticks + la mano vuelve a la pose →
  ~1 starquake/s sostenido con 3 estrellas en campo.

---

## FICHA 3/4 — DRAGON'S WORD, FORMA 1: LÁGRIMAS DE DRAGÓN (龙泪, clic izquierdo)

### Cómo se activa
- Clic izquierdo normal: `Shoot()` (el ítem SÍ castea directo, sin holdout de
  arma — solo `noUseGraphic` NO está; el libro se dibuja con el sprite + capa
  `Glow` dorada). Cada uso: **80 maná, use 60** (1 casteo/s), `autoReuse`.
- **3 lágrimas por casteo**, en abanico rotante:
  `ángulo = 2π/3·i + GameUpdateCount·0.1` → nacen a 22..38 px del centro del
  jugador en 3 direcciones que giran lentamente entre casteos, velocidad
  `dir.RotatedByRandom(0.32) · 3` px/tick.
- Sonido de casteo: **Item92**.

### Identidad del proyectil `DragonsWordProj` (龙言之刃)
| Campo | Valor |
|---|---|
| Daño | 682 por lágrima (cada una a daño completo) |
| `extraUpdates` | **6** (7 sub-pasos por frame — persecución sedosa) |
| Vida | `1220 × 6` sub-ticks ≈ **174 frames ≈ 2,9 s** … ver fases |
| Hitbox | 32×32 · `localNPCHitCooldown = 14` (sub-ticks) |
| `MaxHits` | **18** golpes manuales contados → luego explota (no `penetrate` de motor) |
| Búsqueda | `FindClosestNPC(1600)` |

### Fases del movimiento (el "esqueleto" del código)
1. **t < 150 frames (2,5 s): ÓRBITA** — `velocity = velocity.RotatedBy(SpinRate)`
   (SpinRate = ai[1] = 0.03 rad/sub-tick): la lágrima gira en círculo alrededor
   de su punto de nacimiento. **NO daña** (`CanHitNPC => Time < 150×eu ? false`).
2. **Tick 150: ENCENDIDO** (`IgnitionFX`): 4 DawnEmber + sonido Item20 (vol 0.25,
   pitch 0.4) — "la lágrima entra en filo, el núcleo blanco se eleva".
   `Heat01 = 0.35 + 0.65·clamp((t−150)/25)` — se calienta en 25 frames.
3. **t 160..290 frames (2,2 s): persecución suave** — `SmoothHomingBehavior(
   target.Center, 1, 0.08)` (curva hacia el objetivo).
4. **t > 290: mordisco a velocidad fija** — `ChasingBehavior(target.Center,
   velocity.Length())` (ya no acelera, mantiene el módulo).
5. **Al llegar el fin de vida O 18 golpes → `EnterFade()`**: **PRIMERO explota,
   LUEGO se vuelve inofensiva** (comentario: "si no, la puerta del fade bloquearía
   el daño de la explosión"). Fade 98 sub-ticks (~14 frames) con `velocity ×0.82`
   por sub-tick y la cinta se erosiona 2 puntos/frame desde la cola. `PreKill`
   también explota (muerte externa).

**Golpe**: **Dragonfire (龙火) 420 ticks = 7 s** — el debuff de Yharon: **−480 HP/s
a enemigos (505 con Oiled)** + 2 DawnEmber al 50 %. La explosión final:
`Projectile.Explode()` (extensión InnoVault, radio estándar) + PRT_DawnRing
(20 px, expand 4.5, 0.55, 14 ticks) + 10 DawnEmber.

### ANIMACIÓN — la lágrima y su cinta (shader `DragonsWordFX.fx`, técnica `TechTear` + `TechTrail`)
**Paleta del arma (compartida por las 4 técnicas)**:
| Nombre | RGB | Uso |
|---|---|---|
| HotGold | **(255,214,110)** | oro caliente |
| MoltenOrange | **(255,128,36)** | naranja fundido |
| EmberRed | **(214,58,22)** | brasa roja |
| FireRamp (shader) | (0.10,0.035,0.025)→(0.48,0.09,0.035)→(0.98,0.40,0.09)→(1.05,0.80,0.34) | rampa de enfriamiento **chamuscado→rojo profundo→naranja→oro** |

- **Cuerpo = LÁGRIMA DE METAL FUNDIDO** (TechTear): quad 4 vértices orientado a
  la velocidad, `halfLen 40·(1+stretch)·scaleEnv`, `halfWid 26·(1−0.28·stretch)·scaleEnv`,
  donde `stretch = clamp(speed·0.022, 0, 0.8)` (¡se alarga y estrecha con la
  velocidad! máx 80×52→~144×39 px) y `scaleEnv = (0.45+0.55·Form01)·(0.35+0.65·fade)`
  (nace al 45 %, crece a forma completa en 16 frames). Dentro del shader:
  - **perfil de lágrima** `w = 0.56·rise·cap` con `rise = smoothstep(0.02,0.74,x)^0.62`
    y **casquete redondo** `cap = sqrt(1−((x−0.74)/0.26)²)` — cola fina, cabeza gorda redondeada;
  - **convección interna** en coordenadas de rotación rígida (el ruido gira dentro
    de la gota);
  - **cola desgarrada**: ruido advectado hacia la cola "se come el cuello";
  - **condensación al nacer** (`uForm`): la gota emerge EROSIONADA por el ruido
    y se condensa en gota (`formGate = smoothstep(churn−0.35, churn+0.02, uForm)`);
  - **costra fundida**: manchas oscuras de baja frecuencia pegadas al borde
    (la piel se forma al enfriar, ×(0.75−heat·0.3));
  - **brillo de tensión superficial** en el borde superior (menisco);
  - **núcleo blanco-caliente** en la cabeza `((1.15,1.02,0.72)·core)` que sube
    con `uHeat` (0.30+0.70·heat).
- **Cinta = ESTELA DE FUEGO FUNDIDO** (TechTrail): triangle strip de **30 puntos
  de camino** (`TrailMax`), registrados **1 por frame real** (no por sub-tick),
  semiancho `lerp(3, 15, t^0.7)` (cola de 3 px → cabeza de 15), dirección por
  diferencia central, UV.x = t (0 cola → 1 cabeza). El shader:
  - **muestreo por LONGITUD DE ARCO en píxeles**: `sx = (uOffPx + uv.x·max(uLenPx,40))/600`
    — la fase del ruido queda **anclada al mundo**: cuando la cola se erosiona,
    el patrón NO resbala (`uLenPx`= longitud viva, `uOffPx`= longitud ya erosionada);
  - 2 octavas de ruido perlin (escala 1.7× y 3.9×, velocidades distintas);
  - **borde mordido por lenguas de fuego** (`bite = max((flow−0.44)·0.75, 0)`);
  - **envejecimiento de la cola + desgarrado en hebras**;
  - **3 capas**: borde chamuscado oscuro (charZone exterior) + cuerpo FireRamp +
    **línea núcleo blanca** `(1.12,0.96,0.60)·pow(age,1.6)` en el centro (0..0.15 del ancho).
- **Fondo aditivo**: SoftGlow naranja 0.9·env + oro 0.45·env bajo el cuerpo
  (también sirve de fallback si el shader falta).
- **Iluminación**: `MoltenOrange·(0.4+0.4·heat)` + DawnEmber de cola (1/3 de
  frames, detrás de la cabeza, flotando hacia arriba).

**PRT_DawnEmber (la brasa)**: SoftGlow con **doble dibujo** (halo 0.28× + núcleo
0.12×) y **estela Extra_98 orientada a la velocidad** si v>1.4 (stretch
clamp(v·0.16, 0.3, 1.6)); color = narrativa de temperatura **oro(255,208,96) →
rojo(255,92,30) → chamuscado(118,42,26)**; **flota mientras está caliente y cae
al enfriarse** (`velocity.y ± buoyancy 0.035`); parpadeo 0.76+0.24·sin(t·1.1+seed);
escala ×0.968/frame.

---

## FICHA 4/4 — DRAGON'S WORD, FORMA 2: EL DECRETO DEL DRAGÓN (龙令法阵, clic derecho)

### Cómo se activa
1. **Clic derecho**: `Shoot()` spawnea `DragonsWordMouse` (holdout `BaseHeldProj`)
   con el `damage` del ítem (682). `CanUseItem` del ítem: **solo si NO hay ya un
   decreto en campo** (y el propio decreto bloquea volver a usar el ítem —
   wiki: "mientras vive el círculo no puedes lanzar lágrimas ni otro decreto").
   `useTurn`.
2. **Mantener** con maná suficiente para el coste del ítem (CheckMana de 80):
   - **−1 maná por tick** (≈60/s — el wiki lo dice exacto);
   - **radio +2 px/tick hasta 660 px** (= 41,25 tiles — "41格" del wiki);
   - el centro sigue al cursor con `Lerp(target, mouse, 0.1)` (suave);
   - el jugador queda en pose de canalización (`heldProj`, `itemTime = 2`).
3. **Soltar / quedarse sin maná**: radio **−6 px/tick**; al llegar a 0 → Kill.
   (Desde radio máximo, la retracción dura 110 ticks.)
4. Al nacer: sonido **ProvidenceHolyRay** (¡el rayo sagrado de Providence!)
   + `PRT_DawnRing` anillo de casteo + 8 DawnEmber.

### La ejecución — `SpanDragonsWordCut()` (el corazón)
- **Cada 15 ticks (BeatLen = 0,25 s — "cada 0.25秒" del wiki)**, en el beat:
  para CADA NPC vivo no-amistoso a ≤ `Radius` del centro → spawn de
  **`DragonsWordCut` en `npc.Center`** (proyectil estático de 22×22, vida 22,
  un solo golpe `localNPCHitCooldown = −1`, **ArmorPenetration 1000** =
  ignora defensa).
- **Daño con decaimiento por prioridad**: `newDmg = Projectile.damage · (0.2 +
  num/55)` con `num` empezando en **255** → **×4.84** (≈3.298) para el primer
  enemigo, decreciendo por cada enemigo golpeado (num−−). (Easter egg: si el
  jugador se llama "Sakura", num ×5 → daño inicial ×23,5, y Sakura se auto-inflige
  Hellburn + Darkness al 1/300 — homenaje del autor.)
- **Sonido del beat** (si hay enemigos en rango): rugido **DD2_BetsyFireballShot**
  vol 0.5, pitch −0.55.
- Al morir cada `DragonsWordCut`: `PRT_DragonsWordCut` (la marca de corte:
  StarTexture_White **estirándose** — Ylength ×1.25/frame, Xlength ×0.7 → un
  rayo vertical que se afila; doble dibujo **oro interior ×0.85 ancho** + color
  exterior DarkRed/IndianRed; escala ×0.9; vida 19) + 2 DawnEmber + sonido
  **MurasamaHitOrganic** vol 0.8 pitch 0.6..0.7 con **presupuesto global de 2
  cortes sonoros por frame** (`SoundBudget`).
- **Debuff del corte: HellburnBuff 180 ticks (3 s)**.

### ANIMACIÓN — el círculo (shader `DragonsWordFX.fx`, técnica `TechDecree` + `TechBrand`)
- **El anillo del decreto** (TechDecree): quad de `halfPx = (radius + th·2.8)/0.84`
  px, grosor `th = 30 + radius·0.02` (30..43 px). Todo paramétrico en el shader:
  - **el borde NUNCA es un círculo matemático limpio**: desgarro por ruido +
    flujo radial hacia fuera (2 octavas: una arrastra, otra fluye);
  - **grosor irregular** (tercera octava ×(0.6..1.4));
  - **banda de fuego principal asimétrica**: corte exterior nítido (th·0.5),
    interior arrastrado (th·1.8) — "外锐内拖";
  - **lenguas de fuego lamiendo hacia fuera** (ruido radial estirado);
  - **banda chamuscada** pegada al borde interior;
  - **EL ANILLO DE GLIFOS: 28 celdas de "lenguaje dragón"** — `atan2` SOLO entra
    en `floor/frac` de 28 celdas discretas; cada celda dibuja **2 trazos
    verticales + 1 horizontal** en posiciones hash(cell, beatSeed), algunas
    celdas quedan en blanco (cellGate); **`uBeatSeed` re-escribe TODOS los
    glifos en cada beat** — el decreto se está re-escribiendo constantemente;
    brillo respirando con el beat (0.42..1.0 + flare·0.9);
  - **LA COREOGRAFÍA DEL BEAT**: en el último 45 % de la ventana del beat, una
    **onda viajera** (`wave`) se propaga DEL CENTRO AL BORDE (`wavR =
    radius·smoothstep(0.55,1,beat)`); **cuando llega al borde = cae el golpe**
    (el beat sonoro/cortes están sincronizados al mismo BeatLen); tras el
    impacto, **sobre-exposición** (`flare`) del anillo entero (col += blanco·0.9);
  - **interior**: lavado cálido + convección lenta (rotación rígida muy lenta
    0.06 rad/s) — el área está "viva" pero tenue (0.03..0.075).
- **LA MARCA DEL OJO DE DRAGÓN en cada enemigo** (TechBrand): batch de HASTA 24
  quads (BrandCap, TriangleList) de semiancho `clamp(max(w,h)·0.75, 22, 95)` px
  centrados en cada NPC del área; **la semilla viaja en el canal R del color de
  vértice** y el desvanecido del borde (radius−dist)/70 en el canal A. El shader
  dibuja:
  - **iris** (anillo elíptico 1.30:1 en r=0.5);
  - **pupila VERTICAL que se contrae antes del golpe**: ancho
    `lerp(0.15, 0.045, smoothstep(0.45, 0.96, beat))` — "enfoque de depredador";
  - **3 zarpazos**: `cos(3θ)` polinómico (4cth³−3cth) **sin atan2 en ruido** =
    sin costuras, rotando con la semilla del NPC y el número de beat;
  - **crecimiento tipo papel quemándose** (`burnGate` con ruido) desde que el
    enemigo entra en el área, y **rotura del sello en el beat** (flare ×1.4 de
    sobre-exposición blanca al romperse);
  - color FireRamp(0.72+0.2·flare) + blanco en el flare.
- **Capa aditiva** (sobre el mundo, tras el NonPremultiplied): SoftGlow
  **"garganta de la palabra"** en el centro — oro ×(0.45+0.3·flare) escala
  0.9+0.25·flare + naranja ×0.3 escala 1.7 — **respira con cada beat** (flare =
  1−beat·5). Fallback sin shader: DiffusionCircle4 en el borde.
- **Warp de "cúpula térmica"**: `GravitationalLens` PERO con
  `DontUseBlueshiftEffect() => true` (caliente, sin desplazamiento azul), tamaño
  radius·2.5, intensidad 0.18 + env 0.35·(radius/660) + flare·0.18.
- **Ambiente del área** (mientras vive): brasas ascendentes dentro del círculo
  (presupuesto `1 + radius/240` por frame, 50 % de las veces, v.y −0.6..−1.6) +
  **lenguas lamiendo el borde** (`PRT_DawnTongue`: textura TearFlame01 anclada
  por la base, **2..5 frames de vida con jitter de longitud 0.85+0.3·sin** —
  "la firma temporal del fuego", oro(255,186,74)→rojo(240,96,34), cada 1/3 de
  frames) + luz naranja ×0.55 en el centro y ×0.4 en 6 puntos del borde.

### DPS del decreto (números)
- Beat = 15 ticks → **4 golpes/s por enemigo**. Primer enemigo: 682×4.84 ≈
  **3.298 por beat ≈ 13.200 DPS** (+Hellburn 3 s cada golpe + rugido); con
  varios enemigos el daño decae por el contador `num` hasta ×0.2 mínimo.
- Economía: 80 maná de apertura + 60 maná/s de crecimiento (1/tick); radio
  máximo 660 px en 5,5 s de canalización; retracción 6 px/tick.

---

## 5) TÉCNICA DE IMPLEMENTACIÓN — resumen transversal (con nombres exactos)

| Pieza | Cómo está hecho en CWR | Equivalente Aethon |
|---|---|---|
| Ítem con 2 formas | `AltFunctionUse => true`; NINGUNA usa `altFunctionUse` en `Shoot` para bifurcar salvo DragonsWord (`player.altFunctionUse == 2`); Star Tomb usa holdout `BaseHeldGun` con `CanRightClick => true` y decide en `AI()` por `WantsFireLeft/WantsFireRight` | Igual: holdout propio + `channel` |
| Proyectil invisible | `Texture = "InnoVault/Assets/placeholder"` (1×1 transparente, verificado) + `PreDraw => false` | Nuestro patrón ya es similar (VFX 100 % código) |
| Quad de shader | `placeholder2` (1×1 blanco) estirado con `sb.Draw(quad, drawPos, null, Color.White, 0, origin, scale, …)` dentro de `sb.Begin(Immediate, blend, LinearWrap, …)` + `effect.CurrentTechnique.Passes[0].Apply()` | LumenLib/PrismaLib existentes |
| Primitivas | `DrawUserPrimitives(TriangleStrip, verts, 0, 2)` con `VertexPositionColorTexture`; quads de 4 vértices con UV 0..1; cinta de 30 puntos con semiancho en VÉRTICES y dirección por diferencia central | RiftLib/EstelaLib (mismo contrato) |
| Ruido | `PerlinNoise.png` 512×512 gris (valor medido 0.22..0.78 → normalizar `(n−0.22)·1.786`); Voronoi `Extra_193.png` 256×256 para placas | generar equivalentes |
| Warp de pantalla | `NeutronWarpHelper.DrawWarp`: end/begin con el shader como BlendState y DIBUJA UN PIXEL 1×1 estirado al rect; el shader escribe R=dir G=fuerza; feather en G | nuevo en Aethon (opcional) |
| Sonido con presupuesto | `SoundBudget()` estático: máx 2 sonidos de corte por frame de juego | utilidades nuevas |
| MP | `netImportant`, `SendExtraAI` (quakeCharge/glitch/quake/spin/forcedOut), daño solo owner, `netUpdate` al cambiar de estado | contrato existente |
| Carga 0..1 con umbral de disparo | `quakeCharge += 1/55`; suelta dispara solo si `> 0.06`; las estrellas "sangran" la carga si nadie la alimenta (`Lerp(quake, 0, 0.09)`) | patrón reutilizable |
| Trail anti-resbalo | `uLenPx`/`uOffPx` = fase del ruido anclada a longitud de arco mundial | **importante para EstelaLib** |
| Beat system | `BeatTime % BeatLen == 0` con `Beat01`/`BeatSeed` (nº de beat) alimentando coreografía + re-escritura de glifos | utilidad "metrónomo" |

### Lecciones de shader documentadas por el propio autor (en los .fx, traducidas)
1. "La estrella de neutrones NO es un agujero negro: tiene corteza dura y
   magnetosfera — el cuerpo es un sólido brillante, no un hueco" (por eso 2
   técnicas: Crust alpha + Field aditiva).
2. "Cono HUECO, no barra: el plasma es ópticamente delgado, la pared se ve más
   que el eje" — haz = wall > core > eje fino.
3. "El ángulo polar jamás entra en el ruido": atan2 solo en celdas discretas
   floor/frac (glifos), o cos(k·θ) (zarpazos), o el vector dirección ROTADO
   (ruido azimutal) — si no, línea dura en −x amplified por el warp.
4. "Toda máscara debe morir antes del borde del quad" (turb·0.20 empuja el
   cierre a 0.86 < 1.0) — si no, borde rectangular brillante.
5. "Los vértices van a ancho constante; la estrechez del cono la hace el shader"
   — dos estrechamientos (CPU+GPU) estrangulan el haz.
6. Salida PREMULTIPLICADA (`col·a, a`) para consumir con AlphaBlend; o Additive.
7. El warp escribe R/G (no A); la pluma de borde va en la INTENSIDAD.
8. La rampa de fuego ES la narrativa de temperatura: chamuscado→rojo→naranja→oro.
9. "La firma temporal del fuego": jitter de longitud frame a frame (2..5 frames
   de vida), no una llama estática.
10. El daño por prioridad con decaimiento (num/55) hace que el primer objetivo
    coma ×4.8 y los últimos ×0.2 — escalado implícito anti-multitud.

---

## 6) TRASLADO A AETHON — 4 ARMAS NUEVAS (2 ítems × 2 formas)

Regla general: Aethon ya tiene las librerías (LumenLib, PyraLib, EstelaLib,
BrumaFX, RiftLib, OndaLib) y el debuff **Quemadura Cósmica**; los 4 diseños
siguen el patrón CWR "invisible projectile + quad de shader + partículas PRT
propias + dos teclas que son recursos mutuos".

### 6.1 — «Sembrador del Cementerio Estelar» (basado en Star Tomb forma 1)
- **Identidad**: varita mágica endgame; ítem 30×30 procedural (báculo grafito con
  núcleo violeta MAGENTA (200,79,159) como el original); holdout a una mano.
- **Clic izq. — Sembrar Púlsar**: proyectil 220 dmg ×0.6 del daño de arma, vida
  660, freno geométrico ×0.885 con **TravelFactor 7.375 para aterrizar en el
  cursor**, spin-up 120 ticks `lerp(0.03,0.135,t²)`, núcleo colisión 26 px +
  **2 líneas 560 px × r17** por los polos (`CheckAABBvLineCollision`),
  i-frames 10, **ArmorPen 80**, tope 3 (la 4ª fuerza un quake ×0.35 de la más
  vieja), debuff **Quemadura Cósmica 20 s**, arrastre gravitatorio débil de
  no-boss (2.6·(1−d/R)², radio 220).
- **Visual (colores exactos)**: violeta (138,79,255) / azul (120,181,255) /
  blanco-caliente (199,214,255) / profundo (31,26,128). Cuerpo: quad 260 px,
  Crust (placas Voronoi 2 octavas + grietas que se encienden + Doppler
  `1+cos(θ−spin)·(0.26+0.62·spinRate)` + limb brightening + compresión
  `1/(z·0.34+0.16)`) y Field (anillo fotónico r1.42, dipolo `r=L·sin²θ`,
  casquetes `|cos|²⁴`, halo de estrés). Haz: **cono hueco** (wall>core>eje),
  `coneHalf lerp(0.10, 0.86, along^0.68)`, nudos de choque, caída 1/(1+2.1·x),
  **latido del faro ×(0.35+|cos(spin)|⁵·0.65)**, raíz enterrada 14 px. Warp
  lente 210 px. Partículas: vórtices keplerianos cada 6 t (r 70..140, ω=0.08/r,
  blueshift al centro), chispas polares cada 3 t, estrella-anillo al anclar.
  Luz violeta latiendo con el faro.
- **Nombres alternativos**: "Estela del Cementerio", "Vigía de las Tumbas".

### 6.2 — «Colapso Magnetar» (basado en Star Tomb forma 2)
- **Forma 2 del mismo ítem** (NO gasta maná): mantener clic der. → carga 1/55
  (55 ticks, umbral de disparo 0.06), las estrellas visiblemente frenan (spin
  ×0.15, haz ×0.55, corteza agrietada, jaula enredada ×1.8, chispas de fuga
  cada 2 t si carga>0.25, tether de vórtices en la boca del arma 34..80 px).
  Soltar → **Starquake**: anillo 34 ticks, radio (250+330·power)·easeOutCubic
  (hasta 580 px), daño pulsar×(1.4+1.1·power), **ArmorPen 100**, i-frames 8,
  **solo el FRENTE del anillo golpea** (vacío central 0.45R solo los primeros 8
  ticks), **COME PROYECTILES HOSTILES** en 0.7·Reach, debuff 30 s, sacudida
  4+4·p, y las supervivientes entran en **overclock 82..150 ticks** (spin 0.32,
  haces ×1.55/×2.2, daño ×1.6, arrastre 340 px).
- **Visual del quake**: Field de NeutronPulsar como jaula expulsada
  (quad `Reach·2.9`), warp ShockwaveRing (3 frentes + residuo + ripples
  sin(r·28−t·7)), rayos de reconexión 10+8·p radios ×4 fracturas, 6→2
  estrellas/frame sobre el frente, anillo blanco de sobre-exposición
  2.6+1.4·p.
- **Nombres alternativos**: "Réquiem de Estrella Muerta", "Sentencia del Colapso".

### 6.3 — «Lágrimas del Sol Moribundo» (basado en Dragon's Word forma 1)
- **Identidad**: tomo/ítem endgame, 80 maná, use 60, 3 proyectiles por casteo en
  abanico `2π/3·i + t·0.1` naciendo a 22..38 px, v inicial 3 px/tick.
- **Proyectil "Lágrima Solar"**: **extraUpdates 6**, órbita 2,5 s SIN dañar
  (RotatedBy 0.03), encendido (4 brasas + Item20), persecución suave 2,2 s
  (SmoothHoming 1/0.08) → mordisco a velocidad fija; **18 golpes** contados
  (i-frames 14 sub-ticks) aplicando **Quemadura Cósmica 7 s** (nuestro equivalente
  del Dragonfire 480 HP/s); al agotarse/morir → **EXPLOSIÓN primero, fade
  después** (98 sub-ticks ×0.82, cinta erosionándose 2 puntos/frame).
- **Visual**: re-skinnear la paleta fuego→**paleta estelar**: HotGold→blanco
  estelar (235,245,255), MoltenOrange→oro (255,190,80), EmberRed→carmesí solar
  (255,90,60), FireRamp = chamuscado→carmesí→oro→blanco (misma estructura de 4
  paradas). Cuerpo lágrima: quad halfLen 40/halfWid 26 con `stretch =
  clamp(v·0.022, 0, 0.8)` (se alarga con la velocidad), perfil `0.56·rise·cap`
  (cola fina, cabeza redonda), convección interna en rotación rígida, cola
  desgarrada por ruido, condensación al nacer, costra, menisco, núcleo
  blanco-caliente que sube con el calor. Cinta: 30 puntos/frame real, semiancho
  lerp(3,15,t^0.7), **fase anclada a arco mundial (uLenPx/uOffPx — regla
  antideslizamiento)**, borde mordido, hebras en la cola, línea-núcleo blanca.
  Fondo: SoftGlow doble. Brasa propia: flotar-caliente/caer-frío con rampa de
  temperatura.
- **Nombres alternativos**: "Sol que Llora", "Duelo Estelar".

### 6.4 — «Decreto del Eclipse» (basado en Dragon's Word forma 2)
- **Forma 2**: clic der. abre el **círculo del decreto** en el cursor (holdout,
  Lerp 0.1): **1 solo en campo**, −1 maná/tick, radio **+2/tick hasta 660 px**
  (41 tiles), soltar → −6/tick hasta morir. **Beat cada 15 ticks**: a cada NPC
  en rango le cae un corte estático (22×22, vida 22, 1 golpe, **ArmorPen 1000**,
  daño `arma·(0.2 + num/55)` con num=255 decreciente — **×4.8 el primer
  objetivo**), debuff Quemadura Cósmica 3 s, marca de partícula que se AFILA
  (Y×1.25, X×0.7 por frame, doble dibujo oro×0.85+color), sonido de corte con
  **presupuesto 2/frame**.
- **Visual (re-skin eclipse)**: el anillo = **corona de eclipse**: banda
  asimétrica (borde exterior nítido/interior arrastrado), grosor 30+R·0.02,
  borde desgarrado + lenguas hacia fuera, banda chamuscada interior, **28
  glifos rúnicos que se RE-ESCRIBEN cada beat** (2 trazos verticales + 1
  horizontal por celda, hash por beat, celdas en blanco), **onda viajera del
  centro al borde en el último 45 % del beat que AL LLEGAR dispara el golpe**,
  sobre-exposición tras el impacto, interior con lavado cálido + convección
  lenta. La marca en cada enemigo = **OJO DE ECLIPSE**: iris anular + **pupila
  vertical que se contrae antes del golpe** (0.15→0.045) + 3 zarpazos por
  cos(3θ) + crecimiento por papel quemado + rotura sobre-expuesta en el beat
  (semilla por NPC en el canal R del vértice, fade de borde (R−d)/70 en A; cap
  24 marcas). Glow central "garganta del decreto" que respira con el beat.
  Paleta: oro eclipse (255,214,110) + naranja (255,128,36) + brasa (214,58,22) —
  o versión "eclipse frío" con blanco-corona (255,244,214) + carmesí.
- **Extras robables**: rugido en el beat solo si hay enemigos; brasas ascendentes
  con presupuesto 1+R/240; lenguas en el borde cada 1/3 frames con jitter
  2..5 ticks de vida; luz en 6 puntos del borde; warp cúpula térmica
  (sin blueshift) R·2.5.
- **Nombres alternativos**: "Edicto del Eclipse", "La Palabra que Apaga Soles".

### 6.5 — Qué NO copiar / ajustes
- El easter egg "Sakura" (daño ×5 por nombre de jugador) — dejarlo fuera.
- `Item.width = 1240` gigante: en Aethon ya usamos cajas grandes + Colliding
  custom; mantener la disciplina de que la caja SOLO existe para forzar la
  llamada a Colliding.
- Los sonidos referencian assets de Calamity (`MurasamaHitOrganic`,
  `ProvidenceHolyRay`) — usar equivalentes vanilla o propios.
- Los sprites de ítem de CWR son pixel-art dibujado; en Aethon se generan
  proceduralmente (30×30, esquinas transparentes — contrato de la casa).
- El arrastre de enemigos y el comer-proyectiles son regalos de diseño muy
  baratos de implementar y muy sabrosos: **conservarlos** (DragNearbyEnemies
  2.6·f²·knockBackResist; EatHostileProjectiles en 0.7·Reach).

---

## FUENTES (todas en `research/v631/search_results/t50/`)
- **Código CWR v0.9209** (github hocha113/CalamityOverhaul@main):
  `Content_Items_Magic_NeutronWands_NeutronWand.cs` (ítem+holdout, 297 lín.),
  `..._NeutronPulsar.cs` (509), `..._NeutronStarquake.cs` (214),
  `Content_Items_Magic_DragonsWord.cs` (740 — ítem + VFX + Cut + Mouse + Proj),
  `Content_PRTTypes_PRT_DragonsWordCut.cs`, `Common_NeutronWarpHelper.cs`,
  `Content_Items_Materials_NeutronStarIngot.cs`,
  `Content_Projectiles_BaseHeldGun.cs`, `CWRConstant.cs`,
  7 partículas PRT (`Spark/HeavenfallStar/GravityVortex/SpaceFracture/
  StarPulseRing/DawnshatterPRT[DawnEmber/DawnRing/DawnTongue]`),
  2 buffs (`VoidErosion`, `HellburnBuff`).
- **Shaders**: `Assets_Effects_DragonsWordFX.fx` (4 técnicas documentadas),
  `NeutronPulsar.fx` (Crust+Field), `NeutronPulseBeam.fx`, `NeutronWarp.fx`
  (5 técnicas), + texturas `PerlinNoise 512²`, `Extra_193 Voronoi 256²`,
  `Extra_98 72²` analizadas con PIL; `InnoVault/Assets/placeholder.png`
  **1×1 transparente verificado**.
- **Localización**: `zh-Hans` + `en-US` de Items.Magic y Projectiles (tooltips
  completos con lore).
- **Wiki oficial** (calamity-overhaul.cc, 4 páginas en `t50/pages/`): CN+EN de
  neutronwand y dragonsword + changelog 0.9205 (la línea neutrón).
- **Contexto Calamity** (de t51/pages): Subsuming Vortex 460 dmg, Rock (Boss
  Rush), Yharon Soul Fragment 35-40/kill, Dragonfire −480 HP/s.
- Búsquedas web: z-ai 429 toda la sesión; DDG/Bing de respaldo devolvieron
  ruido (guardado en `busquedas/t50/s60..s63`); las b55-b58 previas (nombres
  exactos + vídeos bilibili BV1BS421X7Uy / BV1XTXsYfEMg / BV1DvSDYREwd) no
  pudieron descargarse (API requiere firma WBI) — el código las sustituye.

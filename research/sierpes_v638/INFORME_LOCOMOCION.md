# INFORME DE INVESTIGACIÓN — LOCOMOCIÓN DE CRIATURAS SEGMENTADAS (v6.38)

**Proyecto:** AethonMod · **Serie:** "más formas como La Sierpe Estelar"
**Pregunta del usuario:** crear otras formas de vida segmentada igual que la sierpe, investigando más allá del código visto en Facebook.
**Fecha:** investigación para v6.38 · 17 búsquedas web + 8 lecturas profundas (JSONs en `busquedas/`).

## 0. PUNTO DE PARTIDA — LA LEY ACTUAL DE LA SIERPE (para comparar)

La Sierpe Estelar combina DOS leyes separables:
- **Ley del líder (nadó):** la cabeza persigue el *punto tangente* de un círculo de `radm=110` px alrededor del puntero (nada en vueltas, no embiste) con giro acotado `±0.07 rad/tick` y vaivén de velocidad `1 ± 0.35·sin(2π·0.9·t/60)`.
- **Ley de cadena (follow clásico de las demos de canvas):** `seg[i] = seg[i-1] + normalize(seg[i] - seg[i-1]) · TamanoSeg` con N=16, `TamanoSeg=18` (cuerpo de 270 px).

Propiedades de ESE follow(): la ley es **unilateral** (el padre arrastra al hijo, el hijo nunca empuja), **sin inercia** (el hijo se teletransporta a distancia exacta), y **sin memoria** (el cuerpo corta las esquinas del camino de la cabeza, como un tren). Los 14 patrones siguientes cambian exactamente uno de esos dial: quién manda, quién se mueve, o qué ley une los eslabones.

---

## 1. CADENA FOLLOW (constraint en cascada — la familia a la que pertenece la sierpe)

**Qué es:** la versión general del follow(): una cadena de "constraints de distancia" resueltos de forma iterativa. Es la base de cuerdas/telas Verlet en juegos.
**Algoritmo (Verlet + constraint bidireccional, traducible a 60 Hz):**
```csharp
// Integración (la velocidad se deriva de la posición previa — INERCIA):
Vector2 tmp = p;  p += (p - pOld) + gravedad * dt;  pOld = tmp;
// Constraint de distancia (los DOS nodos se mueven, repartido 50/50):
Vector2 d = n1 - n2;  float dist = d.Length();
Vector2 correccion = d * ((distObjetivo - dist) / dist * 0.5f);
n1 += correccion;  n2 -= correccion;   // repetir K iteraciones por tick
```
**Parámetros típicos:** cuerdas de juego: 20-40 nodos, `iterations=80` (a más iteraciones, cuerda más rígida); gravedad −20 px/tick² en unidades Unity (a escala Terraria ~0.3 px/tick²). Truco documentado: un constraint extra primero-último reduce iteraciones.
**Diferencia con la sierpe:** la sierpe resuelve el constraint en UNA pasada y de forma unilateral (solo el hijo se mueve) — por eso es estable y barata. El Verlet bidireccional introduce **inercia real**: si sueltas la cabeza, el cuerpo sigue volando (toqoz: "flick the mouse and the object flies naturally").
**Idea Terraria:** "serpiente con panza pesada" — la misma sierpe pero con Verlet: al soltar el control el cuerpo cae/ondea con momentum físico.

**Fuentes:** toqoz.fyi/game-rope.html (código completo C#) · pikuma.com/blog/verlet-integration-2d-cloth-physics-simulation · natureofcode.com/physics-libraries · zalo.github.io/blog/constraints.

## 2. PERSECUCIÓN CÍCLICA / OUROBOROS (cada uno persigue al siguiente)

**Qué es:** N agentes donde cada uno persigue a su vecino y el último persigue al primero (mice problem / mutual pursuit). En un polígono regular de n lados cada agente traza una **espiral logarítmica** y todos se encuentran en el centro tras un tiempo `1/(1−cos(2π/n))` (unidades: lado/velocidad).
**Algoritmo:**
```csharp
for (int i = 0; i < N; i++) {
    Vector2 presa = seg[(i + 1) % N];           // el vecino, ¡el anillo se cierra!
    Vector2 objetivo = presa + vel[(i+1)%N] * k; // k>0: apuntar ADELANTE = anillo estable
    vel[i] = Vector2.Normalize(objetivo - seg[i]) * Vel;
    seg[i] += vel[i];   // radio estable si k ≈ 2π·R/(N·Vel)·algo; sin k, colapsa al centro
}
```
**Parámetros:** anillos de juego: N=8-16, R=90-140 px, Vel=6-9 px/tick; apuntar con adelanto k·Vel (k≈2-4 ticks) mantiene el radio; apuntar directo hace el colapso en espiral (bonito como "muerte" del arma).
**Diferencia con la sierpe:** **no hay líder ni padre** — la causalidad es circular; la "ley de cadena" es una ley de persecución (velocidad hacia el vecino, no distancia exacta).
**Idea Terraria:** "Ouroboros Menor" — 12 orbes persiguiéndose en anillo alrededor del jugador; el anillo barre enemigos (daño por barrido a Vel del orbe) y al expirar colapsan en espiral al centro (golpe final agrupado).

**Fuentes:** mathworld.wolfram.com/MiceProblem.html · en.wikipedia.org/wiki/Mice_problem · mathcurve.com/courbes2d.gb/poursuite/poursuitemutuelle.shtml · codepen.io "Ouroboros Infinite Loop".

## 3. CAMINO-MEMORIA (snake de cola de posiciones — el género slither.io)

**Qué es:** el cuerpo NO reacciona a su padre: lee el **histórico del camino de la cabeza**. Clásico del snake en rejilla (queue: unshift cabeza, pop cola) y de slither.io (muestreo del camino a espaciado fijo).
**Algoritmo:**
```csharp
_ruta.Add(cabeza);                          // cada tick, push frontal
if (_ruta.Count > HistoriaMax) _ruta.RemoveAt(0);
// Segmento i = punto del camino a arclength i*TamanoSeg DETRÁS de la cabeza:
float falta = i * TamanoSeg;  Vector2 p = _ruta[^1];
for (int j = _ruta.Count - 1; j > 0 && falta > 0; j--) {
    Vector2 paso = _ruta[j-1] - _ruta[j];  float L = paso.Length();
    if (L >= falta) { p = _ruta[j] + paso * (falta / L); falta = 0; }
    else { p = _ruta[j-1]; falta -= L; }
}
```
**Parámetros:** historia = (N·TamanoSeg/Vel + margen) ticks ≈ 60-80 para la sierpe; con muestreo cada 2-4 ticks basta (interpolando).
**Diferencia con la sierpe:** el cuerpo recorre la **trayectoria EXACTA** de la cabeza (no corta esquinas) — visualmente "tren/riego" vs " látigo orgánico"; el espaciado es invariante (el cuerpo nunca se encoge en curvas cerradas).
**Idea Terraria:** "Sierpe-Tren Espectral" — el cuerpo envenena el pasillo exacto que la cabeza visitó (zonas de 18 px persistentes 30 ticks); con giro rápido dibuja muros en S.

**Fuentes:** stackoverflow.com/questions/16925099 (sin rejilla) · docs.monogame.net Chapter 22 Snake Mechanics (queue) · gamedev.net "Implementing snake (slither.io) like movement" (2021) · gamedev.stackexchange.com/questions/33786.

## 4. MEDUSA (propulsión por pulsos + tentáculos Verlet pasivos)

**Qué es:** el líder es una campana que **late**: cada contracción expulsa agua → empuje `F ∝ dV/dt`; los tentáculos son cadenas Verlet pasivas ancladas al borde (inercia + drag, sin objetivo).
**Algoritmo:**
```csharp
float fase = (t % Periodo) / Periodo;                    // Periodo ≈ 45 ticks (0.75 Hz)
float contraccion = fase < 0.35f ? fase / 0.35f          // contracción RÁPIDA (40% del ciclo)
                                 : 1 - (fase - 0.35f) / 0.65f;  // relajación lenta
float R = R0 * (1 + 0.22f * contraccion);
cabeza += dir * (-dRdt) * K - velocidad * Drag;          // empuje solo al contraer
// Tentáculos: cadena Verlet (patrón 1) anclada al borde, drag 0.04, sin gravedad (flota)
```
**Parámetros:** contracción asimétrica 40/65; amplitud de campana 0.1-0.25·R0; drag anisotrópico (menor hacia adelante) para que avance en saltos; 6-10 tentáculos de 8-12 nodos.
**Diferencia con la sierpe:** la cadena **no persigue nada** — hereda el movimiento del ancla por pura física; el avance es discreto (solo durante la contracción), no continuo.
**Idea Terraria:** "Medusa del Vacío" — flota hacia el enemigo latiendo; cada pico de contracción (detección por cruce de cos, como El Sol Vivo) suelta un anillo de choque; tentáculos pican por contacto.

**Fuentes:** mysimulator.uk (simulador 2D: "bell contraction drives thrust, tentacles sway on a Verlet chain") · mdpi.com Miles 2019 (8 tentáculos, ciclos) · elonuniversity.contentdm.oclc.org (vórtices simplificados).

## 5. ONDA VIAJERA ANGUILIFORME (el cuerpo ES una fórmula)

**Qué es:** locomoción por onda que viaja de cabeza a cola con **amplitud creciente**: la anguila ondula casi todo el cuerpo; la onda va hacia atrás respecto al agua y empuja fluido perpendicular al eje.
**Ecuación (Lighthill, citada por SMU/Monash):**
```csharp
// s_i = i * TamanoSeg (a lo largo del cuerpo); L = N * TamanoSeg; normal = perpendicular al rumbo
float A(s) = Acabeza + (Acola - Acabeza) * (s / L);       // envolvente lineal (anguiliforme)
float lateral = A(s_i) * MathF.Sin(TwoPi * (s_i / lambda - t * f / 60));  // λ ≈ L
seg[i] = cabeza - dir * s_i + normal * lateral;
```
**Parámetros:** λ ≈ 0.8-1.0·L (AIP 2021: "wavelengths close to body length"); Acola ≈ 0.08-0.1·L, Acabeza ≈ 0.2·Acola; f ≈ 1-2 Hz → 0.1-0.2 rad/tick de fase. Carangiforme = envolvente cuadrática y amplitud solo trasera.
**Diferencia con la sierpe:** **cero dinámica de cadena**: las posiciones son analíticas → la ondulación es SIEMPRE limpia aunque la cabeza gire brusco (la sierpe en cambio se pliega sobre sí misma). Quien manda sigue siendo la cabeza, pero el cuerpo no "decide" nada.
**Idea Terraria:** "Anguila Solar" — nada en S perfectas; daño ×1.5 solo en las CRESTAS de la onda (los segmentos con |lateral| máximo) — enseña al jugador a rozar con la cresta.

**Fuentes:** s2.smu.edu/propulsion/Pages/undulatory.htm · journals.biologists.com JEB 212/4/576 · pubs.aip.org/aip/pof 031911 (λ≈L) · flair.monash.edu.au Gu et al 2022 (envolvente creciente).

## 6. MARCHA METACRONAL (ciempiés: apéndices desfasados)

**Qué es:** cada segmento lleva un apéndice (pata/púa) con ciclo de fase, **desfasado Δφ respecto al vecino** → onda de patas que recorre el cuerpo. Ciempiés: onda retrograda (de atrás hacia delante); con Δφ=45°, patas a 8 segmentos de distancia van sincronizadas.
**Algoritmo:**
```csharp
float phi_i = Fase0 - i * DeltaPhi;                      // DeltaPhi = 45° = π/4; signo = dirección de onda
bool enSuelo = MathF.Sin(phi_i) < 0f;                   // media onda apoyada
pata[i] = seg[i] + (enSuelo ? -dir * Paso : normal * AlturaArc * MathF.Sin(phi_i));
if (enSuelo) velocidad += dir * Empuje / N;             // el apoyo reparte el empuje
// el cuerpo puede seguir siendo la cadena follow() de la sierpe
```
**Parámetros:** Δφ = 2π/8 (45°, "patas gemelas cada 8"); frecuencia de ciclo 0.7-2 Hz según velocidad deseada (gait transition continua: más rápido → subir f, no alargar el paso).
**Diferencia con la sierpe:** la cadena es la misma pero la **locomoción se distribuye**: no hay un líder que empuje — el avance emerge de N apéndices desfasados (sistema nervioso segmentario real).
**Idea Terraria:** "Ciémpiés Rúnico" — pinchos que se plantan alternos: púa plantada = zona de daño 12 px + micro-empuje propio; al alejarse el objetivo sube f (transición de marcha real documentada).

**Fuentes:** nablu.com/2022/12/metachronal-waves-of-legs.html (simulación, 45°/segmento) · frontiersin.org 706064 (osciladores con feedback local) · royalsocietypublishing 20230439 (gait transitions) · sciencedirect (metachronal rhythm).

## 7. LÁTIGO (cadena inercial con afinamiento — amplificación de punta)

**Qué es:** cuerda Verlet anclada en el mango con **masa decreciente por eslabón**: la energía viaja del mango a la punta y al concentrarse en menos masa, `v·√m ≈ const` → `v_punta = v_mango·√(m_mango/m_punta)` (así se rompe la barrera del sonido el "crack").
**Algoritmo:**
```csharp
// Verlet (patrón 1) + constraint repartido por MASA (no 50/50):
Vector2 corr = ...;  float w1 = m2/(m1+m2), w2 = m1/(m1+m2);
n1 += corr * w1;  n2 -= corr * w2;                        // conserva el momento
// m_i ∝ (radio_i)² → con taper 15→4.5 px: m_base/m_punta ≈ 11 → v_punta ≈ 3.3·v_mango
// el mango se barre: angulo += velAngular durante 8 ticks (media vuelta), luego se frena
```
**Parámetros:** 10-16 eslabones, taper cuadrático, barrido del mango π rad en 6-10 ticks, umbral de "crack" |v_punta| > 25 px/tick.
**Diferencia con la sierpe:** la ley es **bidireccional e inercial** (energía base→punta); el líder (mango) apenas se desplaza — quien manda el daño es la PUNTA. La sierpe es cinemática (posición); el látigo es dinámico (momento).
**Idea Terraria:** "Flagelo Estelar" — el mango orbita al jugador y cada 90 ticks barre π rad: la punta hereda ×3.3 velocidad y golpea con daño ∝ v_punta; si supera el umbral, "crack" (anillo de polvo + destello).

**Fuentes:** toqoz.fyi/game-rope.html · datagenetics.com (Verlet simulations) · gdcvault.com (Verlet en 6-DOF, GDC) · pybullet.org foro (constraint-based Verlet).

## 8. BOIDS / CADENA BLANDA (steering: la distancia es un deseo, no una ley)

**Qué es:** cada eslabón es un agente con las 3 reglas de Reynolds (separación + alineación + cohesión). En una cadena "follow-the-leader", cada agente hace steering hacia el punto a distancia d de su padre → el espaciado es ELÁSTICO.
**Algoritmo:**
```csharp
Vector2 ancla = seg[i-1] - vel[i-1] * (d / vel[i-1].Length());   // punto d px detrás del padre
Vector2 deseado = Vector2.Normalize(ancla - seg[i]) * VelMax;
steer = (deseado - vel[i]) * PesoCohesion + (vel[i-1] - vel[i]) * PesoAlineacion;
vel[i] += Vector2.Clamp(steer, -MaxFuerza, MaxFuerza);           // suave: nunca satisface exacto
seg[i] += vel[i];
```
**Parámetros:** VelMax 8-12 px/tick, MaxFuerza 0.3-0.6 px/tick², pesos cohesión 1.0 / alineación 1.2 / separación 1.5.
**Diferencia con la sierpe:** **sin constraint exacto**: el cuerpo se ESTIRA al acelerar y se COMPRIME en los giros (aspecto vivo, nunca "teletransporta"); además el liderazgo puede ROTAR (Hartman & Benes: "change of leadership" — probabilidad de que un agente tome el mando).
**Idea Terraria:** "Cometa-Trenada" — si el líder embiste, los eslabones estirados ganan velocidad propia → daño ∝ |vel_i| (la cadena entera se vuelve arma al estirarse); el liderazgo salta al eslabón más cercano a un enemigo nuevo.

**Fuentes:** en.wikipedia.org/wiki/Boids · red3d.com/cwr/boids.html · cs.toronto.edu (Reynolds 1987, SIGGRAPH) · tandfonline (boids revisited, cambio de liderazgo).

## 9. DOBLE HÉLICE ENTRELAZADA (dos cadenas sobre una espina)

**Qué es:** ADN/mastil de mayo: DOS cadenas a ±R de una **espina central**, con el ángulo de offset girando a lo largo del cuerpo → se entrelazan. Paramétrica del helix: `(R·cosθ, R·sinθ)` con la segunda hebra a `θ+π`.
**Algoritmo:**
```csharp
Vector2 espina_i = seg[i];                                 // ¡la MISMA cadena follow() de la sierpe!
float ang = Fase0 + (i * TamanoSeg / Lambda) * TwoPi;      // Lambda = una vuelta cada P≈6 segmentos
A[i] = espina_i + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Radio;
B[i] = espina_i - new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Radio;
```
**Parámetros:** P = 5-8 segmentos/vuelta (λ = 90-144 px), Radio = 14-24 px (crece hacia la cola como el taper: hélice cónica); Fase0 avanza +ω·t para que el entrelazado "tuerza" vivo.
**Diferencia con la sierpe:** la ley de cadena vive SOLO en la espina; las hebras son **campos de offset** alrededor de ella (no están encadenadas entre sí). Hay dos cuerpos visibles y un solo esqueleto.
**Idea Terraria:** "Sierpe Gemela / Víbora de ADN" — reutiliza la espina tal cual y dibuja dos hebras de chispas: dañan los puntos donde CRUZAN el eje (cada medio período) y al enroscarse (radm pequeño) λ se comprime → las hebras convergen en doble hélice "de barbero" sobre el objetivo.

**Fuentes:** freemathhelp.com (hélice paramétrica) · chemrxiv.org (braiding two chains with a rotor) · pmc.ncbi.nlm.nih.gov (torque en hélices entrelazadas).

## 10. CONSTRICTOR (enroscarse en espiral que encoge alrededor de la presa)

**Qué es:** la boa ancla la cabeza en la presa y envuelve 1-2 vueltas; luego **aprieta sincronizada con la exhalación** y mide el "latido" para decidir cuándo soltar (Boback 2012).
**Algoritmo:**
```csharp
// Ley del líder reemplazada por servo-espiral sobre la presa:
theta += Omega;                                            // ω ≈ 0.15 rad/tick
radio = MathF.Max(radio * (1f - 1f / T_enrosque), RadioMin);// encoge en T≈60 ticks
deseado = presa + new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * radio;
// la cadena: la MISMA ley follow() — el cuerpo envuelve en espiral alrededor
vueltas += Omega / TwoPi;                                  // vueltas completas acumuladas
multiplicador = 1f + MathF.Min(3f, vueltas);               // ×4 tras 3 vueltas
```
**Parámetros:** ω 0.1-0.2 rad/tick, RadioMin = radio de hitbox del enemigo + 20, T de enrosque 50-90 ticks; liberar cuando `presa.life` cae por debajo de umbral (el "latido" del juego).
**Diferencia con la sierpe:** la ley de cadena NO cambia — cambia el **líder**: ya no orbita al puntero sino a una PRESA, con radio decreciente. La cola acaba persiguiendo a la cabeza (semi-ouroboros).
**Idea Terraria:** "Boa del Eclipse" — se lanza, enrosca 2-3 vueltas al enemigo más cercano; cada vuelta completa = +1 stack de "apriete" (daño/tick creciente); al morir la presa, se desenrosca y busca la siguiente.

**Fuentes:** en.wikipedia.org/wiki/Constriction · pmc.ncbi.nlm.nih.gov Boback 2012 (modula por latido) · userweb.ucs.louisiana.edu · snexplores.org (2022).

## 11. COLA DE COMETA (partículas libres en un campo de viento anti-solar)

**Qué es:** la cola de un cometa NUNCA sigue a la velocidad: apunta **SIEMPRE en dirección contraria al Sol** (presión de radiación + viento solar). Cola de iones: recta anti-solar; cola de polvo: curvada (retraso entre anti-solar y estela).
**Algoritmo (campo, no cadena):**
```csharp
Vector2 viento = Vector2.Normalize(nucleo - sol) * FuerzaViento;    // ¡independiente de vel!
foreach (var p in cola) {
    p.vel = p.vel * (1f - Drag) + viento;         // drag bajo en la de iones, alto en polvo
    p.pos += p.vel;
}
// spawn continuo en el núcleo: v inicial = velNucleo*0.3 + scatter; vida 30-60 ticks
```
**Parámetros:** FuerzaViento 0.15-0.3 px/tick², drag iones 0.01 / polvo 0.06, vida 40 ticks.
**Diferencia con la sierpe:** **no hay ley entre segmentos**: cada partícula obedece a un CAMPO (el "sol" puede ser el jugador). La forma de la cola no la decide la historia de la cabeza sino la geometría núcleo-sol — al orbitar, la cola barre como un limpiaparabrisas.
**Idea Terraria:** "Cometa Farero" — el núcleo orbita un punto de luz (o al jugador como sol); cola doble (ión recta + polvo curva) que quema por contacto; la cola barre 180° cuando orbita → arma de barrido pasiva.

**Fuentes:** en.wikipedia.org/wiki/Comet_tail · mars.nasa.gov (cola de iones anti-solar) · sciencedirect.com (Comet Tails overview) · astronomy.swin.edu.au (dos colas).

## 12. FUENTE BALÍSTICA (arcos de proyectiles con respawn)

**Qué es:** "cuerpo" = chorro continuo de partículas con física balística pura (`v.y −= g`) y vida limitada: los segmentos nacen y mueren; no hay padre, hay un EMISOR.
**Algoritmo:**
```csharp
if (t % CadaSpawn == 0) {
    float ang = -MathHelper.PiOver2 + (seed * 0.35f - 0.175f);    // abanico ±10°
    particulas.Add(new P { pos = boca, vel = Vector2.FromTheta(ang) * (7f + ruido), vida = 45 });
}
foreach (var p in ps) { p.vel.Y -= 0.25f;  p.pos += p.vel;  p.vida--; }   // g=0.25 px/tick²
```
**Parámetros:** alcance máximo `V²·sin(2θ)/g` (con V=9, g=0.25 → ~320 px); abanico ±10-15°; spawn cada 3-6 ticks; vida 40-60 ticks. Altura de pico `V²·sin²θ/(2g)`.
**Diferencia con la sierpe:** cadena sustituida por **ciclo de vida**: los eslabones tienen nacimiento/muerte y posición por Newton — ninguna información viaja del padre al hijo.
**Idea Terraria:** "Géiser Viva / Fuente Astral" — boca fija que arquea chispas; el RETROCESO del chorro empuja a la criatura-madre en dirección opuesta cada estallido (físicamente correcto: cantidad de movimiento) — una "serpiente-fuente" que se propulsara a chorros.

**Fuentes:** en.wikipedia.org/wiki/Projectile_motion · vaia.com (fountain ballistic) · dev.to (The Math Behind the Arc).

## 13. CINEMÁTICA INVERSA / FABRIK (la punta manda, la base ancla)

**Qué es:** FABRIK resuelve "¿qué forma toma la cadena para que la PUNTA toque un objetivo?" con dos pasadas de "reach" (estirar hacia el objetivo y re-deslizar). zalo: resolver el constraint de distancia primero en una dirección y luego en la otra ES FABRIK.
**Algoritmo (de sean.fun, literal):**
```csharp
// reach: mover 'a' al objetivo y deslizar 'b' sobre la línea para conservar la longitud:
Vector2 s = b - tgt;  b = tgt + s * (Len(a,b) / s.Length());  a = tgt;
// PASADA 1 (adelante, punta→...): tgt = objetivo; para i de 0..N-2: reach(seg[i], seg[i+1], tgt); tgt = nueva cola
// PASADA 2 (atrás): tgt = base_original; para i de N-1..1: reach(seg[i], seg[i-1], tgt); tgt = nueva punta
```
**Parámetros:** 1-3 iteraciones de doble pasada por tick bastan para animación interactiva; base fija = el ancla del arma.
**Diferencia con la sierpe:** el mando está **INVERTIDO**: en la sierpe la cabeza (segmento 0) es líder-libre; en FABRIK la base es fija y el ÚLTIMO segmento persigue el objetivo — la forma del cuerpo es la solución de un problema, no una historia.
**Idea Terraria:** "Guardián Lámpara" — planta/araña ancla al suelo que extiende su cuerpo (N=20) hasta el enemigo más cercano con FABRIK: siempre parece "alcanzar" en vez de "nadar"; los eslabones queman al contacto y la punta muerde. (CCD, la alternativa simple: rotar cada articulación de punta a base para apuntar.)

**Fuentes:** sean.fun/a/fabrik-algorithm-2d (código completo) · barbegenerativediary.com (FABRIK en Processing) · github.com/chFleschutz/inverse-kinematics-algorithms · reddit r/robotics "simplest IK" (CCD).

## 14. CINTA AL VIENTO (espina + onda lateral de senos inconmensurables)

**Qué es:** una banderola: la posición viene de una espina (recta o cadena) y el "cuerpo visible" es un **offset lateral ondulatorio** — la fórmula clásica de shaders de agua/banderas: `amp·cos(2π(d/λ − f·t))` o suma de 2-3 senos.
**Algoritmo:**
```csharp
Vector2 normal_i = perpendicular de la espina en i;
float off = A1 * MathF.Sin(w1 * t + k1 * s_i + f1)
          + A2 * MathF.Sin(w2 * t + k2 * s_i + f2);   // w1/w2 INCONMENSURABLES (regla de la casa)
cuerpo[i] = espina[i] + normal_i * (off * (s_i / L));  // amplitud crece hacia la punta
```
**Parámetros:** λ = L/1.5 a L/2.5 (una y media a dos ondas y media en el cuerpo), f = 0.8-1.6 Hz, Acola 0.15-0.25·L; amplitud acoplada a la "velocidad del viento" (= velocidad del jugador).
**Diferencia con la sierpe:** la ley de cadena se usa SOLO para la espina; el cuerpo ondula por una fórmula temporal (no hay causalidad padre→hijo en el offset — es viento, no herencia). Encaja 1:1 con la regla de determinismo de la casa (senos inconmensurables, ya usada en el Supernova v5.9x).
**Idea Terraria:** "Cinta-Aurora" — estandarte vivo anclado a la espalda del jugador: la espina es la congregación follow() y el offset ondea; corta enemigos con las crestas y su amplitud crece con la velocidad de carrera (viento = velocidad).

**Fuentes:** godotshaders.com (sin(TIME·f − angle)) · hub.jmonkeyengine.org (f(x,t)=a·sin(x·w+t·p), suma de 3) · math.ucsd.edu (amp·cos(2π(d/λ − freq·t))) · catlikecoding.com (senos procedurales).

---

## TABLA DE SÍNTESIS

| # | Patrón | Ley de cadena | Ley del líder | 1 idea mecánica para Terraria |
|---|--------|---------------|---------------|-------------------------------|
| 0 | Follow clásico (sierpe) | distancia EXACTA al padre, unilateral | persigue tangente del círculo | (base actual) |
| 1 | Verlet bidireccional | distancia con corrección 50/50 e inercia | cualquiera (la cuerda vuela) | serpiente con panza: momentum al soltar |
| 2 | Persecución cíclica | cada uno PERSIGUE al siguiente (anillo) | nadie: todos líderes | ouroboros: anillo que barre y colapsa al centro |
| 3 | Camino-memoria | leer HISTORIA a arclength fijo | igual que la sierpe | tren fantasma: envenena el pasillo exacto |
| 4 | Medusa | tentáculos Verlet pasivos (sin objetivo) | campana late, empuje ∝ dR/dt | pulsos = anillos de choque; picadura pasiva |
| 5 | Onda viajera | FÓRMULA analítica A(s)·sin(2π(s/λ−ft)) | cabeza gira; cuerpo es función | anguila: daño en crestas de onda |
| 6 | Metacronal | follow + apéndices con fase Δφ=45° | distribuido: N ciclos de patas | ciémpiés: pinchos plantados + transición de marcha |
| 7 | Látigo | constraint por MASAS + inercia | el mango gira π; la punta manda | flagelo: v_punta ×3.3 y "crack" |
| 8 | Boids / cadena blanda | steering suave (separ+aline+cohesión) | líder persigue; mando rotable | cometa-trenada: daño ∝ velocidad propia |
| 9 | Doble hélice | dos hebras = offset ±R sobre la espina | la ESPINA es la líder | víbora de ADN: daño en cruces de hebras |
| 10 | Constrictor | follow normal, cola persigue a cabeza | espiral que ENCOGE sobre la presa | boa: stacks de apriete por vuelta |
| 11 | Cometa | NINGUNA: campo de viento anti-sol | núcleo orbita al "sol" | cola doble que barre al orbitar |
| 12 | Fuente balística | NINGUNA: Newton + vida limitada | boca emisora fija | géiser: retroceso propulsor |
| 13 | IK FABRIK | reach adelante+atrás (la PUNTA manda) | objetivo = enemigo; base anclada | guardián lámpara: alcanza, no nada |
| 14 | Cinta al viento | espina follow + offset Σ senos | ancla al jugador; "viento" = tiempo | cinta-aurora: amplitud ∝ velocidad de carrera |

## LECTURA RÁPIDA PARA LA V6.38 (recomendación del investigador)

Los 14 patrones son combinables en tres ejes: (a) **quién manda** (cabeza libre / anillo / punta IK / campo), (b) **qué une los eslabones** (constraint exacto / steering suave / inercia Verlet / fórmula / nada), (c) **de dónde sale el impulso** (líder continuo / pulsos / patas desfasadas / balística). Los de coste CERO nuevo (reusan la infraestructura de la sierpe tal cual): 3 (camino-memoria), 9 (hélice: la espina ES la sierpe), 10 (constrictor: solo cambia la ley del líder), 14 (cinta: la sierpe + offset). Los que más "se siente distinto" en juego: 7 (energía, no posición), 13 (alcance, no nado) y 4 (pulsos, no deslizamiento).

*Informe: subagente de investigación 2-a — sin cambios en el código del mod.*

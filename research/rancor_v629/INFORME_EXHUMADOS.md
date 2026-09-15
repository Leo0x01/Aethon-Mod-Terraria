# INFORME DE INVESTIGACIÓN — v6.29
## Los Exhumados de Calamity: RANCOR y GRUESOME EMINENCE
### (petición del usuario: "investiga el arma Rancor, investiga bien su funcionamiento
### completo, qué librerías y assets usa... investiga el funcionamiento completo de
### Gruesome Eminence, qué librerías y assets usa")

Fuentes: wiki oficial calamitymod.wiki.gg (páginas Rancor + Gruesome Eminence +
Enchantment + Supreme Witch Calamitas), wiki Fandom (versiones históricas), 6
búsquedas web dirigidas. Fecha de consulta: sesión v6.29.

---

## 0. EL CONTEXTO: "ENCHANTED EXHUMED"

Lo que el usuario llamó "Supreme Calamitas Enchanted Exhumed" es el sistema de
**la Brimstone Witch (Calamitas)**: un NPC que presta el servicio de **Encantamiento**
(pagas 10× el valor de venta del ítem) y una selección muy reducida de ítems tiene
acceso al encantamiento único **"Exhume"**, que **transforma el ítem en otro totalmente
nuevo** (identificables con el Brimstone Locus). Rancor y Gruesome Eminence son dos
de esas formas exhumadas:

| Base (pre-exhume) | Forma exhumada |
|---|---|
| Burning Sea | **Rancor** |
| Ghastly Visage | **Gruesome Eminence** |

Ambas armas son post-Moon Lord, ambas usan la rareza especial (violeta/crimson
del sistema de encantamiento), y ambas mantienen la relación con Calamitas
(venderlas/de-exhumarlas viaja en sentido inverso por Shimmer).

---

## 1. RANCOR — funcionamiento COMPLETO

**Ficha**: tipo Arma Mágica (tomo) · daño 333 · maná 25 · use time 25 (Slow) ·
velocidad 9 · knockback 5 · crit 4% (daño crítico ×2) · mejor modificador Mythical.

### 1.1 El ciclo de disparo (la mecánica raíz)
1. **El círculo mágico**: al usar, crea UN CÍRCULO MÁGICO a distancia fija del
   jugador (no en el cursor exacto: a un set distance en la dirección de apuntado).
2. **La carga (3 segundos exactos)**: el círculo carga energía. El diseño del
   círculo está basado en el **Círculo de Transmutación Humana de Fullmetal
   Alchemist** (referencia declarada en Trivia).
3. **EL HAZ**: tras la carga, el círculo dispara **UN GRAN HAZ LÁSER que perfora
   INFINITAMENTE**. El proyectil del láser se llama internamente **"The Angy Beam"**.
4. **Al tocar tiles sólidos** el haz produce:
   - **BRAZOS ESQUELÉTICOS DAÑINOS** que emergen de la superficie → **66.67% del
     daño base** (proyectil "Rancor Arms").
   - **CINDERS (ascuas) grandes** que salen volando → **33.33% del daño base**
     (proyectil "Rancor Cinder").
5. **Efectos puramente visuales**: polvo, **niebla ("The Fog Is Coming")** y lava
   al impactar (proyectil "Rancor Fog").
6. **La muerte especial**: los enemigos que muere por el haz tienen un efecto de
   muerte único — **se desintegran en cenizas** (salvo que mueran de un solo golpe).
7. Balance extra: XM-05 Thanatos recibe 50% del daño del haz.

### 1.2 Librerías y assets que usa (ingeniería inversa documentada)
- **Holdout del libro** (asset de animación de mano) + sonidos propios: Use,
  Laser Beam, **Laser Loop**, Arms, Holdout Book, Magic Circle (6 sonidos).
- Proyectiles constituyentes: **Rancor Arms / Rancor Fog / Rancor Cinder** +
  el haz principal.
- El haz mantiene un **loop de sonido** mientras vive (canal de audio sostenido).
- Parches históricos que revelan la implementación: "Fixed deathray becoming
  disjointed and broken when hitting a large amount of NPCs at once" (el deathray
  es UNA geometría continua, no segmentos), "Fixed arms sometimes being able to
  spawn outside of the bounds of the world" (los brazos nacen EN el tile de
  impacto, clampeados), "Fixed deathray scaling three times with damage" (el
  daño del haz es un multiplicador ÚNICO del base).

### 1.3 Traducción a NUESTRAS reglas (el diseño de EL RENCOR PRIMORDIAL)
| Calamity | Aethon (100% código, nuestras librerías) |
|---|---|
| Círculo de transmutación (asset PNG FMA) | **Círculo rúnico de la casa**: 10 runas doradas CW + 6 violetas CCW + **la estrella de 5 puntas interior dibujada con cápsulas** (el homenaje FMA) — tecnología de runas de TormentaNebular |
| 3 s de carga | **180 ticks**: runas encendiéndose UNA A UNA, BrumaFX espiralando HACIA el círculo, PyraLib ascuas orbitando, bloom LumenLib creciendo, Telegraph del haz, Oscurecer sutil, latido de sonido subiendo |
| The Angy Beam (haz continuo) | **RiftLib.Tear con paleta carmesí-ámbar ×44 px de ancho, 880 px** — la tecnología del DESGARRO CONTINUO v6.28 (UN SOLO QUAD, cero juntas — la lección medida) + LumenLib.LightAlong + Ray de atmósfera |
| Rancor Arms (66.67%) | **LOS BRAZOS ESPECTRALES**: 4 brazos por cápsulas (brazo/antebrazo/dedos abanicados) naciendo del tile de impacto, creciendo con smoothstep, golpe de área ×0.66 |
| Rancor Cinder (33.33%) | **Ascuas de PyraLib.Sparks (rampa SolarFire) + daño de área ×0.33** alrededor del impacto cada 8 ticks |
| Fog + lava visuales | BrumaFX.Puff violeta-gris + luz ámbar pulsante + PyraLib.Flame lamiento el tile |
| Muerte en cenizas | Detección post-golpe: NPC muerto por el haz → **ráfaga de ceniza** (polvo ceniciento-carmesí) |
| Laser Loop | latidos graves + trueno del disparo (Kick perpendicular + Flash del paquete TearImpacto) |

**Nuestra identidad**: maná 0 (regla de la casa), daño 130, rareza Purple (la
jerarquía "especial" de los exhumados).

---

## 2. GRUESOME EMINENCE — funcionamiento COMPLETO

**Ficha**: tipo Arma Mágica · daño 666 · maná 30 (**consumo CONSTANTE mientras
se canaliza**, no un pago único — v2.2.0 lo cambió de 400-una-vez a 30-drenaje) ·
use time 27 · velocidad 9 · knockback 5 · crit 4% (×2) · mejor modificador Mythical.

### 2.1 El ciclo de vida de la CONGREGACIÓN (la mecánica raíz)
1. **La invocación**: al usar, invoca **UNA CONGREGACIÓN DE ESPÍRITUS** (una
   conglomeración gaseosa de espíritus) CERCA DEL CURSOR — proyectil único
   "Spirit Congregation".
2. **El comportamiento salvaje**: la congregación **sigue LOOSEMENTE el cursor**
   y **se mueve aleatoriamente por su cuenta** de cuando en cuando (no es un
   control directo: es "wild").
3. **Los espíritus pequeños**: la congregación **libera espíritus menores**
   conforme pasa el tiempo — PURAMENTE VISUALES: flotan alrededor, **lingüean
   unos segundos y luego SON TIRADOS DE VUELTA hacia la congregación**.
4. **LA ACUMULACIÓN (14 segundos exactos)**: los espíritus se acumulan hasta
   crear **UNA SOLA ABOMINACIÓN — un monstruo único y TOTALMENTE CONTROLABLE**
   por el jugador (el control pasa de loose a directo).
5. **La rampa de daño**: el daño de la congregación **crece gradualmente del
   100% al 185% del daño base** conforme llega a tamaño completo.
6. **Restricciones**: el rango del cursor se clampea a una pantalla 1920×1080;
   11 frames de inmunidad LOCAL; exenta del sistema de pierce-resist; solo 85%
   de daño contra XM-05 Thanatos.
7. **Referencias visuales declaradas**: el interior contiene **dos siluetas
   distintas que recuerdan a Giygas (EarthBound) y otra que recuerda a un feto**
   (también Giygas); el diseño general se parece a los abalorios de The Collector
   (Darkest Dungeon), particularmente la Cabeza de Dismas.

### 2.2 Librerías y assets que usa
- Holdout del arma + sonidos: Use, **Small Spirit**, **Full Size** (el monstruo
  tiene SU PROPIO sonido de tamaño completo — la abominación suena distinto).
- Un ÚNICO proyectil (Spirit Congregation) que escala: el "modelo" crece — el
  mismo asset se dibuja a más escala + los espíritus menores como FX.
- El drenaje de maná es el PRECIO del canal (el arma se usa en retención).

### 2.3 Traducción a NUESTRAS reglas (el diseño de LA EMINENCIA ATROZ)
| Calamity | Aethon (100% código, nuestras librerías) |
|---|---|
| Conglomeración gaseosa de espíritus | **BrumaFX.Cloud Doble** (masa pálida hueso + interior violeta, respirando) — la masa que ocluye |
| Sigue loose el cursor + salvajería propia | **Spring flojo + impulsos de dardo** (gateados por hash): de joven SE LARGA por su cuenta; de adulta obedece |
| Espíritus menores tirados de vuelta | **Espíritus DETERMINISTAS** (semilla+tiempo, MP-gratis): cada uno con su ciclo paramétrico radio-base → FUERA → TIRADO DE VUELTA + estela EstelaLib por su pasada reconstruida |
| 14 s de acumulación | **840 ticks de canal** (cada uso refresca el canal): `_crecimiento` 0→1 |
| 100% → 185% de daño | `mult = 1 + 0.85 * crecimiento` — EXACTO a Calamity |
| La abominación controlable | crecimiento ≥ 1: spring ×2.4 (control directo), velocidad ×1.4, la nube SE APRIETA (×0.85), **el OJO MAYOR** domina, corona de ojos menores, estela Comet de EstelaLib, rugido propio |
| Giygas interior | **LA CARA**: ojo grande + boca que aparecen en ventanas caóticas (gate hash) dentro de la masa cuando el crecimiento > 0.55 — el horror que asoma |
| Drenaje de maná | El CANAL como precio (maná 0 por la regla de la casa, pero el crecimiento SOLO avanza mientras se sostiene el uso) |
| Range clamp 1920×1080 | Clamp del cursor a **la pantalla actual + 100 px** de gracia |

**Nuestra paleta**: espectral **blanco-hueso + acentos carmesí** (la herencia
brimstone de Calamitas — distinta de todos nuestros violetas).

---

## 3. CONCLUSIONES DE DISEÑO (lo que se lleva al código)

1. **La telegrafía es el arma** (lección nº1 de Rancor): 3 segundos de círculo
   cargando = expectativa → el haz es el pago. Nuestro Telegraph de LumenLib ya
   existía para esto.
2. **El daño secundario porcentual** (66.67% / 33.33%): brazos y ascuas NO son
   proyectiles libres — nacen DEL impacto en tile. En nuestra escuela de daño
   manual: golpes de área clampeados al punto de impacto.
3. **La escalera de control** (lección nº1 de Gruesome Eminence): loose → wild →
   total. El ARMA cambia de手感 al crecer: spring y velocidad como funciones de
   `_crecimiento`.
4. **El costo vivo**: el canal como precio (nosotros sin maná, pero sin canal no
   hay crecimiento — y sin crecimiento se decae).
5. **La muerte con firma**: los enemigos del haz mueren EN CENIZA; la abominación
   suena DISTINTO al espíritu. Los detalles de muerte/estado son lo que hace
   "triple-A" a un arma.
6. **Cero texturas ajenas**: todo Calamity se traduce a nuestras 8 librerías
   (runas, RiftLib, BrumaFX, PyraLib, LumenLib, EstelaLib, OndaLib) — la regla
   del 100% código se mantiene.

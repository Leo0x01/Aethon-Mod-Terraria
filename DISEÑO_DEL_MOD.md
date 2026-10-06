# 📜 DISEÑO y ARQUITECTURA del Mod — "Aethon, el Grimorio Eterno"

> **Documento de ARQUITECTURA REAL** (v6.50.61). Describe cómo funciona HOY
> cada sistema del mod, con mapa de archivos y valores exactos — escrito para que un humano o
> una IA puedan retomar el trabajo sin re-leer los ~290 `.cs`.
> Inventario de QUÉ existe → `CARACTERISTICAS.md` · Estado/pendientes → `STABLE-SNAPSHOT.md` ·
> Historial → `CHANGES.md`.

---

## 📑 Índice

1. [Visión y lore (condensado)](#1-visión-y-lore)
2. [El loop de juego completo](#2-el-loop-de-juego)
3. [El Grimorio del Eterno](#3-el-grimorio-del-eterno)
4. [El hambre y la voz del libro (EcoLib)](#4-el-hambre-y-la-voz-del-libro)
5. [LA FURIA — el motor de oleadas](#5-la-furia--el-motor-de-oleadas)
6. [Las esencias y la economía](#6-las-esencias-y-la-economía)
7. [Los jefes](#7-los-jefes)
8. [El pipeline de rayos (100 % código)](#8-el-pipeline-de-rayos)
9. [El destello (historia de la muerte del círculo plano)](#9-el-destello)
10. [El stack VFX de la casa](#10-el-stack-vfx-de-la-casa)
11. [Red, persistencia y configuración](#11-red-persistencia-y-configuración)
12. [Reglas de la casa (no negociables)](#12-reglas-de-la-casa)
13. [Mapa maestro de carpetas](#13-mapa-maestro-de-carpetas)

---

## 1. Visión y lore (v6.50.61 — EL LORE NUEVO)

**Aethon, el Grimorio Eterno**: el libro que cargas NO es un arma — **ES AETHon**, una
entidad primigenia atada en forma de tomo. Los **Altarenes Antiguos** lo adoraron bajo tierra
donde dormía; el jugador lo encuentra como **Fragmento Génesis** (el altar lo entrega) y lo
convierte en el **Grimorio del Eterno**: un libro con vida propia que **come** la experiencia
de cada kill, tiene **hambre**, susurra, se pone celoso y — si no lo alimentas — convoca a
**LA FURIA**: un festín de oleadas para cebarse (el libro se alimenta a sí mismo con el
mundo que lo rodea). Los guardianes dejan **esencias**; devorarlas es lo que más nutre al
libro. Y cuando pronuncias su nombre — **El Nombre de Aethon** — el libro **SE ABRE y se
alza de sus propias páginas**: **AETHON, EL GRIMORIO ETERNO**, el jefe final de 5 fases,
**LA PRUEBA DEL PROPIO GRIMORIO** (el libro juzga si su portador es digno de cargarlo).
Derrotarlo no lo destruye: cierra la prueba con **«El Veredicto»** (fase 5) y te declara
digno — La Forma Ascendida cae a tus pies y su **esencia** es la página que el libro se
arranca a sí mismo al caer.

(v6.50.61 — LA PURGA: los cuatro jefes guardianes del mod y el Testigo fueron BORRADOS
del juego; solo queda Aethon. El lore anterior de "la Luz Primordial" quedó jubilado.)

**Principios de diseño vigentes**: progresión infinita con curva suave · todo el arte visual
**100 % por código** (los rayos sin NI UN sprite) · cero dependencias de otros mods · es-MX y
en-US siempre simétricos (es-ES borrado en la v6.50.70) · "arsenal de pruebas": las armas no cuestan maná y se entregan en bolsas.

---

## 2. El loop de juego

```
Altar Antiguo (worldgen) ──clic──► Fragmento Génesis ──+5 lingotes──► GRIMORIO DEL ETERNO
        │                                                                │
        │  (o drop de King Slime / Ojo de Cthulhu si no tienes libro)    │
        ▼                                                                ▼
Bolsa del Probador (testing)                              MATAR = XP (bestiario ★², ×3 primera, ×2 HM)
                                                                         │
                                              nivel 25+ ── SIN MATAR 75 s = 1 momento de HAMBRE
                                                                         │
                                              4º momento (~5 min) ──► LA FURIA (N oleadas 1..10)
                                                                         │
        chusma del bioma ×(k+1) ── muertes 12+6k ──► GUARDIÁN por zona ×(k+1) ──► ESENCIA
        (+ dado 1 % = Deerclops raro)         90 s máx        + k monedas de platino
                                                                         │
        10×10 vencida ──► EL JUICIO (6 guardianes ×15) ──► saciedad + DERROTAOLEADA10
                                                                         │
        ESENCIA (usar) = +1 NIVEL + voz del sabor del jefe ──► nivel 50/100/150…
                                                                         │
        EL TESTIGO (siempre presente): crónica de jefes + TIENDA de esencias (DerrotaOleada10)
        + Resonancia (nivel 50+)                                        │
                                              nivel alto ──► EL NOMBRE DE AETHON ──► JEFE FINAL
```

---

## 3. El Grimorio del Eterno

**Archivo**: `Content/Weapons/GrimoireEternal.cs` (507 l) + escalados en `Content/Systems/WeaponScaling.cs`.

- Arma mágica `11/22`, **maná 0**, rare Quest, 10 oro. Clic izq = Nightglow vanilla (931) con
  homing; clic der = invoca **CosmicOrbMinion** (buff 5 min; el nivel del libro viaja en `ai[2]`).
- **Niveles por ÍTEM** (GlobalItem `ShardLevelItem`, InstancePerEntity): cada copia del libro
  tiene su nivel/XP; persiste en el `.plr` (`aethonLevel/aethonXP`) + `NetSend/NetReceive`.
- **Coste**: `XPForNextLevel = 100 × nivel^1.5`.
- **XP**: mobs `5×★²` (×3 primera kill de especie vía bestiario, ×2 hardmode); jefes
  `5.000 | 25.000 (lifeMax>20k) | 100.000 (Moon Lord) + lifeMax/10 + ★ + 1.1×nivelLibro×111`
  (partes en cascada de gusanos pagan 0 — `EsParteDeJefe`). Todo lo multiplicado por
  `MultiplicadorXP` de oleada (ver §5) y config.
- **3 estados del libro** (v6.46): **sostenido** (todo) · **barra rápida slots 0-9** (come XP,
  minions persisten, vida/maná topeados +100) · **guardado** (nada).
- **Escalados** (WeaponScaling): daño mágico +2.2 %/nivel · summon +1 %/nivel · crit +0.2 %
  (tope 100) · armor pen +2 % cada 5 (tope 50) · use time −0.3 % (tope −25) · **+1 slot minion
  cada 10 niveles** (tope +10 al 100) · +1 maná cada 4 · +2 vida cada 20 · lifesteal +0.1 %
  cada 7 desde nivel 7 · bolts extra +1 cada 3 · hito cada 50 niveles. Tope configurable
  `MaxShardLevel` (0 = infinito).
- **Tooltip de 2 modos** (TooltipToggleItem): básico (nivel + barra █░) / completo (secciones).
- **El Libro Celoso**: cambiar de arma con hambre > 0 = susurro por clase de arma + frío 8 s.

---

## 4. El hambre y la voz del libro

### 4.1 El hambre — `Content/Players/ShardPlayer.cs` (581 l)

Con libro visible (0-9) **y nivel ≥ 25**: cada **75 s sin matar** = 1 **momento** (tope 10).
Momento 1 = susurro con **sabor a bioma** (`Eco.Bioma.*`); momentos 2-4 = `Eco.Hambre.Susurro1-4`.
La barra dorada palidece + aura de ceniza (`AuraPortadorHalo`). **Al 4º momento** (~5 min):
si config ON y `MundoLibre()` → **LA FURIA**. Si el mundo está ocupado, la hambre SUBE hasta 10
(así existen oleadas naturales > 4). **Cualquier kill del portador perdona el hambre**.

### 4.2 La voz — `Content/VFX/EcoLib.cs` + `Content/Systems/EcoSistema.cs` + `Content/Systems/EcoRed.cs`

- **EcoLib**: cola de voces dramáticas (máx 8, prioridad desaloja; `ElegirClave` nunca repite la
  última variante). Tipeo ~2 chars/tick, texto quieto 4-10 s, fundido 70 t. Render con
  **`FontAssets.DeathText`** centrado al 24 % de altura, deriva −0.12 px/t, pop 1.14→1.0,
  cero `Main.rand`, cero alocaciones por frame.
- **v6.50.30 — LA VOZ EN SILENCIO**: `EcoLib.Update` ya NO reproduce `SoundID.Roar` al nacer una
  voz (el "grito de jefe" odiado). El flag `Rugido` queda como API pero ninguna voz lo usa.
  Rugen los jefes que LLEGAN, no el libro.
- **EcoSistema**: capa UI tras "Vanilla: Death Text". `AnunciarJefeMuerto`: al morir un jefe,
  cada portador con libro visible oye a SU libro (`Eco.VozJefe.<Jefe>1-3`, **23 jefes** con color
  propio). `ClaveSusurroDelBioma` da el sabor del momento 1.
- **EcoRed** (9 mensajes, IDs 3-11): voz/hambre/latidoXP/libros/crónica/pedidos viajan en MP por
  **CLAVE** (cada cliente resuelve su idioma); anuncios del festín a todos por
  `BroadcastChatMessage`; "solo el portador correcto ve los reclamos de su propio grimorio"
  (doble puerta en `Recibir`). Host&Play entrega local por MemoryStream.

---

## 5. LA FURIA — el motor de oleadas

**Archivos**: `Content/Systems/GrimorioFuriaSistema.cs` (1.214 l, EL MOTOR, corre en el server
del mundo) · `Content/Globals/OleadaNPC.cs` (833 l, el SELLO) ·
`Content/Projectiles/Oleadas/AtaqueOleadaProjectile.cs` (479 l, los dientes) ·
`Content/Items/CarnadaDelGrimorio.cs` (ítem de prueba).

### 5.1 La máquina de fases (VOLÁTIL — no se guarda)

`Inactivo → Llamada (200 t) → [Monstruos → Jefe → Interludio (90 t)] × N → Especial (El Juicio)
→ Fin → Terminar`. Constantes: evento total 18.000 t (5 min, red de seguridad mín. 600 t/oleada) ·
jefe máx 5.400 t (90 s) · guardián extra del Juicio cada 900 t · `GuardianesJuicio = 6`.

**Activación** (dos vías):
- **Natural**: hambre al 4º momento + `EventoHambreGrimorio` (config server, ON) + `MundoLibre()`
  (sin invasión, sin jefes vivos).
- **La Carnada del Grimorio** (testing): clic der cicla N = 1..11 (11 = Juicio directo);
  clic izq provoca con `max(N, MomentosHambre)`. Ignora la config.

### 5.2 El motor de spawn = VANILLA (v6.50.29 — la 5ª reparación, la buena)

**La furia NUNCA llama a `NewNPC` para la chusma**: enchufa el motor natural de Terraria
(como Pumpkin/Frost Moon) solo para el ciclo de spawn **del portador**:
- `OleadaNPC.EditSpawnRate`: `spawnRate × max(0.08, 0.20 − 0.012k)` · `maxSpawns = 10 + 4k`
  (k=1: 14 vivos; k=10: 50).
- `OleadaNPC.EditSpawnPool`: pool 100 % reemplazado por el menú del **bioma del portador**
  (cacheado al arrancar la oleada). TODO el pool es vanilla (Infierno: Demon/FireImp/LavaSlime;
  Mazmorra: AngryBones/DungeonSlime/BlazingWheel; Granito; Mármol; Nieve; Jungla; Corrupción;
  Carmesí; Desierto; Playa; Cielo; Subsuelo; Superficie pre/HM día y noche con pools distintos).
- La posición/tile la valida **`NPC.SpawnNPC` de vanilla**: anillo 0.52-0.7× pantalla (nunca en
  pared), límites del mundo. La vieja cuna manual (BuscarCuna/CajaLibre) fue BORRADA en v6.50.29.
- `OleadaNPC.OnSpawn`: **todo hostil que nazca durante la chusma queda sellado** (colados
  incluidos); los hijos heredan el sello (`EntitySource_Parent` — el Devorador entero, Creepers).

**El sello** (`OleadaNPC.Marcar`, idempotente v6.50.10 — captura stats base, nunca encadena):
vida y daño **×(k+1)** (oleada 1 = ×2 … 10 = ×11; Especial ×15) · defensa +2k chusma / +6k jefes ·
`knockBackResist × 0.35` · aura `OleadaGrimorio(k)` · `CheckActive=false` (no despawnean mientras
dure el festín) · agresión (PostAI): empuje hacia la presa `0.20+0.02k`, re-objetivo cada 30 t
(jefes 20 t), homing `0.05+0.008k`, lunge cada `max(120, 300−18k)` t.

**Progresión por MUERTES** (estilo lunas): cada chusma muerta = 1 punto;
`PuntosRequeridos(k) = 12 + 6k` (oleada 1: 18 muertes; 10: 72). El reloj (máx 600 t/oleada) es
solo red de seguridad.

### 5.3 El jefe de oleada + EL DADO DEL INVIERNO (v6.50.30)

- `JefeDelLugar` (por zona del portador, **sin ver la hora** — a prueba de sol v6.50.25):
  mazmorra/nieve → Skeletron · jungla → Abeja · corrupción → Devorador · carmesí → Cerebro ·
  desierto → Rey · playa/cielo/infierno → Ojo (noche) / Rey (día) · subsuelo → mal del mundo ·
  superficie → paridad de oleada (impar Rey; par Ojo de noche / Skeletron de día).
  **Muro de Carne excluido a propósito. Deerclops ya NO guarda nada** (nieve→Skeletron,
  Juicio `LosSiete`→`LosSeis`).
- `SpawnJefeOleada`: nace en el **borde del cuadro** (½ pantalla + 140 px lateral, −240 ± 80
  vertical, sin validación de tiles), 75 % por la espalda; sello jefe + rugido + anuncio.
- **EL DADO**: tras spawnear el guardián, `Main.rand.Next(100) == 0` (**1 %**) →
  `NacerJefeRaro` = **Deerclops** en el **borde opuesto**, sello de la MISMA oleada, anuncio
  propio `Furia.JefeRaro` («EL DADO DEL INVIERNO… 1 de 100»). **No cuenta para la fase**; si
  sobrevive, el mundo lo despawnea al terminar la furia. Paga platino + SU esencia.
- **Los dientes** (`AtaqueOleadaProjectile`, 7 estilos por guardián, daño `16×(k+1)`):
  cuentas del Rey (8 orbes en anillo) · tajos del Ojo (marcas telegrafiadas) · **látigos de
  escarcha de Deerclops** (3 espinas zigzag) · aguijones de la Abeja (5 homing) · fauces del
  Devorador (3 relámpagos curvos) · reflejos del Cerebro (8 radiales) · calaveras de Skeletron
  (3 en órbita que se lanzan).

### 5.4 EL JUICIO (oleada 11 / Especial)

Tras un 10×10 COMPLETO (o Carnada=11): **Los Seis** (KingSlime, Ojo, Abeja, Devorador, Cerebro,
Skeletron) — 2 nacen YA + 1 cada 15 s, **todos ×15**. Termina cuando caen los 6 (conteo por
sellos, no por `npc.boss` — fix del EoW) → `Eco.Furia.JuicioFin` + `PerdonarHambre`.
**Muerte del portador** (cualquier fase): cancelación + **venganza de palabra** (4 variantes).

### 5.5 El indicador de oleada (v6.50.29)

`DibujarIndicador` = clon píxel a píxel de `Main.DrawInvasionProgress`: caja abajo-derecha
«**Oleada k: X %**» + barra vanilla (`ColorBar` + MagicPixel) amarilla/naranja/negra + cajita
de título con el icono del grimorio («Furia del grimorio»). En fase Jefe la barra va LLENA
(«oleada cobrada, jefe pendiente»); en el Juicio cuenta **guardianes caídos de 6**. En MP lee
réplicas del cliente (viajan en `MsgHambre`).

### 5.6 Recompensas

Chusma: k monedas de **oro** + 1 punto + XP ×(k+1). Jefe: k monedas de **platino** (Juicio: 15) +
**SU ESENCIA**. Al caer el jefe de la oleada 10 de un festín 10×10: `DerrotaOleada10 = true`
(persiste en el jugador — v6.50.61: la marca queda como CONSTANCIA; la tienda del Testigo
murió con él).

---

## 6. Las esencias y la economía

### 6.1 Esencias — `Content/Items/Esencias/`

`EsenciaDeJefeItem.UseItem` (devorar): busca el grimorio en slots 0-9 →
`SubirNivelDirecto(1)` (+1 nivel) → **voz del sabor de SU jefe**
(`Mods.AethonMod.Esencia.Sabor.<ClaveJefe>`, 3 variantes por jefe — v6.50.30, eran genéricas
antes) → 18 chispas doradas. **7 vanilla** (KingSlime, Ojo, Deerclops, Abeja, Devorador,
Cerebro, Skeletron — sin límite, de jefes de furia) + **1 del mod** (Aethon — **límite 10 por
mundo**, contador en el TagCompound del mundo, `EsenciasModSistema`; su índice de conteo
conserva el slot 4 histórico para que los mundos viejos no pierdan su cuenta). Valor 10 ORO.
(v6.50.61 — LA PURGA: las esencias de Titán, Guardián del Rift, Arquera y Primer Portador
murieron con sus jefes.)

### 6.2 El Testigo — MUERTO EN LA PURGA (v6.50.61)

**QUÉ ERA**: NPC ciudad amistoso, inmortal (orbe violeta), cronista y mercader de esencias —
nacía solo cada 10 s (`PresenciaNPCSystem`, también borrado) y vendía las esencias vanilla a
quien sobrevivía la oleada 10 + Fragmentos de Resonancia a nivel 50+. **LA PURGA** (la letra:
«borra a todos los NPC del mod, solo deja al jefe principal, Aethon, borra otros jefes y al
testigo») lo borró ENTERO: NPC, presencia, tienda (`NPCShop` + `Condition` + `MsgPedirResonancia`
con su handler), anuncios y ~90 líneas de localización por idioma. LO QUE QUEDA VIVO DE SU
HERENCIA: `ShardPlayer.CronicaJefes`/`CronicaNarrada`/`DerrotaOleada10` siguen registrándose
y persistiendo (constancia silenciosa — los .plr viejos cargan igual) y `ResonanceShard`
sigue cayendo de Aethon. La crónica ya no tiene narrador: el registro es historia.
**Economía**: chusma = oro · jefes de furia = platino · ResonanceShard = moneda secundaria
(cae de Aethon).

---

## 7. Los jefes

**Archivos**: `Content/NPCs/` (AethonBoss 1.269 l · CazadorAstral — el cuerpo del ataque
LA MANADA ASTRAL, vida corta, se apaga si Aethon muere) · proyectiles en
`Content/Projectiles/Jefes/AtaqueJefeProjectile.cs` (959 l, 13 estilos) · fondo del jefe en
`Content/Effects/ColaSierpeSky.cs` (499 l) · arte en `Content/VFX/AethonSierpeArte.cs` (350 l) ·
invocador en `Content/Items/Llamados/LlamadosJefesItems.cs` (El Nombre de Aethon — el único
que queda). **v6.50.61 — LA PURGA**: HollowTitan, RiftKeeper, EchoArcher y EchoBlade fueron
BORRADOS con sus invocadores, esencias y textos.

### 7.1 AETHON, EL GRIMORIO ETERNO (el jefe final — 2.400.000 PV, daño 95 — LA PRUEBA DEL
PROPIO GRIMORIO: el libro se alza de sus páginas para juzgar a su portador)

Invocación: **El Nombre de Aethon** (solo día, reutilizable). Nacimiento: se posiciona BAJO
TIERRA (±560, +900) y asciende mientras el **leviatán de llegada** cruza el cielo (ColaSierpeSky).
**46 vértebras + cola** (HUECO 64 px, ESC 1.40 → ~3.400 px de sierpe; vida compartida:
golpear cualquier hueso duele a la cabeza; índice ≥ 34 se oculta y lo dibuja el fondo del cielo
entre las capas del paisaje — la cola se ENREDA en el horizonte).

**Fases** (histéresis, cura +5 % al cambiar): 1 Polvo Estelar (100-80 %) · 2 La Nebulosa ·
3 La Gravedad (+ **EL ALIENTO PRIMORDIAL**: 40 t de carga + 70 t de arco PerlinBolt boca→presa
+ lluvia de pernos cada 8 t) · 4 El Agujero Negro (singularidad que atrae + jets + runas) ·
5 El Reconocimiento (**ElRecordar**: 7 pernos en abanico + EL GRAN TAJO).

**Estados**: NACIENDO (200 t) → BAJO_TIERRA (nada bajo la presa a vel 13+1.5·fase; lunge
predicho vel 24+2.2·fase) → **EMERGIENDO** (el lunge — v6.50.30: **UNA sola volleada en tick
12**, antes disparaba CADA TICK = hasta 700 pernos) → SUPERFICIE (arco elíptico sobre la presa;
aliento fase 3+) → **CARGA** (v6.50.26: telegraph 36 t + embestida horizontal 26+2.5·fase con
siembra de pernos fase 4+) → HUNDIÉNDOSE (gravedad; fase 4 al clavarse nace la singularidad).

**Arte (v6.50.27 — 100 % código, `PreDraw=false`)**: LA SIERPE ESTELAR — cabeza = **eclipse**
(esfera de noche + anillo de oro fino + corazón blanco) + **fauces en V** (2 hojas oscuras +
2 filos de luz que giran con la abertura de mandíbula 0.05-0.55) + colmillos-estrella + 2 ojos
(oro; violeta fase 3+) + 5 chispas orbitantes · vértebras = placas de vacío en CUENTAS + espina
de oro + apófisis-estrella + aletas de varillas en 6/14/22 · fondo = ColaSierpeSky (proyección
DoG entre capas del paisaje). **Los sprites viejos de la sierpe están MUERTOS** (sin
referencias — candidatas a borrar).

**Muerte**: cine de DESARTICULACIÓN (se yergue; cada 4 t muere un hueso cola→cabeza; a tick 220
estallido) → Esencia de Aethon + **Forma Ascendida** (cosmético) + 250 Resonance.

### 7.2 Los otros 4 (todos 100 % código, invocador solo-día reutilizable, sin recetas)

- **Titán Hueco** (42.000 PV, Fighter que CAMINA): furia < 50 % duplica cadencia · púas que se
  clavan y detonan · coro de 6 esquirlas orbitando · el porrazo · salto antiaéreo · carga
  telegrafiada. Drop: Esencia + 8 Resonance.
- **Guardián del Rift** (95.000, vuela): **teletransporte en 3 fases** (se disuelve en un
  desgarro vertical `RiftLib.TearVacio`, renace al ángulo opuesto a 340 px con jitter
  determinista anti-rubber-band MP) · virotes de vacío con lead · LAS CUATRO PAREDES
  (cortes de realidad en los 4 cardinales avanzando). Drop: Esencia + 45 Resonance.
- **Arquera Estelar** (160.000, vuela): esquivón lateral · flechas con lead · **minas que se
  arman con pulso y detonan en ARCO VOLTAICO** (`StormLib.ChainBolt` + hitbox honesta 14→110 px)
  · lluvia estelar (furia) · **EL ARCO VIVO SE TENSA DE VERDAD**. Drop: Esencia + 110 Resonance.
- **Primer Portador** (180.000, vuela): ciclo del duelista (acecho → marca 18 t → tajo diferido
  donde ESTÁS → embestida → retroceso) · **PARRY 20 %** (`ModifyIncomingHit`, cooldown 120 t) ·
  cuchillas en órbita (furia) · LA HOJA VIVA arquea al golpear. Drop: Esencia + 120 Resonance.

---

## 8. El pipeline de rayos (100 % código)

> Historia en 3 generaciones: sprites (BoltHalo/BoltCore — MUERTOS) → pila de pasadas →
> **hoy: 4 capas, CERO sprites de rayo, el `.tmod` no contiene NI UN PNG de rayo.**

### 8.1 Mapa (4 capas)

| Capa | Archivo | Qué hace |
|---|---|---|
| **GENERACIÓN** | `Content/VFX/RayoLib.cs` (973 l) | Puerto 1:1 del **LightningGenerator de vanilla 1.4.5** (el del clima y del arma Arc Surge): raymarch de 8 px, 4-5 capas de ángulo (la fina cada ~2 pasos; la superior = el QUIEBRE que engendra horquillas), timón hacia el blanco, DES-randomización al 80 %, `Lcg32` determinista (`state·2438992949+1`), horquillas espejo `1+0.4·delta` con timing sincronizado, colisión con tiles y líquidos. **3 presets verbatim**: Tormenta (1000 px) · Arma (750 px) · Arco(a,b) (mano→blanco, remapeos por distancia). |
| **ESTADO** | `RayoSistema` (en RayoLib) | El **pozo de 24 canales** `RayoActivo`: `Lanzar` (2 overloads — uno con la firma VenaTrueno: recada/parpadeo/anchoFin/aperturaColapso), `Recadar` (re-germina el árbol cada 4 t con otra semilla; en agonía el meandro se ABRE ×1→×3.5), luz SteadyLight (AddLight cada 20 puntos), `PostDrawTiles` (parpadeo 50 %, ancho creciente, **ancla elástica del Arc Surge**: el arco CUELGA de la mano con peso cuártico `(1−d²/D²)²`). |
| **RENDER** | `Content/VFX/RayoStrip.cs` (472 l) | **Tira de triángulos** con `BasicEffect` (sin textura), pool de 16.384 vértices (cero GC). 5 columnas de funda gaussiana (halo tenue) + 3 de vena (núcleo blanco). Color **PREMULTIPLICADO por vértice** + blend **soft-add** `dst += rgb·(1−dst)` — dos rayos que se cruzan NUNCA clipean (el fix de los "cortes" del brillo, v6.50.26-27). Lote anidable. |
| **PINCEL** | `Content/VFX/VFXCore.cs` | `Pixel` = MagicPixel 1×1 DEL MOTOR, SoftGlow/GlowOrb/Ring/**DestelloFinal**, sonda de lote por reflexión, presupuesto 24.000 quads × factor de calidad por FPS, Janitor. |

**StormLib** (`Content/VFX/StormLib.cs`, 1.624 l) es la 3ª generación paralela: árbol fractal
`BuildTree/Ramificar` (ramas nacen en VÉRTICES del padre, heredan ×0.62 ancho/×0.74 brillo,
presupuesto 10), `ZigPath`, **`PerlinBolt`** (fBm 1D meandro + Chaikin — la firma distinta del
meandro), `MultiBolt/ChainBolt`, `ArcRing`, `ImpactFlash`, `AddLightAlong`. ~72 archivos consumen
el stack. `BoltRenderer` = primera generación, hoy montado sobre RayoStrip.

### 8.2 Las 5 armas de rayo (bolsa 18)

Bastón de Rayo Primordial (`RayoParams.Tormenta`, impacto instantáneo + cadena) ·
**Arco de Sobretensión** (el Arc Surge de vanilla 1.4.5 portado como arma: anclado a la mano,
+2 hermanos a NPCs en cono 60°, carmesí) · **Colmillo de Vena Trueno** (la firma del Thundervein
Wyvern de Coralite: **trío 1 naranja (219,114,22) + 2 amarillos (255,202,101)**, recada 4 t,
parpadeo 50 %, caída telegrafiada que golpea AL ATERRIZAR) · Cetro del Trueno Rúnico (StormLib
clásico) · Cetro del Trueno Perlin (`PerlinBolt`, el arco sigue al cursor 10 t).

---

## 9. El destello

**La historia completa del "círculo plano"** (3 sesiones de forense):
v6.50.15 mató el velo full-screen · v6.50.16 arregló el color premultiplicado del velo ·
v6.50.17 mató LA SUMA (anillo+gradiente+dusts apilados) → **NovaBurst monótona** ·
v6.50.26-27 → **`VFXCore.DestelloFinal`**: la CRUZ DE 8 RAYOS + corazón caliente (nunca un
disco) · **v6.50.28 mató los velos de disco en la capa de UI** (el "destello del Sol" que
tapaba la cruz — la causa raíz de «no le atinas al destello»).
**HOY**: usuarios del destello = Sol/Supernova (`DestelloFinal` 620 px), BlackHole (560 px),
CosmicShockwave. **No queda NINGÚN círculo plano en rutas activas** (grep-verificado).
Residuos intencionales: BlackDisk (núcleo NEGRO de agujeros), GlowCircleWhite (solo ítems de
test V20), NovaFlash (preset SIN llamadores — doc-rot).

---

## 10. El stack VFX de la casa (67 archivos)

Todo propio (`modReferences` vacío). **Convención del lote del llamador** (v6.10): las funciones
de dibujo esperan el SpriteBatch ABIERTO; el que abre, cierra. **Determinismo**: `VFXCore.Hash01`
es la semilla de TODO el mod — cero `Main.rand` en render. **Cero GC**: pools de vértices/quads
reutilizados. Bibliotecas: VFXCore · VFXPalettes · RuneSunRenderer (+ emisor de anillos público) ·
BlackHoleLensSystem (2 render targets, lente a pantalla completa) · BrumaFX/Brushes/Noise (humo
flipbook premultiplicado) · StormLib · **RayoLib + RayoStrip** · LumenLib (luz) · EstelaLib
(ribbons) · OndaLib (ondas + Kick centralizado) · PyraLib (fuego Doom determinista) · RiftLib
(desgarros) · ParticleManager (data-oriented, 12 texturas) · SigiloLib · SierpesLib · TajoLib ·
EspectroLib · AuraLib (aura-llama con 7 lenguas) · AethonSierpeArte.

---

## 11. Red, persistencia y configuración

**Regla de oro**: la AUTORIDAD es el servidor (SP = mismo proceso). Todo spawn de furia/jefes/
esencias pasa por el server; los clientes ven réplicas (`MsgHambre` lleva fase/oleada/puntos;
`SendExtraAI` lleva el sello + stats escalados exactos — el msg 23 de vanilla NO lleva
damage/defense).

**Qué persiste y dónde**:
- `.plr` (jugador): nivel/XP por libro (`aethonLevel/aethonXP`), `resonanceShards`,
  `cronicaJefes/cronicaNarrada`, `DerrotaOleada10`.
- `.twld` (mundo): contadores de esencias del mod (`esenciasModDadas` — límite 10/jefe), altares.
- **NADA de la furia se guarda** (fase/hambre volátiles — la furia muere con la sesión).

**Config**: `AethonConfig` (cliente: XP multiplier visual, notificaciones, debug) ·
`AethonConfigServidor` (server-side: `EventoHambreGrimorio` ON — desactiva la furia automática
pero NO la Carnada; `XPMultiplier`).

**TODOs de red documentados**: voces del libro en servidor DEDICADO (mitigado por EcoRed en
client-host); `SyncResonance` en ShardSyncSystem es un stub heredado.

---

## 12. Reglas de la casa (no negociables)

1. **GitHub es la fuente de la verdad** — verificar `git fetch` ANTES de actuar (el sandbox
   resetea). Commits con mensaje detallado + tag + push + release con `.tmod`.
2. **Cero sprites de rayo** — todo por RayoLib/RayoStrip/RayoSistema.
3. **Destellos con degradado monotónico** — nunca discos/círculos planos (la saga completa §9).
4. **Color premultiplicado** en lotes aditivos; soft-add para acumulación sin clipping.
5. **`Hash01` determinista** en TODO render (cero `Main.rand`, cero alocaciones por frame).
6. **Lote del llamador** (el que abre el SpriteBatch, lo cierra).
7. **hjson es-MX + en-US simétricos SIEMPRE** (verificación de paquete antes de release; es-ES borrado en la v6.50.70).
8. **Sin maná** en el arsenal de pruebas; ítems de prueba marcados "eliminar antes de release".
9. **Mock 1:1 + VLM** para validar VFX nuevos ANTES de integrar (patrón `tools/mock_*.py`,
   umbral 7/10 de la casa).
10. **Build de verificación**: verify.csproj 0/0 → `-build` REAL → `.tmod` auditado (tabla byte
    a byte) → DLL inspeccionada → servidor headless carga sin excepciones (COMPILACION.md).
11. **Sello idempotente** (`Marcar` captura stats base) — nunca re-marcar desde stats escalados.
12. El nombre del mod = el nombre de la CARPETA (`AethonMod`) — el guardián de identidad de
    `AethonMod.cs` explica y falla con instrucciones si alguien lo rompe.

---

## 13. Mapa maestro de carpetas

```
AethonMod/                      ← EL MOD (build.txt aquí: versión 6.50.30)
├── AethonMod.cs                ← Punto de entrada, guardián de identidad, router de paquetes
├── Content/
│   ├── VFX/                    ← EL CORAZÓN: VFXCore, RayoLib, RayoStrip, StormLib, EcoLib,
│   │                              LumenLib, EstelaLib, OndaLib, PyraLib, RiftLib, AuraLib,
│   │                              AethonSierpeArte, BoltRenderer, …(67 archivos)
│   ├── Systems/                ← GrimorioFuriaSistema, ShardLevel*, EcoRed/EcoSistema,
│   │                              WeaponScaling, EsenciasModSistema, AethonLlegadaSistema,
│   │                              Onda/Ocaso/Bruma/Cielo/CalidadFps/DiagnosticoVFX…
│   ├── Players/                ← ShardPlayer (hambre), TestingPlayer (bolsas), + 4
│   ├── Globals/                ← OleadaNPC (el sello), GlobalNPCXP, ShardLevelItem, + 3
│   ├── NPCs/                   ← AethonBoss, CazadorAstral (la Manada del jefe) — v6.50.61:
│   │                              SOLO Aethon queda (la purga borró a los otros 5)
│   ├── Weapons/Cosmic/         ← ~100 armas por familias (Apuestas/Sierpes/Desgarros/
│   │                              CodigosVivos/RuneSun/RealStar/BlackHole/IdeasGrimorio…)
│   ├── Weapons/ (raíz + V20)   ← GrimoireEternal, 4 fundacionales, tests
│   ├── Projectiles/Cosmic/     ← ~84 proyectiles (incl. Jefes/ y Oleadas/)
│   ├── Items/                  ← Esencias/ (8), Llamados/ (1), Bolsas/ (18), Accessories/,
│   │                              Cosmetics/, Placeables/, sueltos (SeerOrb, GenesisShard…)
│   ├── Buffs/ (9) · Biomes/ (1) · Tiles/ (AncientAltar) · Effects/ (ColaSierpeSky, shaders,
│   │                              Bruma/, BlackHoleLensSystem) · Particles/ (manager+presets)
│   └── Localization/           ← es-MX + en-US (hjson, ~2300 líneas c/u — es-ES borrado en la v6.50.70)
└── (raíz del repo: README, CHANGES, COMPILACION, DISEÑO, CARACTERISTICAS, STABLE-SNAPSHOT,
    _masters/ sprites maestros, tools/ generadores+mocks, ACTUALIZAR-FUENTE.bat/.sh)
```

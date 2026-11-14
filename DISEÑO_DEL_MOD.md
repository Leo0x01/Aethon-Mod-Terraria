# 📜 DISEÑO y ARQUITECTURA del Mod — "Aethon, el Grimorio Eterno"

> 📜 **DOC HISTÓRICA** — Este documento describe una era anterior del
> proyecto. Para el estado vigente y las reglas actuales manda
> **[MAESTRO.md](MAESTRO.md)**; esta copia se conserva como referencia.


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
**100 % por código** (los rayos sin NI UN sprite; EXCEPCIÓN ÚNICA: el Códice
Vivo de la v6.50.71 — el sprite nacido de código que pidió el usuario) ·
cero dependencias de otros mods · es-MX y en-US siempre simétricos + es-ES
espejo generado de es-MX (v6.50.71) · "arsenal de pruebas": las armas no cuestan maná y se entregan en bolsas.

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
del devorador entre capas del paisaje). **Los sprites viejos de la sierpe están MUERTOS** (sin
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
+2 hermanos a NPCs en cono 60°, carmesí) · **Colmillo de Vena Trueno** (la firma del wyvern de venas
de trueno de la referencia del dragón: **trío 1 naranja (219,114,22) + 2 amarillos (255,202,101)**, recada 4 t,
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
7. **hjson es-MX + en-US simétricos SIEMPRE** (verificación de paquete antes de release) + es-ES ESPEJO GENERADO de es-MX tras cada edición (v6.50.71 — todas las variantes de español caen en la nuestra).
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

## 14. LA ERA DEL DESPERTAR 100 (v6.85-v6.88) — la arquitectura de la cima

- **EL RITUAL POR LIGAS** (`RayoConvergenteProjectile`): el ataque combinado lanza un agujero
  ALEATORIO por familia — la bruma NORMAL (`BrumaBlackHoleProjectile`, 1 anillo rúnico, la clase
  del bastón) corre con Olvido/Cósmico/Umbral normales; la bruma ASCENDIDA (doble círculo rúnico)
  SOLO cuando el portador tiene AgujerosAscendidos, con los OTROS Ascendidos. Dos tipos
  diferentes en ligas diferentes — nunca mezcladas.
- **LA PUERTA COMPUESTA 100** (`LosJefesDelDespertar` + `CazaJefesPlayer`): la puerta 100 cobra
  la caza ANTES DE SUBIR (Aethon ×1 ∧ Moon Lord ×10 ∧ Emperatriz ×5) y `IraDisponible` nace con
  el despertar 100 — la cura del «el haz no se lanza» (la v6.86 cobraba DESPUÉS de subir: si no
  los tenía, NUNCA nacía). Las repeticiones (63-99) reparten la caza por la segunda mitad.
- **LAS OFRENDAS REPARTIDAS**: monedas exponenciales 100 cobre → 100 platino (`MonedasPara`,
  g=10^(6/99)) · LA OFRENDA MINERAL VARIADA (`GruposMineralDe` + `CantidadPorGrupo`: mezcla de
  2-3 minerales de la era, 18+nivel/5 por cabeza, pagos GRUPO A GRUPO) · LOS HITOS DEL ESCUADRÓN
  VIVOS (`MultEscuadron`: cada 20 niveles, en porcentaje — lo leen el daño de cada ojo y los
  conjuros combinados; el ×1,25 horneado murió).
- **EL ALTAR DE LA CORRUPCIÓN** (`AltarOfrendasUI` + `CorruptoGlobalItem`): YA sin vínculo con
  el grimorio — corrompe armas (nombre «(Corrupto)» en negro + velo morado-negro en el
  inventario + aura en el suelo). La economía de puntos/canje del 30 % murió de raíz.
- **EL ÁRBOL CAMBIA DE DUEÑO** (`SkillTreePlayer` + `LibroAzul`): los puntos nacen del NIVEL
  DEL LIBRO AZUL (1 + bajas/8; puntos 3 + nivel/5) y los efectos solo viven mientras se LLEVA
  el arma. El Códice de Habilidades fue borrado (clase y PNG).
- **LA GUARDIA DEL DUEÑO** (`EscuadronPersistPlayer.LlevaSuGrimorio`): el escuadrón persistente
  solo se fotografía y solo resucita en el personaje que lleva su Grimorio del Eterno — jamás
  en cualquier otro personaje del mismo jugador.
- **LA ZONA DEL FESTÍN ES SAGRADA** (`FaucesGlobalNPC.ParteDeJefeDevorado` +
  `HayJefeDevoradoCerca` + `FaucesGlobalProjectile`): durante la devoración, la familia del
  jefe ni golpea ni recibe, el PreAI devuelve false, y los proyectiles hostiles YA disparados
  se apagan en vuelo. El WoF cena clavado en su columna (sin teletransporte) con los HUNGRY
  en el nudo, y `HitEffect` mantiene la vida en 1 (sin gore, sin rejilla de cadáver).
- **FUERA DEL QUICKBUFF** (AethonMenor + GrimorioMenor): sin `Item.buffType` — el imán de la
  tecla B murió; el buff nace con el clic del ítem y la mascota lo sostiene (patrón canónico
  `BuffHandle_SpawnPetIfNeededAndSetTime`).
- **LAS DOS BANDAS** (`OleadaHambreMusica`): «El Festín Desatado» (152) y «La Oleada Final»
  (164) — cada oleada tira una moneda (Main.rand.NextBool, viaja bien en MP).
- **LA LECCIÓN v6.88.1 (los dos tapones)**: `-build` empaqueta sin validar y el headless NO
  carga texturas — SOLO el headless carga hjson. `audit_assets` cazó el PNG del perno del Libro
  Azul (tML exige textura de TODO ModProjectile al cargar) y el headless cazó el hjson huérfano
  (el bloque viejo del altar flotando tras la llave de cierre). Ambos murieron ANTES del release
  por la cadena obligatoria de la casa — el usuario jamás los vio.

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
│   └── Localization/           ← es-MX (PRIMARIO) + en-US + es-ES espejo generado (hjson)
└── (raíz del repo: README, CHANGES, COMPILACION, DISEÑO, CARACTERISTICAS, STABLE-SNAPSHOT,
    _masters/ sprites maestros, tools/ generadores+mocks, ACTUALIZAR-FUENTE.bat/.sh)
```

---

## §15 — LA ESCALERA COMPLETA DEL DESPERTAR (v6.90)

La tabla viva de las CIEN puertas: cada nivel con SU arma de magia/
invocador (las alternativas — EL GRIMORIO devora UNA) **O** SU material
(una opción completa — **BASTA UNO DE LOS DOS**; EL GRIMORIO se queda
con la primera opción que alimentes), SUS esencias y SUS monedas. **TODO
lo que el grimorio come debe estar CONSAGRADO por el altar** (el arma y
los materiales llevan el sello — las esencias no lo necesitan: ellas
SON la esencia del Eterno). La leen la ventana de Progresión del
tooltip, El Cielo del Despertar y la mesa del suelo.

| Nivel | Puerta de caza | Arma a devorar (elige 1) | Material (1 opción) | Esencias | Monedas |
|---|---|---|---|---|---|
| 1 | — | Bastón de gelatina | 20× lingote de cobre/lingote de estaño | 1 | 1 plata |
| 2 | — | Bastón de amatista o Bastón de topacio | 20× lingote de cobre/lingote de estaño/amatista/topacio | 2 | 1 plata |
| 3 | Rey Gelatina ✓ | Bastón de zafiro o Bastón de esmeralda | 20× lingote de plata/lingote de tungsteno/zafiro/esmeralda | 3 | 1 plata |
| 4 | — | Bastón de rubí o Bastón de diamante | 20× lingote de oro/lingote de platino/rubí/diamante | 4 | 1 plata |
| 5 | Ojo de Cthulhu ✓ | Arma espacial o Vilethorn o Vara carmesí | 40× lingote de meteorito | 5 | 1 plata |
| 6 | — | Bastón de ámbar | 20× ámbar/fósil | 6 | 2 plata |
| 7 | — | Bastón de flinx | 20× piel de flinx/lingote de oro/lingote de platino | 7 | 2 plata |
| 8 | El Mal del Mundo ✓ | Bastón de diablillo | 30× lingote de piedra infernal | 8 | 2 plata |
| 9 | — | Flamelash | 30× lingote de piedra infernal/lingote de demonita/lingote de carmesí | 9 | 3 plata |
| 10 | Reina Abeja ✓ | Bastón de avispón | 30× cera de abeja | 10 | 3 plata |
| 11 | — | Pistola abeja | 30× cera de abeja | 11 | 4 plata |
| 12 | — | Guadaña demoníaca | 20× lingote de demonita/lingote de carmesí/lingote de piedra infernal | 12 | 4 plata |
| 13 | Skeletron ✓ | Proyectil acuático o Libro de calaveras | 30× lingote de piedra infernal/lingote de demonita/lingote de carmesí | 13 | 5 plata |
| 14 | — | Cetro acuático | 30× lingote de piedra infernal | 14 | 6 plata |
| 15 | — | Misil mágico | 20× lingote de demonita/lingote de carmesí | 15 | 7 plata |
| 16 | — | Daga mágica | 30× lingote de piedra infernal | 16 | 8 plata |
| 17 | — | Flamelash | 30× lingote de piedra infernal | 17 | 9 plata |
| 18 | — | Guadaña demoníaca | 20× lingote de demonita/lingote de carmesí | 18 | 10 plata |
| 19 | — | Bastón de diablillo | 30× lingote de piedra infernal | 19 | 12 plata |
| 20 | La Muralla + EL LIBRO ✓ | Pistola avispa | 30× cera de abeja | 20 | 14 plata |
| 21 | — | Lluvia dorada o Llamas malditas (arma) | 40× icor/llama maldita/alma de la noche | 21 | 16 plata |
| 22 | — | Tormenta de cristal | 40× fragmento de cristal/alma de la luz | 22 | 18 plata |
| 23 | Reina Gelatina (difícil) ✓ | Serpiente de cristal | 40× fragmento de cristal/alma de la luz | 23 | 21 plata |
| 24 | — | Vara del nimbio | 30× alma de la noche/alma de la luz | 24 | 24 plata |
| 25 | Los Gemelos ✓ | Bastón de veneno | 20× alma del poder | 25 | 28 plata |
| 26 | — | Bastón de araña | 20× lingote de cobalto/lingote de paladio | 26 | 32 plata |
| 27 | — | Bastón de la reina araña | 20× lingote de mitril/lingote de oricalco/colmillo de araña | 27 | 37 plata |
| 28 | El Devorador ✓ | Desgarro del cielo | 20× fragmento de luz/alma de la luz | 28 | 43 plata |
| 29 | — | Cabeza de medusa | 20× lingote de adamantita/lingote de titanio | 29 | 49 plata |
| 30 | Skeletron Prime ✓ | Bastón óptico | 20× lingote sagrado/alma de visión | 30 | 57 plata |
| 31 | — | Bastón de ponzoña | 30× lingote de clorofita | 31 | 65 plata |
| 32 | Plantera ✓ | Bastón pigmeo | 30× lingote de clorofita/lingote sagrado | 32 | 75 plata |
| 33 | — | Soplador de hojas | 30× lingote de clorofita | 33 | 86 plata |
| 34 | — | Esfera magnética | 20× ectoplasma | 34 | 1 oro |
| 35 | El Gólem ✓ | Rayo de calor | 20× lingote de hongonita/lingote de espectro | 35 | 1 oro 14 plata |
| 36 | — | Bastón de tierra | 20× lingote de hongonita/lingote de espectro | 36 | 1 oro 32 plata |
| 37 | Duque Fishron ✓ | Pistola de burbujas o Bastón de la tempestad | 20× lingote de hongonita/lingote de espectro | 37 | 1 oro 51 plata |
| 38 | La Emperatriz de la Luz ✓ | Nightglow o Stellar Tune o Terraprisma | 20× lingote de hongonita/lingote de espectro | 38 | 1 oro 74 plata |
| 39 | — | Cetro de los murciélagos | 20× lingote de espectro/lingote de hongonita | 39 | 2 oro |
| 40 | — | Razorpine o Bastón de ventisca o Bastón de la hidra de escarcha | 20× lingote de hongonita/lingote de espectro | 40 | 2 oro 31 plata |
| 41 | — | Bastón alienígena o Ametralladora láser | 20× lingote de hongonita/lingote de espectro | 41 | 2 oro 65 plata |
| 42 | — | Bastón del rayo sombrío | 20× lingote de espectro | 42 | 3 oro 5 plata |
| 43 | — | Tenedor infernal | 20× lingote de hongonita | 43 | 3 oro 51 plata |
| 44 | — | Bastón espectro o Bastón del cuervo | 20× lingote de espectro/lingote de hongonita | 44 | 4 oro 3 plata |
| 45 | El Cultista Lunático ✓ | Cañón blaster cargado | 20× lingote de espectro/lingote de hongonita | 45 | 4 oro 64 plata |
| 46 | — | Arcano de nebulosa | 40× fragmento de nebulosa | 46 | 5 oro 33 plata |
| 47 | — | Bastón del dragón de polvo estelar | 40× fragmento de polvo estelar | 47 | 6 oro 13 plata |
| 48 | — | Fuego de nebulosa | 40× fragmento de nebulosa | 48 | 7 oro 5 plata |
| 49 | — | Bastón de celdas de polvo estelar | 40× fragmento de polvo estelar | 49 | 8 oro 11 plata |
| 50 | El Señor de la Luna ✓ | Prisma final o Destello lunar | 20× lingote de luminita/fragmento de nebulosa/fragmento de polvo estelar | 50 | 9 oro 32 plata |
| 51 | — | Bastón del cristal arcoíris | 20× lingote de luminita/fragmento de polvo estelar | 51 | 10 oro 72 plata |
| 52 | — | Portal lunar | 20× lingote de luminita/fragmento de polvo estelar | 52 | 12 oro 32 plata |
| 53 | — | Prisma final o Destello lunar | 20× lingote de luminita | 53 | 14 oro 17 plata |
| 54 | — | Bastón del cristal arcoíris o Portal lunar | 20× lingote de luminita | 54 | 16 oro 29 plata |
| 55 | — | Fuego de nebulosa | 40× fragmento de nebulosa | 55 | 18 oro 73 plata |
| 56 | — | Arcano de nebulosa | 40× fragmento de nebulosa | 56 | 21 oro 54 plata |
| 57 | — | Bastón del dragón de polvo estelar | 40× fragmento de polvo estelar | 57 | 24 oro 77 plata |
| 58 | — | Bastón de celdas de polvo estelar | 40× fragmento de polvo estelar | 58 | 28 oro 48 plata |
| 59 | — | Prisma final | 20× lingote de luminita | 59 | 32 oro 74 plata |
| 60 | — | Prisma final | 20× lingote de luminita/fragmento de nebulosa | 60 | 37 oro 64 plata |
| 61 | — | Destello lunar | 20× lingote de luminita/fragmento de polvo estelar | 61 | 43 oro 28 plata |
| 62 | — | Bastón del cristal arcoíris | 20× lingote de luminita/fragmento de nebulosa | 62 | 49 oro 77 plata |
| 63 | Señor de la Luna ×2 ✓ | Portal lunar | 20× lingote de luminita/fragmento de polvo estelar | 63 | 57 oro 22 plata |
| 64 | — | Fuego de nebulosa | 40× fragmento de nebulosa | 64 | 65 oro 79 plata |
| 65 | — | Arcano de nebulosa | 40× fragmento de nebulosa | 65 | 75 oro 64 plata |
| 66 | — | Bastón del dragón de polvo estelar | 40× fragmento de polvo estelar | 66 | 86 oro 97 plata |
| 67 | Emperatriz ×2 ✓ | Stellar Tune | 20× lingote de hongonita/lingote de espectro | 67 | 1 platino |
| 68 | — | Terraprisma | 20× lingote de hongonita/lingote de espectro | 68 | 1 platino 14 oro 97 plata |
| 69 | — | Nightglow | 20× lingote de hongonita/lingote de espectro | 69 | 1 platino 32 oro 19 plata |
| 70 | — | Pistola de burbujas | 20× lingote de hongonita/lingote de espectro | 70 | 1 platino 51 oro 99 plata |
| 71 | Señor de la Luna ×3 ✓ | Bastón de la tempestad | 20× lingote de hongonita/lingote de espectro | 71 | 1 platino 74 oro 75 plata |
| 72 | — | Tifón de cuchillas | 20× lingote de hongonita/lingote de espectro | 72 | 2 platino 92 plata |
| 73 | — | Rayo de calor | 20× lingote de hongonita/lingote de espectro | 73 | 2 platino 31 oro 1 plata |
| 74 | — | Bastón de tierra | 20× lingote de hongonita/lingote de espectro | 74 | 2 platino 65 oro 60 plata |
| 75 | Emperatriz ×3 ✓ | Cañón blaster cargado | 20× lingote de espectro/lingote de hongonita | 75 | 3 platino 5 oro 38 plata |
| 76 | — | Bastón de ventisca | 20× lingote de hongonita/lingote de espectro | 76 | 3 platino 51 oro 11 plata |
| 77 | — | Razorpine | 20× lingote de hongonita/lingote de espectro | 77 | 4 platino 3 oro 70 plata |
| 78 | — | Bastón de la hidra de escarcha | 20× lingote de hongonita/lingote de espectro | 78 | 4 platino 64 oro 15 plata |
| 79 | Señor de la Luna ×4 ✓ | Cetro de los murciélagos | 20× lingote de espectro | 79 | 5 platino 33 oro 66 plata |
| 80 | — | Bastón del rayo sombrío | 20× lingote de espectro/lingote de luminita | 80 | 6 platino 13 oro 59 plata |
| 81 | — | Tenedor infernal | 20× lingote de espectro/lingote de luminita | 81 | 7 platino 5 oro 48 plata |
| 82 | — | Esfera magnética | 20× lingote de espectro/lingote de luminita | 82 | 8 platino 11 oro 13 plata |
| 83 | Emperatriz ×4 ✓ | Bastón espectro | 20× lingote de espectro/lingote de luminita | 83 | 9 platino 32 oro 60 plata |
| 84 | — | Bastón del cuervo | 20× lingote de espectro/lingote de luminita | 84 | 10 platino 72 oro 26 plata |
| 85 | — | Soplador de hojas | 20× lingote de espectro/lingote de luminita | 85 | 12 platino 32 oro 84 plata |
| 86 | — | Fuego de nebulosa | 40× fragmento de nebulosa | 86 | 14 platino 17 oro 47 plata |
| 87 | Señor de la Luna ×5 ✓ | Prisma final | 20× lingote de luminita/fragmento de nebulosa | 87 | 16 platino 29 oro 75 plata |
| 88 | — | Ametralladora láser | 20× lingote de hongonita/lingote de espectro | 88 | 18 platino 73 oro 81 plata |
| 89 | — | Bastón alienígena | 20× lingote de espectro/lingote de luminita | 89 | 21 platino 54 oro 43 plata |
| 90 | — | Bastón pigmeo | 30× lingote de clorofita/lingote de espectro | 90 | 24 platino 77 oro 7 plata |
| 91 | Emperatriz ×5 ✓ | Serpiente de cristal | 30× lingote de clorofita/lingote de espectro | 91 | 28 platino 48 oro 3 plata |
| 92 | — | Bastón de la tempestad | 20× lingote de hongonita/lingote de espectro | 92 | 32 platino 74 oro 54 plata |
| 93 | — | Terraprisma | 20× lingote de hongonita/lingote de espectro | 93 | 37 platino 64 oro 93 plata |
| 94 | — | Tifón de cuchillas | 20× lingote de hongonita/lingote de espectro | 94 | 43 platino 28 oro 76 plata |
| 95 | Señor de la Luna ×6 ✓ | Prisma final o Destello lunar | 20× lingote de luminita/fragmento de nebulosa | 95 | 49 platino 77 oro 2 plata |
| 96 | — | Bastón del cristal arcoíris o Portal lunar | 20× lingote de luminita/fragmento de polvo estelar | 96 | 57 platino 22 oro 36 plata |
| 97 | — | Fuego de nebulosa o Arcano de nebulosa | 40× fragmento de nebulosa | 97 | 65 platino 79 oro 33 plata |
| 98 | — | Bastón del dragón de polvo estelar o Bastón de celdas de polvo estelar | 40× fragmento de polvo estelar | 98 | 75 platino 64 oro 63 plata |
| 99 | Señor de la Luna ×7 ✓ | Prisma final o Destello lunar o Bastón del cristal arcoíris o Portal lunar | 20× lingote de luminita/fragmento de nebulosa | 99 | 86 platino 97 oro 49 plata |
| 100 | LA CIMA: Aethon ×1 + Moon Lord ×10 + Emperatriz ×5 ✓ | Prisma final o Destello lunar o Bastón del cristal arcoíris o Portal lunar o Fuego de nebulosa | 20× lingote de luminita/fragmento de nebulosa/fragmento de polvo estelar/lingote de espectro | 100 | 100 platino |

**Cómo se lee**: «Puerta ✓» = su jefe debe haber caído (crédito retroactivo:
bandera del mundo o registro del cazador; «×N» = caído N veces — LOS
CONTADORES SON ACUMULATIVOS: cada muerte cuenta para TODAS las puertas
venideras de una vez). LAS REPETICIONES suman +1 por cada vez que el jefe
se repite: el Señor de la Luna se pide en 50 (×1), 63 (×2), 71 (×3),
79 (×4), 87 (×5), 95 (×6) y 99 (×7) — y la CIMA cobra el total: ×10
 («al final tienes que matar 10 moon lord»); la Emperatriz ×2..×5. El
nivel 20 añade EL LIBRO CORRUPTO (el Eterno lo entrega al llegar al 19;
también cae: 1% matando cualquier criatura, 1% abriendo cualquier cofre,
2% en la mazmorra — criaturas y cofres —, 50% la Muralla). El nivel 100
exige la cima compuesta (Aethon ×1 + Moon Lord ×10 + Emperatriz ×5) y
desbloquea LA IRA DE UN SER EXTERIOR (El Haz). Las monedas escalan de
100 de cobre (nivel 1) a 100 de platino (nivel 100), y las esencias piden
N por nivel (1→1 … 100→100). El material sale de las recetas de las armas
de la era (el DOBLE de la receta, redondeado a decenas, mínimo 20) o del
lingote de su era según la progresión de la wiki. **El arma O el material
pagan el peldaño — SOLO UNO de los dos** (conságralo en el altar y
suéltalo DELANTE del grimorio seleccionado: él devora solo lo que le
falta).

# EFECTOS-Y-LIBRERIAS.md — EL CÓDICE VISUAL Y SONORO DE LA CASA (v1.6)

> **ESTE ES EL ARCHIVO DE CONSULTA OBLIGATORIA PARA CUALQUIER EFECTO.**
> Antes de crear UN SOLO efecto — visual, de sonido, de pantalla, de partícula,
> de shader, de textura, de luz — **ESTE ARCHIVO SE LEE PRIMERO, COMPLETO EN LA
> SECCIÓN QUE TOQUE, Y SE VERIFICA SI LA CASA YA TIENE ALGO QUE SIRVA.**
> El mod tiene **69 librerías VFX (42.584 líneas), 4 shaders compilados, 94
> texturas de efectos y un motor de sonido con 3 capas**. La probabilidad de
> que lo que necesitas YA EXISTE es altísima. **NO SE INVENTA NADA NUEVO SIN
> ANTES HABER REVISADO ESTE CATÁLOGO.**

**Versión del códice:** 1.6 (v6.57.0) · **Mantenimiento:** todo agente (IA o humano)
que cree o modifique una librería, shader, textura, efecto visual o de sonido
**DEBE documentarlo aquí en la misma entrega** (ver §10 — El Decálogo y §11 — Bitácora).

---

## ÍNDICE

0. [La Ley Primera — el protocolo antes de escribir una línea](#0-la-ley-primera)
1. [EL DESCUBRIMIENTO .fx/.fxc — la lección de la v6.55.1](#1-el-descubrimiento-fxfxc)
2. [El mapa en 60 segundos — ¿qué quiero hacer? → ¿qué uso?](#2-el-mapa-en-60-segundos)
3. [El núcleo — VFXCore, RayoStrip y las reglas transversales](#3-el-núcleo)
4. [Catálogo de librerías temáticas](#4-catálogo-de-librerías-temáticas)
5. [Los 4 shaders (.fx + .fxc)](#5-los-4-shaders)
6. [Las texturas de efectos](#6-las-texturas-de-efectos)
7. [El sonido — las 3 capas](#7-el-sonido)
8. [Sistemas de seguridad y diagnóstico de render](#8-sistemas-de-seguridad-y-diagnóstico)
9. [Los renderers — dibujados completos de armas](#9-los-renderers)
10. [El Decálogo del efecto nuevo + la regla de documentación](#10-el-decálogo-del-efecto-nuevo)
11. [Bitácora de documentación](#11-bitácora-de-documentación)

---

## 0. LA LEY PRIMERA

**Protocolo obligatorio antes de crear cualquier efecto:**

1. **LEER la sección 2 (mapa de decisión)** de este archivo. Si hay una fila
   que matchee lo que quieres hacer, **USA ESA LIBRERÍA. Punto.**
2. Si el mapa no matchea, **leer la sección 4** (catálogo completo) buscando
   sinónimos: "niebla"→BrumaFX, "sombra"→SombrasLib, "latigazo eléctrico"→
   StormLib/RayoLib, "aura"→AuraLib, "anillo rúnico"→SigiloLib/OrbitaLib…
3. Si de verdad no existe nada, **crear lo mínimo nuevo reutilizando el
   núcleo (§3)**: VFXCore para quads/lotes, RayoStrip para tiras, las
   texturas existentes (§6), AudioLib para sonido (§7). Y **documentarlo
   en este archivo (§10-§11) EN LA MISMA ENTREGA.**
4. **Casos prohibidos por historia documentada:**
   - **Óvalos/elipses planas para NUBAS/NIEBLA/HUMO** → la casa tiene
     **BrumaFX** (humo fBm procedural, v6.25). El caso ejemplar: la v6.54.7
     — la Ira del Cielo dibujaba "óvalos planos como nubes" y el usuario
     respondió *«¿para qué fabricar óvalos planos como nubes si tenemos las
     librerías de brumas?»*. Los óvalos se fueron; hoy esa arma usa
     BrumaFX.Cloud + MistBand + relámpagos que ILUMINAN las nubes.
   - **Círculos planos para DESTELLOS** → siempre degradado monotónico
     (SoftGlow/NovaBurst/DestelloFinal). Un círculo de color plano es un
     bug de la casa, no un efecto.
   - **Sprites de rayos** → cero sprites de rayo en la vida: RayoLib,
     StormLib, RayoStrip. (BeamLib fue enterrada en la v6.56.9 — §4.5.)
   - **`Main.rand` en render** → todo determinista por `Hash01`/semilla
     (multi-máquina sin red: el efecto se ve IGUAL en todos los clientes).
   - **Dust de vanilla DENTRO de ataques de haz/plasma** → **CERO Dust**
     (LEY v6.56.0). Los sprites redondos/chispas de Dust junto al filo son
     LA SIERRA de los picos (cada Dust duro asoma en la funda y el ojo lo
     lee como una púa) y los que derivan se leen como «círculos planos».
     El plasma texturizado (CarnePlasma + BitsChispa + GlowOrb + SparkleStar)
     **ES el efecto**. Caso ejemplar: el RC2 de la v6.56.0 cargaba polvos
     cromáticos en la carga/lanzamiento — el usuario vio «círculos planos»;
     la cura fue retirarlos COMPLETOS y vestir el foco con anillos de
     contención + micro-arcos StormLib.Bolt. Lo mismo en el Primer Trazo
     (RuidoTinta → CarnePlasma) y el Prisma (la espina verlet vestida de
     plasma, cero polvos).
   - **Quads CPU ENCIMA de un strip GPU (la «pila de capas»)** → **LEY
     v6.56.7**. Para «dar más efecto» a un haz de shader NUNCA se dibujan
     fajitas texturizadas arriba del strip: los quads tienen borde
     POLIGONAL duro (64 nodos = un 64-gono) y las texturas crudas (el
     remolino BINARIO sobre todo) dibujan peines de blobs fuera del borde
     — LOS PICOS de la v6.56.6, cazados por el sim 1:1 y confirmados por
     verificación visual (§5.4). TODO lo extra (desenfoque, glow,
     filamentos, aberración) se hace **PER-PIXEL DENTRO DEL .fx** — una
     curva smoothstep/pow no puede producir picos.
   - **Glow apilado como «boca» de un arma — TAMBIÉN PROHIBIDO (v6.56.8)** →
     la boca del RC2 fueron CUATRO SoftGlow apilados «con el alfa a cero»
     desde 607df36, y el jugador vio UN CÍRCULO PLANO CON ANILLOS
     CONCÉNTRICOS en cada captura (el ojo lee una pila de glows como un
     disco — verificado por visión independiente en la v6.56.8). LA BOCA
     DEL CONJURO SON SUS RUNAS Y SUS SOLES: la corona rúnica que encoge
     (el indicador de carga), los soles de 4 puntas del accesorio, los
     micro-arcos StormLib y la estrella conducto. CERO discos, apilados
     o no. (El bug de la v6.56.4-.56.6 era UN glow con alfa lleno que
     hacía LERP contra el fondo — también prohibido, pero la pila entera
     salió del mod.)
   - **LA PILA DE CAPAS HECHA BIEN — STRIPS, NO QUADS (LEY v6.56.9)** →
     la prohibición de la .56.7 era sobre los QUADS CPU (borde poligonal
     duro + texturas crudas = peines). La forma SANCIONADA de darle capas
     a un haz de shader: **DIBUJAR EL MISMO STRIP VARIAS VECES** (la MISMA
     polilínea, mismos arrays — solo cambia el factor de ancho por capa)
     con **EL SHADER VANILLA DE SPRITES (`Main.pixelShader`)** + una
     textura de PERFIL SUAVE (RampaGlow: 0 en el borde, pico al centro):
     bruma ×2.6-3.6 al 9-20% → glow ×1.75-2.5 con la paleta viajando →
     EL CUERPO del .fx → núcleo ×0.28-0.42 blanco-violeta. Cada borde se
     funde EN CURVA por píxel (cero picos por construcción — verificado
     sim 1:1) y el pipeline es el de TODO Terraria: **GPU-UNIVERSAL**, se
     ve en CUALQUIER máquina (integrada, vieja, sin soporte de .fx — la
     letra: «necesito que cualquier gpu incluso los que no son
     dedicadas deben ver el shader»). LA REGLA DE LAS DISTINTAS
     POLILÍNEAS: si las capas siguen polilíneas DIFERENTES (una curva y
     otra recta), sus bordes se CRUZAN y nacen picos nuevos — misma
     polilínea SIEMPRE. Caso ejemplar: las copias del RC2 (v6.56.9).
   - **LA LEY DEL CORAZÓN DORADO (v6.57)** → el corazón dorado del sol
     rúnico (`SolRunico4Puntas`) es la FIRMA del accesorio A SU ESCALA
     (soles de ~20 px: un Quad de glow radial dorado a ×1.5 del
     tamaño). A ESCALA DE HAZ (la estrella conducto de la boca ~57 px,
     los soles de margen ~60 px) ese mismo corazón se lee como UN
     CÍRCULO AMARILLO GIGANTE flotando en el conducto (la letra:
     *«son como circulos amarillos que aparecen a veces y son
     demasiado grandes, no deben aparecer»*). REGLA: los efectos
     reciclados de accesorio a haz SE PARAMETRIZAN — el corazón escala
     con `corazon:` y los haces lo pasan a 0 (queda la estrella blanca
     de 4 puntas pura; BrilloSolarHalo conserva SU corazón: valor por
     defecto 1). Lo que a escala de amuleto es una firma, a escala de
     cataclismo es un defecto.
   - **Negro puro en lote aditivo** → suma cero = invisible. Para OSCURECER
     hay que salir del aditivo: `FlushAlpha` / `PaseAlpha` / lote AlphaBlend.
     Toda sombra necesita rim/aliento aditivo o rampa de color para ser
     legible de noche (lección v6.50.65: "negro sobre negro").
5. **Los sonidos van en `AI()` gated `!= Server`** en proyectiles (suena en
   SP y en clientes), sin duplicar los del NPC (que corren server-side);
   los de combate pasan por **AudioLib.Sonar** (anti-spam incluido).
6. **LA LEY DEL CÓDICE SINCRONIZADO (v6.56.9 — la anti-recurrencia del
   DESYNC de la .56.8)**: la auditoría encontró este códice desincronizado
   del código en 6 puntos (librerías «con usuarios» que no tenían, la
   «paleta estrella» VoidQueen con 0 usos, la escalera del rayo documentada
   viva tras la purga…) — y como la Ley Primera OBLIGA a consultarlo antes
   de crear efectos, UN CÓDICE QUE MIENTE ES EL MAYOR GENERADOR DE
   DUPLICACIÓN FUTURA. LAS DOS REGLAS QUE QUEDAN: **(a) EL MISMO COMMIT** —
   toda librería/método que nazca, muera o se purgue actualiza SU entrada
   del catálogo (§2-§4) EN LA MISMA ENTREGA (el funeral y la partida de
   nacimiento van juntos); **(b) LA MUESTRA POR AUDITORÍA** — cada
   auditoría de librerías verifica una muestra de entradas del catálogo
   contra `rg` (¿la librería vive? ¿los usuarios listados existen?). Una
   entrada sin verificación reciente es una entrada bajo sospecha.

---

## 1. EL DESCUBRIMIENTO .fx/.fxc

> Documentado por la v6.55.1 (commit be21ef2). **Esta sección es LEY: si se
> ignora, el shader simplemente NO CARGA y nadie se entera hasta que alguien
> abre el archivo a mano.**

### 1.1 Qué descubrimos (la causa raíz)

La revisión forense de la v6.55.1 (decompilación del `tModLoader.dll` REAL
de la 2026.08) reveló:

- **tModLoader 2026.08 registra el lector de efectos (`FxcReader`) SOLO para
  la extensión `.fxc`** — el binario de efecto D3DX9 (magic `0xFEFF0901`).
- **Los `.fx` (fuente HLSL) NO se compilan solos.** Empaquetar un `.fx` sin
  su `.fxc` = el juego **jamás** carga ese shader. No hay error visible, no
  hay excepción: simplemente el asset no existe para el juego.
- A diferencia de tML antiguos (que compilaban `.fx` en runtime), la 2026.08
  espera el binario pre-compilado al perfil `fx_2_0`.

### 1.2 Qué pasó exactamente (la honestidad histórica)

**NO todos los efectos del mod "se estaban viendo mal".** El estado real:

| Shader | Llegó con | .fxc | Estado histórico |
|---|---|---|---|
| `SunShader` | v5.80 (shaders de referencia estudiados desde v6.02) | ✔ ya compilado | **Cargó bien TODA la vida** |
| `RadialShineShader` | v5.80 | ✔ ya compilado | **Cargó bien TODA LA VIDA** |
| `BlackHoleDistortionShader` | v5.76 (restaurado v5.80) | ✔ ya compilado | **Cargó bien TODA LA VIDA** |
| `HazConcentradoShader` | **v6.55.0 (nuevo de la casa)** | ✖ **olvidado** | **Vivió UNA versión por el fallback** |

- Los 3 shaders veteranos **siempre tuvieron su binario** (vinieron
  pre-compilados): el Sol del Dios, las auras estelares y la lente
  gravitacional de los agujeros negros funcionaron bien desde siempre.
- El `HazConcentradoShader` (el shader propio del **Rayo Concentrado II**)
  nació en la v6.55.0 empaquetado **solo con el `.fx`** → el juego nunca lo
  cargó. **El arma no se vio "rota"**: se vio su **FALLBACK de la casa**
  (las tiras RayoStrip ×3 capas: carne + remolino + núcleo — bonito y curado
  de picos, pero SIN la técnica estrella de las 4 texturas multiplicadas).
- La v6.55.1 compiló el binario y **desde entonces los 4 shaders cargan de
  verdad** — y el arma vive el shader GPU con el fallback como red de
  seguridad.

### 1.3 La cura — LA CADENA DE COMPILACIÓN NATIVA (v6.55.1)

Este sandbox no tiene Windows ni `fxc.exe`. La cadena armada (y que queda
instalada para siempre):

```
.fx (fuente HLSL)
  │  1. vkd3d-shader 2.1  (HLSL → SM3; extraído de los .deb de Ubuntu,
  │     corriendo con la glibc 2.43 de sid vía su loader)
  │        → compila VERTEX SHADER y PIXEL SHADER por separado (con CTAB)
  ▼
vs.bin + ps.bin (bytecode SM3)
  │  2. tools/fx20_build.py — EL ENSAMBLADOR DE LA CASA
  │     Parsea las declaraciones del .fx (params, samplers con estados,
  │     técnica/pase) y emite el binario de efecto fx_2_0 con el layout
  │     EXACTO que emite `fxc /T fx_2_0` — layout decodificado BYTE A BYTE
  │     contra los 3 .fxc veteranos de la casa (ground truths).
  ▼
MiShader.fxc
  │  3. tools/fx20_validate.py — EL VALIDADOR CONTRA EL JUEGO REAL
  │     Carga libFNA3D.so.0 (¡la MISMA nativa que usa tML!) y llama
  │     MOJOSHADER_compileEffect + effectBegin + BeginPass: 0 errores,
  │     params/técnica/samplers verificados, GLSL transpilado revisado.
  ▼
AethonMod/Content/Effects/Shaders/MiShader.fxc   ← AL LADO DEL .fx, SIEMPRE
```

**Comandos exactos (plantilla para un .fx nuevo o modificado):**

```bash
# 1) Compilar los dos entry points (vkd3d-shader; el loader de glibc sid ya
#    está instalado en el sandbox — ver CHANGES.md v6.55.1 si hay que reponerlo)
vkd3d-shader-compiler --profile ps_3_0 -E PS MiShader.fx -o ps.bin   # pixel
vkd3d-shader-compiler --profile vs_3_0 -E VS MiShader.fx -o vs.bin   # vertex (si hay)

# 2) Ensamblar el binario de efecto (layout fx_2_0 exacto)
python3 tools/fx20_build.py --fx MiShader.fx --vs vs.bin --ps ps.bin \
    --out AethonMod/Content/Effects/Shaders/MiShader.fxc

# 3) Validar contra el parser REAL del juego (libFNA3D + MojoShader)
python3 tools/fx20_validate.py AethonMod/Content/Effects/Shaders/MiShader.fxc
```

**Herramientas de la cadena** (todas en `tools/`):
- `fx20_build.py` — el ensamblador (usa `fx20_parse.py` + `fx20_ctab.py`).
  Convenciones críticas ya resueltas: token `0xFEFF0901`, centinela nulo en
  el offset 0 de la región base, records alineados a 4, estados de pase
  PIXELSHADER=147/VERTEXSHADER=146 (numeración MojoShader), estados de
  sampler TEXTURE=4/ADDRESSU/V=5,6/FILTROS=9,10,11 con valores D3DX
  WRAP=1/LINEAR=2, y los objetos de textura SIEMPRE con nombre de mapeo en
  la tabla pequeña (FNA lo usa para el `samplerMap`).
- `fx20_validate.py` — la prueba contra libFNA3D.so.0 real.
- `fx20_parse.py` / `fx20_ctab.py` — parser de .fx y decodificador de CTAB.

### 1.4 Las reglas que quedan (LEYES DEL SHADER)

1. **TODO `.fx` NUEVO se entrega CON su `.fxc` compilado** (los dos archivos,
   hermanos, en `Content/Effects/Shaders/`). Un `.fx` sin `.fxc` es código
   muerto que el juego jamás verá.
2. **TODO `.fx` MODIFICADO regenera su `.fxc`** antes de empaquetar
   (COMPILACION.md §"Notas de los shaders" ya lo advierte).
3. **Todo cargador C# sigue el patrón de la casa:** caché estática +
   bandera de fallo (`_shaderFallo`) + **fallback visual** — si el shader
   no carga (driver raro, asset perdido), el efecto CAE a su versión de
   librerías y **el juego jamás se rompe** (ver §5: cada shader documenta
   su fallback exacto).
4. **Validación obligatoria** con `fx20_validate.py` antes del commit.
5. Si un shader pide varias texturas: los samplers s1/s2/s3 se bindean por
   `GraphicsDevice.Textures[n]` + `SamplerStates[n]` ANTES del
   `CurrentTechnique.Passes` (patrón RuneSunRenderer — ver SunShader §5.2).
6. **Al terminar de usar un shader, `VFXCore.Janitor()`** (vía
   `VFXCoreSystem.PostDrawTiles`) anula `Textures[1..3]`/`Samplers[1..3]`
   pegados — ya está cableado, pero cualquier pase nuevo que use slots de
   textura múltiples debe saber que existe.

---

## 2. EL MAPA EN 60 SEGUNDOS

**La tabla de decisión rápida. Si tu caso está aquí, la conversación terminó: usa esa librería.**

| Quiero hacer… | USO ESTO (archivo en `Content/VFX/`) | NUNCA hagas |
|---|---|---|
| Nubes, niebla, humo, bruma, vapor | **BrumaFX** (`Effects/Bruma/BrumaFX.cs`) — Puff/Cloud/Column/MistBand/Tendril | Óvalos/elipses planas |
| Sombra que TAPA, ojos que miran, fauces, tentáculos de sombra | **SombrasLib** (`SombrasLib.cs`) — Masa/Ojo/Devorador/BrumaColumna/Garra | Quads negros sueltos sin rim |
| Rayo estilo vanilla 1.4.5 (clima, Arc Surge) | **RayoLib** (`RayoLib.cs`) + `RayoSistema.Lanzar` | Sprites de rayo, zigzag con Main.rand |
| Arcos eléctricos, chispas, coronas de descarga | **StormLib** (`StormLib.cs`) — Bolt/ChainBolt/Arc | Líneas blancas secas |
| Haz/láser masivo de energía (7 capas, telegraph) | **BeamLib** (`BeamLib.cs`) — Rayo/Telegrafo | Un Quad estirado sin capas |
| Rayo de vértices LISO (tiras de triángulos) | **RayoStrip** (`RayoStrip.cs`) — Filamento/FajaTexturada | Rectángulos encadenados (los "bultos") |
| Onda de choque / impacto / explosión | **OndaLib** (`OndaLib.cs`) — Shock/Pulse/Kick/Flash | Anillos de grosor constante |
| Estela que sigue al proyectil | **EstelaLib** (`EstelaLib.cs`) — Track+Ribbon | Copiar posiciones sin sanitizar |
| Fuego, llamas, brasas, temperatura | **PyraLib** (`PyraLib.cs`) — Tongue/Flame/EmberField | Naranjas planos con alfa |
| Luz/destellos/aura de color vivo | **LumenLib** (`LumenLib.cs`) — Bloom/Flare/Ray + paletas | Círculo plano sin degradado |
| Aura alrededor de NPC/jugador (incl. Forma Divina) | **AuraLib** (`AuraLib.cs`) — 9 patrones + perfiles fluidos | Reprogramar un aura a mano |
| Anillo rúnico SOLAR / corona de glifos | **SigiloLib** (`SigiloLib.cs`) — SelloSolar/ArcoGloria | Runas dibujadas a pulso |
| Anillo rúnico DEL VACÍO / corona de lazos | **OrbitaLib** (`OrbitaLib.cs`) — CoronaConjuro/CoronaArcos | — |
| Desgarro de realidad / portal / glitch | **RiftLib** (`RiftLib.cs` + parciales) — Tear/PortalAnillos/EcoGlitch | Línea blanca recta |
| Media luna de tajo anime (aparece tras el corte) | **TajoLib** (`TajoLib.cs`) — Tajo | Arcos de grosor fijo |
| Criatura segmentada (sierpe, anguila, ciempiés…) | **SierpesLib** (`SierpesLib.cs`) — 11 patrones de locomoción | Física dentro de la librería |
| Fantasma/eco/posimagen/espejo | **EspectroLib** (`EspectroLib.cs`) — Memoria/ColaHistoria/EcoOrbital | — |
| Nebulosa volumétrica que nada (curl noise) | **NebulaLib** (`NebulaLib.cs`) — Nube/Columna | Puffs estáticos |
| Cuerpo compuesto parametrizable (ojo alado…) | **MoldeLib** (`MoldeLib.cs`) — piezas declarativas | — |
| Alas 100% procedurales | **AlasAuroraLib** + `AlasEstilo` (catálogo 15+ anatomías) | Rectángulos recolorizados |
| Cinta con fase anclada al arco (serpentea sin resbalar) | **TelaLib** (`TelaLib.cs`) — Cinta | — |
| Distorsión gravitacional del FONDO | **GravLens** + `BlackHoleLensSystem` (shader §5.1) | Warp con render targets frágiles |
| Texto dramático en pantalla (la voz del libro) | **EcoLib** (`EcoLib.cs`) — Hablar | Cajas de diálogo propias |
| Sacudida de cámara / flash / viñeta / onda de UI | **PantallaLib** (`PantallaLib.cs`) o **OndaLib.Kick/Flash** | Escribir ModifyScreenPosition a mano |
| Sonido de combate (nace/golpea/carga/muere) | **AudioLib.Sonar** (§7) — 6 familias × 5 momentos | PlaySound sueltos sin anti-spam |
| Sonido propio sintetizado en bucle | El patrón del §7.3 (RayoConcentradoLoop.ogg) | .wav cortos con costuras |
| Arcoíris de aceite / color que viaja | **ColorPrisma** (`ColorPrisma.cs`) — Oil/ValueAt/ALoLargo | Lerp de 2 colores |
| Colisiones de formas (segmento/banda/cono/anillo/sector) | **FormaLib** (`FormaLib.cs`) | Hitbox rectangular para todo |
| Coreografía declarativa (ventanas de tiempo) | **CompasLib** (`CompasLib.cs`) — EnVentana/Cada | Timers manuales por todas partes |
| Fuego/humo difuso a media resolución (rendimiento) | **MediaResLib** (`MediaResLib.cs`) | Pases full-res de humo enorme |
| Soporte común de las armas definitivas | **DefinitivasLib** (`DefinitivasLib.cs`) — paleta/búsqueda/cono | — |
| Superficie estelar (un sol de verdad) | **RuneSunRenderer.DrawSunBody** + SunShader (§5.2) | Glow naranja redondo |
| Agujero negro | Los 10 renderers de §9 + shader de lente | — |
| Partículas clásicas gestionadas | `ParticleManager`/`ParticlePresets` (`Content/Particles/`) | Dust de vanilla para efectos épicos |

---

## 3. EL NÚCLEO

### 3.1 VFXCore.cs — `static class VFXCore` + `VFXCoreSystem` (1.466 líneas, v6.03)

**EL CORAZÓN DE TODA LA BIBLIOTECA VISUAL.** Todo efecto de brillo del mod
se describe como una **lista de cuadros de luz** (`GlowQuad`) y se vuelca de
una sola vez. Buffer estático reutilizable de 512 → **cero GC por frame**.
Lo usan **137 archivos** del mod.

**El cuadro:** `struct GlowQuad { Position (MUNDO), Color, Scale, Rotation, Texture, Source }`

**Primitivas públicas:**
- `Begin()` — limpia el buffer
- `Quad(pos, color, escala)` / `Quad(pos, color, Vector2 escala, rot)` — cuadro circular de luz (textura SoftGlow por defecto)
- `Quad(pos, color, escala, rot, Texture2D)` — con textura propia (Ring, GlowOrb…), rotación para cintas orientadas por tangente
- `QuadSrc(pos, color, escala, rot, tex, Rectangle source)` — muestreo POR BANDAS de una textura larga (v6.50.67: la clave de las cintas de carne que FLUYEN)
- `Line(a, b, color, grosor)` — el cuadro estirado A→B (EL primitivo de telegraphs/rayos simples, v6.41)
- `Ribbon(spine, widths, color, tex, uvFlow, edgeInset, edgeOutset)` — **LA CINTA** (v6.50.67): polilínea + perfil de anchos → quads tangentes SOLAPADOS en las juntas, muestreando bandas de la textura con `uvFlow` (la carne avanza raíz→punta); `edgeInset ≈ 0.42` = franja de borde, `edgeOutset ≈ 0.5` = halo que respira
- `RibbonTinted(...)` — la cinta con tinte POR SEGMENTO

**Texturas compartidas** (lazy, null-safe ante recargas calientes):
`SoftGlow` (la workhorse), `GlowOrb` (núcleo sólido + borde suave — vacíos negros), `Ring` (anillo fino; `RingQuadSize(radio)` = radio×2.174), `Pixel` (el MagicPixel 1×1 — EL pincel del 100% código), `NovaBurst` (destello monotónico 256²), `DestelloFinal` (cruz de 8 rayos), `Arcoiris` (512² horneada), y las horneadas v6.50.67: `Carne` (cuerpo orgánico del tentáculo, periódica en V), `Colmillo`, `Ventosa`, `OjoRasgado` (esclerótica + iris carmesí + rendija con alpha 0 — en aditivo añade cero = negra sin salir del lote).

**Los volcados (el sistema de lotes):**
- `FlushAdditive(tex, endActiveBatch)` — vuelco ADITIVO al mundo (neón). `SpriteSortMode.Deferred` (v6.50.66: 1 Map del vertex buffer por volcado — curó el crash OutOfMemory del Map-por-quad) + `Main.GameViewMatrix`; try/finally con limpieza SIEMPRE; **resta `Main.screenPosition` UNA vez aquí** (por eso las quads se emiten en MUNDO). La escala = px finales en pantalla.
- `FlushAlpha(tex, endActiveBatch)` — la hermana con `BlendState.AlphaBlend` (premultiplicado) — **la capa que OSCURECE** (negro en aditivo suma 0: para tapar hay que salir del aditivo).
- `AppendToPlayerDraw(ref PlayerDrawSet, tex, effects)` — emite `DrawData` al caché de capas de jugador (el camino 100% seguro de tML, sin tocar el SpriteBatch).

**EL CONTRATO DEL LOTE DEL LLAMADOR (crítico — crash v6.54.5):**
- `LoteAbierto` / `CerrarLoteSiAbierto()` — la **sonda de reflexión** que lee el `beginCalled` del SpriteBatch de FNA: nunca `Begin` sobre `Begin` (mató 27 first-chance del client.log en v6.50.11).
- `ReabrirLoteVanilla()` — **EL CONTRATO DE CURACIÓN**: reabre con los parámetros EXACTOS del pase de entidades de vanilla (`Deferred·AlphaBlend·DefaultSamplerState·None·Main.Rasterizer·null·Main.Transform`). **Todo PreDraw de la casa sale con el lote ABIERTO** (si llegó roto, se CURA). Idempotente por sonda. 121 archivos lo cumplen (183 llamadas).
- `RestaurarLoteVanilla()` — **v6.56.1, LA CURA DEL LOTE ADITIVO**: `RayoStrip.CerrarLote()` reabre en ADITIVO (para los gorros); si un PreDraw SALÍA así, el resto de proyectiles del pase se dibujaba LAVADO. Este termina el lote abierto (sonda) y reabre con los parámetros del pase de entidades — los sprites en cola vuelcan con SU blend y el pase continúa limpio. **LA SALIDA de todo PreDraw que usó RayoStrip/StormLib** (la ley del lote ahora es: abierto Y en estado vanilla).

**Utilidades:** `Breathe(t, speed, phase, amplitude)` (0.94..1.06), `Sway(t)`, **`Hash01(seed, a, b)`** → [0,1) determinista multi-máquina (h = seed·374761393 + a·668265263 + b·1911520717, mezcla xor-shift — **cero `Main.rand` en render, jamás**), `Ellipse(center, a, b, tilt, t)`, `EsObjetivo(NPC)` (el predicado de la puerta de daño de vanilla: `active && !dontTakeDamage && !friendly` — úsalo para TODA la detección de presas).

**Capas de oclusión:** `enum VFXLayer { DetrasDeTodo, DetrasDeNPCs, SobreNPCs }` + `RegisterLayer(Action<SpriteBatch>, VFXLayer)` (techo 64) + `FlushOcclusion()` — cuando un efecto debe ir DETRÁS de las entidades.

**Presupuesto y calidad:** techo **24.000 quads/frame** × `FactorCalidad` (`Presupuesto(n)` pregunta antes de emitir; `ReportarFps` lo alimenta — <45 FPS adelgaza, >55 recupera). `QuadsDelFrame` para el F8.

**La regla de color premultiplicado de la casa:** el tinte es SIEMPRE `Color·f` (RGB y A escalados a la vez); las texturas horneadas nacen con RGB ya multiplicado por la máscara (convención `(q,q,q,q)`); en aditivo el aporte = máscara·color·f LINEAL; en alfa el humo TAPA.

### 3.2 RayoStrip.cs — `static class RayoStrip`

**Las tiras de vértices de verdad**: `VertexPositionColor` + `BasicEffect` cacheado, pozo de 16.384 vértices — líneas LISAS sin juntas ni "bultos" (la cura del diagnóstico v6.50.25). El rgb se PRE-MULTIPLICA en el vértice; blend de pantalla soft-add propio (`InverseDestinationColor`/`One` → dos rayos cruzándose NUNCA clipean).
- `AbrirLote()` / `CerrarLote()` — el baile End→primitivas→Begin, **ANIDABLE** (`Profundidad` pública)
- `FajaTexturada(pts, semiancho, color, tex, …)` / `...EnLote(...)` — tira con textura y semiancho por nodo
- `Filamento(pts, width, halo, nucleo, …)` / `...EnLote(...)` — el rayo: funda de 5 columnas + vena de 3 (núcleo 0.85)
- Coordenadas de MUNDO (resta screenPosition interno). **11 usuarios** (AethonBoss, las 6 Definitivas, PrimerTrazoFajo, BoltRenderer, StormLib, NebulaLightning).

### 3.3 Las TRES escuelas de lote (memorizar antes de usar cualquier librería)

| Escuela | Librerías | Qué exige el llamador |
|---|---|---|
| **A — Emite al buffer de VFXCore** (coords MUNDO) | BeamLib, SombrasLib, SigiloLib, CodigosLib, EspectroLib, MoldeLib, NebulaLib, GravLens, DefinitivasLib | Llamar a los pinceles; el vuelco (`FlushAdditive/FlushAlpha`) vive en el PreDraw del llamador; salir con `ReabrirLoteVanilla()` |
| **B — Recibe el SpriteBatch ABIERTO** | StormLib, LumenLib, OndaLib, PyraLib, EstelaLib, TelaLib, BrumaFX (con sus `BeginMass/BeginGlow` propios), AudioLib | El llamador gestiona el lote (o usa los helpers AbrirAdditive/AbrirAlpha de la librería si los tiene) |
| **C — Exige el SpriteBatch CERRADO** (lo gestiona la librería) | RiftLib (parciales) y TODOS los renderers de §9 | El PreDraw hace `CerrarLoteSiAbierto()` antes y `ReabrirLoteVanilla()` al final |

**Coordenadas:** TODO el mod trabaja en **MUNDO**, con DOS excepciones que trabajan en **PANTALLA** (restar `Main.screenPosition` antes de llamar): **RiftLib** (parciales) y **BrumaFX** (sus lotes `BeginMass/BeginGlow` usan GameViewMatrix y esperan pantalla). SombrasLib toma MUNDO y convierte internamente.

### 3.4 El resto del núcleo

- **VFXPalettes.cs** — paletas nombradas (`VoidQueen` con `DiskDoppler(t)`/`OuterGlow(t)`, `CrimsonCourt`…). Mata los números mágicos de color.
- **ColorPrisma.cs** — el motor de arcoíris de aceite: `Oil` (magenta→naranja→amarillo→verde→cian), `ValueAt(reloj)`, `ALoLargo(reloj, progress)` (la rebanada móvil que recorre un haz).
- **GravLens.cs** — la distorsión unificada: `Registrar(centro, radio, fuerza, vida)` y CUALQUIER efecto curva el fondo (la fusiona `BlackHoleLensSystem` en el pase del shader §5.1).
- **MediaResLib.cs** — pases a ½ resolución para fuego/humo difusos (fill-rate ÷4): `Empezar(pixelado)` → quads → `Terminar()`, try/finally obligatorio.

---

## 4. CATÁLOGO DE LIBRERÍAS TEMÁTICAS

> Todas en `AethonMod/Content/VFX/` salvo indicación. Formato: **qué dibuja · API · contrato · usuarios**.

### 4.1 SombrasLib.cs (1.127 líneas, v6.50.62) — EL LENGUAJE DE LAS SOMBRAS

**Qué dibuja:** la estética de las sombras devoradoras (la investigación visual de la casa) — masa NEGRA que tapa, OJOS blancos que se abren y MIRAN, dientes, charcos, bruma que se come la luz. Paleta propia: `Negro(8,4,7)`, `Blanco(250,248,244)`, `Rojo(198,18,24)`, `RojoGarganta(120,8,12)`, `Violeta(118,74,190)`, `HumoNegro(18,10,28)`, `HumoVioleta(64,42,96)`.

**API (todas coords MUNDO; gestionan sus lotes vía VFXCore.Flush* — escuela A):**
- `Columna(raiz, cabeza, tiempo, semilla)` / `ColumnaViva(...)` → `Vector2[]` — la ESPINA del tentáculo (IK determinista hacia la presa)
- `Masa(col, grosorRaiz, grosorPunta, alfa, semilla, tiempo)` — el cuerpo de sombra (lote alfa) con vena de Carne
- `Charco(pos, radio, alfa, tiempo, semilla)` — el charco del suelo
- `Ojo(pos, tamaño, mirada, abierto, pupila, rasgada)` — el ojo que MIRA
- `OjosDeMasa(col, presa, presencia, semilla, tiempo, cantidad)` — ojos naciendo a lo largo del tentáculo
- `Bruma(pos, radio, alfa, tiempo, semilla, deriva)` · `BrumaColumna(col, grosor, alfa, tiempo, semilla, cantidad)` · `BrumaBoca(garganta, dir, apertura, alfa, tiempo, semilla, puffs)` · `CabezaDeBruma(...)` — la bruma exhalada (desde v6.50.65 usan BrumaFX por debajo: puffs fBm con rampa violácea + aliento aditivo — legibles de noche)
- `Devorador(punta, jefe, intensidad, alfa, tiempo, semilla, col, trago)` — LA FAUCE devorando al jefe (elipse del hitbox+24%, dientes, el trago inhalando)
- `PurgaVisualMuerte(centro, radio)` · `Vignette(alfa)` — el cierre de ojos de pantalla / el túnel de sombra
- `Garra(basePos, hacia, largo, alfa, gancho)` — la garra de 3 dedos con colmillos
- `OndaChoque(pos, radio, alfa, roja)` · `Alma(pos, tamaño, alfa)` — impacto / alma que escapa
- Helpers: `Alfa`, `Frac`, `semille`, `DeGolpe(t)`

**Usuarios (22):** los 9 proyectiles de Sombras (SombraPagina 1-4, GrietaDeTinta, FauceDevorador, PrimeraCena, BocadoDelSuelo, CaidaDevorada), SombraDeLaPagina2, GrimorioDormido, PartidorDeMundos + IraDelCielo (Definitivas), los cosméticos de sombra, RayoConvergente, TruenoPerlin…

### 4.2 BrumaFX.cs (`Content/Effects/Bruma/`, 744 líneas, v6.25) — EL HUMO DE VERDAD

**Qué dibuja:** humo/niebla/nubes PROCEDURALES — puffs con textura fBm horneada en runtime, invariante de escala (sub-blobs ∝ perímetro, alfa de cobertura constante), flipbook de ruido evolucionado, luz del mundo con piso, viento del mundo REAL (`Main.windSpeedCurrent`), rampa de enfriamiento, LOD propio de 800 quads. **Esta es la librería de las NUBAS — jamás óvalos (§0).**

**API (escuela B: `BeginMass()` = lote AlphaBlend que OCLUYE / `BeginGlow()` = lote Additive LUMINOSO; el llamador cierra con `End()`. Coordenas de PANTALLA — la excepción #2):**
- `Puff(center, radius, color, seed, …)` — la unidad de humo ambiente
- `AnimatedPuff(...)` — con vida (flipbook + enfriamiento + brasa que emite luz)
- `Vapor(center, radius, color, seed, …)` — humo alfa de 1 capa
- `Cloud(center, radius, color, seed, …)` — RACIMO de puffs (nube)
- `Tendril(path, width, color, seed, …)` — voluta serpenteando por una RUTA
- `Wisps(path, width, color, moteColor, …)` — voluta + motas de brasa
- `Column(base, height, width, color, …)` — columna ascendente
- `MistBand(area, color, seed, time, …)` — banda de niebla con paralaje
- `WorldTint(screenPos, color)` — tinta con la luz local del mundo

**Hermanas:** `BrumaNoise` (el motor matemático: `Hash/Value/Fbm/WarpedFbm/Curl/Smoothstep/Erode`), `BrumaBrushes` (la panadería de texturas horneadas premultiplicadas — el fix del bug de los rectángulos), `BrumaSystem` (ciclo de vida + purga de pools).

**Usuarios (16):** BrumaBlackHole, CicloEstelar, DeadStar, MareaGravitatoria, RelojArena, LlegadaBruma, AmbienteOleada, las Definitivas (Testamento, Partidor, Requiem, Ira del Cielo — §0 el caso ejemplar), TormentaNebular, Sinfonia, RencorPrimordial, AbrazoNebulosa, EminenciaAtroz, LanzaAlba, RayoConvergente.

### 4.3 StormLib.cs (1.624 líneas, v6.21+) — LOS ARCOS ELÉCTRICOS

**Qué dibuja:** rayos y arcos por PILA DE PASADAS del pixel del motor (6 anchos `PilaW {5.2…0.72}` × alfas `PilaF` — la receta del LightningArc 466 de vanilla: el degradado transversal ES la suma de pasadas sólidas). Determinista por (semilla, flick). Escuela B.
- `FlickTick(time, hz)` (el reloj de regeneración) · `IsLit(seed, flick, aliveChance)` (parpadeo vivo) · `DeathGrow(lifeT)` · `ZigPath(a, b, …)` (el zigzag) · `Boil(pts, seed, tick, amp)` (el hervor) · `ForkTree/BuildTree` (árbol de ramas) · `Ruido1D/Fbm1D` (el ruido CPU compartido) · `Strand(batch, pts, …)` · `ChainBolt(a, b, …)` (cadena entre 2 puntos) · `Bolt(a, b, …)` (EL rayo con doble tira cuerpo+núcleo, ramas y gorros) · `Arc` (corona de descarga)
**Usuarios (31):** AethonBoss, AtaqueJefe/Fusion, los *Ascendido*, SembradorPulsar, TelarEstrella, LanzaAlba, Apuestas, CodigosVivos, PrimerTrazoFajo…

### 4.4 RayoLib.cs (974 líneas, v6.50.18) — LOS RAYOS DE VANILLA 1.4.5

**Qué dibuja:** recreación 1:1 del `LightningGenerator`+`StormLightningDrawer` de Terraria 1.4.5 (el del clima y del arma Arc Surge): generador raymarch direccional por pasos de 8 px, 4-5 capas de ángulo apiladas, TIMÓN hacia el blanco, des-randomización al 80% (el rayo SE ENDEREZA antes de impactar), HORQUILLAS del quiebre reflejado, **LA OLA** (el frente energizado que recorre el canal — la firma de vanilla) y **TAPER** (50% hacia el impacto).
- Presets `RayoParams.Tormenta/.Arma/.Arco(origen, destino)` · `AnimRayo.Clima/.Arco/.VenaTrueno` · `Generar(p, bolts, semilla)` → `RayoBolt` · `Dibujar(batch, bolt, …)` · **`RayoSistema.Lanzar(p, semilla, blanco, dir)`** — la puerta de alto nivel
**Usuarios:** RayoPrimordial, VenaTrueno, ArcoSobretension + Testamento/IraDelCielo/PrismaPrimordial (vía RayoSistema).

### 4.5 BeamLib.cs — ⚰️ ENTERRADA (v6.56.9, auditoría §3)

El rayo masivo de 7 capas (v6.52.0) murió con 0 usuarios: sus consumidores
documentados (PrimerTrazoFajo, RayoConvergente) ya no existen — la búsqueda
negativa de la auditoría lo certificó y esta entrada mintió hasta la v1.4
reconciliada. **Borrada del mod (376 líneas).** Si algún día se necesita un
haz masivo por raycast: el camino GPU del RC2 (§5.4) o RayoLib/RayoStrip.

### 4.6 BoltRenderer.cs — ⚰️ ENTERRADO (v6.56.9, auditoría §3)

El relámpago determinista de 1ª gen (v6.50.25, montado sobre RayoStrip):
todas sus menciones eran comentarios históricos — el propio StormLib lo
sustituyó. **Borrado del mod (120 líneas).** El chisporroteo eléctrico
vivo es StormLib.Bolt (§4.3).

### 4.7 EstelaLib.cs (v6.25) — LAS ESTELAS

Ribbons de grosor variable siguiendo el camino real: pipeline `Sanitize` (corta teleports >1000 px) → `Smooth` (0.25/0.5/0.25 ×2) → `Resample` (~14 px); 3 capas por tramo (velo · cuerpo · núcleo), LOD, borde roto por hash.
- `Track(ownerId, capacity)` (ring-buffer) · `PurgeTracks/ClearTracks` · `Ribbon(batch, pts, width, EstelaProfile, color, intensity, seed, time, head, taper)` · `GhostTransforms(...)` (fantasmas). Escuela B; pts[último] = CABEZA. **Usuarios (12):** EminenciaAtroz, MareaGravitatoria, LagrimaSolar, Sinfonia, VerboPrimordial, PenduloJuicio, CicloEstelar, Enjambre, Ocaso, GrietaDeTinta…

### 4.8 TelaLib.cs — LA CINTA QUE NO RESBALA

Cintas con (1) fase ANCLADA A ARCO MUNDIAL (la textura avanza con los píxeles reales, no resbala al girar), (2) anchura por vértice con meseta 80% + tapas redondas, (3) solape adaptativo por giro real. Pool cero-GC, determinismo MP. `Adquirir(dueñoId)/De(id)/Soltar/Purgar`. **Usuario:** VerboPrimordial.

### 4.9 LumenLib.cs (v6.03+) — LA LUZ VIVA

El motor de color + los pinceles de luz: paletas `PrismRose/PrismDay/SolarGold/VoidCold/EclipseFire`; `Hue/Drift/Cycle`; `Bloom/BloomPulse/BloomTriple` · `DoublePass` (la doble pasada del Last Prism) · `Flare` · `Ray` (línea de luz) · `Lance/LanceTrail`. Escuela B. **Usuarios (30):** jefes, AtaqueOleada/Jefe/Fusion, LluviaMeteoros, SembradorPulsar, ColapsoMagnetar, TelarEstrella, NovaEncadenada, mini-estrellas, cosméticos.

### 4.10 PyraLib.cs — EL FUEGO COMO TEMPERATURA

Rampas 1D de 37 niveles: `SolarFire` (negro→granate→naranja→oro→blanco), `ColdFire`, `VoidFire`. `Sample(ramp, temp)` · `Tongue` (lengua de llama) · `Flame` · `Estallido` · `EmberField` (campo de brasas con autómata de difusión) · `SeedCell/ClearFields` · `Sparks` · `Light(worldPos, radius, ramp)`. Escuela B. **Usuarios (13):** RedSupergiant, LanzaAlba, CodigosVivos, LagrimaSolar, FragmentoSupernova…

### 4.11 OndaLib.cs (v6.25) + OndaSystem — LAS ONDAS DE IMPACTO

**Contrato de la casa: UNA ONDA SON DOS ANILLOS** (frente fino-brillante + retaguardia gruesa-tenue), BORDE ROTO (12-16 segmentos, radio vivo ±8%), grosor que engorda mientras el alfa muere. `Shock(batch, center, progress)` · `Pulse` · `Telegrafo/TelegrafoAnillo` · `Ground` · **`Kick(fuerzaPx, ticks, dir)`** y **`Flash(color, fuerza, ticks)`** (los efectos de pantalla — los aplica `OndaSystem`: sacudida en `ModifyScreenPosition`, destello GRADIENTE LOCAL centrado en el arma). **El arma de impacto más usada del mod (38 usuarios).**

### 4.12 PulsoLib.cs (v6.31) + PulsoSistema — EL SISTEMA NERVIOSO

El JUICIO DIFERIDO (`AbrirVeredicto(dueño, ticks)` → `Marcar(objetivo, daño)` → `Liquidar(dueño)` — el clang único escalado), el RETROCESO, la CÁMARA (`GolpearCamara`, trauma acumulativo), el SONIDO POR MATERIAL (`Sonar(Material, pos, combo)`), la PANTALLA (`EmpujarPantalla(tinte, fuerza, ticks)`). **Usuarios (7):** Apuestas, SierpesNuevas, VerboPrimordial.

### 4.13 PantallaLib.cs — LOS 4 GESTOS DE PANTALLA

`Sacudir(intensidad, duración, id)` (con ICameraModifier REAL del motor) · `Flash(color)` (la cruz de 8 rayos estallando EN EL MUNDO) · `Vineta(intensidad)` (el túnel de 10 plumones) · `OndaExpansiva(centro, radioMax)` (anillo por la capa de interfaz) + presets `PresetImpacto/PresetGolpeSeco`. Contrato de tiempo derivado (solo frameInicio, cero timers). **Usuarios:** CoreoGuion, Supernova, RayoPrimordial, VenaTrueno, PrimerTrazoHeld, ParticlePresets, Ocaso.

### 4.14 RiftLib.cs (+`.Portales`+`.Glitch`+`.DesgarrosNuevos`, 1.244+ líneas) — LOS DESGARROS

Núcleo: fases `RiftFase {Telegrafo, Apertura, Sostenido, Vibracion, Fractura, Cierre}`, paletas `Vacio/Carmesi/Entropia`; `Tear/Grieta/CaminoDesgarro`, `Star`, `TearImpacto`, `Rasgado`. Parciales: `PortalAnillos/OjoEspacial/HeridaElectrica` (.Portales), `EcoGlitch` (aberración de canal SIN render targets — el llamador re-dibuja 3 veces desplazado) + `DesgarroGlitch` (.Glitch), `VorticeColapso/GargantaVacio/UmbralRoto/LeviatanEspectral` (.DesgarrosNuevos, v6.35). **ESCUELA C + coordenadas de PANTALLA (la excepción #1).** **Usuarios (22).**

### 4.15 OrbitaLib.cs (1.139 líneas, v6.34/37) — LA ESCRITURA DEL VACÍO

Anillos rúnicos de agujeros negros + lado energético. Alfabetos `RunasVacio` (Supremo) y `RunasAbismo` (Umbral), humores sereno/nervioso. Helpers `AbrirAdditive/AbrirAlpha/CerrarBatch`; `AnilloEnergia` (las 20 bandas `sin(θ·20+t·5)` — la misma firma del shader), `EcosAnillo`, `Fotones`, `CoronaArcos` (los 5 lazos de neón), el círculo de conjuro completo, `CoronaConjuro` (la triple corona), `SelloVacio` (variante EROSIONADA). Colores `HotWhite/RingVivo/RingProfundo`. **Usuarios (10).**

### 4.16 SigiloLib.cs (v6.34) — LA ESCRITURA DEL SOL

Anillos orbitales, glifos cabalgando la tangente, perlas. Alfabetos `RunasSolares` (8: astro, llama, rueda, espiga, puerta, corona, cometa, sigilo maestro) y `RunasCorona` (8: lanza, cáliz, puerta, estrella, rayo, arco, espiral, trono). `Runa(pos, rot, alfabeto, i)` · `AnilloRunico` · `SistemaAnillos` · `ArcoGloria` (la corona de 8 glifos del portador) · **`SelloSolar(center, radius, time, seed)`** — EL círculo mágico completo. Escuela A (mundo). **Usuarios (8-9).**

### 4.17 TajoLib.cs (v6.40) — LA MEDIA LUNA DEL ANIME

Los tajos que aparecen DESPUÉS del corte, crecen de punta a punta y se disuelven: creciente con perfil lente `sin(πu)^0.55`, bandas UNIFORMES (sin cuentas), capas HALO (parpadea) + NÚCLEO (nunca) + ECO, revelado DIRECCIONAL. `Tajo(centro, radio, ang0, ang1, …)` + `ArcoToca` (colisión). **Usuarios (4):** AtaqueJefe, VerboPrimordial, TajoAstral, Apuestas3.

### 4.18 SierpesLib.cs (1.352 líneas, v6.38) — LAS CRIATURAS SEGMENTADAS

ONCE patrones de locomoción: `OuroborosAstral` (espiral logarítmica), `CaravanaEspectral` (camino-memoria), `AnguilaSolar` (onda viajera), `CienpiesRunico` (marcha metacronal Δφ=45°), `FlageloEstelar` (Verlet por masa), `ViboraGenesiaca` (doble hélice), `BoaEclipse` (constrictor), `FarolGuardian` (IK FABRIK — la punta manda), `CintaAurora`, `ManadaAstral` (boids), `CriaEstelar`. Todas firman `(segs, …, time, seed)` — la FÍSICA vive en el proyectil, aquí la CARNE.

### 4.19 CodigosLib.cs (v6.36) — LOS CÓDIGOS VIVOS

`DanzaOrbes` (sistema de orbes encadenados) · `OjoAbismo` (agujero con 20 bandas + estrellas dobladas + lente GravLens) · `SolVivo` (sol de 6 materiales) · `SierpeEstelar` (16 segmentos con jerarquía Cabeza/Aletas/Espina). Escuela A (mundo). **Usuarios:** CodigosVivos, VerboPrimordial.

### 4.20 EspectroLib.cs (v6.41) — LOS FANTASMAS

`struct Memoria` + `Crear/Registrar/Pasado/PasadoAng/Profundidad` (la historia del cuerpo) · `ColaHistoria` (la fila de fantasmas decrecientes) · `EcoOrbital` · `CurvaAproximacion`. **Usuarios (5):** FragmentoSupernova, Apuestas, VerboPrimordial.

### 4.21 NebulaLib.cs (v6.34) — LAS NEBULOSAS VOLUMÉTRICAS

Velos (10-24, alfa 0.05-0.14) que NADAN por CURL NOISE alrededor de los ojos del campo (ValueNoise2D+Curl propios). `Nube(centro, radio, time, seed)` · `Columna(...)`. Determinismo por (time, seed); **time en SEGUNDOS**. **Usuario:** VerboPrimordial.

### 4.22 MoldeLib.cs (v6.43) — CUERPOS COMPUESTOS DECLARATIVOS

Lista de `Pieza` (offset/escala/rotación/color/blend/Z) ancladas a un cuerpo vivo. **El contrato de los dos pases:** piezas normales → buffer VFXCore; piezas `PaseAlpha` (las que OSCURECEN) → cola interna que el llamador vuelca AL FINAL con `Molde.FlushPaseAlpha`. Factory `Molde.OjoAlado`. **Usuario:** FragmentoSupernova.

### 4.23 AuraLib.cs (4.347 líneas — EL MÁS GRANDE) — LAS AURAS

9 patrones (`Perlin, Poligono, Anillos, Bruma, Divino, Divino2, Divino3, Divino4, HaloArcoiris`) — hasta LA FORMA DIVINA (halo dorado, alas de luz, círculo rúnico, rayos cayendo) y el HALO ARCOÍRIS. Perfiles factory `FormaAscendida()…()`, `HambreDelGrimorio(intensidad)`, `BrasaDelEclipse()`…; `AuraPerfil` es clase fluida (`.ConRadio().ConPatron().ConHumo()…`); ciclo `Actualizar(npc/player, perfil)` → `DibujarNPC` / `DibujarJugador(ref PlayerDrawSet)` (camino DrawData) / `DibujarJugadorAditivo`. **Usuarios (8).**

### 4.24 AlasAuroraLib.cs + AlasEstilo.cs (v6.52→v6.55) — LAS ALAS

Cero sprites: abanico de plumas que se PLEGA/ABRE, la onda de aleteo SE PROPAGA pluma a pluma (`sin(flap − i·0.45)`), el color viaja por vértice dispersándose, ruido fluyendo en 2 capas, la punta se RASGA por hash. **v6.55 LA REFORMA VECTORIAL:** cada pluma = TIRA DE 9 NODOS con eje curvado y perfil raíz→vientre→punta afilada, ASIMETRÍA DE CUCHILLA (filo convexo, lomo cóncavo), vértices COMPARTIDOS (lección RayoStrip). `AlasCatalogo` declara las anatomías (`De(byte indice)`, `Todos`). `Dibujar(player, estilo, spread, flap, mezclaDia)`. **Contrato C:** cierra el lote (sonda), dibuja con SUS BasicEffect y LO REABRE.

**v6.56.0 — LA CURA DEL CENTRADO + LAS RÚNICAS + LAS ANATOMÍAS:** (1) EL CENTRADO: el ancla del enjambre es ahora SIMÉTRICA al sprite del jugador (el barrido asimétrico que las mandaba a un lado, FUERA) y las alas siguen la rotación del cuerpo con **`pl.fullRotation`** (camelCase — `FullRotation` NO compila en tML 2026.08, es campo de `Player`, no propiedad). (2) **EL OVERLAY RÚNICO `DibujarRunas`** (escuela A): una runa en cada PUNTA de pluma + el anillo de 8 glifos contrarrotante (SigiloLib). (3) **EL CATÁLOGO 17-23**: las 4 RÚNICAS (Solares · del Vacío contrarrotante · del Eclipse · del Juicio con el gradiente caminando) y las 3 ANATOMÍAS (Agujas del Vacío · Cuchillas del Eclipse · Corolas del Alba). Bolsa de las Alas: 15→22. Los ítems viven en `Content/Items/Alas/AlasNuevasV6560.cs`.

**v6.56.4 — EL ESPEJO COMPLETO + EL BARRIDO** (la letra: «las alas no son simétricas y cuando el personaje se mueve en una dirección, las alas no cambian la dirección junto al personaje» — TODAS las alas): (1) **EL FILO DEL LADO**: el ala izquierda ponía su borde GRUESO donde el espejo pondría el fino de la derecha — `perp(Mirror(T)) = −Mirror(perp(T))` — la PERMUTA por lado en `ConstruirPluma` hace el par espejo-simétrico DE VERDAD (la cura v6.56.3 solo simetrizó el EJE; la TIRA seguía torcida: 3.5-14 px por pluma). (2) **EL ESPEJO COMPLETO**: el bloque `direction<0` ahora PERMUTA también los semianchos — antes solo negaba X y el filo se INVERTÍA al mirar a la izquierda (Δ ≈ 2·SemiMas = 50-68 px). (3) **EL BARRIDO DEL HEREDERO** (revive el campo `Barrido` muerto de la v6.52.2): todo el enjambre se INCLINA hacia atrás respecto al MOVIMIENTO, calculado en el MARCO DE LA DERECHA (`velFrame = velX·direction` — si el término de dirección entrase en la fórmula se doble-voltearía con el espejo y girar no cambiaría NADA; el sim lo cazó): 0.4 de reposo + 0.6·velocidad saturada a 6 px/t. (4) EL CENTELLEO SIMÉTRICO (semilla sin término por lado) y EL GIRO DE RUNAS ESPEJADO por ala. **Validado 1:1 en simulación numérica (`/home/z/sim/sim_alas.py` — la réplica exacta del pipeline): par Δ=0.000px eje y bordes; girar == espejo exacto del render; corriendo, la punta se barre 11-13 px y girar en plena carrera la mueve 32-42 px.**

**v6.56.5 — LA PARIDAD DE LAS RUNAS (la 2ª vuelta del espejo rúnico, auditoría C):** el factor `espejoLado` de la .56.4 (−1 izquierda/+1 derecha) rompía el giro espejado con **Plumas IMPAR** — la paridad del índice YA se auto-invierte entre gemelas (`parity(n₄+j) = −parity(j)` cuando n₄ es impar) y el factor EXTRA dejaba ambas alas girando AL MISMO SENTIDO (las 4 alas rúnicas tienen 5 plumas; la .56.3 sin factor era la correcta para impar, la .56.4 arregló par y rompió impar). **La regla:** `L_B = −(−1)^num₄` con `L_A = +1` — impar: ambas +1 (la paridad hace el trabajo); par: +1/−1 (`(i < num4) || ((num4 & 1) == 1) ? 1 : -1`). Además: el giro del glifo VIAJA CON `direction` (al mirar a la izquierda todo el enjambre se espeja — el glifo también, o las runas no eran el espejo de sí mismas), las GEMELAS comparten semilla de fase/centelleo/glifo (semilla por `i % num4`, no por `i` crudo — antes las plumas gemelas centelleaban y giraban distinto: asimetría visible del par) y el ANILLO CENTRAL se espeja con la transformación **π−θ** (el espejo X de un ángulo — el orbital REVERSA con la dirección y el giro del glifo cae exacto: −θ−π/2 ≡ π−θ+π/2, mod 2π).

### 4.25 Las utilidades de soporte

- **EcoLib.cs** — LAS VOCES: `Hablar(texto, tinte, rugido, prioridad)` (cola acotada, la voz del libro SE CUELA al frente), `ElegirVariante` (reparto sin repetición), `Update/Dibujar` (capa tras "Vanilla: Death Text", NUNCA abre lotes propios).
- **CompasLib.cs** — LA COREOGRAFÍA DECLARATIVA: `EnVentana(reloj, ciclo, inicio, dur, out fase01)` · `Cada(reloj, periodo)`; STATELESS = multi-instanciable gratis. + `DepurarDibujar` (F7).
- **FormaLib.cs** — LAS COLISIONES DE FORMAS: segmento/banda/cápsula/anillo/sector, entidades NPC/Jugador/Proyectil, `RecorrerNPCs(centro, radioMax, Action)`. Todo MUNDO.
- **DefinitivasLib.cs** (v6.54.6) — el soporte de las 6 sentencias: paleta del Juicio (`GradienteJuicio(t)`), `BuscarPresa`, `Cono(hitbox, origen, largo, ang, semiang)`, `AnchoCono(progress, time, k)` (la fórmula del haz de fusión), `DeGolpe`, `Sonar` blindado, `Tex(ruta)` null-safe **y las 4 texturas del plasma** (§6.A).
- **RayoConcentradoNivelProjectile.cs** — ⚰️ **PURGADA en la v6.56.6** (la
  letra: «borra todas las nuevas armas y solo deja al rayo concentrado 2»):
  la escalera (Alba/Veredicto/Juicio), el Prisma, el Partidor, el Réquiem,
  la Ira, el Índice, la Pluma y el Ojo MURIERON con sus proyectiles. El RC2
  es LA ÚNICA arma definitiva y su exclusión vive en su propio CanUseItem.
  (Esta entrada mintió desde la .56.6 — la v1.5 la corrige: LEY DEL CÓDICE
  SINCRONIZADO §0.6.)
- **TiemposLib.cs + TiemposSistema + TiemposGlobalNPC** (v6.56.9) — **EL
  HITSTOP DE LA CASA** (la librería nueva nº1 de la auditoría §5.1):
  `Congelar(ticks)` — el mundo CONTIENE LA RESPIRACIÓN. Solo SP (MP = no-op
  documentado), solo la IA de los NPC (PreAI → false): los proyectiles del
  jugador siguen vivos, los golpes siguen conectando. Cola priorizada por
  duración. Consumidor de nacimiento: la ignición de las copias del RC2.
- **ImpactoLib.cs** (v6.56.9) — **EL MOMENTO DEL GOLPE UNIFICADO** (la
  librería nueva nº3 de la auditoría §5.3): `Golpe(pos, intensidad,
  color)` = kick escalado + flash local + baño de luz en 3 focos;
  `GolpeDeCine(pos, i, ticks, color)` = el golpe + EL HITSTOP. La fachada
  que ordena OndaLib/PantallaLib/TiemposLib (Ley Primera §0: no se inventa
  nada, se compone). Hoy: 88 Kick + 18 Flash + 244 AddLight a mano son LA
  DEUDA — cada migración a esta fachada se documenta aquí.
- **RayosPruebaProjectile.cs** (v6.56.9) — **LAS COPIAS DEL RC2**: el
  mismo conducto con LA PILA DE CAPAS (§0 LEY v6.56.9) y LA RECETA en
  `ai[1]` (0=A equilibrada · 1=B titán · 2=C sueño). El RC2 original
  queda INTACTO; las copias se ajustan hasta el look correcto y la
  ganadora se porta al original. Ignición con ImpactoLib.GolpeDeCine.

---

## 5. LOS 4 SHADERS

> Ubicación: `AethonMod/Content/Effects/Shaders/` — cada uno `.fx` (fuente) + `.fxc` (binario que carga el juego). Ver §1 para la LEY.

### 5.1 BlackHoleDistortionShader — LA LENTE GRAVITACIONAL

- **Qué hace:** distorsiona el FONDO del juego a pantalla completa — hasta **5 fuentes** curvan las UVs con rotación que decae `exp(−dist/radio·2)`; las fuentes se aplican EN CASCADA (dos agujeros cercanos curvan compuesto). Es "lente", no "remolino": ángulo pico ~0.8 rad, muere a ~3 radios.
- **Perfil:** ps_3_0, sin vertex shader. Params: `baseTexture` (s0, el frame), `distortionStrength`, `maxLensingAngle`, `sourceRadii[5]`, `sourcePositions[5]`, `zoom`, `aspectRatioCorrectionFactor`.
- **Loader:** `BlackHoleLensSystem` (único). 2 pases sobre copias de `screenTarget` a resolución nativa: **Pase A (fuerte)** — los 11 tipos de agujero negro + ondas CosmicShockwave + lentes externas de `GravLens.Registrar`; **Pase B (sutil)** — el sol en gigante roja, medusa nebulosa, cometas, púlsares.
- **Fallback:** `LensActive = false` → los proyectiles vuelven a su dibujado normal (el agujero JAMÁS desaparece). Dispose de targets encolado a hilo principal.
- **Historia:** v5.76 → restaurado v5.80 → v6.50.14 "LA CASA DEL MOD".

### 5.2 SunShader — LA SUPERFICIE ESTELAR

- **Qué hace:** la "piel" de un sol: coords esféricas (pellizco radial), doble muestreo de ruido de fuego AUTO-DESPLAZADO (la superficie se auto-pliega), manchas oscuras, ríos de lava (offsets ondulando + ruido² = filamentos calientes), corona en 1/|d| solo en el limbo.
- **Perfil:** ps_2_0 (el único 2.0). Params: `fireNoiseTexture` (s0), `accentNoiseTexture` (s1), `uvOffsetNoiseTexture` (s2) — **s1/s2 se bindean por `GraphicsDevice.Textures[1]/[2]` + SamplerStates LinearWrap, NO por parámetro**; `globalTime`, `coronaIntensityFactor`, `sphereSpinTime`, `mainColor/darkerColor/subtractiveAccentFactor`.
- **Loaders (4, todos con caché + bandera `_sunShaderFailed`):** `SunProjectile`, **`RuneSunRenderer.DrawSunBody(...)`** (LA FUNCIÓN COMPARTIDA — paleta parametrizada; disco alpha 1 que OCULTA lo de atrás: los efectos hijos se leen como backlight del limbo), `CicloEstelarRenderer` (paletas por fase), `AtaqueJefeProjectile.DibujarDiscoSolDios()` (el sol del jefe).
- **Usuarios vía DrawSunBody:** RuneSun, CometaErrante, VozCuasar, NovaEncadenada, ColapsoMagnetar, SembradorPulsar, RedSupergiant, DeadStar, Pulsar, WhiteDwarf, Magnetar, NeutronStar.
- **Fallback:** 3 capas aditivas de SoftGlow pulsante (SunProjectile); en RuneSunRenderer el disco no se dibuja y quedan backglow+aura.
- **Historia:** v5.80 → v6.08 → v6.50.14.

### 5.3 RadialShineShader — EL AURA RADIAL RUIDOSA

- **Qué hace:** aura que respira y rota — muestreo POLAR de s0 a dos escalas/velocidades (franjas gruesas lentas + filamentos rápidos) combinadas con raíz del producto; borde exterior apagado con umbral ruidoso; núcleo glow 1/r; contraste pow 2.4; aditivo puro (alpha 0).
- **Perfil:** ps_3_0. Params: `baseTexture` (s0 = WavyBlotchNoise SIEMPRE) + `globalTime`.
- **Loaders:** los mismos 4 del SunShader (capa 2 del stack estelar; escala = 2.72×R×2/texSize).
- **Fallback:** el aura no se dibuja (quedan disco + backglows).
- **Historia:** v5.80 → v6.50.14.

### 5.4 HazConcentradoShader — EL HAZ DE PLASMA (EL PRIMERO NUEVO DE LA CASA)

- **Qué hace:** el plasma del **Rayo Concentrado II** — 4 texturas multiplicadas (`CarnePlasma × SegundoRuido × RampaGlow + BitsChispa` en doble AND) con doble scroll cruzado, deformación sinusoidal transversal, chispas solo donde ambas nubes de blobs coinciden. **Las dos curas de la casa:** `uEscalaV` (los rasgos miden SIEMPRE lo mismo en pantalla — cura los dientes de sierra) y EL NÚCLEO SUAVE (smoothstep, no umbral duro — el borde RESPIRA sin contorno fractal).
- **Perfil:** vs_3_0 + ps_3_0 (el único con vertex shader y matrix). 4 samplers con `sampler_state` completo: `texCarne` s0 · `texSegunda` s1 · `texRampa` s2 · `texBits` s3. Params: `uTransformMatrix` (GameViewMatrix), `uTime`, `uEscalaV` + las 4 texturas.
- **Loader:** `HazConcentradoLib.ObtenerShader()` — caché `_cache` + `_fallo` (el patrón de la casa, v6.55.1; el RC2 conserva el suyo — mismo asset, dos caches, cero costo).
- **Uso:** el RC2 (y sus copias de prueba v6.56.9) dibujan SU strip de
  1500 puntos (color del conjuro viajero, ancho `p^0.8·cbrt·realSize·3500·(1.1+cos·respira)`).
  *(El «camino GPU portado a las armas» — `HazConcentradoLib.Dibujar` —
  fue PURGADO en la v6.56.9: sus consumidores la escalera/Prisma/Partidor/
  Ira murieron en la purga de la .56.6 y la búsqueda negativa de la
  auditoría lo certificó. Lo que vive de esa librería son LOS SOLES RÚNICOS:
  `SolRunico4Puntas` + `SolesRunicosCarga` — LA IMPLOSIÓN v6.56.9: ángulo
  FIJO por hash, cero deriva angular; nacen a 150-240 px y CAEN al centro
  con ease-in vida² — «aparecer lejos y contraerse sin dar vueltas» — +
  `SolesRunicosEstallido`.)*
- **v6.57 — LA VUELTA DEL HAZ C (el ganador de las tres copias):** la
  letra del usuario sobre la receta C, punto por punto, y sus curas:
  1. **LA LEY DEL CORAZÓN DORADO (§0):** «son como circulos amarillos
     que aparecen a veces y son demasiado grandes» — era el corazón
     dorado del sol rúnico a escala de haz (Quad de glow radial ×1.5:
     boca ~57 px, márgenes ~60 px). Los haces pasan `corazon: 0` —
     queda la estrella blanca de 4 puntas pura.
  2. **EL ALMA — LA TELA BLANCA A TODO EL ANCHO:** «debe tener otra
     textura como esa pero que ocupe todo el haz en ancho» — la MISMA
     RampaGlow del núcleo abierta a ×0.88..0.96 del cuerpo,
     blanco-violeta al 20-30%: la pila pasa de seis a SIETE capas
     (…cuerpo → ALMA → núcleo…, la vena arde ENCIMA del alma).
  3. **LOS SOLES NACEN MÁS LEJOS:** 150-240 px → 320-520 px
     (`radioLejos`/`radioVar` en `SolesRunicosCarga`): la implosión
     cae desde el borde lejano del conjuro y se aprieta con la carga.
  4. **EL TEMBLOR SUAVE DEL LANZAMIENTO:** `ImpactoLib.Golpe`/
     `GolpeDeCine` ganan `suave:` — LA VÍA DULCE de PantallaLib
     (Pantalla.Sacudir: 3.2 px de DOBLES SENOS suaves, nada de kick
     seco) + el temblor sostenido baja de 6.5 a 2.6 px para que el
     momento del disparo SE LEA.
  5. **LA CORONA VISTE EL HAZ:** las tres vueltas de la corona viajan
     LA PALETA DEL CONJURO (fases +6/+18/+30, la íntima mezclada al
     blanco-violeta del núcleo) — antes blanco/dorado/violeta fijos y
     se leían ajenas al conducto.
  6. **LA AGUJA Y LA ESTRANGULACIÓN:** `StripWidth` gana dos factores
     MONÓTONOS — `aguja` (el último 16 % se afila a ×0.06: la punta
     roma de ×0.32 ya no) y `cierre` (los últimos 45 ticks colapsan
     TODO el conducto a ×0.07 con ease-in ², TODAS las capas juntas:
     el haz se estrecha como una boca y muere — «hacerse estrecho y
     luego desaparecer»). Verificado: sim 1:1 (`tools/sim_pila_v657.py`)
     — 0 picos de borde en el SOBRE de las 3 recetas EN VIDA Y EN
     MUERTE, aguja monótona (0 rebotes, punta ×0.027 del ancho en
     p=0.84) + verificación visual (tela blanca a todo el ancho, punta
     de aguja, cero círculos amarillos, sobre liso).
- **v6.56.9 — LA PILA DE CAPAS DEL CONDUCTO (las copias del RC2):** el
  diagnóstico forense de la captura del jugador: el núcleo saturado ocupaba
  el 99% del halo medido — TODO el universo visual vivía DENTRO de un solo
  strip y NO EXISTÍA envolvente luminosa fuera de la banda (por eso el ojo
  leía «una sola capa», con shader perfecto). LA CURA (LEY §0 v6.56.9): la
  MISMA polilínea dibujada 6 veces — bruma ×2.6-3.6 (RampaGlow, violeta
  profundo al 9-20%) → glow ×1.75-2.5 (la paleta viajando) → glow² (receta
  C) → **EL CUERPO del .fx** → núcleo ×0.28-0.42 (blanco-violeta) → soles
  conducto en las márgenes. Las capas de rampa van con **Main.pixelShader
  (EL VANILLA)** + textura en `gd.Textures[0]` → **GPU-UNIVERSAL**: si el
  .fxc no carga, el cuerpo cae a una tira de CarnePlasma por el vanilla y
  LA PILA SE VE IGUAL — seis capas en cualquier máquina. Verificado: sim
  1:1 con CERO picos de borde (el único salto es el borde de ataque del
  cono, empinado por diseño) + verificación visual 5 capas legibles.
- **v6.56.4 — LA LECCIÓN DEL LOTE ADITIVO:** el shader nació aditivo (su .fx
  lo declara) pero se dibujaba bajo el pase de entidades (AlphaBlend): las
  texturas del plasma llevan bordes V NEGRO PURO y bajo AlphaBlend el borde
  del haz OSCURECE lo de debajo — las «franjas negras al ensancharse». Toda
  DrawTrail del shader va ahora DENTRO de un lote aditivo explícito
  (`CerrarLoteSiAbierto` + `gd.BlendState = Additive` + `ReabrirLoteVanilla`
  a la salida) — tanto en el RC2 como en la librería.
- **v6.56.5 — LA REGLA DE LA CAÍDA LONGITUDINAL (auditoría A):** el shader
  apaga el extremo `Coord.x→1` (bordes al 42% de la longitud, corazón al
  76%) — la «pluma» intencional del RC2/escalera (su extremo lejano se
  disipa en la distancia, documentado). **PERO el nodo 0 es la RAÍZ que
  brilla: un conducto cuyo extremo de ACCIÓN es el lejano debe ordenar la
  espina con el extremo de acción en el nodo 0** — la Ira (la columna se
  fundía a negro JUSTO al llegar al punto de impacto, donde viven las
  novas) y el Partidor (la telegrafía y el corte perdían su mitad inferior)
  v6.56.5 invierten la espina (`Lerp(abajo, arriba)` / `Lerp(piso, techo)`) y
  TODO lo dependiente se evalúa en la posición física original `p = 1−q`
  (el color viaja hacia el punto de acción como siempre) con uTime NEGADO
  (para conservar el sentido físico del hervor: positivo = hacia la raíz).
  El Prisma conserva su fade hacia la punta (el lenguaje de la pluma del
  RC2 — decisión de diseño documentada).
- **v6.56.7 — LA SOLUCIÓN DE LOS PICOS (LA LEY DEL PIXEL vs EL QUAD):** el
  RC2 de la v6.56.6 dibujaba «LA PILA DE CAPAS» — tres fajitas CPU
  (`RayoStrip.FajaTexturadaEnLote`) SIEMPRE ENCIMA del strip GPU para
  dar «desenfoque, glow y brillo». **ESA PILA ERA LA CAUSA DE LOS PICOS**,
  cazada por el simulador 1:1 de la casa (`/home/z/sim/sim_picaros.py`,
  texturas reales + la matemática exacta del .fx) y confirmada por
  verificación visual:
  1. **LAS FAJITAS SON QUADS DE BORDE POLIGONAL DURO.** Su silueta es la
     arista del polígono — una línea recta por segmento que corta el
     borde del haz a hachazos (64 nodos = un 64-gono a escala de cono).
  2. **LA TEXTURA DEL «DESENFOQUE» ERA `SegundoRuido` — UNA TEXTURA
     BINARIA** (52 % negro / 22 % saturado, blobs duros; §6.A) diseñada
     para MULTIPLICARSE dentro del shader. Vista cruda en una faja al
     1.8× del semiancho, sus blobs duros flotan FUERA del borde del haz:
     peines de dientes (el sim lo midió: pico-valle 222 px contra 163 px
     de la técnica correcta; «dientes rectangulares duros saliendo del
     borde», dijo la verificación visual).
  3. **En el commit 607df36 las fajitas solo vivían en el FALLBACK sin
     shader** — con el .fxc cargado el jugador jamás las veía: POR ESO
     «antes no había picos». LA TÉCNICA CORRECTA ES LA DE 607df36 y
     volvió ENTERA al RC2: strip de 1500 puntos + el shader de las
     cuatro texturas EN LOTE ADITIVO (la cura v6.56.4 de los bordes
     oscuros) + la boca de glows apilados (cuatro, con el alfa a CERO:
     bajo el pase de entidades de FNA `(One, InvSrcAlpha)`, A=0 con
     rgb>0 = SUMA PURA — un bloom de verdad, jamás un disco que tapa) +
     el gradiente `ColorPrisma.ValueAt` (EL COLOR DEL MOD, perdido en la
     reconstrucción binaria de la v6.56.0) + los polvos cromáticos
     MUERTOS (eran los «círculos planos» de esa era). **LA LEY QUE
     QUEDA: todo lo que un haz necesite de más — desenfoque, glow,
     brillo, filamentos, aberración — SE HACE PER-PIXEL DENTRO DEL
     .fx (una curva smoothstep/pow no puede producir picos) O NO SE
     HACE.** Jamás quads CPU encima de un strip GPU.
- **v6.56.7 — EL SHADER MEJORADO (la letra: «dale una mejora de shaders
  tambien»):** cuatro mejoras PER-PIXEL dentro del mismo .fx (receta
  completa en los comentarios del archivo):
  · **EL HALO — el desenfoque de verdad:** la falda geométrica `pow³` que
    muere EXACTAMENTE en el borde del polígono del strip, texturada con
    la carne a su propio flujo (`uTime·1.3 + Coord.x·0.7`) — vive FUERA
    de la envolvente del núcleo (`color.rgb = núcleo·envV + halo`).
  · **LOS FILAMENTOS — la auto-interferencia:** la carne multiplicada por
    una copia de sí misma a MEDIA escala (`VentanaV(Coord.y·0.5+0.25)`,
    flujo inverso `−Coord.x·0.6`) al 60 % — cordones grandes que
    serpentean bajo el hervor fino (la técnica del recetario v6.54.8).
  · **EL NÚCLEO QUE LATE:** el umbral de la vena blanca respira con
    `sin(uTime·4.65)` (≈54 ticks con el `uTime=−localAI·0.025` de la
    casa) — el corazón se hincha y afloja.
  · **LA FRANJA PRISMÁTICA:** el halo se muestrea TRES veces con un
    microdesfase por canal en la zona del filo (`±0.035·(1−envV)`) —
    rojo afuera, azul adentro, la aberración de la casa en el borde.
- **v6.56.7 — EL CONTRATO DE COORDENADAS DE VFXCore (el bug de los soles
  invisibles):** `VFXCore.Quad` recibe coordenadas de **MUNDO** (el
  volcado resta `Main.screenPosition` — §3.1); `OrbitaLib.CoronaConjuro`
  y `Main.EntitySpriteDraw` reciben coordenadas de **PANTALLA** (mundo −
  screenPosition). La v6.56.6 le pasaba PANTALLA a los soles rúnicos de
  la carga (`SolesRunicosCarga/Estallido` → `SolRunico4Puntas` →
  `Quad`): nacían desplazados por `screenPosition` completo — FUERA de
  la pantalla, jamás vistos (por eso la letra «te olvidaste de usar los
  brillos de soles runicos del accesorio»: la receta ESTABA, invisible).
  **REGLA: cada API de dibujo declara su sistema de coordenadas en su
  `<summary>` — leerlo antes de llamar. Los quads VFXCore son MUNDO,
  SIEMPRE.**
- **v6.56.8 — LA CURA DE LA BANDA PLANA + LA PALETA DEL CONJURO + LA REGLA DE LA CIRUGÍA:** la .56.7 reescribió el PS completo (9 muestreos) — validó contra MojoShader pero en la GPU del jugador el haz era UNA BANDA PLANA sin textura (RGB constante ±3, borde duro, cero fBm — las capturas analizadas píxel a píxel). LA CURA: **el .fx vuelve al ESQUELETO EXACTO DE LA v6.55.1** (el bytecode PROBADO en la máquina del jugador durante toda la era 607df36 — mismos constructos, 6 muestreos) y las mejoras viven como DELTAS MÍNIMOS: **LAS VENAS ROJAS** (`SegundoRuido` promovido de multiplicador a SELECTOR de color — el tono salta del violeta profundo al rojo vivo donde su máscara enciende), **EL PULSO VIAJERO** (bandas de brillo que corren a la punta), **EL HALO** (pow³ con UNA muestra de carne, el filo teñido de rojo) y **EL NÚCLEO QUE LATE**. **LA PALETA DEL CONJURO** (la letra: «los colores del mod son principalmente morado, rojo, negro»): `ColorPrisma` MUERE en el RC2 — `RayoConcentradoDosProjectile.ColorConjuro` (violeta arcano → morado profundo → rojo sangre → magenta, ciclo 48 t, viaja a lo largo) alimenta el `input.Color` del shader, la luz del mundo, los anillos, los arcos y la corona. **LA REGLA QUE QUEDA: el .fx del RC2 se cambia CON CIRUGÍA — cada delta se compila, se valida contra MojoShader, se INSPECCIONA SU GLSL transpilado y se verifica en el sim 1:1 ANTES de empaquetar. JAMÁS reescritura completa del PS.** LA BOCA DE GLOWS APILADOS MURIÓ COMPLETA (§0 — era EL círculo plano de las capturas): la boca son la corona + los soles + los arcos + LA ESTRELLA CONDUCTO (`SolRunico4Puntas` latiendo en la boca). Sim 1:1: rugosidad del borde media 1.64 px (shader) / 4.09 px (fallback); 17 % del cuerpo son venas rojas, 16 % violetas.
- **Fallback:** tiras RayoStrip ×4 SUAVES (funda violeta carne 420px · cuerpo paleta-por-nodo carne 300px · venas rojas rampa 240px · núcleo blanco banda 380px — SIN remolino binario, v6.56.8) — el arma
  NUNCA se rompe. La aberración cromática del borde prismático vive en
  `HazConcentradoLib.AberracionCromatica` (vía CPU, fina y lisa).
- **Historia:** v6.55.0 (nació SIN .fxc — §1) → **v6.55.1 compilado con la cadena propia** (validado: 0 errores, 11 params, técnica VS+PS, 4 samplers × 6 estados, GLSL transpilado verificado) → **v6.56.4 portado a las armas** (HazConcentradoLib + la reversión educada de RayoStrip, §6.A).

---

## 6. LAS TEXTURAS DE EFECTOS

> **Regla de generación:** TODA textura de efectos nace de un script Python en
> `tools/` (determinista, con tests de verificación incorporados cuando aplica —
> p.ej. el test anti-picos FFT del `gen_plasma_v6548.py`). Nada de imágenes
> descargadas o pintadas a mano. **449 PNG en el mod (v6.56.1): 91 son de
> efectos, 358 son iconos/sprites.** Los scripts viven FUERA del mod (buildIgnore) y cada
> uno se nombra `gen_<cosa>_v<versión>.py`.

### 6.A El plasma del Rayo Concentrado (`Content/Effects/Procedural/`, 100% casa)

| Archivo | Tamaño | Qué es | Acceso |
|---|---|---|---|
| `CarnePlasma.png` | 256² | LA CARNE: fBm Perlin 1/f (espectro rosa), domain warping ±6px, envolvente suave en V, blur direccional | `DefinitivasLib.TexRuido` → shader uTexturaCarne + fallback |
| `SegundoRuido.png` | 256² | EL SEGUNDO RUIDO: binario (52% negro + 22% saturado) — cruzado con la carne a distinta velocidad, el haz HIERVE | `DefinitivasLib.TexRemolino` |
| `RampaGlow.png` | 256² | LA RAMPA: perfil de calor transversal (la funda del canal) | `DefinitivasLib.TexRampa` |
| `BitsChispa.png` | 512² | LOS BITS: blobs duros elongados 2-3:1, cobertura 12%, SOLO en el corazón V 0.32..0.68 (cura de los picos v6.55.0) | `DefinitivasLib.TexBits` (doble AND) |

Generadores: `tools/gen_plasma_v6548.py` (las 4 + test anti-picos) y `tools/gen_bits_v6550.py`.
**v6.56.4 — LA PILA CPU HERMANA (`HazFusionEspina`) FUE RETIRADA** (0
consumidores): la letra del usuario («solo debes adaptar el código del rayo
concentrado 2 a las características de las armas con pico») mandó los
conductos al camino GPU (§5.4). Su fórmula de color viajero + lente vive en
los llamadores; su aberración en `HazConcentradoLib.AberracionCromatica`; y
`RayoStrip.FajaTexturadaEnLote` volvió a su estado pre-v6.56.3 (ventana V
completa, período de sintonía, UN quad por segmento) conservando SOLO la
mitra por nodo — la lección completa en el propio RayoStrip.cs.

**v6.56.7 — LA LEY DE USO DE LAS CUATRO TEXTURAS (cómo se usan BIEN, la
forma correcta del RC2):** las cuatro texturas del plasma son PIEZAS DE UNA
MULTIPLICACIÓN DE SHADER, no sprites autosuficientes:

| Textura | Para qué NACIÓ | Cómo se usa BIEN | Jamás |
|---|---|---|---|
| `CarnePlasma` (TexRuido) | LA CARNE — el fBm que define el cuerpo | sampler s0 del shader (`uTexturaCarne`) · el núcleo del fallback (faja 420px) · EL HALO del .fx v6.56.7 (3 muestras con desfase prismático) | — |
| `SegundoRuido` (TexRemolino) | EL SELECTOR DE VENAS — **textura BINARIA** (52 % negro / 22 % saturado): su máscara elige el TONO del plasma (violeta ↔ rojo) dentro del shader (v6.56.8 — antes multiplicaba y mataba media textura a negro) | sampler s1 (`uTexturaSegunda`) — la máscara `saturate(r·1.6−0.05)` de LAS VENAS ROJAS | **JAMÁS dibujada cruda como capa ancha «de desenfoque»** — sus blobs duros fuera del borde del haz son LOS PICOS (§5.4, la ley v6.56.7) — y el fallback YA NO LA USA (v6.56.8) |
| `RampaGlow` (TexRampa) | EL PERFIL DE CALOR transversal (la funda del canal) | sampler s2 (`uTexturaRampa`) — la falda del núcleo | — |
| `BitsChispa` (TexBits) | LAS CHISPAS — blobs duros SOLO en el corazón V 0.32-0.68 (la cura v6.55.0) | sampler s3 (`uTexturaBits`) muestreada DOS veces con flujos distintos — el DOBLE AND (arden solo donde ambas nubes coinciden) | muestrearla una sola vez (chispas «apagadas») o fuera del corazón V |

El RC2 completo es EL ejemplar vivo: strip 1500 puntos + el .fx con las
cuatro en su papel (el esqueleto .55.1 + venas/halo/pulso como cirugía) +
la corona rúnica + los soles de 4 puntas del accesorio + la estrella
conducto (§5.4 para el diagnóstico de los picos, la banda plana y el
contrato de coordenadas). CERO discos en la boca (la ley §0).

### 6.B Las texturas del sol (familia SunShader, `Content/Effects/Textures/`)

| Archivo | Tamaño | Qué es |
|---|---|---|
| `DendriticNoiseZoomedOut.png` | 256² | s0 — ruido de fuego de la superficie (granulación dendrítica) |
| `WavyBlotchNoise.png` | 256² | s1 del sol + textura del RadialShine |
| `PsychedelicWingTextureOffsetMap.png` | 256² | s2 — mapa de offsets UV (ríos de lava/corona) |
| `BloomCircleSmall.png` | 200² | el backglow del sol (2 discos: cálido 0.95×R + rojo 1.61×R) |

### 6.C Ruido base y alas (`Content/Effects/Procedural/`)

- `Ruido_0.png` (1024², fBm 8 octavas) + `Ruido_1..19.png` (512² c/u: fBm de contraste variado, células, plasma, anillos) — uno por estilo de ala (`AlasEstilo`). **v6.56.1: las Ruido_3/6/13/14/16/19 YA NO SON HUÉRFANAS** — las alas rúnicas y anatómicas 17-23 las usan TODAS (Runas Solares→13 · del Vacío→6 · del Eclipse→14 · del Juicio→16 · Agujas→19 · Cuchillas→3 · Corolas→6). La lista vieja de huérfanas de esta sección era un boomerang de purga: borrarlas habría ROTO siete alas.
- `Investigacion/Espacio_1.png` (512², nebulosa violeta — ala Vía Láctea).
- `RuidoTinta.png` (256×64, fBm tileable en U + caída gaussiana en V) — BeamLib (y el Primer Trazo).
- Generadores: `gen_investigacion_v6540.py`, `gen_texturas_pro_v6543.py`, `gen_beam_v6522.py`.

### 6.D Primitivas de partículas y VFX (las que usan las librerías)

| Archivo | Tamaño | Uso |
|---|---|---|
| `SoftGlow.png` | 64² | **EL glow universal — la textura más usada del mod (81 archivos)** |
| `Ring.png` | 256² | anillo (24 archivos) |
| `BlackDisk.png` | 256² | el disco del horizonte de sucesos (13) |
| `Star.png` | 16² | estrella 4 puntas (9) |
| `GlowOrb.png` | 128² | orbe glow (7) |
| `NovaBurst.png` | 256² | destello monotónico (VFXCore, PantallaLib, StormLib) |
| `DestelloFinal.png` | 256² | la cruz de 8 brazos |
| `Trail.png` / `TrailGlow.png` | 64×8 / 32×8 | tiras de estela |
| `SparkleStar.png` | 32² | destello estrella con luz |
| `Slash.png` · `Vortex.png` · `Crescent.png` · `Noise.png` | 64² | corte · vórtice · luna · ruido (ParticleManager) |
| `PulsarCore.png` | 128² | núcleo del púlsar |
| `FireRing.png` | 256² | anillo de fuego (supergigante, shockwaves) |
| `CometHead/CometCrown` | 192²/256² | el cometa (StellarCometMinion) |
| `JellyfishBell/Bead/Galaxy` | 256²/64²/128² | la medusa nebular |
| `LumenBlade/Bloom/Flare` | 64×256/200²/128² | la familia de luz (LumenLib) |
| `BoltChain/BoltCore/BoltHalo` | 256×64 | el rayo (StormLib; `gen_bolts_v65015.py`) |
| `BoltImpact.png` | 200² | impacto del rayo (`gen_boltimpact_v65022.py`) |
| `RiftTaper Cuerpo/Nucleo/Velo/Void` | 512×64 | las 4 capas del desgarro (RiftLib) |
| `HexCyan.png` | 64² | la jaula de la enana blanca |
| `InvisiblePixel.png` | 1×1 | proyectil 100% dibujado a mano |

### 6.E Las horneadas EN RUNTIME (sin PNG — las cuece VFXCore/BrumaBrushes al arrancar)

`Arcoiris` (512²), `Carne` (128×512, periódica en V), `Colmillo` (48²), `Ventosa` (32²), `OjoRasgado` (64²) — patrón Arcoiris: **cero assets nuevos**. BrumaBrushes cuece las texturas fBm de la bruma (64/128/160) premultiplicadas.

### 6.F Huérfanas conocidas (empaquetadas, sin referencias — candidatas a purga)

`BeamGold, GlowRay(×3), Shield(×2), HexGold, OblivionBlob, RiftLip, RingShieldNebula, FireNoiseB, GlowCircle Green/Magenta/Purple/Red, GlowOrb Cyan/Gold/Green/Magenta/Purple/White` — 20 en total (**v6.56.1**: las 6 Ruido_N rehabilitadas por las alas 17-23 salen de la lista; los 5 huérfanos nuevos hallados por la auditoría — `GlowCircle.png` base, `CosmicOrbBuff.png`, `InvisiblePixel.png`, `BlackHoleProjectile.png`, `BlackHoleStaff.png` — fueron PURGADOS en la misma entrega). Si una purga las borra, tacharlas aquí.

---

## 7. EL SONIDO — LAS 3 CAPAS

### 7.1 Capa 1 — AudioLib.cs (v6.34): LA IDENTIDAD SONORA

**NO es un sintetizador**: es la capa de VOCES sobre sonidos 100% vanilla 1.4 (cero archivos de audio, cero rutas frágiles).

- `enum Familia { Solar, Vacia, Electrica, Runico, Cosmica, Desgarro }` — las 6 voces, cada una con pitch base propio (Solar +0.10, Vacia −0.30, Electrica +0.40, Runico 0.00, Cosmica −0.10, Desgarro −0.20).
- **`Sonar(Familia, momento, pos, volumen, pitch)`** — EL ÚNICO punto por el que el mod debería reproducir sonido de combate. Momentos: `"apertura"` (nace, 0.90), `"impacto"` (1.00), `"carga"` (0.60, pitch estable sin jitter), `"zona"` (0.45), `"muerte"` (1.00). Aplica pitch de familia + jitter vivo ±0.08, **anti-spam** (mismo familia+momento NO suena más de 1 vez cada 12 ticks — 30 para zona/carga), servidor sordo.
- `SilenciarZona()` — reset del anti-spam (al cambiar de arma, la voz nueva suena YA).
- `Reiniciar()` — higiene entre mundos (lección v6.49 AUD-C).
- El catálogo interno `Elegir(f, m)` documenta el pedigrí de cada SoundID vanilla (p.ej. Solar/impacto = Item45 "el yunque del sol").
- **Adopción actual: 21 llamadas en 6 archivos** (v6.56.1 — la auditoría contó las reales: VerboPrimordial, SierpesNuevas, ApuestasStaffs, GrimorioFuriaSistema + las 3 armas del grimorio v6.56.0 vía `AudioLib.Sonar`). **TODO sonido de combate NUEVO debería pasar por aquí.** `SilenciarZona()` está cableado desde la v6.56.1 (al cambiar de arma, la voz nueva suena YA — `GrimorioFuriaSistema.PostUpdateEverything`).

### 7.2 Capa 2 — Sonidos vanilla directos

294 llamadas `SoundEngine.PlaySound` en 108 archivos (jefes: AethonBoss 36, AethonBossFusionado 19, AtaqueJefe 17). Top: **Item122 ×111** (zumbido grave), Item8 ×40, Item14 ×38, Item4 ×33, Item12 ×33, Item88 ×17 (succión), Roar ×16, Item117 ×16… Helpers blindados por archivo: `DefinitivasLib.Sonar` (try/catch, jamás tumba el frame — las 6 definitivas) y 11 copias privadas del mismo patrón. **Regla de colocación:** en proyectiles, sonar en `AI()` gated `!= Server`; no duplicar los del NPC PreAI.

### 7.3 Capa 3 — EL AUDIO PROPIO DEL RC2: el bucle + LA CARGA (v6.56.4)

- **El bucle** (`Content/Sounds/RayoConcentradoLoop.ogg`): Vorbis q5, 44.1 kHz estéreo, **6.000 s EXACTOS de bucle periódico**.
- **v2 (v6.56.0) — EL PULSO CONSTANTE.** La letra del usuario: *«el sonido va y viene como si fueran ondas, pero el ataque no son ondas — hazlo un pulso constante y luego acabe»*. LA AUTOPSIA DE LA 1ª GEN (el contrajemplo): su GRAVE casi afinado (49.0/49.17 Hz) era un **BATIDO** de 4 ciclos por bucle (el «va y viene»), y su mezcla llevaba **RESPIRACIÓN** de 1 ciclo (el vaivén) y una **BANDA MÓVIL** de ruido (640±280 Hz barriendo — la ilusión de onda). LA RECETA v2: **CERO batido** (un solo grave 48 Hz, sin pareja desafinada), **CERO respiración** (mezcla plana), **BANDA FIJA**, y el ARRANQUE-CONTENIDO-FINAL lo pone la reproducción, no el archivo. Mantiene el truco de la 1ª gen: bucle perfecto por construcción (frecuencias múltiplos de 1/6 Hz, costura del ruido fundida).
- **v3 (v6.56.4) — UN POCO MÁS GRAVE.** La letra: *«el sonido actual es muy agudo
  para el tipo de haz de luz; tiene que ser un poco más grave mientras se
  dispara y mantiene el rayo, y cuando comienza a apagarse el sonido también
  debe [apagarse]»*. La 1ª gen era aguda de FÁBRICA (motor de armónicos en
  588-2352 Hz, plasma en 640±280). La v3 baja el registro una octava+: motor
  en 172-688 Hz, plasma en BANDA FIJA a 340 Hz, crujido una octava abajo (HP
  1200, más quedado) y la mezcla inclinada al grave (0.46/0.30/0.34/0.10).
  El pitch en juego: **−0.22 sostenido** durante el láser y **plane a −0.70**
  en los últimos 60 ticks (antes quedaba CONGELADO en +0.2 — el «muy agudo»);
  el volumen ya viaja con `realSize` → muere con el rayo.
- **EL SONIDO DE CARGA (NUEVO, `Content/Sounds/RayoConcentradoCarga.ogg`):**
  one-shot de 5.15 s (la letra: *«en el momento de carga necesita sonar que
  está cargando, debes generar ése sonido»*). La carga era el ÚNICO silencio
  del arma: el volumen del bucle viaja con realSize (0 hasta el tick ~280),
  así que la carga sonaba a NADA. El one-shot toca desde el tick 2 y su
  CRESCENDO desemboca justo en la ignición: EL GRAVE QUE DESPIERTA (44→55 Hz
  en glide), EL CORO ARMÓNICO QUE SUBE (172-688 Hz deslizándose +15% — nada
  de agudos), EL PULSO ACELERANDO (1.8→4.0 latidos/s) y EL AIRE (succión
  parda); el finale (0.45 s) rampa al pico y corta en 25 ms — la ignición
  la pone el bucle. Generador: `tools/gen_sonido_v6564.py` (ambos .ogg).
- **La reproducción** (`RayoConcentradoDosProjectile.SonidoEnLoop`): un slot ActiveSound con `ProjectileAudioTracker`, `IsLooped`, `MaxInstances=0`; cada frame actualiza Position/Volume/Pitch por la fase del arma; muere con el proyectil. La carga va por `DefinitivasLib.Sonar` (one-shot, `MaxInstances=1`). Extra: `On_Main.UpdateAudio` baja la MÚSICA −30% mientras vive el haz.
- **Si mañana hace falta OTRO sonido propio en bucle:** replicar ESTE patrón (generador numpy con frecuencias múltiplos de 1/L + costura fundida del ruido + SonidoEnLoop con tracker) y documentarlo aquí.

---

## 8. SISTEMAS DE SEGURIDAD Y DIAGNÓSTICO

| Sistema | Qué hace |
|---|---|
| **LoteSeguroFX** (Globals/, v6.54.5) | EL BLINDAJE: `PostDraw` de CADA proyectil llama `VFXCore.ReabrirLoteVanilla()` — si un PreDraw dejó el lote CERRADO, se CURA antes de que el `End` de `Main.DrawProjectiles` reviente el proceso (la autopsia del crash del Ojo del Juicio). Para proyectiles vanilla: no-op de una sonda |
| **VFXCoreSystem** (en VFXCore.cs) | `PostDrawTiles`: vuelca capas de oclusión + **EL JANITOR** (anula `Textures/Samplers[1..3]` que los shaders dejan pegados) + aprende el hilo principal |
| **DiagnosticoVFXSystem** | **EL OVERLAY F8**: FPS suaves, FactorCalidad, quads del frame vs techo 24.000 (con barra), cola de EcoLib, emisores de AuraLib, fuerza de pantalla, fase de la Furia. Cache 4 Hz |
| **CalidadFpsSystem** | `PostUpdateEverything` → `VFXCore.ReportarFps` — el corazón del presupuesto adaptativo (la calidad respira sola) |
| `OndaSystem` | Aplica la SACUDIDA (`ModifyScreenPosition`, máx 2 impulsos ~13 Hz cap 14 px) y el DESTELLO local del arma |
| `BrumaSystem` / `MediaResSistema` / `PantallaSistema` / `PulsoSistema` / `RayoSistema` / `EcoSistema` / `OcasoSystem` | Ciclos de vida de sus librerías (dispose, purga de pools, dibujados de UI) |
| `ParticleManager`/`ParticlePresets` (Content/Particles/) | El sistema de partículas heredado: buffer pre-asignado, batching por blend, frustum culling, componentes bitmask |

**El crash que enseñó todo (v6.54.5):** «End was called, but Begin has not yet been called» — los PreDraw de las Doce Sentencias usaban Flush (que CIERRA el lote de entidades) y salían sin reabrir → tML mataba el proceso. LA CURA EN 4 CAPAS: (1) las 20 reaperturas (todo PreDraw sale con el lote VIVO — el contrato v6.50.11), (2) LoteSeguroFX global, (3) guard de lote muerto en Blit, (4) los adornos del Ojo pasaron a Quads del buffer. **HOY ES IMPOSIBLE romper el lote desde un PreDraw de la casa si sigues el contrato.**

---

## 9. LOS RENDERERS

> `Content/VFX/*Renderer.cs` — el dibujado COMPLETO de un arma/jefe. **Contrato escuela C:** `Draw()` exige el SpriteBatch CERRADO y lo deja CERRADO (masa/oclusión en AlphaBlend PRIMERO, brillos en Additive después); determinismo (time, seed) + Hash01.

**Los 10 agujeros negros** (`Draw(center, scale, time, seed)`, mundo):
`Olvido` (v6.15, el clásico: vacío + plasma + runas doradas) · `Umbral` (v6.16, disco de acreción OBLICUO con DOPPLER — lado que acerca BLANCO, aleja ROJO) · `Cosmic` (v6.16, anillo MAGENTA con 20 bandas) · `Supremo` (v6.18/23, LA FUSIÓN de los 4) · `SupremoAurora` (gradiente aurora radial) · `Bruma` (v6.17, LA DEMOSTRACIÓN de BrumaFX: gélido teal/cian) · `BrumaAscendido` (+ escarcha eléctrica) · `CosmicAscendido` (+ tormenta en los picos) · `UmbralAscendido` (+ lluvia de rayos) · `OlvidoAscendido` (+ rayos espirales + nebulosa fBm).

**Las estrellas** (`Draw(p, lifeT, seed…)`, mundo): `NeutronStar` (STARQUAKE con aberración R/B) · `Pulsar` (EL FARO de 2 haces) · `Magnetar` (campo estrangulado + reconexión) · `RedSupergiant` (coloso con celdas de convección + COLAPSO) · `WhiteDwarf` (destellos hexagonales + MOTAS DE LEECH) · `DeadStar` (LA ENANA NEGRA — la inversa: masa oscura PRIMERO, el aura APAGA la luz) · `CicloEstelar` (UNA VIDA COMPLETA en 5 actos ~18 s: nebulosa→ignición→gigante→supernova→remanente; `ActOf(age)` dice el acto) · `EclipsePrimordial` (EL ECLIPSE TOTAL: ocultador negro, perlas de Baily, anillo de diamante) · **`RuneSunRenderer` (1.291 líneas — LA FAMILIA DE SOLES RÚNICOS por `tier 1..10`, expone `DrawSunBody` el disco compartido)**.

**Los bastones-arma:** `CoroEspectral` (notas musicales dibujadas por código + el director) · `EnjambrePrismatico` (12 abispajas) · `MareaGravitatoria` (la ola rotada por la pendiente del terreno) · `PenduloJuicio` (maza con runas grabadas) · `RelojArena` (26 motas con física + INVERSIÓN).

**Facades de corona:** `RuneCrownRenderer` → `SigiloLib.ArcoGloria` · `ArcCrownRenderer` → `OrbitaLib.CoronaArcos` · `AnilloDorsalRenderer` → `OrbitaLib.CoronaConjuro` · `OcasoBurstFX` (el desgarro que respira al morir).

---

## 10. EL DECÁLOGO DEL EFECTO NUEVO

**El checklist ANTES de escribir (obligatorio):**
1. Leí §2 (el mapa) y §4 (el catálogo) y **no existe** lo que necesito (o lo que existe no sirve de base).
2. El efecto nuevo **reutiliza el núcleo**: VFXCore (quads/lotes/Ribbon/Hash01/EsObjetivo), RayoStrip si son tiras, texturas de §6, AudioLib si suena.
3. Elijo mi **escuela de lote** (A/B/C, §3.3) y la documento en la cabecera del archivo.
4. **Coordenadas de MUNDO** salvo excepción justificada (RiftLib/BrumaFX son las únicas de pantalla).
5. **Determinismo total:** semillas + `Hash01`, cero `Main.rand` en render.
6. **Color premultiplicado** (`Color·f`); si oscurece, lote alfa/`FlushAlpha`/`PaseAlpha` — nunca negro en aditivo.
7. **Presupuesto:** pregunto `VFXCore.Presupuesto(n)` antes de emitir en masa.
8. **Todo PreDraw sale con el lote VIVO** (`ReabrirLoteVanilla()` al final) — LoteSeguroFX es la red, no la excusa.
9. Sonidos en `AI()` gated `!= Server`, por AudioLib si es combate.
10. **SI ES UN SHADER: §1 ES LEY** — `.fx` NUEVO o tocado ⇒ `.fxc` regenerado con la cadena + `fx20_validate.py` + loader con caché/bandera/fallback.

**El checklist DESPUÉS de escribir (LA REGLA DE DOCUMENTACIÓN — deja constancia):**

> **TODA librería, shader, efecto visual, efecto de sonido o textura NUEVA —
> y toda modificación de una existente que cambie su API o su look —
> SE DOCUMENTA EN ESTE ARCHIVO EN LA MISMA ENTREGA en la que se crea.**
> Sin excepciones. Este archivo es la memoria de la casa: si no está aquí,
> el siguiente (IA o humano) lo va a reinventar mal.

1. Añadir/ampliar su entrada en §4/§5/§6/§7 (qué dibuja, API, contrato, usuarios).
2. Añadir fila al mapa §2 si es un "quiero hacer…" que faltaba.
3. Añadir fila en la bitácora §11 (versión, qué nació, dónde vive).
4. Si nació de una lección (un bug, un reclamo del usuario), dejar el caso en §0 como prohibición o en §8 como historia.
5. Si genera assets: el script `tools/gen_*.py` correspondiente referenciado en §6.

---

## 11. BITÁCORA DE DOCUMENTACIÓN

| Versión | Qué nació / cambió | Dónde vive |
|---|---|---|
| v5.76/v5.80 | Los 3 shaders veteranos (+sus .fxc históricos) | §5.1-5.3 |
| v6.03 | VFXCore — el núcleo de quads | §3.1 |
| v6.21+ | StormLib (pilas de pasadas) | §4.3 |
| v6.25 | BrumaFX (humo fBm), EstelaLib, OndaLib | §4.2, §4.7, §4.11 |
| v6.34 | OrbitaLib, SigiloLib, NebulaLib, AudioLib, GravLens | §4.15, §4.16, §4.21, §7.1, §3.4 |
| v6.35-43 | RiftLib parciales, SierpesLib, TelaLib, MoldeLib, CompasLib, FormaLib, MediaResLib | §4.14, §4.18, §4.8, §4.22, §4.25, §3.4 |
| v6.50.11 | EL CONTRATO DEL LOTE (ReabrirLoteVanilla) | §3.1 |
| v6.50.18 | RayoLib — el puerto 1:1 del LightningGenerator vanilla 1.4.5 | §4.4 |
| v6.50.62/65 | SombrasLib + la lección "negro sobre negro" (rims violáceos) | §4.1, §0 |
| v6.54.5 | LoteSeguroFX + las 4 capas de la cura del crash del lote | §8 |
| v6.54.6/7 | DefinitivasLib; LA LECCIÓN DE LOS ÓVALOS (Ira del Cielo → BrumaFX) | §4.25, §0 |
| v6.54.8 | Las 4 texturas del plasma (test anti-picos FFT) | §6.A |
| v6.55.0 | HazConcentradoShader (sin .fxc — el bug), el sonido sintetizado, AlasAuroraLib vectorial | §5.4, §7.3, §4.24 |
| **v6.55.1** | **EL DESCUBRIMIENTO .fx/.fxc + LA CADENA DE COMPILACIÓN (fx20_build/validate, vkd3d)** — y **ESTE CÓDICE (v1.0)** | §1, §10 |
| **v6.56.0** | **LA LEY DEL CERO-DUST** (los Dust junto al filo son la sierra de los picos y los «círculos planos») · la CURA DEL CENTRADO de alas (`pl.fullRotation`) + DibujarRunas + catálogo 17-23 · **LA ESCALERA DEL RAYO** (RayoConcentradoNivelProjectile, nivel en `ai[1]`) · el SONIDO v2 pulso constante · el PARTIDOR a pantalla completa · la IRA DEL CIELO densa — y **ESTE CÓDICE (v1.1)** | §0, §4.24, §4.25, §7.3 |
| **v6.56.1** | **LA RONDA DE CURACIÓN de la auditoría 4×**: la recursión `SendExtraAI` del decompilador (StackOverflow MP), el `ai[2]`=whoAmI envenenando los relojes de las 3 armas nuevas, el switch del Testamento sin gate (sentencia doble en MP), `RestaurarLoteVanilla` (la cura del lote aditivo), la exclusión cruzada de la escalera, el vuelo/mira del RC2 solo del dueño, la aguja de carga recuperada, los pozos cero-GC del RC2, las 4 horneadas con funeral, el latch eterno de las alas, la marca del festín con caducidad, el es-MX deduplicado (−636 líneas) con paridad 1369=1369=1369 al parser REAL, 5 PNG huérfanos purgados y ESTE CÓDICE puesto al día (§3.1/§4.25/§6/§7.1) | §3.1, §4.25, §6, §7.1, §11 |
| **v6.56.4** | **EL CAMINO GPU PORTADO A LAS ARMAS**: `HazConcentradoLib` (§5.4 — la letra: «adapta el código del rayo concentrado 2») con la LECCIÓN DEL LOTE ADITIVO (las franjas negras del RC2) · **LA REVERSIÓN EDUCADA** de `FajaTexturadaEnLote` (mitra por nodo se queda) y `HazFusionEspina` RETIRADA · **EL ESPEJO COMPLETO de las alas** (permuta filo/lomo por lado + permuta de semianchos bajo el espejo + EL BARRIDO revivido con acople a velocidad — validado Δ 0.000px en sim) · **EL PORTAL DEL CONJURO** del RC2 (BlackDisk + CoronaConjuro + GravLens en la boca; la aguja y el GlowOrb planos MUEREN; destellos de soles rúnicos convergentes/estallido) · **LOS SONIDOS v3** (bucle grave + carga one-shot) + `Pantalla.Sacudir` · **LA LOCALIZACIÓN MONOLINGÜE** (es-ES fuente + espejo es-MX generado, en-US retirado) | §5.4, §6.A, §7.3, §4.24, §11 |
| **v6.56.5** | **LA CURA DEL PITIDO + LA AUDITORÍA DE LOS 4 AGENTES**: el seno puro de 1376 Hz del finale de la carga MUERE (→ soplo de presión sin pitch, detector anti-pitido calibrado en el generador) · **EL TEMBLOR PERCEPTIBLE** (el sostenido era SUB-PÍXEL: trauma²·i — 2.6 daba 0.18 px; ahora 6.5/9) · **GravLens SIN MONOPOLIO** (vida 0.06 s — antes pedía 36 lentes contra una mesa de 8) · **EL HORIZONTE DEL PORTAL A TAMAÑO REAL** (2.28·R, la convención BlackDisk — salía a la mitad) · **LA CARGA MUERE CON SU DUEÑO** (`CortarCarga` + `_cargaSlot`) · **LA REGLA DE LA CAÍDA LONGITUDINAL** (§5.4 — la espina de la Ira/Partidor vivía al revés) · **LA PARIDAD DE LAS RUNAS** (§4.24 — `L_B = −(−1)^num₄` + direction + gemelas compartiendo semilla) · los `.fx` fuera del .tmod (buildIgnore) | §5.4, §4.24, §7.3, §3.1 |
| **v6.56.6** | **LA PURGA + LAS 9 VARIANTES + EL RC2 «desnudo»** (coronas rúnicas al 53 %, el disco negro y la lente MUERTOS, los soles de 4 puntas de la carga) + **LA AUDITORÍA DE LAS 33 LIBRERÍAS** (3 fusiones recomendadas, ~40 métodos muertos, 57 copias privadas de Tint/Hash01, TiemposLib/PostFXLib para el futuro) · *el RC2 de esta versión traía LA PILA DE CAPAS — la causa de los picos, curada en la .56.7* | §5.4, §11 |
| **v6.56.7** | **LA SOLUCIÓN DE LOS PICOS (LA LEY DEL PIXEL vs EL QUAD)**: la técnica del commit 607df36 vuelve ENTERA al RC2 (strip + shader de las 4 texturas en lote aditivo + boca de 4 glows apilados + `ColorPrisma` — EL COLOR DEL MOD — y los polvos/círculos planos MUERTOS) tras el diagnóstico forense de la PILA de la .56.6 (quads poligonales + remolino binario crudo = peines de blobs; sim 1:1 + verificación visual) · **EL CONTRATO DE COORDENADAS DE VFXCore** (los soles rúnicos nacían fuera de pantalla — PANTALLA vs MUNDO) · **EL SHADER MEJORADO**: EL HALO per-pixel + LOS FILAMENTOS (auto-interferencia) + EL NÚCLEO QUE LATE + LA FRANJA PRISMÁTICA · **LA LEY DE USO DE LAS 4 TEXTURAS** (§6.A) · **LAS 9 VARIANTES PURGADAS** (la letra: «primero borrar las variantes») | §0, §5.4, §6.A, §11 |
| **v6.56.9** | **LA PILA DE CAPAS GPU-UNIVERSAL** (strips múltiples sobre la MISMA polilínea + Main.pixelShader + RampaGlow — la cura de «una sola capa»: el núcleo ocupaba el 99% del halo, no había envolvente; 3 recetas A/B/C en las COPIAS del RC2, el original intacto) · **LA IMPLOSIÓN DE SOLES** (ángulo fijo, caída radial ease-in — «sin dar vueltas») · **LA AUDITORÍA EJECUTADA**: BeamLib+BoltRenderer+HazConcentradoLib-muerta ENTERRADAS (−1.809 l), 40 métodos muertos purgados (−1.317 l), **LOS HELPERS CANÓNICOS** en VFXCore (Tint/TintAlfa/AbrirAdditive/AbrirAlpha/Capsule/RingQuad — 96 cuerpos delegados) · **TiemposLib (EL HITSTOP) + ImpactoLib (EL GOLPE)** con la ignición de cine de las copias · **LA LEY DEL CÓDICE SINCRONIZADO** (§0.6, la anti-recurrencia del desync de 6 puntos) — y **ESTE CÓDICE (v1.5)** | §0, §4.5-4.6, §4.25, §5.4, §11, AUDITORIA-LIBRERIAS.md |
| **v6.57** | **LA VUELTA DEL HAZ C (el ganador)**: **LA LEY DEL CORAZÓN DORADO** (§0 — `corazon:` parametrizado, los haces a 0: cero círculos amarillos) · **EL ALMA** (la 7ª capa: la misma RampaGlow del núcleo a TODO el ancho) · **LOS SOLES NACEN MÁS LEJOS** (320-520 px) · **EL TEMBLOR SUAVE** (`suave:` en ImpactoLib — la vía dulce de PantallaLib, 3.2 px de dobles senos; sostenido 6.5→2.6) · **LA CORONA VISTE EL HAZ** (la paleta del conjuro viajando por los tres aros) · **LA AGUJA Y LA ESTRANGULACIÓN** de `StripWidth` (punta ×0.06, muerte ×0.07 — sim 0 picos en vida Y muerte) — y **ESTE CÓDICE (v1.6)** | §0, §5.4, §11 |
| **v6.56.8** | **LA CURA DE LA BANDA PLANA + LA PALETA DEL CONJURO + LA REGLA DE LA CIRUGÍA** (el .fx vuelve al esqueleto PROBADO .55.1; los deltas: venas rojas/pulso/halo/latido) · **LA BOCA DE GLOWS APILADOS PROHIBIDA** (§0 — era EL círculo plano de las capturas; la boca = corona + soles + arcos + estrella conducto) · **`ColorConjuro`** (violeta→morado→rojo→magenta — ColorPrisma muere en el RC2) · **EL FALLBACK HERMOSO** (4 tiras suaves, sin remolino) · **LA AUDITORÍA DE LIBRERÍAS entregada** (`AUDITORIA-LIBRERIAS.md`: 2 librerías muertas, ~3.900 líneas recuperables, ~125 copias de helpers, 3 librerías nuevas propuestas — TiemposLib/PostFXLib/ImpactoLib) — y **ESTE CÓDICE (v1.4) RECONCILIADO CON LA REALIDAD** | §0, §5.4, §6.A, AUDITORIA-LIBRERIAS.md |

---

*Este archivo no entra al `.tmod` (`buildIgnore = *.md`): es documentación de desarrollo. Si un efecto del mod no está documentado aquí, eso es un BUG DE DOCUMENTACIÓN — se reporta y se cura en la misma entrega.*

# AethonMod — Aethon, la Luz Primordial

> **Mod de Terraria para tModLoader** · Repo oficial: <https://github.com/Leo0x01/Aethon-Mod-Terraria>
> **Versión actual:** 6.50.43 · **Target:** tModLoader 2026.07.3.0 (Terraria 1.4.4.9, .NET 8) · **Idioma:** es-ES + en-US

## Qué es (en 30 segundos)

Un mod de contenido "final" para Terraria cuyo sello es que **todo el arte visual es 100% código**
(cero sprites de rayos, jefes dibujados con quads del motor, destellos con degradados monotónicos).
El corazón del mod es **el Grimorio del Eterno**: un arma-híbrido con **niveles infinitos** que
gana XP con cada kill, **tiene hambre**, y cuando no lo alimentas provoca **LA FURIA** — un evento
de oleadas estilo Pumpkin/Frost Moon con jefes guardianes que escalan ×(oleada+1). Alrededor:
**6 jefes propios**, **12 esencias de jefe** (devorarlas = +1 nivel), **~115 armas de prueba en
18 bolsas**, y **el Testigo** (NPC cronista/tienda). Los rayos son el puerto 1:1 del
`LightningGenerator` de vanilla 1.4.5 (el sistema del clima y del arma Arc Surge).

## ¿Dónde estamos? (actualizado 2026-09-30, v6.50.43)

- **GitHub es la FUENTE DE LA VERDAD** — local == remoto (tag `v6.50.43`,
  release con `AethonMod.tmod` adjunto y verificado byte a byte).
- Build headless **0 errores / 0 warnings** contra tML 2026.07.3.0 real; servidor headless carga
  sin excepciones; `.tmod` de 395 entradas auditado.
- **v6.50.43 — EL LLAMADO A CUALQUIER HORA (y el Verdugo que ya no se
  gasta)**: «el jefe no puedo invocarlo de noche… no tiene sentido eso ya
  que al invocar el jefe el tiempo pasa hasta que el sol está en el centro
  del cielo, así que no importa la hora de invocarlo» — EL NOMBRE DE
  AETHON ya se puede usar **DE NOCHE** (su llegada corre el reloj hasta el
  próximo mediodía: la noche entera pasa en timelapse y el sol SE POSA en
  el centro); los otros cuatro invocadores siguen siendo de día (su
  presencia no mueve el reloj). Y el **Verdugo de Niveles** ya NO es
  consumible: cada uso suma **+10 niveles** al Grimorio del Eterno **sin
  consumirse** (el patrón de la Carnada).
- **v6.50.42 — EL JEFE QUE NO APARECÍA (y el sol que no se quedaba fijo)**:
  «el sol avanza como está previsto, pero al llegar al centro no queda fijo
  en el centro y el jefe no aparece» — DOS síntomas, **UNA sola causa**,
  cazada y VERIFICADA en servidor headless (la llegada completa corre en
  el CI con un truco de cliente-fantasma que enciende el loop del
  servidor): la materialización de la v6.50.41 usaba la matemática
  pantalla→mundo **en la máquina que corre la IA**, y el servidor no tiene
  pantalla (`screenWidth=0`, matrices identidad) → el jefe nacía en
  **(0, ~5516), FUERA DEL MUNDO, y MORÍA** al materializarse → el espejo
  soltaba el reloj → el sol seguía su curso. AHORA
  `PosicionBajoElSol` es **server-segura** (sobre el jugador — la cámara
  lo centra: «bajo el sol» es el cielo de SU pantalla), exacta en cliente,
  **NUNCA enterrada** (mínimo 300 px sobre el jugador) y **NUNCA fuera del
  mundo** (clamp a los límites). Además **EL CERROJO DEL MEDIODÍA**: con
  el climax/la pelea vivos, un reloj que se pasó de 27001 VUELVE
  activamente a 27000 — jamás la vuelta entera a 110×. **Validación
  empírica completa**: carrera → aterrizaje 27000 exacto → materialización
  visible (−420 px sobre el jugador) → fade → pelea de 360 t con
  `time=27000.00` y `rate=0.00` congelados → deriva inyectada (32400)
  curada al 27000 en el mismo tick → reanudación del reloj al irse el
  jefe.
- **v6.50.41 — EL MEDIO DÍA DEL DESTELLO** (cuatro pedidos en uno):
  (1) **LA CAPA DE OSCURIDAD MUERE DE RAÍZ** — «mejor quita la capa de
  oscuridad, no se ve nada bien, se ve horrible»: VeloLib (el velo bajo la
  interfaz con el mosaico de agujeros), **EL SOL NEGRO**, las luces del
  frame, el aviso del Grimorio y el flash blanco de pantalla completa
  BORRADOS — la llegada queda LIMPIA: temblor → carrera del sol → destello
  → descenso. (2) **EL SOL SIN TELETRANSPORTE DE VERDAD** — el bug de la
  v6.50.40 cazado: con el sol en la TARDE el aterrizaje se disparaba al
  INSTANTE y el sol saltaba HACIA ATRÁS al mediodía; ahora la ventana de
  aterrizaje es [26999, 27001] y la carrera mide EL RESTANTE hasta el
  PRÓXIMO mediodía POR LA NOCHE: **tarde → ocaso → NOCHE COMPLETA →
  amanecer → mañana → mediodía** («si está más allá del centro, un día
  completo avanza con noche completa, un nuevo día hasta el amanecer» —
  simulado en 5 escenarios, jamás salta hacia atrás; ~12-14 s el peor caso,
  el sol y la luna ATRAVIESAN el cielo visiblemente). Si está ANTES del
  centro: directo al centro con aterrizaje desacelerado. (3) **EL DESTELLO
  NACE DEL SOL**: un brillo radial CENTRADO en el sol que crece hasta
  inundar la pantalla y muere TRANSPARENTE justo en los bordes (el
  degradé termina donde termina la pantalla) — y **EL SOL NO SE APAGA**:
  sigue ahí, vivo, ardiendo bajo el destello. (4) **AETHON YA NO NACE DEL
  CENTRO DEL SOL**: se materializa BAJO él, en el borde inferior de su
  halo (conversión espacio-del-fondo → mundo vía la INVERSA de la matriz
  de vista), envuelto en el pico del destello.
- **v6.50.40 — LA OSCURIDAD BAJO LA INTERFAZ, CON AGUJEROS DE LUZ**
  (RETIRADA en v6.50.41 por petición): «la capa de oscuridad no debe estar
  sobre todo, la capa debe estar por debajo de la interfaz de usuario» +
  «no debe cubrir ni al jugador ni al jefe»: el velo pasó a ser LA PRIMERA
  CAPA DE LA INTERFAZ y EL MOSAICO DISJUNTO abrió los agujeros de luz
  (Aethon 780 px entero, el círculo del jugador 235 px solo con Grimorio
  ≥50, las balas 88/130) — verificado por simulación, sin costuras ni
  dobles. La técnica quedó documentada en CHANGES.md para futura
  referencia, pero YA NO VIVE en el mod.
- **v6.50.39 — LA TÉCNICA DE WRATH OF THE GODS**: tres pedidos en uno.
  (1) «quitemos ese sistema… en su lugar revisa como lo hace el mod
  wrath of the gods, y crea una libreria para eso»: ingeniería inversa
  del addon (código público) — su oscuridad NO es una máscara: es UN
  VELO dibujado sobre el frame terminado (`Main.OnPostDraw`) y las
  cosas que deben verse se dibujan DESPUÉS, en el mismo lote. La
  máscara de luz MUERE y nace **VELOLIB**, la librería de la oscuridad
  estilo WotG (`Velo.Ver/Apagar/Luz/SobreElVelo/PintoresSiempre`):
  el velo violeta-negro al 93%, Aethon ardiendo dorado encima (430 px;
  210 violeta en su eclipse), el círculo del jugador SOLO con Grimorio
  ≥50, las balas visibles, EL FLASH blanco del climax como crossfade
  vivo, el sol negro y los telegraphs por el pintor. (2) «el sol no
  debe saltar… correr el tiempo hasta llegar a su posición de forma
  natural»: el corte al alba y el snap ELIMINADOS — la noche entera
  corre a 300× (la luna barre, el alba llega sola) y el mediodía se
  reacha con aterrizaje desacelerado: el sol se POSA, no se teletransporta.
  (3) «al compilar en tmodloader el juego se cierra»: la clase de riesgo
  entera muere con la máscara — cero GraphicsDevice, cero render targets,
  cero capas de interfaz; cerrojo de tres caídas y TODO escrito en el log.
- **v6.50.38 — LA OSCURIDAD ENFOCADA**: «la oscuridad solo hace que la
  pantalla se apague» — CORREGIDO. La máscara de luz aplicaba la
  transformación de zoom DOS VECES (los agujeros ya estaban en píxeles de
  dispositivo y el quad de estampado volvía a pasar por ZoomMatrix): con
  zoom 100% coincidía por casualidad, pero Terraria FUERZA zoom > 1 en
  pantallas grandes (1440p = 1.33×, 4K = 2×) y los agujeros de luz volaban
  fuera de la pantalla — quedaba el apagón plano. Ahora la máscara se
  estampa con IDENTIDAD sobre el viewport: **Aethon dorado, el círculo del
  Grimorio ≥50 y las balas brillan EN SU SITIO a cualquier zoom**. De yapa:
  el render target se devuelve en un `finally` blindado (una excepción ya
  no puede amarrar la máscara al dispositivo y matar la pantalla) y los
  fallos se ESCRIBEN en el log (si algo rompe, cae al velo simple y el log
  lo cuenta).
- **TODO lo acumulado está IMPLEMENTADO** (ver STABLE-SNAPSHOT.md §"Pendiente de verificación"):
  dado del 1% de Deerclops, diálogos de devorar por jefe, libro sin rugido, avalancha de Aethon,
  arte del jefe en código (eclipse estelar), motor de oleadas de vanilla + indicador, muerte del
  destello circular plano, brillo de rayos sin cortes.
- **v6.50.37 — EL MEDIO DÍA DE LA OSCURIDAD**: «has que sea mas grande el
  jefe… cuando Aethon aparece el mundo debe temblar… si es de noche se hace
  de dia y si es de dia el tiempo avanza hasta que el sol quede centrado…
  el sol brilla con intensidad y de ahi aparece Aethon, luego el sol se
  vuelve negro… toda la luz ha sido concentrada en un lugar… la oscuridad
  misma toma el control». EL JEFE ×1.5 (hitbox 220, núcleo 130 px) y LA
  LLEGADA DEFINITIVA EN CUATRO ACTOS: EL MUNDO TIEMBLA (kicks 4→13 px) →
  EL TIEMPO CORRE a 240× (el sol atraviesa el cielo) hasta quedar CLAVADO
  EN EL CENTRO (congelado en el mediodía exacto) → EL SOL BRILLA hasta lo
  cegador… EL FLASH BLANCO… y AETHON NACE DE ÉL → EL SOL SE VUELVE NEGRO
  («TODA LA LUZ HA SIDO CONCENTRADA EN UN LUGAR — SOLO ÉL BRILLA»). Y LA
  OSCURIDAD PRIMORDIAL (la de Don't Starve, MEJORADA): máscara de luz
  multiplicada sobre el mundo — todo se apaga salvo AETHON (1.060 px de
  luz dorada pura) y el PEQUEÑO círculo del jugador… SOLO con Grimorio
  nivel 50+. Las balas del jefe brillan en la negrura; el HUD sigue
  usable. Al morir: la luz ESTALLA, el sol recupera su curso.
- **v6.50.36 — AETHON, LA LUZ PRIMORDIAL, LA ENCARNACIÓN**: la sierpe MUERE
  («el jefe se ve feo… mejor hacerlo una luz brillante») y el jefe ES LA LUZ
  MISMA: un SOL VIVO de código puro (núcleo blanco + halo dorado + rayos
  radiales + coronas de perlas + chispas) con SEIS ataques devastadores:
  EL JUICIO DE LUZ (columnas del cielo), EL RAYO PRIMORDIAL, LA NOVA con
  huecos, LA CRUZ giratoria (P3+), EL DESTELLO encadenado (P3+) y EL
  ECLIPSE (P4+: la luz se apaga, solo las balas brillan — y vuelve con
  nova). La llegada: EL CIELO SE ENCIENDE. Y el género de Aethon corregido:
  ÉL (el título "La Luz Primordial" queda).
- **v6.50.35 — EL SEÑOR DEL MUNDO (corrección de género)**: «la sierpe es macho
  así que sería señor del mundo» — el título corregido en TODOS los frentes y
  ESTRENADO EN EL JUEGO: el anuncio de aparición del jefe ahora dice «LA SIERPE
  DE HUESO DE LA LUZ — EL SEÑOR DEL MUNDO — se alza del subsuelo» (es-ES) /
  «THE LORD OF THE WORLD» (en-US); "her line" → "his line" en la embestida
  en-US; 7 comentarios de código (Señora→Señor, Diosa→Dios) y el release
  v6.50.34 renombrado en GitHub.
- **v6.50.34 — LA SIERPE ESTELAR, SEÑOR DEL MUNDO**: el dragón de sprites
  BORRADO por decreto («se ve horrible») y la sierpe estelar de la v6.50.27
  recuperada como jefe — pero MASIVA (ESC 1.85, 68 vértebras, ~5.700 px de
  columna) y con LA IA MEJORADA: el clavado aéreo, la rotación que nunca repite
  ataque, el ram en cadena del DoG, la predicción adaptativa, el anti-camping
  y la furia de fase 5 con aliento doble.
- **v6.50.33 — EL DRAGÓN DEL CIELO, ENCARNACIÓN SPRITE** (borrada): el jefe rediseñado
  COMPLETO con **sprites por segmento** (la petición: como el Devourer of Gods
  de Calamity): set de 7 sprites generado por código (tools/gen_slifer_sprites_v6533.py,
  5 rondas de QA con visión artificial) — cabeza con máscara plateada + colmillos
  sable + corona de 5 llamas + ojo de oro + gema azul, **mandíbula giratoria con
  LA SEGUNDA BOCA**, anillos escamados con curva cuello→torso→punta, alas de
  murciélago, cola espatulada; **el bug de las alas pegadas al Guía, fixeado de
  raíz** (la IA sobrescribía el puntero de cadena con el estado → Main.npc[0]).
- **v6.50.32 — el intento 100 % código de Slifer** (veredicto: «no se parece en
  nada» — sustituido por los sprites en v6.50.33).
- **v6.50.31 — FIXES FORENSES del client.log del usuario** (verificados: "bien, ya
  no hay errores"): la pareja de
  «Excepción silenciosa» del jefe Aethon (Begin-sobre-Begin de FNA — la garganta
  ardiendo, la corona y el arco del aliento JAMÁS se dibujaban en pelea), la
  ThreadStateException + leak de GPU en cada salida de mundo (funeral de texturas
  al hilo principal) y la simetría hjson (18 claves activadas y traducidas en es-ES).
- **Falta: verificación EN JUEGO por el usuario** de toda la cadena v6.50.24→v6.50.33.

## Mapa de documentación (qué leer según qué necesites)

| Archivo | Contenido | Para quién |
|---|---|---|
| **STABLE-SNAPSHOT.md** | Estado exacto de v6.50.33: qué se entregó, qué falta verificar en juego, doc-rot conocida, próximos pasos | RETOMAR TRABAJO — leer primero |
| **DISEÑO_DEL_MOD.md** | Arquitectura COMPLETA de todos los sistemas (grimorio, hambre, furia/oleadas, esencias, Testigo, voz del libro, jefes, pipeline de rayos, stack VFX, red, persistencia) con mapa de archivos | Entender CÓMO funciona algo |
| **CARACTERISTICAS.md** | Inventario total de contenido: ~115 armas por familia, 18 bolsas, jefes, minions, buffs, cosméticos, sistemas, conteos | Saber QUÉ existe |
| **COMPILACION.md** | Cómo instalar/compilar/actualizar (usuario) + pipeline de build headless y release (desarrollo) | Compilar / publicar |
| **CHANGES.md** | Historial detallado versión a versión (150 entradas ricas, hasta v6.50.33) | Historia / qué cambió |

## Protocolo para retomar el trabajo (sesiones IA / humanos)

1. **Verificar GitHub ANTES de actuar**: `git fetch origin && git rev-list --left-right --count main...origin/main` — el sandbox puede resetearse y quedar atrás. GitHub manda.
2. **Leer el worklog** (`/home/z/my-project/worklog.md` en el entorno de desarrollo): contiene el historial de sesiones (R1…R64 + exploraciones 3-a…3-d) con decisiones y trampas documentadas.
3. **Leer STABLE-SNAPSHOT.md** de este repo: pendientes de verificación en juego + doc-rot.
4. **Entorno de build**: `/tmp/tml` (tModLoader 2026.07.3.0 re-descargable), `/tmp/sdk` (dotnet SDK 8.0.404), `/home/z/.verify/verify.csproj` (los 297 `.cs` con las 9 referencias). Detalle completo en COMPILACION.md §"Pipeline de verificación de la casa".
5. **Reglas de la casa** (no negociables): cero sprites de rayo (todo `RayoLib`/`RayoStrip`); destellos SIEMPRE con degradado monotónico (nunca círculos planos); color premultiplicado en lotes aditivos; `Hash01` determinista (cero `Main.rand` en render); convención del lote del llamador; hjson es-ES y en-US SIMÉTRICOS siempre.
6. **Al terminar**: build 0/0, auditoría del `.tmod`, commit con mensaje detallado, tag, push, GitHub release con el `.tmod` adjunto, entrada en worklog, y este README/STABLE-SNAPSHOT actualizados.

## Estructura del repo

```
Aethon-Mod-Terraria/           ← raíz del repo (docs y herramientas, FUERA del mod)
├── AethonMod/                 ← EL MOD (esto es lo que se compila/empaqueta)
│   ├── build.txt              # ← ¡AQUÍ vive la versión! (6.50.33)
│   ├── AethonMod.cs           # Punto de entrada + guardián de identidad de carpeta
│   ├── Content/               # 296 .cs: Items, Weapons, NPCs, Projectiles, VFX, Systems…
│   └── Localization/          # es-ES / en-US (hjson simétricos, ~2290 líneas c/u)
├── README.md / COMPILACION.md / CHANGES.md / CARACTERISTICAS.md / DISEÑO_DEL_MOD.md / STABLE-SNAPSHOT.md
├── ACTUALIZAR-FUENTE.bat / actualizar-fuente.sh   # repo → ModSources (usuario final)
├── _masters/                  # Sprites maestros de referencia (NO del mod)
└── tools/                     # Generadores de assets y mocks VLM (desarrollo)
```

`build.txt` lleva `buildIgnore = *.md, *.py, *.sh, research/*, tools/*, _masters/*` —
los docs del repo NUNCA entran al `.tmod`.

---

## AVISO LEGAL — TODOS LOS DERECHOS RESERVADOS

**NO está permitido copiar, tomar, usar, modificar, redistribuir, republicar, extraer ni reaprovechar NADA de este repositorio** — ni el código, ni los assets, ni las texturas, ni las ideas de implementación, ni parte alguna del proyecto — ya sea en su totalidad o en fragmentos, con o sin cambios, para uso personal, público o comercial.

- ❌ **Prohibido copiar** el código o cualquier archivo de este proyecto.
- ❌ **Prohibido tomar** el código (total o parcialmente) para otro proyecto.
- ❌ **Prohibido modificar** el código y publicar o distribuir versiones derivadas.
- ❌ **Prohibido redistribuir** o re-subir este proyecto o partes de él.
- ❌ **Prohibido** usar los assets, texturas, efectos o cualquier contenido del proyecto fuera de él.

Este proyecto es propiedad exclusiva de su autor. Cualquier uso no autorizado constituye una violación de estos términos.

© 2026 AethonModTeam. All rights reserved.

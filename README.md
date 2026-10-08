# AethonMod — Aethon, el Grimorio Eterno

> **Mod de Terraria para tModLoader** · Repo oficial: <https://github.com/Leo0x01/Aethon-Mod-Terraria>
> **Versión actual:** 6.50.76 · **Target:** tModLoader 2026.08.3.0 (Terraria 1.4.4.9, .NET 8) · **Idioma:** TODAS las variantes de español caen en el es-MX (es-ES es un espejo GENERADO de es-MX — v6.50.71) + en-US

## Qué es (en 30 segundos)

Un mod de contenido "final" para Terraria cuyo sello es que **todo el arte visual es 100% código**
(cero sprites de rayos, jefes dibujados con quads del motor, destellos con degradados monotónicos).
**EL LORE (v6.50.61)**: el **Grimorio del Eterno** que cargas NO es un arma: **ES AETHON**, una
entidad primigenia atada en forma de libro. Come cada kill, sube de nivel, **tiene hambre** — y
cuando pronuncias su nombre, el libro **SE ABRE y se alza de sus propias páginas** para probarte:
**AETHON, EL GRIMORIO ETERNO**, el jefe final de 5 fases, **LA PRUEBA DEL PROPIO GRIMORIO**.
Vence la prueba y el libro te declara digno (La Forma Ascendida cae a tus pies). Alrededor:
**LAS OLEADAS DEL HAMBRE** (la furia del libro: oleadas estilo Pumpkin/Frost Moon con jefes
guardianes que escalan por nivel y bioma), **las esencias** (devorarlas = +1 nivel) y
**~114 armas de prueba en 19 bolsas**. Los rayos son el puerto 1:1 del `LightningGenerator`
de vanilla 1.4.5 (el sistema del clima y del arma Arc Surge).

## ¿Dónde estamos? (actualizado para v6.50.76)

**NOVEDAD .76 — LA LIMPIEZA DEL FONDO ROTO + EL SOL ECLIPSE PRESTADO**: el
usuario detectó el fondo de las cuevas ROTO (void negro + shards magenta).
Diagnóstico contra el decompile: el estilo de subsuelo del Sagrario (la
librería de fondos v6.43) metía paisajes de cielo 1024×256 en slots que
exigen miniaturas 160×16/160×96 — y el bioma estaba activo en TODAS las
cuevas. LA LIBRERÍA COMPLETA FUE ELIMINADA (CieloLib, escenas, estilos,
Prisma de Paisajes y 15 texturas): **el fondo de las cuevas vuelve a ser
100% vanilla**. Y el sol eclipse de la oleada ahora es **EL ORIGINAL DEL
JUEGO** (`Sun3`, el de los eclipses reales) prestado por referencia pura —
mismo sprite, mismo tamaño — con el tinte morado aplicado por el hook
nativo del color de dibujo; la luna queda **original, solo teñida** de
carmesí-morado. **La .75 trajo EL RELOJ DE ARENA DEL ESCRIBA (un clic: día ⇄ noche)**: la
letra del usuario: «deberías incluir algo para cambiar el día y la noche,
un clic y es de día, otro clic y es de noche». Un ítem de pruebas nuevo en
**La Bolsa del Probador**: UN CLIC gira el reloj del mundo — si es de día,
**cae la noche** (7:30 PM); si es de noche, **amanece** (4:30 AM).
Reutilizable (nunca se consume) y con la arena hecha **TINTA** (la del
Códice Vivo: charco arriba, hilo en la cintura, montón creciendo abajo —
el sprite pixel-art 28×28 nacido de código). **La sinergía**: funciona
incluso A MEDIA OLEADA — el cielo del festín (v6.50.74) es dinámico, así
que girar día/noche a mitad de un festín cruza suave entre el eclipse
morado y la luna carmesí: la vía rápida para ver las DOS caras del festín
sin esperar al atardecer real. En MP la hora es del mundo: el servidor
gira el reloj y lo difunde a todos al instante (nuevo mensaje de red
`MsgCambiarHorario`), con el aviso del escriba en el chat y lluvia de
tinta (dorada al amanecer / violeta al anochecer) alrededor de quien lo
giró.

**NOVEDAD .74 — EL CIELO DE LA OLEADA (ECLIPSE+CEMENTERIO+MORADO) + LA CURA
TRIPLE DEL CÓDICE Y EL NIVEL**: la letra del usuario: «la imagen [del libro]
esta cortada ademas se mueve de abajo hacia arriba… el ataque no se ve y
salta un error… la oleada natural siempre empieza por el nivel 3… que tal
si combinas el ambiente de el eclipse solar mas el ambiente de cementerio,
le das un toque morado a la iluminacion naranja del eclipse… un nuevo
estado… mientras es de dia; si pasa a la noche, una mezcla de lunar de
sangre mas el cementerio… el sol debe cambiar su sprite y la luna roja
tambien… dinamicos… se acaban cuando la oleada termine». **EL CIELO DE LA
OLEADA** (AmbienteOleadaSistema, un ESTADO NUEVO que no toca ni el eclipse
real ni la luna de sangre real ni el cementerio real): **LA LUZ** por el
hook oficial `ModifySunLightColor` — de DÍA eclipse+cementerio+**toque
morado** sobre el naranja; de NOCHE luna de sangre+cementerio+el mismo
morado — **LA NIEBLA DEL CEMENTERIO** (filtro de niebla, oscuridad,
estrellas apagadas y relámpago esporádico, todo desde un solo campo
visual) — **LOS ASTROS**: swap reversible con **EL SOL ECLIPSADO** (disco
violeta + anillo de fuego naranja + llamaradas moradas) de día y **LA LUNA
CARMESÍ** (roja, cráteres, cara oculta FANTASMA, halo violeta, 8 fases
como vanilla) de noche — **DINÁMICO**: atardecer/amanecer cruzan suave
dentro de una MISMA oleada y todo muere con el festín (fade + astros
devueltos) — y en MP el estado viaja por BROADCAST a todos los clientes.
**LA CURA TRIPLE**: (1) el icono del Códice estaba cortado y "subía" porque
el constructor de la animación lleva **TICKS PRIMERO** — `(8,6)` partía el
strip en 6 rodajas de 64 px; ahora `(6,8)` = 8 frames exactos; (2) el
ataque no se veía por un `spriteBatch.Begin` sobre el lote vivo de vanilla
(excepción todos los frames) — ahora dibuja EN el lote; (3) la oleada
natural nacía en nivel 3 porque las victorias de la CARNADA de pruebas
también subían el `FuriaNivel` persistente — ahora SOLO las victorias
naturales suben, la carnada no toca nada y los guardados viejos se
resetean a 1 una única vez.
**NOVEDAD .73 — LA OLEADA ES SU PROPIO AMBIENTE (IA DE MODO MAESTRO) + EL
OJO QUE YA NO SE DESPEGA**: la letra del usuario: «tanto el Ojo de Cthulhu
como otros jefes, en las oleadas no tienen limitacion de dia, noche o
bioma, la oleada es su propio ambiente ya que son jefes modificados…
en niveles altos el ojo se separa mucho del jugador… a cada jefe de
oleada dale la IA que usa Terraria en el modo maestro, pero con las
mejoras actuales que tiene». **EL AMBIENTE PRESTADO**: durante el AI de
cada guardián el mundo se viste de SU ambiente y se devuelve al terminar
el tick — **IA DE MODO MAESTRO** para los SEIS + Deerclops (en vanilla la
IA maestra ES la rama experta; se prestan los overrides de
experto+maestro por reflexión: huesos y +25 de defensa por mano del
Skeletron, ilusiones del Cerebro, limos con púas del Rey, manos de sombra
de Deerclops, acelerones del Ojo…), **NOCHE PERPETUA** para el Ojo y
Skeletron (ya no huye del sol ni despierta el guardián de 9999 de
defensa — nacen y pelean a CUALQUIER hora, y el Ojo vuelve al sorteo de
día), **JUNGLA** prestada a la Reina y **NIEVE** a Deerclops (sin
encorajes ni huidas por bioma), y la talla del sello ahora también en
`defDamage`/`defDefense` (las IA que se re-visten cada tick ya no borran
la oleada). **EL OJO YA NO SE DESPEGA**: fuera del homing/embite
inyectado (lo sobre-empujaba a 600-1000 px de la presa en oleadas altas)
y con **EL ANCLA** — a más de 900 px, el libro lo trae de la mano.
**NOVEDAD .72 — EL CÓDICE VIVO REHECHO + LAS 3 ARMAS BORRADAS + LAS
OLEADAS SIN JEFE REPETIDO**: la letra del usuario: «el sprite del codice
vivo esta mal hecho, solo es la imagen fija subiendo en vertical sin
animaciones ni nada… no tiene ningún ataque… las 3 armas nuevas ya no son
necesarias, solo nos quedamos con La Sombra de la Página… en las oleadas
se repiten mucho los mismos jefes». **EL CÓDICE VIVO** vuelve a nacer de
la MISMA matriz de `codigo.txt` pero animado DE VERDAD: **EL ALIENTO**
(todo el libro respira), **LA ONDA** (un anillo de tinta viva viajando
desde el ojo) y **EL PARPADEO COMPLETO** (el ojo cierra en rendija) —
8.2–36.9% de píxeles cambian por frame (la .71: 3.3–6.3%) y el VLM lo
certificó "VIVA". Al usarlo, **vuela AL PUNTO DONDE CLICAS** y ahí vela
disparando **CUATRO ANDANADAS de TRES chispas autoguiadas** — con
enemigos o SIN ellos, el ataque SIEMPRE se ve — y vuelve de bumerán.
**LAS 3 ARMAS DE LA .71 BORRADAS** (Mano/Tijeras/Página, con sus festines
11/12/13 y sus tooltips): la Bolsa de las Sombras vuelve a tener UNA sola
inaquilina — **La Sombra de la Página**, la aprobada. **LAS OLEADAS**: el
reparto por bioma MUERE — el guardián de cada oleada sale de LA PIZARRA
de los SEIS pre-hardmode **todos con la MISMA probabilidad**, y el que
salió **no repite en las DOS oleadas siguientes** (Deerclops sigue
aparte, en su dado del 1%). **NOVEDAD .71 — TODAS LAS VARIANTES DE ESPAÑOL CAEN EN LA NUESTRA + EL
TENTÁCULO FUSIFORME + LOS ÚTILES DEL ESCRIBA + EL CÓDICE VIVO**: la letra
del usuario: «tengo el juego en español, pero el juego sale en inglés» —
la .70 BORRÓ el es-ES y su juego (en «Español») caía al inglés; AHORA
**es-ES regresa como ESPEJO GENERADO de es-MX** (`tools/sync_es_es_v65071.py`):
«Español (España)» y «Español (Latinoamérica)» ven EL MISMO español
nuestro (es-MX, el idioma PRIMARIO). **EL TENTÁCULO DE LA SOMBRA cambia
de perfil**: base pegada al jugador MUY FINA → se ENGORDA al centro →
FINA otra vez en la punta que toca al enemigo — y justo en esa punta,
MÁS BRUMA (nube propia + 3 orbitantes + aliento de 6 puffs). **LAS 3
ARMAS REDISEÑADAS DE CERO** (el Azote/Mordida/Cría jubilados): **LA MANO
DEL ESCRIBA** (la garra colosal que CAMINA sobre sus dedos de araña, con
un ojo rasgado en la palma — agarra, aprieta y HUNDE al reo en el
charco; festín 11 = EL PUÑO DEL ESCRIBA), **LAS TIJERAS DE LA PÁGINA**
(dos hojas curvas con filo de hueso que TIJERETEAN rebanadas de la
realidad; festín 12 = EL CORTE FINAL) y **LA PÁGINA ARRANCADA** (el marco
de hoja de cuaderno — renglones, margen rojo y folio — que ciñe, tiembla
y ARRRANCA al reo de la página; festín 13 = EL ARREBATO). **EL CÓDICE
VIVO** (la prueba del usuario, COMPLETAMENTE A PARTE): un sprite NACIDO
DE CÓDIGO (su matriz de píxeles, 26 colores) convertido en arma ANIMADA
— 8 frames con el pulso de la tinta y EL PARPADEO del ojo, el icono
ANIMA EN EL INVENTARIO y el códice VUELA disparando chispas autoguiadas.
**NOVEDAD .70 — EL ESPAÑOL ESPAÑA BORRADO**: el borrado fue REVERTIDO en
la .71 (el propio juego del usuario vive en «Español» = cultura es-ES);
la simetría 771/771 de claves y la purga de anglicismos de la .70 siguen
vigentes.
**NOVEDAD .69 — LA COBERTURA TOTAL + EL ACTO DE DEVORACIÓN EN BRUMA +
LA FAMILIA DE TENTÁCULOS**: la bruma del tragado cubre al jefe **ENTERO**
(la elipse real de su hitbox — al Rey Slime completo, corona y pies);
cuando al jefe le queda **1 de vida** el tentáculo GIGANTE sale del
jugador, ENVUELVE al jefe (su sprite DESAPARECE bajo la bruma), lo TRAGA
(la elipse encoge hacia la punta y la corriente vuelve por el cuerpo del
tentáculo) y muere DENTRO de la bruma — **sin gore ni animación de muerte
vanilla**: el loot cae solo y la kill ES REAL (logros, flags, bestiario).
La Sombra exhala DOS capas de bruma y la marca continua dispara el
festín con el golpe mortal de CUALQUIER fuente. LAS 3 ARMAS NUEVAS SON
UNA FAMILIA DE TENTÁCULOS: **EL AZOTE** (el látigo que cruje atravesando
todo en línea), **LA MORDIDA** (el glotón que se cuelga y mastica de
verdad) y **LA CRÍA** (la camada de 3 que cazan solas orbitando al
portador) — cada una con su festín propio. **NOVEDAD .68 — LA BOCA ES
BRUMA**: la punta del tentáculo ES una cabeza de bruma (boca y tentáculo
son un solo cuerpo, nace del jugador) y al morder la masa de bruma negra
y espesa cubre la totalidad del jefe con las almas volviendo al portador.
- **v6.50.61 - LA PURGA DE NPC + EL VIGÍA + EL LIBRO ERA EL JEFE**:
  (1) **BORRADOS TODOS LOS NPC DEL MOD salvo el jefe principal**: el
  Titán Hueco, el Guardián del Rift, la Arquera Estelar, el Primer
  Portador y **El Testigo** murieron con sus ítems de llamado, sus
  esencias, su tienda, su sistema de presencia y sus ~90 líneas de
  localización. SOLO QUEDA AETHON (y su manada — el Cazador Astral es
  un ATAQUE del jefe, no un NPC independiente).
  (2) **EL LORE NUEVO**: **Aethon ES el propio grimorio** — el arma que
  come es el jefe final; la pelea es LA PRUEBA del propio libro (la
  fase 5 pasa a llamarse «El Veredicto»); su esencia es la página que
  el libro se arranca al caer. Mod renombrado: «Aethon, el Grimorio
  Eterno» (antes «la Luz Primordial»).
  (3) **EL VIGÍA DEL FESTÍN** (fix de oleadas): en el SPAWN ORIGINAL del
  mundo (donde viven los NPC del pueblo) el motor natural de spawns de
  Terraria queda **BLOQUEADO POR COMPLETO** (verificado al IL del
  decompile: 1+ townNPCs cerca = cero spawns) — la oleada «no traía
  monstruos hasta que salías del spawn». AHORA un vigía cuenta la
  chusma viva: si el festín ayuna (<3 vivos tras 2,5 s), el propio
  sistema SIRVE la comida del pool de la oleada en cunas limpias
  (PosicionLimpia) — la oleada llega SIEMPRE, en el pueblo o en el
  desierto.

- **v6.50.60 - LA DECIMASÉPTIMA RONDA**:
- **v6.50.60 - LA DECIMASÉPTIMA RONDA — EL ATAQUE ESPECIAL DE CADA FASE +
  EL SOL DE VERDAD + LA CUNA LIMPIA + LA NOCHE DEL GRIMORIO**:
  (1) **UN ESPECIAL POR FASE, Y LA FIRMA ABRE LA FASE**: P1 **EL SOL DEL
  DIOS** (¡además es la APERTURA de toda pelea — lo primero que hace el
  dios al aparecer!), P2 **LA CATEDRAL DEL TIEMPO** (el reloj permanente),
  P3 **EL TELAR** (la jaula estrella), P4 **EL VÓRTICE** (la galaxia que
  colapsa), P5 **LA CORONA** (el prisma de la furia). El «no hace nada
  de lo que tiene que hacer» murió: el arsenal grande estaba encerrado
  en fase 3+ (960k de daño para verlo) — ahora cada fase ARRANCA con su
  gala, y EL SOL está en el menú de TODAS las fases.
  (2) **EL SOL DEL DIOS, DE VERDAD**: cañonazo de 11 px/t, caza
  imparable (giro 0.11, crucero 10.5 — más rápido que toda carrera,
  relevo 14 si te alejas) y **ESPOLETE DE PROXIMIDAD**: a <240 px de ti
  la GIGANTE se hincha ×6 y DETONA cerca (antes explotaba a los 380 t
  SIEMPRE, lejísimos — simulado por la casa: botas→270 px, montura→el
  filo del radio). Nunca más estalla en el vacío.
  (3) **LA CUNA LIMPIA**: los monstruos de la oleada JAMÁS nacen entre
  muros ni bajo tierra — el escupitajo del Devorador (la causa real:
  nacía DENTRO del terreno donde vive el gusano) y TODAS las
  convocaciones pasan por PosicionLimpia (aire 3×3 validado, sin muro en
  superficie, sobre tu suelo); y la capa de tierra ya no es tierra de
  nadie: nada nace más de 10 tiles bajo tus pies.
  (4) **LA NOCHE DEL GRIMORIO**: la furia sale día Y NOCHE, SIEMPRE que
  el libro tenga hambre — la defensa de medianoche (zombies/ojos
  naturales) YA NO alimenta el libro (era la causa: cada kill nocturna
  borraba el hambre), el libro furioso ya no se calma con bocados, y hay
  reintento cada 5 s: mundo libre = festín YA.

## Mapa de documentación (qué leer según qué necesites)

| Archivo | Contenido | Para quién |
|---|---|---|
| **STABLE-SNAPSHOT.md** | Estado exacto de la versión actual: qué se entregó, qué falta verificar en juego, doc-rot conocida, próximos pasos | RETOMAR TRABAJO — leer primero |
| **DISEÑO_DEL_MOD.md** | Arquitectura COMPLETA de todos los sistemas (grimorio, hambre, furia/oleadas, esencias, voz del libro, jefes, pipeline de rayos, stack VFX, red, persistencia) con mapa de archivos | Entender CÓMO funciona algo |
| **CARACTERISTICAS.md** | Inventario total de contenido: ~115 armas por familia, 18 bolsas, jefes, minions, buffs, cosméticos, sistemas, conteos | Saber QUÉ existe |
| **COMPILACION.md** | Cómo instalar/compilar/actualizar (usuario) + pipeline de build headless y release (desarrollo) | Compilar / publicar |
| **CHANGES.md** | Historial detallado versión a versión (150 entradas ricas, hasta v6.50.33) | Historia / qué cambió |

## Protocolo para retomar el trabajo (sesiones IA / humanos)

1. **Verificar GitHub ANTES de actuar**: `git fetch origin && git rev-list --left-right --count main...origin/main` — el sandbox puede resetearse y quedar atrás. GitHub manda.
2. **Leer el worklog** (`/home/z/my-project/worklog.md` en el entorno de desarrollo): contiene el historial de sesiones (R1…R64 + exploraciones 3-a…3-d) con decisiones y trampas documentadas.
3. **Leer STABLE-SNAPSHOT.md** de este repo: pendientes de verificación en juego + doc-rot.
4. **Entorno de build**: `/tmp/tml` (tModLoader 2026.07.3.0 re-descargable), `/tmp/sdk` (dotnet SDK 8.0.404), `/home/z/.verify/verify.csproj` (los 297 `.cs` con las 9 referencias). Detalle completo en COMPILACION.md §"Pipeline de verificación de la casa".
5. **Reglas de la casa** (no negociables): cero sprites de rayo (todo `RayoLib`/`RayoStrip`); destellos SIEMPRE con degradado monotónico (nunca círculos planos); color premultiplicado en lotes aditivos; `Hash01` determinista (cero `Main.rand` en render); convención del lote del llamador; hjson es-MX y en-US SIMÉTRICOS siempre (y es-ES = espejo generado de es-MX tras cada edición — tools/sync_es_es_v65071.py).
6. **Al terminar**: build 0/0, auditoría del `.tmod`, commit con mensaje detallado, tag, push, GitHub release con el `.tmod` adjunto, entrada en worklog, y este README/STABLE-SNAPSHOT actualizados.

## Estructura del repo

```
Aethon-Mod-Terraria/           ← raíz del repo (docs y herramientas, FUERA del mod)
├── AethonMod/                 ← EL MOD (esto es lo que se compila/empaqueta)
│   ├── build.txt              # ← ¡AQUÍ vive la versión! (6.50.61)
│   ├── AethonMod.cs           # Punto de entrada + guardián de identidad de carpeta
│   ├── Content/               # ~290 .cs: Items, Weapons, NPCs, Projectiles, VFX, Systems…
│   └── Localization/          # es-MX (PRIMARIO) / en-US / es-ES (espejo GENERADO de es-MX — todas las variantes de español)
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

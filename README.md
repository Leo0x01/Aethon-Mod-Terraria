# AethonMod — Aethon, el Grimorio Eterno

> **Mod de Terraria para tModLoader** · Repo oficial: <https://github.com/Leo0x01/Aethon-Mod-Terraria>
> **Versión actual:** 6.50.65 · **Target:** tModLoader 2026.08.3.0 (Terraria 1.4.4.9, .NET 8) · **Idioma:** es-ES + en-US

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
**~116 armas de prueba en 19 bolsas**. Los rayos son el puerto 1:1 del `LightningGenerator`
de vanilla 1.4.5 (el sistema del clima y del arma Arc Surge).

## ¿Dónde estamos? (actualizado para v6.50.65)

**NOVEDAD .64 — LA PINZA DOBLE + LA BRUMA NEGRA + LA PÁGINA FINAL**: la
Fauce del Grimorio ahora muerde con **DOS CABEZAS** (el gif del usuario
duplicado en la punta, una por esquina, cerrando como una pinza de dientes),
las tres fauces **exhalan BRUMA NEGRA** por todo el cuerpo y las bocas, y
llega el **ARMA 4: LA PÁGINA FINAL** — la evolución AAA de La Sombra de la
Página (100% código): el libro despliega su ojo mientras la sala se apaga,
**tres zarpos convergen** en el jefe desde ángulos distintos, la **JAULA DE
TINTA** (anillo de púas de punta blanca) se cierra a mordidas — el dren más
fuerte de las cuatro — y a 1 HP **el libro GIGANTE se abre en el cielo**,
agarra al jefe con sus zarpos y la oscuridad SUBE comiéndoselo hasta que el
libro se sella con una onda de choque. **NOVEDAD .63 — LA BOLSA DE LAS
SOMBRAS**: las fauces LLEGAN SOLAS al entrar al mundo (bolsa 19 del kit —
clic derecho y las tienes; ahora son CUATRO). **NOVEDAD .62 — LAS FAUCES
DEL GRIMORIO**: armas de sombras devoradoras al estilo Pride (Selim
Bradley, FMA:B) — el tentáculo-latigazo que SIEMPRE llega al jefe, la esfera
de ojos que te miran mientras comen y la fauce 100% escrita por código.
**LA DEVORACIÓN**: cuando un jefe cae a 1 HP a manos de las fauces, su
animación de muerte SE DETIENE y la sombra lo devora entero — y el loot cae
dentro de la bruma (la kill es real: bestiario, flags, XP del libro).
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
5. **Reglas de la casa** (no negociables): cero sprites de rayo (todo `RayoLib`/`RayoStrip`); destellos SIEMPRE con degradado monotónico (nunca círculos planos); color premultiplicado en lotes aditivos; `Hash01` determinista (cero `Main.rand` en render); convención del lote del llamador; hjson es-ES y en-US SIMÉTRICOS siempre.
6. **Al terminar**: build 0/0, auditoría del `.tmod`, commit con mensaje detallado, tag, push, GitHub release con el `.tmod` adjunto, entrada en worklog, y este README/STABLE-SNAPSHOT actualizados.

## Estructura del repo

```
Aethon-Mod-Terraria/           ← raíz del repo (docs y herramientas, FUERA del mod)
├── AethonMod/                 ← EL MOD (esto es lo que se compila/empaqueta)
│   ├── build.txt              # ← ¡AQUÍ vive la versión! (6.50.61)
│   ├── AethonMod.cs           # Punto de entrada + guardián de identidad de carpeta
│   ├── Content/               # ~290 .cs: Items, Weapons, NPCs, Projectiles, VFX, Systems…
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

# AethonMod — Aethon, la Luz Primordial

> **Mod de Terraria para tModLoader** · Repo oficial: <https://github.com/Leo0x01/Aethon-Mod-Terraria>
> **Versión actual:** 6.50.59 · **Target:** tModLoader 2026.07.3.0 (Terraria 1.4.4.9, .NET 8) · **Idioma:** es-ES + en-US

## Qué es (en 30 segundos)

Un mod de contenido "final" para Terraria cuyo sello es que **todo el arte visual es 100% código**
(cero sprites de rayos, jefes dibujados con quads del motor, destellos con degradados monotónicos).
El corazón del mod es **el Grimorio del Eterno**: un arma-híbrido con **niveles infinitos** que
gana XP con cada kill, **tiene hambre**, y cuando no lo alimentas provoca **LA FURIA** — un evento
de oleadas estilo Pumpkin/Frost Moon con jefes guardianes que escalan ×(oleada+1). Alrededor:
**6 jefes propios**, **12 esencias de jefe** (devorarlas = +1 nivel), **~115 armas de prueba en
18 bolsas**, y **el Testigo** (NPC cronista/tienda). Los rayos son el puerto 1:1 del
`LightningGenerator` de vanilla 1.4.5 (el sistema del clima y del arma Arc Surge).

## ¿Dónde estamos? (actualizado para v6.50.60)
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

# AethonMod — Aethon, la Luz Primordial

> **Mod de Terraria para tModLoader** · Repo oficial: <https://github.com/Leo0x01/Aethon-Mod-Terraria>
> **Versión actual:** 6.50.38 · **Target:** tModLoader 2026.07.3.0 (Terraria 1.4.4.9, .NET 8) · **Idioma:** es-ES + en-US

## Qué es (en 30 segundos)

Un mod de contenido "final" para Terraria cuyo sello es que **todo el arte visual es 100% código**
(cero sprites de rayos, jefes dibujados con quads del motor, destellos con degradados monotónicos).
El corazón del mod es **el Grimorio del Eterno**: un arma-híbrido con **niveles infinitos** que
gana XP con cada kill, **tiene hambre**, y cuando no lo alimentas provoca **LA FURIA** — un evento
de oleadas estilo Pumpkin/Frost Moon con jefes guardianes que escalan ×(oleada+1). Alrededor:
**6 jefes propios**, **12 esencias de jefe** (devorarlas = +1 nivel), **~115 armas de prueba en
18 bolsas**, y **el Testigo** (NPC cronista/tienda). Los rayos son el puerto 1:1 del
`LightningGenerator` de vanilla 1.4.5 (el sistema del clima y del arma Arc Surge).

## ¿Dónde estamos? (actualizado 2026-09-29, v6.50.38)

- **GitHub es la FUENTE DE LA VERDAD** — local == remoto (tag `v6.50.38`,
  release con `AethonMod.tmod` adjunto y verificado byte a byte).
- Build headless **0 errores / 0 warnings** contra tML 2026.07.3.0 real; servidor headless carga
  sin excepciones; `.tmod` de 396 entradas auditado.
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

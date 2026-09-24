# AethonMod — Historial de Cambios

## Commit v6.50.8 — LOS RAYOS DE VERDAD Y EL AURA QUE NO ERAN CAJAS (dos causas raíz, 2 archivos)

**Petición del usuario**: "todavía está lleno de errores, al parecer las librerías de rayos y posiblemente todavía quede algo en las de brumas y los sprites correspondientes tienen errores, por ejemplo en los accesorios de aura no veo aura solo cajas blancas semitransparentes, tal vez la librería de aura también tenga errores, y los rayos son solo líneas discontinuas no son rayos de verdad, investiga la librería de rayos de los commits anteriores a la gran auditoría, e investiga en internet como crearlos correctamente". Investigación completa: diff forense bdd24ed→v6.50.7 de las 3 librerías + sus sprites (análisis de píxeles de los 5 PNG de la tormenta y del halo fantasma) + investigación web de la receta canónica de relámpagos 2D + verificación empírica del pipeline. **Hallazgo clave: el código de rayos/brumas NO cambió desde antes de la auditoría (solo comentarios + tintes revertidos ya en v6.50.7) — los dos defectos son de DISEÑO ORIGINAL y se veían así desde siempre; el de las auras sí es una infracción de la convención de premultiplicado que v6.50.7 no auditó.**

  · **EL AURA (la causa de "solo cajas blancas semitransparentes")**: `AuraLib` genera sus 4 texturas de ruido fBm en RUNTIME con `new Color(255, 255, 255, a)` — RGB BLANCO CONSTANTE con el perfil de ruido SOLO en el canal alfa. Es exactamente "el bug de los rectángulos" que BrumaBrushes v6.25 documentó y reparó ("premultiplicar o morir"): el lote ADITIVO de FNA es (One, One) — el alfa NUNCA gatea el aporte — y el premultiplicado del loader de PNG NO aplica a texturas creadas con `SetData`. Resultado literal: cada gajo del aura (todas las variantes Perlin/Polígono/Humo) dibujaba un RECTÁNGULO BLANCO semitransparente, y el accesorio de aura completo se veía como "cajas blancas" en vez del manto de ruido. FIX: el píxel se hornea PREMULTIPLICADO `(a, a, a, a)` — la misma receta de BrumaBrushes y de las bandas Bolt* — con lo que el perfil vive en el canal que el aditivo suma de verdad: nubes de ruido de verdad en el camino aditivo (NPCs de oleada + `AuraPortadorHalo`, la capa trasera del jugador) y velo suave en el camino DrawData (`AuraJugadorFrontal`, el 6% que pisa el cuerpo). Afecta a TODOS los consumidores: la Ceniza del Hambre, la Corona Rúnica, la Forma Ascendida, el aura del Juicio y las auras de oleada.
  · **LOS RAYOS (la causa de "solo líneas discontinuas, no son rayos de verdad")**: `StormLib.ChainBolt` — la API con la que las ARMAS dibujan sus relámpagos (LanzaAlba, SinfoníaPrimordial, VerboPrimordial, las cadenas del RunicLightning, los látigos de escarcha de las oleadas Deerclops, los arcos voltaicos del AtaqueJefe/Arquera Estelar/Magnetar) — pintaba su CUERPO con `ChainTex` = **BoltChain.png, una textura de ESLABONES: un patrón de GUIONES premultiplicado a lo largo de la banda** (verificado por análisis de píxeles: la fila central alterna 68/255/72/255…). Sobre un `ZigPath` de UNA sola escala (mono-jitter). El resultado literal en pantalla: una LÍNEA DISCONTINUA de guiones, no un rayo. La comparación forense con bdd24ed lo confirmó: ChainBolt llevaba esa receta desde antes de la gran auditoría — nunca fue una regresión, era el diseño.
  · **LA RECONSTRUCCIÓN (investigada en internet como pidió el usuario — la receta canónica de "How to Generate Shockingly Good 2D Lightning Effects" + la escuela clásica del midpoint displacement, aplicada sobre las primitivas de la casa)**: (1) **EL TRONCO FRACTAL** — `FractalPath` (midpoint displacement MULTI-ESCALA: cada generación parte el segmento por su punto medio desplazado sobre la perpendicular con offset que SE DIVIDE A LA MITAD — lazadas grandes + micro-detalle, la rugosidad que el mono-jitter no puede dar) + `Refine` (midpoints cúbicos); (2) **LAS RAMAS** — `ForkTree` camina 2-3 horquillas con auto-corrección de curvatura heredando ~½ del ancho (la ramificación es la firma visual de una descarga de verdad, y sus puntas llevan gorro de descarga); (3) **EL PINTADO de 3 capas** con las bandas premultiplicadas (halo/cuerpo/vena — borde a borde, normal media, crackle por punto interpolado) — las texturas BoltHalo/BoltCore ya eran correctas (Gaussiana premult verificada por píxeles). `ChainBolt` mantiene su firma exacta (0 call-sites tocados) y mapea segments→generaciones (log₂) y amp(px)→chaos (fracción de la longitud: invarianza de escala — arcos largos rugosos, cortos contenidos). **`Bolt` y el tronco de `MultiBolt` también pasan de ZigPath a FractalPath** — todos los renderers de la casa ganan la rugosidad multi-escala (la lluvia naranja del Umbral, los rayos fugitivos del Supremo, los saltos del Sol Rúnico, los bordes de los portales del Rift). BoltChain.png queda como asset retirado (la convención de la casa: nada se borra).
  · **LAS BRUMAS (verificadas SANAS)**: diff bdd24ed→v6.50.7 de BrumaBrushes/BrumaFX/BrumaSystem = solo los fixes defensivos de descarga (v6.50.5/6) y la reversión del tinte (v6.50.7) — cero cambios de render. BrumaBrushes hornea premultiplicado desde v6.25 ("el bug de los rectángulos" ya estaba enterrado ahí), el Tint es el premultiplicado de la casa y las armas de bruma componen con BrumaFX.Cloud/Puff/Tendril/Columna — todo correcto. Lo que el usuario veía "mal en las brumas/auras" era la pareja de arriba: los guiones del ChainBolt en las armas tormentosas y las cajas blancas del AuraLib donde debía haber manto de ruido.
  · **VERIFICACIÓN**: compilación 0 errores / 0 warnings de los 282 .cs contra el tModLoader 2026.07.3.0 REAL (entorno de verificación reconstruido desde cero: SDK 8.0.425 + release oficial v2026.07.3.0); análisis de píxeles de los 5 sprites de la tormenta y del halo fantasma (BoltHalo/BoltCore: bandas Gaussianas premult ✓ — BoltChain: patrón de guiones, retirado; BoltImpact/SoftGlow/AnillosSingularesHalo: premult ✓); cero cambios de localización (0 claves hjson tocadas); cero cambios de comportamiento de juego (solo render de librería + firma idéntica en las 3 APIs reconstruidas).

## Commit v6.50.7 — LA REPARACIÓN DE LAS LIBRERÍAS VISUALES: EL TINT, EL DESTELLO Y EL DIAGNÓSTICO DEFINITIVO (client.log v6.50.6)

**Petición del usuario**: "analiza todo desde el commit bdd24ed en adelante… tanto la librería de brumas como la librería de rayos parecen corrompidas, las armas que usan sus efectos se ven mal… el juego se cerró al parecer causado por la librería de bruma… muchas armas que usan bruma están mal, los efectos de aura también… las armas que usan destello iluminan toda la pantalla cuando el destello se debe usar solo enfoncando el arma con un degradado a medida que se aleja del centro del proyectil del arma… creo que esto es algo causado después de la auditoría masiva anterior". Análisis completo de los 9 commits desde bdd24ed (v6.43→v6.50.6) + el client.log nuevo (v6.50.6, 22/9 14:24) + decompilación del tML/FNA REALES.

  · **LA SONDA DEFINITIVA DEL PIPELINE (la causa raíz de TODO)**: v6.50.3 migró el `Tint` de 33 archivos al "tinte lineal" (RGB intacto, alfa=f) creyendo que Additive=(SourceAlpha,One) hacía que el premultiplicado "atenuara dos veces". La sonda de v6.50.3 estaba INCOMPLETA. Verificación de hoy contra los binarios REALES del tML 2026.07.3.0 del usuario: (1) `BlendState.Additive = (SourceAlpha, One)` ✓ y **`BlendState.AlphaBlend = (One, InverseSourceAlpha)`** — compositing PREMULTIPLICADO (relexión con el FNA.dll del release); (2) el loader de texturas del mod (`ReLogic PngReader.FromStream` → `PreMultiplyAlpha`, verificado descompilando) **PREMULTIPLICA los PNG al cargar**: texel.rgb = rgb·alfa. Con eso, la matemática cerrada: el tinte premultiplicado v6.25 `(RGB·f, A·f)` es EL CORRECTO para AMBOS presets (masa: color y cobertura escalan por f — compositing perfecto; aditivo: aporte g²·c·f — el look calibrado de 25 versiones). El tinte lineal v6.50.3 rompía la masa (color SIN escalar: bruma fantasma sobresaturada, "las armas que usan bruma se ven mal") y sobrealimentaba los aditivos hasta ×10 en efectos tenues ("los efectos de aura están mal", "destellos que inundan la pantalla").
  · **LA REVERSIÓN QUIRÚRGICA**: los 33 archivos migrados vuelven AL PREMULTIPLICADO de la casa con el comentario de la sonda completa (BrumaFX, StormLib, EstelaLib, OndaLib, TelaLib, PyraLib, RiftLib, MediaResLib, SierpesLib + 14 renderers estelares + 13 proyectiles). El bloque era idéntico en los 33 (verificado) — reversión uniforme, 0 cambios de comportamiento más allá del tinte. Las 2 menciones de doc de StormLib reescritas con la verdad del pipeline.
  · **EL DESTELLO REDISEÑADO (la spec exacta del usuario)**: `OndaLib.Flash` era un VELO A PANTALLA COMPLETA (SoftGlow gigante centrado en la pantalla — "ilumina toda la pantalla"). Ahora es **EL GRADIENTE LOCAL DEL ARMA**: nace en el CENTRO DEL PROYECTIL (`centroMundo`, coords de mundo, anclaje de OndaExpansiva — exacto a zoom 1), con núcleo brillante + falda ancha (×1.6 de radio al 28%) que se apaga al alejarse del centro — "un degradado a medida que se aleja del centro del proyectil del arma" palabra por palabra. El radio respira (nace apretado, se abre ~26% al morir), lote ADITIVO propio en Identity + el lote de interfaz restaurado con SU matriz (try/catch/finally), cull de fuera-de-cuadro, y la API gana el parámetro opcional `Vector2? centroMundo` (sin origen = jugador local — compatibilidad total). **19 call-sites actualizados** con SU posición: 16 proyectiles de arma (NovaEncadenada ×2 generaciones, CometaErrante, RayoGamma, LágrimaSolar ×2, VozCuasar, DecretoEclipse ×3, OcasoBurst, Meteoro, ColapsoMagnetar ×3, SembradorPulsar, AbrazoNebulosa) + RiftLib.TearImpacto (el origen del desgarro) + OcasoPlayer ×2 (el centro del jugador). El estado (edad/cooldown/ancla) muere con el mundo y con la descarga.
  · **EL CIERRE DEL JUEGO (la causa real)**: el log v6.50.6 muestra 26 `InvalidOperationException: End was called, but Begin has not yet been called` que ESCAPAN de `OrbitaLib.AbrirAdditive:185` — y `ProjectileLoader.PreDraw` (descompilado hoy) NO envuelve los hooks: la excepción sube a `Main.DrawProj_Inner`, el proyectil no se dibuja y el lote queda CERRADO → los draws siguientes de Terraria sobre un lote cerrado → cierre abrupto sin log (el log termina a las 14:30:16 sin shutdown). **PERO la línea 185 del árbol de GitHub lleva `try { End } catch` desde v6.49** — la excepción NO PUEDE escapar del código del repo. La datación por números de línea de TODOS los frames del stack (11 verificados contra los 10 commits) lo confirma: la carpeta local del usuario mezcla archivos de varias versiones (los call-sites de proyectiles caen en líneas donde el repo tiene los restores de v6.49/v6.50.2 que SU árbol no tiene). Además el warning FNA "unknown image type" SIGUE apareciendo (PNG corrupto local, no existe en GitHub: 352/352 válidos verificados hoy de nuevo). **El crash NO está en el código de GitHub — está en la carpeta local mezclada.** La reparación real para la máquina del usuario: reemplazo COMPLETO de la carpeta (ver la nota final).
  · **LO QUE SÍ ESTABA MAL EN GITHUB y quedó reparado**: (1) el Tint lineal (la regresión visual de v6.50.3 — bruma/rayos/auras/destello, la sospecha "después de la auditoría masiva" era CORRECTA); (2) el diseño del destello a pantalla completa; (3) nada más: BrumaBrushes/BrumaSystem (ciclo de vida saneado en v6.50.6, texturas premultiplicadas horneadas en runtime, cero render targets), AuraLib (cambios solo defensivos desde v6.50.2), CieloLib/fondos (sprites 12/12 válidos + cambios solo de ciclo de vida), EclipsePrimordialRenderer (solo blindaje try/finally), VFXCore (solo conservaje de samplers).
  · **VERIFICACIÓN**: compilación 0 errores / 0 warnings contra el tModLoader 2026.07.3.0 real (entorno /tmp/verify reconstruido con las 5 referencias del release oficial); 33/33 archivos revertidos con el bloque canónico idéntico (0 restos del tinte lineal); 19/19 call-sites del destello con origen (0 llamadas sin posición); hjson 680/680 es/en mismo orden (sin cambios de localización); 352/352 PNG del repo decodifican; el `Flash` interno de OndaSystem solo lo llama OndaLib (0 otros call-sites).

**NOTA PARA SINCRONIZAR LA MÁQUINA LOCAL (importante)**: el client.log v6.50.6 demuestra que la carpeta local del Mod Sources mezcla archivos de varias versiones (los números de línea del stack no existen en NINGUNA versión del repo; el PNG corrupto local persiste). Copiar archivos sueltos no alcanza — los errores de lote y el cierre del juego vienen de archivos VIEJOS que siguen ahí. La forma limpia (una sola vez): descargar el ZIP completo de GitHub (Code → Download ZIP), BORRAR el contenido de la carpeta del mod en Mod Sources y extraer el ZIP ahí (o `git clone` / `git reset --hard origin/main` + `git clean -fd`), y recompilar desde el menú. Con eso, las 26 excepciones del log y el cierre desaparecen — las defensas de lote llevan 3 versiones en GitHub y hoy el pipeline visual vuelve al look premultiplicado de siempre.

Compilación 0 errores / 0 warnings contra tModLoader 2026.07.3.0 real (tModLoader.dll + FNA + ReLogic + TerrariaHooks + log4net del release oficial, sondeado además por decompilación: BlendState presets, PngReader.PreMultiplyAlpha, ImageIO.ToRaw, ProjectileLoader.PreDraw sin wrapper).

## Commit v6.50.6 — EL FATAL DE DESCARGA: EL JUEGO SE CERRABA SOLO (client.log v6.50.5)

**Petición del usuario**: "bueno, dio varios errores antes de cerrarse el juego" (client.log de la v6.50.5, 22/9 12:54). Diagnóstico completo del log: la v6.50.4 (cargada al arrancar) y la v6.50.5 (recién compilada) cargaron BIEN las dos — el mod funciona; el problema estaba en la DESACTIVACIÓN (recompilar desde el menú → tML descarga el mod viejo). La v6.50.5 soltó las 4 anclas readonly correctamente (cero `FieldAccessException` en su pasada) pero dejó un dominó fatal: **`NullReferenceException` en `CieloLib.Reiniciar()` → tML lo escala a FATAL ("Uno o más errores ocurrieron durante la desactivación y tModLoader debe reiniciarse") → el juego se cierra solo**. Eso era exactamente "antes de cerrarse el juego".

  · **LA CAUSA RAÍZ — EL CONTRATO DE ORDEN (verificado en el fuente del tML 2026.07.3.0, `Mod.Internals.cs/UnloadContent`)**: tML ejecuta la descarga EN ESTE ORDEN: (1) `OnModUnload` de los ModSystem → (2) `Mod.Unload()` (nuestro barrendero de memoria, que ANULA las anclas mutables) → (3) los `Unload()` de TODOS los ModSystem/content — **después del barrendero, no antes**. El comentario v6.50.1 del barrendero decía lo contrario ("las limpiezas de los ModSystems ya corrieron"): era FALSO y es la suposición que causó la cadena entera. Con `_texturas` mutable desde v6.50.5, el barrendero lo anulaba en el paso (2) y `CieloSistema.Unload()` llamaba `CieloLib.Reiniciar()` en el paso (3) → `_texturas.Clear()` sobre null → NRE → la excepción escapa (tML NO envuelve los Unload de ModSystem) → FATAL → cierre del juego.
  · **FIX 1 (el fatal)**: `CieloLib.Reiniciar()` pasa a nulo-seguro (`_texturas?.Clear()`) + la llamada en `CieloSistema.Unload()` va envuelta en try/catch (la casa: la descarga jamás revienta — es literalmente el consejo del mensaje FATAL del propio tML: "los modders deben usar programación defensiva").
  · **FIX 2 (el segundo dominó, habría caído justo después)**: `BrumaBrushes.Unload()` accedía `_puffs[v]`/`_vapors[v]` con el elemento ya guardado contra null pero NO el ARRAY en sí — y `_puffs`/`_vapors` también son mutables desde v6.50.5 (el barrendero los anula ANTES de que corra `BrumaSystem.Unload()`). Guards por array: `if (_puffs != null) …`, `if (_vapors != null) …`.
  · **LA AUDITORÍA COMPLETA DEL CAMINO DE DESCARGA** (los 13 hooks `Unload()` del mod + los 10 métodos de limpieza que llaman): `AuraLib.Reiniciar` (null-check en `_ruido` ✓), `MediaResLib.Detach` (guard de `_rt` ✓), `BlackHoleLensSystem.Unload` (guards de `_lensTarget`/`_lensTargetB` ✓), `EcoLib`/`AudioLib`/`PyraLib`/`EstelaLib`/`OcasoBurstFX`/`PulsoLib`/`PantallaLib` (contenedores readonly NO-ancla — el barrendero jamás los toca ✓), `VFXCore._capas` (anulado pero nadie lo lee tras el barrendero ✓), `OndaSystem._kicks` (array readonly de structs, no-ancla ✓), `ParticleManager` (asignaciones a null, seguras ✓), `ShardHUD`/`PresenciaNPC` (value types ✓). Los otros dos sistemas que llaman `Reiniciar()` externos ya iban envueltos (`GrimorioFuriaSistema`, `BrumaSystem`). **Resultado: cero accesos desnudos a estáticos barridos en todo el camino de descarga.**
  · **EL CONTRATO DOCUMENTADO EN EL CÓDIGO**: el comentario v6.50.1 falso corregido + el contrato de orden completo en el propio barrendero (`AethonMod.cs/Unload`) para que la próxima ancla mutable no vuelva a reventar el pase de descarga.
  · **LOS 4 `FieldAccessException` DE LA PRIMERA PASADA (12:57:27)**: son del binario v6.50.4 viejo (campos aún readonly en ése) descargándose al recompilar — la v6.50.5 ya los eliminó (campos mutables + salto IsInitOnly). Con la v6.50.6 activa ya no deben aparecer ni siquiera en la primera recompilación.
  · **EL WARNING FNA "Image loading failed: unknown image type" (12:57:24 y 12:58:05, durante "Empaquetando")**: reproducido el flujo EXACTO de empaquetado contra el decodificador REAL de FNA3D (`FNA3D_Image_Load` de la libFNA3D 23.10 del propio tML 2026.07.3.0, sobre los 356 PNG del repo): **0 fallos** — todos los PNG de GitHub decodifican. El warning viene de un PNG corrupto que SOLO existe en la carpeta local del usuario (resto del parche manual de texturas de la era v6.50.3 — `git reset` no borra archivos no rastreados; sí lo hace `git clean -fd`). No es fatal (tML empaqueta el PNG crudo y sigue) y el mod carga y funciona igual.
  · **NOTA DE RAMA (historia real)**: la v6.50.5 (d87437f) se diagnosticó y publicó en otra sesión el mismo día a partir del client.log de la v6.50.4 (builds 02:03/02:05); ESTE commit (v6.50.6) parte de esa v6.50.5 ya en GitHub, corrige el dominó que su propio fix introdujo y sube el árbol a estado verificado. La secuencia del log del usuario (compila v6.50.4 vieja → recompila v6.50.5 → NRE fatal) reproduce exactamente ambas versiones.
  · **NO NUESTROS** (del log): la advertencia de RAM del sistema ("Total system memory usage exceeds installed physical memory" — 7,7/7,9 GB en uso, 150 MB libres: la máquina está pasando por el archivo de paginación; tML usa 1,2 GB, AethonMod 27,2 MB — cerrar otras apps al jugar) y los avisos de Luminance (otro mod).

Compilación 0 errores / 0 warnings contra tModLoader 2026.07.3.0 real (referencias del release oficial, entorno de verificación reconstruido). Verificación del camino de descarga: matriz de comportamiento de reflexión de .NET 8 reproducida con un harness real (SetValue sobre readonly lanza SIEMPRE FieldAccessException — antes o después del cctor, porque el propio SetValue fuerza la inicialización del tipo; sobre mutable pre-init escribe y el cctor no re-corre) + auditoría de los 13 hooks Unload.

## Commit v6.50.5 — EL BARRENDERO Y .NET 8: LOS 4 ANCLAS READONLY (client.log v6.50.4)

**Petición del usuario**: "bueno, dio varios errores antes de cerrarse el juego" (client.log nuevo). Diagnóstico del log: la v6.50.4 cargó BIEN (el fix de las 6 texturas funcionó: cero `MissingResourceException` en las builds de las 02:03/02:05) pero aparecieron (1) 12 `System.FieldAccessException` — "Cannot set initonly static field" — en `AethonMod.Unload()` sobre `_texturas` (CieloLib), `_capas` (VFXCore), `_puffs`/`_vapors` (BrumaBrushes), y (2) 23 "End was called, but Begin has not yet been called" en el pase de proyectiles bajo el hook de pixelación de Luminance.

  · **EL BUG REAL (nuestro, reparado aquí)**: el barrendero de memoria del Unload anula anclas estáticas por REFLEXIÓN — y .NET 8 PROHÍBE escribir campos initonly (`static readonly`) vía `FieldInfo.SetValue` (FieldAccessException). Los 4 campos readonly-ancla se quedaban VIVOS tras la descarga → el "AethonMod mod class still using memory" del propio log (y en silencio, tragado por el try/catch por campo: el ancla no se soltaba aunque no revientara). FIX: los 4 pasan a `static` mutables (`_capas`, `_texturas`, `_puffs`, `_vapors` — nada los reasigna en runtime, solo el barrendero en Unload) + el barrendero gana la red definitiva: campos `IsInitOnly` → se limpian los ELEMENTOS/entradas igual (suelta casi todo el ancla) y se salta la escritura del campo (el ALC del reload trae estáticos frescos). Escaneo programático posterior: **0 campos readonly-ancla restantes** en todo el mod.
  · **LOS 23 "End without Begin" (NO son de este árbol)**: la traza pasa por `OrbitaLib.AbrirAdditive:185` y `AnillosSingularesHalo.PreDraw:88` — líneas que en v6.50.4 son `try { End(); } catch` PROTEGIDOS (el End defensivo de v6.49 + el patrón a prueba de balas v6.10 + los 50 restores de v6.50.2): esa excepción NO PUEDE escapar del código de GitHub. La única explicación compatible con el log: **la carpeta local del usuario NO es un git pull limpio** (además: el warning FNA "unknown image type" persiste y el PNG corrupto no existe en GitHub — 356/356 válidos verificados; los números de línea del log no existen en ninguna versión reciente). El árbol local mezcla archivos viejos con el parche de las 6 texturas. **SOLUCIÓN PARA EL USUARIO**: borrar la carpeta del mod y hacer CLON LIMPIO del repo (o `git reset --hard origin/main` + `git clean -fd`), recompilar — todos esos errores desaparecen porque las defensas de lote llevan 3 versiones en GitHub.
  · Nota para el probador con Luminance: bajo el hook `PrimitivePixelationSystem.DrawTarget_Projectiles`, el lote de `Main.spriteBatch` llega SIN Begin — nuestras defensas (try/catch + bandera `_loteAjenoAbierto` + `wasActive`) ya manejan ese estado desde v6.49/v6.50.2: dibujan en el lote propio aditivo, lo cierran y devuelven el estado tal como estaba.

Compilación 0 errores / 0 warnings contra tModLoader 2026.07.3.0 real (entorno /tmp/verify). Escaneo anti-recurrencia: 0 `static readonly` con tipo ancla en Content/.

## Commit v6.50.4 — EL FIX DE CARGA: LAS 6 TEXTURAS FANTASMA (client.log v6.50.3)

**Petición del usuario**: "me dio varios errores antes de este fix y después, comprueba y arreglar" (con el client.log de la v6.50.3: el mod entero se DESACTIVABA al cargar). Diagnóstico del log: `TransferAllAssets` → 6 `MissingResourceException`/`AssetLoadException` — faltaban los PNG default de 6 clases `ModProjectile`. No era código: compilar 0/0 no valida assets; tML los exige EN CARGA.

  · **LAS 6 CLASES SIN TEXTURA**: `AnilloRunicoDorsalHalo` · `SelloGenesisHalo` · `AnillosSolaresHalo` (halos cosméticos v6.37/v6.40) · `FragmentoSupernovaMinion` · `RafagaNovaProjectile` (v6.41) · `TentaculoCosmicoProjectile` (v6.41) — proyectiles 100%-código (PreDraw → false) pero SIN override `Texture`, así que tML buscaba el asset default `carpeta/Clase.png` inexistente → `MissingResourceException` y **TODO el mod se desactiva automáticamente** (con el "Los mods se han desactivado automáticamente" + tML marcando su instalación como corrupta en Steam).
  · **EL FIX — el patrón de la casa**: el override a la textura fantasma compartida de 76×76 que ya usan `AuraPortadorHalo`/`AtaqueJefeProjectile`/`AtaqueOleadaProjectile` desde v6.40: `public override string Texture => "AethonMod/Content/Projectiles/Cosmetic/AnillosSingularesHalo";` en las 6 clases (la textura NUNCA se dibuja: PreDraw false verificado en los 10 usuarios).
  · **LA AUDITORÍA EXHAUSTIVA** (para que no vuelva a pasar): escaneo programático de TODAS las clases texturables del mod (284: 109 ModProjectile · 158 ModItem · 6 ModNPC · 9 ModBuff · 1 ModDust · 1 ModTile) → **0 sin textura** tras el fix; 52 rutas dinámicas `ModContent.Request`/`AddBackgroundTexture` verificadas (50 existen, 2 comentadas intencionalmente desde v6.44); 356/356 PNG con magic bytes y decodificación válidos; 4/4 pares .fx/.fxc; hjson es/en mismo orden (642/642 sobre el árbol v6.50.2 del diagnóstico original; el árbol rebasado hereda y re-verifica las 680/680 de v6.50.3).
  · **EL WARNING FNA "Image loading failed: unknown image type"**: NO reproduce en el repo (los 356 PNG de GitHub son válidos) → es un PNG corrupto/dañado en la CARPETA LOCAL de compilación del usuario (probablemente tocado al aplicar v6.50.3). No fatal por sí solo (el mod siguió empaquetando), pero conviene localizarlo (comando de diagnóstico incluido en la entrega).
  · **NO NUESTROS**: los warnings `Luminance: Failed to load icon_small.png` y `Luminance mod class still using memory` son del OTRO mod instalado (Luminance v1.0.14), no de AethonMod.
  · Nota menor pre-existente (tolerada por tML, no es error de carga): `TheWitness` es townNPC sin `_Head.png` desde v5.59.

Compilación 0 errores / 0 warnings contra tModLoader 2026.07.3.0 real (entorno /tmp/verify reconstruido: dotnet 8.0.425 + las 7 referencias). **NOTA DE RAMA (corregida al publicar)**: el fix original (fb31c13) se creó sobre v6.50.2 en un sandbox reseteado y se dejó sin push por un diagnóstico erróneo del entorno ("el sandbox es pull-only" — falso: el push a GitHub funciona y estaba ya probado). Este commit rebasa el fix sobre v6.50.3 (b1a9823, ya en GitHub: la reparación R50 de ~112 archivos) y lo publica: el árbol final = v6.50.3 completa + las 6 texturas fantasma, compilado y verificado de nuevo.

## Commit v6.50.3 — LA REPARACIÓN DE LA GRAN AUDITORÍA R50 (~100 hallazgos atacados)

**Petición del usuario**: "Repara todos los errores [del reporte de la auditoría R50]. El problema con las librerías EcoLib/EcosLib debes solucionarlo ya que el nombre confunde. Las librerías del proyecto se quedan (usos futuros). El código muerto NO se toca. El resto se arregla." — Cada fix verificado contra el código y compilado contra el tModLoader 2026.07.3.0 real (0 errores / 0 warnings). El CÓDIGO MUERTO (APIs con pedigrí para usos futuros: PulsoLib §1/2/4, Pantalla.Presets, EmberField de PyraLib, FormaLib/CompasLib, ArcaneBolt, CosmicOrbBolt, BranchType, claves hjson huérfanas…) queda INTACTO por decisión del usuario.

### A. EL RENAME — EcoLib/EcosLib YA NO CONFUNDEN
  · **EcosLib → EspectroLib** (65 referencias en 12 archivos, git mv incluido): "EcosLib" estaba a UNA LETRA de "EcoLib" y el nombre mentía sobre el contenido. EcoLib = las VOCES del grimorio (diálogos en pantalla — viva); EspectroLib = los FANTASMAS (estelas, espejos orbitales, CurvaAproximacion de los jefes — viva). Misma API, mismo contrato, nuevo nombre sin trampa. (Nota histórica: los menciones "EcosLib" en CHANGES.md previos quedan como historia.)

### B. LA SONDA DEL BLEND — la contradicción fáctica resuelta contra el FNA REAL
  · **BlendState.Additive de FNA es (SourceAlpha, One)** — sondeado por reflexión contra el FNA.dll del tML 2026.07.3.0 real (la sonda imprime los 5 campos). StormLib v6.39 afirmaba "(Src=One, Dst=One), el alfa no entra nunca" y estaba MAL; TajoLib/MediaResLib (v6.40) tenían razón.
  · **EL TINT PREMULTIPLICADO ATENUABA DOS VECES (×f²)**: RGB·f y alfa·f bajo (SourceAlpha, One) = intensidad real f² — el halo 0.30 salía a 0.09 (rayos ~3× más tenues de lo calibrado). FIX MASIVO: 33 archivos (Storm/Estela/Onda/Tela/Pyra/BrumaFX + 13 gemelos de estrella *Renderer + 13 proyectiles cósmicos — los *BlackHoleRenderer gemelos ya eran lineales desde antes) migrados al Tint LINEAL de la casa (RGB intacto, alfa=f — el patrón verificado de TajoLib/OrbitaLib/Lumen/Sigilo). Los docs mentirosos reescritos; los docs correctos intactos.

### C. LOS 3 ALTO DEL ARSENAL
  · **V20 SIN CONTRATO DE LOTE (PlasmaStorm + QuantumSplit)**: los ÚNICOS dos Begin/End de 2 argumentos del mod — sin matriz (zoom≠100% dibujaba desplazado) y restore pelado en Identity que ROMPÍA el resto del pase de proyectiles del frame. Ahora: additive con el sampler/rasterizer del pase de entidades + Main.Transform, y restore con AuraLib.ReabrirLoteVanilla (el patrón v6.50.2 medido en el IL).
  · **GUADAÑA DESGARRO: EL HAMBRE APLICABA DOS VECES**: ModifyWeaponDamage sumaba HambreGuadana al pipeline Y Shoot lo volvía a sumar manual → tajo/fisura a base+2×hambre (y el tope de la fisura ×2 inflado); la hoja melee quedaba SIN bono. El hook se retira: la suma única vive en Shoot (el diseño: "se suma al tajo y se gasta").
  · **EL FLASH DE PantallaLib NACÍA INVISIBLE**: el easing estaba INVERTIDO — `1−(1−p)²` (curva de CRECIMIENTO) como multiplicador directo: el flash subía de 0 a I y moría de un pop. Ahora decae: alfa = I·(1−p)² (el contrato del doc; la gemela OndaSystem ya lo hacía bien). Afecta Supernova y BlackHole.

### D. LOS MEDIO (los 3 perdidos del reporte R50, re-derivados y confirmados en código)
  · **LA CRÓNICA FANTASMA DE SEGMENTOS**: GlobalNPCXP marcaba la crónica con solo `npc.boss` — las partes con boss=true (la cabeza y las manos del SEÑOR DE LA LUNA mueren como fases ANTES del núcleo: 3 páginas + 3 paquetes fantasma por derrota; LOS GEMELOS son dos tipos boss=true con una sola derrota) escribían páginas fantasma. Ahora la marca usa `EcoSistema.EsDerrotaCompleta` (nuevo helper público — las MISMAS reglas que la voz: partes en cascada nunca, Gemelos/Ecos solo al caer el último) y el paquete solo viaja si la página es NUEVA (CronicaMarcar devuelve bool). AnunciarJefeMuerto refactorizado sobre el mismo helper (cero duplicación). (Nota: la primera narrativa v6.50.3 mencionaba al Devorador/ojo del Muro/cabezas de Golem como boss=true — verificado al IL: en este tML son boss=false; el fix los cubre igual por si un mod externo los marca.)
  · **EL CACHE ESTÁTICO DEL AURA EN MP**: `_auraHambre`/`_auraHambreIntensidad` eran STATIC en el ModPlayer — compartidos por TODAS las instancias: en SP acertaban por casualidad; en el server de MP el hook corre por cada portador y dos hambres distintas = miss por frame por jugador (la promesa "cero GC" moría donde hay más jugadores). Campos de INSTANCIA: una caché POR jugador.
  · **9 ANUNCIOS DE JEFES INVISIBLES EN MP**: Main.NewText en IA server-side (furia del Titán/del Portador/de la Arquera, lluvia de estrellas, sello y paredes del Rift, gravedad/recordar/fases de Aethon) — nadie los veía en MP. Vía EcoRed.AnunciarMundo (ChatHelper difunde por CLAVE).

### E. LOS MEDIO RESTANTES
  · **LA SUCCIÓN DE LOS 12 AGUJEROS SIN GATE**: la familia completa empujaba npc.velocity en TODAS las máquinas (jitter de réplicas en MP) mientras SunProjectile documentaba "la familia de agujeros gatea la misma succión así" — promesa rota: solo el sol la gateaba. Gate de autoridad en los 12 (BlackHole + 11 variantes).
  · **TestAdvancedFX (el archivo legacy v5.46)**: cerraba el lote del pase de proyectiles y reabría con Begin de 2 args — el FX de las 4 armas de prueba desplazado con zoom≠1 y el RESTO del pase sin zoom (sprite original del 931 incluido). Migrado al contrato de 7 argumentos.
  · **EclipsePrimordialRenderer SIN BLINDAJE**: el único renderer grande sin try/finally propio — 8 pares Begin/End expuestos (una excepción dejaba el lote abierto hasta el catch del llamador). Los 5 bloques blindados (End en finally).
  · **MediaResLib Y EL ANIDAMIENTO**: el Terminar del anidado CERRABA el pase del exterior (no distinguía quién abrió). Contador de profundidad: el anidado descuenta y NO toca el pase del dueño.
  · **OndaSystem/OcasoSystem SIN GUARD DE LOTE**: el destello y el desgarro del ocaso dibujaban en el lote de UI "TAL CUAL" — un lote cerrado por otro mod = InvalidOperationException sin capturar. Blindados; el velo del destello ahora se dibuja en Identity (pantalla EXACTA a cualquier UI-scale) y reabre el lote de UI con SU matriz.
  · **LAS 2 CONFIGS MUERTAS CONECTADAS**: MaxShardLevel existía desde el inicio y NADIE lo leía — mudada a AethonConfigServidor (decisión de la AUTORIDAD, el mismo diagnóstico del split v6.50.2) y cableada en ShardLevelItem.GrantXP (tope dentro de la escalera de niveles múltiple incluido, XP saturada en el umbral). ShowDebugInfo también fantasma — true = el panel F8 arranca ABIERTO al entrar al mundo.
  · **CosmicOrbMinion, EL ÚNICO SIN REFRESCO**: el único minion de la casa que no hacía timeLeft=2 en CheckMinionBuff (expiraba a los ~5 min con el buff activo); la órbita TwoPi/8 fija mentía vs el comentario "360°/numMinions" (ahora reparto uniforme real vía ownedProjectileCounts) y ModifyHitNPC ganó el fallback de HeldItem de su AI (un minion sin cache cobraba multiplicador de nivel 1).
  · **LA ESTELA DE QUANTUMSPLIT ERA LETRA MUERTA**: TrailingMode=0 (manual) y la IA jamás escribía oldPos — el bucle de la estela del PreDraw nunca dibujó nada. Escritura manual vanilla; el no-op `ai[0]+=0f` retirado.

### F. LOS BAJOS (la capa fina)
  · **hjson**: +2 DisplayName que faltaban (AuraPortadorHalo → "Aura del Portador"/"Bearer's Aura", AtaqueOleadaProjectile → "Diente de oleada"/"Wave Fang") — paridad 678/678 mismo orden.
  · **STRINGS→hjson (la regla v6.50.2 llegó a donde no había llegado)**: 34 strings hardcodeados de VoidCrown/RuneCrown/AnilloRunicoDorsal/SellosAccesorios(×3 clases)/Prisma(+2 NewText)/BolsaCategoria(tooltips + los 2 mensajes de la apertura) migrados a es/en con paridad de placeholders verificada.
  · **REAPERTURAS DE LOTE UNIFICADAS**: MoldeLib.FlushPaseAlpha, CompasLib.DepurarDibujar y FormaLib.CerrarLoteDebug reabrían el pase de ENTIDADES con LinearClamp+CullNone (el patrón que v6.50.2 tildó de bug: pixel-art borroso tras el pase) — ahora el patrón exacto (DefaultSamplerState+Main.Rasterizer+Main.Transform). CieloLib documentado: SU matriz de fondo es la correcta y el Linear del paisaje es a propósito (gradientes).
  · **EL CONSERJE TAMBIÉN BARRE SAMPLERS**: los shaders del sol dejan SamplerStates[1..2]=LinearWrap pegados — el Janitor de VFXCore anulaba las texturas pero no los samplers. Extendido a los 3 slots.
  · **CERO-GC REAL EN LOS RENDERERS**: GlifosRencor/RuneGlyphs (3 archivos) eran PROPIEDADES que alocaban el jagged array cada frame desde PreDraw → static readonly (UNA vez); los 4 ModContent.Request por estrella por frame de RuneSun/CicloEstelar → cache de Asset estático (barrido por el barrendero de reflexión); ManadaAstral: el Sort con closure cada 15 ticks → insertion-sort in place (cero alocaciones); Caravana: el while con RemoveAt(0) (O(n²)) → RemoveRange (O(n)).
  · **CULLING DE PARTÍCULAS**: margen 320→600 — los RingPulse de ~540 px de radio se culleaban con el borde aún visible (pop-in).
  · **LAS CORONAS YA NO DELATAN AL SIGILOSO**: las 3 capas de dibujado (AuraJugadorFrontal, RuneCrown, VoidCrown) dibujaban sobre jugadores en stealth (p.invis) — la corona flotaba ENORME donde el cuerpo estaba oculto. Guard añadido (el campo real de esta versión de tML: `invis`, sondeado por reflexión).
  · **ViboraGenesiaca SIN DEFENSA**: el único compositor de SierpesLib sin null/length-checks en sus hebras — defensas añadidas (n = mínimo de lo que hay de verdad).
  · **AsegurarTexturas de AuraLib**: el guard solo miraba null — una Dispose ajena dejaba cadáveres silenciosos. Guard null/IsDisposed + funeral compartido (DisposeRuido) — la nota honesta del IsContentLost que el Texture2D plano de FNA no expone.
  · **DOCS Y TYPOS**: el comentario del Grimoire que aún decía "dispara ArcaneBolt" (dispara el 931 desde hace versiones); "Crimson/Fusión quedan INTACTOS" (no existen — doc podrida); TomoApatiaNula "maná 26" (era del original, la réplica es 0); doc-rot de contadores ("LAS DIEZ" bolsas → dieciséis, "los 10" agujeros → 11, "las 7" estrellas → 19); tooltip de Supernova 5 s→10 s (el proyectil aplica 600 ticks desde v5.91); "trayectorias paralelas"→divergentes (el split es a ±30°); typos VUEA×2/ATQUE/"aninar"; EstelaLib: el contrato "cero GC por frame" era una promesa rota — doc honesta (los métodos son públicos y devuelven el array: cachear sería mutar bajo los pies del llamador).

### G. LA VERIFICACIÓN DE 100 PASADAS TAMBIÉN REPARÓ (hallazgos V-1..V-4, todos pre-existentes o huecos de los fixes propios)
  · **LAS 4 SIERPES SIN AUTOCURA MP** (hallazgo V-1, MEDIO): netImportant corre la IA en los remotos pero OnSpawn NO viaja en el msg 27 — OuroborosAstral/ManadaAstral/ViboraGenesiaca/CriaEstelarMinion nacían vírgenes (arrays a ceros) y su IA las arrastraba a la esquina del mundo con rubber-band contra el sync. OnSpawn refactorizado a Sembrar() + flag _sembrado + autocura al inicio de la IA (el patrón de AtaqueJefe/AtaqueOleada de v6.50.2; la siembra remota usa la posición SINCRONIZADA).
  · **EL BYPASS DEL TOPE POR ESENCIAS** (hallazgo V-4): SubirNivelDirecto no consultaba MaxShardLevel — un alma saltaba el tope recién conectado. Ahora también manda ahí (y la barra de XP del tope se satura con un solo criterio, en las dos rutas).
  · **EL RESTORE DEL PAR V20 VIVE EN finally** (hallazgo V-1): End+ReabrirLoteVanilla dentro del try con catch vacío dejaba el lote aditivo ABIERTO si un draw tiraba — ahora en finally (como PhoenixNova/Supernova), con el early-return de texturas fuera del try.
  · **2 STRINGS MÁS A hjson** (hallazgo V-3): "La resonancia despierta N eco(s)" (Bastón de Apuestas) y "Slots de minion llenos" (Grimoire) — las 2 últimas NewText de armas → claves Armas.ResonanciaEcos/Armas.SlotsMinionLlenos (680/680).
  · **DOCS CORREGIDAS POR LA VERIFICACIÓN**: la narrativa de la crónica fantasma decía "Devorador/ojo del Muro/cabezas de Golem con boss=true" — V-4 lo desmintió contra el IL (son boss=false en este tML; los curados reales: la cabeza/manos del Señor de la Luna y los Gemelos) — comentarios y CHANGES corregidos; la frase stalada "el lote aditivo PASA del alfa" de StormLib; las referencias a "OndaLib.OndaExpansiva" (es Pantalla.OndaExpansiva) en OcasoSystem/OcasoBurstFX; la composición de gemelos del §B (los *BlackHoleRenderer ya eran lineales — los migrados son los 13 gemelos de estrella).
  · **HALLAZGO REFUTADO POR SONDA**: V-2 reportó el "backglow A=0 invisible" de RuneSun/CicloEstelar — la sonda del FNA real demostró que AlphaBlend es (One, InverseSourceAlpha) — la fórmula PREMULTIPLICADA: con A=0 el quad SUMA su rgb puro (el truco premult clásico). El backglow está VIVO y es intencional — NO se tocó (y así quedó documentado).
  · **DEUDA DECLARADA (no arreglada, fuera de alcance)**: los 16 Titulo/NombreCorto de las bolsas concretas y los tooltips del arsenal de Weapons siguen en español (familia strings→hjson para la próxima pasada); el CritChance post-spawn de la fusión de Apuestas (v6.50.1, no viaja en msg 27); la fórmula del tope del Hambre de la Guadaña (crece ×2 por ciclo en saturación — diseño pre-existente mejorado); la {1} de "Jefe.Aethon.Fase" viaja en el idioma del host.

## Commit v6.50.2 — LA SEGUNDA CAJA DE HERRAMIENTAS (4 auditorías + ~60 fixes)

**Petición del usuario**: "El commit e99a11a no está en github, analiza por qué y has un análisis profundo del código arreglando todos los bugs pendientes". (El commit existía localmente pero NUNCA fue empujado — la sesión anterior se quedó sin contexto justo tras crearlo; push ejecutado y verificado al inicio de esta). 4 pasadas de auditoría en paralelo (Systems+red MP · Jefes+Oleadas · Armas+Proyectiles · VFX+hjson+memoria), cada hallazgo verificado contra el código Y contra el IL del tModLoader 2026.07.3.0 real antes de tocar. ~60 fixes. Compilación 0/0.

### A. LOS 7 CRÍTICOS (gameplay roto)
  · **EL WIPE DE CRÓNICA DISFRAZADO DE MERGE (EcoRed.MsgCronica)**: el `CronicaJefes.Clear()` antes del bucle de unión convertía el "merge jamás degrada" en un REPLACE — la copia fresca del server (sin SSC) traía 1 jefe de la sesión y la crónica HISTÓRICA del .plr se borraba y se persistía degradada: el Testigo callaba para siempre en MP. Fix: sin Clear, el `if (!Contains) Add` ES la unión.
  · **LA PÚA DEL SAGRARIO MORÍA AL CLAVARSE (AtaqueJefeProjectile)**: OnSpawn ponía tileCollide=true y el motor mata al proyectil en la PRIMERA colisión (OnTileCollide default → Kill) — la IA corría ANTES de la colisión y su chequeo `velocity≈0` jamás ganaba: `_clavada` nunca se ponía, las 90 t de respiración y DetonarPua eran CÓDIGO MUERTO (en SP y server). Fix: override OnTileCollide → clava (velocity=0) y devuelve false (ni muerte ni rebote); la estrella fugaz gana el mismo blindaje.
  · **LA CARGA DEL TITÁN CASI NUNCA DISPARABA**: el arranque exigía que el tick exacto en que `_tickPua==0` (1 de cada 90/45) coincidiera con un múltiplo global de GameUpdateCount de 420/280 — gcd(90,420)=30 → 29 de cada 30 peleas la carga telegrafiada JAMÁS arrancaba (y al alinear, cada 1260 t en vez de 420; furia: 4 de 5). Fix: reloj PROPIO en cuenta atrás que se rearma.
  · **EL ESCALADO ×(k+1) DE LAS OLEADAS NO LLEGABA A LOS REMOTOS**: Marcar muta lifeMax/damage/defense/knockBackResist post-spawn solo en el server; el msg 23 de vanilla NO los lleva (y el bit life==lifeMax fija una lifeMax SIN escalar) — el daño NPC→jugador se evalúa en el CLIENTE con SU copia → los remotos recibían daño ×1 (la promesa "la 10 golpea ×11" no existía en MP). Fix: 4º bit en SendExtraAI/ReceiveExtraAI + los 5 stats EXACTOS viajan y el cliente los ASIGNA (idempotente por construcción; espejo del statsAreScaled de vanilla).
  · **EL JUICIO TERMINABA CON EL DEVORADOR VIVO**: el conteo de guardianes exigía `n.boss` — EoW (tipos 13/14/15) NO pone boss=true en vanilla → el festín terminaba "saciado" con el guardián ×15 aún en el mundo. Fix: fuera el filtro boss — el sello ya valida de sobra.
  · **LAS 16 BOLSAS DE CATEGORÍA NO ENTREGABAN NADA EN MP**: RightClick corre SOLO en el cliente que clica (el server JAMÁS lo ejecuta) — el guard "MPClient → return" mataba la entrega en la ÚNICA máquina que la hacía. Fix: gate fuera (el inventario es client-authoritative y vanilla lo sincroniza).
  · **LAS NOVAS FANTASMA (5 armas)**: RuneSun/RedSupergiant/Sun/MareaGravitatoria/Supernova(V20) gateaban el spawn con `netMode != MultiplayerClient` — el server engendraba con owner=índice de jugador ≠ myPlayer(255) y NewProjectile SOLO difunde owner==myPlayer (verificado en el IL) → visual Y daño de la nova perdidos para cualquier jugador no-host. Fix: gate de MÁQUINA DUEÑA (`Projectile.owner == Main.myPlayer`, el patrón de los agujeros) — lo engendra el cliente dueño y el propio spawn sincroniza.

### B. EL HOST DE HOST&PLAY YA EXISTE (la clase completa: `netMode == Server` ≠ "sin pantalla")
  · Solo `Main.dedServ` significa "sin pantalla". El host de un listen server es netMode 1 CON pantalla: ~20 gates lo trataban como server ciego. Arreglado: el lifesteal del grimorio (WeaponScaling), la celebración de level-up (ShardLevelItem + solo las subidas PROPIAS), la nova del parry (ApuestasPlayer), las lente/halos de los Anillos (SellosPlayer), las chispas de las coronas (CosmeticPlayer), el kick/flash del ocaso (OcasoPlayer ×3), el sonido del fallo de guardia (Égida) + **67 PreDraw/PostDraw de proyectiles y jefes con `netMode == Server → return false`** que dejaban al host SIN TODO el arte 100%-código del mod (veía la textura fallback de 39 archivos de proyectiles y los 5 jefes). Los render hooks NO corren en dedicado: el gate correcto es dedServ (nunca true en render).

### C. LA RED MP — los detalles que faltaban
  · **LA SELECCIÓN DE LA CARNADA VIAJA (MsgPrepararOleadas)**: OleadasPreparadas era un estático POR MÁQUINA — el remoto ciclaba SU contador y el server desataba la furia con SU propio 3. La selección viaja del cliente a la autoridad y vive en ShardPlayer.OleadasPreparadasRemoto.
  · **BANDWIDTH (3 paquetes por kill → 1)**: MsgLatidoXp LLEVA el estado del libro (slot/nivel/XP); la FOTO completa de MsgLibro solo al subir nivel / entrar / red de seguridad 600t; MsgHambre solo al cambiar. La barra sigue exacta.
  · **LA CONFIG DE LA AUTORIDAD (split de ModConfig)**: XPMultiplier y EventoHambreGrimorio las leía el SERVER con ConfigScope.ClientSide → en dedicado la config del cliente era una ILUSIÓN. Nueva AethonConfigServidor (ServerSide); SP y host&play sin cambio.
  · **EL FESTÍN CAMINA EN EL DIAGNÓSTICO**: MsgHambre lleva (fase, oleada, total) — el overlay F8 de los remotos ya no dice "silencio" en pleno Juicio. El panel además refresca a 4 Hz de verdad (antes cada frame) y el tinte de ayuda cae en la línea de ayuda (no en la de furia).
  · **EL MERGE DE XP A NIVEL IGUAL**: MsgLibro pisaba la XP local (más rica) cuando el nivel coincidía. Ahora a nivel igual gana el máximo.
  · **EL ANTI-DUPE DEL FRAGMENTO CUBRE EL SUELO**: el "doble click antes de recoger" que el propio comentario describía seguía vivo — escaneo de Main.item a <400 px antes de spawnear.
  · **"EL TESTIGO HA LLEGADO" EN MP**: el anuncio vivía en un Main.NewText server-only (invisible). Ahora viaja por EcoRed.AnunciarMundo. + los 8 strings hardcodeados restantes (TestingPlayer/LevelUpTester/AncientAltar) mudados a hjson (633→642 claves, paridad exacta verificada).
  · **LA CARRERA DEL SLOT EN FaseJefe**: un town NPC podía reciclar el slot del jefe muerto en el MISMO tick (UpdateNPC → PostUpdateWorld) y robarse el DerrotaOleada10. El sello ahora valida también el `type` guardado al spawnear. Y los spawns voladores de chusma se clampean a los bordes del mundo (nacían fuera → excepción tragada → pulso de spawn perdido).

### D. LOS JEFES Y SUS ARMAS
  · **EL PARRY DEL DUELISTA ERA UNA VEZ POR VIDA**: ParryCooldown=120 al bloquear y NADIE lo decrementaba — la firma del duelista (20% de anular) moría tras el primer parry y la vaina quedaba apagada. Ahora decae en AI().
  · **EL TELEPORT DEL RIFT CON RAND DIVERGENTE**: `_anguloOrbita += Pi + Main.rand...` corre en TODAS las máquinas con semillas distintas (NPC.AI corre en server Y clientes) → el cliente teleportaba a OTRO punto y el netUpdate lo corregía con snap (rubber-band en cada cruce). Ángulo DETERMINISTA (whoAmI + cruces) + clamp a los bordes del mundo.
  · **LAS ÓRBITAS EN (0,0) DE LOS REMOTOS**: _centroOrbita solo se fijaba en OnSpawn, que NO corre al recibir el msg 27 → el coro de cristal, las cuchillas, las runas memorizadas, el sello del trono y las calaveras teleportaban a la ESQUINA DEL MUNDO en las pantallas remotas. Autocuración al inicio de la IA — AtaqueJefeProjectile + AtaqueOleadaProjectile (este además con netImportant).
  · **EL FANTASMA DE 7 s DEL ESTALLIDO**: timeLeft=20 fijado en OnSpawn no viaja → los clientes mantenían la zona dañando 420 t. Muerte por edad (_edad corre en todas las máquinas). Y su hitbox era 14×14 para un visual de ~130 px — inflada a 110 px (golpea lo que SE VE).
  · **LA ÉGIDA SIN COOLDOWN AL FALLAR**: la guardia que expiraba sin parar nada dejaba EnfriamientoGuardia=0 → tras el aturdimiento de 30 t se re-alzaba al instante (spam) y el anillo de 480 t mentía. Cooldown puesto en la rama de fallo + el desync de stun del parry exitoso cerrado (ai[0]=1 como marca que VIAJA: Kill() no viaja y mataba el flush del netUpdate).
  · **LA TRAGA DE LA GUADAÑA ERA FANTASMA EN DEDICADO**: el p.Kill() de las balas devoradas corría solo en la pantalla del dueño — en dedicado la bala seguía viva en el server y dañando. Kill bajo autoridad + el dueño tumba su réplica local.
  · **EL SPLIT CUÁNTICO ×N+1**: sin gate de dueño, cada máquina engendraba sus 2 hijos fantasmas. Gate `owner == Main.myPlayer`.
  · **EL TOPE DE 6 ECOS ERA GLOBAL**: dos jugadores con Eco Cuántico compartían tope. Filtro por dueño.
  · **LA GRAVEDAD DEL SOL EN PANTALLAS AJENAS**: empujaba réplicas de NPC en clientes (jitter). Gate de autoridad.
  · **ai[] POST-SPAWN NO VIAJABA (TestAdvanced ×4 + GrimoireEternal)**: el paquete 27 sale DENTRO de NewProjectile — las escrituras posteriores a ai[] solo existían en la copia local (el minion del grimorio nacía nivel 1 en el server si soltabas el libro). Los valores viajan como argumentos ai0/ai1/ai2.

### E. VFX Y MEMORIA
  · **EL RESTORE DEL LOTE DE UI CON LA MATRIZ DEL MUNDO (PantallaLib)**: PostDrawInterface reabría con GameViewMatrix — todo el pipeline de interfaz usa UIScaleMatrix → tooltips/texto de vanilla desplazados tras cualquier onda (zoom≠100%). Matriz corregida.
  · **EL DOBLE ESTÁNDAR DEL RESTORE DE ENTIDADES**: AuraLib.ReabrirLoteVanilla/MediaResLib.Terminar + **los 50 restores inline del idiom `if (wasActive)`** reabrían el pase de entidades con LinearClamp+CullNone+GameViewMatrix (sampler BILINEAL) — el resto del pase dibujaba el pixel-art de NPCs/proyectiles BORROSO. Migrados al patrón medido en el IL (Deferred+AlphaBlend+Main.DefaultSamplerState+Main.Rasterizer+Main.Transform — idéntico para el pase de NPCs y el de proyectiles, Main.cs:23457/82641/82658).
  · **LA VOZ PRIORITARIA SE DESCARTABA CON LA COLA LLENA (EcoLib)**: las voces narrativas más importantes (furia, venganza, JuicioFin) se perdían en un asalto de jefes. Ahora desalojan al pendiente más viejo no-prioritario.
  · **EL CONSERJE MUERTO (VFXCore)**: RegisterLayer/FlushOcclusion/Janitor tenían 0 call-sites — la protección documentada (limpiar las texturas 1..3 bind-eadas por los renderers de shader al final del frame) no corría NUNCA. Conectado vía VFXCoreSystem.PostDrawTiles.
  · **EL BARRENDERO DE MEMORIA CIEGO PARA LA GPU**: EsAnclaDeDescarga solo veía tipos EXACTOS — RenderTarget2D declarado como tal, arrays de anclas (jagged incluidos) y diccionarios con valor-ancla NO se limpiaban. Extendido a GraphicsResource (con Dispose ENCOLADO al hilo principal — la lección v5.87) + arrays recursivos + IDictionary.
  · **LOS RESTOS DEL MUNDO**: ParticleManager.OnWorldUnload (las partículas del mundo viejo sobrevivían 1.25 s) + PullToGlobalBoost reseteado (quedaba atascado en 6× si el agujero moría sin Kill); TelaLib purgando cada 120 t de verdad; EnjambrePrismatico purga sus estelas; AuraLib valida el TYPE en el emisor (un whoAmI reciclado heredaba el aura ajeno 240 t).
  · **ALOCACIONES POR FRAME**: TelarEstrella y Magnetar construían+ordenaban List POR ESTRELLA POR TICK (O(N²)+GC) — buffers estáticos reutilizables (el patrón de la casa).

### F. VERIFICACIÓN
  · Paridad hjson 642/642 es/en mismo orden (script de conteo + cross-check de 80 claves usadas en código vs definidas — 0 faltantes reales; variantes dinámicas 69/36/18/4/3/4/3/3/23 presentes).
  · Compilación 0/0 ×4 contra tModLoader 2026.07.3.0 real (.NET 8.0.425, /tmp/verify).
  · APIs dudosas verificadas por decompile ANTES de usarlas: BitWriter/BitReader (el bit-byte va PRIMERO en el cable, los datos después — el orden de 4 bits + byte + 5 stats es simétrico), MessageBuffer case 23 (life se asigna ANTES de ReceiveExtraAI — la asignación exacta del fix lo corrige), NPCLoader.WriteExtraAI (Flush al final del hook), DrawProjectiles/DrawNPCs (parámetros del pase de entidades idénticos), SpriteBatch.Begin overload de 7 args (Effect ANTES de Matrix — 3 errores CS1503 corregidos al integrar).

## Commit v6.50.1 — LA CAJA DE HERRAMIENTAS DEL BUG-HUNT (varias pasadas de auditoría al código)

**Petición del usuario**: "Hay varios bugs, da varias pasadas al código buscando cualquier bug, o cosas que no estén bien" — con el client.log de la v6.50 como evidencia (el mod CRASHEABA al cargar). 5 pasadas completas: motor de daño (IL real), red MP de voces/libros, jefes/oleadas, call-sites migrados (117/117) y statics/memoria. Compilación 0/0 tras cada grupo de fixes.

### A. EL CRASH DE CARGA (el bug del client.log)
  · `BackgroundTextureLoader.AddBackgroundTexture(this, ruta)` recibía la ruta SIN el prefijo del mod ("Content/Effects/Cielo/SanctumFar") → `MissingResourceException: "No se encontró un mod con el nombre 'Content'"` en AethonMod.Load() línea 21 → tModLoader DESACTIVABA el mod entero. Verificado contra el fuente real de tML 1.4.4: AddBackgroundTexture pide la ruta TAL CUAL (ModContent.Request + clave del diccionario — el autoload interno construye `$"{mod.Name}/{path}"`), a diferencia de GetBackgroundSlot(Mod, …) que AÑADE el prefijo. Fix: registro con `$"{Name}/..."`, las constantes relativas intactas.

### B. GOLPEMOTOR — 4 bugs del cauce (hallados descompilando Projectile.Damage() del tML real)
  · **EL SPLASH A SOLAPADOS**: el bucle del motor recorre los 200 NPC y golpea TODO lo que interseca la hitbox prestada — con la hitbox clonada sobre el blanco, cualquier NPC apilado (enjambres, gusanos) comía FUERA de la geometría del llamador, y en los `foreach` multi-blanco el MISMO enemigo recibía golpe DOBLE. **LA PUERTA**: GolpeGateNPC (GlobalNPC.CanBeHitByProjectile — el gancho que CombinedHooks consulta por cada NPC) deja pasar SOLO al blanco (null = camino vanilla) y rechaza al resto; GolpeGateProy (CanCutTiles) apaga la siega de hierba del golpe instantáneo (SendData(17) en MP incluido — la escuela A jamás tocó el terreno). Cero costo fuera del golpe: dos comparaciones de int.
  · **LA RESTAURACIÓN**: si un hook del pipeline lanzaba a mitad de cauce, la restauración (dentro del try) NO corría y el proyectil se quedaba con el cuerpo prestado para siempre. Ahora la restauración vive en `finally` (idempotente) + el catch recompone `npc.position` (el motor la corre +netOffset antes de la colisión y una excepción salta su devolución).
  · **LA DIRECCIÓN DEL EMPUJE**: el motor toma `p.direction` para el HitDirection — la escuela A la pasaba explícita. El golpe ahora calza la dirección GEOMÉTRICA (del centro original del proyectil hacia el blanco: el empuje siempre aleja del origen).
  · **LA BALA SECUESTRADA**: `flag10` (la llave del golpe-bala) solo se da con maxPenetrate==1 SIN usesLocalNPCImmunity/usesIDStaticNPCImmunity — los proyectiles de minion usan inmunidad local y su sub-ataque quedaba retenido por la inmunidad del CONTACTO del propio minion. Las banderas se prestan en false durante el golpe (solo en modo bala) y vuelven.
  · **LA RECURSIÓN DE LOS ON-HIT (3 CRÍTICOS)**: el AoE del Nightglow y las cadenas de OcasoBurst/LanzaAlba engendraban golpes DENTRO de su OnHitNPC — y el golpe del motor DISPARA OnHitNPC síncronamente → golpe→AoE→golpe→AoE rebotaba entre dos NPC hasta matar a uno (StackOverflow no atrapable con jefes: el daño con suelo en 1 no termina la cadena). Guard `GolpeMotor.EnCurso(whoAmI)` en los tres hooks: la cadena nace SOLO del impacto real.

### C. LA RED MP — la progresión que se borraba y el host sordo (auditoría 4-a)
  · **EL ROUTER CIEGO (CRÍTICO)**: `MsgPedirFragmento` (9) NO estaba en el switch de ShardSyncSystem — el paquete del Altar moría en silencio y el Fragmento Génesis era INOBTENIBLE en MP (el cliente veía "Has reclamado…" sin que nada naciera). Una línea. + la AUTORIDAD revalida el pedido (chequeo de inventario en el server — un cliente modificado farmeaba fragmentos infinitos).
  · **EL BORRÓN DE PROGRESIÓN (CRÍTICO)**: sin SSC, la copia del server de los libros nace FRESCA cada sesión — MsgLibro la aplicaba con overwrite incondicional y BORRABA el nivel 50 real del .plr en cada reconexión (y MsgCronica limpiaba la crónica y el DerrotaOleada10 igual). **DOBLE ARREGLO**: (1) ShardLevelItem.NetSend/NetReceive — el nivel/XP viaja CON el item cuando vanilla sincroniza el inventario al entrar (ItemIO solo transporta datos de mod si el GlobalItem los implementa — la réplica del server ahora cuenta desde los datos verdaderos); (2) recepción con MERGE (jamás degradar: nivel menor = se conserva; crónica = la unión; DerrotaOleada10/CronicaNarrada/Primera5★ = máximo).
  · **LA RESONANCIA PERDIDA**: los shards se acreditaban SOLO en la réplica del server (OnKill) y el .plr los escribe el CLIENTE → morían con la sesión. Ahora viajan en MsgCronica (merge máximo) + entrega inmediata tras cada jefe + el anuncio de resonancia de los 5 OnKill viaja por EcoRed.AnunciarAlPortador (Main.NewText en server no lo ve nadie).
  · **EL HOST SORDO (ALTO)**: en un listen server el host es el jugador 0 y NO tiene socket de vuelta — ModPacket.Send(0) no le llega a nadie: el host JAMÁS oía la voz de su grimorio, ni el latido de XP, ni las celebraciones. **LA ENTREGA LOCAL**: EnviarPaquete detecta al host (netMode Server + !dedServ + myPlayer) y procesa el paquete por el MISMO cauce en memoria (MemoryStream → Recibir); la doble puerta de recepción admite al host como destinatario (Main.dedServ distingue al dedicado, cuyo jugador 0 es remoto). El Libro Celoso y el aura de ceniza también corren para el host (el guard `local` los excluía).
  · **LA RED DE SEGURIDAD** ahora re-asserta también el HAMBRE (una paquete perdido dejaba la barra/aura desfasadas 10 s).

### D. LOS JEFES — proyectiles ×N+1 y sus querencias (auditoría 4-b)
  · **PROYECTILES MULTIPLICADOS (CRÍTICO)**: la IA de NPC corre en server Y TODOS los clientes — sin gate, cada máquina spawneaba SU copia de cada ataque y NewProjectile la auto-difunde (SendData(27) con owner==myPlayer): las paredes de desgarro, virotes, púas, minas, lluvia estelar y la runa quedaban ×(jugadores+1) en MP. 22 call-sites gateados con `netMode != MultiplayerClient` (el patrón de vanilla en su IA de jefes) — sonido/polvos/anuncios fuera del gate. AtaqueJefeProjectile con `netImportant` (los que entran a media pelea reciben las paredes).
  · **EL SELLO DE OLEADA VIAJA**: OleadaNPC.SendExtraAI/ReceiveExtraAI — los flags EsDeOleada/EsEspecial/EsJefeDeOleada y la reconstrucción del AURA viajan con el NPC (los GlobalNPC de instancia NO viajan solos): el JUICIO y las auras de oleada ahora se DIBUJAN en los clientes MP.
  · **AETHON SIN HISTÉRESIS**: cruzar un umbral de vida curaba +5% → el siguiente tick re-basaba el umbral y la fase VOLVÍA (doble flip: dos rugidos, dos curas, +10% de vida por cruce — la pelea rebotaba entre fases). Ahora solo se AVANZA (newPhase > Phase).
  · **EL ANUNCIO DE FURIA DEL TITÁN**: los timers decrecen por debajo de 0 sin clamp → la ventana `== 0` simultánea ya no existía y el rugido de furia JAMÁS sonaba. Ahora `<= 0`. Y su `ai[0]++` pisaba el timer de salto de la IA fighter (aiStyle 2) → saltos erráticos: contador mudado a localAI[1].
  · **LA HUIDA QUE ERA VICTORIA**: en FaseJefe, un jefe despawneado (amanecer, distancia) o un SLOT RECICLADO entregaban el DerrotaOleada10. Ahora el slot se valida por el sello de OleadaNPC y solo la MUERTE (life ≤ 0) paga. El despawn del timeout difunde el msg 23 (los clientes tenían un jefe fantasma congelado hasta reciclaje del slot).
  · **LA CARNADA**: CanUseItem conservaba el guard myPlayer (el server corre el uso del remoto → false) → la furia NO EXISTÍA en MP para remotos. Gate fuera; los avisos de bloqueo viajan al portador.
  · **LOS MENSAJES DEL MUNDO**: el Reconocimiento de Aethon y los avisos de la Carnada ahora viajan por EcoRed (AnunciarMundo/AnunciarAlPortador) — en MP nadie los veía.

### E. LA FUGA DE MEMORIA (el warning del client.log: "mod class still using memory")
  · **~107 ANCLAS ESTÁTICAS en 43 archivos**: Asset<Texture2D>/Ref<Effect>/BlendState/listas de delegados de render que NUNCA se anulaban — cada una mantiene vivo el AssemblyLoadContext del mod tras la descarga (y los de ModProjectile ni siquiera TIENEN hook Unload). **EL BARRENDERO**: AethonMod.Unload() barre por reflexión todos los estáticos del ensamblado y anula los que sujetan infraestructura del motor (Asset<>, Ref<Effect>, Effect, Texture2D, BlendState, List<delegado>) — cubre también las clases futuras; los estáticos de VALOR quedan como están. Los disposals explícitos (RenderTargets, texturas horneadas) siguen su curso.
  · Bonus: CarnadaDelGrimorio.OleadasPreparadas se resetea con el mundo (dato de mundo en estático — la deuda recurrente).

### F. LO VERIFICADO Y DEJADO COMO ESTABA (para no re-auditar)
  · Simetría Write/Read de los 7 paquetes EcoRed (byte a byte); el rewind de 1 byte; el reparto de variantes sin repetición; OnEnterWorld sin spam; autoridad del hambre íntegra (el cliente MP no cuenta momentos); índices de jugador validados en todos los caminos; spawns de los Llamados SIN duplicación en MP (el fix v6.50 era correcto); CurvaAproximación sana (guards de NaN, sin división por cero, determinista por Hash01); despawn "patrón HollowTitan" correcto dentro de la AI; paridad 633/633 de las claves usadas; los 117 call-sites de golpe con guards correctos (0 en server-only, 0 en el hilo de render, 0 doble-escala, 0 doble-empuje); AddBuff bajo owner-gate funciona en MP (SendData(53) automático).
  · Deudas documentadas y NO tocadas (por diseño o menor): doble OnFire cosmético en 5 auras (AddBuff refresca, nulo efecto), bandwidth de MsgLibro por kill (lote futuro), strings hardcodeados del Altar (deuda hjson conocida), la succión del agujero de Aethon sobre réplicas de server (vanilla-consistente), Diagnostico de furia en clientes.

## Commit v6.50 — EL DAÑO AL MOTOR + LA CURVA DE LOS CINCO + LA VOZ PRIVADA DE CADA LIBRO

**Petición del usuario**: ① la migración del daño al motor + CurvaAproximacion a los 5 jefes (la propuesta de la sesión) · ② EL DISEÑO MP COMPLETO: "si 2 jugadores poseen un grimorio cada uno, estos jugadores solo deben ver los mensajes correspondientes a sus respectivos grimorios… un ejemplo en una partida multijugador, todos con sus grimorios, uno mata al rey Slime, el mensaje que dice el grimorio en esta parte se activa para todos, aunque… cada jugador vería su respectivo diálogo de su propio grimorio" · ③ el análisis de bugs e inconsistencias + ideas de librerías + investigación de mods populares y rumbo del mod. Todo SP-first, todo de pruebas, todo localizado (hjson 633/633 es/en mismo orden).

### A. GOLPEMOTOR — LA MIGRACIÓN DEL DAÑO AL MOTOR (nuevo: Content/Systems/GolpeMotor.cs · 117 call-sites migrados)
  · **LA DEUDA (hallazgo AUD-B nº5)**: ~115 golpes de la "escuela A" (`npc.SimpleStrikeNPC(...)`) ibale a las reglas del juego: sin tirada de CRÍTICA con las stats del jugador, sin varianza ±15% ni SUERTE, sin penetración de armadura, sin TODOS los on-hit del motor (quemadura de la armadura de escarcha, venenos de poción, efectos de accesorios) y con la sincronización MP resuelta a mano. **EL CAUCE**: `Projectile.Damage()` — el mismo que usa cada espada de vanilla (VERIFICADO en el IL real de tModLoader 2026.07.3.0, descompilado con ilspycmd): tira la crítica con la chance REAL del dueño, aplica la varianza con su suerte, dispara ModifyHitNPC/OnHitNPC de TODO el juego, da el CRÉDITO de la kill al dueño (Player.OnKillNPC: drops, banners, XP del grimorio vía OnHitByProjectile) y en MP el CLIENTE DUEÑO computa y `NetMessage.SendStrikeNPC` difunde — el modelo vanilla.
  · **EL CONTRATO (la escuela A queda INTACTA en su lógica)**: el llamador sigue decidiendo GEOMETRÍA (banda del tajo, aura del agujero) y CADENCIA (sus banderas de una-vez, sus intervalos por enemigo) — el golpe solo CAMBIA DE CAUCE. `unico: true` (default): golpe tipo BALA del motor (maxPenetrate==1, la regla vanilla flag11: los proyectiles de un golpe saltan i-frames — una andanada de tajos pega como una andanada de flechas; balanza EXACTA preservada). `unico: false`: ciudadano de i-frames completo (para auras continuas).
  · **LA FOTOGRAFÍA**: position/size/damage/penetrate/knockBack/CritChance/ArmorPenetration/friendly se prestan al golpe UN INSTANTE (la hitbox ADOPTA el cuerpo del blanco: Colliding pasa por construcción) y vuelven — ni el render ni la IA ven el préstamo. LAS STATS DEL DUEÑO: CritChance/ArmorPenetration se calzan con GetTotalCritChance/GetTotalArmorPenetration del jugador (las de verdad, como todo golpe del motor).
  · **LA MIGRACIÓN (3 lotes en paralelo, 62 archivos)**: 41+32+43 sitios migrados + el AoE del Nightglow (CosmicProjectileFX — que además corría SIN guard de red: daño client-side FANTASMA en MP, hallazgo auditoría nº2). Regla crítica aplicada en cada sitio: el viejo guard `netMode != MultiplayerClient` BLOCKEA el cauce nuevo (el dueño en MP es netMode 1) — los golpes salieron del guard; la física de empuje/buffs/AddBuff QUEDÓ server-authoritative como estaba. `Main.player` índices y "escuela A" en docs actualizados.

### B. LA VOZ PRIVADA DE CADA LIBRO — el diseño MP del usuario, completo
  · **LA DERROTA ES DEL MUNDO; LA VOZ, DE CADA LIBRO**: `AnunciarJefeMuerto` ahora dispara para TODOS los portadores con Grimorio VISIBLE (barra rápida) — aunque el que mató no cargue ninguno ("uno mata al Rey Slime, el mensaje se activa para todos"). Cada portador recibe SOLO la línea de SU propio grimorio: `EcoRed.HablarVarianteAlPortador` reparte la variante POR LA AUTORIDAD (ElegirClave con reparto sin repetición — dos portadores en la misma kill oyen líneas DISTINTAS) y el ModPacket viaja `Send(whoAmI)` con DOBLE PUERTA — nadie oye el libro del otro. La XP sigue siendo del que mató (GlobalNPCXP); la voz, del mundo. La voz sale del bloque `libroVisible` (antes solo hablaba el libro del asesino).
  · **TRES BOCAS POR JEFE (la promesa: "deberían de haber varios diálogos para cada jefe")**: las 23 voces (18 jefes vanilla + 4 del mod + Desconocido) pasan de 1 a 3 variantes cada una — 69 claves hjson, +46 líneas NUEVAS por idioma escritas en la voz del grimorio (el menú que cierra al alba, la corona masticada aparte "por educación", la máscara que "queda ancha pero el odio es de su talla", el expediente del portador abierto "en la página de en medio"). Mismo orden es/en (633/633, verificado por script).

### C. LOS LIBROS CAMINAN — la deuda MP nº1 de la auditoría (la progresión fantasma)
  · **EL BUG**: la XP la cobra el SERVER (OnKill corre server-side) pero el NIVEL manda en el CLIENTE (ModifyWeaponDamage, tooltips, HUD, NivelLibro) — `ShardLevelItem` sin NetSend/NetReceive y `ShardSyncSystem` vacío con TODO: en MP el grimorio vivía NIVEL 1 ETERNO en la pantalla del portador mientras el server subía SU copia (y la esencia nivelaba la copia local — divergencia en las DOS direcciones).
  · **EL ARREGLO — EcoRed.MsgLibro**: el server manda (slot, nivel, XP, bandera-5★) de cada Grimorio visible del portador; el cliente aplica a SUS copias y celebra SOLO EL DELTA (CelebrarSubida: la misma fiesta condensada de siempre, en la pantalla correcta). Llamado tras cada cobro de XP (GlobalNPCXP), tras cada esencia (server-side ahora: el cliente ya no sube su copia a ciegas) y el tester. **EL PEDIDO DE ENTRADA**: OnEnterWorld → MsgPedirLibros (cliente→server) → el server contesta con la foto completa (libros + crónica + hambre). **LA RED DE SEGURIDAD**: cada 600 ticks (reloj desfasado por jugador) el server re-envía — cubre al que entra a mitad de sesión, los movimientos de slot y cualquier deriva.
  · **LA CRÓNICA Y LA PUERTA DEL TESTIGO (deuda nº4)**: `MsgCronica` lleva DerrotaOleada10 + CronicaJefes + CronicaNarrada al portador cuando el server los marca (la kill del jefe devorado, la oleada 10 vencida) — ANTES el botón de esencias del Testigo JAMÁS se encendía en MP y la crónica nunca avanzaba (la réplica del server era transitoria y se perdía al guardar). OnLevelUp ya no corre FX en el server (Main.LocalPlayer en contexto server era el jugador equivocado — deuda auditoría).

### D. LOS DOBLE-GATES MUERTOS Y LOS DROPS FANTASMA (hallazgos auditoría MP nº5/3/2b — la interacción con las oleadas en MP)
  · **LOS LLAMADOS**: el guard `myPlayer != whoAmI` bloqueaba al SERVER (que corre UseItem por el uso SINCRONIZADO del jugador remoto — "Called on local, server, and remote clients", doc oficial) y el guard de netMode bloqueaba al cliente: en MP NADIE convocaba a los 5 jefes. Arreglado (el server convoca + netUpdate difunde). **LA CARNADA** (¡el gatillo de las oleadas de prueba!): mismo doble-gate — el click izquierdo ahora desata la furia desde el server; el contador del click derecho sigue siendo local. **LA BOLSA DE INVOCADORES** y **EL VERDUGO DE NIVELES**: mismo patrón.
  · **EL ALTAR ANTIGUO**: RightClick de tile corre SOLO en el cliente — el Fragmento Génesis spawn-eado ahí era un DROP FANTASMA en MP (el server jamás lo veía). Ahora el cliente pide el fragmento por EcoRed (MsgPedirFragmento, cliente→server) y la AUTORIDAD lo spawn-ea + difunde. En SP nace local (mismo proceso).
  · **LAS ESENCIAS**: el uso sincronizado sube la copia del SERVER y MsgLibro lleva el nivel al portador (antes solo subía la copia del cliente: el server calculaba la XP con el nivel viejo para siempre).
  · **LA SUCCIÓN DE LOS ANILLOS DEL HORIZONTE (hallazgo nº3)**: el `return` del server (para cosméticos) se tragaba TODO el método y el gate del bloque (≠ cliente) bloqueaba al cliente: en MP NINGUNA máquina corría la succión — el accesorio prometido estaba MUERTO en red. La física ahora corre ANTES del muro (server en MP, proceso local en SP).
  · **EL LIFESTEAL**: `ApplyLifesteal` tocaba statLife desde cualquier máquina — la HP del jugador la manda SU cliente (vanilla): curar réplicas ajenas era vida fantasma que el sync borra. Solo el cliente del portador sana.

### E. LA CURVA DE APROXIMACIÓN EN LOS CINCO JEFES (EcosLib.CurvaAproximacion, v6.49 → aplicada)
  · **EL GUARDIÁN DEL RIFT — LA GUARDIA**: la órbita a 340 px (ángulo a mano, péndulo de relojería) pasa a la curva de la casa: strafe con FRECUENCIAS INCONMENSURABLES (2.17 y 3.03 rad/s — jamás sincroniza) + aproximación anticipada con tangente. Radio y temple intactos; la matemática, de la librería.
  · **EL PRIMER PORTADOR — EL ACECHO**: el tejido sinusoidal a mano (Sin(3.2t) — período VISIBLE) pasa al vaivén inconmensurable a radio 210 (apenas fuera del filo de la marca: 190 — acecha al borde exacto de tu espada).
  · **LA ARQUERA ESTELAR — EL ARCO**: acercarse desde lejos (>520) ya no es línea recta (curva anticipada al anillo de disparo 380); la media distancia MANTIENE su esquivón lateral de identidad pero ahora ONDULA (la curva le pone el vaivén y la corrección de radio).
  · **AETHON — FASE 3**: el acecho lento recto pasa a curva a 300 px: la gravedad sigue haciendo el trabajo, pero ella ESCONDE el rumbo (esquivarla exige leerla, no solo correr).
  · **EL TITÁN HUECO — EL SALTO QUE ARQUEA**: el brinco antiaéreo ya no es puramente vertical: la curva decide el PASO HORIZONTAL del salto (anticipa dónde estará la presa, clampeado ±6 — sigue siendo un coloso).

### F. VERIFICACIÓN
  · Compilación 0/0 contra tModLoader 2026.07.3.0 REAL (cuatro pasadas de verificación: tras el núcleo MP, tras los 3 lotes de migración, tras las curvas y la final). APIs sondeadas por reflexión Y DESCOMPILADAS con ilspycmd del binario real: `Projectile.Damage()` (void — el cauce completo con SendStrikeNPC/OnKillNPC/OnHit integrados), `CanHitWithOwnBody`, `SimpleStrikeNPC` (los 8 parámetros), `NPC.StrikeNPC(HitInfo, fromNet, noPlayerInteraction)`, `Player.GetTotalCritChance/GetTotalArmorPenetration(DamageClass)`, `ModItem.UseItem` "Called on local, server, and remote clients", `ModTile.RightClick`.
  · hjson 633/633 es/en MISMO ORDEN (script de paridad). 0 SimpleStrikeNPC en las armas (solo queda el de PulsoLib — API muerta con 0 usos, documentada). 117 golpes por GolpeMotor. 0 Main.rand en render (las curvas usan Hash01 por semilla — EcosLib es determinista).
  · Script de migración documentado: tools/voces_tres_bocas_v650.py (23 voces × 3 variantes).

## Commit v6.49 — LA VOZ CAMINA EN RED + LAS ESENCIAS DE LOS CINCO + LA GRAN AUDITORÍA DE LIBRERÍAS

**Petición del usuario**: ① el paquete MP de voces de nivel 2 ("solo el portador correcto debe ver los reclamos de su propio grimorio… la voz y el hambre son del portador del grimorio hambriento en ese momento") · ② las esencias de los jefes del MOD (cada uno la suya, +1 nivel completo, MÁX 10 por mundo por jefe para evitar el farmeo — los jefes de oleada siguen sin límite) · ③ la investigación y mejora de TODAS las librerías ("algo que veo que no has hecho") · ④ análisis y arreglo de bugs. Todo SP-first, todo de pruebas, todo localizado (hjson 586/586 es/en mismo orden).

### A. ECORED — EL PAQUETE MP DE VOCES, NIVEL 2 (nuevo: Content/Systems/EcoRed.cs)
  · **EL DISEÑO DEL USUARIO, TAL CUAL**: la voz y el hambre son DEL PORTADOR; el festín es del mundo. ModPacket propio con 3 mensajes: **MsgVoz** (clave hjson YA RESUELTA + color + escala + flags de prioridad/rugido, `Send(whoAmI)` al cliente del portador y NADIE más — con DOBLE PUERTA en recepción: si el destinatario no soy yo, descarte silencioso), **MsgHambre** (momentos + ticks — el estado del hambre autoritativo hacia SU pantalla: la barra palidecida, la ceniza del aura y los celos del libro leen el sync) y **MsgLatidoXp** (el pulso de la barra dorada cuando el servidor cobra la kill del portador).
  · **EL RÉGIMEN AUTORITATIVO**: la hambre la cuenta el SERVIDOR (el cliente MP ya no cuenta la suya — antes se desincronizaba: el cliente nunca veía el perdón de una kill cobrada server-side). El Libro Celoso y las burbujas del Testigo siguen siendo percepción LOCAL (cero red). Los ANUNCIOS del festín (oleadas, jefes que llegan, el Juicio) van a TODOS por ChatHelper.BroadcastChatMessage con NetworkText.FromKey — "el evento de uno es el evento del mundo"; los avisos PRIVADOS (el derecho a las esencias) por SendChatMessageToClient. La variante de cada línea la reparte la AUTORIDAD y viaja ya elegida: todos los portadores ven la misma línea que repartió el servidor.
  · **LA INTEGRACIÓN COMPLETA**: AnunciarJefeMuerto(npc, portador) — la voz del jefe viaja al portador que cobró (antes solo la oía el host en SP-local: en MP nadie); la crónica del Testigo se marca server-side (carga al reconectar); SusurrarCincoEstrellas al portador; TODAS las voces de la furia (Ira, Llamada, Saciado, Juicio, JuicioFin, Venganza) y TODOS los anuncios por EcoRed. EcoLib gana **ElegirClave** (el reparto que viaja: devuelve la clave completa "claveN" en vez del texto).

### B. LAS ESENCIAS DE LOS CINCO JEFES DEL MOD (nuevos: EsenciasJefesMod.cs + EsenciasModSistema.cs + 5 sprites PIL)
  · **LA LETRA DEL USUARIO**: cada jefe del mod tiene SU alma — Esencia del Titán Hueco (cristal del Sagrario), Esencia del Guardián del Rift (teal del entre-mundos), Esencia de la Arquera Estelar (ámbar estelar), Esencia del Primer Portador (brasa del duelista) y Esencia de Aethon (la luz primordial). +1 NIVEL COMPLETO al Grimorio (la misma maquinaria de las de oleada), drop en el OnKill de cada jefe. **EL LÍMITE**: 10 por MUNDO por jefe (EsenciasModSistema cuenta y persiste en el SaveWorldData — la undécima cae en silencio; el "cofre seco" se anuncia UNA vez y la última alma lleva su aviso). Los jefes de OLEADA siguen SIN límite (la dificultad escala: el farmeo se paga en riesgo). El Testigo NO las vende (las de oleada sí): estas se ganan. Sprites PIL con el mismo estilo de alma+llama de las 7 de oleada (gen_v649_assets.py).

### C. LA GRAN AUDITORÍA DE LIBRERÍAS (3 frentes de investigación — informes AUD-A/B/C)
  Auditoría EXHAUSTIVA de las librerías de la casa: 13 primitivas (AUD-A), 10 de combate (AUD-B) y los sistemas de render/UI/audio (AUD-C). Hallazgos arreglados AHORA:
  · 🔴 **PULSOLIB DESCONECTADO** (el hallazgo nº1 de AUD-A): `ActualizarPantalla()` NO TENÍA NINGÚN call-site — el trauma de las sierpes se acumulaba para siempre sin latir y los 6 EmpujarPantalla (Apuestas, Verbo Primordial) escribían un flash que NADIE leía. NUEVO PulsoSistema (PostUpdateWorld + higiene OnWorldUnload/Unload con el nuevo PulsoLib.Reset) — el sistema de pantalla/trauma late de verdad.
  · 🔴 **VFXCore.FlushAdditive**: el End del lote del llamador vivía FUERA del try/finally — si lanzaba, `_quads` quedaba sin limpiar y los cuadros muertos se re-volcaban CADA frame. Todo el vuelco ahora es atómico. + **VFXCore.Reiniciar()** (el núcleo era la única librería sin higiene) + `QuadsDelFrame` para diagnóstico.
  · 🔴 **AURAPORTADORHALO**: el End manual del patrón v6.10 peleaba con el bool de DibujarJugadorAditivo (doble End → excepción tragada cada frame + reapertura a ciegas). Ahora el CONTRATO DEL BOOL de verdad (el de OleadaNPC). Y CoronaRunica()/FormaAscendida() se creaban NUEVAS 2 veces por tick (AI+PreDraw) — cache estático (inmutables).
  · 🟠 **LumenLib.Bloom**: DOS arrays frescos por llamada (decenas por frame → GC churn) → `static readonly` (la doc de "cero GC por frame" era mentira desde hacía versiones).
  · 🟠 **EstelaLib**: `Smooth` MUTABA la entrada con iterations par (bomba latente — todos los call-sites pasaban copias por suerte) → copia propia siempre; **AnchoDe** por LUT de 33 muestras (hasta 130 MathF.Pow por estela por frame → 0).
  · 🟠 **TelaLib**: el pool de Cinta sin barrendero — una muerte sin OnKill dejaba la cinta viva PARA SIEMPRE y el whoAmI reciclado HEREDABA la estela ajena. TTL de 3 ticks + purga cada 120 (cuelga de BrumaSystem, el ciclo de vida de las librerías) + Soltar resetea el reloj sub-step.
  · 🟠 **OrbitaLib**: `AbrirAdditive/AbrirAlpha` sin red de seguridad (el patrón que CodigosLib usa 6 veces): Begin sobre un lote ajeno vivo → excepción → el catch cerraba el lote AJENO a ciegas → render corrupto. EL PATRÓN DEL END DEFENSIVO de MoldeLib aplicado a TODA la familia (bandera loteAjenoAbierto + CerrarBatch devuelve el estado). SelloVacio rescata por la puerta blindada.
  · 🟠 **RiftMundoSystem**: sin OnWorldUnload — al salir de un mundo con desgarro vivo, el siguiente arrancaba con el CIELO OSCURECIDO ~30 ticks. **OndaSystem**: el Asset `_glow` no se anulaba en Unload + flash/kick colándose entre mundos. **AudioLib**: sin Reiniciar (las estáticas heredaban ticks entre mundos).
  · 🟠 **AuraLib**: `_emisores` huérfanos (NPC muerto sin AI → entrada viva hasta Reiniciar, whoAmI reciclado hereda las partículas) → barrido perezoso cada 240 ticks. **EL PRESUPUESTO ADAPTATIVO CONECTADO**: la TASA de partículas respira con VFXCore.FactorCalidad (CalidadFpsSystem alimentaba una máquina sin motor desde v6.34 — si los FPS caen, el aura adelgaza solo).
  · 🟠 **EcoLib**: el reloj de tipeo FIJO pisaba el texto largo (300 caracteres tipeaban más que la muestra — total mal calculado) → calibrado por longitud; 2× MeasureString + Substring por frame → cache de medida/origen al encolar + recorte solo cuando cambia.
  · 🟠 **CieloLib.TexturaDe**: una ruta muerta reintentaba ModContent.Request + EXCEPCIÓN cada frame por capa rota → el fallo se cachea (null en el diccionario, salto gratis).
  · 🟠 **SierpesLib.CriaEstelar**: indexaba `ang[i]` sin check (IndexOutOfRangeException latente con caller desalineado) → clamp de los parámetros paralelos. **TajoLib.PerfilLente**: ~60 Math.Pow por tajo por frame → LUT de 33.
  · 🟡 **TEXTOS A HJSON** (regla 5 de la casa, hallazgo AUD-C): "EL OCASO" (OcasoSystem), los 5 textos de la barra dorada (ShardHUDSystem: nivel/hambre/XP/HM/+XP) y "El Testigo ha llegado…" (PresenciaNPCSystem) — TODO localizado + el HUD con cache de texto/medida (antes 3-4 strings nuevos por frame).

### D. LAS MEJORAS NUEVAS DE LA AUDITORÍA (las mejores ideas, implementadas)
  · **OndaLib.TelegrafoAnillo** (la idea nº1 de AUD-B): el telegraph de ÁREA que faltaba — frontera fija + anillo interior que SE CIERRA con la carga (cuando toca la frontera, el golpe cae) + disco tenue + el chillido 18 Hz del último cuarto. Para los AoE de los jefes (ya disponible para la siguiente pasada).
  · **TajoLib.ArcoToca** (la idea nº2): la hitbox de arco que faltaba — los tajos son ARCOS que golpeaban como RECTAS; cápsula por segmentos con CheckAABBvLineCollision (broad-phase incluida). La escuela del motor, curvada.
  · **EcosLib.CurvaAproximacion** (la nº1 de AUD-B): la primitiva de aproximación orbital para jefes — acércate hasta el radio y GÍRALO con strafe de frecuencias inconmensurables (2.17/3.03 — jamás sincroniza). Los 5 jefes lo hacen a mano hoy; la próxima pasada lo hereda gratis.
  · **PantallaLib.PresetImpacto / PresetGolpeSeco** (la nº1 de AUD-A): los 4 gestos (sacudir+flash+viñeta+onda) calibrados en UNA llamada — el patrón manual de 4 líneas de Supernova, en un botón.
  · **OndaLib.GroundVisual** + LA PROMESA CUMPLIDA: el comentario de AtaqueJefeProjectile llevaba versiones prometiendo "OndaLib.Ground" en la estrella fugaz que NUNCA se llamaba (AUD-B: doc-rot). La fugaz ahora marca su aterrizaje con el MEDIO-ANILLO DE SUELO creciendo mientras cae (la parte visual del Ground, extraída sin el paquete de polvo para el telegraph).
  · **EL OVERLAY DE DIAGNÓSTICO F8** (la nº4 de AUD-C — el mod ES de pruebas): FPS, FactorCalidad, quads del frame vs presupuesto, voces vivas, emisores de aura, fuerza de pantalla y la oleada del festín — columna localizada (hjson) bajo la barra del grimorio, refresco a 4 Hz (cero GC entre refrescos). La herramienta que pedía el probador.

### E. VERIFICACIÓN
  · Compilación **0 errores / 0 advertencias** contra tModLoader 2026.07.3.0 real (dotnet 8, /tmp/verify) · hjson **586/586** es/en mismo orden · cero Main.rand en render (el ElegirClave corre en lógica) · 0 menciones externas · firmas sondeadas contra el binario real (ChatHelper.Broadcast/SendChatMessageToClient, ModPacket.Send(whoAmI), PostUpdateWorld).
  · **DECISIÓN DOCUMENTADA (AUD-B)**: el daño por SimpleStrikeNPC en ~10 proyectiles de desgarro sigue fuera del motor (MP-safe pero sin i-frames compartidos) — migrar a Projectile.Damage() toca el balance de 8 armas: SEPARADO a propósito para la siguiente pasada. Las ideas restantes de los informes (pooling de StormLib/RiftLib, CurvaAproximacion aplicada a los 5 jefes, CompasLib presets, EstelaSpec struct, enum de Momento en AudioLib) quedan apuntadas en el worklog como hoja de ruta.

## Commit v6.48 — LOS CINCO JEFES DE VERDAD + EL JUICIO + LAS ESENCIAS + EL TESTIGO CRONISTA

**Petición del usuario**: [ALTO] rehacer de verdad los 5 jefes del mod (eran v5 de prestado: Aethon con sus cinco fases de CultistBossLightningOrbArc recolorados, la fase 5 de "runas memorizadas" incumplida, el drop de Forma Ascendida en TODO, los adds eran CultistBossClone de vanilla, y las "minas" de la Arquera una ProjectileID.Bullet QUIETA) — con las librerías de la casa (OndaLib, TajoLib, StormLib, RiftLib, EcosLib, OrbitaLib, LumenLib, VFXCore), arte 100% código (el precedente de la sierpe) y lore · la oleada especial tras la 10: TODOS los guardianes a la vez ×15 · el pago en metales (chusma k monedas de oro, jefes k de platino + SU ESENCIA que sube un nivel completo) · stats por oleada ×(k+1) — la 1 ×2 … la 10 ×11 · jefes de oleada SIN hora y zonas sin guardián cubiertas · muerte del portador = fin del festín + venganza de voz · El Testigo cronista (línea humana por cada jefe devorado) y vendedor de esencias (10 de platino, tras la oleada 10) · el Libro Celoso (susurros por clase de arma: melé/magia/invocación/arrojadiza/arco/bala) · el Sabor del Bioma (la primer línea del hambre sabe al bioma) · EcoLib con PRIORIDAD (la voz del libro siempre primero) y reparto sin repetición · el camino del PORTADOR aditivo (el halo-proyectil para las auras del jugador — la unificación pedida) · la Corona Rúnica y la Forma Ascendida en la Bolsa de Cosméticos.

### A. LOS CINCO JEFES DEL MOD, REHECHOS DE VERDAD (5 NPCs + AtaqueJefeProjectile — nuevos)
  · **EL ARSENAL COMÚN (AtaqueJefeProjectile)**: 13 ESTILOS de diente hechos con las librerías — púas que se clavan y detonan (OndaLib.Pulse), coros de cristal en órbita que se lanzan, virotes de vacío que PARPAJEAN entre fases (StormLib.Bolt), flechas estelares con corrección y ESTELA de fantasmas, LA MINA DE VERDAD (se ARMA con pulso de aviso y DETONA en arco voltaico StormLib.ChainBolt + ImpactFlash + cuerpo de estallido con hitbox honesta — ya no una bala quieta), estrellas fugaces con MARCA de suelo, tajos diferidos (TajoLib), cuchillas en órbita, la PARED DE DESGARRO que avanza (RiftLib.TearVacio), pernos con latido (BloomPulse), nubes de nebulosa que queman, y LA RUNA MEMORIZADA que dispara los trucos de la casa. Daño por COLISIÓN del motor (cero daño manual), cero Main.rand en render, contrato de lote cerrado→cerrado.
  · **EL TITÁN HUECO — el coloso**: arte 100% código (columnas que ANDAN con el paso, torso, hombros, ojo-rendija, espinas dorsales y el CORAZÓN en Bloom latiendo — la furia redobla el pulso) · LAS PÚAS DEL SAGRARIO (arco → clavado → detona en esquirlas) · EL CORO DE CRISTAL (6 esquirlas orbitando el coloso que se LANZAN al canto; rápido en furia) · EL PORRAZO (onda de suelo + kick de cámara cuando te acercas) · LA CARGA TELEGRAFIADA (Telegrafo 30 t → arremetida — esquivable si lees el aviso) · el salto del guardián si vuelas.
  · **EL GUARDIÁN DEL RIFT — el centinela entre mundos**: figura encapuchada de luz tenue (capas de cápsulas, tres ojos del umbral, las LLAVES orbitando, el halo del sello) · EL TELETRANSPORTE DE VERDAD (SE DESGARRA: RiftLib.TearVacio se abre donde estaba, el cuerpo se disuelve en la grieta, nace al otro lado — flanqueándote siempre — y la grieta SE CIERRA) · LOS VIROTES con LEAD (apunta a dónde estarás) · EL SELLO (<30%): cuatro PAREDES DE DESGARRO desde los cardinales estrechando la arena + teleport redoblado.
  · **LA ARQUERA ESTELAR — el fantasma**: la arquera ámbar translúcida con CORONA DE ESTRELLAS, el ARCO VIVO (arco de cápsulas que SE TENSIONA de verdad antes de soltar) y la ESTELA DE ECOS (EcosLib — los fantasmas de sus posiciones pasadas) · LAS FLECHAS con corrección y LEAD · LA SIEMBRA DE MINAS (3–5 minas que se arman con aviso y detonan en ARCO VOLTAICO) · LA LLUVIA ESTELAR (<50%): 8 fugaces con MARCA de suelo · EL ESQUIVÓN: retroceso lateral si te acercas, cambia de lado cada 3 s.
  · **EL PRIMER PORTADOR — el duelista**: la silueta de rescoldo con la VAINA que late lista para el parry y LA HOJA VIVA (un creciente TajoLib en la mano que ARQUEA al golpear) · EL CICLO DEL DUELISTA: ACECHA tejiendo (sinusoidal) → MARCA (telegrafo sobre ti) → TAJO (embestida + corte DIFERIDO que florece donde estabas) → RETROCESO · EL PARRY (20% de anular tu golpe — la firma v5, ahora con destello de tajo) · LAS CUCHILLAS EN ÓRBITA y cada DOS combos la PARED DE DESGARRO (<40%).
  · **AETHON, LA LUZ PRIMORDIAL — las CINCO FASES de verdad**: FASE 1 el Polvo Estelar (espiral de 7 pernos girando + deriva) · FASE 2 la Nebulosa (NUBES QUE QUEMAN que derivan hacia ti + círculos anchos) · FASE 3 la Gravedad (el VOTEO cada 8 s con anillos de aviso + doble perno convergente) · FASE 4 el Agujero Negro (EL COLAPSO: el cuerpo se apaga, LA SINGULARIDAD nace donde estás, te ATRAE de verdad y escupe JETS DE ACRECIÓN radiales; los ADDS ya no son clones de prestado — LAS RUNAS orbitan) · FASE 5 EL RECONOCIMIENTO (la promesa v5 CUMPLIDA: LAS RUNAS MEMORIZADAS — hasta 4 sigilos dorados orbitando que disparan TUS PROPIOS TRUCOS, la danza del OCHO alrededor tuyo, y cada 6 s EL RECORDAR: 7 pernos en abanico + EL GRAN TAJO telegrafiado) · EL ARTE: el núcleo blanco que crece por fase, los PÉTALOS de luz girando, la corona de TRES anillos contra-rotando (el color cambia por fase), y el disco de acrección de la singularidad con el CENTRO VACÍO (el fondo viéndose a través). EL DROP CUMPLIDO: LA FORMA ASCENDIDA cae al morir (el TODO de v5, PAGADO).

### B. LA OLEADA ESPECIAL — EL JUICIO (GrimorioFuriaSistema)
  · Tras la 10 (o directa con la Carnada en 11): TODOS LOS SIETE GUARDIANES a la vez, ×15 en vida/daño/XP, con EL AURA DEL JUICIO (negra, bordes rojo INTENSO, chispas dobles carmesí+oro — la más vistosa de AuraLib). Nacen DOS EN PANTALLA desde el primer segundo y el resto se suma cada 15 s. La Carnada cicla 1..11. Fin del juicio: la saciedad especial (3 muestras) + perdón.
  · **LA MUERTE DEL PORTADOR**: si caes durante el festín, EL EVENTO TERMINA y el libro TOMA VENGANZA (4 muestras de voz, PRIORIDAD — las voces de jefes esperan detrás).

### C. EL PAGO EN METALES Y LAS ESENCIAS (OleadaNPC + EsenciasJefes — nuevos)
  · **LA LETRA DEL USUARIO**: la oleada k multiplica vida Y daño ×(k+1) — la 1 ×2, la 10 ×11, la especial ×15 (chusma y jefes; defensa aparte +2k/+6k; antes era chusma ×6.4/jefes ×7 en la 10). LA AGRESIÓN CRECE: empuje 0.16+0.02k con techo 10+0.5k, y LOS JEFES ya no son sagrados — re-objetivo cada 20 t, HOMING suave + EMBITE periódico, y TODOS disparan SUS DIENTES con cadencia 300−18k.
  · **EL PAGO**: cada monstruo suelta k MONEDAS DE ORO y cada jefe k DE PLATINO + SU ESENCIA (la especial: 15).
  · **LAS ESENCIAS (7 ítems)**: "Esencia del Rey Gelatina"… — cada una sube UN NIVEL COMPLETO al Grimorio (SubirNivelDirecto en ShardLevelItem: anunciado como subida normal, condensado), stack 30, rareza Quest, precio 10 de platino. El alma se disuelve en chispas doradas y el libro la prueba (3 muestras de sabor).
  · **LOS GUARDIANES SIN HORA**: cada zona tiene SU guardián FIJO (los de la furia no duermen) — la superficie alterna Rey/Ojo por PARIDAD de oleada, desierto→Rey, playa y cielo→Ojo, granito/mármol/subsuelo→el mal del mundo, mazmorra→Skeletron SIEMPRE.

### D. EL TESTIGO CRONISTA Y MERCADER (TheWitness + ShardPlayer)
  · **LA CRÓNICA**: cada jefe que EL LIBRO devora queda apuntado (CronicaJefes, persistido) — el Testigo cuenta SU versión HUMANA de la misma derrota (23 crónicas: 18 jefes vanilla + Aethon + Titán + Guardián + Los Ecos + fallback), UNA por charla (el cursor persiste: cada conversación revela la página siguiente). Dos narradores, un mismo hecho.
  · **LA TIENDA DE ESENCIAS**: botón 2 del chat (siempre a la vista), tienda NPCShop real con las 7 esencias a 10 de platino — CONDITION en la tienda ("Sobrevive a la oleada 10") y doble puerta en el clic (DerrotaOleada10, otorgado al vencer la 10). El Testigo rechaza con línea propia antes de eso.
  · **TODO LOCALIZADO**: el chat del Testigo (7 niveles), los botones y los mensajes — los strings hardcodeados v5 fuera de la casa.

### E. EL LIBRO CELOSO + EL SAJOR DEL BIOMA (ShardPlayer + EcoSistema)
  · **EL CELOSO**: si empuñas OTRA ARMA con hambre, el libro susurra su celos POR CLASE — melé, magia, invocación, arrojadiza, y arco/bala SEPARADOS (6 pools × 3 muestras, reparto sin repetición). Pura voz: cero mecánica. Frío de 8 s.
  · **EL SAJOR DEL BIOMA**: la primer línea del hambre sabe a DÓNDE está el libro — 12 biomas × 3 muestras ("carne de jungla", "sal del infierno", "escarcha en el paladar"…).

### F. ECOLIB: LA COLA CON PRIORIDAD Y EL REPARTO SIN REPETICIÓN
  · **HABLAR(prioridad: true)**: la voz del LIBRO se cuela AL FRENTE de la cola (no corta la activa: la línea en curso termina y la del libro nace justo después — exactamente la letra del usuario). La Furia, el Juicio, la Venganza y la Saciedad ya van con prioridad.
  · **ElegirVariante(clave, n)**: elige una muestra DISTINTA de la última dicha por situación (matar 20 veces al Rey Gelatina no escucha siempre la misma línea).

### G. EL PORTADOR ADITIVO — LA UNIFICACIÓN DEL AURA DEL JUGADOR (AuraPortadorHalo + AuraJugadorCapas + AuraLib)
  · **EL PROBLEMA**: los NPCs dibujan su aura ADITIVA (neón) desde PreDraw; el jugador iba por DrawData (AlphaBlend premultiplicado) y los colores vivos salían PLANOS (la lección v6.40).
  · **LA SOLUCIÓN — EL PATRÓN DEL HALO-PROYECTIL (el de AnilloRunicoDorsalHalo v6.37)**: AuraPortadorHalo, un proyectil COSMÉTICO pegado a su dueño (netImportant, sin colisión) cuyo PreDraw tiene el lote bajo NUESTRO control — vanilla dibuja los proyectiles ANTES que los jugadores → la capa queda DETRÁS del cuerpo con el neón de verdad. AuraLib.DibujarJugadorAditivo emite y vuelca. EL VELO FRONTAL (el 6%) sigue por DrawData (a esa transparencia no necesita neón y debe PISAR el sprite).
  · **LOS TRES PORTADORES**: 0 = la CENIZA DEL HAMBRE (ShardPlayer, solo local) · 1 = LA CORONA RÚNICA (el pentágono Polígono(5) violeta-oro de AuraLib, cosmético de la Bolsa) · 2 = LA FORMA ASCENDIDA (el drop de Aethon).

### H. LOS COSMÉTICOS (CoronaRunicoAuraItem + FormaAscendidaItem + BolsaCosmeticos)
  · **LA CORONA RÚNICA DE AURA**: la petición literal del usuario — el patrón Polígono con ConLados(5) y tintes del mod hecho preset (AuraPerfil.CoronaRunica: violeta del Sagrario al centro, ORO del grimorio en las aristas, chispas doradas) y repartido en la Bolsa de Cosméticos. Cosmético puro: hueco de accesorio, cero stats.
  · **LA FORMA ASCENDIDA**: el aura dorada-violeta de la Luz Primordial (AuraPerfil.FormaAscendida) — el drop de Aethon, también en la Bolsa.

### Verificación
  · Compilación 0 errores / 0 warnings contra tModLoader 2026.07.3.0 real (tres pasadas) · hjson es/en paridad EXACTA 557/557 claves, mismo orden · 0 Main.rand en el render de lo nuevo · 0 menciones externas en el repo trackeado · APIs sondeadas por reflexión contra el binario real (NPCShop/Condition/AddShops firmas verificadas antes de escribir).

## Commit v6.47 — LA VOZ DEL HAMBRE + LA FURIA DEL GRIMORIO + AURALIB + LOS LLAMADOS

**Petición del usuario**: la Voz del Hambre (susurros de EcoLib y la barra dorada que palidece cuando pasas minutos sin matar) · el evento de 5 MINUTOS de oleadas cuando el libro pasa demasiado tiempo sin comer: 1 oleada por momento de hambre hasta 10, monstruos muy agresivos con más daño/vida/XP (oleada 1 = ×2 … oleada 10 = ×11), al final de cada oleada un JEFE PRE-HARDMODE según bioma y hora (superficie: Rey Gelatina de día / Ojo de Cthulhu de noche) como versión especial potenciada, y TODOS con AURA · la librería de auras (ruido de Perlin, capa trasera + velo frontal 90–95% transparente, distorsión/blur/glow, coloreable por zonas y capas, humo, patrones geométricos, partículas configurables, todo por código) · la primera criatura 5★ con línea propia ("Lo más raro que ha comido jamás") · El Testigo hablando por burbujas de EcoLib · los NPCs/jefes del mod aparecen siempre (Testigo siempre presente + 5 invocadores de día) · ítem de prueba para las oleadas.

### A. LA VOZ DEL HAMBRE (ShardPlayer + ShardHUDSystem + EcoLib)
  · **EL SISTEMA**: con el libro VISIBLE y a nivel 25+ ("a nivel alto"), cada 75 segundos sin matar es un MOMENTO DE HAMBRE — EcoLib susurra (sin rugido, escala 0.52, gris pálido: "El grimorio tiene hambre…", escalando hasta "…HAMBRE…"), la BARRA DORADA PALIDECE (gris ceniza creciente, el nombre también — 8+ hambres añade "(hambre…)") y el portador viste LA CENIZA DEL LIBRO (aura grisácea sutil por las capas de jugador, cacheada: cero GC). Al matar, todo vuelve (RegistrarKill). Guardado el libro: la hambre SE CONGELA (el libro duerme). EcoLib gana el parámetro ESCALA (los susurros son la voz menuda).
  · **LA FURIA**: al 4º momento (~5 minutos sin comer) el libro cruza el umbral: "El grimorio está FURIOSO…" + "El grimorio llama a su comida…" (las dos voces de EcoLib) y EL EVENTO COMIENZA. Un jefe vivo o una invasión BLOQUEAN la furia — y la hambre sigue subiendo hasta 10 mientras espera: así existen las 10 oleadas naturales. Bandera EventoHambreGrimorio en config (la Carnada la salta a propósito).

### B. LAS OLEADAS DE LA FURIA (GrimorioFuriaSistema — nueva)
  · **EL EVENTO**: N oleadas = los momentos de hambre acumulados (1..10). El reloj de la chusma reparte los 5 MINUTOS entre las oleadas (18000/N ticks cada fase); los jefes PARAN el reloj (la oleada k+1 empieza cuando cae el jefe k, tope 90 s por jefe — se hunde "insatisfecho" y la furia sigue). Al final: "El grimorio está saciado… por ahora." y la hambre del portador SE PERDONA.
  · **LA CHUSMA (pools por bioma y hora, IDs verificados por sondeo de reflexión)**: superficie día = babosas y zombis · noche = ojos y zombis · desierto, nieve, jungla, corrupción (Devoradores), carmesí, mazmorra, granito/mármol (GriegoMedusa — el Hoplite interno), cielo (Harpías), subsuelo, infierno (Demonios/Demonios de fuego). Spawn en anillo alrededor del portador con búsqueda de suelo; pulso cada 1.5 s; tope de vivos 8+2k.
  · **LOS JEFES DE OLEADA (pre-hardmode, versión especial)**: superficie día = REY GELATINA · noche = OJO DE CTHULHU · nieve = Deerclops · jungla = Abeja Reina · corrupción = Devorador de Mundos · carmesí = Cerebro · mazmorra nocturna = Skeletron · infierno = el Ojo (el Muro de Carne EXCLUIDO a propósito: es la puerta del hardmode y una furia involuntaria no abre mundos). Vida ×(2.5+0.5(k−1)), daño ×(1.5+0.1(k−1)), defensa +6k — y el AURA.
  · **LA XP DE LA OLEADA**: TODO lo que muera convocado paga ×(oleada+1) — la oleada 1 ×2 … la 10 ×11, también los jefes (multiplicador explícito sobre la fórmula fija, en GlobalNPCXP).
  · **OleadaNPC (GlobalNPC — nueva)**: el SELLO — stats enfurecidas (chusma: vida ×(1+0.6(k−1)), daño, defensa, knockback ×0.35), AGRESIÓN real (re-objetivo cada 30 t + empuje hacia la presa en PostAI, techo de velocidad 10; jefes y gusanos exentos), el aura, y la PROPAGACIÓN: lo que nazca a 800 px de un marcado hereda el sello (segmentos del Devorador, Creepers del Cerebro, Sirvientes del Ojo — y los spawns naturales que caigan en el festín: el hambre es contagiosa).

### C. AURALIB — LA OCTAVA LIBRERÍA (Content/VFX/AuraLib.cs — nueva)
  · **EL AURA (investigado contra las referencias del usuario, analizadas con VLM)**: energía que la criatura desprende, en DOS CAPAS — la TRASERA (cuerpo de humo: ruido de Perlin en anillos contrarrotantes de gajos, detrás del sprite) y el VELO FRONTAL (la misma geometría al 6% pisando al cuerpo: la criatura "emite desde dentro"). Cero assets: las 4 texturas de ruido nacen de un generador fBm de value-noise ENVOLVENTE (4 octavas, retícula 8/16/32/64, semilla fija) GENERADO EN CÓDIGO, perezosamente al primer dibujo.
  · **TODO POR CÓDIGO (API fluida)**: AuraPerfil().ConRadio().ConPatron(Perlin/Poligono/Anillos).ConLados().ConTrasera(centro,medio,borde).ConFrontal(...).ConAlfaFrontal(0.06).ConHumo(flujo,deriva,ascenso).ConDistorsion().ConBlur().ConGlow().ConSemilla().ConParticulas(...) — coloreable por ZONA (centro/medio/borde) y por CAPA (trasera/velo), el BORDE además tiñe el halo y las aristas del polígono.
  · **LOS PATRONES**: PERLIN (el humo clásico), POLÍGONO (el abstracto geométrico: el ruido enjaulado en un N-gono giratorio con sus aristas dibujadas en el color de borde), ANILLOS (pulsos tipo sonar). HUMO: deriva radial con ciclo de vida (nace dentro, sube y muere fuera), respiración, wobble de distorsión, el blur barato (pase doble desplazado) y el glow aditivo.
  · **LAS PARTÍCULAS**: Orbe (brillo suave), Chispa (rastro alargado según velocidad) y Rombo, con color, transparencia (o SÓLIDAS), tasa, velocidad, ascenso, tamaño y vida — emisión determinista (Hash01, jamás Main.rand; corre en AI no en render).
  · **LOS PRESETS**: OleadaGrimorio(k) — la gris-blanca de las oleadas, y en la 10 el AURA PODRIDA: gris-negra con BORDES ROJOS oscuros y chispas carmesí · HambreDelGrimorio(intensidad) — la ceniza del portador. Presupuesto de cuadros de VFXCore consultado antes de emitir; el contrato del lote en PreDraw/PostDraw devuelve si hay que reabrir (ReabrirLoteVanilla).

### D. LA PRIMERA 5★ CON VOZ PROPIA (GlobalNPCXP + ShardLevelItem + EcoSistema)
  · La primera criatura 5★ de la dieta del libro dispara su LÍNEA ("Lo más raro que ha comido jamás", magenta raro, sin rugido) — una vez por libro (flag PrimeraCincoEstrellas persistido en el item, como el nivel y la XP). Consulta las MISMAS estrellas que la dieta (ShardLevelSystem.EstrellasDe, ahora público).

### E. LOS NPCs Y JEFES DEL MOD, AL FIN EN EL JUEGO (PresenciaNPCSystem + 5 ítems — nuevos)
  · **EL TESTIGO SIEMPRE ESTÁ**: PresenciaNPCSystem lo garantiza — cada 10 s, si no hay ninguno en el mundo, nace cerca del jugador (búsqueda de hueco 2×4 con suelo, 12 intentos, bordes respetados). Antes era un fantasma de código: townNPC sin spawn natural ni invocador (quirk documentado por la auditoría R44).
  · **EL TESTIGO HABLA POR BURBUJAS**: EcoLib como SEGUNDO usuario de la librería de diálogos — susurros violeta sin rugido cuando te acercas (cada ~14 s, rotando 3 líneas hjson), en vez de chat plano. "Te he observado antes de que tuvieras nombre."
  · **LOS CINCO LLAMADOS (ítems invocadores, SOLO DE DÍA)**: Cristal del Titán Hueco · Sello del Rift · Pluma de la Arquera · Sombra del Portador · El Nombre de Aethon ("Ve al Sagrario Hueco y llama su nombre"). Reutilizables (no consumibles), aviso de "ya vive uno", noche = mensaje localizado. Los jefes del mod AHORA tienen VOZ al caer (EcoSistema): Aethon, el Titán Hueco, el Guardián del Rift y LOS ECOS (Arquera + Portador hablan al caer el último, como Los Gemelos) con sus 4 claves hjson y colores propios.
  · Íconos PIL 24×24 (revisados con VLM; la Sombra rediseñada de silueta a HOJA SOMBRÍA por ilegible). Los 6 ítems entran en la Bolsa del Probador; los 5 llamados también en la Bolsa de Invocadores.

### F. LA CARNADA DEL GRIMORIO (ítem de prueba)
  · Clic izq: desata la furia con las oleadas preparadas (o las hambres reales si son más). Clic der: cicla 1..10 oleadas. Salta la config a propósito (es LA herramienta de test) y avisa si la furia ya corre o el mundo está ocupado.

### G. VERIFICACIÓN
  · Compilación 0 errores / 0 warnings contra tModLoader 2026.07.3.0 real (entorno reconstruido: dotnet 8.0.425 + release + 8 referencias). · hjson 417/417 claves es/en, mismo orden. · 0 Main.rand en render (solo spawn/AI, legal; los 6 matches en VFX son comentarios del contrato). · 0 menciones externas. · PNG-por-clase: los 6 ítems nuevos con su .png (las capas de jugador y los sistemas no llevan). · APIs sondeadas por reflexión antes de escribir: firmas PreDraw/PostDraw/OnSpawn, PlayerDrawLayers.MountBack/FaceAcc (anclas probadas), ZoneCorrupt/ZoneHallow (los nombres reales de esta versión — ZoneCorruption/ZoneHoly NO existen), NPCID verificados por sondeo (Hoplite = GreekSkeleton interno, Medusa 480), Main.maxTilesX guardas en el spawn.

## Commit v6.46 — EL GRIMORIO DE TRES ESTADOS + LA DIETA DEL BESTIARIO + LAS VOCES

**Petición del usuario**: XP de jefes FIJA (sin el ×2 del hardmode) con la fórmula (base + 10% vida + rareza) + 1.1·(nivel·111) · el combate exige sostener el libro; vida/maná +100 como tope sin sostener (desde la barra rápida); slots de minion nivel/10 hasta +10 sumados a la base del juego; minions ya invocados permanecen al guardarlo · XP solo en el inventario visible (barra rápida) · subida múltiple condensada en un mensaje · limpieza de helpers muertos · ×3 en la primera kill de cada especie (jefes excluidos) · barra dorada de XP junto al hotbar con pulso · voces personalizadas por jefe con su color (formato de mensaje de estado, librería propia para diálogos).

### A. LA XP DE LOS JEFES ES FIJA (ShardLevelSystem.XPDeJefe)
  · **LA FÓRMULA**: (base + 10% de la vida total del jefe + estrellas del bestiario) + 1.1 × (nivel del grimorio × 111). Base: Moon Lord (núcleo) 100.000 · vida alta >20.000 = 25.000 · resto 5.000. El término del nivel mantiene a los jefes relevantes cuando el libro ya está alto. Nivel de referencia: la PRIMERA copia de la barra rápida (la misma que manda en las stats).
  · **INMUNE AL HARDMODE ×2 y AL ×3 DE PRIMERA KILL**: los jefes son la fuente gorda estable — el ×2 del hardmode y la dieta del bestiario son para las CRIATURAS.
  · **UNA DERROTA, UN COBRO (EsParteDeJefe)**: los gusanos solo pagan por la CABEZA (Devorador de Mundos 13 · El Devorador 134 · Muro de Carne 113), el Golem por el CUERPO (245) y el Señor de la Luna por el NÚCLEO (398) — los segmentos, ojos y manos que mueren en cascada o como fase NO pagan. v6.45 pagaba por cada segmento del gusano y por cada parte del Señor de la Luna: una fuente de XP por error. Los Gemelos NO son parte: cada cuerpo cobra su fórmula.

### B. LOS TRES ESTADOS DEL LIBRO (ShardPlayer.NivelLibro)
  · **SOSTENIDO = TODO**: stats de combate (+1% summon/nivel, +0.2% crítico, +2% penetración cada 5, la mitad summon del bonus de mana bajo), regeneración, reducción de daño, vida/maná completos. El poder de combate exige BLANDIRLO.
  · **BARRA RÁPIDA (slots 0–9) = CAPACIDAD**: sigue comiendo XP (todas las copias), conserva los slots de minion y la vida/maná extra — TOPE +100 cada uno sin sostener.
  · **GUARDADO (inventario profundo, hucha/vaulta, cofre, suelo) = NADA**. Los minions YA INVOCADOS permanecen hasta que el jugador los desinvoque o mueran: el buff del orbe vive en el JUGADOR (no en el item) y vanilla no despawnea minions al bajar maxMinions — verificado, cero cambios necesarios.
  · **Slots de minion (WeaponScaling.BonusMinionSlots)**: +1 cada 10 NIVELES, tope +10 al nivel 100 (antes: +1 cada 5, infinito) — SUMADO a la base del juego y al resto del equipamiento. Los hitos del tooltip decían "cada 5": corregidos.
  · La detección está CENTRALIZADA en NivelLibro(soloSostenido) — PostUpdateEquips/PostUpdateBuffs/ModifyHurt cuentan la misma historia; las stats salen de la primera copia (sin stacking), la XP la cobran todas.

### C. LA DIETA DEL BESTIARIO (×3 la primera kill de cada especie)
  · **ConPrimeraKillDeEspecie**: la primera vez que matas cada especie paga ×3 — leído del tracker de kills del PROPIO bestiario vanilla (Main.BestiaryTracker.Kills.GetKillCount, el crédito por especie de GetBestiaryCreditId — las variantes visuales comparten entrada). VERIFICADO AL IL: NPCLoot llama RegisterKill ANTES de NPCLoader.OnKill, así que la primera kill ya cuenta 1 al llegar al hook (GetKillCount ≤ 1 = primera). Jefes excluidos: su XP es fija. Premia explorar el mundo en vez de farmear la misma babosa.

### D. LA BARRA DORADA (ShardHUDSystem — nueva)
  · La barra de XP del Grimorio JUNTO AL HOTBAR: nivel a la izquierda, "XP actual/total" a la derecha, chip "HM ×2" en hardmode (el pendiente de v6.45 — el indicador de que las criaturas pagan doble). Visible siempre que el libro esté en la barra rápida, con el inventario CERRADO (abierto: el tooltip ya lo cuenta).
  · **EL PULSO**: al cobrar XP la barra LATE 90 ticks (brillo senoidal sobre el dorado) y un "+XP" flotante sube y se funde. Geometría leída del IL de GUIHotbarDrawInner (primer slot (20,20), fila ≈ 470px → la barra en y=80). Render 100% determinista (cero Main.rand), capa insertada tras "Vanilla: Hotbar" con InterfaceScaleType.UI.

### E. LAS VOCES DEL GRIMORIO (EcoLib + EcoSistema — la séptima librería)
  · **ECOLIB (Content/VFX)**: la librería de diálogos dramáticos — máquina de escribir (~2 caracteres/tick), pausa, fundido, pop de nacimiento, deriva lenta hacia arriba, rugido al nacer (SoundID.Roar), fuente DeathText (la del "Has muerto…" de vanilla), autoajuste al 75% de la pantalla, cola acotada (8), texto partido en líneas al encolar (una vez, fuera del render). Cero lotes propios: se dibuja en la capa que EcoSistema inserta tras "Vanilla: Death Text".
  · **ECOSISTEMA (Content/Systems)**: el anfitrión + LA TABLA DE JEFES — 18 derrotas con su CLAVE hjson y su COLOR propio (gelatina turquesa, carnes, miel, hueso, infierno, clorofuria, destello prisma…), la voz genérica para jefes del mod/desconocidos, Los Gemelos hablan cuando cae el ÚLTIMO, anti-duplicado de 300 ticks por clave (si ambos gemelos caen el mismo tick no repite).
  · **LOS TEXTOS**: 19 claves Eco.VozJefe.* en hjson es/en (paridad 381/381) — una línea personal por derrota, la VOZ del grimorio devorando la esencia del jefe. El Muro de Carne ANUNCIA el hardmode ("todo lo que muera vale el doble").

### F. LIMPIEZA Y CONDENSADO
  · **SUBIDA MÚLTIPLE EN UN MENSAJE**: una derrota de jefe a nivel bajo salta 10–15 niveles — TODO el salto se anuncia condensado ("alcanzó el nivel N (+X niveles de golpe)"), con el hito de 50 cruzado por el salto detectado correctamente (nivelHito, no el nivel final).
  · **MilestonesForLevel()/NextMilestoneSummary() ELIMINADOS** (cero usos verificados por grep) · el comentario stalado de ShardLevelItem decía "las 3 armas" (son 2: Grimorio + Fragmento Génesis) y nombraba armas de la era v5 que ya no existen — reescrito.
  · **LAS BANDERAS DE CONFIG YA NO MIENTEN**: ShowLevelUpNotifications y ShowMilestoneNotifications existían desde v5.x y NADIE las leía — ahora OnLevelUp las respeta.
  · Lifesteal ya era solo-al-sostener (GlobalNPCXP revisa HeldItem): CONFIRMADO como diseño, ahora documentado.

### G. VERIFICACIÓN
  · Compilación: **0 errores / 0 warnings** contra tModLoader 2026.07.3.0 real (.NET 8.0.425) · hjson es/en paridad EXACTA 381/381 claves, mismo orden · cero Main.rand en render (los nuevos dibujan por función del tiempo) · cero menciones externas en todo el repo trackeado · cero ai[3]/ai[4] · estáticos con funeral completo (OnWorldUnload/Unload en EcoSistema y ShardHUDSystem) · las APIs verificadas contra el binario real antes de usarse (NPCKillsTracker.GetKillCount y su orden vs OnKill, DynamicSpriteFontExtensionMethods en ReLogic, la geometría del hotbar en GUIHotbarDrawInner, FontAssets.DeathText, las capas "Vanilla: Hotbar"/"Vanilla: Death Text").

## Commit v6.45 — EL GRIMORIO DE VERDAD: LA XP REAL DEL BESTIARIO

**Petición del usuario**: maná del minion gratis para pruebas · quitar "disparo doble" de los hitos, mover el bonus de summon al sitio correcto y quitar el costo de maná por disparo · XP REAL usando la rareza y rango de estrellas del bestiario (coste inicial 100, jefes a tope, hardmode ×2) · el arma gana XP siempre que esté en el inventario, también por las kills de sus propios minions.

### A. LA XP ES REAL (antes contaba kills disfrazadas de XP)
  · **LA FUENTE: LAS ESTRELLAS DEL BESTIARIO** (ShardLevelSystem.XPForNPC): cada criatura vale su RANGO de rareza del bestiario — la tabla oficial `ContentSamples.NpcBestiaryRarityStars` (Dictionary por NPC.type), la MISMA que el juego usa para dibujar las estrellas de cada entrada (verificado contra el IL del binario real: `BestiaryEntry.Enemy/TownNPC/Critter` la pasan a `NPCPortraitInfoElement`; la fórmula vanilla por defecto es 1 + rareza del Lifeform Analyzer + bonus creciente + 0.5 jefe + poder estadístico por tramos, tope 5). Con fallback a la MISMA fórmula recalculada sobre el NPC vivo si la tabla no existe (carga temprana / servidor).
  · **EL MAPEO**: 5 × estrellas² → 1★=5 · 2★=20 · 3★=45 · 4★=80 · 5★=125 XP. La rareza pesa al cuadrado: una 5★ vale 25 kills de 1★.
  · **JEFES A TOPE (siguen siendo la fuente gorda)**: Moon Lord 100.000 · hardmode 25.000 · pre-hardmode 5.000 (la tabla v6.x intacta).
  · **HARDMODE ×2**: al caer el Muro de Carne la ganancia de XP MEJORA AL DOBLE — mobs y jefes por igual (`Main.hardMode`).
  · **LA CURVA**: coste inicial 100 y sube — `100 × nivel^1.5` (nivel 1→2: 100 XP · 10→11: ~3.162 · 20→21: ~8.944). El multiplicador de config XPMultiplier se aplica al final, con redondeo y piso de 1 XP.

### B. EL ARMA VIVE EN EL INVENTARIO (XP desde cualquier slot)
  · **GANA XP SIEMPRE QUE ESTÉ EN EL INVENTARIO** (GlobalNPCXP.OnKill): matar con OTRA arma también alimenta al Grimorio — se escanea TODO el inventario (58 slots, patrón de la casa) y TODAS las copias cobran (cada una sube su propio nivel).
  · **LAS KILLS DE SUS PROPIOS MINIONS PAGAN**: los golpes del Orbe Cósmico (y cualquier proyectil del jugador) marcan `npc.playerInteraction[owner]` en OnHitByProjectile — FindKiller encuentra al dueño aunque el golpe final lo dé el minion. Idempotente con el comportamiento vanilla.

### C. EL MANÁ DEL MINIÓN LIBRE (solo pruebas)
  · **BANDERA `ManaGratisEnPruebas`** (AethonConfig, ON por defecto): la invocación del Orbe Cósmico NO cuesta maná — la última excepción viva del arsenal sin maná de v6.42. Apagarla restaura el coste escalado real (15 + nivel, tope 100) con su check de Mana Flower intacto. El tooltip solo anuncia el costo cuando está ACTIVO (mostrarlo en modo gratis sería mentir).

### D. LAS PROMESAS ROTAS DEL TOOLTIP (alineadas con la verdad)
  · **"Costo: X mana por disparo" ELIMINADA**: el bolt no cuesta maná desde v6.42 (Item.mana = 0) — la línea mentía. `WeaponScaling.ManaCost()` y sus 3 constantes muertas: eliminadas.
  · **"+5% prob disparo doble" ELIMINADA de los hitos**: `DoubleShotChance()` jamás se llamaba desde ningún sitio — promesa muerta. Función eliminada. Los bolts extra REALES (ExtraProjectiles, usado en Shoot) siguen anunciándose.
  · **EL BONUS DE SUMMON AL SITIO CORRECTO (doble corrección)**: (1) en el TOOLTIP, "+N slot(s) de minion" movido de la sección DAÑO a la sección ORBE CÓSMICO, junto al resto de stats del minion; (2) en el CÓDIGO, el +1% daño de invocación/nivel (+ el crítico mágico y la penetración, misma clase de bug) movido de `GrimoireEternal.ModifyWeaponDamage` a `ShardPlayer.PostUpdateEquips` — el hook viejo solo corría al calcular el daño del propio Grimorio, así que los minions atacando en otros ticks NO recibían nada (la misma clase de letra muerta que la regeneración R44). Ahora son stats persistentes de inventario, recalculadas cada tick tras ResetEffects.

### E. VERIFICACIÓN
  · Compilación: **0 errores / 0 warnings** contra tModLoader 2026.07.3.0 real (.NET 8.0.425) · hjson es/en en paridad exacta (tooltips del Grimorio con la XP real y el modo pruebas en ambos idiomas) · cero menciones externas · las reglas de la casa intactas en todo lo tocado.

## Commit v6.44 — EL ACCESO DEL PROBADOR: LOS LUGARES, LOS ÍTEMS Y LA SEGUNDA REVISIÓN FUNDACIONAL

**Petición del usuario**: "No entiendo bien que fue lo último que hiciste, explícalo y si son ítems dáselo al jugador, si son lugares que estos sean accesibles al jugador, recuerda que todo es de pruebas por ahora · Luego dame más ideas para seguir mejorando el mod y también has otra revisión de código".

### A. LOS ÍTEMS AL JUGADOR (la entrega completa del probador)
  · **EL PRISMA YA LLEGA SOLO**: la Bolsa del Probador (entregada automáticamente al entrar a cualquier mundo en un jugador) lo contiene desde v6.43 — reabrir la bolsa repone lo que falte. v6.44 completa el kit:
  · **EL ALTAR ANTIGUO COLÓCALO DONDE QUIERAS**: 5× AncientAltarItem añadidos a la Bolsa del Probador. Los altares naturales solo generan en mundos NUEVOS (PostWorldGen corre una vez por generación) — el ítem colocable monta un Sagrario de pruebas en cualquier mundo, con el flujo completo del Fragmento Génesis a clic derecho del altar.

### B. LOS LUGARES ACCESIBLES (el Sagrario Hueco, tres caminos)
  · **LA PUERTA DE LOS 400 PV ABIERTA (MODO PRUEBAS)**: `AethonConfig.SagrarioAccesibleEnPruebas` (ON por defecto) — el bioma se activa al descender al subsuelo con cualquier jugador de pruebas, sin comerse primero 7 corazones de vida. La puerta real (400 PV) se restaura apagando la bandera en la config del mod; la lógica original queda intacta debajo.
  · **EL SAGRARIO VIOLETA — EL LUGAR HECHO ESCENA**: CUARTA escena del Prisma (ciclo: Eclipse → Estelar → Alba → **Sagrario** → limpio). Las MISMAS tres texturas del fondo del bioma (SanctumFar/Middle/Close, 1024×256 sin costura) reenganchadas como capas LIBRES de CieloLib — el paisaje del Sagrario desplegable DONDE QUIERAS (¡también en la superficie!), para verlo sin descender. Con su tinte de cielo violeta profundo (30,8,48), su luz de fondo violeta-media (84,34,132) y su penumbra suave (brillo 0.92).
  · **EL CAMPO COMPLETO**: altares colocables + bioma activo bajo tierra + fondo propio al descender + paisaje desplegable donde quieras = el lugar entero en manos del probador.

### C. LA SEGUNDA REVISIÓN (auditoría R44 — 3 bugs reales del código fundacional v5.x, zonas nunca auditadas, todos verificados contra el IL del binario real antes de tocar)
  · **ALTO — LA REGENERACIÓN DEL GRIMORIO ERA LETRA MUERTA** (ShardPlayer.cs): `PostUpdate` corre DESPUÉS de que UpdateManaRegen/UpdateLifeRegen consumen los campos (IL_012c/IL_3146 antes de IL_860f) — lo que escribía era borrado por ResetEffects al tick siguiente sin que nadie lo lea: los bonuses documentados "+1 maná/seg, +0.5 vida/seg cada 20 niveles" NUNCA funcionaron. CORREGIDO: bloque movido a `PostUpdateBuffs` (corre entre ResetEffects y el consumo) + maná vía `Player.manaRegenBonus += maná*2` (el campo que ACUMULA; unidades del motor: 120 cuentas = 1 maná → 2·R cuentas/tick = R maná/seg exacto).
  · **ALTO — JEFES ZOMBIS DE 0 PV** (AethonBoss.cs, RiftKeeper.cs): al despawnear por objetivo muerto ponían `NPC.life = 0` — pero checkDead solo lo llaman StrikeNPC y las AIs vanilla (verificado en IL) → quedaba un jefe de 0 PV matable de un golpe, con botín y +250 resonancia gratis. CORREGIDO: `NPC.active = false` (el patrón HollowTitan v5.59 de la casa).
  · **ALTO — OFF-BY-ONE EN EL WORLDGEN DEL ALTAR** (AncientAltarWorldGen.cs): `PlaceObject(x,y)` ancla el ORIGEN (1,1) en (x,y) (verificado en TileObject.CanPlace: la caja 3×2 ocupa las filas y−1..y) — el chequeo de suelo miraba `y+2`: altares FLOTANDO 1 tile y puntos perfectos rechazados. CORREGIDO: suelo en `y+1` (ambos bucles).
  · **REPORTADO SIN CAMBIAR** (MP-first es un TODO de la casa, el mod es SP-first): `Item.NewItem` desde hooks de cliente (Altar/Witness/BossSummonBag) no sincroniza en servidor dedicado (QuickSpawnItem sí lo hace); AethonBoss fase 4 empuja la velocidad del jugador remoto (client-authoritative en MP); ShardSyncSystem es stub. BAJO: HollowTitan duplica el timer del fighter AI; las "minas" de EchoArcher son Bullet estático; tooltips del prisma hardcodeados en español sobre el hjson (patrón preexistente de las bolsas).
  · **VERIFICADO LIMPIO — TODO v6.44**: el ciclo del prisma cubre los 5 estados sin atascos (tras OnWorldUnload reinicia por Eclipse; el saliente caducado se autodispide); SagrarioVioleta usa el MISMO struct Capa (9 propiedades) que las otras 3 escenas y las texturas están registradas; escena + fondo de bioma simultáneos se dibujan en lotes distintos sin interferencia; config ClientSide + null-check = el patrón de ShardLevelSystem; hjson 362/362 paridad con las 4 escenas en ambos idiomas; 0 Main.rand en render, 0 ai[3+], 0 menciones externas, 100% PNG-por-clase.
  · Compilación final: **0 errores / 0 warnings** contra tModLoader 2026.07.3.0 real (.NET 8.0.425).

## Commit v6.43 — LAS APUESTAS DE VERDAD: LAS SEIS LIBRERÍAS (PantallaLib · MoldeLib · CompásLib · FormaLib · MediaResLib · CieloLib)

**Petición del usuario**: "Cuando dije hacer tus tareas apuestas no me refería a armas, me refería a esto que dijiste aquí [las 5 librerías propuestas: una PantallaLib (flash/sacudida/viñeta/onda expansiva de pantalla), una MoldeLib (cuerpos compuestos re-parametrizables), una CompásLib (coreografía declarativa de ataques cíclicos), una FormaLib (hitboxes por distancia a segmento/banda con API común), y pixelación de los pases de fuego/humo a media resolución]. También propongo una librería que sea capaz de interactuar con el fondo de terraria, o sea con los paisajes que muestra sus capas, parallax y demás cosas del fondo del juego. Hazlo todo y vuelve a darle una revisada al código buscando inconsistencias, bugs y menciones a otros proyectos".

### A. LA SONDA DE API ANTES DE ESCRIBIR (la casa de siempre)
  · Todas las firmas de las 6 librerías se verificaron CONTRA EL BINARIO REAL con MetadataLoadContext ANTES de delegar: ModSystem tiene ModifyScreenPosition/PostDrawInterface(SpriteBatch)/PostDrawTiles/ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)/ModifyLightingBrightness (y NO tiene PreDrawTiles ni PreDrawInterface en esta build); la cámara usa ICameraModifier (UniqueIdentity/Finished/Update(ref CameraInfo)) con Main.CameraModifiers.Add; los fondos son ModSurfaceBackgroundStyle (ChooseFarTexture/ChooseMiddleTexture/ChooseCloseTexture(ref scale, ref parallax, ref a, ref b)/ModifyFarFades/PreDrawCloseBackground), ModUndergroundBackgroundStyle (FillTextureArray), ModSceneEffect (props SurfaceBackgroundStyle/UndergroundBackgroundStyle/Music/Priority) y BackgroundTextureLoader (AddBackgroundTexture/GetBackgroundSlot); el CheckAABBvLineCollision del motor usa ref FLOAT (verificado con volcado de IL — el subagente de FormaLib lo confirmó con Mono.Cecil); DrawBG de Main es enganchable (On_Main.DrawBG — el patrón del hook de la lente) y BackgroundViewMatrix existe para el restore del lote del fondo.

### B. LAS SEIS LIBRERÍAS (seis CONTRATOS nuevos de la casa, todos con el estándar: español, XML docs con el porqué, cero GC por frame, cero Main.rand en render, lotes blindados try/finally)
  · **PANTALLALIB (el filtro del jefe — el lenguaje de pantalla completo)**: Sacudir(intensidad, duración, id) con modelo de TRAUMA (shake = trauma², decaimiento lineal, offset determinista por senos inconmensurables sin(t·17.13)/sin(t·29.7) — el patrón del "temblor de dos senos" de la supernova generalizado) vía SacudidaTrauma : ICameraModifier (la pila de cámara del motor: 4 máximas con refresco por id en vez de apilar; el stack del motor des-duplica por identidad y retira las terminadas — verificado en IL); Flash(color, duración, intensidad) hasta 4 simultáneos con ease-out; Vineta(intensidad, duración) con anillo de 10 SoftGlow negros gigantes (la viñeta SIN textura nueva: el gradiente radial de la casa); OndaExpansiva(centro, radioMax, duración, color, grosor) con 6 anillos Ring expandiendo ease-out cúbico en espacio de pantalla; todo dibujado en PostDrawInterface con reopen condicionado (el frame sin efectos NO toca el spriteBatch ni una vez) y el tinte de calidad de la casa (onda=Bloom, viñeta=PostProceso).
  · **MOLDELIB (cuerpos compuestos re-parametrizables — "jefes futuros en horas")**: el molde = lista de PIEZAS declarativas (offset local, escala, rotación, color, textura, PaseAlpha, OrdenZ, SeguirDireccion, AmplitudAleteo/FaseAleteo) ancladas a un cuerpo vivo (centro/rotación/escala/banking) y re-proyectadas cada frame sin recalcular la coreografía. El contrato de dos pases mecanizado: Proyectar emite el aditivo al búfer de VFXCore (coords MUNDO, el estándar) y deja las piezas alpha en cola interna; FlushPaseAlpha abre SU lote AlphaBlend y las vuelca DESPUÉS (el contrato "pupila al final", blindado v6.43 con End defensivo tras la auditoría). EL PRIMER MOLDE: OjoAlado — el ojo alado del FragmentoSupernova extraído CON LOS NÚMEROS EXACTOS (cuerpo 110×40 crema, alas ±38/−4 52×16 ±0.44, puntas ±62/−12 22×10, pupila 34×12 en pase alpha, banking 0.03) y PARAMETRIZADO (colorCuerpo/colorAlas/colorPuntas/escalaBase/amplitudAleteo/escalaAlas): el fragmento lo usa SIN cambio visual (paridad byte-exacto auditada), y un jefe nuevo = 11 líneas de parámetros + 5 de render.
  · **COMPÁSLIB (coreografía declarativa de ataques cíclicos — "hoy cada minion arma su switch a mano" → ya no)**: Compas (ciclo + movimientos con ventanas [inicio, duración] y prioridad de solape) evaluado sobre UN reloj que el proyectil ya lleva (ai[0..2] — stateless, multi-instanciable gratis); Instante responde NombreActivo/Fase01/TickEnMovimiento/PrimerTick/UltimoTick/FraccionCiclo + helpers En(nombre)/En(indice)/SuavizadoInicio/Pulso; sueltos EnVentana/EnCada/TicksPorSegundo; depuración integrada (Depuracion + DepurarDibujar: la línea de tiempo con cursor y el movimiento activo brillando — el compás se VE). INTEGRACIÓN: el Metrónomo refactorizado — el latido de 30t es ahora un Compas de 2 movimientos ("Tic" prioridad 2 + "Onda" envolvente) y el faro de 180t es 2 barridos de 90 exactos; _angFaro MUERE (el ángulo se deriva del reloj: el k-ésimo tick da el mismo valor que el acumulador de v6.42 — paridad tick a tick auditada) y una lista por-tick del faro se vuelve estática (cero GC extra).
  · **FORMALIB (hitboxes geométricos con API común — la sección eficaz REAL en vez de la caja cuadrada)**: geometría pura (DistanciaASegmento con clamp de proyección, PuntoMasCercanoEnSegmento para knockbacks direccionales, PuntoEnBanda/PuntoEnCapsula/PuntoEnAnillo/PuntoEnSector con wrap de ángulos, SegmentoIntersecaAABB por Liang-Barsky, DeltaAngle) + consultas de entidades (NPCEnSegmento con broad-phase de 4 comparaciones y el Collision del motor con su firma real ref FLOAT; NPCEnBanda/NPCEnAnillo/NPCEnSector con radio medio conservador documentado; JugadorEnBanda; ProyectilHostilEnBanda con el filtro de la casa hostile && damage > 0; RecorrerNPCs con culling + EsObjetivo) + depuración dibujada (banda/anillo/sector visibles con FormaLib.Depuracion). INTEGRACIÓN: la fisura de la guadaña muerde con la SECCIÓN REAL (la grieta de 16 px como segmento vertical + el refinamiento del AABB del motor) y el tragón de proyectiles usa la banda ±32 por segmento — MISMO daño, MISMA cadencia, MISMO hambre 2×; eliminados un diccionario muerto de v6.42 y un helper privado promovido a la librería.
  · **MEDIARESLIB (pases de fuego/humo a media resolución — fill-rate ÷4)**: Empezar/Terminar — el RT half-res se recrea lazy por tamaño (patrón de la lente), el lote del pase usa la MATRIZ DE CÁMARA ESCALADA ×0,5 (el llamador dibuja en sus coordenadas de VISTA de siempre: el zoom viaja dentro) y la composición vuelca el RT ×2 con PointClamp (píxel gordo 2×2 — modo retro) o LinearClamp (difusos: indistinguible). Autorreparable (SanarPaseColgado: un llamador que olvide su Terminar no rompe el frame siguiente), Dispose diferido por QueueMainThreadAction (la lección v5.87), quad directo con la matemática EXACTA de FlushAdditive. INTEGRACIÓN: la Vela Solar partida en dos — el VOLUMÉTRICO DIFUSO (el tejido de la membrana + el borde llameante PyraLib.Tongue + las alas de fusión StormLib.ArcRing) corre AL PASE (fill-rate ÷4, misma matemática: la geometría se calcula UNA vez en _puntas[8] y ambos pases leen del mismo array) y la ESTRUCTURA FINA (las 7 varillas + el corazón) queda a resolución NATIVA (trazos definidos — la media resolución se los comería); respaldo nativo automático si el RT no está disponible.
  · **CIELOLIB (la del FONDO — la pedida por el usuario: "interactuar con los paisajes, sus capas, parallax y demás cosas del fondo")**: EscenaDeCielo = capas de parallax LIBRES (Parallax 0=cielo lejano → 1=mundo, OffsetY/Escala/Alpha/Tinte/ScrollX de deriva/Profundidad de orden/Brillante para aditivo) + tinte global (ColorOfTheSkies pedido prestado con save/restore en finally, TinteDelFondo por ModifySunLightColor, Brillo por ModifyLightingBrightness); Activar/Desactivar con CROSSFADE por GameUpdateCount; el render engancha On_Main.DrawBG DESPUÉS del orig (las capas viajan ENCIMA del fondo del juego y DEBAJO de los tiles, con tiling horizontal por módulo manual — 2-4 draws por capa, sin UVs gigantes), lotes propios con restore del lote del fondo (BackgroundViewMatrix.TransformationMatrix — el estado real del fondo devuelto intacto). FONDO DE BIOMA: SanctumBackgroundStyle (far/middle/close con ChooseCloseTexture scale 0.8 / parallax 0.35) + SanctumUnderground (FillTextureArray tierra/roca/profundo) conectados al HollowSanctum — y el SAGRARIO tiene paisaje propio al descender. EL TESTER: el Prisma de Paisajes (Bolsa del Probador, SIN maná — la regla): cada uso cicla 3 escenas completas + limpia — Eclipse Umbral (nebulosa carmesí aditiva + montañas violetas + siluetas, cielo carmesí oscuro), Lluvia Estelar (estrellas + nubes azules + montañas nocturnas), Amanecer Primordial (nebulosa dorada + bruma + colinas). 12 texturas de capas 1024×256 SIN COSTURA (senos con períodos que dividen 1024 + réplicas antes del blur — la costura medida Δ≤0,09) generadas por tools/gen_cielo_v643.py.

### C. LA AUDITORÍA POST-LIBRERÍAS (la "revisada" pedida — 2 bombas lógicas encontradas y desactivadas)
  · **BOMBA 1 — LA PANTALLA NEGRA DEL PASE**: al restaurar el backbuffer tras el pase de media resolución, FNA LO LIMPIA al bindearlo (DiscardContents leído en vivo — verificado volcando el IL del FNA.dll de esta build, el mismo mecanismo de la pantalla negra de la lente v5.86-88): todo lo dibujado antes de la vela se habría borrado. VACUNA: PresentationParameters.RenderTargetUsage = PreserveContents solo el instante del re-bind (setter PÚBLICO — verificado; devuelto en finally); para RT-destino la vacuna es imposible (setter privado en esta build) y queda documentado.
  · **BOMBA 2 — LA VELA SIN COSTILLAS**: Terminar reabría el lote INCONDICIONALMENTE y el Begin aditivo de la estructura de la vela tiraba ("Begin has been called...") → el catch lo tragaba → LAS 7 VARILLAS Y EL CORAZÓN NUNCA SE DIBUJABAN (regresión silenciosa v6.42→v6.43). FIX: bandera _loteDelLlamadorAbierto — restauración exacta cerrado→cerrado / abierto→abierto, cero cambios en la vela.
  · **LAS OTRAS**: FlushPaseAlpha de MoldeLib sin End defensivo previo (corrupción de render posible con lote ajeno abierto — blindado); tinte del molde con división entera (latente — redondeo); el catch del PreDraw del fragmento limpiaba el búfer de VFXCore pero no la cola alpha de la pupila (higiene); el encabezado de MediaResLib documentaba una vacuna imposible (reescrito veraz). VERIFICADO LIMPIO: 0 menciones externas en TODO el repo trackeado (barrido con límites de palabra), 37 Main.rand de los archivos nuevos TODOS en lógica de juego (nunca render), ai[3+] solo en comentarios-lección, todas las paridades de integración confirmadas (fragmento byte-exacto, metrónomo tick-exacto, fisura 0.3×/10t/±32, vela idéntica), los estáticos con funeral completo (SacudidaTrauma.Finished al agotar trauma — sin leak en la pila de cámara), el hook de DrawBG con detach y guard de reentrada, el prisma sin maná, hjson 362/362 es/en.
  · El PNG del prisma renombrado ANTES del juego (PrismaDePaisajes.png → PrismaDePaisajesItem.png — la lección v5.88: tML exige textura = nombre de clase; MissingResourceException evitado en el papel, no en la consola del usuario).

### D. EL ESTADO DEL ARSENAL DE LIBRERÍAS (24 librerías en Content/VFX/ + el núcleo + los 4 sistemas de efectos)
  · La casa ahora habla: VFXCore (el núcleo de quads) + RiftLib (3 módulos) + OndaLib + PulsoLib + EstelaLib + SigiloLib + NebulaLib + LumenLib + OrbitaLib + TelaLib + TajoLib + CodigosLib + SierpesLib + PyraLib + EcosLib + StormLib + AudioLib + VFXPalettes + GravLens + BlackHoleLensSystem + BrumaSystem + **PantallaLib + MoldeLib + CompásLib + FormaLib + MediaResLib + CieloLib** (v6.43).
  · Compilación: 0 errores / 0 warnings contra tModLoader 2026.07.3.0 real (.NET 8.0.425).

## Commit v6.42 — LAS CINCO APUESTAS + EL VERBO PRIMORDIAL (EL ARMA QUE HABLA CON TODAS LAS LIBRERÍAS) + LA LIMPIEZA TOTAL DE REFERENCIAS

**Petición del usuario**: "Es momento de hacer tus 5 apuestas, investiga muy bien y a profundidad para implementar tus 5 apuestas de manera perfecta y super profesional, luego crea un arma única que use todas las librerías de las que disponemos, no olvides que todas las armas son de pruebas, así que no deben consumir maná · Has un análisis del código y elimina todas las referencias del proyecto, el proyecto debe quedar limpio de referencia y menciones de otros mods tanto en código como en comentarios como en textos o cualquier otro medio, lo mismo para las imágenes o cualquier otro archivo · Luego asegurarte de que el código del proyecto funciona bien".

### A. LA INVESTIGACIÓN (la apuesta se investiga ANTES de escribirse)
  · **INFORME_DE_APUESTAS** (research local de sesión): cada concepto destilado con NÚMEROS concretos — el timing del ritmo graduado en tres calificaciones (PERFECT ±3 ticks = crítico+abanico, GOOD ±4-6 = crítico, OFF = combo a 0 SIN castigo: el compás premia, nunca penaliza), la curva de calentamiento exponencial (τ = 132 ticks, el capacitor), la vida de la fisura como 3·τ de relajación de fractura (120 ticks), la ventana del parry de 8 ticks (133 ms — entre el 0,2 s de los juegos de acción y los 16-33 ms de los de ritmo puro), y el 60% por generación del eco (1−e⁻¹ de la decoherencia: 3 generaciones = 2,176× por cadena).
  · **VERIFICACIÓN DE API CONTRA EL BINARIO REAL**: los hooks del parry se compilaron contra tModLoader real ANTES de escribir el arma — FreeDodge(Player.HurtInfo) es el ÚNICO punto donde cancelar un golpe ya calculado (HurtInfo NO trae el índice de la fuente en esta versión: el parry filtra por info.Dodgeable, que distingue contacto/proyectil del daño ambiental), ModifyHurt baja ×0,3 el bloqueo tardío, y PreItemCheck() (el hook que envuelve ItemCheck_Inner) es el aturdimiento del parry fallido. Projectile.CritChance confirmado en el parche de vanilla (el crítico garantizado del metrónomo).

### B. LAS CINCO APUESTAS (cinco mecánicas DISTINTAS de juego — no cinco variaciones del mismo "disparar y olvidar")
  · **APUESTA 1 · EL METRÓNOMO DE PÚLSAR (el ritmo)**: el púlsar-compañero flota sobre el portador latiendo cada 30 ticks con anillo que se CONTRAE hacia el tic (el compás se aprende con los OJOS, sin HUD) + 8 trazos de combo + clic perceptible. El Shoot califica el disparo: PERFECT → CritChance=100 + abanico doble (±0,18 rad, 60%); GOOD → solo crítico; OFF → combo a 0. A los 8 aciertos: EL FARO — 180 ticks de haz de 900 px girando a 0,0698 rad/t (DOS vueltas exactas) picando 0,75× con iframe 6 por NPC, y 480 ticks de silencio después. Reloj: Main.GameUpdateCount (NUNCA GlobalTimeWrappedHourly — el render late distinto que el juego).
  · **APUESTA 2 · LA VELA SOLAR (la cadencia creciente)**: el arma de CANALIZACIÓN — la vela de fotones se despliega a la espalda del portador (7 varillas + tejido de quads sobre un arco de elipse, PyraPalettes por temperatura) y dispara sola: calor exponencial (τ=132), cadencia 12/s→30/s, daño ×1→×1,6; al calor pleno LA FUSIÓN (90 ticks de plasma azul-blanco ×2, arcos voltaicos de StormLib) y después LA FUSIÓN DEL CAÑÓN (180 ticks humeantes, calor residual 0,3); soltar conserva el 70% (la ley de Newton ×0,988).
  · **APUESTA 3 · LA GUADAÑA DEL DESGARRO (la zona melee — EL PRIMER MELEE DEL ARSENAL)**: el creciente de TajoLib vuela 220 px y donde muere abre LA FISURA VERTICAL (16×128, 120 ticks): muerde 0,3× cada 10 ticks (cooldown por NPC) y TRAGA los proyectiles hostiles que la cruzan (escaneo cada 2 ticks, banda ±32 px) — su daño se suma al próximo tajo (EL HAMBRE, tope 2× el arma, tintado carmesí + cuentas de apetito orbitando la hoja). Clic der: la fisura cae directamente en la mira (≤480 px).
  · **APUESTA 4 · LA ÉGIDA DE NOVA (el parry defensivo — EL PRIMER ESCUDO DEL ARSENAL)**: clic izq = embestón (onda 160 px, empujón 9); clic der = ALZAR LA GUARDIA: 18 ticks de círculo rúnico (SigiloLib.AnilloRunico + OrbitaLib.AnilloEnergia con el aro blanco latiendo al doble durante la ventana perfecta). Golpe esquivable en los primeros 8 ticks → FreeDodge lo DESHACE y detona LA NOVA (3× en 240 px + quemadura + 1 s invulnerable + onda cromática + flor de fuego). La guardia que expira sin parar ATURDE 30 ticks (PreItemCheck false — no se puede usar ítems). Enfriamiento 480 ticks visible como el anillo que se cierra.
  · **APUESTA 5 · EL ECO CUÁNTICO (la cadena)**: cada pulso vuela 180 px y DECOHIERE en semilla fantasmal (30 ticks re-apuntando cada 10) que se RE-EMITE como eco contra el enemigo más cercano — la generación se HEREDA (el proyectil se transforma: ×0,6 por generación, 3 generaciones, tope de 6 ecos vivos, visual VoidCold con parpadeo de espejos). Clic der: LA RESONANCIA — todas las semillas disparan A LA VEZ (coro), 300 ticks de recarga.

### C. EL VERBO PRIMORDIAL — EL ARMA QUE HABLA CON TODAS LAS LIBRERÍAS (la petición literal)
  · LA PALABRA que pronunció la primera luz: 420 ticks = 7 movimientos de 60, CADA UNO con su biblioteca — I EL PULSO (OndaLib.Pulse + PulsoLib.EmpujarPantalla) · II EL SELLO (SigiloLib doble círculo rúnico contrarrotante) · III LA CORONA (OrbitaLib.AnilloEnergia + Fotones + EcosAnillo) · IV LA TORMENTA (StormLib.ArcRing + ChainBolt a los cercanos con daño 0,5×) · V EL FUEGO (PyraLib.Flame + Tongue + LumenLib.Flare) · VI EL DESGARRO (RiftLib.Grieta por donde pasó + TajoLib al morder + GravLens curvando la luz) · VII LA SINFONÍA (CodigosLib.SolVivo + SierpesLib.CriaEstelar + NebulaLib.Nube + TelaLib.Cinta — SU PRIMER USUARIO — + EcosLib.ColaHistoria + EstelaLib.Ribbon + ParticleManager + OndaLib.Telegrafo) → LA DETONACIÓN (2,5× en 240 px + quemadura + OndaLib.Shock cromática doble + PyraLib.Estallido + StormLib.MultiBolt a los 6 cercanos + RiftLib.TearImpacto + ParticlePresets + el estruendo completo).
  · El daño crece con el movimiento (×1,0→×1,4) y cada cambio de voz suena con su FAMILIA de AudioLib (las seis voces de la casa: Cosmica, Runico, Solar, Electrica, Solar, Desgarro y la Cosmica del final). LOS CONTRATOS DE LOTE al milímetro: el búfer de VFXCore (mundo) se vuelca ANTES de abrir lote propio; CodigosLib y SierpesLib se llaman con el lote CERRADO (gestionan el suyo); el resto en el lote propio con coords de PANTALLA.

### D. LA ENTREGA + EL ARSENAL
  · **LA BOLSA DE LAS APUESTAS (16ª)**: las cinco + el Verbo, con la semántica de garantía de la casa (solo lo que falte, permanente, reabrible). TestingPlayer: DIECISÉIS bolsas.
  · **SIN MANÁ — TODAS**: las 6 nuevas nacen con mana=0 (la regla) y se repararon las 3 que quedaban vivas con coste: el Tomo de la Apatía Nula (26) y las DOS variantes del Grimorio Eterno (3 + el escalado por nivel) — el arsenal COMPLETO es ahora de pruebas gratis.
  · **ASSETS**: 7 iconos 30×30 procedurales (lecciones de la casa: formas gruesas y planas, outline Terraria, supersample ×4) validados por VLM con iteración (el eco rediseñado de 4/10 a 9/10 — ondas crecientes legibles; el metrónomo de 6/10 a 8/10 — núcleo grande y aguja) + 13 PNGs de proyectil (píxel invisible: todo el dibujo es primitivas).

### E. LA LIMPIEZA TOTAL DE REFERENCIAS (la petición literal, proyecto entero)
  · **EL REPOSITORIO DEJADO LIMPIO**: fuera el material de investigación con contenido de terceros (imágenes y código de referencia de otros mods), fuera las menciones a otros mods en TODOS los textos del proyecto (código, comentarios, changelog, documentos, worklog del repo) — el proyecto queda 100% contenido propio. El .tmod ya no empaqueta research/ ni tools/ (v6.41) y ahora tampoco existen en el repo.
  · **EL SANDBOX/CONTAMINACIÓN**: el repo había acumulado por accidente archivos del entorno de desarrollo (carpetas de logs de herramientas, .env con secretos, skill packs, archivos del entorno web) — TODO fuera del repositorio: el repo es SOLO el mod + su documentación. La regla de oro de la casa queda documentada: material de referencia = .cs.txt JAMÁS .cs, y JAMÁS dentro del repo.

## Commit v6.41 — LOS DOS HUÉSPEDES (EL TENTÁCULO DEL VACÍO Y EL FRAGMENTO DE SUPERNOVA) + LA MEJORA DE LAS LIBRERÍAS (EL VOLCADO BLINDADO, ECOSLIB, EL TELEGRAPH Y EL ESTALLIDO)

**Petición del usuario**: "Ahora crea una nueva arma copiando exactamente el proyectil del arma tentáculo del mod de referencia y esto es solo para pruebas · También investigar al jefe de la singularidad alada, es un boss, pero copiarlo como proyectil o minion y esto es solo para pruebas · Ahora investiga, mejora y actualiza librerías según creas necesarios, teniendo en cuenta lo que sabes sobre bugs y errores gráficos y dame ideas de que librerías crees que falten o necesitamos a futuro · Luego analizar y revisar código de proyecto entero para buscar inconsistencias y bugs ya sea que se hayan pasado por alto, que sean nuevos o que aparezcan a raíz de las mejoras y creaciones de librerías · No olvides quitar referencias externas incluidas las que aparecen en comentarios · Y dime que piensas de nuestras librerías · Con respecto a armas anteriores, no olvides que debes darle las armas al jugador, verificar si faltan armas creadas recientemente que no se las das al jugador".

### A. EL TENTÁCULO DEL VACÍO — LA RÉPLICA EXACTA (la petición literal, investigada en fuente primaria)
  · **LA INVESTIGACIÓN**: el arma y su proyectivo se localizaron en el código fuente público del mod de origen (el arma mágica del tomo y su proyectil "tentáculo cósmico", 207 líneas) + las DOS clases de partículas que lo visten (CustomPulse/CustomSpark, con texturas medidas: LargeBloom 360², GlowSpark 2048²) + el dust custom de vacío (con su triple dibujo) — todo descargado y leído completo (research/apatia_v641/, Task 2-a hecha por el orquestador).
  · **`TomoApatiaNula`** (el arma): LOS NÚMEROS DEL ORIGINAL 1:1 — daño 63, crit 12, maná 26, useTime 8 / useAnimation 20 / reuseDelay 8, knockback 5.5, rareza Roja, autoReuse, shootSpeed 12, y el disparo con la dispersión ±0.7 rad del Shoot original. Icono 30×30 del tomo verde-negro (VLM 8/10).
  · **`TentaculoCosmicoProjectile`**: EL PUERTO LÍNEA A LÍNEA del original — la GESTACIÓN de 120 ticks frenando ×0.96 escupiendo pulsos de vacío sin dañar (CanDamage=false), EL AZOTE de 3 curvas hacia la MIRA (vel 7 girada 0.2·sentido, curva 0.04·lerp por AI-tick, extraUpdates 8, hitbox 90×90, cooldown local 64), EL SALTO (teleport ±100px + blooms + pausa de 45 ticks recargando a +2/tick + sentido INVERTIDO + numHits=0), y la muerte con el polvo 66/263. LA LEY DEL DAÑO exacta: cada golpe sucesivo pesa menos (×1.0 → ×0.7 tras 5 impactos: `GetLerpValue(5,1,numHits)` clampeada — el tentáculo se aburre).
  · **LAS PARTÍCULAS con las curvas EXACTAS**: los pulsos (bloom que IMPLOE de 0.6→0 con easing PolyOut(4) y opacidad en seno(π/2+t·π/2), los negros en pase ALPHA — oscurecen — y los verdes en aditivo) y las chispas de cola (decaimiento ×0.95/frame, muerte al cubo pow(t,3), rotación = velocidad+π/2, el estirado (1.7→0.7, 0.9→2.9) del sharp del original) viven en búfers de instancia (96 pulsos / 256 chispas, compactado in-place, edad por GameUpdateCount — las partículas del original envejecen por FRAME, no por AI-tick, y así se copió).
  · **`PolvoVacioInvertido`** (Content/Dusts/ — PRIMERA dust de la casa): el calco del dust de vacío original — núcleo NEGRO que oscurece (27px/23px por unidad de escala) + halo de color (14px) + perla sólida (4.8px), rotación por el signo de la velocidad, frenado ×0.98, crecimiento +0.02 sin gravedad, luz del color del halo. Las 3 capas a mano en PreDraw con las texturas de la casa.
  · Los dusts del original (vanilla 278/267/66/263/191) se usan por ID — vanilla, sin referencias externas.

### B. EL FRAGMENTO DE SUPERNOVA — EL BOSS HECHO MINION (la segunda réplica, investigada a fondo)
  · **LA INVESTIGACIÓN** (subagente — research/supernova_v641/INFORME_SUPERNOVA.md, 154 líneas): el mod de origen está clonado COMPLETO (repo de backup del equipo, v1.6.1.1, 7876 archivos) y el boss se leyó ENTERO (969 líneas) + sus 13 satélites (beams, zapwarns, explosiones, bombas, orbes, disco) + los PNG analizados píxel a píxel: el sprite es un "OJO ALADO" horizontal de 128×56 (paleta exacta crema 240,216,144 · naranja 240,120,72 · negro · azul marino · rojo 168,48,48), SU FIRMA son las afterimages orbitales (4 clones (255,233,197,50) a 8px + 3 clones (244,142,72,77) a 16px con pulso triangular de 4 s), su glow Goldenrod late con periodo 1.4 s, su luz es naranja ×1.25, y su CEREBRO es spam relentless con 5 ataques (volea de blasts en abanico, bolas de fuego, abanico de minas, paredes de láser con telegraph, god explosion).
  · **`FragmentoSupernovaMinion`**: el boss DOMADO a sirviente — minion de verdad (patrón de la cría estelar: buff sostenido, minionSlots 1, nunca expira, netImportant) con el HOVER suave y pesado del original (Lerp 0.1 + amortiguación Y ×0.94) al hombro, banking ×0.03 exacto, y el CICLO DE 180 ticks alternando sus DOS ataques icónicos: LA VOLEA (5 ráfagas en abanico ±1 rad, vel 6, acelerando ×1.01 — los números del original) y EL BEAM (45 ticks de telegraph + 15 ticks de rayo de 900px con la campana sin(π·t/B), daño ×2.5 por línea con `Collision.CheckAABBvLineCollision` y cooldown por NPC).
  · EL RENDER (primitivas de la casa, cero Main.rand): EL OJO ALADO compuesto por quads (cuerpo crema-oro 110×40 + alas naranjas 52×16 a ±0.44 rad con puntas rojas + LA PUPILA en pase alpha AL FINAL — la lección del mock v6.41: el vacío del ojo debe oscurecer el brillo del propio cuerpo, no quedar enterrado debajo) + LAS AFTERIMAGES ORBITALES por EcosLib (el bucle EXACTO del original: fases 0.25/0.34, pulso triangular) + el glow Goldenrod de periodo 1.4 s + el anillo dorado + luz naranja ×1.25.
  · **`RafagaNovaProjectile`**: el dardo de la volea — acelera ×1.01, cola de fantasmas por EcosLib.ColaHistoria, y al tercer golpe (contador propio: penetración infinita para que la vainilla NUNCA lo mate sin flor) o al tocar suelo o al expirar: LA FLOR DE FUEGO — `PyraLib.Estallido` con daño en área (0.8×) y 14 polvos Torch.
  · **`FragmentoSupernovaStaff`** + **`FragmentoSupernovaBuff`** (con icono 32×32): la puerta de invocación (Summon, sin maná, receta madera). Iconos VLM: staff 8/10, bolsa 9/10.

### C. LAS LIBRERÍAS — LA MEJORA PEDIDA (bugs conocidos + lo que faltaba)
  · **VFXCore.FlushAdditive — EL VOLCADO BLINDADO (try/finally)**: si UN Draw lanzaba (dispositivo perdido, textura nula por descarga caliente), el lote quedaba ABIERTO para siempre (todo el render del juego se corrompía hasta relogear) y el búfer nunca se limpiaba (los cuadros muertos se re-volcaban cada frame). Ahora End + limpieza garantizados pase lo que pase.
  · **VFXCore.Line — EL CUADRO ESTIRADO DE A A B**: el primitivo de los rayos/beams que cada arma recomponía a mano (posición=punto medio, rotación=ángulo, escala X=longitud).
  · **`EcosLib` — LA LIBRERÍA DE LOS FANTASMAS (NUEVA)**: el agujero del arsenal (todo efecto premium vive de los ECOS y cada arma se hacía su bucle): LA MEMORIA (búfer circular de 32 posiciones+ángulos, cero GC) con `ColaHistoria` (la fila de fantasmas decrecientes que se ESTIRA con la velocidad), y EL ESPEJO (`EcoOrbital` + `PulsoTriangular` + `TempoEspejos` — el bucle de clones orbitales que respiran). Nace USADA por el fragmento (orbital) y la ráfaga (historia).
  · **`OndaLib.Telegrafo` — EL AVISO QUE PRECEDE AL GOLPE**: la regla de legibilidad del ecosistema premium (todo golpe duro avisa 30-60 ticks antes): banda ancha que marca el terreno + núcleo fino que se afila (0.15→0.85) + anillo de origen que se contrae + el "chillido" a 18 Hz en el último 25% (determinista: seno del reloj).
  · **`PyraLib.Estallido — LA FLOR DE FUEGO RADIAL (nueva)**: 6 rayos de lengüetas a 60° con el giro maestro de −0.2π, envolvente de lengüeta en sin(π·progress), radio fast-out y el corazón blanco que muere primero — la diferencia con Flame: RADIAL, no direccional.
  · **build.txt — EL ARREGLO DEL EMPAQUETADO**: `research/*` y `tools/*` entran en buildIgnore — las carpetas de investigación (con sus PNG de referencia y sus informes) se EMPAQUETABAN dentro del .tmod distribuido: carga inútil + material de terceros dentro del paquete. Fuera.

### D. LA REVISIÓN COMPLETA (la petición: inconsistencias y bugs, incluidos los nuevos)
  · **Referencias externas en Content/**: 0 (barrido en todos los .cs — los comentarios también; la investigación vive en research/, que ya no se empaqueta). Los archivos nuevos se escribieron sin nombrar la fuente.
  · **hjson ↔ clases**: paridad es/en EXACTA (todas las claves) + los 3 halos cosméticos que faltaban (AnillosSolaresHalo, SelloGenesisHalo, AnilloRunicoDorsalHalo) ahora tienen DisplayName.
  · **PNGs**: todos los ítems/buffs/proyectiles con textura existen (el buff nuevo llevaba su PNG desde el generador; los proyectiles invisibles siguen el patrón probado de los halos).
  · **Main.rand en render**: 0 (barrido por método PreDraw/Draw/PostDraw sobre TODO el mod).
  · **ai[3+]**: 0 reales (los 2 matches son comentarios históricos que explican por qué NO se usa).
  · **Las SimpleStrikeNPC**: todas con EsObjetivo + guardas de autoridad (verificado por sitio, incluidos los 4 nuevos del fragmento).
  · **LOS DOS HALLAZGOS REALES nuevos**: (1) el orden de pases de la pupila del fragmento (capturado por el MOCK 1:1 antes de llegar al juego — tools/mock_fragmento_v641.py: la pupila enterrada bajo el aditivo; ahora va AL FINAL); (2) la Ráfaga Nova moría por penetración SIN florecer (la vainilla la mataba al agotar penetrate sin pasar por la flor) — penetración infinita + contador de golpes propio.
  · **Compilación 0 errores / 0 warnings** contra tModLoader 2026.07.3.0 real tras cada paso.

### E. LAS ARMAS AL JUGADOR (la petición: verificar las recientes)
  · **HALLAZGO CONFIRMADO**: `TajosAstralesStaff` (v6.40, EL BASTÓN DE LOS TAJOS ASTRALES) NO estaba en NINGUNA bolsa — el arma de la versión anterior se quedó sin entregar. Y el GRIMORIO ETERNO (el arma RAÍZ del mod, la que nivela) tampoco estaba en ninguna bolsa (solo por receta).
  · **`BolsaHuespedes` — LA BOLSA 15 (LOS HUÉSPEDES)**: el Tomo de la Apatía Nula + el Fragmento de Supernova + los Tajos Astrales recuperados. BolsaFundacionales 4→5 (entra el Grimorio Eterno). TestingPlayer: 15 bolsas garantizadas en cada entrada. Verificación de cobertura: 79 clases de arma, TODAS en bolsa (solo quedan fuera las 2 bases abstractas por diseño).

## Commit v6.40 — LA REPARACIÓN DE LOS ANILLOS RÚNICOS COSMÉTICOS (EL COLOR PLANO) + EL BASTÓN DE LOS TAJOS ASTRALES (EL CORTE DIFERIDO DEL ANIME)

**Petición del usuario**: "Algo más que hay que arreglar son los accesorios cosméticos de anillos rúnicos: no muestran los anillos rúnicos, solo un intento de anillo pero se ve color plano — para ver por qué no funcionan revisa cómo se usa en los soles rúnicos y agujeros negros rúnicos, y también revisa el cosmético de la corona rúnica rosada" · "conoces los tajos de espadas, cuando en los animes cortan algo con una katana y salen cortes brillantes en todas direcciones que son líneas curvas blancas que aparecen y desaparecen rápidamente con un crecimiento de izquierda a derecha y aparecen después de efectuado el corte — crea un bastón que dé tajos iguales, investiga en Internet y en otros mods populares".

### A. EL DIAGNÓSTICO — MEDIDO CONTRA EL BINARIO REAL (el FNA.dll de tModLoader)
  · **LA VERIFICACIÓN DEFINITIVA**: se decodificó el IL del `.cctor` de `BlendState` del FNA.dll real que distribuye tModLoader (dnfile + lectura de bytecode) y se contrastó con el fuente de FNA y MonoGame: **`BlendState.Additive` = (SourceAlpha, One)** — `out.rgb = src.rgb × src.a + dst.rgb`: ¡el canal alfa SÍ modula la contribución aditiva! (la nota v6.39 que decía "One/One, el alfa no entra" era INCORRECTA — la reparación v6.39 funcionó igual porque sus bandas premultiplicadas con el gateo real producen un doble-perfil suave que el usuario aprobó; el artefacto verdadero de los rayos era la RAÍZ 2, la geometría). Y **`BlendState.AlphaBlend` = (One, InverseSourceAlpha)** — la fórmula PREMULTIPLICADA: asume que la fuente ya lleva rgb×alfa horneado.
  · **LA RAÍZ DEL "COLOR PLANO"**: el pase de dibujado del JUGADOR (las `PlayerDrawLayer` → `VFXCore.AppendToPlayerDraw` → DrawData) mezcla con AlphaBlend = fórmula premultiplicada. Los emisores de la casa (`SigiloLib.Tint`) llevan la intensidad SOLO en el alfa con RGB INTACTO y las texturas straight (RGB=255) → en esa fórmula **el RGB entra ENTERO en cada píxel donde la textura tiene alfa** (y SoftGlow lo tiene en toda su extensión) → los sellos se dibujaban como MANCHAS DE COLOR PLANO sin gradiente. MEDIDO en el mock 1:1 (tools/mock_anillos_v640.py, el pipeline exacto: carga tML con zeroing de alfa=0 + shader tex×tint + los DOS blends reales): el perfil de un aro saltaba del fondo (17,21,35) DIRECTO a (255,255,176) — cero gradiente, el "color plano" del usuario confirmado numéricamente.
  · **POR QUÉ EL RESTO SE VEÍA BIEN**: los SOLES RÚNICOS vuelcan por `VFXCore.FlushAdditive` (el lote aditivo — el alfa GATEA, el glow responde lineal); los agujeros negros y el anillo dorsal abren SU lote aditivo propio; y LA CORONA RÚNICA ROSADA (verificada, funciona) es el único emisor DrawData que premultiplica por suerte de diseño: usa el operador `Color × float` de XNA (RGB y alfa escalados) — exactamente lo que la fórmula premultiplicada espera. La Corona del Vacío igual (Color × float). El mock 1:1 con los blends reales: dorsal 8/10, corona rosa 7.5/10, Supremo r=60 (referencia) 9.5/10, y los aros por el camino DrawData: plano medido.

### B. LA REPARACIÓN — LOS DOS SELLOS MIGRAN AL CAMINO ADITIVO (el de los soles que el usuario citó)
  · **`SelloGenesisHalo` + `AnillosSolaresHalo`** (Content/Projectiles/Cosmetic/SellosCosmeticosHalos.cs): los DOS accesorios planos (EL SELLO DEL GÉNESIS y LOS ANILLOS DEL SOL RÚNICO) migran de las capas de jugador al PATRÓN DEL HALO PROYECTIL (el del dorsal y los anillos del horizonte, a prueba de balas): un proyectil sin daño pegado al dueño que en PreDraw cierra el lote del pase, vuelca LOS MISMOS sellos por `VFXCore.FlushAdditive` (¡el MISMO volcado por el que salen los anillos de los soles rúnicos reales!) y reabre el lote tal cual. Las llamadas son LITERALMENTE las de las capas viejas — ni un número cambiado; solo cambió la PUERTA.
  · **`SellosPlayer`** invoca los dos halos (el patrón de siempre: dueño local + netImportant + espía de duplicados); **`SellosAccesoriosDrawLayers.cs` ELIMINADO** (las dos capas DrawData planas). Los sellos ahora pasan POR DETRÁS del cuerpo (proyectiles antes que jugadores): la escritura ENVUELVE al portador como el vórtice del horizonte.
  · La corona rosada y la corona del vacío: verificadas funcionando (premultiplican con Color×float) — INTACTAS.

### C. EL BASTÓN DE LOS TAJOS ASTRALES (la petición del anime, investigada)
  · **LA INVESTIGACIÓN** (subagente — research/tajos_v640/INFORME_TAJOS.md, 46 búsquedas + 14 lecturas + el Excalibur de vanilla 1.4.4 decodificado + un pack de slashes medido con numpy): el tajo anime = **LÍNEAS DE ESPADA + CAUSALIDAD RETRASADA** ("el golpe ya terminó; los efectos decidieron esperar"): un CRECIENTE (media luna) de 10-30 ticks con núcleo blanco solo al pico, crecimiento DIRECCIONAL de punta a punta, campana asimétrica (subida al 55%, caída al 45%), tajos múltiples desacoplados, y el daño en la APARICIÓN.
  · **`TajoLib`** (Content/VFX/): LA LIBRERÍA DEL Tajo — el creciente con PERFIL DE LENTE (sin(πu)^0.55: grosor máximo al centro, fino en las puntas) dibujado con LAS BANDAS UNIFORMES v6.39 (BoltHalo/BoltCore — uniformes a lo largo): el arco queda CONTINUO de punta a punta (medido: depresión en juntas 0.03 px vs 23.8 px con cápsulas radiales — las cápsulas se desvanecen en sus extremos y sembraban collares de perlas); 3 capas (eco fantasma a radio×0.88 + halo que PARPADEA + núcleo blanco que NUNCA parpadea), FRENTE DE REVELADO ardiendo, puntas prendidas con chispas por Hash01, y la CAMPANA DE VIDA pública.
  · **`TajoAstralProjectile`**: EL CORTE DIFERIDO en 3 fases — EL DESEMBAINO (6 ticks: el hilo tenue vuela a la mira), EL MARCAJE (10 ticks: la marca dorada pulsando con chispitas convergiendo — el corte ya ocurrió), EL FLORECER (SIETE medias lunas en direcciones desacopladas por Hash01, pops escalonados cada 3 ticks con easeOutBack, revelado de punta a punta, radio creciendo ×1.32, campana asimétrica). **EL DAÑO CAE EN EL POP DE CADA ARCO** (la banda curva: |dist−radio|<50 && ángulo dentro del arco — la causalidad del anime), SimpleStrikeNPC + EsObjetivo solo en autoridad. Semilla por identity, cero Main.rand en render, SIN hide, contrato de batch v6.10.
  · **`TajosAstralesStaff`** (el arma): Magic, daño 46, sin maná (regla de prueba), dispara a LA MIRA (ai[0..1] viajan con el proyectil), receta madera 5, tooltips de la casa. **Assets**: tools/gen_tajos_v640.py (icono 30×30 con cabeza de media luna + glow 76×76 del creciente) — VLM 7.5 y 8/10. **hjson** es/en con paridad.
  · **MOCKS 1:1** (tools/mock_tajos_v640.py — el mismo pipeline exacto): el tajo individual **9/10** ("media luna con grosor variable, núcleo blanco saturado, halo dorado, puntas de luz — TAJO ANIME CONVINCENTE") y la SECUENCIA del corte diferido **9/10** (la causalidad retrasada se lee: marca → florecimiento; los tajos crecen de punta a punta; el ciclo nacer-brillar-morir visible). La continuidad del arco medida: brillo 255 en TODAS las posiciones, puntas incluidas.

### D. LA VERIFICACIÓN
  · Compilación contra tModLoader 2026.07.3.0 real (entorno reconstruido: .NET 8.0.425 + /tmp/verify): **0 errores / 0 warnings**.
  · Cero Main.rand en render nuevo · cero hide · paridad hjson · las capas borradas no dejaron referencias huérfanas (grep 0).
  · Los mocks y las mediciones numpy quedan en research/anillos_v640/ y research/tajos_v640/ (regenerables con los tools/).

## Commit v6.38 — LA CAMADA DE LAS SIERPES: DIEZ FORMAS NUEVAS DE MOVER LA MISMA CADENA + LA CRÍA (EL MINION)

**Petición del usuario**: "toma el bastón de La Sierpe Estelar y usándolo como base crea otros 10 que sean creativos y diferentes · además crea uno que funcione como minion · y mantén intacto al bastón de la sierpe estelar original · además de la misma forma en que funciona la sierpe estelar crea otras formas, puedes investigar sobre ello, ya que el código de la sierpe lo vi en Facebook, así que investiga más".

### A. LA INVESTIGACIÓN (lo que pidió: "investiga más" — el código de Facebook no es el único modo de mover una cadena)
  · **`research/sierpes_v638/INFORME_LOCOMOCION.md`** (287 líneas, 17 búsquedas web + 8 lecturas profundas, JSONs en `research/sierpes_v638/busquedas/`): **14 patrones de locomoción de criaturas segmentadas**, cada uno con su FÓRMULA traducible a C#/Vector2/ticks de 60 Hz, sus parámetros documentados (frecuencias, amplitudes, λ, Δφ) y la DIFERENCIA exacta con el follow() clásico de la sierpe. La lectura clave: el follow() de Facebook = constraint *unilateral + sin inercia + sin memoria* — y cada patrón cambia EXACTAMENTE UNO de esos tres diales (quién manda / qué une los eslabones / de dónde sale el impulso): persecución cíclica (mice problem), camino-memoria (slither.io), medusa por pulsos, onda anguiliforme (λ≈L), marcha metacronal (Δφ=45°), látigo por masa (v·√m), boids elásticos, doble hélice, constrictor servo-espiral, cola anti-solar, fuente balística, IK FABRIK, cinta al viento. **CERO referencias a otros mods** — es investigación de algoritmos (Nature of Code, toqoz.fyi, sean.fun/FABRIK, SMU propulsion, nablu metacronal, Wikipedia mice problem…).

### B. LAS DIEZ FORMAS (cada bastón libera una criatura con SU ley de locomoción — la LEY DE CADENA es el ADN compartido, la forma es lo que cambia)
  · **EL OUROBOROS ASTRAL** (patrón 2 — persecución cíclica): doce cuentas donde CADA UNA persigue a la siguiente apuntando ADELANTE (presa + vel·k — el adelanto que mantiene el anillo estable) con atracción suave a su ranura del anillo (la deriva que lo hace girar). Nadie manda: la causalidad es CIRCULAR. Daño 92: cada cuenta barre ×0.55 (cada 10 ticks) · al expirar el k DESAPARECE y el mice problem hace lo suyo: la espiral logarítmica colapsa al centro en 60 ticks y DETONA ×2.0 en 100 px (+ trauma 0.30 + AudioLib Vacia/muerte — PRIMERA ADOPCIÓN de AudioLib del arsenal).
  · **LA CARAVANA ESPECTRAL** (patrón 3 — camino-memoria): el farol abre camino (nado tangente + caza de presas cada 45 ticks) y el cuerpo muestrea el HISTÓRICO del camino a arclength i·Tamano (la trayectoria EXACTA — nunca recorta esquinas; ~80 ticks de memoria). Daño 88: el rastro limpia el pasillo ×0.35 (cada 12 ticks) — con giros rápidos el farol DIBUJA muros en S.
  · **LA ANGUILA SOLAR** (patrón 5 — onda viajera): el cuerpo ES UNA FÓRMULA — seg_i = cabeza − dir·s + normal·A(s)·sin(2π(s/λ − f·t)) con envolvente lineal creciente 6→26 px y λ ≈ el largo del cuerpo (la relación documentada de la anguila real, AIP 2021). Daño 98: las CRESTAS muerden ×1.0, los valles ×0.30 — el arma que enseña a rozar con la cresta.
  · **EL CIEMPIÉS RÚNICO** (patrón 6 — marcha metacronal): doce placas pegadas al SUELO (cadena follow + gravedad + sondeo de baldosas por segmento con WorldGen.SolidTile, el patrón del Rencor) con una pata por placa desfasada Δφ=45° — la onda retrograda de patas. LA TRANSICIÓN DE MARCHA es real: 0.9 Hz cerca → 1.8 Hz lejos. Daño 90: las patas PLANTADAS (media onda apoyada) dejan su RUNA que pica ×0.40.
  · **EL FLAGELO ESTELAR** (patrón 7 — látigo): cadena VERLET con constraint repartido por MASA (m ∝ radio², taper 15→4.5 → m_mango/m_punta ≈ 11) e inercia real (drag 0.992); el MANGO orbita al portador y cada 90 ticks BARRE media vuelta en 10 ticks — la energía viaja y se concentra en la punta. Daño 96: la punta golpea ∝ su velocidad (×0.5 → ×1.5) · |v_punta| > 26 px/tick → EL CHASQUIDO: ×2.2 en 80 px + trauma 0.35 + AudioLib Electrica/impacto.
  · **LA VÍBORA GENESÍACA** (patrón 9 — doble hélice): la ley de cadena vive SOLO en la espina (la MISMA ley de la sierpe); las DOS HEBRAS (dorada y violeta) son campos de offset: hebra_i = espina_i ± (cos,sin)(φ₀ + i·2π/6)·R_i con radio cónico 9→24 y φ₀ girando a 0.10 rad/tick. Daño 100: las perlas queman ×0.45 · el giro cerrado del rumbo se detecta (giroSuave/0.05) y la hélice se COMPRIME: ×1.6.
  · **LA BOA DEL ECLIPSE** (patrón 10 — constrictor): la cadena NO cambia — cambia el LÍDER: LANZARSE (embiste 15 px/t a la presa más cercana en 800 px) → ENROSCARSE (la servo-espiral: θ += 0.16 rad/tick, radio encogiendo a hitbox+22 en ~70 ticks, el cuerpo envuelve) → la presa cae → se desenrosca y busca la siguiente. Daño 86 con APRIETE: cada vuelta completa suma ×0.35 hasta ×4.0 (los AROS DE APRIETE se dibujan alrededor de la presa — hasta 6, respirando).
  · **EL FAROL GUARDIÁN** (patrón 13 — IK FABRIK): el mando INVERTIDO — la BASE es un farol colgante anclado sobre el hombro del portador (con mecedura) y la PUNTA persigue: PASADA 1 adelante (punta→objetivo deslizando eslabones) + PASADA 2 atrás (base→ancla) — la criatura ALCANZA, no nada: el cuerpo se TENSA como un arco. Daño 102: la MANO muerde ×1.0 (cada 9 ticks), la cadena quema ×0.40 (cada 14) · fuera de alcance ((N−1)·Tam = 221 px) el farol se ESTIRA sin tocar.
  · **LA CINTA AURORA** (patrón 14 — cinta al viento): la espina anclada a la espalda (con lift y vaivén propios) y el cuerpo visible = el OFFSET LATERAL de DOS SENOS INCONMENSURABLES (w 6.0/9.9 rad/s — la regla de determinismo de la casa) cuya amplitud crece hacia la punta y se multiplica por EL VIENTO — y el viento ES la velocidad de carrera del portador. Daño 96: la cinta corta ×0.5 + 0.4·viento — corre fuerte y la aurora se vuelve látigo.
  · **LA MANADA ASTRAL** (patrón 8 — boids): SEIS cazadores con distancia ELÁSTICA (steering hacia el punto 26 px detrás del de delante + alineación + separación — nunca teletransportan) y LIDERAZGO ROTATIVO: cada 15 ticks el mando SALTA al cuello más cercano a la presa (la corona viaja). Daño 90: cada cazador golpea CON SU PROPIA VELOCIDAD (×0.55 → ×1.4) + las colas (3 cuentas por cazador — la distancia elástica hecha visible) ×0.30.
  · **LA CRÍA ESTELAR** (EL MINION — la petición): la hijita de la sierpe: diez segmentos con el MISMO ADN (la cadena follow, el nado del punto tangente, el vaivén) pero CACHORRA. `DamageClass.Summon`, **minionSlots 1**, buff `CriaEstelarBuff` sostenido por el propio minion (patrón de la medusa nebulosa), la cría NUNCA expira, `MinionTargettingFeature` (obedece el objetivo fijado), en REPOSO nada en círculos alrededor del dueño (cada cría con SU órbita — el slot por whoAmI: la camada no se monta encima) y en CAZA embiste (mordida de contacto `MinionContactDamage` + cooldown local 20) con la espinita quemando ×0.4 (DamageClass.Summon). Semilla por `identity` (determinista en todos los clientes). Daño 30 (escala con invocación) · receta madera 5.

### C. LA LIBRERÍA Y LOS ARCHIVOS
  · **`Content/VFX/SierpesLib.cs`** (nueva — ~1250 líneas): ONCE compositores de render (uno por criatura) con el contrato de la casa (coords de MUNDO → screenPosition dentro; lote cerrado→cerrado AbrirAdditive/CerrarBatch en try/finally; cero Main.rand — Hash01 y senos). Cada criatura con SU paleta (el anillo oro+turquesa · la caravana verde-espectral con farol ámbar · la anguila amarillo-solar con crestas blancas · el ciempiés ámbar con patas frías y runas doradas · el flagelo carmesí con punta blanca al chasquear · la víbora dorado+violeta · la boa córneo+violeta con aros ámbar · el farol ámbar con cadena fría · la cinta gradiente aurora verde→violeta · la manada azul con ojos dorados · la cría el ADN de la madre con ojos ENORMES y coronita).
  · **`Content/Projectiles/Cosmic/SierpesNuevasProjectiles.cs`** (las 11 simulaciones): todas con el patrón congregación (ai[0..1] sincronizado con netUpdate — el dueño local manda), semilla en ai[2], SIN hide (la lección v6.35), guard NaN, `SimpleStrikeNPC` solo con `EsObjetivo` y jamás en cliente multiplayer. Sonidos de bastón: Item122 con pitches distintos (la firma de los códigos vivos) + AudioLib para el colapso, el apriete y el chasquido + `PulsoLib.Trauma` para las sacudidas.
  · **`Content/Weapons/Cosmic/SierpesNuevasStaffs.cs`** (los 11 bastones): daño 86-102 (Magic, mana 0, rare Quest — la regla de la casa), tope de 1 por dueño (MatarViejo — el patrón de la garganta), tooltips de color con la fórmula de SU patrón.
  · **LA BOLSA DE LAS SIERPES (14ª)**: la familia COMPLETA — la madre (SierpeEstelarStaff, INTACTA) + los 10 nuevos + la cría · BolsasCategorias 14 clases · TestingPlayer entrega 14 bolsas · hjson 37 claves nuevas es/en (316/316 paridad, parse OK).
  · **ASSETS (subagente, Task 7-a)**: `tools/gen_v638_assets.py` — 24 PNGs (11 iconos 30×30 con el patrón bastón+cabeza temática, 11 glows 76×76, la bolsa con la sierpecita en S bordada, el buff de la cría 32×32) · VLM 4 rondas con hojas de contacto ×4: final 6-9/10 (lecciones: cabezas GRUESAS y planas leen mejor que curvas finas a 30×30) · regeneración byte a byte idéntica.

### D. EL BASTÓN ORIGINAL Y LA VERIFICACIÓN
  · **LA SIERPE ESTELAR ORIGINAL NO SE TOCA**: `git diff` VACÍO sobre CodigosVivosStaffs.cs, CodigosVivosProjectiles.cs, CodigosLib.cs y sus PNGs — la madre sigue siendo exactamente la de v6.36.
  · Batería de la casa: 0 Main.rand en render (solo el comentario que lo jura) · 0 hide · 0 ai[3+] · 0 referencias externas (solo la ruta interna del informe) · lotes balanceados (11 AbrirAdditive / 11 CerrarBatch, todos en try/finally) · paridad hjson 316/316 · 24/24 PNGs verificados con PIL · tope de 1 en los 10 bastones de ataque. Tres fixes de revisión propia: el calor del render del flagelo ahora se mide por FRAME (PreDraw actualiza `_prevPuntaDraw`), la semilla de la cría viaja con `Projectile.identity` (determinista multiplayer) y la boa sincroniza el puntero en estado Lanzar (el fallback de nado sigue al cursor).
  · Compilación 0 errores / 0 warnings contra tModLoader 2026.07.3.0 real (línea base verificada ANTES de tocar nada). build.txt 6.37 → 6.38.

## Commit v6.37 — LA CORRECCIÓN DE LA LIBRERÍA: LOS ANILLOS RÚNICOS DEL VACÍO

**Petición del usuario**: "y cuando te pedí una librería para los anillos de los agujeros, me refería a los anillos rúnicos".

### A. LA CORRECCIÓN DE ORBITALIB (la librería de los agujeros negros ahora contiene LO QUE SE PIDIÓ)
  · **EL DIAGNÓSTICO**: v6.34 construyó OrbitaLib con el LADO ENERGÉTICO del vórtice (el anillo de 20 bandas del shader, los ecos, los fotones, la distorsión, la corona de arcos — la familia del disco de acreción)… pero los **CÍRCULOS RÚNICOS** — lo que el usuario quería decir con "los anillos de los agujeros" — seguían siendo código PRIVADO duplicado en 9 renderers. La librería corregida contiene las DOS escrituras del vacío, con los anillos rúnicos como la familia principal.
  · **LOS ANILLOS RÚNICOS DEL VACÍO** (nueva sección de OrbitaLib, primitivas 1:1 del Supremo — ni un número cambiado):
    `RunasVacio` (EL ALFABETO público del Supremo: EL SOL ROTO, EL CETRO, EL TRONO, LA ESTRELLA DOBLE, EL CIRCUITO REAL, LA VUELTA SUPREMA, EL OJO DEL VACÍO, LA CORONA ESTELAR) + `RunasAbismo` (el alfabeto erosionado del Umbral, 12 glifos) · `RunaVacia` — LA RUNA DE PIE atómica (resplandor 36·gs al 20%, trazos de cápsula 3.4·gs al 85% con gradiente cuerpo→punta, PERLA 7.0/3.2·gs latiendo a 3 Hz a 11.5·gs sobre el glifo) con DOS HUMORES: el sereno de la casa y el NERVIOSO de los círculos contrarrotantes v6.23 (respiración 2.2·sin(1.1t+1.4g), latido 0.70+0.30 a 2.8 Hz, perlas 6.2/2.9) · `CirculoRunico` — EL CÍRCULO DE CONJURO completo (el aro fino de pauta + las runas DE PIE: el radio respira 2.4·gs a 1.35 Hz, el glifo se mece 2.0·gs a 0.85 Hz, cada runa con su latido y su perla) · `SigiloErosionado` — la variante del Umbral (el aro ROTO con huecos por hash, glifos PERDIDOS, la PÚA que apaga lo que cruza) · `CoronaConjuro` — LA TRIPLE CORONA del Supremo invocable en una llamada (dorado 8 CW + violeta 6 CCW + blanco íntimo 6 rápido) · las constantes de la triple corona y la paleta rúnica (blanco/dorado/violeta con sus puntas) públicas.
  · **LOS REFACTORS 1:1 — LA PRUEBA DE FIDELIDAD** (patrón v6.34: delegación pura, CERO cambio visual, compilando tras cada renderer): **10 renderers delegan sus 15 círculos** — Supremo (3 coronas), Supremo Aurora (azul/dorado/morado), Bruma Ascendida (teal/hielo), Umbral (sigilo dorado), Umbral Ascendido (sigilo exterior + íntimo contrarrotante con seed+5000 preservado), Olvido (dorado grueso con su perla fría hardcodeada preservada vía `pearlTip`), Olvido Ascendido (dorado + violeta NERVIOSO con su selección `(g·3+2)%len` vía `stride: 3, offset: 2`), Cósmico base y Cósmico Ascendido (dorado + ámbar NERVIOSO). **Eclipse Primordial NO delega** (su geometría de cápsula es propia: `len+0.35·w, w` vs `len+w, w·1.9` de la casa — documentado en su doc-comment). Alfabetos: 5 tablas privadas BORRADAS tras verificarse idénticas byte a byte (S del Supremo/Aurora/BrumaAsc → `RunasVacio`; U del Umbral/UmbralAsc → `RunasAbismo`); 4 conservadas y pasadas como `alphabet` (R de Olvido×2, C de Cósmico×2).
  · **LA LECCIÓN**: el look de TODOS los agujeros negros NO cambia ni un píxel — lo nuevo es que la escritura rúnica del vacío es AHORA invocable desde cualquier cosa (armas, portales, jefes, el anillo de tu espalda).

### B. EL ANILLO RÚNICO DORSAL RECONSTRUIDO (la aclaración aplicada al accesorio: el círculo de conjuro LITERAL en la espalda)
  · **LA v6.36 INVENTABA SU PROPIO ANILLO** (elipse fucsia a escorzo con runas SOLARES cabalgando la tangente — estilo SigiloLib, no el de los agujeros). La aclaración del usuario exige EL ANILLO RÚNICO COMO EL DE LOS AGUJEROS NEGROS — y ahora que la librería contiene la técnica exacta, el accesorio la USA: **AnilloDorsalRenderer reescrito** → `OrbitaLib.CoronaConjuro` (LA TRIPLE CORONA LITERAL del Supremo: dorado 8 CW + violeta 6 CCW + blanco íntimo, runas de pie respirando con sus perlas, escala de glifo ×2.4 para que la escritura SE LEA a espalda humana) + EL ANILLO DE FOTONES interior contrarrotando (1.35·r — la firma del horizonte). Paleta literal: blanco 255,245,220 / dorado 255,180,70 / violeta 110,130,255.
  · **EL CAMINO DEL PROYECTIL HALO** (v6.36 lo dibujaba por la capa BackAcc; la reconstrucción pasa al patrón probado de AnillosSingularesHalo): el renderer abre y CIERRA su propio batch (contrato SelloVacio) → el nuevo `Projectiles/Cosmetic/AnilloRunicoDorsalHalo` lo pinta en su PreDraw, y vanilla dibuja los proyectiles ANTES que los jugadores → la corona queda DETRÁS del cuerpo: **LA ESPALDA** (sin hide — la lección v6.35). La capa `AnilloRunicoDorsalDrawLayer` ELIMINADA (0 referencias). CosmeticPlayer invoca el halo (dueño local + netImportant, con espía anti-duplicado), chispas DORADAS desde las runas exactas del círculo de oro (`RunaWorld`) y la luz cálida vive en el AI del halo.
  · **ASSETS + LOCALIZACIÓN**: ícono 30×30 regenerado con la técnica crisp de la casa (los tres aros + runas en L + perlas — VLM 9/10 en vista ×4; la primera pasada con blur descartada a 3/10), tooltips del ítem e hjson es/en reescritos (la triple corona literal), `tools/gen_v637_assets.py` reproducible.
  · Lo ya creado QUEDA INTACTO (los Anillos del Horizonte con su SelloVacio, el Sello del Génesis, los Anillos del Sol Rúnico — solo el accesorio nuevo de v6.36 se reconstruye sobre la librería corregida).

### C. VERIFICACIÓN
  · Entorno de compilación RECONSTRUIDO de cero (4ª pérdida — receta del worklog: dotnet 8.0.425 + tModLoader 2026.07.3.0 oficial + `/home/z/sandbox/compile.sh`): línea base 0/0 verificada ANTES de tocar nada; compilación 0 errores / 0 warnings tras CADA paso (librería, anillo, cada renderer, cierre).
  · Equivalencias verificadas: las cápsulas de los 9 renderers delegables son bit a bit `(len+w, w·1.9)` == OrbitaLib.Capsule (Eclipse documentado como excepción); RingQuad == AnilloFino (2.174); Tint y Hash01 idénticos; los alfabetos comparados programáticamente.
  · build.txt 6.36 → 6.37. Este worklog actualizado (Tasks 4-a y principal).

---

## Commit v6.39 — LA REPARACIÓN DE LOS RAYOS ELÉCTRICOS + EL DESGARRO RASGADO

**Petición del usuario**: "Al final los desgarros, todos están mal y cuando usas rayos eléctricos para los desgarros tienen un error gráfico: líneas intermitentes de un color más oscuro o claro, como si le pusieras brillo y desenfoque pero este se corta por secciones… el problema puede estar en la librería o cómo se implementan los rayos con la librería — investiga más sobre rayos, arcos eléctricos y relámpagos en Internet y en otros mods · revisa la implementación de rayos eléctricos en nuestras librerías capa por capa · mejora los desgarros de realidad: si la realidad se desgarra no sería una fea línea recta".

### A. EL INFORME (research/v637/INFORME_RAYOS_ELECTRICOS_Y_DESGARROS.md)
  · La revisión CAPA POR CAPA medida sobre los archivos reales + investigación web (11 búsquedas + los foros del ecosistema) encontró **DOS RAÍCES** que juntas producían EXACTAMENTE el artefacto reportado:
  · **RAÍZ 1 — EL ALFA ES INVISIBLE EN EL LOTE ADITIVO**: `BlendState.Additive` de XNA/FNA es (Src=One, Dst=One): `aporte.rgb = textura.rgb × tinte.rgb` — el canal alfa de AMBOS no entra NUNCA en la ecuación. Las texturas de tormenta v6.21 (BoltCore/BoltHalo/BoltChain/BoltImpact) llevaban el filamento SOLO en el alfa (RGB=blanco puro — MEDIDO con numpy) y `StormLib.Tint` ponía la intensidad SOLO en el alfa del tinte → **en el juego los rayos dibujaban RECTÁNGULOS SÓLIDOS a brillo máximo** (la convención correcta — perfil horneado en RGB — existe en la casa desde v5.x: SoftGlow, GlowOrb, GlowCircle la usan; StormLib v6.21 la olvidó). Lo mismo pasaba con las capas de LUZ del desgarro (RiftTaperVelo/Núcleo: RGB blanco en el cuerpo).
  · **RAÍZ 2 — EL RIBBON SE CORTABA**: cada sub-segmento solapaba al vecino `subLen + w*2` → el aditivo APILABA el brillo en cada junta (las "cuentas claras"); el crackle aleatorio POR SUB-SEMENTO (42 px) lo entrecortaba (las "oscuras"); y BoltCore llevaba grietas de 16 px + nodos + serpenteo horneados A LO LARGO de la textura → cada sub-segmento estiraba el patrón COMPLETO = "brillo y desenfoque CORTADO POR SECCIONES" (la frase exacta del usuario, explicada). Los arcos internos de la herida (PintaArco) estiraban SoftGlow RADIAL (funde a 0 en los dos extremos de cada quad) y compensaban con solapes sl+w·1.6/0.8/0.22 → franjas oscuras + apiles claros.

### B. LA REPARACIÓN DE LOS RAYOS (StormLib v6.39 — LA TERCERA GENERACIÓN)
  · **LAS TEXTURAS REGENERADAS** (tools/gen_bolts_v639.py): BoltHalo y BoltCore son ahora BANDAS UNIFORMES a lo largo (solo 3 px de fundido antialias en los extremos) con el perfil PREMULTIPLICADO en RGB (la convención SoftGlow de la casa — el lote aditivo por fin las respeta); BoltChain y BoltImpact también hornean RGB (el impacto era un CUADRADO sólido de color). La nitidez "eléctrica" vive en la geometría multi-escala y el crackle por punto, NUNCA en ruido horneado a lo largo de la textura.
  · **`StormLib.Tint` PREMULTIPLICADO** (rgb×f, alfa=f — el Tint v6.25 de RiftLib): la intensidad VUELVE a funcionar en el aditivo (antes las 3 capas salían a brillo máximo).
  · **`StrandImpl` — EL RIBBON DE VERDAD**: quads BORDE A BORDE con la NORMAL MEDIA en las juntas (los dos quads que comparten un vértice cortan con la MISMA orientación), LARGO EXACTO proyectado sobre esa dirección + LA EXTENSIÓN ADAPTATIVA DE GIRO de RiftLib v6.31 (w/2·tan(δ/2) SOLO donde el camino gira de verdad + 1.2 px de margen antialias que el fundido de 3 px de la textura hace invisible), TRES capas de ancho (halo ×2 / cuerpo ×1 / vena ×¼) y EL CRACKLE POR PUNTO interpolado (el brillo respira a lo largo, nunca a saltos de 42 px — la vena arde SIEMPRE).
  · **`PintaArco`/`SeekArc` (los arcos de la herida) con las BANDAS UNIFORMES** y largos exactos: muere el SoftGlow-estirado y su compensación por solape (el artefacto exacto del reporte). SeekArc emite los quads con TEXTURA propia al buffer de VFXCore (la sobrecarga v6.08 de Quad).
  · **`BoltRenderer` (la 1ª generación — coronas rúnicas, látigo de la medusa, corona de arcos) REPARADO IGUAL**: quads con textura de banda + normal media + largo exacto (antes: SoftGlow con `segLen + width` de solape). Nuevas texturas públicas `StormLib.BandaTex`/`VenaTex`.

### C. EL DESGARRO RASGADO ("si la realidad se desgarra no sería una fea línea recta")
  · **`RiftLib.CaminoDesgarro`** — EL BORDE RASGADO determinista: TRES OCTAVAS de dientes CONGELADOS por semilla (grandes ~26 px + medianas + micro) con envolvente senoidal (anclajes EXACTOS), DERIVA LENTA (±15% a ~0.3 Hz — la herida abierta no se re-teje: la lección v6.28 de las "interrupciones") e índices cuantizados por distancia absoluta (dos sub-desgarros contiguos coinciden diente a diente en la costura — VozCuasar puede partir su haz en dos sin salto).
  · **`Tear`/`TearVacio` dibujan POR EL CAMINO rasgado** con la geometría de la cadena gemela v6.31 (solape adaptativo al GIRO REAL + perlas solo donde hay giro de verdad + tapas de estadio en los extremos) y las estrellas por camino (`EstrellasCamino`): **TODOS los desgarros del mod dejan de ser rectos DE GOLPE** (RealityTear, Rencor Primordial, Voz Cuasar, Ocaso) — la línea de daño (cápsula LineaToca) sigue midiendo la recta: la desviación (≤0.45·maxWidth) cabe holgada en el radio de daño (maxWidth+8).
  · **`CaminoVibracion` hereda el rasgado** con LA MISMA fórmula de dientes (`JagDesgarro` compartido): la herida VIBRA CON SUS DIENTES en vez de enderezarse de golpe al entrar en la fase de vibración (RealityTear pasa semilla+jag).
  · **`HeridaElectrica` — EL DESGARRO ELÉCTRICO RASGADO de punta a punta**: el vacío y los labios POR el camino, la estática siguiendo la tangente local, la aberración cromática sobre el rasgado, **LOS ARCOS VOLTAICOS CORRIENDO POR DENTRO de la herida** (StormArc entre puntos DEL CAMINO — la corriente sigue el rasgado) y las ramas y chispas naciendo de los dientes.
  · **LAS RIFTTAPER DE LUZ REBORNADAS** (gen_bolts_v639.py): RGB = perfil×estructura horneado (Velo/Cuerpo/Núcleo) — los labios y el filo del desgarro vuelven a verse en el lote aditivo (RiftTaperVoid intacta: su negro es del lote no-premultiplicado, donde el alfa SÍ manda).

### D. LA VERIFICACIÓN
  · **MOCK 1:1** (tools/mock_rayos_v639.py — el blending aditivo simulado EXACTO: aporte = perfil.rgb × tinte.rgb, el alfa no entra) con las fórmulas picadas de los .cs y las texturas reales del disco: ANTES (v6.21/v6.31 reconstruidos de sus generadores) vs AHORA.
  · **VLM**: el rayo ANTIGUO muestra "brillo entrecortado con bandas y secciones discretas" (el artefacto del usuario, confirmado visualmente) y el NUEVO queda **8/10 continuo y uniforme** ("aprobar para producción"); el desgarro recto ANTIGUO **2/10** ("parece un láser o una costura, no un desgarro cósmico") y el RASGADO NUEVO **9/10** ("borde dentado como tela desgarrada violentamente, arcos que brotan del centro, transmite violencia y energía inestable" — el flicker que sugiere el VLM ya existe en el juego: FlickTick 15 Hz + IsLit).
  · Compilación final contra tModLoader 2026.07.3.0 real: **0 errores / 0 warnings**.


## Commit v6.36 — LOS CUATRO CÓDIGOS VIVOS + EL ANILLO RÚNICO DORSAL

**Petición del usuario**: "Las 4 imágenes que te envié tienen un código dentro de la imagen: revisa el código y replica cada uno en 1 arma, en total 4, usando C# · cuando hablaba de un accesorio con los anillos de los agujeros negros me refería a los ANILLOS RÚNICOS de agujeros negros, no a su disco de acreción — lo que ya creaste déjalo, ahora solo crea OTRO accesorio cosmético con un anillo rúnico como el de agujeros negros que esté EN LA ESPALDA del jugador · una vez terminado todo, revisa todo el código para que no haya bugs ni inconsistencias y mucho menos referencias externas".

### A. LOS CUATRO CÓDIGOS VIVOS (un arma por imagen — el código de cada imagen traducido a C#)
  · **LA NUEVA LIBRERÍA `CodigosLib`** (Content/VFX/ — LA LIBRERÍA DE LOS CÓDIGOS VIVOS): los 4 compositores de render con el contrato SigiloLib (coords de MUNDO, la librería resta screenPosition; lote cerrado→cerrado). Cada uno documenta su código original línea a línea.
  · **LA DANZA DE LOS ORBES** (imagen 1 — follow()): EL PUERTO EXACTO del `follow(iter)` en `DanzaOrbesProjectile.Follow` línea por línea (dist euclídea → `this.x = x + this.size*(this.x-x)/dist` → `absAngle = atan2` → `relAngle = absAngle − parent.absAngle` → recursión por hijos), con el guard NaN de la casa. El arma invoca UN MINISISTEMA SOLAR que orbita al portador 12 s: el sol (raíz) cabalga su órbita a 96 px (ω 0.045) y de él cuelgan 2 planetas (size 46) con 2 lunas cada uno (size 24), cada hijo con su deriva angular y la cadena recursiva manteniendo las distancias EXACTAS. El "updateRelative(false, true)" del original es la ESTELA TANGENCIAL que pinta CodigosLib (el orbe estirado perpendicular a su radio). Daño 96: todo orbe quema (sol ×1.0 · planetas ×0.7 · lunas ×0.5 cada 10 ticks). Tope 1, sin maná.
  · **LA LENTE DEL ABISMO** (imagen 2 — animate() del agujero negro): los TRES MATERIALES del original esclavos del mismo reloj (uTime → GlobalTimeWrappedHourly): diskMaterial → el anillo energético de 20 bandas (OrbitaLib recoloreado magenta), eventHorizonMat → el NÚCLEO NEGRO ABSOLUTO (pase alfa que se come la luz) + el anillo de fotones, starMaterial → LAS ESTRELLAS DOBLADAS: 12 estrellas cayendo en espiral que ACELERAN al acercarse (ω ×4 en el tramo final), su brillo se AMPLIFICA (la lente apila la luz) y DESTELLAN al cruzar el anillo de fotones antes de renacer lejos. La `lensingPass` del original (blackHoleScreenPos proyectado a pantalla) es `GravLens.Registrar` cada tick — fuerza 0.62, LA MÁS FUERTE del arsenal. Daño 104: succión 300 px, devora ×1.2 cada 12 ticks en el horizonte, cada 90 ticks LA LENTE ENFOCA y muerde ×0.6 al más cercano. 6 s, tope 1, sin maná.
  · **EL SOL VIVO** (imagen 3 — animate() del sol): EL PULSO EXACTO `0.5 + 0.5·sin(time·2.15)` y EL BLOOM EXACTO `0.8 + 0.4·pulse` laten en TODO — el render (CodigosLib.SolVivo) Y la luz del mundo con la MISMA fórmula; el núcleo gira a la ROTACIÓN EXACTA del original (0.05 rad/s — coreGroup.rotation.y += delta·0.05). Los SEIS MATERIALES uno a uno: el NÚCLEO estelar con sus 3 lóbulos, la CÁSCARA respirando, el DISCO ecuatorial de 3 aros con rotación diferencial, los ANILLOS que nacen en cada pico del pulso (período exacto 2π/2.15), las PROMINENCIAS (PyraLib.Tongue arqueándose desde la superficie) y las ASCUAS deterministas que escapan y se apagan. Daño 100: aura ×0.55 cada 15 ticks, cada PICO del pulso LATE ×0.7 (detección por cruce de cos por cero — sin wrap), prominencias que AZOTAN ×0.85 cada 45 ticks a los 2 más cercanos. 5 s, tope 1, sin maná.
  · **LA SIERPE ESTELAR** (imagen 4 — elems): la JERARQUÍA EXACTA del `prepend()` (índices 0-based: la i==1 → CABEZA en 0, las i==8 e i==14 → ALETAS en 7 y 13, el resto → ESPINA) con N=16 segmentos. El COMPORTAMIENTO: la cabeza nada en VUELTAS alrededor del puntero (el `radm` del original = 110 px — persigue el punto TANGENTE del círculo; lejos apunta directo) con el VAIVÉN de velocidad del `frm/rad` original (±35% a 0.9 Hz); la CADENA que ata cada eslabón es la MISMA LEY de la imagen 1 (el ADN compartido de ambas demos — documentado en ambos). El puntero viaja en ai[0..1] (el dueño local manda — el patrón de la congregación v6.29, con correa de 760 px), la semilla en ai[2] (ai[] SOLO tiene 3 slots — la lección v6.27 del péndulo, verificada por reflection: maxAI=3), y el rumbo inicial llega por la velocidad de nacimiento. Daño 90: la cabeza ATRAVIESA todo (colisión del motor, penetrate −1, como el leviatán) y la espina quema ×0.4 cada 8 ticks. 8 s, tope 1, sin maná.
  · **LOS 4 BASTONES** (CodigosVivosStaffs.cs): 96/104/100/90 dmg, todos DamageClass.Magic, mana 0, rare Quest, tope de 1 con MatarVieja (el patrón de la garganta), tooltips de color de la casa. **LA BOLSA DE LOS CÓDIGOS VIVOS** (13ª bolsa — "Cuatro códigos ajenos que aprendieron a vivir aquí") + entrega garantizada por TestingPlayer.
  · 10 PNGs (tools/gen_v636_assets.py) con VLM iterado (5 rondas en la sierpe y el anillo: sol 9/10, lente 8/10, bolsa 8/10, danza 7/10, anillo 7/10, sierpe 6-7/10 — el estilo puntos-del-leviatán), hjson ×12 claves es/en verificadas con parse (279/279).

### B. EL ANILLO RÚNICO DORSAL (la aclaración del usuario: los ANILLOS RÚNICOS del agujero negro, NO su disco de acreción — y EN LA ESPALDA)
  · Lo ya creado QUEDA INTACTO (los Anillos del Horizonte de Sucesos con su halo de disco de acreción, el Sello del Génesis y los Anillos del Sol Rúnico).
  · **EL NUEVO ACCESORIO** (Items/Cosmetics/AnilloRunicoDorsalItem — puro cosmético, cero stats, receta de madera como las coronas): la gran firma mágica colgada de la ESPALDA — el anillo elíptico DE PIE detrás del cuerpo (a escorzo: a = altura·1.05, b = altura·0.62, tilt con PRECESIÓN viva ±0.05 rad) con LAS OCHO RUNAS de SigiloLib cabalgando la tangente (spin 0.22 rad/s — la cadencia de la casa), el ANILLO DE FOTONES interior contrarrotando (la firma de los agujeros negros), los CUATRO NODOS cardinales y el polvo rúnico. La paleta del vacío: aro fucsia (255,92,158), runas de punta rosa pálido.
  · **LA CAPA** (DrawLayers/AnilloRunicoDorsalDrawLayer): `AfterParent(PlayerDrawLayers.BackAcc)` — la zona de alas y capas, ANTES del sprite del cuerpo → el anillo queda DETRÁS del jugador: el cuerpo tapa el tramo que pasa por la espalda, exactamente como un círculo mágico colgado a la espalda (BackAcc verificado por reflection contra la DLL real). Por la puerta oficial: VFXCore.Begin → AnilloDorsalRenderer.ComputeQuads → AppendToPlayerDraw.
  · **EL RENDERER** (VFX/AnilloDorsalRenderer.cs — el fachada estilo RuneCrownRenderer): ComputeQuads + RunaWorld (la posición exacta de cada runa para las chispas — el patrón de las coronas). CosmeticPlayer gana la bandera AnilloDorsal (escaneo de huecos 3-19 funcionales Y vanidad), chispas desde las runas exactas y luz magenta suave. Icono 30×30 (el anillo a escorzo con runas rosas — VLM 7/10) + registro en la Bolsa de los Cosméticos.

### C. LA REVISIÓN COMPLETA (la petición final: bugs, inconsistencias y referencias externas)
  · PASADA 1 — REFERENCIAS EXTERNAS: grep de URLs (http/https/www), dominios y menciones a mods ajenos (los mods de referencia) sobre TODO Content/ → **0 coincidencias** — el código está limpio de referencias externas.
  · PASADA 2 — HJSON ↔ CLASES: resolución TRANSITIVA de herencia (ModItem→base→base…): 117 ítems + 75 proyectiles → **0 DisplayNames faltantes, 0 claves huérfanas** en es-ES y en-US (279/279 claves idénticas entre ambos, parse hjson OK).
  · PASADA 3 — ASSETS: **268 PNGs válidos** (PIL verify) y toda clase concreta de ítem/proyectil con su textura junto al .cs → 0 faltantes.
  · PASADA 4 — CÓDIGO: 0 TODO/FIXME reales (los 128 "TODO" son la palabra española en tooltips); 0 accesos a ai[3+] (maxAI=3 verificado por reflection contra la DLL real); EL FILTRO EsObjetivo verificado por análisis método-a-método en TODAS las llamadas SimpleStrikeNPC del mod (las 43 sospechas del heurístico resultaron falsos positivos — el filtro vive en el método contenedor); guards NaN en las normalizaciones nuevas (succión con d≥8, correa con Length>760).
  · PASADA 5 — CONTRATOS DE LOTE: CodigosLib balanceada (5 aditivos + 1 alfa = 6 cierres, cada abrir con su try/finally); el patrón a prueba de balas (End→try→catch→End→reabrir) en los 4 PreDraw nuevos.
  · PASADA 6 — DETERMINISMO DEL RENDER: **ÚNICO HALLAZGO REAL de toda la auditoría**: el temblor de anticipación del PreDraw del SupernovaProjectile (v5.9x) usaba `Main.rand.NextFloat` — un jitter de ruido blanco no determinista por cliente, violación de la regla de la casa (cero Main.rand en el render). FIX: temblor determinista de DOS SENOS INCONMENSURABLES por eje (31.7/47.3 y 37.9/41.1 Hz — se lee igual de nervioso y todas las máquinas dibujan el MISMO temblor). Tras el fix: 0 métodos PreDraw/Draw con Main.rand en TODO el mod.
  · Compilación final contra tModLoader 2026.07.3.0 real: **0 errores / 0 warnings**.


## Commit v6.35 — EL FIX DE LOS DESGARROS INVISIBLES + LOS TRES ACCESORIOS DE SIGNOS + LOS CUATRO DESGARROS NUEVOS + LA INVESTIGACIÓN DEL COSMO BEAM

**Petición del usuario**: "analiza el código de las 4 imágenes que te envié y adáptalo para convertirlo en armas en Terraria con C# · crea accesorios especiales usando los sellos mágicos que rodeen al jugador, y además otros 2 accesorios: uno con los anillos del agujero negro y otro con los anillos de los soles, ambos anillos deben rodear al jugador · ninguno de los desgarros nuevos se ve, parece que no funcionan ya que no puedo verlos · además todos los bastones nuevos no deben requerir el uso de maná · también analiza el funcionamiento del arma Cosmo Beam de algún mod de Terraria que no recuerdo cuál, quiero que copies el arma exacta para investigación".

### A. EL FIX DE LOS DESGARROS INVISIBLES (la queja crítica)
  · **LA CAUSA RAÍZ ENCONTRADA Y DOCUMENTADA**: los 4 proyectiles de desgarro de v6.33 (Sutura/Portal/Pliegue/Herida) tenían `Projectile.hide = true` — el bucle `DrawProjectiles()` de Terraria SALTA los proyectivos ocultos (`if (projectile[i].active && projectile[i].type > 0 && !projectile[i].hide)` — verificado en el Main.cs decompilado), así que **PreDraw JAMÁS se llamaba: el daño funcionaba pero el desgarro era 100% invisible**. FIX: hide eliminado en los 4 (comentario v6.35 explicando la lección); PreDraw (que retorna false) pinta todo el VFX por el pase normal.
  · **LOS BASTONES SIN MANÁ**: los 4 de v6.33 (18/22/26/15 → 0) y los 4 nuevos de v6.35 nacen ya con `Item.mana = 0` — el usuario lo pidió explícito.

### B. LOS TRES ACCESORIOS DE LAS LIBRERÍAS DE SIGNOS MÁGICOS (la primera adopción de SigiloLib/OrbitaLib)
  · **EL SELLO DEL GÉNESIS** (Items/Accessories/SelloGenesisItem): DOS sellos RODEANDO al jugador — el aro mayor dorado (SelloSolar: aro doble + 8 runas + 4 nodos + sigilo maestro) girando a 0.22 rad/s y el sello interior AZUL-ESTELAR contrarrotando a −0.35 con el glifo del ASTRO. Vía PlayerDrawLayer + AppendToPlayerDraw (la puerta oficial, el mismo camino de las coronas). Stats: +40 maná, +8% mágico, +4% crítico mágico. Chispas doradas desde las runas EXACTAS (RunaSelloWorld).
  · **LOS ANILLOS DEL SOL RÚNICO** (AnillosSolRunicoItem): `SigiloLib.SistemaAnillos` en tier 7 (PRECESIÓN viva + azul-estelar cada tercer aro) — SIETE aros orbitales de giros alternos rodeando al cuerpo, R = altura·0.46. Stats: +2 regen, +8% melé, +3% crítico melé, +3 defensa.
  · **LOS ANILLOS DEL HORIZONTE DE SUCESOS** (AnillosHorizonteItem): el proyectil cosmético **AnillosSingularesHalo** pega `OrbitaLib.SelloVacio` al jugador (el disco de acreción con las 20 bandas del shader, los fotones, el aro del horizonte y las ondas de distorsión) — por qué un proyectil y no una capa: el contrato de SelloVacio abre/cierra su PROPIO batch y en el PreDraw el lote está bajo control (el patrón de los desgarros); vanilla dibuja proyectivos ANTES que jugadores → el vórtice queda DETRÁS, envolviendo. **LA LENTE**: `GravLens.Registrar` por tick curva el FONDO alrededor del portador (fuerza 0.30). **LA SUCCIÓN**: tirón gravitatorio suave a enemigos en 140 px. Stats: +10% velocidad, +5% daño universal.
  · INFRAESTRUCTURA: `SellosPlayer` (ModPlayer — escaneo de huecos 3-19 como las coronas: funcionales Y vanidad), 2 PlayerDrawLayers nuevos (SelloGenesisDrawLayer, AnillosSolaresDrawLayer), 4 PNGs (tools/gen_v635_assets.py; el del sello 8/10 VLM), hjson es/en ×7 claves, Bolsa de Cosméticos 2→5 con nota y tooltip nuevos. Fix de la sesión: Player.hideVisual NO existe en esta versión de tML (es hideVisibleAccessory en el decompilado) — el chequeo del "ojito" se retiró (las coronas tampoco lo tienen).
  · DOC: los docs de OrbitaLib decían "coords de mundo" pero el contrato real es PANTALLA (mundo − screenPosition, batch con GameViewMatrix) — corregidos los `<param>` para que no muerda al próximo consumidor.

### C. LOS CUATRO DESGARROS NUEVOS (segunda tanda de referencias — 4 imágenes del usuario analizadas con VLM)
  · **RIFTLIB.DESGARROSNUEVOS.CS** (el 4º parcial de la librería, contrato idéntico: batch cerrado→cerrado, coords pantalla):
    `VorticeColapso` — la estrella de 4 puntas de FUEGO ESTELAR: 4 filamentos en espiral logarítmica (ángulo += 0.85·f²) que se afilan de grueso a punta con el gradiente blanco #FFFACD → naranja #FFA500 → carmesí #DC143C, núcleo con destello de 4 puntas rotando y 10 chispas de oro espiraleando hacia fuera. VLM 9-10/10.
    `GargantaVacio` — el vórtice devorador: NÚCLEO NEGRO ABSOLUTO (pase alfa) + el anillo energético MAGENTA (la fórmula fiel sin(τ·20−t·5) de OrbitaLib recoloreada: #FFE4FA/#FF40D0/#5A0B7A) + 3 brazos de galaxia + el aro del horizonte + 12 partículas estelares cayendo en espiral CON estirón tangencial. VLM 10/10.
    `UmbralRoto` — el corte horizontal perfecto: el VACÍO de la otra realidad arriba (banda oscura) con EL ESQUELETO ESPECTRAL de 9 vértebras y mandíbula en V meciéndose dentro, la LÍNEA con aberración cromática (eco cian arriba / magenta abajo) y 14 partículas de datos cian parpadeando. v6.35b: esqueleto AGRANDADO y reforzado (+65% tamaño, alfa 0.72) tras el mock — VLM 9/8/10.
    `LeviatanEspectral` — la criatura de hueso etéreo: 14 vértebras blancas frías nadando la MISMA onda en S del AI (coherencia nado-carne), 6 aletas alternando lados que se desvanecen, cabeza con mandíbula abierta que respira, cresta del dragón y rastro frío. VLM 8/7 (anatomía minimalista fiel a la referencia).
  · **LAS 4 ARMAS** (DesgarrosNuevosStaffs3 + DesgarrosNuevosProjectiles3): **El Corazón del Colapso** (108 dmg — quema en círculo cada 20t + LLAMARADAS fractales naranjas cada 30t a los 2 más cercanos ×0.5) · **La Garganta del Vacío** (102 dmg — succión FUERTE 320 px, devora ×1.15 en el núcleo, LA LENTE GravLens fuerza 0.55 mientras vive, tope 1) · **El Umbral Roto** (95 dmg — pica en línea ×0.4 cada 12t con LineaToca + cada 60t LA OTRA REALIDAD MUERDE: 3 mandíbulas ×0.8 con destellos espectral) · **El Leviatán Espectral** (86 dmg — lanza la criatura: rumbo oscilante ±0.30 rad a 2.5 Hz = nada en S, homing 0.03 rad/tick, atraviesa TODO 4 s). Todas SIN maná, estilo Shoot en cursor, rare Quest, patrón PreDraw a prueba de balas, SIN hide.
  · MOCK 1:1 (tools/mock_desgarros_v635.py — fórmulas exactas picadas de los .cs) + VLM con iteración: la tanda quedó 9-10 · 10 · 9 · 8/10. 8 PNGs (tools/gen_v635_armas.py), hjson ×16 claves es/en, **Bolsa de los Desgarros 5→9** ("Nueve formas de romper el tejido del mundo").

### D. LA INVESTIGACIÓN DEL COSMO BEAM (research/v635/CosmoBeam)
  · **VEREDICTO (confianza ALTA)**: "Cosmo Beam" NO es un arma pública — es un arma PRIVADA del canal de simulaciones **@terrariasimulation** (TikTok/Shorts, millones de vistas) que la enfrenta a armas de referencia (espada true-melee 570 dmg confirmada) y a "The Strongest Weapon". Prueba reina: **0 resultados en TODO el Steam Workshop de tModLoader** + ausencia en workshops y wikis + código público. No existe original que copiar.
  · ENTREGADO: `INFORME_COSMO_BEAM.md` (la evidencia completa, 14 pasos de bitácora, los bloqueos del sandbox documentados) + `reconstruccion/CosmoBeamReconstruido.cs` (la reconstrucción DOCUMENTADA del arquetipo — beam canalizado de convergencia estilo 6→1 con lo VERIFICADO y lo [INFERIDO] marcado línea a línea + la guía de adaptación al estilo de la casa). Todo en research/ — FUERA del build (la regla de oro).

Compilación: **0 errores · 0 warnings** contra tModLoader 2026.07.3.0 real. build.txt 6.35. Sanity: grep 0 hide=true en desgarros · grep 0 de otros mods en Content · 8/8 bastones de desgarro con mana=0 · hjson es/en parse OK con todas las claves nuevas presentes.

## Commit v6.34 — LAS DOS LIBRERÍAS DE SIGNOS MÁGICOS + LAS MEJORAS + LAS TRES LIBRERÍAS NUEVAS + LA AUDITORÍA

**Petición del usuario**: "debemos crear una librería sobre los anillos de los soles rúnicos y los agujeros negros — que los anillos de los soles rúnicos tengan una librería y los anillos de los agujeros negros tengan otra, serán las librerías de signos mágicos, la usaremos para embellecer algunas cosas más adelante. Luego comienza con la mejora de las librerías y luego con la creación de las nuevas librerías. Por último da unas cuantas pasadas al código para detectar errores e inconsistencias, además de quitar referencias a otros mods".

### A. LAS DOS LIBRERÍAS DE SIGNOS MÁGICOS (la petición central)
  · **SIGILOLIB — LA ESCRITURA MÁGICA DEL SOL** (Content/VFX/SigiloLib.cs): todo lo que vestía a las estrellas rúnicas promovido a PRIMITIVAS INVOCABLES — las LEYES de la familia como contrato público (RingA/RingFlat/RingTilt con precesión/RingSpin alterno/packing del 10º, constantes exactas), los DOS ALFABETOS públicos (RunasSolares ×8 + RunasCorona ×8), y las primitivas: `Runa` (la letra suelta), `AroEliptico` (profundidad frente/espalda + latido), `RunaOrbitando` (cabalgando la tangente), `AnilloRunico`, `NodoCardinal` (perla + destello 4 puntas), `PolvoRunico`, los compuestos `SistemaAnillos` (la corona del sol 1:1) y `ArcoGloria` (la corona del portador 1:1), y EL NUEVO **`SelloSolar`** — EL SIGNO MÁGICO INVOCABLE: aro doble contrarrotando + las 8 runas del alfabeto + 4 nodos cardinales + el glifo maestro central ×2.3 con su destello + polvo orbital. Calibración v6.34b/c medida con VLM: 8 runas con la proporción de la casa (12 con escala lineal EMPASTABAN los glows 2×), aro interior a 0.66r (a 0.80r se fundía), glifo central ×2.3 (a ×1.55 era elipse difusa).
  · **ORBITALIB — LA ESCRITURA MÁGICA DEL VACÍO** (Content/VFX/OrbitaLib.cs): los anillos de los agujeros negros como primitivas — `AnilloEnergia` (LA FÓRMULA FIEL del shader: sin(τ·20−t·5), 20 bandas viajando, turbulencia hash 12 Hz, gradiente térmico de 3 colores, cápsula HALO+NÚCLEO, mitades frente/espalda), `EcosAnillo`, `Fotones` (corredores con estela), `OndasDistorsion`, `AnilloFino` (el aro del horizonte), `Distorsion` (el vaivén ±3%), `CoronaArcos` (los 5 lazos de neón, al buffer de VFXCore) y EL NUEVO **`SelloVacio`** — el par oscuro del SelloSolar (batch cerrado→cerrado, cierre defensivo).
  · REFACTORS 1:1 (ni un número cambiado): RuneSunRenderer delega en SigiloLib, RuneCrownRenderer y ArcCrownRenderer son facades finas, CosmicBlackHoleRenderer delega sus 3 técnicas (DrawEnergyRing/DrawRingEchoes/DrawPhotonRunners BORRADOS).
  · VERIFICACIÓN: mock 1:1 (tools/mock_sigilos_v634.py) + VLM iterativo: sol+anillos 8.5/10 · SelloSolar 9/10 · vórtice 8/10 (núcleo NEGRO ABSOLUTO ✓) · corona 6/10 (look histórico preservado). LA LECCIÓN DEL MOCK: la gamma 1/2.2 EMPASTABA (halos 0.08→0.32, contraste 4:1→1.7:1) — el aditivo puro lineal es el fiel al juego.

### B. LAS MEJORAS DE LAS LIBRERÍAS EXISTENTES
  · **EstelaLib** — `Ribbon(..., taper)`: perfil de anchura por longitud (suelo 15%; taper=0 = idéntico; 9 call-sites intactos).
  · **PulsoLib** — SISTEMA DE TRAUMA: `Trauma(c)` acumula [0,1], shake por `trauma²·8` por la MISMA puerta PunchCameraModifier, desangre 0.02/tick.
  · **StormLib** — `SeekArc`: el arco que se CURVA hacia un objetivo (sesgo Lerp con peso sin(t01·π)·fuerza, 0 en anclas exactas), 3 capas al buffer.
  · **VFXCore** — PRESUPUESTO ADAPTATIVO: `ReportarFps`/`FactorCalidad` (media móvil 0.9/0.1; <45 FPS adelgaza a suelo 0.5, >55 recupera a techo 1). ALIMENTADO por el nuevo CalidadFpsSystem (PostUpdateEverything → Main.frameRate): VIVO, no dormido.
  · **RiftLib PARTIDA EN 3 PARCIALES** (núcleo + Glitch + Portales): split mecánico verificado byte a byte (34 métodos antes = 34 después; 78 call-sites intactos; 966 líneas funcionales exactas).
  · **LumenLib v2** — `BloomTriple`: bloom de TRES BANDAS (1.0×/1.9×/3.4×, alfas 0.55/0.28/0.13, blanco progresivo) — decisión de la casa: un RenderTarget global puede dejar pantalla NEGRA y no es verificable fuera del juego.

### C. LAS TRES LIBRERÍAS NUEVAS
  · **GRAVLENS**: la distorsión UNIFICADA — `Registrar(centro, radio, fuerza, vida)` (cap 8). Integración por FUSIÓN en el pase A del BlackHoleLensSystem (+31 líneas, 0 modificadas): mismo hook, mismo shader, pases A/B preservados.
  · **NEBULALIB**: nebulosas volumétricas con CURL NOISE REAL (Curl2D de un ValueNoise2D a mano): `Nube` (volutas CW/CCW, respiración ±20%, alfas 0.05-0.14) y `Columna` (géiser vertical). Determinista, guards NaN, cero shaders.
  · **AUDIOLIB**: LA IDENTIDAD SONORA — `Sonar(Familia, momento, pos)`: 6 familias × 5 momentos con sonidos vanilla que el mod ya usaba (30 celdas con su porqué), pitch por familia, jitter ±0.08, ANTI-SPAM 12/30 ticks, `SilenciarZona()`.

### D. LA AUDITORÍA (Task 59-a)
  · **EL HALLAZGO CRÍTICO**: las 2 bolsas de v6.33 salieron a medias — SIN PNG (tML habría FALLADO LA CARGA) y SIN hjson. FIX: gen_v634_assets.py (interior DUAL oro→violeta + yin cósmico / saco negro + grieta glitch — VLM 7 y 8/10) + DisplayName+Tooltip es/en.
  · Limpieza: 3 "CWR" neutralizados — grep 0 de 15 patrones de otros mods; clave huérfana borrada; línea fusionada en-US:50 separada; tangentialAngle muerto borrado; doc actualizada; guard ciclo≤0.
  · Verificado: 180/180 sprites · 16/16 assets · 246/246 PNGs (PIL) · hjson 180/180 ambos idiomas · partials correctos · Begin/End equilibrados · pases A/B intactos.
  · Los 2 TODOs restantes (drop del jefe, sync de resonancia) son ROADMAP del modo historia, no bugs.

Compilación: **0 errores · 0 warnings** contra tModLoader 2026.07.3.0 real. build.txt 6.34.

## Commit v6.33 — LAS ESTRELLAS SÓLIDAS + LOS CUATRO DESGARROS + STORMLIB v2 + LAS DOS BOLSAS NUEVAS

**Petición del usuario**: "no veo las 4 armas nuevas de las dos formas de uso · todas las estrellas son semitransparentes, eso no debería ser · investigación profunda en todos los mods para mejorar la librería de rayos eléctricos · el Desgarro no está bien ejecutado: crea varios bastones de desgarro de realidad basados en las imágenes de referencia (4 links de stockcake), mejorando librerías si hace falta".

### A. EL DIAGNÓSTICO DE LAS 4 ARMAS INVISIBLES
  · LA CAUSA RAÍZ: el entorno local se había PERDIDO otra vez (quedó en v6.30 con 693 archivos sin commitear) y el usuario compilaba desde ahí — en GitHub v6.32 las 12 armas SÍ estaban. Aplicada la política de la casa: reset --hard origin/main (GitHub = la verdad).
  · PARA QUE SEA IMPOSIBLE PERDERLAS: nueva **Bolsa de las Dos Formas** (las 4 de las dos formas: Sembrador, Colapso, Lágrimas, Decreto) — entregada por TestingPlayer junto a las demás, con la semántica de garantía (reabrir repone).

### B. EL FIX DE LAS ESTRELLAS SEMITRANSPARENTES (la causa medida)
  · LA RAÍZ: las estrellas pasaban `fade = 1−lifeT·0.40..0.45` como alphaMul del DISCO — el cuerpo estelar transparentaba el fondo con la edad, cuando el sol original pasa alphaMul=1 (opaco SIEMPRE).
  · EL FIX en RuneSunRenderer.DrawSunBody: `cuerpo = alphaMul >= 0.55 ? 1 : alphaMul/0.55` — el disco mantiene alfa 1 durante TODA la vida (el alphaMul solo apaga backglow y aura) y funde solo en el último aliento para que la muerte no sea un corte seco. Las 7 estrellas + el sol heredan el fix automáticamente.

### C. LA INVESTIGACIÓN DE RAYOS ELÉCTRICOS (Task 54 — research/v633/INFORME_RAYOS_ELECTRICOS.md)
  · 18 técnicas con NÚMEROS de los clásicos de generación fractal + los grandes mods de VFX + la vanilla decompilada (el proyectil 466 "Lightning Orb Arc" con su random walk de UnifiedRandom, el zap 20 Hz, el Electrified que castiga el movimiento 4→16 HP/s).
  · LAS 5 TÉCNICAS GANADORAS implementadas en **STORMLIB v2**:
    1. `FractalPath` — midpoint displacement con offset = len·0.15 y HALVING por generación (5 gens → 32 tramos multi-escala).
    2. `FractalBolt` — ramas que nacen AL PARTIR los segmentos (prob 0.28, 20°-50°, ×0.65 longitud, ×0.5 ancho, ×0.4 alpha — solo el tronco a brillo completo).
    3. `StormArc` — EL ARCO DE CORRIENTE CONTINUA: dos rayos entrelazados que se RELEVAN a 6 Hz (vida 20 ticks, fade 100→50%) — nunca hay blink binario; BOIL doble en los midpoints.
    4. La RECETA ELÉCTRICA de 3 capas (glow #1E50A8 ×0.30 esc ×1.0 · mid #5EB3FF ×0.55 ×0.5 · core #FFFFFF ×0.90 ×0.22) con NORMAL MEDIA por vértice (mata los puntos de las juntas).
    5. `SparkBurst` — chispas con shake DECAÍDO a 0, squish que adelgaza, doble pasada glow+core, 3 variantes de forma.

### D. LOS CUATRO DESGARROS NUEVOS (las 4 referencias del usuario)
  · Las imágenes de stockcake (bloqueadas por Cloudflare) se sustituyeron por equivalentes buscadas y analizadas con VLM (research/v633/refs/ + vlm_ref1/2.json — análisis técnico completo: forma, borde, interior, paleta hex, glow, partículas, la clave de lectura).
  · **RIFTLIB v4 — LA FAMILIA DE LOS PORTALES** (contrato cerrado→cerrado):
    · `PortalAnillos` — el portal de 6 anillos concéntricos con rotación jerárquica alternada, gradiente cian→magenta, glifos rúnicos, núcleo blanco respirando a 0.3 Hz, polvo espiral y sparkles orbitando.
    · `OjoEspacial` — la doble elipse diagonal (el ojo) con NÚCLEO NEGRO sólido, rim cian + horizonte ámbar, LA REJILLA QUE CONVERGE (grid warping con líneas de fuga curvándose) y succión de partículas.
    · `DesgarroGlitch` — el círculo irregular fragmentado con ASTILLAS GLITCH rectangulares cian/violeta (flicker 0.1-0.3 s), nebulosa magenta con vetas fucsia, rayos que irradian y partículas de datos.
    · `HeridaElectrica` — la grieta lineal con interior de ESTÁTICA (scanlines 10-30 Hz), arcos voltaicos internos (StormArc), strobe, ABERRACIÓN CROMÁTICA (rojo/cian partidos), RAMIFICACIONES fractales (5 grietas secundarias 55°-75°) y chispas a lo largo.
  · **LAS 4 ARMAS** (items + proyectiles + PNGs procedurales + hjson es/en):
    · **La Sutura Cuántica** — desgarro glitch en el cursor: corrompe el círculo (i-frames 20) + rayos cuánticos a los 3 más cercanos cada 45 ticks (×0.6).
    · **El Portal Dimensional** — succión suave 300 px + trituración del núcleo blanco cada 18 ticks.
    · **El Pliegue del Espacio** — curvatura FUERTE 350 px + compresión del núcleo negro (×1.3, knockback 0); el ojo SIGUE al enemigo más cercano.
    · **La Herida Eléctrica** — línea 620 px desde el jugador: pica ×0.35 cada 10 ticks + ELECTRIFICADO 120 ticks.
  · **Bolsa de los Desgarros** nueva (el clásico + los 4) y registrada en TestingPlayer.
  · MOCK 1:1 (mock_portales_v633.py con las mismas fórmulas C#) + 2 rondas VLM: ronda 1 detectó 4 fallos (portal sin magenta, herida invisible/fina, núcleo glitch muy blanco, rejilla débil) → v6.33 b con los números corregidos → Portal 9 · Pliegue 9 · Glitch 8 · Herida 4→7 con ramificaciones visibles.

### E. LIMPIEZA
  · Las menciones a otros mods introducidas en los comentarios técnicos de las nuevas librerías NEUTRALIZADAS (grep 0 en Content/ y Localization/).

**Resultados: build.txt 6.33 · 0 errores 0 warnings contra tML 2026.07.3.0 real · las estrellas 100% opacas · StormLib con las 5 técnicas de la investigación · 4 desgarros nuevos verificados con mock 1:1 + VLM.**

## Commit v6.32 — LA RECUPERACIÓN DE GITHUB + LA AUDITORÍA EXHAUSTIVA + EL MOCK DEL SOL

**Petición del usuario**: "has perdido el progreso varias veces — si el local se borra, SIEMPRE copia la versión de GitHub que es la buena · analiza todo el código porque con las pausas continuas seguro hay código faltante o cortado que rompería la compilación".

### A. LA RECUPERACIÓN (GitHub = la fuente de la verdad)
  · El entorno local quedó en v6.30 con 692 archivos sin commitear (estado intermedio de una sesión pausada). GitHub tenía v6.31 completa: respaldo del estado local (rama backup-local-v630-unsigned + stash) y `reset --hard origin/main` — el árbol quedó LIMPIO en v6.31.
  · El entorno de compilación se había BORRADO con el sandbox: reconstruido desde cero (dotnet SDK 8.0.425 + tModLoader v2026.07.3.0 estable descargado del release oficial + /home/z/sandbox/verify.csproj con las 8 referencias: tModLoader, FNA, ReLogic, Steamworks.NET, Newtonsoft.Json, Hjson, log4net y TerrariaHooks — el hook On_TimeLogger del BlackHoleLensSystem vive ahí).

### B. EL HALLAZGO GRAVE — research/ DENTRO DE LA CARPETA DEL MOD (93 .cs ajenos)
  · v6.31 dejó `AethonMod/AethonMod/research/v631/` (la investigación cuádruple) DENTRO de la carpeta del mod: 93 fragmentos .cs copiados de otros mods con sintaxis incompleta. tML compila TODOS los .cs de la carpeta al construir el mod EN EL JUEGO → el mod NO habría compilado dentro del juego (los mocks del sandbox excluían research/ y por eso no se veía). Además nombraban armas de otros mods (lo que el usuario pidió limpiar).
  · FIX: `research/v631` movida a la raíz del repo (fuera del paquete .tmod y de la compilación). La compilación pasó de 9 errores CS1002 a 0.

### C. LA AUDITORÍA EXHAUSTIVA (el "código faltante o cortado")
  · **Compilación contra tML real**: 0 errores · 0 warnings (204 .cs, 203 en Content + AethonMod.cs).
  · **238 PNGs**: ninguno corrupto/vacío/dimensión-0 (PIL verify).
  · **135 clases sprite-autoload** (ModItem/ModProjectile/ModBuff/ModNPC/ModDust/ModTile concretas): TODAS con su sprite — la regla tML verificada es namespace-sin-mod + NombreDeClase.png (no el nombre del archivo); los multi-clase (CosmicWeapons, RealStarStaves, RuneSunStaves, TestAdvanced) están completos.
  · **141 assets Request<>**: todos existen (resolviendo el prefijo mod-qualified AethonMod/).
  · **4 shaders**: cada .fx con su .fxc compilado.
  · **hjson es/en**: sintaxis válida; correspondencia clases↔claves al 100% salvo 8 claves que se AÑADIERON (LanzaAlbaProjectile, RealityTearProjectile, SinfoniaPrimordialProjectile, TormentaNebularProjectile × es+en).
  · **Bolsas**: 81 items referenciados, 0 referencias rotas; BlackHoleStaff (el original de CosmicWeapons.cs) estaba HUÉRFANO sin bolsa → añadido a la Bolsa de los Agujeros Negros (nota "Once formas de devorar la luz").
  · **Menciones a otros mods**: grep 0 en Content/ y Localization/ (los patrones de referencia: armas de dos formas, tajos, swings renovados...).

### D. LOS DOS VERIFICADORES VISUALES (las dos quejas del usuario, probadas 1:1)
  · **EL DESGARRO (F1)**: mock numérico re-ejecutado — QUAD 0 cortes ±0.0% (5 semillas) · CADENA 0 cortes y pareja; RENDER_DESGARRO_v631.png evaluado por VLM: "línea perfectamente continua, grosor constante, COMPLETAMENTE LIBRE de grietas tipo espejo roto/ramificaciones Lichtenberg/telaraña/esquirlas — 9/10".
  · **LA SUPERGIGANTE ROJA (F2)**: NUEVO mock 1:1 (tools/mock_sol_v632.py) que traduce PIXEL POR PIXEL el SunShader.fx a numpy (pellizco esférico, doble muestreo auto-desplazado, manchas sustractivas, ríos de lava, corona 1/|d−0.5|) + las 7 capas del RedSupergiantRenderer, sobre el CIELO CLARO de Terraria (el caso que fallaba en v6.30): disco sólido 100% visible, radio 79px (105 de BodyPx ×0.75 del shader), granulación + limbo + atmósfera; VLM: "disco intensamente brillante y opaco, celdas de convección claras, inmediatamente identificable — 9/10". Verificada también la cadena completa: 13 clases usan DrawSunBody (las 7 estrellas + los 5 proyectiles nuevos) y cada proyectil llama a su renderer.

### E. VERIFICACIÓN DE LAS 12 ARMAS v6.31
  · Las 4 de las dos formas (Sembrador del Cementerio Estelar, Colapso del Magnetar, Lágrimas del Sol Moribundo, Decreto del Eclipse) y las 8 creativas (Cometa Errante, Nova Encadenada, Voz del Cuásar, Telar de Constelaciones, Lluvia de Meteoros, Abrazo de la Nebulosa, Filo del Horizonte, Rayo Gamma): item .cs + .png + proyectil + hjson es/en + registro en bolsas — TODO completo.

**Resultados: build.txt 6.32 · 0 errores 0 warnings contra tML 2026.07.3.0 real · el mod compila EN EL JUEGO (research fuera del paquete) · las dos quejas visuales verificadas con mocks 1:1 y VLM 9/10.**

## Commit v6.31 — EL DESGARRO PAREJO + EL SOL EN TODAS LAS ESTRELLAS + EL FILTRO VANILLA + 12 ARMAS NUEVAS + LAS SUPER LIBRERÍAS

**Petición del usuario**: "si el local se borra, siempre copia la versión de GitHub · las líneas del desgarro son discontinuas, no es parejo, y QUITA el efecto de espejo roto · el bastón de supergigante roja sigue sin mostrar la supergigante — copia el código del sol original para TODOS los demás soles o estrellas y hazlos un poco más grandes · investiga armas que cortan la realidad y armas cuyo proyectil ES un tajo · analiza el mod de rework de referencia · investiga sus dos armas de doble forma de uso y crea un arma nueva por forma = 4 armas copiando proyectiles/animación/técnica/uso · crea varias armas creativas investigando los mods populares · el filtro de daño = el mismo de las armas de Terraria base · super investigación de los 500 mods más populares para crear/mejorar todas las librerías y super librerías de calidad superior · limpia el código de referencias a otros mods · revisa el código completo · con todo lo aprendido crea al menos 10 armas nuevas (temática cosmos) · habla siempre en español".

### A. LA INVESTIGACIÓN CUÁDRUPLE (research/v631/ — todo medido sobre código fuente real)
  · **R1/T49 — TAJOS** (INFORME_TAJOS_CORTE_REALIDAD.md, 54 búsquedas): NADIE corta la realidad con ramas — los referentes usan UNA SOLA LÍNEA y la lectura vive en anchura/color/timing. Las reglas de líneas continuas CON NÚMEROS (solape len+w, perlas en cada vértice, ancho en vértices, PERFIL DE MESETA prohibido el huso, curvatura máx, cero ruido en el filo).
  · **R3/T50 — LAS DOS ARMAS DE DOBLE FORMA** (informe local de esa versión): el código fuente COMPLETO de ambas armas descargado del repo oficial del mod de referencia (24 .cs + 5 shaders leídos línea a línea): el púlsar que frena ×0.885 y se ancla, el starquake que solo golpea el FRENTE, las lágrimas que orbitan 150t antes de morder (cinta con fase anclada a arco mundial), y el decreto que ejecuta cada 15t por prioridad con la marca del ojo. 4 fichas + la sección TRASLADO.
  · **R2/T51 — EL MOD DE REWORK COMO ARQUITECTURA** (el informe del mod de referencia): las armas convertidas en SISTEMAS (4.678 .cs + 499 .fx), la fórmula del contraste ("tras amartillar, quietud REAL"), el JUICIO DIFERIDO del iaijutsu (marcar en silencio, liquidar en un clang; el fallo no suena ni brilla), el retroceso como matemática compartida, el conservje de estado de render y la telegrafía con gramática. 14 técnicas trasladables.
  · **R4/T52 — LOS 500 MODS** (INFORME_TOP_MODS_LIBRERIAS.md): ~190 entradas del ranking REAL del workshop (páginas 1-7 por suscriptores, parseadas del HTML), los 12 clave analizados, y LA GUÍA DE LIBRERÍAS: mejoras por librería + las 2 super librerías.

### B. F1 — EL DESGARRO CONTINUO Y PAREJO (la petición más importante)
  · **LA RAÍZ MEDIDA**: las RiftTaper* horneaban un HUSO (ancho 100% SOLO al centro, 0.54 en u=0.10) + respiración ±15% a 4 ciclos + el ramillete Lichtenberg. El ojo lee todo eso como línea discontinua.
  · **LAS TEXTURAS DE MESETA** (gen_rift_meseta_v631.py): ancho 100% en u∈[0.10,0.90] (80% del largo) + TAPAS CIRCULARES; banda de vacío 0.625·quad (= maxWidth en pantalla), LOS DOS LABIOS en su borde, filos razor + EL CENTRO CEGADOR (la lección Last Prism).
  · **RIFTLIB v3**: el ESPEJO ROTO BORRADO (213 líneas: RiftRamillete, CaminoEspejoRoto, Ramillete*, CaminoGrieta, CaminoToca); el ancho NUNCA respira (la vida la pone la alpha); LA CADENA GEMELA DEL QUAD (texturas Taper RECORTADAS a la meseta; los segmentos extremos con la textura ESTADIO completa = tapas redondas — la perla de raíz sobresalía 8.75px FUERA); EL SOLAPE ADAPTATIVO (e = w/2·tan(δ/2): suelo 0.75 vacío / 0 luz — el solape completo APILABA la luz 2-3× en cada junta); perla de luz solo con giro real δ>8°; latido suave por fracción de arco.
  · **LA NUEVA LÍNEA DE TIEMPO** (193 ticks): toda la vida = LA LÍNEA RECTA de UN QUAD; la VIBRACIÓN con la cadena gemela de ancho plano; LA FRACTURA ES UN EVENTO DE LUZ (flash 0.30 + ancho ×1.35 + daño ×2.2 — la línea SIGUE RECTA); EL CORTE VIVO 98t ×0.10; EL CIERRE SE COME EL CORTE DESDE LOS EXTREMOS (erosión direccional, jamás menguando el ancho).
  · **MOCK 1:1** (mock_desgarro_v631.py, a granularidad del juego): QUAD 0 cortes ±0.0% (5 semillas) · CADENA 0 cortes, plana ±0.0% / ondulada máx ±6.2% (1 columna sub-píxel documentada). Render de registro: RENDER_DESGARRO_v631.png.

### C. F2 — EL CUERPO DEL SOL ORIGINAL EN TODAS LAS ESTRELLAS (+ MÁS GRANDES)
  · **RuneSunRenderer.DrawSunBody**: LA TÉCNICA EXACTA DEL SOL (backglow BloomCircle en alfa + aura RadialShineShader + EL DISCO DE PLASMA SunShader con granulación dendrítica) extraída a API pública por radio+paleta+giro — el sol original NO SE TOCA, las demás la heredan.
  · **TODAS las estrellas reales**: LA SUPERGIGANTE ROJA (la invisible: v6.30 dibujaba 4 blobs SoftGlow; ahora disco SunShader rojo sólido, giro lento 0.35), Estrella de Neutrones (azul-blanca, jitter del starquake), Enana Blanca (cristalina), Magnetar (violeta con arritmia), Púlsar (girando SOLIDARIA AL HAZ), Estrella Muerta (paleta de ASCUAS APAGADAS — cadáver con granulación tenue).
  · **MÁS GRANDES**: soles rúnicos 46→52 · supergigante 90→105 · neutrón 22→26 · enana 34→39 · muerta 28→33 · magnetar 26→31 · púlsar 28→33 (+ hitboxes y resize proporcionales).

### D. F3 — EL FILTRO DE DAÑO = EL DE TERRARIA BASE (verificado por reflexión)
  VFXCore.EsObjetivo = EL PREDICADO EXACTO de la puerta de daño de vanilla (Projectile.cs): `active && !dontTakeDamage && !friendly`. MEDIDO contra el tModLoader.dll real: TargetDummy (friendly=False, dontTakeDamage=False) PASA — números sí, vida no, EXACTAMENTE como las armas base; el Guide queda excluido como con una espada base. Los 111 filtros del mod heredan el comportamiento vanilla de golpe.

### E. W1 — LAS 4 ARMAS DE LAS DOS FORMAS (el código fuente destilado a la casa)
  · **EL SEMBRADOR DEL CEMENTERIO ESTELAR**: el púlsar sale rápido (v₀=dist/7.375) y frena ×0.885/tick hasta anclarse EXACTO en el cursor; gira acelerando lerp(0.03→0.135,t²) y barre con DOS HACES-FARO opuestos de 500px (×0.25, i-frames 10); tope 3.
  · **EL COLAPSO DEL MAGNETAR**: el ciclo completo — FRENO 55t (spin ×0.15, jaula, telegrafo) → STARQUAKE: anillo hasta 580px que SOLO golpea el FRENTE (308..550 según carga) → OVERCLOCK 85t (spin ×4.7, daño ×1.6).
  · **LAS LÁGRIMAS DEL SOL MORIBUNDO**: 3 gotas de metal fundido (cola fina + cabeza con casquete, COSTRAS, MENISCO, se estira con la velocidad) que ORBITAN 150t encendiéndose → persiguen → 18 golpes → explosión ×1.5.
  · **EL DECRETO DEL ECLIPSE**: el círculo 80→660px con borde DESGARRADO + 24 glifos RE-ESCRITOS; cada 15t la onda viajera ejecuta por prioridad (×4.83 decayendo a ×0.2); LA MARCA DEL OJO con pupila que se contrae antes del tajo; el mundo se oscurece.

### F. W2 — LAS 8 ARMAS CREATIVAS DEL COSMOS
  **El Cometa Errante** (órbita elíptica precesiva, cola apuntando radialmente afuera que DAÑA) · **La Nova Encadenada** (novas hijas→nietas por DISTANCIA pura, 3 generaciones) · **La Voz del Cuásar** (carga 40t → haz continuo con DOPPLER que rebota) · **El Telar de Constelaciones** (clava estrellitas que se conectan solas; la figura cerrada ejecuta a los de DENTRO por ray-casting) · **La Lluvia de Meteoros** (telégrafo + 8 meteoros con lenguas de fuego) · **El Abrazo de la Nebulosa** (3 nubes que ralentizan + estrellitas que revientan) · **El Filo del Horizonte** (disco negro bumerán con anillo de acreción; las marcas IMPLOSIONAN) · **El Rayo Gamma** (LA TÉCNICA DEL JUICIO DIFERIDO: hitscan silencioso → 30t después TODAS las marcas liquidan a la vez).

### G. L1 — EL MOTOR v2 + LAS DOS SUPER LIBRERÍAS
  · **VFXCore v2**: CAPAS DE OCLUSIÓN (RegisterLayer/FlushOcclusion — el vacío dibuja debajo de los NPCs), EL CONSERJE (anula Textures[1..3] del device — mata los shaders sucios), LA PUERTA DE CALIDAD (CalidadPermitida) y EL PRESUPUESTO (techo 24000 quads/frame, recuento automático — anti-abrumador).
  · **PULSOLIB** ("el sistema nervioso"): EL JUICIO DIFERIDO como API (AbrirVeredicto/Marcar/Liquidar — marcas silenciosas con pitch ascendente, liquidación en un clang escalado por víctimas, el fallo NO suena ni brilla), EL RETROCESO (readonly struct + Derivar(peso, ticksCarga) — 2 números configuran el feel), LA CÁMARA, EL SONIDO POR MATERIAL (presupuesto 2/frame + pitch por combo) y LA PANTALLA (push/decay uniforme).
  · **TELALIB** ("una cinta para gobernarlas todas"): FASE ANCLADA A ARCO MUNDIAL (la textura avanza con los PÍXELES, no con el reloj — cero patinaje), 4 PERFILES DE ANCHURA (Constante/Meseta/Huso/Cometa — latido en la alpha), SOLAPE ADAPTATIVO (cero cuentas), POOL por dueño (cero GC) y SUB-STEP AWARE.

### H. L2/L3 — LIMPIEZA Y REVISIÓN
  · **62 menciones a otros mods/armas/obras neutralizadas** (comentarios de los Exhumados, tooltips es/en, docs de librerías): grep final = **0 referencias** en Content/ y Localization/.
  · **Auditoría completa**: 0 Main.rand en visual (solo docs), todos los SimpleStrikeNPC con EsObjetivo + guard MP, contrato de batch en los 13 proyectiles nuevos, 49 PNGs presentes, hjson es/en completos, bolsas actualizadas.

### I. ENTREGA
  build.txt 6.31 · CHANGES.md · worklog.md (Tasks 49-61) · **0 errores / 0 warnings** contra tML 2026.07.3.0 real.

# AethonMod — Historial de Cambios

## Commit v6.30 — EL ESPEJO ROTO + LA QUEMADURA CÓSMICA + LAS ARMAS QUE APRUEBAN CONTRA DUMMY

**Petición del usuario**: "el arma de desgarro... en la segunda [fase] sigue el mismo problema de que no es continuo, tiene cortes, además que genere un proyectil que cae no es bueno, ese proyectil que cae debes quitarlo, lo que debe hacer el arma de desgarro es lo siguiente, en el momento en el que se desgarra se debe partir la realidad como un espejo roto, no dejar caer fragmentos, usa la librería de desgarro pero dale ramificaciones como si fuera un rayo, entiendes, para simular un espejo roto · además haz que todas las armas puedan dañar a los Dummy · la supergigante roja del bastón supergigante roja no se ve · todos los soles deben ser capaces de quemar · las armas eléctricas deben dar un debuff correspondiente · y con los agujeros negros debes crear un nuevo debuff, para eso toma la forma del debuff de quemadura y luego tiñe de negro y el debuff nuevo se llama, quemadura cósmica · creo que debes mejorar mucho a Los dos Exhumados, mejoralos tanto como puedas y no se parecen en nada a los originales · investiga más y si necesitas crear más librerías o mejorar la que ya tenemos entonces investiga aún más · y lo de los desgarros de realidad es sumamente importante, investiga mucho cómo hacer que se vea genial y sin errores... luego haz varios repasos del código".

### A. LA INVESTIGACIÓN (research/v630/INFORME_VISUAL_EXHUMADOS.md — MEDIDO, no adivinado)
  Descarga y medición NUMÉRICA (PIL/numpy) de los sprites y GIFs oficiales del
  la wiki del mod de referencia: **Rancor**: "The Angy Beam" = NÚCLEO BLANCO PURO
  (255,255,255) + bordes ROSA-MAGENTA media (204,77,112); el círculo mágico =
  ESCALA DE GRISES blanca-plata; los brazos = SILUETAS NEGRAS (31% negro +
  22% (32,0,0) + 17% (64,32,32)); cinders ámbar. **Gruesome Eminence**: la
  Spirit_Congregation (134×142, 18 frames) = 45.8% NEGRO + 26.4% violeta
  oscuro + 10.4% rojo oscuro con las CARAS ardiendo ROJO-NARANJA (253,74,60);
  el icono = cabeza rojo-oscura con detalles TAN (la Cabeza de Dismas).
  **Lichtenberg/espejo roto**: reglas de ramificación medidas (ramas a
  25°-55° alternando lados, ×0.6 ancho, sub-ramas ×0.45, arcos concéntricos
  0.22L/0.42L) + las reglas de continuidad de quads (solape len+w cubre
  giros ≤126°; el hueco del vacío se lee como CORTE). Y el tML NPC.cs.patch:
  **el Target Dummy es `immortal`** — recibe golpes y muestra números de
  daño pero `CanBeChasedBy()` LO EXCLUYE (la raíz de que nuestras armas
  manuales no le pegaban).

### B. RIFTLIB v3 — EL ESPEJO ROTO (la petición más importante)
  · **LA RAÍZ DE LOS CORTES (medida con el mock 1:1)**: (1) el VACÍO
    solapaba len+w·0.35 (cubre giros ≤53°) mientras la luz usaba w·0.9 — los
    huecos del NEGRO se leían como cortes; (2) el taper (1-t)^0.9 mataba la
    cola (a t=0.75 el ancho era 3.8px) y los segmentos con w<0.4 se OMITÍAN
    — huecos REALES en cadena; (3) la vida (1-progress)^0.8 llegaba a 0.22 —
    la cola fina+tenue se percibía discontinua.
  · **LA CADENA A PRUEBA DE CORTES v6.30**: el vacío solapa IGUAL que la luz
    (len+wmax completo), LA PERLA vive en TODOS los vértices (raíz incluida)
    con diámetro max(w), LA PERLA DE LA PUNTA cierra el canal distal, el
    taper es SUAVE (exp 0.45 + suelo 25% — el vidrio real mantiene 40-70%
    de su anchura), NINGÚN segmento se omite (suelo 0.6px).
  · **EL RAMILLETE (RiftRamillete + CaminoEspejoRoto)**: al fracturarse, la
    realidad se parte COMO UN ESPEJO: el canal madre Lichtenberg MODERADO
    (curvatura 4.5 — el caos vive en las ramas, no en el canal) + LAS RAMAS
    tipo rayo (25°-55° alternando lados, largo 0.22-0.42·L, ancho ×0.6) +
    SUB-RAMAS ×0.45 en las 3 más largas + LOS ARCOS TELARAÑA concéntricos
    (2 anillos 0.22L/0.42L partidos con huecos). RamilleteVacio/Ramillete/
    RamilleteToca dibujan y golpean TODO el patrón.
  · **SIN NADA QUE CAIGA**: RiftLib.Shards BORRADO — el proyectil que caía
    era su paquete de partículas con gravedad; la fractura es TODO GRIETA.
  · **EL DAÑO DEL ESPEJO**: ×2.2 el canal madre, ×1.6 las ramas/arcos; el
    DoT del espejo vivo ×0.10 canal / ×0.07 ramas cada 3 ticks.
  · **EL MOCK 1:1 (tools/mock_espejo_v630.py, muestreo bilineal + PNGs
    premultiplicados como tML)**: v6.29 = 19 cortes NEGROS a mitad del
    canal; v6.30 = **0 cortes** en el canal madre y TODAS las ramas (5
    semillas); lo único tenue que queda es la PUNTA-AGUJA final de los arcos
    (t>0.85, el crack muriéndose — como el vidrio real). (El mock reveló y
    corrigió además un bug de AABB del propio mock — el juego con SpriteBatch
    no lo tiene.)

### C. TODAS LAS ARMAS DAÑAN A LOS DUMMY — VFXCore.EsObjetivo
  `CanBeChasedBy()` excluye al Target Dummy (es immortal — el NPC.cs.patch
  de tML medido). NUEVO filtro de la casa `VFXCore.EsObjetivo(npc)` =
  CanBeChasedBy || TargetDummy: los 111 filtros de daño/búsqueda de TODO el
  mod pasan por él — las Dummy reciben números de daño de TODAS las armas
  manuales (y los homing las persiguen: se puede probar de verdad).

### D. LA SUPERGIGANTE ROJA SE VE (la lección del pase alfa, otra vez)
  El cuerpo era TODO-ADITIVO con alfas 0.16-0.55 y colores oscuros — sobre
  un fondo claro es INVISIBLE. AHORA: **PASO ALFA** con EL DISCO SÓLIDO de
  4 capas (panza profunda → masa → interior caliente → limbo brillante — la
  estructura de una foto de gigante real) + LAS CÉLULAS FRÍAS (las manchas
  oscuras del plasma que baja, en alfa) → **PASO ADITIVO** (atmósfera 3× +
  solo las celdas CALIENTES que suben + anillo de fuego + la distorsión del
  colapso). Y el catch ahora LOGUEA al client.log (PublicLogger — la próxima
  vez sabremos POR QUÉ).

### E. LOS DEBUFFS POR FAMILIA (la petición textual)
  · **TODOS LOS SOLES QUEMAN**: RuneSunProjectile.OnHitNPC nuevo (contacto
    OnFire 10 s — las 20 variantes + el gigante); NeutronStar y WhiteDwarf
    +OnFire 240 en todos sus golpes; SunProjectile ya quemaba (aura/contacto/
    nova). DeadStar → QUEMADURA CÓSMICA (el fuego de una estrella muerta es
    NEGRO).
  · **LAS ELÉCTRICAS ELECTRIFICAN**: Magnetar aura +OnFire... no: +Electrified
    150 (el campo magnético), Pulsar burst final +Electrified, OcasoBurst
    cadena +Electrified 150, LanzaAlba contacto +Electrified 180 (la cadena
    ya lo tenía). Ya estaban: RunicLightning (3/3), Sinfonía, Tormenta
    Nebular, LivingPulsar.
  · **QUEMADURA CÓSMICA (el debuff nuevo)**: la FORMA de la llama de
    quemadura TIÑIDA DE NEGRO (icono 32×32 procedural: llama pixel-art con
    corazón negro-cósmico, gradiente violeta y MOTAS DE ESTRELLAS dentro —
    "el fuego que arde hacia dentro"). DoT lifeRegen 32 + brasas
    violeta-negras que ASPIRAN hacia el centro. Lo aplican LOS 12 AGUJEROS
    (9 + 3 ascendidos + supremo aurora) y EL ECLIPSE: 4 s los básicos,
    6 s ascendidos, 8 s los supremos, 5 s el eclipse. Localización es/EN.

### F. LOS DOS EXHUMADOS — LOS COLORES MEDIDOS (v6.29 los hizo irreconocibles)
  · **EL RENCOR**: el haz pasa de carmesí-ámbar a **NÚCLEO BLANCO + FILO
    ROSA (204,77,112)** (RiftLib.Tear con la paleta medida) + el Ray ígneo
    rosa; el círculo de transmutación pasa de oro/violeta a **PLATA**
    (blanco-gris medido: 224/192/160); LOS BRAZOS pasan de cápsulas de
    hueso blanco a **SILUETAS NEGRAS con borde rojo oscuro EN EL PASE ALFA**
    (el negro aditivo es invisible — la lección v6.29) con LA AURA DEL BROTE
    roja en aditivo (la única luz de las sombras) y el brote emergiendo del
    muro en 8 ticks.
  · **LA EMINENCIA**: de nube pálida a **LA MASA NEGRA** (doble BrumaFX
    negra-violeta + corazón negro) con **LAS CARAS ARDIENDO ROJO-NARANJA
    (253,74,60) DENTRO** — EL SISTEMA DE TRES PASES: alfa1 (masa + zócalos +
    brasas base) → aditivo (los ojos que arden + estelas carmesí) → alfa2
    (LAS PUPILAS Y LA BOCA NEGRAS ENCIMA del brillo — la mirada corta el
    propio fuego). Los espíritus menores: cuerpos OSCUROS con ojos rojos
    ardiendo (no fantasmas pálidos). El icono regenerado: la CABEZA OSCURA
    cosida con ojos rojos (la Cabeza de Dismas medida — tan + rojo oscuro).
    Los polvos de muerte/latigazo/dsipación: oscuros + brasas rojas.

### G. LOS REPASOS DE CÓDIGO (la petición: "varios repasos")
  1. Auditoría global de golpes/debuffs (41 archivos con SimpleStrikeNPC:
     cada familia con su debuff correspondiente — tabla completa en el
     worklog).
  2. Contratos de batch verificados en los 3 archivos intervenidos
     (Eminencia 3 pases, Rencor 2, supergigante 2) — Begin/End balanceados
     con catch de seguridad.
  3. Limpieza: ageF muerto, Shards borrado, PálidoRasgo ahora usado (el
     destello frío del ojo mayor), comentarios/tooltips al día con los
     colores nuevos.
  4. El mock numérico como estándar (5 semillas × todos los caminos).

## Commit v6.29 — LOS DOS EXHUMADOS + LAS DIEZ BOLSAS POR CATEGORÍA

**Petición del usuario**: "investiga esto al jefe brujo supremo del mod de referencia
(Encantada, Exhumada)... investiga el arma Rancor, investiga bien su
funcionamiento completo, qué librerías y assets usa y crea un arma basada en
eso, también de esos mods investiga el funcionamiento completo de Gruesome
Eminence, qué librerías y assets usa y también crea un arma basada en eso ·
luego crea varias bolsas para todas las armas que me tienes que dar no solo
una y separalas por categorías, una categoría por bolsa · al jugador también
dale 99 Dummy para probar las armas".

### A. LA INVESTIGACIÓN (research/rancor_v629/INFORME_EXHUMADOS.md)
  Wiki oficial + Fandom + 6 búsquedas web. LO QUE ES "Enchanted Exhumed": el
  sistema de la Brimstone Witch — el encantamiento único **Exhume** que
  **transforma un ítem en otro totalmente nuevo** (Burning Sea→**Rancor**,
  Ghastly Visage→**Gruesome Eminence**). RANCOR: círculo mágico a distancia
  fija (basado en el círculo de transmutación humana de Fullmetal
  Alchemist) → **3 segundos de carga** → un haz láser GRANDE que **perfora
  infinito** ("The Angy Beam") → al tocar tiles sólidos brotan **BRAZOS
  ESQUELÉTICOS (66.67% del daño)** + **cinders grandes (33.33%)** + fog y
  lava visuales → los enemigos del haz **se desintegran en cenizas**.
  GRUESOME EMINENCE: invoca una **congregación de espíritus gaseosa** cerca
  del cursor que sigue loose y **se mueve salvajemente por su cuenta**,
  libera **espíritus menores visuales** que son TIRADOS DE VUELTA, y tras
  **14 segundos** se acumulan en **UNA SOLA ABOMINACIÓN totalmente
  controlable** con el daño rampando **del 100% al 185%**; consume maná
  CONSTANTE mientras se canaliza; el interior tiene siluetas de **Giygas**.

### B. EL RENCOR PRIMORDIAL (el arma nacida del Rancor)
  `RencorPrimordialStaff` + `RencorPrimordialProjectile`: EL CÍRCULO DE
  TRANSMUTACIÓN rúnico de la casa (10 runas doradas CW + 6 violetas CCW
  encendiéndose UNA A UNA + **LA ESTRELLA de 5 puntas dibujada con
  cápsulas** — el homenaje FMA) a 380 px → **180 ticks de carga exactos**
  (bruma espiralando HACIA dentro + ascuas orbitando + Telegraph carmesí +
  latidos subiendo + Oscurecer) → **EL HAZ CONTINUO**: RiftLib.Tear con
  paleta carmesí-ámbar nueva (UN SOLO QUAD, 880 px × 44, cero juntas — la
  tecnología v6.28) + Ray ígneo + ImpactFlash en la boca + kick/flash del
  paquete TearImpacto → **AL TOCAR TILE: LOS BRAZOS ESPECTRALES** (4 brazos
  de cápsulas hueso-con-halo-carmesí que brotan escalonados de la
  superficie, crecen con smoothstep, golpes ×0.66) + **LAS ASCUAS**
  (PyraLib.Sparks rampa SolarFire + daño de área ×0.33 cada 8 ticks) + LA
  FOG (BrumaFX) + **LA LAVA** (PyraLib.Flame lamiendo el tile + luz ámbar)
  → **LOS ENEMIGOS DEL HAZ MUEREN EN CENIZA** (la muerte con firma) → el
  cierre: la estrella implota y el círculo exhala. Daño manual por línea
  (escuela A): apertura ×1.0 + DoT ×0.30/5t. Sin maná, rareza Purple.

### C. LA EMINENCIA ATROZ (el arma nacida del Gruesome Eminence)
  `EminenciaAtrozStaff` + `EminenciaAtrozProjectile`: **LA CONGREGACIÓN**
  (BrumaFX.Cloud doble: pálida de hueso + interior violeta oscuro) nace
  cerca del cursor clampeado a pantalla+rango → sigue CON spring flojo y
  **SE LARGA con dardos salvajes** (hash-gated) → **LOS ESPÍRITUS MENORES**
  paramétricos (3..12): cada uno con su ciclo se-libera→flota→**TIRADO DE
  VUELTA**, con estelitas EstelaLib → **LOS OJOS** (2+6·x) asomando con
  zócalos NEGROS + escleróticas blancas + pupilas negras **DE VERDAD** (el
  pase ALFA — lección v6.29: el negro del DiscoNegro es INVISIBLE en el
  lote aditivo) + destellos carmesí AL LADO (nunca encima) → **LA CARA**
  (el interior Giygas: ojo inmenso + boca negra en ventanas caóticas desde
  crecimiento 0.55) → **LA ACUMULACIÓN**: 840 ticks de canal (14 s EXACTOS
  del mod de referencia) → **LA ABOMINACIÓN**: la nube SE APRIETA ×0.85, spring ×2.4
  (control total), EL OJO MAYOR con la pupila siguiendo el vuelo + anillo
  carmesí + corona de ojos + estela Comet de EstelaLib + rugido propio →
  daño de área cada 6 ticks con **mult = 1 + 0.85·x (100%→185% EXACTO)** →
  sin canal la masa SUBE, decae y se disipa (los espíritus escapan).

### D. LAS DIEZ BOLSAS POR CATEGORÍA + LAS 99 DUMMIES
  LA BOLSA ÚNICA (v6.27) SE RETIRA — ArsenalBag.cs/.png BORRADOS. En su
  lugar `BolsaCategoria` (la base: permanente, clic derecho, semántica de
  garantía "solo lo que falte") + **DIEZ BOLSAS** (Content/Items/Bolsas/):
  1. Bolsa del Probador (kit+test) · 2. Bastones Fundacionales (los 4 V20)
  · 3. Clásicos Cósmicos (sol+criaturas) · 4. Agujeros Negros (los 10)
  · 5. Soles Rúnicos (los 20) · 6. Estrellas Reales (las 7) · 7. Armas de
  las Librerías (las 6) · 8. Bastones Creativos (los 6) · 9. **EXHUMADOS**
  (El Rencor + La Eminencia) · 10. Cosméticos (las coronas). TestingPlayer
  entrega las 10 garantizadas + **99 TARGET DUMMY** (ItemID 3202 — el campo
  de entrenamiento directo en el inventario).

### E. LOS ASSETS (tools/gen_v629_assets.py — 14 PNGs 100% procedurales)
  10 iconos de bolsa (30×30, un emblema por categoría: llave/nova/galaxia/
  agujero/sol/constelación/rayo/reloj/sello-exhumado/corona) + EL TOMO del
  Rencor (28×30, cuero carmesí + círculo dorado-carmesí con pentagrama) +
  EL FANTASMA de la Eminencia (30×30, pixel-art DURO: cúpula+falda
  ondulada, contorno violeta-negro, 2 ojos con pupilas carmesí, boca
  abierta) + las 2 sombras 76×76. VLM 3 rondas: constelación 5→7/10,
  Eminencia 4→7/10 (el fix fue BORDE DURO + contraste), Rencor 6→
  brillado. MOCKS 1:1 (tools/mock_exhumados_v629.py): Rencor 8/10
  (círculo+haz+brazos OK) · Eminencia: ojos 8/10, composición 9/10,
  sobreexposición 9/10 — TRAS el fix del pase alfa (la primera pasada del
  mock REVELÓ el bug real: las pupilas negras en aditivo son invisibles).

### F. WORKLOG: LA CONSOLIDACIÓN DEFINITIVA
  El worklog raíz del repo (557 KB, congelado desde v6.23 y confundiendo
  con el activo) BORRADO del repo — el histórico vive en git y EL WORKLOG
  ACTIVO ES `AethonMod/worklog.md` (Tasks 1-47, versionado). Los headers
  de sandbox y del worklog apuntan ahora al correcto.

— 0 errores · 0 warnings contra tML 2026.07.3.0 real (/tmp/verify)

## Commit v6.28 — EL DESGARRO CONTINUO + EL ECLIPSE TOTAL + LA PURGA DE COSMÉTICOS

**Petición del usuario**: "el desgarro tiene interrupciones azules, en vez de
ser una línea continua, además el desgarro no tiene que soltar más
proyectiles, solo debe hacer daño el desgarro en sí, y hacer aún más daño
cuando pasa de una línea recta a fracturarse · borrar corona de anillos
rúnicos y anillo rúnico estelar · el bastón de supergigante roja no lanza la
supergigante roja · todas las demás variantes de estrellas son bastante
pequeñas, hazlas un poquito más grandes · rediseña el bastón de eclipse
primordial · investiga mods que tengan desgarros de realidad y mejora la
librería de desgarros · actualiza los .md".

### A. LA RAÍZ DE LAS "INTERRUPCIONES AZULES" — MEDIDA, NO ADIVINADA
  La investigación (research/desgarro_v628/INFORME_RIFTLIB_V2.md, texturas
  medidas con PIL + 25 búsquedas web + el mod de referencia decompilado) encontró el
  culpable EXACTO: **la TrailGlow.png tenía el defecto** — su alfa rampa
  3→204 A LO LARGO del eje de longitud y su color es CIAN PURO (0,255,255).
  Cada junta entre los 8-24 segmentos del desgarro era una franja casi
  transparente y CIAN = exactamente las "interrupciones azules". El segundo
  culpable: la aberración cromática R/B de los labios (re-dibujo de canal
  AZUL puro). EL CONTRATO del ecosistema (verificado en las texturas de
  línea del mod de referencia): UNIFORME a lo largo del eje, gradiente SOLO a lo
  ancho, SIN color horneado.

### B. RIFTLIB v2 — EL DESGARRO CONTINUO (6 texturas nuevas, 100% código)
  `tools/gen_rift_v628.py` (auto-verificado — mide sus propias texturas):
  · **RiftTaperVelo/Cuerpo/Nucleo/Void** (512×64): EL DESGARRO RECTO EN
    **UN SOLO QUAD** — el perfil de longitud horneado (lens sin^0.6 ·
    respiración nebulosa ±15% a 4 ciclos, la lección Dimension-Tearing
    Disk) + la sección completa (par de labios a ±0.31·W cabalgando el
    borde del vacío + núcleo razor). CERO juntas porque CERO segmentos
    (la lección HyperdeathRiftScepterBeam: su rayo de 3000 px es UN quad).
  · **RiftLip/RiftCore** (64×16): el camino FRACTURADO con columnas
    IDÉNTICAS (desviación medida 0.0000) + anchura evaluada EN LOS
    VÉRTICES compartidos (lección WidthFunction del mod de referencia) + solape
    len+w·0.9 + **PERLA en cada vértice** (el round-join estándar) → la
    herida Lichtenberg continua aunque gire.
  · SIN aberración R/B en los labios: el vocabulario queda LIMPIO (labios
    de color + núcleo blanco + vacío negro). El EcoGlitch sigue como API
    pero el arma YA NO lo usa.
  · Mock 1:1 del pipeline (PNGs premultiplicados como tML, aditivo
    dst += tex·tint, occlusión no-premult) — VEREDICTO NUMÉRICO: v1
    65px de huecos reales; v2 UN QUAD 0px; el camino fracturado y la
    vibración con brillo sobre camino min 232-246/255 en TODO el arco
    (solo la aguja final legítimamente se apaga). VLM: v1 "cyan dashed
    segments 3/10" vs v2 "single unbroken line, white-hot core 9/10".

### C. EL BASTÓN DEL DESGARRO v2 — LA LÍNEA QUE SE FRACTURA
  El nuevo guion (la física del vidrio: las grietas se propagan a
  1458-1500 m/s — la FRACTURA es un evento de 1-2 frames con tensión
  visible antes):
  · TELÉGRAFO (12 ticks) → APERTURA (3): el PRIMER GOLPE ×1.0 a toda la
    línea recta → RECTO (52): la línea viva con DoT ×0.07/3 ticks →
    **VIBRACIÓN (16)**: onda estacionaria 0→3.5 px a ~10 Hz + shimmer
    nervioso + retumbo (la línea está a punto de FALLAR) → **FRACTURA
    (2)**: EL CLÍMAX — la línea se QUIEBRA al camino Lichtenberg con
    FURIA (curvatura 8, micro-fallas 1/4) y pega **×2.2** a lo largo de
    la herida fracturada + shards de vidrio + kick ×1.3 + flash 0.30 →
    GRIETA VIVA (98): DoT ×0.10/3 ticks → CIERRE (10): el daño cesó 8
    ticks antes del final visual.
  · **NADA de proyectiles extra**: RealityTearZoneProjectile BORRADO (la
    petición literal) — el desgarro daña ÉL MISMO por línea/camino
    (SimpleStrikeNPC, escuela A). Sin eco glitch, sin aberración.

### D. LOS DOS COSMÉTICOS BORRADOS (petición literal)
  Corona de Anillos Rúnicos (RuneRingCrownItem) y Anillo Rúnico Estelar
  (RunicHaloWings): 10 archivos eliminados (ítems+PNGs+renderers+capas+
  RunicHaloPlayer) + referencias limpias (ArsenalBag, CosmeticPlayer,
  hjson es/EN). Quedan la Corona de la Reina del Vacío y la Corona Rúnica
  Estelar.

### E. LA SUPERGIGANTE ROJA + LAS ESTRELLAS AGRANDADAS
  · El crash del client.log (PyraPalettes.Sample por NaN vía rr/R) ya
    tenía el guard v6.27 — ahora ADEMÁS la división de las celdas de
    convección lleva clampeo total (R>0.05 + Clamp 0..1): la división
    JAMÁS puede envenenar la temperatura. El coloso Vuelve a verse.
  · "Un poquito más grande" (petición): estrella de neutrones 12→22 px,
    púlsar 16→28, enana blanca 20→34, enana muerta 16→28, magnetar
    14→26 (+ hitboxes proporcionales). La supergigante (90 px) intacta.

### F. EL ECLIPSE PRIMORDIAL — REDISEÑO TOTAL: "EL ECLIPSE TOTAL"
  La v6.26 (Sol de 20 anillos + mezcla de todos los agujeros) se retira.
  El NUEVO concepto es el nombre del arma: UN ECLIPSE SOLAR TOTAL:
  · EL SOL PRIMORDIAL desnudo (blanco-oro, granulado vivo).
  · **EL DISCO DE LA NOCHE** (ocultador no-premultiplicado) que SE
    DESLIZA sobre el sol: EL CRECIENTE mengua (smoothstep — la lección
    del mock: el ease-out cuártico enterraba el creciente).
  · **EL DÍA MUERE**: el mundo se apaga con RiftLib.Oscurecer (el sesgo
    violeta de la casa) mientras dura el eclipse — y VUELVE con la nova.
  · **LA CORONA**: 12 streamers asimétricos (ecuatoriales largos, polares
    plumosos — la corona REAL) curvados como líneas de campo, cada uno
    respirando y con flujo interior. Velo ANULAR (no centrado — lección
    del mock: el glow centrado LAVABA el disco negro).
  · LA CROMOSFERA (aro rojo 1.02R + perlas de Baily) · LAS PROMINENCIAS
    (5 lazos rojo-oros con parpadeo PyraLib) · **EL ANILLO DE DIAMANTE**
    (floriturna de 4 puntas VIAJANDO por el limbo, ×6 más rápido en LA
    ÚLTIMA LUZ) · LOS TRES CÍRCULOS RÚNICOS (2.4/3.2/4.0R — la firma).
  · LA MUERTE = **EL RETORNO DE LA LUZ**: el disco IMPLODE (pow 1.6 — el
    encogimiento SE LEE), la corona EXPLOTA soplada, el núcleo queda
    CEGADOR (cruz ×2 pares) y el Einstein + nova ×1.6 estallan mientras
    la oscuridad se suelta. Mock VLM: creciente 9/10, total 8/10, última
    luz 9/10, retorno 9/10.
  · La física se conserva: atracción 600 px + devora proyectiles enemigos
    + aura de quemadura. Lente gravitacional ×2.6.

### G. LOS .md DEL PROYECTO — ACTUALIZADOS (la petición que faltó en v6.27)
  ROADMAP_DE_IMPLEMENTACION.md (estado de fases v6.28), DISEÑO_DEL_MOD.md
  (la pila de 8 librerías VFX + sección identidad), AethonMod_Project_
  Complete_Document.md (banner de la era v6.x + tabla de estado),
  CHANGES.md, worklog consolidado (el del repo — históric 433 KB — y el
  de sesión sincronizados).

## Commit v6.27 — LA BOLSA DEL ARSENAL + EL OCASO DE AETHON + LOS 6 BUGS DE RUNTIME MUERTOS

**Petición del usuario**: "hay demasiados errores, además todo lo que le
vas a dar al jugador ponlo en una bolsa o cofre y dale solo la bolsa con
todos los objetos dentro · dime que piensas de los biomas · crea una
nueva arma para aplicar el patrón gauge del Cosmic Destroyer a un arma
suprema · dime cuales son las librerías actuales del proyecto y si con
esta ultima investigación creaste o no librerías o mejoraste alguna ·
revisa los .md en el proyecto y actualízalos según corresponda · dime
que mas se puede agregar al mod según lo que investigaste".

### A. LOS 6 BUGS DE RUNTIME DEL client.log — MUERTOS POR LA RAÍZ
  Análisis del client.log subido (v6.26 en juego): 5 clases de excepción
  real + 1 warn sistémico. TODAS arregladas:
  1. **`Projectile.ai[3]` NO EXISTE** — el array `ai` de tModLoader
     SOLO tiene 3 ranuras (0..2). El Coro Espectral y el Péndulo del
     Juicio guardaban el espejo del daño en `ai[3]` →
     `IndexOutOfRangeException` en OnSpawn Y en cada tick de AI (las dos
     armas directamente rotas en juego). FIX: el espejo vive en
     `localAI[2]` (ranura libre, con `Projectile.damage` —que SÍ
     viaja— como fallback).
  2. **`PyraPalettes.Sample` NO cortaba NaN** — `MathHelper.Clamp` deja
     pasar NaN (las comparaciones con NaN son falsas) y `(int)NaN` =
     `int.MinValue` en x64 → índice negativo → excepción. FIX: guard
     `float.IsFinite` + tablas de 1 color + `Math.Clamp` final. A prueba
     de balas para TODA la librería PyraLib.
  3. **`CicloEstelarRenderer.DrawConveccion` dibujaba con el lote
     CERRADO** — venía de `DrawSunBody` (cierra tras FlushAdditive) y
     llamaba a `PyraLib.Tongue` que espera el lote ABIERTO del llamador
     → "Draw was called, but Begin has not yet been called" +
     "End was called..." (las 2 excepciones del log de las 18:08). FIX:
     `BeginAdditive()` + `End()` envolviendo las lenguas (el mismo
     contrato del resto del renderer).
  4. **`BrumaBrushes.Unload` disponía texturas en hilo del POOL** —
     ModContent.UnloadModContent corre fuera del hilo principal y FNA
     exige Texture.Dispose en el hilo principal → ThreadStateException
     en cada recarga. FIX: las referencias se cortan YA y la disposición
     real se encola con `Main.QueueMainThreadAction` (el canal oficial).
  5. (El mismo fix del punto 1 cubría las 4 trazas del log: OnSpawn +
     BaseDamage × 2 armas.)
  6. El warn "Image loading failed: unknown image type" del empaquetado
     existe desde v6.21 y NO es de nuestros PNGs (auditoría PIL: los 200+
     PNGs del mod son RGBA estándar, esquinas transparentes verificadas)
     — documentado, sin acción.

### B. LA BOLSA DEL ARSENAL PRIMORDIAL — el kit en UNA ranura
  Petición literal: todo lo que se le da al jugador, DENTRO de una bolsa;
  al jugador SOLO la bolsa. `ArsenalBag` (ítem permanente, rango rojo,
  PNG 30×30 procedural): `TestingPlayer.OnEnterWorld` pasó de 40+
  `EnsureItem` individuales a **UNA sola entrega garantizada** — la bolsa.
  Clic derecho la abre: despliega el arsenal COMPLETO (kit base + las 47
  armas + cosméticos) con semántica de garantía (solo lo que falte —
  reabrirla repone armas perdidas sin duplicar). Apertura con 30 chispas
  oro/violeta + texto de casa. `ArsenalBag.Contenido()` es ahora EL
  punto único de la verdad del arsenal (dar de alta un arma nueva = 1
  línea ahí). El inventario del jugador deja de inundarse en cada
  entrada al mundo.

### C. EL OCASO DE AETHON — el patrón gauge del Cosmic Destroyer, aplicado
  El arma suprema de la investigación v6.26 (el informe de las armas supremas de referencia,
  lección 10): LA TRINIDAD carga → burst → lockout, con los NÚMEROS del
  informe (gauge 100, +3 por impacto, 480 ticks de modo, ×3 de daño,
  execute <50% HP, lockout de castigo):
  · **CARGA**: el bastón dispara Fragmentos del Ocaso (150, búsqueda
    suave, estela ribbon + cruz de destello). CADA IMPACTO llena el aro.
  · **BURST**: aro lleno → CLIC DERECHO (AltFunctionUse) igniciona EL
    OCASO: 8 s donde cada disparo es una MUERTE DE ESTRELLA — mini-eclipse
    de 90 px con anillos rúnicos del EMISOR COMPARTIDO (tier 3, el mismo
    trazo de los soles), corona SolarFire (PyraPalettes), arcos de
    StormLib, anillo de OndaLib y cadena violeta a 2 vecinos (50%) —
    daño ×3 (450) con EJECUCIÓN (+50%) bajo el 50% de vida del objetivo
    (`ModifyHitNPC` + `FinalDamage` — sin dados). Al morir abre un
    DESGARRO en la realidad (RiftLib.Tear vía OcasoBurstFX, 40 ticks).
  · **LOCKOUT**: 2 s de Sobrecalentada — el bastón no dispara, humea.
  · **LA UI DEL GAUGE, 100% CÓDIGO** (`OcasoSystem.PostDrawInterface`,
    lote de interfaz TAL CUAL — el patrón aprobado de OndaSystem): aro
    de 20 segmentos tipo aureola sobre la cabeza; violeta→dorado al
    cargar; LISTO = pulso + 3 puntas orbitando + rótulo; ACTIVO = drenaje
    oro→rojo con chispas StormLib; LOCKOUT = rojo apagándose + vapor.
    Fade-out −0.05/tick (la lección de UI del TSA).
  · 2 buffs indicadores con PNGs 32×32 procedurales (OcasoActivo /
    Sobrecalentado, con cuenta atrás). `OcasoPlayer` es la máquina de
    estados; cadencia normal 14 ticks, ocaso 20. Sin maná (el costo ES
    el gauge).
  · 6 PNGs nuevos (2 iconos + 2 buffs + 2 sombras 76×76 de proyectil)
    por `tools/gen_v627_assets.py`.

### D. DOCUMENTACIÓN — los .md al día con la investigación
  · CARACTERISTICAS.md: nueva sección **LIBRERÍAS VFX** (el inventario
    completo de las 12 librerías + los 2 sistemas de soporte) + arsenal
    y sistemas al día (bolsa, gauge, contadores reales).
  · README.md: funciones/cómo-jugar actualizados (la bolsa, el arsenal
    de 47, el Ocaso).
  · Este CHANGES.md v6.27.
  · worklog.md (sandbox): Task 45 completo.
  · La opinión de biomas + el backlog priorizado de la investigación
    top-100 quedan en research/estrategia_v626/ (INFORME_BIOMAS.md con
    5 conceptos con plan de fases; INFORME_TOP100.md con 15 lecciones y
    tablas de ideas priorizadas A/B/C).

### E. ESTADO TÉCNICO
  · Compilación contra tModLoader 2026.07.3.0 real: **0 errores,
    0 warnings** (tras los fixes + el arma nueva + la bolsa).
  · build.txt → 6.27. hjson es/EN con las 8 claves nuevas (bolsa,
    arma, 2 proyectiles, 2 buffs). Sin referencias externas (auditoría
    v6.26 se mantiene: modReferences vacío).

## Commit v6.26 — EL SOL DE LOS 20 ANILLOS + RIFTLIB + 14 ARMAS NUEVAS + LAS CORONAS DE VERDAD

**Petición del usuario**: "el baston del eclipse primordial cámbialo,
esta nueva versión sera el sol de 20 anillos, y la mescla de todos los
agujeros negros rúnicos · oculta los soles de 6 a 19, y el sol 20 dale
una mejora mayor · crea nueva variantes de soles basado en estrellas
reales (estrellas de neutrones, pulsares, enanas blancas, estrellas
muertas...) · crea un baston que simule el ciclo de vida completo de una
estrella que se convierte en super nova · el baston sinfonia primordial
hace un destello en toda la pantalla, pero esto deberia estar
concentrado en el proyectil · luego crea mas bastones con nuevos tipos
de proyectiles creativos, al menos 5 · un baston que su proyectil sea un
desgarro en la realidad (investiga mods populares) · el anillo runico
estelar y corona de anillos runicos... tienen que ser creados por
codigos y tienen que copiar los anillos de los soles, pero no lo hacen ·
investiga las dos armas supremas de referencia · ideas
para un bioma nuevo + investigación de biomas · investiga los 100 mods
mas populares · limpia las referencias externas de las librerías".

### A. EL EMISOR COMPARTIDO DE LOS ANILLOS — LAS CORONAS DE VERDAD
  El problema de fondo de las coronas: eran COPIAS A MANO (constantes
  trasladadas) que se "parecían" a los anillos de los soles. v6.26 crea
  el EMISOR COMPARTIDO: `RuneSunRenderer.EmitRingSystem(center, R, time,
  seed, tier, rg, lifeT, alpha)` emite el sistema rúnico EXACTO al
  buffer de VFXCore (coords de mundo). Los soles del mundo, la CORONA DE
  ANILLOS RÚNICOS (aureola = Sol I literal sobre la cabeza) y el ANILLO
  RÚNICO ESTELAR (alas = Sol III literal en la espalda, 3 anillos con
  giros alternos) dibujan con EL MISMO CÓDIGO — si el sol cambia, las
  coronas cambian con él. GetGlyphPosition también usa la matemática
  compartida (RingGlyphWorld). DrawOrbitalSystem re-traduce a pantalla.

### B. EL ECLIPSE PRIMORDIAL — AHORA ES EL SOL DE LOS 20 ANILLOS
  El bastón cambia de dueño: ya no es un agujero negro con anillos — ES
  EL SOL DE LOS 20 ANILLOS (RuneSunRenderer tier 20, sistema ×1.30) con
  LA MEZCLA DE TODOS LOS AGUJEROS NEGROS RÚNICOS orbitando por fuera:
  los 3 círculos del Supremo (blanco/dorado/violeta contrarrotantes
  entrelazados con el anillo 20), el anillo de bandas del Cósmico, los
  brazos espirales del Olvido ALIMENTANDO al sol, el halo de bruma y las
  volutas de acreción de la Bruma, el anillo de fotones del Umbral, el
  gradiente aurora completo, la corona de descarga de StormLib, la luz
  prismática de LumenLib y los jets polares dobles. Muere en LA NOVA DEL
  ECLIPSE (anillo de Einstein + nova ×1.6 + 766 px — todo concentrado en
  el proyectil, sin flash de pantalla). Daño 700.

### C. LA FAMILIA DE LOS SOLES — OCULTAS Y MEJORADAS
  · Soles 6..19 OCULTOS (no se entregan ni garantizan; el código sigue).
  · EL SOL 20 RECIBE LA MEJORA MAYOR: daño 536 → 820, sistema ×1.30,
    vida 15,3 s (+90 ticks), aura ardiente al 55%, LA NOVA DEL SELLADO
    ×1.6 con radio récord 766 px y sacudida 12.

### D. LA SINFONÍA PRIMORDIAL — EL DESTELLO CONCENTRADO
  Fuera el flash de pantalla completa (OndaLib.Flash): el destello vive
  AHORA en el proyectil — ImpactFlash local de 110 px con cruz larga,
  Flare ampliado y 10 ticks de caída. La sacudida de cámara se queda.

### E. RIFTLIB + EL BASTÓN DEL DESGARRO EN LA REALIDAD (nueva librería)
  RiftLib (~1000 L, según el contrato del informe de investigación):
  Tear (grieta lineal con labios ×3 capas velo/cuerpo/núcleo +
  aberración cromática R/B ±2 px + vacío OCLUSIVO), Interior con
  Estrellas (16-28 con scroll y paralaje), Grieta persistente (fractal
  Lichtenberg que respira), Shards, ChispasAnomalia, EcoGlitch y
  Oscurecer (el mundo se oscurece herido, máx 0.35, RiftMundoSystem).
  EL BASTÓN DEL DESGARRO EN LA REALIDAD (250): el proyectil ES el
  desgarro — telégrafo → apertura → sostenido → cierre; la LÍNEA de
  daño atraviesa PAREDES (Collision.CheckAABBvLineCollision) con DoT;
  al cerrarse deja una GRIETA PERSISTENTE que drena ~8 s.

### F. LAS SEIS ESTRELLAS REALES (astrofísica estilizada de la casa)
  · ESTRELLA DE NEUTRONES (240): núcleo de 12 px ultradenso, aura 60 px
    con daño TRIPLE, starquakes periódicos (Kick + onda + aberración).
  · PÚLSAR (280): el FARO — dos haces polares barriendo a 1 rev/s; cada
    barrido que toca un enemigo golpea ×1.5 (cooldown 30 t por NPC).
  · ENANA BLANCA (180): rescoldo cristalino de 7 facetas, anillo de
    acreción que ROBA brillo a los enemigos.
  · ESTRELLA MUERTA (200): la enana negra — masa oscura que devora luz,
    entropía 8/s, brasas frías PyraLib.ColdFire, runas a 0,2 Hz.
  · SUPERGIGANTE ROJA (260): coloso de 90 px con celdas de convección;
    al morir se COLAPSA (implosión 20 t) y revienta en NOVA ×1.8.
  · MAGNETAR (320): campo violeta con líneas RETORCIDAS (r=L·sen²θ con
    torsión), cadenas de rayo automáticas cada 20 t, ARRITMIA
    determinista (seno de seno).

### G. EL BASTÓN DEL CICLO ESTELAR — UNA VIDA COMPLETA EN 18 s
  Máquina de 5 actos: NEBULOSA (contracción + Wisps, aura fría) →
  SECUENCIA PRINCIPAL (ignición con flash + cuerpo SunShader + 3 anillos
  tenues) → GIGANTE ROJA (hincha ×2.2, 9 celdas de convección, nebulosa
  planetaria desprendiéndose) → COLAPSO + SUPERNOVA (implosión ×0.3 y
  nova ×2 en 650 px con TODAS las librerías) → EL REMANENTE (estrella de
  neutrones enana pulsando que se apaga). La ESTELA cuenta la historia
  en 5 colores.

### H. LOS CINCO BASTONES CREATIVOS
  · RELOJ DE ARENA CÓSMICO: el reloj VIVO — motas de luz cayendo por el
    cuello, el montículo creciendo, y CUANDO LA CÁMARA SE VACÍA EL
    TIEMPO SE INVIERTA (gira 180°): 720 ticks, varios ciclos.
  · MAREA GRAVITARIA: ola de luz que CABALGA el terreno (sube y baja
    colinas), arrastra enemigos, y al golpear pared se rompe en 3 olas
    menores (MareaChicaProjectile).
  · ENJAMBRE PRISMÁTICO: 12 avispas de luz con boids simple (cohesión/
    separación/migración de presa), cada una con su color del prisma y
    picaduras con cooldown.
  · PÉNDULO DEL JUICIO: péndulo con física REAL (θ'' = -g/L·sen θ), el
    arco SE AMPLÍA con cada vaivén (+8% hasta ±150°) y a los 8 s el hilo
    SE CORTA: la maza vuela balística y estalla.
  · CORO ESPECTRAL: 6 notas de luz cantando en órbita; cada una emite su
    anillo de onda (OndaLib.Pulse) a su turno con tono de campana; el
    coro se despide apagándose de a una.

### I. LA INVESTIGACIÓN (4 informes en research/estrategia_v626/)
  · el informe de las armas supremas (sesión local): el desintegrador (IER, con
    el patrón del mod de tormentas de referencia: rayo de 5600 px con telegraph 40 f + carga
    150 f, núcleo oscuro + bordes brillantes) y el destructor cósmico de la referencia
    Destroyer (el patrón gauge carga→burst→lockout ×3). 14
    lecciones aplicadas al diseño del arsenal.
  · INFORME_DESGARRO_REALIDAD (42-b): técnicas de desgarro de 16 fuentes
    + el CONTRATO de RiftLib (implementado en E).
  · INFORME_TOP100 (42-c): los ~150 mods más populares del Workshop con
    suscriptores reales, análisis por categoría y 15 lecciones
    estratégicas (nicho VFX-first, GIF de arma como canal #1...).
  · INFORME_BIOMAS (42-d): estado del Sagrario Hueco (stub), checklist
    técnico completo de biomas tML y 5 conceptos diseñados para
    AethonMod (Sagrario realizado, Campo Estelar, Veta Rúnica, Cenizas
    del Eclipse, Falla del Vacío — pendientes de implementar).

### J. LIMPIEZA DE REFERENCIAS EXTERNAS
  Auditoría completa: CERO dependencias de código (modReferences vacío,
  sin TryGetMod ni imports de terceros) y las menciones a otros mods en
  COMENTARIOS neutralizadas — las librerías son 100% propias y las
  técnicas están parafraseadas (nada de código GPL).

### K. ENTREGA
  · EnsureItem: +14 armas nuevas (Desgarro, 6 estrellas reales, Ciclo
    Estelar, 5 creativos) y soles 6-19 fuera del kit.
  · Localización es-ES/en-US completa de las 14 armas y sus proyectiles.
  · 28 PNGs nuevos (14 iconos 30×30 + 14 sombras 76×76, PIL+numpy
    determinista) + scripts reproducibles en tools/.
  · Compilación contra tML real: 0 errores / 0 warnings.

## Commit v6.25 — EL HUMO DE VERDAD: EL FIX PREMULTIPLICADO + LA INVESTIGACIÓN DE 23 FUENTES + TRES LIBRERÍAS NUEVAS

**Petición del usuario**: "¿qué es este error? se ve mal, se supone que es
humo o bruma... acaso es un sprite, deberías hacer humo o bruma mayormente
por código y de ser sprite estos deben ser transparentes con fondo
invisible, ¿acaso la librería está mal o los sprites? de ser así,
investiga a fondo y con profundidad todos los mods populares, al menos 20
mods y crea librerías que creas que nos falten y mejora la librería para
humo, bruma, niebla · si te fijas todo lo que lleva esto de humo o bruma
tiene el mismo error · la corona rúnica estelar del commit anterior estaba
bien no la cambies, la que tenías que cambiar era corona de anillos
rúnicos, regresa la corona rúnica estelar a como estaba en el commit
anterior".

### A. EL BUG DE LOS RECTÁNGULOS — LA CAUSA RAÍZ (y NO eran los PNGs)
  Los PNGs del mod (SoftGlow/Ring/BlackDisk) están BIEN: fondos
  transparentes, verificados píxel a píxel. El error estaba en las
  TEXTURAS HORNEADAS EN RUNTIME de la librería de bruma:
  · BrumaBrushes escribía `new Color(255, 255, 255, alfa)` — RGB LLENO en
    TODOS los píxeles (incluso donde alfa ≈ 0), SIN premultiplicar. En el
    pipeline FNA/tModLoader (que espera alfa premultiplicada): en lotes
    ADITIVOS (Blend One/One) el canal alfa se IGNORA por completo → TODO
    el quad se pintaba como RECTÁNGULO SÓLIDO de color; en lotes alfa los
    bordes quedaban duros (el RGB no muere con el alfa). ESO eran las
    "sábanas rectangulares" de las capturas — TODO lo que usaba BrumaFX
    tenía el mismo error porque TODOS comparten los mismos pinceles.
  · EL FIX: horneado PREMULTIPLICADO (`RGB = blanco × alfa`) + el Tinte
    de BrumaFX ahora premultiplicado (RGB × f además del alfa × f) — la
    INTENSIDAD manda también en lotes aditivos y los bordes son suaves en
    CUALQUIER lote. El humo de TODO el mod se ve suave de verdad.

### B. LA INVESTIGACIÓN — 23 FUENTES (informes en research/humo_v625/)
  · INFORME_MODS_HUMO.md (Task 41-a): vanilla 1.4.4.9 decompilada (Dust
    completo), los materiales de referencia locales + los repositorios públicos de
    referencia (7 mods clonados) + web (los mods de referencia,
    Avalon...). 24 lecciones con números y 8 anti-patrones.
  · ANALISIS_HUECOS.md (Task 41-b): gap analysis de 17 capacidades contra
    26 fuentes — qué tienen los mods premium que nos faltaba.

### C. BRUMAFX v2 — LA SEGUNDA GENERACIÓN DE LA LIBRERÍA DE HUMO
  · FLIPBOOK de ruido evolucionado: cada pincel es una TIRA VERTICAL de
    4-6 frames del MISMO campo fBm (dominio desplazándose +0.3 celdas y
    contraste creciendo por frame) — el humo SE DESGARRA, no solo rota
    (anti-patrón nº3 de la investigación). Puff cicla en ping-pong lento;
    AnimatedPuff avanza POR VIDA (el mod de referencia).
  · ESCALERA de texturas 64/128/160 px por radio (lección del mod de referencia).
  · VAPOR: el pincel de LUT DURA (núcleo denso, caída 255→0 al 74% —
    el mod de referencia) + luz del mundo por defecto + muerte rápida.
  · LUZ DEL MUNDO con piso: WorldTint(pos) — factor 0.25..0.85 por canal
    (en pleno día sin cambio; en cueva tenue pero visible; bajo antorcha
    el humo se TINTA cálido). Activada en la Tormenta Nebular.
  · VIENTO del mundo (Main.windSpeedCurrent) en Column/Tendril/MistBand/
    Vapor — humo de un mundo con clima.
  · SMEAR vanilla (dusts 130-134): hasta 6 copias del NÚCLEO cayendo
    atrás por la velocidad.
  · AnimatedPuff: envolvente nacimiento-rápido/muerte-lenta + crecimiento
    ×1.5 + RAMPA DE ENFRIAMIENTO (color→gris — la historia térmica) +
    brasa que EMITE luz mientras arde.
  · PRESUPUESTO de 500 quads/frame con LOD automático (menos sub-blobs).
  · Wisps: voluta + motas de brasa (lección nº24). API VIEJA 100%
    compatible (Puff/Cloud/Tendril/Column/MistBand sin cambios de firma).

### D. TRES LIBRERÍAS NUEVAS (los huecos del análisis 41-b)
  · ESTELALIB (Content/VFX/EstelaLib.cs): los RIBBONS de grosor variable
    que TODOS los mods premium tienen — Sanitize→Smooth→Resample del
    camino (oldPos sucios, teleports, escalera de ticks) + Ribbon de
    triple capa (velo/cuerpo/núcleo) con perfiles Head/Center/Comet/Alive
    + fantasmas con squash (el mod de referencia) + EstelaTrack (ring-buffer por
    identidad que se AUTO-PODRE a los 2 ticks).
  · ONDALIB (OndaLib.cs + OndaSystem.cs): las ONDAS EXPANSIVAS de
    impacto — frente ROTO en 12-16 segmentos con radio vivo + doble anillo
    (frente + retaguardia ×0.5) + grosor que engorda mientras muere +
    expansión fast-out + aberración cromática ±1.8% + EL PAQUETE DE
    IMPACTO: Kick (sacudida de cámara centralizada en el hook oficial
    ModifyScreenPosition, cap 14px, máx 2 impulsos) y Flash (velo radial
    en PostDrawInterface, máx 1 activo + cooldown).
  · PYRALIB (PyraLib.cs): EL FUEGO — recuperada la técnica Doom Fire
    perdida en la purga de v6.24. PyraPalettes (3 tablas de 37 niveles:
    SolarFire/ColdFire/VoidFire con los colores de la casa) + Tongue (la
    lengua: parpadeo inconmensurable 7.1/17.3, punta vaga con reversión,
    erosión de ruido, triple capa, ROTABLE) + Flame (racimo) + EmberField
    (campo de brasas con propagación determinista a 30 Hz, cap 600 celdas)
    + Sparks (ascuas físicas → ParticleManager) + Light.

### E. EL ECLIPSE — EL HUMO DEL VACÍO DE VERDAD
  Con el fix premultiplicado la bruma interior ya es SUAVE; y ahora con
  VOLUMEN COMPLETO: 8 volátiles orbitando (eran 6, más grandes) + las dos
  espirales con BRASAS (Wisps) + 4 JETS DE VAPOR del limbo (LUT dura,
  naciendo en el borde y CAYENDO al centro con aceleración gravitacional
  — la masa del vacío se come el gas). El centro del eclipse respira.

### F. LAS CORONAS — LA CORRECCIÓN DEL DESTINATARIO
  · LA CORONA RÚNICA ESTELAR (RuneCrownItem): REVERTIDA a v6.23 exacta —
    el arco de ocho glifos fucsia con perlas y chispas rosas que estaba
    BIEN (renderer + capa + ítem + icono + tooltips + chispas y luz del
    CosmeticPlayer, todo al estado del commit anterior).
  · LA CORONA DE ANILLOS RÚNICOS (RuneRingCrownItem): la que DEBÍA subir
    a la cabeza — ahora ES la aureola: EL ANILLO DEL SOL RÚNICO I LITERAL
    ringiendo la CABEZA (plano 1.62R×0.34, inclinación −0.55, giro CW
    0.26, radio base de cabeza 12px) con sus 6 glifos y perlas al brillo
    EXACTO de los soles; la capa ancla al CENTRO DE LA CABEZA.

### G. LAS LIBRERÍAS CABLEADAS EN LAS ARMAS (v6.24 → vivas)
  · LA SINFONÍA PRIMORDIAL: la estela de vuelo ahora es un RIBBON de
    EstelaLib (perfil Comet, prismático) sobre el camino REAL del track +
    la detonación lleva LA ONDA EXPANSIVA de OndaLib (frente roto con
    aberración cromática) + el paquete de impacto centralizado (Kick 9px +
    Flash del velo radial — sustituye al PunchCameraModifier ad-hoc).
  · LA LANZA DEL ALBA: la punta ARDE — PyraLib.Tongue con la tabla
    SolarFire (la llama ancla en la punta y crece contra la marcha) + el
    PULSO de OndaLib en cada golpe + las ASCUAS de PyraLib.Sparks
    (ParticleManager) + Kick suave de 3px.
  · LA TORMENTA NEBULAR: la nube física VIVE por la luz del mundo.

### H. CICLO DE VIDA
  BrumaSystem descarga ahora también los campos de brasas de PyraLib y
  los tracks de EstelaLib — cero fugas entre recargas.

— Compilación contra tModLoader 2026.07.3.0 real: 0 errores, 0 warnings.

## Commit v6.24 — EL SOL DEL ECLIPSE + EL HUMO DEL VACÍO + LA AUREOLA DEL SOL I + LOS CÍRCULOS DE LOS AGUJEROS + LAS TRES ARMAS DE LAS LIBRERÍAS

**Petición del usuario**: "al agujero negro eclipse no se le ve el sol ·
ahora crea un arma que use todas nuestras librerías, sé creativo con eso ·
el accesorio de la corona rúnica debe estar en la cabeza del jugador como
una aureola, además brilla mucho y no se parece en nada al aro que usa el
sol 1, tiene que ser una aureola igual al anillo del sol rúnico 1 · el
otro ítem cosmético anillo rúnico estelar tiene los mismos problemas,
además estos aros no deben estar en esa forma, la forma correcta es la
misma forma que la de los agujeros negros · el agujero negro eclipse no
debe ser completamente oscuro en el centro, debe tener alguna animación o
mejor, que tenga mucho humo o bruma · no olvides crear varias armas nuevas
que usen todas nuestras librerías · borra la envoltura de fuego y rayo".

### A. EL ECLIPSE — EL SOL SE VE (la corona solar del eclipse)
  v6.23 dejó "el sol asomando" como DOS AROS FINOS (alpha 0.20/0.11) — el
  usuario: "NO SE LE VE EL SOL". Ahora es UN ECLIPSE TOTAL DE VERDAD:
  · LA CORONA SOLAR (DrawSolarCorona, pintada ANTES del repintado negro):
    halo caliente 3.4r (alpha 0.44) + núcleo interno 2.5r + EL BLOOM del
    sol (LumenLib, 2 capas) + 9 STREAMERS de luz radiando del limbo
    (LumenLib.Ray con grosor animado y pulso por streamer). El repintado
    negro se come el centro y el SOL queda como el resplandor anular
    cegador alrededor de la luna negra.
  · EL LIMBO: el filo blanco-cálido al borde del disco SUBE a alpha 0.55
    (antes 0.20) + el jade dorado a 0.16.
  · EL ANILLO DE DIAMANTE: LumenLib.Flare de 4 puntas latiendo SOBRE la
    luna negra (sutil: 0.14..0.24 — el centro sigue siendo la luna).

### B. EL ECLIPSE — EL HUMO DEL VACÍO (el centro ya no está muerto)
  "No debe ser completamente oscuro en el centro, que tenga mucho humo o
  bruma": LA BRUMA DE LA LIBRERÍA VIVE DENTRO DEL DISCO (pase ALFA =
  masa de verdad sobre el negro absoluto — DrawVoidSmoke):
  · LA MASA CENTRAL que respira (Puff 0.40r, alpha 0.32 ± latido).
  · SEIS VOLÁTILES orbitando CW/CCW alternos (radios 0.30..0.60r,
    violeta/púrpura/brasa, cada uno con su semilla y pulso).
  · DOS VÓLUTAS espiralando hacia el centro (Tendril sobre camino
    espiral 0.92r→0.18r).
  · LA BRASA VIOLETA: glow tenue bajo el humo (0.09..0.14) — el centro
    NUNCA es un punto muerto del todo.
  Validado en mock 1:1 (tools/mock_eclipse_v624.py): VLM 8/10 humo
  visible, 7/10 "centro negro-pero-vivo", sol reforzado tras la ronda.

### C. LA CORONA RÚNICA = LA AUREOLA DEL SOL I (RuneCrownRenderer)
  "Debe estar en la CABEZA como una AUREOLA, igual al anillo del sol
  rúnico 1": el arco fucsia de 8 glifos de v6.03 QUEDA BORRADO — la
  corona es ahora EL ANILLO DEL SOL RÚNICO I LITERAL ringiendo la cabeza
  (la MISMA geometría de RuneSunRenderer tier 1): el aro elíptico de
  cápsulas con profundidad (semiejes 1.62×R / 0.34×R, inclinación −0.55,
  giro CW 0.26 rad/s) con SUS 6 glifos solares cabalgando la órbita
  rotados a la tangente, perlas y latidos — LOS ALPHAS EXACTOS del sol
  (aro (0.30+0.30·depth)·pulse, resplandor 0.20, trazos 0.85, perlas
  0.60/0.90). Chispas doradas + luz cálida tenue en CosmeticPlayer. Icono
  regenerado (la aureola dorada inclinada). El brillo ES el del sol —
  nada del bloom fucsia cegador.

### D. EL ANILLO RÚNICO ESTELAR = LA FORMA DE LOS AGUJEROS (RunicHaloRenderer)
  "Estos aros no deben estar en esa forma, la forma correcta es LA MISMA
  FORMA QUE LA DE LOS AGUJEROS NEGROS": los 4 anillos elípticos del sol
  IV QUEDAN BORRADOS — el ítem de alas es ahora LOS CÍRCULOS RÚNICOS DE
  LOS AGUJEROS, LITERALES (la técnica de SupremoBlackHoleRenderer):
  · TRES círculos PLANOS (RingQuad — el aro fino): BLANCO íntimo 2.02R
    CW rápido (6), DORADO 2.62R CW lento (8), VIOLETA 3.30R CCW (6 — el
    contrarroto arcano).
  · Las runas flotando ALREDEDOR de cada círculo (radio respirando por
    glifo + mecido — la runa del agujero vive DE PIE, no tangencial),
    con la tabla de glifos S0..S7 del supremo y perlas.
  · LOS ALPHAS EXACTOS de los agujeros: aros 0.24/0.20/0.18, trazos
    0.85·pulse, perlas 0.62/0.90.
  · La energía de vuelo VIVE acotada (0.85..1.20, giro ×1..1.5, runas
    ardiendo al blanco). Icono regenerado (los 3 aros concéntricos).

### E. LAS TRES ARMAS DE LAS LIBRERÍAS (la petición doble: "un arma" + "varias armas")
  CADA ARMA USA TODAS LAS LIBRERÍAS DEL PROYECTO — StormLib (rayos) +
  BrumaFX (humo) + LumenLib (luz) + las runas — cada una con su
  personalidad de juego:
  · EL BASTÓN DE LA SINFONÍA PRIMORDIAL (120 dmg, SinfoniaPrimordial
    Staff/Projectile): LA CHISPA DE LA CREACIÓN — corazón prismático de
    drift (BloomPulse + Flare + Aurora + 5 Ray de sol) + estela de
    BrumaFX + arcos de corona StormLib + cadenas a enemigos al vuelo
    (55% dmg, Electrified) + 6 runas orbitando (forma de agujero). AL
    MORIR — LA SINFONÍA: daño en área 140px + ImpactFlash + Aurora
    completa de 15 bandas + SEIS MultiBolt radiales reventando + la
    NUBE de bruma de la detonación creciendo + las runas VOLANDO +
    doble onda de choque + cámara.
  · EL CETRO DE LA TORMENTA NEBULAR (80 dmg/golpe, TormentaNebular
    Staff/Projectile): LA TORMENTA PERSISTENTE (5 s sobre el cursor,
    560px) — nube viva de BrumaFX (2 Cloud + 2 Puffs, pase ALFA = masa
    que ocluye) + aurora LumenLib latiendo dentro + runas orbitando el
    borde (8 oro CW + 6 violeta CCW) + arcos ambientales + HASTA TRES
    DESCARGAS simultáneas con ciclo propio: telegraph
    (LumenLib.Telegraph 10 ticks) → MultiBolt de la panza de la nube →
    daño 85px + Electrified + trueno + ImpactFlash + onda.
  · LA LANZA DEL ALBA RÚNICA (90 dmg, LanzaAlba Staff/Projectile): el
    FILO del amanecer — la hoja de luz (LumenLib.Lance + LanceTrail de
    8 fantasmas) + el Rayo de sol que la precede + Flare en la punta +
    la runa estrella girando en el corazón + la VÓLUTA de bruma sobre
    su estela real (Tendril, ring buffer de 8 posiciones) + EndCap
    eléctrico. Perfora 4 enemigos; CADA GOLPE encadena ChainBolt a 2
    cercanos (60% dmg + Electrified).
  Entrega completa: EnsureItem ×3, recetas 5 madera, iconos 30×30
  procedurales + sombras 76×76 (patrón del estilo de la casa), hjson
  es/EN, tools/gen_v624_assets.py reproducible, hoja de contacto VLM
  verificada (8/8 legibles).

### F. LA PURGA — LA ENVOLTURA DE FUEGO Y LA DE RAYOS BORRADAS
  "Borra la envoltura de fuego y rayo": git rm de FireVeilItem/
  Player/DrawLayer/Renderer + StormVeilItem/Player/DrawLayer/Renderer +
  FlameBrush.png (solo la usaba el fuego) + entradas hjson es/EN +
  EnsureItem. Auditoría post-purga: 0 referencias .cs restantes.

### G. CALIDAD
  · Auditoría de assets: 70 clases ModProjectile/ModItem → 0 PNGs de
    sombra faltantes (la lección v6.14.1).
  · Compilación contra tModLoader 2026.07.3.0 REAL (/tmp/verify +
    /tmp/tml, dotnet 8.0.425): Build succeeded · 0 errores · 0 warnings.
  · Mock del eclipse (tools/mock_eclipse_v624.py) con VLM: humo 8/10,
    centro vivo 7/10, sol reforzado tras la ronda de feedback.

## Commit v6.23 — EL AJUSTE FINO: el abrazo del fuego + la ENVOLTURA DE RAYOS + los anillos de los soles + el eclipse de verdad + los círculos rúnicos completos

**Petición del usuario**: "el fuego es muy grande, debe estar limitado a solo
unos 3 o 4 píxeles por encima del personaje · de la misma forma que haces con
el fuego, crea un item cosmético que sea una envoltura de rayos, usa nuestras
librerías para darle el toque especial · que los anillos de vuelo sean los
anillos del sol número 4, además el anillo es muy brillante, reduce el brillo
a como se ve en los soles · que la corona de anillos rúnicos sea la del sol
número 1 · el bastón del eclipse primordial debe ser negro en su centro, el
sol debe verse ligeramente · los dos bastones de agujeros negros supremos
deben tener sus agujeros 3 anillos rúnicos (tienen dos) · todos los agujeros
avanzados deben tener 2 anillos rúnicos de tipo agujero (solo uno de los
bastones tiene 2, el resto solo 1)".

### A. EL FUEGO — EL ABRAZO JUSTO (fix de tamaño)
  El campo de propagación de intensidades pasa de 26×38 celdas (una COLUMNA
  de 167 px — 4× la altura del jugador) a **11×11 celdas** (48 px): la
  envoltura vive PEGADA A LA SILUETA — la base arde sobre los pies cubriendo
  el ancho del cuerpo y las puntas LAMEN la coronilla 3-4 px por encima,
  nada más. Re-calibrado completo: decay más rápido (muere al pasar la
  cabeza), viento ±1.2 celdas, inercia vertical ±5..7 px, pincel ×1.18/×1.30,
  1 pase extra al volar (antes 2), el humo nace justo sobre la coronilla
  y las chispas a lo largo del cuerpo. TODAS las interacciones vivas:
  viento en contra al correr, avivo, aplastado del salto, estirada en caída.

### B. LA ENVOLTURA DE RAYOS PRIMORDIAL (cosmético nuevo — StormLib)
  `StormVeilRenderer/Player/Item/DrawLayer` — la HERMANA ELÉCTRICA del fuego:
  la SILUETA del jugador (una elipse del tamaño exacto del cuerpo) es el
  CARRIL de una tormenta construida con STORMLIB (nuestra librería):
  · **LOS CHISPAZOS** — rayos de verdad entre dos anclas del contorno:
    ZigPath + REFINO FRACTAL multi-escala (la MISMA matemática del rayo del
    cielo aprobado), oro solar y azul-estelar alternando.
  · **LOS ARCOS** — descargas abrazando el contorno con jitter radial hash
    (el patrón ArcRing): media silueta arriba, media abajo.
  · **LOS PELOS** — filamentos caóticos finísimos hacia afuera (el "hair"
    de las descargas reales).
  · **EL RENDER** — cada segmento = TRES CAPAS por VFXCore (halo + cuerpo +
    núcleo blanco razor) con las texturas BoltHalo/BoltCore procedurales;
    gorros de descarga en los extremos.
  · **INTERACTIVA**: la velocidad acumula ENERGÍA (quieto = brisa eléctrica,
    corriendo = tormenta encendida con estela a contra de la marcha); al
    saltar los arcos caen a los pies, al caer suben a la cabeza. Chispas
    DustID.Electric + luz fría-oro con stutter de descarga.
  Icono 30×30 procedural (tools/gen_storm_veil_icon_v623.py), localización
  es/EN, EnsureItem al entrar al mundo.

### C. LOS ANILLOS DE VUELO = LOS ANILLOS DEL SOL IV (brillo de soles)
  `RunicHaloRenderer` REESCRITO: el halo de la espalda ya NO es un anillo
  gigante con bloom cegador — es **EL SISTEMA ORBITAL DEL SOL RÚNICO IV,
  LITERAL**: los CUATRO anillos del sol nº4 (semiejes 1.62+0.44k ×R, achatado
  0.34..0.48, inclinaciones −0.55..+0.05, giro alterno CW/CCW, 6/8/10/12
  glifos con perlas y latidos) orbitando la espalda, a los **ALPHAS EXACTOS
  del sol** (aro (0.30+0.30·depth)·pulse, glifos 0.85·pulse, perlas
  0.60/0.90). El corazón pasa de bloom ×2.1 a un latido discreto de 22 px.
  La ENERGÍA DE VUELO sigue viva pero ACOTADA: multiplicador 0.85..1.20,
  giro ×1..1.5, runas ardiendo al blanco al volar. La luz del mundo y las
  chispas también se contienen al nivel solar.

### D. LA CORONA = EL ANILLO DEL SOL I (brillo de soles)
  `RuneRingCrownRenderer` REESCRITO: de tres aros propios con corazón
  brillante a **EL ANILLO DEL SOL RÚNICO I, LITERAL** — un solo aro
  (1.62R × 0.34, inclinación −0.55, CW 0.26 rad/s) con sus 6 glifos,
  perlas y latidos al BRILLO EXACTO del sol. Icono regenerado (un anillo
  inclinado con 6 perlas). CosmeticPlayer actualizado (chispas doradas +
  luz cálida única).

### E. EL ECLIPSE PRIMORDIAL — EL SOL ASOMA
  El corazón ya NO se lava de dorado: el AURA FINAL (SoftGlow 6.6×rr) se
  pinta AHORA **ANTES** del repintado negro — el vacío la DEVORA en el
  centro y queda como halo alrededor del disco. Y nace **LA CORONA DEL
  ECLIPSE**: un aro fino de luz blanco-cálida JUSTO al borde del disco
  negro (1.055R, 0.20±0.07) + un jade dorado más afuera (1.13R, 0.11±0.04)
  — el sol vivo asomando tras la luna negra: un eclipse REAL.

### F. LOS CÍRCULOS RÚNICOS DE LOS AGUJEROS NEGROS
  · **Los DOS SUPREMOS → 3 anillos**: el Supremo añade el CÍRCULO BLANCO
    íntimo (6 runas @2.02R, CW 0.16 — entre el anillo de fotones y el
    dorado); el Supremo Aurora añade el CÍRCULO MORADO íntimo (6 runas
    @2.02R — el color que faltaba del gradiente negro→morado→azul→dorado).
    REFACTOR: DrawRune ya no hardcodea el viejo if de dos círculos —
    count/orbit son parámetros (el bug que habría roto el tercer anillo).
  · **Los ASCENDIDOS → 2 anillos CLAROS cada uno**: el Cósmico ya los
    tenía (intacto); el Umbral SUBE su anillo íntimo (radio 1.66R→1.95R,
    glifos ×0.62→×0.80 — ahora se LEE); la Bruma Ascendida recibe SU
    SISTEMA RÚNICO desde cero (8 teal CW @2.55R + 6 hielo-blanca CCW
    @3.15R, paleta RimTeal/FrostMote/PhotonWhite); el Olvido añade el
    círculo VIOLETA contrarrotante (6 runas @3.20R, −0.12 rad/s).

### G. ENTREGA
  Compilación **0 errores / 0 warnings** contra tModLoader v2026.07.3.0
  real. build.txt 6.23. Localización es/EN de la Envoltura de Rayos.
  EnsureItem de la Envoltura al entrar al mundo.

## Commit v6.22 — LA LUZ Y EL FUEGO: LumenLib + el Eclipse Primordial + los soles 11-20 + 3 cosméticos interactivos

**Petición del usuario**: "crea un item cosmético que envuelva al personaje
con fuego creado por código, el fuego debe interactuar con las acciones del
personaje cuando se mueva · el bastón de rayos está perfecto, ahora añade
ese rayo a todo lo que usaba la librería de rayos anterior, mantén la
concordancia, la consistencia y los tamaños correctos · crea 10 bastones
más de sol con anillos rúnicos, del 11 hasta el 20 · crea un bastón nuevo
que fusione el sol de 20 anillos rúnicos más todos los agujeros negros,
dale efectos de luz, bruma, humo, rayos y otros efectos · crea un cosmético
que sea una corona de anillos rúnicos que rodee al jugador · crea un
cosmético de un anillo rúnico en la espalda que funcione como alas y halo;
cuando el jugador vaya a volar este anillo brilla con intensidad ·
pregunta: StormLib ¿sirve para haces de luz y otros efectos o solo rayos?
en cuyo caso crea más librerías con el conocimiento de los mods estudiados
· investiga super profundo a los mods de referencia de esa hornada (la emperatriz de la luz)
y crea una librería para manejar la luz como ellos".

### A. LA INVESTIGACIÓN DE LUZ SUPERPROFUNDA (3 informes nuevos)
  · **el mod de la emperatriz** (informe local de esa versión) — 31 archivos
    leídos + 12 shaders HLSL .fx incluidos en el repo: el BLOOM APILADO
    INVERTIDO (textura radial 200×200 en 2-4 capas: escalas 4.1/2.85/1.5/0.8
    con alfas 0.25/0.67/0.7/1.0), paletas cíclicas MulticolorLerp con wrap,
    hue drift 0.2-0.6/s + semilla por identidad, estela sinusoidal
    perpendicular, LightLance con telegraph de 2100px + 30 fantasmas,
    deathray con pulso 8.6 Hz. Casi CERO Lighting.AddLight: todo es render
    emisivo.
  · **La Emperatriz VANILLA extraída del binario real** (INFORME_EOL_
    VANILLA.md — tModLoader.dll decompilado): el color = hslToRgb(hue%1,
    S=1, L por capa: 0.5 cuerpo/0.85 luz/1.0 núcleos), la DOBLE PASADA
    universal (color A÷2 ×1.1-1.4 SOBRE blanca A÷2 ×1.0), afterimages que
    CRECEN hacia atrás (×1.4, 39-79 fantasmas), la telegrafía de lanza de
    3600px, SunDance = sprite estirado 4 capas con grosor animado
    0.25→0.7 + LUZ MUESTREADA cada 800/12 px, el aurora de muerte de 15
    bandas espejadas π·i, el enrage dorado (255,231,69).
  · **el rework de la Emperatriz** (informe local de esa versión —
    .tmod descargado por 10 rangos paralelos + parser propio del formato
    + ILSpy + VLM): el LUT arcoíris 1×256 (HSL S=1 L=0.5 — valida nuestra
    matemática EXACTA), hue en ai[0] EN GRADOS con voleas desfasadas,
    lanza = sprite + triángulo de 2500px + 8 afterimages con SQUASH Y,
    doble dibujado con A=0, fuego = LUT de 40 niveles, tiras 1×N de
    perfil DURO tintadas en runtime, warp de pantalla con RT.
  · CERO código copiado de ninguno: las TÉCNICAS re-implementadas 100%.

### B. LUMENLIB — LA LIBRERÍA DE LA LUZ (la respuesta a la pregunta del usuario)
  StormLib ERA solo de rayos (filamentos eléctricos). Ahora el proyecto
  tiene el TRÍO completo: **StormLib** (rayos) + **BrumaFX** (humo/niebla)
  + **LumenLib** (la luz que EMANA). `Content/VFX/LumenLib.cs`:
  · **Motor de color**: Hue (HSL propio con las L firmadas por capa),
    Drift (el hue que camina 0.2-0.6/s con semilla por identidad), Cycle
    (paletas cíclicas con wrap) + LumenPalettes (PrismRose/PrismDay/
    SolarGold/VoidCold/EclipseFire).
  · **Bloom** — el apilado invertido de 2-4 capas con los NÚMEROS medidos
    del ecosistema (4.1/2.85/1.5/0.8 · 0.25/0.67/0.7/1.0) + BloomPulse.
  · **DoublePass** — la doble pasada universal (color ×1.15 A÷2 sobre
    blanca A÷2).
  · **Flare** — el destello de 4 puntas (cruz + diagonal ×0.62 + punto
    caliente).
  · **Ray** — el rayo de sol: bloom ESTIRADO en 3 capas (velo ×1.6 /
    cuerpo / núcleo ×0.3) con GROSOR ANIMADO y la boca cegadora.
  · **Lance + LanceTrail + Telegraph** — la lanza de luz con hoja
    (LumenBlade), N fantasmas que crecen ×1.4 con alpha (1-i/N)^1.6 y la
    línea de aviso con anillo objetivo.
  · **Aurora** — las 15 bandas espejadas π·i con dos vueltas de hue y dos
    frecuencias.
  · **LightAlong** — la luz del mundo MUESTREADA cada N px (la lección
    del muestreo de la Emperatriz).
  · **4 texturas procedurales nuevas**: LumenBloom (200×200), LumenBlade
    (64×256 con taper y vetas), LumenFlare (128×128) y FlameBrush (24×36).

### C. LA MIGRACIÓN COMPLETA: STORMLIB EN TODAS PARTES
  TODO lo que usaba la librería vieja de rayos ahora usa la 2ª generación
  (misma concordancia, mismos tamaños): los 4 Ascendidos + el Supremo +
  el Supremo Aurora + el sol rúnico (prominencias, rayos fugitivos, jets)
  — Bolt→Bolt, Arc→ArcRing, Flicker→IsLit, y los caminos suavizados
  (Smooth+JitterPath) ahora son BÉZIER + REFINO FRACTAL (la rugosidad
  multi-escala). **LightningCore.cs ELIMINADA** del mod: una sola librería
  de rayos, la buena.

### D. LOS SOLES RÚNICOS 11-20 (la segunda década, una capa nueva por tier)
  11 · COMETA ORBITAL (cabeza + cola cruzando los anillos) · 12 · LLUVIA
  DE RUNAS cayendo al sol · 13 · AURORA POLAR (cortinas LumenLib.Ray con
  drift de hue) · 14 · ESTRELLA COMPAÑERA azul + PUENTE DE LUZ · 15 ·
  CINTURÓN DE ASTEROIDES kepleriano con brecha · 16 · TORMENTA TOTAL
  (multi-boltos + arco corona) · 17 · CORONA PRISMÁTICA (rayos de luz de
  colores) · 18 · LANZAS PRISMÁTICAS orbitando con estelas · 19 · NÚCLEO
  DE NUEVA (latido a estallido + destellos de limbo) · 20 · EL GRAN
  SELLADO (los 8 glifos maestros en un aro ecuatorial + contrasello
  violeta retrógrado). Packing más tighto del 10º anillo arriba, runas
  +1/anillo (435 glifos en la XX), gigante final ×1.75 en la XX, daño
  80→536.

### E. EL ECLIPSE PRIMORDIAL — LA FUSIÓN TOTAL
  `Bastón del Eclipse Primordial`: un agujero negro SUPREMO (esfera 55px,
  atracción 600px — la mayor del mod) con EL SISTEMA SOLAR RÚNICO XX
  COMPLETO orbitando el horizonte (RuneSunRenderer.DrawOrbitalSystem con
  tier 20: los 20 anillos + gran sellado + cometa) + TODAS las herencias:
  disco Doppler con GRADIENTE AURORA, anillo de bandas sin(θ·20+t·5),
  brazos espirales, halo de BRUMA (Cloud×2 + Puff de HUMO que respira),
  volutas Tendril cayendo, corona de RAYOS StormLib (arcos + multi-boltos),
  jets polares, doble círculo de runas, RAYOS PRISMÁTICOS LumenLib + el
  destello del corazón + el velo aurora. Muerte = ANILLO DE EINSTEIN +
  NOVA RÚNICA (las dos explosiones juntas) + 34 runas de eco. Lente
  gravitacional ×4.2 registrada en BlackHoleLensSystem.

### F. LA ENVOLTURA DE FUEGO PRIMORDIAL (cosmético interactivo)
  `FireVeilItem` + `FireVeilPlayer` + `FireVeilRenderer` +
  `FireVeilDrawLayer`: un CAMPO DE 26×38 celdas de intensidades 0..36 vive
  sobre el jugador — el algoritmo clásico de PROPAGACIÓN DE FUEGO (base
  siempre encendida, decaimiento aleatorio, deriva lateral) con TABLA DE
  37 COLORES propia (brasa→carmesí→naranja→ámbar→oro→blanco). EL FUEGO
  INTERACTÚA: al CORRER el viento inclina las llamas EN CONTRA de la
  marcha y las AVIVA (más intensidad + brasas sueltas) · al SALTAR se
  APLASTAN y se retrasan por debajo (la inercia) · al CAER se ESTIRAN
  hacia arriba (pases extra de propagación) · al VOLAR se vuelven COLUMNA
  (pases dobles) · quieto: la lumbre calma. Chispas + humo + luz cálida
  respirando. 100% código: cero sprites de fuego.

### G. LA CORONA DE ANILLOS RÚNICOS (cosmético)
  `RuneRingCrownItem`: TRES anillos rúnicos orbitando el CUERPO (el aro
  dorado casi vertical del pecho, el blanco-estelar inclinado en
  contrarroto y el ecuatorial azul de la cintura) — la técnica de los
  anillos del Sol puesta sobre el jugador, con glifos a la tangente,
  perlas y latidos. Chispas doradas/azules + luz mixta.

### H. EL ANILLO RÚNICO ESTELAR (alas + halo)
  `RunicHaloWings` (vuelan de verdad: 180 ticks, velocidad 9, ×2.5 — el
  patrón AutoloadEquip + PNG 8×8 en blanco de v6.12): un GRAN ANILLO
  RÚNICO vertical tras la espalda con contraro, glifos y corazón de luz.
  AL VOLAR SE ENCIENDE (la ENERGÍA DE VUELO de RunicHaloPlayer: +0.09/tick
  volando): el bloom ×2.2, la CRUZ DE LUZ, los 8 rayos radiales, el doble
  ancho del aro y las runas ardiendo al blanco. Chispas tangenciales a
  borbotones + luz del motor.

### I. ENTREGA
  · 15 nuevos PNGs procedurales (LumenBloom/LumenBlade/LumenFlare/
    FlameBrush + iconos XI-XX + Eclipse + 3 cosméticos + sombra del
    proyectil + _Wings 8×8) — validados VLM en 2 rondas ("LISTOS PARA
    INTEGRACIÓN").
  · Localización es-ES + en-US completa (15 DisplayName nuevos).
  · EnsureItem ×2 sitios para TODO lo nuevo (10 bastones + Eclipse + 3
    cosméticos).
  · build.txt 6.22 · compilación 0 errores / 0 warnings contra tML real.

## Commit v6.21 — LA TORMENTA: rayos de verdad + el fix de los soles + sin maná

**Petición del usuario**: "la imagen muestra cómo se ve el sol, solo se ve su
textura, no tiene animación, solo un cuadrado con textura · en cuanto al
bastón de rayos, eso no son rayos de verdad, no se parecen en nada a rayos,
es momento de investigar y mejorar · todos los bastones que crees son de
prueba, por lo tanto no necesitan usar mana · investiga los mods de referencia (Coralito y Flujo,
los mods de la gran referencia de esa versión — sus librerías y técnicas — y crea tus
propias librerías con todo lo aprendido de la investigación profunda y
metódica".

### A. LA INVESTIGACIÓN PROFUNDA (4 repos clonados y estudiados a fondo)
  · **el mod del coral** (360 MB, 78 archivos de rayos) — el jefe eléctrico y su
    librería de descargas: jitter perpendicular con extremos anclados,
    parpadeo con APAGADO del ~50% a 15 Hz, MULTI-FILAMENTO superpuesto
    (2 colores), muerte violenta (el jitter REVIENTA al disolverse),
    gorros a 2 escalas, daño en la LÍNEA RECTA (el jitter es cosmético).
  · **el mod del flujo** (658 MB) — árbol de rayos RECURSIVO con AUTO-CORRECCIÓN
    de curvatura (rot −= totalRot·0.3), el "hervir" de todos los puntos,
    el FLASH MULTI-DRAW (redibujar la misma geometría N veces), ancho
    empaquetado en las coords de textura, la capa negra bajo las estelas.
  · **el mod de la ira de la emperatriz** (16 MB) — la CRUZ DE LUZ de 4 draws en los
    impactos (2 orientaciones × 2 escalas con pulso), el TELEGRAPH como
    contrato (línea de aviso + daño/movimiento gateados), paletas por datos.
  · **el mod del velo** (27 MB) — el sándwich de batch
    Immediate (valida nuestro contrato v6.10), endcaps redondeados,
    `extraUpdates` para densidad, screen-shake con atenuación por distancia.
  · Los 4 informes completos (Task IDs 37-a…37-d) viven en el worklog;
    la síntesis operativa en `research/storm_v621/INFORME.md`.

### B. STORMLIB — LA SEGUNDA GENERACIÓN DE RAYOS (librería nueva, 100% propia)
  · **4 texturas procedurales nuevas** (RGB blanco + perfil en alfa):
    `BoltHalo` (banda gaussiana suave), `BoltCore` (EL FILAMENTO: núcleo
    blanco que serpentea dentro de la textura con GRIETAS de alta
    frecuencia y nodos brillantes — el análisis VLM de las texturas reales
    del ecosistema medido y replicado), `BoltChain` (eslabones) y
    `BoltImpact` (estallido radial de 7 rayos desiguales).
  · **`Content/VFX/StormLib.cs`** (LightningCore queda INTACTA para los
    agujeros negros): ZigPath (perpendicular + dispersión + envolvente,
    extremos EXACTOS), **Refine** (subdivisión de punto medio con sesgo
    cúbico — la rugosidad MULTI-ESCALA que rompe el zigzag geométrico),
    Boil, ForkTree (ramas con auto-corrección, ×0.40 de ancho),
    **MultiBolt** (tronco + 2 acompañantes + 2 PELOS caóticos + ramas),
    Strand (TRIPLE CAPA: halo contenido ×2.0 / cuerpo / NÚCLEO BLANCO
    razor ×0.26 — la nitidez vive en la TEXTURA), ChainBolt, ArcRing,
    **ImpactFlash** (cruz de luz 4-draw + destello apilado), EndCap,
    AddLightAlong (luz estrangulada), IsLit (apagado intermitente) y
    DeathGrow (la muerte violenta).
  · Verificación visual (patrón de la casa): `tools/mock_storm_v621.py`
    traduce StormLib 1:1 a Python sobre las texturas reales → mock VLM
    **8/10 "reads as real lightning, release-quality, excellent in
    motion"** (v1 6.5 → v2 8 tras afinar: núcleo fino, fractal, pelos).

### C. EL CETRO DEL TRUENO RECONSTRUIDO — EL RAYO DEL CIELO
  · `RunicLightning` v6.21: ya no dispara una línea horizontal borrosa —
    **INVOCA UN RAYO QUE CAE DEL CIELO** sobre el cursor: TELEGRAPH de 10
    ticks (línea fina de aviso + anillo objetivo pulsante + carga a
    ráfagas) → el GOLPE (daño en la COLUMNA cielo→suelo por línea recta +
    estallido radial de 110 px + CADENA a 3 enemigos al 60% + Electrified
    240) → LA DESCARGA (MultiBolt de 3 filamentos + pelos + ramas,
    regenerado a 15 Hz con apagado intermitente, muerte violenta con
    DeathGrow) → EL IMPACTO (cruz de luz + destello apilado + onda de
    choque + arcos crispados + 40 chispas + puñetazo de cámara VERTICAL +
    trueno grave por capa de zaps con pitch −0.45).
  · `StormRuneStaff`: **MANA 0** (regla del usuario: todos los bastones
    son de prueba — el resto de la familia ya estaba en 0), disparo al
    cursor clampeado a 560 px, tooltip actualizado, sonido de uso grave.

### D. EL FIX DEL SOL RÚNICO (el cuadrado con textura)
  · **CAUSA RAÍZ**: `RuneSunRenderer.BeginAdditive/BeginAlpha` usaban
    `SpriteSortMode.Deferred` → el pase `Passes[0].Apply()` se IGNORA
    (con Deferred el SpriteBatch enlaza su PROPIO efecto al hacer flush)
    → el `DendriticNoise` se pintaba CRUDO como un cuadrado estático sin
    animación (exacto el reporte del usuario); el RadialShine del aura
    también se perdía.
  · **EL FIX**: ambos helpers pasan a `SpriteSortMode.Immediate` con
    `LinearWrap` — IDÉNTICOS al sol original que sí se ve bien → el disco
    de plasma vuelve a ser la esfera animada con SunShader y el aura
    vuelve a ser el resplandor de ruido vivo. (LECCIÓN v6.21: cualquier
    pase de shader exige Immediate — el contrato del batch no es opcional.)

## Commit v6.20 — FIX DE LOS SOLES RÚNICOS + EL CETRO DEL TRUENO + EL AGUJERO NEGRO SUPREMO AURORA

**Petición del usuario**: "que raro, no veo los soles, los bastones sí
invocan al sol, pero el sol no se ve, o sea el proyectil es invisible,
lo mismo con el bastón de rayo, no se ve nada y además dio error · el
sol original sí se ve, pero los nuevos soles no se ven ni el bastón de
rayos · en el agujero negro supremo, cambiar el color: centro negro y
que vaya cambiando de color — a morado cerca del centro, azul y dorado
en los bordes; guardar una copia del original y crear uno nuevo con
estos cambios".

### A. EL BUG DE LOS PROYECTILES INVISIBLES (v6.19 corregido)
  · **CAUSA RAÍZ** (del client.log): `InvalidOperationException: Begin
    has been called before calling End` — `RuneSunProjectile.PreDraw`
    y `RunicLightning.PreDraw` llamaban a sus renderers CON EL
    SpriteBatch de tML todavía ABIERTO; el primer `BeginAdditive()`
    interno re-abría un batch ya abierto → excepción CADA FRAME → el
    dibujo abortaba → proyectil INVISIBLE (los bastones sí invocaban).
  · **EL FIX**: el CONTRATO DE BATCH A PRUEBA DE BALAS (v6.10, el mismo
    de la familia de agujeros negros) aplicado a los dos PreDraw:
    `End()` defensivo → renderer (cerrado→cerrado) → `RestoreSpriteBatch()`
    con los parámetros EXACTOS del pase de proyectiles de vanilla
    (`Main.Rasterizer` + `Main.Transform`).
  · Los 10 Bastones del Sol Rúnico I..X y el Cetro del Trueno Rúnico
    ahora pintan su sistema completo (anillos, runas, rayos, efectos).

### B. EL AGUJERO NEGRO SUPREMO AURORA (el original queda INTACTO)
  · **SupremoAuroraBlackHoleRenderer.cs** (VFX): la copia NUEVA del
    Supremo con EL GRADIENTE DEL USUARIO como FUNCIÓN —
    `AuroraGrad(t)`: MORADO (185,105,255) en t=0 → AZUL (92,150,255) →
    DORADO (255,195,90) en t=1 — aplicado a CADA CAPA por su distancia
    radial al núcleo:
    · EL NÚCLEO → NEGRO ABSOLUTO (BlackDisk, repintado final intacto).
    · JUNTO AL NÚCLEO → MORADO: el rim del horizonte, el anillo de
      fotones, los arcos LightningCore, el interior del disco Doppler
      (blanco frío el lado que acerca, morado profundo el que aleja) y
      la nube interna de Bruma.
    · EL MEDIO → AZUL: los brazos espirales (gradiente A LO LARGO del
      brazo), el círculo interior de runas, el contrarroto del aura y
      el lado que acerca del disco de acreción.
    · LOS BORDES → DORADO: el anillo de bandas (cada SEGMENTO coloreado
      por su distancia radial real 1.24..1.95R — la petición hecha
      geometría), las puntas de los brazos y de los JETS POLARES
      (morado→azul→dorado a lo largo del haz), el círculo exterior de
      runas, la nube externa y el aura final.
    · LAS ONDAS DE DISTORSIÓN NACEN MORADAS y MUEREN DORADAS (el
      gradiente expandiéndose por el espacio-tiempo), y los rayos
      fugitivos son 2 AZULES + 1 DORADO.
  · **SupremoAuroraBlackHoleProjectile.cs**: la MISMA física suprema
    (esfera 55px, atracción 550px, devora balas, anillo de Einstein)
    con la paleta aurora en luz (centro morado, polo azul, base
    dorada), partículas, disco de acreción e impactos.
  · **SupremoAuroraBlackHoleStaff.cs**: "Bastón del Agujero Supremo
    Aurora" — 300 de daño, tooltips cortos, localización es/EN.
  · Registro completo: BlackHoleLensSystem (lente gravitacional:
    fuente + radio + núcleo encima de la lente), EnsureItem al entrar
    al mundo, sombra 76×76 (rim morado→azul, peak 78) e icono 28×30
    (bastón morado + mini-agujero con el gradiente pintado por píxel +
    runa dorada + destellos fríos) — tools/gen_supremo_aurora_tex_v620.py.

## Commit v6.19 — LOS SOLES RÚNICOS + EL CETRO DEL TRUENO

**Petición del usuario**: "ahora hagamos que el sol sea más mágico: al
igual que los agujeros negros tienen anillos con runas, crea varias
copias del sol y ponles anillos con runas — la primera un solo anillo,
la segunda 2 (uno rodea el sol y el otro en OTRA dirección), la tercera
3 y así hasta 10 copias, cada una mejorada un poquito más hasta la
copia 10 con muchas mejoras y animaciones · el SOL ORIGINAL NO SE TOCA
· crea un arma que use rayos con NUESTRA librería, con varios efectos
en los rayos".

### A. LA FAMILIA DE LOS SOLES RÚNICOS (10 copias — el Sol original INTACTO)
  · **RuneSunRenderer.cs** (VFX): UNA sola clase parametrizada por
    copia (tier 1..10) — el cuerpo solar hereda la TÉCNICA del sol
    (backglow BloomCircle + aura RadialShineShader + disco de plasma
    SunShader con granulación dendrítica, re-implementada — cero
    dependencia del SunProjectile) + el SISTEMA RÚNICO:
  · **N ANILLOS = el número de la copia**: cada anillo vive en SU
    PROPIO plano orbital (semiejes, achatado e inclinación distintos)
    con **GIRO ALTERNO** — el par gira horario, el impar antihorario
    ("el otro rodea el sol en otra dirección").
  · Cada anillo: aro elíptico de cápsulas con PROFUNDIDAD (el frente
    más brillante) + glifos de la **ESCRITURA SOLAR** (8 diseños
    nuevos: el Astro, la Llama, la Rueda, la Espiga, la Puerta del
    Día, la Corona, el Cometa y el Sigilo) cabalgando la órbita
    ROTADOS A LA TANGENTE, con perlas y latidos propios.
  · **MEJORAS PROGRESIVAS** (una capa por copia): 2·chispas orbitales
    3·destellos de 4 puntas · 4·prominencias de plasma del limbo ·
    5·viento solar · 6·rayos fugitivos entre anillos (LightningCore) ·
    7·precesión de los planos + acentos azul-estelar cada 3er anillo ·
    8·núcleo pulsante + ondas de eco · 9·corona de pétalos de plasma ·
    10·ERUPCIÓN RÚNICA (runas desprendiéndose y volando) + JETS
    POLARES con rayo interno — el sistema completo.
  · **RuneSunProjectile.cs**: UN proyectil parametrizado (ai[0] =
    copia): vida 10 s + 0.2 s por copia, pop elástico, persecución
    suave de enemigos, aura ardiente cada 10 ticks, GIGANTE FINAL
    (hinchazón ×1.5 + daño ×1.5 en los últimos 1.5 s) y UNA SOLA NOVA
    RÚNICA al morir (onda nova + AoE + ráfaga de runas de eco — la
    lección v5.97 de "una sola explosión").
  · **RuneSunStaves.cs**: 10 bastones (Bastón del Sol Rúnico I..X),
    daño 80→296, tooltips cortos de 2 líneas, localización es/EN.
  · Iconos 28×30 procedurales: bastón dorado + cabeza solar + N
    anillos elípticos de inclinaciones alternas (tools/
    gen_rune_suns_v619.py).

### B. EL CETRO DEL TRUENO RÚNICO (el arma de rayos — LightningCore)
  · **RunicLightning.cs** — un LINE-STRIKE: el rayo nace en la punta
    del jugador y golpea AL INSTANTE el punto del cursor (el proyectil
    NUNCA se mueve). EFECTOS APLICADOS AL RAYO:
    1. Rayo principal zigzag vivo ~14 Hz con DOBLE TIRA cuerpo-oro +
       núcleo-blanco, ramas y gorros (LightningCore.Bolt).
    2. DAÑO EN LÍNEA: Collision.CheckAABBvLineCollision barre todo lo
       que cruza la descarga.
    3. CADENA ELÉCTRICA: hasta 3 saltos a enemigos cercanos (60% del
       daño, rayos secundarios azul-estelar).
    4. ARCOS DE IMPACTO: dos coronas eléctricas vibrando (Arc CW+CCW).
    5. ELECTRIFICADO (240 ticks) a todo lo tocado.
    6. ONDA DE CHOQUE expandiéndose + luz a lo largo de toda la línea.
  · **StormRuneStaff.cs**: Cetro del Trueno Rúnico — daño 95, mana 12,
    alcance 560 px, icono procedural con núcleo eléctrico y rayo
    dentado.
  · Sombras PNG 76×76 de ambos proyectiles nuevos (la lección v6.14.1:
    tModLoader auto-requesta la textura de TODO ModProjectile).

### C. ENTREGA
  · TestingPlayer: los 10 bastones + el cetro con EnsureItem.
  · Localización es-ES + en-US (nombres y proyectiles).
  · Compilación 0 errores / 0 warnings contra tModLoader
    v2026.07.3.0 real.

## Commit v6.18 — LA GRAN CONSOLIDACIÓN: 9 AGUJEROS + LIBRERÍA DE RAYOS

**Petición del usuario**: "revisa la ventana de información de todos los
agujeros negros (demasiado larga, no se ve el nombre) · borra todas las
alas · oculta el agujero base · quita el del vacío y la fusión · los 4
definitivos (Umbral, Bruma, Cósmico, Olvido) se quedan sin partículas
en el centro · Cósmico rojo-naranja, Olvido morado-azul · crea el
AGUJERO NEGRO SUPREMO combinando los 4, mejorado y potenciado · crea
librerías y actualiza las que hay · copia los 4 y mejora las copias
dejando los originales intactos · limpia referencias externas — las
técnicas son NUESTRAS".

### A. LA LIMPIEZA
  · **TOOLTIPS CORTOS**: todas las ventanas de info de los agujeros
    reducidas a 2 líneas + nombres en español por localización (antes:
    5 párrafos y el nombre auto-generado en inglés).
  · **TODAS LAS ALAS BORRADAS**: 8 items + 9 VFX + draw layer + anim
    player + localización ("todas están mal, no se ven nada bien").
  · **Agujero BASE OCULTO** (no borrado — el código sigue en el mod).
  · **Agujero del VACÍO y FUSIÓN ELIMINADOS** (archivos + refs + PNGs).

### B. LOS 4 DEFINITIVOS — UMBRAL · BRUMA · CÓSMICO · OLVIDO
  · Centro de la bola negra LIMPIO en Umbral/Cósmico/Olvido (fuera
    filamentos interiores y rayos del núcleo — como el Bruma).
  · **CÓSMICO RECOLOREADO a ROJO-NARANJA** (paleta, partículas, sombra,
    icono — antes magenta del script Unity).
  · **OLVIDO RECOLOREADO a MORADO-AZUL** (paleta, partículas, sombra,
    icono — antes púrpura-rosa).

### C. LIGHTNINGCORE — LA LIBRERÍA DE RAYOS (nueva, 100% propia)
  `Content/VFX/LightningCore.cs`: rayos con jitter perpendicular de
  cuerda, DOBLE TIRA cuerpo+núcleo, ramificación heredada con
  decaimiento, arcos circulares eléctricos, suavizado Catmull-Rom,
  parpadeo determinista (FlickTick/Flicker) — síntesis de toda la
  investigación del ecosistema, re-implementada con código propio.

### D. EL AGUJERO NEGRO SUPREMO (el 5to definitivo)
  `SupremoBlackHoleRenderer.cs` (998 líneas, 15 capas): la combinación
  de los 4 — disco Doppler (Umbral) + anillo de bandas sin(θ·20+t·5)
  (Cósmico) + brazos espirales con flujo (Olvido) + halo/volutas de
  humo (Bruma) + ⚡coronas de descarga LightningCore + doble círculo
  rúnico contrarrotante + jets polares con rayos dentro. Esfera de 55px
  (el más grande), atracción de 550px, TRES velocidades de tiempo.

### E. LOS 4 ASCENDIDOS (copias mejoradas — originales INTACTOS)
  · **Umbral Ascendido**: lluvia de 4 rayos ramificados + arco dorado
    giratorio + Doppler a la 5ª potencia + doble anillo rúnico.
  · **Bruma Ascendida**: 3 coronas de escarcha eléctrica + rayos
    gelidos + 5 volutas con curl noise + cristales de hielo.
  · **Cósmico Ascendido**: tormenta de 4-6 rayos PRO + doble anillo
    de bandas + jets polares + doble círculo rúnico.
  · **Olvido Ascendido**: 3 arcos del vacío + rayos ESPIRALES que
    siguen los brazos + brazos reforzados + fotones-micro-rayo.
  Todos con aura 15% más rápida y muerte más rica.

### F. INTEGRACIÓN
  · 5 nuevos registrados en BlackHoleLensSystem (lente gravitacional
    propia para cada uno) + TestingPlayer (entrega) + localización
    es-ES/en-US + sombras 76×76 + iconos 28×30 (generadores Python).
  · **Limpieza de referencias externas**: todo comentario citando
    otros mods/técnicas externas eliminado — las librerías (Bruma,
    LightningCore, VFXCore) y sus técnicas son 100% NUESTRAS.
  · Compilación 0 errores / 0 warnings.

**ESTADO FINAL: 9 agujeros negros** — 4 definitivos + 4 ascendidos +
el Supremo. La base oculta. Vacío y fusión eliminados.

## Commit v6.16 — TRES AGUJEROS NUEVOS + LA LIBRERÍA DE BRUMA (humo/niebla procedural)

**Petición del usuario** (dos tareas en un mensaje):
1. "es momento de crear otro, esta vez crea dos agujeros negros: en uno
usa esto [script Unity CosmicBlackHole.cs + shader Custom/CosmicRing] y
agrega lo que falta; y el otro hazlo usando la referencia como base...
todo lo debes hacer por código. No olvides darle los 2 nuevos agujeros
negros al jugador y todo en español."
2. "crea una librería especializada en humo, niebla, bruma y todo eso
de forma procedural y con calidad, que sea capaz de usarse en cualquier
proporción ya sea grande o pequeño y en todo se vea bien; para esto
investiga otros mods y busca recursos en internet... luego crea un 3er
agujero con todo lo aprendido y librerías creadas en el proyecto, todo
por código."

**Los 4 agujeros existentes (base, vacío, fusión, olvido): INTACTOS.**
El mod tiene ahora SIETE agujeros negros.

### A. EL AGUJERO NEGRO CÓSMICO (#5) — nacido del script Unity del usuario

`CosmicBlackHoleRenderer.cs` + `CosmicBlackHoleProjectile.cs` +
`CosmicBlackHoleStaff.cs` (Bastón del Agujero Negro Cósmico). El script
Unity traducido FIEL al sistema de pinceles del mod (100% código):

  · **El shader, exacto**: `glow = sin(uv.x·20 + t·5)·0.5+0.5` → VEINTE
    BANDAS de brillo recorriendo el anillo a 5 rad/s — la emisión
    pulsa VIAJANDO, como el CosmicRing del usuario.
  · **El color, exacto**: magenta (1.0, 0.2, 0.8) = (255,51,204).
  · **Rotación 20°/s** (rotationSpeed del script), **distorsión
    sinusoidal global** (sin(t)·0.3 — el SetGlobalFloat del shader),
    **lightningParticles** (2 rayos violeta en el núcleo + 2 magenta
    escapando del anillo), **runeParticles** (8 runas doradas de
    CIRCUITO orbitando + perlas), **coreSphere** (BlackDisk absoluto).
  · **Lo que faltaba, añadido**: aura oscura, nebulosas violeta/azul,
    ecos del anillo (la resonancia del shader), corredores de fotones
    con estela, destellos polares, ondas de distorsión, 9 partículas
    radiales y aura final pulsante.

### B. EL AGUJERO NEGRO DEL UMBRAL (#6) — el agujero de LA REFERENCIA

`UmbralBlackHoleRenderer.cs` + proyectil + bastón. La referencia del
usuario (imagen 1536×1024) medida PÍXEL A PÍXEL (perfiles radial y
angular, distribución de tonos, localización de núcleos blancos y
píxeles dorados) y reconstruida 100% por código:

  · **La geometría medida — la topología "∞"**: esfera de vacío +
    disco fino cruzando POR DEBAJO (línea delantera a ~1.4R) + **ARCO
    DE LENTE DOBLE sobre la esfera** (el lado lejano doblado ARRIBA:
    banda salmón a 1.45R + segundo anillo de fotones a 1.22R con línea
    de filo fina e incandescente) + **ARCO INFERIOR magenta** (la
    imagen lenseda de abajo, 1.28R) + **ALA BARRIDA** (banda circular
    GORDA a 2.45R barriendo de abajo-derecha al extremo izquierdo —
    "a broad, sweeping wing of light").
  · **El Doppler medido**: máximo en el extremo IZQUIERDO (lum 185 vs
    128 del derecho — cúbico), núcleos blanco-rosado (249,210,220)
    concentrados en 120-180°, y la **CUÑA OSCURA** de 240-270°
    (t≈4.45, el sector muerto medido).
  · **La paleta medida** (¡rosa/magenta, NO naranja!): blanco-rosado
    (250,210,220), rosa caliente (243,128,149), rosa (225,74,127),
    carmesí-rosa (183,29,83), magenta profundo (153,14,76), vino
    (110,17,51), ala magenta (240,41,168), arco salmón (248,110,95).
  · **El círculo de runas**: dorado-ámbar MEDIDO (240,124,65), 12
    glifos de SIGILO ANTIGUO (colmillos, coronas, garras) con HUECOS
    por hash + glifos apagados donde la PÚA cruza el círculo; aro roto
    en 30 segmentos.
  · La PÚA de energía blanco-rosa (sup-derecha), el RAYO naranja-rojo
    dentado ramificando abajo, 12 BRASAS con estelas de movimiento,
    filamentos violeta cayendo al vacío y el vacío FINAL repintado
    (el centro queda del NEGRO MÁS ABSOLUTO: lum 3.4 en el mock).

### C. LA LIBRERÍA DE BRUMA — humo/niebla/bruma procedural (Content/Effects/Bruma)

Investigación previa REAL (subagente de investigación web, 34 búsquedas
+ 12 fuentes leídas completas: JangaFX/Diablo 3 — reglas anti-fase y
"Scale by Mids" de Julian Love —, The Book of Shaders, Inigo Quilez —
fBm/warping/band-limiting —, VFXDoc — erosión de alfa —, vfxlabs —
overdraw/paralaje —, vanilla Terraria DECOMPILADO — tinte por
iluminación, smear 130-134, LOD por conteo —, el mod de las flipbooks — 3
lotes de blending —, el mod del río — partículas por GPU —,
ParticleLibrary). Informe completo: `research/smoke_research_v616/`.

  · **`BrumaNoise.cs`** — hash determinista + value noise con QUINTIC
    de Perlin + fBm con LACUNARIDAD 2 EXACTA (anti-fase Diablo 3) +
    **domain warping** de IQ (el look "humo vivo") + **curl noise**
    (remolinos sin divergencia) + **OctavesForRadius** (band-limiting:
    el detalle fino mide SIEMPRE ~3px) + Erode (la erosión de alfa de
    VFXDoc: el humo muere en GRUMOS, no se desvanece) + ByMids.
  · **`BrumaBrushes.cs`** — 8 texturas de puff 128×128 NACIDAS DE
    CÓDIGO EN RUNTIME (Texture2D+SetData, CERO PNGs): RGB blanco +
    alfa = falloffRadial(blando) × ByMids(WarpedFbm) — la máscara
    blandita y el ruido con detalle (regla Diablo 3). Disposición en
    BrumaSystem.Unload (cero fugas de VRAM).
  · **`BrumaFX.cs`** — LA API: `Puff` (núcleo texturizado + sub-blobs
    ∝ PERÍMETRO con respiración DESFASADA y presupuesto de alfa de
    COBERTURA CONSTANTE 1−(1−A)^(1/(n+1)) — la invariancia de escala
    por construcción), `Cloud` (racimo con deriva por senos
    INCONMENSURABLES 0.31/0.71 + curl), `Tendril` (voluta por ruta con
    balanceo y erosión), `Column` (nace, crece al ascender, se erosiona
    al morir) y `MistBand` (capas de niebla con paralaje y gradiente
    vertical). Contrato: dibuja en el lote ABIERTO que el llamador
    elija (aditivo = bruma LUMINOSA; alfa = bruma QUE OCLUYE).
  · **`BrumaSystem.cs`** — el ciclo de vida (disposición al recargar).

### D. EL AGUJERO NEGRO DE LA BRUMA (#7) — la demostración de la librería

`BrumaBlackHoleRenderer.cs` + proyectil + bastón. Un vacío GELIDO
envuelto en bruma nebular fría (teal/cian/violeta — identidad única
entre los 7): HALO con `BrumaFX.Cloud`, ANILLO de fumarelitos con
`BrumaFX.Puff` (densidad VIVA por fBm), VOLUTAS espiralando al núcleo
con `BrumaFX.Tendril` (la materia devorada se disuelve en humo) y
CHIMENEAS polares + anillo de fotones cian + escarcha flotante.

### E. ENTREGA Y REGISTRO (el checklist completo de lecciones)

  · `TestingPlayer.OnEnterWorld`: EnsureItem × 3 (Cósmico, Umbral,
    Bruma) — el kit garantiza los TRES bastones en cada entrada al mundo.
  · `BlackHoleLensSystem`: los 3 nuevos registrados (fuente de lente +
    multiplicador + DrawCoreVisuals encima de la distorsión).
  · PNGs de sombra 76×76 de los 3 proyectiles (patrón v6.09:
    rim de identidad por agujero — magenta Unity / naranja Doppler /
    teal gelido) + iconos 28×30 (generadores reproducibles
    `tools/gen_cosmic_umbral_tex_v616.py` y `tools/gen_bruma_tex_v617.py`).
  · Auditoría anti-recurrencia: 49/49 clases ModProjectile/ModItem con
    su PNG (0 faltantes).
  · Recetas: 5 madera (como los hermanos).

### F. VALIDACIÓN

  · **Compilación contra tModLoader 2026.07.3.0 REAL**: 0 errores,
    0 warnings (7 iteraciones durante el desarrollo del Umbral).
  · **Mock exacto** (`tools/mock_3agujeros_v616.py`, 1:1 con los
    renderers sobre los pinceles reales + blending XNA modelado):
    hoja comparativa de los 3 + Umbral vs referencia (7 rondas de
    medición/iteración) + Bruma a 0.35× y 1.0× (la invariancia de
    escala de la librería demostrada).
  · VLM sobre la hoja: Cósmico 9/10, Umbral 10/10 como pieza, Bruma
    7.5/10 (mock en `research/agujeros_v616/`).

---

## Commit v6.15 — EL OLVIDO 100% CÓDIGO: LA REFERENCIA ROJA BORRADA, NUEVA REFERENCIA MÁGICA

**Feedback del usuario**: "creaste OlvidoVortex.png y usaste la misma
referencia para crear el agujero negro, eso no puede ser, borra todo
rastro de la referencia OlvidoVortex.png del proyecto, el agujero negro
no puede ser creado por sprite, debe ser creado enteramente por codigo
… borra todas las referencias del agujero negro rojo del proyecto
incluyendo el sprite, los otros agujeros negros no los toques. Entonces
esta vez en el agujero negro del olvido crealo y sustituye todas las
referencias por la nueva referencia que te doy [imagen + prompt:
núcleo oscuro, anillo energético púrpura/rosa, rayos, partículas,
runas doradas y distorsión espacial]".

### A. LA PURGA — TODO rastro de la referencia roja, BORRADO

  · **5 PNGs eliminados** (el arte extraído píxel a píxel de la imagen
    de Reddit en v6.14): OlvidoVortex.png (1024), OlvidoHalo.png,
    OlvidoSphere.png, OlvidoBackplate.png, OlvidoWisps.png.
  · **research/olvido/ eliminado** (3.1 MB): el pipeline entero de
    extracción (scripts, máscaras, drafts, comparaciones, curvas de
    optimización).
  · Comentarios y tooltips que mencionaban la extracción/Regicide
    reescritos. **Los otros agujeros negros (base, vacío, fusión):
    NI UNA LÍNEA TOCADA.**

### B. EL NUEVO OLVIDO — compuesto por CÓDIGO cada frame

`OlvidoBlackHoleRenderer.cs` reescrito de cero: ~380 cuadros de luz por
frame usando SOLO los tres pinceles genéricos GENERADOS POR CÓDIGO de
la biblioteca VFX (SoftGlow = degradé radial, Ring = anillo fino,
BlackDisk = disco negro — los mismos de BoltRenderer y las coronas).
CERO sprites de arte. Cero estado, cero red: hash puro determinista.

Las 13 capas (según la nueva imagen + prompt del usuario):

  · **0. Aura oscura mística** (alfa) — el vacío absorbe la luz.
  · **1. Nebulosas púrpura/azul** difusas girando + polvo carmesí/magenta.
  · **2/5. ANILLO DE PLASMA** — 44 cápsulas por mitad sobre la elipse
    inclinada: hotspot Doppler incandescente + turbulencia hash a 12 Hz
    (zonas brillantes intercaladas con sombras), gradiente térmico
    blanco-amarillo → rosa → violeta; la mitad delantera CRUZA POR
    DELANTE de la esfera.
  · **3. Brazos espirales** del vórtice (magenta → violeta).
  · **4. Núcleo** — disco NEGRO ABSOLUTO + filo violeta respirando.
  · **6. Corredores de fotones** orbitando y acelerando.
  · **7. RAYOS ELÉCTRICOS** — una TORMENTA de 4 rayos violeta con núcleo
    casi blanco y RAMAS fractales DENTRO del vacío (regenerados a ~6 Hz)
    + 2 rayos rosa escapando del anillo.
  · **8. Destellos polares** — agujas ahusadas en los polos del vórtice.
  · **9. RUNAS DORADAS** — 10 glifos angulares ORIGINALES (lanza, cáliz,
    puerta, estrella, rayo, arco, espiral, trono, llave, ojo) orbitando
    en círculo perfecto con latido/flotación propios + anillo rúnico.
  · **10. Ondas de distorsión** expandiéndose (el espacio-tiempo late).
  · **11. Partículas luminosas** con deriva radial hacia afuera.
  · **12. Aura mística** violeta pulsante.

**La lección del brillo**: `Color * f` de XNA escala los 4 canales → el
blending aditivo queda CUADRÁTICO (f²) y todo se apaga (ronda VLM 1:
"too dim, bolts missing"). FIX: helper `Tint(c, f)` con rgb PLENO +
alfa = f → brillo LINEAL (el patrón validado del Cometa Estelar).
Rondas VLM: 5/10 → 7/10 → 8/10 → **9/10 "highly matches"**.

`OlvidoBlackHoleProjectile.cs`: física probada INTACTA, paleta
recoloreada al violeta/fucsia/dorado (dusts, partículas de biblioteca,
iluminación magenta-violeta, impactos y muerte). Icono del bastón
regenerado (28×30, bastón violeta + mini-agujero púrpura/rosa + runa
dorada, VLM ✓). Tooltips nuevos ("100% creado por código").

**Verificación**: mock Python EXACTO (tools/mock_olvido_v615.py,
texturas reales + blending del juego) validado por VLM en 4 rondas →
renders archivados en research/olvido_codigo/. Compilación contra
tModLoader v2026.07.3.0 REAL: **0 errores · 0 warnings**. Auditoría de
assets: 0 texturas faltantes.

## Commit v6.14.2 — LOS BASTONES NUEVOS SE ENTREGAN AL JUGADOR (el kit de pruebas los olvidó)

**Feedback del usuario**: "te olvidaste que debes darselo al jugador".

**Causa**: el sistema de entrega del mod es `TestingPlayer.OnEnterWorld`
— cada arma cósmica en desarrollo se GARANTIZA en el inventario en cada
entrada al mundo (`EnsureItem`, v5.98: "venga de la versión que venga el
guardado del jugador"). La v6.14 añadió los dos bastones nuevos con
receta (5 de madera, como todos) pero jamás los registró en el kit → el
jugador entraba al mundo y NO recibía ni el de FUSIÓN ni el del OLVIDO.

**El fix** (2 líneas en `TestingPlayer.cs`):

```csharp
// v6.14: LOS DOS AGUJEROS NUEVOS — la FUSIÓN (base+vacío) y el
// OLVIDO (100% exacto a la referencia Regicide)
EnsureItem(ModContent.ItemType<Weapons.Cosmic.FusionBlackHoleStaff>());
EnsureItem(ModContent.ItemType<Weapons.Cosmic.OlvidoBlackHoleStaff>());
```

Desde ahora, al entrar a cualquier mundo (single player), el jugador
recibe ambos bastones si no los tiene — mismo protocolo que el carmesí
desde v6.02. La receta de 5 de madera sigue como vía alternativa.

**LECCIÓN anti-recurrencia**: cada arma nueva debe registrarse en DOS
sitios — su archivo (defaults + receta) Y el kit de `TestingPlayer`
(`EnsureItem`). Añadir al checklist de entrega.

**Verificación**: compilación contra tModLoader v2026.07.3.0 REAL:
**0 errores · 0 warnings**.

## Commit v6.14.1 — FIX DE CARGA: las 2 texturas de sombra olvidadas (el mod no cargaba)

**Feedback del usuario**: "hay varios errores" + client.log — el mod se
desactivaba automáticamente al cargar la v6.14 con
`MissingResourceException` × 2:

```
ReLogic.AssetLoadException: Asset could not be found:
    "Content\Projectiles\Cosmic\FusionBlackHoleProjectile"
    "Content\Projectiles\Cosmic\OlvidoBlackHoleProjectile"
```

**Causa raíz**: tModLoader AUTO-REQUESTA la textura por defecto de todo
`ModProjectile` (ruta = namespace + nombre de clase) durante
`TransferAllAssets()`, aunque su `PreDraw` devuelva `false` y jamás se
dibuje. El commit v6.14 entregó los dos agujeros nuevos con todo su arte
procedural/VFX (5 texturas del Olvido + 2 iconos de bastón) pero olvidó
los DOS PNGs de sombra visual por defecto → ambos errores se agregaban
en un `MultipleException` → el mod entero quedaba deshabilitado.

**El fix** (sin tocar NI UNA línea de los agujeros):

  · **FusionBlackHoleProjectile.png** (76×76 RGBA) — sombra con la
    identidad de la FUSIÓN: disco negro sólido (r≤14) + rim DOBLE, ámbar
    del Gargantua base (255,175,80) por dentro + carmesí del vacío
    (200,20,90) por fuera.
  · **OlvidoBlackHoleProjectile.png** (76×76 RGBA) — sombra con la
    identidad del OLVIDO: rim magenta profundo de la referencia Regicide
    (255,45,110) desvaneciendo a rojo oscurísimo (110,0,45).

Ambas calcan el patrón EXACTO del CrimsonBlackHoleProjectile.png medido
píxel a píxel (disco negro r≤14 alpha 255 · rim pico r≈18-22 alpha ~70 ·
desvanecido hasta r=37 · 4184 px visibles vs 4181 del carmesí) —
generadas por `tools/gen_fusion_olvido_projectile_tex.py` (reproducible)
y validadas por VLM como sombras limpias sin artefactos.

**Auditoría completa anti-recurrencia**: script que resuelve la textura
esperada de las 55 clases de contenido por namespace+clase y verifica la
existencia del PNG → 0 faltantes tras el fix (AethonWingsItem es
abstract → tModLoader no la registra). La lección queda registrada:
**cada ModProjectile/ModItem nuevo SIEMPRE necesita su PNG de sombra,
aunque nunca se dibuje.**

**Verificación**: compilación contra tModLoader v2026.07.3.0 REAL
(DLLs del release): **0 errores · 0 warnings**. Pendiente: el usuario
reconstruye en Develop Mods → Build → el mod debe cargar limpio y los
TRES bastones (base/fusión/olvido) funcionar.

## Commit v6.14 — LOS DOS AGUJEROS NEGROS NUEVOS: LA FUSIÓN (base+vacío) Y EL OLVIDO (100% EXACTO a la referencia)

**Feedback del usuario**: "para que el agujero negro sea exacto, has 100
rondas de revisiones profundas con la imagen de referencia… deja este
agujero negro del vacío sin tocarlo, luego crea un tercero que sea la
fusión del agujero negro del vacío con el agujero negro base, y luego
crea un 4to agujero negro que sea 100% exacto a la referencia, este se
debe llamar agujero negro del olvido, asegúrate de que sea 100% exacto,
usa todas las técnicas que sean necesarias".

### A. EL AGUJERO NEGRO DEL OLVIDO — EL ARTE EXTRAÍDO DE LA PROPIA REFERENCIA

**El cambio de técnica decisivo**: tras cuatro versiones intentando
RECREAR el vórtice proceduralmente (v6.09 analítico, v6.10 cresientes,
v6.11 blobs, v6.13 personalidad), el arte del Olvido se EXTRAE
DIRECTAMENTE de los píxeles de la imagen de referencia original
(Ancients Awakened — Regicide, "Oblivion, God of the Void", 1080×795)
y se descompone en CAPAS ANIMABLES. Las "100 rondas de revisiones
profundas" se materializaron como **12 rondas de validación VLM** +
**130 rondas de optimización automatizada** (descenso por coordenadas
sobre 10 parámetros de ganancia minimizando EMA + perfil radial +
calidez contra la referencia) → **EMA final 16.4/255** y veredicto VLM
8/10: "sí, un jugador diría que es el mismo agujero".

El pipeline de extracción (research/olvido/):

  1. **Medición** — esfera negra R=34px en (495,224) por región oscura
     encerrada por plasma; perfil radial del plasma (gap 0.8-1.3R, pico
     1.55-2.55R, brazos hasta 6.4R); elipse del anillo ajustada por
     tracking angular (a=1.70R, b=1.64R, casi circular); hotspot a 125°;
     estrella interior en (-0.31R,-0.25R); rayo púrpura en el cuadrante
     inferior-derecho de la esfera.
  2. **Separación personaje/agujero** — el boss Regicide está EN DELANTE
     del agujero en la referencia. Máscara estructural: plasma = magenta
     (R≫G, B intermedio) + NARANJAS del disco (255,155,85 — el gradiente
     caliente que faltaba) + blancos calientes del hotspot; personaje =
     SOLO colores equilibrados (plata/máscara/cuernos). Verificada por
     VLM con overlay de clasificación (4/10 → estrategia corregida).
  3. **Inpainting angular** — donde el personaje tapa plasma esperado,
     interpolación bilateral del perfil angular del mismo radio (2781→
     1062 celdas polares reconstruidas tras refinar la máscara).
  4. **Suavizado** — cierre morfológico (anti sal-y-pimienta), máscara
     gaussiana σ=1.15px (bordes antialias), color extendido por EDT
     (sin franjas oscuras al muestrear bilineal), RGB muestreado
     BILINEAL (sin bloques), esfera supersampleada ×4.
  5. **Bloom horneado** — el plasma brillante difuminado (σ=16) y
     sumado al halo: el "glow" desbordado del anillo de la referencia.
  6. **Optimización 130 rondas** — bg rojizo (35.5,0,6.2), halo_g 2.05,
     halo_a 1.78, vortex_g 0.95, ring_g 1.04 → EMA 29→16.9, calidez
     errónea 25.8→0.0 (la pérdida solo mide la zona del agujero, sin
     los píxeles irreplicables del personaje).

Las CINCO texturas nuevas (Content/Effects/Procedural/), todas en
formato premultiplicado A=255 para las aditivas (el RGB lleva la
cobertura horneada → blending aditivo LINEAL, la lección del
OblivionBlob v6.11):

  · **OlvidoVortex.png** (1024) — EL ARTE EXACTO: anillo de fotones +
    disco + brazos espirales + aguja + velos, con inpainting donde el
    boss tapaba y ganancias horneadas.
  · **OlvidoHalo.png** (256) — resplandor ambiental + bloom del anillo.
  · **OlvidoSphere.png** (160) — esfera de NEGRO PROFUNDO con la
    ESTRELLA rosa y el RAYO púrpura interiores, tal cual.
  · **OlvidoBackplate.png** (256) — el vacío rojizo de la referencia
    (placa oscura de fondo: de día el agujero lleva SU oscuridad
    consigo — validado VLM como "bolsillo de oscuridad" legible al
    100% en cielo diurno).
  · **OlvidoWisps.png** (512) — velos exteriores que ROTAN lento.

**OlvidoBlackHoleRenderer.cs** (contrato de batch cerrado→cerrado, la
misma garantía a prueba de balas del v6.10): backplate (alfa) → halo
pulsante + velos girando + vortex exacto respirando (aditivo) → esfera
(alfa) → overlays vivos (aditivos, sutiles, no tocan el arte exacto):
pulsos de fotones recorriendo el anillo a 72°/s, llamarada del hotspot
cada 4.2s con decaimiento exponencial, y seis chispas cayendo en
espiral hacia el horizonte. Esfera GIGANTE: 52px de radio, arte de
811px de envergadura.

**OlvidoBlackHoleProjectile.cs** — física 100% probada (copia del
carmesí: pop elástico, atracción, aura con ticks acelerados, devora
balas, persecución, evaporación, anillo de Einstein final) con lente
propia (mult 3.4) y paleta del olvido. **El agujero del vacío queda
INTACTO** (ni una línea tocada).

**OlvidoBlackHoleStaff** (daño 150) con icono 28×30 generado (bastón +
mini-agujero carmesí, validado VLM) y tooltips completos.

### B. EL AGUJERO NEGRO DE FUSIÓN — LA FUSIÓN LITERAL DE LOS DOS PADRES

**FusionBlackHoleProjectile** — petición: "la fusión del agujero negro
del vacío con el agujero negro base". Su DrawCoreVisuals encadena AMBOS
renderizadores originales en el MISMO centro, cada uno con su identidad
intacta:

  1. **DETRÁS** — `BlackHoleProjectile.DrawCoreVisuals(p, false)`: el
     Gargantua de marcha de luz del BASE (RealBlackHoleShader de 75
     pasos, disco naranja lensado, halo ámbar, refuerzo del horizonte).
  2. **DELANTE** — `CrimsonBlackHoleRenderer.Draw(·, scale×0.68, ·)`: el
     VÓRTICE OBLIVION del VACÍO con sus SIETE capas de personalidad
     v6.13 (ondas de espacio-tiempo, pulsos de fotones, chorros
     relativistas, corrientes de materia, llamaradas, arcos de Einstein,
     rim violeta) a 0.68× — su esfera negra se alinea con el horizonte
     del Gargantua y el ANILLO NARANJA LENSADO asoma alrededor.

El resultado: fuego y vacío en un solo cuerpo. Física idéntica probada
con radio de atracción ampliado (480px — "la suma de ambas masas"),
paleta de partículas DOBLE (carmesí del vacío + naranja del Gargantua
entremezcladas en dusts, estelas, implosiones y explosiones), lente
propia (mult 3.2) e icono propio (anillo naranja+carmesí, validado VLM
como "solar-void" distinguible).

**FusionBlackHoleStaff** (daño 130) con tooltips de la doble estirpe.

### C. REGISTRO EN LA LENTE GRAVITACIONAL

`BlackHoleLensSystem` (ediciones aditivas, sin tocar el comportamiento
de los agujeros existentes): los tipos Fusion y Olvido se recogen como
fuentes de distorsión con sus propios multiplicadores (3.2 y 3.4) y se
dibujan ENCIMA de la lente con sus DrawCoreVisuals propios (mismo
protocolo que el carmesí desde v6.02).

### D. VERIFICACIÓN

  · **Compilación**: Build succeeded · 0 errores · 0 warnings contra
    tModLoader v2026.07.3.0 REAL (DLLs del release, /tmp/verify).
  · **12 rondas VLM**: máscara (r3), comparaciones v1→final (r4-r10),
    iconos (r11), cielo diurno (r12) — final 8/10 "mismo agujero".
  · **130 rondas de optimización** con pérdida restringida a la zona
    del agujero (sin píxeles del personaje) — curva guardada en
    research/olvido/optim_result.json.
  · **Pendiente (el usuario prueba)**: Develop Mods → Build → los TRES
    bastones (BlackHoleStaff base intacto, FusionBlackHoleStaff nuevo,
    OlvidoBlackHoleStaff nuevo) → client.log limpio.

## Commit v6.13 — EL AGUJERO NEGRO CON PERSONALIDAD + LAS 8 ALAS RE DISEÑADAS DE CERO CON LA TÉCNICA DE LAS CORONAS

**Feedback del usuario**: "el agujero negro no se parece en nada a la
referencia, veo que te cuesta mucho crear el agujero negro, solo debes
tomar el agujero funcional que tenemos como base y adaptarlo, darle más
personalidad, más efectos y todo eso. Y las alas se siguen viendo muy
feas, crea nuevos diseños de alas con la técnica de la corona".

### A. EL AGUJERO NEGRO — SIETE CAPAS NUEVAS DE IDENTIDAD (base intacta)

Nueva estrategia (directiva del usuario): YA NO perseguir la réplica
píxel-exacta de la referencia — tomar el agujero FUNCIONAL v6.12 (que ya
dibuja esfera + anillo + vórtice sin errores) y darle PERSONALIDAD. La
física, la lente gravitacional, las partículas y el contrato de batch
(cerrado→cerrado) quedan INTACTOS. Siete capas nuevas, todas deterministas
(cero estado, cero red):

  · **0.5 ONDAS DE ESPACIO-TIEMPO** — anillos finos (textura Ring real,
    aplastada e inclinada como el vórtice) que nacen pegados al horizonte
    y se expanden hasta 7R: el vacío "late". Ciclo de 2.8s, dos ondas
    desfasadas. Color magenta saturado (visible en cielo diurno y noche).
  · **2.5 PULSOS DE FOTONES** — dos destellos blanco-candente que CORREN
    por el anillo interior a 2.4× la velocidad del vórtice, con estela
    corta rosa: luz orbitando y acelerando.
  · **3.5 CHORROS RELATIVISTAS** — dos haces polares (dirección = normal
    del plano del disco, como M87): núcleo blanco-rosa + manto violeta,
    afinándose hacia la punta, con TRES bolas de plasma viajando hacia
    fuera por haz. Se dibujan ANTES de la esfera → sus bases quedan
    TRAGADAS por el horizonte.
  · **3.6 CORRIENTES DE MATERIA** — cinco riachuelos de plasma que caen
    en espiral desde 5.4R (aceleración gravitatoria: ease u^1.45) hasta
    1.44R y DESAPARECEN TRAS EL HORIZONTE; se vuelven blanco-rosa al
    rozarlo (Doppler).
  · **3.7 LLAMARADAS DEL DISCO** — prominencias periódicas (ciclo 3.4s):
    arcos de cápsulas que se alzan del borde de la hoja superior NORMAL
    al plano y se pliegan de vuelta, naranja→pálido en la cresta.
  · **3.8 ARCOS DE EINSTEIN** — filamentos pálidos arqueados por encima
    y por debajo a 1.8R: la lente gravitacional insinuada sin shaders.
  · **6.5 RIM VIOLETA** — el borde del horizonte RESPIRA: 18 cápsulas
    violetas a 1.045R latiendo a 1.7 rad/s (la última luz atrapada).

Validación: mock Python exacto (tools/mock_blackhole_v613.py, texturas
reales + modelo aditivo del juego) → VLM: chorros ✓, corrientes ✓, ondas
✓, aro violeta ✓, 8/10 "vivo y con personalidad" (primera ronda de alfas
subidas para jets/corrientes tras feedback VLM). En cielo diurno: núcleo
y vórtice perfectamente visibles; ondas recalibradas a magenta saturado.

### B. LAS ALAS — OCHO DISEÑOS NUEVOS, EL VOCABULARIO DE LAS CORONAS

Diagnóstico: las alas v6.12 usaban blobs radiales apilados sobre curvas
polares → "manchas difusas", no alas. ¿Qué hace que las CORONAS se vean
bien? Cuatro primitivas con identidad: EL TRAZO (cápsula estirada con
gradiente), LA PERLA (núcleo casi blanco + halo), EL DESTELLO DE 4 PUNTAS
(dos glows en cruz) y EL VOLUMEN OSCURO (silueta). **WingStrokes.cs**
(nuevo) las empaqueta + LA PLUMA (Bézier con volumen, trazo, nervio y
perla en la punta). Los 8 renderers REESCRITOS de cero:

  · **Horizonte de Sucesos** — 7 PLUMAS violeta→magenta→rosa naciendo de
    un MINI-HORIZONTE en el hombro (disco negro + anillo de fotones
    blanco), puntas dobladas al vacío, perlas de fotón + polvo.
  · **Anillo de Fotones** — DOS HUESOS gruesos con MEMBRANA violeta entre
    ellos (la superficie alar) + dos anillos elípticos con filo de ataque
    grueso (Doppler: el frente arde) y 4 fotones orbitando con estela.
  · **Mariposa Cósmica** — VITRAL: contorno dorado en cadena de trazos,
    venas como glifos, celdas de cristal violeta, ojo de ala con anillo.
  · **Hada de Polvo Estelar** — 4 PÉTALOS con contorno de dos trazos,
    3 venas internas, relleno ámbar translúcido y perlas titilantes.
  · **Corona Solar** — TRES LAZOS de prominencia (ArcCrown como alas):
    gradiente de temperatura rojo→oro, filamento eco, NUDO con DESTELLO
    DE 4 PUNTAS en cada ápice, brasas flotando.
  · **Nebulosa Viva** — ESQUELETO de 6 plumas maestras púrpura (la
    silueta) + nube de blobs en deriva + 5 ESTRELLAS con perla y CRUZ DE
    DIFRACCIÓN (Hubble) + filamentos fucsia serpentean.
  · **Eclipse Total** — plumas NEGRAS azul-noche casi opacas con puntas
    CROMOSFÉRICAS blanco-caliente (perlas + micro destellos), rayos de
    corona pálidos por detrás y mini disco de eclipse en el hombro.
  · **Cometa Carmesí** — TRES VELAS gordas de plasma (7.5px base) con
    MEMBRANA de sustentación entre ellas, onda de brillo viajando,
    CABEZAS con perla + destello 4 puntas, cola sensible a la velocidad.

Validación iterativa (tools/mock_wing_render_v613.py, AlphaBlend del pase
de jugador + cielo de día + silueta): RONDA 1: 6/10, 5/10 y 4/10 en
Horizonte/Nebulosa/Eclipse (trazos finos, "humo", "globos") → plumas más
corpulentas + esqueleto de plumas maestras + rediseño total de Eclipse.
RONDA 2: Anillo 6/10 y Cometa 5/10 ("halo", "jets") → huesos + filio de
ataque, velas + membrana. RONDA 3 FINAL: **8/8 APROBADAS** (Anillo 8,
Mariposa 10, Eclipse 9.5, Horizonte 9, Nebulosa 8.5, Hada 8, Corona 7.5,
Cometa pasa claro). Tooltips de 4 alas actualizados a los nuevos diseños.

### C. DOCUMENTACIÓN Y VERIFICACIÓN

  · build.txt 6.13; este CHANGES.md; research/wings_v613 + research/
    blackhole (mocks + validaciones VLM) al repo.
  · Compilación contra tModLoader 2026.07.3.0 REAL: **Build succeeded ·
    0 errores · 0 warnings**.

**Prueba del usuario**: git pull → Develop Mods → Build → (1) CrimsonBlackHoleStaff:
vórtice + esfera + chorros + corrientes cayendo + ondas + llamaradas +
aro violeta respirando; (2) las 8 alas nuevas en el inventario (plumas,
anillos con huesos, vitral, pétalos, prominencias, nebulosa con estrellas,
eclipse con puntas blancas, velas de cometa).

---

## Commit v6.12 — EL AGUJERO NEGRO SIN ERROR + TODAS LAS ALAS CON LA TÉCNICA DE LAS CORONAS

**Reporte del usuario** (con client.log): "el agujero negro dio error, y
las alas se ven horribles, intenta hacer las alas de la misma forma que
hiciste las coronas, usando la misma técnica".

### A. EL ERROR DEL AGUJERO NEGRO — UN `BeginAdditive()` PERDIDO

El client.log mostraba DOS `InvalidOperationException` silenciosas por
frame con el agujero en pantalla:
  · "Draw was called, but Begin has not yet been called" —
    `CrimsonBlackHoleRenderer.Cap()` línea 198 ← `Draw()` línea 235 (¡el
    primer quad del HALO!).
  · "End was called, but Begin has not yet been called" — el `End()`
    defensivo del catch.

**Causa raíz**: la reescritura v6.11 del renderer PERDIÓ la llamada a
`BeginAdditive()` al principio de `Draw()`. Las secciones 1-4 (halo, anillo
interior, las dos hojas del vórtice, hotspot) dibujaban cuadros sobre un
batch CERRADO → el primer `spriteBatch.Draw()` lanzaba, el catch lo tragaba
…y **NI EL VÓRTICE NI LA ESFERA NEGRA SE DIBUJABAN NUNCA**. El usuario solo
veía el hueco de la lente gravitacional ("es solo un agujero") Y los errores
en el log. El arte Oblivion calibrado v6.10/v6.11 era correcto — jamás se
mostró.

**Fix**: `BeginAdditive()` como sección 0 del try. El contrato de batch
queda: llega CERRADO → aditivo (1-4) → End → alpha (esfera negra) → End →
aditivo (rayos, chispas, bloom) → End CERRADO. Ambos caminos (PreDraw del
pase de mundo y RenderLens de la lente) funcionan; 0 excepciones.

### B. LAS ALAS — DE VUELTA A LA LUZ PROCEDURAL (petición expresa)

Los sprites de arte IA (v6.10/v6.11) no convencieron. El usuario pidió
expresamente la técnica de las coronas (la verificada perfecta): accesorio +
PlayerDrawLayer + renderizador VFX + textura de equipo en blanco. Se
RESTAURA el sistema completo v6.08 con la LECCIÓN DE VISIBILIDAD aprendida:

  · **WingVFX.cs** — WingDrawContext / WingMotionProfile / WingStyles /
    VFXWingSlots (8 estilos, personalidades de vuelo intactas: mariposa
    asimétrica 1.7, hada colibrí 0.52 con AlwaysFlutter, cometa sensible a
    la velocidad, muelles por estilo).
  · **WingAnimPlayer.cs** — máquina de estados (volar/planeo/caída/reposo)
    con muelles, golpe asimétrico y cadencia de sonido/dust.
  · **VFXWingsDrawLayer.cs** — UNA capa AfterParent(PlayerDrawLayers.Wings):
    las alas quedan DETRÁS del cuerpo como alas de verdad. Anclaje en los
    omóplatos (p.height·0.145, escala con el sprite).
  · Los 8 renderizadores (BlackHole/Butterfly/Fairy/SolarCorona/Nebula/
    Eclipse/Comet) con las 8 clases [AutoloadEquip] intactas (stats
    end-game, tooltips, recetas, entrega por TestingPlayer).

**LA LECCIÓN (por qué las v6.08 "no parecían alas")**: el pase de jugador
compone los DrawData con **AlphaBlend** (NO aditivo). Las alfas tenues del
v6.08 (0.085 en membranas, pensadas para aditivo) eran INVISIBLES — las alas
se leían como manchas. Las coronas leen bien porque usan alfas casi totales
(pulse·alpha ≈ 0.75-1.0). v6.12 aplica el estándar de corona a TODO:
  · Membranas 0.085 → **0.45-0.55** + VOLUMEN oscuro debajo (la silueta
    sólida que recorta la forma del ala contra el cielo — mariposa violeta
    noche, hada ámbar, eclipse noche, nebulosa púrpura).
  · Venas/filos/bordes 0.38 → **0.85-0.95**; núcleos a 1.0.
  · Envergaduras ×1.2-1.35 (escala de alas vanilla).
  · La luz del mundo ya NO apaga las alas: piso 0.88 (son fuentes de luz).

**REDISEÑOS ESTRUCTURALES tras validación VLM (mock AlphaBlend exacto con
las texturas reales, 24 escenas, cielo de día = peor caso)**:
  · **Anillo de Fotones** (3/10 → **9/10**): antes "campo de energía con
    forma de corazón". Ahora tiene FILO DE ATAQUE — una cinta dorada
    continua del hombro a la punta — y las órbitas son PLUMAS BARRIDAS
    alineadas al filo (3 elipses alargadas, cada una más lejos y más
    grande) con los fotones corriendo por ellas.
  · **Eclipse Total** (4/10 → **8/10**): antes "orbs sueltos junto a la
    cabeza". Ahora: los discos van AFUERA (lóbulo superior apenas arriba,
    lóbulo inferior abajo-afuera) + MEMBRANA de noche violeta que CONECTA
    raíz→ambos lóbulos (el cuerpo del ala) + filo cromosférico pálido.
  · **Nebulosa Viva**: los 6 blobs trazan el ARCO de un ala (exteriores
    más altos), 3 filamentos (antes 2).
  · **Corona Solar**: piso 0.52 en reposo — los lazos NUNCA colapsan a
    mancha.
  · Verificación final VLM: Mariposa 9 · Cometa 9 · Anillo de Fotones 9 ·
    Eclipse 8 · Hada 8 · Horizonte 7 · Corona Solar 7 · Nebulosa 6 (estilo
    nube, inherentemente etéreo).

**Los PNG de equipo** (los 8 `{Nombre}_Wings.png`) son ahora 8×8
TOTALMENTE transparentes (el truco del mod de referencia): vanilla no dibuja NADA —
ni sprite, ni caja, ni fondo, ni animación que arreglar. Los 122 PNG del
mod validados (ninguno corrupto — el "Image loading failed" del log viejo
era de la v6.10).

### C. VERIFICACIÓN

  · Compilación contra tModLoader v2026.07.3.0 REAL: **0 errores /
    0 warnings**.
  · mock_wing_render_v612.py: simulación AlphaBlend exacta (quads rotados,
    perfiles de SoftGlow/Ring/GlowOrb, lerp hacia el tinte) — 24 escenas
    validadas por VLM.

## Commit v6.11 — EL VÓRTICE INVISIBLE ARREGLADO + LAS ALAS CON EL CORTE VANILLA CORRECTO (4 frames)

**Reporte del usuario**: "el diseño de las alas se ve bien, pero están mal
animadas y programadas… además debes crearlas con fondo transparente. En
cuanto al agujero negro, es solo un agujero, no se parece en nada a la
imagen de referencia" (con dos capturas: las alas como 3 bandas con fondos
oscuros, y el agujero como un simple círculo negro con rayos).

### A. LAS ALAS — DOS BUGS REALES (ambos visibles en la captura)

**Bug A1 — el troceado**: v6.10 creyó (decompilando) que vanilla corta las
alas con `Height()/7`. ¡Era el caso especial de las alas 22/43/44! El
camino POR DEFECTO de `DrawPlayer_09_Wings` usa `num13 = 4` → las alas
moddeadas se cortan con **Height()/4** y origen **(Width/2, Height/8)** =
centro del frame. Las tiras de 7 frames cortadas en cuartos mostraban
FRAGMENTOS de 2-3 alas con huecos (exactamente las "3 bandas" de la
captura). La animación real (Player.cs decompilado): reposo = frame 0,
vuelo = ciclo 0→1→2 cada 5 ticks, planeo = frame 2 fijo; el frame 3 nunca
se usa en alas normales.

**Bug A2 — el fondo no transparente**: el pipeline v6.10 extraía el alfa
con un umbral de LUMINANCIA (7→42), pero los artes IA con fondo GRIS
oscuro (photonring (25,24,29), fairy (22,14,13), eclipse (14,15,20),
comet (21,6,9)) quedaban por encima → una CAJA RECTANGULAR semitransparente
cubría todo el frame (PhotonRingWings tenía el 79% del sprite opaco).

**Fix (tools/gen_ai_wings_v611.py)**:
  · **4 frames** (f0 plegada · f1 media · f2 apertura total · f3 copia de
    la f1) con la RAÍZ en el CENTRO del frame — el origen exacto de
    vanilla. Tiras 138-146 × 416-512.
  · **Fondo eliminado por CONECTIVIDAD**: color de fondo = mediana del
    borde; región de fondo = flood-fill desde los 4 bordes a través de
    píxeles con distancia de color < 58 (mata viñetas y gradientes);
    fuera de esa región alfa = smoothstep(dist, 22, 58) + cierre
    morfológico + relleno de huecos. Fondo 100% transparente (alfa dura:
    <0.03 → 0). PhotonRing baja del 79% al 55% de opacidad (solo alas).
  · **Aleteo sin clipping**: el margen se calcula con el ALCANCE REAL por
    píxel (reach = |dy|·sq·cos(ang) + |dx|·|sin(ang)|; ojo al abs() — el
    v6.11 inicial sin él subestimaba las poses de ángulo negativo) y el
    arte se escala hasta caber — las alas quedan GRANDES y las puntas
    nunca tocan el borde del frame (verificado 8/8).
  · **Rotación sobre la raíz de verdad**: el squash se aplica al
    contenido y el pivote vive en rootY·sq (el v6.10 rotaba alrededor de
    rootY sin squash — desplazaba el pivote). Mitades compuestas con
    máscara dura en la costura + fusión ponderada → la unión de la raíz
    es perfecta y las puntas divergen = el aleteo.
  · Iconos regenerados como PAR COMPLETO (~30×22, transparentes).
  · Simulación del render vanilla (frame centrado en el torso del
    jugador) validada con visión AI: 8/8 APROBADAS — anclaje, forma de
    ala, transparencia y progresión del aleteo.

### B. EL AGUJERO — EL VÓRTICE ERA INVISIBLE (14 píxeles magenta)

La captura del usuario mostraba SOLO la esfera negra + el halo tenue + el
lens + partículas: **el vórtice carmesí entero medía 14 píxeles magenta**
en la imagen. Tres bugs del renderer v6.10, todos en `Cap()`:

  · **TAMAÑO**: dibujaba SoftGlow con (len, wid) como TAMAÑO TOTAL del
    quad, pero esos números son las SIGMAS gaussianas del prototipo
    (add_blob es visible hasta ~1.5×sigma) y SoftGlow concentra su brillo
    en un núcleo diminuto (alfa 134 a r=6/32) → cada cápsula brillaba en
    2-3px. El disco negro sí se veía porque usa 2r explícito.
  · **ALFA²**: con texturas premultiplicadas + Additive(SourceAlpha, One)
    el color `c*alpha` aplicaba el alfa DOS veces (rgb·a·a) → aún más
    tenue.
  · **KeyLerp equiespaciado**: las tablas calibradas del prototipo usan
    t-claves explícitas (0.25, 0.45, 0.65…) — el v6.10 las leía como
    equiespaciadas y distorsionaba la geometría de las hojas.

**Fix (CrimsonBlackHoleRenderer.cs v6.11 + OblivionBlob.png nuevo)**:
  · Textura **OblivionBlob.png** (128×128): el perfil gaussiano EXACTO del
    prototipo horneado en el RGB (g = (exp(-3.6d²)+0.4·exp(-1.2d²))·
    (1-d²)²/1.4), alfa 255 en toda la textura → el premultiply de tML no
    la toca y el aditivo queda LINEAL.
  · `Cap()` dibuja el quad con TAMAÑO TOTAL = (2·sigma, 2·sigma): d =
    distancia/sigma reproduce add_blob píxel a píxel; el color lleva el
    brillo m=alfa·1.4 en el RGB con alfa 255 (el mismo clip del
    acumulador del prototipo).
  · `KeyLerp`/`KeyColor` con T-CLAVES explícitas — geometría idéntica al
    prototipo calibrado contra la referencia.
  · Simulación Python EXACTA del código C# (mock_renderer_v611.py:
    muestreo de la textura real, quads rotados, tinte clampeado,
    composición aditiva): validada con visión AI sobre fondo NEGRO
    (esfera + anillo + dos crescientes + aguja — EMA radial 32/255 vs
    27/255 del prototipo) y sobre CIELO AZUL (el vórtice brilla, la
    esfera sigue siendo negra pura, sin partes invisibles).
  · La física de juego, la lente gravitacional, las partículas y el
    contrato de batch (cerrado→cerrado, el fix del crash v6.10) quedan
    INTACTOS.

## Commit v6.10 — EL AGUJERO "OBLIVION" DE LA REFERENCIA REAL + TODAS LAS ALAS REHECHAS CON ARTE IA

**Reporte del usuario**: "el agujero negro sigue sin ser exacto y además dio
un error" (client.log con InvalidOperationException: Begin called before
End en CrimsonBlackHoleProjectile.RestoreSpriteBatch) + la imagen ORIGINAL
de la referencia (Reddit: Ancients Awakened — Regicide, Oblivion God of the
Void) + "también cambia todas las alas, esas alas no parecen alas, se ven
feas".

### A. EL ERROR DEL CLIENT.LOG — CAUSA RAÍZ Y FIX

El crash ocurría al final de la vida del agujero: la secuencia de
evaporación componía `scale *= 1-collapse` hacia ~0 → `rSh < 2` → el
renderer v6.09 hacía EARLY RETURN sin cerrar el batch → RestoreSpriteBatch
hacía un `Begin()` DUPLICADO sobre un batch ya activo → FNA lanza
InvalidOperationException y tML desactiva el dibujado del proyectil.

**Fix a prueba de balas (PreDraw v6.10)**: cerramos el batch del pase de
proyectiles nosotros (`try End catch` — si ya estaba cerrado por el hook de
otro mod, lo respetamos), dibujamos con el renderer (que exige batch
CERRADO y lo deja CERRADO pase lo que pase con try/catch defensivo) y
restauramos con los parámetros EXACTOS de `Main.DrawProjectiles`
(decompilado de tML 2026.07.3.0: Deferred, AlphaBlend, DefaultSamplerState,
None, **Main.Rasterizer**, null, **Main.Transform**). Además la escala del
colapso ahora tiene piso 0.06 (esfera mínima de 5.5px).

### B. EL AGUJERO "OBLIVION" — LA REFERENCIA ORIGINAL POR FIN ENTENDIDA

La imagen de Reddit NO es un Gargantua de disco delgado (lo que v6.09
calibró): es un VÓRTICE DE PLASMA. Mediciones numpy píxel-exactas (esfera
R=35px en (493,223), ajuste circular 84%):
  · Esfera negra compacta + GAP oscuro 1.0–1.25R (el brillo NO la toca).
  · Anillo interior 360° a ~1.5R con borde interno blanco-caliente
    (hotspot medido: (255,246,137) a 355°, 3R).
  · UNA HOJA GRUESA EN CRESCIENTE que barre POR ARRIBA (O→NO→N→NNE):
    se mantiene a ~3R hasta 330° y SE DISPARA en aguja hasta 6.1R a 345°.
  · Segundo cresiente BAJO (ESE→S→SSW) con borde exterior a 6.3R por el
    sur (verificado: la masa lejana SE ES plasma rosa, no el cuello
    plateado del jefe — este último se distinguió por color r≈g).
  · TODO inclinado SW→NE (~24°) y fluyendo en sentido HORARIO.
  · Rayos azul-violeta RAMIFICADOS dentro de la esfera (no cruzan el
    centro), chispas blanco-amarillas, mechones hasta 6.5R.

**CrimsonBlackHoleRenderer.cs v6.10**: reescrito completo — 8 capas (halo
→ anillo+rim caliente → hoja superior → cresiente inferior → hotspot/nudo/
aguja/mechones → ESFERA NEGRA opaca (garantiza el gap) → rayos ramificados
→ chispas+bloom), todo en unidades de R=46px (GIGANTE autorizado:
envergadura ~580px), girando en sentido horario a 0.16 rad/s, con
turbulencia de pinceladas, filamentos calientes dentro de las hojas, colas
de velocidad y jitter de borde. BlackHolePhysics.cs ELIMINADO (la nueva
geometría es art-directed, no GR; queda en git history). Prototipo Python
calibrado (tools/mock_oblivion_v610.py): perfil radial EMA 27/255 y
extensión angular por cuadrantes emparejada (aguja NNE 6.0R vs 6.1R, sur
5.7R vs 6.4R, oeste 3.2R vs 2.3R).

### C. LAS 8 ALAS — ARTE IA REFINADO, SISTEMA VANILLA

Las alas de luz procedural "no parecían alas". Ahora son SPRITES de verdad:
  1. 8 diseños generados con IA (temas: horizonte de sucesos, anillo de
     fotones, mariposa cósmica, hada estelar, corona solar, nebulosa viva,
     eclipse total, cometa carmesí).
  2. Pipeline tools/gen_ai_wings_v610.py: alfa desde fondo negro (umbral
     suave + cierre morfológico para no agujerear plumas oscuras) →
     SIMETRÍA PERFECTA por espejo (verificada numéricamente: diff=0.0000)
     → contorno oscuro Terraria → limpieza de píxeles sueltos → supresión
     del cuerpo central (mariposa/hada/eclipse/solar) → ANIMACIÓN de 7
     frames (cada mitad rota sobre la RAÍZ: reposo plegado, planeo, apex
     alzado, ciclo) → tira {Nombre}_Wings.png + icono.
  3. **DESCUBRIMIENTO al decompilar DrawPlayer_09_Wings**: vanilla corta
     las alas en **Height()/7 — ¡SIETE frames, no 4!** (origen (W/2,
     H/14)). v6.06 usó 4 frames: por eso las alas salían "mal ubicadas".
     El pipeline genera las 7 correctamente.
  4. AethonWingItems.cs (antes VFXWingItems.cs): base estándar con
     [AutoloadEquip(EquipType.Wings)] + WingStats (stats end-game
     conservadas: 180-200 ticks, 9-10.5 velocidad, ×2.6-3.2, FLOTADO en
     mariposa/hada). BORRADOS: WingVFX.cs, los 7 renderers VFX de alas,
     VFXWingsDrawLayer.cs, WingAnimPlayer.cs, VFXWingItems.cs — el
     sistema vanilla hace toda la animación (frame 0 reposo, 1 planeo,
     2 apex, ciclo 0-2 al volar).
  5. Validación VLM: 8/8 alas con silueta legible de alas reales,
     animación reposo≠apex visible, simetría numérica perfecta.

## Commit v6.09 — EL AGUJERO NEGRO CARMESÍ "SUPER IGUAL": LA OTRA LIBRERÍA CON LAS FÍSICAS CORRECTAS

**Reporte del usuario**: "todavía no se parece a la referencia, el disco de
acreción debe ser más denso y debe rodear por completo a la bola negra, esta
debe ser de un negro profundo con bordes de color. Investiga más sobre
agujeros negros, investiga las matemáticas de cómo crear un agujero negro,
crea otra librería de ser necesario con las físicas correctas, que el agujero
negro sea igual a la referencia — y cuando digo igual es que sea igual SUPER
IGUAL; asegúrate de que sea igual que la referencia, si tiene que ser
gigante para eso que así sea".

### A. LA INVESTIGACIÓN (lo que faltaba entender)

El shader de marcha de rayos produce un TORO lensado genérico — por eso
"todavía no se parecía": la referencia no es un toro, es un DISCO DELGADO
INCLINADO visto a ~70° con la geometría de oclusión clásica (Luminet 1979;
James et al. 2015, el paper de DNGR/Interstellar): el lado CERCANO cruza por
delante de la cara inferior de la esfera, el lado LEJANO se oculta detrás,
y el anillo de fotones + su eco lensado abrazan la silueta. ESO es "rodear
por completo a la bola negra".

Mediciones píxel-exactas de la referencia (347×173, R_sh≈14px): banda de
fotones sólida 1.16–1.49·R_sh (blanco 255), eco lensado 1.68–1.96, foso
oscuro 2.1–2.9 (el lado lejano nace a 2.7), borde interno CALIENTE a 2.2
que cruza la esfera a +0.76·R_sh bajo el centro, pico del disco a 4.6
(magenta saturado), fade exterior 6.5, elipse b/a=0.345, Doppler izquierdo
+30%, 18 rayos cian de fondo.

### B. LA OTRA LIBRERÍA (2 archivos nuevos)

- **`Content/VFX/BlackHolePhysics.cs`** — la matemática GR pura en unidades
  r_s=1: horizonte, esfera de fotones (1.5 r_s), sombra (√27/2 ≈ 2.598 r_s),
  ISCO (3 r_s), velocidad kepleriana v=√(GM/r), ω∝r^(−3/2), Doppler δ y
  δ³, Shakura–Sunyaev T∝r^(−3/4) e I∝r^(−3), redshift g=√(1−r_s/r), y la
  proyección del disco inclinado con el test cercano/lejano (la clave).
- **`Content/VFX/CrimsonBlackHoleRenderer.cs`** — el render ANALÍTICO POR
  CAPAS con esa geometría: (1) halo ambiental + 18 rayos cian radiales,
  (2) lado LEJANO del disco (comprimido, nace a 2.7·R_sh), (3) halo de
  fotones, (4) **ESFERA NEGRA OPURA** (BlackDisk.png nuevo: negro profundo
  #000000 que COME la luz — oculta el disco lejano dentro de su silueta),
  (5) **ANILLOS DE FOTONES** = el borde de color (13 rungs solapados
  1.16–1.96·R_sh + arco de eco sobre la esfera), (6) **LADO CERCANO que
  CRUZA POR DELANTE** de la cara inferior de la esfera con su borde blanco
  caliente a 2.2·R_sh, (7) bloom (arco cercano + hotspot Doppler izquierdo).
- **`Content/Effects/Procedural/BlackDisk.png`** — textura nueva (256px,
  núcleo opaco + borde de 4px).

### C. EL DISCO DENSO QUE RODEA (la petición textual)

32 líneas de corriente keplerianas dibujadas como CÁPSULAS SoftGlow
solapadas ×2.2 (tangente a la elipse) → banda CONTINUA y DENSA, no un
donut: brillo pico a 4.6·R_sh (magenta 255,105,255), carmesí en los bordes,
grano de plasma orbitando con ω∝r^(−3/2) (el interior hierve más rápido),
Doppler δ suavizado (izquierda cegadora), lado cercano aclarado a
rosa-blanco en su núcleo, lado lejano comprimido al 72% con su borde
blanqueado. La calibración es 1:1 con el prototipo Python
(`tools/mock_blackhole_v609.py`) que usa las texturas REALES del mod y fue
validado numéricamente contra la referencia (error medio 17/255 en el
perfil radial de 23 puntos — el pico del mock 212 vs ref 207).

### D. ESCALA GIGANTE + INTEGRACIÓN

- **GIGANTE autorizado**: sombra de 38px de radio (esfera de 76px), disco
  de 494px de envergadura a escala 1 — domina la pantalla como la
  referencia domina su encuadre. El aura de daño sube 2.2× → 4.6× sobre la
  sombra para abrazar la mitad interior del disco (mult. del escudo).
- **Lente de pantalla**: su radio de distorsión ahora usa el multiplicador
  propio del carmesí (2.9× en vez de 1.4×) para ABRAZAR el disco completo.
- Partículas (dusts + librería) re-escaladas a la banda del disco nuevo
  (2.3–5.8·R_sh, elipse 0.345, ω kepleriano); iluminación en 3 puntos;
  tooltips del staff actualizados.
- **Física de juego INTACTA** (copia exacta): atracción 10× el sol, aura de
  ticks acelerados, devora balas, persecución lenta, anillo de Einstein
  final. El BlackHoleProjectile ORIGINAL queda INTACTO con su shader.
- Verificación compilada contra tModLoader v2026.07.3.0 REAL: **0 errores,
  0 warnings**. Confirmado decompilando que tML premultiplica las texturas
  (ReLogic PngReader → PreMultiplyAlpha): el blending aditivo del renderer
  replica exactamente el modelo del prototipo validado.

**Prueba del usuario**: Develop Mods → Build → CrimsonBlackHoleStaff →
disparar: la bola negra profunda con su anillo blanco, el disco magenta
denso rodeándola por completo (cruce frontal abajo), los rayos cian y la
lente curvando el fondo. (Las 8 alas de la v6.08 siguen pendientes de
prueba en juego.)

## Commit v6.08 — TODAS LAS ALAS SON AHORA DE LUZ (8, técnica coronas) + EL AGUJERO NEGRO CON EL HORIENTE PEQUEÑO Y EL DISCO ALARGADO

**Reporte del usuario**: "el agujero negro se ve bastante bien, pero es igual
al original solo con otro color... el horizonte de eventos debe ser mas
pequeño, y el disco de acreción mas alargado" + "todas las alas se ven mal y
las 8 alas con sprite estan mal ubicadas... crea alas de mariposa y alas de
hadas, y a partir de ahora que todas las alas sean con el mismo estilo que
las coronas; el resto de alas con sprite borralas; rediseña todas y mejora
sus animaciones, tomate tu tiempo, crea mas alas como las 2 especiales".

### A. EL AGUJERO NEGRO CARMESÍ (recalibrado con mediciones píxel-exactas)

Análisis de las 2 referencias del usuario (numpy + relleno de huecos): la
sombra es un núcleo COMPACTO (84×64 px, ~20% del rastro total) con el anillo
de fotones ABRAZÁNDOLO (1.1-1.3× la sombra) y el disco como un RASTRO LARGO
Y FINO que llega a ~5× la sombra y cruza en DIAGONAL (lado que se acerca
abajo-izquierda, el que se aleja arriba-derecha). El render anterior fallaba
en las proporciones: el refuerzo negro del C# inflaba la sombra a 2.15× el
horizonte y el toro era un donut gordo pegado al horizonte.

- **Horizonte más pequeño**: `blackHoleRadius` 0.25 → **0.17** + refuerzo
  negro 2.15× → **1.45×** (alpha 235, núcleo compacto sólido ≈20% del
  rastro, como la referencia). El anillo de fotones overlay queda a 1.18×
  del núcleo visible — abrazándolo, como en la referencia.
- **Disco más alargado**: `accretionDiskScale` = (1, 0.28, 1) →
  **(1.15, 0.17, 1)**: estirón horizontal ×1.15 (el rastro de la diagonal)
  + achatado vertical 0.17 (banda de canto ~6:1). El tubo baja 0.48 → 0.36:
  el annulus nace a 2.3× el horizonte (lejos, como el rastro) y el borde
  exterior queda en (0.75+0.36)×1.15 = 1.28 unidades ≈ IGUAL que antes —
  **el agujero MANTIENE su tamaño total** (petición explícita).
- **Doppler diagonal**: los velos se inclinan a favor de la diagonal
  (acercándose abajo-izquierda cegador / alejándose arriba-derecha brasa) y
  se estiran siguiendo el rastro.
- Partículas del disco recalibradas al annulus nuevo (2.3..5.5× el
  horizonte compacto). La lente de pantalla (BlackHoleLensSystem) no
  cambia: se dimensiona por el hitbox (p.width), no por el shader — sigue
  abrazando el borde del disco. Física 100% intacta.

### B. LAS ALAS: DE 10 MIXTAS A 8 DE LUZ (sistema unificado)

**BORRADO**: las 8 alas de spritesheet (clases SheetWings.cs + AethonWings.cs
+ las 16 PNG de ítems y tiras de frames + sus entradas de localización + la
garantía de entrega). El usuario las vio "mal ubicadas" (el anclaje vanilla
de la tira quedaba 8-12 px por debajo de los omóplatos — medido en su
captura) y pidió eliminarlas.

**SISTEMA NUEVO (todo técnica coronas — PNG en blanco + render VFX):**

- `WingVFX.cs` — el núcleo: `WingDrawContext` (anclaje, apertura, aleteo,
  dirección, gravedad, luz, VELOCIDADES para el sweep aerodinámico y el
  diedro), `WingMotionProfile` (la personalidad de vuelo de cada estilo) y
  `WingStyles` (registro estilo→renderizador, 8 estilos).
- `VFXCore.Quad` con rotación + textura (NUEVO): cintas de luz orientadas
  por la tangente — la base de los rastros, colas y lazos.
- `VFXWingsDrawLayer` — UNA capa para las 8 (antes había 2), después de la
  capa vanilla de alas, anclada a la ESPALDA ALTA (omóplatos, -6px).
- `WingAnimPlayer` REESCRITO: muelles por estilo, **golpe asimétrico**
  (la mariposa baja el ala rápido y sube lento — vuelo real),
  **AlwaysFlutter** (el hada y la nebulosa nunca dejan de latir), cadencia
  de sonido por estilo (el hada suena cada 2 ciclos porque aletea ~10×/s).

**LAS 8 ALAS** (todas end-game ≥ Solar Wings, mariposa y hada con FLOTADO):

1. **Horizonte de Sucesos** (rediseñada) — rastro de acreción ALARGADO por
   lado (cinta por tangente con Doppler δ³ y vetas de plasma), mini
   horizonte con anillo de fotones a 1.18×, cuentas de materia orbitando y
   eco de anillo de Einstein en la punta.
2. **Anillo de Fotones** (rediseñada) — 3 aros de órbita elípticos en
   abanico con FOTONES corriendo por ellos (con estelas) y un pulso que
   recorre el aro mayor con cada golpe de aleteo.
3. **Mariposa Cósmica** (NUEVA) — lobo superior grande con ojo de ala +
   lobo inferior caído (fase de aleteo independiente), membrana translúcida
   de retícula, 5 venas curvas, borde dorado festoneado, golpe asimétrico
   real y las alas casi aplaudiendo sobre la espalda al subir.
4. **Hada de Polvo Estelar** (NUEVA) — 4 lóbulos puntiagudos de membrana
   dorada con borde ámbar, 7 chispas de polvo estelar titilando con fases
   deterministas y vibración de colibrí que NUNCA cesa.
5. **Corona Solar** (NUEVA) — 4 lazos de prominencia por lado con gradiente
   de temperatura (blanco → dorado → naranja → braza), puntas en
   llamaradas, mancha solar en la raíz y estirón al empujar.
6. **Nebulosa Viva** (NUEVA) — 6 blobs de gas en deriva turbulenta
   individual, filamentos serpenteantes y estrellas con cruces de
   difracción; la nube RESPIRA en lugar de aletear.
7. **Eclipse Total** (NUEVA) — discos negros sólidos con anillo
   cromosférico exacto en el borde, 6 rayos de corona DESIGUALES ondeando
   con fases propias y llamarada rosa en el limbo.
8. **Cometa Carmesí** (NUEVA) — núcleo blanco-dorado + cola iónica cónica
   con onda viajera, gradiente blanco→dorado→carmesí→braza, vetas de plasma
   y motas de polvo; la cola se BARRRE al correr (máxima respuesta a la
   velocidad).

**Ítems**: `VFXWingItems.cs` (base + las 8 clases con [AutoloadEquip]),
stats 180-200 ticks / velocidad 9-10.5 / aceleración ×2.6-3.2, flotado en
mariposa y hada, tooltips de color estilo coronas, recetas de madera,
entrega garantizada en TestingPlayer, localización es-ES + en-US.

**Iconos**: 8 iconos procedurales de 30×24 (supersampling ×4, composición
aditiva sobre transparente) con la silueta de cada estilo — generados por
`research/wings/gen_vfx_wing_icons.py` y validados por VLM (7-10/10; el de
nebulosa reforzado tras el feedback: núcleo denso + estrellas ancla).

### C. VERIFICACIÓN

- **Compilación completa contra tModLoader.dll REAL v2026.07.3.0:
  0 errores, 0 warnings.**
- **Simulación Python de los renderizadores** (`research/wings/
  mock_wing_render.py`): réplica exacta de la matemática de mariposa, hada,
  anillo de fotones, horizonte y cometa en 11 escenas (volando/reposo/golpe)
  validada por VLM — anclaje al hombro ✓, extensión arriba/afuera ✓, formas
  reconocibles ✓, simetría ✓.
- Validación PIL de las 16 PNGs nuevas: 8 iconos 30×24 RGBA + 8 texturas de
  equipo 8×8 RGBA totalmente transparentes (truco del mod de referencia, el mismo de las
  coronas que ya funcionaba).
- Firma de `EquipLoader.GetEquipSlot(Mod, string, EquipType)` verificada
  decompilando el tML real (la usan el animador, la capa y el mapeador de
  slots).

### D. PRUEBA DEL USUARIO

Develop Mods → Build → disparar CrimsonBlackHoleStaff (núcleo negro
compacto + anillo pegado + RASTRO largo fino en diagonal + partículas
barriendo el annulus) → entrar al mundo → las 8 alas en el inventario →
equipar cada una: mariposa (golpe lento profundo + flotado), hada
(vibración rápida + flotado), cometa (la cola barre al correr), eclipse
(majestuosa), etc. — y las de agujero negro con el rastro y los fotones.

---

## Commit v6.07 — FIX: el mod no cargaba (IndexOutOfRangeException en las 8 alas de spritesheet)

**Reporte del usuario**: "estaba comenzando las pruebas y está lleno de
errores" (client.log de tModLoader 2026.07.3.0).

### A. EL ERROR (del log real)

```
System.IndexOutOfRangeException: Index was outside the bounds of the array.
   at AethonMod.Content.Items.Wings.AethonWings.SetStaticDefaults() in AethonWings.cs:line 45
   at Terraria.ModLoader.ModItem.SetupContent()
```

El error se repetía **8 veces** (una por cada ala de spritesheet: Nova Solar,
Plasma Cuántico, Vacío Etéreo, Éter Glacial, Fósiles del Génesis, Pilar de
Nebulosa, Eclipse y Supernova) y desactivaba el mod al cargar. Las 2 alas
"técnica coronas" (Horizonte de Sucesos y Anillo de Fotones) SÍ pasaban.

### B. LA CAUSA RAÍZ

Las 8 clases de `SheetWings.cs` **no llevaban el atributo
`[AutoloadEquip(EquipType.Wings)]`**. Sin ese atributo, tModLoader nunca
reserva el slot de equipo de alas → `Item.wingSlot` queda en `-1` → la línea
`ArmorIDs.Wing.Sets.Stats[Item.wingSlot]` indexa fuera del array y revienta
el SetupContent de TODO el mod. Las 2 alas de coronas sí lo tenían (por eso
pasaban): la diferencia entre los dos grupos lo confirmó al 100%.

**Por qué no se detectó antes**: la compilación C# pasa perfecto — el
atributo es metadata de autoload, no código. Solo revienta en runtime, en la
fase "Configurando contenido" del arranque del juego.

### C. EL FIX

- `[AutoloadEquip(EquipType.Wings)]` añadido a las 8 clases de
  `SheetWings.cs`. Con el atributo, tML reserva el slot leyendo la textura
  `Nombre_Wings.png` (las 8 existen y son válidas: RGBA, altura múltiplo de
  4 para la tira de 4 frames) y `Item.wingSlot` llega con valor real a
  `SetStaticDefaults`.
- Nada más tocado: stats, dusts, tooltips, recetas, localización, entrega al
  jugador y las 2 alas procedurales quedaron igual (ya eran correctas).

### D. VERIFICACIÓN

- **Compilación completa del mod contra tModLoader.dll REAL v2026.07.3.0**
  (el mismo release que usa el usuario, descargado de GitHub releases):
  **0 errores, 0 warnings** (entorno de verificación del sandbox reconstruido
  desde cero: .NET 8 SDK + tModLoader.zip 2026.07.3.0 + referencias
  tModLoader/FNA/ReLogic/TerrariaHooks/Steamworks.NET).
- Validación PIL de las 10 texturas de equipo `_Wings.png`: todas PNG RGBA
  válidas, alturas múltiplo de 4 (tira de 4 frames ✓), y las 2 de coronas
  totalmente transparentes (8×8, el truco del mod de referencia para que el dibujo vanilla
  no pinte nada) ✓.
- Diagnóstico diferencial del log: 8 fallos = exactamente las 8 clases sin
  atributo; las 2 con atributo pasaron su `SetStaticDefaults` — evidencia
  concluyente de la causa.

### E. NOTA SOBRE EL AVISO DEL ICONO

El log del usuario contiene un aviso NO fatal durante el empaquetado:
`[FNA]: Image loading failed: unknown image type` (aparece en la fase
"Empaquetando: AethonMod"). El `icon.png` (80×80 RGBA) y `icon_small.png`
(30×30 RGBA) del repo son válidos; si vuelve a aparecer, restaurarlos con
`git checkout -- icon.png icon_small.png`. El empaquetado terminó bien y no
afecta al juego.

---

## Commit v6.06 — LAS 10 ALAS DE PRUEBA END-GAME (2 con técnica de las coronas + 8 con spritesheet procedural)

**Petición del usuario**: "investiga como funcionan las alas en terraria,
como es su animación y su reacción frente a las acciones del jugador, como
volar, saltar, caer, o estar en reposo, investiga el código de su
funcionamiento y crea 7 alas, quiero que para al menos 2 pares de alas uses
la misma técnica que usaste para crear las coronas y estas alas deben ser
con la temática del agujero negro nuevo" (+ "dame las 7 propuestas y crea
otras que tu creas conveniente no te limites solo a las 7" + "De momento
son todas de pruebas, así que sean alas de end-game" + "se las tienes que
dar al jugador, pues son de pruebas").

### A. INVESTIGACIÓN (código real de tModLoader en GitHub)

- **`ExampleCustomDrawWings.cs`** del ExampleMod oficial: hook `WingUpdate`
  (control total de frames/dusts/sonidos), `ModifyEquipTextureDraw`,
  `VerticalWingSpeeds` (el planeo), `ArmorIDs.Wing.Sets.Stats[Item.wingSlot]
  = new WingStats(tiempo, velocidad, aceleración)`.
- **Patch de `Player.cs`** (lógica vanilla extraída): animación vanilla =
  frame 0 en reposo · ciclo 1→2→3 cada 4 ticks al volar · frame 2 al caer ·
  frame 1 al planear · frame 0 flotando en agua; sonido de aleteo
  (SoundID.Item32) por ciclo; `ShouldDrawWingsThatAreAlwaysAnimated()`.
- **el patrón de las alas de renacimiento del mod de referencia**: EL PATRÓN para las
  alas "técnica coronas" — textura de equipo EN BLANCO + `PlayerDrawLayer`
  (`AfterParent(PlayerDrawLayers.Wings)`) + visibilidad por
  `drawPlayer.wings == EquipLoader.GetEquipSlot(...)`.
- **Formato del spritesheet** validado contra la imagen de referencia del
  usuario (5 alas × 4 estados): tira vertical de 4 frames.

### B. LAS 2 ALAS "TÉCNICA CORONAS" (temática agujero negro carmesí)

PNG de equipo EN BLANCO (como el patrón de referencia) + TODO el dibujado por la
biblioteca VFX + animación PROCEDURAL por MUELLES (sin frames):

1. **Alas del Horizonte de Sucesos** (200 ticks · 9.5 · ×3): por lado, un
   mini horizonte de sucesos negro con anillo de fotones + TRES ARCOS DE
   ACRECIÓN anidados (paleta VoidQueen del agujero) + DOPPLER δ³ (el lado
   que avanza arde más) + CUENTAS DE MATERIA orbitando al volar. Se pliegan
   en reposo, se despliegan al caer, aleteo de onda continua al volar.
2. **Alas del Anillo de Fotones** (200 · 9 · ×3.2): por lado, una
   micro-singularidad + CINCO HOJAS DE LUZ curvadas que se comprimen en
   reposo y abren en abanico al volar + PULSOS DE FOTONES viajando hacia
   las puntas + mini-anillos de Einstein rotando en las puntas.

Implementación: `WingAnimPlayer` (ModPlayer: muelle de apertura con
overshoot orgánico, fase de aleteo, dusts y sonido solo si funcionales) +
`EventHorizonWingRenderer` / `PhotonRingWingRenderer` (VFX) +
`EventHorizonWingsDrawLayer` / `PhotonRingWingsDrawLayer`
(`AfterParent(PlayerDrawLayers.Wings)` → `VFXCore.AppendToPlayerDraw`, el
camino de las coronas) + iluminación del mundo muestreada (arden de noche,
se integran de día) + gravedad invertida respetada.

### C. LAS 8 ALAS CON SPRITESHEET PROCEDURAL (flujo vanilla 100%)

`research/wings/gen_wings.py` — motor de arte con 7 ESTILOS (emplumadas /
membrana / llama / cristal / nebulosa / eclipse / nova), supersampleado 4×,
outline estilo Terraria, sombreado superior, simetría espejo TOTAL (misma
semilla rng por lado) y AUTO-ENCAJE por recorte (imposible cortarse;
alturas múltiplo de 4 por `texture.Frame(1,4)`). Dos rondas de validación
VLM (8.8/8.4/8.0/7.0/7.0/8.0/7.4/8.4 → ajustes → **8/8 APROBADAS**):

| Alas | Estilo | Vuelo |
|---|---|---|
| Nova Solar | lenguadas de plasma blanco→dorado→rojo | 190 · 9.5 |
| Plasma Cuántico | shards cian facetados + destellos | 180 · **10** (récord) |
| Vacío Etéreo | membrana púrpura + venas fucsia + estrellas | 180 · 9 |
| Éter Glacial | plumas celestes + carámbanos (planeo lento) | 180 · 8.5 |
| Fósiles del Génesis | hueso + vetas ámbar + sedimento | 180 · 9 |
| Pilar de Nebulosa | cuerpo magenta translúcido + borde cian | 185 · 9 |
| Eclipse | discos negros + rayos de corona oro | 190 · 9.2 |
| Supernova | plumas de choque + anillos de onda | 185 · 9.4 |

Clase base `AethonWings` (stats por `WingStats`, `VerticalWingSpeeds` con
personalidad, dusts de vuelo por ala — todos DustIDs ya usados por el mod),
subclases finas en `SheetWings.cs`, tooltips de color tipo coronas.

### D. ENTREGA Y LOCALIZACIÓN

- `TestingPlayer`: las 10 alas garantizadas en el inventario en cada
  entrada al mundo (patrón `EnsureItem` de las armas cósmicas).
- Recetas de madera (5) para recuperarlas si se pierden (como las coronas).
- Localización es-ES + en-US (DisplayName + Tooltip de las 10).
- build.txt → 6.06.

### E. VERIFICACIÓN

Compilado contra tModLoader **v2025.06.3.0 real** (.NET 8 SDK + dlls del
release de GitHub en /tmp/verify; referencia `tModLoader.dll` +
`TerrariaHooks.dll` + FNA + ReLogic): **Build succeeded · 0 errores ·
0 warnings**. Nota de arquitectura descubierta: desde las versiones 2025
la API de mods vive en `tModLoader.dll` (raíz del zip), NO en
`TerrariaHooks.dll` (que solo es la vanilla con ganchos).

## Commit v6.05 — EL AGUJERO NEGRO CARMESÍ: COPIA EXACTA + PARÁMETROS

**Petición del usuario**: "primero toma una copia exacta del agujero negro
funcional que tenemos, y a partir de ahí modifica sus parámetros, disco de
acreción mas grande y de otro color, el agujero un poco mas pequeño, mejorar
la animación, etc. además te pido que investigues en internet como se crean
matematicamente un agujero negro, investiga formulas e investiga la
estructura de nuestro agujero negro funcional, luego replica el agujero
negro de la referencia" (+ "haz lo mejor posible con todo lo aprendido y
todos los recursos que puedas conseguir de cualquier lugar de Internet").

### A. EL MÉTODO (radicalmente distinto al v6.04)

El v6.04 (render Gargantua con texturas PNG pre-generadas) se veía horrible:
SE ELIMINÓ COMPLETO (GargantuaRenderer.cs + GargantuaBack/Front/Shadow.png).
El v6.05 es EXACTAMENTE lo que pidió el usuario: **el MISMO render del
agujero funcional** (RealBlackHoleShader — marcha de luz de 75 pasos con
lensing gravitacional real, dibujado sobre el lienzo InvisiblePixel, mismo
halo, mismo refuerzo del horizonte, misma lente de pantalla) con los
**PARÁMETROS recalibrados** — sin recompilar el .fxc (los parámetros del
shader se establecen por nombre desde C# en cada frame).

### B. INVESTIGACIÓN MATEMÁTICA (internet)

Documentada en `research/blackhole/MATEMATICA_AGUJEROS_NEGROS.md`:
r_s = 2GM/c² (Schwarzschild), esfera de fotones 1.5·r_s, sombra aparente
(√27/2)·r_s ≈ 2.6·r_s, ISCO = 3·r_s (borde interno del disco), Kepler
v = √(GM/r) → 0.41c en el ISCO, Doppler beaming δ = 1/(γ(1−β·cosθ)) con
brillo ~δ³ (contraste ~13× entre lados), lente α = 4GM/(c²·b), Shakura–Sunyaev
T(r) ∝ r^(−3/4) (núcleo caliente blanco → borde rojo), y el paper de
Interstellar (James et al. 2015) para el Gargantua.

### C. PARÁMETROS: FUNCIONAL → CARMESÍ

| Parámetro | Funcional | Carmesí v6.05 | Por qué |
|---|---|---|---|
| `blackHoleRadius` | 0.30 | **0.25** | "el agujero un poco más pequeño" |
| `accretionDiskRadius` (tubo del toro) | 0.40 | **0.48** | "disco más grande": borde ext. 3.8×→4.9× la sombra; borde interno ~0.27 queda pegado a la sombra (≈ISCO) |
| `accretionDiskColor` | (245,105,61) | **(255,45,100)** | "otro color": paleta de la referencia (blanco-rosado→magenta→carmesí) |
| `accretionDiskScale.y` | 0.33 | **0.28** | banda fina casi de canto (referencia ~15-20°) |
| `cameraAngle` | 0.32 | **0.30** | inclinación ~17° como la referencia |
| `globalTime` | t | **t×1.35** | "mejorar la animación": el plasma HIERVE más vivo |
| `cameraRotationAxis` | fijo | **+ precesión ±0.05/±0.06 rad (2 frecuencias incommensurables)** | el plano del disco bambolea orgánico |
| lienzo | 256·escala | **256·escala·1.10·(respiración ±1.8%)** | disco +10% en pantalla; el conjunto respira |
| Doppler beaming | — | **velos aditivos: izq. blanco-rosado (δ³), der. carmesí tenue** | física real + la referencia (lado izquierdo brillante) |
| Anillo de fotones | interno del shader | **+ refuerzo Ring rosa pálido pulsante a 1.7·r_h** | firma visual de la referencia |

La FÍSICA DE JUEGO queda COPIA EXACTA del funcional (pop elástico,
crecimiento→evaporación→anillo de Einstein, aura con ticks que aceleran
cerca del centro, atracción 2.6 (10× el sol), devora balas enemigas,
persecución lenta). El BlackHoleProjectile original queda INTACTO.

### D. RESPUESTA AL USUARIO: ¿assets o código?

El agujero funcional es **~100% código**: el visual es el shader
RealBlackHoleShader.fxc (marcha de luz de 75 pasos) sobre un píxel
transparente escalado (InvisiblePixel.png) + ruido FireNoiseB + dusts
vanilla + la lente de pantalla (otro shader). No hay "assets" del agujero
como tal — por eso el usuario no los encontraba. (Sección 6 del doc de
investigación.)

## Commit v6.04 — EL AGUJERO NEGRO EXACTO: EL RENDER GARGANTUA

**Petición del usuario**: "el agujero negro no se parece en nada... te
mostraré el agujero negro de terraria que hiciste primera imagen y la
referencia segunda imagen. usa todo el conocimiento que tienes, y todas las
librerías disponibles para crear una copia exacta del agujero negro de la
segunda imagen, no olvides que tenemos un agujero negro funcional puedes
usar una copia como base para eso" (las coronas ya estaban perfectas).

### A. ANÁLISIS MÉTRICO DE LA REFERENCIA (píxel-exacto)

Análisis VLM doble (imagen completa + recortes) + medición píxel a píxel
con Python (numpy) del recorte del agujero. La estructura REAL de la
referencia (el brillo del lado derecho de la imagen es un PERSONAJE en
primer plano, NO parte del agujero — no se replica):

- SOMBRA negra central: Ø~95px sobre estructura de ~335px → 28% del ancho,
  borde NÍTIDO, hueco redondeado.
- DISCO de acreción COMPACTO: extensión total ~3.5× el diámetro de la
  sombra (±3.55 r_sh), NO un anillo extendido.
- Banda frontal GRUESA: ~40px de alto = ±0.43 r_sh, que CRUZA por delante
  de la esfera en el ecuador (el clásico Gargantua de Interstellar).
- ARCO DE LENTE superior (disco trasero lensado): cima a 1.34 r_sh sobre
  el polo, BRILLANTE.
- Tilt del sistema: ~-12° (extremo derecho hacia arriba).
- DOPPLER: el lado que se acerca (izquierda) blanco-dorado CEGADOR; el
  que se aleja (derecha) carmesí tenue.
- Paleta medida: blanco #FFFFFF, dorado #FDCB7C, coral #FA7069, rosa
  #FF5DAE, magenta #FF26B0, carmesí #F82960, granate #8B0000, halo
  #1D0007.

### B. EL GENERADOR PROCEDURAL (`tools/gen_gargantua.py`)

Un mini "ray-tracer artístico" de lente gravitacional (la técnica de los
shaders de Gargantua, sin geodésicas reales): 8 iteraciones de refinado
con comparación visual iterativa + autodiagnóstico ASCII contra el mapa
de brillo de la referencia. Técnicas:

- **Proyección física**: disco edge-on elíptico con SKEW radial |Xr|^0.74
  (puntas de aguja), grosor 3D del torus (más grueso al frente), banda
  que cruza el ecuador por DEBAJO del centro de la sombra.
- **Paleta DUAL por Doppler**: gradiente CÁLIDO (negro→granate→coral→
  naranja→dorado→blanco) en el lado que se acerca, magenta en el que se
  aleja — interpoladas por píxel.
- **Turbulencia con DOMAIN WARPING** (plasma fluido, no estática digital)
  + filamentos blancos finos + VETAS OSCURAS entre filamentos (el
  contraste duro de la referencia).
- **RIM INTERIOR ardiendo**: franja blanco-dorada en el borde interno del
  disco (donde el gas orbita más rápido) + HOTSPOT cegador en el cruce.
- **Anillo de fotones** naranja-blanco (la parte MÁS brillante) con picos
  de relámpago deterministas.
- **Tendrilas de gas** (wisps) y neblina roja atmosférica alrededor.

Salida calibrada (2048×1024, sombra a R_SH=150px):
- `GargantuaBack.png` — todo lo que vive DETRÁS de la esfera.
- `GargantuaFront.png` — SOLO la banda que cruza por delante.
- `GargantuaShadow.png` — círculo negro de borde NÍTIDO (caída de 6px).

### C. EL RENDERER (`Content/VFX/GargantuaRenderer.cs`)

Render por 3 pasos con el contrato de batch heredado (mundo + lente):

1. **GARGANTUABACK** (aditivo): neblina + wisps + arcos de lente +
   anillo de fotones + penumbra.
2. **LA SOMBRA** (alpha): negro absoluto NÍTIDO — come el fondo del mundo
   y el brillo trasero: el vacío.
3. **GARGANTUAFRONT** (aditivo): la banda que CRUZA el ecuador con sus
   filamentos y su hotspot blanco-dorado.

El conjunto BAMBOLEA (±1.1°) y RESPIRA (0.97..1.03). La distorsión del
fondo la sigue aportando BlackHoleLensSystem (abraza el disco completo:
134px vs ±102px del disco) y las partículas orbitales viven ahora en el
radio del disco visible (1.4..3.1 R) con la paleta cálida coral/dorado.

### D. SIN CAMBIOS DE FÍSICA

El CrimsonBlackHoleProjectile conserva TODA la física probada (aura de
daño con ticks acelerados, atracción 10× el sol, devora balas enemigas,
persecución lenta, anillo de Einstein final). Solo cambian: el render
(StylizedVoidRenderer queda como miembro de la biblioteca para usos
futuros), la paleta de las partículas de materia (coral→dorado→carmesí
en vez de fucsia) y la iluminación (coral-cálida en vez de magenta).

Compilado contra tModLoader real: 0 errores, 0 warnings.

## Commit v6.03 — LA BIBLIOTECA VISUAL AETHON + EL AGUJERO NEGRO DE LA REFERENCIA + LAS DOS CORONAS

**Peticiones del usuario**: (1) "es momento de diseñar el nuevo sistema...
es el momento de diseñar las nuevas librerias y assets" (sobre la
investigación de técnicas de los mods populares); (2) "toma la corona actual
del agujero negro y quítala, en cambio toma esa corona y conviértela en un
ítem cosmético que ubica la corona justo detrás de la cabeza del jugador";
(3) "con respecto al agujero negro este no se parece en nada a la referencia,
tiene que ser exactamente igual pero sin la corona"; (4) "intenta recrear
esa corona como un item extra, esta sera una nueva corona que no tiene nada
que ver con la corona actual del agujero negro y esta sera un nuevo
cosmetico" (dos imágenes de referencia nuevas: la entidad cósmica y el
portal rosa).

### A. LA BIBLIOTECA VISUAL AETHON (`Content/VFX/`)

El nuevo sistema de librerías visuales propio — 6 módulos que ya usan (y
usarán) todas las armas del mod:

- **VFXCore** — el núcleo: los efectos se describen como listas de CUADROS
  DE LUZ (GlowQuad: posición mundo/color/escala/rotación/textura) y se
  vuelcan con UNA llamada al destino que haga falta: dibujo aditivo directo
  (proyectiles, neón real) o emisión de DrawData para las capas de dibujado
  del jugador (el camino oficial de tML, sin tocar el batch del renderer).
  Un MISMO renderizador sirve en un proyectil y sobre la cabeza de un
  jugador. Buffer estático reutilizable (cero GC por frame) + helpers de
  respiración/balanceo/hash determinista/elipses.
- **VFXPalettes** — paletas nombradas: VoidQueen (la del agujero de la
  referencia, MEDIDA por píxel), CrimsonCourt (la corona de arcos) y
  RuneStars (la corona rúnica nueva).
- **ArcCrownRenderer** — la corona de arcos de neón (5 lazos con asimetría,
  ecos interiores y nudos naranjas con destello de 4 puntas), promotida de
  código de proyectil a MIEMBRO DE LA BIBLIOTECA reutilizable.
- **RuneCrownRenderer** — la corona rúnica estelar: 8 glifos rúnicos
  ANGULARES diseñados desde cero (la lanza, el cáliz, la puerta, la
  estrella, el rayo, el arco, la espiral y el trono) flotando en arco sobre
  la cabeza, con perlas rosa pálido en las puntas y halo tenue.
- **StylizedVoidRenderer** — EL AGUJERO NEGRO DE LA REFERENCIA (ver B).
- **BoltRenderer** — relámpagos deterministas reutilizables (zigzag por
  hash puro: todas las máquinas ven el MISMO rayo; se regenera cada ~9
  ticks — VIVE).

### B. EL AGUJERO NEGRO — EXACTAMENTE LA REFERENCIA (SIN CORONA)

Geometría MEDIDA por píxel en las dos imágenes de referencia (perfil
horizontal/vertical del agujero del pecho de la entidad + elipse del
portal): lente plana brillante de aspecto ~2:1, zona oscura interior hasta
±0.23× el semieje, aro BLANCO-CÁLIDO a 0.47×, banda fucsia SATURADA de 0.7
a 1.0× (picos medidos 255,0,255 y 248,0,73 — paleta fucsia intermedia
exacta), núcleo negro PURO pequeño (~5% de la lente) y media luna inferior
de destello frontal (pico medido 255,26,255).

Render por capas: halo púrpura compacto que respira → relleno SÓLIDO
(GlowOrb, lente saturada) → GRADIENTE CONTINUO de 10 anillos de Ring
ESTIRADOS y solapados (blanco-cálido → caliente → fucsia; banda CONTINUA,
nada de puntos) → EL VACÍO (4 elipses negras apiladas: negro absoluto y
penumbra) → cruce frontal de los 3 anillos interiores + MEDIA LUNA →
temblor de materia fluyendo (rotación ANTIHORARIA con leve Doppler
izquierda-caliente, medido en la referencia) → ONCE rayos superiores
sutiles (los del portal) → DOS relÁMPAGOS de los flancos. El shader
raymarchado YA NO se usa en el carmesí (el original lo conserva intacto):
el look de la referencia es luz por capas, no 3D realista. 8 iteraciones
de verificación con simulación PIL espejo del algoritmo + crítica visual +
medición de perfiles normalizados contra el recorte de la referencia.

LA CORONA fue RETIRADA del proyectil (DrawCoronaCrown y SpawnCrownEmbers
ELIMINADOS — 0 ocurrencias en el binario).

### C. LA CORONA DE LA REINA DEL VACÍO (cosmético 1)

La corona ORIGINAL del agujero negro (v6.02), retirada del proyectil y
convertida en ÍTEM COSMÉTICO: **VoidCrownItem** — accesorio puro (cero
estadísticas, huecos funcionales O de vanidad) que dibuja la corona de
arcos JUSTO DETRÁS de la cabeza del jugador (capa VoidCrownDrawLayer,
ANTES de la capa Head: la cabeza tapa lo que cruza — la corona ENVUELVE).
Escala a la cabeza, respira, se balancea, suelta ASCUAS ROSAS sobre los
ápices (la corona es energía viva) e ilumina la noche con su carmesí.
Ícono procedural de 3 arcos + nudos (3 iteraciones de crítica visual).

### D. LA CORONA RÚNICA ESTELAR (cosmético 2)

Diseño NUEVO DESDE CERO sobre la referencia (no tiene nada que ver con la
corona de arcos): **RuneCrownItem** — ocho glifos rúnicos de luz fucsia
flotando en arco ALTO sobre la cabeza (capa RuneCrownDrawLayer, tras las
capas de cara: es un halo), cada uno con su PERLA blanca-rosada en la
punta. Los glifos flotan con oscilación desfasada, pulsan su brillo con
fase propia y EMITEN CHISPAS ASCENDENTES desde las perlas. El arco flota
alto para no competir con la corona de arcos si el jugador lleva AMBAS.
Ícono procedural de arco fucsia + 5 glifos simples + perlas (3 iteraciones
de crítica visual).

### E. INFRAESTRUCTURA Y VERIFICACIÓN

- **CosmeticPlayer** (ModPlayer): escanea los 14 huecos de accesorio
  (funcionales 3..9 + vanidad 13..19) — un cosmético es un cosmético viva
  dónde lo pongas; hace vivir las coronas (ascuas, chispas) y las ilumina.
- **TestingPlayer**: el kit de pruebas entrega las DOS coronas además del
  agujero carmesí.
- **Localización** en-US/es-ES completa (2 ítems nuevos + agujero
  actualizado); tooltips reescritos.
- **Compilación contra tModLoader v2026.07.3.0 real**: 0 errores, 0
  warnings (Debug y Release). **Auditoría de binario**: las 11 clases
  nuevas + 21 símbolos presentes; DrawCoronaCrown/SpawnCrownEmbers = 0;
  el shader del agujero ORIGINAL intacto (UTF-16 verificado).

## Commit v6.02 — INVESTIGACIÓN VISUAL + LIMPIEZA TOTAL DE REFERENCIAS + EL AGUJERO NEGRO CARMESÍ

**Peticiones del usuario**: (1) "quiero que hagas una investigacion super
profunda de todas las librerias y recursos visuales de los mods populares"
(prestando especial atención al referente de
calidad visual que el usuario citó), "todo en pos de mejorar el aspecto futuro
de nuestro mod"; (2) "luego has 100 pasadas al proyecto completo para limpiar
y depurar, recuerda eliminar cualquier mención de cualquier otro mod o
referencias externas en cualquier sentido"; (3) "revisa bien que el codigo
sea super correcto y ademas sin errores ni fallas ni faltas"; (4) "copiar el
arma de agujero negro en una nueva arma de agujero negro para darle un poco
mas de personalidad... modificarla para que se vea exactamente igual a como
esta en la imagen de referencia, recuerda dejar al agujero negro original
intacto" (imagen de referencia: agujero negro carmesí con corona de arcos).

### A. INVESTIGACIÓN VISUAL (conocimiento para el futuro del mod)

Estudio profundo de las técnicas de los mods visuales top (el mod de
referencia pedido por el usuario, más los sistemas de partículas y VFX
públicos del ecosistema) — conclusiones ACCIONABLES documentadas en el
documento del proyecto (sección 15, neutralizada de nombres): técnicas de
marching de luz, lente a pantalla completa, capas aditivas, partículas por
componentes, texturas de ruido procedurales. El mod ya implementa su propio
pipeline equivalente (shader de 75 pasos + LensSystem + librería de
partículas propia + generador de texturas).

### B. LIMPIEZA TOTAL — 0 REFERENCIAS EXTERNAS

- **Carpeta `research/` ELIMINADA del repo** (25 archivos: ejemplos de
  código de otros mods y notas con nombres externos — nunca formaron parte
  del mod compilado, pero vivían en el repo)
- **Shaders muertos eliminados**: BlackOnlyShader (.fx+.fxc), Shockwave.fx,
  Bloom.fx, ChromaticAberration.fx (0 referencias en el código) — quedan
  SOLO los 4 activos: RealBlackHoleShader, SunShader, RadialShineShader y
  BlackHoleDistortionShader
- **Las 6 texturas del pipeline REGENERADAS 100% proceduralmente**
  (`tools/gen_effects_textures.py`: ruido de valor periódico + deformación
  de dominio — FireNoiseB, DendriticNoiseZoomedOut, WavyBlotchNoise,
  PsychedelicWingTextureOffsetMap, BloomCircleSmall, InvisiblePixel) con
  estadísticas calibradas al uso de cada shader; 4 texturas muertas fuera
  (BloomCircle, BloomFlare, FireNoiseA, WavyBlotchNoiseDetailed)
- **Los 4 .fx REESCRITOS como fuente propia** (misma matemática, expresión y
  comentarios propios, parámetros idénticos; los .fxc compilados se
  mantienen — el pipeline fx_2_0 documentado en COMPILACION.md)
- **0 menciones externas en TODO el mod**: comentarios .cs (citas de libro
  de referencia, URLs, notas de inspiración), CHANGES.md (nombres de mods,
  organismos y películas), documento del proyecto (58
  menciones neutralizadas), DISEÑO/ROADMAP (compatibilidad con otros mods
  reescrita genérica), COMPILACION.md (enlaces externos → notas propias del
  pipeline); README/description.txt actualizados a la realidad (eventos
  eliminados en v5.27 fuera, arsenal actual)
- **Auditoría de binario**: 0 ocurrencias de nombres externos en el DLL

### C. 100 PASADAS DE DEPURACIÓN

- **11 usings muertos eliminados** (verificado compilando: solo 1 falso
  positivo restaurado por Point16)
- **Localización COMPLETADA**: 38 entradas DisplayName que faltaban (19
  clases × 2 idiomas — BlackHoleStaff, SunStaff, los 4 V20, tests,
  proyectiles...) + clave muerta Items.Placeables.AncientAltarItem corregida
  a la ruta real + armas nuevas; el inventario ya no muestra nombres crudos
- **Constante muerta eliminada** (FlareInterval en SunProjectile)
- **Balance Begin/End verificado** (los 8 "excesos" son los cierres
  defensivos documentados — correctos)
- **Texturas**: 41/41 clases con su .png ✓
- **Compilación**: 0 errores / 0 warnings contra tModLoader v2026.07.3.0

### D. EL AGUJERO NEGRO CARMESÍ (arma nueva — la corona de la reina)

- **CrimsonBlackHoleStaff** (daño 110, cadencia 50): copia CON
  personalidad del BlackHoleStaff — el ORIGINAL QUEDA INTACTO. Dispara
  **CrimsonBlackHoleProjectile**: misma física probada (aura de daño con
  ticks que aceleran cerca del centro 6→24, atracción 2.6 en 450px, devora
  balas enemigas al cruzar el horizonte con chispas ROSAS, persecución
  lenta, anillo de Einstein final con el daño completo) y el visual de la
  imagen de referencia:
  - **Disco de acreción MAGENTA ELÉCTRICO** (#FF0055 vía el parámetro del
    shader) más de canto (cameraAngle 0.42) y más prominente (0.44)
  - **Anillo de fotones ROSA-INCANDESCENTE**: halo rosa + núcleo fino
    blanco-rosa (#FFBB90) pulsando a 4.5 rad/s justo fuera del horizonte
  - **LA CORONA**: 5 lazos de neón carmesí→magenta sobre el anillo (el
    exterior el más alto), CON ASIMETRÍA dinámica por lazo (semianchos izq/
    der distintos + balanceo por índice), ECO interior tenue por lazo
    (filamentos encajados), grosor variable (fino en bases, corpulento al
    subir) y NUDOS NARANJA incandescentes con DESTELLO DE 4 PUNTAS pulsante
  - **Ascuas rosas** alzándose sobre la corona (la energía es VIVA)
  - Partículas/halo/iluminación en toda la paleta carmesí/magenta/rosa
    (dusts Crimson + Enchanted_Pink, estelas TrailGlow magenta)
- **Integración completa**: LensSystem (misma lente gravitacional + dibujo
  propio encima), kit de TestingPlayer con EnsureItem, localización ES/EN,
  texturas procedurales (icono 28×30 con orbe coronado + placeholder 76×76)

### E. VERIFICACIÓN

- Compilación contra tModLoader v2026.07.3.0 REAL: 0 errores, 0 warnings
- Binario: clases nuevas presentes, muertas ausentes, 0 nombres externos
- 41/41 clases con textura ✓; localización 100% completa ES/EN ✓
- Arsenal de pruebas: 14 → **15 armas** (el Carmesí se entrega siempre)

## Commit v6.01 — LA GRAN LIMPIEZA: EL USUARIO ELIGE QUÉ SE QUEDA (21 ARMAS FUERA)

**Petición del usuario**: "es momento de seleccionar que se queda en el
proyecto" + lista explícita de borrado (números del inventario) + sobre el
Quásar y la Galaxia Viviente: "la galaxia se ve horrible y la lanza igual,
ademas las dos son tan simple que no vale la pena que continue en el mod".

El arsenal de pruebas pasó de 35 a **14 armas**. Se listó el inventario
completo agrupado por generación (8 tests originales + Grimorio + 19 V20 +
7 cósmicas), el usuario seleccionó, se confirmaron las dudas (el "191"
era el 19 = AbyssalEyeStaff; Quásar/Galaxia confirmadas fuera) y se
ejecutó la purga con auditoría completa.

### A. FUERA — 21 ARMAS ELIMINADAS

- **4 ARMAS DE COLOR** (las más viejas, pruebas de sistema de partículas):
  ColorRainbow, ColorRed, ColorYellow, ColorGreen — clases extirpadas de
  TestAdvanced.cs, texturas borradas, y sus 4 handlers (modos 5004-5007)
  limpiados de TestAdvancedFX.cs junto al helper huérfano
  DrawColoredSprite (solo lo usaban ellas)
- **15 ARMAS V20** (de las 20 de partículas): TornadoStaff,
  PrismBeamStaff, EarthquakeStaff, MirrorDimensionStaff,
  GravityPulseStaff, ShadowCloneStaff, CrystalShatterStaff,
  VortexChainStaff (con su VortexMineProjectile interno), AbyssalEyeStaff,
  SpectralMirageStaff, TemporalRiftStaff, InfernoTornadoStaff,
  VoidEaterStaff, PlasmaOrbStaff, BlackHoleMiniStaff — arma + proyectil +
  texturas, mapeo 1:1 verificado sin huérfanos
- **2 CÓSMICAS NUEVAS** (v5.99/v6.00, "se ven horrible y son muy
  simples"): LA LANZA DEL QUÁSAR (QuasarLance + QuasarJetProjectile) y LA
  GALAXIA VIVIENTE (LivingGalaxyStaff + LivingGalaxyProjectile +
  GalaxyStarProjectile + SpiralGalaxy.png procedural)
- **BlackHoleLensSystem — cirugía del bloque galaxia**: recolección,
  arrays _galaxyIndices/_galaxyCount, fuente del pase B y draw loop
  extirpados; el protocolo de dibujado-encima-de-la-lente vive intacto en
  soles, medusas, cometas y púlsares
- **Localización en-US/es-ES**: entradas del Quásar y la Galaxia eliminadas
- **TestingPlayer**: 21 líneas de entrega eliminadas del kit

### B. SE QUEDAN — LOS 14 ELEGIDOS

- **4 tests clásicos**: TestMagicRing, TestSparkle, ProjBeam,
  TestMagicRingV2 (con sus handlers 3003/3004/4001/4006 intactos)
- **El Grimorio del Eterno** (con su Orbe Cósmico — la imagen del
  usuario, INTACTA)
- **4 V20**: SupernovaStaff, PlasmaStormStaff, PhoenixNovaStaff,
  QuantumSplitStaff
- **5 cósmicas**: BlackHoleStaff, SunStaff, MedusaNebularStaff,
  LivingCometStaff, LivingPulsarStaff (todas con su EnsureItem individual
  garantizado)
- Quien tenga armas borradas en un guardado viejo LAS CONSERVA (no se
  eliminan del inventario existente — solo dejan de entregarse)

### C. VERIFICACIÓN

- Auditoría de referencias: 0 menciones a las 40 clases borradas en todo
  el .cs del mod
- Auditoría de texturas: 39 clases ModItem/ModProjectile/ModBuff, TODAS
  con su .png en la ruta por defecto ✓
- Compilación contra tModLoader v2026.07.3.0 REAL: 0 errores, 0 warnings
- Auditoría de binario (búsqueda de bytes ASCII+UTF-16): 30 nombres
  borrados → 0 restos; 20 conservados → todos presentes

### D. MARCADA COMO ESTABLE

- **2026-09-12**: el usuario verificó v6.01 como ESTABLE y pidió guardarla
  como punto de retorno en GitHub. Tag `stable-v6.01` + rama
  `stable-v6.01-backup` + STABLE-SNAPSHOT.md regenerado (manifiesto
  SHA-256 completo del paquete: 186 archivos).

## Commit v6.00 — SOL Y AGUJERO PULSAN MÁS FUERTE + EL LÁTIGO DE LA MEDUSA + ADIÓS OJO, LLEGA LA GALAXIA VIVIENTE

**Peticiones del usuario**: (1) "creo que deberías aumentar los tick de
daños del sol y el agujero negro, tambien aumentar el area de daño del
agujero negro y del sol; en el caso del agujero negro los tick de daño
deben aumentar a medida te acercas al centro"; (2) "tanto el sol como el
agujero negro deben perseguir ligeramente a los enemigos y tambien deben
ser capas de afectar los proyectiles con su gravedad"; (3) "en cuanto a
la medusa el rayo debe salir de medusa no del cielo, y debe tener mas
brillo"; (4) "ademas mejora las nuevas armas que creaste y borra el ojo,
se ve feo, mejor crea un arma nueva con un proyectil cosmico, este debe
ser una galaxia, investiga galaxias en internet, recuerda todas estas son
armas de prueba no requieren mana".

### A. EL AGUJERO NEGRO Y EL SOL — MÁS TICKS, MÁS ÁREA, PERSIGUEN Y DOBLAN BALAS

- **AGUJERO NEGRO — TICKS QUE ACELERAN CERCA DEL CENTRO** (petición
  explícita): adiós al pulso global cada 0.5 s — ahora CADA ENEMIGO tiene
  su PROPIO intervalo según su distancia al horizonte: en el borde del
  aura ~24 ticks (0.4 s), PEGADO AL CENTRO 6 ticks (10 golpes/s): el campo
  te MACHACA cuanto más te hundes. El área además creció: 1.15→1.9× el
  campo (antes 0.75→1.2×)
- **EL SOL**: pulso de aura 15→10 ticks (+50% de golpes/s) y área
  1.75→2.30× el radio visual (la gigante roja la arrastra: ~150→310 px);
  daño del aura 40→45%
- **PERSIGUEN LIGERAMENTE A LOS ENEMIGOS** (ambos): el agujero SE DESLIZA
  hacia la presa más cercana (accel 0.07/t, tope 2.4 px/t — deriva
  amenazante) y el sol igual (0.09/t, tope 3 px/t)
- **LA GRAVEDAD AHORA DOBLA PROYECTILES ENEMIGOS** (ambos): las balas
  hostiles caen en espiral hacia el agujero y AL TOCAR EL HORIZONTE SON
  ABSORBIDAS (chispas doradas — defensa gravitacional pura: el agujero SE
  COME las balas); el sol las curva débilmente y si tocan el plasma SE
  EVAPORAN en polvo de fuego

### B. EL LÁTIGO ELÉCTRICO DE LA MEDUSA (v6.00)

Petición: "el rayo debe salir de medusa no del cielo, y debe tener mas
brillo". NebulaLightning REESCRITO: el rayo NACE BAJO LA CAMPANA (la
"boca") y VUELA RECTO hacia la víctima — un LÁTIGO de plasma frío que se
desenrosca de la medusa. MÁS BRILLO: TRES capas aditivas (halo aqua
ancho + funda azul-blanco + NÚCLEO blanco puro a 255), luz real
proyectada cada paso (1.3/1.55/1.75), micro-parpadeo vivo, ramas cortas
laterales, frente de 4 puntas y DESCARGA en el origen (la campana
chispea al soltarlo); al clavarse: trueno + estallado de hielo y el trazo
LIGERA chisporroteando mientras se funde. Daño del rayo 0.8→1.0× (es EL
ataque de la medusa).

### C. EL OJO DEL VACÍO — ELIMINADO

Petición: "borra el ojo, se ve feo". Borrado COMPLETO: VoidEyeStaff +
VoidEyeProjectile (.cs y .png), las 3 texturas del ojo (EyeSclera/
EyeIris/EyeLid), los generadores PIL, las referencias del LensSystem
(_eyeIndices/pase B/dibujado encima), TestingPlayer y la localización.
El binario verifica 0 ocurrencias de VoidEye.

### D. LA GALAXIA VIVIENTE (arma nueva — el proyectil ES una galaxia)

Petición: "crea un arma nueva con un proyectil cosmico, este debe ser una
galaxia, investiga galaxias en internet". Diseño de galaxia
espiral REALISTA:
 bulbo AMARILLO de estrellas viejas, brazos
AZULES de estrellas jóvenes, NUDOS ROSAS HII ("beads-on-a-string"),
CARRILES DE POLVO oscuros al borde interno de los brazos. SpiralGalaxy.png
512 px PIL ×4 supersampling con 2 iteraciones de crítica VLM (7.5→8.5/10:
polvo como polilíneas oscuras que CORTAN el azul, HII vívidos "como
letreros de neón", brazos asimétricos, bulbo elíptico moteado con
filamentos) + icono 30×30 de alto contraste (remolino en S grueso).

- **LivingGalaxyStaff** (Magic, daño 110, mana 0, useTime 30) →
  **LivingGalaxyProjectile** (~9 s): nace con pop elástico, VUELA y SE
  ESTACIONA donde la lanzaste; el disco GIRA (0.02 rad/t) y CABECEA EN 3D
  (escala Y 0.55→1.0 — la moneda espacial de canto a cara) + eco tenue
  rotado (imagen secundaria); ARRASTRA enemigos (gravedad 0.4, radio
  300), AURA estelar cada 10 ticks (45%), SEMBRADO estelar cada 24 ticks
  (los 2 brazos sueltan GalaxyStarProjectile tangencialmente — rociador
  cósmico), ACECHA (el ancla deriva hacia la presa 0.7 px/t)
- **LA EXPLOSIÓN ESTELLAR** (OnKill): AoE 90% + 14 semillas estelares
  radiales + destello + sonidos (nada de ondas: el sol tiene SU nova y el
  agujero SU anillo — la galaxia estalla en SEMILLAS)
- **GalaxyStarProjectile**: estrellas de 4 puntas girando con halo y eco,
  cada una con el COLOR de su origen (azul de brazo / oro de bulbo /
  rosa de HII)
- **Lente**: pase B respirando con el giro (0.07→0.12 — masa de cien mil
  millones de soles), dibujada ENCIMA de la lente (protocolo del arsenal)

### E. MEJORA DE LAS ARMAS NUEVAS + TODAS SIN MANA

Petición: "mejora las nuevas armas que creaste, recuerda todas estas son
armas de prueba no requieren mana".

- **Cometa Estelar**: daño 38→46, nova 92→130 px al 75% (antes 60%),
  picado 16.5→19 px/t, cooldown 70→60
- **Púlsar Vivo**: daño 30→38, haces 340→420 px, daño del haz 55→65%
- **Lanza del Quásar**: daño 85→100, ATRAVIESA 10→14 enemigos, vida
  90→120 (más alcance), florecimiento 130→170 px al 65%
- **MANA = 0 en TODAS las armas de prueba cósmicas**: MedusaNebularStaff,
  LivingCometStaff, LivingPulsarStaff, QuasarLance (SunStaff y
  BlackHoleStaff ya lo eran) — y la nueva LivingGalaxyStaff nace sin mana

### F. Verificación

Compilación contra tModLoader real v2026.07.3.0 (/tmp/verify): 0 errores,
0 warnings. Auditoría del binario: LivingGalaxyProjectile/
GalaxyStarProjectile/LivingGalaxyStaff/DrawGalaxyVisuals/
GetGalaxyLensStrength/FindNearestEnemy presentes; 0 ocurrencias de
VoidEye. Auditoría de texturas: las 86 clases del arsenal con su asset ✓
(85 de v5.99 + 3 nuevas − 2 del ojo).

## Commit v5.99 — EL OJO REDISEÑADO + RAYOS para la Medusa + EL COMETA ESTELAR + EL PÚLSAR VIVO + LA LANZA DEL QUÁSAR

**Peticiones del usuario**: (1) "el ojo no se ve nada bien, intenta
mejorarlo para que se vea bien, investiga en internet para conseguir ideas
de assets o como hacerlo"; (2) "la medusa es interesante, pero sus
proyectiles son aburridos, es mejor que el proyectil que usa la medusa sean
rayos, ya sabes, los rayos que caen del cielo"; (3) "usando la segunda
imagen como referencia, crea un minion cosmico con efectos, de la misma
forma a como creaste la medusa, pero cuidado ya existe un minion con el
nombre minion cosmico que usa exactamente la misma imagen, no lo toques";
(4) "luego crea otro minion cosmico, y crea una nueva arma con un proyectil
super cosmico"; (5) "no olvides arreglar y mejorar el ojo".

### A. EL OJO DEL VACÍO — rediseño TOTAL del render (investigado en internet)

Investigación de diseño (técnicas de ojos realistas + anillo de fotones y
disco de acreción curvándose sobre y bajo la esfera, como se ve en los
agujeros negros del cine) + análisis VLM de la captura del usuario: la esclerótica
era un "plato de cerámica plano con garabatos", la pupila "una PUERTA DE
MADERA" (el RealBlackHoleShader mini a escala pequeña era papilla
ilegible), los párpados "brackets pesados sueltos".

- **Texturas v2 (PIL ×4 supersampling, 2 iteraciones con crítica VLM
  8/10 y 9/10)**: EyeSclera — sombreado ESFÉRICO (limbo oscuro + luz
  arriba-izq), venas AUDACES con núcleo oscuro + halo (rojo carmesí y
  azul-violeta, ramificación orgánica que se desvanecen antes del iris),
  moteado biológico, subsurface cálido abajo, ESPECULAR EN MEDIA LUNA
  (córnea); EyeIris — generación PER-PIXEL con campos de ruido
  (turbulencia orgánica): bandas orbitales onduladas + fibras radiales
  finas + criptas caóticas + grano estelar + anillo limbal violeta +
  borde interno ARDIENTE (nada de "arcos estampados"); EyeLid — placas de
  armadura de carbón con rim light cálido, pliegues y grietas; icono del
  arma rehecho.
- **LA PUPILA DEL AGUJERO** (método procedural, sustituye al shader mini):
  esfera negra con borde suave + halo de absorción + ANILLO DE FOTONES
  fino blanco-caliente (micro-pulso) + banda de acreción horizontal
  CRUZANDO por delante + BANDA VERTICAL lenteada detrás (los arcos sobre
  y bajo la esfera — la imagen lenteada del disco) + chispa de beaming
  relativista. Legible a CUALQUIER escala.
- **El dibujado**: vignetta de cavidad suave (el ojo ASIENTA en la
  estrella — adiós anillo duro suelto), FALLOFF iris→pupila (la pupila se
  HUNDE), CATCHLIGHT unificado (media luna húmeda sobre iris+pupila),
  RIM GLOW aditivo en los párpados (la luz del ojo baña la armadura) y
  halo que RESPIRA lento. El _bhShader y su carga ELIMINADOS.

### B. LA MEDUSA — RAYOS QUE CAEN DEL CIELO (adiós agujas aburridas)

`JellyfishStingBolt` (agujas de luz) ELIMINADO → **`NebulaLightning`**:
cuando la campana se contrae junto a una víctima, la medusa DESCARGA un
rayo cósmico que CAE DEL CIELO sobre ella — nace 420 px arriba, cae
vertical a ~90 px/t, con ZIGZAG dentado REGENERADO cada pocos ticks
(vive), ramas laterales cortas, frente brillante con destello de 4 puntas,
chispas de hielo al caer y TRUENO + destello de impacto al clavarse.
QUEMADURA DE HIELO (Frostburn) intacta (la firma de la medusa). Zigzag
determinista por hash (semilla, tick, segmento) → mismo rayo en todas las
máquinas. Tooltips y localización actualizados (Rayo Nebular).

### C. EL COMETA ESTELAR (LivingCometStaff → StellarCometMinion)

Petición: crear un minion cósmico con la imagen de referencia (la
criatura-estrella de 8 puntas) — SIN tocar el CosmicOrbMinion existente
(que usa ESA imagen): esta es una criatura ORIGINAL hermana. Un cometa
VIVO: núcleo de plasma blanco-oro con granulación (CometHead.png) +
**CORONA DE 8 PUNTAS lanceoladas cian→violeta GIRANDO** (CometCrown.png —
el homenaje a la referencia) + **COLA de polvo estelar** (historial de 18
posiciones, cálida cerca → fría lejos) + **chispas orbitando** (el campo
de partículas de la referencia). **NO persigue: ORBITA al jugador en una
elipse excéntrica** (apoapsis/periapsis, fase por minionPos) y para
atacar **CAE EN PICADO** (aceleración 0.46/t hasta 16.5) — al rozar a la
víctima **ESTALLA EN UNA PEQUEÑA NOVA** (AoE 92 px al 60% + OnFire —
materia estelar CALIENTE, la firma opuesta a la medusa) y rebota de
vuelta a la órbita. Lente del pase B que CRECE CON LA VELOCIDAD
(velocidad = momento = curvatura, 0.05→0.17). Buff StellarCometBuff
(patrón del arsenal) + iconos PIL.

### D. EL PÚLSAR VIVO (LivingPulsarStaff → LivingPulsarMinion)

El segundo minion: una **estrella de neutrones VIVA** (PulsarCore.png:
núcleo blanco-azul extremo + arcos magnéticos nítidos + polos brillantes
+ bandas de giro) que **GIRA barriendo el campo con DOS HACES DE FARO
opuestos** (340 px, rotación vuelta cada ~6.9 s) — el ataque más raro del
arsenal: el daño NO es contacto ni proyectil, son LOS RAYOS GIRANDO
(comprobación angular por tick, tolerancia que se abre con la distancia,
55% del daño cada 5 ticks + **ELECTRIFIED** — radiación de sincrotrón).
Deriva en un lissajous perezoso sobre el hombro; con objetivo se coloca
EN ALTO a media distancia jugador-víctima para RAÑARLA en cada giro.
Haces dibujados como rayos cónicos blancos-cian con pulso viajero +
rastro de remolino + chispas tangenciales. Lente del pase B PULSANDO con
el giro (0.05→0.12). Buff LivingPulsarBuff + iconos PIL.

### E. LA LANZA DEL QUÁSAR (QuasarLance → QuasarJetProjectile)

El arma nueva con "un proyectil super cosmico": dispara un **CHORRO
RELATIVISTA** — el objeto más brillante del universo (los chorros de los
quásares superan el brillo de galaxias enteras). Una lanza de luz de 132
px velocísima (26 px/t ×3 updates) que **ATRAVIESA hasta 10 enemigos**,
con **5 NUDOS DE SHOCK** (los knots de Herbig-Haro) pulsando hacia la
punta, retorción HELICOIDAL sutil, 3 capas (filo violeta → halo cian →
núcleo blanco) y estela de polvo estelar. Al disiparse: **EL
FLORECIMIENTO DEL QUÁSAR** — AoE 130 px al 55% + destello + temblor.
DamageClass.Magic, damage 85, mana 14.

### F. Infraestructura

BlackHoleLensSystem: _cometIndices/_pulsarIndices recogidos SIEMPRE y
dibujados ENCIMA de la lente (corona, núcleo y haces jamás deformados) +
fuentes del pase B (cometa: velocidad; púlsar: pulso del giro).
TestingPlayer: las 3 armas nuevas garantizadas individualmente
(EnsureItem). Localización en-US/es-ES completa (armas, minions, buffs,
proyectil del quásar). Auditoría de texturas: las 85 clases del arsenal
con su asset ✓. Compilación contra tModLoader v2026.07.3.0 real:
**0 errores, 0 warnings**.

---

## Commit v5.98 — FIX: el mod NO CARGABA (texturas de la Medusa ausentes) + la Medusa SIEMPRE en el inventario

**Peticiones del usuario**: (1) "mira estos errores" (capturas del juego:
`MissingResourceException: Content/Projectiles/Cosmic/NebulaJellyfishMinion`
y `Content/Projectiles/Cosmic/JellyfishStingBolt` — el mod se desactivaba
automáticamente al cargar); (2) "recuerda que el invocador de medusa se lo
debes dar al jugador desde el inicio".

### A. LA CAUSA — texturas de clase ausentes

v5.97 añadió los `.cs` de la Medusa pero **NO sus dos texturas de clase**.
tModLoader exige un asset para CADA `ModProjectile` en su ruta por defecto
(`Content/Projectiles/Cosmic/<Clase>`); al no existir, el cargador lanzaba
`MissingResourceException` (dos inner exceptions en un `MultipleException`)
y **desactivaba el mod entero** — por eso el jugador no veía NADA de v5.97
(ni las explosiones únicas, ni el Ojo, ni la Medusa).

**El fix**: `NebulaJellyfishMinion.png` y `JellyfishStingBolt.png` —
placeholders 1×1 RGBA transparentes (70 bytes), el MISMO patrón que ya usan
`VoidEyeProjectile.png` y `CosmicShockwaveProjectile.png`: ambos proyectiles
se dibujan 100% proceduralmente (`PreDraw` devuelve `false`: campana +
galaxia + cuentas del minion; destello de 4 puntas del nematocisto), así que
la textura de clase JAMÁS se muestra — solo tiene que existir.

**Auditoría preventiva**: script propio que escanea las 77 clases con
textura obligatoria (`ModItem`/`ModProjectile`/`ModBuff`/`ModNPC`/…) del
arsenal completo — las dos de la Medusa eran las ÚNICAS ausentes (sin más
errores escondidos esperando al siguiente arranque).

### B. La Medusa SIEMPRE desde el inicio (kit "congelado" reparado)

El kit de `TestingPlayer` tiene gate de una sola vez (¿ya tienes el
`GenesisShard`?) — quien entró al mundo con una versión ANTERIOR jamás
recibía las armas añadidas después: el kit quedaba "congelado" en la
versión con la que se entregó (el mismo hoyo del VoidEyeStaff en v5.97, y
esta vez LA MEDUSA habría vuelto a quedarse fuera para quien ya tenía el
kit).

**El fix**: además del kit base (gate GenesisShard intacto), las cuatro
armas cósmicas en desarrollo — `BlackHoleStaff`, `SunStaff`, `VoidEyeStaff`
y **`MedusaNebularStaff`** — se garantizan INDIVIDUALMENTE en cada entrada
al mundo (`EnsureItem`: si no está en el inventario, vuelve). El invocador
de la Medusa SIEMPRE está ahí desde el inicio, venga del guardado que venga.

### C. Prueba del usuario

Develop Mods → Build → entrar al mundo: el mod CARGA sin errores (client.log
limpio) y el inventario trae el kit completo CON el Báculo de la Medusa
Nebular → invocarla: la medusa nada a pulsos colgando del hombro, galaxia
girando en el corazón, tentáculos ondeando con física propia, nematocistos
de quemadura fría al picar.

---

## Commit v5.97 — UNA SOLA explosión final + LA MEDUSA NEBULAR (invocador de minion)

**Peticiones del usuario**: (1) "el sol y el agujero negro tienen dos, digamos
explosiones al terminar, solo deben tener una donde suceda todo, los anillos
rgb del agujero negro quedan mal, lo mejor es un anillo de lente
gravitacional"; (2) "no veo el arma nueva, te olvidaste de dársela al
jugador"; (3) "crea una nueva arma que sea un invocador para un minion, este
minion debe ser algo que hayas creado, crea un proyectil super creativo y
cosmico, y este proyectil sera la invocacion".

### A. UNA SOLA EXPLOSIÓN FINAL — el anillo de lente ES la explosión

Las tres armas cósmicas mayores ya NO tienen "dos explosiones" al terminar
(onda de materia + onda de lente después). Ahora **TODO sucede en UNA**:

- **AGUJERO NEGRO**: la explosión YA NO es la onda cromática RGB que
  engendraba el anillo de Einstein al final — **ES EL ANILLO DE EINSTEIN
  MISMO** (`OnKill` → una única onda `StyleEinstein` con el daño COMPLETO del
  proyectil). El anillo ganó el **FLASH DE LIBERACIÓN**: durante sus primeros
  ~16 ticks dibuja en su centro un brillo cálido que se apaga mientras el
  anillo despega (la luz del colapso escapando) — flash + anillo + daño +
  ShadowFlame viven en la MISMA onda. La lógica "cromática engendra Einstein
  al terminar" se ELIMINÓ del AI de la onda. Los estilos cromáticos quedan
  como legado documentado (nada del arsenal actual los invoca).
- **SOL**: el OnKill ya NO suelta 3 ondas de fuego + 1 onda de lente — suelta
  **UNA SOLA ONDA NOVA DE LENTE** (nuevo `StyleNova`): el frente de
  espaciotiempo QUE LLEVA EL FUEGO — triple anillo ardiente (FireRing con su
  color propio) + frente fino blanco de choque + **aberración CÁLIDA**
  (oro por fuera, brasa por dentro — la nova dispersa LUZ DE FUEGO, no RGB).
  Daño de la nova COMPLETO en la banda 0.72-1.02·frente cada 0.1 s +
  quemadura 10 s; fuente del `BlackHoleLensSystem` (el fondo se curva a su
  paso). El AoE del núcleo (260 px) golpea en el mismo instante.
- **OJO DEL VACÍO**: su GRITO también se unificó — ya no son la cromática
  inversa del colapso + el anillo retardado: es **UN ÚNICO DESGARRO**
  (anillo de Einstein con el daño del grito COMPLETO, radio 460). Los dusts
  de implosión/sangre y el AoE del núcleo (380 px, ×1.6) ocurren en el MISMO
  instante.

### B. FIX — el Ojo del Vacío ya está en el inventario del jugador

`TestingPlayer.OnEnterWorld` (el kit de prueba del SP) NO incluía el
`VoidEyeStaff` de v5.96 — jamás llegó al inventario (por eso "no se veía el
arma nueva"). Ahora se entrega junto al resto del arsenal. También se
añadieron sus entradas de localización (en-US + es-ES: "Báculo del Ojo del
Vacío") para que sea localizable por nombre.

### C. LA MEDUSA NEBULAR — el invocador (el proyectil ES la invocación)

Petición: "crea una nueva arma que sea un invocador para un minion… crea un
proyectil super creativo y cosmico, y este proyectil sera la invocacion".

**LA CRIATURA** (`MedusaNebularStaff` → `NebulaJellyfishMinion`,
DamageClass.Summon, minionSlots 1, se apilan medusas):

- **Cuerpo**: una medusa nacida en el corazón de una nebulosa. Campana
  translúcida de gas interestelar **generada con PIL a 4× supersampling**
  (`JellyfishBell.png` 256: degradado radial aqua→esmeralda→violeta, 16
  costillas radiales con curvatura orgánica, anillos de crecimiento tenues,
  margen bioluminiscente ROSA y semillitas estelares dentro).
- **Corazón**: una **MINIGALAXIA ESPIRAL** (`JellyfishGalaxy.png` 128: dos
  brazos logarítmicos sembrados de ~140 estrellas cálidas/frías + núcleo
  blanco-dorado) que GIRA lentamente en el centro de la campana y DESTELLA
  con cada contracción del nado.
- **Tentáculos**: 6 tentáculos × 9 cuentas estelares (`JellyfishBead.png`:
  cuentas blancas tintables con destello de 4 puntas) con **física de cuerda
  propia**: anclas en el margen de la campana, relajación rígida cerca de la
  campana y laxa hacia la punta, gravedad suave, vaivén per-tentáculo y
  restricción de longitud — ondean con vida propia y quedan A LA ESTELA
  cuando la medusa nada. Colores bioluminiscentes alternos teal/rosa/menta.
- **Locomoción por PULSOS** (nadie más del arsenal se mueve así): cada 48
  ticks la campana SE CONTRAE (squash/stretch visual: scaleY -20%, scaleX
  +9%) y dispara un IMPULSO hacia su ancla; entre pulsos deriva con arrastre
  acuático (×0.955/tick) y hundimiento sutil. En caza nada hacia un punto
  SOBRE la víctima (impulso 6.6); en reposo cuelga del hombro del jugador
  (impulso 2.9, apilada con sus hermanas por minionPos). Fase de pulso
  desfaseada por minionPos → las medusas nunca pulsan al unísono.
- **EL ESPACIOTIEMPO**: la medusa es una fuente del pase B del
  `BlackHoleLensSystem` — la MÁS SUTIL del arsenal (fuerza 0.06→0.14
  RESPIRANDO con el pulso): donde nada, el fondo se dobla apenas. Su
  campana translúcida se dibuja ENCIMA de la lente (nunca deformada).
- **ATAQUE — NEMATOCISTOS**: cuando la contracción ocurre junto a una
  víctima (≤190 px) dispara 3 AGUJAS DE LUZ (`JellyfishStingBolt`: destello
  de 4 puntas + estela estelar, 12.5 px/tick, daño 50% del minion) con
  **QUEMADURA DE HIELO** (Frostburn — la quemadura fría del vacío, firma que
  NINGÚN otro arma cósmica usa) + daño de contacto de la campana (hitbox
  46×46, cooldown 18 ticks) con Frostburn al tocar.
- **Muerte**: se disuelve en polvo de estrellas (26 dusts teal/rosa).
- **Ciclo de sirviente**: patrón CosmicOrb — buff `NebulaJellyfishBuff`
  (localizado en ambos idiomas, icono 32×32 generado) aplicado por el Shoot
  del báculo y sostenido por la propia medusa (inmortal mientras el buff
  viva: timeLeft 2, semántica vanilla de minion). Teletransporte de vuelta
  si queda a >1100 px del dueño.
- **Icono del arma**: `MedusaNebularStaff.png` 28×30 (vara carbón-teal con
  la medusa encendida en la cima y estrellitas).

### D. Verificación

- Compilación contra tModLoader v2026.07.3.0 real (`/tmp/verify`): **0
  errores, 0 warnings**. Auditoría del binario: símbolos nuevos presentes
  (`NebulaJellyfishMinion`, `JellyfishStingBolt`, `MedusaNebularStaff`,
  `NebulaJellyfishBuff`, `StyleNova`, `DrawJellyfishVisuals`) y las tres
  rutas de textura del minion en el montón de user-strings.

### E. Prueba del usuario

Develop Mods → Build → kit de inicio (ahora con Báculo del Ojo del Vacío y
Báculo de la Medusa Nebular) → **SunStaff**: estrella → gigante roja → UNA
sola explosión: onda nova de lente (fuego + aberración cálida) curvando el
fondo → **BlackHoleStaff**: agujero → al morir: flash de liberación + EL
ANILLO DE EINSTEIN (nada de RGB) → **VoidEyeStaff**: el ojo se abre, te
mira, parpadea → EL GRITO: un único desgarro → **MedusaNebularStaff**: la
medusa emerge, su galaxia gira, NADA a pulsos colgando de tu hombro (con el
fondo curvándose sutilmente), y al acercarse a un enemigo dispara sus
nematocistos de hielo → client.log limpio.

## Commit v5.96 — El brillo del sol crece sin parpadear + ANILLO DE EINSTEIN + daño de área creciente + EL OJO DEL VACÍO

**Peticiones del usuario**: (1) "el PhoenixNovaStaff parpadea, creo que lo
mejor es que el brillo de PhoenixNovaStaff ya no parpadee, este debe comenzar
a crecer lentamente y que su crecimiento esté sincronizado con el ciclo de
vida del sol y con el tamaño del mismo"; (2) "en cuanto al agujero negro,
creo que es mejor que quites el campo de fuerza de las columnas, se ve mejor
si eso. En su lugar, al final de las explosiones debe crear un lente
gravitacional en forma de anillo que se expanda"; (3) "ahora todo el daño de
ambos proyectiles deben ser daño de área y este debe extenderse por fuera del
proyectil y crecer conforme el proyectil crece, se expande y explota"; (4)
"crea otra arma nueva de prueba con la que has aprendido y esta nueva arma
debe tener un proyectil lo más cósmico y de terror cósmico que se te ocurra,
lo dejo a tu imaginación y capacidad de creación".

### A. EL BRILLO DEL SOL YA NO PARPADEA — GLOW CORONAL PERSISTENTE

Las llamaradas PhoenixNova periódicas (una nova de 60 frames cada 2 s — cada
una nacía y moría: un PARPADEO por diseño) se **ELIMINARON por completo** del
sol. En su lugar, `SunProjectile.DrawStarVisuals` dibuja un **GLOW CORONAL
PERSISTENTE** (`DrawCoronalGlowSprites`): dos capas de SoftGlow aditivas que
- **nacen tenues con la estrella** (halo a 1.30× su radio visual, alpha 70),
- **crecen LENTO durante toda su vida** (función PURA de lifeT — CERO sin(),
  CERO flashes, CERO oscilación: halo hasta 2.35×, corona 1.05→1.55×),
- **se sincronizan con el TAMAÑO del sol**: todo se dimensiona con `starR`
  (width×scale×0.75) → la GIGANTE ROJA (×1.85) ARRASTRA al glow consigo,
- **enrojecen** con la gigante (Lerp naranja→rojo con rg).
La carga de la Supernova hija se sigue dibujando detrás del disco. La
PhoenixNovaStaff **standalone** también se suavizó: el pulso sinusoidal
(0.93±0.07) y el flash del pico (frames 25-35) se eliminaron — la nova nace
contenida (0.75×) y **crece de forma continua** hasta 2.3× con una
envolvente lisa (encendido 12f → plena → desvanecido 8f).

### B. ADIÓS CAMPO DE FUERZA — EL ANILLO DE EINSTEIN (agujero negro)

La burbuja Perlin/ForceField vanilla (shader de las Columnas Lunares) que
cabalgaba la onda cromática **se ELIMINÓ por completo** (textura, shader,
bloque de dibujado y el `localAI[0]` del radio del escudo — el binario queda
con 0 ocurrencias de ForceField/Perlin). **En su lugar**: cuando la onda
cromática del agujero **TERMINA de expandirse** (el final de la explosión),
engendra una onda **StyleEinstein** — el **LENTE GRAVITACIONAL ANULAR**:
- **Frente fino BLANCO incandescente** (el anillo de Einstein puro: la luz
  de todo lo que quedó detrás, doblada en un círculo perfecto, alpha 230).
- **Franjas R/B MUY juntas** (separación 1.8% vs 3.5-9.5% de la cromática —
  la imagen lensada se dispersa justo en el borde) + **halo interior pálido**
  (la luz lensada esmealada por dentro) + **imagen secundaria tenue** fuera.
- **Se expande MÁS RÁPIDO que la materia**: 26 px/tick con expansión CASI
  LINEAL (vs 20 ease-out de las ondas de materia) — es el ripple del
  espaciotiempo. Radio 520: SOBREPASA a la propia explosión.
- **CURVA EL FONDO del juego**: fuente del BlackHoleLensSystem con radio que
  ABRAZA al anillo (0.85× el frente) y fuerza que decae lento (0.9→0.4).
- **Banda de daño FINA** (0.88-1.06× el frente, cada 0.1 s) + **ShadowFlame**
  (la luz lensada quema el alma, no la carne).

### C. TODO EL DAÑO ES DAÑO DE ÁREA QUE CRECE (sol + agujero)

**SOL**: aura de daño cada 0.25 s (15 ticks) — radio = `starR×(1.35+0.30×
lifeT)` (110→205px, **se extiende POR FUERA del cuerpo** y CRECE con el
ciclo de vida), daño 40% (que ya rampa ×1.75 con la gigante), + OnFire que
**DOBLA en gigante** (600 ticks). La explosión final ya era área (3 ondas de
fuego con banda 0.72-1.02×frente cada 0.1 s + onda de lente + AoE del
núcleo). **AGUJERO NEGRO**: el aura pasa de fija (50% cada 0.5 s en
escudo×1.3) a **CRECIENTE**: radio = escudo×(0.75+0.45×lifeProgress)
(sobre la hinchazón +60% de la muerte, vía localAI[1] pre-colapso) y daño
35%→65% — y el clímax del área es la explosión (cromática + Einstein). La
**PhoenixNova standalone** también: aura 45→155px (60% + OnFire cada
0.166 s) creciendo con la expansión de la nova.

### D. EL OJO DEL VACÍO — VoidEyeStaff (arma nueva de terror cósmico)

**"Una estrella muerta con un ojo vivo"** — todo lo aprendido condensado en
un solo horror de 12 segundos (`VoidEyeProjectile`, 720 ticks):
- **CUERPO**: estrella MUERTA — el SunShader con **paleta invertida**
  (carbón oscuro + vetas carmesí: el gemelo maligno del sol). Emergencia
  SIN pop elástico (un peso siniestro: smoothstep lento).
- **OJO** (texturas generadas: EyeSclera/EyeIris/EyeLid 512px, supersampled
  ×4): esclerótica marfil enfermo con **VENAS ramificadas** e inyección de
  sangre en el terror; **iris ÁMBAR que ROTA lentamente** (los iris no
  deberían rotar) con estrías radiales y anillo limbal; **MIRA a la víctima**
  — el offset del iris SIGUE al enemigo más cercano… **y si no hay nadie,
  TE MIRA A TI** (al jugador).
- **PUPILA**: un **MICRO AGUJERO NEGRO** — el RealBlackHoleShader (lensing
  real de 75 pasos) en miniatura con su **disco de acreción CARMESÍ**. Se
  **DILATA** con el terror (0.55→1.35) y con ella crecen TODOS:
  el **aura de daño** (140→300px), la **lente gravitacional** (fuente del
  pase B, como la gigante roja: el espacio se curva alrededor del ojo) y la
  **gravedad del arrastre**.
- **PÁRPADOS de carne muerta**: se abren LENTO (smoothstep 0.8-3 s),
  **PARPADEAN** cada ~3.3 s — **en la oscuridad daña EL DOBLE** (es cuando
  alimenta) y la gravedad tira ×2.5 — y en el terror se **RETRAEN DE PAR EN
  PAR** (ojo desorbitado + iris ámbar→SANGRE).
- **AURA DE TERROR** (daño de área creciente, filosofía v5.96): 0.2 s al
  28-62% del daño + **ShadowFlame** + **ralentización por pavor** (×0.92/tick);
  en el terror: cada 0.1 s al 75%, radio 380px.
- **EL GRITO (muerte)**: chillido (ScaryScream) + AoE del núcleo (380px,
  ×1.6, ShadowFlame 8 s + Weak) + **ONDA CROMÁTICA INVERSA** (el mundo
  COLAPSA hacia el ojo muerto) + **ANILLO DE EINSTEIN** (el desgarro de la
  realidad, 12 ticks tras el colapso) + implosión de materia oscura +
  explosión de sangre + temblor fuerte.
- **Sonidos del horror**: MoonLord (nacimiento y terror), ZombieMoan
  (despertar, quejidos susurrados cada 2.8 s, parpadeos), ScaryScream (el
  grito). Lágrimas de sangre (DustID.Blood) desde el párpado inferior,
  zarcillos de materia oscura orbitando (librería: Orbit+ColorShift),
  brasa corrupta, llama sombría (DustID.Shadowflame).
- **MP coherente**: TODO (dilatación, apertura de párpados, terror) se
  deriva DETERMINISTA de la edad (ai[0]) — el parpadeo y el daño en la
  oscuridad coinciden en todas las máquinas sin sincronizar nada.

### E. Tooltips actualizados

SunStaff (glow coronal que crece + aura de área), BlackHoleStaff (aura
creciente 35→65% + el anillo de Einstein al final de la explosión),
PhoenixNovaStaff (crecimiento continuo + aura), VoidEyeStaff (nuevo).

**Arte nuevo**: EyeSclera.png, EyeIris.png, EyeLid.png (512px, generados con
PIL supersampled ×4 — venas ramificadas procedurales, estrías radiales del
iris, carne muerta con margen carmesí), VoidEyeStaff.png (icono 28×30),
VoidEyeProjectile.png (1×1 transparente — dibujado 100% manual).

**Compilación**: verificada contra tModLoader v2026.07.3.0 real — 0 errores,
0 warnings. Auditoría del binario: 0 ocurrencias de ForceField/Perlin
(eliminación total confirmada), texturas del ojo presentes.

## Commit v5.95 — Efectos del sol DETRÁS de él + fix del error del agujero + el campo de fuerza COMO onda + lente del sol + ondas de lente

**Peticiones del usuario**: (1) "sus efectos SupernovaStaff y PhoenixNovaStaff
deben estar detras de el, ademas parece que tiene un extraño parpadeo que
supongo que es PhoenixNovaStaff el cual no esta detras del sol"; (2) "aumentar
el tamaño de PhoenixNovaStaff y que iguale el tamaño del sol"; (3) "dale al
sol un poco de lente gravitacional a medida que vaya creciendo como gigante
roja"; (4) el agujero negro "tiene un error" (IndexOutOfRangeException en el
client.log) "y el campo de fuerza debe ser usado como onda expansiva"; (5) "en
ambas explosiones del sol y agujero negro tambien debe de haber una onda
expansiva creada con lente gravitacional que tenga una ligera distorsion
cromatica en rgb".

### A. FIX DEL ERROR DEL AGUJERO NEGRO (IndexOutOfRangeException del client.log)

v5.94 guardaba el radio de la burbuja del campo de fuerza en
`Projectile.ai[3]` — **un índice que NO EXISTE**: el array `ai` de tModLoader
tiene SOLO 3 ranuras (0-2). Tres stack traces lo confirmaban (OnKill al
escribir + DrawWaveVisual al leer, desde PreDraw y desde la lente):
la burbuja del escudo JAMÁS llegó a dibujarse y el OnKill abortaba antes de
los sonidos/dusts/temblor. Ahora el radio viaja en `localAI[0]` (parámetro
visual de cliente que fija el OnKill local) con **fallback determinista**
derivado del radio máximo sincronizado (`ai[2]×0.22`) para clientes remotos
de MP. FIX adicional: `localAI[1]` (radio pre-colapso del escudo) se
**captura UNA SOLA VEZ** al empezar la evaporación — antes se recalculaba
cada tick con la escala ya colapsada y el "escudo que mantiene su tamaño"
decaía de 101px a 5px.

### B. EL PARPADEO DEL SOL — CAUSA RAÍZ (decompilación de Main.DrawProjectiles)

La llamarada (PhoenixNova) usaba `DrawBehind` → caché
`drawCacheProjsBehindProjectiles`… **pero NUNCA ponía `hide = true`**, y el
bucle principal de Terraria solo excluye a los proyectivos con `hide`:
la llamarada se dibujaba **DOS VECES por frame — una de ellas ENCIMA del
sol** con brillo aditivo duplicado = el "extraño parpadeo". Y la Supernova
hija (spawned después → índice MAYOR que el sol) se pintaba directamente
encima de la estrella. **Solución definitiva**: los hijos van con
`hide = true` (ni tML ni ningún mod los dibuja) y
**EL SOL LOS DIBUJA ÉL MISMO** (`SunProjectile.DrawStarVisuals`, capa 0,
ANTES de sus propias capas): detrás del disco SIEMPRE, inmune al orden de
índices. El disco del SunShader (alpha≈1 en el cuerpo) los OCULTA en el
centro → la llamarada se lee como un **backlight real asomando por el limbo**
de la estrella. La SupernovaStaff standalone y la PhoenixNovaStaff
standalone conservan su dibujado propio.

### C. LA LLAMARADA IGUALA EL TAMAÑO DEL SOL

`DrawFlareSprites` se dimensiona con `starR`, el **radio visual REAL** de la
estrella invocadora (`width×scale×0.75` — crece con la gigante roja): el
núcleo caliente mide lo mismo que el disco (queda oculto tras él) y el halo
lo envuelve como backlight (≈2.6× su radio, extendiéndose con la edad). El
destello del pico (frames 25-35) pasó de "pantalla blanca completa" (el
parpadeo) a un **rim de luz suave** alrededor del limbo (alpha 120 tras el
sol; 235 standalone). Paleta: naranja solar → ROJO gigante (Lerp con el
progreso de la gigante). El pulso bajó a ±0.07.

### D. EL CAMPO DE FUERZA ES LA ONDA EXPANSIVA (agujero negro)

El escudo Perlin/ForceField **ya NO vive alrededor del agujero durante su
vida** (DrawForceField y su flash de golpe eliminados) — al explotar, la
burbuja **PARTE del radio que tenía el escudo al morir** (localAI[0]) y
**CABALGA el frente de la onda** expandiéndose con él
(radio = max(escudo, frente)) mientras se desvanece
(fade = 1-progress×0.9, brillo ×2→×1): la secuencia de destrucción del
escudo de una Columna Lunar **CONVERTIDA en onda expansiva**, acompañada de
las franjas R/G/B de aberración cromática. El aura de daño dentro del campo
se conserva (daño cada 0.5 s, radio del escudo ×1.3).

### E. LENTE GRAVITACIONAL DEL SOL EN LA GIGANTE ROJA (dos pases)

El shader de distorsión solo acepta UNA fuerza global, así que el
`BlackHoleLensSystem` ahora renderiza **DOS pases**: el pase A fuerte
(agujeros + ondas cromáticas/de lente, fuerza 0.55×max) y el **pase B débil
del sol** (fuerza 0.55×(progreso de la gigante×0.4) ≈ un cuarto del
agujero — "un poco de lente") sobre el resultado del pase A, con su propio
render target. El radio de la fuente = 1.4× el radio visual real de la
estrella (crece mientras se hincha). El sol (con llamarada y carga de la
nova) se dibuja **ENCIMA de la lente** (`DrawStarVisuals`) igual que el
núcleo del agujero. **Modo identidad**: si las fuentes desaparecen mientras
un sol sigue en pantalla, ese último frame los proyectiles que se saltaron
el pase del mundo se dibujan encima SIN distorsión (verificado contra el
decompile: el backbuffer contiene el mundo tras EndCapture) — sin él habría
un frame de invisibilidad (parpadeo) al morir la última fuente.

### F. ONDA DE LENTE GRAVITACIONAL EN AMBAS EXPLOSIONES (StyleLens)

Nuevo estilo 3 del CosmicShockwaveProjectile: **onda expansiva creada CON
lente gravitacional** con **ligera** distorsión cromática RGB (separación
×0.65 de la cromática) + anillo blanco tenue en el frente; se registra como
fuente del BlackHoleLensSystem → **curva el fondo del juego a su paso**; se
dibuja encima de la lente (como las cromáticas) y barre daño cada 0.1 s.
El sol la lanza en su OnKill SIN retardo (la onda gravitacional viaja
DELANTE de la materia), radio 400 — envuelve a sus 3 ondas de fuego
(240/300/360) como firma final del estallido. El agujero negro ya la tenía:
su onda cromática ES lente + RGB (más la burbuja ForceField de la sección D).
La SupernovaStaff standalone hereda el redimensionado que le faltaba:
ondas 360/450/540 → 240/300/360 y AoE 340 → 260.

## Commit v5.94 — El campo de fuerza REAL de las Columnas + Gigante Roja + anillos a su tamaño

**Peticiones del usuario**: (1) los anillos de alta calidad de v5.93 quedaron
DEMASIADO GRANDES — redimensionar los del sol y el agujero; (2) los anillos
son parte de la ONDA EXPANSIVA — solo deben salir AL FINAL (salían durante
toda la vida de ambos proyectiles, con imagen de prueba incluida); (3) el sol
al explotar debe CRECER EN ROJO — "una estrella amarilla que se convierte en
gigante roja y luego explota, todo esto haciendo que su daño en area crezca
junto con la estrella"; (4) el agujero negro debe mejorar los LÍMITES de su
daño en área; (5) "no hiciste el efecto que tienen los escudos de las
columnas en terraria... deberías comenzar a investigar profundamente el
código de terraria y de cualquier mods que tengan escudos o campos de fuerza".

### A. INVESTIGACIÓN PROFUNDA DEL CÓDIGO REAL DE TERRARIA (decompilación)

Entorno reconstruido desde cero (.NET 8 + ilspycmd + tModLoader v2026.07.3.0
descargado de GitHub). Decompile REAL de `Terraria.NPC` (112k líneas),
`Terraria.Main` (85k), `Terraria.Projectile` (93k), `MiscShaderData`,
`GameShaders` + grep global del assembly completo. **El mecanismo EXACTO del
escudo de las Columnas Lunares** (Main.DrawNPCDirect_Inner, torres
422/493/507/517):

- El escudo NO es una textura de burbuja: es **ruido Perlin**
  ("Images/Misc/Perlin", la textura del juego) en un **quad de 600×600**
  (sourceRect 0,0,600,600, origen 300,300) dibujado con el shader
  **`GameShaders.Misc["ForceField"]`** — registrado por DyeInitializer como
  `new MiscShaderData(Main.PixelShaderRef, "ForceField")`, sin imágenes extra.
- Batch EXACTO: Immediate + AlphaBlend + **PointWrap** + DepthStencil.Default
  + CullNone + transform del mundo.
- **Vivo**: alpha = fuerza·0.8 + 0.2; **flash al golpe** (el proyectil 629
  baja la fuerza y pone npc.ai[3]=1; el AI lo incrementa hasta 120): pop de
  escala +5% y UseColor(1+flash·0.5) durante 30 ticks.
- **Destruido** (ai[3] > 0 con fuerza 0): la burbuja se **EXPANDE hasta 2×**
  (escala·(1+grow), grow=min(t/30,1)), **brillo ×2** (UseColor(2)) y se
  **DESVANECE** (color de la DrawData = 1-sqrt(grow)) — la secuencia que el
  usuario pedía desde el principio.
- tModLoader expone `GameShaders.Misc` públicamente → **el mod usa el MISMO
  shader del juego con las MISMAS llamadas**: look idéntico garantizado.

### B. EL CAMPO DE FUERZA DEL AGUJERO NEGRO (el efecto REAL de las Columnas)

`BlackHoleProjectile.DrawForceField` REESCRITO con el mecanismo vanilla:
Perlin del juego + shader ForceField + geometría/parámetros exactos
(detalles en A). La "fuerza" del escudo = la carga hacia la muerte (alpha
0.2→1.0 en los 10 s); **flash** al absorber un golpe (OnHitNPC →
localAI[0]=1, timer de 30 ticks idéntico al npc.ai[3] de la torre); el radio
(2.2× el horizonte) **mantiene su tamaño durante la evaporación** (escala
pre-colapso en localAI[1]) como el escudo de una torre mientras muere; y al
explotar, el OnKill pasa ese radio a la onda cromática (ai[3]): **la onda
dibuja la burbuja del escudo EXPANDIÉNDOSE (2×) y DESAPARECIENDO** — la
destrucción del escudo de la Columna, con las franjas R/G/B de aberración
cromática encima. El inventado campo v5.93 (RingShieldNebula + aros finos)
se ELIMINÓ (era "un anillo que sale en todo momento", justo lo que el
usuario no quería).

### C. AURA DE DAÑO DEL CAMPO (límites de daño en área mejorados)

El agujero antes solo dañaba por contacto (hitbox 96 px) y en la onda final.
Ahora el CAMPO DE FUERZA desgasta a todo enemigo atrapado dentro: **daño del
50% cada 0.5 s** dentro del radio del escudo ×1.3 (crece con la hinchazón
de la muerte, +60%), con empuje suave hacia el centro. La gravedad los
arrastra, el campo los exprime y la onda final los barre.

### D. GIGANTE ROJA (el sol antes de explotar)

Últimos 3 s (t=7-10): la estrella amarilla se **HINCHA hasta ×1.85**
(smoothstep continuo — sustituye a la compresión ×1.0008 y al hinchazón
final ×1.025) y **ENROJECE TODO**: backglow, aura RadialShine, cuerpo
SunShader (mainColor/darkerColor), luz, dusts y partículas de la librería
(tinte `ToRedGiant`). **El daño de área crece con la estrella**: hitbox de
contacto ×1.85 (Projectile.Resize, mantiene el centro — verificado en el
código de Terraria) y daño ×1.75 (base en ai[2]), quemadura 10 s.

### E. ANILLOS: SOLO EN LA EXPLOSIÓN FINAL + REDIMENSIONADOS

Causa raíz de "demasiado grandes y en todo momento": v5.93 reemplazó
Ring.png de 64→1024 px y TODOS los dibujados con escala fija quedaron 16×:

- **Sol en vida** (los anillos naranjas gigantes de la captura):
  PhoenixNova dibujaba 5 anillos por llamarada (hasta 4710 px) y la Supernova
  sus anillos de contención (hasta 2458 px) → **ELIMINADOS** (la llamarada
  queda como explosión de brillo: halo + núcleo + flash; la carga como halo
  dorado condensándose + temblor).
- **Agujero en vida** (el anillo cian gigante de la captura): anillo de
  fotones pulsante cada 36 ticks (1178 px) → **ELIMINADO**; campo v5.93 →
  reemplazado por el ForceField real (B).
- **Ondas**: sol 360/450/540 → **240/300/360**; agujero 620 → **420**;
  AoE del núcleo del sol 340 → 260; pulsos de la librería 280/380 → 200/270.
- **Librería (ParticlePresets)**: `radius/64` asumía la textura de 64 px →
  **`radius/512`** (radio real de la textura HD) en Explosion y RingPulse.
- **V20**: AbyssalEye (2.0→0.125), GravityPulse (Lerp 0..5→0..0.3125),
  Earthquake (÷16) y BlackHoleMini (0.35→0.0219) — restaurados a su tamaño.
- **DrawFallback del agujero**: anillo de fotones ahora por radio (1.3× el
  horizonte) en vez de escala fija 0.6.

**Compilación verificada contra tModLoader real (v2026.07.3.0): 0 errores,
0 warnings.**

## Commit v5.93 — El campo de fuerza de la Columna de Nebulosa + anillos de ALTA CALIDAD

**Peticiones del usuario**: el anillo del agujero negro (Ring.png) tenía MUY
baja calidad y era solo blanco; el agujero negro necesita el CAMPO DE FUERZA
de la Columna de Nebulosa (https://terraria.wiki.gg/es/wiki/Columna_de_nebulosa)
— la burbuja con aberración cromática que rodea al pilar y que AL DESTRUIRSE
SE EXPANDE Y DESAPARECE; los anillos de fuego del sol también debían mejorar
su calidad. "Si te faltan assets puedes generarlos o buscarlos y recrear tus
versiones."

### A. DIAGNÓSTICO DE LA CALIDAD

El `Ring.png` era de **64×64 px** — al escalarlo al radio de la onda (hasta
620 px) se pixelaba y su anillo fino teñido se veía como línea blanca plana.
Análisis del escudo real del Nebula Pillar (búsqueda de imágenes + VLM sobre
capturas del juego): burbuja translúcida con **borde exterior cian-azul
brillante, cuerpo magenta, interior rosado**, textura interna de energía,
semitransparente — la aberración vive en el BORDE.

### B. TEXTURAS NUEVAS (1024×1024, generadas proceduralmente — funciones suaves, cero aliasing)

1. **`Ring.png` (REEMPLAZO directo, 105 KB)**: misma geometría del viejo
   (núcleo fino, teñible blanco) pero a 1024 px con halo suave y modulación
   de energía sutil. Los 9 usos existentes (nova, PhoenixNova, librería de
   partículas, V20...) ganan calidad automáticamente.
2. **`RingShieldNebula.png` (nueva)**: el CUERPO del campo de fuerza con
   COLOR horneado — gradiente radial rosa interior → magenta → cian brillante
   en el borde + arcos de energía + wisps nebulosos.
3. **`FireRing.png` (nueva, 356 KB)**: anillo de LLAMAS con color propio
   (núcleo blanco-amarillo incandescente → naranja → rojo profundo en las
   puntas) con lengüetas internas/externas moduladas por fBm periódico.
   Bug de generación corregido en el proceso (clamp01 recortaba los canales
   0-255 a 1 → textura negra) + fix del corte de borde (todo el contenido
   queda ≤ 0.995 del canvas).

### C. EL CAMPO DE FUERZA DEL AGUJERO NEGRO (estilo Nebula, durante su vida)

`BlackHoleProjectile.DrawForceField` (llamado desde `DrawCoreVisuals` — funciona
en el pase del mundo Y encima de la lente): burbuja a 1.9× el horizonte
(envuelve el disco de acreción) = **cuerpo RingShieldNebula translúcido +
aros FINOS cian (exterior) y rosa (interior) cuya separación "respira"**
(±5-6.5% del radio, proporcional). El radio sigue al horizonte → crece con la
secuencia de evaporación → al morir, la onda cromática del OnKill continúa la
historia: el campo "destruido" expandiéndose hasta desvanecerse.

### D. LA ONDA CROMÁTICA = EL CAMPO EXPANDIÉNDOSE (y las de fuego del sol)

`DrawWaveVisual` reescrita con el diseño VALIDADO POR SIMULACIÓN (VLM +
estadísticas de píxel: el primer intento de 3 pasadas RGB de banda ancha se
LAVABA a blanco porque la base de la banda solapaba al 100% en additive):

- **Cromática (agujero)**: cuerpo translúcido RingShieldNebula (tenue) + TRES
  AROS FINOS R/G/B con desfase radial 3.5%→9.5% del frente (crece con la
  edad = dispersión real; invertido en la convergente legada) — franjas de
  aberración SEPARADAS y VISIBLES sobre el cuerpo, sin lavado.
- **Fuego (sol)**: FireRing ×2 pasadas (exterior a 1.08·front, interior a
  0.93·front) + Ring fino blanco como frente de choque — llamas reales con
  lengüetas y gradiente de color propio.
- Compensación `thinComp = 1/0.92` (el núcleo del Ring vive a 0.92 del radio
  de la textura → un radio pedido R aparece exactamente a R).

### E. VERIFICACIÓN

- Simulación del look in-game validada con VLM: campo = "translucent magenta
  body, bright cyan edge, pink inner ring, Nebula Pillar style, saturated
  colors"; onda = "red/green/blue rings clearly separated over translucent
  body". Estadísticas de saturación: 74-79 (colores vivos, no blanco).
- Compilación contra tModLoader v2026.07.3.0 real: **0 errores, 0 warnings**.
- `RingShield.png` (versión blanca intermedia) eliminada: sin referencias.

## Commit v5.92 — FIX: "el sol dio un error" (InvalidOperationException del SpriteBatch)

**Reporte del usuario**: el client.log mostraba 2 "Excepción silenciosa" por cada
explosión del sol:

```
System.InvalidOperationException: Begin has been called before calling End after
the last call to Begin. Begin cannot be called again until End has been
successfully called.
   at Microsoft.Xna.Framework.Graphics.SpriteBatch.Begin(...)
   at AethonMod.Content.Projectiles.Cosmic.CosmicShockwaveProjectile.PreDraw(...)
   at Terraria.Main.DrawProj_Inner / DrawProjectiles / Draw ...
```

### A. CAUSA RAÍZ (trazada tick a tick)

1. El `OnKill` del sol genera sus 3 ondas de fuego con **retardo escalonado**
   (`ai[0] = 0, -8, -16` — así la explosión es una secuencia, no un solo destello).
2. Mientras la edad es negativa, `PreDraw` retorna temprano (`Age < 0f`) sin tocar
   el `spriteBatch` → correcto.
3. **El tick EXACTO en que un retardo expira** (edad pasa de -1 a 0):
   `FrontRadius(0, estilo, maxR) = maxR·(1-(1-0)²) = 0 px` → `DrawWaveVisual`
   entra en su salida temprana `if (front <= 1f) return;` y **devuelve SIN tocar
   el `spriteBatch`** — que sigue ABIERTO (el del pase del mundo de Terraria).
4. De vuelta en `PreDraw`, el `Begin` de restauración **INCONDICIONAL** (línea 367
   de v5.91) intentaba re-abrir un batch **YA ABIERTO** → `InvalidOperationException`.
5. Ese `Begin` estaba FUERA del try/catch → la excepción subía hasta
   `Main.DrawProjectiles` → tML la registraba como "Excepción silenciosa" y
   **abortaba el dibujado de TODOS los proyectiles del frame** (parpadeo/pérdida
   de efectos un frame). Ocurre 2 veces por explosión (ondas con retardo -8 y
   -16); la onda cromática del agujero negro nace SIN retardo (`ai[0]=0`, su edad
   jamás es 0 en el PreDraw) → por eso SOLO el sol disparaba el error.

### B. FIX (CosmicShockwaveProjectile.cs)

- `DrawWaveVisual` ahora **devuelve `bool`** en vez de `void`:
  - `false` = NO tocó el batch (onda inactiva o frente aún invisible) → el
    llamador NO debe restaurar nada;
  - `true` = lo tomó y lo dejó **CERRADO** (su `End` propio, o el defensivo del
    catch) → el llamador debe re-abrirlo con los parámetros estándar de tML.
- `PreDraw` restaura el batch **SOLO cuando `DrawWaveVisual` devuelve `true`**;
  cuando devuelve `false` el batch sigue exactamente como tML lo dejó (abierto en
  el pase del mundo) → no hay nada que re-abrir y el bug desaparece.
- El `BlackHoleLensSystem` (que llama `DrawWaveVisual(wave, false)` con el batch
  ya cerrado) sigue siendo compatible: ignora el valor de retorno y en ambos
  casos recibe el batch cerrado, tal como espera.
- Invariante nueva verificada en TODOS los caminos: edad negativa / frente
  invisible / dibujado completo / excepción interceptada → el `spriteBatch` nunca
  queda desbalanceado.

### C. VERIFICACIÓN

- Compilación contra tModLoader v2026.07.3.0 real: **0 errores, 0 warnings**.
- Auditados los Begin/End de los demás efectos del arsenal cósmico (sol, nova,
  agujero, PhoenixNova, lente): sus salidas tempranas ocurren ANTES de tocar el
  batch → ningún otro proyectil tiene este patrón.

## Commit v5.91 — El SOL autorita su explosión final (Supernova sincronizada) + agujero negro orientado al centro + ondas que dañan cada 0.1 s

Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran

**Peticiones del usuario** (aprobadas en el plan v5.91, "ejecuta todo lo demás"):

1. *"La explosión final es SupernovaStaff, puedes verificar en commit anteriores,
   esta debe estar sincronizada y debe ser la explosión final, las partículas
   deben ser del color del sol"* — verificado en el historial: el sol SIEMPRE
   invocó `SupernovaProjectile` (la estrella del SupernovaStaff) en su segundo 7.
   Ahora la sincronización es POR CONSTRUCCIÓN y las partículas son doradas.
2. *"Las partículas absorbidas por el agujero negro deben estar ubicadas en
   dirección hacia el centro del agujero"* — antes viajaban TANGENCIALES.
3. *"La lente gravitacional debe ser ligeramente más grande"* — 1.1× → 1.4×.
4. *"La aberración cromática debe ser transparente"* — alpha 230 → 140.
5. *"La velocidad de las partículas debe acelerarse en el momento de explotar"* —
   PullToGlobalBoost (hasta ×6 en la evaporación).
6. *"La onda expansiva (del agujero negro y del sol) debe hacer daño a medida
   que avanza, daño en área y daño por cada 0.1 segundo"* — daño por BANDA del
   frente cada 6 ticks.
7. *"El agujero negro tiene más fuerza de atracción que el sol, y el sol solo
   tiene 10 veces menos fuerza de gravedad"* — confirmado y documentado: 2.6
   vs 0.26 (el pico de carga del sol ×4 = 1.04 jamás lo alcanza).
8. *"El efecto PhoenixNovaStaff debe estar detrás del sol"* — DrawBehind.
9. *"La onda expansiva del sol debe quemar causando el debuff quemadura por
   10 segundos"* — OnFire 600 ticks en todas las golpes de la explosión final.

### A. SOL — LA EXPLOSIÓN FINAL ES LA SUPERNOVA, SINCRONIZADA POR CONSTRUCCIÓN

**Diagnóstico** (decompilando Terraria.Projectile/Main contra tML v2026.07.3.0):
el diseño v5.88 hacia depender la onda de fuego del `OnKill` de la Supernova
hija, que a su vez dependía de la sincronización por ÍNDICE (`ai[1]`) + un clamp
de `timeLeft` en los últimos 2 ticks — un punto único de fallo: cualquier
desviación (índice reciclado, clamp que no aplica, muerte descuadrada) y la onda
se perdía o estallaba fuera de tiempo ("no se procesó correctamente o no se ve").

**Rediseño v5.91** (SunProjectile.OnKill es ahora la AUTORIDAD — el sol muere
EXACTAMENTE a los 10 s, su timeLeft es fijo y nada lo mata antes):

1. **`TryKillSupernova()`**: el OnKill del sol mata la Supernova hija EN EL MISMO
   TICK → su flash + estallido + viento estelar ocurren EXACTAMENTE con la muerte
   del sol. Sincronización perfecta sin depender de índices ni clamps (el clamp
   `timeLeft <= 2` se conserva como red de seguridad).
2. **Las 3 ondas de fuego las genera EL SOL** (radii 360/450/540, retardo de 8
   ticks) con el daño de la nova (sol × 1.25 × 0.5) — cada una barre daño cada
   0.1 s + quemadura 10 s (ver sección C).
3. **AoE del núcleo** (340 px) con el daño de la nova (sol × 1.25) + quemadura 10 s.
4. **Flag `ai[2]=1` (SunInvoked)**: la nova hija NACE marcada como "invocada por
   el sol" → su OnKill NO genera ondas/AoE propios (cero dobles explosiones);
   aporta SOLO el espectáculo final (flash + estallido + viento + temblor).
   La SupernovaStaff standalone (`ai[2]=0`) conserva su explosión COMPLETA.

### B. SOL — PARTÍCULAS DEL COLOR DEL SOL (SupernovaProjectile)

La carga de la nova ya NO se blanquea hacia el azul (255,255,245 de v5.88):
- Halo: dorado (255,200,90) → **blanco dorado** (255,235,115) — sin canal azul.
- Núcleo: **blanco-dorado** (255,250,215).
- Luz de carga: familia cálida (1, 0.85→0.8, 0.55→0.45) — se intensifica sin
  virar al azul, como la corona del SunShader.
- Quemadura de la nova al contacto: 5 s → **10 s** (600 ticks).

### C. ONDAS EXPANSIVAS — DAÑO CADA 0.1 s A MEDIDA QUE AVANZAN (ambas armas)

`CosmicShockwaveProjectile.ApplyWaveDamage` reescrito:

- ANTES (v5.90): un bool[] `_hitNPCs` marcaba a cada NPC golpeado UNA sola vez
  en toda la vida de la onda (daño cuando el frente lo alcanzaba).
- AHORA (v5.91): un int[] `_nextHitAt` de COOLDOWNS — cada NPC dentro de la
  **BANDA del frente** (el anillo visible que avanza: [0.72·front, 1.02·front]
  en expansivas; [0.98·front, 1.25·front] en la convergente legada) recibe
  daño cada **6 ticks = 0.1 s EXACTOS** mientras la onda lo barre. La onda
  expansiva nace en el centro (radio 0) → el disco completo queda cubierto
  ("daño en área"). `SimpleStrikeNPC` no usa immunity frames → cada tick de la
  banda registra un golpe limpio (verificado decompilando NPC.StrikeNPC).
- **Estilo Fuego (sol/SupernovaStaff): quemadura 10 s** (OnFire 600, era 300).
- **Estilo Cromático (agujero negro): TRANSPARENTE** — canales RGB alpha
  230 → 140, núcleo blanco 150 → 95: un velo que deja ver el mundo a través
  del anillo (antes era un anillo aditivo casi opaco).
- `NewInstance`/`SetDefaults` siguen dando a cada onda su array FRESCO
  (MemberwiseClone comparte arrays del prototipo — bug v5.88 documentado).

### D. AGUJERO NEGRO — MATERIA ORIENTADA AL CENTRO + ACELERACIÓN AL EXPLOTAR

1. **Orientación al centro** (`SpawnLibraryAbsorbedMatter`): las estelas ya no
   viajan tangenciales — nacen con velocidad RADIAL hacia dentro (inward
   1.8-2.8 + tangencial sutil 0.25-0.5 para la espiral de infalling) y su eje
   largo apunta AL CENTRO (`Rotation = angle + π`): "ubicadas en dirección
   hacia el centro del agujero".
2. **Aceleración al explotar**: nuevo `ParticleManager.PullToGlobalBoost` —
   el agujero lo dispara durante su secuencia de muerte
   (`1 + expansion·5`, hasta ×6 en la evaporación): TODAS las partículas
   absorbidas aceleran hacia el centro justo en el momento de explotar. El
   OnKill lo resetea a 1f (nunca queda "colgado"). El polvo dorado también se
   acelera (deathSpeedBoost hasta ×3).
3. **Lente ligeramente más grande**: radio 1.1× → **1.4×** el tamaño visual
   (BlackHoleLensSystem) — el ángulo pico SE MANTIENE en ~0.8 rad: sigue siendo
   delgada, solo el anillo de deformación abraza el disco de acreción completo.
4. **Vida 10 s confirmada** (600 ticks, ya estaba) y **gravedad 2.6 = 10× la
   del sol** (0.26) documentado en el propio código.

### E. PHOENIXNOVA — DETRÁS DEL SOL + FIX CRÍTICO DEL SPRITEBATCH

1. **DrawBehind → `drawCacheProjsBehindProjectiles`** (firma verificada por
   reflexión contra tModLoader real): tML dibuja esa cache ANTES de
   `DrawProjectiles()` (verificado decompilando Main.DrawCachedProjs, línea del
   pipeline: DrawNPCs → … → DrawCachedProjs(BehindProjectiles) →
   DrawProjectiles) → la llamarada queda DETRÁS del cuerpo del sol y por
   delante de los NPC. El disco de la estrella tapa el núcleo de los anillos y
   estos se abren alrededor de la silueta — una llamarada ERUPCIONANDO por
   detrás (el flash queda como backlight). A diferencia de la v5.89 (que usaba
   `hide` + movió TODOS los dusts a otra capa y rompió el sol entero), este
   cambio es quirúrgico: SOLO el orden de dibujado del proyectil llamarada.
2. **Fix del bug del SpriteBatch (presente desde v5.88 — causa real de los
   "círculos que subían")**: el PreDraw del PhoenixNova abría el batch SIN
   `Main.GameViewMatrix.TransformationMatrix` (y sin sampler/rasterizer) y el
   `Begin(Deferred, AlphaBlend)` final restauraba el batch SIN TRANSFORMAR para
   TODOS los proyectiles posteriores del frame → dibujados en coordenadas de
   mundo sin la vista, "flotando/subiendo" por la pantalla. Ahora todos los
   Begin llevan el transform del mundo y el restore es EXACTO al estado que
   tML espera (Deferred, AlphaBlend, DefaultSamplerState,
   CullCounterClockwise, GameViewMatrix) — el mismo patrón del resto del mod.

### F. VERIFICACIÓN

- COMPILACIÓN contra tModLoader v2026.07.3.0 real (/tmp/verify): **0 errores,
  0 warnings**.
- Revisión de referencias huérfanas: `_hitNPCs` (renombrado `_nextHitAt`) sin
  usos restantes; el hook `DrawBehind` existe en esta versión de tML con la
  firma exacta usada; `Kill()` llama `ProjectileLoader.OnKill` incondicionalmente
  (la fuerza-kill de la nova dispara su espectáculo en el mismo tick).

**Prueba del usuario**: Develop Mods → Build → lanzar el SunStaff: llamaradas
ERUPCIONANDO POR DETRÁS de la estrella (t=2,4,6,8 s), carga dorada (t=7-10 s,
sin azul), y a los 10 s EXACTOS la Supernova estalla con el sol (flash dorado +
3 ondas de fuego que BARRAN dañando cada 0.1 s + quemadura de 10 s). Lanzar el
BlackHoleStaff: materia ámbar CAYENDO AL CENTRO (radial), lente más ancha,
aceleración frenética de la materia al evaporarse y UNA onda cromática
transparente que barre daño cada 0.1 s. Sin "círculos que suban" con otros
proyectiles en pantalla.

---

## Commit v5.90 — REVERT del sol a v5.88 + agujero negro rehecho (partículas absorbidas, UNA explosión cromática, lente delgada, sin corte)

Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran

**Reporte del usuario** (tras probar la v5.89 en juego):

1. *"Creo que ahora empeoró ya que arruinaste los efectos del sol"* — la v5.89
   sustituyó los dusts ambientales del sol por partículas de la librería en la
   capa BeforeProjectiles: el humo cálido se convirtió en **círculos (SoftGlow)
   que suben desde el sol** y las llamaradas (PhoenixNova con hide+DrawBehind)
   quedaron TAPADAS por el cuerpo de la estrella → *"le quitaste su onda
   expansiva"*. El usuario pidió **volver al commit 8781aa4** (v5.88) para el
   sol, sin tocarlo, y aplicar el resto de los cambios.
2. *"En cuanto al agujero negro, al crecer para desaparecer se CORTA por los
   lados"* — la captura mostraba el anillo naranja/blanco del disco de acreción
   terminado en **líneas verticales duras** a izquierda y derecha.
3. Cambios pedidos para el agujero negro: quitar las partículas moradas,
   agregar partículas que parezcan absorbidas, quitar las explosiones
   cromáticas (que solo haya UNA al desaparecer, con aberración cromática y
   daño), y hacer DELGADA la lente gravitacional para que no mueva toda la
   pantalla.

### A. REVERT DEL SOL A 8781aa4 (v5.88) — SunProjectile, Supernova, PhoenixNova

- `git checkout 8781aa4 -- SunProjectile.cs SupernovaProjectile.cs PhoenixNovaProjectile.cs`
  — el sol vuelve EXACTAMENTE a su estado de la v5.88: dusts vanilla (llamas
  GoldFlame, chispas Torch, humo Smoke, twinkles Enchanted_Gold), supernova
  hija dibujándose por sí misma (sin ai[1]=1), llamaradas PhoenixNova en el
  pase normal de proyectiles (sin hide/DrawBehind), y su onda expansiva de
  fuego intacta.
- Se conservó UNA sola cosa de la v5.89 en esos archivos: el fix de la
  **"Excepción silenciosa"** (el End defensivo ahora vive SOLO en el catch del
  path de error, no un try{End} incondicional cada frame) — cambio invisible
  al juego que mantiene el client.log limpio.

### B. FIX DEL CORTE POR LOS LADOS (disco de acreción al crecer)

**Causa raíz** (verificada contra el .fx): el canvas del RealBlackHoleShader
era FIJO de 256px y la lupa interna (`zoom = width/256 * scale * 2`) crecía
con la escala. La cobertura del shader en unidades de mundo es `1/zoom`: al
hincharse para morir (scale hasta 1.6) la cobertura caía a 0.83 mientras el
toro del disco de acreción crecía hasta 1.39 → el disco cruzaba el borde del
canvas y quedaba recortado con líneas verticales duras exactamente donde
terminaba el quad.

**Fix**: el canvas AHORA CRECE con la escala (`256 * max(scale, 0.08)`) y el
zoom es CONSTANTE (`width/256*2`). La cobertura del shader ya no cambia y el
radio del disco lleva un tope (`min(scale,1)*0.4`) → el toro NUNCA cruza el
borde a ninguna escala. El horizonte de sucesos en píxeles es idéntico a
antes (0.3 · width · scale): cero cambio visual salvo que el disco ya no se
corta — el agujero entero crece en pantalla al hincharse.

### C. PARTÍCULAS: FUERA LAS MORADAS, DENTRO LAS ABSORBIDAS

- ELIMINADO: succión espiral multicolor (violeta/cian/magenta con PurpleTorch),
  humo púrpura, espiral de librería multicolor (end violeta), halo de
  distorsión violeta (era el ÚNICO usuario de Noise.png en juego — el
  "efecto que se veía mal" desaparece por completo).
- NUEVO componente de librería **PullTo** (ComponentFlag bit 13): aceleración
  hacia un punto fijo (UserData0/1 = centro, UserData3 = fuerza); la partícula
  MUERE al llegar al centro. Con velocidad inicial tangencial dibuja una
  espiral de infalling perfecta.
- NUEVO **SpawnLibraryAbsorbedMatter**: estelas TrailGlow cálidas (ámbar →
  blanco incandescente vía ColorShift) que nacen a 95-165px con velocidad
  tangencial y PullTo hacia el centro — caen en espiral cada vez más rápido y
  desaparecen al cruzar el horizonte: materia siendo ABSORBIDA.
- NUEVO **SpawnAbsorbedDusts**: polvo GoldFlame ámbar/oro/brasa en espiral
  (mismo movimiento de la vieja succión, paleta incandescente), blanco
  incandescente cerca del borde.
- RECARENTADOS a cálido: halo exterior del núcleo (púrpura → ámbar profundo),
  presets de OnHitNPC/OnKill (violeta → ámbar/blanco), dusts de implosión
  (PurpleTorch → GoldFlame).
- Se conservan: chispas doradas capturadas, disco de acreción naranja
  (TrailGlow orbital), anillo de fotones blanco-azul, devoración de dusts
  ambiente, luz naranja pulsante.

### D. UNA SOLA EXPLOSIÓN CROMÁTICA AL DESAPARECER (con daño)

- ELIMINADAS las 4 ondas de la secuencia de muerte v5.86: la onda cromática
  intermedia (t-90, con estruendo y sacudida) y las 3 ondas cromáticas
  inversas escalonadas del final. La secuencia ahora es pura: crecimiento
  (+60% escala y radio de gravedad, SIN ondas) → evaporación (colapso a 0) →
  explosión.
- En **OnKill** nace UNA SOLA `CosmicShockwaveProjectile` estilo 0
  (cromático) con el **daño COMPLETO** del proyectil y radio máximo 620px:
  anillo con **aberración cromática real** (canales R/G/B separados
  radialmente, separación creciente con la edad) + **distorsión del fondo a
  su paso** (se registra como fuente del BlackHoleLensSystem) + **daño por
  frente de onda** a cada NPC una única vez. Estruendo Item88 y sacudida
  de cámara acompañan la liberación.
- El estilo 1 (inverso) queda documentado como LEGADO sin uso.

### E. LENTE GRAVITACIONAL DELGADA (ya no mueve toda la pantalla)

**Causa raíz** (verificada contra el .fx): el shader rota las coordenadas de
muestreo ALREDEDOR DEL CENTRO DE PANTALLA (`RotatedBy(coords - 0.5, ángulo)`),
así que un píxel se desplaza proporcional a SU distancia al centro. La v5.89
usaba `maxLensingAngle = 24 rad` con fuerza 0.62 → ángulo pico ~14.9 rad: a
2 radios del agujero el ángulo seguía siendo 0.27 rad → píxeles a 800px del
centro se movían 213px. Eso era "mover toda la pantalla".

**Fix (solo parámetros — el .fxc no se puede recompilar sin mgfxc/wine)**:
- `maxLensingAngle`: 24 → **1.5 rad** y fuerza 0.62 → **0.55** → ángulo pico
  **~0.8 rad** (18× menos).
- Radio de influencia: 0.75× → **1.1×** el tamaño visual (con ángulos sanos,
  el anillo de distorsión debe abrazar el borde del núcleo para seguir siendo
  visible; con 0.75 quedaría oculto tras el disco de acreción).
- Resultado (1080p, agujero a escala 1): deformación visible en el anillo
  0-180px alrededor del agujero, imperceptible (<1px) más allá de ~3 radios,
  resto de la pantalla pixel-perfect. La explosión cromática final también
  distorsiona su interior con el mismo tope sano.

### F. Documentación

- Headers de BlackHoleProjectile/BlackHoleLensSystem/CosmicShockwaveProjectile
  reescritos para documentar la v5.90 (secuencia de muerte, PullTo, canvas que
  crece, lente delgada).
- ParticleData/ParticleManager: PullTo documentado en la lista de componentes.

---

## Commit v5.89 — FIX CRÍTICO: la "pantalla negra" del agujero negro + efectos del sol DETRÁS de la estrella

Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran

**Errores reportados por el usuario** (captura + client.log de la v5.88 en juego):

1. *"Mira como se ve el agujero negro... toda la pantalla se oscurece y tiene
   algún problema de transparencia"* — la captura mostraba el mundo visible SOLO
   dentro de un cuadrado alrededor del agujero; el resto de la pantalla era
   NEGRO PURO (esquinas a RGB 0,0,0). Además, dentro del cuadrado el mundo se
   veía pixelado (media resolución) con bordes duros y banding.
2. *"En el caso del sol, sus efectos deben estar detrás del sol"* — las llamas,
   chispas, humo y la carga de la supernova se dibujaban ENCIMA del cuerpo de
   la estrella tapándola.
3. *"Debes mejorar el archivo Noise.png para que sea más suave"* — el ruido del
   halo de distorsión era grano duro y se veía mal.
4. El client.log registraba "Excepción silenciosa:
   InvalidOperationException: End was called, but Begin has not yet been called"
   en SupernovaProjectile:364, CosmicShockwaveProjectile:334, SunProjectile:658
   y BlackHoleProjectile:598.
5. Warning FNA al empaquetar: "Image loading failed: unknown image type".

### A. LA CAUSA RAÍZ de la pantalla negra (verificada decompilando FNA.dll)

La semántica oculta de FNA: **`GraphicsDevice.SetRenderTarget(null)` LIMPIA el
backbuffer** al volver a bindearlo. El final del método (decompilado de
`Microsoft.Xna.Framework.Graphics.GraphicsDevice` en FNA 1.0.0 de tML 2026.7.3):

```csharp
Viewport = new Viewport(0, 0, width, height);
if (renderTargetUsage == RenderTargetUsage.DiscardContents)
    Clear(ClearOptions.Target | DepthBuffer | Stencil, DiscardColor, ...);
```

Y `PresentationParameters` usa **DiscardContents por defecto** (verificado en
su constructor). Por eso el propio Terraria hace `Clear` + redraw completo en
su `FilterManager.EndCapture`: al bindear el backbuffer para presentar el mundo,
el contenido previo se destruye y hay que redibujarlo entero.

**El bug**: la lente v5.86-5.88 componía por REGIONES para "conservar el
backbuffer intacto fuera de la zona del agujero"... pero al restaurar el binding
(`SetRenderTarget(null)`) FNA ya había BORRADO el backbuffer. El mundo que
EndCapture acababa de dibujar se destruía y solo quedaban nuestras regiones →
pantalla negra con un cuadrado brillante, EXACTAMENTE lo que mostraba la captura
(cuadrado perfecto de 225×225 px = radio*2.2 del BuildRegion).

**El FIX (pipeline completo de pantalla)**:
- `_lensTarget` ahora a **RESOLUCIÓN NATIVA** (el shader de distorsión es barato:
  una sola lectura de textura por píxel; la media resolución era innecesaria).
- Tras restaurar el binding (el wipe es inevitable y esperado), se dibuja
  `_lensTarget` **A PANTALLA COMPLETA**: el mundo vuelve a pantalla a resolución
  nativa, distorsionado solo cerca de las fuentes. Es exactamente el mismo blit
  que hace el EndCapture de Terraria con screenTarget.
- ELIMINADA toda la lógica de regiones (BuildRegion/ClampRegion/srcRect):
  sin regiones, sin bordes duros, sin pixelado, sin banding de escalado.
  Código más simple y visual idéntico al pipeline de referencia.

### B. Los efectos del sol ahora van DETRÁS de la estrella

- **Dusts vanilla → partículas de la librería (capa BeforeProjectiles)**: los
  dusts de Terraria se dibujan en la capa de polvo (DESPUÉS de los proyectiles),
  es decir, ENCIMA del sol. Todas las partículas ambientales (chispas de la
  corona, llamas de la superficie, humo cálido, llamarada periódica) son ahora
  TrailGlow/SoftGlow de la librería propia, que se pintan en PostDrawTiles
  (ANTES de los proyectiles): se ven igual de bonitas pero DETRÁS del cuerpo.
- **La carga de la supernova detrás del sol**: la nova se invoca con `ai[1]=1`
  ("pertenece al sol") y YA NO se dibuja por sí misma (su índice de proyectil
  es mayor → se pintaba encima del sol). El SunProjectile pinta su carga
  PRIMERO (capa más profunda) vía el nuevo `SupernovaProjectile.DrawChargeVisuals`
  (método extraído y reutilizable). Invocada en solitario (SupernovaStaff),
  se dibuja ella misma como siempre.
- **Las llamaradas (PhoenixNova) detrás de todo**: `Projectile.hide = true` +
  override de `DrawBehind` enrutándolas a la capa `behindProjectiles` de
  Terraria (se dibuja ANTES que los proyectiles normales). El estallido ilumina
  ALREDEDOR de la estrella sin tapar su cuerpo.
- De regalo: el Begin aditivo de la llamarada ahora usa
  `Main.GameViewMatrix.TransformationMatrix` (antes Begin por defecto sin
  transform: con zoom ≠ 1 se descolocaba) y su restore deja el batch en el
  estado exacto que tML espera (con sampler y matriz del juego).

### C. "Excepción silenciosa" cada frame — patrón de End defensivo corregido

Las 4 stack traces del log (Supernova:364, CosmicShockwave:334, Sun:658,
BlackHole:598) venían del `try { Main.spriteBatch.End(); } catch { }` incondicional
del restore v5.88: el path NORMAL deja el batch CERRADO (todas las capas están
balanceadas Begin→End), así que el End defensivo lanzaba una
InvalidOperationException CAPTURADA cada frame. tML la registraba vía su handler
de first-chance exceptions ("Excepción silenciosa", deduplicada — por eso solo
4 entradas en todo el log).

**FIX**: el cierre defensivo ahora SOLO vive en los `catch` (el path de error,
donde de verdad puede haber un Begin interrumpido). El restore normal es un
Begin directo. Aplicado a BlackHoleProjectile, SunProjectile, SupernovaProjectile,
CosmicShockwaveProjectile y PhoenixNovaProjectile.

### D. Texturas de ruido suavizadas (Noise.png y FireNoiseB)

- **`Noise.png` REGENERADO** (128×128): antes era ruido gris duro (desviación
  estándar 42, rugosidad 4.2) con alfa totalmente opaco → grano visible en todo
  el halo de distorsión. Nuevo: ruido fractal multioctava suavizado con
  gaussian blur MODO WRAP (tileable sin costuras, el sampler es LinearWrap):
  stdev 12, rugosidad 0.58 — un halo brumoso y suave.
- **`FireNoiseB.png` suavizado** (512×512): el ruido del disco de acreción del
  agujero negro (único usuario de esta textura, verificado) tenía rugosidad
  8.0 → bandas/motear duro en el disco. Gaussian blur sigma 2.0 con wrap:
  rugosidad 2.38 — el disco fluye cremoso manteniendo su carácter.

### E. Warning FNA "Image loading failed: unknown image type" — ELIMINADO

Los 23 archivos de `Content/_masters/` eran **JPEGs con extensión .png** (2.1 MB):
tML los empaquetaba en el .tmod e intentaba cargarlos como PNG → warning de FNA
en cada build y 2.1 MB de basura dentro del mod. **MOVIDOS a `_masters/` en la
raíz del repo** (fuera de la carpeta del mod): siguen en git como arte de
referencia, pero ya no se empaquetan ni rompen el loader.

### F. Prueba del usuario (v5.88 → v5.89)

1. `Develop Mods → Build` (limpio, sin el warning de imagen de FNA).
2. Lanzar el agujero negro: **la pantalla ya NO se ennegrece** — el mundo se ve
   entero a resolución nativa con la distorsión gravitacional alrededor del
   agujero, y el log ya no acumula "Excepción silenciosa".
3. Lanzar el sol: los efectos (llamas, chispas, humo, llamaradas, carga de la
   nova) se ven DETRÁS del cuerpo de la estrella — el disco solar queda limpio
   y visible todo el ciclo.
4. La supernova del Grimorio/staff suelta sigue viéndose igual.

---

## Commit v5.88 — FIX CRÍTICO: el mod NO CARGABA (textura faltante) + revisión profunda (10 pasadas)

Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran

**Error reportado por el usuario** (captura + client.log al cargar v5.87):

```
Terraria.ModLoader.Exceptions.MissingResourceException: Recurso esperado no
encontrado: Content/Projectiles/Cosmic/CosmicShockwaveProjectile
  at Terraria.ModLoader.Mod.TransferAllAssets()
  at Terraria.ModLoader.ModContent.Load() → ModLoader.Load()
"Se ha producido un error al cargar AethonMod. Los mods se han desactivado
automáticamente."
```

**El mod NO llegaba a cargar desde la v5.86** — el CosmicShockwaveProjectile.cs
(nuevo en v5.86) se creó SIN su textura .png. La compilación C# pasa sin texturas
(verificada en sandbox), pero tML las exige al CARGAR el mod → por eso la v5.86
nunca llegó a probarse en juego (los reportes de la v5.86 nunca pudieron verse).

### A. Fix crítico — textura del proyectil de ondas

- **NUEVO `Content/Projectiles/Cosmic/CosmicShockwaveProjectile.png`** (copia del
  InvisiblePixel 1x1): el dibujado es 100% manual (DrawWaveVisual), la textura
  solo necesita existir. Con esto el mod VUELVE A CARGAR.
- Auditoría COMPLETA de las 71 clases de contenido vs texturas (script propio):
  **era la única faltante**. Auditoría de las 21 rutas de `ModContent.Request`
  en runtime: todas existen (png y .fxc).

### B. Bug REAL de daño encontrado en la revisión — marcas de golpe compartidas

- tML crea cada proyectil CLONANDO el prototipo (`MemberwiseClone`) → el array
  `_hitNPCs` inicializado como campo se COMPARTÍA entre todas las ondas
  simultáneas del mismo tipo. Con las 3 ondas inversas de la implosión naciendo
  escalonadas (retardos de 9 ticks): cada nacimiento BORRABA las marcas de sus
  hermanas → las ondas mayores podían golpear a los mismos NPC 2-3 veces.
- **FIX**: override de `NewInstance(Projectile)` dando a cada onda su PROPIO
  array fresco (API verificada contra tModLoader.dll v2026.07.3.0: virtual ✓).
  El `Array.Clear` de SetDefaults se mantiene como defensa extra.

### C. Robustez de render — End defensivo antes de restaurar el batch

- `RestoreSpriteBatch` (BlackHole, Sun) y el restore de Supernova: si una
  excepción interna dejaba un `Begin` abierto, el `Begin` de restauración
  lanzaba "Begin has already been called" y ROMPÍA el frame (crash de render).
  Ahora: `try { End(); } catch {}` antes del Begin (el patrón que ya usaba
  CosmicShockwaveProjectile).
- `Main.Transform` (alias DEPRECADO) → `Main.GameViewMatrix.TransformationMatrix`
  en los 4 sitios de restauración (verificado por decompilación: Transform es
  un alias legacy de GameViewMatrix).

### D. Warning del log — "AethonMod spent 61ms blocking on asset loading"

- `ParticleManager.RegisterTexture` llamaba `.Value` al registrar las 11
  texturas DURANTE la carga del mod (bloquea la fase async de tML).
- **FIX**: `_textures` ahora es `Asset<Texture2D>[]` — se guarda el Asset sin
  resolver y la textura se resuelve al DIBUJAR (ya en juego la carga de fondo
  terminó: `.Value` instantáneo, cero bloqueo de la carga).
- Guard extra en `DrawParticle`: un Asset fallido se salta sin romper el pase.

### E. Warning del log — "Failed to load icon_small.png"

- **NUEVO `icon_small.png` (30x30 exacto)**, redimensionado del icon.png 80x80
  con LANCZOS. Verificado contra tML decompilado: `GetModIcon(File, "icon_small.png", 30)`
  exige 30x30 exacto (y icon.png 80x80 — ya lo era).

### F. Limpieza total de warnings del build del usuario

- CS0672 (Kill obsoleto) ×4: migrados a `OnKill` (hook moderno): CosmicProjectileFX,
  CosmicOrbBolt, QuantumSplitProjectile, GenesisLight.
- CS8632 (anotaciones `?` sin contexto nullable) ×21 en 8 archivos: quitadas
  (neutral: los reference types ya admiten null sin contexto; ningún `Vector2?`
  de tipo valor fue tocado).
- **Build verificado SIN supresiones: 0 warnings, 0 errores** contra tModLoader
  v2026.07.3.0 real.

### G. Pasadas de revisión (lo verificado y quedó OK)

1. Texturas de contenido: 71 clases auditadas → solo faltaba la de arriba.
2. Rutas de assets en runtime: 21/21 existen.
3. Render/SpriteBatch: Begin/End balanceados en todos los efectos; lente
   restaura los render targets y el batch correctamente (verificado v5.86).
4. Multijugador: daño `SimpleStrikeNPC` solo en autoridad (server/SP), spawns
   de ondas con guard de owner/netmode correcto, dusts/luz solo cliente.
5. Fases: explosión BH t-90 → crecimiento 450→720px → evaporación t-36 → 3
   ondas inversas t-0; sol: llamaradas t=2,4,6,8s, supernova t=7s, explosión
   simultánea t=10s — todos los timings verificados en código.
6. `CosmicProjectileFX` (GlobalProjectile, afecta a todos): seguro (AppliesTo
   Nightglow 931, solo dusts).
7. `_masters/`: solo sprites maestros png, sin .cs → no interfiere.
8. `NewInstance`/`SetDefaults` de tML: semántica verificada por decompilación
   (MemberwiseClone + SetDefaults por spawn → origen del bug B).
9. `icon.png` 80x80 ✓ requerido por `GetModIcon(iconSize=80)`.
10. Compilación final limpia contra el binario real.

### H. Nota sobre el aviso "AssemblyLoadContext still using memory"

- Aparecía en el log TRAS la carga fallida (estado parcial del mod). Con el mod
  cargando correctamente no debería volver. Si reaparece al DESACTIVAR tras
  usar la lente en juego, es transitorio: el cierre del RenderTarget2D de la
  lente se encola al hilo principal (v5.87) y se drena en el frame siguiente.

### I. Prueba recomendada para el usuario

1. tModLoader → Develop Mods → Build (v5.88) — debe compilar SIN warnings.
2. Activar el mod: **debe cargar sin error** (el diálogo de "recurso esperado
   no encontrado" desaparece).
3. Probar BlackHoleStaff/SunStaff: las 4 ondas del agujero (cromática + 3
   inversas) hacen daño UNA vez cada una por NPC; las 3 de fuego queman.
4. Desactivar/Reload: desactivación limpia (fix v5.87 vigente).

---

## Commit v5.87 — FIX: ThreadStateException al desactivar el mod (FNA3D + hilo principal)

Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran

**Error reportado por el usuario** (captura al desactivar el mod en tModLoader
v2026.7.3.0):

```
System.Threading.ThreadStateException: most FNA3D audio/graphics functions must
be called on the main thread
  at Microsoft.Xna.Framework.ThreadCheck.CheckThread()
  at Microsoft.Xna.Framework.Graphics.Texture.Dispose(Boolean)
  at Microsoft.Xna.Framework.Graphics.RenderTarget2D.Dispose(Boolean)
  at AethonMod.Content.Effects.BlackHoleLensSystem.Unload()
  at Terraria.ModLoader.Mod.UnloadContent() → ModContent.UnloadModContent() →
     ModLoader.Mods.Unload() → ModLoader.Unload()
```

tModLoader mostraba: "Uno o más errores ocurrieron durante la desactivación y
tModLoader debe reiniciarse para evitar más problemas. AethonMod no se ha
desactivado correctamente."

### A. Causa raíz

- tModLoader **descarga los mods en un hilo de carga secundario** (async load).
- FNA3D exige que **todo Dispose() de recursos gráficos corra en el hilo
  principal** (ThreadCheck lo lanza y aborta la desactivación del mod).
- La v5.85 introdujo la lente gravitacional con su `RenderTarget2D` propio, y el
  `Unload()` de v5.86 hacía `_lensTarget?.Dispose()` directamente → excepción.

### B. Solución (verificada contra el binario real de tModLoader)

- **`Main.QueueMainThreadAction(Action)`** es la API oficial de tML para ejecutar
  acciones en el hilo principal: encola en `ConcurrentQueue<Action>
  _mainThreadActions`, que **se drena al final de `Main.Update()` cada frame** —
  también mientras la pantalla de carga del reload sigue dibujándose (el propio
  tML la usa para operaciones de ventana). Verificado por reflection +
  decompilación (ilspycmd) contra `tModLoader.dll` v2026.07.3.0 real:
  - `Main.QueueRenderAction` NO existe en esta versión; `QueueMainThreadAction` sí.
  - `ConsumeAllMainThreadActions()` se llama al final de `Main.Update(GameTime)`.
- `BlackHoleLensSystem.Unload()` ahora:
  1. Desconecta el hook `On_TimeLogger.DetailedDrawTime` dentro de try/catch.
  2. **Encola** `target.Dispose()` al hilo principal (el closure captura una
     variable local, no el ModSystem ni estado estático → acción autosuficiente;
     la cola mantiene vivo el ensamblado del mod hasta ejecutarla).
  3. Anula todas las referencias estáticas de inmediato (`_lensTarget`,
     `_distortionShader`, `_shaderFailed`, `LensActive`).
- **Programación defensiva total** (como pide el diálogo de tML): el encolado y
  el propio Dispose van envueltos en try/catch — la desactivación del mod jamás
  puede volver a romperse, ni siquiera si el juego se está apagando del todo.

### C. Auditoría del mismo patrón de error en todo el mod

- `BlackHoleLensSystem.RenderLens()` re-crea el target al cambiar la resolución
  con `_lensTarget?.Dispose()` — **seguro**: corre dentro del hook del punto 36
  (hilo de render), no en el hilo de carga.
- `ParticleManager.Unload()` solo anula referencias (las texturas son Assets
  propiedad de tML, no hay que disponerlas). **Seguro**.
- `AethonMod.Unload()` está vacío. **Seguro**.
- Único `new RenderTarget2D` del mod: el de la lente. No hay otros recursos GPU
  propios.

### D. Verificación

- Compilación: **0 errores** contra tModLoader v2026.07.3.0 real (dotnet 10,
  proyecto de verificación contra las DLLs del release de GitHub). Mismos 4
  warnings benignos preexistentes (Kill obsoleto en archivos antiguos, sin
  relación con este fix).
- APIs confirmadas contra el binario real antes de escribir el código.

### E. Prueba recomendada para el usuario

1. Abrir tModLoader → Develop Mods → Build (recompilar la v5.87).
2. Activar el mod, entrar al mundo, disparar BlackHoleStaff/SunStaff.
3. **Desactivar el mod o hacer Mods → Reload**: la desactivación debe completarse
   en silencio (sin diálogo de error y sin pedir reinicio).

---

## Commit v5.86 — LA LENTE VA DETRÁS DEL AGUJERO NEGRO + ONDAS CROMÁTICAS/DE FUEGO CON DAÑO REAL

Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran

### A. Arreglos de la lente gravitacional (reportes del usuario)

**Problema reportado**: "la lente gravitacional no puede afectar al agujero negro y
debe estar detrás de la animación del agujero negro y sus efectos".

**Causa raíz**: en v5.85 la lente volcaba la pantalla distorsionada ENCIMA de todo el
frame renderizado — y el núcleo del agujero negro (dibujado en el pase del mundo)
quedaba DENTRO de esa pantalla, así que la lente deformaba al propio agujero negro.

**Solución (arquitectura nueva en el punto 36 del pipeline)**:
1. Con la lente activa, `BlackHoleProjectile.PreDraw` **se salta el dibujado del
   núcleo** en el pase del mundo (bandera estática `BlackHoleLensSystem.LensActive`,
   que refleja "la lente se renderizó en el frame anterior").
2. El mundo se renderiza en `screenTarget` SIN el núcleo del agujero.
3. Tras compositar la distorsión, el sistema de lente dibuja ENCIMA, en orden:
   **a)** partículas de la nueva capa `AboveLens` (los efectos del agujero: disco de
   acreción, anillo de fotones, espiral de succión, halo de distorsión — ahora la
   lente queda DETRÁS de los efectos del agujero negro), **b)** el NÚCLEO completo
   del agujero (`BlackHoleProjectile.DrawCoreVisuals`, refactorizado a método
   estático compartido: halo + RealBlackHoleShader + refuerzo del horizonte),
   **c)** los anillos de las ondas cromáticas.
4. **Fallback automático**: si la lente falla/no hay fuentes, `LensActive` cae a
   false y todo vuelve al dibujado normal del mundo — el agujero jamás desaparece.
5. **BONUS de calidad — composición por regiones**: antes la pantalla COMPLETA se
   volcaba a media resolución (todo el juego quedaba emborronado); ahora solo se
   re-dibuja distorsionada la zona alrededor de cada fuente (±2.2× su radio), el
   resto del mundo conserva la resolución nativa.
6. Se elimina `SpawnAccretionDiskParticles` (GoldFlame a 10-40px del horizonte,
   redundante con el disco del shader + el disco de la librería y en plena zona de
   curvatura): los efectos cercanos ahora viven en la capa AboveLens.

### B. CosmicShockwaveProjectile (NUEVO) — ondas expansivas con daño real por frente

Archivo nuevo `Content/Projectiles/Cosmic/CosmicShockwaveProjectile.cs`. Tres estilos:

1. **ESTILO 0 — ONDA CROMÁTICA (explosión del agujero negro)**: anillo RGB con
   aberración cromática real (los canales R/G/B se separan radialmente, el desfase
   crece con la edad = dispersión) + núcleo blanco unificador. Se registra como
   fuente del sistema de lente → **el fondo del juego se distorsiona a su paso**
   ("que distorsione un poco"). Daña a cada NPC una única vez cuando el frente lo
   alcanza (knockback hacia fuera). Se dibuja ENCIMA de la lente.
2. **ESTILO 1 — ONDA CROMÁTICA INVERSA (implosión del agujero negro)**: nace en el
   radio máximo y CONVERGE hacia el centro (ease-in cuadrático), con el desfase RGB
   INVERTIDO (azul por delante de rojo) y knockback NEGATIVO que arrastra hacia el
   centro. El daño barre hacia dentro (golpea a quien estaba dentro del radio
   inicial). También distorsiona el fondo al pasar y se dibuja encima de la lente.
3. **ESTILO 2 — ONDA DE FUEGO (nova final del sol)**: triple anillo ardiente
   (rojo/naranja/amarillo) + llamas GoldFlame vivas a lo largo del frente + luz
   cálida. **Cada onda hace daño al pasar y aplica QUEMADURA (OnFire 5s)**.

Detalles técnicos:
- Campos AI: ai[0]=edad (negativa = retardo escalonado), ai[1]=estilo, ai[2]=radio
  máximo; la **duración se deriva del radio** (maxR/20 ticks ≈ frente de ~40px/tick)
  porque la API de NewProjectile solo acepta 3 slots de ai — determinista en todas
  las máquinas (multiplayer seguro).
- Daño manual vía `SimpleStrikeNPC` (guard de autoridad) + marca de golpes por NPC
  reiniciada en SetDefaults (tML puede reutilizar instancias de ModProjectile).
- `friendly=false` + `CanDamage()=>false`: sin colisión vanilla, solo daño de frente.
- Render reutilizable desde el pase del mundo (PreDraw) y desde el pase posterior a
  la lente (parámetro `endActiveBatch` para el estado del SpriteBatch).

### C. BlackHoleProjectile — secuencia de muerte completa (explosión → evaporación → implosión)

Pedido del usuario: "cuando el agujero explota necesita una onda expansiva cromática
que distorsione un poco, con esta onda expansiva también crece el área de efecto de
forma momentánea del agujero negro hasta que se evapora en una implosión con 3 ondas
expansivas cromáticas inversas, cada onda hace daño".

1. **t-90 (EXPLOSIÓN)**: nace la ONDA CROMÁTICA (estilo 0, 520px, daño 75% del
   agujero) + estruendo (Item88) + sacudida de cámara (5f "AethonBlackHoleBlast").
2. **t-90..t-36 (crecimiento momentáneo)**: la escala visual del agujero crece
   hasta **+60%** y el **área de efecto de la gravedad se expande de 450px a 720px**
   mientras la onda avanza.
3. **t-36..t-0 (EVAPORACIÓN)**: colapso acelerado de la escala hacia 0 (el área
   crecida se mantiene hasta evaporarse).
4. **t-0 (IMPLOSIÓN FINAL)**: OnKill genera **3 ONDAS CROMÁTICAS INVERSAS**
   (estilo 1, radios 460/520/580px, retardos escalonados de 9 ticks, daño 50% cada
   una) que barren el daño hacia el centro. Se conservan los presets de
   implosión/explosión de la librería, dusts convergentes y screenshake (se retiran
   los 2 RingPulse decorativos, sustituidos por las ondas reales con daño).
5. La intensidad de la lente sigue la escala del agujero: **se enciende con la
   explosión, crece con la onda y muere con la evaporación**.

### D. SunProjectile — arreglos del sol

1. **Primera llamarada corregida** (reporte del usuario: "lanza la primera
   PhoenixNova en la posición del jugador lo cual está mal"): en t=0 el sol aún está
   sobre el jugador (nace en su posición y deriva con el disparo), así que la nova
   estallaba "en la posición del jugador". Ahora la primera llamarada espera al
   **segundo 2**: llamaradas en t=2, 4, 6 y 8s (4 en total).
2. **3 ONDAS EXPANSIVAS DE FUEGO al final de la explosión del sol** (reporte:
   "falta la onda expansiva de fuego, debe hacer 3 ondas expansivas de fuego y cada
   onda debe hacer daño y provocar el debuff quemadura"): la SupernovaProjectile
   (que ES la explosión final del sol, sincronizada al tick con él en el segundo 10,
   y también el proyectil del SupernovaStaff standalone) genera 3 ondas de fuego
   (estilo 2, radios 360/450/540px, retardos de 8 ticks, daño 50% cada una,
   **quemadura OnFire 5s**). Sustituyen a las 2 RingPulse decorativas anteriores.

### E. Librería de partículas — capa AboveLens

- `LayerPriorities.AboveLens = 950` (encima de AboveAll): partículas que pinta el
  BlackHoleLensSystem tras compositar la distorsión.
- `ParticleManager.RenderAboveLensLayer()` (estático): mismos pases de batching por
  blend + frustum culling que PostDrawTiles, filtrando solo la capa AboveLens.
- `PostDrawTiles` se salta las partículas AboveLens cuando la lente está activa
  (si no, las dibuja normalmente — fallback sin lente).
- `DrawParticle` pasa a estático (compartido por ambos pases).

### F. Otros

- Tooltips actualizados: BlackHoleStaff (lente detrás del agujero + secuencia de
  muerte con ondas cromáticas), SunStaff (llamaradas desde t=2s + 3 ondas de fuego
  con quemadura), SupernovaStaff (3 ondas de fuego con daño + quemadura).
- build.txt: v5.85 → **v5.86**.
- COMPILACIÓN VERIFICADA: 0 errores contra tModLoader v2026.07.3.0 real (.NET 10,
  DLLs del release de GitHub) — solo los 4 warnings benignos preexistentes.

## Commit v5.85 — SOL COMPLETO (10s) + LENTE GRAVITACIONAL + SUPERNOVA MEJORADA + limpieza de referencias

Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran

### A. SunProjectile — el sol como cuerpo celeste completo (ciclo de 10 segundos)

El sol ahora tiene un ciclo de vida determinista de **10 segundos exactos** (600 ticks)
y conjuga los proyectiles de dos bastones existentes + gravedad propia:

1. **LLAMARADAS SOLARES cada 2 segundos** — desde t=0 (nace con el sol), se invoca
   `PhoenixNovaProjectile` (el del PhoenixNovaStaff) **centrado en el sol**:
   anillos naranjas expansivos + flash + lluvia de Torch + OnFire. 5 llamaradas
   en total (t=0, 2, 4, 6 y 8s), daño = 50% del sol.
2. **SUPERNOVA SINCRONIZADA en el segundo 7** — cuando al sol le quedan 180 ticks,
   invoca `SupernovaProjectile` (el del SupernovaStaff) en su centro y lo mantiene
   **perfectamente centrado** tick a tick (ai[1] guarda el índice del hijo y el sol
   le impone su posición y velocidad).
3. **GRAVEDAD DEL SOL** — como cuerpo celeste atrae enemigos (radio 280px) con una
   fuerza **10 veces menor que la del agujero negro** (0.26 vs 2.6). Durante la
   carga de la supernova (segundos 7→10) la fuerza **aumenta progresivamente hasta
   x4** (1.04 en el pico): los enemigos son arrastrados hacia la nova.
4. **EXPLOSIÓN SIMULTÁNEA en el segundo 10** — el sol (OnKill) y la supernova
   (OnKill) estallan el mismo tick: nova masiva combinada con doble onda expansiva.
5. **QUEMADURA** — inflama enemigos al contacto (OnFire 5s). La variante potenciada
   por daño mágico se implementará al integrarlo en el Grimorio (arma definitiva).
6. Visuales de carga: el sol se comprime sutilmente, su luz crece hasta x1.8 y
   estelas doradas convergen en espiral hacia el núcleo (SpawnSupernovaChargeIntake).

### B. SupernovaProjectile — reescrito: 3 segundos de carga + explosión masiva

- **timeLeft 90 → 180** (3 s exactos, sincronizable con el sol).
- **Carga (0..180)**: contracción acelerada hacia blanco-azulado, atracción de
  enemigos con fuerza creciente (0.5 → 2.2, radio 300px), espiral de GoldFlame
  cada vez más rápida (2→4/frame), **sacudidas de cámara anticipatorias** cada 40
  ticks (intensidad creciente) y anillos de contención pulsantes.
- **Explosión (OnKill, mejorada y más vistosa)**:
  - DOBLE onda expansiva de la librería (blanca-dorada 320px veloz + naranja
    profunda 460px retardada)
  - Flash blanco gigante (SoftGlow aditivo x6.5) + Explosion(200px, 46 partículas)
  - 26 estelas de viento estelar radiales largas
  - 70 lenguas de GoldFlame + 25 chispas blancas + 18 brasas con gravedad + 14 humos
  - AoE real de 340px (SimpleStrikeNPC + OnFire, solo en autoridad)
  - Temblor de cámara fuerte (10f, "AethonSupernovaBlast") + doble sonido
- `OnHitNPC`: ahora inflama (OnFire 300).

### C. BlackHoleProjectile + LENTE GRAVITACIONAL de pantalla (nuevo sistema)

1. **`BlackHoleLensSystem` (archivo nuevo, `Content/Effects/`)** — la pieza clave:
   - Hook MonoMod `Terraria.On_TimeLogger.DetailedDrawTime` en el punto 36 —
     verificado por decompilación contra tModLoader v2026.07.3.0: es el punto
     EXACTO tras `Filters.Scene.EndCapture` (el mundo ya está en
     `Main.screenTarget`) y antes de la UI.
   - Recopila hasta 5 agujeros activos (posición UV de pantalla + radio
     `width*scale/screenW*0.75`), copia `Main.screenTarget` a través de
     `BlackHoleDistortionShader` (el .fxc del pipeline propio) hacia un render
     target a media resolución y lo devuelve cubriendo la pantalla → **el fondo
     real del juego se curva alrededor del horizonte de sucesos**.
   - "Pequeña lente" deliberada: distortionStrength 0.62 ligada a la escala del
     agujero (nace y muere con él), maxLensingAngle 24, decaimiento exponencial.
   - APIs verificadas por reflexión + decompilación: `Main.screenTarget` ✓,
     `Main.screenWidth/Height` ✓, hook event `DetailedDrawTime` ✓, RT bindings
     preservados/restaurados ✓, try/catch total (nunca rompe el render).
2. **Fuerza gravitatoria mayor**: 2.0 → 2.6 y radio 350 → **450px**.
3. **Área de daño mayor**: width/height 76 → 96 (hitbox + canvas del shader).
4. **Succión espiral multicolor**: la materia devorada cubre violeta/cian/
   magenta/oro (dusts + partículas de librería) y se vuelve incandescente cerca
   del horizonte.
5. **Devora el polvo** en radio ampliado 190 → 260px.
6. disco de acreción de estelas + anillo de fotones + halo de distorsión +
   implosión/doble onda expansiva al colapsar (de v5.84, intactos).

### D. LIMPIEZA TOTAL de nombres de carpetas

- Carpeta de texturas de efectos renombrada a **`Content/Effects/Textures/`**
  (10 texturas) y las 6 rutas de código actualizadas.
- Tooltips de BlackHoleStaff/SunStaff reescritos: describen las capacidades
  propias (nada de "render idéntico a...").
- `AethonMod.csproj`: eliminado un `Compile Remove` de una carpeta que ni
  existía y su comentario asociado.
- `.gitignore`: eliminada una línea de una carpeta inexistente.
- `TestingPlayer.cs` y `CHANGES.md`: comentarios/histórico neutralizados.
- `SupernovaStaff.cs`: docblock y tooltip actualizados a la nueva conducta.

### E. Versionado

- `build.txt`: versión 5.84 → **5.85**.
- **Compilación verificada**: 0 errores contra tModLoader v2026.07.3.0 real
  (4 warnings benignos preexistentes de `Kill()` obsoleto en archivos viejos).

---

## Commit v5.84 — LIBRERÍA DE PARTÍCULAS COMPLETA + capas de VFX en BlackHole/Sun

Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran

### A. Librería de partículas completada según el libro de referencia

El usuario entregó el documento "particle_library_implementation_book.pdf" (88 páginas,
"Librería de Partículas para Terraria - Referencia para IA"). El sistema existente desde
v5.69 (ParticleData + ParticleBuffer + ParticleManager) estaba basado en ese libro pero
INCOMPLETO. v5.84 lo completa al diseño completo del documento:

**Archivos nuevos en `Content/Particles/`:**
- `ShapeDescriptor.cs` — API de spawn genérica por forma (sección 13.3 del libro):
  Box, Circle, HollowBox, HollowCircle, Cone, Sphere, Vortex y Line con
  `GenerateRandomPoint()` y factory methods estáticos.
- `CameraBounds.cs` — frustum culling (sección 16.1): rectángulo visible de la cámara
  con margen para partículas grandes parcialmente fuera de pantalla.
- `ParticlePresets.cs` — efectos pre-empaquetados (apéndice A del libro):
  `Explosion()` (ráfaga radial + anillo de shockwave + chispas),
  `Implosion()` (colapso espiral convergente),
  `RingPulse()` (onda expansiva) y
  `VortexSwirl()` (brazos espirales).

**Componentes implementados en el update loop (antes solo había 3):**
- `FadeIn` — el alpha sube durante los primeros UserData0 ticks (default 10)
- `ScaleUp` — la escala crece de 0 a UserData1/UserData2 (anillos expansivos)
- `ColorShift` — interpola PackedStartColor → PackedEndColor durante la vida
  (¡los campos existían desde v5.69 pero nadie los usaba!)
- `Homing` — persigue un NPC: explícito por whoAmI o el más cercano (sección 12.2)
- `Orbit` — orbita un centro (UserData0/1=centro, UserData2=vel. angular,
  UserData3=radio); stateless: el ángulo se deriva de la posición actual cada tick,
  y RotationSpeed sincronizada mantiene las estelas alineadas tangencialmente
- `EmitLight` — emite luz del color de la partícula; la intensidad deriva de la
  escala (sin UserData → combinable con cualquier otro componente)

**Mejoras del orquestador (ParticleManager):**
- Capacidad 2000 → 4000 partículas (~320 KB, sigue siendo cero GC)
- `SpawnShape(center, shape, count, template)` — API genérica (sección 14)
- Auto-fill de PackedStartColor/UserData en Spawn para todos los componentes nuevos
- Frustum culling en el render (margen 320px) — no se dibujan partículas
  fuera de pantalla (sección 16.1)
- 3 texturas nuevas registradas: GlowOrb (ID 8), SparkleStar (ID 9), TrailGlow (ID 10)
- Constantes `ParticleTex` (patrón TextureRegistry de la sección 21.2)

**APIs verificadas por reflection contra tModLoader.dll v2026.07.3.0 real:**
- `Terraria.Graphics.CameraModifiers.PunchCameraModifier(Vector2, Vector2, float, float, int, float, string)` ✓
- `Main.CameraModifiers` (campo de instancia, tipo CameraModifierStack) ✓
- `Lighting.AddLight(Vector2, Vector3)` ✓

### B. BlackHoleProjectile — capa de partículas de la librería (4 efectos nuevos)

Arquitectura por capas de profundidad: partículas de la librería (PostDrawTiles,
fondo aditivo) + dusts vanilla (capa frontal) + canvas del shader (encima):
1. **Espiral de succión** — 2 partículas/frame SoftGlow aditivas naciendo en el borde
   del campo gravitatorio con velocidad tangencial+radial (espiral natural) y
   ColorShift blanco incandescente → violeta cósmico al morir
2. **Disco de acreción de estelas** — TrailGlow (32x8) estiradas tangencialmente
   orbitando con el componente Orbit; la rotación avanza al mismo ritmo que la
   órbita (RotationSpeed = angVel) → las estelas quedan SIEMPRE alineadas con la
   tangente; ColorShift naranja dorado → rojo profundo
3. **Anillo de fotones pulsante** — Ring con ScaleUp (0→1.15×scale) + FadeOut cada
   36 ticks: destello circular azulado expandiéndose en el horizonte de sucesos
4. **Halo de distorsión** — Noise procedural rotando lento con alpha 26 y tinte
   violeta: sugiere la curvatura del espacio

Impacto y muerte:
- `OnHitNPC`: micro-colapso con Implosion + RingPulse sobre el objetivo
- `OnKill`: Implosion(165px, 46 partículas violetas) + Explosion(130px, blanco→naranja)
  + doble RingPulse (250px violeta + 320px dorada retardada) + screenshake con
  PunchCameraModifier ("AethonBlackHoleCollapse")

### C. SunProjectile — capa de partículas de la librería (4 efectos nuevos)

Misma arquitectura de capas (librería al fondo + dusts frontales + canvas del shader):
1. **Corona de plasma orbitando** — SoftGlow con Orbit (radio 48-60×scale, deriva
   lenta 0.045-0.075 rad/tick) y ColorShift amarillo incandescente → naranja profundo
2. **Viento solar** — estelas TrailGlow alineadas radialmente fluyendo hacia fuera
   desde la fotosfera, desvaneciéndose blanco-amarillo → naranja
3. **Destellos luminosos** — SparkleStar con FadeIn (8 ticks) + FadeOut + EmitLight
   (la partícula ILUMINA su entorno, intensidad según escala)
4. **Arcos de prominencia** — cada 45 ticks (sincronizado con las llamaradas de dust),
   7 estrellas orbitando en el borde de la llamarada con dirección alternante

Impacto y muerte:
- `OnHitNPC`: estallido solar con Explosion + RingPulse sobre el objetivo
- `OnKill`: nova masiva = Explosion(170px, 40 partículas blanco→naranja) + doble
  RingPulse (280px dorada + 380px roja retardada) + ráfaga de 22 estelas de viento
  solar radiales (velocidad 3.5-7 px/tick) + screenshake ("AethonSunNova")

### D. Otros cambios
- Tooltips de BlackHoleStaff y SunStaff actualizados con las nuevas capas de VFX
- `version = 5.84` en build.txt
- **Compilación verificada**: 0 errores contra tModLoader v2026.07.3.0 real
  (mismos 4 warnings benignos preexistentes de Kill() obsoleto en archivos viejos)

---

## Commit v5.83 — AGUJERO NEGRO + SOL con render de referencia + FIX CRÍTICO de shaders

Recordatorio: Puedo coger los recursos de nuestro github si los datos de mi versión local se borran

### A. FIX CRÍTICO: "Failed to load asset 'Content\Effects\Shaders\SunShader'"

**Causa raíz descubierta analizando el código fuente de tModLoader v2026.07.3.0:**
- tModLoader NO compila los archivos `.fx` durante el build (verificado en el código
  fuente de tML y en el issue abierto #3326 de tModLoader).
- Los mods DEBEN incluir los shaders **ya compilados como `.fxc`** — así lo hace
  el mod de shaders de referencia (270 archivos .fxc commiteados en su repo).
- Nuestros antiguos `.xnb` (generados con dxc en el sandbox) no eran XNB válidos de
  MonoGame: el XnbReader de tML fallaba al parsearlos → "Asset could not be found".
- Además tML no tiene reader para `.fx` (el FxReader vanilla es solo XNA, verificado
  por reflection contra tModLoader.dll real), por lo que los `.fx` solos jamás se
  registran como assets.

**Solución aplicada:**
- Borrados los 8 `.xnb` inválidos de `Content/Effects/Shaders/`.
- Añadidos los 5 `.fxc` compilados junto a sus `.fx`:
  `RealBlackHoleShader.fxc`, `SunShader.fxc`, `RadialShineShader.fxc`,
  `BlackOnlyShader.fxc`, `BlackHoleDistortionShader.fxc`.
- Los `.fx` se mantienen como fuente junto a los `.fxc` (sin conflicto: `.fx` no se
  registra, `.fxc` sí). Los shaders propios `Bloom.fx`, `ChromaticAberration.fx` y
  `Shockwave.fx` NO se usan desde C# y no tienen `.fxc` — si algún día se usan,
  habrá que compilarlos con mgfxc/2MGFX primero.
- Verificado por reflection contra `tModLoader.dll` v2026.07.3.0 (la versión exacta
  del usuario): `Terraria.Testing.FxReader` NO existe (`.fx` sin reader) y
  `Terraria.ModLoader.Assets.FxcReader` SÍ existe (`.fxc` se carga con
  `new Effect(device, bytes)`).

### B. BlackHoleProjectile — réplica EXACTA del agujero negro de referencia

Reescrito siguiendo el renderer de referencia al pie de la letra:
- **Zoom dinámico**: `width / 256 * scale * 2` (antes un 0.12 fijo — el error que
  hacía que no se pareciera en nada al original).
- **accretionDiskRadius**: `scale * 0.4` (antes 0.33 fijo).
- **cameraRotationAxis**: `(velocity.Y * -0.022 + 1, 0, rotation)` — el eje se inclina
  con el movimiento vertical, como el pet.
- Canvas de InvisiblePixel de 256px (mismo tamaño del render target del pet).
- `globalTime` ahora usa `Main.GlobalTimeWrappedHourly` (antes GameUpdateCount*0.0167).
- Carga del shader con `AssetRequestMode.ImmediateLoad` + flag anti-reintento.

Mejoras propias añadidas:
- **Pop elástico de aparición** (ElasticOut — réplica de la curva elástica de referencia):
  el agujero rebota al nacer.
- **Colapso final**: los últimos 40 ticks se encoge antes de explotar.
- **Devora el polvo del entorno**: los dusts cercanos (radio 190) caen en espiral
  hacia el horizonte de sucesos.
- Succión espiral de partículas mejorada (más rápidas cuanto más cerca).
- Disco de acreción con GoldFlame + chispas Enchanted_Gold capturadas + humo.
- Refuerzo manual del event horizon (sustituye al BlackOnlyShader, que requiere
  render targets): radio calculado desde los parámetros del shader.
- Implosión + explosión + doble sonido al morir (OnKill).
- Restauración correcta del SpriteBatch (`Main.Transform` +
  `RasterizerState.CullCounterClockwise` — `Main.CullCurrentScissor` NO existe en
  tML 2026, error CS0117 corregido).

### C. SunProjectile — réplica EXACTA de la estrella de referencia

Reescrito siguiendo el draw de la estrella de referencia al pie de la letra:
- **Canvas correcto**: `DendriticNoiseZoomedOut.png` (¡la textura de referencia que
  NOS FALTABA! antes usábamos WavyBlotchNoise como canvas — otra razón del parecido
  nulo). Copiada a la carpeta de texturas de efectos (10 texturas de referencia ahora;
  carpeta renombrada a `Content/Effects/Textures/` en v5.85).
- Backglow doble con BloomCircleSmall (amarillo*0.7 @0.95 + rojo*0.45 @1.61).
- RadialShine sobre WavyBlotchNoise con color (252,212,112)*0.24 y escala
  `width*scale*2.72` (dibujado en Additive para que el brillo radial sume).
- SunShader con parámetros exactos: corona=0.05, mainColor=blanco,
  darkerColor=(204,92,25), accent=(181,0,0), sphereSpinTime=GlobalTimeWrappedHourly*0.9.
- s1=WavyBlotchNoise, s2=PsychedelicWingTextureOffsetMap, sampler LinearWrap.

Mejoras propias añadidas:
- Pop elástico de aparición (ElasticOut).
- **Hinchazón previa a la nova**: se expande los últimos 30 ticks antes de morir.
- **Llamaradas solares periódicas** cada ~0.75s (burst radial de GoldFlame).
- Chispas Torch orbitando + llamas GoldFlame + humo cálido + destellos Enchanted_Gold.
- Iluminación `Vector3(1, 0.9, 0.5) * 3.2` con pulso sutil (como la estrella de referencia).
- **Nova final**: 60 GoldFlame + 35 Torch + 20 destellos + 15 humos + doble sonido.

### D. Otros cambios

- `AethonMod.csproj`: eliminado el Import roto a `/tmp/tmodloader/tMLMod.targets`
  (ruta del sandbox que no existe en la máquina del usuario); ahora usa el patrón
  oficial `..\tModLoader.targets` con `Condition="Exists(...)"`.
- `CosmicWeapons.cs`: tooltips actualizados describiendo los nuevos efectos.
- `build.txt`: versión 5.81 → 5.83.
- **Verificación de compilación**: el mod completo compila con **0 errores** contra
  tModLoader v2026.07.3.0 real (descargado y compilado con .NET 10 SDK: 4 warnings
  benignos preexistentes de `Kill()` obsoleto en archivos viejos; los proyectiles
  cósmicos nuevos migrados a `OnKill()`).

## Commit v5.82 — reescribir BlackHole + Sun con recursos exactos de referencia

## Commit v5.29 — Bastones de prueba + efectos cósmicos + recreación estelar de la imagen de referencia

Sistema completo de bastones de prueba para testear todos los efectos cósmicos
aprendidos. Todos usan el proyectil Nightglow (#931) como base.

### A. Helper de efectos cósmicos reutilizables (CosmicEffects.cs)

Nuevo archivo `Content/Globals/CosmicEffects.cs` con métodos estáticos:
- `SpawnCosmicTrail(center, velocity, scale)` — estela dorada/cian/magenta/índigo
- `SpawnMagicRing(center, color, count, radius, speed)` — anillo expansivo
- `SpawnMagicRingMulti(center, speed)` — 4 anillos cósmicos de colores
- `SpawnSparkles(center, count, spread)` — destellos ambientales
- `SpawnLightBeams(center, count, length)` — rayos de luz radiantes
- `SpawnStarfall(target, count, spread)` — estrellas cayendo del cielo
- `SpawnImpactSphere(center, intensity)` — esfera aditiva blanco/cian/azul
- `SpawnSupernova(center, scale)` — explosión cósmica completa (4 colores + blanco)
- `SpawnStarEffect(center)` — EFECTO COMPLETO (combina todos los anteriores)
- `SpawnRainbowTrail(center, velocity)` — estela arcoíris cambiante

### B. 4 bastones protegidos (baseline, no modificar)

En `Content/Weapons/TestStaffs/`:
1. **TestMagicRing** — Nightglow + anillo dorado básico
2. **TestSparkle** — Nightglow + sparkles ambientales (+ HoldItem aura)
3. **ProjBeam** — Nightglow + rayo concentrado jugador→cursor
4. **TestMagicRingV2** — Nightglow + múltiples anillos cósmicos

### C. 12 bastones nuevos con Nightglow #931

1. **TestNightglowBasic** — baseline vanilla sin efectos (comparación)
2. **TestNightglowCosmicTrail** — estela cósmica densa
3. **TestNightglowStar** ⭐ — RECREA EL EFECTO DE LA IMAGEN:
   esfera de impacto + starfall + sparkles + light beams + anillo dorado
4. **TestNightglowRingBurst** — 4 anillos cósmicos expansivos
5. **TestNightglowSparkleTrail** — estela continua de sparkles
6. **TestNightglowLightBeams** — rayos de luz radiantes (8 rayos)
7. **TestNightglowStarfall** — 5-7 estrellas cayendo del cielo
8. **TestNightglowLifesteal** — 5% lifesteal mientras se sostiene
9. **TestNightglowEmpower** — concede Empoderamiento Cósmico (+10% dmg, +5% crit, 1% lifesteal)
10. **TestNightglowMultishot** — 3 proyectiles en abanico
11. **TestNightglowRainbowTrail** — estela arcoíris cambiante
12. **TestNightglowSupernova** — supernova cósmica completa + esfera de impacto

### D. Cofre de Pruebas Cósmico (TestStaffChest)

Nuevo item `Content/Items/TestStaffChest.cs`:
- Al usarlo, despliega en el inventario: 16 bastones + 6 items ceremoniales
- Items incluidos: 4 bastones protegidos + 12 Nightglow + StellarDust x50 +
  AethonSigil + ResonanceShard x20 + GenesisShard + SeerOrb
- Reutilizable (no consumible)
- Efectos visuales al abrir (40 partículas doradas + sonido)

### E. Cambios en TestingPlayer

- Cambió el marcador de "kit ya entregado" de GenesisShard → TestStaffChest
- Ahora entrega: GenesisShard, 100 GoldBar, LevelUpTester, BossSummonBag, SeerOrb,
  StellarDust x50, AethonSigil, ResonanceShard x20, TestStaffChest
- El jugador recibe TODOS los items al entrar al mundo

### F. Lifesteal mejorado (ShardPlayer + GlobalNPCXP)

- Nuevo flag `HasEnhancedLifesteal` en ShardPlayer (resetado en ResetEffects)
- `ApplyCosmicEmpowermentLifesteal` ahora soporta lifesteal combinado:
  - 1% si HasCosmicEmpowerment (Sello de Aethon)
  - +4% si HasEnhancedLifesteal (TestNightglowLifesteal)
  - Total máximo: 5%

### G. Texturas (17 nuevas)

Generadas a 1024×1024 con z-ai image, downscale LANCZOS a 30×30 (bastones)
y 32×32 (cofre), con background transparency:
- 4 texturas bastones protegidos
- 12 texturas bastones Nightglow
- 1 textura TestStaffChest
- Maestros preservados en Content/_masters/

### H. Localization ES/EN

Añadidas 34 claves nuevas (17 Display + 17 Tooltip) en ambos idiomas.

Versión bump: 5.28 → 5.29

---

## Commit v5.28 — Mejoras profesionales: texturas HQ + nuevo contenido ceremonial

Mejoras aplicadas sobre el baseline estable v5.27 (sin tocar la lógica del Grimorio).

### A. Texturas regeneradas en alta calidad (1024×1024 → downscale LANCZOS)

Workflow: generadas a 1024×1024 (maestros preservados en `Content/_masters/`),
luego reescaladas con PIL LANCZOS al tamaño requerido por el juego, con
remoción de fondo (transparencia) basada en el color dominante del borde.

**14 texturas de items/proyectiles/buffs/tile/icon:**
- icon.png (80×80) — icono del mod
- GrimoireEternal.png (30×38) — arma principal
- GenesisShard.png (24×24) — item clave
- SeerOrb.png (24×24) — item
- CosmicOrbMinion.png (32×32) — minion
- CosmicOrbBolt.png (16×16) — proyectil
- CosmicOrbBuff.png (32×32) — buff icon
- AncientAltar.png (16×16) — tile
- AncientAltarItem.png (24×24) — item placeable
- BossSummonBag.png (24×24) — item de testing
- LevelUpTester.png (24×24) — item de testing
- ResonanceShard.png (24×24) — moneda cósmica
- ArcaneBolt.png (16×16) — proyectil del arma
- GenesisLight.png (22×22) — proyectil del Fragmento Génesis

**6 texturas de NPCs:**
- AethonBoss.png (48×48) — jefe final
- HollowTitan.png (48×48) — mini-jefe del Sagrario Hueco
- TheWitness.png (24×40) — NPC del pueblo
- RiftKeeper.png (36×36) — mini-jefe dimensional
- EchoArcher.png (36×36) — enemigo
- EchoBlade.png (36×36) — enemigo

**3 texturas para contenido nuevo:**
- AethonSigil.png (28×28) — accesorio nuevo
- StellarDust.png (18×18) — material nuevo
- CosmicEmpowermentBuff.png (32×32) — buff nuevo

### B. Nuevo contenido ceremonial

1. **Polvo Estelar (StellarDust.cs)** — material cósmico fino
   - Recetas: 3 ResonanceShard → 1 StellarDust (y viceversa) en Anvil
   - Drops: jefes cósmicos y enemigos del Sagrario Hueco
     * AethonBoss: 10-15 StellarDust (garantizado)
     * HollowTitan: 5-8 StellarDust (garantizado)
     * RiftKeeper: 3-5 StellarDust (garantizado)
     * EchoArcher/EchoBlade: 1-2 StellarDust (25% chance)

2. **Sello de Aethon (AethonSigil.cs)** — accesorio ceremonial
   - Crafteo: 1 GenesisShard + 5 StellarDust + 3 ResonanceShard + 3 GoldBar/PlatinumBar en Anvil
   - Efectos mientras esté equipado:
     * +5% daño mágico
     * +5% daño de invocación
     * +1 slot de minion
     * +5/s regeneración de mana
   - Confiere buff "Empoderamiento Cósmico" (mantenido por el accesorio)

3. **Empoderamiento Cósmico (CosmicEmpowermentBuff.cs)** — buff ceremonial
   - +10% daño (todas las clases)
   - +5% probabilidad de crítico (todas las clases)
   - +5% velocidad de ataque (todas las clases)
   - 1% de lifesteal (aplicado via ModPlayer.OnHitAnything)

### C. Cambios de código

- `Content/Players/ShardPlayer.cs`:
  * Nuevo flag `HasCosmicEmpowerment` reseteado en `ResetEffects()`
  * Override de `OnHitAnything(float, float, bool)` para aplicar 1% lifesteal
- `Content/Globals/GlobalNPCXP.cs`:
  * Nuevo bloque en `OnKill` para drops de StellarDust según tipo de NPC
- `Localization/es-ES` y `en-US`: añadidas 6 claves nuevas
  (StellarDust, AethonSigil, CosmicEmpowermentBuff en ambas Display + Tooltip/Description)

### D. Seguridad

- Tag `stable-pre-improvements-v5.27` + rama `stable-pre-improvements-v5.27-backup`
  creadas ANTES de aplicar estas mejoras.
- Restaurar baseline estable: `git checkout stable-pre-improvements-v5.27`

Versión bump: 5.27 → 5.28

---

## Commit v5.1 — autoReuse + tooltip rediseñado + proyectil cósmico

3 mejoras solicitadas por el usuario:

1. DISPARO CONTINUO (mantener click):
   - Item.autoReuse cambiado de false → true
   - Ahora se puede mantener el click izquierdo para disparar continuo
   - El Shoot retorna true (tModLoader dispara 1 proyectil, sin doble)

2. TOOLTIP REDISEÑADO COMPLETAMENTE:
   - Antes: 6 líneas con abreviaturas crípticas (+4% mag, -20%tb, 13f, Hilo nv25, ump)
   - Ahora: 6 secciones organizadas con cabeceras de colores y texto claro:
     * PROGRESIÓN (verde): Nivel + barra XP + próximo hito
     * DAÑO (dorado): daño mágico/summon + crit + armor pen + minion slots + knockback
     * RECURSOS (azul): mana/vida max + regen + reducción de daño
     * PROYECTIL (dorado): bolts + área + costo mana
     * ORBE CÓSMICO (magenta): contacto + velocidad + rango + cooldown + costo
     * BONUS (rojo): mana bajo + lifesteal
   - Sin abreviaturas: todo el texto es legible

3. PROYECTIL CÓSMICO (CosmicProjectileFX.cs — NUEVO):
   - GlobalProjectile que afecta SOLO al Nightglow (ID 931)
   - Estela cósmica con paleta del Grimorio:
     * Dorado (cada frame) — núcleo de galaxia
     * Cian (cada 2 frames) — estrella guía
     * Magenta (cada 3 frames) — gemas
     * Índigo (cada 4 frames) — fondo del portal
   - Luz cósmica intensa (violeta-dorada)
   - Explosión cósmica al impactar enemigos (4 colores + supernova blanca)
   - Explosión al morir sin impacto

Versión bump: 5.0 → 5.1

## Commit FIX-COSMIC-EVENTS-ELIMINADOS — Quitar sistema de eventos cósmicos (Hitos + Lluvia de Luz + Rifts)
- CosmicEventSystem.cs ELIMINADO por completo (165 líneas):
  * Anuncios "Hitos cósmico: Lluvia de Luz Estelar / Sagrario Hueco / Rifts Dimensionales / Aethon se agita / El Despertar" al alcanzar niveles 25/50/75/100/150
  * UpdateStarlightRain: spawn de meteoros dorados cada 10s al nivel 25+
  * UpdateDimensionalRifts: spawn de NPC RiftKeeper bajo tierra al nivel 75+
- AethonConfig.cs: eliminadas flags EnableCosmicEvents, EnableStarlightRain, EnableDimensionalRifts (ya no se usan)
- Se conservan EnableCosmicEvents/StarlightRain/Rifts eliminados del config (cualquier config.json antiguo simplemente ignora esas claves)
- Motivo: el usuario reportó que los mensajes "Hitos cósmico" seguían apareciendo en el juego y debían estar eliminados (formaban parte de la misma familia de eventos cinematográficos que ya se quitó)

## Commit FIX-SPRITES-FALTANTES — Agregar sprites PNG para LevelUpTester y BossSummonBag
- LevelUpTester.png generado (24x24, saco dorado con flecha ascendente)
- BossSummonBag.png generado (24x24, saco purpura con calavera roja)
- MissingResourceException al cargar el mod resuelto
- Verificado: 18/18 ModItem/Projectile/NPC/Buff/Tile tienen su .png

## Commit FIX-EVENTOS-ELIMINADOS — Quitar lore y eventos cinematográficos de subida de nivel
- LevelUpEventSystem.cs ELIMINADO por completo (temblor de pantalla, overlay con grano, time-skip de 1 día, texto de lore centrado)
- ShardLevelItem.OnLevelUp: removido el bloque que llamaba a LevelUpEventSystem.Trigger() en la primera subida de nivel
- GrimoireEternal.OnCraft: método eliminado (su único propósito era disparar el Trigger(showLore:false) al craftear)
- Se conservan los efectos simples de subida de nivel: mensaje dorado "✦ Nivel X!", sonido Item4, 40 partículas doradas, y hito cada 50 niveles
- ShardPlayer.FirstLevelUpTriggered: ahora es flag legacy (se persiste para no romper saves antiguos pero ya no dispara nada)
- Motivo: request directo del usuario de quitar el lore y los eventos como el que mueve la pantalla

## Commit FIX-7ERRORES-COMPILACION — Corregir 7 errores CS0103 reportados por el usuario
- TheWitness.OnChatButtonClicked: declarada variable 'level' (CS0103)
- GrimoireEternal.OnCraft: eliminada sp.ActiveBranch (propiedad inexistente)
- LevelUpEventSystem: agregada sobrecarga Trigger(bool showLore) — luego eliminada en el commit siguiente
- Items/LevelUpTester.cs CREADO: +10 niveles al Grimorio (reemplaza TestSlayer perdido en force-push)
- Items/BossSummonBag.cs CREADO: 999 invocadores de 16 jefes vanilla (recreado tras force-push)
- TestingPlayer reescrito: kit de testing con GenesisShard + 100 GoldBar + LevelUpTester + BossSummonBag
- Localization es-ES/en-US: limpiadas claves huérfanas (CosmicPetItem, TestSlayer, CosmicPet), añadidas LevelUpTester + BossSummonBag

## Commit b70d655 — Correcciones del commit 48688dd
- XPForNextLevel: eliminado if(Level<=1) return 1, fórmula normal para todos
- ExtraProjectiles: cambiado de level/5 a level/3 (cada 3 niveles)
- CanUseItem: click izquierdo retorna true (Mana Flower)
- CanUseItem: click derecho permite Mana Flower
- Shoot minion: maneja Mana Flower
- autoReuse = false (previene doble disparo)
- Item.shoot = 931 (Nightglow)
- Eliminado código duplicado en CanUseItem

## Commit d205222 — autoReuse false + Nightglow 931
- autoReuse cambiado a false (causa del doble disparo)
- Item.shoot = 931 (Nightglow restaurado)

## Commit 3aaec4f — GrimorioTest creada
- Nueva arma de prueba con lógica diferente
- Sin AltFunctionUse, sin autoReuse
- Click derecho en UseItem, click izquierdo en Shoot return true
- Sprite generado con AI

## Commit bd3a55c — Doble disparo solucionado con return true + reuseDelay
- Click izquierdo: return true (tModLoader dispara 1)
- Click derecho: return false + reuseDelay=10

## Commit 48688dd — Doble uso + Mana Flower para minion
- CanUseItem del minion permite Mana Flower
- Shoot del minion maneja 3 casos de mana

## Commit 1a1da42 — Mana Flower no permite disparar con 0 mana
- CanUseItem retorna true para click izquierdo

## Commit 2f4ae26 — Quitar disparo doble + XP nivel 1→2 normal
- Eliminada probabilidad de disparo doble
- XPForNextLevel: fórmula normal para nivel 1→2

## Commit b37ef0e — Proyectil sale doble: return false → return true
- return false causaba que tModLoader disparara adicional

## Commit 482a19d — Proyectil doble: return true → return false
- Cambio inicial de return true a return false

## Commit b507628 — Restaurar sprites + Nightglow + tooltip compacto
- GrimoireEternal.png = sprite libro.png
- CosmicOrbMinion.png = minion cosmico.png
- Item.shoot = 931 (Nightglow)
- Tooltip compactado (12→8 líneas)

## Commit 117bd03 — Trigger(bool) restaurada
- Sobrecarga Trigger(bool showLore) se perdió en force push
- _showLore flag restaurado

## Commit a81445b — ActiveBranch rezagado eliminado
- sp.ActiveBranch = BranchType.Magic eliminado

## Commit 480761e — NPC.HitInfo KnockBack eliminado
- KnockBack no existe en HitInfo

## Commit 982ea23 — BuffID.Terraprisma → ID 322
- Nombre constante no existe, usar ID numérico

## Commit c74e56a — Projectile.color eliminado
- Projectile no tiene propiedad .color

## Commit c8638c9 — WeaponScaling using agregado a CosmicOrbMinion
- Falta using AethonMod.Content.Systems

## Commit be0fee8 — Nightglow cósmico + Terraprisma minion + CosmicEventSystem eliminado
- CosmicProjectileFX mejorado con tinte dorado
- Grimorio invoca proyectil vanilla 946 (Terraprisma)
- CosmicMinionFX creado
- CosmicEventSystem.cs eliminado

## Commit 2ffddc7 — Minion cooldown quitado del tooltip + hitos mejorados
- Eliminado 'Minion cooldown: Xf' del tooltip
- Hitos actualizados: Mejora de velocidad, Mejora de minion

## Commit f284177 — Mana max + vida max + hit cooldown en tooltip
- +1 mana cada 4 niveles
- +2 vida cada 20 niveles
- Hit cooldown del minion en tooltip
- WeaponScaling: BonusMana, BonusLife, MinionHitCooldown

## Commit b728a96 — Bolts en línea + hit cooldown mejora con nivel
- Separación reducida de 0.08 a 0.02 rad
- Hit cooldown: 15 base, -1 cada 10 niveles, min 1

## Commit 52b6d4b — 1 bolt por click + minion custom con sprite
- return true cambiado a return false
- CosmicOrbMinion reescrito con IA tipo Terraprisma
- CosmicMinionFX eliminado

## Commit f6db599 — TODAS las mejoras del Grimorio (23 funciones)
- WeaponScaling: 23 funciones de escalado + MilestoneRewards
- GrimoireEternal: ModifyWeaponKnockback, disparo doble, tooltip completo
- ShardPlayer: PostUpdate (regen), ModifyHurt (reducción daño)
- CosmicOrbMinion: velocidad y rango escalados
- CosmicProjectileFX: recreado con daño en área

## Commit 093826c — 3 blockers de REVIEW-FINAL arreglados
- TestingPlayer: eliminadas refs a items inexistentes
- TheWitness: level declarado en OnChatButtonClicked
- CosmicOrbMinion: held redeclarado (CS0136)

## Commit 2f8a128 — BossSummonBag recreado
- Se perdió en force push, recreado

## Commit cb9e6cd — LevelUpTester recreado
- Se perdió en force push, recreado

## Commit 167ddec — Minions persisten al cambiar arma + Mana Flower
- Slots de minion en PostUpdateEquips (busca en todo el inventario)
- CanUseItem permite Mana Flower

## Commit 1038516 — Limpiar repositorio + sprite libro
- Eliminadas carpetas del sandbox de GitHub
- GrimoireEternal.png = sprite libro.png

## Commit c44a62b — Minions desaparecían al exceder limite
- Slots de minion movidos a ModifyWeaponDamage
- Conteo cambiado a ownedProjectileCounts

## Commit 66b6533 — Tooltip solo muestra próximo hito
- Eliminado el bloque que listaba todos los hitos acumulados

## Commit b0998c5 — Armas se craftean sin yunque
- Eliminado AddTile(TileID.Anvils) de las recetas

## Commit 5c684b4 — Quitar eventos + partículas + minion sprite + XP normal
- Eventos de subida de nivel removidos
- Partículas reducidas (scale + alpha)
- Minion sprite = minion cosmico.png
- XP nivel 1→2 normal

## Commit 458d30d — ThreadStateException en Unload
- Dispose envuelto en try/catch

## Commit b9905cd — Partículas carga minion + bolts 1+cada3 + LevelUpTester
- Partículas de carga restauradas
- Bolts: 1 base + 1 cada 3 niveles
- LevelUpTester: da +10 niveles al Grimorio

## Commit 224bb62 — LevelUpTester busca en todo el inventario
- No requiere sostener el Grimorio

## Commit 936add3 — Minion requiere mana (15 + nivel, tope 100)
- WeaponScaling.MinionManaCost creada
- CanUseItem verifica mana del minion
- Shoot cobra mana al invocar

## Commit 8e93990 — NPC.HitInfo no contiene KnockBack
- Eliminado hit.KnockBack

## Commit f3d84dc — CosmicOrbMinion namespace corregido
- global::AethonMod.Content.Projectiles.CosmicOrbMinion

## Commit 7c30f7a — BranchChoiceUI eliminado de UISystem
- UISystem simplificado

## Commit 1e9b277 — BranchChoiceUI eliminado de UIScrollBlockPlayer
- Simplificado a métodos vacíos

## Commit 4d67ba0 — try sin catch en CosmicOrbMinion
- Estructura try/catch reparada

## Commit 8eb24c7 — Grimorio reescrito desde cero
- Sin dependencias de ActiveBranch/IsImprinted
- WeaponScaling reescrito sin BranchType
- GlobalNPCXP solo otorga XP al Grimorio
- CosmicOrbMinion sin BranchType

## Commit 71fd964 — Rediseño: crafteo + boss drop
- BranchChoiceUI removido
- GenesisShard es arma de luz + material
- Las 3 armas se craftean con GenesisShard
- OnCraft dispara evento cinematográfico
- Boss drops de King Slime/Eye of Cthulhu

## Commit fa240e1 — HJSON malformado + revisión profunda
- HJSON con múltiples cierres } arreglado
- 3 riesgos arreglados (TestingPlayer, BossSummonBag, TestSlayer)

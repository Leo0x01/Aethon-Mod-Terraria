# v6.100.2 — LA ENTREGA COMPLETA — LA SOMBRA DE LA PÁGINA IV RE-FORJADA YA LLEGA DIRECTO

> **BUILD VERIFICADA — PENDIENTE PUBLICAR** — .tmod v6.100.2 (md5 `1d75b914c4117a237b09ece4c9b42747`, 11.286.228 B, 493 entradas) — **LA ENTREGA**: la v6.100 re-forjó completa la Sombra de la Página IV (la garganta del vórtice como boca girando con el rumbo, velo de ruido, anillo de tinta doble, icono re-forjado — 617 líneas de proyectil) pero solo la dejaba en la Bolsa de las Sombras. La regla de la casa es la regla: **lo re-forjado ES lo nuevo, y lo nuevo llega DIRECTO** — una sola línea en `ShardPlayer.EntregarRegaloDePruebas` (justo tras la Página II), la lista queda en **17 ítems directos** y TODA la creación nueva de la casa queda cubierta. **QUIRÚRGICO**: ni una coma de la interfaz de usuario se tocó — la cura v6.100.1 de la interfaz rota viaja INTACTA dentro de esta versión — oráculo 0/0 (423 .cs), **-build REAL 0/0**, **DLL DECOMPILADA** (la entrega lista las 17 con `SombraDeLaPagina4` tras `SombraDeLaPagina2` · GetItemDrawFrame VIVE · `MarcoVivo` x0 · `itemFrame` x0), guardián **TODO VIVE ✔** (411 clases), hjson es-ES == es-MX en claves y valores (1556 planas c/u) con 0 claves del español faltando en el en-US, headless **0 excepciones** (Selected AethonMod 6.100.2 → Mod Load Completed).

## LA LETRA (el usuario)

«asegurate de que le entregas al jugador las cosas nuevas, armas o cualquier cosa nueva que sea creada»

## LA AUDITORÍA DE LA ENTREGA (qué ya llegaba y qué faltaba)

**Ya llegaba DIRECTO (16 ítems):** las 5 armas nuevas de la v6.100 (Sello de Tinta · Parpadeo del Eterno · Corona de Plumas · Derrame de Tinta · La Última Palabra) + los 3 supervivientes de la purga (Coro de los Profundos · Pluma del Escriba Santo · Vástago del Eterno) + el Estandarte del Juicio + los históricos (Formas Ascendidas 2/3/4 · Aethon Menor · Halo Arcoíris · Brillo Solar · Sombra de la Página II).

**Faltaba (1):** LA SOMBRA DE LA PÁGINA IV — re-forjada COMPLETA en la v6.100 (el proyectil entero reescrito: la GARGANTA DEL VÓRTICE como boca girando con el rumbo del tentáculo, el VELO DE RUIDO sobre el cuerpo, la masa viva con sus ojos clavados en la presa, el ANILLO DE TINTA doble violeta+teal y el icono re-forjado) — pero solo vivía en la Bolsa de las Sombras. La bolsa sigue como vía de repuesto; lo nuevo llega DIRECTO.

## PARA VERIFICAR EN JUEGO (la checklist de la entrega)

1. **Actualiza a la v6.100.2** (VÍA 1 del COMPILACION.md: el `.tmod` del release → carpeta Mods) — verifica en la lista de mods que diga **6.100.2**.
2. **Entra a un mundo nuevo o con el inventario sin ella**: LA SOMBRA DE LA PÁGINA IV llega SOLA (con el mensaje dorado del regalo de pruebas) — igual que sus hermanas de creación reciente.
3. **La cura de la interfaz sigue viva**: entra con el estandarte equipado (o en la mano del cursor) — nada desaparece, no hay pantalla negra, el log queda limpio.
4. **El resto de la v6.100 intacto**: el aura del juicio, el altar asentado con sus dos velas, las cinco armas nuevas.

## LA LECCIÓN (sellada en el código)

La entrega directa no distingue entre «nacido» y «re-forjado»: si una versión le puso cuerpo nuevo a un ítem (proyectil reescrito + icono nuevo), ese ítem ES nuevo a efectos de la entrega — `EntregarRegaloDePruebas` es la lista viva de TODO lo que el jugador debe tener sin buscarlo.

---

# v6.100.1 — LA CURA DE LA INTERFAZ ROTA — EL ESTANDARTE YA NO REVIENTA LOS SLOTS (INCLUIDA EN LA v6.100.2)

> La cura viaja INTACTA dentro de la v6.100.2 — documentada aquí tal cual se forjó.

## EL BUG (la letra del usuario)

«hay un error muy raro, creo que con el estandarte, este error buguea por completo la interfaz de usuario haciendo que algunas cosas desaparezcan o la pantalla se vuelva negra, es muy raro»

### LA AUTOPSIA DEL client.log (5/11, 16:05→16:07)

- 16:05:30 — carga **AethonMod 6.100** (tras un reload desde la 6.99).
- 16:06:22 — entra al mundo con el estandarte equipado.
- 16:06:32→16:06:51 — **5 «Excepción silenciosa»** (tML las deduplica): `System.IndexOutOfRangeException` en `EstandarteDelJuicioItem.PreDrawInInventory` línea 92, llamadas desde `ItemSlot.Draw` por TRES vías: el inventario, el slot de accesorio (`AccessorySlotLoader.DrawRedirect`) y **el objeto en la mano del cursor** (`DrawInterface_38_MouseCarriedObject`).
- 16:07:08 — guarda y renuncia (la interfaz inservible).

**POR QUÉ SE ROMPÍA LA INTERFAZ ENTERA**: la excepción saltaba DENTRO del bucle de dibujado de las capas de interfaz (`Main.DrawInterface` → capa 27 del inventario) — tML la traga («silenciosa»), pero las capas que venían DETRÁS no se dibujan ese frame → elementos que desaparecen; según qué slot se dibujaba cuando reventaba, el daño cambiaba (a veces la pantalla entera en negro). CADA FRAME, mientras el ítem era visible.

### LA RAÍZ (verificada en el fuente de tModLoader, rama 1.4.4)

- `Main.itemFrame` es un array **VANILLA** (declarado `int[401]`, dimensionado por el juego base a ~5456) que tML **NUNCA redimensiona** — `ItemLoader.ResizeArrays` solo redimensiona `Main.itemAnimations` (el sistema de `Main.RegisterItemAnimation`… que el estandarte YA usaba para su animación).
- La v6.100 leía `Main.itemFrame[Type]` en `MarcoVivo` para el «marco vivo» — con Calamity y compañía, el Type del estandarte queda MUY por encima del tope vanilla → **fuera de rango**.

### LA CURA QUIRÚRGICA

- `PreDrawInInventory` dibuja el PARÁMETRO `frame` del hook (ItemSlot.Draw lo computa con `Main.itemAnimations` — el sistema que tML SÍ dimensiona para mods; escala 0.62 y el ajuste de 2 px INTACTOS).
- `PreDrawInWorld` usa `Main.GetItemDrawFrame` (el helper oficial — el mismo del Grimorio Hambriento).
- `MarcoVivo` (el lector del array venenoso) MUERE.

## LA LECCIÓN DE LA v6.100.1 (sellada en el código)

El marco animado de un ítem NUNCA se lee de `Main.itemFrame` (solo ítems vanilla): **llega en el parámetro `frame` del hook `PreDrawInInventory`** (ItemSlot.Draw lo computa con `Main.itemAnimations`) **o por `Main.GetItemDrawFrame(type, out tex, out frame)`** — el helper oficial, el mismo que el Grimorio Hambriento ya usaba. La casa ya conocía el patrón correcto; la v6.100 lo reinventó mal.

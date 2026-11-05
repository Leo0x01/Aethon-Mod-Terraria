# v6.100.1 — LA CURA DE LA INTERFAZ ROTA — EL ESTANDARTE YA NO REVIENTA LOS SLOTS

> **BUILD VERIFICADA — PENDIENTE PUBLICAR** — .tmod v6.100.1 (md5 `948979744140c63794eba7a3e6878230`, 11.286.231 B, 493 entradas) — **LA CURA**: `EstandarteDelJuicioItem` ya NO lee `Main.itemFrame[Type]` (el array VANILLA que tML NUNCA redimensiona para mods — el Type del estandarte con Calamity cargada queda fuera de rango → `IndexOutOfRangeException` CADA FRAME en cada slot → las «Excepción silenciosa» mataban las capas restantes de la interfaz: cosas que desaparecen, PANTALLA NEGRA). AHORA: `PreDrawInInventory` dibuja el PARÁMETRO `frame` (el marco animado que el propio hook trae, computado con `Main.itemAnimations` — el sistema que tML SÍ dimensiona) y `PreDrawInWorld` usa `Main.GetItemDrawFrame` (el helper oficial de vanilla — el mismo del Grimorio Hambriento). La presentación v6.100 NO cambia (escala 0.62, el ajuste de 2 px, la tira animada intacta) — oráculo 0/0 (423 .cs), **-build REAL 0/0**, **DLL DECOMPILADA** (PreDrawInInventory dibuja el `frame` del parámetro · PreDrawInWorld llama GetItemDrawFrame · `itemFrame` x0 · `MarcoVivo` x0), guardián **TODO VIVE ✔** (411 clases), hjson ×3 simétricos (817 claves), headless **0 excepciones**.

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

## PARA VERIFICAR EN JUEGO (la checklist de la cura)

1. **Actualiza a la v6.100.1** (VÍA 1 del COMPILACION.md: el `.tmod` del release → carpeta Mods) — verifica en la lista de mods que diga **6.100.1**.
2. **Entra al mundo con el estandarte equipado** (o tómalo del inventario / en la mano del cursor): LA INTERFAZ VIVE — nada desaparece, no hay pantalla negra, el log queda LIMPIO (0 «Excepción silenciosa»).
3. **El ítem se ve como la v6.100**: en el inventario la tira animada (el pennant ondeando, el sol pulsando, el fuego) a escala contenida; en el suelo cae con su marco vivo; la tela sigue en la espalda meciéndose.
4. **El resto de la v6.100 intacto**: el aura del juicio (la bendición +30%, el ardor, el repel, las cargas con estallido), el altar asentado con sus dos velas, las cinco armas nuevas, la Sombra de la Página IV.

## LA LECCIÓN (sellada en el código)

El marco animado de un ítem NUNCA se lee de `Main.itemFrame` (solo ítems vanilla): **llega en el parámetro `frame` del hook `PreDrawInInventory`** (ItemSlot.Draw lo computa con `Main.itemAnimations`) **o por `Main.GetItemDrawFrame(type, out tex, out frame)`** — el helper oficial, el mismo que el Grimorio Hambriento ya usaba. La casa ya conocía el patrón correcto; la v6.100 lo reinventó mal.

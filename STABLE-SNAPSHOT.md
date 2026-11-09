# SNAPSHOT ESTABLE — v6.119 — LA CURA DEL CRASH DEL INTERIOR + LAS DOS PUERTAS (el ítem que no se gasta y el altar de las mil esencias)

> .tmod v6.119 verificada: 494 entradas · 11.568.886 B · EOF exacto · 494/494 blobs inflan · oráculo 0/0 · -build REAL 0/0 · headless 0 excepciones.

## LA LETRA (el usuario)

«hay errores, tambien intenta que el item que lleva al mundo interior no sea consumible y se pueda seguir usando siempre, ademas recuerda hacer al consagrar 1000 esencias el altar se pueda ir al mundo interior» — con el client.log de la sesión del 9/11: Main engine crash (`Begin has been called before calling End`) en `DrawSuperSpecialProjectiles` justo al entrar al mundo interior («El libro se abre…» → Width 800 × Height 450 → crash).

## LA AUTOPSIA DEL CRASH (leída en el IL del tML 2026.09.3.1 real)

El pase de `Main.DoDraw` alrededor de nuestro hook es EXACTO:

```
spriteBatch.End();                          ← vanilla CIERRA el lote de tiles
SystemLoader.PostDrawTiles();              ← EL HOOK DEL MOD corre AQUÍ
TimeLogger.DetailedDrawTime(35);
SortDrawCacheWorms();
DrawSuperSpecialProjectiles(DrawCacheFirstFractals)
    → spriteBatch.Begin(...)                ← vanilla ABRE (el que CRASHEÓ)
```

`SubmundoGrimorio.PostDrawTiles` (las brumas del interior, v6.118) terminaba con `finally { VFXCore.ReabrirLoteVanilla(); }` — y `ReabrirLoteVanilla` RE-ABRE un lote cuando no hay uno vivo. El frame salía del hook con un Begin HUÉRFANO → el Begin de `DrawSuperSpecialProjectiles` lanzaba `InvalidOperationException` → Main engine crash. Solo crasheaba DENTRO del interior porque el hook tiene el guard `!Activo` (en el mundo real jamás corría). **LA CURA**: `finally { VFXCore.CerrarLoteSiAbierto(); }` — salir SIEMPRE CERRADO (quien sigue — `DrawSuperSpecialProjectiles` — abre su propio lote). Es el patrón que `RayoLib.PostDrawTiles` y `ParticleManager.PostDrawTiles` ya usaban; `ReabrirLoteVanilla` queda SOLO para los PreDraw (el lote ABIERTO del pase de entidades). VERIFICADO en el IL de la DLL compilada: ReabrirLoteVanilla AUSENTE del PostDrawTiles, CerrarLoteSiAbierto VIVO en el finally.

## LAS DOS PUERTAS

1. **LA PUERTA PORTÁTIL (el ítem) NO SE GASTA**: `MundoInteriorEternoItem` nació `consumable = false` y ahora queda blindado: NINGÚN camino consume el ítem; el guard nuevo de `CanUseItem` (UN festín a la vez — si una DevoracionEterna ya vive para este portador, el uso espera a que el trago termine) evita la doble devoración; tras cada viaje el ítem está DONDE ESTABA: se puede usar SIEMPRE. El tooltip ganó la línea del contrato (Linea4 ×3 idiomas: «LA PUERTA NO SE GASTA…»).
2. **LA PUERTA DEL ALTAR (las mil esencias consagradas)**: `SubmundoGrimorio.EsenciasConsagradas` (persistida con el mundo: SaveWorldData/LoadWorldData — `esenciasConsagradasAltar` en el TagCompound; el cerrojo del interior garantiza que jamás se contamina desde adentro; NO se resetea al entrar/salir del interior) acumula TODAS las esencias que el Eterno cobró por sellar ofrendas. A las MIL (`EsenciasPuerta = 1000`): el ANUNCIO al mundo + LA SECCIÓN NUEVA de la ventana del altar — «EL MUNDO INTERIOR DEL ETERNO» con la cuenta viva (X/1000 + la pista) mientras falta y, al abrirse, EL BOTÓN «IR AL MUNDO INTERIOR» (mismo gesto que el ítem: cierra la ventana, nace DevoracionEterna, el libro devora al portador SIN daño y el interior lo recibe con la revelación de niebla). El rect del botón se apaga cada frame (`_geoPuerta = Rectangle.Empty`) — jamás un clic fantasma; la geometría fluye por la MISMA función de medida y dibujo de la casa (el panel no salta).

## EL WARN DEL EMPAQUETADO (investigado a fondo — es del tML, no del mod)

«[FNA]: Image loading failed: unknown image type» durante «Empaquetando»: el empaquetador del tML compara `icon_small.png` con su plantilla leyendo el archivo HASTA EL FINAL y luego intenta la conversión rawimg desde el EOF → StbImage responde «unknown image type» → WARN… y después resetea la posición y empaqueta el archivo CORRECTO (verificado: el icon_small.png del .tmod es byte-idéntico, 2279 B). Probado contra el FNA3D_Image_Load REAL del tML 2026.09.3.1: los 474 PNG del mod cargan BIEN (secuencial y 40 rondas en paralelo: cero fallos). INOFENSIVO.

## VERIFICACIÓN (la cadena de la casa)

- oráculo verify.csproj: **0 errores / 0 warnings** (contra el tModLoader.dll + FNA + ReLogic + TerrariaHooks + log4net + CsvHelper + MP3Sharp + NVorbis del tML 2026.09.3.1 real).
- **-build REAL 0/0**: 494 entradas, 11.568.886 B, EOF exacto, sum(comp)+tabla == fsize, 494/494 blobs inflan.
- **Las firmas de la cura y la puerta VIVEN** en la DLL: EsenciasConsagradas · PuertaAbierta · EsenciasPuerta · esenciasConsagradasAltar · PuertaAbiertaAnuncio · AltarMundoInterior · MundoTitulo/MundoPuerta/MundoProgreso (UTF-16 #US) · PostDrawTiles.
- **PostDrawTiles curado VERIFICADO EN EL IL** de la DLL empaquetada (ReabrirLoteVanilla AUSENTE, CerrarLoteSiAbierto en el finally).
- **hjson ×3**: es-MX regenerado por `tools/sync_es_mx_v6564.py` (espejo puro), en-US traducido — empaquetado == fuente IDÉNTICO, claves nuevas COMPLETAS en los 3.
- **Servidor headless 0 EXCEPCIONES** (Sandboxing → Constructing → Adding Content → Configuring → Finalizing → Adding Recipes → Choose World; generación de mundo corriendo con el mod activo sin errores).

## PARA VERIFICAR EN EL JUEGO

- [ ] **EL CRASH MURIÓ**: usa MUNDO INTERIOR DEL ETERNO (o el botón del altar) — «El libro se abre…» → ENTRAS AL INTERIOR SIN CRASH y las brumas del mundo (columnas, volutas, la reactiva) viven sobre el terreno.
- [ ] **EL ÍTEM NO SE GASTA**: úsalo, viaja, vuelve por el prisma y úsalo OTRA VEZ (y otra): el ítem siempre está en la bolsa; spamearlo durante la devoración ya no apila libros.
- [ ] **LA CUENTA DEL ALTAR**: consagra armas/materiales — la sección «EL MUNDO INTERIOR DEL ETERNO» de la ventana del altar muestra la cuenta viva subiendo (X/1000).
- [ ] **LA PUERTA DEL ALTAR**: al llegar a las 1000 consagradas, el ANUNCIO al chat + EL BOTÓN «IR AL MUNDO INTERIOR» aparece en la ventana del altar — y traga igual que el ítem.
- [ ] **LA PERSISTENCIA**: la cuenta del altar SOBREVIVE guardar/cargar el mundo (y entrar/salir del interior no la toca).
- [ ] **TODO LO DEMÁS DE LA v6.118 INTACTO** (si funciona, no se toca): la superficie del cuerpo, el ciclo eterno de oleadas, el cielo de tentáculos, la revelación, las alas y la salida limpia.

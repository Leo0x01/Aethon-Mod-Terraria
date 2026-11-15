# SNAPSHOT ESTABLE — v6.138 — LA CURA DEL MAPA ASESINADO

> **PUBLICADA — [release 445918299](https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.138) + CDN verificado byte a byte (md5 `c21002edea9cfe45e4235282a37e86a4`, 19.357.744 B, 523 entradas)**

## LA LETRA (sesión 19)

«revisa, un error cerro el juego, debe ser lo ultimo añadido al log» + el client.log: **Main engine crash** en `DrawToMap_Section → IndexOutOfRangeException` al recargar el mundo real (13413) después de visitar el mundo interior con la v6.136/137.

## EL CRIMEN COMPLETO (contra el decompile REAL)

1. **02:02:16** (dentro del interior): el AUTOSAVE del jugador (10 min: `DoUpdate_AutoSave → saveToonWhilePlaying → Player.SavePlayer → InternalSaveMap → Main.Map.Save()`) corrió DENTRO del interior — **NO pasa por `WorldFile.SaveWorld`**, así que el cerrojo v6.114 no lo veía.
2. `MapHelper.InternalSaveMap` escribe `<carpetaJugador>/<Main.worldID>.map` — y `clearWorld` NO cambia `worldID` → **el mapa 800×600 del interior SOBREESCRIBIÓ el .map del mundo real** (mismo nombre de archivo).
3. **04:03:25**: al cargar el mundo real, `WorldMap.Load()` deserializa → `SetTile` fuera de rango (excepción SILENCIOSA del hilo) → el mapa queda roto → el primer frame `DrawToMap_Section` → **FATAL**. (Detalle letal: `Main.Map` solo nace en el CONSTRUCTOR de Main — el mapa chico sobrevive al menú y a TODO.)

## LA CURA TRIPLE (v6.138)

1. **EL CERROJO DEL MAPA**: hook `Terraria.Map.On_MapHelper.SaveMap` — mientras el interior vive, NINGÚN guardado de mapa corre (el mismo patrón del cerrojo de SaveWorld).
2. **EL MAPA QUE RENACE**: `OnWorldLoad` (dentro de `LoadWorld_Version2`, ANTES de deserializar) compara `Main.Map.MaxWidth/MaxHeight` contra el mundo — si no calzan, el mapa RENACE con las dims correctas antes del primer frame.
3. **LA SONDA MEJORADA**: `BlindarMapa` compara dimensiones directamente (la sonda por excepción queda de red).

## LA RECUPERACIÓN DEL MUNDO DEL USUARIO

Con la v6.138, el mundo 13413 **vuelve a cargar solo** — el mapa dañado renace limpio. Solo se pierde la exploración del minimapa (se rehace caminando). La partida está intacta. (Alternativa manual: borrar el `.map` de la carpeta del jugador.)

## PARA VERIFICAR

- Oráculo **0/0** · build REAL **0/0** (523 entradas, md5 `c21002ed…`) · **GUARDIÁN: TODO VIVE ✔** · **AUDIT de la cura ✔** · **headless 0 excepciones (v6.138)**

## PENDIENTE PARA LA PRÓXIMA SESIÓN

1. **Verificar en juego**: entrar al interior, esperar >10 min DENTRO (el autosave no debe escribir el .map — el log no debe mostrar "Guardando datos de mapa" dentro), salir por el pilar Y TAMBIÉN por guardar-y-salir, recargar el mundo → sin crash.
2. El mundo 13413 del usuario debe cargar limpio con la v6.138 (el mapa renace).
3. Doc-rot conocida: CARACTERISTICAS.md y DISEÑO_DEL_MOD.md desactualizados en conteos — MAESTRO.md lleva los datos vigentes.

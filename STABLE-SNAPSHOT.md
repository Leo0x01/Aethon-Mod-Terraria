# SNAPSHOT ESTABLE — v6.137 — EL PILAR DEL CUERPO DEL DIOS: LA ARENA INTERIOR

> **PUBLICADA — [release 445870013](https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.137) + CDN verificado byte a byte (md5 `96d9c3029ea013bac2639545257dbfad`, 19.366.311 B, 523 entradas)**

## LA LETRA (sesión 18 — completa)

Valquiria ×2 (sin plumas · lisa) · cuchillas=colmillos · muñeco (receta 5+5 · el círculo→bruma · 999) · textos del altar · grimorio encima del altar · un solo altar activo (extras desactivados + mapa) · interior limpio (solo el pilar) · textos del pilar + LA OLEADA a elección + LA ARENA de jefes devorados (+25%/muerte · IA 1-5 normal/6 experto/8 agresiva/10 combos · jefes del mod solo poder+experto).

## LO QUE VIVE

- **ALAS**: estilos 12 (Valquiria Sin Plumas — 2 estructurales) y 13 (Valquiria Lisa — la vela continua); items + iconos 30×30 de la casa; Bolsa de las Alas a 13.
- **COLMILLOS**: las cinco dagas visten ColmilloAbismo.png (48×96 — la encía violeta): tinte del alma por tipo, el ojo en la encía, punta al rumbo. Máquina intacta.
- **MUÑECO**: receta 5 madera + 5 Esencia del Eterno (lote 5) · el SoftGlow plano MURIÓ → BrumaFX.Puff (la biblioteca) · 999 en el regalo de pruebas.
- **ALTAR ÚNICO**: AltarUnicoSistema (persistente) · rebote al colocar duplicados · apagado sin luz + cursor que lo dice · promoción al picar el activo · LA LLEGADA: el libro nace ~40 px SOBRE la cima · el interior sin altares ni grimorios.
- **EL PILAR**: la ventana medida (cero desbordes) con VOLVER · INVOCAR A AETHON · LA OLEADA (selector 1..FuriaNivel, natural:false — desbloquear es completar FUERA) · LA ARENA (jefes devorados con nivel de poder, scroll con la rueda).
- **LA ARENA**: JefeInteriorGlobalNPC — aura OleadaGrimorio(3) · +25% vida/daño/def por kill interior (ShardPlayer.MuertesInterior*, persistente) · IA: 1-5 vanilla · 6-7 préstamo del modo MAESTRO por reflexión (_overrideForExpertMode — la máquina v6.50.73) · 8-9 CoreografiaPrestada (los ataques temáticos del festín) · 10 cadencia 36 t · jefes del mod: solo stats+experto (EsVanilla gatea) · MundoLibre ve el sello (el festín espera).

## LAS DECISIONES DE LA CASA (el cuestionario — respondidas y ejecutadas)

1. **Los altares extra**: rebotan al COLOCAR el ítem (mensaje claro) — más honesto que muebles muertos: nunca hay altares zombie. Los que queden atrás (mundo viejo) se ven apagados (sin luz) y el cursor lo dice.
2. **La marca del mapa**: esta era del tML NO tiene ModifyMapEntryName (verificado contra el DLL: 0 apariciones; solo UNA MapEntry por tipo). La casa marca al ACTIVO con el cursor + su luz viva; el nombre del mapa no se puede cambiar por-tile.
3. **El nivel de IA del jefe interior** = 1 + kills DENTRO del interior (los kills de afuera no entrenan la arena — la letra: «seran potenciado por cada muerte que el jugador haga dentro del mundo interior»).
4. **La oleada del pilar** NO sube el FuriaNivel (natural:false — la ley v6.50.74: probar no es progresar; desbloquear = completar FUERA).
5. **El préstamo experto**: la IA de nivel 6+ corre vestida de MODO MAESTRO (el préstamo por reflexión del festín — maestro incluye los patrones expertos de vanilla y sus stats aparte).

## PARA VERIFICAR

- Oráculo **0/0** · build REAL **0/0** (523 entradas, 19.366.311 B, md5 `96d9c302…`)
- **GUARDIÁN: TODO VIVE ✔** (los 2 iconos nuevos incluidos)
- **AUDIT v6.137: TODO VERIFICADO ✔** — 74 checks (alas ×2 · colmillos · muñeco · altar único · pilar · arena · paridad 1679³ · las 15 claves nuevas ×3 · no-regresiones del arcángel y la máquina de cuchillas)
- **headless**: Sandboxing v6.137, **0 excepciones**

## PENDIENTE PARA LA PRÓXIMA SESIÓN

1. **Verificación en juego** por el usuario: las dos valquirias (estética), los colmillos en movimiento, el libro sobre el altar en una llegada nueva (mundos ya generados conservan su altar — la ley aplica desde YA para colocar/promover), la ventana del pilar y la arena (los niveles 6+/8+/10 solo se ven tras matar al mismo jefe 5/7/9 veces dentro).
2. Los mundos existentes con YA un altar: la llegada no vuelve a correr (CaidaRecibida persiste) — la ley del único aplica a colocar/promover, y el altar existente queda como activo tras la primera promoción.
3. Doc-rot conocida: CARACTERISTICAS.md y DISEÑO_DEL_MOD.md desactualizados en conteos — MAESTRO.md lleva los datos vigentes.

# SNAPSHOT ESTABLE — v6.139 — LA SESIÓN DE LAS NUEVE CURAS

> **PUBLICADA — [release 445948793](https://github.com/Leo0x01/Aethon-Mod-Terraria/releases/tag/v6.139) + CDN verificado (md5 `ec353a3e45a8442a2c416eab46220fd3`, 19.358.919 B, 524 entradas)**

## LA LETRA (sesión 20)

«mejora los textos del altar… los textos del prisma del eterno se desbordan… el grimorio parece que no esta devorando la primera muerte de los jefes… el altar principal no se muestra en el mapa, los altares inactivos sean devorados… las alas valquiria lisa solo muestra las runas… alas runicas… analiza que todos los sistemas que calculan daños lo haga correctamente… mejora el texto de info de La Forma Eterna… dame una idea sobre que se podria hacer con todo el texto del altar» + el client.log (un error en la última entrada).

## LAS SIETE ENTREGAS

1. **EL ERROR DEL LOG CURADO** — DummyNegroNPC pintaba `BrumaFX.Puff` con el lote CERRADO (`Draw without Begin`): el PreDraw cerraba el lote del pase de NPCs y llamaba Puff a ciegas. Cura: `BeginMass` explícito + `RestaurarLoteVanilla` de salida (el contrato v6.56.1).
2. **EL FESTÍN CURADO** — la regla v6.97 («solo jefes del checklist») era OBLIGATORIA para todas las cenas, pero la escalera solo tiene puertas de jefe en niveles 3,5,8,10,13,20… → con despertar intermedio NINGUNA primera cena arrancaba. Cura: 1ª muerte cena SIEMPRE; repeticiones por checklist.
3. **EL ALTAR EN EL MAPA + DEVORADO** — nameFunc por coordenada («(ACTIVO)»/«(devorado por el grimorio)» — la API real del decompile) + los altares inactivos DEVORADOS en RandomUpdate (cuerpo 2×3, sin botín).
4. **TEXTOS RECORTADOS** — altar, prisma y Forma Eterna en 3 idiomas: la misma info, la mitad de texto.
5. **LAS ALAS DE ESCRITURA** — Valquiria Lisa = `SoloRunas` (el cuerpo muere; las runas SON las alas) + **Alas Rúnicas nuevas** (estilo 14): el enjambre — borde maestro tangente con pulso viajero, vela de runas menores, runa MAYOR (1.9×) por punta. En la bolsa, 3 idiomas.
6. **AUDITORÍA DE DAÑO** — convención vanilla verificada en el decompile (`lifeRegenCount`; golpe de num con −120·num): aura 1→10+bono por cercanía con 2→6 ticks/s EXACTO · quemadura 1% vida máx/medio s EXACTO · +10 def/+10% daño/−15% maná ✔ · Forma Eterna 7 stats ✔ · grimorio multiplicativo con topes ✔ · esencias +0,1%/u ≤+100% ligadas al libro ✔. TODO SANO.
7. **EL PERGAMINO DEL ALTAR** — mockup fullHD (`pergamino_altar_mockup_fullhd.png` en download/): TODA la info del altar en un pergamino del grimorio. PROPUESTA PENDIENTE DE LA LETRA: si le gusta al usuario, la próxima sesión lo implementa como ventana real del juego (ítem consumible «El Pergamino del Altar» o botón «?» en el panel).

## PARA VERIFICAR EN JUEGO

- El muñeco del banco de pruebas con su bruma violeta viva (y el log SIN la excepción silenciosa).
- Matar un jefe NUEVO (nunca devorado) con cualquier despertar → LA CENA ARRANCA (se detiene a 100 HP).
- El minimapa: el altar dice «(ACTIVO)»; colocar un segundo altar → el grimorio lo devora con el anuncio.
- Los paneles del altar y el prisma: textos cortos, nada desborda.
- Valquiria Lisa: solo runas · Alas Rúnicas (bolsa de alas): el enjambre completo.
- Doc-rot conocida: CARACTERISTICAS.md/DISEÑO_DEL_MOD.md desactualizados — MAESTRO.md manda.

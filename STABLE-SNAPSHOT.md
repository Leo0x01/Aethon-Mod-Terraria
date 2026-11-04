# v6.94 — LA VERIFICACIÓN DE LAS SESIONES PERDIDAS + EL BARRIDO FINAL — EL CHECKLIST

> **PUBLICADA** — .tmod v6.94 FINAL (md5 `fe30342e7b9c73bab34f70f5972f13ea`, 11.235.782 B) — 490 entradas, blobs íntegros, EOF EXACTO, las DOS músicas del festín dentro, oráculo 0 errores/0 warnings (414 .cs · 79 refs del tML 2026.07.3.0 real), **audit_v694 TODO VERIFICADO** (las 7 texturas de la casa VIVAS con dimensiones exactas + las 6 copiadas de la investigación AUSENTES · 14 firmas v6.93/94 vivas en la DLL (UTF-8 y UTF-16) · 6 muertas ausentes (MiradaSolyn, SolynTagTeamBeam, Relicario, Grilletes, WHitoEscuadron, EntitySource_ItemDrop) · **0 caracteres chinos y 0 menciones IP en los 414 .cs + hjson** · hjson ×3 simétricos · LICENSE propietaria dentro del .tmod), headless **Sandboxing v6.94 → Finalizing → Adding Recipes → Server started, 0 excepciones**.
>
> La entrega: **LA VERIFICACIÓN COMPLETA tras las sesiones perdidas** (la letra: «como hubieron muchas sesiones perdidas intenta realizar una verificación y que todo este completado y sin errores… está en chino habla español y verifica que todo este correcto») — el repositorio local quedó ATRÁS del remoto (v6.92 con cambios sin commit) y fue sincronizado a la v6.93 publicada (el asset del release verificado BYTE A BYTE contra el respaldo local), **EL ÚLTIMO TEXTO EN CHINO del repositorio murió** (el kanji del conejo de los comentarios de la plaga — con él, TODAS las menciones a la IP del anime que la inspiró: el archivo de la mascota y el CHANGES.md quedaron en palabras de la casa, la mecánica INTACTA porque es implementación propia), y **TODO el pipeline re-verificado desde cero**: oráculo 0/0 sobre el árbol limpio, -build REAL 0 errores/0 warnings, audit_v694 con el nuevo guardián anti-chino/anti-IP en los fuentes, y headless 0 excepciones.

## LO QUE SE VERIFICÓ (todo vivo, todo de la casa)
1. **EL ESTADO DEL REPO**: el remoto (v6.93 fe97c44) es la verdad — el asset del release = el respaldo local BYTE A BYTE (md5 `397e9467786e907d31012f5a8263082d`); los cambios huérfanos del working copy eran un SUBCONJUNTO viejo de lo ya publicado (comparados contra el remoto y descartados).
2. **EL CHINO + LA IP**: el único texto CJK del repositorio (el kanji del conejo, en el comentario de investigación de la plaga) ELIMINADO — junto a las ~22 menciones al anime (el nombre de la bruja, el del protagonista, el del mansionero, el «Santuario») reescritas como «LA IDEA ORIGINAL DE LA CASA» en `AethonMenorPareja.cs` y en los 3 pasajes históricos del `CHANGES.md`. La plaga/coro de hadas NO se tocó: es teatro 100% original.
3. **LAS ESENCIAS** (las dos mesas): criaturas normales 0,1% → 1 · jefes 90%→1 / 5%→3 / 1%→10 (+ el resto 2-9) · oleada: chusma 1% → 1 · guardián 10 planas — y las presas de ESTATUA también caen en la mesa normal (ninguna mesa las excluye).
4. **LAS ESTATUAS**: `SpawnedFromStatue` → hambre **+0,1%** (0,001) con cifra ROJA en CombatText, el diálogo del grimorio («Esto es solo una ilusión… me da MÁS hambre») y la línea `ComoEstatuas` en la mesa del altar.
5. **LA LISTA DEL DESPERTAR**: la forma de la letra línea por línea (Para Despertar X · Esencia · dinero · barra X/100 · Devorar (A)/(M) con sprite+nombre · el Jefe J o J0/X con la cima compuesta de 3 presas · Próximo hito con el % de daño y Nivel X: (H) · la nota del altar) y las TRES TINTAS (verde `7FE89B` cumplido · rojo `FF7B7B` falta · gris oscuro `4A4A55` bloqueado tras las 2 pagas).
6. **EL BANCO RETROACTIVO + EL ARROJO + EL JEFE POR CHECKLIST**: `BancoGuardarMonedas`/`BancoCobrarMonedas`/`BancoGrupoDisponible` vivos en la DLL · `ArrojadoGlobalItem` solo marcan `EntitySource_DropAsItem` o padre-jugador · `JefeEnElChecklistActual` cena SIEMPRE en su peldaño, fuera solo la primera vez.
7. **LAS ARMAS DE LA TÉCNICA NUEVA**: `HazLib` (la librería de haz documentada, el filo anti-picos como ley) vive y la usan las TRES: **MiradaDelEterno** (con las texturas de la casa: DestelloEstelar/ResplandorChico/RuidoDendritico), **RayoConcentradoDos** y **RenglónFinal** (la definitiva nueva).
8. **LA PURGA CONFIRMADA**: Relicario del Avispón, Grilletes de Andrómeda, CampoAndrómeda y las 6 texturas copiadas AUSENTES de la DLL y del paquete; la LICENSE es propietaria (© 2026 Leo0x01) y VIAJA dentro del .tmod; el README legal sin una atribución.

## EL PLAYTEST (la lista de la casa — verifica y REHACE)
(0) [ ] **LA LIMPIEZA TOTAL**: en el CÓDIGO FUENTE ya no queda ni un carácter chino ni una mención IP (el guardián del audit lo exige en cada build); la plaga del conejo/coro de hadas se comporta IGUAL que siempre (puro teatro original).
(a) [ ] **LA LISTA**: abre la PROGRESIÓN del grimorio (clic der) — el apartado Despertar sigue la forma de la letra con sus TRES tintas.
(b) [ ] **DEVORAR 2**: consagra 2 opciones de un peldaño — las cumplidas VERDES, las demás GRIS OSCURO.
(c) [ ] **EL BANCO**: lo devorado en niveles bajos paga los checklist futuros (el retroactivo de la letra).
(d) [ ] **EL ARROJO**: el grimorio SOLO come lo que ARROJAS tú; el botín de enemigos espera en el suelo.
(e) [ ] **LAS ESTATUAS**: presa de estatua muerta por el grimorio → hambre +0,1% (cifra roja) y su diálogo.
(f) [ ] **EL JEFE**: el jefe del peldaño vivo cena SIEMPRE; fuera de su checklist solo su primera vez.

## PRÓXIMOS PASOS
- El playtest de arriba (la letra manda: verificar CADA punto en juego y rehacerlo hasta que funcione).
- EL RESTO DEL MOTOR DE PÍXEL (244 en el catálogo): el motor es de la casa — solo elegir.
- LOS CICLOS ANIMADOS del minigrimorio y Aethon desde una imagen base (el método del Vástago).

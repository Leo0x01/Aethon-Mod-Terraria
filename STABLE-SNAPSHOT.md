# SNAPSHOT ESTABLE — v6.130 — LAS CUCHILLAS DEL ETERNO: LA SFORJA + EL OJO DEL GRIMORIO ANIMADO

> **PUBLICADA — release 445120423 + CDN verificado byte a byte (md5 707ca2f38e8838608a8443a3e7f12213)**

## LA LETRA (el usuario — la sesión 11)

«Haber, regeneraste el sprite pero no usaste la SForja, usa la SForja y que sean cuchillas estilosas y recuerda usar y animar los ojos usando los ojos minions» + «Veo que en el sprite de las dagas hay un agujero que se supone que es el ojo, bueno en esa zona va el ojo del grimorio, ya tienes el sprite son los mismos ojos que se usan para los minions, pero más pequeños y adaptados a las dagas, bueno más que dagas es mejor que sean cuchillas y quitar el mango de las dagas en el sprite, así que sería mejor regenerar el sprite con eso en mente»

## LA RECETA `cuchilla` DE LA SFORJA — LA DAGA MUERE

LA DAGA v6.128 ERA 40 % MANGO (guarda + empuñadura vendada + pomo joya — medido por el QA VLM) — TODO eso SE BORRA. Nace LA CUCHILLA: TODO hoja (del filo al lomo, la base ES hoja), UN SOLO FILO con su lomo, y EL TALADRO DEL OJO taladrado DE VERDAD (ventana TRANSPARENTE interior con su anillo por alma). LAS FIRMAS: CRISTAL la daga-luna curvada (curva −0,15) · LLAMA la lengua de fuego SERPENTEANTE (la autopsia: `serp` estaba definido pero `lados()` JAMÁS lo sumaba — la hoja no serpenteba NADA; ahora ondula ±2,5 px) · ROCA el cincel macizo (lomo 13) con las venas de oro LEJOS del taladro (la ley del taladro: zona muerta 3,8 radios) · VACÍO la puerta RECTA (crec_tope 0,88) con el MARCO DORADO del ojo · VEREDICTO la balanza SIMÉTRICA (lomo=vientre=6,4, curva 0) con la gema centrada al pie. EL TALADRO VIVE SOBRE EL EJE de la hoja (el centro fijo x=17 se salía por el vientre curvo: el ojo nacía en C abierta). 10 RONDAS QA VLM → SHIP 9/9/9/8 + comparación A/B.

## EL OJO DEL GRIMORIO ANIMADO — EL TRASPLANTE DEL SISTEMA DEL ESCUADRÓN

LA ALMENDRA recortada del ojo del minion (CuchillaOjo_Abierto/Medio/Cerrado 27×15 — la esclerótica blanca PURA: el ojo entero 56×54 a 9 px era todo párpado marrón) · EL PARPADEO INDEPENDIENTE (el reloj de la casa: 6–18 s por hash, la fase 10→1, cortado durante viento/golpe/clavada) · EL IRIS VIVO GrimorioHambriento_Iris (3,7 px) que MIRA a la presa (rotación inversa: se desplaza hacia la presa EN EL MUNDO) · EL TRONO POR TIPO (TronoDelOjo: offsets medidos con flood-fill del taladro real de cada marco). SIM 1:1 + GIF del parpadeo y la mirada; 5 rondas QA VLM → SHIP 9/8/7/9.

## PARA VERIFICAR

- oráculo verify.csproj **0/0** (465 .cs contra tML 2026.09.3.1 real)
- **-build REAL 0/0** (527 entradas, 19.364.946 B, md5 707ca2f38e8838608a8443a3e7f12213)
- **AUDIT v6.130 TODO VERIFICADO ✔** (EOF · blobs 527/527 · hjson ×3 == fuente · SIN MANGO medido · TALADRO 5/5 · los 3 recortes DENTRO · firmas VIVAS en la DLL · iris y ojos del escuadrón INTACTOS · paridad 1686/1686/1686)
- **headless**: Sandboxing v6.130 → Adding Recipes → Server started, 0 excepciones
- QA VLM del sprite: SHIP 9/9/9/8 (10 rondas) · QA del ojo: SHIP 9/8/7/9 (5 rondas)

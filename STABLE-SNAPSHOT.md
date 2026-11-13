# SNAPSHOT ESTABLE — v6.129 — EL ALA LISA SIN PLUMAS + EL CETRO QUE INVOCABA + LA DEVORACIÓN DE LA PRIMERA VEZ

> **PUBLICADA — release 444667317 + CDN verificado byte a byte (md5 bdca26034758b43bb8bbf2906eeea95d)**

## LA LETRA (el usuario — la sesión 10)

«las alas siguen con picos, diente de sierra, supongo que esos dientes de sierra son supuestas plumas, esas plumas las puedes eliminar, aunque ahora las alas se ven mejor, pero esos dientes de sierra siguen arruinandolas» + «el arma cetro de las dagas del eterno no invoca nada, mejora eso y revisa porque no invoca nada, haz que esta arma no sea de invocador asi no se ven afectado los minions» + «quita eso ya que no hace diferencias y todo lo que el jugador usa está separado del jugador cuando es devorado, te dire algo, la primera vez que se puso la mecanica de que el grimorio del eterno devore al jugador esta animacion estaba correcta, asi que investiga como funcionaba la primera vez que se puso esto de que el jugador sea devorado»

## EL ALA LISA — LAS PLUMAS MUEREN (la letra manda)

LAS PLUMAS ERAN LOS DIENTES — la autopsia contra el sim 1:1 nuevo (`tools/sim_alas_v6129.py`, con la rasterización baricéntrica CORREGIDA: la de la v6.127 solo pintaba una línea por triángulo) encontró a los TRES culpables: **LAS TIRAS RADIALES** (las capas sólidas del núcleo·cuerpo·halo: cinco rayos convergiendo en cinco puntas) · **LA PALETA VIAJERA** (el tono por franjas cae al FONDO NEGRO entre plumas: «huecos negros») · **EL BORDE POR LAS CINCO PUNTAS** (la Catmull-Rom por radios desiguales 90→52 px hace OVERSHOOT y ONDULA: ±25 px medidos). LA CURA (nuevo parámetro del catálogo `Lisas`, SOLO en Runas del Vacío): las tiras MUEREN (las plumas quedan como ARMADURA invisible) · el tono es LA RAMPA RADIAL (vivo → hondo puro → el blanco del bordillo: el fondo JAMÁS) · LA VELA SE ESTIRA al BORDE LISO (el camino u: nacimiento → cúspide → tendida → nacimiento, en smoothstep por tramos — cada eje corre hasta SU punto del borde) · EL BORDE MUESTREADO del camino (SIN Catmull: liso por construcción) · LAS RUNAS coronan el borde en las direcciones de las plumas. QA VLM: «0 tiras radiales; superficie continua; el borde es UNA CURVA CONTINUA y suave; la forma de abanico se conserva» — **9/10 · 8/10**. Solares, Eclipse, Juicio y Corolas INTACTAS (abanico de siempre).

## EL CETRO QUE NO INVOCABA — DOS CAUSAS RAÍZ

**(A) EL GATILLO FALTABA**: el cetro nació sin `Item.shoot` y el hook `Shoot` de tModLoader SOLO corre cuando `Item.shoot > 0` — el grimorio de la casa lleva `Item.shoot = 931` por eso. Ahora el cetro lleva SU PROPIO escuadrón como gatillo (nada de vanilla vuela: Shoot devuelve false). **(B) LOS PUESTOS ASESINABAN AL ESCUADRÓN**: `minionSlots = 1` × 16 unidades contra `maxMinions` 1-11: la maquinaria vanilla mataba a los minions que no cabían (los del jugador Y los propios — «no invoca nada» y «se ven afectados los minions», literales). LA CURA: el cetro es `DamageClass.Magic` (la clase del grimorio — el daño sigue la ley v6.51 con DanoOjosDelEterno) y **`minionSlots = 0`** en el ojo y la daga: el escuadrón tiene SU PROPIO contador (ni desplaza a nadie, ni muere por exceso — el techo vivo del ShardPlayer y el minionPos del círculo VIVEN).

## LA DEVORACIÓN DE LA PRIMERA VEZ (la v6.117, leída contra el git)

**DevoracionCosmeticosSistema y la ventana «Arrastrando» MURIERON.** LA AUTOPSIA de su inutilidad: los halos cosméticos NUNCA se dibujan desde su posición de proyectil — todos dibujan desde el CUERPO VIVO del dueño en su PreDraw (con `pl.fullRotation` aplicada, desde la v6.117): pegar `pr.Center` tras todas las IA era pegar NADA. Y lo que SEPARABA las alas del cuerpo era EL PLEGADO v6.126-127 (las alas se contraían y desaparecían apenas empezaba el arrastre, mientras el cuerpo seguía viajando). LO QUE VIVE (exactamente la primera vez): el arrastre de siempre (lerp suave + fullRotation · la boca y la cacería inversa intactas), los cosméticos viajando TAL CUAL con el cuerpo (las alas ABIERTAS girando CON él) y al TRAGO todo desaparece JUNTO (hideEntirePlayer + los halos apagados por Tragado). Nada de pegamentos, nada de plegados, nada de ventanas.

## VERIFICACIÓN (la casa, completa)

oráculo verify.csproj **0/0** (465 .cs contra el tML 2026.09.3.1 real) · **-build REAL 0/0** (524 entradas, 19.361.323 B, md5 bdca26034758b43bb8bbf2906eeea95d) · **GUARDIÁN TODO VIVE ✔** · **AUDIT v6.129 TODO VERIFICADO ✔** (EOF exacto · 524/524 blobs · hjson ×3 empaquetado==fuente · las firmas VIVAS en la DLL: EstirarVelaLisa·PuntoCaminoLisa·PADLisa·RNaceLisa·Lisas·Tragado·Devorando · los MUERTOS JAMÁS en la DLL: DevoracionCosmeticosSistema·Arrastrando · el tooltip del ala lisa ×2 · paridad de claves 1686/1686/1686) · **SIM 1:1 + QA VLM 9/10 · 8/10** · **headless**: Sandboxing v6.129 → Adding Recipes → Choose World SIN UNA EXCEPCIÓN.

---

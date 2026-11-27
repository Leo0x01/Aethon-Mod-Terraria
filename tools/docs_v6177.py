#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""docs_v6177.py — LA DOCUMENTACIÓN DEL RELEASE v6.177.

CHANGES.md (la letra de la sesión citada) + STABLE-SNAPSHOT.md (el
estado de la publicada) + MAESTRO.md (la versión, la era, los tamaños
y LA LECCIÓN de la barra: ModBossBar.ModifyInfo devuelve TRUE o la
base PISA tus valores — y sus últimos dos parámetros son SHIELD y
SHIELD_MAX, no «scale» y «dps»).
"""
import re

# ══ 1. CHANGES.md — LA LETRA DE LA SESIÓN 30 ═════════════════════════
R = 'CHANGES.md'
s = open(R, encoding='utf-8').read()
entrada = '''## v6.177 — EL PARTIDO DE LOS GIGANTES, LA ASPIRADORA UNIVERSAL Y EL VISOR DEL PROYECTADO — la letra del usuario (sesión 30: «recuerdo el loop de retroalimentacion constante para mejorar y depurar errores, que este loop sea repetido tantas veces como sea posible» + «y comienza con esto, asegurate de no tener errores: Los 3 archivos de +3.000 líneas (baja): se pueden partir por responsabilidades sin tocar comportamiento» + los reportes: los textos desbordados · el slot de ofrendas con texto innecesario · la ventana flotante ilegible · los jefes de niveles altos con el mismo proyectil · el nivel limitado a 10 · la barra 1/0 · los devorados que no callan · la aspiradora solo en el interior · el tentáculo rezagado · los tentáculos que no nacen del grimorio · los jefes del interior según clima/hora/bioma · el Faro (su ATAQUE) · «el arma del el plano no tiene nada que ver te voy a dar el archivo .tmod» · la bruma del fondo del paisaje)

1. **EL PARTIDO DE LOS TRES GIGANTES** (la letra: «comienza con esto, asegurate de no tener errores»): los tres archivos de +3.000 líneas PARTIDOS por responsabilidades SIN tocar comportamiento — cada método mudado INTACTO byte a byte por `tools/parte_archivos_v6177.py`, con el multiset de líneas del original == la suma de los repartidos y el oráculo 0/0 de garante: **AuraLib** (4.864) → `AuraPerfil.cs` (las RECETAS: ParticulasAura + AuraPerfil con sus fábricas) + `AuraLib.cs` (el MOTOR) + `AuraLibDibujo.cs` (los DIBUJOS: DibujarNPC · DibujarJugador · las Formas Divinas · HaloArcoiris) · **AethonBoss** (4.072) → `AethonBoss.cs` (la PELEA) + `AethonBossDibujo.cs` (PreDraw · PostDraw · FindFrame) · **AtaqueJefeProjectile** (3.753) → `AtaqueJefeProjectile.cs` (EL ARSENAL + AnilloCoro) + `AtaqueJefeDibujo.cs` (los pases de dibujo). Las clases viven `partial`: mismos namespaces, mismos nombres, mismo comportamiento.
2. **LA BARRA QUE LLEGA DE VERDAD** (la letra: «las barras de vida de los jefes dentro del mundo interior siempre empieza con 1/0 y siempre aparece justo asi… deberia el numero reflejar el aumento de vida sin que la barra se desborde»). LA AUTOPSIA contra el decompile del tML real: `ModBossBar.ModifyInfo` devuelve **null** hace que la base PISE los life/lifeMax que uno calculó con los de LA PIEZA seguida — nuestra suma del gusano entero NUNCA llegaba a la pantalla; y los dos últimos parámetros de la firma NO son «scale» y «dps»: son **shield y shieldMax** — nuestra «escala de nivel» vestía un escudo fantasma. LA CURA: `return true` — la barra muestra NUESTRO número (la suma viva de todas las piezas selladas de la arena) y nadie toca el escudo.
3. **LA ASPIRADORA UNIVERSAL DE LA SEGUNDA INVOCACIÓN** (la letra: «la animacion de devorar al Devoramundos esta mal… lo probe en el mundo interior y siempre lo devora · en las oleadas de hambre y no lo devora correctamente · en el mundo normal y lo devora una pieza a la vez… esta animacion se debe hacer solo la segunda vez que el Devoramundos sea invocado»): EL VIGÍA UNIVERSAL — cada CABEZA de Devorador (EoW o Destructor) que NACE en el mundo cuenta UNA invocación del portador más cercano (`FaucesGlobalNPC.OnSpawn`: pilar · oleadas · comida de gusano del mundo real — TODO cuenta) y la SEGUNDA invocación EXACTA arranca el cine espacial. Las demás veces (primera, tercera en adelante): la devoración normal del Devoramundos está MUERTA EN TODAS PARTES (`EsGusanoInvocado` sin sello también) — pelea normal, muerte normal con botín.
4. **EL CINE PULIDO**: la VIGILIA espera también a que la OLEADA termine de ALARGAR la cadena (`GrimorioFuriaSistema.ExtensionDevoradorPendiente` — antes capturaba a mitad de construcción) y la CAPTURA le suelta la cadena a la oleada; y EL VAIVÉN DE LA MANADA (la letra: «uno de los tentaculos se queda medio rezagado»): las piezas ondeaban con FRECUENCIAS distintas (i%5, i%3) — una siempre leía más lenta. Ahora TODAS ondean a la MISMA frecuencia con SU fase propia.
5. **EL CORO NACE DEL LIBRO** (la letra: «los tentaculos… deben aparecer desde el mismo grimorio y deben extenderse hacia afuera en direcciones al jefe que sera devorado, luego estos lo cubren e inicia el resto de la animacion»): DOS TIEMPOS LIMPIOS en la primera cena — (1) EXTENDER: la punta nace EN el libro y viaja hacia afuera hasta SU puesto en la jaula; (2) CUBRIR: ya extendidos, la jaula se ciñe y la cobertura Devorador crece DESPUÉS (antes «alcance = sale·0.35 + festín·0.65» mezclaba ambos tiempos y los brazos nunca se leían creciendo del libro).
6. **LA PURGA DE LA MESA** (la letra: «todos los jefes devorados mientras son devorados deben callar en todos los sentidos»): al ARRANCAR cualquier devoración (primera cena · festín clásico · aspiradora), los proyectiles hostiles que vuelan alrededor del preso SE APAGAN con una mota — nada del jefe sigue volando mientras el libro come.
7. **LA PUERTA SIEMPRE ABIERTA** (la letra: «asegurate de hacer que el mundo interior sea valido para invocar a todos los jefes de terraria sin importar el clima, tiempo o bioma»): DENTRO del Mundo Interior TODO objeto de invocación de Terraria FUNCIONA SIEMPRE (`InteriorInvocaGlobalItem`): el ojo sospechoso al mediodía, la comida de gusano sin corrupción, el gusano mecánico de día, el muñeco del sastre sin sastre… el jefe nace delante del portador y el consumible paga su unidad. Fuera del interior: vanilla manda como siempre.
8. **LA ESCALERA SIN TECHO** (la letra: «los jefes no necesitan estar limitados a nivel 10, que este nivel sea infitino y cuanto mas nivels mas complicado, mas inteligente y tiene nuevos ataques, combos y nuevos movimientos, asi como una IA mas avanzada»): el nivel del sello es 1+kills SIN TOPE y cada tramo añade MOVIMIENTOS NUEVOS — **nivel 8+: EL DASH DOBLE** (cerrada la embestida, si la presa sigue en rango, una SEGUNDA inmediata con telégrafo corto) · **10+: LA VOLEA DOBLE** (dos voleas seguidas de patrones distintos) · **12+: EL SALTO DE SOMBRA** (el jefe se desvanece y reaparece DETRÁS de la presa — telégrafo incluido) · **15+: EL COMBO DE CUATRO PASOS** (dash → anillo → andanada → dash certero final) · **20+: LA FURIA** (todo se acelera 20%). La vitrina del pilar muestra la estatura REAL del jefe.
9. **LAS VOLEAS TEMÁTICAS VANILLA** (la letra: «parecen que los jefes en niveles altos… tienen el mismo ataque de proyectiles luminoso, eso esta mal, el propio terraria cuenta con proyectiles originales… un ejemplo es el Devoramundos, en su bioma estan los devoralmas que lanzan un proyectil especial»): el mapa `ProyectilTematico` — CADA jefe dispara SUS proyectiles de Terraria (verificados contra el decompile): el gusano de la corrupción escupe la LLAMA MALDITA (CursedFlameHostile — la de los devoralmas) · el Cerebro el ICHOR · la Abeja su AGUIJÓN · Esqueletrón la CALAVERA · la Muralla la BOLA DE FUEGO · los MECÁNICOS el LÁSER DE LA MUERTE / la llama / la BOMBA del Prime · Plantera sus SEMILLAS y las venenosas · el Duque el RAYO DEL TIBURÓN · la Emperatriz su ESTELA ARCOÍRIS · el Señor de la Luna el OJO FANTASMAL y la ESFERA · Deerclops su FRAGMENTO DE HIELO · el Sectario su BOLA DE FUEGO y su ORBE DE RAYOS. El PATRÓN (abanico · anillo · lluvia · espiral) sigue siendo de la casa; el PROYECTIL es de Terraria.
10. **LA TUMBA DE NIEBLA DEL PAISAJE** (la letra: «cuando digo en el fondo del mundo interior me refiero al fondo del paisaje, esos tentaculos que se ven a los lejos… necesitan brumas en el fondo para que no se vean cortados»): la niebla de la v6.176 (UN puff a ras de punta) era un velo tímido — el borde se leía cortado debajo. Ahora TRES masas a lo largo del rumbo final SEPULTAN la punta de cada tentáculo en las TRES capas del cielo: en ella · bajo ella · más ancha y floja más abajo. El tentáculo se APAGA en la bruma, no termina.
11. **LA INTERFAZ QUE SE LEE** (la letra: «el texto en los botones del altar se desborda un poco… el slot de ofrendas deberia estar el slot de ofrenda sin textos… la ventana flotante tiene un texto muy pequeño… con que diga elige un arma es suficiente… todos los textos del altar son muy pequeños… en el pilar los textos de los botones se desbordan… demasiado claros en cuanto a color»): EL SLOT de la ofrenda queda LIMPIO (el «ofrenda» de dentro MUERE) · LA VENTANA FLOTANTE con el título GRANDE (escala 1.0) y su título del trío ahora dice solo **ELIGE UN ARMA** · TODAS las escalas del altar y el pilar SUBEN (~+0.10) · LOS BOTONES a escala base 1.05 y 46 px de alto, medidos al trazo con margen de halo — nada desborda y se lee · EL HALO un punto más manso (0.82→0.75 de alfa).
12. **EL VISOR DEL PROYECTADO — EL ARMA DE VERDAD DEL MÍMEO** (la letra: «el arma del el plano no tiene nada que ver te voy a dar el archivo .tmod del el plano del haz»): el plano del haz fue EL PLANO (mecánica y espíritu, decompilado y leído); TODO lo demás es de la casa. **EL VISOR** (daño MÁGICO que escala SOLO: 11 etapas por los jefes caídos, +5% por cada opcional, bono por enemigos con tope por etapa) · **EL RAYO CONTINUO** (mantén clic: sale del ojo, sigue al cursor, 900 px — la PILA DE CAPAS de la casa sobre LA MISMA polilínea: bruma → glow → cuerpo → núcleo del rojo del meme) · **LA PROYECCIÓN (tecla X)**: 3,5 s de carga — el visor SE LEVANTA de la cara, el ojo de abajo SE ENCIENDE, las motas convergen y el mundo tiembla — y el rayo ENORME que ATRAVIESA TODO (2.600 px), empuja al portador y tiñe la pantalla de rojo; 20 s de enfriamiento (el buff los cuenta) · **LA ENERGÍA PROYECTADA** (una barra propia: nada de maná — 100 que crecen con las mejoras hasta 300, recarga 15/s, se agota a 0 y vuelve a los 25) · **LAS DIEZ MEJORAS QUE DUELEN** (cada jefe suelta su consumible: la CARTA QUE NUNCA LE DISTE del Ojo · la CAPTURA DEL VISTO de Esqueletrón · el «NO ERES TÚ, SOY YO» de la Muralla · los TRES PROYECTORES de los mecánicos por orden de caída · las FLORES AMARILLAS de Plantera · el «YA LO SUPERÉ» de Gólem · el «HAY MÁS PECES EN EL MAR» del Duque · LA LUNA QUE TE IBAS A BAJAR del Señor de la Luna: ¡PROYECCIÓN INFINITA!) · **EL HUD** (la barra de energía con el número vivo y la marca de lo agotado; la palabra INFINITA para el que se bajó la luna) · **LA CAPA** (el visor vive en la cara — y se levanta flotando durante la proyección mientras el ojo arde) · la entrega al PRIMER mundo de cada personaje. El arma VIEJA (Proyección del plano + su rayo) MUERE con todo su árbol.
13. **EL ECLIPSE QUE BARRE — EL ATAQUE DEL FARO** (la letra: «el faro del eclipse, bueno lo que tenias que rediseñar era su ataque o proyectil, asi que rediseñalo»): el conducto continuo MUERE. AHORA: el faro dispara EL DISCO NEGRO (la luna del eclipse) que vuela hacia donde apuntas → se CLAVA y nace EL ECLIPSE (el disco negro con SU filo violeta y su CORONA PICADA de 22 picos que SE ENCIENDEN cuando el haz los cruza) → y de su centro EL HAZ DEL FARO GIRA barriendo TODO a su paso (~una vuelta cada 1,2 s, 640 px, la pila de capas de la casa) → el eclipse vive 3,5 s y IMPLOTA. (QA visual: 7.5 SHIP.)
14. **LOS DAÑOS DEL GRIMORIO, VERIFICADOS DE VERDAD** (la letra: «asegurate de que los daños de invocacion y magicos del grimorio del eterno realmente se aplican»): la auditoría contra el decompile del tML real — el MÁGICO fluye por `ModifyWeaponDamage` (nivel × despertar × devorado × esencias) y vanilla lo viste con el equipo mágico; el de INVOCACIÓN nace de `DanoOjosDelEterno` y vanilla lo recalcula CADA TICK (`GetTotalDamage(Summon).ApplyTo(originalDamage)` — minion=true + DamageType=Summon): el equipo de invocador del portador SÍ cuenta, exactamente como el tooltip lo promete.
15. **LA 2ª PURGA DEL EN-US**: el loop cazó 183 claves fósiles (tooltips de bolsas que el es-ES ya no pide + los restos de una doble inserción de la sesión) — purgadas. **PARIDAD PERFECTA: 1900 = 1900, cero huérfanas en ambos sentidos.** Y el LOOP DE RETROALIMENTACIÓN corrió y comparó TODA la sesión (la letra: «que este loop sea repetido tantas veces como sea posible»): cazó y curó la regresión de claves EN-sin-ES (183→0) y las pedidas-sin-texto (18→16 — las 16 falsas positivas dinámicas de siempre) hasta el verde.

**GATES**: oráculo 0/0 (486 .cs) · build REAL 0/0 + Packaging (536 entradas) · GUARDIÁN TODO VIVE ✔ (el visor, sus diez mejoras y el buff con sus sprites) · AUDIT v6.177 TODO VERIFICADO ✔ (firmas vivas · el arma vieja MUERTA con su PNG fuera del .tmod · los SPLIT verificados contra la fuente · paridad 1900=1900) · headless 0 excepciones (Sandboxing v6.177) · loop retro SIN REGRESIONES (fósiles 183→0 · pedidas 18→16) · sim aspiradora SHIP · sim visor+faro SHIP (QA 7.5) · QA sprites 8/10 SHIP.

'''
# insertar al inicio (después del título si lo hubiera — el archivo
# empieza con la entrada más reciente)
assert '## v6.176' in s
s = s.replace('## v6.176', entrada + '## v6.176', 1)
open(R, 'w', encoding='utf-8', newline='\n').write(s)
print('OK CHANGES.md')

# ══ 2. STABLE-SNAPSHOT.md ════════════════════════════════════════════
R = 'STABLE-SNAPSHOT.md'
s = open(R, encoding='utf-8').read()
m = re.search(r'md5 `([a-f0-9]+)`, ([\d,.]+) B', s)
vieja = s
nuevo = '''# SNAPSHOT ESTABLE — v6.177 — EL PARTIDO DE LOS GIGANTES Y EL VISOR DEL PROYECTADO

> **PUBLICADA — release de GitHub + CDN verificado (md5 en CHANGES.md)**

## LOS QUINCE PUNTOS DE LA SESIÓN 30

1. **Los tres gigantes PARTIDOS** (AuraLib → perfil+motor+dibujo · AethonBoss → pelea+dibujo · AtaqueJefe → arsenal+dibujo): clases `partial`, métodos intactos byte a byte, oráculo 0/0 de garante.
2. **La barra 1/0 MUERTA**: `ModifyInfo` devuelve TRUE — la base ya no pisa la suma del cuerpo entero (y los parámetros 4-5 son shield/shieldMax, no scale/dps).
3. **LA ASPIRADORA UNIVERSAL**: el vigía `OnSpawn` cuenta TODAS las invocaciones del Devorador (pilar · oleadas · mundo real) y el cine espacial nace SOLO la SEGUNDA vez; las demás son pelea y muerte normal.
4. **El cine pulido**: la vigilia espera la extensión de la oleada y el vaivén de la manada ondea EN FRECUENCIA UNA (adiós, tentáculo rezagado).
5. **El coro NACE del libro y SE EXTIENDE** hacia el jefe antes de cubrirlo (dos tiempos limpios).
6. **LA PURGA DE LA MESA**: al devorar, los proyectiles del jefe SE APAGAN — calla en todos los sentidos.
7. **La puerta siempre abierta**: en el interior TODO ítem de invocación de Terraria funciona sin clima, hora ni bioma.
8. **La escalera sin techo**: nivel 1+kills SIN tope — 8+ dash doble · 10+ volea doble · 12+ salto de sombra · 15+ combo de cuatro · 20+ furia.
9. **Las voleas temáticas VANILLA**: cada jefe dispara SUS proyectiles de Terraria (el gusano, la llama maldita de los devoralmas).
10. **La tumba de niebla del paisaje**: tres masas sepultan la punta de cada tentáculo de fondo en las tres capas.
11. **La interfaz que se lee**: slot de ofrenda limpio · ventana flotante con título grande que dice ELIGE UN ARMA · escalas +0.10 · botones 1.05 y 46 px medidos con halo.
12. **EL VISOR DEL PROYECTADO** (el arma de verdad del plano del haz, 100 % casa): rayo continuo + la PROYECCIÓN (X) + energía 100→300 + 11 etapas + las DIEZ MEJORAS QUE DUELEN de los jefes + HUD + el visor en la cara. El arma vieja murió.
13. **EL ECLIPSE QUE BARRE**: el ataque del Faro rediseñado — disco negro, corona picada viva y el haz del faro GIRANDO.
14. **Los daños del grimorio VERIFICADOS** contra el decompile: el mágico por ModifyWeaponDamage; el de invocación recalculado por vanilla cada tick (GetTotalDamage(Summon) sobre originalDamage).
15. **La 2ª purga del en-US**: 183 fósiles purgados — paridad 1900 = 1900; el loop corrió y cazó la regresión hasta el verde.

## VERIFICACIÓN
Oráculo 0/0 · build 0/0 · GUARDIÁN ✔ · AUDIT v6.177 ✔ · headless 0 exc · loop sin regresiones (fósiles 183→0) · sims SHIP (aspiradora · visor+faro 7.5 · sprites 8/10).
'''
open(R, 'w', encoding='utf-8', newline='\n').write(nuevo)
print('OK STABLE-SNAPSHOT.md')

# ══ 3. MAESTRO.md ════════════════════════════════════════════════════
R = 'MAESTRO.md'
s = open(R, encoding='utf-8').read()
s = s.replace('| **Versión actual** | **6.176** (vive en `AethonMod/build.txt`, línea `version = 6.176`) |',
              '| **Versión actual** | **6.177** (vive en `AethonMod/build.txt`, línea `version = 6.177`) |')
s = s.replace('**La era v6.176**: LA ASPIRADORA QUE ESPERA (la vigilias de 5 s de la letra + el trago simultáneo) · EL GUSANO QUE CALLA (CongeladoPorAspiradora) · EL ARMA DEL HAZ (ProyeccionDelel plano) · LAS TIJERAS DEL LIBRO (nada desborda el papel) · LA PURGA DEL EN (194→0) · EL AZAR SEMBRADO (7 llamadas reales) · R21/R22 en el MAESTRO.',
              '**La era v6.177**: EL PARTIDO DE LOS GIGANTES (los tres +3.000 partidos por responsabilidades, partial e intactos) · LA BARRA QUE LLEGA (ModifyInfo return true — la base no pisa la suma) · LA ASPIRADORA UNIVERSAL (solo la 2ª invocación, en todas partes) · LA PURGA DE LA MESA · LA PUERTA SIEMPRE ABIERTA · LA ESCALERA SIN TECHO (el salto de sombra y los tramos 8/10/12/15/20) · LAS VOLEAS VANILLA TEMÁTICAS · LA TUMBA DE NIEBLA · EL VISOR DEL PROYECTADO (el arma del meme, 100 % casa) · EL ECLIPSE QUE BARRE (el ataque del Faro) · la 2ª purga del EN (paridad 1900=1900).\n\n**La era v6.176**: LA ASPIRADORA QUE ESPERA (la vigilias de 5 s de la letra + el trago simultáneo) · EL GUSANO QUE CALLA (CongeladoPorAspiradora) · EL ARMA DEL HAZ (ProyeccionDelel plano) · LAS TIJERAS DEL LIBRO (nada desborda el papel) · LA PURGA DEL EN (194→0) · EL AZAR SEMBRADO (7 llamadas reales) · R21/R22 en el MAESTRO.')
# los contadores de tamaño
s = s.replace('| **Tamaño** | 456 archivos .cs · 500 png · 78 librerías VFX · 446 herramientas · 49 versiones publicadas en CHANGES.md |',
              '| **Tamaño** | 486 archivos .cs · 500 png · 79 librerías VFX · 446 herramientas · 50 versiones publicadas en CHANGES.md |')
# LA LECCIÓN nueva en el §4.2 (la técnica de la AUDIT) — apuntarla:
s = s.replace('### 4.3 El publicador',
'''### 4.2.1 LA LECCIÓN DE LA BARRA (v6.177, la letra: «la barra siempre empieza con 1/0»)

`ModBossBar.ModifyInfo` devuelve **true** para que TUS life/lifeMax
sean los que se dibujen — al devolver **null** la base PISA ambos con
los de la pieza seguida («life = Clamp(nPC.life…); lifeMax =
nPC.lifeMax»), y tu cálculo muere en la firma. Y los dos últimos
parámetros de la firma son **shield y shieldMax** — aunque el nombre
que les pongas diga otra cosa, un valor ≠ 0 ahí viste UN ESCUDO.
(Verificado contra el decompile del tML 2026.09.3.1.)

### 4.3 El publicador''')
open(R, 'w', encoding='utf-8', newline='\n').write(s)
print('OK MAESTRO.md')

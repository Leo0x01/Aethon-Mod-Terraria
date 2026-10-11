#!/usr/bin/env python3
"""v6.50.105 — la cirugía de docs: CHANGES + STABLE-SNAPSHOT + README."""


CHANGES = '''## Commit v6.50.105 — LOS OJOS DEL ETERNO

**La letra**: «también debemos cambiar los minions, los minion que use el
grimorio será su propio ojo […] es tiempo de animar al minion, yo te daré
los sprite del ojo usando código es lo mejor para que pudieses editarlo en
caso de que convenga, ya sabes como animar los ojos, ahora ten en cuenta
que el ojo se mueve en la direccion en la que mira, o sea para moverse
primero mira y luego se mueve, algo mas, estos minion se posicionan encima
del jugador como una corona, y si el grimorio esta fuera comienzan a
orbitar al grimorio en 2 circulos uno de 5 minion, otro de 6 y asi
aumentando los circulos orbitales a mas minion pueda lanzarce […] cada
minion mira a su objetivo y usa el rayo con ruido de perlin ademas de que
este rayo usado por los minion es negro, con bordes morado y con bruma en
la punta que toca al monstruo, cada minion debe tener un pequeño circulo
magico igual que el de los agujeros negros […] cuando el minion dispara
tiene un pequeño retroceso y luego se reincorpora, los ojos de cada minion
son independientes de cada uno de ellos y del libro y no se ven afectados
por la animacion de hambre del grimorio».

**EL CAMBIO**: el Orbe Cósmico muere entero (minion + bolt + buff + 2
rawimg) y nacen LOS OJOS DEL LIBRO:

- **EL SPRITE** (tools/gen_ojo_eterno_v650105.py): el recorte del ojo del
  sprite EXACTO del usuario (caja 226,344-524,596: la almendra blanca +
  el armazón lila + el párpado inferior dorado), 44×37 ×3 estados
  (abierto/medio/cerrado) por LANCZOS. El IRIS de la .84 va como CAPA
  que se desliza (escala 0.52 — el arte trae el iris sobredimensionado:
  a escala proporcional taparía la vista entera y la mirada no leería).
- **LA MÁQUINA DE LA MIRADA** («primero mira y luego se mueve»): el ojo
  fija el iris en el destino y FRENA (MIRANDO 6 ticks), y solo entonces
  se desplaza (MOVIENDO); el ángulo del iris (localAI[2]) barre 0,22
  rad/tick; la mirada ±7,5 px horizontal / ±3,5 px vertical (la
  geometría de la almendra), alcance por distancia.
- **LA FORMACIÓN**: CORONA sobre la cabeza del dueño (arco 200°→340°,
  radio 52+2,2·n, vaivén por ojo) y con el ESPÍRITU FUERA
  (PosicionDelLibro del Nervioso, cacheado por frame) ÓRBITAS
  CONCÉNTRICAS AL LIBRO: el anillo k lleva 5+k ojos (5, 6, 7… — la
  letra), radio 58+42·k, contrarrotando (par +0,34 / impar −0,27).
- **EL RAYO** (negro con bordes morados): el canal Perlin VERBATIM del
  PerlinBolt (fBm 3 octavas + jitter 2 + el arco congelado + la
  envolvente ^10 + Chaikin) dibujado en DOS capas: la FUNDA MORADA
  aditiva (RayoStrip.Filamento, violeta vivo 190,110,255, ancho 9,5) y
  encima EL NÚCLEO NEGRO en AlphaBlend (cadena de SoftGlow negros
  5,5→3 px que OSCURECEN — el patrón del horizonte de sucesos del
  agujero negro) + los gorros de descarga en ambos electrodos + LA
  BRUMA (SombrasLib.Bruma: humo negro + aliento violeta) en la punta
  que toca al monstruo. El golpe por GOLPEMOTOR (el cauce del motor:
  crítica real, varianza, on-hit, crédito de la kill), daño
  ×MinionContactDamageMult(nivel), cadencia 110−nivel/3 (piso 55).
- **EL RETROCESO**: patada física 3,0 px/tick contra el rayo al
  disparar y la formación lo reincorpora (el freno del fuego 0,86/tick).
- **EL CÍRCULO MÁGICO**: OrbitaLib.SigiloErosionado — EL MISMO de los
  agujeros negros (RunasAbismo, dorado-ámbar 240,124,65 / 255,205,140,
  el aro roto con huecos), r 13 (anillo a 33 px) que RESPIRA a 16 en la
  carga del disparo; la presencia violácea detrás del ojo.
- **EL PARPADEO INDEPENDIENTE**: reloj propio por ojo (hash de whoAmI,
  6–18 s, cero RNG), SIN leer NADA del hambre del libro; al apuntar, el
  párpado se corta y la mirada queda FIJA en blanco (la mirada del
  cazador).
- **hjson ×3** (es-MX espacios primario / es-ES+en-US tabs por BYTES):
  Ojos del Eterno / El Ojo del Eterno / Eyes of the Eternal + el
  tooltip del ítem «invoca los Ojos del Eterno (corona/órbitas + rayo
  negro)».

**VERIFICACIÓN**: audit_texturas 0 · oráculo 0/0 · build real 0/0 (ruta
absoluta) · VLM ×3 (los sprites: hoja de validación — limpio, la mirada
lee, el parpadeo lee; el rayo: 3 rondas de calibración — 11 px tapaba la
vena → ancho 9,5 + violeta vivo + negro 5,5→3; SHIP por juicio de la
casa) · audit_v650105 TODO OK: .tmod 2.584.158 B md5
e7cd269982ffbd1dfa5b8b77b325dbe1, 387 entradas (386 − 2 del Orbe + 3 de
los Ojos), EOF exacto, paquete==árbol ×3, rawimg píxel-idénticas al árbol
(12 B de cabecera + RGBA — la .100), símbolos OjoEternoMinion/OjoEternoBuff/
DrawElSello/CanalPerlin vivos + CosmicOrb* AUSENTES, las constantes de la
letra en el IL (0.52/7.5/3.5/0.22/3.0/3.49/5.93/58/42/2.55 + el reloj
1/216000 y el 0.10 del jefe), regresión .102/.103/.104 completa · headless
«Sandboxing v6.50.105 → Adding Recipes → Choose World» 0 excepciones.

**PUBLICACIÓN**: commit `1853196` + tag `v6.50.105` → release
**409768350** (make_latest) + asset AethonMod.tmod **631162660**
2.584.158 B (md5 `e7cd269982ffbd1dfa5b8b77b325dbe1`), CDN VERIFICADO
BYTE A BYTE, `/releases/latest` = v6.50.105. Respaldo
`/home/sync/AethonMod-v6.50.105.tmod`.

**LECCIONES**: (a) el rawimg del .tmod es 12 B de cabecera (1, w, h) +
RGBA puro — se compara contra el PNG del árbol DECODIFICADO, no contra
los bytes del archivo; (b) el .cs nunca es entrada del .tmod — el oráculo
de presencia de una clase es el heap #Strings, el de una textura es la
rawimg; (c) el VLM con mocks de composite aditivo OSCILA (3 rondas
contradictorias sobre el mismo arte) — tras calibrar con la física del
render (la vena de RayoStrip es ~2 px EN EL EJE: el borde morado vive en
la columna 0→0,42 de la funda), SHIP por juicio de la casa; (d) el hjson
trae el tooltip del ÍTEM en la sección Items — la cirugía de nombres no
basta: grep global del nombre viejo antes de cerrar.

---

'''

SNAP_HEADER = '''# AethonMod — ESTADO ACTUAL (v6.50.105)

> **Este documento = "¿por dónde nos quedamos?"** — se actualiza en cada entrega.
> Última actualización: v6.50.105 (LOS OJOS DEL ETERNO. La letra: «los
> minion que use el grimorio será su propio ojo […] el ojo se mueve en la
> direccion en la que mira, o sea para moverse primero mira y luego se
> mueve […] se posicionan encima del jugador como una corona, y si el
> grimorio esta fuera comienzan a orbitar al grimorio en 2 circulos uno
> de 5 minion, otro de 6 y asi […] cada minion mira a su objetivo y usa
> el rayo con ruido de perlin […] es negro, con bordes morado y con
> bruma en la punta que toca al monstruo, cada minion debe tener un
> pequeño circulo magico igual que el de los agujeros negros […] cuando
> el minion dispara tiene un pequeño retroceso y luego se reincorpora,
> los ojos de cada minion son independientes de cada uno de ellos y del
> libro». El Orbe Cósmico muere entero y nacen LOS OJOS DEL LIBRO: el
> recorte del sprite exacto del usuario 44×37 ×3 estados + el iris de la
> .84 como capa; la máquina de la mirada (frena, fija el iris, luego se
> mueve); la CORONA sobre el dueño / las ÓRBITAS concéntricas al libro
> flotante (anillos de 5, 6, 7…, contrarrotando); el rayo Perlin NEGRO
> de bordes morados (la funda aditiva + el núcleo que oscurece) con la
> BRUMA en la punta; el golpe por GolpeMotor; el retroceso físico; el
> círculo mágico de los agujeros negros (SigiloErosionado); el parpadeo
> independiente por ojo, sin leer el hambre del libro. .tmod 2.584.158 B
> md5 e7cd269982ffbd1dfa5b8b77b325dbe1, 387 entradas, oráculo 0/0,
> build real 0/0, headless 0 excepciones.)'''

SNAP_ITEM = '''1. **✅ v6.50.105 PUBLICADA — LOS OJOS DEL ETERNO** — la letra: «los
   minion que use el grimorio será su propio ojo […] el ojo se mueve en
   la direccion en la que mira, o sea para moverse primero mira y luego
   se mueve […] estos minion se posicionan encima del jugador como una
   corona, y si el grimorio esta fuera comienzan a orbitar al grimorio
   en 2 circulos uno de 5 minion, otro de 6 y asi aumentando los
   circulos orbitales a mas minion pueda lanzarce […] cada minion mira
   a su objetivo y usa el rayo con ruido de perlin ademas de que este
   rayo usado por los minion es negro, con bordes morado y con bruma en
   la punta que toca al monstruo, cada minion debe tener un pequeño
   circulo magico igual que el de los agujeros negros […] cuando el
   minion dispara tiene un pequeño retroceso y luego se reincorpora,
   los ojos de cada minion son independientes de cada uno de ellos y
   del libro y no se ven afectados por la animacion de hambre del
   grimorio». (1) EL SPRITE: el recorte del ojo del sprite EXACTO del
   usuario (la almendra + el armazón lila + el párpado inferior dorado),
   44×37 ×3 estados (abierto/medio/cerrado) por LANCZOS; el iris de la
   .84 como CAPA (escala 0.52 — a escala proporcional el iris taparía la
   vista entera); el parpadeo con la convención de fases del libro
   (fase>7||fase<4 = medio). (2) LA MÁQUINA DE LA MIRADA: el iris fija
   el destino y FRENA (6 ticks) antes de moverse; el ángulo barre 0,22
   rad/tick; mirada ±7,5/±3,5 px. (3) LA FORMACIÓN: la CORONA (arco
   200°→340°) y con el espíritu fuera ÓRBITAS AL LIBRO (anillo k = 5+k
   ojos, radio 58+42k, contrarrotando). (4) EL RAYO: el canal Perlin
   VERBATIM con la funda morada aditiva (RayoStrip, violeta vivo, ancho
   9,5) + EL NÚCLEO NEGRO en AlphaBlend encima (la cadena que oscurece)
   + LOS GORROS + LA BRUMA en la punta que toca al monstruo; el golpe
   por GOLPEMOTOR, cadencia 110−nivel/3; LA PATADA del retroceso 3,0
   px/tick y la formación lo reincorpora. (5) EL CÍRCULO MÁGICO:
   OrbitaLib.SigiloErosionado (el de los agujeros negros, dorado-ámbar)
   alrededor de cada ojo, respirando a 16 px en la carga. (6) EL
   PARPADEO INDEPENDIENTE: hash propio por whoAmI (6–18 s), sin leer
   NADA del hambre; al apuntar el párpado se corta (la mirada del
   cazador). VERIFICACIÓN: audit_texturas 0 · oráculo 0/0 · build real
   0/0 · VLM ×3 (sprites limpios; el rayo calibrado en 3 rondas — SHIP
   por juicio de la casa) · audit_v650105 TODO OK — .tmod 2.584.158 B
   md5 e7cd269982ffbd1dfa5b8b77b325dbe1, 387 entradas (386−2 Orbe+3
   Ojos), EOF exacto, paquete==árbol ×3, rawimg píxel-idénticas (12 B
   de cabecera + RGBA), símbolos OjoEternoMinion/OjoEternoBuff/
   DrawElSello/CanalPerlin vivos + CosmicOrb* AUSENTES, las constantes
   de la letra en el IL, regresión .102/.103/.104 completa · headless
   «Sandboxing v6.50.105 → Adding Recipes → Choose World» 0 excepciones.
   CHECKLIST en juego: (a) clic der invoca OJOS (no orbe), (b) la corona
   sobre tu cabeza, (c) el parpadeo de cada ojo es suyo, (d) con el
   espíritu fuera: anillos concéntricos al libro (5/6/7…),
   contrarrotando, (e) cada ojo mira al objetivo y frena antes de
   moverse, (f) el rayo negro de bordes morados con la bruma en la
   punta, (g) el retroceso al disparar y la reincorporación, (h) el
   círculo mágico de runas doradas alrededor de cada ojo.
   PUBLICACIÓN: commit 1853196 + tag v6.50.105 + push (c669ae4..1853196)
   · release **409768350** (make_latest) + asset AethonMod.tmod
   **631162660** 2.584.158 B (md5
   e7cd269982ffbd1dfa5b8b77b325dbe1), CDN VERIFICADO BYTE A BYTE,
   /releases/latest = v6.50.105, respaldo /home/sync/AethonMod-v6.50.105.tmod.
'''

HISTORIAL_ROW = '''| **v6.50.105** | ✅ Build-verificada (audit_texturas 0, oráculo 0/0, build real 0/0 con ruta absoluta canónica, .tmod 2.584.158 B md5 e7cd269982ffbd1dfa5b8b77b325dbe1, 387 entradas (386 − 2 rawimg del Orbe + 3 de los Ojos), EOF exacto, paquete==árbol ×3 idiomas, rawimg píxel-idénticas al árbol (12 B de cabecera (1,w,h) + RGBA — comparadas contra el PNG DECODIFICADO, no los bytes del archivo), símbolos OjoEternoMinion/OjoEternoBuff/DrawElSello/DrawElRayo/CanalPerlin/SlotDeFormacion vivos + CosmicOrbMinion/CosmicOrbBuff/CosmicOrbBolt AUSENTES del heap, las constantes de la letra en el IL (float32: 0.52 el iris · 7.5/3.5 la mirada · 0.22 el giro · 3.0 la patada · 3.49/5.93 la corona · 58/42 las órbitas · 2.55 el aro + la regresión 1/216000 y 0.10), regresión .102/.103/.104 completa, headless «Sandboxing v6.50.105 → Adding Recipes → Choose World» 0 excepciones), ✔ PUBLICADA (release 409768350 + asset AethonMod.tmod 631162660 2.584.158 B, CDN verificado byte a byte md5 e7cd2699…, /releases/latest = v6.50.105, respaldo /home/sync/AethonMod-v6.50.105.tmod), ⏳ en juego | LA LETRA (2026-10-11): «también debemos cambiar los minions, los minion que use el grimorio será su propio ojo […] es tiempo de animar al minion, yo te daré los sprite del ojo usando código es lo mejor para que pudieses editarlo en caso de que convenga, ya sabes como animar los ojos, ahora ten en cuenta que el ojo se mueve en la direccion en la que mira, o sea para moverse primero mira y luego se mueve, algo mas, estos minion se posicionan encima del jugador como una corona, y si el grimorio esta fuera comienzan a orbitar al grimorio en 2 circulos uno de 5 minion, otro de 6 y asi aumentando los circulos orbitales a mas minion pueda lanzarce […] cada minion mira a su objetivo y usa el rayo con ruido de perlin ademas de que este rayo usado por los minion es negro, con bordes morado y con bruma en la punta que toca al monstruo, cada minion debe tener un pequeño circulo magico igual que el de los agujeros negros […] cuando el minion dispara tiene un pequeño retroceso y luego se reincorpora, los ojos de cada minion son independientes de cada uno de ellos y del libro y no se ven afectados por la animacion de hambre del grimorio». (1) EL ORBE CÓSMICO muere entero (minion + bolt + buff + 2 rawimg) — el Eterno invoca LOS OJOS DEL LIBRO: OjoEternoMinion + OjoEternoBuff (hjson ×3: Ojos del Eterno / El Ojo del Eterno / Eyes of the Eternal + el tooltip del ítem). (2) EL SPRITE: gen_ojo_eterno_v650105.py — el recorte (226,344)-(524,596) del sprite EXACTO del usuario (la almendra blanca + el armazón lila + el pármado inferior dorado), 44×37 ×3 estados por LANCZOS; el iris de la .84 como CAPA a escala 0.52 (el arte trae el iris sobredimensionado respecto de la esclera: a escala proporcional el disco taparía la vista entera y la mirada no leería); el parpadeo con la convención de fases del libro (fase>7||fase<4 = medio, la .86). (3) LA MÁQUINA DE LA MIRADA: MIRANDO (6 ticks frenando, el iris fija el destino) → MOVIENDO; el ángulo de la mirada (localAI[2]) barre 0,22 rad/tick hacia el foco; mirada ±7,5 px horizontal / ±3,5 px vertical (la geometría de la almendra), alcance por distancia (clamp dist/90). (4) LA FORMACIÓN: la CORONA sobre la cabeza del dueño (arco 200°→340°, radio 52+2,2·n tope 86, vaivén por ojo) y con el ESPÍRITU FUERA (GrimorioHambrientoNervioso.PosicionDelLibro cacheado por frame para la banda) ÓRBITAS CONCÉNTRICAS AL LIBRO: el anillo k lleva 5+k ojos (5, 6, 7… — la letra), radio 58+42·k, contrarrotando (par +0,34 / impar −0,27 rad/s); el reparto por Projectile.minionPos (el orden de invocación). (5) EL RAYO: CanalPerlin — el tronco del PerlinBolt VERBATIM (meandro fBm 3 octavas 2,8 ciclos + jitter 2 octavas 10 ciclos + el arco congelado + la envolvente exp(−5000·(t−½)^10) + Chaikin) dibujado en DOS capas: la FUNDA MORADA aditiva (RayoStrip.Filamento, violeta vivo (190,110,255), ancho 9,5) y encima EL NÚCLEO NEGRO en AlphaBlend (cadena de SoftGlow (8,3,14,215) 5,5→3 px al paso 2,5 — el patrón del horizonte de sucesos del agujero negro) + StormLib.EndCap en ambos electrodos + SombrasLib.Bruma (humo negro + aliento violeta) en la punta que toca al monstruo; el golpe por GOLPEMOTOR (el cauce del motor: crítica con las stats del dueño, varianza ±15%, TODOS los on-hit, el crédito de la kill — el guard «sólo el dueño resuelve»), daño ×WeaponScaling.MinionContactDamageMult(nivel), cadencia Math.Clamp(110−nivel/3, 55, 110); LA PATADA: velocity = −dir·3,0 al disparar y la formación lo reincorpora (freno 0,86/tick durante el fuego). (6) EL CÍRCULO MÁGICO: OrbitaLib.SigiloErosionado — EL MISMO de los agujeros negros (alfabeto RunasAbismo, dorado-ámbar (240,124,65)/(255,205,140) MEDIDO del Umbral, el aro roto con huecos por hash, 8 runas, órbita 0,06), r 13 (anillo a 2,55·13≈33 px) que RESPIRA a 16 en la carga del disparo (MathHelper.Lerp por el progreso del AIM); la presencia violácea detrás del ojo (SoftGlow (96,48,150) pulsando). (7) EL PARPADEO INDEPENDIENTE: localAI[0/1] — reloj propio por ojo (hash de whoAmI: 360+720·Hash01, 6–18 s, cero RNG), SIN leer NADA del hambre del libro (la letra: «no se ven afectados por la animacion de hambre»); al apuntar el párpado se corta (localAI[1]=1) y la mirada queda FIJA en blanco; el iris dibujado a 0,52 con la luz del mundo (Lighting.GetColor). MP: la IA corre en el dueño (tML) y el draw re-deriva el objetivo (el más cercano válido — la misma lista en todos los clientes); el parpadeo es visual-local (la deuda documentada de la casa). LECCIONES: (a) el rawimg del .tmod es 12 B de cabecera (1, w, h) + RGBA puro — se compara contra el PNG del árbol DECODIFICADO con PIL, no contra los bytes del archivo (la .100 midió «3.612 B = 30×30×4+12»: la fórmula, no el png); (b) el .cs nunca es entrada del .tmod — el oráculo de presencia de una clase es el heap #Strings de la DLL, el de una textura es la rawimg; (c) el VLM con mocks de composite aditivo OSCILA (r1 «sin borde morado» con negro 11 px → r2 «demasiado tenue» con 9,5+violeta vivo → r3 el fondo estrés del mock sigue sin resolverlo) — tras calibrar con la FÍSICA del render (la vena de RayoStrip es ~2 px EN EL EJE: la vena brillante vive BAJO el negro; el borde morado visible vive en la columna 0→0,42 de la FUNDA), SHIP por juicio de la casa y el checklist del usuario es el juez final; (d) el hjson trae el texto del tooltip del ÍTEM en la sección Items (es-MX línea 43) — la cirugía de nombres/DisplayName no basta: grep global del nombre viejo antes de cerrar (la audit lo cazó: «sobran: Orbe Cósmico») |
'''


def main():
    # === CHANGES.md: la entrada nueva al tope ===
    p = 'CHANGES.md'
    data = open(p, encoding='utf-8').read()
    ancla = '## Commit v6.50.104 — LA DIETA POR RAREZA + EL ICONO VIVO'
    assert data.count(ancla) == 1
    data = data.replace(ancla, CHANGES + ancla, 1)
    open(p, 'w', encoding='utf-8', newline='').write(data)
    print('CHANGES.md: entrada .105 al tope')

    # === STABLE-SNAPSHOT.md ===
    p = 'STABLE-SNAPSHOT.md'
    lines = open(p, encoding='utf-8').read().split('\n')
    # 1) el header (líneas 1..20 aprox: desde '# AethonMod' hasta el párrafo
    #    «...headless 0 excepciones.)» inclusive)
    i0 = next(i for i, l in enumerate(lines) if l.startswith('# AethonMod — ESTADO ACTUAL'))
    i1 = next(i for i, l in enumerate(lines) if l.startswith('- **Tag**')) - 1
    assert 'v6.50.104' in lines[i0]
    lines[i0:i1 + 1] = SNAP_HEADER.split('\n')
    # 2) PRÓXIMOS PASOS: insertar .105 como item 1, renumerar, podar .101
    j = next(i for i, l in enumerate(lines)
             if l.startswith('1. **✅ v6.50.104 PUBLICADA'))
    k104 = next(i for i, l in enumerate(lines)
                if l.startswith('2. **✅ v6.50.103 PUBLICADA'))
    k103 = next(i for i, l in enumerate(lines)
                if l.startswith('3. **✅ v6.50.102 PUBLICADA'))
    k102 = next(i for i, l in enumerate(lines)
                if l.startswith('4. **✅ v6.50.101 PUBLICADA'))
    k101_end = next(i for i, l in enumerate(lines)
                    if l.startswith('1. **✅ v6.50.100 PUBLICADA'))
    # PODAR la .101 (k102..k101_end-1) — doc-rot de la lista
    del lines[k102:k101_end]
    # renumerar (después del del, relocalizar)
    j = next(i for i, l in enumerate(lines)
             if l.startswith('1. **✅ v6.50.104 PUBLICADA'))
    lines[j] = lines[j].replace('1. **✅ v6.50.104', '2. **✅ v6.50.104', 1)
    j2 = next(i for i, l in enumerate(lines)
              if l.startswith('2. **✅ v6.50.103 PUBLICADA'))
    lines[j2] = lines[j2].replace('2. **✅ v6.50.103', '3. **✅ v6.50.103', 1)
    j3 = next(i for i, l in enumerate(lines)
              if l.startswith('3. **✅ v6.50.102 PUBLICADA'))
    lines[j3] = lines[j3].replace('3. **✅ v6.50.102', '4. **✅ v6.50.102', 1)
    # insertar la .105 al tope de la lista
    lines.insert(j, SNAP_ITEM.rstrip('\n'))
    # 3) HISTORIAL: la fila .105 antes de la .104
    h = next(i for i, l in enumerate(lines) if l.startswith('| **v6.50.104**'))
    lines.insert(h, HISTORIAL_ROW.rstrip('\n'))
    open(p, 'w', encoding='utf-8', newline='').write('\n'.join(lines))
    print('STABLE-SNAPSHOT.md: header .105 + item 1 + renumeración (.104→2, .103→3, .102→4, .101 PODADA) + fila HISTORIAL')

    # === README.md ===
    p = 'README.md'
    data = open(p, encoding='utf-8').read()
    assert 'Versión actual:** 6.50.104' in data
    data = data.replace('Versión actual:** 6.50.104', 'Versión actual:** 6.50.105', 1)
    open(p, 'w', encoding='utf-8', newline='').write(data)
    print('README.md: 6.50.104 → 6.50.105')


if __name__ == '__main__':
    main()

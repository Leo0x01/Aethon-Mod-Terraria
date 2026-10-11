#!/usr/bin/env python3
"""v6.50.106 — la cirugía de docs: CHANGES + STABLE-SNAPSHOT + README."""


CHANGES = '''## Commit v6.50.106 — EL OJO DEL USUARIO, SIN RECORTE

**La letra**: «no debes hacer recorte ya que arruinaste el recorte que yo
mismo hice, solo debes acomodar el iris para que pueda realmente funcionar,
los rayos que lanzan los ojos son demasiado gruesos y no hacen daño, ademas
la animacion de la corona no existen, se mueven erraticamente de arriba
hacia abajo, ademas la animacion que necesita de movimienot es la misma que
este invocador Ruinous_Staff investiga mucho como funciona el Ruinous_Staff
en las versiones mas actuales de Terraria» + «el icono del grimorio en el
evento de oleada no tiene animacion en el ojo» + «el proyecril que debe
usar el minion debe ser el mismo que Cetro de Trueno de Perlin pero teñido
de negro en el centro y morado en los bordes con bruma en la punta» +
«te volvere a dar el codigo del minion por si lo perdiste al recortarlo,
recuerda no debes recortar el sprite que te di ya que lo recorte perfecto
como debe ser».

**EL CAMBIO**: tres cirugías sobre los OJOS DEL ETERNO de la .105:

- **EL SPRITE** (tools/gen_ojo_eterno_v650106.py): LOS 3 ARCHIVOS DEL
  USUARIO YA ERAN SU RECORTE PERFECTO (bbox opaco (121,199)-(632,698) =
  512×500: el ojo con su armazón dorado, sus 8 remaches y sus chispas
  moradas — VLM: sin ninguna parte del libro) y la .105 le hizo OTRO
  recorte encima ((226,344)-(524,596): la almendra sin el aro exterior
  ni los remaches) moliéndolo a 44×37. Ahora CERO recorte: el contenido
  opaco del archivo ES el sprite (56×54, 9,1×) y el IRIS de la .84 va
  ACOMODADO en el centro de la ALMENDRA medida del arte (fracción
  (0.513, 0.543)·(56,54), escala 0.41 = disco ~11 px, viaje ±7,5 X ·
  ±2,5 Y). VLM: la hoja ×4 lee (el aro, los remaches, la mirada en las
  4 direcciones, el parpadeo medio/cerrado, sin clipping).
- **EL MOVIMIENTO** (la wiki del Ruinous Staff 1.4.5.7 — AI_206_
  ForbiddenMinion: planeo continuo): MUERE la máquina MIRANDO/MOVIENDO
  de la .105 (frenaba en seco cerca del hueco, aceleraba al salir del
  umbral y REBOTABA arriba-abajo — el «se mueven erráticos»); NACE UNA
  LEY ÚNICA: lejos (>90 px) inercia con arranque suave (velocidad
  deseada capped por dist×0,22), cerca amortiguación CRÍTICA (velocidad
  ∝ al hueco restante — el ojo SIGUE al hueco móvil sin sobrepasarlo
  NUNCA). La corona VIVE: el arco se BALANCEA (±11° a 0,30 rad/s), el
  radio RESPIRA (±3 px) y cada ojo lleva su vaivén propio (5,7 s — la
  .105 casi no se movía: 0,09 rad/s = 70 s de período). Órbitas
  recalibradas al ojo grande (66+44·k, contrarrotación ±0,32/−0,26);
  cadencia 110−nivel/3 ≈ 1,83 s (LA DEL FORBIDDEN RUIN).
- **EL RAYO** (la letra: el mismo del CETRO DEL TRUENO PERLIN): el
  minion YA NO dibuja su rayo — LO DISPARA: TruenoPerlinProjectile en
  MODO OJO (ai[0]=1, ai[1]=el ojo, ai[2]=la presa). El ancla nace
  PEGADA al monstruo y LO SIGUE mientras vive (la bruma en la punta que
  toca a la presa), el arco cuelga del OJO (no del bastón), el golpe es
  EL DEL CETRO (Strike radio 100 + cadena ×2 al 60% + Electrified, por
  GOLPEMOTOR — el daño de verdad) con DamageType Summon. Teñido: la
  funda morada aditiva AL GROSOR DEL CETRO (7,5) y encima el NÚCLEO
  NEGRO en AlphaBlend — LA LECCIÓN DE LA .105: las cápsulas SoftGlow a
  escala 5,5 = ¡352 px cada una! (SoftGlow es 64×64: la escala es
  ABSOLUTA) — el rayo leía como NEBLINA GORDA («demasiado gruesos»);
  ahora cápsulas de 19 px (escala 0,30). El zap oscuro (−0,45), las
  chispas de SOMBRA violeta y la luz violácea (0,50/0,32/0,90).
- **EL ICONO DE LA OLEADA** (GrimorioFuriaSistema): el ojo del libro de
  la cajita SIGUE AL CURSOR (el modelo del libro en mano: SnapDireccion
  ±(3,5·2,8) texels, lerp 0,22) + el PARPADEO determinista (fase 11→0,
  la convención de la .86, período 4-8 s por hash) con los párpados
  _Medio/_Cerrado de la familia + en el SOBRESALTO la mirada ESCAPA al
  lado del tirón (lerp 0,5). IRIS_ESC protected→internal (el icono usa
  la MISMA escala del iris del libro).
- **LA MIRADA DEL CAZADOR corregida**: la .105 forzaba fase=1 al apuntar
  = «medio» (el ojo entornado — el bug de fase); ahora fase→0: el ojo
  queda ABIERTO clavado en su presa.

**VERIFICACIÓN**: audit_texturas 0 · oráculo 0/0 (--no-incremental ×2) ·
build real 0/0 · LA NORMALIZACIÓN DE tML (el empaquetador pone a CERO el
RGB de los píxeles alpha=0 — el LANCZOS deja ringing 1-2: el gen
normaliza el árbol igual o el audit ve DIFF en ~19 px invisibles) ·
audit_v650106 TODO OK: .tmod 2.594.318 B md5 894cb4c910ca7449681e8b9a422165a4,
387 entradas, EOF exacto, paquete==árbol ×3, rawimg 56×54 píxel-idénticas,
DrawArcoOjo/CanalOjo/BocaDelOjo/PresaDelAncla/PasoOjoIcono/SnapDireccionIcono
vivos + DrawElRayo/CanalPerlin (mudados al cetro) y CosmicOrb* MUERTOS, las
constantes de la letra en el IL, regresión .102-.105 completa · headless
«Sandboxing v6.50.106 → Adding Recipes → Choose World» 0 excepciones.

---

'''

SNAP_HEADER = '''# AethonMod — ESTADO ACTUAL (v6.50.106)

> **Este documento = "¿por dónde nos quedamos?"** — se actualiza en cada entrega.
> Última actualización: v6.50.106 (EL OJO DEL USUARIO, SIN RECORTE. La
> letra: «no debes hacer recorte ya que arruinaste el recorte que yo
> mismo hice, solo debes acomodar el iris para que pueda realmente
> funcionar» + «los rayos que lanzan los ojos son demasiado gruesos y no
> hacen daño» + «la animacion de la corona no existen, se mueven
> erraticamente de arriba hacia abajo, ademas la animacion que necesita
> de movimienot es la misma que este invocador Ruinous_Staff» + «el
> icono del grimorio en el evento de oleada no tiene animacion en el
> ojo» + «el proyecril que debe usar el minion debe ser el mismo que
> Cetro de Trueno de Perlin pero teñido de negro en el centro y morado
> en los bordes con bruma en la punta». Tres cirugías: (1) EL SPRITE —
> los 3 archivos del usuario YA ERAN su recorte perfecto (512×500: el
> ojo con su armazón dorado, sus 8 remaches y sus chispas moradas) y la
> .105 le hizo OTRO recorte encima moliéndolo a 44×37; ahora CERO
> recorte (56×54) y el iris de la .84 ACOMODADO en el centro de la
> almendra medida (escala 0.41, viaje ±7,5/±2,5). (2) EL MOVIMIENTO —
> la ley del Ruinous Staff (AI_206: planeo continuo): muere la máquina
> mirando/moviendo de la .105 (el rebote errático) y nace UNA ley (lejos
> inercia suave, cerca amortiguación crítica); la corona VIVE (balanceo
> ±11°, radio que respira, vaivén propio 5,7 s); órbitas 66+44k. (3) EL
> RAYO — el proyectil DEL CETRO DEL TRUENO PERLIN en modo ojo: el ancla
> nace pegada al monstruo y lo sigue, el golpe es el del cetro (radio
> 100 + cadena ×2 + Electrified — el daño de verdad), el núcleo negro
> de cápsulas de 19 px sobre la funda de 7,5 DEL CETRO (la .105 ponía
> las cápsulas a 352 px — SoftGlow 64×64, escala ABSOLUTA: el rayo leía
> como neblina gorda). Y EL ICONO DE LA OLEADA: el ojo de la cajita
> sigue al cursor y parpadea. .tmod 2.594.318 B
> md5 894cb4c910ca7449681e8b9a422165a4, 387 entradas, oráculo 0/0,
> build real 0/0, headless 0 excepciones.)'''

SNAP_ITEM = '''1. **✅ v6.50.106 PUBLICADA — EL OJO DEL USUARIO, SIN RECORTE** — la
   letra: «no debes hacer recorte ya que arruinaste el recorte que yo
   mismo hice, solo debes acomodar el iris para que pueda realmente
   funcionar, los rayos que lanzan los ojos son demasiado gruesos y no
   hacen daño, ademas la animacion de la corona no existen, se mueven
   erraticamente de arriba hacia abajo, ademas la animacion que necesita
   de movimienot es la misma que este invocador Ruinous_Staff […] el
   icono del grimorio en el evento de oleada no tiene animacion en el
   ojo […] el proyecril que debe usar el minion debe ser el mismo que
   Cetro de Trueno de Perlin pero teñido de negro en el centro y morado
   en los bordes con bruma en la punta». (1) EL SPRITE: los 3 archivos
   del usuario YA ERAN su recorte perfecto (bbox opaco (121,199)-
   (632,698) = 512×500 — el ojo con su armazón dorado, sus 8 remaches y
   sus chispas moradas) y la .105 le hizo OTRO recorte encima
   ((226,344)-(524,596): la almendra sin el aro ni los remaches)
   moliéndolo a 44×37 por LANCZOS; ahora CERO recorte: el contenido
   opaco del archivo ES el sprite (56×54, 9,1×) y el iris de la .84
   ACOMODADO en el centro de la almendra medida del arte (fracción
   (0.513, 0.543), escala 0.41 = disco ~11 px, viaje ±7,5 X · ±2,5 Y);
   VLM: la hoja ×4 lee. (2) EL MOVIMIENTO (la wiki del Ruinous Staff
   1.4.5.7 — AI_206_ForbiddenMinion): muere la máquina MIRANDO/
   MOVIENDO (el rebote errático arriba-abajo); UNA ley de planeo:
   lejos (>90 px) inercia con arranque suave (capped por dist×0,22),
   cerca amortiguación CRÍTICA (velocidad ∝ al hueco — sigue al hueco
   móvil sin sobrepasarse); la corona se BALANCEA (±11° a 0,30 rad/s),
   el radio RESPIRA y cada ojo lleva su vaivén (5,7 s); órbitas 66+44k
   contrarrotando; cadencia ≈1,83 s (la del Forbidden Ruin). (3) EL
   RAYO: el minion YA NO dibuja — DISPARA TruenoPerlinProjectile en
   MODO OJO (ai[0]=1, ai[1]=el ojo, ai[2]=la presa): el ancla nace
   PEGADA al monstruo y lo SIGUE (la bruma en la punta), el arco cuelga
   del OJO, el golpe es EL DEL CETRO (radio 100 + cadena ×2 al 60% +
   Electrified, por GOLPEMOTOR — el daño de verdad), DamageType Summon;
   teñido: funda morada AL GROSOR DEL CETRO (7,5) + NÚCLEO NEGRO de
   cápsulas de 19 px (la .105 las ponía a 352 px — SoftGlow 64×64, la
   escala es ABSOLUTA: el rayo leía como neblina gorda). (4) EL ICONO
   DE LA OLEADA: el ojo de la cajita SIGUE AL CURSOR (SnapDireccion
   ±(3,5·2,8), lerp 0,22) + parpadeo determinista (fase 11→0, 4-8 s
   por hash) + la mirada ESCAPA en el sobresalto; IRIS_ESC internal.
   VERIFICACIÓN: audit_texturas 0 · oráculo 0/0 (--no-incremental ×2) ·
   build real 0/0 · LA NORMALIZACIÓN DE tML (el empaquetador pone a
   CERO el RGB de los alpha=0 — el LANCZOS deja ringing: el gen
   normaliza el árbol) · audit_v650106 TODO OK — .tmod 2.594.318 B md5
   894cb4c910ca7449681e8b9a422165a4, 387 entradas, EOF exacto,
   paquete==árbol ×3, rawimg 56×54 píxel-idénticas, DrawArcoOjo/
   CanalOjo/BocaDelOjo/PresaDelAncla/PasoOjoIcono/SnapDireccionIcono
   vivos + DrawElRayo/CanalPerlin (mudados al cetro) y CosmicOrb*
   AUSENTES, las constantes de la letra en el IL, regresión .102-.105
   completa · headless «Sandboxing v6.50.106 → Adding Recipes → Choose
   World» 0 excepciones. CHECKLIST en juego: (a) el sprite del ojo es
   TU recorte (armazón dorado + 8 remaches + chispas moradas), (b) la
   corona se mueve suave, sin rebotes; cada ojo con su vaivén, (c) el
   rayo fino como el del cetro: negro al centro, morado en los bordes,
   bruma en la punta, (d) EL RAYO HACE DAÑO (radio + cadena + Electrified),
   (e) el retroceso al disparar y la reincorporación, (f) el ojo del
   icono de la oleada sigue tu cursor y parpadea.
   PUBLICACIÓN: commit 7b03f3c + tag v6.50.106 + push (0250365..7b03f3c)
   · release **409795447** (make_latest) + asset AethonMod.tmod
   **631271623** 2.594.318 B (md5
   894cb4c910ca7449681e8b9a422165a4), CDN VERIFICADO BYTE A BYTE,
   /releases/latest = v6.50.106, respaldo /home/sync/AethonMod-v6.50.106.tmod.
'''

HISTORIAL_ROW = '''| **v6.50.106** | ✅ Build-verificada (audit_texturas 0, oráculo 0/0 ×2 --no-incremental, build real 0/0 con ruta absoluta canónica, .tmod 2.594.318 B md5 894cb4c910ca7449681e8b9a422165a4, 387 entradas, EOF exacto, paquete==árbol ×3 idiomas, rawimg 56×54 píxel-idénticas al árbol (la NORMALIZACIÓN DE tML: el empaquetador pone a CERO el RGB de los píxeles alpha=0 — el LANCZOS deja ringing 1-2 y el gen del árbol debe normalizar igual), símbolos OjoEternoMinion/OjoEternoBuff/DrawElSello/DrawElOjo/SlotDeFormacion/Disparar/IRIS_REST + TruenoPerlinProjectile/DrawArcoOjo/CanalOjo/BocaDelOjo/PresaDelAncla/OjoHalo/OjoNegro + PasoOjoIcono/SnapDireccionIcono/_iconoDesp vivos + DrawElRayo/CanalPerlin (mudados de la .105 al cetro) y CosmicOrb* AUSENTES del heap, las constantes de la letra en el IL (float32: 0.41 el iris · 7.5/2.5 la mirada · 0.7/2.3 IRIS_REST · 0.15/0.35/0.25 la ley del planeo · 0.22 el tope de arranque · 0.20 el balanceo · 1.1 el vaivén · 66/44 las órbitas · 3.2 la patada · 16 el sello · 0.30 la cápsula negra · 0.55 el ancla pegada · 3.5/2.8 la mirada del icono + la regresión 1/216000 y 0.10), regresión .102/.103/.104/.105 completa, headless «Sandboxing v6.50.106 → Adding Recipes → Choose World» 0 excepciones), ✔ PUBLICADA (release 409795447 + asset AethonMod.tmod 631271623 2.594.318 B, CDN verificado byte a byte md5 894cb4c9…, /releases/latest = v6.50.106, respaldo /home/sync/AethonMod-v6.50.106.tmod), ⏳ en juego | LA LETRA (2026-10-11): «no debes hacer recorte ya que arruinaste el recorte que yo mismo hice, solo debes acomodar el iris para que pueda realmente funcionar, los rayos que lanzan los ojos son demasiado gruesos y no hacen daño, ademas la animacion de la corona no existen, se mueven erraticamente de arriba hacia abajo, ademas la animacion que necesita de movimienot es la misma que este invocador Ruinous_Staff investiga mucho como funciona el Ruinous_Staff en las versiones mas actuales de Terraria» + «el icono del grimorio en el evento de oleada no tiene animacion en el ojo» + «el projecril que debe usar el minion debe ser el mismo que Cetro de Trueno de Perlin pero teñido de negro en el centro y morado en los bordes con bruma en la punta» + «te volvere a dar el codigo del minion por si lo perdiste al recortarlo, recuerda no debes recortar el sprite que te di ya que lo recorte perfecto como debe ser». (1) EL DIAGNÓSTICO DEL RECORTE: los 3 archivos del usuario YA ERAN su recorte perfecto (bbox opaco (121,199)-(632,698) = 512×500: el ojo con su armazón dorado, sus 8 remaches y sus chispas moradas — VLM: sin ninguna parte del libro) y la .105 le hizo OTRO recorte encima ((226,344)-(524,596): la almendra sin el aro exterior ni los remaches) moliéndolo a 44×37. AHORA: CERO recorte — el contenido opaco del archivo ES el sprite (56×54) y el iris de la .84 va ACOMODADO en el centro de la ALMENDRA medida (fracción (0.513, 0.543)·(56,54), escala 0.41, viaje ±7,5/±2,5 — la geometría de la almendra 27,7×16,1). (2) EL MOVIMIENTO (la wiki del Ruinous Staff 1.4.5.7 — AI_206_ForbiddenMinion: «hovers above the player… tries to stay at a short fixed distance… continuously adjusts its position»): muere la máquina MIRANDO/MOVIENDO de la .105 (frenaba en seco cerca del hueco, aceleraba al salir del umbral y REBOTABA — el «se mueven erráticos»); UNA ley: lejos (>90 px) inercia con arranque suave (velocidad deseada capped por dist×0,22), cerca amortiguación CRÍTICA (velocidad ∝ al hueco restante — sigue al hueco móvil sin sobrepasarse); la corona se BALANCEA (±11° a 0,30 rad/s), el radio RESPIRA (±3 px) y cada ojo lleva su vaivén propio (5,7 s — la .105 casi no se movía: 0,09 rad/s = 70 s de período); órbitas recalibradas (66+44·k, contrarrotación ±0,32/−0,26); la cadencia 110−nivel/3 ≈ 1,83 s (LA DEL FORBIDDEN RUIN — wiki: 1,75–1,92 s). (3) EL RAYO: el minion YA NO dibuja su rayo — LO DISPARA: TruenoPerlinProjectile en MODO OJO (ai[0]=1, ai[1]=el ojo que disparó, ai[2]=la presa): el ancla nace PEGADA al monstruo y LO SIGUE mientras vive (la bruma en la punta que toca a la presa — la punta pegada), el arco cuelga del OJO (no del bastón), el golpe es EL DEL CETRO (Strike radio 100 + cadena ×2 al 60% + Electrified, por GOLPEMOTOR) con DamageType Summon; teñido: la funda morada aditiva AL GROSOR DEL CETRO (7,5 — RayoStrip.Filamento) y encima el NÚCLEO NEGRO en AlphaBlend con cápsulas de 19 px (escala SoftGlow 0,30 — LA LECCIÓN: la .105 las ponía a escala 5,5 = 352 px cada una (SoftGlow es 64×64: la escala es ABSOLUTA), el rayo leía como NEBLINA GORDA); el zap oscuro (Item12 −0,15 / Item122 −0,45), las chispas de SOMBRA violeta (DustID.Shadowflame) y la luz violácea (0,50/0,32/0,90); LA MIRADA DEL CAZADOR corregida (la .105 forzaba fase=1 = «medio» — el bug; ahora fase→0: ABIERTO clavado en la presa). (4) EL ICONO DE LA OLEADA (GrimorioFuriaSistema.DibujarIndicador): el ojo del libro de la cajita SIGUE AL CURSOR (el modelo del libro en mano: SnapDireccion ±(3,5·2,8) texels, lerp 0,22, el cursor encima = fijo a los ojos) + el PARPADEO determinista (fase 11→0, la convención de la .86, período 240+240·Hash01 por ciclo — 4-8 s) con los párpados GrimorioHambriento_Medio/_Cerrado de la familia + en el SOBRESALTO la mirada ESCAPA al lado del tirón (lerp 0,5); IRIS_ESC protected→internal (el icono usa la MISMA escala 0,2773 del iris del libro, pegado a la MISMA transform del icono: bob + giro + escala). LECCIONES: (a) el archivo del usuario YA ERA el recorte — el bbox opaco del archivo es SU criterio, no el mío: NUNCA más recortar sobre un recorte (la .105 confundió «el LIBRO ENTERO» con el recorte porque el canvas es 706×967: el CONTENIDO opaco es lo que cuenta); (b) el RGB de los píxeles alpha=0: tML lo pone a CERO al empaquetar — el LANCZOS deja ringing 1-2 ahí y el audit ve DIFF paquete≠árbol en ~19 px invisibles: normalizar el PNG del árbol igual (rgb=0 donde alpha=0); (c) la escala de las cápsulas SoftGlow es ABSOLUTA (textura 64×64: scale 5,5 = 352 px en pantalla) — los grosores se miden en PX DE PANTALLA, nunca en «unidades de textura» |
'''


def main():
    # === CHANGES.md: la entrada nueva al tope ===
    p = 'CHANGES.md'
    data = open(p, encoding='utf-8').read()
    ancla = '## Commit v6.50.105 — LOS OJOS DEL ETERNO'
    assert data.count(ancla) == 1
    data = data.replace(ancla, CHANGES + ancla, 1)
    open(p, 'w', encoding='utf-8', newline='').write(data)
    print('CHANGES.md: entrada .106 al tope')

    # === STABLE-SNAPSHOT.md ===
    p = 'STABLE-SNAPSHOT.md'
    lines = open(p, encoding='utf-8').read().split('\n')
    # 1) el header: desde '# AethonMod' hasta el párrafo «...headless 0 excepciones.)»
    i0 = next(i for i, l in enumerate(lines) if l.startswith('# AethonMod — ESTADO ACTUAL'))
    i1 = next(i for i, l in enumerate(lines) if l.startswith('- **Tag**')) - 1
    assert 'v6.50.105' in lines[i0]
    lines[i0:i1 + 1] = SNAP_HEADER.split('\n')
    # 2) PRÓXIMOS PASOS: insertar .106 como item 1, renumerar, podar .102
    j = next(i for i, l in enumerate(lines)
             if l.startswith('1. **✅ v6.50.105 PUBLICADA'))
    k105 = next(i for i, l in enumerate(lines)
                if l.startswith('2. **✅ v6.50.104 PUBLICADA'))
    k104 = next(i for i, l in enumerate(lines)
                if l.startswith('3. **✅ v6.50.103 PUBLICADA'))
    k103 = next(i for i, l in enumerate(lines)
                if l.startswith('4. **✅ v6.50.102 PUBLICADA'))
    k102_end = next(i for i, l in enumerate(lines)
                    if l.startswith('1. **✅ v6.50.100 PUBLICADA'))
    # PODAR la .102 (k103..k102_end-1) — doc-rot de la lista
    del lines[k103:k102_end]
    # renumerar (después del del, relocalizar)
    j = next(i for i, l in enumerate(lines)
             if l.startswith('1. **✅ v6.50.105 PUBLICADA'))
    lines[j] = lines[j].replace('1. **✅ v6.50.105', '2. **✅ v6.50.105', 1)
    j2 = next(i for i, l in enumerate(lines)
              if l.startswith('2. **✅ v6.50.104 PUBLICADA'))
    lines[j2] = lines[j2].replace('2. **✅ v6.50.104', '3. **✅ v6.50.104', 1)
    j3 = next(i for i, l in enumerate(lines)
              if l.startswith('3. **✅ v6.50.103 PUBLICADA'))
    lines[j3] = lines[j3].replace('3. **✅ v6.50.103', '4. **✅ v6.50.103', 1)
    # insertar la .106 al tope de la lista
    lines.insert(j, SNAP_ITEM.rstrip('\n'))
    # 3) HISTORIAL: la fila .106 antes de la .105
    h = next(i for i, l in enumerate(lines) if l.startswith('| **v6.50.105**'))
    lines.insert(h, HISTORIAL_ROW.rstrip('\n'))
    open(p, 'w', encoding='utf-8', newline='').write('\n'.join(lines))
    print('STABLE-SNAPSHOT.md: header .106 + item 1 + renumeración (.105→2, .104→3, .103→4, .102 PODADA) + fila HISTORIAL')

    # === README.md ===
    p = 'README.md'
    data = open(p, encoding='utf-8').read()
    assert 'Versión actual:** 6.50.105' in data
    data = data.replace('Versión actual:** 6.50.105', 'Versión actual:** 6.50.106', 1)
    open(p, 'w', encoding='utf-8', newline='').write(data)
    print('README.md: 6.50.105 → 6.50.106')


if __name__ == '__main__':
    main()

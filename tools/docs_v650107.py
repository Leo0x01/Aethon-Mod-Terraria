#!/usr/bin/env python3
"""v6.50.107 — la cirugía de docs: CHANGES + STABLE-SNAPSHOT + README."""


CHANGES = '''## Commit v6.50.107 — EL TAMAÑO DEL LIBRO, LA LUNA Y EL RITMO PROPIO

**La letra**: «los minion son demasiado grande, deben ser mas pequeño, el
tamaño debe ser el mismo que el del grimorio, ademas ellos atacan al mismo
tiempo, cada ojo debe ser individual y deben tener la libertad de atacar
cuando quieran y a la velocidad que deseen ya que la velocidad de ataque
aumenta en cuanto mas nivel tengan el grimorio, hasta un maximo de 50% mas
rapido a nivel 100 del grimorio subiendo a partir de nivel 1» + «el
proyectil que usan los minion es muy grueso, debe ser un poco mas fino» +
«la forma en la que deben moverse al rededor del libro es orbitar como una
luna a un planeta no solo dar vuelta en ciruclo» + «la caja de informacion
del grimorio eterno es demasiado grande con datos que no necesitan solo
debe decir: daño magico, daño de invocacion, datos y buff propios del
juego, un pequeño texto de lore donde explique un poco que es el libro sin
decir mucho, al menos 2 lineas, luego en progresion debe salir la barra de
hambre y la de exp».

**EL CAMBIO**: cinco cirugías sobre los OJOS DEL ETERNO y el tooltip:

- **EL TAMAÑO** (ESCALA 0.75): el espíritu viste el libro 36×49 a esc 1.15
  = 41×56 px; el ojo 56×54 × 0.75 = 42×41 en pantalla — LA LUNA CABE EN LA
  SOMBRA DEL PLANETA. Todo el layout escala con la MISMA constante: hitbox
  25×15, el sello r12→15 (antes 16→20), el aliento 2.5×2.1 (antes
  3.3×2.8), el iris y la mirada ×ESCALA; la corona se acerca (radio
  56+2.8n tope 88 — era 64+3.2n tope 98).
- **EL RITMO PROPIO**: MUERE la cadencia global de la .106 (110−nivel/3,
  piso 55); NACE la cadencia de ESTE ojo: 110 / (VelocidadNivel ×
  RitmoPropio) — VelocidadNivel = 1 + 0.5·min(nivel−1, 99)/99 (1.0× en
  nivel 1 subiendo LINEAL hasta 1.5× = +50% más rápido en nivel 100,
  CLAVADA ahí) y RitmoPropio = 0.85–1.15 por Hash01(whoAmI). EL DESFASE
  DE NACIMIENTO: el primer disparo de cada ojo cae entre el 35% y el
  100% de SU intervalo — la banda invocada de golpe JAMÁS dispara en
  bloque: cada ojo es INDIVIDUAL.
- **LA LUNA**: con el libro fuera MUERE el círculo (66+44k a velocidad
  constante); NACE la elipse con LA SEGUNDA LEY DE KEPLER — a = 62+42k,
  b = 0.8a (la órbita INCLINADA), precesión apsidal lenta (tilt = 0.9k +
  0.18·sin(0.07t)), dθ = ω·a²/r² (el área barrida por tick es CONSTANTE):
  el ojo ACELERA en el perigeo (cerca del libro) y FRENA en el apogeo
  (lejos) — ORBITAR, no dar vuelta en círculo. La fase de cada ojo vive
  en localAI[3] y SÓLO la avanza PasoOrbita (UNA vez por tick desde la
  AI — el draw la LEE, jamás la mueve).
- **EL RAYO FINO** (sólo el MODO OJO — el cetro propio conserva su
  grosor): funda 7.5→6.0, cápsulas negras 19→15 px (0.30→0.235), cadenas
  3.6→2.9, gorro 6.0→4.8, ancla 1.5/0.8→1.2/0.65, estallido 46→38,
  arcos de corona 2.8→2.3, bruma en la punta 24→20.
- **LA CAJA COMPACTA**: MUERE la vista completa de 25 líneas del tooltip
  del Eterno (los hitos con premios, las secciones DAÑO/RECURSOS/
  PROYECTIL/BONUS, el conmutador de modos); el hjson ×3 pierde el
  mega-tooltip (19 líneas) y gana el LORE de 2 líneas («Un libro viejo
  que devora lo que su portador mata. / Mantén sus páginas llenas... o
  será tu alma la que llene la siguiente.»); el C#: daño mágico (vanilla,
  sin tocar) + daño de invocación pegado al daño + PROGRESIÓN con la
  barra de EXP y la pareja del hambre REUBICADA (HambreBarra/
  HambreEstado al final, dentro de progresión).

**VERIFICACIÓN**: audit_texturas 0 · oráculo 0/0 · build real 0/0 ·
audit_v650107 TODO OK: .tmod 2.590.979 B md5 b6f789b6a2af363de8fb03e1b83f6f20,
387 entradas, EOF exacto, paquete==árbol ×3, rawimg 56×54 píxel-idénticas,
PasoOrbita/FaseOrbital/AnilloDe/CadenciaPersonal/VelocidadNivel/RitmoPropio/
ESCALA/CADENCIA_BASE vivos + get_Cadencia (la .106) MUERTO, los ldstr de la
vista completa MUERTOS en #US (UTF-16) y la pareja HambreBarra/HambreEstado
VIVA, las constantes de la letra en el IL, regresión .102–.106 completa ·
headless «Sandboxing v6.50.107 → Adding Recipes → Choose World» 0 excepciones.

---

'''

SNAP_HEADER = '''# AethonMod — ESTADO ACTUAL (v6.50.107)

> **Este documento = "¿por dónde nos quedamos?"** — se actualiza en cada entrega.
> Última actualización: v6.50.107 (EL TAMAÑO DEL LIBRO, LA LUNA Y EL RITMO
> PROPIO. La letra: «los minion son demasiado grande, deben ser mas
> pequeño, el tamaño debe ser el mismo que el del grimorio» + «ellos
> atacan al mismo tiempo, cada ojo debe ser individual y deben tener la
> libertad de atacar cuando quieran y a la velocidad que deseen ya que la
> velocidad de ataque aumenta en cuanto mas nivel tengan el grimorio,
> hasta un maximo de 50% mas rapido a nivel 100 del grimorio subiendo a
> partir de nivel 1» + «el proyectil que usan los minion es muy grueso,
> debe ser un poco mas fino» + «la forma en la que deben moverse al
> rededor del libro es orbitar como una luna a un planeta no solo dar
> vuelta en ciruclo» + «la caja de informacion del grimorio eterno es
> demasiado grande con datos que no necesitan solo debe decir: daño
> magico, daño de invocacion, datos y buff propios del juego, un pequeño
> texto de lore donde explique un poco que es el libro sin decir mucho,
> al menos 2 lineas, luego en progresion debe salir la barra de hambre y
> la de exp». Cinco cirugías: (1) EL TAMAÑO — ESCALA 0.75: el ojo 56×54
> viste 42×41 px (el libro del espíritu es 36×49×1.15 = 41×56): la luna
> cabe en la sombra del planeta; todo el layout escala (hitbox 25×15,
> sello r12→15, corona 56+2.8n tope 88). (2) EL RITMO PROPIO — 110 /
> (VelocidadNivel × RitmoPropio): nivel 1 = 1.0× subiendo LINEAL a 1.5×
> (+50%) en nivel 100; ritmo personal 0.85–1.15 por hash; el primer
> disparo cae entre el 35% y el 100% del intervalo (el desfase de
> nacimiento) — la banda JAMÁS dispara en bloque. (3) LA LUNA — elipse
> a=62+42k, b=0.8a, precesión apsidal lenta y LA SEGUNDA LEY DE KEPLER
> (dθ = ω·a²/r²): acelera en el perigeo, frena en el apogeo; la fase
> vive en localAI[3] y sólo avanza PasoOrbita (una vez por tick). (4) EL
> RAYO FINO — funda 6.0, cápsulas 15 px, cadenas 2.9, gorro 4.8, ancla
> 1.2/0.65, estallido 38, bruma 20 (el cetro propio conserva su grosor).
> (5) LA CAJA COMPACTA — muere la vista completa de 25 líneas; el hjson
> gana el LORE de 2 líneas y el C# sólo pone daño de invocación +
> PROGRESIÓN (EXP + hambre). .tmod 2.590.979 B
> md5 b6f789b6a2af363de8fb03e1b83f6f20, 387 entradas, oráculo 0/0,
> build real 0/0, headless 0 excepciones.)'''

SNAP_ITEM = '''1. **✅ v6.50.107 PUBLICADA — EL TAMAÑO DEL LIBRO, LA LUNA Y EL RITMO
   PROPIO** — la letra: «los minion son demasiado grande, deben ser mas
   pequeño, el tamaño debe ser el mismo que el del grimorio, ademas ellos
   atacan al mismo tiempo, cada ojo debe ser individual y deben tener la
   libertad de atacar cuando quieran y a la velocidad que deseen ya que la
   velocidad de ataque aumenta en cuanto mas nivel tengan el grimorio,
   hasta un maximo de 50% mas rapido a nivel 100 del grimorio subiendo a
   partir de nivel 1» + «el proyectil que usan los minion es muy grueso,
   debe ser un poco mas fino» + «la forma en la que deben moverse al
   rededor del libro es orbitar como una luna a un planeta no solo dar
   vuelta en ciruclo» + «la caja de informacion del grimorio eterno es
   demasiado grande con datos que no necesitan solo debe decir: daño
   magico, daño de invocacion, datos y buff propios del juego, un pequeño
   texto de lore donde explique un poco que es el libro sin decir mucho,
   al menos 2 lineas, luego en progresion debe salir la barra de hambre y
   la de exp». (1) EL TAMAÑO (ESCALA 0.75): el espíritu viste el libro
   36×49 a esc 1.15 = 41×56 px; el ojo 56×54 × 0.75 = 42×41 — la luna
   cabe en la sombra del planeta; todo el layout escala con la MISMA
   constante (hitbox 25×15, el sello r12→15, el aliento 2.5×2.1, el iris
   y la mirada ×ESCALA); la corona se acerca (56+2.8n tope 88). (2) EL
   RITMO PROPIO: 110 / (VelocidadNivel × RitmoPropio) — VelocidadNivel =
   1 + 0.5·min(nivel−1, 99)/99 (1.0× nivel 1 → 1.5× nivel 100, CLAVADA),
   RitmoPropio = 0.85–1.15 por Hash01(whoAmI, 577, 131); EL DESFASE DE
   NACIMIENTO: el primer disparo cae entre el 35% y el 100% de SU
   intervalo (Hash01(whoAmI, 811, 23)) — la banda JAMÁS dispara en
   bloque. (3) LA LUNA: elipse a = 62+42k, b = 0.8a, precesión apsidal
   lenta (0.9k + 0.18·sin(0.07t)) y LA SEGUNDA LEY DE KEPLER — dθ =
   ω·a²/r² (el área barrida por tick es CONSTANTE): el ojo ACELERA en el
   perigeo y FRENA en el apogeo; la fase vive en localAI[3] (natal 0.37)
   y SÓLO la avanza PasoOrbita (una vez por tick desde la AI — el draw la
   LEE, jamás la mueve). (4) EL RAYO FINO (sólo el MODO OJO — el cetro
   propio conserva su grosor): funda 7.5→6.0, cápsulas negras 19→15 px
   (0.30→0.235), cadenas 3.6→2.9, gorro 6.0→4.8, ancla 1.5/0.8→1.2/0.65,
   estallido 46→38, arcos 2.8→2.3, bruma 24→20. (5) LA CAJA COMPACTA:
   muere la vista completa de 25 líneas (hitos con premios, secciones
   DAÑO/RECURSOS/PROYECTIL/BONUS, el conmutador); el hjson ×3 pierde el
   mega-tooltip y gana el LORE de 2 líneas («Un libro viejo que devora lo
   que su portador mata. / Mantén sus páginas llenas... o será tu alma la
   que llene la siguiente.»); el C#: daño mágico (vanilla) + daño de
   invocación + PROGRESIÓN (barra de EXP + la pareja del hambre
   REUBICADA). VERIFICACIÓN: audit_texturas 0 · oráculo 0/0 · build real
   0/0 · audit_v650107 TODO OK — .tmod 2.590.979 B md5
   b6f789b6a2af363de8fb03e1b83f6f20, 387 entradas, EOF exacto,
   paquete==árbol ×3, rawimg 56×54 píxel-idénticas, PasoOrbita/
   FaseOrbital/AnilloDe/CadenciaPersonal/VelocidadNivel/RitmoPropio/
   ESCALA/CADENCIA_BASE vivos + get_Cadencia (la .106) MUERTO, los ldstr
   de la vista completa MUERTOS en #US (UTF-16) + HambreBarra/HambreEstado
   VIVOS, las constantes de la letra en el IL, regresión .102–.106
   completa · headless «Sandboxing v6.50.107 → Adding Recipes → Choose
   World» 0 excepciones. CHECKLIST en juego: (a) los ojos del TAMAÑO DEL
   LIBRO, (b) cada ojo dispara POR SU CUENTA — nunca en bloque, (c) a
   nivel 100 los ojos disparan un 50% más rápido que a nivel 1, (d) con
   el libro fuera: elipse kepleriana — ACELERAN al acercarse al libro y
   frenan al alejarse, (e) el rayo un poco más fino, (f) el tooltip del
   Eterno compacto: daño mágico + daño de invocación + lore 2 líneas +
   PROGRESIÓN (EXP + hambre).
   PUBLICACIÓN: commit 5f55f27 + tag v6.50.107 + push (71b01f0..5f55f27)
   · release **409818581** (make_latest) + asset AethonMod.tmod
   **631370474** 2.590.979 B (md5
   b6f789b6a2af363de8fb03e1b83f6f20), CDN VERIFICADO BYTE A BYTE,
   /releases/latest = v6.50.107, respaldo /home/sync/AethonMod-v6.50.107.tmod.
'''

HISTORIAL_ROW = '''| **v6.50.107** | ✅ Build-verificada (audit_texturas 0, oráculo 0/0, build real 0/0, .tmod 2.590.979 B md5 b6f789b6a2af363de8fb03e1b83f6f20, 387 entradas, EOF exacto, paquete==árbol ×3 idiomas, rawimg 56×54 píxel-idénticas, símbolos PasoOrbita/FaseOrbital/AnilloDe/CadenciaPersonal/VelocidadNivel/RitmoPropio/ESCALA/CADENCIA_BASE vivos + get_Cadencia (la cadencia global de la .106) AUSENTE del heap, los ldstr de la vista completa de 25 líneas MUERTOS en el heap #US (UTF-16LE) y la pareja HambreBarra/HambreEstado VIVA, las constantes de la letra en el IL (float32: 0.75 la escala · 0.5 el 50% · 0.85/0.30 el ritmo propio · 0.35/0.65 el desfase de nacimiento · 62/42/0.80 la elipse · 0.32/−0.27 la contrarrotación · 0.9/0.18/0.07 la precesión · 0.37 la fase natal · 56/2.8/88 la corona · 12/15 el sello · 6.0/0.235/2.9/4.8/1.2/0.65/38/2.3/20 el rayo fino + la regresión 1/216000, 0.10, 0.41, 7.5, 3.2, 2.55, 0.55, 3.5/2.8), regresión .102–.106 completa, headless «Sandboxing v6.50.107 → Adding Recipes → Choose World» 0 excepciones), ✔ PUBLICADA (release 409818581 + asset AethonMod.tmod 631370474 2.590.979 B, CDN verificado byte a byte md5 b6f789b6…, /releases/latest = v6.50.107, respaldo /home/sync/AethonMod-v6.50.107.tmod), ⏳ en juego | LA LETRA (2026-10-11): «los minion son demasiado grande, deben ser mas pequeño, el tamaño debe ser el mismo que el del grimorio, ademas ellos atacan al mismo tiempo, cada ojo debe ser individual y deben tener la libertad de atacar cuando quieran y a la velocidad que deseen ya que la velocidad de ataque aumenta en cuanto mas nivel tengan el grimorio, hasta un maximo de 50% mas rapido a nivel 100 del grimorio subiendo a partir de nivel 1» + «el proyectil que usan los minion es muy grueso, debe ser un poco mas fino» + «la forma en la que deben moverse al rededor del libro es orbitar como una luna a un planeta no solo dar vuelta en ciruclo» + «la caja de informacion del grimorio eterno es demasiado grande con datos que no necesitan solo debe decir: daño magico, daño de invocacion, datos y buff propios del juego, un pequeño texto de lore donde explique un poco que es el libro sin decir mucho, al menos 2 lineas, luego en progresion debe salir la barra de hambre y la de exp». (1) EL TAMAÑO: ESCALA 0.75 — el espíritu viste el libro 36×49 a esc 1.15 = 41×56 px; el ojo 56×54 × 0.75 = 42×41 en pantalla: LA LUNA CABE EN LA SOMBRA DEL PLANETA; todo el layout escala con la MISMA constante (hitbox 34×20→25×15, el sello r16→12 respirando a 15, el aliento 3.3×2.8→2.5×2.1, IRIS_REST+mirada ×ESCALA, IRIS_ESCALA×ESCALA); la corona se acerca (radio 64+3.2n tope 98 → 56+2.8n tope 88). (2) EL RITMO PROPIO: muere la cadencia global de la .106 (110−nivel/3 piso 55 — get_Cadencia); nace la cadencia de ESTE ojo: CADENCIA_BASE 110 (1.83 s, la del Forbidden Ruin) / (VelocidadNivel × RitmoPropio); VelocidadNivel = 1 + 0.5·min(nivel−1, 99)/99 — 1.0× en nivel 1 subiendo LINEAL hasta 1.5× (un 50% más rápido) en nivel 100, CLAVADA ahí (el tope de la letra); RitmoPropio = 0.85 + 0.30·Hash01(whoAmI, 577, 131) — cada ojo ataca a la velocidad que LE da la gana; EL DESFASE DE NACIMIENTO: el primer disparo de cada ojo cae entre el 35% y el 100% de SU intervalo (Hash01(whoAmI, 811, 23)) — la banda invocada de golpe JAMÁS dispara en bloque (ai[1]==0 && frame==0 = el tick de nacimiento); SalirDeFuego arranca CadenciaPersonal (la de ESTE ojo — nivel × ritmo). (3) LA LUNA: con el libro fuera muere el círculo (radio 66+44k a velocidad angular constante); nace la elipse con LA SEGUNDA LEY DE KEPLER — semieje mayor a = 62+42k, semieje menor b = 0.8a (la órbita INCLINADA), precesión apsidal lenta (tilt = 0.9k + 0.18·sin(time·0.07 + k·1.3)), y el área barrida por tick CONSTANTE: dθ = ω·a²/r² (ω = 0.32 par / −0.27 impar, en rad/s ÷ 60 por tick) — el ojo ACELERA en el perigeo (r corto, cerca del libro) y FRENA en el apogeo (r largo, lejos): ORBITAR COMO UNA LUNA, no dar vuelta en círculo; la fase de cada ojo vive en localAI[3] (natal 0.37 + enAnillo/cap·2π + 0.7k — jamás 0: no renace) y SÓLO la avanza PasoOrbita (llamado UNA vez por tick desde la AI, ANTES del SlotDeFormacion — el draw la LEE vía FocoVivo, jamás la mueve: sin doble avance); AnilloDe extrae la escalera 5, 6, 7… y el vaivén propio (sin(1.1t + 1.63·enAnillo)·3) sigue vivo en la corona Y en la órbita. (4) EL RAYO FINO (sólo el MODO OJO de TruenoPerlinProjectile — DrawArcoOjo…CanalOjo; el DrawArco del cetro propio conserva su grosor de siempre): funda 7.5→6.0 (RayoStrip.Filamento), cápsulas negras 19→15 px (SoftGlow 0.30→0.235 — 64×64: la escala es ABSOLUTA), cadenas 3.6→2.9, gorro del electrodo 6.0→4.8 (EndCap), ancla 1.5/0.8→1.2/0.65, estallido de impacto 46→38 (ImpactFlash), arcos de corona 2.8→2.3 (ArcRing), bruma en la punta 24→20 (SombrasLib.Bruma). (5) LA CAJA COMPACTA: muere la vista completa de 25 líneas del tooltip del Eterno (ShowExtendedTooltip, MilestoneRewards con su lista, las secciones ═══ DAÑO ═══/═══ RECURSOS ═══/═══ PROYECTIL ═══/═══ BONUS ═══, el conmutador «Click der para vista»); el hjson ×3 (es-ES/en-US con TABS por BYTES — cirugia por python newline='', es-MX con espacios: la línea Tooltip: a 16 espacios, las triples comillas a 24, el contenido a 32) pierde el mega-tooltip de 19 líneas y gana el LORE de 2 líneas («Un libro viejo que devora lo que su portador mata. / Mantén sus páginas llenas... o será tu alma la que llene la siguiente.» — en-US: «An old book that devours whatever its bearer kills. / Keep its pages full... or your soul will be the one to fill the next.»); el C# sólo añade: la línea de DAÑO DE INVOCACIÓN pegada al Damage vanilla (que ya dice «XX daño mágico»), y PROGRESIÓN al final (barra de EXP de 20 █░ + la pareja HambreBarra/HambreEstado del Nervioso REUBICADA de tras-el-daño al final, dentro de progresión). LECCIONES: (a) los ldstr (los strings literales de C#) viven en el heap #US en UTF-16LE — los NOMBRES de las TooltipLine («HambreBarra») son ldstr, NO identificadores #Strings: el audit los caza con s.encode('utf-16-le'); (b) las ausencias de patrones del cetro se comprueban SOLO en el trozo del MODO OJO (cetro[cetro.index('private void DrawArcoOjo()'):cetro.index('private static Vector2[] CanalOjo')]) — DrawArco es el cetro PROPIO y conserva su grosor (1.5f·pulso y 46f viven ahí legítimamente); (c) «Click der para vista» vive en SeerOrb (SU toggle legítimo): una ausencia se comprueba en el ÁRBOL del archivo tocado, no en la DLL entera; (d) el /releases/latest SIN token da RATE LIMIT y devuelve None — la verificación de latest SIEMPRE con -H Authorization; (e) el es-MX usa 16/24/32 ESPACIOS (no tabs) — el byte-edit de su bloque Tooltip debe usar las sangrías exactas del archivo, y el patrón de búsqueda debe anclarse con \\n previo (el «8 espacios + Tooltip:» es SUBSTRING del «16 espacios + Tooltip:» de las otras 123 entradas) |
'''


def main():
    # === CHANGES.md: la entrada nueva al tope ===
    p = 'CHANGES.md'
    data = open(p, encoding='utf-8').read()
    ancla = '## Commit v6.50.106 — EL OJO DEL USUARIO, SIN RECORTE'
    assert data.count(ancla) == 1
    data = data.replace(ancla, CHANGES + ancla, 1)
    open(p, 'w', encoding='utf-8', newline='').write(data)
    print('CHANGES.md: entrada .107 al tope')

    # === STABLE-SNAPSHOT.md ===
    p = 'STABLE-SNAPSHOT.md'
    lines = open(p, encoding='utf-8').read().split('\n')
    # 1) el header: desde '# AethonMod' hasta el párrafo «...headless 0 excepciones.)»
    i0 = next(i for i, l in enumerate(lines) if l.startswith('# AethonMod — ESTADO ACTUAL'))
    i1 = next(i for i, l in enumerate(lines) if l.startswith('- **Tag**')) - 1
    assert 'v6.50.106' in lines[i0]
    lines[i0:i1 + 1] = SNAP_HEADER.split('\n')
    # 2) PRÓXIMOS PASOS: insertar .107 como item 1, renumerar, podar .103
    j = next(i for i, l in enumerate(lines)
             if l.startswith('1. **✅ v6.50.106 PUBLICADA'))
    k106 = next(i for i, l in enumerate(lines)
                if l.startswith('2. **✅ v6.50.105 PUBLICADA'))
    k105 = next(i for i, l in enumerate(lines)
                if l.startswith('3. **✅ v6.50.104 PUBLICADA'))
    k104 = next(i for i, l in enumerate(lines)
                if l.startswith('4. **✅ v6.50.103 PUBLICADA'))
    k103_end = next(i for i, l in enumerate(lines)
                    if l.startswith('1. **✅ v6.50.100 PUBLICADA'))
    # PODAR la .103 (k104..k103_end-1) — doc-rot de la lista
    del lines[k104:k103_end]
    # renumerar (después del del, relocalizar)
    j = next(i for i, l in enumerate(lines)
             if l.startswith('1. **✅ v6.50.106 PUBLICADA'))
    lines[j] = lines[j].replace('1. **✅ v6.50.106', '2. **✅ v6.50.106', 1)
    j2 = next(i for i, l in enumerate(lines)
              if l.startswith('2. **✅ v6.50.105 PUBLICADA'))
    lines[j2] = lines[j2].replace('2. **✅ v6.50.105', '3. **✅ v6.50.105', 1)
    j3 = next(i for i, l in enumerate(lines)
              if l.startswith('3. **✅ v6.50.104 PUBLICADA'))
    lines[j3] = lines[j3].replace('3. **✅ v6.50.104', '4. **✅ v6.50.104', 1)
    # insertar la .107 al tope de la lista
    lines.insert(j, SNAP_ITEM.rstrip('\n'))
    # 3) HISTORIAL: la fila .107 antes de la .106
    h = next(i for i, l in enumerate(lines) if l.startswith('| **v6.50.106**'))
    lines.insert(h, HISTORIAL_ROW.rstrip('\n'))
    open(p, 'w', encoding='utf-8', newline='').write('\n'.join(lines))
    print('STABLE-SNAPSHOT.md: header .107 + item 1 + renumeración (.106→2, .105→3, .104→4, .103 PODADA) + fila HISTORIAL')

    # === README.md ===
    p = 'README.md'
    data = open(p, encoding='utf-8').read()
    assert 'Versión actual:** 6.50.106' in data
    data = data.replace('Versión actual:** 6.50.106', 'Versión actual:** 6.50.107', 1)
    open(p, 'w', encoding='utf-8', newline='').write(data)
    print('README.md: 6.50.106 → 6.50.107')


if __name__ == '__main__':
    main()

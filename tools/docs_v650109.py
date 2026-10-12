#!/usr/bin/env python3
"""v6.50.109 — la cirugía de docs: CHANGES + STABLE-SNAPSHOT + README."""


CHANGES = """## Commit v6.50.109 — LA MITAD, LA FORMACIÓN GIRATORIA DEL RUINOUS Y LA IRA EN CADENA

**La letra**: «reduce el tamaño de los minion en un 50% y mejora la
animacion, que sea mas parecida a Ruinous_Staff en el momento en el que
estan alrededor del libro» + «añadir una estadistica para el grimorio
eterno que mejore a los minion».

**EL CAMBIO**: tres cirugías — la mitad, la formación y la cadena:

- **EL TAMAÑO A LA MITAD**: `ESCALA` 0,75 → **0,375** — el ojo (56×54)
  pasa de 42×41 a **21×20 px** en pantalla: LA MITAD EXACTA de la
  letra. Hitbox 25×15 → 12×8; el sello mágico a la ESCALA (r =
  ESCALA·16 ≈ 6 px → ESCALA·20 ≈ 7,5 en la carga); el aliento violáceo
  a la ESCALA; la corona sobre la cabeza apretada (radio 56+2,8n tope
  88 → **44+2,4n tope 70**); las órbitas apretadas (62+42k → **46+34k**).
- **LA FORMACIÓN GIRATORIA DEL RUINOUS** (la wiki del Forbidden Ruin —
  el minion del Ruinous Staff: «they form a spinning orbital formation»
  + «continuously adjusts its position to stay along the line segment
  between the player and its target»): MUERE la elipse kepleriana de
  la .107 (la 2ª ley aceleraba el paso en el perigeo y el giro LEÍA A
  TIRONES) y MUERE el freno en seco de la carga (velocity *= 0,75 — el
  ojo se congelaba al apuntar). NACE EL GIRO CONSTANTE por anillo (ω
  par +0,42 rad/s ≈ una vuelta cada 15 s / impar −0,36 contrarrotando),
  el anillo casi circular (0,94 — se lee como UNA RUEDA), el radio que
  RESPIRA despacio (±3,5%), la micro-deriva propia de cada ojo (1,6 px
  en 2D, lenta) y EL PLANEO DE LA CARGA: durante los 12 ticks de
  puntería el ojo SE REPOSICIONA deslizándose hacia la línea ancla↔presa
  (el punto a 35% de la distancia, tope 220 px; vmax 6·MinionSpeedMult)
  — una LEVANTADA hacia la presa; el movimiento NUNCA muere.
- **LA IRA EN CADENA — la estadística NUEVA que mejora a los minion**:
  los rayos de los ojos saltan a **+1 enemigo extra cada 20 niveles**
  del grimorio (base 2 en nivel 1 → tope 7 en nivel 100) —
  `WeaponScaling.MinionChainCount` +
  `TruenoPerlinProjectile.Strike` en modo ojo (`NivelDelOjo` lee el
  nivel del ai[2] del ojo que disparó; los arrays de cadena
  `MaxCadenasOjo = 7`; el CETRO propio conserva sus 2 saltos de
  siempre) + la línea NUEVA «Rayo en cadena: N enemigos» en la segunda
  ventana (MinionCadena, junto a la de velocidad de ataque) + «+1 salto
  de cadena del rayo» en los hitos 20/40/60/80/100 (MilestoneRewards,
  sólo cuando el salto SUBE).

**VERIFICACIÓN**: audit_texturas 0 · oráculo 0/0 · build real 0/0 ·
audit_v650109 TODO OK: .tmod 2.596.043 B md5 ec37b2b1830d928eb7ed164360db02b8,
387 entradas, EOF exacto, paquete==árbol ×3, rawimg 56×54 píxel-idénticas,
MinionChainCount/NivelDelOjo/MaxCadenasOjo VIVOS en el heap + los ldstr
«MinionCadena»/«Rayo en cadena: »/«+1 salto de cadena del rayo» VIVOS en
#US (UTF-16), las constantes de la .109 en el IL (0.375 la mitad · 46/34
las órbitas · 0.94 la rueda · 0.42/−0.36 el giro constante · 0.035 la
respiración · 1.6 la deriva · 6/0.12/0.18/220 el planeo de la carga ·
44/2.4/70 la corona · 6/7.5 el sello a la ESCALA) + la regresión
.102–.108 ÍNTEGRA, Kepler (dθ ∝ a²/r²) y el freno en seco AUSENTES del
árbol, localAI[3] AUSENTE del código, el ORDEN barra→estado y la segunda
ventana INTACTOS (.108) · headless «Sandboxing v6.50.109 → Adding
Recipes → Choose World» 0 excepciones.

---

"""

SNAP_HEADER = """# AethonMod — ESTADO ACTUAL (v6.50.109)

> **Este documento = "¿por dónde nos quedamos?"** — se actualiza en cada entrega.
> Última actualización: v6.50.109 (LA MITAD, LA FORMACIÓN GIRATORIA DEL
> RUINOUS Y LA IRA EN CADENA. La letra: «reduce el tamaño de los minion
> en un 50% y mejora la animacion, que sea mas parecida a Ruinous_Staff
> en el momento en el que estan alrededor del libro» + «añadir una
> estadistica para el grimorio eterno que mejore a los minion»).
> Tres cirugías: (1) EL TAMAÑO A LA MITAD — ESCALA 0,75→0,375 (el ojo
> 21×20 px); hitbox 12×8, sello/aliento a la ESCALA, corona 44+2,4n
> tope 70, órbitas 46+34k. (2) LA FORMACIÓN GIRATORIA DEL RUINOUS —
> MUERE la elipse kepleriana de la .107 (aceleración de perigeo =
> tirones) y MUERE el freno en seco de la carga; NACE el GIRO
> CONSTANTE por anillo (ω +0,42/−0,36 rad/s), el anillo casi circular
> (0,94), el radio que respira (±3,5%), la micro-deriva propia (1,6 px)
> y EL PLANEO DE LA CARGA hacia la línea ancla↔presa (una LEVANTADA
> hacia la presa — el movimiento nunca muere). (3) LA IRA EN CADENA —
> los rayos de los ojos saltan a +1 enemigo cada 20 niveles del
> grimorio (base 2 → tope 7 en nivel 100): WeaponScaling.
> MinionChainCount + Strike en modo ojo + la línea «Rayo en cadena» en
> la segunda ventana + el hito «+1 salto de cadena del rayo» en
> 20/40/60/80/100. .tmod 2.596.043 B
> md5 ec37b2b1830d928eb7ed164360db02b8, 387 entradas, oráculo 0/0,
> build real 0/0, headless 0 excepciones.)"""

SNAP_ITEM = """1. **✅ v6.50.109 PUBLICADA — LA MITAD, LA FORMACIÓN GIRATORIA DEL
   RUINOUS Y LA IRA EN CADENA** — la letra: «reduce el tamaño de los
   minion en un 50% y mejora la animacion, que sea mas parecida a
   Ruinous_Staff en el momento en el que estan alrededor del libro» +
   «añadir una estadistica para el grimorio eterno que mejore a los
   minion». (1) EL TAMAÑO A LA MITAD: ESCALA 0,75→0,375 — el ojo 56×54
   → 21×20 px en pantalla (LA MITAD EXACTA); hitbox 25×15→12×8; el
   sello a la ESCALA (r 6→7,5 en la carga); el aliento a la ESCALA; la
   corona 44+2,4n tope 70 (era 56+2,8n/88); las órbitas 46+34k (era
   62+42k). (2) LA FORMACIÓN GIRATORIA DEL RUINOUS (la wiki del
   Forbidden Ruin: «they form a spinning orbital formation» +
   «continuously adjusts its position to stay along the line segment
   between the player and its target»): MUERE la elipse kepleriana de
   la .107 (la 2ª ley aceleraba en el perigeo — el giro leía a
   tirones) y MUERE el freno en seco de la carga (velocity *= 0,75);
   NACE EL GIRO CONSTANTE por anillo (ω par +0,42 rad/s ≈ vuelta cada
   15 s / impar −0,36 contrarrotando), el anillo casi circular (0,94 —
   UNA RUEDA), el radio que RESPIRA (±3,5%), la micro-deriva propia
   (1,6 px, 2D) y EL PLANEO DE LA CARGA: en los 12 ticks de puntería
   el ojo SE REPOSICIONA hacia la línea ancla↔presa (punto al 35% de
   la distancia, tope 220 px, vmax 6·MinionSpeedMult) — una LEVANTADA
   hacia la presa; el movimiento NUNCA muere. (3) LA IRA EN CADENA —
   LA ESTADÍSTICA NUEVA: los rayos de los ojos saltan a +1 enemigo
   extra cada 20 niveles (base 2 → tope 7 en nivel 100) —
   WeaponScaling.MinionChainCount + TruenoPerlinProjectile.Strike en
   modo ojo (NivelDelOjo lee el nivel del ai[2] del ojo;
   MaxCadenasOjo = 7; el cetro propio conserva sus 2 saltos) + la
   línea «Rayo en cadena: N enemigos» en la segunda ventana +
   «+1 salto de cadena del rayo» en los hitos 20/40/60/80/100.
   VERIFICACIÓN: audit_texturas 0 · oráculo 0/0 · build real 0/0 ·
   audit_v650109 TODO OK — .tmod 2.596.043 B md5
   ec37b2b1830d928eb7ed164360db02b8, 387 entradas, EOF exacto,
   paquete==árbol ×3, rawimg 56×54 píxel-idénticas,
   MinionChainCount/NivelDelOjo/MaxCadenasOjo VIVOS en el heap + los
   ldstr «MinionCadena»/«Rayo en cadena: »/«+1 salto de cadena del
   rayo» VIVOS en #US (UTF-16), las constantes de la .109 en el IL +
   la regresión .102–.108 ÍNTEGRA, Kepler y el freno en seco
   AUSENTES del árbol, localAI[3] AUSENTE del código, el ORDEN
   barra→estado y la segunda ventana INTACTOS (.108) · headless
   «Sandboxing v6.50.109 → Adding Recipes → Choose World» 0
   excepciones. CHECKLIST en juego: (a) los ojos a LA MITAD del
   tamaño de la .108, (b) con el libro fuera: los anillos GIRAN LISO
   a velocidad constante (sin tirones — la formación giratoria del
   Ruinous) y el radio respira, (c) al apuntar: el ojo SE INCLINA
   hacia la presa en vez de frenarse en seco, (d) la línea «Rayo en
   cadena: N enemigos» en la segunda ventana, (e) a nivel 20/40/… el
   rayo salta a un enemigo MÁS, (f) los hitos 20/40/60/80/100 listan
   «+1 salto de cadena del rayo».
   PUBLICACIÓN: commit 5aa4a8a + tag v6.50.109 + push (1217354..5aa4a8a)
   · release **410028555** (make_latest) + asset AethonMod.tmod
   **632240478** 2.596.043 B (md5
   ec37b2b1830d928eb7ed164360db02b8), CDN VERIFICADO BYTE A BYTE,
   /releases/latest = v6.50.109, respaldo /home/sync/AethonMod-v6.50.109.tmod.
"""

HISTORIAL_ROW = """| **v6.50.109** | ✅ Build-verificada (audit_texturas 0, oráculo 0/0, build real 0/0, .tmod 2.596.043 B md5 ec37b2b1830d928eb7ed164360db02b8, 387 entradas, EOF exacto, paquete==árbol ×3 idiomas, rawimg 56×54 píxel-idénticas, MinionChainCount/NivelDelOjo/MaxCadenasOjo VIVOS en el heap #Strings + los ldstr «MinionCadena»·«Rayo en cadena: »·«+1 salto de cadena del rayo» VIVOS en el heap #US (UTF-16LE), las constantes de la .109 en el IL (0.375 la mitad · 46/34 las órbitas · 0.94 la rueda · 0.42/−0.36 el giro constante · 0.035 la respiración · 1.6 la deriva · 6/0.12/0.18/220 el planeo de la carga · 44/2.4/70 la corona · 6/7.5 el sello a la ESCALA) + la regresión .102–.108 ÍNTEGRA en el IL, Kepler (dθ ∝ a²/r²) y el freno en seco (velocity *= 0.75f) AUSENTES del ÁRBOL, localAI[3] AUSENTE del código, el ORDEN barra→estado y la segunda ventana INTACTOS (.108), headless «Sandboxing v6.50.109 → Adding Recipes → Choose World» 0 excepciones), ✔ PUBLICADA (release 410028555 + asset AethonMod.tmod 632240478 2.596.043 B, CDN verificado byte a byte md5 ec37b2b1…, /releases/latest = v6.50.109, respaldo /home/sync/AethonMod-v6.50.109.tmod), ⏳ en juego | LA LETRA (2026-10-12): «reduce el tamaño de los minion en un 50% y mejora la animacion, que sea mas parecida a Ruinous_Staff en el momento en el que estan alrededor del libro» + «añadir una estadistica para el grimorio eterno que mejore a los minion». (1) EL TAMAÑO: ESCALA 0,75→0,375 — LA MITAD EXACTA (el ojo 21×20 px); hitbox 12×8, sello ESCALA·16→ESCALA·20 (6→7,5 px), aliento a la ESCALA, corona 44+2,4n tope 70, órbitas 46+34k. (2) EL RUINOUS (investigado en la wiki del Forbidden Ruin — el minion del Ruinous Staff: hovers above the player · aggressively chases · spinning orbital formation · se reposiciona continuamente sobre la línea jugador↔presa): la elipse kepleriana de la .107 MURIÓ (el usuario vio los tirones de la 2ª ley) — NACE el GIRO CONSTANTE (ω fija por anillo, contrarrotación +0,42/−0,36 rad/s), el anillo casi circular (0,94), el radio respirando (±3,5%), la micro-deriva (1,6 px 2D) y el PLANEO DE LA CARGA (el freno en seco murió: el ojo se desliza hacia la línea ancla↔presa a vmax 6·MinionSpeedMult — «continuously adjusts its position», la firma del Ruinous). (3) LA IRA EN CADENA (la estadística pedida): +1 salto de cadena del rayo por cada 20 niveles (2→7): WeaponScaling.MinionChainCount (nuevo) + el MODO OJO de TruenoPerlinProjectile.Strike lee el nivel vía NivelDelOjo (el ai[2] del ojo) y los arrays MaxCadenasOjo=7; el CETRO propio NO cambia (sus 2 saltos); tooltip: «MinionCadena» («Rayo en cadena: N enemigos (base 2, +1 cada 20 niveles)») en la sección OJOS DEL ETERNO; hitos: «+1 salto de cadena del rayo» cuando MinionChainCount(level−5) < MinionChainCount(level) — niveles 20/40/60/80/100. LECCIONES: (a) el sandbox PUEDE VOLVER ATRÁS el repo local (estaba en .65 con GitHub en .108): fetch + diff ANTES de tocar nada — el usuario lo avisó («recuerda revisar y comparar siempre la version de github»); el reset --hard a origin/main restaura TODO (los untracked colisionantes eran idénticos a origin); (b) el ENTORNO también se wipea (/tmp/tml, /tmp/sdk, ~/.dotnet/tools, .verify, el token de .env, ~/.git-credentials): reconstruir tML del release zip 2026.08.3.0 + SDK 8.0.404 + verify.csproj (10 refs managed de Libraries) + ilspycmd 9.0.0.7889 (DOTNET_ROOT=/tmp/sdk); (c) el Ruinous Staff NO está en tML 2026.08 (es 1.4.5): la fuente de verdad es la WIKI (terraria.wiki.gg/wiki/Ruinous_Staff — cadencia 1,75–1,92 s, rango 50 tiles, línea jugador↔presa, spinning orbital formation); (d) las constantes const-fold: ESCALA·16f se pliega a 6f en el IL (ESCALA es const — Roslyn pliega const×const), así que el audit busca el RESULTADO (6f/7.5f) y el 0.375f crudo vive en las multiplicaciones con VARIABLES | """


def main():
    # === CHANGES.md: la entrada nueva al tope ===
    p = 'CHANGES.md'
    data = open(p, encoding='utf-8').read()
    ancla = '## Commit v6.50.108 — EL FIX DE LOS OJOS, EL ORDEN DEL HAMBRE Y LA SEGUNDA VENTANA'
    assert data.count(ancla) == 1
    data = data.replace(ancla, CHANGES + ancla, 1)
    open(p, 'w', encoding='utf-8', newline='').write(data)
    print('CHANGES.md: entrada .109 al tope')

    # === STABLE-SNAPSHOT.md ===
    p = 'STABLE-SNAPSHOT.md'
    lines = open(p, encoding='utf-8').read().split('\n')
    # 1) el header
    i0 = next(i for i, l in enumerate(lines) if l.startswith('# AethonMod — ESTADO ACTUAL'))
    i1 = next(i for i, l in enumerate(lines) if l.startswith('- **Tag**')) - 1
    assert 'v6.50.108' in lines[i0]
    lines[i0:i1 + 1] = SNAP_HEADER.split('\n')
    # 2) PRÓXIMOS PASOS: insertar .109 como item 1, renumerar, podar .105
    k108 = next(i for i, l in enumerate(lines)
                if l.startswith('1. **✅ v6.50.108 PUBLICADA'))
    k107 = next(i for i, l in enumerate(lines)
                if l.startswith('2. **✅ v6.50.107 PUBLICADA'))
    k106 = next(i for i, l in enumerate(lines)
                if l.startswith('3. **✅ v6.50.106 PUBLICADA'))
    k105 = next(i for i, l in enumerate(lines)
                if l.startswith('4. **✅ v6.50.105 PUBLICADA'))
    k105_end = next(i for i, l in enumerate(lines)
                    if l.startswith('1. **✅ v6.50.100 PUBLICADA'))
    # PODAR la .105 (doc-rot)
    del lines[k105:k105_end]
    # renumerar
    j = next(i for i, l in enumerate(lines)
             if l.startswith('1. **✅ v6.50.108 PUBLICADA'))
    lines[j] = lines[j].replace('1. **✅ v6.50.108', '2. **✅ v6.50.108', 1)
    j2 = next(i for i, l in enumerate(lines)
              if l.startswith('2. **✅ v6.50.107 PUBLICADA'))
    lines[j2] = lines[j2].replace('2. **✅ v6.50.107', '3. **✅ v6.50.107', 1)
    j3 = next(i for i, l in enumerate(lines)
              if l.startswith('3. **✅ v6.50.106 PUBLICADA'))
    lines[j3] = lines[j3].replace('3. **✅ v6.50.106', '4. **✅ v6.50.106', 1)
    # insertar la .109 al tope
    lines.insert(j, SNAP_ITEM.rstrip('\n'))
    # 3) HISTORIAL: la fila .109 antes de la .108
    h = next(i for i, l in enumerate(lines) if l.startswith('| **v6.50.108**'))
    lines.insert(h, HISTORIAL_ROW.rstrip('\n'))
    open(p, 'w', encoding='utf-8', newline='').write('\n'.join(lines))
    print('STABLE-SNAPSHOT.md: header .109 + item 1 + renumeración (.108→2, .107→3, .106→4, .105 PODADA) + fila HISTORIAL')

    # === README.md ===
    p = 'README.md'
    data = open(p, encoding='utf-8').read()
    assert 'Versión actual:** 6.50.108' in data
    data = data.replace('Versión actual:** 6.50.108', 'Versión actual:** 6.50.109', 1)
    open(p, 'w', encoding='utf-8', newline='').write(data)
    print('README.md: 6.50.108 → 6.50.109')


if __name__ == '__main__':
    main()

#!/usr/bin/env python3
"""v6.50.108 — la cirugía de docs: CHANGES + STABLE-SNAPSHOT + README."""


CHANGES = '''## Commit v6.50.108 — EL FIX DE LOS OJOS, EL ORDEN DEL HAMBRE Y LA SEGUNDA VENTANA

**La letra**: «el texto que dice lo que hara el libro con el hambre, por
ejemplo este: Fuera de Caza - flota sobre ti buscando su propia comida,
bueno ese texto debe ir justo debajo del hambre, no encima» + «la segunda
ventana de informacion del grimorio desaprecio, recuerda que el grimorio
necesita una segunda ventana de info para visualisar sus estadisticas» +
«en progresion quitaste las estadisticas del proximo nivel, vuelve a
ponerlas» + «ademas, hay errores los minion no aparecen» (client.log:
IndexOutOfRangeException en OjoEternoMinion.FaseOrbital, línea 415).

**EL CAMBIO**: cuatro cirugías — el crash, el orden, la ventana y el hito:

- **EL CRASH DE LOS OJOS**: `Projectile.localAI` es `float[3]` en
  tModLoader (índices 0–2 válidos) — la .107 guardaba la fase orbital de
  la luna en `localAI[3]` y la AI tiraba `IndexOutOfRange` en cada tick
  con el libro fuera: LOS OJOS NO APARECÍAN (la excepción abortaba la AI
  antes del posicionamiento). La fase ahora vive en el CAMPO DE
  INSTANCIA `_faseOrbital` (una fase POR ojo, determinista en todos los
  clientes: nace del puesto en el anillo — 0.37 + enAnillo/cap·2π + 0.7k
  — y SÓLO la avanza PasoOrbita, una vez por tick desde la AI).
- **EL ORDEN DE LA LETRA**: en PROGRESIÓN la BARRA de hambre primero y el
  estado («FUERA DE CAZA — flota sobre ti buscando su propia comida.»)
  JUSTO DEBAJO — la .107 lo ponía encima de la barra.
- **LA SEGUNDA VENTANA VUELVE**: el conmutador de siempre (clic derecho
  sobre el libro en el inventario, TooltipToggleItem.ShowExtendedTooltip)
  reabre la vista completa con TODAS las estadísticas — DAÑO / RECURSOS /
  PROYECTIL / OJOS DEL ETERNO / BONUS + la línea NUEVA de la velocidad de
  ataque de los ojos (tope +50% en nivel 100 — la .107) + PROGRESIÓN al
  final con la pareja del hambre y el próximo hito. La vista BÁSICA queda
  como la .107 (compacta: daño mágico vanilla + daño de invocación +
  datos/buff del juego + lore de 2 líneas + PROGRESIÓN con EXP + hambre).
- **LAS ESTADÍSTICAS DEL PRÓXIMO NIVEL**: el PRÓXIMO HITO (el siguiente
  múltiplo de 5) y su escalera de mejoras (MilestoneRewards) — de vuelta
  dentro de PROGRESIÓN, debajo de la pareja del hambre.

**VERIFICACIÓN**: audit_texturas 0 · oráculo 0/0 · build real 0/0 ·
audit_v650108 TODO OK: .tmod 2.593.519 B md5 01e03580e15a356d097143bae98a4537,
387 entradas, EOF exacto, paquete==árbol ×3, rawimg 56×54 píxel-idénticas,
_faseOrbital VIVO en el heap #Strings + Projectile.localAI[3] AUSENTE del
código, los ldstr de la segunda ventana VIVOS en #US (UTF-16: «Próximo
hito: Nivel», «═══ DAÑO ═══», «═══ RECURSOS ═══», «═══ PROYECTIL ═══»,
«═══ BONUS ═══», «Click der para vista completa/básica») y el ORDEN
barra→estado verificado por POSICIÓN, las constantes de la .107 (regresión
íntegra) en el IL, regresión .102–.107 completa · headless «Sandboxing
v6.50.108 → Adding Recipes → Choose World» 0 excepciones.

---

'''

SNAP_HEADER = '''# AethonMod — ESTADO ACTUAL (v6.50.108)

> **Este documento = "¿por dónde nos quedamos?"** — se actualiza en cada entrega.
> Última actualización: v6.50.108 (EL FIX DE LOS OJOS, EL ORDEN DEL
> HAMBRE Y LA SEGUNDA VENTANA. La letra: «el texto que dice lo que hara
> el libro con el hambre, por ejemplo este: Fuera de Caza - flota sobre
> ti buscando su propia comida, bueno ese texto debe ir justo debajo del
> hambre, no encima» + «la segunda ventana de informacion del grimorio
> desaprecio, recuerda que el grimorio necesita una segunda ventana de
> info para visualisar sus estadisticas» + «en progresion quitaste las
> estadisticas del proximo nivel, vuelve a ponerlas» + «ademas, hay
> errores los minion no aparecen» — client.log: IndexOutOfRange en
> OjoEternoMinion.FaseOrbital línea 415). Cuatro cirugías: (1) EL CRASH
> — Projectile.localAI es float[3] en tML (índices 0–2): la .107 guardaba
> la fase orbital de la luna en localAI[3] y la AI tiraba
> IndexOutOfRange en cada tick con el libro fuera (los ojos no
> aparecían); la fase ahora vive en el CAMPO DE INSTANCIA _faseOrbital
> (una fase POR ojo, determinista en todos los clientes). (2) EL ORDEN —
> en PROGRESIÓN la BARRA de hambre primero y el estado («FUERA DE
> CAZA…») JUSTO DEBAJO. (3) LA SEGUNDA VENTANA — vuelve la vista completa
> por el conmutador de siempre (clic der sobre el libro) con TODAS las
> estadísticas + la línea nueva de la velocidad de ataque de los ojos
> (tope +50% nivel 100); la vista básica queda como la .107. (4) EL HITO
> — el PRÓXIMO HITO y su escalera de mejoras, de vuelta dentro de
> PROGRESIÓN. .tmod 2.593.519 B
> md5 01e03580e15a356d097143bae98a4537, 387 entradas, oráculo 0/0,
> build real 0/0, headless 0 excepciones.)'''

SNAP_ITEM = '''1. **✅ v6.50.108 PUBLICADA — EL FIX DE LOS OJOS, EL ORDEN DEL HAMBRE Y
   LA SEGUNDA VENTANA** — la letra: «el texto que dice lo que hara el
   libro con el hambre, por ejemplo este: Fuera de Caza - flota sobre ti
   buscando su propia comida, bueno ese texto debe ir justo debajo del
   hambre, no encima» + «la segunda ventana de informacion del grimorio
   desaprecio, recuerda que el grimorio necesita una segunda ventana de
   info para visualisar sus estadisticas» + «en progresion quitaste las
   estadisticas del proximo nivel, vuelve a poneras» + «ademas, hay
   errores los minion no aparecen» (client.log:
   IndexOutOfRangeException en OjoEternoMinion.FaseOrbital línea 415, por
   PasoOrbita línea 436, desde la AI línea 159). (1) EL CRASH DE LOS
   OJOS: Projectile.localAI es float[3] en tML (índices 0–2) — la .107
   guardaba la fase orbital en localAI[3]: IndexOutOfRange en cada tick
   con el libro fuera y la AI abortaba antes del posicionamiento (los
   ojos no aparecían); la fase ahora vive en el CAMPO DE INSTANCIA
   _faseOrbital (una fase POR ojo, determinista: nace del puesto en el
   anillo — 0.37 + enAnillo/cap·2π + 0.7k — y SÓLO la avanza PasoOrbita;
   el campo entra al heap #Strings y el código jamás toca localAI[3]).
   (2) EL ORDEN DE LA LETRA: en PROGRESIÓN la BARRA de hambre primero y
   el estado («FUERA DE CAZA — flota sobre ti…») JUSTO DEBAJO — la .107
   lo ponía encima. (3) LA SEGUNDA VENTANA VUELVE: el conmutador de
   siempre (clic der sobre el libro, TooltipToggleItem.ShowExtendedTooltip)
   reabre la vista completa con TODAS las estadísticas — DAÑO / RECURSOS
   / PROYECTIL / OJOS DEL ETERNO / BONUS + la línea NUEVA de la velocidad
   de ataque de los ojos (tope +50% en nivel 100, la .107) + PROGRESIÓN
   al final con la pareja del hambre y el próximo hito; la vista BÁSICA
   queda como la .107 (daño mágico vanilla + daño de invocación + lore 2
   líneas + EXP + hambre). (4) LAS ESTADÍSTICAS DEL PRÓXIMO NIVEL: el
   PRÓXIMO HITO (siguiente múltiplo de 5) y su escalera de mejoras
   (MilestoneRewards), de vuelta dentro de PROGRESIÓN. VERIFICACIÓN:
   audit_texturas 0 · oráculo 0/0 · build real 0/0 · audit_v650108 TODO
   OK — .tmod 2.593.519 B md5 01e03580e15a356d097143bae98a4537, 387
   entradas, EOF exacto, paquete==árbol ×3, rawimg 56×54 píxel-idénticas,
   _faseOrbital VIVO en el heap + Projectile.localAI[3] AUSENTE del
   código, los ldstr de la segunda ventana VIVOS en #US (UTF-16) + el
   ORDEN barra→estado verificado por posición, las constantes de la .107
   (regresión íntegra) en el IL, regresión .102–.107 completa · headless
   «Sandboxing v6.50.108 → Adding Recipes → Choose World» 0 excepciones.
   CHECKLIST en juego: (a) LOS OJOS APARECEN (con el libro fuera orbitan
   la elipse kepleriana — sin excepciones en el log), (b) en PROGRESIÓN:
   la barra de hambre ARRIBA y el estado («FUERA DE CAZA…») JUSTO
   DEBAJO, (c) clic der sobre el libro: la SEGUNDA VENTANA con todas las
   estadísticas, (d) en PROGRESIÓN: el PRÓXIMO HITO con su lista de
   mejoras.
   PUBLICACIÓN: commit 2e4f16d + tag v6.50.108 + push (83e727e..2e4f16d)
   · release **409833698** (make_latest) + asset AethonMod.tmod
   **631436547** 2.593.519 B (md5
   01e03580e15a356d097143bae98a4537), CDN VERIFICADO BYTE A BYTE,
   /releases/latest = v6.50.108, respaldo /home/sync/AethonMod-v6.50.108.tmod.
'''

HISTORIAL_ROW = '''| **v6.50.108** | ✅ Build-verificada (audit_texturas 0, oráculo 0/0, build real 0/0, .tmod 2.593.519 B md5 01e03580e15a356d097143bae98a4537, 387 entradas, EOF exacto, paquete==árbol ×3 idiomas, rawimg 56×54 píxel-idénticas, el CAMPO _faseOrbital VIVO en el heap #Strings + Projectile.localAI[3] AUSENTE del código (el crash de la .107), los ldstr de la SEGUNDA VENTANA VIVOS en el heap #US (UTF-16LE: «Próximo hito: Nivel» · «═══ DAÑO ═══» · «═══ RECURSOS ═══» · «═══ PROYECTIL ═══» · «═══ BONUS ═══» · «Click der para vista completa/básica» · «MinionRitmo») y el ORDEN barra→estado verificado por POSICIÓN (find(tooltips.Add(barraHambre)) < find(tooltips.Add(estadoHambre))), las constantes de la .107 (regresión íntegra) en el IL (0.75 la escala · 0.5 el 50% · 0.85/0.30 el ritmo · 0.35/0.65 el desfase · 62/42/0.80 la elipse · 0.32/−0.27 la contrarrotación · 0.9/0.18/0.07 la precesión · 0.37 la fase natal · 56/2.8/88 la corona · 12/15 el sello · 6.0/0.235/2.9/4.8/1.2/0.65/38/2.3/20 el rayo fino + la regresión 1/216000, 0.10, 0.41, 7.5, 3.2, 2.55, 0.55, 3.5/2.8), regresión .102–.107 completa, headless «Sandboxing v6.50.108 → Adding Recipes → Choose World» 0 excepciones), ✔ PUBLICADA (release 409833698 + asset AethonMod.tmod 631436547 2.593.519 B, CDN verificado byte a byte md5 01e03580…, /releases/latest = v6.50.108, respaldo /home/sync/AethonMod-v6.50.108.tmod), ⏳ en juego | LA LETRA (2026-10-11): «el texto que dice lo que hara el libro con el hambre, por ejemplo este: Fuera de Caza - flota sobre ti buscando su propia comida, bueno ese texto debe ir justo debajo del hambre, no encima» + «la segunda ventana de informacion del grimorio desaprecio, recuerda que el grimorio necesita una segunda ventana de info para visualisar sus estadisticas» + «en progresion quitaste las estadisticas del proximo nivel, vuelve a poneras» + «ademas, hay errores los minion no aparecen». (1) EL CRASH (client.log del usuario: System.IndexOutOfRangeException at OjoEternoMinion.FaseOrbital(Int32,Int32,Int32) line 415, at .PasoOrbita line 436, at .AI line 159): Projectile.localAI es float[3] en tML — la .107 guardaba la fase orbital de la luna en localAI[3]; con el libro fuera, cada tick de la AI tiraba IndexOutOfRange (excepción silenciosa de tML) y abortaba ANTES del SlotDeFormacion: los ojos no se posicionaban ni disparaban; la fase ahora vive en el CAMPO DE INSTANCIA _faseOrbital de OjoEternoMinion (los ModProjectile son una instancia POR proyectil: una fase POR ojo), determinista en todos los clientes (natal 0.37 + enAnillo/cap·2π + 0.7k, avance por PasoOrbita UNA vez por tick) — el índice fuera de rango MUERE y el campo entra al heap #Strings (los nombres de CAMPO también viven ahí, no sólo los de método). (2) EL ORDEN: PROGRESIÓN = SectionProgress + Level (barra de EXP 20 █░) + HambreBarra + HambreEstado DEBAJO (la .107 ponía el estado ENCIMA de la barra — la letra lo quiere JUSTO DEBAJO). (3) LA SEGUNDA VENTANA: la .107 mató la vista completa de 25 líneas (ShowExtendedTooltip, MilestoneRewards, las secciones ═══ DAÑO/RECURSOS/PROYECTIL/BONUS ═══ y el conmutador «Click der para vista») — el usuario la PIDE DE VUELTA («el grimorio necesita una segunda ventana de info para visualisar sus estadisticas»); VUELVE íntegra por el MISMO conmutador (TooltipToggleItem — GlobalItem que aplica SOLO a GrimoireEternal y alterna con clic der en el inventario) + la línea NUEVA «velocidad de ataque de los ojos (tope +50% en nivel 100)» (ritmoOjos = (1+0.5·min(nivel−1,99)/99 − 1)·100 — la fórmula de la .107 en versión tooltip) + PROGRESIÓN al final EN AMBAS VISTAS (EXP + hambre + próximo hito — la progresión ya no es exclusiva de la básica). (4) EL PRÓXIMO HITO: «en progresion quitaste las estadisticas del proximo nivel, vuelve a poneras» — el bloque NextMilestone (siguiente múltiplo de 5) + la lista MilestoneRewards (los «+1 slot de minion», «+2% armor penetration»…), de vuelta DEBAJO de la pareja del hambre. LECCIONES: (a) Projectile.localAI y Projectile.ai son float[3] en tML — NUNCA índices ≥3: el estado extra de un ModProjectile va en CAMPOS DE INSTANCIA (una instancia POR proyectil, viva mientras el proyectil exista) — el heap #Strings guarda también los nombres de CAMPO (el audit de presencia puede cazarlos igual que los de método); (b) el stack trace del client.log del usuario DA LA LÍNEA EXACTA del crash — leerlo ANTES de tocar código: FaseOrbital@415 = el localAI[3]; (c) la .107 mató los ldstr de la vista completa y la .108 los REVIVE — el audit de una versión es una FOTO, no una ley: los VIVOS/MUERTOS_US se recalculan por versión (los ldstr viajan en #US UTF-16LE); (d) el ORDEN de dos líneas del tooltip se audita por POSICIÓN (find(a) < find(b)), no por presencia — ambas líneas están presentes en cualquier orden | '''


def main():
    # === CHANGES.md: la entrada nueva al tope ===
    p = 'CHANGES.md'
    data = open(p, encoding='utf-8').read()
    ancla = '## Commit v6.50.107 — EL TAMAÑO DEL LIBRO, LA LUNA Y EL RITMO PROPIO'
    assert data.count(ancla) == 1
    data = data.replace(ancla, CHANGES + ancla, 1)
    open(p, 'w', encoding='utf-8', newline='').write(data)
    print('CHANGES.md: entrada .108 al tope')

    # === STABLE-SNAPSHOT.md ===
    p = 'STABLE-SNAPSHOT.md'
    lines = open(p, encoding='utf-8').read().split('\n')
    # 1) el header: desde '# AethonMod' hasta el párrafo anterior a «- **Tag**»
    i0 = next(i for i, l in enumerate(lines) if l.startswith('# AethonMod — ESTADO ACTUAL'))
    i1 = next(i for i, l in enumerate(lines) if l.startswith('- **Tag**')) - 1
    assert 'v6.50.107' in lines[i0]
    lines[i0:i1 + 1] = SNAP_HEADER.split('\n')
    # 2) PRÓXIMOS PASOS: insertar .108 como item 1, renumerar, podar .104
    j = next(i for i, l in enumerate(lines)
             if l.startswith('1. **✅ v6.50.107 PUBLICADA'))
    k107 = j
    k106 = next(i for i, l in enumerate(lines)
                if l.startswith('2. **✅ v6.50.106 PUBLICADA'))
    k105 = next(i for i, l in enumerate(lines)
                if l.startswith('3. **✅ v6.50.105 PUBLICADA'))
    k104 = next(i for i, l in enumerate(lines)
                if l.startswith('4. **✅ v6.50.104 PUBLICADA'))
    k104_end = next(i for i, l in enumerate(lines)
                    if l.startswith('1. **✅ v6.50.100 PUBLICADA'))
    # PODAR la .104 (k104..k104_end-1) — doc-rot de la lista
    del lines[k104:k104_end]
    # renumerar (después del del, relocalizar)
    j = next(i for i, l in enumerate(lines)
             if l.startswith('1. **✅ v6.50.107 PUBLICADA'))
    lines[j] = lines[j].replace('1. **✅ v6.50.107', '2. **✅ v6.50.107', 1)
    j2 = next(i for i, l in enumerate(lines)
              if l.startswith('2. **✅ v6.50.106 PUBLICADA'))
    lines[j2] = lines[j2].replace('2. **✅ v6.50.106', '3. **✅ v6.50.106', 1)
    j3 = next(i for i, l in enumerate(lines)
              if l.startswith('3. **✅ v6.50.105 PUBLICADA'))
    lines[j3] = lines[j3].replace('3. **✅ v6.50.105', '4. **✅ v6.50.105', 1)
    # insertar la .108 al tope de la lista
    lines.insert(j, SNAP_ITEM.rstrip('\n'))
    # 3) HISTORIAL: la fila .108 antes de la .107
    h = next(i for i, l in enumerate(lines) if l.startswith('| **v6.50.107**'))
    lines.insert(h, HISTORIAL_ROW.rstrip('\n'))
    open(p, 'w', encoding='utf-8', newline='').write('\n'.join(lines))
    print('STABLE-SNAPSHOT.md: header .108 + item 1 + renumeración (.107→2, .106→3, .105→4, .104 PODADA) + fila HISTORIAL')

    # === README.md ===
    p = 'README.md'
    data = open(p, encoding='utf-8').read()
    assert 'Versión actual:** 6.50.107' in data
    data = data.replace('Versión actual:** 6.50.107', 'Versión actual:** 6.50.108', 1)
    open(p, 'w', encoding='utf-8', newline='').write(data)
    print('README.md: 6.50.107 → 6.50.108')


if __name__ == '__main__':
    main()

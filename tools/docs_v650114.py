#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# docs_v650114.py — CHANGES entrada .114 + STABLE-SNAPSHOT (header .114/.113/.112
# — la .111 SALE — + fila HISTORIAL + item 1 de PRÓXIMOS con renumeración
# .113→2/.112→3/.111→4 y la .110 PODADA).
# NOTA .114: YA NO SE TOCA EL README (legal-only desde el Task 32 — el
# aviso legal y el robots.txt son la portada; la versión vive AQUÍ).

CHANGES = """# AethonMod — Historial de Cambios

## Commit v6.50.114 — EL DAÑO MASIVO TAMBIÉN CENA, LA PELÍCULA DEL LIBRO Y EL TECHO DE LOS OJOS

**La letra**: «cuando el daño que recibe el jefe es masivo no se aplica
eso de que su primera muerte sea absorbido por el grimorio» + «cuando
es asborvido por el grimorio no se activa correctamente la animacion,
el jefe de ser rodeado por anillos iguales a los de los proyectiles
agujeros negro y ademas tambien poner anillos como los de los soles
runicos, ser llevado al centro de la pantalla y ser absorvido por el
libro, todo esto en su primera muerte por el grimorio eterno, luego de
este no vuelve ocurrir a no ser que vuelva a ser la primera vez, esto
se aplica a todos los jefes» + «al parecer los minion no tienen limite,
revisa eso, deberian tener un limite de 10, pero actualiza a que
tengan un limite de 20 con el libro y mas con items y armadura de
invocador, ademas mejora las animacion en cuanto estan alrededor del
grimorio en su fase de casa y sobre la animacion de ataque de los
minion has que al atacar tenga un pequeño retroceso».

**EL CAMBIO**: cuatro entregas — el fix del golpe masivo, la película
nueva de la cena, el techo del escuadrón y las animaciones de los ojos:

- **EL DAÑO MASIVO TAMBIÉN CENA**: el gatillo de LA PRIMERA CENA vivía
  solo en `PreAI` y exigía `life > 0` — un golpe que cruzaba de lleno a
  ≤ 0 sin pasar por las 100 de vida le robaba la cena al portador (el
  jefe moría con su animación normal). Ahora la muerte también se
  intercepta en `CheckDead` (ANTES de la marca: la cena tiene
  PRIORIDAD sobre el festín clásico) y `Iniciar` ENTREGA a la cena
  cuando el drain de las armas salta masivo: **la primera muerte de
  cada jefe ES la cena, venga como venga** — todos los jefes, solo la
  primera vez (la Página en Blanco la reinicia).
- **LA PELÍCULA NUEVA — EL LIBRO SE LO COME**: los TRES ANILLOS DE
  FOTONES de los agujeros negros (la textura Ring con aberración
  cromática RGB y el aro blanco caliente — el patrón EXACTO del
  `BlackHoleProjectile`) y LOS ANILLOS RÚNICOS de los soles (el emisor
  compartido `SigiloLib.SistemaAnillos`, tres aros de runas 6·8·10 que
  giran, se inclinan y CORREN al comer) aprisionan al jefe (0-60), lo
  elevan al CENTRO DE LA PANTALLA (60-150, el motor de la .110
  intacto)… y **EL LIBRO ABSORBE**: el Eterno sale de la mano del
  portador, CRECE ×2,6, cuelga DEBAJO del preso, se ABRE (el párpado
  Medio, el iris CLAVADO en su comida), come desde 195 (las almas y la
  bruma corren a SU boca — ya no al jugador —, la herida roja, el
  masticar), los anillos ENTEROS colapsan DENTRO del libro en el trago
  (345-430) y a los 400 **SE CIERRA de golpe** (párpado Cerrado +
  chasquido + onda de choque) y VUELVE a la mano en el poso. La sombra
  de la página MURIÓ en la cena: el devorador es el libro.
- **EL TECHO DE LOS OJOS**: los minions «sin límite» eran un BUG — el
  techo vivo de la .111 (la promesa de no cullar lo vivo) subía
  `maxMinions` con cada ojo vivo, y la compuerta de invocación leía
  ESE número: JAMÁS se cerraba. Ahora `WeaponScaling.TechoOjosEterno`:
  **ESCUADRÓN DE 20 + el equipo de invocador (lo que armadura y
  accesorios sumen más allá de la base) + la escalera por nivel** (+1
  cada 10, tope +10 — nivel 100 con equipo completo: ~34). El techo se
  cachea por tick (`ShardPlayer._techoOjos`) y la compuerta del Eterno
  lee `ShardPlayer.TechoOjosDe` — ya NO lee `maxMinions`. La promesa
  de la .111 SIGUE EN PIE: lo ya invocado jamás se culla (el techo
  nuevo es un PISO para `maxMinions`, nunca un recorte de lo vivo).
- **LA ÓRBITA Y EL RETROCESO**: los anillos alrededor del grimorio en
  fase de caza ahora CABECEN (precesión ±0,30 rad, contrarrotando
  par/impar — la rueda viva se lee en tres dimensiones, como Saturno) y
  cada ojo deja ESTELA (tres fantasmas ámbar en sus posiciones pasadas
  del anillo, deterministas). Y el disparo tiene su PEQUEÑO RETROCESO
  de verdad: patada 5,6 + estado de 14 ticks que la ley del planeo
  RESPETA 8 ticks (la patada de 3,2 de la .109 moría en 2-3 t y no se
  veía) + pop de escala 1,16 y desplazamiento de 6,5 px en la
  dirección de la patada — el ojo REBOTA y la formación lo reincorpora.

**VERIFICACIÓN**: oráculo 0/0 (79 refs, 311 .cs) · build real 0/0 ·
.tmod 2.622.071 B md5 887331145f60e46a4cba2282d99a6b43 (390 entradas,
EOF exacto) · headless 0 excepciones · `audit_v650114.py` TODO OK
(237 ✓): heap/ldstr/constantes .104–.114, el árbol con las 4 piezas +
la regresión .110–.113 ÍNTEGRA, hjson ×3 sin cambios, los iconos del
usuario SIN cambios (PNG crudos byte a byte + rawimg píxel a píxel).

"""


def changes():
    p = "CHANGES.md"
    s = open(p, encoding="utf-8").read()
    s = s.replace("# AethonMod — Historial de Cambios\n\n", "", 1)
    s = CHANGES + s
    open(p, "w", encoding="utf-8").write(s)
    assert "v6.50.114 — EL DAÑO MASIVO TAMBIÉN CENA" in s
    print("CHANGES.md OK")


HEADER_114 = """> Última actualización: v6.50.114 (EL DAÑO MASIVO TAMBIÉN CENA,
> LA PELÍCULA DEL LIBRO Y EL TECHO DE LOS OJOS. La letra: «cuando el
> daño que recibe el jefe es masivo no se aplica eso de que su primera
> muerte sea absorbido por el grimorio» + «el jefe debe ser rodeado por
> anillos iguales a los de los proyectiles agujeros negro y además
> también poner anillos como los de los soles runicos, ser llevado al
> centro de la pantalla y ser absorbido por el libro… esto se aplica a
> todos los jefes» + «al parecer los minion no tienen limite…
> deberían tener un limite de 10, pero actualiza a que tengan un limite
> de 20 con el libro y más con items y armadura de invocador» + «mejora
> la animación… alrededor del grimorio en su fase de caza… al atacar
> tenga un pequeño retroceso»). Cuatro entregas: (1) EL FIX MASIVO —
> el gatillo de PreAI exigía vida > 0 y el golpe que cruzaba a ≤0 sin
> pasar por las 100 le robaba la cena al portador; ahora CheckDead
> intercepta la muerte de un golpe ANTES de la marca (la cena tiene
> prioridad sobre el festín clásico) e Iniciar ENTREGA a la cena en el
> drain masivo — la primera muerte ES la cena, venga como venga.
> (2) LA PELÍCULA NUEVA — TRES anillos de FOTONES (Ring + aberración
> cromática RGB + aro blanco caliente, el patrón exacto del
> BlackHoleProjectile) + LOS ANILLOS RÚNICOS de los soles
> (SigiloLib.SistemaAnillos) aprisionan, elevan al centro (motor .110
> intacto) y EL LIBRO ABSORBE: el Eterno sale de la mano, CRECE ×2,6,
> cuelga bajo el preso, se ABRE (párpado Medio, iris clavado), come
> (almas y bruma a SU boca), los anillos colapsan DENTRO del libro y a
> los 400 SE CIERRA de golpe (Cerrado + chasquido + onda) y VUELVE a
> la mano — la sombra de la página murió en la cena. (3) EL TECHO DE
> LOS OJOS — el bug era el techo vivo .111 (cada ojo vivo subía
> maxMinions → la compuerta jamás se cerraba); ahora
> WeaponScaling.TechoOjosEterno (20 + equipo de invocador + escalera
> por nivel tope +10), cacheado por tick en ShardPlayer._techoOjos, y
> la compuerta lee ShardPlayer.TechoOjosDe (NO maxMinions) — la
> promesa .111 sigue en pie (el techo nuevo es PISO). (4) LA ÓRBITA Y
> EL RETROCESO — precesión ±0,30 rad (Saturno, contrarrotante) +
> estela de tres fantasmas + patada 5,6 con estado de 14 t que la ley
> del planeo respeta 8 t (la de 3,2 moría en 2-3 t) + pop 1,16 y
> desplazamiento 6,5 px. .tmod 2.622.071 B md5
> 887331145f60e46a4cba2282d99a6b43, 390 entradas, oráculo 0/0, build
> real 0/0, headless 0 excepciones, audit_v650114 TODO OK (237 ✓).
> PENDIENTE EN JUEGO: (a) matar a un jefe de UN golpe con daño
> masivo → LA PRIMERA CENA arranca igual (el fix), (b) los anillos de
> fotones + los rúnicos alrededor del preso, elevado al centro,
> absorbido POR EL LIBRO (se cierra de golpe a los ~6,6 s y vuelve a
> la mano), (c) el tope: 20 ojos con el libro (más con equipo de
> invocador y nivel), la compuerta avisa «Espacios de esbirro llenos»,
> (d) la órbita cabecea y deja estela, (e) cada disparo REBOTA al ojo,
> (f) Página en Blanco reinicia la cena de un tipo ya devorado).
"""


def snapshot():
    p = "STABLE-SNAPSHOT.md"
    lineas = open(p, encoding="utf-8").read().splitlines(keepends=True)

    # ---------- 1) EL HEADER: título .114 + bloques .114/.113/.112 ----------
    i_b113 = next(i for i, l in enumerate(lineas)
                  if l.startswith("> Última actualización: v6.50.113 ("))
    i_b112 = next(i for i, l in enumerate(lineas)
                  if l.startswith("> Última actualización: v6.50.112 ("))
    i_b111 = next(i for i, l in enumerate(lineas)
                  if l.startswith("> Última actualización: v6.50.111 ("))
    bloque_113 = "".join(lineas[i_b113:i_b112])
    bloque_112 = "".join(lineas[i_b112:i_b111])
    # el fin del bloque .111: la primera línea que NO empieza con '>' tras i_b111
    i_fin111 = next(i for i in range(i_b111 + 1, len(lineas))
                    if not lineas[i].startswith(">"))
    header_nuevo = [
        "# AethonMod — ESTADO ACTUAL (v6.50.114)\n",
        "\n",
        "> **Este documento = \"¿por dónde nos quedamos?\"** — se actualiza en cada entrega.\n",
        HEADER_114,
        bloque_113,
        bloque_112,
    ]
    lineas = header_nuevo + lineas[i_fin111:]
    s = "".join(lineas)
    assert "ESTADO ACTUAL (v6.50.114)" in s
    assert s.count("Última actualización: v6.50.114") == 1
    assert "Última actualización: v6.50.111" not in s.split("## ✅ ESTADO VERIFICADO")[0]
    print("  header OK (bloques .114/.113/.112 — la .111 salió)")

    # ---------- 2) LA FILA DEL HISTORIAL (antes de la fila de la .113) ----------
    ancla = "| **v6.50.113** |"
    i = s.find(ancla)
    assert i > 0, "no encontré la fila .113 del HISTORIAL"
    s = s[:i] + FILA_114 + s[i:]
    assert s.count("| **v6.50.114** |") == 1
    print("  fila HISTORIAL OK")

    # ---------- 3) PRÓXIMOS: doc-rot POR LÍNEAS con anclas por versión ----------
    # 3a. PODAR la .110 (de su línea de inicio a la línea previa a la .100)
    lin = s.splitlines(keepends=True)
    i_110 = next(i for i, l in enumerate(lin)
                 if l.startswith("4. **✅ v6.50.110 PUBLICADA"))
    i_100 = next(i for i, l in enumerate(lin)
                 if l.startswith("1. **✅ v6.50.100 PUBLICADA"))
    assert i_110 < i_100, "la .110 debería estar antes que la .100"
    del lin[i_110:i_100]
    s = "".join(lin)
    assert "v6.50.110 PUBLICADA" not in s
    print("  .110 PODADA de PRÓXIMOS")

    # 3b. RENUMERAR (del 3 hacia arriba — anclas únicas por versión)
    for viejo, nuevo, version in [
        ("3. **✅ v6.50.111 PUBLICADA", "4. **✅ v6.50.111 PUBLICADA", ".111"),
        ("2. **✅ v6.50.112 PUBLICADA", "3. **✅ v6.50.112 PUBLICADA", ".112"),
        ("1. **✅ v6.50.113 PUBLICADA", "2. **✅ v6.50.113 PUBLICADA", ".113"),
    ]:
        assert s.count(viejo) == 1, f"ancla no única para {version}: {viejo!r}"
        s = s.replace(viejo, nuevo, 1)
    print("  renumeración .113→2 · .112→3 · .111→4")

    # 3c. INSERTAR la .114 como item 1 (tras el header de PRÓXIMOS)
    ancla = "## 🚧 PRÓXIMOS PASOS SUGERIDOS (en orden)\n\n"
    i = s.find(ancla)
    assert i > 0
    i += len(ancla)
    s = s[:i] + ITEM_114 + s[i:]
    assert s.count("v6.50.114 PUBLICADA — EL DAÑO MASIVO") == 1
    print("  item 1 (.114) insertado")

    open(p, "w", encoding="utf-8").write(s)
    print("STABLE-SNAPSHOT.md OK")


FILA_114 = """| **v6.50.114** | ✅ Build-verificada (oráculo 0/0 sobre 311 .cs con 79 refs, build real 0/0, .tmod 2.622.071 B md5 887331145f60e46a4cba2282d99a6b43, 390 entradas (los mismos nombres de la .113: solo cambia código), EOF exacto, heap/ldstr/constantes .104–.114 (TechoOjosEterno/TechoOjosDe/_techoOjos/AnilloFotones/DibujarElLibro/T_CIERRE/anclaLibro/_retroceso VIVOS; los ldstr viejos « slot(s) de minion (máx +10)]» y los de la cena de la sombra MUERTOS; las constantes 195/400/5,6/14/2,6/0,30 VIVAS en el IL), el árbol con las 4 piezas + la regresión .110–.113 ÍNTEGRA (la compuerta vieja `int maxMinions = player.maxMinions;` MUERTA, el `+= BonusMinionSlots` MUERTO, la patada 3,2 MUERTA, ColumnaViva/CabezaDeBruma/SigiloErosionado MUERTOS en la cena, el plano 0.01 y el decimal "0.0" VIVOS), hjson ×3 sin cambios (es-MX byte a byte), iconos del usuario SIN cambios (PNG crudos byte a byte + rawimg píxel a píxel), headless «Sandboxing v6.50.114 → Adding Content → Server started» 0 excepciones (serverconfig114, mundo cena114, puerto 7783)), ✔ PUBLICADA (release 411803422 + asset AethonMod.tmod 637370231 2.622.071 B, CDN verificado byte a byte md5 887331145f60e46a4cba2282d99a6b43, /releases/latest = v6.50.114, respaldo /home/sync/AethonMod-v6.50.114.tmod), ⏳ en juego | LA LETRA (2026-10-13): «cuando el daño que recibe el jefe es masivo no se aplica eso de que su primera muerte sea absorbido por el grimorio» + «el jefe debe ser rodeado por anillos iguales a los de los proyectiles agujeros negro y además también poner anillos como los de los soles runicos, ser llevado al centro de la pantalla y ser absorbido por el libro… esto se aplica a todos los jefes» + «al parecer los minion no tienen limite… deberían tener un limite de 10, pero actualiza a que tengan un limite de 20 con el libro y más con items y armadura de invocador, además mejora las animación… alrededor del grimorio en su fase de caza y… al atacar tenga un pequeño retroceso». CUATRO ENTREGAS: (1) EL FIX MASIVO — el gatillo de la cena en PreAI exigía vida>0: el golpe que cruzaba de lleno a ≤0 robaba la cena; AHORA CheckDead intercepta la muerte de un golpe ANTES de la marca (la cena tiene prioridad sobre el festín) e Iniciar ENTREGA a la cena en el drain masivo — la primera muerte ES la cena venga como venga. (2) LA PELÍCULA NUEVA — TRES anillos de FOTONES (Ring+aberración RGB+aro blanco del BlackHoleProjectile) + ANILLOS RÚNICOS (SigiloLib.SistemaAnillos) aprisionan/elevan (motor .110 intacto) y EL LIBRO ABSORBE: sale de la mano, crece ×2,6, cuelga bajo el preso, se abre (Medio+iris clavado), come desde 195 (almas/bruma a SU boca), colapsa los anillos DENTRO y a los 400 SE CIERRA (Cerrado+chasquido+onda) y VUELVE — la sombra murió en la cena. (3) EL TECHO DE LOS OJOS — el bug era el techo vivo .111 (cada ojo vivo subía maxMinions → compuerta jamás cerrada); ahora TechoOjosEterno (20+equipo+nivel tope +10) cacheado por tick y la compuerta lee TechoOjosDe (NO maxMinions) — la promesa .111 intacta (techo = PISO). (4) ÓRBITA+RETROCESO — precesión ±0,30 rad contrarrotante + estela de 3 fantasmas + patada 5,6/estado 14 t respetado 8 t por la ley del planeo + pop 1,16 y 6,5 px. LECCIONES: (a) un gatillo de estado vitado en un hook con condición de vida>0 NUNCA atrapa las muertes de un golpe — el interceptor de la MUERTE vive en CheckDead; (b) un «techo vivo» que protege lo existente JAMÁS debe ser el mismo número que la compuerta de entrada (runaway por construcción): la protección y el límite son DOS números; (c) los strings interpolados de C# viajan PARTIDOS en el DLL (InterpolatedStringHandler) — auditar por TROZOS literales, jamás por el formato {0} completo; (d) --data-binary SIN @ sube la RUTA como cuerpo (asset de 60 B) — siempre @archivo |
"""

ITEM_114 = """1. **✅ v6.50.114 PUBLICADA — EL DAÑO MASIVO TAMBIÉN CENA, LA
   PELÍCULA DEL LIBRO Y EL TECHO DE LOS OJOS** (la letra: «cuando el
   daño que recibe el jefe es masivo no se aplica eso de que su
   primera muerte sea absorbido por el grimorio» + «anillos iguales a
   los de los proyectiles agujeros negro y además… como los de los
   soles runicos, ser llevado al centro de la pantalla y ser
   absorbido por el libro» + «deberían tener un limite de 10, pero…
   limite de 20 con el libro y más con items y armadura de invocador»
   + «mejora la animación… alrededor del grimorio… al atacar tenga un
   pequeño retroceso»). (1) CheckDead intercepta la muerte de un golpe
   ANTES de la marca + Iniciar entrega a la cena — la primera muerte
   ES la cena. (2) Anillos de fotones + rúnicos aprisionan, elevan y
   EL LIBRO absorbe (crece ×2,6, se abre, come, SE CIERRA de golpe a
   los 400 y vuelve a la mano — la sombra murió en la cena). (3) El
   techo: ESCUADRÓN 20 + equipo de invocador + nivel (la compuerta ya
   no lee maxMinions — el runaway del techo vivo .111 muerto; la
   promesa .111 intacta). (4) Precesión Saturno + estela + el
   retroceso del disparo de verdad (patada 5,6, 14 t, pop 1,16).
   **PENDIENTE EN JUEGO**: (a) matar un jefe de UN golpe masivo → la
   cena arranca igual, (b) los anillos de fotones + rúnicos, el preso
   elevado al centro y EL LIBRO comiéndoselo (se cierra ~6,6 s y
   vuelve), (c) 20 ojos con el libro (más con equipo/nivel) y el
   aviso «Espacios de esbirro llenos», (d) la órbita cabecea con
   estela, (e) el rebote de cada disparo, (f) Página en Blanco
   reinicia la cena de un tipo ya devorado.

"""


def main():
    changes()
    snapshot()
    print("DOCS .114 OK (README intocado — legal-only)")


if __name__ == "__main__":
    main()

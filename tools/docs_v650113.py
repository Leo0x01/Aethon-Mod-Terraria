#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# docs_v650113.py — CHANGES entrada .113 + README 6.50.113 + STABLE-SNAPSHOT
# (header con el doc-rot de los bloques Última actualización .113/.112/.111
# — la .110 SALE — + fila HISTORIAL + item 1 de PRÓXIMOS con renumeración
# POR LÍNEAS: .112→2, .111→3, .110→4 y la .109 PODADA — la lección .110:
# JAMÁS regex sobre «\nN. **», se confunde con las sublistas internas).
import re

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/"

CHANGES = """# AethonMod — Historial de Cambios

## Commit v6.50.113 — LA SOMBRA COME PLANO, EL MEDIDOR CON DECIMALES Y LOS TRES ICONOS DEL USUARIO

**La letra**: «si el libro se alimenta por si solo, cada muerte que el
libro haga por si mismo debe reducir 1 de hambre, actualmente solo lo
hace en 0%» + «tambien debes permitir que visualizar el medidor de
hambre tenga decimales» + «el icono que debes usar para el buff cuando
sale el minion es este, icono de minion.txt / tambien para el icono del
mod o sea lo que se ve cuando instala el mod es este, icono del
mod.txt / ahora para el icono de la mascota de luz es este icono de
mascota de luz.txt».

**EL CAMBIO**: tres entregas — el festín del autóctono, el decimal del
medidor y el arte del usuario:

- **EL PLANO DEL AUTÓCTONO** (la sombra come plano): `ReduccionHambre`
  — el camino del festín autónomo (`AbsorberPresa`: la sombra de la
  página devora presas ENTERAS cuando el libro sale a cazar solo) —
  devolvía la dieta por rareza de la .104 (`HambrePorPresa`: 1★=0,1% —
  la chusma común no movía NI un entero del medidor: «solo lo hace en
  0%»); ahora devuelve `0.01f` PLANO: **cada muerte que el libro hace
  POR SÍ MISMO baja 1% de hambre**, Eterno y Nervioso por igual. La
  dieta por rareza SIGUE VIVA en el otro camino (la baja acreditada del
  jugador: `GlobalNPCXP.OnKill` → `HambrePorPresa` — la letra de la
  .104 no se toca ahí): la sombra del libro come plano, el juego del
  jugador sigue cobrando por rareza.
- **EL MEDIDOR CON DECIMALES**: la barra del tooltip
  (`GrimorioHambriento`) y la voz de la Esencia de Hambre usaban `(int)`
  TRUNCADOR (47,9% → «47%»); ahora `ToString("0.0")` — **UN decimal
  SIEMPRE visible** (47,3%), a juego barra y voz (con el plano de 1% en
  1% y el reloj de la hora añadiendo 0,1% cada ~22 s, el decimal se
  GANA).
- **LOS TRES ICONOS DEL USUARIO**: sus tres `SPRITE_EXACTO` 2048×2048
  (base64 → tuplas de 5 bytes n,R,G,B,A — runs de 255 máx) decodificados
  y reducidos con el filtro que eligió el VLM (NEAREST los 32×32,
  LANCZOS el 80×80): `OjoEternoBuff.png` 32×32 (el buff del minion),
  `AethonMenorBuff.png` 32×32 (el buff de la mascota de luz),
  `icon.png` 80×80 (EL ICONO DEL MOD — el que se ve en el menú Mods al
  instalarlo; el viejo era de la .13: 924 B) y su hermano pequeño
  `icon_small.png` 30×30 (vistas compactas; el viejo seguía mostrando
  el arte viejo). DECOMPILE verificado (tML 2026.08.3.0):
  `ContentConverters` EXIME `icon.png` de la conversión rawimg (viaja
  PNG crudo) y `AddResource` consume el stream de `icon_small.png`
  comparándolo con la plantilla (ToRaw falla → también viaja PNG
  crudo); `ModLoader.GetModIcon` exige 80×80/30×30 EXACTOS («icon.png
  is not 80x80» si no).

**VERIFICACIÓN**: audit_texturas 0 · oráculo /home/z/.verify 0/0
(regenerado con `tools/regen_oracle.py`: TODAS las DLLs gestionadas de
Libraries/** + tModLoader.dll, menos tModCodeAssist/tModPorter/Tests/
Native/runtimes — 79 refs, 311 .cs) · build real 0/0 → .tmod 2.621.290
B · headless serverconfig113 (seed iconos113, autocreate, puerto 7782):
«Sandboxing v6.50.113 → Adding Content → Adding Recipes → world gen →
Server started» 0 excepciones · audit_v650113 TODO OK (190 ✓): 390
entradas (los mismos nombres de la .112 — los iconos son REEMPLAZOS),
EOF exacto, `icon.png`/`icon_small.png` PNG crudos byte a byte +
80×80/30×30 EXACTOS, las dos rawimg de buff píxel-idénticas al árbol
nuevo, el plano (`return 0.01f` en ReduccionHambre;
`HambrePorPresa(presa)` AUSENTE de la sombra pero VIVO en OnKill), el
decimal (`0.0` en barra y voz, los `(int)` truncadores MUERTOS) + la
regresión .104–.112 ÍNTEGRA.

**PUBLICACIÓN**: release 410912156 + asset AethonMod.tmod 634680327
2.621.290 B (md5 b627b56a…), CDN verificado byte a byte,
/releases/latest = v6.50.113.

"""

HEADER_113 = """> Última actualización: v6.50.113 (LA SOMBRA COME PLANO, EL
> MEDIDOR CON DECIMALES Y LOS TRES ICONOS DEL USUARIO. La letra: «si el
> libro se alimenta por si solo, cada muerte que el libro haga por si
> mismo debe reducir 1 de hambre, actualmente solo lo hace en 0%» +
> «tambien debes permitir que visualizar el medidor de hambre tenga
> decimales» + «el icono que debes usar para el buff cuando sale el
> minion es este / para el icono del mod o sea lo que se ve cuando
> instala el mod es este / para el icono de la mascota de luz es este»).
> Tres entregas: (1) EL PLANO DEL AUTÓCTONO — ReduccionHambre (el camino
> de AbsorberPresa: la sombra de la página devora presas ENTERAS cuando
> el libro sale a cazar SOLO) devolvía la dieta por rareza de la .104
> (1★=0,1% — la chusma común no movía NI un entero: «solo lo hace en
> 0%»); ahora 0.01f PLANO — cada muerte del libro POR SÍ MISMO baja 1%
> de hambre (Eterno y Nervioso por igual); la dieta por rareza SIGUE en
> la baja acreditada del jugador (GlobalNPCXP.OnKill → HambrePorPresa).
> (2) EL MEDIDOR CON DECIMALES — la barra del tooltip y la voz de la
> Esencia usaban (int) truncador (47,9% → «47%»); ahora ToString("0.0")
> — un decimal SIEMPRE visible (47,3%), a juego barra y voz. (3) LOS
> TRES ICONOS DEL USUARIO — sus SPRITE_EXACTO 2048×2048 decodificados
> (base64 → tuplas 5B n,R,G,B,A) y reducidos con filtro VLM (NEAREST
> 32×32, LANCZOS 80×80): el buff del minion, el buff de la mascota de
> luz, EL ICONO DEL MOD en el menú Mods (80×80; el viejo era de la .13)
> y su hermano pequeño 30×30; decompile verificado: ContentConverters
> exime icon.png del rawimg (viaja PNG crudo) y AddResource consume el
> stream de icon_small.png (ToRaw falla → también PNG crudo);
> GetModIcon exige 80×80/30×30 exactos. .tmod 2.621.290 B
> md5 b627b56ac0bfb58e6bc051de5c6cdb62, 390 entradas, oráculo 0/0,
> build real 0/0, headless 0 excepciones, audit 190 ✓.)
"""

FILA_113 = (
    "| **v6.50.113** | ✅ Build-verificada (audit_texturas 0, oráculo 0/0 — regenerado con "
    "tools/regen_oracle.py: 79 refs TODAS las DLLs gestionadas + tModLoader.dll, 311 .cs —, "
    "build real 0/0, .tmod 2.621.290 B md5 b627b56ac0bfb58e6bc051de5c6cdb62, 390 entradas "
    "(los mismos nombres de la .112 — los cuatro .png son REEMPLAZOS), EOF exacto, hjson "
    "semántico == árbol ×3 SIN cambios (es-MX byte a byte), icon.png + icon_small.png PNG "
    "crudos byte a byte con 80×80/30×30 EXACTOS (GetModIcon), OjoEternoBuff.rawimg + "
    "AethonMenorBuff.rawimg 32×32 píxel-idénticas al árbol NUEVO, el plano vivo (return "
    "0.01f en ReduccionHambre; HambrePorPresa(presa) AUSENTE de la sombra, VIVO en "
    "GlobalNPCXP.OnKill), el decimal vivo (ToString(\"0.0\") en barra y voz; los (int) "
    "truncadores MUERTOS del árbol) + la regresión .104–.112 ÍNTEGRA (heap, ldstr, "
    "constantes del muelle, Ceiling, la cena, las tres ventanas), headless «Sandboxing "
    "v6.50.113 → Adding Content → Server started» 0 excepciones), ✔ PUBLICADA (release "
    "410912156 + asset AethonMod.tmod 634680327 2.621.290 B, CDN verificado byte a byte "
    "md5 b627b56a…, /releases/latest = v6.50.113, respaldo /home/sync/AethonMod-"
    "v6.50.113.tmod), ⏳ en juego "
    "| LA LETRA (2026-10-12): «si el libro se alimenta por si solo, cada muerte que el "
    "libro haga por si mismo debe reducir 1 de hambre, actualmente solo lo hace en 0%» + "
    "«tambien debes permitir que visualizar el medidor de hambre tenga decimales» + «el "
    "icono que debes usar para el buff cuando sale el minion es este… / para el icono del "
    "mod o sea lo que se ve cuando instala el mod es este… / ahora para el icono de la "
    "mascota de luz es este…». TRES ENTREGAS: (1) EL PLANO DEL AUTÓCTONO — ReduccionHambre "
    "(AbsorberPresa: la sombra de la página devora presas enteras cuando el libro sale a "
    "cazar solo) devolvía la dieta por rareza de la .104 (1★=0,1% — la chusma común no "
    "movía ni un entero del medidor); ahora 0.01f PLANO — cada muerte del libro POR SÍ "
    "MISMO baja 1% (Eterno y Nervioso por igual); la dieta por rareza SIGUE VIVA en la "
    "baja acreditada del jugador (GlobalNPCXP.OnKill → HambrePorPresa — la .104 intacta). "
    "(2) EL MEDIDOR CON DECIMALES — la barra del tooltip (GrimorioHambriento) y la voz de "
    "la Esencia (EsenciaHambre) usaban (int) truncador; ahora ToString(\"0.0\") — un "
    "decimal SIEMPRE (47,3%), a juego barra y voz. (3) LOS TRES ICONOS DEL USUARIO — sus "
    "SPRITE_EXACTO 2048×2048 (base64 → tuplas 5B n,R,G,B,A) decodificados por tools/"
    "gen_iconos_v650113.py con filtro VLM (NEAREST 32×32, LANCZOS 80×80): OjoEternoBuff "
    "32×32, AethonMenorBuff 32×32, icon.png 80×80 (el menú Mods — el viejo era de la .13: "
    "924 B) y icon_small.png 30×30 (el viejo seguía mostrando el arte viejo); decompile "
    "verificado: ContentConverters EXIME icon.png del rawimg (viaja PNG crudo) y "
    "AddResource consume el stream de icon_small.png (ToRaw falla → PNG crudo); "
    "GetModIcon exige 80×80/30×30 EXACTOS. LECCIONES: (a) el sandbox AMANECIÓ REWIND-EADO "
    "otra vez (repo a v6.50.65 + /home/sync a la .99): la restauración fue fetch+reset "
    "desde GitHub (el .env con el token sobrevivió — la fuente de verdad es origin/main) "
    "y el oráculo se REGENERÓ con tools/regen_oracle.py (la lección de las DLLs sueltas: "
    "refs = TODAS menos tModCodeAssist/tModPorter/Tests/Native/runtimes); (b) los "
    "SPRITE_EXACTO del usuario traen SU PROPIO decodificador en el archivo (atob → tuplas "
    "5B n,R,G,B,A): el formato se lee del propio .txt, cero adivinación; (c) el mod icon "
    "de tML 2026.08: icon.png 80×80 EXACTOS (GetModIcon) viaja PNG crudo (exención de "
    "ContentConverters) y icon_small.png 30×30 viaja PNG crudo POR BUG SERENDÍPICO (el "
    "template-compare consume el stream → ToRaw falla → copia cruda) |\n"
)

ITEM_113 = """1. **✅ v6.50.113 PUBLICADA — LA SOMBRA COME PLANO, EL MEDIDOR CON
   DECIMALES Y LOS TRES ICONOS DEL USUARIO**
   — la letra: «si el libro se alimenta por si solo, cada muerte que el
   libro haga por si mismo debe reducir 1 de hambre, actualmente solo
   lo hace en 0%» + «tambien debes permitir que visualizar el medidor de
   hambre tenga decimales» + «el icono que debes usar para el buff
   cuando sale el minion es este / para el icono del mod o sea lo que
   se ve cuando instala el mod es este / para el icono de la mascota de
   luz es este».
   (1) EL PLANO DEL AUTÓCTONO: ReduccionHambre (el camino de
   AbsorberPresa — la sombra de la página devorando presas ENTERAS
   cuando el libro sale a cazar solo) devolvía la dieta por rareza de
   la .104 (1★=0,1% — la chusma común no movía NI un entero del
   medidor); ahora 0.01f PLANO — cada muerte del libro POR SÍ MISMO
   baja 1% de hambre (Eterno y Nervioso por igual); la dieta por
   rareza SIGUE VIVA en la baja acreditada del jugador
   (GlobalNPCXP.OnKill → HambrePorPresa — la .104 intacta ahí).
   (2) EL MEDIDOR CON DECIMALES: la barra del tooltip y la voz de la
   Esencia de Hambre usaban (int) truncador (47,9% → «47%»); ahora
   ToString("0.0") — UN decimal SIEMPRE visible (47,3%), a juego barra
   y voz.
   (3) LOS TRES ICONOS DEL USUARIO: sus SPRITE_EXACTO 2048×2048
   decodificados (traen su propio decodificador: atob → tuplas 5B
   n,R,G,B,A) y reducidos con filtro elegido por VLM: el buff del
   minion (OjoEternoBuff 32×32 NEAREST), el buff de la mascota de luz
   (AethonMenorBuff 32×32 NEAREST), EL ICONO DEL MOD (icon.png 80×80
   LANCZOS — el que se ve en el menú Mods al instalarlo; el viejo era
   de la .13) y su hermano pequeño (icon_small.png 30×30 — el viejo
   seguía mostrando el arte viejo).
   VERIFICACIÓN: audit_texturas 0 · oráculo 0/0 (regenerado con
   tools/regen_oracle.py — 79 refs) · build real 0/0 · headless 0
   excepciones · audit_v650113 TODO OK (190 ✓) — .tmod 2.621.290 B
   md5 b627b56ac0bfb58e6bc051de5c6cdb62, 390 entradas (los mismos
   nombres de la .112 — los iconos son REEMPLAZOS), EOF exacto, los
   PNG crudos byte a byte (80×80/30×30 EXACTOS), las rawimg
   píxel-idénticas, el plano y el decimal VIVOS + la regresión
   .104–.112 ÍNTEGRA. CHECKLIST en juego: (a) dejar que el libro salga
   a cazar solo (25% de hambre + quietud): cada presa que la sombra
   devora entera baja el hambre 1% PLANO y el texto «Absorbe: −1%
   hambre» — el medidor del tooltip SE MUEVE; (b) el medidor del
   tooltip y la voz de la Esencia muestran UN decimal (47,3%); (c) el
   buff del minion muestra EL ARTE NUEVO (32×32); (d) el buff de la
   mascota de luz muestra EL ARTE NUEVO (32×32); (e) el menú Mods
   muestra EL ICONO NUEVO del mod (80×80) — y las vistas compactas, el
   pequeño (30×30); (f) el ojo EN JUEGO no cambió (sigue siendo el
   sprite de siempre) ni el ítem de la mascota.
   PUBLICACIÓN: commit d13e113 + tag v6.50.113 + push · release
   **410912156** + asset AethonMod.tmod **634680327** 2.621.290 B (md5
   b627b56ac0bfb58e6bc051de5c6cdb62), CDN VERIFICADO BYTE A BYTE,
   /releases/latest = v6.50.113, respaldo /home/sync/AethonMod-v6.50.113.tmod.
"""


def changes():
    p = BASE + "CHANGES.md"
    s = open(p, encoding="utf-8").read()
    s = s.replace("# AethonMod — Historial de Cambios\n\n", "", 1)
    s = CHANGES + s
    open(p, "w", encoding="utf-8").write(s)
    assert s.count("## Commit v6.50.113") == 1
    print("CHANGES.md OK")


def readme():
    p = BASE + "README.md"
    s = open(p, encoding="utf-8").read()
    s = re.sub(r"\*\*Versión actual:\*\* 6\.50\.112",
               "**Versión actual:** 6.50.113", s, count=1)
    open(p, "w", encoding="utf-8").write(s)
    assert "6.50.113" in s
    print("README.md OK")


def snapshot():
    p = BASE + "STABLE-SNAPSHOT.md"
    lineas = open(p, encoding="utf-8").read().splitlines(keepends=True)

    # ---------- 1) EL HEADER: título .113 + bloques .113/.112/.111 ----------
    i_b112 = next(i for i, l in enumerate(lineas)
                  if l.startswith("> Última actualización: v6.50.112 ("))
    i_b111 = next(i for i, l in enumerate(lineas)
                  if l.startswith("> Última actualización: v6.50.111 ("))
    i_b110 = next(i for i, l in enumerate(lineas)
                  if l.startswith("> Última actualización: v6.50.110 ("))
    bloque_112 = "".join(lineas[i_b112:i_b111])
    bloque_111 = "".join(lineas[i_b111:i_b110])
    # el fin del bloque .110: la primera línea que NO empieza con '>' tras i_b110
    i_fin110 = next(i for i in range(i_b110 + 1, len(lineas))
                    if not lineas[i].startswith(">"))
    header_nuevo = [
        "# AethonMod — ESTADO ACTUAL (v6.50.113)\n",
        "\n",
        "> **Este documento = \"¿por dónde nos quedamos?\"** — se actualiza en cada entrega.\n",
        HEADER_113,
        bloque_112,
        bloque_111,
    ]
    lineas = header_nuevo + lineas[i_fin110:]
    s = "".join(lineas)
    assert "ESTADO ACTUAL (v6.50.113)" in s
    assert s.count("Última actualización: v6.50.113") == 1
    assert "Última actualización: v6.50.110" not in s.split("## ✅ ESTADO VERIFICADO")[0]
    print("  header OK (bloques .113/.112/.111 — la .110 salió)")

    # ---------- 2) LA FILA DEL HISTORIAL (antes de la fila de la .112) ----------
    ancla = "| **v6.50.112** |"
    i = s.find(ancla)
    assert i > 0, "no encontré la fila .112 del HISTORIAL"
    s = s[:i] + FILA_113 + s[i:]
    assert s.count("| **v6.50.113** |") == 1
    print("  fila HISTORIAL OK")

    # ---------- 3) PRÓXIMOS: doc-rot POR LÍNEAS con anclas por versión ----------
    # 3a. PODAR la .109 (de su línea de inicio a la línea previa a la .100)
    lin = s.splitlines(keepends=True)
    i_109 = next(i for i, l in enumerate(lin)
                 if l.startswith("4. **✅ v6.50.109 PUBLICADA"))
    i_100 = next(i for i, l in enumerate(lin)
                 if l.startswith("1. **✅ v6.50.100 PUBLICADA"))
    assert i_109 < i_100, "la .109 debería estar antes que la .100"
    del lin[i_109:i_100]
    s = "".join(lin)
    assert "v6.50.109 PUBLICADA" not in s
    print("  .109 PODADA de PRÓXIMOS")

    # 3b. RENUMERAR (del 3 hacia arriba — anclas únicas por versión)
    for viejo, nuevo, version in [
        ("3. **✅ v6.50.110 PUBLICADA", "4. **✅ v6.50.110 PUBLICADA", ".110"),
        ("2. **✅ v6.50.111 PUBLICADA", "3. **✅ v6.50.111 PUBLICADA", ".111"),
        ("1. **✅ v6.50.112 PUBLICADA", "2. **✅ v6.50.112 PUBLICADA", ".112"),
    ]:
        assert s.count(viejo) == 1, f"ancla no única para {version}: {viejo!r}"
        s = s.replace(viejo, nuevo, 1)
    print("  renumeración .112→2 · .111→3 · .110→4")

    # 3c. INSERTAR la .113 como item 1 (tras el header de PRÓXIMOS)
    ancla = "## 🚧 PRÓXIMOS PASOS SUGERIDOS (en orden)\n\n"
    i = s.find(ancla)
    assert i > 0
    i += len(ancla)
    s = s[:i] + ITEM_113 + s[i:]
    assert s.count("v6.50.113 PUBLICADA — LA SOMBRA COME PLANO") == 1
    print("  item 1 (.113) insertado")

    open(p, "w", encoding="utf-8").write(s)
    print("STABLE-SNAPSHOT.md OK")


def main():
    changes()
    readme()
    snapshot()
    print("DOCS .113 OK")


if __name__ == "__main__":
    main()

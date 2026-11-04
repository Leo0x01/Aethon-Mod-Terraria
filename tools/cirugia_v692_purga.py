#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=============================================================================
 v6.92 — LA PURGA DE LAS INVESTIGACIONES (cirugía 1).

 La letra del usuario: «El relicario lo puedes borrar y también puedes
 borrar los grilletes de andromeda» + «limpia todo el mod de referencias
 externas y contenido con derechos de autor… todo debe ser de nuestra
 propiedad exclusiva».

 Esta cirugía:
   1. es-ES hjson — borra los bloques de RelicarioDelAvispon,
      GrilletesDeAndromeda, RelicarioCdBuff y el bloque Keybinds (la tecla
      del relicario muere con su arma); borra las líneas de
      CampoAndromedaProj.DisplayName; reescribe los textos con menciones
      externas (Heroic Weapons, MEAC/XiHe, sprite-gen, pixelfx, la lista
      de mods de la bolsa de investigaciones).
   2. AethonMod.cs — arranca el registro del keybind RelicarioSolar
      (Load) y su liberación (Unload).
   3. BolsasCategorias.cs — saca el Relicario y los Grilletes de la Bolsa
      de las Investigaciones y limpia los comentarios que nombraban mods
      ajenos.

 Después: python3 tools/sync_es_mx_v6564.py (los espejos se regeneran).
=============================================================================
"""
import os
import sys

BASE = os.path.join(os.path.dirname(__file__), "..", "AethonMod")
HJSON = os.path.join(BASE, "Localization", "es-ES_Mods.AethonMod.hjson")
CS_MOD = os.path.join(BASE, "AethonMod.cs")
CS_BOLSAS = os.path.join(BASE, "Content", "Items", "Bolsas", "BolsasCategorias.cs")


def leer(ruta):
    with open(ruta, encoding="utf-8") as f:
        return f.read().split("\n")


def escribir(ruta, lineas):
    with open(ruta, "w", encoding="utf-8") as f:
        f.write("\n".join(lineas))


def borrar_bloque(lineas, cabecera, fin):
    """Borra el bloque que empieza en una línea == cabecera y termina en la
    primera línea == fin (más la línea en blanco que lo sigue, si hay)."""
    out, i, borrados = [], 0, 0
    while i < len(lineas):
        if lineas[i] == cabecera:
            j = i + 1
            while j < len(lineas) and lineas[j] != fin:
                j += 1
            assert j < len(lineas), f"bloque sin cierre: {cabecera}"
            i = j + 1
            if i < len(lineas) and lineas[i].strip() == "":
                i += 1
            borrados += 1
            continue
        out.append(lineas[i])
        i += 1
    assert borrados == 1, f"cabecera {cabecera!r} encontrada {borrados} veces"
    return out


def borrar_lineas_con(lineas, trozo, veces=1):
    out = [l for l in lineas if trozo not in l]
    assert len(lineas) - len(out) == veces, (
        f"{trozo!r}: esperaba {veces} líneas, había {len(lineas) - len(out)}")
    return out


def reemplazar_texto(lineas, viejo, nuevo, veces=1):
    out, vistos = [], 0
    for l in lineas:
        if viejo in l:
            l = l.replace(viejo, nuevo)
            vistos += 1
        out.append(l)
    assert vistos == veces, f"{viejo!r}: esperaba {veces}, hallé {vistos}"
    return out


def poner_tooltip(lineas, clave, nuevo_cuerpo):
    """Reescribe el Tooltip: ''' … ''' de la entrada Items.<clave>."""
    cab = f"\t{clave}: {{"
    i = next((k for k, l in enumerate(lineas) if l == cab), None)
    assert i is not None, f"falta la entrada {clave}"
    j = next((k for k in range(i, len(lineas)) if lines_strip(lineas[k]) == "Tooltip:"), None)
    assert j is not None and j < i + 6, f"{clave}: sin Tooltip cerca"
    # la sangría de las líneas de contenido = la del ''' de apertura
    k = j
    while "'''" not in lineas[k]:
        k += 1
    sangria = lineas[k][: len(lineas[k]) - len(lineas[k].lstrip())]
    k += 1  # primera línea de contenido
    while "'''" not in lineas[k]:
        k += 1
    # k apunta al ''' de cierre
    return lineas[:j + 1] + [sangria + "'''"] + \
        [sangria + l for l in nuevo_cuerpo] + [sangria + "'''"] + lineas[k + 1:]


def lines_strip(l):
    return l.strip()


def cirugia_hjson():
    l = leer(HJSON)
    # 1) bloques muertos
    l = borrar_bloque(l, "\tRelicarioDelAvispon: {", "\t}")
    l = borrar_bloque(l, "\tGrilletesDeAndromeda: {", "\t}")
    l = borrar_bloque(l, "\tRelicarioCdBuff: {", "\t}")
    l = borrar_bloque(l, "Keybinds: {", "}")
    # 2) líneas de DisplayName del proyectil muerto (comentadas y vivas)
    l = borrar_lineas_con(l, "CampoAndromedaProj.DisplayName", veces=2)
    # 3) menciones externas en textos vivos
    l = reemplazar_texto(
        l, "✦ El altar de la consagración (investigación: Heroic Weapons)",
        "✦ El altar de la consagración del Eterno")
    l = reemplazar_texto(
        l, "vuela a tu lado (idle y vuelo animados — el pipeline sprite-gen).",
        "vuela a tu lado (idle y vuelo animados, nacidos en el Taller).")
    l = reemplazar_texto(
        l, "R3 — el sol de XiHe (MEAC): un minion con núcleo térmico y 2 anillos",
        "R3 — el sol en miniatura: un minion con núcleo térmico y 2 anillos")
    l = reemplazar_texto(l, " (Taller de efectos: pixelfx)", " (Taller de efectos)", veces=3)
    l = reemplazar_texto(
        l, "// v6.90 — EL TALLER DE EFECTOS (pixelfx + sprite-gen).",
        "// v6.90 — EL TALLER DE EFECTOS (el motor de píxeles de la casa).")
    # 4) tooltips reescritos de raíz
    l = poner_tooltip(l, "BolsaInvestigaciones", [
        "Lo que nació de las INVESTIGACIONES de la casa: primero los",
        "dos CLICKERS de la v6.82 (El Compás Rúnico y la Furia Solar),",
        "y de ahí en adelante TODO lo que nazca de estudiar el arte",
        "de los efectos: armas, altares y códices del Eterno.",
    ])
    l = poner_tooltip(l, "BolsaTallerDeEfectos", [
        "Lo que nació del Taller de Efectos de la casa: el sigilo con",
        "su acto, las llamas de ruido fractal, el eclipse de corona",
        "viva y los sprites animados. Armas de PRUEBA: 0 maná.",
    ])
    escribir(HJSON, l)
    print(f"OK hjson es-ES: {len(l)} líneas")


def cirugia_cs_mod():
    l = leer(CS_MOD)
    viejo = [
        "            // v6.85 — LA TECLA DEL RELICARIO SOLAR (la investigación R1:",
        "            // el aura del Bombus Apis — el original usa la tecla L). La",
        "            // referencia del informe: KeybindLoader.RegisterKeybind.",
        "            // v6.91 — OJO: REGISTRAR NO ES ASIGNAR (la letra: «dice",
        "            // tocar L pero al tocar no hace nada»): tML guarda la \"L\"",
        "            // como DefaultBinding pero JAMÁS la escribe en el perfil",
        "            // con el que juegas — la auto-asignación al entrar al mundo",
        "            // vive en RelicarioTeclaSistema (RelicarioDelAvispon.cs).",
        "            Content.Items.Investigaciones.RelicarioPlayer.TeclaRelicario =",
        "                KeybindLoader.RegisterKeybind(this, \"RelicarioSolar\", \"L\");",
    ]
    nuevo = [
        "            // v6.92 — LA PURGA DE LAS INVESTIGACIONES: el Relicario y",
        "            // los Grilletes de Andrómeda dejaron el mod por voluntad del",
        "            // usuario — su keybind murió con ellos (aquí no se registra",
        "            // nada: el mod ya NO tiene teclas propias).",
    ]
    texto = "\n".join(l)
    assert "\n".join(viejo) in texto, "AethonMod.cs: no encuentro el bloque Load del keybind"
    texto = texto.replace("\n".join(viejo), "\n".join(nuevo))
    viejo2 = [
        "            // v6.85 — LA TECLA DEL RELICARIO se suelta en la descarga (el",
        "            // barrendero cubre Asset/Texture2D; los ModKeybind se anulan",
        "            // a mano — son anclas de UI).",
        "            try { Content.Items.Investigaciones.RelicarioPlayer.TeclaRelicario = null; }",
        "            catch { }",
        "",
        "",
    ]
    assert "\n".join(viejo2) in texto, "AethonMod.cs: no encuentro el bloque Unload del keybind"
    texto = texto.replace("\n".join(viejo2), "", 1)
    escribir(CS_MOD, texto.split("\n"))
    print("OK AethonMod.cs: keybind arrancado (Load + Unload)")


def cirugia_bolsas():
    l = leer(CS_BOLSAS)
    texto = "\n".join(l)

    def sustituir(viejo, nuevo):
        nonlocal texto
        assert viejo in texto, f"BolsasCategorias.cs: falta:\n{viejo[:120]}"
        assert texto.count(viejo) == 1, f"BolsasCategorias.cs: repetido:\n{viejo[:120]}"
        texto = texto.replace(viejo, nuevo)

    # 1) el encabezado de la bolsa de investigaciones (la lista de mods ajenos)
    sustituir(
        "    //  nazca de estudiar otros mods (Bombus Apis, VFX+, MEAC, Boss\n"
        "    //  Checklist, Heroic Weapons, Umbra/Path of Terraria, Aureum Fang,\n"
        "    //  Solyn, los Grilletes de Andrómeda…).",
        "    //  nazca de las investigaciones de la casa (estudiar el arte de los\n"
        "    //  efectos, las ideas de la comunidad y nuestras propias obsesiones).")

    # 2) el contenido: mueren el Relicario (R1) y los Grilletes (R9)
    sustituir(
        "            // v6.85 — LOS CINCO FRUTOS DE LA SESIÓN (R1-R9):\n"
        "            // R1 — EL RELICARIO DEL AVISPÓN (Bombus Apis: el aura solar).\n"
        "            l.Add((ModContent.ItemType<Items.Investigaciones.RelicarioDelAvispon>(), 1));\n"
        "            // R8 — LA MIRADA DE SOLYN (el rayo de energía).\n"
        "            l.Add((ModContent.ItemType<Weapons.Investigaciones.MiradaSolyn>(), 1));\n"
        "            // R9 — LOS GRILLETES DE ANDRÓMEDA (el campo de fuerza).\n"
        "            l.Add((ModContent.ItemType<Items.Investigaciones.GrilletesDeAndromeda>(), 1));\n",
        "            // v6.85 — LOS FRUTOS DE LAS INVESTIGACIONES:\n"
        "            // R8 — LA MIRADA (el rayo de energía).\n"
        "            l.Add((ModContent.ItemType<Weapons.Investigaciones.MiradaSolyn>(), 1));\n")

    # 3) el comentario del altar
    sustituir(
        "            // v6.86 — R5 — EL ALTAR DE OFRENDAS (Heroic Weapons: el altar\n"
        "            // que devora lo que NO se necesita y NO está corrupto).",
        "            // v6.86 — R5 — EL ALTAR DE OFRENDAS (el altar que consagra\n"
        "            // la ofrenda por el Eterno).")

    # 4) el comentario del códice
    sustituir(
        "            //     v6.86: el Códice (Umbra/Path of Terraria).",
        "            //     v6.86: el Códice de habilidades.")

    # 5) el encabezado de la bolsa del taller
    sustituir(
        "    //  REELS (patch-gg/pixelfx: 244 efectos como código + el método\n"
        "    //  «borrador, render, mirar, refinar» · aldegad/sprite-gen: sprites\n"
        "    //  animados del taller). Sus frutos: las TRES armas del taller de",
        "    //  TALLER DE EFECTOS de la casa (el motor de píxeles propio + el\n"
        "    //  método «borrador, render, mirar, refinar» + los sprites\n"
        "    //  animados del taller). Sus frutos: las TRES armas del taller de")

    # 6) la nota de la bolsa del taller
    sustituir(
        '        protected override string Nota => "Lo que nació de estudiar los efectos vivos (pixelfx + sprite-gen)";',
        '        protected override string Nota => "Lo que nació de estudiar los efectos vivos (el motor de píxeles de la casa)";')

    # 7) los comentarios de las tres armas del taller + la mascota
    sustituir(
        "            // EL SIGILO DEL ETERNO (shadow/hex-sigil — el acto completo:\n"
        "            // trazo, glifos, destello, ascuas).",
        "            // EL SIGILO DEL ETERNO (el acto completo: trazo, glifos,\n"
        "            // destello, ascuas).")
    sustituir(
        "            // LAS LLAMAS DE SOMBRA (shadow/shadow-flames — la columna de\n"
        "            // fBm real que sube del suelo).",
        "            // LAS LLAMAS DE SOMBRA (la columna de fBm real que sube del\n"
        "            // suelo).")
    sustituir(
        "            // EL ECLIPSE VIVO (shadow/eclipse — la nova del disco negro\n"
        "            // de corona blanca).",
        "            // EL ECLIPSE VIVO (la nova del disco negro de corona blanca).")
    sustituir(
        "            // v6.91 — EL VÁSTAGO DEL ETERNO (la mascota del pipeline\n"
        "            // sprite-gen — la letra: «crea una mascota nueva usando\n"
        "            // esto: Reel 1 — aldegad/sprite-gen… la mascota minigrimorio\n"
        "            // y Aethon pueden ganar sus idle/walk animados desde una\n"
        "            // imagen base»): el librito alado con SUS CICLOS ANIMADOS.",
        "            // v6.91 — EL VÁSTAGO DEL ETERNO (la mascota del Taller — el\n"
        "            // librito alado con SUS CICLOS ANIMADOS: idle y vuelo desde\n"
        "            // una imagen base).")

    # 8) el comentario del cofre maestro (bolsa 27)
    sustituir(
        "            // v6.90 — LA BOLSA DEL TALLER DE EFECTOS (bolsa 27 del cofre):\n"
        "            // lo que nació de la investigación de los reels (pixelfx +\n"
        "            // sprite-gen — la letra: «crea una bolsa nueva y dale la bolsa\n"
        "            // al jugador, esta bolsa tendras una copia de todas las armas\n"
        "            // que sean creadas con esta investigacion»).",
        "            // v6.90 — LA BOLSA DEL TALLER DE EFECTOS (bolsa 27 del cofre):\n"
        "            // lo que nació del Taller de Efectos de la casa (la letra:\n"
        "            // «crea una bolsa nueva y dale la bolsa al jugador, esta bolsa\n"
        "            // tendras una copia de todas las armas que sean creadas con\n"
        "            // esta investigacion»).")

    escribir(CS_BOLSAS, texto.split("\n"))
    print("OK BolsasCategorias.cs: purga de contenido + menciones")


if __name__ == "__main__":
    print("CIRUGÍA v6.92 — LA PURGA:")
    cirugia_hjson()
    cirugia_cs_mod()
    cirugia_bolsas()
    print("PURGA COMPLETA — ahora: python3 tools/sync_es_mx_v6564.py")
    sys.exit(0)

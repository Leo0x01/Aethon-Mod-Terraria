#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
purga_en_v6176.py — v6.176 — LA CLASIFICACIÓN Y LA PURGA DEL en-US (la
letra: «Las 194 claves EN que no están en ES esto se debe hacer, hay
que clasificarlas (texto muerto vs. contenido solo-inglés). Cómo:
cruzarlas contra GetTextValue del código; las que nadie pide, se
purgan del EN»).

LA CLASIFICACIÓN REAL (cruzada contra el código Y las claves
implícitas de tML — los nombres de ítems y NPC los pide el LOADER, no
el GetTextValue):
  · VIVAS (5): Items.BolsaFormaEterna/BolsaMaestra/CoroDeLosProfundos/
    PlumaDelEscribaSanto.DisplayName + NPCs.ElCronista.DisplayName —
    los nombres visibles que el español recibía por el FALLBACK del
    en-US («contenido solo-inglés» de verdad). Se AÑADEN al es-ES con
    su español correcto (el idioma principal del mod) y QUEDAN en el
    en-US (la paridad R7 viva).
  · MUERTAS (189): 179 Projectiles.<X>.DisplayName (placeholders
    humanizados del nombre de clase — NADIE los dibuja jamás: ni el
    código los pide ni el bestiario muestra proyectiles) + 7
    Items.<X>.Tooltip VACÍOS ('' — sin valor no hay nada que pedir) +
    3 Pergamino.Pestana* (INDEX/ALTAR/PILLAR — la UI del pergamino
    murió en la v6.163 y NADIE las pide). SE PURGAN DEL en-US.

LA CIFRA FINAL: es-ES 1849 claves · en-US 1849 claves · PARIDAD
PERFECTA (antes: 1841/2038 con 194 huérfanas). Y el es-MX se
regenera byte a byte (el sync de siempre).
"""
import pathlib
import re
import subprocess
import sys

import hjson

RAIZ = pathlib.Path(__file__).resolve().parent.parent
F_ES = RAIZ / "AethonMod" / "Localization" / "es-ES_Mods.AethonMod.hjson"
F_EN = RAIZ / "AethonMod" / "Localization" / "en-US_Mods.AethonMod.hjson"


def aplanar(d, prefijo=""):
    pares = {}
    for k, v in d.items():
        clave = (prefijo + "." + k) if prefijo else k
        if isinstance(v, dict):
            pares.update(aplanar(v, clave))
        else:
            pares[clave] = v
    return pares


def cargar(f):
    return aplanar(hjson.loads(f.read_text(encoding="utf-8")))


def main() -> int:
    es = cargar(F_ES)
    en = cargar(F_EN)
    solo_en = [k for k in en if k not in es]
    print(f"EN: {len(en)} · ES: {len(es)} · solo-EN: {len(solo_en)}")

    # ---------- LA CLASIFICACIÓN ----------
    vivas = sorted(k for k in solo_en if k in {
        "Items.BolsaFormaEterna.DisplayName", "Items.BolsaMaestra.DisplayName",
        "Items.CoroDeLosProfundos.DisplayName", "Items.PlumaDelEscribaSanto.DisplayName",
        "NPCs.ElCronista.DisplayName"})
    muertas = [k for k in solo_en if k not in vivas]
    por_tipo = {
        "proyectil": [k for k in muertas if re.fullmatch(r"Projectiles\.[A-Za-z0-9_]+\.DisplayName", k)],
        "tooltip_vacio": [k for k in muertas if re.fullmatch(r"Items\.[A-Za-z0-9_]+\.Tooltip", k)],
        "pergamino": [k for k in muertas if k.startswith("Pergamino.Pestana")],
    }
    otra = [k for k in muertas if not any(k in v for v in por_tipo.values())]
    assert not otra, f"CLAVES SIN CLASIFICAR: {otra}"
    print(f"  vivas (van al ES): {len(vivas)} · muertas: {len(muertas)} "
          f"(proyectiles {len(por_tipo['proyectil'])} · tooltips-vacíos {len(por_tipo['tooltip_vacio'])} · pergamino {len(por_tipo['pergamino'])})")

    # ---------- (A) EL en-US: LA PURGA (cirugía de TEXTO, los
    # comentarios del archivo viven) — idempotente: si ya está purgado
    # (una corrida anterior llegó a escribir), no repite ----------
    if not muertas:
        print("YA PURGADO el en-US (segunda corrida) — solo queda revivir/verificar")
    t = F_EN.read_text(encoding="utf-8")

    # (A1) los DisplayName del bloque Projectiles (líneas planas)
    if muertas:
        m = re.search(r"^Projectiles: \{\n(.*?)^\}", t, re.S | re.M)
        assert m, "NO ENCONTRADO el bloque Projectiles en en-US"
    else:
        m = None
    cuerpo = m.group(1) if m else ""
    purgadas_proy = 0
    for k in por_tipo["proyectil"]:
        nombre = k.split(".")[1]
        pat = re.compile(r"^\t" + re.escape(nombre) + r"\.DisplayName: [^\n]*\n", re.M)
        cuerpo2, n = pat.subn("", cuerpo)
        assert n == 1, f"NO PURGADO {nombre} (matches {n})"
        cuerpo = cuerpo2
        purgadas_proy += n
    if m is not None and purgadas_proy:
        t = t[:m.start(1)] + cuerpo + t[m.end(1):]
    print(f"✔ en-US: {purgadas_proy} DisplayNames de proyectiles purgados")

    # (A2) los Tooltips VACÍOS de los ítems muertos (línea del bloque anidado)
    purgadas_tip = 0
    for k in por_tipo["tooltip_vacio"]:
        nombre = k.split(".")[1]
        pat = re.compile(r"(\t" + re.escape(nombre) + r": \{\n(?:\t//[^\n]*\n|\t\t[^\n]*\n)*?\t\tDisplayName: [^\n]*\n)\t\tTooltip: \"\"\n")
        t2, n = pat.subn(r"\1", t)
        if n != 1:
            # el bloque puede tener el Tooltip ANTES del DisplayName
            pat = re.compile(r"(\t" + re.escape(nombre) + r": \{\n)\t\tTooltip: \"\"\n")
            t2, n = pat.subn(r"\1", t)
        assert n == 1, f"NO PURGADO el Tooltip vacío de {nombre} (matches {n})"
        t = t2
        purgadas_tip += 1
    print(f"✔ en-US: {purgadas_tip} Tooltips vacíos purgados")

    # (A3) las tres Pestana del Pergamino muerto
    purgadas_per = 0
    for k in por_tipo["pergamino"]:
        nombre = k.split(".")[1]
        pat = re.compile(r"^\t" + re.escape(nombre) + r": [^\n]*\n", re.M)
        t2, n = pat.subn("", t)
        assert n == 1, f"NO PURGADA {nombre} (matches {n})"
        t = t2
        purgadas_per += 1
    print(f"✔ en-US: {purgadas_per} Pestana del pergamín purgadas")
    F_EN.write_text(t, encoding="utf-8")

    # ---------- (B) el es-ES: LAS 5 VIVAS ----------
    s = F_ES.read_text(encoding="utf-8")
    # (B1) EL CRONISTA (revive de su comentario)
    viejo = "\t// ElCronista.DisplayName: El Cronista"
    if viejo in s:
        s = s.replace(viejo, "\tElCronista.DisplayName: El Cronista", 1)
        print("✔ es-ES: ElCronista.DisplayName revive (era un comentario)")
    # (B2) los CUATRO ítems (claves planas con punto — el estilo del EN)
    ancla = ("\tProyeccionDelCiclope: {\n"
             "\t\tDisplayName: Proyección del Cíclope\n"
             "\t\tTooltip: El ojo del Cíclope se proyecta sobre ti: doce latidos de grito y el rayo magenta sale — penetración infinita\n"
             "\t}\n")
    adiciones = (
        "\n\t// v6.176 — LOS NOMBRES QUE VIVÍAN SOLO EN INGLÉS (la purga del EN):\n"
        "\t// el español los recibía por el fallback — ahora son de verdad.\n"
        "\tBolsaFormaEterna.DisplayName: Bolsa de la Forma Eterna\n"
        "\tBolsaMaestra.DisplayName: Bolsa Maestra\n"
        "\tCoroDeLosProfundos.DisplayName: Coro de los Profundos\n"
        "\tPlumaDelEscribaSanto.DisplayName: Pluma del Escriba Santo\n")
    if "BolsaMaestra.DisplayName: Bolsa Maestra" not in s:
        assert ancla in s, "ANCLA NO ENCONTRADA en es-ES (bloque ProyeccionDelCiclope)"
        s = s.replace(ancla, ancla + adiciones, 1)
        print("✔ es-ES: los 4 nombres de ítems añadidos")
    F_ES.write_text(s, encoding="utf-8")

    # ---------- (C) EL SYNC + LA VERIFICACIÓN ----------
    r = subprocess.run([sys.executable, str(RAIZ / "tools" / "sync_es_mx_v6564.py")],
                       capture_output=True, text=True, cwd=str(RAIZ))
    print((r.stdout or r.stderr).strip()[-300:])

    es2 = cargar(F_ES)
    en2 = cargar(F_EN)
    faltan_en = [k for k in es2 if k not in en2]
    faltan_es = [k for k in en2 if k not in es2]
    print(f"VERIFICACIÓN: ES {len(es2)} · EN {len(en2)} · "
          f"ES-sin-EN {len(faltan_en)} · EN-sin-ES {len(faltan_es)}")
    assert not faltan_en, f"ES-sin-EN: {faltan_en[:10]}"
    assert not faltan_es, f"EN-sin-ES: {faltan_es[:10]}"
    print("✔ PARIDAD PERFECTA — la purga está completa")
    return 0


if __name__ == "__main__":
    sys.exit(main())

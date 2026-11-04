#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=============================================================================
 v6.93 — LA CIRUGÍA DE LA MIRADA DEL ETERNO (hjson, cirugía de bytes).
=============================================================================
 LA MISIÓN: renombrar la localización del arma recreada (MiradaSolyn →
 MiradaDelEterno) en los DOS es-ES (la fuente única de la casa) SIN tocar
 una sola línea más — la edición a mano expandió los TABS del hjson a
 espacios y dejó un diff de 3.700 líneas de puro ruido blanco. Esta
 cirugía opera sobre los BYTES de HEAD: dos reemplazos quirúrgicos en el
 es-ES principal (el bloque del ítem y las claves de los proyectiles) y
 una extracción en el Escriba — con ASERCIÓN de que cada corte encontró
 exactamente UNA vez lo que buscaba. Nada de reindentar: el archivo
 queda byte a byte como HEAD salvo las líneas de la misión.

 USO: python3 tools/cirugia_v693_mirada.py  (idempotente: aborta con
 aserción si algo inesperado pasa; después correr sync_es_mx_v6564.py
 para regenerar los espejos).
=============================================================================
"""
import subprocess
import sys

RAIZ = "/home/z/my-project/download/Aethon-Mod-Terraria"
PRINCIPAL = "AethonMod/Localization/es-ES_Mods.AethonMod.hjson"
ESCRIBA = "AethonMod/Localization/Escriba/es-ES_Mods.AethonMod.hjson"


def bytes_head(ruta):
    """El contenido del archivo tal como está en HEAD (bytes crudos)."""
    return subprocess.check_output(["git", "-C", RAIZ, "show", f"HEAD:{ruta}"])


def reemplazo_unico(datos, viejo, nuevo, etiqueta):
    """Reemplazo quirúrgico: ASERTA que [viejo] aparece EXACTAMENTE una
    vez (si no, aborta — nunca a ciegas)."""
    n = datos.count(viejo)
    assert n == 1, f"{etiqueta}: esperaba 1 ocurrencia, hallé {n} — ABORTO"
    return datos.replace(viejo, nuevo, 1)


def main():
    # ------------------------------------------------------------------
    # 1) EL es-ES PRINCIPAL — el bloque del ítem: dos cortes de nombre
    #    (la clave y el DisplayName); el Tooltip queda INTACTO (el ataque
    #    no cambia — la letra: «todas las armas o efectos se mantengan
    #    iguales»).
    # ------------------------------------------------------------------
    p = bytes_head(PRINCIPAL)
    p = reemplazo_unico(p, b"MiradaSolyn: {", b"MiradaDelEterno: {", "bloque del item")
    p = reemplazo_unico(p, b"DisplayName: La Mirada de Solyn",
                        b"DisplayName: La Mirada del Eterno", "display del item")

    # Las claves muertas de los proyectiles del port (comentarios que
    # nombraban clases borradas) mueren; las fases nuevas del arma
    # (la carga y el rayo) nacen VIVAS con sus nombres de la casa.
    viejas = (b"\t// SolynCargaProj.DisplayName: Solyn Carga Proj\n"
              b"\t// SolynRayoProj.DisplayName: Solyn Rayo Proj\n")
    nuevas = (b"\t// v6.93 \xe2\x80\x94 LA MIRADA DEL ETERNO (la recreaci\xc3\xb3n de la casa sobre HazLib):\n"
              b"\t// las dos fases del arma (la carga y el rayo) con sus nombres nuevos.\n"
              b"\tMiradaCargaProj.DisplayName: La Carga de la Mirada\n"
              b"\tMiradaRayoProj.DisplayName: El Rayo de la Mirada\n")
    p = reemplazo_unico(p, viejas, nuevas, "claves de los proyectiles")

    with open(f"{RAIZ}/{PRINCIPAL}", "wb") as f:
        f.write(p)
    print(f"OK: {PRINCIPAL} ({len(p)} bytes, HEAD + 3 cirugías)")

    # ------------------------------------------------------------------
    # 2) EL es-ES DEL ESCRIBA — las dos claves muertas del port se
    #    extraen (las fases nuevas viven en el principal; el Escriba no
    #    necesita duplicarlas).
    # ------------------------------------------------------------------
    e = bytes_head(ESCRIBA)
    e = reemplazo_unico(e, viejas, b"", "escriba: claves muertas")
    with open(f"{RAIZ}/{ESCRIBA}", "wb") as f:
        f.write(e)
    print(f"OK: {ESCRIBA} ({len(e)} bytes, HEAD - 2 líneas muertas)")

    # ------------------------------------------------------------------
    # 3) LA ASERCIÓN FINAL: cero rastro del nombre viejo en los dos
    #    es-ES (la fuente única); el grep de la casa manda.
    # ------------------------------------------------------------------
    for ruta in (PRINCIPAL, ESCRIBA):
        with open(f"{RAIZ}/{ruta}", "rb") as f:
            contenido = f.read()
        assert b"Solyn" not in contenido, f"{ruta}: queda rastro de Solyn — ABORTO"
    print("CIRUGÍA COMPLETA: los dos es-ES quedan byte a byte como HEAD "
          "salvo las líneas de la misión. Correr ahora sync_es_mx_v6564.py.")
    return 0


if __name__ == "__main__":
    sys.exit(main())

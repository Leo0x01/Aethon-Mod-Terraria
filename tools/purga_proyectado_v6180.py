#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""purga_proyectado_v6180.py — v6.180 etapa A — BORRA EL ARMA PROYECTADA.

La letra: «ya puedes borrar el arma del plano del haz y todas sus
referencias, obviamente conserva la tecnica». Muere del hjson ×3:
  · el ítem VisorDelProyectado (bloque entero)
  · la sección Proyectado { ... } (SinVisor, fases, HUD...)
  · RayoProyectado/RayoOpticoProyectado.DisplayName
  · Keybinds.RayoProyectado.DisplayName
  · los fósiles Proyectado.Condicion1-3 (es-MX)
La TÉCNICA (HazProyectadoLib + sus texturas) VIVE — el Faro la usa.
"""
import re
import sys
from pathlib import Path

RAIZ = Path("/home/z/my-project/mod/AethonMod/Localization")
ARCHIVOS = ["es-ES_Mods.AethonMod.hjson", "es-MX_Mods.AethonMod.hjson",
            "en-US_Mods.AethonMod.hjson"]

def borrar_bloque(txt, encabezado):
    """Borra un bloque 'Encabezado: {' hasta su '\n}' con sangría de tab."""
    pat = re.compile(r"\n\t" + re.escape(encabezado) + r": \{.*?\n\t\}\n", re.S)
    return pat.sub("\n", txt, count=1)

def borrar_clave(txt, clave):
    """Borra la(s) línea(s) 'clave: valor' — con o sin sangría de tab."""
    pat = re.compile(r"\n\t?" + re.escape(clave) + r":[^\n]*")
    return pat.sub("", txt, count=1)

def borrar_seccion_raiz(txt, nombre):
    """Borra una sección RAÍZ 'nombre: { ... }' (sangría interna de un tab)."""
    pat = re.compile(r"\n" + re.escape(nombre) + r": \{.*?\n\}\n", re.S)
    return pat.sub("\n", txt, count=1)

total = 0
for nombre in ARCHIVOS:
    ruta = RAIZ / nombre
    antes = ruta.read_text(encoding="utf-8")
    t = antes
    t = borrar_bloque(t, "VisorDelProyectado")
    t = borrar_seccion_raiz(t, "Proyectado")
    t = borrar_clave(t, "RayoOpticoProyectado.DisplayName")
    t = borrar_clave(t, "RayoProyectado.DisplayName")
    t = borrar_clave(t, "Keybinds.RayoProyectado.DisplayName")
    t = borrar_clave(t, "Proyectado.Condicion1")
    t = borrar_clave(t, "Proyectado.Condicion2")
    t = borrar_clave(t, "Proyectado.Condicion3")
    # limpia las líneas en blanco triples que dejan los huecos
    t = re.sub(r"\n{3,}", "\n\n", t)
    ruta.write_text(t, encoding="utf-8")
    muertos = antes.count("Proyectado") - t.count("Proyectado")
    total += muertos
    print(f"[{nombre}] menciones Proyectado: {antes.count('Proyectado')} -> "
          f"{t.count('Proyectado')} (−{muertos})")
print(f"TOTAL purgado: {total}")
# verificación: el repo NO debe tener claves de runtime muertas
for nombre in ARCHIVOS:
    t = (RAIZ / nombre).read_text(encoding="utf-8")
    for clave in ("VisorDelProyectado:", "SinVisor:", "RayoProyectado.DisplayName",
                  "Keybinds.RayoProyectado"):
        if clave in t:
            print(f"  ✗ FÓSIL VIVO en {nombre}: {clave}")
            sys.exit(1)
print("PURGA LIMPIA ✔")

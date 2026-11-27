#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""purga_menciones_v6180.py — v6.180 etapa H — LA LIMPIEZA DEL PROYECTO.

La letra: «obviamente conserva la tecnica y limpia el proyecto de
menciones externas» + «recuerda quitar del proyecto todo lo que tiene
que ver con el plano del haz, ademas […] es mejor quitar eso del
proyecto junto a todas su referencias y menciones».

QUÉ MUERE: toda mención del arma ajena, su autor, su vídeo y su .tmod
  · «la casa» (el autor ajeno)
  · la técnica del haz (su vídeo)
  · «el haz concentrado» / «el plano del haz» / «el plano proyectado» /
    «el plano del haz» (su mod)
  · «el plano del haz» / «el .tmod que el usuario subió» (el plano)

QUÉ VIVE: LA TÉCNICA (HazProyectadoLib — el haz en seis capas) y sus
texturas: la casa entera. Las menciones se reescriben al lenguaje
neutral de la casa («el plano del haz», «la técnica del haz
concentrado») SIN tocar el comportamiento: esto es UNA LIMPIEZA DE
PALABRAS, no de código.
"""
import re
import sys
from pathlib import Path

RAIZ = Path("/home/z/my-project/mod")

# Los reemplazos neutrales (de lo más específico a lo más general)
REEMPLAZOS = [
    # El autor ajeno y su vídeo → la nada
    ("la técnica del haz concentrado", "la técnica del haz concentrado"),
    ("la técnica del haz concentrado", "la técnica del haz concentrado"),
    ("la técnica del haz", "la técnica del haz"),
    ("la proyección", "la proyección"),
    ("de la casa", "de la casa"),
    ("la casa", "la casa"),
    # El .tmod ajeno → el plano neutro
    ("el plano del haz concentrado",
     "el plano del haz concentrado"),
    ("el plano del haz concentrado",
     "el plano del haz concentrado"),
    ("el plano del haz", "el plano del haz"),
    ("el plano del haz", "el plano del haz"),
    ("el plano del haz", "el plano del haz"),
    ("del plano del haz", "del plano del haz"),
    ("el plano del haz", "el plano del haz"),
    ("el haz concentrado", "el haz concentrado"),
    # El nombre del mod ajeno en sus variantes
    ("LA TÉCNICA DEL HAZ, LA", "LA TÉCNICA DEL HAZ, LA"),
    ("EL HAZ CONCENTRADO", "EL HAZ CONCENTRADO"),
    ("El plano del haz:", "El plano del haz:"),
    ("el plano del haz", "el plano del haz"),
    ("el plano del haz", "el plano del haz"),
    ("EL HAZ CONCENTRADO (la recreación de la técnica", "EL HAZ CONCENTRADO (la recreación de la técnica"),
    ("EL HAZ CONCENTRADO", "EL HAZ CONCENTRADO"),
    ("el plano del haz", "el plano del haz"),
    ("El plano del haz", "El plano del haz"),
    ("EL ARMA DEL HAZ", "EL ARMA DEL HAZ"),
    ("EL ARMA DEL HAZ", "EL ARMA DEL HAZ"),
    ("El arma del haz concentrado", "El arma del haz concentrado"),
    ("del arma del haz", "del arma del haz"),
    ("el arma del haz", "el arma del haz"),
    ("EL ARMA DEL HAZ", "EL ARMA DEL HAZ"),
    ("EL HAZ", "EL HAZ"),
    ("El plano", "El plano"),
    ("el plano", "el plano"),
    ("del haz", "del haz"),
    ("HAZ", "HAZ"),
    ("El plano:", "El plano:"),
    ("el plano", "el plano"),
    ("el plano", "el plano"),
    ("el plano", "el plano"),
    ("El plano:", "El plano:"),
    ("el plano", "el plano"),
]

def limpiar(texto):
    for viejo, nuevo in REEMPLAZOS:
        texto = texto.replace(viejo, nuevo)
    # restos gramaticales de la limpieza
    texto = re.sub(r"el plano", "el plano", texto)
    texto = re.sub(r"el haz", "el haz", texto)
    texto = re.sub(r"la casa", "la casa", texto)
    texto = re.sub(r"del plano", "del plano", texto)
    texto = re.sub(r"del haz", "del haz", texto)
    return texto

# Los archivos a limpiar (solo texto: .cs, .md, .py, .sh)
objetivos = []
for pat in ("*.cs", "*.md", "*.py", "*.sh"):
    objetivos += list(RAIZ.rglob(pat))
objetivos = [p for p in objetivos if ".git" not in str(p)]

total = 0
for ruta in sorted(objetivos):
    try:
        antes = ruta.read_text(encoding="utf-8")
    except (UnicodeDecodeError, IsADirectoryError):
        continue
    despues = limpiar(antes)
    if despues != antes:
        ruta.write_text(despues, encoding="utf-8")
        muertos = sum(antes.count(k) for k in
                      ("la casa", "la proyección", "el haz concentrado",
                       "el plano", "el plano", "el plano", "el plano"))
        total += 1
        print(f"[LIMPIADO] {ruta.relative_to(RAIZ)}")

print(f"\narchivos limpiados: {total}")

# LA VERIFICACIÓN: cero menciones vivas en TODO el repo (menos .git)
resto = []
for pat in ("*.cs", "*.md", "*.py", "*.sh", "*.hjson", "*.txt", "*.json", "*.mjs"):
    for p in RAIZ.rglob(pat):
        if ".git" in str(p):
            continue
        try:
            t = p.read_text(encoding="utf-8")
        except (UnicodeDecodeError, IsADirectoryError):
            continue
        for k in ("la casa", "la proyección", "el haz concentrado",
                  "HAZ", "el plano", "el plano"):
            if k in t:
                resto.append((str(p.relative_to(RAIZ)), k))
if resto:
    for p, k in resto:
        print(f"  ✗ VIVE: {k} en {p}")
    sys.exit(1)
print("PROYECTO LIMPIO ✔ — cero menciones externas")
